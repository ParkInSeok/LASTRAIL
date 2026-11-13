import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { id, nat, pos, bps, sha, templateSchema } from "./ce03-local-schema";
import { resolveCarriageType } from "./train-loadout";
import { createRailArtilleryConfig, railArtillerySchema } from "./ce03-rail-artillery";

const stat = z.enum(["SQUAD_DAMAGE", "TRAIN_DAMAGE", "SHIELD_MAGNITUDE"]);
const bonuses = z.strictObject({ SQUAD_DAMAGE: nat, TRAIN_DAMAGE: nat, SHIELD_MAGNITUDE: nat });
const effect = z.strictObject({ kind: z.literal("ADD_BPS"), stat, valueBps: bps });
// Additive local catalog approved for second-final branch implementation.
// Contributions are added to existing package bonuses, then the existing engine
// caps and final-floor damage/shield calculation apply. No grants or permanent stats.
export const railAugmentCatalog = [
  { id: "rail_aug_squad_5", label: "분대 화력", description: "이번 운행 요원 피해 보너스 +5%p (기존 상한 적용)", effect: { kind: "ADD_BPS", stat: "SQUAD_DAMAGE", valueBps: 500 } },
  { id: "rail_aug_train_5", label: "열차 화력", description: "이번 운행 열차·포격 객차 피해 보너스 +5%p (기존 상한 적용)", effect: { kind: "ADD_BPS", stat: "TRAIN_DAMAGE", valueBps: 500 } },
  { id: "rail_aug_shield_5", label: "방어막 증폭", description: "이번 운행 방어막 발급량 보너스 +5%p (기존 상한 적용)", effect: { kind: "ADD_BPS", stat: "SHIELD_MAGNITUDE", valueBps: 500 } }
] as const;
const option = z.strictObject({ id, routeId: id, label: z.string().min(1).max(100), description: z.string().min(1).max(300), augmentId: id, effect });
const carriage = z.strictObject({ id, slot: nat.max(3), type: z.enum(["Combat", "Gun", "Cargo", "Defense", "Medical"]) });
export const railJourneySchema = z.strictObject({ version: z.literal("rail-journey-v1"), catalogVersion: z.literal("rail-augment-catalog-v1"),
  selectionScaleBps: z.literal(100), recoveryMs: z.literal(600), baseSpeedMillimetersPerSecond: pos.max(1000000),
  throttleMinimumBps: z.literal(7000), throttleCruiseBps: z.literal(10000), throttleMaximumBps: z.literal(13000), timeoutMs: z.null(), totalWaves: z.literal(15),
  baseBonusesBps: bonuses, capsBps: bonuses,
  offers: z.array(z.strictObject({ id, afterCompletedWaves: pos.max(14), options: z.array(option).length(2) })).min(1).max(14),
  carriages: z.array(carriage).min(2).max(4), artillery: railArtillerySchema
}).superRefine((c, ctx) => {
  const names = new Set<string>();
  for (const [i, o] of c.offers.entries()) {
    if (names.has(o.id) || i > 0 && o.afterCompletedWaves <= c.offers[i - 1]!.afterCompletedWaves)
      ctx.addIssue({ code: "custom", message: "ordered distinct offers required" });
    names.add(o.id);
    if (new Set(o.options.map(x => x.augmentId)).size !== 2) ctx.addIssue({ code: "custom", message: "distinct choices required" });
    for (const choice of o.options) {
      const authored = railAugmentCatalog.find(a => a.id === choice.augmentId);
      if (!authored || canonicalJson(authored.effect) !== canonicalJson(choice.effect) || names.has(choice.id) || names.has(choice.routeId))
        ctx.addIssue({ code: "custom", message: "authored effect and unique choice identity required" });
      names.add(choice.id); names.add(choice.routeId);
    }
  }
  for (const key of stat.options) if (c.baseBonusesBps[key] > c.capsBps[key] || c.capsBps[key] > 10000)
    ctx.addIssue({ code: "custom", message: "bonus caps invalid" });
  if (new Set(c.carriages.map(x => x.id)).size !== c.carriages.length || c.carriages.some((x, i) => x.slot !== i || resolveCarriageType(x.id) !== x.type.toUpperCase()))
    ctx.addIssue({ code: "custom", message: "ordered distinct carriages required" });
});
export type RailJourneyConfig = z.infer<typeof railJourneySchema>;
export const railOptionsSchema = z.strictObject({ version: z.literal("rail-journey-v1"), baseSpeedMillimetersPerSecond: pos.max(1000000),
  afterCompletedWaves: z.array(pos.max(14)).min(1).max(14).optional() });
export type RailJourneyOptions = z.infer<typeof railOptionsSchema>;

