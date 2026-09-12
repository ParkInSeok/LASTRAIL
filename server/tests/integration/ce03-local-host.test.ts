import { describe, expect, it, vi } from "vitest";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ce03ProducerFixture, ce03Inputs } from "../helpers/ce03-producer-fixture";
import { body } from "../helpers/departure-fixture";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog, localLeaderChargePolicy } from "../../apps/game-api/src/ce03-command-contract";
import { requiredCommandConsumerCoverage, requiredPresentationConsumerCoverage } from "../../apps/game-api/src/ce03-consumer-attestation";
import { presentationFixture } from "../helpers/ce03-presentation-fixture";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { mkdir, writeFile } from "node:fs/promises";

async function fixture(evidence = true, approved = false) {
  const f = await ce03ProducerFixture(); f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
  const artifacts = async () => {
    if (!evidence) return undefined;
    // Synthetic executed-report stand-in: tests host wiring, NOT Unity conformance.
    const algorithmSource = "synthetic-host-test-code", checkpointSchema = "synthetic-host-test-checkpoint";
    const coverageList = approved ? requiredPresentationConsumerCoverage : requiredCommandConsumerCoverage;
    const vectorsJson = canonicalJson({ cases: coverageList.map((coverage, i) => ({ id: `case.${i}`, coverage })) });
    const reportJson = canonicalJson({ schemaVersion: approved ? "ce03-consumer-runner-report-v3" : "ce03-consumer-runner-report-v2", scope: "FULL_LOCAL_CONSUMER", runId: "synthetic.host.runner",
      ...(approved ? { presentationManifestSha256: await sha256Hex(canonicalJson(prepared.presentationManifest as never)), prepareSha256: prepared.prepareSha256 } : {}),
      simulationVersion: (prepared.draft as { simulationVersion: string }).simulationVersion,
      draftSha256: await sha256Hex(canonicalJson(prepared.draft as never)), commandContractSha256: await sha256Hex(canonicalJson(prepared.commandContract as never)),
      algorithmSourceSha256: await sha256Hex(algorithmSource), vectorsSha256: await sha256Hex(vectorsJson), checkpointSchemaSha256: await sha256Hex(checkpointSchema),
      results: coverageList.map((coverage, i) => ({ caseId: `case.${i}`, coverage, status: "PASS" })), passed: coverageList.length, failed: 0, skipped: 0 });
    return { algorithmSource, checkpointSchema, vectorsJson, reportJson };
  };
  const verifier = { verify: vi.fn(async () => true) };
  const options = { disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext, inputs: ce03Inputs(),
    presentation: approved ? { mode: "APPROVED_VIEW_V1" as const, manifest: presentationFixture() } : { mode: "LEGACY_R4" as const },
    supportCatalog: localSupportCatalog, leaderPolicy: localLeaderChargePolicy, artifacts, verifier: evidence ? verifier : undefined };
  const host = createCe03LocalHost(options);
  const request = (path: string, data?: unknown, key = "original.start.key !") => host.request(`http://127.0.0.1${path}`, {
    method: data === undefined ? "GET" : "POST", headers: { ...f.headers, "Idempotency-Key": key }, ...(data === undefined ? {} : { body: JSON.stringify(data) }) }, f.env);
  const supportBody = { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion, expectedRevision: 0,
    slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) };
  expect((await request("/__local/ce03/support-loadout", supportBody, "support-save")).status).toBe(200);
  const p = await request("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1 });
  expect(p.status).toBe(200); const prepared = await p.json();
  const preview = await (await f.preview()).json();
  const startBody = { operationPreviewId: preview.operationPreviewId, expectedSupportRevision: 1, prepareSha256: (prepared as unknown as { prepareSha256: string }).prepareSha256 };
  return { ...f, host, request, startBody, supportBody, prepared, verifier, options };
}

