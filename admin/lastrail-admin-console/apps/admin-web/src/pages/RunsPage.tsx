import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { runResultLabel } from "../shared/labels/adminLabels";
import { versionSummary } from "../shared/runs/runVersions";
import "../shared/runs/runVersions.css";

export function runTimeLabel(value?: string, result?: string): string {
  if (!value) return result === "IN_PROGRESS" ? "진행 중" : "미기록";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "미기록";
  const parts = new Intl.DateTimeFormat("en-GB", {
    timeZone: "Asia/Seoul", year: "numeric", month: "2-digit", day: "2-digit",
    hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23"
  }).formatToParts(date);
  const part = (type: string) => parts.find(p => p.type === type)?.value;
  return `${part("year")}-${part("month")}-${part("day")} ${part("hour")}:${part("minute")}:${part("second")}`;
}

export function RunsPage() {
  const [searchParams] = useSearchParams();
  const accountId = searchParams.get("accountId") ?? undefined;
  const query = useQuery({
    queryKey: ["runs", accountId],
    queryFn: () => adminApi.searchRuns(accountId)
  });

  return (
    <main className="content">
      <header>
        <h1>출격</h1>
        <p>출격 검색과 이력 맥락</p>
      </header>
      {query.isLoading && <p>출격을 불러오는 중</p>}
      {query.error && <p className="error">출격 요청 실패</p>}
      {query.data && (
        <div className="runs-table-scroll"><table className="runs-table">
          <thead>
            <tr>
              <th>출격</th>
              <th>계정</th>
              <th>작전 시작 시각 (KST)</th>
              <th>작전 종료 시각 (KST)</th>
              <th>결과</th>
              <th>위험도</th>
              <th>밸런스</th>
              <th>시작 앱 / 서버 버전</th>
              <th>완료 앱 / 서버 버전</th>
            </tr>
          </thead>
          <tbody>
            {query.data.items.map((run) => (
              <tr key={run.runId}>
                <td>
                  <Link to={`/runs/${run.runId}`}>{run.runId}</Link>
                </td>
                <td>{run.accountId}</td>
                <td>{runTimeLabel(run.startedAt)}</td>
                <td>{runTimeLabel(run.completedAt, run.result)}</td>
                <td>{runResultLabel(run.result)}</td>
                <td>{run.riskTier}</td>
                <td>{run.balanceVersion}</td>
                <td className="run-version-value">{versionSummary(run.versionEvidence.start)}</td>
                <td className="run-version-value">{versionSummary(run.versionEvidence.finish)}</td>
              </tr>
            ))}
          </tbody>
        </table></div>
      )}
    </main>
  );
}
