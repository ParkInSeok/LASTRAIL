import { mkdir, mkdtemp, readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { Buffer } from "node:buffer";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ce03ProducerFixture, ce03Inputs } from "../../tests/helpers/ce03-producer-fixture";
import { body } from "../../tests/helpers/departure-fixture";
import { resolveAuthContext } from "../../apps/game-api/src/index";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog } from "../../apps/game-api/src/ce03-command-contract";
import { frontlinePolicyId } from "../../apps/game-api/src/ce03-targeting-policy";
import { individualUltimatePolicyId, generalActivePolicyId, generalActiveAutoSource, generalAutoExecutionPolicySha256 } from "../../apps/game-api/src/ce03-ultimate-policy";

// New in-memory account fixture and exclusive output directory, never the settled host.
const f = await ce03ProducerFixture();
try {
  const legacy = await (await f.request()).json();
  const legacyHash = await sha256Hex(canonicalJson(legacy.draft));
  if (legacyHash !== "c5cbc882a853a1a1e157d52e4497204fb8ce142a5f057f81f68b5900ee71a544") throw new Error("Legacy draft changed");
  f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
  f.sql.run("UPDATE account_formation SET ultimate_operator_id=NULL WHERE account_id='a'");
  const host = createCe03LocalHost({ disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
    inputs: ce03Inputs(), supportCatalog: localSupportCatalog, targetingPolicyId: frontlinePolicyId, ultimatePolicyId: individualUltimatePolicyId, activePolicyId: generalActivePolicyId });
  const post = (path: string, data: unknown) => host.request("http://127.0.0.1" + path,
    { method: "POST", headers: { ...f.headers, "Idempotency-Key": "frontline-fixture-support" }, body: JSON.stringify(data) }, f.env);
  const support = await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
    expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) });
  if (support.status !== 200) throw new Error("Fixture support setup failed");
  const selection = { ...body }; Reflect.deleteProperty(selection, "expectedUltimateRevision");
  const before = f.sql.export(), response = await post("/__local/ce03/prepare", { selection, expectedSupportRevision: 1 });
  if (response.status !== 200) throw new Error("Prepare failed");
  const result = await response.json(), { ready, status, prepareSha256, ...prepared } = result;
  if (ready !== false || status !== "MISSING_EXECUTION_EVIDENCE" || prepared.draft.template.execution.policySha256 !== generalAutoExecutionPolicySha256 ||
      prepareSha256 !== await sha256Hex(canonicalJson(prepared)) || !Buffer.from(before).equals(Buffer.from(f.sql.export())) ||
      f.scalar("SELECT COUNT(*) FROM runs") !== 0) throw new Error("Prepared identity or read-only boundary failed");
  const sourceHash = await sha256Hex(generalActiveAutoSource);
  if (prepared.draft.sourceFiles.find((r: { logicalName: string }) => r.logicalName === "generalActiveAutoPolicyV1")?.rawSha256 !== sourceHash)
    throw new Error("Policy source pin mismatch");
  await mkdir(resolve("audit-results"), { recursive: true });
  const output = await mkdtemp(resolve("audit-results/ce03-general-active-auto-"));
  const files: Record<string, string> = { "prepared.json": JSON.stringify(result, null, 2) + "\n",
    "draft.json": JSON.stringify(prepared.draft, null, 2) + "\n", "active-auto-policy.txt": generalActiveAutoSource,
    "verification.json": JSON.stringify({ scope: "NEW_DISPOSABLE_IN_MEMORY_DB", ready: false, runCreated: false, dbUnchangedDuringPrepare: true,
      legacyDraftCanonicalSha256: legacyHash, targetingPolicyId: frontlinePolicyId, ultimatePolicyId: individualUltimatePolicyId, activePolicyId: generalActivePolicyId, policySha256: generalAutoExecutionPolicySha256,
      policySourceSha256: sourceHash, prepareSha256, newDraftCanonicalSha256: await sha256Hex(canonicalJson(prepared.draft)),
      commandContractVersion: prepared.commandContract.version, presentationIncluded: false, unityExecutionVerified: false }, null, 2) + "\n" };
  for (const [name, bytes] of Object.entries(files)) {
    const path = resolve(output, name); await writeFile(path, bytes, { flag: "wx" });
    if (await readFile(path, "utf8") !== bytes) throw new Error("Fixture readback failed");
  }
  console.log(JSON.stringify({ output, prepareSha256, policySourceSha256: sourceHash, ready: false, runCreated: false }));
} finally { f.sql.close(); }
