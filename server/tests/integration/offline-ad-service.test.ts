import {publishConfiguredDisclosuresForTest} from '../helpers/published-disclosure-fixture';
import {createStartedOfflineAdIntent as createOfflineAdIntent, seedStartedAdForTest} from '../helpers/started-ad-fixture';
import { describe, expect, it, vi } from "vitest";
import { setup } from "../helpers/departure-fixture";
import { readOfflineAdIntent, spinOfflineAd, recordOfflineAdVerification } from "../../apps/game-api/src/offline-ad-service";
import { parseOfflineAdPolicy } from "../../apps/game-api/src/offline-ad-policy";
import * as offlineDraw from "../../apps/game-api/src/offline-ad-policy";
import initSqlJs from "sql.js";
import { SqlJsD1Database } from "../helpers/growth-d1";
import app from "../../apps/game-api/src/index";
import { Hono } from "hono";
import { generateKeyPairSync, sign } from "node:crypto";
import { registerOfflineAdRoutes } from "../../apps/game-api/src/offline-ad-routes";

const policy = parseOfflineAdPolicy({ version: "TEST_ONLY", weightsBps: [10000, 0, 0], challengeLifetimeMs: 60000,
  callbackGraceMs: 60000, maxAdsPerDay: 2, dayStartUtcMinutes: 0, adUnit: "TEST_UNIT", appId: "TEST_APP", placement: "OFFLINE_SUPPLY",
  environment: "DEV", rewardItem: "TEST_REWARD", rewardAmount: "1" });
