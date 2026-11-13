import { expect, it } from "vitest";
import { writeFile } from "node:fs/promises";
import { setup } from "../helpers/departure-fixture";
import { SeraHealActionQueue } from "../../apps/game-api/src/combat-sera-action-queue";

async function make(mode = "ORDINARY_SOLO") {
  const f = await setup(true, false), ids = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"];
  ids.forEach((id, slot) => {
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)", [id]);
    f.sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id,trained_level) VALUES('a',?,?,1)", [slot, id]);
  });
  f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'", [JSON.stringify({ schemaVersion: 1, operators: ids.map((operatorId, slot) => ({ slot, operatorId })) })]);
  const q = await SeraHealActionQueue.prepare(f.env.DB, "a", { expectedFormationRevision: 1, expectedLoadoutRevision: 1 },
    { simulationId: "queue-attempt-1", mode });
  const input = q.inputSnapshot(), actor = (id: string) => input.entities.find(e => e.definitionId === id)!.entityId;
  const damage = (hp: number, atMs = 0, sequence = 1, entity = "op_kyle") => q.schedule({ command: {
    kind: "OBSERVE_DAMAGE_HP", eventId: `damage-${entity}-${sequence}`, actionId: `damage-${sequence}`, atMs, actorId: actor(entity), hp }, sequence });
  const cast = (atMs = 0, sequence = 2, effectSequence = 3, actionId = "heal1") => q.schedule({ command: {
    kind: "CAST", eventId: `cast-${actionId}`, actionId, atMs, sourceId: actor("op_sera"), abilityId: "op_sera.active" }, sequence, effectSequence });
  return { f, q, input, actor, damage, cast };
}
it("queue emits the accepted heal at its due time without caller RESOLVE and catches up once", async () => {
  const c = await make(); c.damage(100); c.cast();
  const accepted = c.q.advanceTo(0); expect(accepted.map(e => e.receipt.status)).toEqual(["APPLIED", "ACCEPTED"]);
  expect(c.q.pending()).toMatchObject([{ dueMs: 400, sequence: 3, payload: { kind: "RESOLVE" } }]);
  expect(c.q.advanceTo(399)).toEqual([]); expect(c.q.health(c.actor("op_kyle")).hp).toBe(100);
  const pendingCheckpoint = c.q.snapshot(), restored = SeraHealActionQueue.restore(c.input, pendingCheckpoint);
  const effects = c.q.advanceTo(12000);
  const restoredEffects = restored.advanceTo(12000);
  expect(restoredEffects).toEqual(effects);
  expect(restored.snapshot()).toEqual(c.q.snapshot());
  expect(effects).toMatchObject([{ dueMs: 400, receipt: { actualAmount: 70, afterHp: 170 } }]);
  expect(c.q.advanceTo(12000)).toEqual([]); expect(c.q.pending()).toEqual([]);
  await writeFile("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-sera-queue-observed-sql-test-20261001.json",
    JSON.stringify({ environment: "LOCAL_SQL_TEST_SYNTHETIC_ACCOUNT_NOT_DEPLOYED_PROFILE", input: c.input, accepted, effects,
      pendingCheckpoint, restoredEffects, checkpoint: c.q.snapshot(), committedRuns: c.f.scalar("SELECT count(*) FROM runs") }, null, 2));
});
it.each([4, 6])("same-tick down uses supplied authority sequence %i rather than scheduling arrival", async downSequence => {
  const d = await make(); d.damage(100); d.cast(0, 2, 5); d.q.advanceTo(0); d.damage(0, 400, downSequence);
  const emitted = d.q.advanceTo(400);
  expect(emitted.map(e => e.sequence)).toEqual(downSequence === 4 ? [4, 5] : [5, 6]);
  expect(emitted.find(e => e.receipt.actionId === "heal1")!.receipt.status).toBe(downSequence === 4 ? "CANCELED" : "APPLIED");
});
it("same-tick cancellation removes pending effect without refund and does not emit stale RESOLVE", async () => {
  const c = await make(); c.damage(100); c.cast(0, 2, 5); c.q.advanceTo(0);
  c.q.schedule({ command: { kind: "CANCEL", eventId: "cancel", actionId: "heal1", atMs: 400 }, sequence: 4 });
  expect(c.q.advanceTo(400)).toMatchObject([{ receipt: { reason: "ACTION_INTERRUPTED" } }]);
  expect(c.q.pending()).toEqual([]); c.cast(700, 6, 7, "next");
  expect(c.q.advanceTo(700)[0]!.receipt).toMatchObject({ status: "REJECTED", reason: "SOURCE_NOT_READY" });
});
it("pending queue and original input/HP restore matches continuous execution", async () => {
  const c = await make(); c.damage(100); c.cast(); c.q.advanceTo(200);
  const checkpoint = c.q.snapshot(), restored = SeraHealActionQueue.restore(c.input, JSON.parse(JSON.stringify(checkpoint)));
  expect(restored.pending()).toEqual(c.q.pending());
  expect(restored.advanceTo(500)).toEqual(c.q.advanceTo(500));
  expect(restored.snapshot()).toEqual(c.q.snapshot());
  const tampered = structuredClone(checkpoint); tampered.queue.pending[0]!.dueMs++;
  expect(() => SeraHealActionQueue.restore(c.input, tampered)).toThrow("combat_sera_queue_state_invalid");
  expect(() => SeraHealActionQueue.restore(c.input, { ...checkpoint, version: "old" })).toThrow("combat_sera_queue_state_invalid");
});
it("replays schedule identity, rejects late, rewind, unknown order and manual RESOLVE", async () => {
  const c = await make(); const scheduled = c.cast(); expect(c.cast()).toMatchObject({ ...scheduled, replayed: true });
  expect(() => c.cast(0, 2, 9)).toThrow("combat_sera_queue_event_conflict");
  expect(() => c.q.schedule({ command: { kind: "RESOLVE", eventId: "fake", actionId: "heal1", atMs: 400 }, sequence: 9 }))
    .toThrow("combat_sera_queue_command_invalid");
  c.q.advanceTo(100); const before = c.q.snapshot();
  expect(() => c.damage(100, 100, 10)).toThrow("combat_logical_queue_closed_tick");
  expect(() => c.q.advanceTo(99)).toThrow("combat_logical_queue_clock_rewind");
  expect(c.q.snapshot()).toEqual(before);
});
it("failed catch-up is atomic for queue, receipts and HP", async () => {
  const c = await make(); c.damage(100); c.cast();
  c.q.schedule({ command: { kind: "OBSERVE_DAMAGE_HP", eventId: "bad", actionId: "bad", atMs: 401, actorId: "foreign", hp: 0 }, sequence: 4 });
  const before = c.q.snapshot(); expect(() => c.q.advanceTo(500)).toThrow("combat_sera_actor_unknown");
  expect(c.q.snapshot()).toEqual(before);
});