describe("disposable authenticated CE03 host (synthetic verifier fixtures only)", () => {
  it("requires an explicit presentation manifest for the approved view before preparing", async () => {
    const f = await fixture(), host = createCe03LocalHost({ ...f.options, presentation: { mode: "APPROVED_VIEW_V1" } });
    const response = await host.request("http://127.0.0.1/__local/ce03/prepare", { method: "POST", headers: f.headers,
      body: JSON.stringify({ selection: body, expectedSupportRevision: 1 }) }, f.env);
    expect(response.status).toBe(409); expect(await response.json()).toMatchObject({ errorCode: "ce03_presentation_manifest_missing" });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
  });
  it("pins r6 presentation content and full prepare context while retaining profile-r5 commands", async () => {
    const f = await fixture(true, true), response = await f.request("/__local/ce03/start", f.startBody);
    expect(response.status).toBe(200); const accepted = await response.json();
    expect(accepted.combat.profile.schemaVersion).toBe("ce03-isolated-command-profile-r5");
    expect(accepted.combat.contentManifest.schemaVersion).toBe("ce03-isolated-presentation-content-r6");
    expect(accepted.combat.contentManifest.presentationManifest).toEqual(f.prepared.presentationManifest);
    expect(accepted.combat.contentManifest.legacyBindings).toHaveLength(226);
    expect(accepted.combat.contentSha256).toBe(await sha256Hex(canonicalJson(accepted.combat.contentManifest)));
    const { ready: _ready, status: _status, prepareSha256, ...context } = f.prepared;
    expect(_ready).toBe(false); expect(_status).toBe("MISSING_EXECUTION_EVIDENCE");
    expect(prepareSha256).toBe(await sha256Hex(canonicalJson(context)));
    expect(f.verifier.verify).toHaveBeenCalledWith(expect.objectContaining({ prepareSha256,
      presentationManifestSha256: await sha256Hex(canonicalJson(f.prepared.presentationManifest)) }));
    await mkdir("audit-results/ce03-local-host/synthetic-r6-exchanges", { recursive: true });
    await writeFile(`audit-results/ce03-local-host/synthetic-r6-exchanges/${accepted.runId}.json`, JSON.stringify({
      scope: "SYNTHETIC_VERIFIER_TRANSPORT_ONLY", actualConsumerVerified: false, actualBattleExecuted: false,
      prepare: f.prepared, startRequest: f.startBody, startIdempotencyKey: "original.start.key !", startResponse: accepted,
    }, null, 2) + "\n");
  });
  it.each(["v2", "manifestHash", "ownerHash", "coverage"])("refuses %s presentation evidence before admission", async mode => {
    const f = await fixture(true, true), artifacts = (await f.options.artifacts())!;
    const report = JSON.parse(artifacts.reportJson);
    if (mode === "v2") { report.schemaVersion = "ce03-consumer-runner-report-v2"; delete report.prepareSha256; delete report.presentationManifestSha256; }
    if (mode === "manifestHash") report.presentationManifestSha256 = "0".repeat(64);
    if (mode === "ownerHash") report.prepareSha256 = "0".repeat(64);
    if (mode === "coverage") { report.results = report.results.filter((r: { coverage: string }) => r.coverage !== "PRESENTATION_BINDINGS"); report.passed--; }
    const host = createCe03LocalHost({ ...f.options, artifacts: async () => ({ ...artifacts, reportJson: canonicalJson(report) }) });
    const response = await host.request("http://127.0.0.1/__local/ce03/start", { method: "POST", headers: { ...f.headers, "Idempotency-Key": "new" }, body: JSON.stringify(f.startBody) }, f.env);
    expect(response.status).toBe(409); expect(f.verifier.verify).not.toHaveBeenCalled();
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0); expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);
  });
  it("fails closed without actual configured evidence before seed, debit or receipt", async () => {
    const f = await fixture(false), random = vi.spyOn(crypto, "getRandomValues"); random.mockClear();
    const response = await f.request("/__local/ce03/start", f.startBody);
    expect(response.status).toBe(409); expect(await response.json()).toMatchObject({ errorCode: "ce03_consumer_artifacts_missing" });
    expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    expect(f.scalar("SELECT COUNT(*) FROM ce03_start_receipts")).toBe(0);
    expect(random).not.toHaveBeenCalled(); random.mockRestore();
  });
  it("freezes selected order, owner and seed once; original response survives lost response and current changes", async () => {
    const f = await fixture();
    const first = await f.request("/__local/ce03/start", f.startBody), raw = await first.text();
    expect(first.status).toBe(200); const accepted = JSON.parse(raw);
    expect(accepted).toMatchObject({ accepted: true, startStatus: "STARTED", scope: "DISPOSABLE_LOCAL", combat: { schemaVersion: "ce03-accepted-combat-v1", seed: { lifetime: "ORIGINAL_RUN" } } });
    expect(accepted.combat.profile.commandContract.support.slots.map((s: { skillId: string }) => s.skillId)).toEqual(["barrier", "support_fire", "emergency_repair"]);
    expect(accepted.combat.profileSha256).toBe(await sha256Hex(canonicalJson(accepted.combat.profile)));
    expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(20);
    const calls = f.verifier.verify.mock.calls.length;
    f.sql.run("UPDATE account_carriages SET revision=revision+1");
    const random = vi.spyOn(crypto, "getRandomValues"); random.mockClear();
    const retry = await f.request("/__local/ce03/start", f.startBody);
    expect(await retry.text()).toBe(raw); expect(f.verifier.verify).toHaveBeenCalledTimes(calls); expect(random).not.toHaveBeenCalled(); random.mockRestore();
    const restartOptions = { ...f.options }; Reflect.deleteProperty(restartOptions, "verifier"); Reflect.deleteProperty(restartOptions, "artifacts");
    const restarted = createCe03LocalHost(restartOptions);
    expect(await (await restarted.request("http://127.0.0.1/__local/ce03/start", { method: "POST", headers: { ...f.headers, "Idempotency-Key": "original.start.key !" }, body: JSON.stringify(f.startBody) }, f.env)).text()).toBe(raw);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
    const recovered = await f.request("/__local/ce03/start-result", { ...f.startBody, startIdempotencyKey: "original.start.key !" });
    expect((await recovered.json()).startResponse).toEqual(accepted);
    expect((await f.request("/__local/ce03/start", { ...f.startBody, prepareSha256: "0".repeat(64) })).status).toBe(409);
    await mkdir("audit-results/ce03-local-host/synthetic-exchanges", { recursive: true });
    await writeFile(`audit-results/ce03-local-host/synthetic-exchanges/${accepted.runId}.json`, JSON.stringify({
      scope: "SYNTHETIC_VERIFIER_TRANSPORT_ONLY", actualConsumerVerified: false, actualBattleExecuted: false,
      request: f.startBody, startResponse: accepted, originalReplayByteIdentical: true, fuelDebitedOnce: true,
    }, null, 2) + "\n");
  });
  it("keeps owner and loopback isolation and refuses caller effects and stale preparation", async () => {
    const f = await fixture();
    expect((await f.host.request("https://example.com/__local/ce03/start", { method: "POST", headers: f.headers, body: JSON.stringify(f.startBody) }, f.env)).status).toBe(403);
    expect((await f.host.request("http://127.0.0.1/__local/ce03/start", { method: "POST", body: JSON.stringify(f.startBody) }, f.env)).status).toBe(401);
    expect((await f.request("/__local/ce03/start", { ...f.startBody, seed: 1 })).status).toBe(400);
    f.sql.run("UPDATE account_carriages SET revision=revision+1");
    expect((await f.request("/__local/ce03/start", f.startBody)).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
  });
  it("commits one debit for concurrent original-key requests", async () => {
    const f = await fixture();
    const responses = await Promise.all([f.request("/__local/ce03/start", f.startBody), f.request("/__local/ce03/start", f.startBody)]);
    const texts = await Promise.all(responses.map(r => r.text()));
    expect(responses.map(r => r.status)).toEqual([200, 200]); expect(texts[1]).toBe(texts[0]);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1); expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
  });
  it("rolls back the complete start when the final receipt write fails", async () => {
    const f = await fixture();
    f.sql.run("CREATE TRIGGER fail_ce03_receipt BEFORE INSERT ON ce03_start_receipts BEGIN SELECT RAISE(ABORT,'injected receipt failure'); END");
    expect((await f.request("/__local/ce03/start", f.startBody)).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0); expect(f.scalar("SELECT COUNT(*) FROM run_snapshots")).toBe(0);
    expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);
    expect(f.scalar("SELECT status FROM operation_previews")).toBe("ACTIVE");
    f.sql.run("DROP TRIGGER fail_ce03_receipt");
    expect((await f.request("/__local/ce03/start", f.startBody)).status).toBe(200);
  });
  it("refuses competing new keys and a support edit injected at the atomic boundary", async () => {
    const f = await fixture();
    let injected = false;
    f.adapter.beforeBatch = () => {
      if (!injected) { injected = true; f.sql.run("UPDATE ce03_support_loadouts SET revision=revision+1"); }
    };
    expect((await f.request("/__local/ce03/start", f.startBody)).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0); expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);
    const other = await fixture();
    const responses = await Promise.all([other.request("/__local/ce03/start", other.startBody, "key-one"), other.request("/__local/ce03/start", other.startBody, "key-two")]);
    expect(responses.filter(r => r.status === 200)).toHaveLength(1);
    expect(other.scalar("SELECT COUNT(*) FROM runs")).toBe(1); expect(other.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
  });
  it("recovers a committed response even when the DB reports a lost acknowledgement", async () => {
    const f = await fixture(), batch = f.adapter.batch.bind(f.adapter);
    f.adapter.batch = async statements => {
      const result = await batch(statements);
      if (statements.some(s => s.sql.includes("INSERT INTO ce03_start_receipts"))) throw new Error("lost commit acknowledgement");
      return result;
    };
    const first = await f.request("/__local/ce03/start", f.startBody);
    expect(first.status).toBe(200); const raw = await first.text();
    expect(await (await f.request("/__local/ce03/start", f.startBody)).text()).toBe(raw);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
  it("allows explicit role selection but refuses absent effects and stale command attestation", async () => {
    const f = await fixture();
    const changed = { ...f.supportBody, expectedRevision: 1, slots: ["support_fire", "barrier", "ce03.role.smoke"].map((skillId, slot) => ({ slot, skillId })) };
    expect((await f.request("/__local/ce03/support-loadout", changed, "new-role")).status).toBe(200);
    const unavailable = await f.request("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 2 });
    expect(unavailable.status).toBe(409); expect(await unavailable.json()).toMatchObject({ errorCode: "ce03_support_effects_unavailable" });
    expect((await f.request("/__local/ce03/support-loadout", { ...f.supportBody, expectedRevision: 2 }, "restore-order")).status).toBe(200);
    const prepared = await (await f.request("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 3 })).json();
    const stale = await f.request("/__local/ce03/start", { ...f.startBody, expectedSupportRevision: 3, prepareSha256: prepared.prepareSha256 });
    expect(stale.status).toBe(409); expect(await stale.json()).toMatchObject({ errorCode: "ce03_consumer_evidence_mismatch" });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
  });
  it("keeps missing verifier, partial command coverage and foreign owner closed", async () => {
    const f = await fixture(), artifacts = await f.options.artifacts();
    const report = JSON.parse(artifacts!.reportJson); report.results = report.results.filter((r: { coverage: string }) => r.coverage !== "LEADER_CHARGE"); report.passed--;
    const partial = createCe03LocalHost({ ...f.options, artifacts: async () => ({ ...artifacts, reportJson: canonicalJson(report) }) });
    const send = (host: typeof partial, headers = f.headers, env = f.env) => host.request("http://127.0.0.1/__local/ce03/start", { method: "POST", headers: { ...headers, "Idempotency-Key": "original.start.key !" }, body: JSON.stringify(f.startBody) }, env);
    expect((await send(partial)).status).toBe(409);
    const missing = { ...f.options }; Reflect.deleteProperty(missing, "verifier");
    expect((await (await send(createCe03LocalHost(missing))).json()).errorCode).toBe("ce03_consumer_verifier_missing");
    expect((await send(f.host, f.headers, { ...f.env, DB: {} as D1Database })).status).toBe(403);
    const now = Date.now();
    f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('s-b','b','h-b','f-b',0,'ACTIVE',?,?)", [now, now + 60000]);
    const token = await createAccessToken({ secret: f.env.AUTH_SIGNING_KEY_CURRENT!, accountId: "b", sessionId: "s-b", nowMs: now, ttlMs: 60000 });
    const bhost = createCe03LocalHost({ ...f.options, allowedAccountIds: ["a", "b"] });
    expect((await send(bhost, { ...f.headers, Authorization: `Bearer ${token}` })).status).toBe(403);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
  });
  it("reuses original lifecycle and explicit claim on the isolated run, without mounting public start", async () => {
    const f = await fixture(), started = await (await f.request("/__local/ce03/start", f.startBody)).json();
    const lifecycle = await (await f.request(`/v1/runs/${started.runId}/lifecycle`)).json();
    expect(lifecycle).toMatchObject({ startResponse: started, nextAction: "RECOVER_COMBAT_CONSUMER" });
    expect((await f.request("/v1/runs/start", { operationPreviewId: f.startBody.operationPreviewId })).status).toBe(404);
    const outcome = { runId: started.runId, outcome: "VICTORY", currentStage: "Complete", currentWaveNumber: 0, completedWaveCount: 15,
      encounterType: "ORDINARY_15_WAVE", runtimeStateJson: "{}", tacticSelections: [], clientCompletedAt: new Date().toISOString() };
    // Synthetic lifecycle transport exercise, not a real battle result.
    const finishedResponse = await f.request(`/v1/runs/${started.runId}/finish`, outcome, "finish-original");
    expect(finishedResponse.status).toBe(200); const finished = await finishedResponse.json();
    expect(finished.accepted).toBe(true); expect(f.scalar("SELECT COUNT(*) FROM reward_transactions")).toBe(0);
    const claim = await f.request(`/v1/runs/${started.runId}/reward-preview/${finished.rewardPreviewId}/claim`, { runId: started.runId, rewardPreviewId: finished.rewardPreviewId }, "claim-original");
    expect(claim.status).toBe(200);
    expect(await (await f.request(`/v1/runs/${started.runId}/lifecycle`)).json()).toMatchObject({ nextAction: "RETURN_TO_LOBBY", reward: { status: "CLAIMED" }, startResponse: started });
  });
  it("replays a same-key commit between the initial receipt read and idempotency lookup", async () => {
    const f = await fixture(), prepare = f.adapter.prepare.bind(f.adapter);
    let injected = false, committed = "";
    f.adapter.prepare = sql => {
      const statement = prepare(sql);
      if (sql.startsWith("SELECT 1 FROM idempotency_records")) {
        const first = statement.first.bind(statement);
        statement.first = async <T>() => {
          if (!injected) { injected = true; committed = await (await f.request("/__local/ce03/start", f.startBody)).text(); }
          return first<T>();
        };
      }
      return statement;
    };
    const response = await f.request("/__local/ce03/start", f.startBody);
    expect(response.status).toBe(200); expect(await response.text()).toBe(committed);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
});
