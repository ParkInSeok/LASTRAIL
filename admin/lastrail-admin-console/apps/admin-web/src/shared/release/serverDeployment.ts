export const deploymentServices = ["game-api", "admin-api", "async-worker"] as const;
export type DeploymentService = typeof deploymentServices[number];
function record(value: unknown): Record<string, unknown> { return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {}; }
function optional(value: unknown): string | null { return typeof value === "string" && value.length > 0 ? value : null; }
export function mapDeployment(raw: unknown, service: DeploymentService, kind: "current" | "history", expectedVersion?: string) {
  const root = record(raw), item = record(root[kind]);
  const environment = typeof root.environment === "string" ? root.environment.toLowerCase() : "";
  const required = ["serverVersion", "releaseId", "cloudflareDeploymentId", kind === "current" ? "updatedAt" : "createdAt"];
  if (root.accepted !== true || root.permission !== "SERVER_DEPLOYMENT_READ" || root.permissionMode !== "ROLE_PERMISSION" || !["dev", "prd"].includes(environment) || root.service !== service || item.service !== service || item.environment !== environment || required.some(key => !optional(item[key])) || (expectedVersion !== undefined && item.serverVersion !== expectedVersion)) throw new Error("server_deployment_evidence_mismatch");
  const historyKey = `server-deployments/${environment}/${service}/${item.serverVersion}.json`;
  if ((kind === "current" && item.historyKey !== historyKey) || (kind === "history" && root.historyKey !== historyKey)) throw new Error("server_deployment_evidence_mismatch");
  if (kind === "history" && item.pointerStatus !== "CURRENT" && item.pointerStatus !== "DEGRADED") throw new Error("server_deployment_evidence_mismatch");
  const database = record(item.database), content = record(item.contentRelease), verification = record(item.verification);
  return {
    environment, service, serverVersion: item.serverVersion as string, releaseId: item.releaseId as string, cloudflareDeploymentId: item.cloudflareDeploymentId as string, historyKey,
    timestamp: (kind === "current" ? item.updatedAt : item.createdAt) as string,
    commitSha: optional(item.commitSha), releaseBranch: optional(item.releaseBranch), deploymentTag: optional(item.deploymentTag), previousDeploymentId: optional(item.previousDeploymentId),
    compatibility: optional(database.compatibility), minimumVersionCode: optional(database.minimumVersionCode), migrationRange: optional(database.migrationRange),
    contentReleaseId: optional(content.releaseId), contentVersion: optional(content.contentVersion),
    localRegression: optional(verification.localRegression), dryRun: optional(verification.dryRun), remoteSmoke: optional(verification.remoteSmoke), pointerStatus: optional(item.pointerStatus)
  };
}
export function deploymentFailure(error: unknown) {
  const item = record(error), code = item.errorCode;
  if (item.status === 404 && (code === "server_deployment_current_not_found" || code === "server_deployment_history_not_found")) return "배포 기록 없음";
  if (item.status === 403) return "서버 배포 기록 조회 권한 확인 필요";
  if (item.status === 401 || item.status === 302 || code === "ADMIN_API_NON_JSON_RESPONSE") return "Access/admin 인증 확인 필요";
  if (code === "server_deployment_history_invalid" || code === "server_deployment_current_invalid") return "저장된 배포 기록 형식 확인 필요";
  if (code === "deployment_target_mismatch" || code === "server_deployment_current_target_mismatch" || code === "server_deployment_history_target_mismatch" || item.message === "server_deployment_evidence_mismatch") return "배포 기록 환경·서비스·버전 일치 여부 확인 필요";
  if (code === "version_storage_unavailable") return "배포 기록 저장소 미연결";
  if (code === "server_deployment_storage_read_failed") return "배포 기록 저장소 읽기 실패";
  if (item.status === 404) return "조회 API 경로·배포 상태 확인 필요";
  return "배포 기록 조회 실패 · 연결 또는 응답 확인 필요";
}