export function createRailJourneyConfig(templateInput: unknown, optionsInput: unknown, verifiedCarriages?: unknown): RailJourneyConfig {
  const template = templateSchema.parse(templateInput), options = railOptionsSchema.parse(optionsInput);
  if (template.execution.activePolicyId !== "ce03.general-active-auto-v1") throw new Error("rail_requires_general_auto_profile");
  const value = (filter: string) => {
    const found = template.package.augments.filter(a => a.eventFilter === filter);
    if (found.length !== 1) throw new Error("rail_base_package_invalid");
    return found[0]!.valueBps;
  };
  const pairs = [[0, 1], [2, 0], [1, 2], [0, 2]];
  return railJourneySchema.parse({ version: options.version, catalogVersion: "rail-augment-catalog-v1", selectionScaleBps: 100, recoveryMs: 600,
    baseSpeedMillimetersPerSecond: options.baseSpeedMillimetersPerSecond, throttleMinimumBps: 7000, throttleCruiseBps: 10000, throttleMaximumBps: 13000, timeoutMs: null, totalWaves: 15,
    baseBonusesBps: { SQUAD_DAMAGE: value("OPERATOR_DAMAGE"), TRAIN_DAMAGE: value("LOCOMOTIVE_GUN_DAMAGE_INCLUDING_SUPPORT_EXCLUDING_COMBAT"), SHIELD_MAGNITUDE: value("SHIELD_ISSUED") },
    capsBps: { SQUAD_DAMAGE: template.rules.outgoingBuffCapBps, TRAIN_DAMAGE: template.rules.outgoingBuffCapBps, SHIELD_MAGNITUDE: template.rules.magnitudeBonusCapBps },
    carriages: verifiedCarriages ?? template.cars.map(c => ({ id: c.type === "Combat" ? "car_combat" : "car_gun", slot: c.slot, type: c.type })),
    artillery: createRailArtilleryConfig(options.baseSpeedMillimetersPerSecond),
    offers: (options.afterCompletedWaves ?? [3, 6, 9, 12]).map((wave, index) => ({ id: `rail_offer_${wave}`, afterCompletedWaves: wave,
      options: pairs[index % pairs.length]!.map((n, side) => { const a = railAugmentCatalog[n]!; return {
        id: `rail_option_${wave}_${side === 0 ? "left" : "right"}`, routeId: `rail_route_${wave}_${side === 0 ? "left" : "right"}`,
        label: a.label, description: a.description, augmentId: a.id, effect: a.effect }; }) })) });
}

export async function createRailPreparation<T extends Parameters<typeof canonicalJson>[0] & { draft: { template: Parameters<typeof canonicalJson>[0] } }>(basePreparation: T, options: RailJourneyOptions,
  selectedCarriageOwnership?: { rulesVersion: string; loadoutRevision: number; carriages: { carriageId: string; slot: number; type: string; level: number; revision: number }[] }) {
  const railJourney = createRailJourneyConfig(basePreparation.draft.template, options,
    selectedCarriageOwnership?.carriages.map(c => ({ id: c.carriageId, slot: c.slot, type: c.type })));
  const railJourneySha256 = await sha256Hex(canonicalJson(railJourney));
  const payload = { version: "ce03-rail-preparation-v1" as const, ready: false as const, basePreparation, railJourney, railJourneySha256,
    domainProjection: "LEGACY_COMBAT_GUN_ONLY" as const, ...(selectedCarriageOwnership === undefined ? {} : { selectedCarriageOwnership }) };
  return { ...payload, preparationSha256: await sha256Hex(canonicalJson(payload)) };
}

const eventBase = { sequence: pos, tick: nat, completedWaves: nat.max(15) };
export const railControlEventSchema = z.discriminatedUnion("kind", [
  z.strictObject({ ...eventBase, kind: z.literal("OPEN"), offerId: id }),
  z.strictObject({ ...eventBase, kind: z.literal("PREVIEW"), offerId: id, optionId: id }),
  z.strictObject({ ...eventBase, kind: z.literal("CONFIRM"), offerId: id, optionId: id }),
  z.strictObject({ ...eventBase, kind: z.literal("THROTTLE"), throttleBps: nat.max(13000) })
]);
type RailControlEvent = z.infer<typeof railControlEventSchema>;
const journalSchema = z.array(railControlEventSchema).max(10000);
const confirmedSchema = z.strictObject({ offerId: id, optionId: id, routeId: id, augmentId: id, sequence: pos, tick: nat });
const stateSchema = z.strictObject({ version: z.literal("rail-control-state-v1"), owner: id, sessionId: id, configSha256: sha,
  nextSequence: pos, tick: nat, completedWaves: nat.max(15), throttleBps: nat.max(13000), activeOfferId: id.nullable(), previewOptionId: id.nullable(),
  confirmed: z.array(confirmedSchema).max(14), bonusBps: bonuses, journal: journalSchema });
