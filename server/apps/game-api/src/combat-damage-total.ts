import { z } from "zod";
import { ExactCombatValue, type ExactCombatSnapshot } from "./combat-exact-value";
export const combatDamageTotalVersion = "combat.damage-total.v1";
const MAX_RECEIPTS = 100000;
const id = z.string().min(1).max(200);
const hitSchema = z.strictObject({ sourceOwnerId: id, actorId: id, actionId: id,
  hitOrdinal: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER), amount: z.unknown() });
type HitReceipt = Omit<z.infer<typeof hitSchema>, "amount"> & { amount: ExactCombatSnapshot };
const key = (r: HitReceipt) => JSON.stringify([r.sourceOwnerId, r.actorId, r.actionId, r.hitOrdinal]);
const equal = (a: ExactCombatSnapshot, b: ExactCombatSnapshot) => a.numerator === b.numerator && a.denominator === b.denominator;
/** Exact total of already-authorized hits. The host owns deadline/eligibility checks and
 * must persist this with its damage ledger in the same transaction. Not a reward ledger.
 */
export class CombatDamageTotal {
  private readonly receipts = new Map<string, HitReceipt>();
  private total = ExactCombatValue.from({ numerator: "0", denominator: "1" });
  static empty() { return new CombatDamageTotal(); }
  static restore(input: unknown) {
    const s = z.strictObject({ version: z.literal(combatDamageTotalVersion), total: z.unknown(),
      receipts: z.array(hitSchema).max(MAX_RECEIPTS) }).parse(input);
    const restored = CombatDamageTotal.empty();
    for (const receipt of s.receipts) {
      if (restored.record(receipt).replayed) throw new Error("combat_damage_duplicate_receipt");
    }
    if (!equal(restored.total.snapshot(), ExactCombatValue.from(s.total).snapshot())) throw new Error("combat_damage_total_mismatch");
    return restored;
  }
  record(input: unknown) {
    const parsed = hitSchema.parse(input), amount = ExactCombatValue.from(parsed.amount);
    const receipt: HitReceipt = { ...parsed, amount: amount.snapshot() }, identity = key(receipt);
    const previous = this.receipts.get(identity);
    if (previous) {
      if (!equal(previous.amount, receipt.amount)) throw new Error("combat_damage_receipt_conflict");
      return { replayed: true };
    }
    if (this.receipts.size >= MAX_RECEIPTS) throw new Error("combat_damage_receipt_limit");
    const next = this.total.add(amount); // Validate before either state mutation.
    this.receipts.set(identity, receipt);
    this.total = next;
    return { replayed: false };
  }
  get score() { return this.total.floor(); }
  snapshot() {
    return { version: combatDamageTotalVersion, total: this.total.snapshot(),
      receipts: [...this.receipts.values()].map(r => ({ ...r, amount: { ...r.amount } })) };
  }
}
