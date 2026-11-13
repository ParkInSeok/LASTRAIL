import { z } from "zod";
import { canonicalJson, HealthRebaseState, sha256Hex, type HealthRebaseSnapshot } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { readOwnedCombatProfile } from "./combat-owned-profile";
import { captureCombatPolicyRequirements } from "./combat-policy-catalog";
import { captureCombatAuthority } from "./combat-authority";
import { captureOwnedCombatBaseDefinitions } from "./combat-owned-base-definitions";

const id = z.string().min(1).max(400), time = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const common = { eventId: id, actionId: z.string().regex(/^[\x21-\x7e]{1,100}$/u), atMs: time };
export const commandSchema = z.discriminatedUnion("kind", [
  z.strictObject({ ...common, kind: z.literal("OBSERVE_DAMAGE_HP"), actorId: id, hp: time }),
  z.strictObject({ ...common, kind: z.literal("CAST"), sourceId: id, abilityId: id }),
  z.strictObject({ ...common, kind: z.literal("RESOLVE") }),
  z.strictObject({ ...common, kind: z.literal("CANCEL") }),
]);
type Command = z.infer<typeof commandSchema>;
type Receipt = { eventId: string; actionId: string; status: "ACCEPTED" | "REJECTED" | "APPLIED" | "CANCELED";
  reason?: string; sourceId?: string; targetId?: string; effectAtMs?: number; effectEventId?: string;
  attemptedAmount?: number; actualAmount?: number; beforeHp?: number; afterHp?: number };
type Action = { actionId: string; sourceId: string; targetId: string; acceptedAtMs: number;
  effectAtMs: number; effectEventId: string; status: "PENDING" | "APPLIED" | "CANCELED" };
type State = { clockMs: number; cooldownUntilMs: number; busyUntilMs: number;
  health: { entityId: string; health: HealthRebaseSnapshot }[]; actions: Action[] };

async function prepareInput(db: D1Database, accountId: string, request: unknown, rawScope: unknown) {
  const scope = z.strictObject({ simulationId: z.string().regex(/^[A-Za-z0-9_-]{1,64}$/u),
    mode: z.enum(["ORDINARY_SOLO", "ORDINARY_COOP"]) }).safeParse(rawScope);
  if (!scope.success) throw new ActivityError(409, "combat_sera_scope_unsupported");
  const profile = await readOwnedCombatProfile(db, accountId, request), policy = captureCombatPolicyRequirements();
  if (profile.operators.length !== 5 || profile.operators.some(o => o.permanentStats === null)
    || !profile.operators.some(o => o.operatorId === "op_sera")) throw new ActivityError(409, "combat_sera_owned_input_missing");
  const ability = z.object({ id: z.literal("op_sera.active"), effect: z.literal("HEAL_ONE"),
    healBps: time, cooldownMs: time, windupMs: time, recoveryMs: time,
    target: z.literal("LIVING_INJURED_LOWEST_HP_RATIO_SLOT_ID_INCLUDING_SELF"),
    lockTargetAt: z.literal("ACCEPTANCE"), retarget: z.literal(false), overheal: z.literal("DISCARD") })
    .parse(policy.skills.find(s => s.id === "op_sera.active"));
  const passive = z.object({ operatorId: z.literal("op_sera"), source: z.literal("OWN_HEAL"),
    group: z.literal("HEAL_MAGNITUDE"), valueBps: time }).parse(policy.passives.find(p => p.operatorId === "op_sera"));
  const entities = profile.operators.map(o => ({ entityId: canonicalJson([scope.data.simulationId, accountId, o.operatorId]),
    definitionId: o.operatorId, ownerAccountId: accountId, role: "OPERATOR" as const,
    placement: { formationSlot: o.slot }, capability: "LIVING_OPERATOR_HEAL" as const,
    permanentStats: o.permanentStats!, skillMagnitude: o.skillMagnitude, sources: o.sources }));
  const packet = { version: "combat.sera-heal-input.v1", scope: scope.data,
    binding: "LOCAL_CANDIDATE_NOT_ATOMIC_START", canExecute: false,
    authority: captureCombatAuthority(scope.data.mode), profileHash: profile.contentHash, sourceHash: profile.sourceHash,
    definitionVersion: profile.definitionVersion, definitionHash: profile.definitionHash,
    policyVersion: policy.version, policyHash: profile.policyHash, supportedAbility: ability, passive, entities,
    unsupported: ["OTHER_ABILITIES", "RUN_STAT_MODIFIERS", "GENERAL_SHARED_ACTION_QUEUE", "PUBLIC_START_AND_CHECKPOINT_COMMIT"] };
  return { ...packet, inputHash: `sha256:${await sha256Hex(canonicalJson(packet))}` };
}
export type SeraHealInput = Awaited<ReturnType<typeof prepareInput>>;

