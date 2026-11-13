import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import type { SuspiciousPlayerDetail } from "@lastrail/admin-contracts";
import { adminApi } from "../shared/api/client";
import {
  classificationLabel,
  enforcementLabel,
  categoryLabel,
  evidenceCodeLabel,
  priorityLabel,
  reviewStatusLabel,
  runResultLabel
} from "../shared/labels/adminLabels";

type SuspiciousRun = SuspiciousPlayerDetail["suspiciousRuns"][number];

export function runTypeLabel(runType: SuspiciousRun["runType"]): string {
  return runType === "RAID_RUN" ? "레이드 출격" : "출격";
}

export function runHref(runId: string): string {
  return `/runs/${encodeURIComponent(runId)}`;
}

export function auditDecisionLabel(action: string): string {
  if (action === "INTEGRITY_REVIEW_MARK" || action === "INTEGRITY_REVIEW_STATUS_CHANGE") {
    return "판정 기록";
  }
  return action;
}

export function signalEvidenceSummary(evidence: unknown): string {
  return JSON.stringify(evidence);
}

function formatDate(value: string | null): string {
  return value ? new Date(value).toLocaleString() : "-";
}

export function SuspiciousPlayerDetailPage() {
  const { accountId } = useParams();
  const query = useQuery({
    enabled: Boolean(accountId),
    queryKey: ["suspicious-player-detail", accountId],
    queryFn: () => adminApi.suspiciousPlayerDetail(accountId ?? "")
  });

  if (!accountId) return <main className="content error">계정 ID가 없습니다</main>;

  return (
    <main className="content">
      <header>
        <h1>의심 플레이어 상세</h1>
        <p>계정 단위 리스크 신호, 의심 출격, 관련 기록을 읽기 전용으로 검토합니다</p>
      </header>
      {query.isLoading ? <p>의심 플레이어 상세를 불러오는 중</p> : null}
      {query.error ? <p className="error">의심 플레이어 상세 요청 실패</p> : null}
      {query.data ? <SuspiciousPlayerDetailContent detail={query.data} /> : null}
    </main>
  );
}

function SuspiciousPlayerDetailContent({ detail }: { detail: SuspiciousPlayerDetail }) {
  return (
    <>
      <section className="summary-grid" aria-label="계정 요약">
        <div>
          <span>계정</span>
          <strong>{detail.account.displayName}</strong>
          <small>{detail.account.accountId}</small>
        </div>
        <div>
          <span>위험도</span>
          <strong>{priorityLabel(detail.summary.priority)}</strong>
          <small>{evidenceCodeLabel(detail.summary.primaryReason)}</small>
        </div>
        <div>
          <span>리뷰 상태</span>
          <strong>{reviewStatusLabel(detail.summary.reviewStatus)}</strong>
          <small>{detail.summary.reviewOwner ?? "미배정"}</small>
          <small>{detail.summary.reviewReason ?? "판정 사유 없음"}</small>
        </div>
        <div>
          <span>운영 조치</span>
          <strong>{enforcementLabel(detail.summary.currentEnforcement)}</strong>
          <small>읽기 전용</small>
        </div>
      </section>

      <section className="admin-panel">
        <h2>리스크 신호</h2>
        <table>
          <thead>
            <tr>
              <th>탐지 시각</th>
              <th>출격</th>
              <th>분류</th>
              <th>사유</th>
              <th>리뷰</th>
              <th>증거</th>
            </tr>
          </thead>
          <tbody>
            {detail.riskSignals.map((signal) => (
              <tr key={signal.id}>
                <td>{formatDate(signal.createdAt)}</td>
                <td>{signal.runId ? <Link to={runHref(signal.runId)}>{signal.runId}</Link> : "-"}</td>
                <td>{classificationLabel(signal.classification)}</td>
                <td>
                  {evidenceCodeLabel(signal.evidenceCode)}
                  <small>{categoryLabel(signal.category)}</small>
                </td>
                <td>
                  {reviewStatusLabel(signal.reviewStatus)}
                  <small>{signal.reviewOwner ?? "미배정"}</small>
                </td>
                <td>
                  <small className="audit-summary">{signalEvidenceSummary(signal.evidence)}</small>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="admin-panel">
        <h2>의심 출격/레이드</h2>
        <table>
          <thead>
            <tr>
              <th>종류</th>
              <th>출격 ID</th>
              <th>결과</th>
              <th>위험도</th>
              <th>밸런스 버전</th>
              <th>시작/종료</th>
            </tr>
          </thead>
          <tbody>
            {detail.suspiciousRuns.map((run) => (
              <tr key={run.runId}>
                <td>{runTypeLabel(run.runType)}</td>
                <td>
                  <Link to={runHref(run.runId)}>{run.runId}</Link>
                </td>
                <td>{runResultLabel(run.result)}</td>
                <td>{run.riskTier}</td>
                <td>{run.balanceVersion}</td>
                <td>
                  {formatDate(run.startedAt)}
                  <small>{formatDate(run.completedAt)}</small>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="admin-panel">
        <h2>노트/판정 이력</h2>
        <div className="note-history">
          {detail.notes.map((note) => (
            <article key={note.id}>
              <header>
                <strong>{note.authorDisplayName}</strong>
                <small>{formatDate(note.createdAt)}</small>
              </header>
              <p>{note.note}</p>
            </article>
          ))}
          {detail.decisions.map((decision) => (
            <article key={decision.id}>
              <header>
                <strong>{auditDecisionLabel(decision.action)}</strong>
                <small>{formatDate(decision.createdAt)}</small>
              </header>
              <p>{decision.principalDisplayName}</p>
              <small className="audit-summary">{signalEvidenceSummary(decision.changeSummary)}</small>
            </article>
          ))}
          {detail.notes.length === 0 && detail.decisions.length === 0 ? <p>노트/판정 이력이 없습니다</p> : null}
        </div>
      </section>
    </>
  );
}
