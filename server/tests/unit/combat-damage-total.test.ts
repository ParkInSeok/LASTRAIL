import { expect, it } from "vitest";
import { CombatDamageTotal } from "../../apps/game-api/src/combat-damage-total";
const hit = (ordinal: number, numerator = "3", denominator = "5") => ({ sourceOwnerId: "operator-owner", actorId: "eve", actionId: "action", hitOrdinal: ordinal, amount: { numerator, denominator } });
it("accumulates distinct hits exactly and does not count repeated boss windows twice", () => {
  const total = CombatDamageTotal.empty();
  expect(total.record(hit(0)).replayed).toBe(false);
  expect(total.record(hit(0)).replayed).toBe(true);
  total.record(hit(1));
  expect(total.snapshot().total).toEqual({ numerator: "6", denominator: "5" });
  expect(total.score).toBe("1");
});
it("restores receipts and rejects conflicting retransmission without changing state", () => {
  const total = CombatDamageTotal.empty(); total.record(hit(0));
  const restored = CombatDamageTotal.restore(JSON.parse(JSON.stringify(total.snapshot())));
  expect(restored.record(hit(0)).replayed).toBe(true);
  const before = restored.snapshot();
  expect(() => restored.record(hit(0, "4"))).toThrow("combat_damage_receipt_conflict");
  expect(restored.snapshot()).toEqual(before);
});
it("keeps zero receipts and isolates owner/actor/action/hit identities", () => {
  const total = CombatDamageTotal.empty(); total.record(hit(0, "0"));
  expect(total.record(hit(0, "0")).replayed).toBe(true);
  total.record({ ...hit(0), sourceOwnerId: "another-owner" });
  total.record({ ...hit(0), actorId: "another-actor" });
  expect(total.score).toBe("1");
  const snapshot = total.snapshot();snapshot.receipts.length = 0;
  expect(total.snapshot().receipts).toHaveLength(3);
});
it("rejects corrupted totals, incompatible versions and duplicate restored receipts", () => {
  const total = CombatDamageTotal.empty();total.record(hit(0));const s = total.snapshot();
  expect(() => CombatDamageTotal.restore({...s,total:{numerator:"99",denominator:"1"}})).toThrow();
  expect(() => CombatDamageTotal.restore({...s,version:"old"})).toThrow();
  expect(() => CombatDamageTotal.restore({...s,receipts:[...s.receipts,...s.receipts]})).toThrow();
});
it("does not commit a hit when numeric accumulation overflows", () => {
  const total = CombatDamageTotal.empty(); total.record(hit(0,"9".repeat(256),"1"));const before=total.snapshot();
  expect(() => total.record(hit(1,"9".repeat(256),"1"))).toThrow("combat_numeric_range_exceeded");
  expect(total.snapshot()).toEqual(before);
});
