import { readFileSync } from "node:fs";
import { Hono } from "hono";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { registerLocalCombatProducer } from "../../apps/game-api/src/ce03-local-producer";
import { setup, body } from "./departure-fixture";

export const ce03Inputs = () => ({ designJson: readFileSync("tests/fixtures/ce03/combat-stats.json", "utf8"),
  selectionJson: readFileSync("tests/fixtures/ce03/selection.json", "utf8"), bindingEvidenceJson: readFileSync("tests/fixtures/ce03/typed-bindings.json", "utf8") });
export const ce03OperatorIds = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"];

export async function ce03ProducerFixture() {
  const f = await setup(false, false);
  // In-memory DB only. Explicit named automated fixture chooses Reina; no UI default.
  for (let slot = 0; slot < 5; slot++) {
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)", [ce03OperatorIds[slot]!]);
    f.sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id) VALUES('a',?,?)", [slot, ce03OperatorIds[slot]!]);
  }
  f.sql.run("UPDATE account_formation SET formation_json=?,ultimate_operator_id='op_reina',ultimate_revision=1 WHERE account_id='a'",
    [JSON.stringify({ schemaVersion: 1, operators: ce03OperatorIds.map((operatorId, slot) => ({ operatorId, slot })) })]);
  const local = new Hono<{ Bindings: Env }>();
  registerLocalCombatProducer(local, resolveAuthContext, { inputs: ce03Inputs(), disposableDatabase: f.env.DB, allowedAccountIds: ["a"] });
  const request = (data: object = body, headers = f.headers) => local.request("http://127.0.0.1/__local/ce03/prepare", {
    method: "POST", headers, body: JSON.stringify(data) }, f.env);
  return { ...f, local, request };
}
