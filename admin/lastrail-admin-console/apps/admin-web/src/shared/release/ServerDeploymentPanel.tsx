import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../api/client";
import { deploymentServices, deploymentFailure, type DeploymentService } from "./serverDeployment";

const labels: Record<DeploymentService, string> = { "game-api": "Game API", "admin-api": "Admin API", "async-worker": "Async Worker" };
export function ServerDeploymentPanel() {
  const [service, setService] = useState<DeploymentService>("game-api");
  return <div aria-label="서버 배포 기록">
    <label>서비스 <select value={service} onChange={event => setService(event.target.value as DeploymentService)}>{deploymentServices.map(value => <option key={value} value={value}>{labels[value]}</option>)}</select></label>
    <ServiceEvidence key={service} service={service} />
  </div>;
}
function ServiceEvidence({ service }: { service: DeploymentService }) {
  const [input, setInput] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const current = useQuery({ queryKey: ["server-deployment-current", service], queryFn: () => adminApi.serverDeploymentCurrent(service), retry: false });
  const version = selected ?? current.data?.serverVersion;
  const history = useQuery({ queryKey: ["server-deployment-history", service, version], queryFn: () => adminApi.serverDeploymentHistory(service, version!), enabled: Boolean(version), retry: false });
  const value = history.data;
  const mismatch = selected === null && current.data && value && (current.data.cloudflareDeploymentId !== value.cloudflareDeploymentId || current.data.releaseId !== value.releaseId || current.data.environment !== value.environment);
  return <>
    <dl className="version-policy"><div><dt>현재 포인터 버전</dt><dd>{current.isLoading ? "조회 중" : current.error ? deploymentFailure(current.error) : current.data?.serverVersion ?? "없음"}</dd></div>
      {current.data && <><div><dt>조회 환경 / 서비스</dt><dd>{current.data.environment.toUpperCase()} / {service}</dd></div><div><dt>현재 Worker 배포 ID</dt><dd>{current.data.cloudflareDeploymentId}</dd></div></>}
    </dl>
    <form onSubmit={event => { event.preventDefault(); if (input.trim()) setSelected(input.trim()); }}>
      <label>조회할 서버 버전 <input aria-label="조회할 서버 버전" value={input} maxLength={100} pattern="[A-Za-z0-9._-]+" onChange={event => setInput(event.target.value)} /></label>
      <button type="submit" disabled={!input.trim()}>이력 조회</button>
      {selected !== null && <button type="button" onClick={() => setSelected(null)}>현재 포인터 조회</button>}
    </form>
    {version && <p>조회 버전: {version}</p>}
    {history.isLoading && <p role="status">조회 중</p>}
    {history.error && <p role="status">{deploymentFailure(history.error)}</p>}
    {value && <>
      <p role="status">{mismatch || value.pointerStatus === "DEGRADED" ? "대조 필요 (DEGRADED) · 배포 이력과 현재 포인터 확인 필요" : "이력에 기록된 포인터 상태: CURRENT"}</p>
      <dl className="version-policy">{[
        ["이력 환경 / 서비스", `${value.environment.toUpperCase()} / ${value.service}`], ["릴리스", value.releaseId], ["이력 Worker 배포 ID", value.cloudflareDeploymentId], ["이전 Worker 배포 ID", value.previousDeploymentId],
        ["커밋", value.commitSha], ["브랜치", value.releaseBranch], ["배포 태그", value.deploymentTag],
        ["DB 호환성", value.compatibility], ["DB 최소 앱 버전코드", value.minimumVersionCode], ["마이그레이션 범위", value.migrationRange],
        ["API 호환성", value.apiCompatibility], ["API 최소 앱 버전코드", value.apiMinimumVersionCode],
        ["API 롤백 검증", value.apiRollbackVerified == null ? "없음" : value.apiRollbackVerified ? "검증됨 (true)" : "미검증 (false)"],
        ["API 롤백 근거", value.apiCompatibility === "ROLLBACK_COMPATIBLE" && value.apiRollbackVerified === true ? "API 롤백 호환 검증 완료 · 실제 Worker 롤백은 현재 DB 호환성 별도 확인 필요" : "가능 근거 미확인"],
        ["콘텐츠 릴리스", value.contentReleaseId], ["콘텐츠 버전", value.contentVersion], ["로컬 회귀", value.localRegression], ["Dry-run", value.dryRun], ["원격 smoke", value.remoteSmoke], ["기록 시각", value.timestamp]
      ].map(([label, text]) => <div key={label}><dt>{label}</dt><dd>{text ?? "없음"}</dd></div>)}</dl>
    </>}
  </>;
}
