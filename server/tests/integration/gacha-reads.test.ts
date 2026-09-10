import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { SqlJsD1Database } from "../helpers/growth-d1";
import { launchGachaPool } from "../../packages/balance/src/gacha-policy";

async function setup() {
  const SQL = await initSqlJs(), sql = new SQL.Database();
  sql.create_function("unixepoch", () => Date.now() / 1000);
  sql.run("PRAGMA foreign_keys=ON");
  for (const f of readdirSync("packages/db/migrations").filter(f => /^\d+_.+\.sql$/u.test(f)).sort()) sql.run(readFileSync(join("packages/db/migrations", f), "utf8"));
  const now = Date.now(), secret = "gacha-read-test-secret-at-least-32-chars";
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0),('b',0,0)");
  sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('s','a','h','f',0,'ACTIVE',?,?)", [now, now + 60000]);
  sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'test','{}',0,'test')");
  const pool = JSON.stringify({ results: [{ resultId: "mat_scrap", resultType: "TRAINING_MATERIAL", resultRarity: "COMMON", quantity: 7, weight: 1 }] });
  const seed = (id: string, status = "ACTIVE", start = now - 1000, end: number | null = null) => sql.run("INSERT INTO gacha_banners VALUES(?,'STANDARD',?,1,NULL,?,?,?, ?,0,0)", [id, status, start, end, '{"rare":10,"elite":30,"pickup":60}', pool]);
  seed("banner_test");
  sql.run("INSERT INTO account_items VALUES('a','cur_command_ticket',20,0)");
  const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs: 60000 });
  const env = { DB: new SqlJsD1Database(sql) as unknown as D1Database, ENVIRONMENT: "DEV", AUTH_SIGNING_KEY_CURRENT: secret } as Env;
  const headers = { Authorization: `Bearer ${token}` };
  const get = (path: string) => app.request(path, { headers }, env);
  const pull = (key = "pull", pullCount = 1) => app.request("/v1/gacha/banners/banner_test/pulls", { method: "POST", headers: { ...headers, "Content-Type": "application/json", "Idempotency-Key": key }, body: JSON.stringify({ pullCount }) }, env);
  const scalar = (query: string) => sql.exec(query)[0]?.values[0]?.[0];
  return { sql, now, seed, get, pull, scalar, env };
}

