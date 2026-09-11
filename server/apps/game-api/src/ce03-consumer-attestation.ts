import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { bindingSchema, id, policySchema, pos, sha, sourceFileSchema, templateSchema } from "./ce03-local-schema";
import { validateLocalBindingClosure } from "./ce03-local-producer";
import { commandContractSchema } from "./ce03-command-contract";
import { validatePresentationManifest } from "./ce03-presentation-manifest";

export const requiredConsumerCoverage = ["STRICT_PROFILE", "FULL_EFFECTS", "MANUAL_COMMANDS", "FIFTEEN_WAVES",
  "CHECKPOINT_RESTORE", "DETERMINISM", "OWNER_ISOLATION", "JOURNAL_RECOVERY"] as const;
const coverage = z.enum(requiredConsumerCoverage), closed = z.strictObject;
const draftSchema = closed({ profileId: id, engineProfileVersion: id, effectiveStatsVersion: id, roundingVersion: id, simulationVersion: id,
  template: templateSchema, executionPolicy: policySchema, bindings: z.array(bindingSchema).min(1), sourceFiles: z.array(sourceFileSchema).min(1) });
const reportSchema = closed({ schemaVersion: z.literal("ce03-consumer-runner-report-v1"), scope: z.literal("FULL_LOCAL_CONSUMER"), runId: id,
  simulationVersion: id, draftSha256: sha, algorithmSourceSha256: sha, vectorsSha256: sha, checkpointSchemaSha256: sha,
  results: z.array(closed({ caseId: id, coverage, status: z.literal("PASS") })).min(1), passed: pos, failed: z.literal(0), skipped: z.literal(0) });
export const requiredCommandConsumerCoverage = [...requiredConsumerCoverage, "SUPPORT_SELECTION", "LEADER_CHARGE"] as const;
const commandCoverage = z.enum(requiredCommandConsumerCoverage);
const commandReportSchema = reportSchema.extend({ schemaVersion: z.literal("ce03-consumer-runner-report-v2"), commandContractSha256: sha,
  results: z.array(closed({ caseId: id, coverage: commandCoverage, status: z.literal("PASS") })).min(1) });
export const requiredPresentationConsumerCoverage = [...requiredCommandConsumerCoverage, "PRESENTATION_BINDINGS"] as const;
const presentationCoverage = z.enum(requiredPresentationConsumerCoverage);
const presentationReportSchema = commandReportSchema.extend({ schemaVersion: z.literal("ce03-consumer-runner-report-v3"),
  presentationManifestSha256: sha, prepareSha256: sha,
  results: z.array(closed({ caseId: id, coverage: presentationCoverage, status: z.literal("PASS") })).min(1) });
const artifactsSchema = closed({ algorithmSource: z.string().min(1).max(8_000_000), checkpointSchema: z.string().min(1).max(2_000_000),
  vectorsJson: z.string().min(1).max(2_000_000), reportJson: z.string().min(1).max(2_000_000) });

export type ConsumerAttestationRequest = Readonly<{
  reportJson: string; reportSha256: string; draftSha256: string; simulationVersion: string;
  algorithmSource: string; algorithmSourceSha256: string;
  vectorsJson: string; vectorsSha256: string; checkpointSchema: string; checkpointSchemaSha256: string;
  commandContractJson?: string; commandContractSha256?: string;
  presentationManifestJson?: string; presentationManifestSha256?: string; prepareSha256?: string;
}>;
export interface ConsumerAttestationVerifier {
  // Injected only by trusted local host composition, never by HTTP/body content.
  // Must authenticate the actual runner report against the exact code, vectors,
  // checkpoint implementation and draft and validate full executed coverage.
  // Shape/hash/count checks below are NOT proof that tests ran. No default verifier.
  verify(request: ConsumerAttestationRequest): Promise<boolean>;
}
const fail = (code: string): never => { throw new ActivityError(409, code); };
function validUtf16(s: string): boolean {
  for (let i = 0; i < s.length; i++) {
    const code = s.charCodeAt(i);
    if (code >= 0xd800 && code <= 0xdbff) {
      const low = s.charCodeAt(++i);
      if (!(low >= 0xdc00 && low <= 0xdfff)) return false;
    } else if (code >= 0xdc00 && code <= 0xdfff) return false;
  }
  return true;
}
function json(raw: string): unknown {
  try { return JSON.parse(raw); } catch { return fail("ce03_consumer_report_invalid"); }
}

/** Verification interface only: no registered route, no DB, no Profile/Content/S
 * freeze, no seed/start. A verified result is not an admission capability. */
