function record(value: unknown): Record<string, unknown> { return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {}; }
function text(value: unknown) { return typeof value === "string" && value ? value : null; }
function snapshot(value: unknown, source: string) {
  const item = record(value);
  if (item.source !== source) return null;
  const client = record(item.clientInstalled), deployment = record(item.serverAcceptedDeployment), content = record(item.contentRelease), admission = record(item.clientAdmission);
  return { source, appVersion: text(client.appVersion), versionCode: text(client.versionCode), platform: text(client.platform), channel: text(client.channel), runtimeDeploymentId: text(deployment.runtimeDeploymentId), environment: text(deployment.environment), contentVersion: text(content.contentVersion), releaseId: text(content.releaseId), minimumVersionCode: text(admission.minimumVersionCode), capturedAt: text(item.capturedAt) };
}
export function mapRunVersions(value: unknown) {
  const item = record(value);
  return {
    start: { status: text(item.runStartVersionSnapshotStatus) ?? "UNRECORDED_LEGACY", snapshot: snapshot(item.runStartVersionSnapshot === undefined ? item.runVersionSnapshot : item.runStartVersionSnapshot, "server_accepted_run_start") },
    finish: { status: text(item.runFinishVersionSnapshotStatus) ?? "UNRECORDED_LEGACY", snapshot: snapshot(item.runFinishVersionSnapshot, "server_accepted_run_finish") }
  };
}
export type RunVersionEvidence = ReturnType<typeof mapRunVersions>;
export function versionSummary(evidence: RunVersionEvidence["start"]) {
  if (!evidence.snapshot) return evidence.status === "NOT_FINISHED" ? "미종료" : evidence.status === "AVAILABLE" ? "버전 기록 형식 확인 필요" : evidence.status === "UNRECORDED_LEGACY" ? "미기록 (이전 기록)" : evidence.status;
  const value = evidence.snapshot;
  return `앱 ${value.appVersion ?? "미기록"} / 코드 ${value.versionCode ?? "미기록"} · Worker 버전 참조 ${value.runtimeDeploymentId ?? "미기록"}`;
}
