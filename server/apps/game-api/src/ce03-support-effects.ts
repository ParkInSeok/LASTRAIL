import { z } from "zod";
import { canonicalJson } from "@lastrail/common";
import { bps, id, nat, pos, sha } from "./ce03-local-schema";
import { firepowerApplies, supportDefinitions, supportVersion, targetMaxHpAmount } from "./ce03-support-v2";
import { parseSupportProfileDraft } from "./ce03-support-profile";
import { restoreSupportState } from "./ce03-support-state";

const tick = nat.max(1_000_000_000);
const actorSchema = z.strictObject({ entityId: id, side: z.enum(["OPERATOR", "ENEMY", "TRAIN", "DEVICE"]), lane: z.enum(["INTERIOR", "EXTERIOR"]),
  hp: nat, maxHp: pos, atk: nat, armorBps: bps, outgoingBps: bps, protectedUntil: tick, returning: z.boolean(), controlImmune: z.boolean(),
  action: z.strictObject({ id, kind: z.enum(["BASIC", "SKILL", "CHANNEL"]), interruptible: z.boolean(), immune: z.boolean() }).nullable(),
}).refine(a => a.hp <= a.maxHp);
const statusSchema = z.strictObject({ sourceId: id, targetId: id, abilityId: id, effectIndex: nat,
  type: z.enum(["SMOKE", "MARK", "FIREPOWER", "SHIELD", "STUN"]), value: nat, expiryTick: tick });
const baseEvent = { key: id, sequence: pos, dueTick: tick, sourceId: id };
const supportEvent = z.strictObject({ ...baseEvent, type: z.literal("SUPPORT"), skillId: id.refine(s => supportDefinitions.some(d => d.id === s)),
  pins: z.array(z.strictObject({ entityId: id, actionId: id.nullable() })).min(1).max(4096).refine(p => new Set(p.map(x => x.entityId)).size === p.length) });
const damageEvent = z.strictObject({ ...baseEvent, type: z.literal("DAMAGE"), targetId: id, actionId: id.nullable(), committed: z.boolean(),
  category: z.enum(["TRAIN_BASIC", "COMBAT_DEVICE", "GUN_DEVICE", "OPERATOR", "SUPPORT", "MECHANIC_FIXED"]), amount: nat });
