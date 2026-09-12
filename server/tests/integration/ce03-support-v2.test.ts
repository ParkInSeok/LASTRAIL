import { expect, it } from "vitest";
import { pinSupportTargets, targetMaxHpAmount, resolveSupportThree, validSupportPins, firepowerApplies } from "../../apps/game-api/src/ce03-support-v2";
import { ce03ProducerFixture } from "../helpers/ce03-producer-fixture";
const actor = (entityId: string, hp = 50, slot = 0) => ({ entityId, hp, maxHp: 100, slot, spawnOrder: slot, side: "OPERATOR", lane: "INTERIOR", returning: false, action: null });
const enemy = (entityId: string, order: number, lane = "INTERIOR") => ({ ...actor(entityId, 100, order), side: "ENEMY", lane });
it("computes target HP amounts exactly and clamps healing without unsafe multiplication", () => {
  expect(targetMaxHpAmount(1000, 950, "HEAL")).toBe(50);
  expect(targetMaxHpAmount(1000, 100, "SHIELD")).toBe(250);
  expect(targetMaxHpAmount(Number.MAX_SAFE_INTEGER, 0, "SHIELD")).toBe(2251799813685247);
  expect(() => targetMaxHpAmount(100, 101, "HEAL")).toThrow();
});
it("pins interior barrage focus first and never pulls exterior enemies", () => {
  const actors = [enemy("a", 0), enemy("b", 1), enemy("c", 2), enemy("d", 3), enemy("outside", 4, "EXTERIOR")];
  expect(pinSupportTargets("suppressive_barrage", actors, "d", null).map(x => x.entityId)).toEqual(["d", "a", "b"]);
});
it("heals living operators including return protection but rejects all full", () => {
  expect(pinSupportTargets("medical_support", [actor("a"), { ...actor("b"), returning: true }, actor("dead", 0)], null, null)).toEqual([{ entityId: "a", actionId: null }]);
  expect(() => pinSupportTargets("medical_support", [actor("full", 100)], null, null)).toThrow();
});
it("protects lowest exact HP ratio then slot then ordinal ID", () => {
  expect(pinSupportTargets("emergency_protection", [actor("z", 25, 1), actor("b", 25), actor("a", 25)], null, null)[0]?.entityId).toBe("a");
});
it("requires an independent target and pins interruptible current action identity", () => {
  const target = { ...enemy("outside", 1, "EXTERIOR"), action: { id: "cast1", kind: "CHANNEL", interruptible: true, immune: false } };
  expect(pinSupportTargets("electronic_disruption", [target], null, "outside")).toEqual([{ entityId: "outside", actionId: "cast1" }]);
  expect(() => pinSupportTargets("electronic_disruption", [enemy("a", 0)], "a", "a")).toThrow();
  expect(() => pinSupportTargets("weakness_scan", [target], "outside", null)).toThrow();
});
it("rejects duplicate world identity and undeclared actor fields", () => {
  expect(() => pinSupportTargets("smoke", [actor("a"), actor("a")], null, null)).toThrow();
  expect(() => pinSupportTargets("smoke", [{ ...actor("a"), owned: true }], null, null)).toThrow();
});
it("resolves exactly three distinct selected roles in slot order", () => {
  const keys = ["ce03.role.medical_support", "ce03.role.smoke", "ce03.role.electronic_disruption"];
  expect(resolveSupportThree(keys, []).map(x => x.id)).toEqual(keys);
  expect(() => resolveSupportThree([keys[0], keys[0], keys[2]], [])).toThrow();
  expect(() => resolveSupportThree(["unknown", ...keys], [])).toThrow();
});
it("never interrupts a replacement action or retargets after a pinned actor dies", () => {
  const a = { ...enemy("a", 0), action: { id: "new", kind: "CHANNEL", interruptible: true, immune: false } };
  expect(validSupportPins([{ entityId: "a", actionId: "old" }], [a])).toEqual([]);
  expect(validSupportPins([{ entityId: "a", actionId: null }], [{ ...a, hp: 0 }, enemy("b", 1)])).toEqual([]);
  expect(validSupportPins([{ entityId: "a", actionId: "new" }], [a])).toHaveLength(1);
});
it("limits firepower to train basic and both carriage damage sources", () => {
  for (const source of ["TRAIN_BASIC", "COMBAT_DEVICE", "GUN_DEVICE"]) expect(firepowerApplies(source)).toBe(true);
  for (const source of ["OPERATOR", "SUPPORT", "HEAL", "REPAIR", "MECHANIC_FIXED"]) expect(firepowerApplies(source)).toBe(false);
  expect(() => firepowerApplies("invented")).toThrow();
});
it("mixes proven legacy and new definitions without mutating the old prepared template", async () => {
  const f = await ce03ProducerFixture();
  const prepared = await (await f.request()).json();
  const legacy = prepared.draft.template.train.manual;
  const before = JSON.stringify(legacy);
  const selected = resolveSupportThree(["barrier", "ce03.role.medical_support", "support_fire"], legacy);
  expect(selected.map(x => x.id)).toEqual(["barrier", "ce03.role.medical_support", "support_fire"]);
  selected[0]!.id = "modified";
  expect(JSON.stringify(legacy)).toBe(before);
  expect(() => resolveSupportThree(["barrier", "ce03.role.smoke", "support_fire"], [{ ...legacy[0], injected: true }])).toThrow();
});