export type RailControlState = z.infer<typeof stateSchema>;
const requireRail = (condition: boolean, reason: string) => { if (!condition) throw new Error(reason); };

export async function createRailState(configInput: unknown, owner: string, sessionId: string): Promise<RailControlState> {
  const c = railJourneySchema.parse(configInput);
  return { version: "rail-control-state-v1", owner: id.parse(owner), sessionId: id.parse(sessionId), configSha256: await sha256Hex(canonicalJson(c)),
    nextSequence: 1, tick: 0, completedWaves: 0, throttleBps: c.throttleCruiseBps, activeOfferId: null, previewOptionId: null,
    confirmed: [], bonusBps: { SQUAD_DAMAGE: 0, TRAIN_DAMAGE: 0, SHIELD_MAGNITUDE: 0 }, journal: [] };
}

function transition(c: RailJourneyConfig, previous: RailControlState, e: RailControlEvent): RailControlState {
  requireRail(previous.journal.length < 10000 && e.sequence === previous.nextSequence, "rail_sequence_conflict");
  requireRail(e.tick >= previous.tick && e.completedWaves >= previous.completedWaves && e.completedWaves < c.totalWaves, "rail_progress_invalid");
  const next = c.offers[previous.confirmed.length];
  // No crossing an unresolved node. Opening happens at the configured completed
  // wave, and selection has no timeout. The combat owner supplies trusted ticks.
  if (next) requireRail(e.completedWaves <= next.afterCompletedWaves, "rail_offer_skipped");
  const s = structuredClone(previous);
  if (e.kind === "THROTTLE") {
    requireRail(e.throttleBps >= c.throttleMinimumBps && e.throttleBps <= c.throttleMaximumBps, "rail_throttle_invalid");
    s.throttleBps = e.throttleBps;
  } else {
    requireRail(next !== undefined && next.id === e.offerId && e.completedWaves === next.afterCompletedWaves, "rail_offer_not_due");
    if (e.kind === "OPEN") {
      requireRail(s.activeOfferId === null, "rail_offer_already_open");
      s.activeOfferId = e.offerId;
    } else {
      requireRail(s.activeOfferId === e.offerId, "rail_offer_not_open");
      const selected = next!.options.find(o => o.id === e.optionId);
      requireRail(selected !== undefined, "rail_option_invalid");
      if (e.kind === "PREVIEW") s.previewOptionId = e.optionId;
      else {
        const choice = selected!;
        s.bonusBps[choice.effect.stat] += choice.effect.valueBps;
        s.confirmed.push({ offerId: e.offerId, optionId: choice.id, routeId: choice.routeId, augmentId: choice.augmentId, sequence: e.sequence, tick: e.tick });
        s.activeOfferId = null; s.previewOptionId = null;
      }
    }
  }
  s.tick = e.tick; s.completedWaves = e.completedWaves; s.nextSequence++;
  s.journal.push(e);
  return stateSchema.parse(s);
}

export async function restoreRailState(configInput: unknown, owner: string, sessionId: string, stateInput: unknown): Promise<RailControlState> {
  const c = railJourneySchema.parse(configInput), stored = stateSchema.parse(stateInput);
  let replayed = await createRailState(c, owner, sessionId);
  requireRail(stored.owner === owner && stored.sessionId === sessionId && stored.configSha256 === replayed.configSha256, "rail_checkpoint_identity_mismatch");
  for (const e of stored.journal) replayed = transition(c, replayed, e);
  requireRail(canonicalJson(replayed) === canonicalJson(stored), "rail_checkpoint_replay_mismatch");
  return replayed;
}

/** Pure transition: caller atomically persists the whole result with its combat
 * checkpoint before publishing/applying the branch delta. Never mutate inputs. */
export async function applyRailControl(configInput: unknown, stateInput: unknown, eventInput: unknown): Promise<RailControlState> {
  const c = railJourneySchema.parse(configInput), s = stateSchema.parse(stateInput);
  const restored = await restoreRailState(c, s.owner, s.sessionId, s);
  return transition(c, restored, railControlEventSchema.parse(eventInput));
}

export function effectiveRailBonuses(configInput: unknown, bonusInput: unknown) {
  const c = railJourneySchema.parse(configInput), added = bonuses.parse(bonusInput);
  return Object.fromEntries(stat.options.map(key => [key, Math.min(c.capsBps[key], c.baseBonusesBps[key] + added[key])]));
}