const eventSchema = z.discriminatedUnion("type", [supportEvent, damageEvent]);
const frameSchema = z.strictObject({ version: z.literal("ce03-support-effects-v2"), policyVersion: z.literal(supportVersion), profileSha256: sha,
  tick, actors: z.array(actorSchema).min(1).max(4096), statuses: z.array(statusSchema).max(10000),
  applied: z.array(z.strictObject({ key: id, sequence: pos, dueTick: tick, eventJson: z.string().max(1_000_000), outcome: z.enum(["APPLIED", "FIZZLED", "CANCELLED"]) })).max(10000),
}).superRefine((f, ctx) => {
  const fail = (message: string) => ctx.addIssue({ code: "custom", message });
  const ids = new Set(f.actors.map(a => a.entityId));
  if (ids.size !== f.actors.length || f.actors.filter(a => a.side === "TRAIN").length !== 1) fail("unique actors and one train required");
  if (new Set(f.applied.map(a => a.key)).size !== f.applied.length || new Set(f.applied.map(a => a.sequence)).size !== f.applied.length) fail("duplicate applied identity");
  if (new Set(f.statuses.map(statusKey)).size !== f.statuses.length || f.statuses.some(s => !ids.has(s.sourceId) || !ids.has(s.targetId))) fail("invalid status identity");
  for (const a of f.actors) if (f.statuses.filter(s => s.type === "SHIELD" && s.targetId === a.entityId).reduce((n, s) => n + BigInt(s.value), 0n) > BigInt(a.maxHp) * 4n / 10n) fail("shield cap exceeded");
  if (f.statuses.some(s => s.type !== "SHIELD" && s.value > 10000)) fail("invalid status magnitude");
  for (const [i, r] of f.applied.entries()) {
    const previous = f.applied[i - 1];
    if (r.dueTick > f.tick || (previous && (r.dueTick < previous.dueTick || (r.dueTick === previous.dueTick && r.sequence <= previous.sequence)))) fail("invalid applied order");
    try {
      const event = eventSchema.parse(JSON.parse(r.eventJson));
      if (canonicalJson(event) !== r.eventJson || event.key !== r.key || event.sequence !== r.sequence || event.dueTick !== r.dueTick) fail("applied event identity mismatch");
    } catch { fail("invalid applied event payload"); }
  }
});
export type SupportEffectFrame = z.infer<typeof frameSchema>;
type Actor = SupportEffectFrame["actors"][number];
type Status = SupportEffectFrame["statuses"][number];
function statusKey(s: Status) { return canonicalJson([s.sourceId, s.targetId, s.abilityId, s.effectIndex, s.type]); }
const ordinal = (a: string, b: string) => a < b ? -1 : a > b ? 1 : 0;
const safeNumber = (n: bigint) => { const value = Number(n); return nat.parse(value); };
const living = (a: Actor | undefined): a is Actor => !!a && a.hp > 0 && !a.returning;
function active(f: SupportEffectFrame, target: string, type: Status["type"]) { return f.statuses.filter(s => s.targetId === target && s.type === type && s.expiryTick > f.tick); }
function strongest(f: SupportEffectFrame, target: string, type: Status["type"]) { return active(f, target, type).reduce((value, s) => Math.max(value, s.value), 0); }
function upsert(f: SupportEffectFrame, status: Status) {
  const prior = f.statuses.find(s => statusKey(s) === statusKey(status));
  if (prior && prior.value > status.value) return;
  if (prior) f.statuses.splice(f.statuses.indexOf(prior), 1);
  f.statuses.push(status);
}
function shield(f: SupportEffectFrame, target: Actor, status: Status) {
  const others = active(f, target.entityId, "SHIELD").filter(s => statusKey(s) !== statusKey(status)).reduce((n, s) => n + BigInt(s.value), 0n);
  const room = BigInt(target.maxHp) * 4n / 10n - others;
  status.value = Math.min(status.value, safeNumber(room > 0n ? room : 0n));
  if (status.value > 0) upsert(f, status);
}
function damage(f: SupportEffectFrame, source: Actor, target: Actor, basis: bigint, denominator: bigint, category: z.infer<typeof damageEvent>["category"]) {
  if (!living(target) || target.protectedUntil > f.tick) return;
  let amount: bigint;
  if (category === "MECHANIC_FIXED") amount = basis / denominator;
  else {
    const train = f.actors.find(a => a.side === "TRAIN")!;
    const extra = firepowerApplies(category) ? active(f, train.entityId, "FIREPOWER").reduce((n, s) => n + BigInt(s.value), 0n) : 0n;
    const rawBonus = BigInt(source.outgoingBps) + extra;
    const outgoing = rawBonus < 3000n ? rawBonus : 3000n;
    const mark = BigInt(Math.min(2000, strongest(f, target.entityId, "MARK"))), armor = BigInt(Math.min(6000, target.armorBps));
    amount = basis * (10000n + outgoing) * (10000n + mark) * (10000n - armor) / (denominator * 10000n ** 3n);
    if (target.side === "OPERATOR") amount = amount * BigInt(10000 - Math.min(3000, strongest(f, target.entityId, "SMOKE"))) / 10000n;
  }
  const layers = active(f, target.entityId, "SHIELD").sort((a, b) => a.expiryTick - b.expiryTick || ordinal(a.sourceId, b.sourceId) || ordinal(a.abilityId, b.abilityId) || a.effectIndex - b.effectIndex);
  for (const layer of layers) { const used = amount < BigInt(layer.value) ? amount : BigInt(layer.value); layer.value -= safeNumber(used); amount -= used; }
  target.hp -= safeNumber(amount < BigInt(target.hp) ? amount : BigInt(target.hp));
}
export function createEffectFrame(profile: unknown, actors: unknown): SupportEffectFrame {
  return frameSchema.parse({ version: "ce03-support-effects-v2", policyVersion: supportVersion, profileSha256: profile, tick: 0, actors, statuses: [], applied: [] });
}
export function expireEffectFrame(input: unknown, nowInput: unknown): SupportEffectFrame {
  const f = frameSchema.parse(input), now = tick.parse(nowInput);
  if (now < f.tick) throw new Error("effect clock cannot rewind");
  f.tick = now; f.statuses = f.statuses.filter(s => s.expiryTick > now && (s.type !== "SHIELD" || s.value > 0));
  return f;
}
// All events in this call share the current simulation tick. Sequence belongs to
// the unified engine allocator, not the support-local receipt counter.
// Caller persists the returned whole checkpoint atomically; inputs are not mutated.
export function applySupportFrame(input: unknown, eventsInput: unknown): SupportEffectFrame {
  const f = expireEffectFrame(input, frameSchema.parse(input).tick);
  const events = z.array(eventSchema).max(10000).parse(eventsInput).sort((a, b) => a.dueTick - b.dueTick || a.sequence - b.sequence);
  for (const event of events) {
    const eventJson = canonicalJson(event), prior = f.applied.find(r => r.key === event.key);
    if (prior) { if (prior.eventJson !== eventJson) throw new Error("effect event key conflict"); continue; }
    const last = f.applied.at(-1);
    if (event.dueTick !== f.tick || f.applied.some(r => r.sequence === event.sequence) || (last?.dueTick === event.dueTick && last.sequence >= event.sequence)) throw new Error("invalid unified effect ordering");
    const source = f.actors.find(a => a.entityId === event.sourceId);
    if (!source) throw new Error("unknown effect source");
    let outcome: "APPLIED" | "FIZZLED" | "CANCELLED" = "FIZZLED";
    if (event.type === "DAMAGE") {
      const target = f.actors.find(a => a.entityId === event.targetId);
      if (!event.committed && (!living(source) || strongest(f, source.entityId, "STUN") > 0 || (event.actionId !== null && source.action?.id !== event.actionId))) outcome = "CANCELLED";
      else if (living(target)) { damage(f, source, target, BigInt(event.amount), 1n, event.category); outcome = "APPLIED"; }
    } else {
      if (source.side !== "TRAIN") throw new Error("support source must be train");
      const d = supportDefinitions.find(x => x.id === event.skillId)!;
      if ((d.role === "suppressive_barrage" && event.pins.length > 3) || (["weakness_scan", "electronic_disruption", "emergency_protection", "firepower_boost"].includes(d.role) && event.pins.length !== 1)) throw new Error("invalid support pin count");
      if (living(source)) for (const pin of event.pins) {
        const target = f.actors.find(a => a.entityId === pin.entityId);
        if (!living(target)) continue;
        const enemyRole = ["suppressive_barrage", "weakness_scan", "electronic_disruption"].includes(d.role);
        if ((enemyRole && target.side !== "ENEMY") || (!enemyRole && target.side !== (d.role === "firepower_boost" ? "TRAIN" : "OPERATOR"))) throw new Error("support target side mismatch");
        if (d.role === "suppressive_barrage" && target.lane !== "INTERIOR") throw new Error("barrage target lane mismatch");
        const status = (type: Status["type"], value: number, effectIndex = 0): Status => ({ sourceId: source.entityId, targetId: target.entityId, abilityId: d.id, effectIndex, type, value, expiryTick: f.tick + d.durationMs / 50 });
        if (d.role === "electronic_disruption") {
          if (pin.actionId === null || target.action?.id !== pin.actionId || target.action.kind === "BASIC" || !target.action.interruptible || target.action.immune) continue;
          target.action = null;
        } else if (pin.actionId !== null) throw new Error("unexpected action pin");
        else if (d.role === "medical_support") target.hp += targetMaxHpAmount(target.maxHp, target.hp, "HEAL");
        else if (d.role === "emergency_protection") shield(f, target, status("SHIELD", targetMaxHpAmount(target.maxHp, target.hp, "SHIELD")));
        else if (d.role === "smoke") upsert(f, status("SMOKE", 2000));
        else if (d.role === "weakness_scan") upsert(f, status("MARK", 1500));
        else if (d.role === "firepower_boost") upsert(f, status("FIREPOWER", 1500));
        else if (d.role === "suppressive_barrage") { damage(f, source, target, BigInt(source.atk) * 6n, 5n, "SUPPORT"); if (living(target) && !target.controlImmune) upsert(f, status("STUN", 1, 1)); }
        outcome = "APPLIED";
      }
    }
    f.applied.push({ key: event.key, sequence: event.sequence, dueTick: event.dueTick, eventJson, outcome });
  }
  return frameSchema.parse(f);
}

