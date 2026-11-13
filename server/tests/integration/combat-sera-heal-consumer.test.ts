import { expect, it } from "vitest";
import { writeFile } from "node:fs/promises";
import { setup } from "../helpers/departure-fixture";
import { prepareSeraHealConsumer, SeraHealConsumer } from "../../apps/game-api/src/combat-sera-heal-consumer";

const request = { expectedFormationRevision: 1, expectedLoadoutRevision: 1 };
const ids = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"];
async function make(mode = "ORDINARY_SOLO") {
  const f = await setup(true, false);
  ids.forEach((id, slot) => {
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)", [id]);
    f.sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id,trained_level) VALUES('a',?,?,1)", [slot, id]);
  });
  f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'", [JSON.stringify({ schemaVersion: 1, operators: ids.map((operatorId, slot) => ({ slot, operatorId })) })]);
  const consumer = await prepareSeraHealConsumer(f.env.DB, "a", request, { simulationId: "logical-attempt-1", mode });
  const input = consumer.inputSnapshot();
  const actor = (id: string) => input.entities.find(e => e.definitionId === id)!.entityId;
  const damage = (id: string, hp: number, atMs = 0, eventId = `damage-${id}-${atMs}`) => consumer.dispatch({ kind: "OBSERVE_DAMAGE_HP", eventId, actionId: eventId,
    atMs, actorId: actor(id), hp });
  const cast = (actionId = "heal1", atMs = 0) => consumer.dispatch({ kind: "CAST", eventId: `cast-${actionId}`, actionId, atMs,
    sourceId: actor("op_sera"), abilityId: "op_sera.active" });
  return { f, consumer, input, actor, damage, cast };
}
it("binds actual owned five entities and applies Sera heal through native health state once", async () => {
  const c = await make();
  expect(c.input.entities).toHaveLength(5);
  expect(c.input.authority.combatOwner).toBe("UNITY_LOCAL");
  expect(c.input.canExecute).toBe(false);
  expect(c.input.entities[0]).toMatchObject({ ownerAccountId: "a", definitionId: "op_kyle", placement: { formationSlot: 0 }, capability: "LIVING_OPERATOR_HEAL" });
  c.damage("op_kyle", 100);
  const accepted = c.cast();
  expect(accepted).toMatchObject({ status: "ACCEPTED", targetId: c.actor("op_kyle"), effectAtMs: 400 });
  const command = { kind: "RESOLVE", eventId: accepted.effectEventId, actionId: "heal1", atMs: 400 };
  const result = c.consumer.dispatch(command);
  expect(result).toMatchObject({ status: "APPLIED", attemptedAmount: 70, actualAmount: 70, beforeHp: 100, afterHp: 170 });
  expect(c.consumer.dispatch(command)).toMatchObject({ ...result, replayed: true });
  expect(c.consumer.health(c.actor("op_kyle")).hp).toBe(170);
  expect(c.f.scalar("SELECT count(*) FROM runs")).toBe(0);
  const checkpoint = c.consumer.snapshot(), restored = SeraHealConsumer.restore(c.input, checkpoint);
  await writeFile("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-sera-heal-observed-sql-test-20261001.json",
    JSON.stringify({ environment: "LOCAL_SQL_TEST_SYNTHETIC_ACCOUNT_NOT_DEPLOYED_PROFILE",
      input: c.input, accepted, effectCommand: command, result, checkpoint,
      restoredDuplicate: restored.dispatch(command), restoredHealth: restored.health(c.actor("op_kyle")),
      committedRuns: c.f.scalar("SELECT count(*) FROM runs") }, null, 2));
});
it("rejects no-target cast without cooldown and includes injured caster in recipient selection", async () => {
  const c = await make();
  expect(c.cast("empty")).toMatchObject({ status: "REJECTED", reason: "NO_ELIGIBLE_TARGET" });
  c.damage("op_sera", 1);
  expect(c.cast("self")).toMatchObject({ status: "ACCEPTED", targetId: c.actor("op_sera") });
});
it("locks acceptance target, cancels when it goes down and never retargets or refunds cooldown", async () => {
  const c = await make();
  c.damage("op_kyle", 100); c.damage("op_reina", 400);
  const accepted = c.cast();
  c.damage("op_kyle", 0, 200);
  expect(c.consumer.dispatch({ kind: "RESOLVE", eventId: accepted.effectEventId, actionId: "heal1", atMs: 400 }))
    .toMatchObject({ status: "CANCELED", reason: "LOCKED_TARGET_DOWN" });
  expect(c.consumer.health(c.actor("op_reina")).hp).toBe(400);
  expect(c.cast("tooSoon", 700)).toMatchObject({ status: "REJECTED", reason: "SOURCE_NOT_READY" });
});
it("explicit action cancellation preserves cost and state survives restoration without reapplying events", async () => {
  const c = await make("ORDINARY_COOP");
  expect(c.input.authority.combatOwner).toBe("UNITY_HOST");
  c.damage("op_kyle", 100); c.cast();
  expect(c.consumer.dispatch({ kind: "CANCEL", eventId: "cancel1", actionId: "heal1", atMs: 100 }))
    .toMatchObject({ status: "CANCELED" });
  const snapshot = c.consumer.snapshot();
  const restored = SeraHealConsumer.restore(c.input, JSON.parse(JSON.stringify(snapshot)));
  expect(restored.snapshot()).toEqual(snapshot);
  expect(restored.health(c.actor("op_kyle")).hp).toBe(100);
  expect(restored.dispatch({ kind: "CAST", eventId: "cast-next", actionId: "next", atMs: 700,
    sourceId: c.actor("op_sera"), abilityId: "op_sera.active" })).toMatchObject({ status: "REJECTED", reason: "SOURCE_NOT_READY" });
  await expect(make("WORLD_BOSS_SOLO")).rejects.toMatchObject({ code: "combat_sera_scope_unsupported" });
});
it("rejects foreign actors, unsupported effects, event conflicts and corrupted or wrong-version state", async () => {
  const c = await make();
  expect(() => c.consumer.dispatch({ kind: "OBSERVE_DAMAGE_HP", eventId: "foreign", actionId: "foreign", atMs: 0, actorId: "other-owner", hp: 0 }))
    .toThrow("combat_sera_actor_unknown");
  c.damage("op_kyle", 100);
  expect(() => c.consumer.dispatch({ kind: "OBSERVE_DAMAGE_HP", eventId: "damage-op_kyle-0", actionId: "damage-op_kyle-0", atMs: 0, actorId: c.actor("op_kyle"), hp: 99 }))
    .toThrow("combat_sera_event_conflict");
  expect(() => c.consumer.dispatch({ kind: "CAST", eventId: "other", actionId: "other", atMs: 0, sourceId: c.actor("op_sera"), abilityId: "op_sera.ultimate" }))
    .toThrow("combat_sera_effect_unsupported");
  const state = c.consumer.snapshot();
  expect(() => SeraHealConsumer.restore(c.input, { ...state, version: "old" })).toThrow("combat_sera_state_invalid");
  expect(() => SeraHealConsumer.restore(c.input, { ...state, inputHash: "wrong" })).toThrow("combat_sera_state_invalid");
  state.state.health[0]!.health.hp = 999;
  expect(() => SeraHealConsumer.restore(c.input, state)).toThrow("combat_sera_state_invalid");
});

