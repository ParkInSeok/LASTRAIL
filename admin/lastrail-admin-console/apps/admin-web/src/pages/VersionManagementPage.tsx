import { type FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
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
  const [submitStatus, setSubmitStatus] = useState<string>(versionManagementExportAction.status);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const canSubmit = current.data?.canExport === true && targetVersion.trim().length > 0 && /^[1-9]\d{0,9}$/.test(targetVersionCode.trim()) && Number(targetVersionCode) <= 2147483647 && !isSubmitting;

  async function submitVersionExport(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canSubmit) return;

    setIsSubmitting(true);
    setSubmitStatus("내보내는 중");
    try {
      const result = await adminApi.exportVersionToR2({
        version: targetVersion.trim(),
        versionCode: targetVersionCode.trim()
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
      <section className="summary-grid" aria-label="버전 관리 요약">
        {versionManagementServerCards.map((card) => (
          <div key={card.code}>
            <span>{card.label}</span>
            <strong>{current.data ? (card.code === "currentServer" ? current.data.server : card.code === "currentVersion" ? current.data.version ?? "없음" : current.data.versionCode ?? "없음") : readStatus}</strong>
          </div>
        ))}
      </section>
      <section className="admin-panel">
        <header>
          <h2>버전 변경</h2>
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
              <small>현재 서버에 내보낼 목표 버전을 입력합니다</small>
            </label>
          ))}
          <div className="metric-card">
            <span>R2 갱신</span>
            <strong>{submitStatus}</strong>
            <button type="submit" disabled={!canSubmit}>
              {isSubmitting ? "내보내는 중" : versionManagementExportAction.label}
            </button>
            <small>{versionManagementExportAction.note}</small>
          </div>
        </form>
      </section>
    </main>
  );
}
