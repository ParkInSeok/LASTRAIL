import { readFileSync } from "node:fs";
import { Hono } from "hono";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { registerLocalCombatProducer } from "../../apps/game-api/src/ce03-local-producer";
import { setup, body } from "./departure-fixture";
import { sha256Hex } from "@lastrail/common";
import economySnapshot from "../../packages/balance/src/launch-economy-v1.json";

export const ce03Inputs = () => ({ designJson: readFileSync("tests/fixtures/ce03/combat-stats.json", "utf8"),
  selectionJson: readFileSync("tests/fixtures/ce03/selection.json", "utf8"), bindingEvidenceJson: readFileSync("tests/fixtures/ce03/typed-bindings.json", "utf8") });
export const ce03OperatorIds = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"];

export async function ce03ProducerFixture(lobbyBootstrap = false) {
  const f = await setup(false, false);
  if (lobbyBootstrap) {
    // Explicit UI QA bootstrap only; historical battle fixtures stay unchanged.
    const json = JSON.stringify(economySnapshot);
    f.sql.run("INSERT INTO economy_versions VALUES(?,?,?,0)", [economySnapshot.version, await sha256Hex(json), json]);
    f.sql.run("INSERT INTO economy_version_bindings VALUES(1,?)", [economySnapshot.version]);
    f.sql.run("INSERT INTO crystal_policies VALUES('ce03-lobby-read-only',?)", [JSON.stringify({ currencyId: "cur_crystal", ticketExchange: null, purchase: null })]);
    f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'ce03-lobby-read-only')");
  }
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
