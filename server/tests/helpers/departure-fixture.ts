
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { SqlJsD1Database } from "./growth-d1";
import { launchRunRewardsConfig } from "../../packages/balance/src/index";
import { startDeparture } from "../../apps/game-api/src/departure";
import { ActivityError } from "../../apps/game-api/src/activity-policy";

export const operations = ["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop", "opn_04_scrapstar_show", "opn_05_night_boarders", "opn_06_broken_line", "raid_raider_king_engine"];
export const body = { operationId: operations[0], risk: "I", expectedFormationRevision: 1, expectedLoadoutRevision: 1, expectedUltimateRevision: 1 };
export async function setup(coreOnly = false, selectUltimate = true) {
  const SQL = await initSqlJs(), sql = new SQL.Database();
  sql.create_function("unixepoch", () => Date.now() / 1000); sql.run("PRAGMA foreign_keys=ON");
  for (const f of readdirSync("packages/db/migrations").filter(f => /^\d+_.+\.sql$/u.test(f)).sort()) sql.run(readFileSync(join("packages/db/migrations", f), "utf8"));
  const now = Date.now(), secret = "departure-test-secret-at-least-32-chars";
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
  sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('s','a','h','f',0,'ACTIVE',?,?)", [now, now + 60000]);
  sql.run("INSERT INTO account_train(account_id,train_id,revision,updated_at) VALUES('a','train_a',1,0)");
  for (const id of ["car_combat", "car_gun", "car_cargo", "car_defense"]) sql.run("INSERT INTO account_carriages(account_id,carriage_id) VALUES('a',?)", [id]);
  const operators = Array.from({ length: 5 }, (_, slot) => ({ slot, operatorId: `op${slot}` }));
  for (const o of operators) sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)", [o.operatorId]);
  sql.run("INSERT INTO fuel_state VALUES('a',30,100,0,0)");
  sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,1,'test','{\"fuel\":{\"runStartCost\":10}}',0,'test')");
  sql.run("UPDATE balance_config_versions SET config_json=?", [JSON.stringify({ fuel: { runStartCost: 10 }, runRewards: launchRunRewardsConfig })]);
  sql.run("INSERT INTO balance_config_activations(id,version,activation_type,activated_by,activated_at) VALUES('act',1,'PUBLISH','test',0)");
  sql.run("INSERT INTO balance_config_active VALUES(1,1,'act',0)");
  const adapter = new SqlJsD1Database(sql), env = { DB: adapter as unknown as D1Database, ENVIRONMENT: "DEV", AUTH_SIGNING_KEY_CURRENT: secret } as Env;
  const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs: 60000 });
  const headers = { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
  const post = (path: string, data: object, key = crypto.randomUUID()) => app.request(path, { method: "POST", headers: { ...headers, "Idempotency-Key": key }, body: JSON.stringify(data) }, env);
  await post("/v1/formation/presets/p/save", { expectedRevision: 0, name: "squad", operators });
  await post("/v1/formation/presets/p/activate", { expectedRevision: 1, expectedFormationRevision: 0 });
  if (selectUltimate) await post("/v1/formation/ultimate", { rulesVersion: "ultimate-selection-v1", operatorId: "op0", expectedRevision: 0, expectedFormationRevision: 1 });
  const install = (ids = ["car_combat", "car_gun"], expectedRevision = 0) => post("/v1/trains/loadout", { rulesVersion: "train-loadout-v1", trainId: "train_a", expectedRevision, carriages: ids.map((carriageId, slot) => ({ slot, carriageId })) });
  await install();
  const preview = (data: object = body, key = crypto.randomUUID()) => post("/v1/operations/preview", data, key);
  const start = async (id: string, key = crypto.randomUUID()): Promise<Response> => {
    if (!coreOnly) return post("/v1/runs/start", { operationPreviewId: id }, key);
    // Exercise the retained atomic domain core independently of the HTTP combat
    // gate. This is not a client flag or proof that actual combat can launch.
    const row = await env.DB.prepare("SELECT * FROM operation_previews WHERE id=? AND account_id='a'").bind(id).first<Parameters<typeof startDeparture>[2]>();
    if (!row?.admission_version) return post("/v1/runs/start", { operationPreviewId: id }, key);
    try {
      const result = await startDeparture(env.DB, "a", row, key, async (preview, runId, at) => {
        const m = JSON.parse(preview.preview_json), raid = m.encounterType === "RAID_BOSS";
        return { accepted: true, startStatus: "STARTED", runId, operationPreviewId: id,
          operationId: m.operationId, actId: m.actId, regionId: m.regionId, encounterType: m.encounterType,
          risk: preview.risk, riskLevel: preview.risk, balanceVersion: String(preview.balance_config_version), contentVersion: m.contentVersion,
          allowedOperatorSlots: 5, startAugmentPackageRef: m.startAugmentPackageRef, startTacticPackageRef: m.startTacticPackageRef,
          ordinary: raid ? null : { totalWaves: 15, startWaveNumber: 1 },
          raid: raid ? { raidId: m.raidId, bossId: m.bossId, bossPhaseRefs: m.bossPhaseRefs } : null,
          raidId: m.raidId, bossId: m.bossId, bossPhaseRefs: m.bossPhaseRefs,
          serverStartedAt: new Date(at).toISOString(), runVersionSnapshot: null, errorCode: null, message: null };
      });
      return Response.json(result);
    } catch (error) {
      if (error instanceof ActivityError) return Response.json({ accepted: false, errorCode: error.code }, { status: error.status });
      throw error;
    }
  };
  const scalar = (query: string) => sql.exec(query)[0]?.values[0]?.[0];
  const progress = (ids: string[]) => sql.run("INSERT INTO account_story_progress(account_id,act_id,highest_cleared_operation_id,highest_story_progress_id,cleared_operations_json,story_flags_json,updated_at) VALUES('a','act_01','unused','unused',?,'[]',0) ON CONFLICT(account_id) DO UPDATE SET cleared_operations_json=excluded.cleared_operations_json", [JSON.stringify(ids)]);
  return { sql, adapter, env, headers, post, preview, start, scalar, progress, install };
}