// Bind producer -> accepted support state -> effect checkpoint. Global mappings
// come from the trusted engine queue and must be persisted there at acceptance.
export async function consumeSupportOutbox(draftInput: unknown, stateInput: unknown, frameInput: unknown, mappingInput: unknown, damageInput: unknown): Promise<SupportEffectFrame> {
  const draft = await parseSupportProfileDraft(draftInput);
  const state = restoreSupportState(stateInput, draft.profileSha256, draft.selectedSupport), frame = frameSchema.parse(frameInput);
  if (frame.profileSha256 !== draft.profileSha256 || frame.tick !== state.tick) throw new Error("effect frame binding or clock mismatch");
  const mappings = z.array(z.strictObject({ commandKey: id, globalSequence: pos })).max(10000)
    .refine(m => new Set(m.map(x => x.commandKey)).size === m.length && new Set(m.map(x => x.globalSequence)).size === m.length).parse(mappingInput);
  const sourceId = frame.actors.find(a => a.side === "TRAIN")!.entityId;
  const events = state.outbox.map(intent => {
    const sequence = mappings.find(m => m.commandKey === intent.commandKey)?.globalSequence;
    if (sequence === undefined) throw new Error("missing unified sequence mapping");
    return { type: "SUPPORT" as const, key: intent.commandKey, sequence, dueTick: intent.dueTick, sourceId, skillId: intent.skillId, pins: intent.pins };
  });
  return applySupportFrame(frame, [...events, ...z.array(damageEvent).max(10000).parse(damageInput)]);
}
