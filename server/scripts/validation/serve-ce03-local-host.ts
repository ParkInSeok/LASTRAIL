import { createServer } from "node:http";
import { randomBytes } from "node:crypto";
import { Buffer } from "node:buffer";
import { mkdir, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { ce03ProducerFixture, ce03Inputs } from "../../tests/helpers/ce03-producer-fixture";
import { body } from "../../tests/helpers/departure-fixture";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog, localLeaderChargePolicy } from "../../apps/game-api/src/ce03-command-contract";

// Explicit disposable local account bootstrap. No remote DB argument or provider
// bypass. This executable NEVER installs a synthetic accepting verifier.
const args = process.argv.slice(2), selfCheck = args[0] === "--self-check";
if (args.length > 1 || (args.length === 1 && !selfCheck)) throw new Error("Only --self-check is supported");
const f = await ce03ProducerFixture();
f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
const now = Date.now(), ttlMs = 8 * 60 * 60 * 1000, secret = randomBytes(32).toString("base64url");
f.env.AUTH_SIGNING_KEY_CURRENT = secret;
f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'", [now + ttlMs]);
const token = await createAccessToken({ secret, accountId: "a", sessionId: "s", nowMs: now, ttlMs });
const headers = { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
const host = createCe03LocalHost({ disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
  inputs: ce03Inputs(), supportCatalog: localSupportCatalog, leaderPolicy: localLeaderChargePolicy });
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
const output = resolve("audit-results/ce03-local-host"); await mkdir(output, { recursive: true });
if (selfCheck) {
  try {
    const post = (path: string, data: unknown, key: string) => fetch(origin + path, { method: "POST", headers: { ...headers, "Idempotency-Key": key }, body: JSON.stringify(data) });
    const denied = await fetch(origin + "/__local/ce03/support-loadout");
    const saved = await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
      expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) }, "self-check-support");
    const prepared = await (await post("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1 }, "self-check-prepare")).json();
    const quote = await (await post("/v1/operations/preview", body, "self-check-preview")).json();
    const response = await post("/__local/ce03/start", { operationPreviewId: quote.operationPreviewId, expectedSupportRevision: 1, prepareSha256: prepared.prepareSha256 }, "self-check-start");
    const refused = await response.json();
    if (denied.status !== 401 || saved.status !== 200 || response.status !== 409 || refused.errorCode !== "ce03_consumer_artifacts_missing" || f.scalar("SELECT COUNT(*) FROM runs") !== 0 || f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'") !== 30) throw new Error("Self-check failed");
    await writeFile(resolve(output, "prepare-v5.json"), JSON.stringify(prepared, null, 2) + "\n");
    const verification = { scope: "DISPOSABLE_LOOPBACK_HTTP", authenticated: true, unauthorizedStatus: denied.status,
      startStatus: response.status, errorCode: refused.errorCode, consumerVerifierInstalled: false,
      runCreated: false, combatSeedIssued: false, fuel: 30, selectedSupport: prepared.commandContract.support, leaderPolicy: prepared.commandContract.leader };
    await writeFile(resolve(output, "verification.json"), JSON.stringify(verification, null, 2) + "\n");
    console.log(JSON.stringify({ output, ...verification }));
  } finally {
    await new Promise<void>((resolveClose, reject) => server.close(error => error ? reject(error) : resolveClose())); f.sql.close();
  }
} else {
  // Credentials stay in a local private bootstrap file, never console logs or reports.
  const connection = resolve(output, "connection.private.json");
  await writeFile(connection, JSON.stringify({ origin, accountId: "a", authorization: headers.Authorization, expiresAt: now + ttlMs,
    disposable: true, dataLifetime: "PROCESS", consumerVerifierInstalled: false }, null, 2) + "\n", { mode: 0o600 });
  console.log(JSON.stringify({ origin, connection, disposable: true, consumerVerifierInstalled: false }));
}
