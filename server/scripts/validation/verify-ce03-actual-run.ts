import { spawn } from "node:child_process";
import { strict as assert } from "node:assert";
import { createServer } from "node:http";
import { createHash, randomBytes } from "node:crypto";
import { mkdir, mkdtemp, readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ce03ProducerFixture, ce03Inputs } from "../../tests/helpers/ce03-producer-fixture";
import { body } from "../../tests/helpers/departure-fixture";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog, localLeaderChargePolicy } from "../../apps/game-api/src/ce03-command-contract";
import { loadPresentationFromAudit } from "../local/ce03-presentation-projection";
import { executeCe03ConsumerRunner } from "../local/ce03-executed-runner";

// Frozen actual local integration packet; never connects to a remote database.
const project = "E:/Projects/LASTRAIL/unity-project";
const packet = "C:/Users/dlstj/.codex/worktrees/5f93/LASTRAIL/QA/Ce03CommandIntegration/runner-20260912T035826";
const reviewedBundleSha256 = "13ed4e80b15615290038885cf383133e62a911c9731dfaa5c6da92f8fd393f09";
if (process.argv.length !== 2) throw new Error("No command-line overrides supported for this frozen diagnostic");
const sourceFiles = z.array(z.string()).length(558).parse(JSON.parse(await readFile(resolve(packet, "source-files.json"), "utf8")));
const files = [];
for (const path of [...sourceFiles].sort()) {
  if (!/^(Assets|Packages|ProjectSettings|QA)\//u.test(path) || path.includes("\\") || path.split("/").some(p => !p || p === "." || p === "..")) throw new Error("Invalid reviewed source path");
  const content = await readFile(resolve(project, path), "utf8"); files.push({ path, content, sha256: await sha256Hex(content) });
}
if (await sha256Hex(canonicalJson({ schemaVersion: "ce03-source-bundle-v1", files })) !== reviewedBundleSha256) throw new Error("Frozen reviewed source changed");
const presentation = await loadPresentationFromAudit(resolve(project, "QA/Ce03CommandIntegration/presentation-closure-audit.json"), project,
  { "com.unity.ugui": resolve(project, "Library/PackageCache/com.unity.ugui@20629863fcab") });
const f = await ce03ProducerFixture(); f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
const now = Date.now(), ttlMs = 8 * 60 * 60 * 1000, secret = randomBytes(32).toString("base64url");
f.env.AUTH_SIGNING_KEY_CURRENT = secret; f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'", [now + ttlMs]);
const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs });
const hostOptions = { disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
  inputs: ce03Inputs(), supportCatalog: localSupportCatalog, leaderPolicy: localLeaderChargePolicy,
  presentation: { mode: "APPROVED_VIEW_V1" as const, manifest: presentation.manifest } };