export async function attestLocalConsumer(draftInput: unknown, artifactInput: unknown, verifier: ConsumerAttestationVerifier | undefined, commandInput?: unknown,
  presentationContext?: { manifest: unknown; prepareSha256: string }) {
  if (artifactInput === undefined || artifactInput === null) return fail("ce03_consumer_artifacts_missing");
  const parsed = artifactsSchema.safeParse(artifactInput);
  if (!parsed.success) return fail("ce03_consumer_report_invalid");
  if (Object.values(parsed.data).some(s => !validUtf16(s))) return fail("ce03_consumer_report_invalid");
  if (!verifier) return fail("ce03_consumer_verifier_missing");
  const draftResult = draftSchema.safeParse(draftInput);
  if (!draftResult.success) return fail("ce03_consumer_draft_invalid");
  const draft = draftResult.data, a = parsed.data;
  validateLocalBindingClosure(draft.bindings, draft.template);
  if (draft.template.execution.policyVersion !== draft.executionPolicy.version ||
      draft.simulationVersion !== draft.executionPolicy.version ||
      draft.template.execution.policySha256 !== await sha256Hex(canonicalJson(draft.executionPolicy))) return fail("ce03_consumer_draft_invalid");
  const commands = commandInput === undefined ? undefined : commandContractSchema.parse(commandInput);
  const commandContractJson = commands === undefined ? undefined : canonicalJson(commands);
  const commandContractSha256 = commandContractJson === undefined ? undefined : await sha256Hex(commandContractJson);
  const presentation = presentationContext === undefined ? undefined : validatePresentationManifest(presentationContext.manifest);
  if (presentation !== undefined && commands === undefined) return fail("ce03_consumer_draft_invalid");
  const presentationManifestJson = presentation === undefined ? undefined : canonicalJson(presentation);
  const presentationManifestSha256 = presentationManifestJson === undefined ? undefined : await sha256Hex(presentationManifestJson);
  const prepareSha256 = presentationContext === undefined ? undefined : sha.parse(presentationContext.prepareSha256);
  const checked = (presentation !== undefined ? presentationReportSchema : commands === undefined ? reportSchema : commandReportSchema).safeParse(json(a.reportJson));
  if (!checked.success) return fail("ce03_consumer_report_invalid");
  const report = checked.data;
  // New runner handoff reports use canonical JSON; this rejects duplicate keys,
  // parser-dependent spellings and implicit reserialization. Existing operation
  // journal/HTTP receipt bytes are unrelated and remain untouched.
  if (canonicalJson(report) !== a.reportJson) return fail("ce03_consumer_report_invalid");
  const vectors = z.object({ cases: z.array(z.object({ id, coverage: presentationCoverage })).min(1) }).safeParse(json(a.vectorsJson));
  if (!vectors.success) return fail("ce03_consumer_coverage_incomplete");
  const caseMap = new Map(vectors.data.cases.map(v => [v.id, v.coverage]));
  const results = new Map(report.results.map(r => [r.caseId, r.coverage]));
  if (caseMap.size !== vectors.data.cases.length || results.size !== report.results.length ||
      report.passed !== results.size || results.size !== caseMap.size ||
      report.results.some(r => caseMap.get(r.caseId) !== r.coverage) ||
      (presentation !== undefined ? requiredPresentationConsumerCoverage : commands === undefined ? requiredConsumerCoverage : requiredCommandConsumerCoverage).some(c => !report.results.some(r => r.coverage === c))) return fail("ce03_consumer_coverage_incomplete");
  const request: ConsumerAttestationRequest = Object.freeze({ ...a,
    reportSha256: await sha256Hex(a.reportJson), draftSha256: await sha256Hex(canonicalJson(draft)), simulationVersion: draft.simulationVersion,
    algorithmSourceSha256: await sha256Hex(a.algorithmSource), vectorsSha256: await sha256Hex(a.vectorsJson),
    checkpointSchemaSha256: await sha256Hex(a.checkpointSchema), ...(commands === undefined ? {} : { commandContractJson: commandContractJson!, commandContractSha256: commandContractSha256! }),
    ...(presentation === undefined ? {} : { presentationManifestJson: presentationManifestJson!, presentationManifestSha256: presentationManifestSha256!, prepareSha256: prepareSha256! }) });
  if (report.draftSha256 !== request.draftSha256 || report.simulationVersion !== request.simulationVersion ||
      report.algorithmSourceSha256 !== request.algorithmSourceSha256 || report.vectorsSha256 !== request.vectorsSha256 ||
      report.checkpointSchemaSha256 !== request.checkpointSchemaSha256 ||
      (commands !== undefined && (!("commandContractSha256" in report) || report.commandContractSha256 !== commandContractSha256)) ||
      (presentation !== undefined && (!("presentationManifestSha256" in report) || report.presentationManifestSha256 !== presentationManifestSha256 ||
        !("prepareSha256" in report) || report.prepareSha256 !== prepareSha256))) return fail("ce03_consumer_evidence_mismatch");
  let accepted: boolean;
  try { accepted = await verifier.verify(request); } catch { return fail("ce03_consumer_verifier_unavailable"); }
  if (accepted !== true) return fail("ce03_consumer_attestation_rejected");
  return { status: "ATTESTATION_VERIFIED" as const, ready: false as const, frozen: false as const,
    draftSha256: request.draftSha256, reportSha256: request.reportSha256,
    checkpointSchemaSha256: request.checkpointSchemaSha256, runnerRunId: report.runId,
    executionEvidence: { algorithmSourceSha256: request.algorithmSourceSha256, conformanceVectorsSha256: request.vectorsSha256 } };
}