async function fixture() {
  const f = await setup();
  const db = f.env.DB;
  return { ...f, db };
}
describe("offline ad pinned snapshot and ledger", () => {
  it("uses only the stored winner across competing random proposals and historical claimed replay", async () => {
    const { db, sql, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", { ...policy, weightsBps: [5000, 0, 5000] }, "compete");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "compete-tx", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    const draw = vi.spyOn(offlineDraw, "drawOfflineAdMultiplier").mockReturnValueOnce(2).mockReturnValue(4);
    try {
      const results = await Promise.all([spinOfflineAd(db, "a", intent.intentId, "compete-a"), spinOfflineAd(db, "a", intent.intentId, "compete-b")]);
      expect(draw).toHaveBeenCalledTimes(2);
      expect(results[0]).toEqual(results[1]);
      expect(results[0]!.multiplier).toBe(scalar("SELECT multiplier FROM offline_ad_fixed_results"));
      expect(scalar("SELECT count(*) FROM offline_ad_fixed_results")).toBe(1);
      expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
      sql.run("DELETE FROM offline_ad_fixed_results"); // Pre-migration CLAIMED rows have only their original receipt.
      const calls = draw.mock.calls.length;
      expect(await spinOfflineAd(db, "a", intent.intentId, "historical")).toEqual(results[0]);
      expect(draw).toHaveBeenCalledTimes(calls);
    } finally { draw.mockRestore(); }
  });
  it("keeps a fixed result but grants nothing when basic supply wins after a failed payout", async () => {
    const { db, sql, scalar, headers, env } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "basic-wins-fixed");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "basic-wins-tx", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    sql.run("CREATE TRIGGER payout_fixed_failure BEFORE INSERT ON currency_ledger BEGIN SELECT RAISE(ABORT,'fixed failure'); END");
    await expect(spinOfflineAd(db, "a", intent.intentId, "before-basic")).rejects.toThrow("fixed failure");
    expect(scalar("SELECT count(*) FROM offline_ad_fixed_results")).toBe(1);
    sql.run("DROP TRIGGER payout_fixed_failure");
    const basic = await app.request("/v1/offline-rewards/claim", { method: "POST", headers: { ...headers, "Idempotency-Key": "fixed-basic" } }, env);
    expect(basic.status).toBe(200);
    await expect(spinOfflineAd(db, "a", intent.intentId, "after-basic")).rejects.toThrow("ad_supply_already_consumed");
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
    expect((await readOfflineAdIntent(db, "a", intent.intentId)).status).toBe("SUPPLY_CONSUMED");
  });
  it("preserves the first random result after payout rollback and database reopen", async () => {
    const { db, sql, scalar } = await fixture();
    const mixed = parseOfflineAdPolicy({ ...policy, weightsBps: [5000, 0, 5000] });
    const intent = await createOfflineAdIntent(db, "a", "DEV", mixed, "fixed-create");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "fixed-tx", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    const draw = vi.spyOn(offlineDraw, "drawOfflineAdMultiplier").mockReturnValueOnce(2).mockReturnValue(4);
    try {
      sql.run("CREATE TRIGGER payout_failure BEFORE INSERT ON currency_ledger BEGIN SELECT RAISE(ABORT,'fixed payout failure'); END");
      await expect(spinOfflineAd(db, "a", intent.intentId, "fixed-spin")).rejects.toThrow("fixed payout failure");
      expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
      sql.run("DROP TRIGGER payout_failure");
      const SQL = await initSqlJs(), reopened = new SQL.Database(sql.export());
      reopened.create_function("unixepoch", () => Date.now() / 1000);
      const restored = new SqlJsD1Database(reopened) as unknown as D1Database;
      try {
        const result = await spinOfflineAd(restored, "a", intent.intentId, "fixed-retry");
        expect(result.multiplier).toBe(2);
        expect(draw).toHaveBeenCalledTimes(1);
        expect(await spinOfflineAd(restored, "a", intent.intentId, "fixed-again")).toEqual(result);
        expect(reopened.exec("SELECT count(*) FROM reward_transactions")[0]!.values[0]![0]).toBe(1);
      } finally { reopened.close(); }
    } finally { draw.mockRestore(); }
  });
  it("accepts exact callback boundaries once and preserves verified payout after the window closes", async () => {
    const { db, scalar } = await fixture();
    const pinned = parseOfflineAdPolicy({ ...policy, version: "EXPIRY_PIN", weightsBps: [0, 0, 10000] });
    const intent = await createOfflineAdIntent(db, "a", "DEV", pinned, "boundary");
    const callback = { provider: "ADMOB" as const, transactionId: "boundary-tx", timestamp: intent.expiresAt,
      userId: intent.userId, customData: intent.customData, adUnit: pinned.adUnit };
    const clock = vi.spyOn(Date, "now").mockReturnValue(intent.expiresAt + pinned.callbackGraceMs);
    try {
      const verified = await Promise.all([recordOfflineAdVerification(db, intent.intentId, callback), recordOfflineAdVerification(db, intent.intentId, callback)]);
      expect(verified).toEqual([{ accepted: true }, { accepted: true }]);
      expect(scalar("SELECT count(*) FROM ad_provider_receipts")).toBe(1);
      clock.mockReturnValue(intent.expiresAt + pinned.callbackGraceMs + 1);
      expect(await recordOfflineAdVerification(db, intent.intentId, callback)).toEqual({ accepted: true });
      expect((await readOfflineAdIntent(db, "a", intent.intentId)).canSpin).toBe(true);
      const [first, second] = await Promise.all([spinOfflineAd(db, "a", intent.intentId, "boundary-spin"), spinOfflineAd(db, "a", intent.intentId, "boundary-other")]);
      expect(first).toMatchObject({ multiplier: 4, policyVersion: "EXPIRY_PIN" });
      expect(second).toEqual(first);
      expect((await createOfflineAdIntent(db, "a", "DEV", policy, "boundary")).result).toEqual(first);
      expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
    } finally { clock.mockRestore(); }
  });
  it("rolls back a failed verification batch and recovers the same callback only while eligible", async () => {
    const { db, sql, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "verification-failure");
    const callback = { provider: "ADMOB" as const, transactionId: "verification-failure-tx", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit };
    sql.run("CREATE TRIGGER test_verify_failure BEFORE INSERT ON ad_provider_receipts BEGIN SELECT RAISE(ABORT,'injected verification failure'); END");
    await expect(recordOfflineAdVerification(db, intent.intentId, callback)).rejects.toThrow("injected verification failure");
    expect((await readOfflineAdIntent(db, "a", intent.intentId)).status).toBe("AD_PENDING");
    expect(scalar("SELECT count(*) FROM ad_provider_receipts")).toBe(0);
    expect(scalar("SELECT count(*) FROM offline_ad_guard")).toBe(0);
    sql.run("DROP TRIGGER test_verify_failure");
    expect(await recordOfflineAdVerification(db, intent.intentId, callback)).toEqual({ accepted: true });
    expect(scalar("SELECT count(*) FROM ad_provider_receipts")).toBe(1);
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
  });
  it("rejects callback expiry inside its commit batch without reserving a provider receipt", async () => {
    const { db, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "expiry-race");
    const callback = { provider: "ADMOB" as const, transactionId: "expiry-race-tx", timestamp: intent.expiresAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit };
    const deadline = intent.expiresAt + policy.callbackGraceMs;
    const clock = vi.spyOn(Date, "now").mockReturnValue(deadline);
    const originalBatch = db.batch.bind(db);
    const batch = vi.spyOn(db, "batch").mockImplementationOnce(statements => {
      clock.mockReturnValue(deadline + 1);
      return originalBatch(statements);
    });
    try {
      await expect(recordOfflineAdVerification(db, intent.intentId, callback)).rejects.toThrow("ad_verification_expired");
      expect((await readOfflineAdIntent(db, "a", intent.intentId)).status).toBe("EXPIRED");
      expect(scalar("SELECT count(*) FROM offline_ad_intents WHERE status='AD_VERIFIED'")).toBe(0);
      expect(scalar("SELECT count(*) FROM ad_provider_receipts")).toBe(0);
      expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
      await expect(recordOfflineAdVerification(db, intent.intentId, callback)).rejects.toThrow("ad_verification_expired");
      await expect(spinOfflineAd(db, "a", intent.intentId, "expired-spin")).rejects.toThrow("ad_verification_expired");
      expect(scalar("SELECT count(*) FROM offline_ad_guard")).toBe(0);
    } finally { batch.mockRestore(); clock.mockRestore(); }
  });
  it("discloses pinned non-default probabilities on create, read and replay despite replacement policy", async () => {
    const { db, scalar } = await fixture();
    const pinned = parseOfflineAdPolicy({ ...policy, version: "PINNED_ODDS", weightsBps: [1250, 0, 8750] });
    const intent = await createOfflineAdIntent(db, "a", "DEV", pinned, "odds-create");
    const expected = {
      policyVersion: "PINNED_ODDS", displayMultipliers: [2, 4],
      probabilities: [{ multiplier: 2, weightBps: 1250 }, { multiplier: 3, weightBps: 0 }, { multiplier: 4, weightBps: 8750 }],
    };
    expect(intent).toMatchObject(expected);
    expect(await readOfflineAdIntent(db, "a", intent.intentId)).toMatchObject(expected);
    expect(await createOfflineAdIntent(db, "a", "DEV", policy, "odds-create")).toMatchObject(expected);
    expect(await createOfflineAdIntent(db, "a", "DEV", policy, "odds-new-key")).toMatchObject(expected);
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
  });
  it("matches read disposition for expired and consumed pending intents without granting", async () => {
    const { db, env, headers, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create");
    const clock = vi.spyOn(Date, "now").mockReturnValue(intent.expiresAt + policy.callbackGraceMs + 1);
    try {
      expect((await readOfflineAdIntent(db, "a", intent.intentId)).status).toBe("EXPIRED");
      await expect(spinOfflineAd(db, "a", intent.intentId, "spin")).rejects.toThrow("ad_verification_expired");
      expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
    } finally { clock.mockRestore(); }
    const basic = await app.request("/v1/offline-rewards/claim", { method: "POST", headers: { ...headers, "Idempotency-Key": "basic" } }, env);
    expect(basic.status).toBe(200);
    expect((await readOfflineAdIntent(db, "a", intent.intentId)).status).toBe("SUPPLY_CONSUMED");
    await expect(spinOfflineAd(db, "a", intent.intentId, "spin")).rejects.toThrow("ad_supply_already_consumed");
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
  });
  it("runs real authenticated intent/read/spin routes with a signed synthetic provider callback", async () => {
    const { db, sql, env, headers, scalar } = await fixture();
    sql.run("INSERT INTO offline_ad_policies VALUES(?,'DEV','ACTIVE',?,0,'TEST_ONLY')", [policy.version, JSON.stringify(policy)]);
    await publishConfiguredDisclosuresForTest(env.DB);
    const pair = generateKeyPairSync("ec", { namedCurve: "prime256v1" });
    const providerApp = new Hono<{ Bindings: Env }>();
    registerOfflineAdRoutes(providerApp, async () => ({ accountId: "a" }), async id => id === "7" ? pair.publicKey.export({ type: "spki", format: "der" }).toString("base64") : null);
    const url = "/v1/offline-rewards/ad-intents";
    const created = await app.request(url, { method: "POST", headers: { ...headers, "Idempotency-Key": "create" } }, env);
    expect(created.status).toBe(200);
    const intent = await created.json() as Awaited<ReturnType<typeof createOfflineAdIntent>>;
    await seedStartedAdForTest(db,'OFFLINE',intent.intentId);
    expect(intent.canSpin).toBe(false);
    expect(intent.displayMultipliers).toEqual([2]);
    const probabilities = [{ multiplier: 2, weightBps: 10000 }, { multiplier: 3, weightBps: 0 }, { multiplier: 4, weightBps: 0 }];
    expect(intent).toMatchObject({ probabilities });
    const postSpin = () => app.request(`${url}/${intent.intentId}/spin`, { method: "POST", headers: { ...headers, "Idempotency-Key": "spin" } }, env);
    expect(await (await postSpin()).json()).toMatchObject({ errorCode: "ad_verification_pending" });
    const raw = `ad_network=1&ad_unit=TEST_UNIT&custom_data=${encodeURIComponent(intent.customData)}&reward_amount=1&reward_item=TEST_REWARD&timestamp=${intent.createdAt}&transaction_id=abc123&user_id=${intent.userId}`;
    const callback = `/v1/admob/ssv?${raw}&signature=${sign("sha256", Buffer.from(raw), pair.privateKey).toString("base64url")}&key_id=7`;
    expect((await providerApp.request(callback.replace("ad_unit=TEST_UNIT", "ad_unit=FAKE"), {}, env)).status).toBe(403);
    expect((await providerApp.request(callback, {}, env)).status).toBe(200);
    expect((await providerApp.request(callback, {}, env)).status).toBe(200);
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
    const verified = await app.request(`${url}/${intent.intentId}`, { headers }, env);
    expect(await verified.json()).toMatchObject({ status: "AD_VERIFIED", canSpin: true, result: null, probabilities });
    const result = await (await postSpin()).json();
    expect(result).toMatchObject({ accepted: true, status: "CLAIMED", multiplier: 2 });
    expect(await (await postSpin()).json()).toEqual(result);
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
    expect(await readOfflineAdIntent(db, "a", intent.intentId)).toMatchObject({ canSpin: false, probabilities });
  });
  it("fences an actual concurrent basic claim against spin", async () => {
    const { db, env, headers, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "tx", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    const results = await Promise.allSettled([
      spinOfflineAd(db, "a", intent.intentId, "spin"),
      app.request("/v1/offline-rewards/claim", { method: "POST", headers: { ...headers, "Idempotency-Key": "basic" } }, env),
    ]);
    expect(results).toHaveLength(2);
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
    const stored = scalar("SELECT reward_json FROM reward_transactions") as string;
    const lines = (JSON.parse(stored) as { rewardLines: Array<{ id: string; quantity: number }> }).rewardLines;
    const currencies: Record<string, string> = { cur_credits: "CREDITS", cur_machine_parts: "MACHINE_PARTS", cur_tactical_data: "TACTICAL_DATA", cur_operator_exp: "OPERATOR_EXP" };
    for (const line of lines) expect(scalar(`SELECT balance FROM account_currencies WHERE account_id='a' AND currency='${currencies[line.id]}'`)).toBe(line.quantity);
  });
  it("delayed spin multiplies only its pinned base and preserves later accrued time", async () => {
    const { db, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create");
    const base = JSON.parse(scalar("SELECT base_json FROM offline_ad_intents") as string) as Array<{ id: string; quantity: number }>;
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "tx", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    const time = vi.spyOn(Date, "now").mockReturnValue(intent.createdAt + 3600000);
    try {
      const result = await spinOfflineAd(db, "a", intent.intentId, "spin");
      expect(result.rewardLines.map(line => ({ id: line.id, quantity: line.quantity }))).toEqual(base.map(line => ({ id: line.id, quantity: line.quantity * 2 })));
      expect(scalar("SELECT last_offline_reward_claim_at FROM account_offline_reward_state WHERE account_id='a'")).toBe(intent.createdAt);
    } finally { time.mockRestore(); }
  });
  it("coalesces concurrent creations and enforces an explicit daily limit", async () => {
    const { db, sql, scalar } = await fixture();
    const limited = { ...policy, maxAdsPerDay: 1 };
    const [a, b] = await Promise.all([createOfflineAdIntent(db, "a", "DEV", limited, "one"), createOfflineAdIntent(db, "a", "DEV", limited, "two")]);
    expect(b.intentId).toBe(a.intentId);
    expect(scalar("SELECT count(*) FROM offline_ad_intents")).toBe(1);
    sql.run("INSERT INTO account_offline_reward_state(account_id,last_offline_reward_claim_at,last_story_progress_id,revision,updated_at) VALUES('a',1,'none',1,0)");
    await expect(createOfflineAdIntent(db, "a", "DEV", limited, "three")).rejects.toThrow("ad_daily_limit_reached");
    expect((await readOfflineAdIntent(db, "a", a.intentId)).status).toBe("SUPPLY_CONSUMED");
  });
  it("exposes policy-unconfigured without breaking the original basic claim", async () => {
    const { env, headers } = await fixture();
    const response = await app.request("/v1/offline-rewards/ad-intents", { method: "POST", headers: { ...headers, "Idempotency-Key": "new-ad" } }, env);
    expect(response.status).toBe(503);
    expect(await response.json()).toMatchObject({ accepted: false, errorCode: "ad_reward_policy_unconfigured" });
    const basic = await app.request("/v1/offline-rewards/claim", { method: "POST", headers: { ...headers, "Idempotency-Key": "basic" } }, env);
    expect(basic.status).toBe(200);
  });
  it("restores a lost creation response after its active policy is retired", async () => {
    const { env, headers, sql } = await fixture();
    sql.run("INSERT INTO offline_ad_policies VALUES(?,'DEV','ACTIVE',?,0,'TEST_ONLY')", [policy.version, JSON.stringify(policy)]);
    await publishConfiguredDisclosuresForTest(env.DB);
    const request = () => app.request("/v1/offline-rewards/ad-intents", { method: "POST", headers: { ...headers, "Idempotency-Key": "lost-create" } }, env);
    const first = await (await request()).json();
    sql.run("UPDATE offline_ad_policies SET status='RETIRED'");
    const retry = await request();
    expect(retry.status).toBe(200);
    expect(await retry.json()).toEqual(first);
  });
  it("requires verification then atomically grants once across retry and reconnect", async () => {
    const { db, sql } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create-1");
    await expect(spinOfflineAd(db, "a", intent.intentId, "spin-1")).rejects.toThrow("ad_verification_pending");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "tx-1", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    const result = await spinOfflineAd(db, "a", intent.intentId, "spin-1");
    expect(result.multiplier).toBe(2);
    expect(await spinOfflineAd(db, "a", intent.intentId, "spin-1")).toEqual(result);
    expect((await readOfflineAdIntent(db, "a", intent.intentId)).result).toEqual(result);
    expect(sql.exec("SELECT count(*) FROM reward_transactions")[0]?.values[0]?.[0]).toBe(1);
    expect(sql.exec("SELECT count(*) FROM currency_ledger")[0]?.values[0]?.[0]).toBe(4);
  });
  it("binds all reads/spins to account and reuses one intent per supply", async () => {
    const { db } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create-1");
    expect((await createOfflineAdIntent(db, "a", "DEV", policy, "create-2")).intentId).toBe(intent.intentId);
    await expect(readOfflineAdIntent(db, "b", intent.intentId)).rejects.toThrow("ad_intent_not_found");
    await expect(spinOfflineAd(db, "b", intent.intentId, "spin")).rejects.toThrow("ad_intent_not_found");
  });
  it("loses safely to the existing basic claim and preserves the original idempotency alias", async () => {
    const { db, env, headers, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create-1");
    await createOfflineAdIntent(db, "a", "DEV", policy, "create-2");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "tx-1", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    const basic = await app.request("/v1/offline-rewards/claim", { method: "POST", headers: { ...headers, "Idempotency-Key": "basic-1" } }, env);
    expect(basic.status).toBe(200);
    expect((await basic.json()).accepted).toBe(true);
    await expect(spinOfflineAd(db, "a", intent.intentId, "spin-1")).rejects.toThrow("ad_supply_already_consumed");
    expect((await createOfflineAdIntent(db, "a", "DEV", policy, "create-2")).intentId).toBe(intent.intentId);
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
  });
  it("rolls the entire payout back when a ledger statement fails", async () => {
    const { db, sql, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "tx-1", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    sql.run("CREATE TRIGGER test_failure BEFORE INSERT ON currency_ledger BEGIN SELECT RAISE(ABORT,'injected ledger failure'); END");
    await expect(spinOfflineAd(db, "a", intent.intentId, "spin")).rejects.toThrow("injected ledger failure");
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
    expect(scalar("SELECT count(*) FROM account_currencies")).toBe(0);
    expect((await readOfflineAdIntent(db, "a", intent.intentId)).status).toBe("AD_VERIFIED");
    sql.run("DROP TRIGGER test_failure");
    expect((await spinOfflineAd(db, "a", intent.intentId, "spin")).status).toBe("CLAIMED");
  });
  it("commits only one winner for concurrent spins", async () => {
    const { db, scalar } = await fixture();
    const intent = await createOfflineAdIntent(db, "a", "DEV", policy, "create");
    await recordOfflineAdVerification(db, intent.intentId, { provider: "ADMOB", transactionId: "tx-1", timestamp: intent.createdAt,
      userId: intent.userId, customData: intent.customData, adUnit: policy.adUnit });
    const [first, second] = await Promise.all([spinOfflineAd(db, "a", intent.intentId, "spin-1"), spinOfflineAd(db, "a", intent.intentId, "spin-2")]);
    expect(second).toEqual(first);
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
  });
  it("rejects cross-account provider replay and late or wrong bindings", async () => {
    const { db, scalar } = await fixture();
    const a = await createOfflineAdIntent(db, "a", "DEV", policy, "create");
    const b = await createOfflineAdIntent(db, "b", "DEV", policy, "create");
    const verified = { provider: "ADMOB" as const, transactionId: "tx-1", timestamp: a.createdAt,
      userId: a.userId, customData: a.customData, adUnit: policy.adUnit };
    await expect(recordOfflineAdVerification(db, b.intentId, verified)).rejects.toThrow("ad_ssv_binding_mismatch");
    await expect(recordOfflineAdVerification(db, a.intentId, { ...verified, timestamp: a.expiresAt + 1 })).rejects.toThrow("ad_verification_expired");
    await recordOfflineAdVerification(db, a.intentId, verified);
    await recordOfflineAdVerification(db, a.intentId, verified);
    await expect(recordOfflineAdVerification(db, b.intentId, { ...verified, timestamp: b.createdAt, userId: b.userId, customData: b.customData })).rejects.toThrow("ad_reward_state_conflict");
    expect(scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
  });
});
