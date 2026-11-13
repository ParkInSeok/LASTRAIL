import { useEffect, useRef, useState, type FormEvent } from "react";
import { adminRequestFailureDisplay } from "../api/errorDisplay";
import { buildRegistryCommand, registryPolicyReady, registryStatusLabel, type ClientRegistry, type RegistryEditContext, type RegistryInput, type RegistryTarget } from "./clientReleaseRegistry";

type Props = {
  target: RegistryTarget;
  data?: ClientRegistry;
  loading: boolean;
  fetching: boolean;
  saving: boolean;
  error: unknown;
  onTargetChange: (target: RegistryTarget) => void;
  onReload: () => Promise<ClientRegistry>;
  onSave: (context: RegistryEditContext, input: RegistryInput) => Promise<ClientRegistry>;
};
type Draft = {
  operation: RegistryInput["operation"];
  versionCode: string;
  version: string;
  releaseId: string;
  contentVersion: string;
  catalogUrl: string;
  catalogHash: string;
  compatibleMinimum: string;
  compatibleMaximum: string;
  minimumVersionCode: string;
  status: "INTERNAL" | "STABLE" | "DISABLED";
  reason: string;
  confirmed: boolean;
};
const emptyDraft = (): Draft => ({ operation: "register", versionCode: "", version: "", releaseId: "", contentVersion: "", catalogUrl: "", catalogHash: "", compatibleMinimum: "", compatibleMaximum: "", minimumVersionCode: "", status: "INTERNAL", reason: "", confirmed: false });
const operationLabels = { register: "내부 테스트 버전 등록", status: "등록 버전 상태 변경", promote: "동일 아티팩트 정식 승급", minimum: "최소 지원 버전 변경" };
function toInput(draft: Draft): RegistryInput {
  const common = { reason: draft.reason };
  switch (draft.operation) {
    case "register": return { ...common, operation: "register", version: draft.version, versionCode: draft.versionCode, confirmed: draft.confirmed, contentRelease: { status: "APPROVED", releaseId: draft.releaseId.trim(), contentVersion: draft.contentVersion.trim(), catalogUrl: draft.catalogUrl.trim(), catalogHash: draft.catalogHash.trim(), minimumVersionCode: draft.compatibleMinimum, ...(draft.compatibleMaximum ? { maximumVersionCode: draft.compatibleMaximum } : {}) } };
    case "minimum": return { ...common, operation: "minimum", minimumVersionCode: draft.minimumVersionCode, confirmed: draft.confirmed };
    case "status": return { ...common, operation: "status", versionCode: draft.versionCode, status: draft.status };
    case "promote": return { ...common, operation: "promote", versionCode: draft.versionCode, confirmed: draft.confirmed };
  }
}
export function ClientReleaseRegistryPanel(props: Props) {
  // Target changes remount the editor, so DEV drafts cannot become PRD commands.
  return <RegistryEditor key={props.target} {...props} />;
}
function RegistryEditor({ target, data, loading, fetching, saving, error, onTargetChange, onReload, onSave }: Props) {
  const freshDraft = (): Draft => ({ ...emptyDraft(), status: target === "DEV" ? "STABLE" : "INTERNAL" });
  const labels = { ...operationLabels, register: target === "DEV" ? "정식 버전 등록" : operationLabels.register };
  const [draft, setDraft] = useState(freshDraft);
  const [revision, setRevision] = useState<string | null | undefined>();
  const [needsReview, setNeedsReview] = useState(false);
  const [message, setMessage] = useState("");
  const [working, setWorking] = useState(false);
  const inFlight = useRef(false);
  const mounted = useRef(false);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  const mismatch = !!data && data.target !== target;
  const busy = working || saving;
  const unavailable = fetching || loading || !!error || !data || mismatch || saving;
  const context: RegistryEditContext = { target, editingRevision: revision, readUnavailable: unavailable, needsReview };
  const input = toInput(draft);
  let validation = "등록 정책 조회 필요";
  if (data) {
    try { buildRegistryCommand(data, context, input); validation = ""; }
    catch (error) { validation = error instanceof Error ? error.message : "입력값 확인 필요"; }
  }
  const canSave = !validation && !busy;
  const policyReady = !!data && registryPolicyReady(data);
  const editable = policyReady && !unavailable && !busy && !needsReview && data?.canMutate === true && !!data.revision;
  const selected = data?.rows.find(row => row.versionCode === draft.versionCode);
  function edit(patch: Partial<Draft>) {
    if (revision === undefined && editable) setRevision(data?.revision);
    setDraft(previous => ({ ...previous, ...patch }));
  }
  function reset() { setDraft(freshDraft()); setRevision(undefined); setNeedsReview(false); }
  async function reload() {
    if (!mounted.current || inFlight.current || busy) return;
    inFlight.current = true;
    setWorking(true); setNeedsReview(true);
    try { await onReload(); if (mounted.current) { reset(); setMessage("최신 정책 조회 완료 · 이전 입력 폐기"); } }
    catch (error) { if (mounted.current) setMessage(adminRequestFailureDisplay(error, { resourceLabel: "버전 등록", resourceId: "/api/client-releases" }).reason); }
    finally { inFlight.current = false; if (mounted.current) setWorking(false); }
  }
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!mounted.current || !canSave || inFlight.current) return;
    inFlight.current = true;
    setWorking(true); setNeedsReview(true);
    try {
      await onSave(context, input);
      if (!mounted.current) return;
      reset();
      setMessage("저장 처리 후 최신 정책 조회 완료 · 다음 변경은 다시 확인 필요");
    } catch (error) {
      if (mounted.current) setMessage(adminRequestFailureDisplay(error, { resourceLabel: "버전 등록 변경", resourceId: "/api/client-releases" }).reason);
    } finally { inFlight.current = false; if (mounted.current) setWorking(false); }
  }
  const field = (name: keyof Draft, label: string, numeric = false) => <label key={name}>{label}<input aria-label={label} inputMode={numeric ? "numeric" : "text"} value={String(draft[name])} onChange={e => edit({ [name]: e.target.value })} /></label>;
  return <section className="registry-section" aria-label="버전코드별 콘텐츠 등록">
    <h2>버전코드별 콘텐츠</h2>
    <div className="registry-toolbar">
      <label>대상 서버<select aria-label="등록 대상 서버" value={target} disabled={busy} onChange={e => onTargetChange(e.target.value as RegistryTarget)}><option value="DEV">DEV</option><option value="PRD">PRD</option></select></label>
      <button type="button" disabled={busy || fetching} onClick={() => void reload()}>등록 정책 다시 조회</button>
      <span>{data ? `${data.target} · ${registryStatusLabel(data.registryStatus)} · ${data.canMutate ? "변경 권한 있음" : "읽기 전용"}` : loading ? "조회 중" : "조회 데이터 없음"}</span>
    </div>
    {error ? <p role="alert">버전 등록 조회 실패 · {adminRequestFailureDisplay(error, { resourceLabel: "버전 등록", resourceId: "/api/client-releases" }).reason}</p> : null}
    {mismatch && <p role="alert">선택 대상과 응답 서버 불일치</p>}
    {loading && <p role="status">조회 중</p>}
    {data && <>
      <dl className="version-policy">
        <div><dt>문서 전체 revision</dt><dd>{data.revision ?? "미설정 · 저장 불가"}</dd></div>
        <div><dt>등록 정책 최소 버전코드</dt><dd>{data.minimumVersionCode ?? "미설정"}</dd></div>
        <div><dt>감사 이력 참조</dt><dd>{data.auditHead ?? "없음"}</dd></div>
      </dl>
      <div className="registry-table-wrap"><table className="registry-table"><thead><tr><th>앱 버전 / 코드</th><th>상태</th><th>콘텐츠 / 호환 범위</th><th>아티팩트</th></tr></thead><tbody>
        {data.rows.map(row => <tr key={row.versionCode}><td>{row.version}<br /><code>{row.versionCode}</code></td><td>{registryStatusLabel(row.status)}<br /><small>{row.status}</small></td><td>{row.contentRelease.releaseId}<br />{row.contentRelease.contentVersion}<br />{row.contentRelease.minimumVersionCode} ~ {row.contentRelease.maximumVersionCode ?? "상한 없음"}</td><td><details><summary>불변 식별자 · {registryStatusLabel(row.validation.status)}</summary><dl><dt>메타데이터 SHA256</dt><dd>{row.artifactIdentity}</dd><dt>카탈로그</dt><dd>{row.contentRelease.catalogUrl}</dd><dt>카탈로그 SHA256</dt><dd>{row.contentRelease.catalogHash}</dd></dl></details><span>{row.validation.artifactBytesVerified ? "서버 보고: 바이트 검증됨" : "바이트 미검증"}</span></td></tr>)}
        {!data.rows.length && <tr><td colSpan={4}>등록된 버전 없음</td></tr>}
      </tbody></table></div>
    </>}
    {target === "PRD" && <p>테스터 관리 API 미지원 · 별도 권한 준비 필요</p>}
    {data && !policyReady && <p role="alert">배포 정책 확인 필요 · 저장 잠금</p>}
    <form onSubmit={submit} aria-label="버전코드 등록 정책 변경">
      <fieldset disabled={!editable}>
        <legend>등록 정책 변경 · 입력 초안</legend>
        <label>변경 항목<select aria-label="등록 변경 항목" value={draft.operation} onChange={e => { setDraft({ ...freshDraft(), operation: e.target.value as Draft["operation"] }); setRevision(undefined); setMessage(""); }}>{Object.entries(labels).filter(([key]) => key !== "promote" || (target === "PRD" && data?.capabilities.promote)).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
        <div className="registry-fields">
          {draft.operation === "register" && <>{field("version", "등록 앱 버전")}{field("versionCode", "등록 버전코드", true)}{field("releaseId", "릴리스 ID")}{field("contentVersion", "콘텐츠 버전 ID")}{field("catalogUrl", "HTTPS 카탈로그 주소")}{field("catalogHash", "카탈로그 SHA256")}{field("compatibleMinimum", "콘텐츠 호환 최소 버전코드", true)}{field("compatibleMaximum", "콘텐츠 호환 최대 버전코드 (선택)", true)}</>}
          {(draft.operation === "status" || draft.operation === "promote") && <label>등록 버전코드<select aria-label="변경할 버전코드" value={draft.versionCode} onChange={e => edit({ versionCode: e.target.value, confirmed: false })}><option value="">선택</option>{data?.rows.map(row => <option key={row.versionCode} value={row.versionCode}>{row.versionCode} · {row.version} · {registryStatusLabel(row.status)}</option>)}</select></label>}
          {draft.operation === "status" && <label>변경 상태<select aria-label="변경 상태" value={draft.status} onChange={e => edit({ status: e.target.value as Draft["status"] })}>{target === "DEV" ? <option value="STABLE">정식</option> : <option value="INTERNAL">내부 테스트</option>}<option value="DISABLED">사용 중지</option></select></label>}
          {draft.operation === "minimum" && field("minimumVersionCode", "새 최소 지원 버전코드", true)}
        </div>
        {selected && <p>변경 불가 아티팩트: <code className="registry-hash">{selected.artifactIdentity}</code></p>}
        <label>변경 사유<textarea aria-label="등록 정책 변경 사유" maxLength={500} value={draft.reason} onChange={e => edit({ reason: e.target.value })} /></label>
        {draft.operation !== "status" && <label className="registry-confirm"><input type="checkbox" checked={draft.confirmed} onChange={e => edit({ confirmed: e.target.checked })} />{draft.operation === "register" ? "정확히 등록된 버전코드만 허용하며 미등록 기존 버전은 차단됨을 확인했습니다." : draft.operation === "promote" ? "동일 아티팩트만 정식 승급하며 최소 지원 버전은 변경하지 않음을 확인했습니다." : "최소 지원 버전 미만의 접속이 제한됨을 별도로 확인했습니다."}</label>}
      </fieldset>
      <p role="status">{message || (revision !== undefined ? validation : "변경 전 대상·권한·최신 revision 확인 필요")}</p>
      {(needsReview || (revision !== undefined && revision !== data?.revision)) && <p role="alert">최신 정책을 다시 조회하고 입력·확인을 새로 진행하세요. 자동 재저장하지 않습니다.</p>}
      <button type="submit" disabled={!canSave}>{busy ? "처리 중" : labels[draft.operation]}</button>
    </form>
  </section>;
}
