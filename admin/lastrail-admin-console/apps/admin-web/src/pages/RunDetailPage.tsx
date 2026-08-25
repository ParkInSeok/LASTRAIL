import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";

export function RunDetailPage() {
  const { runId = "" } = useParams();
  const query = useQuery({
    queryKey: ["run", runId],
    queryFn: () => adminApi.runDetail(runId),
    enabled: runId.length > 0
  });

  if (query.isLoading) return <main className="content">Loading run</main>;
  if (query.error) return <main className="content error">Run request failed</main>;
  if (!query.data) return <main className="content">No run selected</main>;

  const run = query.data;

  return (
    <main className="content">
      <header>
        <h1>{run.runId}</h1>
        <p>{run.accountId}</p>
      </header>
      <section className="summary-grid">
        <div>
          <span>Run Balance Version</span>
          <strong>{run.balanceVersion}</strong>
        </div>
        <div>
          <span>Current Active Balance Version</span>
          <strong>{run.currentActiveBalanceVersion}</strong>
        </div>
        <div>
          <span>Client Build</span>
          <strong>{run.clientBuild}</strong>
        </div>
        <div>
          <span>Validator Runtime Revision</span>
          <strong>{run.validatorRuntimeRevision}</strong>
        </div>
      </section>
      <h2>Snapshot</h2>
      <table>
        <thead>
          <tr>
            <th>Operator</th>
            <th>Effective Level</th>
            <th>Level Source</th>
            <th>Sync At Start</th>
            <th>Trained At Start</th>
            <th>Skill</th>
            <th>Promotion</th>
          </tr>
        </thead>
        <tbody>
          {run.snapshot.operators.map((operator) => (
            <tr key={operator.operatorId}>
              <td>{operator.operatorId}</td>
              <td>{operator.effectiveLevel}</td>
              <td>{operator.levelSource}</td>
              <td>{operator.syncLevelAtStart}</td>
              <td>{operator.trainedLevelAtStart ?? "-"}</td>
              <td>{operator.skillTraining}</td>
              <td>{operator.promotion}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}