it("rejects changed authority, approved ability or identity when restoring an expected input", async () => {
  const c = await make(), checkpoint = c.consumer.snapshot();
  const wrongAbility = structuredClone(c.input); wrongAbility.supportedAbility.healBps = 99999;
  expect(() => SeraHealConsumer.restore(wrongAbility, checkpoint)).toThrow("combat_sera_state_invalid");
  const wrongOwner = structuredClone(c.input); wrongOwner.entities[0]!.ownerAccountId = "b";
  expect(() => SeraHealConsumer.restore(wrongOwner, checkpoint)).toThrow("combat_sera_state_invalid");
  const world = { ...c.input, scope: { ...c.input.scope, mode: "WORLD_BOSS_SOLO" },
    authority: { ...c.input.authority, mode: "WORLD_BOSS_SOLO", combatOwner: "SERVER", combatClock: "SERVER_COMBAT" } };
  expect(() => SeraHealConsumer.restore(world as typeof c.input, checkpoint)).toThrow("combat_sera_state_invalid");
});

it("does not skip a scheduled effect event, accepts no rewind and preserves failure state", async () => {
  const c = await make(); c.damage("op_kyle", 100); const cast = c.cast();
  const before = c.consumer.snapshot();
  expect(() => c.cast("skip", 12000)).toThrow("combat_sera_effect_event_missing");
  expect(c.consumer.snapshot()).toEqual(before);
  const resolved = c.consumer.dispatch({ kind: "RESOLVE", eventId: cast.effectEventId, actionId: "heal1", atMs: 400 });
  const restored = SeraHealConsumer.restore(c.input, c.consumer.snapshot());
  expect(restored.health(c.actor("op_kyle")).hp).toBe(170);
  expect(restored.dispatch({ kind: "RESOLVE", eventId: cast.effectEventId, actionId: "heal1", atMs: 400 }))
    .toMatchObject({ ...resolved, replayed: true });
  expect(() => restored.dispatch({ kind: "CAST", eventId: "rewind", actionId: "rewind", atMs: 0,
    sourceId: c.actor("op_sera"), abilityId: "op_sera.active" })).toThrow("combat_sera_clock_rewind");
});

it("does not reuse a rejected cast action identity for a later acceptance", async () => {
  const c = await make(); c.cast("empty"); c.damage("op_kyle", 100);
  const before = c.consumer.snapshot();
  expect(() => c.consumer.dispatch({ kind: "CAST", eventId: "retry-empty", actionId: "empty", atMs: 0,
    sourceId: c.actor("op_sera"), abilityId: "op_sera.active" })).toThrow("combat_sera_action_conflict");
  expect(c.consumer.snapshot()).toEqual(before);
  expect(c.cast("fresh").status).toBe("ACCEPTED");
});

it("breaks equal HP ratios by actual formation slot and discards overheal", async () => {
  const c = await make(); c.damage("op_kyle", 700); c.damage("op_reina", 450);
  expect(c.cast().targetId).toBe(c.actor("op_kyle"));
  const capped = await make(); capped.damage("op_kyle", 1390); const accepted = capped.cast();
  expect(capped.consumer.dispatch({ kind: "RESOLVE", eventId: accepted.effectEventId, actionId: "heal1", atMs: 400 }))
    .toMatchObject({ attemptedAmount: 70, actualAmount: 10, afterHp: 1400 });
});

it("cancels a locked heal when its caster goes down and preserves that result on restore", async () => {
  const c = await make(); c.damage("op_kyle", 100); const accepted = c.cast(); c.damage("op_sera", 0, 200);
  const command = { kind: "RESOLVE", eventId: accepted.effectEventId, actionId: "heal1", atMs: 400 };
  const result = c.consumer.dispatch(command);
  expect(result).toMatchObject({ status: "CANCELED", reason: "CASTER_DOWN" });
  const restored = SeraHealConsumer.restore(c.input, c.consumer.snapshot());
  expect(restored.dispatch(command)).toMatchObject({ ...result, replayed: true });
  expect(restored.health(c.actor("op_kyle")).hp).toBe(100);
});
