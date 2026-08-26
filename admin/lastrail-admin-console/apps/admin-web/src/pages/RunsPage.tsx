import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { runResultLabel } from "../shared/labels/adminLabels";

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
        <table>
          <thead>
            <tr>
              <th>출격</th>
              <th>계정</th>
              <th>결과</th>
              <th>위험도</th>
              <th>밸런스</th>
            </tr>
          </thead>
          <tbody>
            {query.data.items.map((run) => (
              <tr key={run.runId}>
                <td>
                  <Link to={`/runs/${run.runId}`}>{run.runId}</Link>
                  <small>{run.startedAt}</small>
                </td>
                <td>{run.accountId}</td>
                <td>{runResultLabel(run.result)}</td>
                <td>{run.riskTier}</td>
                <td>{run.balanceVersion}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
