import { createServer } from "node:http";
import { randomBytes } from "node:crypto";
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

// One frozen, reviewed diagnostic packet. Review found accepted-envelope and
// START journal coverage gaps. This executable deliberately NEVER installs its
// returned verifier in a host, even if the external runner reports all PASS.
const project = "D:/Project/LASTRAIL/unity-project";
const packet = "D:/Project/LASTRAIL/Worktrees/5f93/LASTRAIL/QA/Ce03CommandIntegration/runner-20260912T033903";
const reviewedBundleSha256 = "be341ad1bfe76ebe4828cf0a7b523c305f69407b12f1bd188215b57359327546";
if (process.argv.length !== 2) throw new Error("No command-line overrides supported for this frozen diagnostic");
const sourceFiles = z.array(z.string()).length(554).parse(JSON.parse(await readFile(resolve(packet, "source-files.json"), "utf8")));
const files = [];
for (const path of [...sourceFiles].sort()) {
  if (!/^(Assets|Packages|ProjectSettings)\//u.test(path) || path.includes("\\") || path.split("/").some(p => !p || p === "." || p === "..")) throw new Error("Invalid reviewed source path");
  const content = await readFile(resolve(project, path), "utf8"); files.push({ path, content, sha256: await sha256Hex(content) });
}
if (await sha256Hex(canonicalJson({ schemaVersion: "ce03-source-bundle-v1", files })) !== reviewedBundleSha256) throw new Error("Frozen reviewed source changed");
const presentation = await loadPresentationFromAudit(resolve(project, "QA/Ce03CommandIntegration/presentation-closure-audit.json"), project,
  { "com.unity.ugui": resolve(project, "Library/PackageCache/com.unity.ugui@20629863fcab") });
const f = await ce03ProducerFixture(); f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
const now = Date.now(), ttlMs = 8 * 60 * 60 * 1000, secret = randomBytes(32).toString("base64url");
f.env.AUTH_SIGNING_KEY_CURRENT = secret; f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'", [now + ttlMs]);
const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs });
const host = createCe03LocalHost({ disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
  inputs: ce03Inputs(), supportCatalog: localSupportCatalog, leaderPolicy: localLeaderChargePolicy,
  presentation: { mode: "APPROVED_VIEW_V1", manifest: presentation.manifest } });
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
const output = await mkdtemp(resolve(outputRoot, "closed-review-"));
try {
  const post = (path: string, data: unknown, key: string) => fetch(origin + path, { method: "POST",
    headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json", "Idempotency-Key": key }, body: JSON.stringify(data) });
  const denied = await fetch(origin + "/__local/ce03/support-loadout");
  const saved = await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
    expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) }, "closed-review-support");
  if (denied.status !== 401 || saved.status !== 200) throw new Error("Local auth/support boundary failed");
  const response = await post("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1 }, "closed-review-prepare");
  if (response.status !== 200) throw new Error("Local prepare failed");
  const prepared = await response.json() as Parameters<typeof executeCe03ConsumerRunner>[0]["prepare"];
  await writeFile(resolve(output, "prepare.json"), JSON.stringify(prepared, null, 2) + "\n");
  const executed = await executeCe03ConsumerRunner({ executable: "D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe", cwd: project, sourceRoot: project,
    sourceFiles, outputRoot: output, prepare: prepared, buildArgs: paths => ["-batchmode", "-nographics", "-projectPath", project,
      "-executeMethod", "Ce03ConsumerEvidenceRunner.Run", "-ce03Output", paths.outputDirectory, "-ce03SourceBundle", paths.sourceBundlePath,
      "-ce03Prepare", paths.preparePath, "-logFile", resolve(output, "unity.log")] });
  if (executed.execution.sourceBundleSha256 !== reviewedBundleSha256) throw new Error("Executed source differs from reviewed packet");
  // Test the same host after execution: review gaps keep provider installation
  // disabled. No test-seed Victory is submitted as a live account finish.
  const quote = z.object({ operationPreviewId: z.string() }).parse(await (await post("/v1/operations/preview", body, "closed-review-quote")).json());
  const start = await post("/__local/ce03/start", { operationPreviewId: quote.operationPreviewId, expectedSupportRevision: 1,
    prepareSha256: prepared.prepareSha256 }, "closed-review-start");
  const refused = z.object({ errorCode: z.string() }).parse(await start.json());
  if (start.status !== 409 || refused.errorCode !== "ce03_consumer_artifacts_missing" || f.scalar("SELECT COUNT(*) FROM runs") !== 0 ||
    f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'") !== 30) throw new Error("Closed review boundary failed");
  const report = JSON.parse(executed.artifacts.reportJson);
  const summary = { status: "EXECUTED_REVIEW_BLOCKED_NO_PROVIDER", output, execution: executed.execution,
    prepareSha256: prepared.prepareSha256, reportSha256: await sha256Hex(executed.artifacts.reportJson), runnerReportedPassed: report.passed,
    reviewGaps: ["R6_ACCEPTED_CONTENT_VERSION_MISMATCH", "START_JOURNAL_RECOVERY_NOT_EXECUTED"],
    unauthorizedStatus: denied.status, startStatus: start.status, errorCode: refused.errorCode, providerInstalled: false,
    actualAccountRun: false, runCreated: false, fuel: 30, presentationVerification: presentation.verification };
  await writeFile(resolve(output, "verification.json"), JSON.stringify(summary, null, 2) + "\n"); console.log(JSON.stringify(summary));
} finally { await new Promise<void>((done, reject) => server.close(error => error ? reject(error) : done())); f.sql.close(); }
