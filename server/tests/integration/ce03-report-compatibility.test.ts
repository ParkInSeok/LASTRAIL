import { describe, expect, it } from "vitest";
import { mkdir, mkdtemp, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { executeCe03ConsumerRunner } from "../../scripts/local/ce03-executed-runner";
import { prepareLocalCombatDraft } from "../../apps/game-api/src/ce03-local-producer";
import { createCommandContract, createIndividualCommandContract, createAutomaticActiveCommandContract, localLeaderChargePolicy, localSupportCatalog } from "../../apps/game-api/src/ce03-command-contract";
import { attestLocalConsumer, requiredCommandConsumerCoverage, requiredIndividualConsumerCoverage, requiredGeneralAutoConsumerCoverage } from "../../apps/game-api/src/ce03-consumer-attestation";
import { presentationSourcePaths } from "../../apps/game-api/src/ce03-presentation-manifest";
import { ce03Inputs, ce03ProducerFixture } from "../helpers/ce03-producer-fixture";
import { presentationFixture } from "../helpers/ce03-presentation-fixture";

const matrix = [
  { generation: 1, presentation: false, report: 2 }, { generation: 1, presentation: true, report: 3 },
  { generation: 2, presentation: false, report: 4 }, { generation: 2, presentation: true, report: 5 },
  { generation: 3, presentation: false, report: 4 }, { generation: 3, presentation: true, report: 5 }
] as const;
type Row = typeof matrix[number];

// Synthetic process/report fixtures test compatibility and byte identity only.
// No Unity execution, actual combat coverage, verifier installation or START.
async function fixture(row: Row, fault?: "legacy-schema" | "missing-coverage" | "legacy-file") {
  const f = await ce03ProducerFixture();
  const { ownedSelection, draft: legacy } = await (await f.request()).json();
  f.sql.close();
  const draft = row.generation === 1 ? legacy : await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, {
    targetingPolicyId: "ce03.frontline-first-v1", ultimatePolicyId: "ce03.per-operator-ultimate-v1",
    ...(row.generation === 3 ? { activePolicyId: "ce03.general-active-auto-v1" as const } : {})
  });
  const support = { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion, revision: 1,
    slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) };
  const commandContract = row.generation === 1 ? createCommandContract(support, localLeaderChargePolicy) :
    row.generation === 2 ? createIndividualCommandContract(support) : createAutomaticActiveCommandContract(support);
  const root = await mkdtemp(join(tmpdir(), "ce03-report-compat-")), entry = join(root, "runner.mjs");
  const sourceFiles = ["runner.mjs"];
  const presentationManifest = row.presentation ? presentationFixture() : undefined;
  if (presentationManifest) for (const item of presentationManifest.sourceFiles) {
    const path = presentationSourcePaths[item.logicalName as keyof typeof presentationSourcePaths];
    const content = `// synthetic ${item.logicalName}`;
    await mkdir(dirname(join(root, path)), { recursive: true }); await writeFile(join(root, path), content);
    item.rawSha256 = await sha256Hex(content); sourceFiles.push(path);
  }
  const context = { ownedSelection, carriageOwnership: {}, draft, commandContract, ...(presentationManifest ? { presentationManifest } : {}) };
  const prepare = { ...context, prepareSha256: await sha256Hex(canonicalJson(context)) };
  const coverage: string[] = [...(row.generation === 1 ? requiredCommandConsumerCoverage : row.generation === 2 ?
    requiredIndividualConsumerCoverage : requiredGeneralAutoConsumerCoverage), ...(row.presentation ? ["PRESENTATION_BINDINGS"] : [])];
  if (fault === "missing-coverage") coverage.splice(coverage.indexOf(row.generation === 3 ? "GENERAL_ACTIVE_AUTO" : "ULTIMATE_AUTO"), 1);
  const cases = coverage.map((coverage, i) => ({ id: `synthetic_${i}`, coverage }));
  const vectorsJson = canonicalJson({ cases }), checkpointSchema = "synthetic checkpoint";
  const reportVersion = fault === "legacy-schema" ? (row.presentation ? 3 : 2) : row.report;
  const report = { schemaVersion: `ce03-consumer-runner-report-v${reportVersion}`, scope: "FULL_LOCAL_CONSUMER", runId: "synthetic.compatibility",
    simulationVersion: draft.simulationVersion, draftSha256: await sha256Hex(canonicalJson(draft)),
    algorithmSourceSha256: "filled by child", vectorsSha256: await sha256Hex(vectorsJson), checkpointSchemaSha256: await sha256Hex(checkpointSchema),
    commandContractSha256: await sha256Hex(canonicalJson(commandContract)),
    ...(presentationManifest ? { presentationManifestSha256: await sha256Hex(canonicalJson(presentationManifest)), prepareSha256: prepare.prepareSha256 } : {}),
    results: cases.map(c => ({ caseId: c.id, coverage: c.coverage, status: "PASS" })), passed: cases.length, failed: 0, skipped: 0 };
  const fileVersion = fault === "legacy-file" ? (row.presentation ? 3 : 2) : row.report;
  await writeFile(entry, `import {readFile,writeFile} from 'node:fs/promises';import {join} from 'node:path';import {createHash} from 'node:crypto';
const [out,bundle]=process.argv.slice(2), source=await readFile(bundle,'utf8'), report=${JSON.stringify(report)};
const canonical=x=>JSON.stringify(x===null||typeof x!=='object'?x:Array.isArray(x)?x.map(normalize):normalize(x));
function normalize(x){return x===null||typeof x!=='object'?x:Array.isArray(x)?x.map(normalize):Object.fromEntries(Object.keys(x).sort().map(k=>[k,normalize(x[k])]))}
report.algorithmSourceSha256=createHash('sha256').update(source).digest('hex');
for(const [name,bytes] of Object.entries({'algorithm-source.json':source,'checkpoint-schema.json':${JSON.stringify(checkpointSchema)},'vectors.json':${JSON.stringify(vectorsJson)},'runner-report-v${fileVersion}.json':canonical(report)}))await writeFile(join(out,name),bytes);
`);
  return { draft, commandContract, presentationManifest, prepare, options: { executable: process.execPath, cwd: root, sourceRoot: root,
    sourceFiles, outputRoot: root, prepare, buildArgs: (p: { outputDirectory: string; sourceBundlePath: string }) => [entry, p.outputDirectory, p.sourceBundlePath], timeoutMs: 5000 } };
}