it("owns effect reservations, rejects missing or duplicate order and preserves original restore input", async () => {
  const c = await make(); c.damage(100); c.cast(); const before = c.q.snapshot();
  expect(() => c.damage(0, 400, 3)).toThrow("combat_logical_queue_sequence_conflict");
  expect(() => c.q.schedule({ command: { kind: "CAST", eventId: "missing", actionId: "missing", atMs: 0,
    sourceId: c.actor("op_sera"), abilityId: "op_sera.active" }, sequence: 4 })).toThrow("combat_sera_queue_command_invalid");
  expect(c.q.snapshot()).toEqual(before);
  const wrong = structuredClone(c.input); wrong.entities[0]!.ownerAccountId = "foreign";
  expect(() => SeraHealActionQueue.restore(wrong, before)).toThrow("combat_sera_queue_state_invalid");
});

it("keeps host authority, supports another explicit cast after cooldown and does not synthesize idle casts", async () => {
  const c = await make("ORDINARY_COOP"); expect(c.input.authority.combatOwner).toBe("UNITY_HOST");
  c.damage(100); c.cast(); c.q.advanceTo(12000);
  c.cast(12001, 4, 5, "second");
  expect(c.q.advanceTo(20000).map(e => e.receipt.status)).toEqual(["ACCEPTED", "APPLIED"]);
  expect(c.q.health(c.actor("op_kyle")).hp).toBe(240);
  expect(c.q.advanceTo(100000)).toEqual([]);
  await expect(make("WORLD_BOSS_SOLO")).rejects.toMatchObject({ code: "combat_sera_scope_unsupported" });
});

it.each([true, false])("reserves generated effect identity against external commands in either arrival order (%s)", async castFirst => {
  const c = await make(), eventId = JSON.stringify(["queue-attempt-1", "heal1", "op_sera.active", "HEAL_ONE"]);
  const external = () => c.q.schedule({ command: { kind: "OBSERVE_DAMAGE_HP", eventId, actionId: "collision",
    atMs: 400, actorId: c.actor("op_kyle"), hp: 0 }, sequence: 4 });
  if (castFirst) { c.cast(); const before = c.q.snapshot(); expect(external).toThrow("combat_sera_queue_event_conflict"); expect(c.q.snapshot()).toEqual(before); }
  else { external(); const before = c.q.snapshot(); expect(() => c.cast()).toThrow("combat_sera_queue_event_conflict"); expect(c.q.snapshot()).toEqual(before); }
});

it("rejects a self-consistent checkpoint with duplicate scheduled event identity", async () => {
  const c = await make(); c.damage(100); const checkpoint = c.q.snapshot();
  const operation = structuredClone(checkpoint.operations[0]!);
  if (operation.kind !== "SCHEDULE") throw new Error("fixture");
  operation.request.sequence = 9; checkpoint.operations.push(operation);
  const pending = structuredClone(checkpoint.queue.pending[0]!); pending.sequence = 9;
  checkpoint.queue.pending.push(pending); checkpoint.queue.usedSequences.push(9);
  expect(() => SeraHealActionQueue.restore(c.input, checkpoint)).toThrow("combat_sera_queue_state_invalid");
});
