import { describe, expect, it } from "vitest";
import { CombatUltimateState, ultimateStateVersion } from "../../apps/game-api/src/combat-ultimate-state";

const basic = (actionId: string, overrides = {}) => ({ actionId, hitOrdinal: 0, sourceActorId: "eve", sourceAlive: true,
  sourceKind: "BASIC" as const, mode: "ORDINARY" as const, resolvedDamage: 1, ...overrides });
function charged(count: number, expanded = false) {
  const state = CombatUltimateState.start("eve", expanded);
  for (let i = 0; i < count; i++) state.credit(basic(`action-${i}`));
  return state;
}
describe("latest ultimate run state", () => {
  it("starts at zero and separates the expanded capacity from cost", () => {
    expect(CombatUltimateState.start("eve", true).snapshot()).toMatchObject({ version: ultimateStateVersion, charge: 0, capacity: 200, cost: 100 });
  });
  it.each([24, 25, 26, 50])("accepts exactly 100 from %i basic actions with capacity 200", count => {
    const state = charged(count, true);
    const before = count * 4;
    expect(state.ready).toBe(before >= 100);
    const result = state.cast("cast-1", true);
    expect(result.accepted).toBe(before >= 100);
    expect(state.snapshot().charge).toBe(before >= 100 ? before - 100 : before);
  });
  it("discards overflow and never grants charge on capacity expansion", () => {
    const state = charged(26);
    expect(state.snapshot().charge).toBe(100);
    state.expandCapacity();
    expect(state.snapshot().charge).toBe(100);
    state.credit(basic("action-25"));
    expect(state.snapshot().charge).toBe(100);
    state.credit(basic("new-action"));
    expect(state.snapshot().charge).toBe(104);
  });
  it("charges once per original action, including multi-hit and restored replay", () => {
    const state = CombatUltimateState.start("eve");
    state.credit(basic("burst", { resolvedDamage: 0 }));
    expect(state.credit(basic("burst", { hitOrdinal: 1 })).gained).toBe(4);
    const restored = CombatUltimateState.restore(state.snapshot());
    expect(restored.credit(basic("burst", { hitOrdinal: 2 })).replayed).toBe(true);
    expect(restored.snapshot().charge).toBe(4);
    expect(restored.snapshot().credits[0]).toMatchObject({ actionId: "burst", hitOrdinal: 1 });
  });
  it.each([{ sourceActorId: "partner" }, { sourceAlive: false }, { sourceKind: "SKILL" },
    { sourceKind: "SUMMON" }, { resolvedDamage: 0 }, { resolvedDamage: 0.6 }])("excludes ineligible ordinary sources %j", input => {
    const state = CombatUltimateState.start("eve");
    expect(state.credit(basic("x", input)).gained).toBe(0);
    expect(state.snapshot().charge).toBe(0);
  });
  it("allows positive fractional valid boss damage without converting it to ordinary HP loss", () => {
    const state = CombatUltimateState.start("eve");
    expect(state.credit(basic("x", { mode: "WORLD_BOSS", resolvedDamage: 0.6 })).gained).toBe(4);
  });
  it("replays acceptance after charge changes and restoration without a second spend", () => {
    const state = charged(50, true);
    const original = state.cast("accepted", true);
    state.cast("another", true);
    const restored = CombatUltimateState.restore(state.snapshot());
    expect(restored.cast("accepted", false)).toEqual({ ...original, replayed: true });
    expect(restored.snapshot().charge).toBe(0);
  });
  it("does not spend on rejected preflight and preserves its original rejection", () => {
    const state = charged(25);
    expect(state.cast("rejected", false).accepted).toBe(false);
    expect(state.cast("rejected", true)).toMatchObject({ accepted: false, replayed: true });
    expect(state.snapshot().charge).toBe(100);
    expect(state.cast("new", true).accepted).toBe(true);
  });
  it("replays credited actions before evaluating current liveness or damage", () => {
    const state = CombatUltimateState.start("eve");
    const original = state.credit(basic("x"));
    expect(state.credit(basic("x", { sourceAlive: false, resolvedDamage: 0 }))).toEqual({ ...original, replayed: true });
    expect(state.snapshot().charge).toBe(4);
  });
  it("rejects a forged balance even when the balance fits the capacity", () => {
    expect(() => CombatUltimateState.restore({ ...charged(1).snapshot(), charge: 8 })).toThrow();
  });
  it("retains accepted costs across cancellation represented by restoring the run", () => {
    const state = charged(25);
    state.cast("cast-cancelled-after-acceptance", true);
    expect(CombatUltimateState.restore(state.snapshot()).snapshot().charge).toBe(0);
  });
  it("does not expose mutable state through snapshots", () => {
    const state = charged(1);
    const snapshot = state.snapshot();
    snapshot.charge = 200;
    snapshot.credits.length = 0;
    expect(state.snapshot().charge).toBe(4);
    expect(state.snapshot().credits).toHaveLength(1);
  });
  it.each([{ version: "ce03.per-operator-ultimate-v1" }, { charge: 101 }, { cost: 200 }, { capacity: 104 }])("rejects incompatible or corrupt snapshots %j", change => {
    expect(() => CombatUltimateState.restore({ ...charged(1).snapshot(), ...change })).toThrow();
  });
  it("rejects duplicate receipt identities in a restored snapshot", () => {
    const snapshot = charged(1).snapshot();
    snapshot.credits.push(snapshot.credits[0]!);
    expect(() => CombatUltimateState.restore(snapshot)).toThrow();
  });
  it.each([NaN, Infinity, -1])("rejects invalid resolved damage %s at the boundary", resolvedDamage => {
    expect(() => CombatUltimateState.start("eve").credit(basic("x", { resolvedDamage }))).toThrow();
  });
});