/** Internal reference consumer for one unmodified HEAL_ONE path. Ordinary combat
 * remains UNITY_LOCAL/UNITY_HOST. This class never verifies rewards, mutates DB HP,
 * accepts a network hit report, enables START or replaces the general action owner.
 * OBSERVE_DAMAGE_HP is a handoff of an already resolved HP result from that logical
 * owner, not a damage calculator or public arbitrary-HP command.
 */
export class SeraHealConsumer {
  private state: State;
  private readonly events: { command: Command; receipt: Receipt }[] = [];
  private constructor(private readonly input: SeraHealInput) {
    const policy = captureCombatPolicyRequirements();
    if (input.version !== "combat.sera-heal-input.v1" || input.policyVersion !== policy.version
      || input.definitionVersion !== captureOwnedCombatBaseDefinitions().version
      || !["ORDINARY_SOLO", "ORDINARY_COOP"].includes(input.scope.mode)
      || input.entities.length !== 5 || new Set(input.entities.map(e => e.entityId)).size !== 5
      || new Set(input.entities.map(e => e.placement.formationSlot)).size !== 5
      || canonicalJson(input.authority) !== canonicalJson(captureCombatAuthority(input.scope.mode))) {
      throw new ActivityError(409, "combat_sera_input_invalid");
    }
    this.input = structuredClone(input);
    this.state = { clockMs: 0, cooldownUntilMs: 0, busyUntilMs: 0, actions: [],
      health: input.entities.map(e => ({ entityId: e.entityId, health: HealthRebaseState.create({
        policyVersion: "health.max-hp-rebase.v1", hp: e.permanentStats.maxHp, maxHp: e.permanentStats.maxHp }).snapshot() })) };
  }
  static async prepare(db: D1Database, accountId: string, request: unknown, scope: unknown) {
    return new SeraHealConsumer(await prepareInput(db, accountId, request, scope));
  }
  inputSnapshot(): SeraHealInput { return structuredClone(this.input); }
  health(entityId: string) {
    const row = this.state.health.find(h => h.entityId === entityId);
    if (!row) throw new ActivityError(409, "combat_sera_actor_unknown");
    return { ...row.health };
  }
  dispatch(raw: unknown) {
    const parsed = commandSchema.safeParse(raw);
    if (!parsed.success) throw new ActivityError(400, "combat_sera_command_invalid");
    const command = parsed.data, original = this.events.find(e => e.command.eventId === command.eventId);
    if (original) {
      if (canonicalJson(original.command) !== canonicalJson(command)) throw new ActivityError(409, "combat_sera_event_conflict");
      return { ...original.receipt, replayed: true };
    }
    const candidate = structuredClone(this.state);
    if (command.atMs < candidate.clockMs) throw new ActivityError(409, "combat_sera_clock_rewind");
    if (candidate.actions.some(a => a.status === "PENDING" && a.effectAtMs < command.atMs))
      throw new ActivityError(409, "combat_sera_effect_event_missing");
    if (this.events.length >= 10000) throw new ActivityError(409, "combat_sera_event_limit");
    candidate.clockMs = command.atMs;
    const receipt = this.apply(candidate, command);
    this.state = candidate; this.events.push({ command: structuredClone(command), receipt: structuredClone(receipt) });
    return { ...receipt, replayed: false };
  }
  private apply(state: State, c: Command): Receipt {
    const result = { eventId: c.eventId, actionId: c.actionId };
    const get = (entityId: string) => {
      const entity = this.input.entities.find(e => e.entityId === entityId), row = state.health.find(h => h.entityId === entityId);
      if (!entity || !row) throw new ActivityError(409, "combat_sera_actor_unknown");
      return { entity, row };
    };
    if (c.kind === "OBSERVE_DAMAGE_HP") {
      const { row } = get(c.actorId);
      if (c.hp > row.health.hp) throw new ActivityError(409, "combat_sera_damage_direction_invalid");
      const beforeHp = row.health.hp, health = HealthRebaseState.restore(row.health);
      health.recordActualHp(c.hp); row.health = health.snapshot();
      return { ...result, status: "APPLIED", targetId: c.actorId, beforeHp, afterHp: c.hp };
    }
    const source = this.input.entities.find(e => e.definitionId === "op_sera")!;
    const sourceHp = get(source.entityId).row.health;
    if (c.kind === "CAST") {
      if (c.abilityId !== "op_sera.active") throw new ActivityError(409, "combat_sera_effect_unsupported");
      if (c.sourceId !== source.entityId) throw new ActivityError(409, "combat_sera_source_mismatch");
      if (this.events.some(e => e.command.kind === "CAST" && e.command.actionId === c.actionId))
        throw new ActivityError(409, "combat_sera_action_conflict");
      if (sourceHp.hp === 0 || c.atMs < state.cooldownUntilMs || c.atMs < state.busyUntilMs)
        return { ...result, status: "REJECTED", reason: "SOURCE_NOT_READY" };
      const eligible = state.health.filter(h => h.health.hp > 0 && h.health.hp < h.health.maxHp).sort((a, b) => {
        const ratio = BigInt(a.health.hp) * BigInt(b.health.maxHp) - BigInt(b.health.hp) * BigInt(a.health.maxHp);
        return ratio < 0n ? -1 : ratio > 0n ? 1 : get(a.entityId).entity.placement.formationSlot - get(b.entityId).entity.placement.formationSlot
          || (a.entityId < b.entityId ? -1 : a.entityId > b.entityId ? 1 : 0);
      });
      if (!eligible.length) return { ...result, status: "REJECTED", reason: "NO_ELIGIBLE_TARGET" };
      const ability = this.input.supportedAbility;
      const effectAtMs = time.parse(c.atMs + ability.windupMs);
      state.cooldownUntilMs = time.parse(c.atMs + ability.cooldownMs);
      state.busyUntilMs = time.parse(effectAtMs + ability.recoveryMs);
      const effectEventId = canonicalJson([this.input.scope.simulationId, c.actionId, "op_sera.active", "HEAL_ONE"]);
      state.actions.push({ actionId: c.actionId, sourceId: source.entityId, targetId: eligible[0]!.entityId,
        acceptedAtMs: c.atMs, effectAtMs, effectEventId, status: "PENDING" });
      return { ...result, status: "ACCEPTED", sourceId: source.entityId, targetId: eligible[0]!.entityId, effectAtMs, effectEventId };
    }
    const action = state.actions.find(a => a.actionId === c.actionId);
    if (!action || action.status !== "PENDING") throw new ActivityError(409, "combat_sera_action_conflict");
    if (c.kind === "CANCEL") {
      action.status = "CANCELED";
      return { ...result, status: "CANCELED", reason: "ACTION_INTERRUPTED", sourceId: action.sourceId, targetId: action.targetId };
    }
    if (c.eventId !== action.effectEventId || c.atMs !== action.effectAtMs) throw new ActivityError(409, "combat_sera_effect_tick_mismatch");
    const { row } = get(action.targetId);
    if (row.health.hp === 0 || sourceHp.hp === 0) {
      action.status = "CANCELED";
      return { ...result, status: "CANCELED", reason: row.health.hp === 0 ? "LOCKED_TARGET_DOWN" : "CASTER_DOWN",
        sourceId: action.sourceId, targetId: action.targetId };
    }
    const atk = source.permanentStats.attack, rank = source.skillMagnitude;
    const amount = time.parse(Number(BigInt(atk.numerator) * BigInt(this.input.supportedAbility.healBps)
      * BigInt(rank.numerator) * BigInt(10000 + this.input.passive.valueBps)
      / (BigInt(atk.denominator) * 10000n * BigInt(rank.denominator) * 10000n)));
    const beforeHp = row.health.hp, afterHp = Math.min(row.health.maxHp, beforeHp + amount);
    const native = HealthRebaseState.restore(row.health);
    native.recordActualHp(afterHp); row.health = native.snapshot(); action.status = "APPLIED";
    return { ...result, status: "APPLIED", sourceId: action.sourceId, targetId: action.targetId,
      attemptedAmount: amount, actualAmount: afterHp - beforeHp, beforeHp, afterHp };
  }
  snapshot() { return { version: "combat.sera-heal-state.v1", inputHash: this.input.inputHash,
    inputCanonicalJson: canonicalJson(this.input),
    state: structuredClone(this.state), events: this.events.map(e => structuredClone(e)) }; }
  /** Expected input is the ORIGINAL trusted producer packet, never a fresh account read. */
  static restore(expectedInput: SeraHealInput, raw: unknown) {
    try {
      const checkpoint = z.strictObject({ version: z.literal("combat.sera-heal-state.v1"), inputHash: id,
        inputCanonicalJson: z.string().min(1).max(100000),
        state: z.unknown(), events: z.array(z.strictObject({ command: commandSchema, receipt: z.unknown() })).max(10000) }).parse(raw);
      if (checkpoint.inputHash !== expectedInput.inputHash || checkpoint.inputCanonicalJson !== canonicalJson(expectedInput))
        throw new Error("input mismatch");
      const restored = new SeraHealConsumer(expectedInput);
      for (const entry of checkpoint.events) {
        const result = restored.dispatch(entry.command), { replayed, ...receipt } = result;
        if (replayed || canonicalJson(receipt) !== canonicalJson(entry.receipt as Parameters<typeof canonicalJson>[0])) throw new Error("event mismatch");
      }
      if (canonicalJson(restored.snapshot()) !== canonicalJson(checkpoint as Parameters<typeof canonicalJson>[0])) throw new Error("state mismatch");
      return restored;
    } catch { throw new ActivityError(409, "combat_sera_state_invalid"); }
  }
}
export const prepareSeraHealConsumer = SeraHealConsumer.prepare;