describe("gacha read and result recovery", () => {
  it("lists only visible active windows and returns actual prices/probabilities/pity without writing", async () => {
    const { get, seed, now, scalar } = await setup();
    seed("draft", "DRAFT"); seed("future", "ACTIVE", now + 3600000); seed("ended", "ACTIVE", now - 2000, now - 1000);
    seed("banner_dev_gacha_verify_hidden");
    const response = await get("/v1/gacha/banners");
    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body.banners.map((b: { bannerId: string }) => b.bannerId)).toEqual(["banner_test"]);
    expect(body.banners[0]).toMatchObject({ prices: [{ pullCount: 1, itemId: "cur_command_ticket", quantity: 1 }, { pullCount: 10, itemId: "cur_command_ticket", quantity: 10 }], pityState: { totalPullCount: 0 }, nextGuarantee: "NONE" });
    expect(body.banners[0].probabilities[0]).toMatchObject({ resultId: "mat_scrap", quantity: 7, baseProbability: 1, nextPullProbability: 1 });
    expect(JSON.stringify(body)).not.toContain('"weight"');
    expect((await get("/v1/gacha/banners?accountId=b")).status).toBe(400);
    expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
  });
  it("recovers exact quantities from the ledger when the stored response summary is sparse", async () => {
    const { pull, get, sql, scalar } = await setup();
    const original = await (await pull()).json();
    expect(original.accepted).toBe(true);
    sql.run("UPDATE gacha_pull_batches SET result_summary_json='{\"responseResults\":[]}'");
    const response = await get(`/v1/gacha/batches/${original.batchId}`);
    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body.recoveryStatus).toBe("COMPLETE");
    expect(body.results).toEqual(original.results);
    expect((await (await pull()).json()).results).toEqual(original.results);
    expect(scalar("SELECT quantity FROM account_items WHERE item_id='mat_scrap'")).toBe(7);
    expect(scalar("SELECT COUNT(*) FROM gacha_pull_batches")).toBe(1);
  });
  it("pages only owned history and reports unrecoverable holes without manufacturing grants", async () => {
    const { pull, get, sql, scalar } = await setup();
    const first = await (await pull("one")).json(); await pull("two");
    sql.run("UPDATE gacha_pull_batches SET created_at=1");
    const page = await (await get("/v1/gacha/history?limit=1")).json();
    expect(page.batches).toHaveLength(1); expect(page.nextBeforeBatchId).not.toBeNull();
    const second = await (await get(`/v1/gacha/history?limit=1&beforeBatchId=${page.nextBeforeBatchId}`)).json();
    expect(second.batches).toHaveLength(1);
    expect(second.batches[0].batchId).not.toBe(page.batches[0].batchId);
    sql.run("UPDATE gacha_pull_batches SET result_summary_json='{}' WHERE id=?", [first.batchId]);
    sql.run("DELETE FROM gacha_pull_results WHERE batch_id=?", [first.batchId]);
    expect(await (await get(`/v1/gacha/batches/${first.batchId}`)).json()).toMatchObject({ recoveryStatus: "INCOMPLETE", missingSequences: [1], results: [] });
    sql.run("UPDATE gacha_pull_batches SET account_id='b' WHERE id=?", [first.batchId]);
    expect((await get(`/v1/gacha/batches/${first.batchId}`)).status).toBe(404);
    expect(scalar("SELECT COUNT(*) FROM gacha_pull_batches")).toBe(2);
  });
  it("matches the real approved pool and pickup guarantee precedence", async () => {
    const { get, sql, pull } = await setup();
    sql.run("UPDATE gacha_banners SET banner_type='PICKUP',pickup_operator_id='op_vex',pool_snapshot_json=?", [JSON.stringify(launchGachaPool)]);
    sql.run("INSERT INTO player_gacha_pity(account_id,banner_id,total_pull_count,elite_pity_count,pickup_elite_pity_count,rare_or_higher_pity_count,revision,updated_at) VALUES('a','banner_test',59,29,59,9,2,0)");
    const { banner } = await (await get("/v1/gacha/banners/banner_test")).json();
    expect(banner.nextGuarantee).toBe("PICKUP_ELITE_60");
    expect(banner.probabilities.filter((p: { resultRarity: string }) => p.resultRarity === "ELITE").reduce((sum: number, p: { baseProbability: number }) => sum + p.baseProbability, 0)).toBeCloseTo(0.04, 12);
    expect(banner.probabilities.find((p: { resultId: string }) => p.resultId === "op_vex").nextPullProbability).toBe(1);
    expect((await (await pull()).json()).results[0]).toMatchObject({ resultId: "op_vex", guaranteeType: banner.nextGuarantee });
  });
  it("keeps missing quantity unknown and accepts partial legacy summaries without inventing one", async () => {
    const { get, pull, sql } = await setup(); const first = await (await pull()).json();
    sql.run("UPDATE gacha_pull_batches SET result_summary_json='{\"responseResults\":[{\"pullSequence\":1}]}'");
    expect((await (await get(`/v1/gacha/batches/${first.batchId}`)).json()).results[0].reward.quantity).toBe(7);
    sql.run("UPDATE gacha_pull_results SET item_ledger_id=NULL");
    const body = await (await get(`/v1/gacha/batches/${first.batchId}`)).json();
    expect(body).toMatchObject({ recoveryStatus: "INCOMPLETE", unknownQuantitySequences: [1] });
    expect(body.results[0].reward.quantity).toBeNull();
  });
  it("requires a valid session and does not leak a scoped diagnostic ID through pagination", async () => {
    const { get, env, seed, sql } = await setup();
    expect((await app.request("/v1/gacha/banners", {}, env)).status).toBe(401);
    seed("banner_dev_gacha_verify_hidden");
    expect(JSON.stringify(await (await get("/v1/gacha/banners?limit=1")).json())).not.toContain("banner_dev_gacha_verify");
    expect((await get("/v1/gacha/history?limit=0")).status).toBe(400);
    sql.run("UPDATE auth_sessions SET status='REVOKED'");
    expect((await get("/v1/gacha/banners/banner_test")).status).toBe(401);
  });
  it("does not label conversion tokens as operators when duplicate metadata is missing", async () => {
    const { get, pull, sql } = await setup();
    sql.run("UPDATE gacha_banners SET pool_snapshot_json=?", [JSON.stringify({ results: [{ resultId: "op_vex", resultType: "ELITE_OPERATOR", resultRarity: "ELITE", weight: 1 }] })]);
    sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a','op_vex',0)");
    const original = await (await pull()).json();
    expect(original.results[0].reward.kind).toBe("ITEM");
    sql.run("UPDATE gacha_pull_batches SET result_summary_json='{}'");
    sql.run("UPDATE gacha_pull_results SET duplicate_conversion_json='broken'");
    const recovered = await (await get(`/v1/gacha/batches/${original.batchId}`)).json();
    expect(recovered.recoveryStatus).toBe("INCOMPLETE");
    expect(recovered.results[0].reward).toEqual({ kind: "UNKNOWN", id: null, quantity: null });
  });
  it("keeps captured balance versions after banner edits and does not expose extra summary fields", async () => {
    const { get, pull, sql } = await setup();
    const original = await (await pull()).json();
    const summary = JSON.parse(String(sql.exec("SELECT result_summary_json FROM gacha_pull_batches")[0].values[0][0]));
    summary.responseResults[0].internalSecret = "do-not-expose";
    summary.responseResults[0].reward.internalNote = "do-not-expose";
    sql.run("UPDATE gacha_pull_batches SET result_summary_json=?", [JSON.stringify(summary)]);
    sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(2,1,0,'two','{}',0,'test')");
    sql.run("UPDATE gacha_banners SET balance_config_version=2,status='ENDED'");
    const history = await (await get(`/v1/gacha/batches/${original.batchId}`)).json();
    expect(history.balanceVersion).toBe("1");
    expect(JSON.stringify(history)).not.toContain("do-not-expose");
    expect((await (await pull()).json()).balanceVersion).toBe("1");
    expect((await pull("pull", 10)).status).toBe(409);
  });
});