describe("offline producer/adapter/attestation report compatibility", () => {
  it("rejects an invalid command contract before starting the runner", async () => {
    const f = await fixture(matrix[0]);
    const { prepareSha256: _old, ...context } = f.prepare;
    expect(_old).toHaveLength(64);
    const invalid = { ...context, commandContract: {} };
    let launched = false;
    await expect(executeCe03ConsumerRunner({ ...f.options,
      prepare: { ...invalid, prepareSha256: await sha256Hex(canonicalJson(invalid)) },
      buildArgs: p => { launched = true; return f.options.buildArgs(p); } }))
      .rejects.toThrow("ce03_runner_command_contract_invalid");
    expect(launched).toBe(false);
  });
  it.each(matrix)("command v$generation, presentation=$presentation reads report v$report and stays unready", async row => {
    const f = await fixture(row), executed = await executeCe03ConsumerRunner(f.options);
    expect(JSON.parse(executed.artifacts.reportJson).schemaVersion).toBe(`ce03-consumer-runner-report-v${row.report}`);
    const presentation = f.presentationManifest ? { manifest: f.presentationManifest, prepareSha256: f.prepare.prepareSha256 } : undefined;
    await expect(attestLocalConsumer(f.draft, executed.artifacts, executed.verifier, f.commandContract, presentation))
      .resolves.toMatchObject({ status: "ATTESTATION_VERIFIED", ready: false, frozen: false });
    await expect(attestLocalConsumer(f.draft, executed.artifacts, undefined, f.commandContract, presentation))
      .rejects.toMatchObject({ code: "ce03_consumer_verifier_missing" });
    await expect(attestLocalConsumer(f.draft, executed.artifacts, { verify: async () => false }, f.commandContract, presentation))
      .rejects.toMatchObject({ code: "ce03_consumer_attestation_rejected" });
  });
  it.each(matrix.filter(r => r.generation !== 1))("rejects legacy-only report file for command v$generation presentation=$presentation", async row => {
    const f = await fixture(row, "legacy-file");
    await expect(executeCe03ConsumerRunner(f.options)).rejects.toThrow();
  });
  for (const fault of ["legacy-schema", "missing-coverage"] as const) it.each(matrix.filter(r => r.generation !== 1))(
    `rejects ${fault} for command v$generation presentation=$presentation before verifying`, async row => {
      const f = await fixture(row, fault), executed = await executeCe03ConsumerRunner(f.options);
      let calls = 0;
      const verifier = { verify: async () => { calls++; return true; } };
      await expect(attestLocalConsumer(f.draft, executed.artifacts, verifier, f.commandContract,
        f.presentationManifest ? { manifest: f.presentationManifest, prepareSha256: f.prepare.prepareSha256 } : undefined))
        .rejects.toMatchObject({ code: fault === "legacy-schema" ? "ce03_consumer_report_invalid" : "ce03_consumer_coverage_incomplete" });
      expect(calls).toBe(0);
    });
});
