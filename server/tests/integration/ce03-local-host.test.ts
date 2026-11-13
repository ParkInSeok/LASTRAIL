import { describe, expect, it, vi } from "vitest";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ce03ProducerFixture, ce03Inputs } from "../helpers/ce03-producer-fixture";
import { body } from "../helpers/departure-fixture";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog, localLeaderChargePolicy } from "../../apps/game-api/src/ce03-command-contract";
import { requiredCommandConsumerCoverage, requiredPresentationConsumerCoverage, requiredPlayerConsumerCoverage } from "../../apps/game-api/src/ce03-consumer-attestation";
import { readFileSync } from "node:fs";
import { executeCe03PlayerHost } from '../../scripts/local/ce03-player-host';
import * as executedRunner from '../../scripts/local/ce03-executed-runner';
import { createServer, request as httpRequest } from 'node:http';
import { presentationFixture } from "../helpers/ce03-presentation-fixture";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { mkdir, writeFile } from "node:fs/promises";

async function fixture(evidence = true, approved = false, lobbyBootstrap = false, player = false) {
  const f = await ce03ProducerFixture(lobbyBootstrap); f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
  const artifacts = async () => {
    if (!evidence) return undefined;
    // Synthetic executed-report stand-in: tests host wiring, NOT Unity conformance.
    const algorithmSource = "synthetic-host-test-code", checkpointSchema = "synthetic-host-test-checkpoint";
    const coverageList = player ? requiredPlayerConsumerCoverage : approved ? requiredPresentationConsumerCoverage : requiredCommandConsumerCoverage;
    const vectorsJson = canonicalJson({ cases: coverageList.map((coverage, i) => ({ id: `case.${i}`, coverage })) });
    const reportJson = canonicalJson({ schemaVersion: player ? "ce03-consumer-runner-report-v6" : approved ? "ce03-consumer-runner-report-v3" : "ce03-consumer-runner-report-v2", scope: "FULL_LOCAL_CONSUMER", runId: "synthetic.host.runner",
      ...(player ? { playerAssetManifestSha256: prepared.playerAssetBinding.manifestSha256, prepareSha256: prepared.prepareSha256 } : {}),
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
    supportCatalog: localSupportCatalog, leaderPolicy: localLeaderChargePolicy, artifacts, verifier: evidence ? verifier : undefined,
    ...(player ? { playerAssetManifestJson: readFileSync('D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BattleAssets424/manifest.json', 'utf8') } : {}) };
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
  it("serves the owner's lobby reads while retaining local auth and explicit route scope", async () => {
    const f = await fixture(false, false, true);
    for (const path of ["/v1/account/state", "/v1/wallet/crystals", "/v1/growth/config", "/v1/trains/loadout", "/v1/formation/ultimate"]) {
      const response = await f.request(path); expect(response.status, path).toBe(200);
      expect(response.headers.get("Cache-Control")).toBe("no-store");
      expect((await f.host.request(`http://127.0.0.1${path}`, {}, f.env)).status).toBe(401);
      expect((await f.host.request(`https://example.com${path}`, { headers: f.headers }, f.env)).status).toBe(403);
      expect((await f.request(path, {})).status).toBe(404);
    }
    expect((await f.request("/v1/account/state?accountId=b")).status).toBe(400);
    expect((await f.request("/v1/profile")).status).toBe(404);
    expect((await f.request("/v1/runs/start", {})).status).toBe(404);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);
  });
  it("pins Player assets only with matching executed Player coverage and preserves the original receipt", async () => {
    const f = await fixture(true, false, false, true);
    expect(f.prepared.ready).toBe(false);
    expect(f.prepared.playerAssetBinding.admissionGranted).toBe(false);
    const response = await f.request('/__local/ce03/start', f.startBody);
    expect(response.status).toBe(200);
    const raw = await response.text(), accepted = JSON.parse(raw);
    expect(accepted.combat.schemaVersion).toBe('ce03-accepted-player-combat-v1');
    expect(accepted.combat.contentManifest).toMatchObject({ schemaVersion: 'ce03-player-content-v1',
      resolver: { mode: 'UNITY_SERIALIZED_PLAYER_BINDING' }, playerAssetBinding: f.prepared.playerAssetBinding });
    expect(accepted.combat.snapshot.playerAssetBinding).toEqual(f.prepared.playerAssetBinding);
    expect(accepted.combat.snapshot.version).toBe('ce03-player-snapshot-v1');
    expect(accepted.combat.contentSha256).toBe(await sha256Hex(canonicalJson(accepted.combat.contentManifest)));
    expect(f.verifier.verify).toHaveBeenCalledWith(expect.objectContaining({ playerAssetManifestSha256: f.prepared.playerAssetBinding.manifestSha256,
      prepareSha256: f.prepared.prepareSha256 }));
    const restarted = createCe03LocalHost({ ...f.options, playerAssetManifestJson: undefined, verifier: undefined, artifacts: undefined });
    expect(await (await restarted.request('http://127.0.0.1/__local/ce03/start', { method: 'POST', headers: { ...f.headers, 'Idempotency-Key': 'original.start.key !' }, body: JSON.stringify(f.startBody) }, f.env)).text()).toBe(raw);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
  });
  it.each(['legacy', 'hash', 'prepare', 'coverage'])('rejects invalid Player %s evidence before consumption', async mode => {
    const f = await fixture(true, false, false, true), original = await f.options.artifacts();
    const report = JSON.parse(original!.reportJson);
    if (mode === 'legacy') report.schemaVersion = 'ce03-consumer-runner-report-v2';
    if (mode === 'hash') report.playerAssetManifestSha256 = '0'.repeat(64);
    if (mode === 'prepare') report.prepareSha256 = '0'.repeat(64);
    if (mode === 'coverage') { report.results = report.results.filter((r: {coverage:string}) => r.coverage !== 'PLAYER_EXIT_RELEASE'); report.passed--; }
    const host = createCe03LocalHost({ ...f.options, artifacts: async () => ({ ...original!, reportJson: canonicalJson(report) }) });
    const response = await host.request('http://127.0.0.1/__local/ce03/start', { method: 'POST', headers: { ...f.headers, 'Idempotency-Key': 'new-player-key' }, body: JSON.stringify(f.startBody) }, f.env);
    expect(response.status).toBe(409);
    expect(f.verifier.verify).not.toHaveBeenCalled();
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(0);
    expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(30);
  });
  it('restores the original Player roster without mixing a later lobby formation', async () => {
    const f = await fixture(true, false, false, true);
    const first = await f.request('/__local/ce03/start', f.startBody);
    expect(first.status).toBe(200);
    const raw = await first.text(), original = JSON.parse(raw);
    const verifiedCalls = f.verifier.verify.mock.calls.length;
    f.sql.run("UPDATE account_formation SET formation_json=?,revision=revision+1 WHERE account_id='a'",
      [JSON.stringify({ schemaVersion: 1, operators: [] })]);
    const options = { ...f.options };
    Reflect.deleteProperty(options, 'artifacts'); Reflect.deleteProperty(options, 'verifier');
    const restarted = createCe03LocalHost(options);
    const replay = await restarted.request('http://127.0.0.1/__local/ce03/start', {
      method: 'POST', headers: { ...f.headers, 'Idempotency-Key': 'original.start.key !' },
      body: JSON.stringify(f.startBody),
    }, f.env);
    expect(replay.status).toBe(200);
    expect(await replay.text()).toBe(raw);
    const recovered = await restarted.request('http://127.0.0.1/__local/ce03/start-result', {
      method: 'POST', headers: f.headers,
      body: JSON.stringify({ ...f.startBody, startIdempotencyKey: 'original.start.key !' }),
    }, f.env);
    expect(recovered.status).toBe(200);
    expect((await recovered.json()).startResponse).toEqual(original);
    expect(original.combat.snapshot.ownedSelection.formation.operators).toHaveLength(5);
    expect(f.verifier.verify).toHaveBeenCalledTimes(verifiedCalls);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
  });
  it.each(['before-handler', 'after-commit'] as const)('recovers one actual socket loss %s with the original Player START key', async loss => {
    const f = await fixture(true, false, false, true);
    const requests: { key: string; body: string }[] = [];
    let committedRaw = '', handlerCalls = 0;
    const errors: unknown[] = [];
    const server = createServer(async (req, res) => {
      try {
        const chunks: Buffer[] = [];
        for await (const chunk of req) chunks.push(Buffer.from(chunk));
        const raw = Buffer.concat(chunks).toString('utf8');
        requests.push({ key: String(req.headers['idempotency-key']), body: raw });
        if (requests.length === 1 && loss === 'before-handler') { req.socket.destroy(); return; }
        handlerCalls++;
        const headers = new Headers();
        for (const [name, value] of Object.entries(req.headers)) if (typeof value === 'string') headers.set(name, value);
        const result = await f.host.request('http://127.0.0.1/__local/ce03/start', {
          method: 'POST', headers, body: raw,
        }, f.env);
        const responseBody = await result.text();
        if (requests.length === 1 && loss === 'after-commit') {
          if (result.status !== 200) throw new Error(responseBody);
          committedRaw = responseBody;
          req.socket.destroy(); return; // No HTTP headers/body delivered after the DB commit.
        }
        res.writeHead(result.status, { 'Content-Type': 'application/json' }); res.end(responseBody);
      } catch (error) { errors.push(error); req.socket.destroy(); }
    });
    try {
      await new Promise<void>((resolve, reject) => { server.once('error', reject); server.listen(0, '127.0.0.1', resolve); });
      const address = server.address(); if (!address || typeof address === 'string') throw new Error('No TCP address');
      const send = () => new Promise<{ status: number; raw: string }>((resolve, reject) => {
        const req = httpRequest({ hostname: '127.0.0.1', port: address.port, path: '/__local/ce03/start', method: 'POST',
          headers: { ...f.headers, 'Idempotency-Key': 'socket-loss-original', Connection: 'close' }, agent: false }, response => {
          const chunks: Buffer[] = []; response.on('data', chunk => chunks.push(Buffer.from(chunk)));
          response.once('error', reject); response.once('end', () => resolve({ status: response.statusCode!, raw: Buffer.concat(chunks).toString('utf8') }));
        });
        req.once('error', reject); req.setTimeout(5000, () => req.destroy(new Error('TCP timeout')));
        req.end(JSON.stringify(f.startBody));
      });
      await expect(send()).rejects.toMatchObject({ code: 'ECONNRESET' });
      expect(errors).toEqual([]);
      expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(loss === 'after-commit' ? 1 : 0);
      expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(loss === 'after-commit' ? 1 : 0);
      const recovered = await send(); // Exactly one retry, no implicit client retry.
      expect(recovered.status).toBe(200);
      if (loss === 'after-commit') expect(recovered.raw).toBe(committedRaw);
      expect(requests).toHaveLength(2); expect(requests[1]).toEqual(requests[0]);
      expect(handlerCalls).toBe(loss === 'after-commit' ? 2 : 1);
      expect(f.verifier.verify).toHaveBeenCalledTimes(1);
      expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
      expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
      expect(f.scalar('SELECT COUNT(*) FROM reward_transactions')).toBe(0);
      expect(errors).toEqual([]);
    } finally {
      server.closeAllConnections();
      if (server.listening) await new Promise<void>((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
      f.sql.close();
    }
  });
  it('preserves a committed Player run when receipt validation is temporarily unavailable', async () => {
    const f = await fixture(true, false, false, true);
    const started = await f.request('/__local/ce03/start', f.startBody);
    expect(started.status).toBe(200);
    const raw = await started.text(), committed = JSON.parse(raw);
    const key = 'original.start.key !';
    const calls = f.verifier.verify.mock.calls.length;
    // Server integrity-error boundary only; this is not a client journal fault.
    expect(() => f.sql.run('UPDATE ce03_start_receipts SET response_sha256=? WHERE account_id=? AND idempotency_key=?',
      ['0'.repeat(64), 'a', key])).toThrow('ce03 start receipt immutable');
    const prepare = f.adapter.prepare.bind(f.adapter);
    f.adapter.prepare = sql => {
      const statement = prepare(sql);
      if (sql.startsWith('SELECT c.*,r.account_id AS run_owner')) {
        const first = statement.first.bind(statement);
        statement.first = async <T>() => {
          const row = await first<T>();
          return row === null ? null : { ...row, snapshot_json: null };
        };
      }
      return statement;
    };
    const unavailable = await f.request('/__local/ce03/start-result', { ...f.startBody, startIdempotencyKey: key });
    expect(unavailable.status).toBe(409);
    expect(await unavailable.json()).toMatchObject({ accepted: false, errorCode: 'ce03_snapshot_unavailable' });
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
    f.adapter.prepare = prepare;
    const recovered = await f.request('/__local/ce03/start-result', { ...f.startBody, startIdempotencyKey: key });
    expect(recovered.status).toBe(200);
    expect(await recovered.json()).toEqual({ lookupStatus: 'COMMITTED', startResponse: committed, newAttemptAuthorized: false });
    expect(f.verifier.verify).toHaveBeenCalledTimes(calls);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
    expect(f.scalar('SELECT COUNT(*) FROM reward_transactions')).toBe(0);
  });
  it('isolates a committed Player receipt from another authenticated account using the same key', async () => {
    const f = await fixture(true, false, false, true);
    const start = await f.request('/__local/ce03/start', f.startBody);
    expect(start.status).toBe(200); const original = await start.json();
    const now = Date.now();
    f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('isolation-b','b','hb','fb',0,'ACTIVE',?,?)", [now, now + 60000]);
    const token = await createAccessToken({ secret: f.env.AUTH_SIGNING_KEY_CURRENT!, accountId: 'b', sessionId: 'isolation-b', nowMs: now, ttlMs: 60000 });
    const host = createCe03LocalHost({ ...f.options, allowedAccountIds: ['a', 'b'] });
    const headers = { ...f.headers, Authorization: `Bearer ${token}` };
    const lookupBody = { ...f.startBody, startIdempotencyKey: 'original.start.key !' };
    const foreign = await host.request('http://127.0.0.1/__local/ce03/start-result', { method: 'POST', headers, body: JSON.stringify(lookupBody) }, f.env);
    expect(foreign.status).toBe(200);
    expect(await foreign.json()).toEqual({ lookupStatus: 'NOT_FOUND', startResponse: null, newAttemptAuthorized: false });
    expect((await host.request(`http://127.0.0.1/v1/runs/${original.runId}/lifecycle`, { headers }, f.env)).status).toBe(403);
    const owner = await host.request('http://127.0.0.1/__local/ce03/start-result', { method: 'POST', headers: f.headers, body: JSON.stringify(lookupBody) }, f.env);
    expect(owner.status).toBe(200);
    expect((await owner.json()).startResponse).toEqual(original);
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
  });
  it('installs a Player host only after execution identity and full v6 attestation succeed', async () => {
    const f = await fixture(true, false, false, true);
    const { artifacts: supply, verifier, ...hostOptions } = f.options;
    // Orchestration fixture only: actual process identity is tested separately.
    const result = { artifacts: await supply(), verifier } as unknown as Awaited<ReturnType<typeof executedRunner.executeCe03ConsumerRunner>>;
    const runner = vi.spyOn(executedRunner, 'executeCe03ConsumerRunner').mockResolvedValue(result);
    try {
      const options = { executable: 'unused-synthetic', cwd: 'unused', sourceRoot: 'unused', sourceFiles: ['fixture'], outputRoot: 'unused',
        prepare: f.prepared, player: {manifestJson: f.options.playerAssetManifestJson!,buildRoot:'unused',buildManifestPath:'unused'}, buildArgs: () => [] };
      await expect(executeCe03PlayerHost({...hostOptions,playerAssetManifestJson:'different'},options)).rejects.toThrow();
      expect(runner).not.toHaveBeenCalled();
      verifier!.verify.mockResolvedValueOnce(false);
      await expect(executeCe03PlayerHost(hostOptions,options)).rejects.toThrow();
      expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(0);
      const installed = await executeCe03PlayerHost(hostOptions,options);
      const response=await installed.host.request('http://127.0.0.1/__local/ce03/start',{method:'POST',headers:{...f.headers,'Idempotency-Key':'installed-player'},body:JSON.stringify(f.startBody)},f.env);
      expect(response.status).toBe(200);
      expect((await response.json()).combat.schemaVersion).toBe('ce03-accepted-player-combat-v1');
      expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
    } finally { runner.mockRestore(); }
  });
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
  it("recovers the original entry assets after the current presentation source disappears without preparing or consuming again", async () => {
    const f = await fixture(true, true);
    const first = await f.request("/__local/ce03/start", f.startBody);
    expect(first.status).toBe(200);
    const raw = await first.text(), accepted = JSON.parse(raw);
    const original = accepted.combat;
    expect(original.contentManifest.presentationManifest).toEqual(f.prepared.presentationManifest);
    expect(original.snapshot.ownedSelection).toEqual(f.prepared.ownedSelection);
    expect(original.snapshot.accountId).toBe("a");
    expect(original.snapshot.runId).toBe(accepted.runId);
    expect(original.contentManifest.resolver).toEqual({ mode: "UNITY_EDITOR_ASSET_DATABASE", playerBuildAllowed: false });
    expect(original.contentSha256).toBe(await sha256Hex(canonicalJson(original.contentManifest)));
    expect(original.snapshotSha256).toBe(await sha256Hex(canonicalJson(original.snapshot)));
    // A new host instance has no current presentation or execution evidence.
    // This is in-process recovery, not a Player restart or real asset loading.
    const host = createCe03LocalHost({ ...f.options,
      presentation: { mode: "APPROVED_VIEW_V1" }, artifacts: undefined, verifier: undefined });
    const before = f.sql.export(), verifyCalls = f.verifier.verify.mock.calls.length;
    const request = (path: string, payload: unknown) => host.request(`http://127.0.0.1${path}`, {
      method: "POST", headers: { ...f.headers, "Idempotency-Key": "original.start.key !" },
      body: JSON.stringify(payload),
    }, f.env);
    const lookup = await request("/__local/ce03/start-result", { ...f.startBody, startIdempotencyKey: "original.start.key !" });
    expect(lookup.status).toBe(200);
    expect(await lookup.json()).toEqual({ lookupStatus: "COMMITTED", startResponse: accepted, newAttemptAuthorized: false });
    expect(await (await request("/__local/ce03/start", f.startBody)).text()).toBe(raw);
    const prepare = await request("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1 });
    expect(prepare.status).toBe(409);
    expect(await prepare.json()).toMatchObject({ errorCode: "ce03_presentation_manifest_missing" });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.verifier.verify).toHaveBeenCalledTimes(verifyCalls);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
    expect(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")).toBe(20);
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
    const f = await fixture(), originalRaw = await (await f.request("/__local/ce03/start", f.startBody)).text();
    const started = JSON.parse(originalRaw);
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
    // Leaving the pending UI queue must not remove the server recovery original.
    expect(() => f.sql.run('DELETE FROM ce03_start_receipts WHERE run_id=?', [started.runId])).toThrow('ce03 start receipt immutable');
    const verifiedCalls = f.verifier.verify.mock.calls.length;
    const options = { ...f.options };
    Reflect.deleteProperty(options, 'artifacts'); Reflect.deleteProperty(options, 'verifier');
    const restarted = createCe03LocalHost(options);
    const replay = await restarted.request('http://127.0.0.1/__local/ce03/start', {
      method: 'POST', headers: { ...f.headers, 'Idempotency-Key': 'original.start.key !' }, body: JSON.stringify(f.startBody),
    }, f.env);
    expect(replay.status).toBe(200); expect(await replay.text()).toBe(originalRaw);
    const restoredLifecycle = await restarted.request(`http://127.0.0.1/v1/runs/${started.runId}/lifecycle`, { headers: f.headers }, f.env);
    expect(restoredLifecycle.status).toBe(200);
    expect(await restoredLifecycle.json()).toMatchObject({ version: 'run-lifecycle-v1', runId: started.runId,
      nextAction: 'RETURN_TO_LOBBY', startResponse: started, reward: { status: 'CLAIMED' },
      grantsOnRead: false, newAttemptAuthorized: false });
    expect(f.verifier.verify).toHaveBeenCalledTimes(verifiedCalls);
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
    expect(f.scalar('SELECT COUNT(*) FROM reward_transactions')).toBe(1);
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
