import { type FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
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
  const [params] = useSearchParams();
  const serverTab = params.get("tab") === "server";
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
      <nav className="version-tabs" aria-label="버전 관리 구분">
        <Link to="?tab=frontend" aria-current={!serverTab ? "page" : undefined}>프론트</Link>
        <Link to="?tab=server" aria-current={serverTab ? "page" : undefined}>서버</Link>
      </nav>
      {serverTab ? <section aria-label="서버 버전 정책">
        <dl className="version-policy">
          <div><dt>조회 대상 Admin API</dt><dd>{current.data?.server ?? readStatus}</dd></div>
          <div><dt>Worker 배포 버전</dt><dd>서버 버전 API 미연동</dd></div>
          <div><dt>서버 배포·롤백</dt><dd>이 화면에서 미지원</dd></div>
          <div><dt>API 호환 정책</dt><dd>기존 필드·타입·의미 유지, 추가 필드는 선택·nullable 우선</dd></div>
          <div><dt>호환성을 깨는 DB/API 변경</dt><dd>새 앱 배포와 실제 접속 제한 검증 후 적용</dd></div>
          <div><dt>Worker 롤백 조건</dt><dd>현재 DB 구조·데이터와 대상 코드의 호환성 확인 필요</dd></div>
          <div><dt>DB 복구</dt><dd>후속 마이그레이션·감사 기록 기반 복구. DB 롤백 미지원</dd></div>
        </dl>
      </section> : <section aria-label="프론트 앱 버전 정책">
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
      <p className="version-policy-warning" role="status">접속 제한 미연결 · 현재 저장값만으로 구버전 앱이 차단되지는 않습니다.</p>
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
          <div><dt>활성 리소스 릴리스</dt><dd>조회 API 미연동</dd></div>
          <div><dt>콘텐츠 롤백</dt><dd>미지원 · 지원 앱과 호환되는 승인 manifest 포인터 전환으로 별도 관리</dd></div>
          <div><dt>보존 기준</dt><dd>지원 앱·롤백 대상이 참조하는 리소스 보존. 최소 버전 자동 하향·이전 파일 자동 삭제 없음</dd></div>
        </dl>
      </section>
      </section>}
    </main>
  );
}
