import { createServer } from "node:http";
import { randomBytes } from "node:crypto";
import { Buffer } from "node:buffer";
import { mkdir, mkdtemp, readFile, stat, writeFile, rename } from "node:fs/promises";
import { createInterface } from "node:readline";
import { resolve } from "node:path";
import { z } from "zod";
import { ce03ProducerFixture, ce03Inputs } from "../../tests/helpers/ce03-producer-fixture";
import { body } from "../../tests/helpers/departure-fixture";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog, localLeaderChargePolicy, commandContractSchema } from "../../apps/game-api/src/ce03-command-contract";
import { id, sha } from "../../apps/game-api/src/ce03-local-schema";
import { loadPresentationFromAudit } from "../local/ce03-presentation-projection";
import {loadRailPresentationFromAudit} from "../local/ce03-rail-presentation";
import { executeCe03ConsumerRunner } from "../local/ce03-executed-runner";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { attestLocalConsumer } from "../../apps/game-api/src/ce03-consumer-attestation";
import { assertUnityExited, trustedFinishCe03 } from "../local/ce03-trusted-finish";

// Explicit disposable local account bootstrap. No remote DB argument or provider
// bypass. This executable NEVER installs a synthetic accepting verifier.
const args = process.argv.slice(2), flags = new Map<string, string>();
for (let i = 0; i < args.length; i++) {
  const arg = args[i]!;
  if (flags.has(arg) || !["--rail-presentation-zone02-v1","--rail-zone02-manifest","--rail-presentation-v1","--rail-presentation-extension","--rail-selection-manifest","--rail-journey", "--self-check", "--legacy-r4", "--lobby-bootstrap", "--dynamic-resource-closure", "--presentation-audit", "--unity-project", "--ugui-package-root", "--reviewed-source-files", "--reviewed-source-sha256"].includes(arg)) throw new Error("Invalid or duplicate argument");
  const value = ["--rail-presentation-zone02-v1","--rail-presentation-v1","--rail-journey", "--self-check", "--legacy-r4", "--lobby-bootstrap", "--dynamic-resource-closure"].includes(arg) ? "true" : args[++i];
  if (!value || value.startsWith("--")) throw new Error("Missing argument value"); flags.set(arg, value);
}
const selfCheck = flags.has("--self-check"), legacy = flags.has("--legacy-r4");
const rail = flags.has("--rail-journey");
if (rail && ["--legacy-r4", "--reviewed-source-files", "--reviewed-source-sha256", "--dynamic-resource-closure"].some(k => flags.has(k)))
  throw new Error("Rail local profile cannot enable legacy admission or reviewed provider commands");
const presentationKeys = ["--presentation-audit", "--unity-project", "--ugui-package-root"];
const zone02=flags.has("--rail-presentation-zone02-v1");
if(zone02&&flags.has("--rail-presentation-v1"))throw new Error("Rail presentation profiles are mutually exclusive");
if(zone02!==flags.has("--rail-zone02-manifest"))throw new Error("Zone02 requires its explicit profile and selection manifest together");
const currentRailPresentation=flags.has("--rail-presentation-v1")||zone02;
if(currentRailPresentation&&(!rail||[...presentationKeys,"--rail-presentation-extension","--rail-selection-manifest"].some(k=>!flags.has(k))))
  throw new Error("Rail presentation requires rail journey, audit, extension, selected manifest and both physical roots");
if(!currentRailPresentation&&["--rail-presentation-extension","--rail-selection-manifest"].some(k=>flags.has(k)))throw new Error("Rail presentation inputs require explicit profile");
const railWithoutPresentation = rail && (currentRailPresentation||presentationKeys.every(k => !flags.has(k)));
const reviewed = flags.has("--reviewed-source-files");
if (reviewed !== flags.has("--reviewed-source-sha256") || reviewed && (legacy || selfCheck)) throw new Error("Reviewed source pair requires persistent Approved mode");
if (reviewed) sha.parse(flags.get("--reviewed-source-sha256"));
if (flags.has("--dynamic-resource-closure") && !reviewed) throw new Error("Dynamic resource closure requires reviewed persistent mode");
if (legacy && flags.size > (selfCheck ? 2 : 1)) throw new Error("Legacy and presentation arguments cannot be combined");
if (!legacy && !railWithoutPresentation && presentationKeys.some(k => !flags.has(k)))
  throw new Error("Approved view requires --presentation-audit, --unity-project and --ugui-package-root. Historical regression requires explicit --legacy-r4.");
