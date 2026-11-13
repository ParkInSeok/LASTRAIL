// No CLI, remote client, deploy callback, or smoke execution is provided here.
// This adapter is exercised only with local fake storage until separately approved.
import { parseServerDeploymentEvidence, saveServerDeploymentEvidence, type ServerDeploymentHistoryBucket } from "@lastrail/common";

export async function backfillServerDeploymentEvidence(
  bucket: ServerDeploymentHistoryBucket,
  value: unknown,
  expected: { environment: string; service: string; serverVersion: string; commitSha: string; cloudflareDeploymentId: string },
  expectedCurrentEtag: string | null,
) {
  const evidence = parseServerDeploymentEvidence(value);
  if (evidence === null) throw new Error("deployment_history_invalid");
  for (const key of ["environment", "service", "serverVersion", "commitSha", "cloudflareDeploymentId"] as const) {
    if (evidence[key] !== expected[key]) throw new Error("deployment_target_mismatch");
  }
  if (evidence.verification.localRegression !== "PASSED" || evidence.verification.dryRun !== "PASSED" || evidence.verification.remoteSmoke !== "PASSED") {
    throw new Error("deployment_backfill_verification_incomplete");
  }
  return saveServerDeploymentEvidence(bucket, evidence, { expectedCurrentEtag });
}
