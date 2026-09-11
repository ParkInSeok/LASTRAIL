import { describe, expect, it, vi } from "vitest";
import { Buffer } from "node:buffer";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ce03ProducerFixture } from "../helpers/ce03-producer-fixture";
import { attestLocalConsumer, requiredConsumerCoverage } from "../../apps/game-api/src/ce03-consumer-attestation";

async function fixture() {
  const f = await ce03ProducerFixture(), { draft } = await (await f.request()).json();
  // Synthetic verifier fixtures ONLY: no claim these bytes implement Unity combat.
  const algorithmSource = "test-only algorithm bundle bytes", checkpointSchema = "test-only closed checkpoint bytes";
  const vectorsJson = JSON.stringify({ cases: requiredConsumerCoverage.map((coverage, i) => ({ id: `case.${i}`, coverage })) });
  const report = { schemaVersion: "ce03-consumer-runner-report-v1", scope: "FULL_LOCAL_CONSUMER", runId: "fixture.runner.1",
    simulationVersion: draft.simulationVersion, draftSha256: await sha256Hex(canonicalJson(draft)),
    algorithmSourceSha256: await sha256Hex(algorithmSource), vectorsSha256: await sha256Hex(vectorsJson),
    checkpointSchemaSha256: await sha256Hex(checkpointSchema),
    results: requiredConsumerCoverage.map((coverage, i) => ({ caseId: `case.${i}`, coverage, status: "PASS" })),
    passed: requiredConsumerCoverage.length, failed: 0, skipped: 0 };
  return { f, draft, report, artifacts: { algorithmSource, checkpointSchema, vectorsJson, reportJson: canonicalJson(report) } };
}
describe("full consumer attestation interface (no installed verifier)", () => {
  it("fails closed for absent artifacts or trusted verifier", async () => {
    const { draft, artifacts } = await fixture();
    await expect(attestLocalConsumer(draft, undefined, undefined)).rejects.toMatchObject({ code: "ce03_consumer_artifacts_missing" });
    await expect(attestLocalConsumer(draft, artifacts, undefined)).rejects.toMatchObject({ code: "ce03_consumer_verifier_missing" });
  });
  it("rejects partial basic/vitals reports even with many passed cases", async () => {
    const { draft, artifacts, report } = await fixture();
    const verify = vi.fn(async () => true);
    report.scope = "PARTIAL_BASIC_VITALS";
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: canonicalJson(report) }, { verify })).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    expect(verify).not.toHaveBeenCalled();
  });
  it("rejects missing coverage, skipped/failing vectors, mismatched bytes and different draft", async () => {
    const { draft, artifacts, report } = await fixture(), verifier = { verify: vi.fn(async () => true) };
    const missing = { ...report, results: report.results.slice(1), passed: report.passed - 1 };
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: canonicalJson(missing) }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_coverage_incomplete" });
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: canonicalJson({ ...report, skipped: 1 }) }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: canonicalJson({ ...report, failed: 1 }) }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    await expect(attestLocalConsumer(draft, { ...artifacts, algorithmSource: artifacts.algorithmSource + "changed" }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_evidence_mismatch" });
    const changed = structuredClone(draft); changed.template.selectedUltimateOperatorId = "op_kyle";
    await expect(attestLocalConsumer(changed, artifacts, verifier)).rejects.toMatchObject({ code: "ce03_consumer_evidence_mismatch" });
    expect(verifier.verify).not.toHaveBeenCalled();
  });
  it("requires provider acceptance and reports provider failure without producing evidence", async () => {
    const { draft, artifacts } = await fixture();
    await expect(attestLocalConsumer(draft, artifacts, { verify: async () => false })).rejects.toMatchObject({ code: "ce03_consumer_attestation_rejected" });
    await expect(attestLocalConsumer(draft, artifacts, { verify: async () => { throw Error("offline"); } })).rejects.toMatchObject({ code: "ce03_consumer_verifier_unavailable" });
  });
  it("binds exact artifacts and full vectors through injected test verifier, never enables admission", async () => {
    const { f, draft, artifacts, report } = await fixture(), before = f.sql.export();
    const verify = vi.fn(async () => true);
    const result = await attestLocalConsumer(draft, artifacts, { verify });
    expect(verify).toHaveBeenCalledTimes(1);
    expect(verify.mock.calls[0]?.[0]).toMatchObject({ reportJson: artifacts.reportJson, draftSha256: report.draftSha256 });
    expect(result).toMatchObject({ ready: false, frozen: false, status: "ATTESTATION_VERIFIED",
      executionEvidence: { algorithmSourceSha256: report.algorithmSourceSha256, conformanceVectorsSha256: report.vectorsSha256 } });
    expect(result).not.toHaveProperty("snapshotSha256"); expect(result).not.toHaveProperty("seed");
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("rejects duplicates, malformed JSON and unknown report fields", async () => {
    const { draft, artifacts, report } = await fixture(), verifier = { verify: vi.fn(async () => true) };
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: "{" }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    await expect(attestLocalConsumer(draft, { ...artifacts, algorithmSource: "\ud800" }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: artifacts.reportJson.replace('"failed":0', '"failed":0,"failed":0') }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: canonicalJson({ ...report, ready: true }) }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    const duplicate = { ...report, results: [...report.results, report.results[0]], passed: report.passed + 1 };
    await expect(attestLocalConsumer(draft, { ...artifacts, reportJson: canonicalJson(duplicate) }, verifier)).rejects.toMatchObject({ code: "ce03_consumer_coverage_incomplete" });
  });
});
