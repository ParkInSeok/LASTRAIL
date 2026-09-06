import { type FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import "./versionManagement.css";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import {
  simpleVersionExportAction,
  simpleVersionInputFields,
  simpleVersionServerCards
} from "../shared/release/releaseManagementContract";

export const versionManagementServerCards = simpleVersionServerCards;
export const versionManagementInputFields = simpleVersionInputFields;
export const versionManagementExportAction = simpleVersionExportAction;

export function VersionManagementPage() {
  const current = useQuery({ queryKey: ["current-version"], queryFn: adminApi.currentVersion, retry: false });
  const readStatus = current.isLoading ? "조회 중" : current.isError
    ? adminRequestFailureDisplay(current.error, { resourceLabel: "현재 버전", resourceId: "/api/versions" }).reason
    : "없음";
  const [targetVersion, setTargetVersion] = useState("");
  const [targetVersionCode, setTargetVersionCode] = useState("");
  const [minimumVersionCode, setMinimumVersionCode] = useState<string | null>(null);
  const effectiveMinimum = minimumVersionCode ?? current.data?.minimumVersionCode ?? "";
  const [submitStatus, setSubmitStatus] = useState<string>(versionManagementExportAction.status);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const minimumValid = /^[1-9]\d{0,9}$/.test(effectiveMinimum) && Number(effectiveMinimum) <= Number(targetVersionCode);
  const canSubmit = current.data?.canExport === true && targetVersion.trim().length > 0 && /^[1-9]\d{0,9}$/.test(targetVersionCode.trim()) && Number(targetVersionCode) <= 2147483647 && minimumValid && !isSubmitting;

  async function submitVersionExport(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canSubmit) return;

    setIsSubmitting(true);
    setSubmitStatus("내보내는 중");
    try {
      const result = await adminApi.exportVersionToR2({
        version: targetVersion.trim(),
        versionCode: targetVersionCode.trim(),
        minimumVersionCode: effectiveMinimum
      });
      setSubmitStatus(result.status);
      if (result.accepted) await current.refetch();
    } catch (error) {
      const failure = adminRequestFailureDisplay(error, {
        resourceLabel: "버전 내보내기",
        resourceId: "/api/versions/export"
      });
      setSubmitStatus(failure.reason);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="content">
      <header>
        <h1>버전 관리</h1>
      </header>
      <section aria-label="프론트 앱 버전 정책">
      <h2>프론트 버전</h2>
      <section className="summary-grid" aria-label="버전 관리 요약">
        {versionManagementServerCards.map((card) => (
          <div key={card.code}>
            <span>{card.code === "currentServer" ? "정책 저장 대상" : card.code === "currentVersion" ? "저장된 앱 버전" : "저장된 앱 버전코드"}</span>
            <strong>{current.data ? (card.code === "currentServer" ? current.data.server : card.code === "currentVersion" ? current.data.version ?? "없음" : current.data.versionCode ?? "없음") : readStatus}</strong>
          </div>
        ))}
        <div>
          <span>최소 지원 버전코드</span>
          <strong>{current.data ? current.data.minimumVersionCode ?? "미설정" : readStatus}</strong>
        </div>
      </section>
      <p className="version-policy-warning" role="status">{current.data?.admissionPolicy ? `접속 정책: ${current.data.admissionPolicy.status ?? "미기록"} · 실제 Game API 적용 검증은 별도입니다.` : "접속 정책 조회 미연동 · 현재 저장값만으로 구버전 앱 차단 여부를 확인할 수 없습니다."}</p>
      <section className="admin-panel">
        <header>
          <h2>앱 버전 정책 저장</h2>
        </header>
        <form className="metric-grid" onSubmit={submitVersionExport}>
          {versionManagementInputFields.map((field) => (
            <label key={field.key} className="metric-card" htmlFor={field.key}>
              <span>{field.label}</span>
              <input
                id={field.key}
                value={field.key === "targetVersion" ? targetVersion : targetVersionCode}
                placeholder={field.placeholder}
                aria-label={field.label}
                inputMode={field.key === "targetVersionCode" ? "numeric" : "text"}
                onChange={(event) =>
                  field.key === "targetVersion"
                    ? setTargetVersion(event.target.value)
                    : setTargetVersionCode(event.target.value)
                }
              />
            </label>
          ))}
          <label className="metric-card" htmlFor="minimumVersionCode">
            <span>최소 지원 버전코드</span>
            <input id="minimumVersionCode" aria-label="최소 지원 버전코드" inputMode="numeric" value={effectiveMinimum} onChange={(event) => setMinimumVersionCode(event.target.value)} placeholder="예: 1" />
            <small>접속 허용 하한의 정책값입니다. 리소스 폴더명·삭제 기준이 아닙니다.</small>
            {effectiveMinimum && targetVersionCode && !minimumValid && <small role="alert">최소 버전코드는 1 이상이며 배포할 버전코드를 초과할 수 없습니다.</small>}
          </label>
          <div className="metric-card">
            <span>R2 갱신</span>
            <strong>{submitStatus}</strong>
            <button type="submit" disabled={!canSubmit}>
              {isSubmitting ? "내보내는 중" : versionManagementExportAction.label}
            </button>
            <small>R2 정책 문서만 갱신합니다. 앱·서버 배포나 콘텐츠 활성화는 수행하지 않습니다.</small>
          </div>
        </form>
      </section>
      <section aria-label="콘텐츠 버전 정책">
        <h2>콘텐츠</h2>
        <dl className="version-policy">
          <div><dt>저장된 콘텐츠 릴리스</dt><dd>{current.data?.contentRelease === undefined ? "조회 API 미연동" : current.data.contentRelease?.releaseId ?? "미설정"}</dd></div>
          <div><dt>콘텐츠 버전</dt><dd>{current.data?.contentRelease?.contentVersion ?? "미기록"}</dd></div>
          <div><dt>콘텐츠 승인 상태</dt><dd>{current.data?.contentRelease?.status ?? "미기록"}</dd></div>
          <div><dt>콘텐츠 롤백</dt><dd>미지원 · 지원 앱과 호환되는 승인 manifest 포인터 전환으로 별도 관리</dd></div>
          <div><dt>보존 기준</dt><dd>지원 앱·롤백 대상이 참조하는 리소스 보존. 최소 버전 자동 하향·이전 파일 자동 삭제 없음</dd></div>
        </dl>
      </section>
      </section>
      <section className="server-version-section" aria-label="서버 버전 정책">
        <h2>서버 버전</h2>
        <dl className="version-policy">
          <div><dt>조회 대상 Admin API</dt><dd>{current.data?.server ?? readStatus}</dd></div>
          <div><dt>Worker 배포 버전</dt><dd>서버 버전 API 미연동</dd></div>
          <div><dt>버전 기준</dt><dd>Unity 앱 버전과 독립된 배포·진단 버전</dd></div>
          <div><dt>서버 배포·롤백</dt><dd>이 화면에서 미지원</dd></div>
          <div><dt>API 호환 정책</dt><dd>API-only 변경은 롤백 가능한 설계 우선. 호환되는 추가 변경만으로 최소 버전 상향 불필요</dd></div>
          <div><dt>호환성을 깨는 DB/API 변경</dt><dd>새 앱 이용 가능 여부 확인, minimumVersionCode 상향과 서버 접속 제한 검증 후 적용</dd></div>
          <div><dt>Worker 롤백 조건</dt><dd>현재 DB 구조·데이터와 대상 코드의 호환성 확인 필요</dd></div>
          <div><dt>DB 복구</dt><dd>하위 호환 후속 마이그레이션 또는 별도 승인된 감사 기록 기반 복구. DB 롤백 미지원</dd></div>
          <div><dt>버전 이력·로그</dt><dd>배포 증거이며 DB 백업 지점이 아님</dd></div>
        </dl>
      </section>
    </main>
  );
}
