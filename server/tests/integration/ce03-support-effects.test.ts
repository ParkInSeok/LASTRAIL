import { expect, it } from "vitest";
import { createHash, randomUUID } from "node:crypto";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { applySupportFrame, createEffectFrame, expireEffectFrame, consumeSupportOutbox } from "../../apps/game-api/src/ce03-support-effects";
import { ce03ProducerFixture } from "../helpers/ce03-producer-fixture";
import { createSupportProfileDraft } from "../../apps/game-api/src/ce03-support-profile";
import { acceptSupportCommand, createSupportState, stepSupportState } from "../../apps/game-api/src/ce03-support-state";
const actor = (entityId: string, side = "OPERATOR", hp = 500) => ({ entityId, side, lane: "INTERIOR", hp, maxHp: 1000, atk: 60, armorBps: 0, outgoingBps: 0, protectedUntil: 0, returning: false, controlImmune: false, action: null });
const world = () => createEffectFrame("a".repeat(64), [actor("train", "TRAIN", 1000), actor("op"), actor("enemy", "ENEMY", 1000)]);
const support = (role: string, sequence: number, target = "op", actionId: string | null = null) => ({ type: "SUPPORT", key: `cmd${sequence}`, sequence, dueTick: 0, sourceId: "train", skillId: `ce03.role.${role}`, pins: [{ entityId: target, actionId }] });
const pulse = (sequence: number, committed = false) => ({ type: "DAMAGE", key: `pulse${sequence}`, sequence, dueTick: 0, sourceId: "enemy", targetId: "op", actionId: "channel1", committed, category: "OPERATOR", amount: 100 });
it("applies target HP heal and shield once and persists replay receipt with effects", () => {
  const event = support("medical_support", 1), initial = world();
  const applied = applySupportFrame(initial, [event]);
  expect(applied.actors[1]?.hp).toBe(650);
  expect(initial.actors[1]?.hp).toBe(500);
  expect(applySupportFrame(JSON.parse(JSON.stringify(applied)), [event])).toEqual(applied);
  expect(() => applySupportFrame(applied, [{ ...event, skillId: "ce03.role.smoke" }])).toThrow();
  const shield = applySupportFrame(applied, [support("emergency_protection", 2)]);
  expect(shield.statuses[0]?.value).toBe(250);
  expect(shield.actors[1]?.hp).toBe(650);
});
it("applies smoke after armor and before shield and expires at the exact boundary", () => {
  const smoked = applySupportFrame(world(), [support("smoke", 1)]);
  expect(applySupportFrame(smoked, [{ ...pulse(2), actionId: null }]).actors[1]?.hp).toBe(420);
  const expired = expireEffectFrame(smoked, 120);
  expect(expired.statuses).toHaveLength(0);
  expect(applySupportFrame(expired, [{ ...pulse(3), dueTick: 120, actionId: null }]).actors[1]?.hp).toBe(400);
});
it("keeps strongest source mark and caps shield without lowering a stronger same-source layer", () => {
  const f = world();
  f.statuses.push({ sourceId: "train", targetId: "enemy", abilityId: "other_mark", effectIndex: 0, type: "MARK", value: 2500, expiryTick: 100 });
  f.statuses.push({ sourceId: "train", targetId: "op", abilityId: "ce03.role.emergency_protection", effectIndex: 0, type: "SHIELD", value: 300, expiryTick: 80 });
  const r = applySupportFrame(f, [support("weakness_scan", 1, "enemy"), support("emergency_protection", 2)]);
  expect(r.statuses.find(s => s.type === "SHIELD")).toMatchObject({ value: 300, expiryTick: 80 });
  const hit = { ...pulse(3), sourceId: "train", targetId: "enemy", actionId: null, category: "SUPPORT" };
  expect(applySupportFrame(r, [hit]).actors[2]?.hp).toBe(880);
});
it("rejects exterior barrage and invalid cardinality without mutating the checkpoint", () => {
  const f = world(); f.actors[2]!.lane = "EXTERIOR";
  expect(() => applySupportFrame(f, [support("suppressive_barrage", 1, "enemy")])).toThrow();
  expect(f.applied).toHaveLength(0);
  expect(() => applySupportFrame(world(), [{ ...support("emergency_protection", 1), pins: [{ entityId: "op", actionId: null }, { entityId: "train", actionId: null }] }])).toThrow();
});
it("orders same-tick pulses and disruption by global sequence, preserving committed impacts", () => {
  const f = world(); f.actors[2]!.action = { id: "channel1", kind: "CHANNEL", interruptible: true, immune: false };
  const events = [pulse(3), support("electronic_disruption", 2, "enemy", "channel1"), pulse(1), pulse(4, true)];
  const result = applySupportFrame(f, events);
  expect(result.actors[1]?.hp).toBe(300);
  expect(result.applied.map(x => x.outcome)).toEqual(["APPLIED", "APPLIED", "CANCELLED", "APPLIED"]);
  expect(result.actors[2]?.action).toBeNull();
});
it("cannot interrupt a replacement action and does not remove its pulse", () => {
  const f = world(); f.actors[2]!.action = { id: "channel2", kind: "CHANNEL", interruptible: true, immune: false };
  const r = applySupportFrame(f, [support("electronic_disruption", 1, "enemy", "channel1"), { ...pulse(2), actionId: "channel2" }]);
  expect(r.actors[1]?.hp).toBe(400);
  expect(r.applied[0]?.outcome).toBe("FIZZLED");
});
it("barrage deals 72 at train ATK60, control immunity only blocks stun", () => {
  const f = world(); f.actors[2]!.controlImmune = true;
  const r = applySupportFrame(f, [support("suppressive_barrage", 1, "enemy")]);
  expect(r.actors[2]?.hp).toBe(928);
  expect(r.statuses).toHaveLength(0);
});
it("firepower includes train/devices, excludes support; scan uses strongest mark", () => {
  const f = applySupportFrame(world(), [support("firepower_boost", 1, "train"), support("weakness_scan", 2, "enemy")]);
  const hit = { ...pulse(3), sourceId: "train", targetId: "enemy", actionId: null, category: "TRAIN_BASIC" };
  expect(applySupportFrame(f, [hit]).actors[2]?.hp).toBe(868);
  expect(applySupportFrame(f, [{ ...hit, category: "SUPPORT" }]).actors[2]?.hp).toBe(885);
});
it("consumes actual bound command outbox with explicit global sequence and survives serialized replay", async () => {
  const fixture = await ce03ProducerFixture(), prepared = await (await fixture.request()).json();
  const selected = ["ce03.role.medical_support", "ce03.role.smoke", "ce03.role.electronic_disruption"];
  const draft = await createSupportProfileDraft(prepared.draft, selected);
  let state = createSupportState(draft.profileSha256, selected);
  state = acceptSupportCommand(state, { version: "ce03-support-command-v2", key: "heal", skillId: selected[0], targetId: null }, [{ entityId: "op", hp: 500, maxHp: 1000, slot: 0, spawnOrder: 0, side: "OPERATOR", lane: "INTERIOR", returning: false, action: null }], null);
  for (let i = 0; i < 7; i++) state = stepSupportState(state);
  const initial = expireEffectFrame(createEffectFrame(draft.profileSha256, world().actors), 7);
  const mapping = [{ commandKey: "heal", globalSequence: 17 }];
  const result = await consumeSupportOutbox(draft, state, initial, mapping, []);
  expect(result.actors[1]?.hp).toBe(650);
  expect(result.applied[0]?.sequence).toBe(17);
  expect(await consumeSupportOutbox(draft, state, JSON.parse(JSON.stringify(result)), mapping, [])).toEqual(result);
  await expect(consumeSupportOutbox(draft, state, initial, [], [])).rejects.toThrow();
  await expect(consumeSupportOutbox(draft, state, { ...initial, profileSha256: "f".repeat(64) }, mapping, [])).rejects.toThrow();
});
it("shares the 40 percent shield cap and absorbs earliest expiry first", () => {
  const f = world();
  f.statuses.push({ sourceId: "train", targetId: "op", abilityId: "earlier", effectIndex: 0, type: "SHIELD", value: 200, expiryTick: 50 });
  const shielded = applySupportFrame(f, [support("emergency_protection", 1)]);
  expect(shielded.statuses.map(s => s.value)).toEqual([200, 200]);
  const hit = applySupportFrame(shielded, [{ ...pulse(2), actionId: null, amount: 250 }]);
  expect(hit.actors[1]?.hp).toBe(500);
  expect(hit.statuses.map(s => s.value)).toEqual([0, 150]);
});
it("keeps stronger source expiry, caps summed firepower and leaves fixed damage unmodified", () => {
  const f = world();
  f.statuses.push({ sourceId: "train", targetId: "op", abilityId: "ce03.role.smoke", effectIndex: 0, type: "SMOKE", value: 3000, expiryTick: 20 });
  f.statuses.push({ sourceId: "train", targetId: "train", abilityId: "other_firepower", effectIndex: 0, type: "FIREPOWER", value: 2500, expiryTick: 20 });
  const r = applySupportFrame(f, [support("smoke", 1), support("firepower_boost", 2, "train")]);
  expect(r.statuses.find(s => s.type === "SMOKE")).toMatchObject({ value: 3000, expiryTick: 20 });
  expect(applySupportFrame(r, [{ ...pulse(3), actionId: null, sourceId: "train", targetId: "enemy", category: "TRAIN_BASIC" }]).actors[2]?.hp).toBe(870);
  expect(applySupportFrame(r, [{ ...pulse(3), actionId: null, category: "MECHANIC_FIXED" }]).actors[1]?.hp).toBe(400);
});
it("rolls back the whole proposed batch on invalid ordering and rejects forged receipt identity", () => {
  const f = world();
  expect(() => applySupportFrame(f, [support("medical_support", 1), { ...support("smoke", 1), key: "other" }])).toThrow();
  expect(f.actors[1]?.hp).toBe(500);
  expect(f.applied).toHaveLength(0);
  const r = applySupportFrame(f, [support("medical_support", 1)]);
  r.applied[0]!.sequence = 2;
  expect(() => applySupportFrame(r, [])).toThrow();
});
it("exports reproducible Node effect vectors without claiming Unity conformance", () => {
  const cases = [
    { id: "medical", input: world(), events: [support("medical_support", 1)] },
    { id: "shield", input: world(), events: [support("emergency_protection", 1)] },
    { id: "smoke-hit", input: world(), events: [support("smoke", 1), { ...pulse(2), actionId: null }] },
    { id: "scan-hit", input: world(), events: [support("weakness_scan", 1, "enemy"), { ...pulse(2), actionId: null, sourceId: "train", targetId: "enemy", category: "TRAIN_BASIC" }] },
    { id: "firepower-hit", input: world(), events: [support("firepower_boost", 1, "train"), { ...pulse(2), actionId: null, sourceId: "train", targetId: "enemy", category: "TRAIN_BASIC" }] },
    { id: "barrage", input: world(), events: [support("suppressive_barrage", 1, "enemy")] },
    { id: "interrupt-order", input: world(), events: [pulse(3), support("electronic_disruption", 2, "enemy", "channel1"), pulse(1), pulse(4, true)] },
  ];
  cases[6]!.input.actors[2]!.action = { id: "channel1", kind: "CHANNEL", interruptible: true, immune: false };
  const results = cases.map(c => ({ ...c, expected: applySupportFrame(c.input, c.events) }));
  expect(results[0]!.expected.actors[1]?.hp).toBe(650);
  expect(results[6]!.expected.actors[1]?.hp).toBe(300);
  const files = ["ce03-support-v2.ts", "ce03-support-profile.ts", "ce03-support-state.ts", "ce03-support-effects.ts"];
  const sourceHashes = files.map(file => ({ file, sha256: createHash("sha256").update(readFileSync(resolve("apps/game-api/src", file))).digest("hex") }));
  const json = JSON.stringify({ version: "ce03-support-effect-reference-v1", scope: "NODE_EFFECT_PHASE_ONLY", unityExecuted: false, sourceHashes, cases: results }, null, 2);
  const directory = resolve("audit-results/ce03-support-effects"); mkdirSync(directory, { recursive: true });
  const path = resolve(directory, `vectors-${randomUUID()}.json`); writeFileSync(path, json, "utf8");
  expect(readFileSync(path, "utf8")).toBe(json);
  console.info(`Support reference vectors: ${path}; SHA256=${createHash("sha256").update(json).digest("hex")}`);
});
it("keeps amplified damage bigint until bounded shield and HP deductions", () => {
  const f = world(); f.actors[0]!.outgoingBps = 3000; f.actors[0]!.atk = Number.MAX_SAFE_INTEGER;
  expect(applySupportFrame(f, [{ ...pulse(1), sourceId: "train", targetId: "enemy", actionId: null, amount: Number.MAX_SAFE_INTEGER, category: "TRAIN_BASIC" }]).actors[2]?.hp).toBe(0);
  expect(applySupportFrame(f, [support("suppressive_barrage", 1, "enemy")]).actors[2]?.hp).toBe(0);
});