const reportSchema = z.strictObject({ version: z.literal("ce03-rail-control-report-v1"), owner: id, sessionId: id, configSha256: sha,
  modelSchema: z.literal("ce03-rail-headless-state-v1"), modelSha256: sha, finalTick: nat, completedWaves: nat.max(15),
  journal: journalSchema, bonusBps: bonuses });
const reportPinsSchema = z.strictObject({ owner: id, sessionId: id, configSha256: sha, modelSha256: sha,
  finalTick: nat, completedWaves: nat.max(15), journalSha256: sha });

/** Pins must come from the trusted local executed checkpoint/report adapter,
 * never the same untrusted report or HTTP caller. This verifies control replay,
 * not distance/time integration, combat execution or admission. */
export async function verifyRailControlReport(configInput: unknown, reportInput: unknown, trustedPins: unknown) {
  const c = railJourneySchema.parse(configInput), r = reportSchema.parse(reportInput), pins = reportPinsSchema.parse(trustedPins);
  let replayed = await createRailState(c, pins.owner, pins.sessionId);
  requireRail(pins.configSha256 === replayed.configSha256 && r.configSha256 === pins.configSha256 &&
    r.owner === pins.owner && r.sessionId === pins.sessionId && r.modelSha256 === pins.modelSha256 &&
    r.finalTick === pins.finalTick && r.completedWaves === pins.completedWaves &&
    await sha256Hex(canonicalJson(r.journal)) === pins.journalSha256, "rail_report_identity_mismatch");
  for (const e of r.journal) replayed = transition(c, replayed, e);
  requireRail(replayed.tick <= r.finalTick && replayed.completedWaves <= r.completedWaves, "rail_report_progress_invalid");
  const requiredCount = c.offers.filter(o => o.afterCompletedWaves <= r.completedWaves).length;
  requireRail(replayed.confirmed.length === requiredCount && replayed.activeOfferId === null, "rail_report_selection_incomplete");
  requireRail(canonicalJson(r.bonusBps) === canonicalJson(replayed.bonusBps), "rail_report_bonus_mismatch");
  return { status: "CONTROL_REPLAY_VERIFIED" as const, ready: false as const, legacyFinishAllowed: false as const,
    confirmed: replayed.confirmed, effectiveBonusesBps: effectiveRailBonuses(c, replayed.bonusBps), journalSha256: pins.journalSha256 };
}

const modelPinsSchema = z.strictObject({ owner: id, sessionId: id, modelSha256: sha, configurationSha256: sha });
const modelControlSchema = z.object({ schema: z.literal("ce03-rail-headless-state-v1"), configurationSha256: sha,
  clock: z.object({ completed: nat }), completedWaves: nat.max(15),
  railJourney: z.object({ currentTick: nat, completedWaves: nat.max(15), journal: journalSchema,
    confirmed: z.array(z.strictObject({ offerId: id, optionId: id, routeId: id, augmentId: id, sequence: pos, atTick: nat, stat, valueBps: bps })).max(14) }) });

/** Binds control replay to exact bytes of an independently pinned executed model.
 * The original local session supplies configurationSha256 and modelSha256; HTTP
 * reports must never supply their own trusted pins. Other combat fields remain
 * the Unity model validator's responsibility, and no finish admission is enabled. */
export async function verifyRailModelControlReport(configInput: unknown, rawModel: string, reportInput: unknown, trustedPins: unknown) {
  const c = railJourneySchema.parse(configInput), pins = modelPinsSchema.parse(trustedPins);
  requireRail(await sha256Hex(rawModel) === pins.modelSha256, "rail_model_bytes_mismatch");
  const model = modelControlSchema.parse(JSON.parse(rawModel));
  requireRail(model.configurationSha256 === pins.configurationSha256 && model.railJourney.currentTick === model.clock.completed &&
    model.railJourney.completedWaves === model.completedWaves, "rail_model_identity_mismatch");
  const result = await verifyRailControlReport(c, reportInput, { owner: pins.owner, sessionId: pins.sessionId,
    configSha256: await sha256Hex(canonicalJson(c)), modelSha256: pins.modelSha256,
    finalTick: model.clock.completed, completedWaves: model.completedWaves, journalSha256: await sha256Hex(canonicalJson(model.railJourney.journal)) });
  const expected = result.confirmed.map(receipt => {
    const selected = c.offers.find(o => o.id === receipt.offerId)!.options.find(o => o.id === receipt.optionId)!;
    const { tick, ...rest } = receipt;
    return { ...rest, atTick: tick, stat: selected.effect.stat, valueBps: selected.effect.valueBps };
  });
  requireRail(canonicalJson(expected) === canonicalJson(model.railJourney.confirmed), "rail_model_receipt_mismatch");
  return result;
}