const presentation = legacy || railWithoutPresentation ? undefined : await loadPresentationFromAudit(flags.get("--presentation-audit")!, flags.get("--unity-project")!,
  { "com.unity.ugui": flags.get("--ugui-package-root")! });
const railPresentation=currentRailPresentation?await loadRailPresentationFromAudit({auditPath:flags.get("--presentation-audit")!,extensionPath:flags.get("--rail-presentation-extension")!,
  selectionPath:flags.get("--rail-selection-manifest")!,projectRoot:flags.get("--unity-project")!,packageRoots:{"com.unity.ugui":flags.get("--ugui-package-root")!},
  ...(zone02?{profile:"second-final-zone02-v1" as const,zone02SelectionPath:flags.get("--rail-zone02-manifest")!}:{})}):undefined;
const f = await ce03ProducerFixture(flags.has("--lobby-bootstrap"));
f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
const now = Date.now(), ttlMs = 8 * 60 * 60 * 1000, secret = randomBytes(32).toString("base64url");
f.env.AUTH_SIGNING_KEY_CURRENT = secret;
f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'", [now + ttlMs]);
const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs });
const headers = { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
const hostOptions = { disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
  presentation: presentation ? { mode: "APPROVED_VIEW_V1" as const, manifest: presentation.manifest } : { mode: "LEGACY_R4" as const },
  inputs: ce03Inputs(), supportCatalog: localSupportCatalog, leaderPolicy: localLeaderChargePolicy,...(railPresentation?{railPresentation:railPresentation.value}:{}),
  ...(rail ? { targetingPolicyId: "ce03.frontline-first-v1" as const, ultimatePolicyId: "ce03.per-operator-ultimate-v1" as const,
    activePolicyId: "ce03.general-active-auto-v1" as const,
    railJourney: { version: "rail-journey-v1" as const, baseSpeedMillimetersPerSecond: 10000 } } : {}) };
let host = createCe03LocalHost(hostOptions);
let origin = "";
const server = createServer(async (req, res) => {
  try {
    const chunks: Buffer[] = []; let size = 0;
    for await (const chunk of req) {
      const bytes = Buffer.from(chunk); size += bytes.length;
      if (size > 16384) { res.writeHead(413); res.end(); return; }
      chunks.push(bytes);
    }
    const requestHeaders = new Headers();
    for (const [name, value] of Object.entries(req.headers)) if (typeof value === "string") requestHeaders.set(name, value);
    const method = req.method ?? "GET";
    const response = await host.request(new URL(req.url ?? "/", origin).href, { method, headers: requestHeaders,
      ...(method === "GET" || method === "HEAD" ? {} : { body: Buffer.concat(chunks) }) }, f.env);
    res.writeHead(response.status, Object.fromEntries(response.headers)); res.end(Buffer.from(await response.arrayBuffer()));
  } catch { res.writeHead(503, { "Content-Type": "application/json" }); res.end('{"errorCode":"ce03_local_transport_unavailable"}'); }
});
await new Promise<void>(resolveListen => server.listen(0, "127.0.0.1", resolveListen));
const address = server.address(); if (!address || typeof address === "string") throw new Error("No loopback listener");
origin = `http://127.0.0.1:${address.port}`;
const outputRoot = resolve("audit-results/ce03-local-host"); await mkdir(outputRoot, { recursive: true });
const output = await mkdtemp(resolve(outputRoot, selfCheck ? "self-check-" : "session-"));
if (rail) {
  // Bootstrap disposable support through the real authenticated HTTP route.
  // No START/preview/finish calls, accepting provider or persistent account edits.
  try {
    const post = (path: string, value: unknown) => fetch(origin + path, { method: "POST", headers: { ...headers, "Idempotency-Key": "rail-local-bootstrap" }, body: JSON.stringify(value) });
    const saved = await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
      expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) });
    const selection = { ...body }; Reflect.deleteProperty(selection, "expectedUltimateRevision");
    const request = { selection, expectedSupportRevision: 1 };
    const before = Buffer.from(f.sql.export());
    const response = await post("/__local/ce03/rail-prepare", request);
    const prepared = await response.json() as { version: string; railJourney: Parameters<typeof canonicalJson>[0]; railJourneySha256: string; preparationSha256: string };
    const { preparationSha256, ...payload } = prepared;
    const denied = await fetch(origin + "/__local/ce03/rail-prepare", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) });
    if (saved.status !== 200 || response.status !== 200 || denied.status !== 401 || prepared.version !== "ce03-rail-preparation-v1" ||
      await sha256Hex(canonicalJson(prepared.railJourney)) !== prepared.railJourneySha256 || await sha256Hex(canonicalJson(payload)) !== preparationSha256 ||
      !Buffer.from(f.sql.export()).equals(before) || f.scalar("SELECT COUNT(*) FROM runs") !== 0 || f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'") !== 30)
      throw new Error(`Rail HTTP bootstrap verification failed: support=${saved.status}, prepare=${response.status}, unauthorized=${denied.status}`);
    await writeFile(resolve(output, "rail-prepare-request.json"), JSON.stringify(request, null, 2) + "\n");
    await writeFile(resolve(output, "rail-preparation.json"), JSON.stringify(prepared, null, 2) + "\n");
    const verification = { profile: "RAIL_JOURNEY_LOCAL", scope: "DISPOSABLE_LOOPBACK_HTTP", origin, prepareStatus: response.status,
      unauthorizedStatus: denied.status, startCalled: false, runCreated: false, fuel: 30, prepareDatabaseUnchanged: true,
      railJourneySha256: prepared.railJourneySha256, preparationSha256, consumerVerifierInstalled: false,
      ...(railPresentation?{railPresentationManifestSha256:railPresentation.value.manifestSha256,railPresentationVerification:railPresentation.verification}:{}) };
    await writeFile(resolve(output, "rail-verification.json"), JSON.stringify(verification, null, 2) + "\n");
    if (selfCheck) console.log(JSON.stringify({ output, ...verification }));
  } catch (error) { await new Promise<void>(done => server.close(() => done())); f.sql.close(); throw error; }
}
if (selfCheck && rail) {
  await new Promise<void>(done => server.close(() => done())); f.sql.close();
} else if (selfCheck) {
  try {
    const post = (path: string, data: unknown, key: string) => fetch(origin + path, { method: "POST", headers: { ...headers, "Idempotency-Key": key }, body: JSON.stringify(data) });
    const denied = await fetch(origin + "/__local/ce03/support-loadout");
    const saved = await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
      expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) }, "self-check-support");
    const prepared = z.object({ prepareSha256: sha, commandContract: commandContractSchema }).passthrough().parse(
      await (await post("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1 }, "self-check-prepare")).json());
    const quote = z.object({ operationPreviewId: id }).parse(await (await post("/v1/operations/preview", body, "self-check-preview")).json());
    const response = await post("/__local/ce03/start", { operationPreviewId: quote.operationPreviewId, expectedSupportRevision: 1, prepareSha256: prepared.prepareSha256 }, "self-check-start");
    const refused = z.object({ errorCode: z.string() }).parse(await response.json());
    if (denied.status !== 401 || saved.status !== 200 || response.status !== 409 || refused.errorCode !== "ce03_consumer_artifacts_missing" || f.scalar("SELECT COUNT(*) FROM runs") !== 0 || f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'") !== 30) throw new Error("Self-check failed");
    const lobbyReads: Record<string, number> = {};
    if (flags.has("--lobby-bootstrap")) for (const path of ["/v1/account/state", "/v1/wallet/crystals", "/v1/growth/config", "/v1/trains/loadout", "/v1/formation/ultimate"]) {
      const read = await fetch(origin + path, { headers }); lobbyReads[path] = read.status;
      if (read.status !== 200 || read.headers.get("Cache-Control") !== "no-store") throw new Error("Lobby bootstrap read failed");
    }
    await writeFile(resolve(output, presentation ? "prepare-r6.json" : "prepare-v5.json"), JSON.stringify(prepared, null, 2) + "\n");
    if (presentation) await writeFile(resolve(output, "presentation-manifest.json"), JSON.stringify(presentation.manifest, null, 2) + "\n");
    const verification = { scope: "DISPOSABLE_LOOPBACK_HTTP", authenticated: true, unauthorizedStatus: denied.status,
      startStatus: response.status, errorCode: refused.errorCode, consumerVerifierInstalled: false,
      presentationVerification: presentation?.verification ?? null,
      lobbyReads,
      runCreated: false, combatSeedIssued: false, fuel: 30, selectedSupport: prepared.commandContract.support,
      ...(prepared.commandContract.version === "ce03-command-contract-v1" ? { leaderPolicy: prepared.commandContract.leader } : { ultimatePolicy: prepared.commandContract.ultimates }) };
    await writeFile(resolve(output, "verification.json"), JSON.stringify(verification, null, 2) + "\n");
    console.log(JSON.stringify({ output, ...verification }));
  } finally {
    await new Promise<void>((resolveClose, reject) => server.close(error => error ? reject(error) : resolveClose())); f.sql.close();
  }
} else {
  // Credentials stay in a local private bootstrap file, never console logs or reports.
  const connection = resolve(output, "connection.private.json");
  await writeFile(connection, JSON.stringify({ origin, accountId: "a", authorization: headers.Authorization, expiresAt: now + ttlMs,
    disposable: true, dataLifetime: "PROCESS", sessionDirectory: output, consumerVerifierInstalled: false,
    providerStatusPath: resolve(output, "provider-status.json"), uiPreparePath: resolve(output, "ui-prepare.json"),
    ...(rail ? { profile: "RAIL_JOURNEY_LOCAL", railPreparePath: "/__local/ce03/rail-prepare", railPrepareRequestPath: resolve(output, "rail-prepare-request.json"),
      railPreparationPath: resolve(output, "rail-preparation.json") } : {}) }, null, 2) + "\n", { mode: 0o600 });
  const statusPath = resolve(output, "provider-status.json");
  const publish = async (value: object) => {
    await writeFile(statusPath + ".pending", JSON.stringify({ origin, accountId: "a", ...value }, null, 2) + "\n");
    await rename(statusPath + ".pending", statusPath);
  };
  await publish({ status: "WAITING_FOR_PREPARE", reviewedSourceBundleSha256: flags.get("--reviewed-source-sha256") ?? null });
  console.log(JSON.stringify({ origin, connection, sessionDirectory: output, disposable: true, consumerVerifierInstalled: false }));
  // Only the trusted local terminal can request execution. There is no HTTP
  // accepting route, editable status-file bypass, or output-only provider load.
  let busy = false;
  let finishContext: { prepared: Parameters<typeof executeCe03ConsumerRunner>[0]["prepare"]; executed: Awaited<ReturnType<typeof executeCe03ConsumerRunner>> } | undefined;
  const terminal = createInterface({ input: process.stdin });
  terminal.on("line", line => {
    if (rail) { console.log("Rail local profile has no attest or finish command."); return; }
    if (line.trim().startsWith("finish ")) {
      if (busy || !reviewed || !finishContext || finishContext.executed.execution.dynamicResourceFileCount !== 66) { console.log("Trusted finish requires an idle verified session with dynamic resource pins."); return; }
      const context = finishContext; busy = true;
      void (async () => {
        const finishStatusPath = resolve(output, "finish-status.json");
        const finishStatus = async (value: object) => { await writeFile(finishStatusPath + ".pending", JSON.stringify(value, null, 2) + "\n"); await rename(finishStatusPath + ".pending", finishStatusPath); };
        try {
          const outputDirectory = z.string().min(1).parse(JSON.parse(line.trim().slice(7)));
          await finishStatus({ status: "VERIFYING" });
          const result = await trustedFinishCe03({ outputDirectory, owner: "a", origin, prepared: context.prepared,
            sourceBundleSha256: context.executed.execution.sourceBundleSha256,
            dynamicResourceFileCount: context.executed.execution.dynamicResourceFileCount,
            assertUnityExited, verifyExecutionIdentity: context.executed.verifyExecutionIdentity,
            lookupOriginalStart: async (key, request) => {
              const receipt = await f.env.DB.prepare("SELECT request_hash,response_json,response_sha256 FROM ce03_start_receipts WHERE account_id=? AND idempotency_key=?")
                .bind("a", key).first<{ request_hash: string; response_json: string; response_sha256: string }>();
              if (!receipt || receipt.request_hash !== await sha256Hex(canonicalJson(request)) || receipt.response_sha256 !== await sha256Hex(receipt.response_json)) return null;
              return receipt.response_json;
            },
            sendFinish: async (path, request, key) => {
              const response = await fetch(origin + path, { method: "POST", headers: { ...headers, "Idempotency-Key": key }, body: JSON.stringify(request) });
              return { status: response.status, responseRaw: await response.text() };
            },
          });
          const ackPath = resolve(result.outputDirectory, "finish-ack.json"), temporary = ackPath + "." + randomBytes(8).toString("hex") + ".pending";
          await writeFile(temporary, JSON.stringify({ version: "ce03-trusted-finish-ack-v1", ...result }, null, 2) + "\n", { flag: "wx" });
          await rename(temporary, ackPath);
          await finishStatus({ status: "FINISHED", runId: result.runId, rewardPreviewId: result.rewardPreviewId, ackPath, processRestartVerified: false });
          console.log(JSON.stringify({ finishStatusPath, status: "FINISHED", runId: result.runId, ackPath }));
        } catch (error) {
          await finishStatus({ status: "FAILED", error: error instanceof Error ? error.message : "Trusted finish failed" });
          console.log(JSON.stringify({ finishStatusPath, status: "FAILED" }));
        } finally { busy = false; }
      })().catch(() => { busy = false; console.error("Trusted finish status persistence failed; retry only the original intent."); });
      return;
    }
    if (line.trim() !== "attest" || busy || !reviewed) { console.log("Command unavailable; reviewed idle session accepts attest only."); return; }
    busy = true;
    void (async () => {
      // Clear the previous provider before any new attempt; original receipt
      // recovery remains available through the unchanged database.
      host = createCe03LocalHost(hostOptions);
      finishContext = undefined;
      try {
        await publish({ status: "EXECUTING" });
        const uiPath = resolve(output, "ui-prepare.json");
        if ((await stat(uiPath)).size > 8_000_000) throw new Error("UI prepare too large");
        const ui = z.object({ prepareSha256: sha, commandContract: commandContractSchema }).passthrough().parse(JSON.parse(await readFile(uiPath, "utf8")));
        const response = await fetch(origin + "/__local/ce03/prepare", { method: "POST", headers,
          body: JSON.stringify({ selection: body, expectedSupportRevision: ui.commandContract.support.revision }) });
        if (response.status !== 200) throw new Error("Current prepare rejected");
        const current = await response.json() as Parameters<typeof executeCe03ConsumerRunner>[0]["prepare"];
        if (current.prepareSha256 !== ui.prepareSha256) throw new Error("UI prepare changed");
        for (const field of ["ownedSelection", "carriageOwnership", "draft", "commandContract", "presentationManifest"] as const)
          if (canonicalJson(current[field] ?? null) !== canonicalJson(ui[field] as Parameters<typeof canonicalJson>[0])) throw new Error("UI context differs");
        const sourceFiles = z.array(z.string()).min(1).max(1024).parse(JSON.parse(await readFile(flags.get("--reviewed-source-files")!, "utf8")));
        const executed = await executeCe03ConsumerRunner({ executable: "D:/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe",
          cwd: flags.get("--unity-project")!, sourceRoot: flags.get("--unity-project")!, sourceFiles, outputRoot: output,
          reviewedSourceBundleSha256: flags.get("--reviewed-source-sha256")!, prepare: current,
          ...(flags.has("--dynamic-resource-closure") ? { dynamicResourceClosurePath: "QA/Ce03CommandIntegration/dynamic-resource-closure.json" } : {}),
          buildArgs: paths => ["-batchmode", "-nographics", "-projectPath", flags.get("--unity-project")!, "-executeMethod", "Ce03ConsumerEvidenceRunner.Run",
            "-ce03Output", paths.outputDirectory, "-ce03SourceBundle", paths.sourceBundlePath, "-ce03Prepare", paths.preparePath,
            "-logFile", resolve(paths.outputDirectory, "..", "unity.log")] });
        await attestLocalConsumer(current.draft, executed.artifacts, executed.verifier, current.commandContract,
          { manifest: current.presentationManifest, prepareSha256: current.prepareSha256 });
        // START revalidates owner/revision/prepare and source on every new run;
        // a support edit during execution cannot turn this status into admission.
        host = createCe03LocalHost({ ...hostOptions, artifacts: async () => executed.artifacts, verifier: executed.verifier });
        finishContext = { prepared: current, executed };
        await publish({ status: "VERIFIED", prepareSha256: current.prepareSha256, sourceBundleSha256: executed.execution.sourceBundleSha256,
          execution: executed.execution, sourceBundlePath: resolve(executed.execution.outputDirectory, "..", "source-input.json") });
        console.log(JSON.stringify({ providerStatusPath: statusPath, status: "VERIFIED", prepareSha256: current.prepareSha256 }));
      } catch (error) {
        finishContext = undefined;
        host = createCe03LocalHost(hostOptions);
        await publish({ status: "FAILED", error: error instanceof Error ? error.message : "Local execution failed" });
        console.log(JSON.stringify({ providerStatusPath: statusPath, status: "FAILED" }));
      } finally { busy = false; }
    })().catch(() => { host = createCe03LocalHost(hostOptions); finishContext = undefined; busy = false; console.error("Local provider status persistence failed; provider removed."); });
  });
}
