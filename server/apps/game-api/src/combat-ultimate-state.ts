import { z } from "zod";

// F1/WB3 latest policy. Historical CE03 checkpoints retain their original schema.
// The run snapshot owner must commit this state with its combat event/command receipt.
// This module has no public mutation API and does not authorize client hit reports.
export const ultimateStateVersion = "combat.ultimate.2026-09-22.1";
const COST = 100, BASE_CAPACITY = 100, EXPANDED_CAPACITY = 200, BASIC_GAIN = 4;
const identity = z.string().min(1).max(200);
const chargeValue = z.number().int().min(0).max(EXPANDED_CAPACITY).multipleOf(BASIC_GAIN);
const hitSchema = z.strictObject({ actionId: identity, hitOrdinal: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),
  sourceActorId: identity, sourceAlive: z.boolean(), sourceKind: z.enum(["BASIC", "SKILL", "SUMMON", "DERIVED", "TRAIN", "DEVICE"]),
  mode: z.enum(["ORDINARY", "WORLD_BOSS"]), resolvedDamage: z.number().finite().min(0).max(Number.MAX_SAFE_INTEGER) });
const creditSchema = z.strictObject({ actionId: identity, hitOrdinal: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),
  gained: z.union([z.literal(0), z.literal(BASIC_GAIN)]), before: chargeValue, after: chargeValue })
  .refine(r => r.after === r.before + r.gained, "invalid credit receipt");
const castSchema = z.strictObject({ commandId: identity, accepted: z.boolean(), before: chargeValue, after: chargeValue })
  .refine(r => r.after === r.before - (r.accepted ? COST : 0), "invalid cast receipt");
export const snapshotSchema = z.strictObject({ version: z.literal(ultimateStateVersion), actorId: identity,
  capacity: z.union([z.literal(BASE_CAPACITY), z.literal(EXPANDED_CAPACITY)]), cost: z.literal(COST), charge: chargeValue,
  credits: z.array(creditSchema), casts: z.array(castSchema) }).superRefine((s, ctx) => {
    if (s.charge > s.capacity || s.credits.some(r => r.before > s.capacity || r.after > s.capacity)
      || s.casts.some(r => r.before > s.capacity || r.after > s.capacity))
      ctx.addIssue({ code: "custom", message: "charge exceeds capacity" });
    if (new Set(s.credits.map(r => r.actionId)).size !== s.credits.length || new Set(s.casts.map(r => r.commandId)).size !== s.casts.length)
      ctx.addIssue({ code: "custom", message: "duplicate receipt identity" });
    if (s.credits.reduce((sum, r) => sum + r.gained, 0) - s.casts.filter(r => r.accepted).length * COST !== s.charge)
      ctx.addIssue({ code: "custom", message: "charge does not reconcile with receipts" });
  });
export type UltimateSnapshot = z.infer<typeof snapshotSchema>;
type CreditReceipt = z.infer<typeof creditSchema>;
type CastReceipt = z.infer<typeof castSchema>;

/** One instance per operator per run, never a shared or account-level charge pool. */
export class CombatUltimateState {
  private readonly credits: Map<string, CreditReceipt>;
  private readonly casts: Map<string, CastReceipt>;
  private constructor(private readonly state: UltimateSnapshot) {
    this.credits = new Map(state.credits.map(r => [r.actionId, r]));
    this.casts = new Map(state.casts.map(r => [r.commandId, r]));
  }
  static start(actorId: string, expanded = false) {
    return CombatUltimateState.restore({ version: ultimateStateVersion, actorId, cost: COST,
      capacity: expanded ? EXPANDED_CAPACITY : BASE_CAPACITY, charge: 0, credits: [], casts: [] });
  }
  static restore(snapshot: unknown) { return new CombatUltimateState(snapshotSchema.parse(snapshot)); }
  get ready() { return this.state.charge >= COST; }
  expandCapacity() { this.state.capacity = EXPANDED_CAPACITY; }

  /** Call only after authoritative damage resolution. World damage is pre-score-floor. */
  credit(input: unknown): CreditReceipt & { replayed: boolean } {
    const hit = hitSchema.parse(input);
    // Foreign sources cannot collide with this actor's credited action identities.
    const ownBasic = hit.sourceActorId === this.state.actorId && hit.sourceKind === "BASIC";
    const original = ownBasic ? this.credits.get(hit.actionId) : undefined;
    if (original) return { ...original, replayed: true };
    const eligible = ownBasic && hit.sourceAlive
      && (hit.mode === "WORLD_BOSS" ? hit.resolvedDamage > 0 : hit.resolvedDamage >= 1);
    if (!eligible) return { actionId: hit.actionId, hitOrdinal: hit.hitOrdinal, gained: 0,
      before: this.state.charge, after: this.state.charge, replayed: false };
    const before = this.state.charge;
    const gained = Math.min(BASIC_GAIN, this.state.capacity - before) as 0 | 4;
    const receipt: CreditReceipt = { actionId: hit.actionId, hitOrdinal: hit.hitOrdinal, gained, before, after: before + gained };
    this.credits.set(hit.actionId, receipt); // Record capped hits too: expansion never retroactively credits them.
    this.state.charge = receipt.after;
    return { ...receipt, replayed: false };
  }

  /** Preflight is supplied by the actor action queue (alive, target, availability).
   * Replay precedes current preflight; post-acceptance cancellation has no refund transition.
   */
  cast(commandId: string, preflightEligible: boolean): CastReceipt & { replayed: boolean } {
    identity.parse(commandId);
    z.boolean().parse(preflightEligible);
    const original = this.casts.get(commandId);
    if (original) return { ...original, replayed: true };
    const before = this.state.charge, accepted = preflightEligible && this.ready;
    const receipt = { commandId, accepted, before, after: before - (accepted ? COST : 0) };
    this.casts.set(commandId, receipt);
    this.state.charge = receipt.after;
    return { ...receipt, replayed: false };
  }
  snapshot(): UltimateSnapshot {
    return { version: ultimateStateVersion, actorId: this.state.actorId, cost: COST,
      capacity: this.state.capacity, charge: this.state.charge,
      credits: [...this.credits.values()].map(r => ({ ...r })), casts: [...this.casts.values()].map(r => ({ ...r })) };
  }
}