let host = createCe03LocalHost(hostOptions);
let origin = "";
const server = createServer(async (req, res) => {
  try {
    const chunks: Buffer[] = []; let size = 0;
    for await (const chunk of req) { const bytes = Buffer.from(chunk); size += bytes.length; if (size > 16384) { res.writeHead(413); res.end(); return; } chunks.push(bytes); }
    const headers = new Headers(); for (const [name, value] of Object.entries(req.headers)) if (typeof value === "string") headers.set(name, value);
    const method = req.method ?? "GET";
    const response = await host.request(new URL(req.url ?? "/", origin).href, { method, headers,
      ...(method === "GET" || method === "HEAD" ? {} : { body: Buffer.concat(chunks) }) }, f.env);
    res.writeHead(response.status, Object.fromEntries(response.headers)); res.end(Buffer.from(await response.arrayBuffer()));
  } catch { res.writeHead(503); res.end(); }
});
await new Promise<void>(done => server.listen(0, "127.0.0.1", done));
const address = server.address(); if (!address || typeof address === "string") throw new Error("Missing loopback address");
origin = `http://127.0.0.1:${address.port}`;
const outputRoot = resolve("audit-results/ce03-actual-runner"); await mkdir(outputRoot, { recursive: true });
const output = await mkdtemp(resolve(outputRoot, "actual-integration-"));
try {
  const post = (path: string, data: unknown, key: string) => fetch(origin + path, { method: "POST",
    headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json", "Idempotency-Key": key }, body: JSON.stringify(data) });
  const denied = await fetch(origin + "/__local/ce03/support-loadout");
  const saved = await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
    expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) }, "actual-integration-support");
  if (denied.status !== 401 || saved.status !== 200) throw new Error("Local auth/support boundary failed");
  const response = await post("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1 }, "actual-integration-prepare");
  if (response.status !== 200) throw new Error("Local prepare failed");
  const prepared = await response.json() as Parameters<typeof executeCe03ConsumerRunner>[0]["prepare"];
  await writeFile(resolve(output, "prepare.json"), JSON.stringify(prepared, null, 2) + "\n");
  const executed = await executeCe03ConsumerRunner({ executable: "D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe", cwd: project, sourceRoot: project,
    sourceFiles, outputRoot: output, prepare: prepared, buildArgs: paths => ["-batchmode", "-nographics", "-projectPath", project,
      "-executeMethod", "Ce03ConsumerEvidenceRunner.Run", "-ce03Output", paths.outputDirectory, "-ce03SourceBundle", paths.sourceBundlePath,
      "-ce03Prepare", paths.preparePath, "-logFile", resolve(output, "unity.log")] });
  if (executed.execution.sourceBundleSha256 !== reviewedBundleSha256) throw new Error("Executed source differs from reviewed packet");
  // Independent source review cleared the two historical gaps for this exact
  // packet. Install only the fresh process-bound verifier on this same DB.
  host = createCe03LocalHost({ ...hostOptions, artifacts: async () => executed.artifacts, verifier: executed.verifier });
  const get = (path: string) => fetch(origin + path, { headers: { Authorization: `Bearer ${token}` } });
  const quoteResponse = await post("/v1/operations/preview", body, "actual-quote"); assert.equal(quoteResponse.status, 200);
  const quote = z.object({ operationPreviewId: z.string() }).parse(await quoteResponse.json());
  const startBody = { operationPreviewId: quote.operationPreviewId, expectedSupportRevision: 1, prepareSha256: prepared.prepareSha256 };
  const startKey = "actual.original.start.key !";
  // Persist the original request before the one sender issues it.
  await writeFile(resolve(output, "start-request.json"), canonicalJson({ owner: "a", origin, key: startKey, body: startBody, prepared }));
  const started = await post("/__local/ce03/start", startBody, startKey);
  const responseRaw = await started.text(); await writeFile(resolve(output, "start-response.json"), responseRaw);
  assert.equal(started.status, 200, responseRaw);
  const accepted = z.object({ accepted: z.literal(true), runId: z.string(), combat: z.object({ seed: z.object({ value: z.number().int() }), snapshotSha256: z.string() }) }).parse(JSON.parse(responseRaw));
  assert.equal(f.scalar("SELECT COUNT(*) FROM runs"), 1); assert.equal(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'"), 20);
  const lookupResponse = await post("/__local/ce03/start-result", { ...startBody, startIdempotencyKey: startKey }, "actual-lookup");
  assert.equal(lookupResponse.status, 200);
  const lookup = z.object({ startResponse: z.unknown() }).parse(await lookupResponse.json()); assert.deepEqual(lookup.startResponse, JSON.parse(responseRaw));
  assert.equal(await (await post("/__local/ce03/start", startBody, startKey)).text(), responseRaw);
  const lifecycleBefore = await (await get(`/v1/runs/${accepted.runId}/lifecycle`)).json();
  assert.equal(z.object({ nextAction: z.string() }).parse(lifecycleBefore).nextAction, "RECOVER_COMBAT_CONSUMER");
  const original = { version: "ce03-original-start-v1", owner: "a", origin, key: startKey, body: startBody, responseRaw, prepared };
  const originalPath = resolve(output, "original-start.json"), sourcePath = resolve(output, "accepted-source.json");
  await writeFile(originalPath, canonicalJson(original)); await writeFile(sourcePath, executed.artifacts.algorithmSource);
  const consumerOutput = resolve(output, "actual-consumer"); await mkdir(consumerOutput);
  const executableHash = async () => createHash("sha256").update(await readFile("D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe")).digest("hex");
  assert.equal(await executableHash(), executed.execution.executableSha256);
  const consumerStartedAt = Date.now();
  await new Promise<void>((done, reject) => {
    const child = spawn("D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe", ["-batchmode", "-nographics", "-projectPath", project,
      "-executeMethod", "Ce03AcceptedRunConsumer.Run", "-ce03Output", consumerOutput, "-ce03SourceBundle", sourcePath,
      "-ce03Start", originalPath, "-ce03Owner", "a", "-ce03Origin", origin + "/", "-logFile", resolve(output, "actual-consumer.log")],
    { cwd: project, shell: false, windowsHide: true, stdio: "ignore" });
    let timedOut = false; const timer = setTimeout(() => { timedOut = true; child.kill(); }, 600000);
    child.once("error", error => { clearTimeout(timer); reject(error); });
    child.once("close", code => { clearTimeout(timer); if (timedOut || code !== 0) reject(new Error("Actual consumer failed; do not finish")); else done(); });
  });
  // Independently recheck the frozen source after the actual consumer exits.
  assert.equal(await executableHash(), executed.execution.executableSha256);
  for (const file of files) assert.equal(await sha256Hex(await readFile(resolve(project, file.path), "utf8")), file.sha256);
  const completionRaw = await readFile(resolve(consumerOutput, "completion.json"), "utf8");
  const completion = z.object({ actualAccountRun: z.literal(true), scope: z.literal("DISPOSABLE_LOCAL"), runId: z.string(), seed: z.number().int(),
    originalSnapshotSha256: z.string(), algorithmSourceSha256: z.string(), prepareSha256: z.string(), completedWaveCount: z.number().int().min(0).max(15),
    outcome: z.enum(["VICTORY", "FAILURE"]), modelSha256: z.string(), durableRevision: z.number().int().positive(), restoredDuringRun: z.literal(true) }).parse(JSON.parse(completionRaw));
  assert.equal(completion.runId, accepted.runId); assert.equal(completion.seed, accepted.combat.seed.value);
  assert.equal(completion.originalSnapshotSha256, accepted.combat.snapshotSha256); assert.equal(completion.algorithmSourceSha256, reviewedBundleSha256);
  assert.equal(completion.prepareSha256, prepared.prepareSha256);
  assert.equal(await sha256Hex(await readFile(resolve(consumerOutput, "model-final.json"), "utf8")), completion.modelSha256);
  const intent = z.object({ method: z.literal("POST"), path: z.string(), idempotencyKey: z.string().min(1), sent: z.literal(false), body: z.object({
    runId: z.string(), outcome: z.enum(["VICTORY", "FAILURE"]), completedWaveCount: z.number().int(), runtimeStateJson: z.string() }).passthrough() })
    .parse(JSON.parse(await readFile(resolve(consumerOutput, "finish-intent.json"), "utf8")));
  assert.equal(intent.path, `/v1/runs/${accepted.runId}/finish`); assert.equal(intent.body.runId, accepted.runId);
  assert.equal(intent.body.outcome, completion.outcome); assert.equal(intent.body.completedWaveCount, completion.completedWaveCount);
  assert.equal(intent.body.runtimeStateJson, completionRaw);
  const finishedResponse = await post(intent.path, intent.body, intent.idempotencyKey);
  const finishRaw = await finishedResponse.text(); await writeFile(resolve(output, "finish-response.json"), finishRaw); assert.equal(finishedResponse.status, 200, finishRaw);
  const finished = z.object({ accepted: z.literal(true), rewardPreviewId: z.string() }).parse(JSON.parse(finishRaw));
  assert.equal(f.scalar("SELECT COUNT(*) FROM reward_transactions"), 0);
  const finishReplayResponse = await post(intent.path, intent.body, intent.idempotencyKey); assert.equal(finishReplayResponse.status, 200);
  const finishReplay = await finishReplayResponse.json() as Record<string, unknown>;
  assert.equal(finishReplay.resultStatus, "DUPLICATE_REPLAY"); assert.equal(finishReplay.accepted, false);
  for (const field of ["runId", "rewardPreviewId", "serverReceivedAt", "balanceVersion", "staminaRefund", "runStartVersionSnapshot", "runFinishVersionSnapshot"])
    assert.deepEqual(finishReplay[field], (JSON.parse(finishRaw) as Record<string, unknown>)[field]);
  await writeFile(resolve(output, "finish-replay.json"), JSON.stringify(finishReplay));
  await writeFile(resolve(output, "finish-ack.json"), canonicalJson({ key: intent.idempotencyKey, responseRaw: finishRaw }));
  const claimPath = `/v1/runs/${accepted.runId}/reward-preview/${finished.rewardPreviewId}/claim`;
  const claimBody = { runId: accepted.runId, rewardPreviewId: finished.rewardPreviewId };
  const claimedResponse = await post(claimPath, claimBody, "actual-original-explicit-claim");
  const claimRaw = await claimedResponse.text(); await writeFile(resolve(output, "claim-response.json"), claimRaw); assert.equal(claimedResponse.status, 200, claimRaw);
  const claimReplayResponse = await post(claimPath, claimBody, "actual-original-explicit-claim"); assert.equal(claimReplayResponse.status, 200);
  const claimReplay = await claimReplayResponse.json() as Record<string, unknown>; assert.equal(claimReplay.status, "DUPLICATE_REPLAY");
  for (const field of ["runId", "rewardPreviewId", "rewardClaimId", "serverClaimedAt", "balanceVersion"])
    assert.deepEqual(claimReplay[field], (JSON.parse(claimRaw) as Record<string, unknown>)[field]);
  assert.equal(f.scalar("SELECT COUNT(*) FROM reward_transactions"), 1);
  await writeFile(resolve(output, "claim-replay.json"), JSON.stringify(claimReplay));
  const lifecycle = await (await get(`/v1/runs/${accepted.runId}/lifecycle`)).json();
  assert.equal(z.object({ nextAction: z.string(), reward: z.object({ status: z.literal("CLAIMED") }) }).parse(lifecycle).nextAction, "RETURN_TO_LOBBY");
  assert.deepEqual(z.object({ startResponse: z.unknown() }).parse(lifecycle).startResponse, JSON.parse(responseRaw));
  assert.equal(f.scalar("SELECT COUNT(*) FROM runs"), 1); assert.equal(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'"), 1);
  const summary = { status: "ACTUAL_LOCAL_RUN_CLAIMED", output, execution: executed.execution, consumerStartedAt, consumerFinishedAt: Date.now(),
    completion, lifecycle, originalStartByteReplay: true, originalFinishIdentityReplay: true, originalClaimIdentityReplay: true,
    providerInstalled: true, database: "DISPOSABLE_IN_MEMORY", remoteDeployment: false, visualCapturePerformed: false,
    presentationVerification: presentation.verification };
  await writeFile(resolve(output, "verification.json"), JSON.stringify(summary, null, 2) + "\n");
  console.log(JSON.stringify({ status: summary.status, output, completion, nextAction: "RETURN_TO_LOBBY", rewardStatus: "CLAIMED" }));
} finally { await new Promise<void>((done, reject) => server.close(error => error ? reject(error) : done())); f.sql.close(); }
