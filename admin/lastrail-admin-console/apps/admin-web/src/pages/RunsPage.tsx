import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";

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
        <h1>Runs</h1>
        <p>Run search and historical context</p>
      </header>
      {query.isLoading && <p>Loading runs</p>}
      {query.error && <p className="error">Runs request failed</p>}
      {query.data && (
        <table>
          <thead>
            <tr>
              <th>Run</th>
              <th>Account</th>
              <th>Result</th>
              <th>Risk</th>
              <th>Balance</th>
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
                <td>{run.result}</td>
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

