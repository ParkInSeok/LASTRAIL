import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";

export function PlayerDetailPage() {
  const { accountId = "" } = useParams();
  const query = useQuery({
    queryKey: ["player", accountId],
    queryFn: () => adminApi.playerDetail(accountId),
    enabled: accountId.length > 0
  });

  if (query.isLoading) return <main className="content">Loading player</main>;
  if (query.error) return <main className="content error">Player request failed</main>;
  if (!query.data) return <main className="content">No player selected</main>;

  const player = query.data;

  return (
    <main className="content">
      <header>
        <h1>{player.displayName}</h1>
        <p>{player.accountId}</p>
      </header>
      <section className="summary-grid">
        <div>
          <span>Status</span>
          <strong>{player.status}</strong>
        </div>
        <div>
          <span>Operator EXP</span>
          <strong>{player.progression.operatorExp}</strong>
        </div>
        <div>
          <span>Sync Level</span>
          <strong>{player.progression.syncLevel}</strong>
        </div>
        <div>
          <span>Core Level Spread</span>
          <strong>{player.progression.coreLevelSpread}</strong>
        </div>
      </section>
      <h2>Training Core</h2>
      <table>
        <thead>
          <tr>
            <th>Slot</th>
            <th>Operator</th>
            <th>Trained Level</th>
          </tr>
        </thead>
        <tbody>
          {player.progression.trainingCore.map((core) => (
            <tr key={core.slot}>
              <td>{core.slot}</td>
              <td>{core.name}</td>
              <td>{core.trainedLevel}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <h2>Operators</h2>
      <table>
        <thead>
          <tr>
            <th>Operator</th>
            <th>Effective Level</th>
            <th>Level Source</th>
            <th>Trained Level</th>
            <th>Skill</th>
            <th>Promotion</th>
            <th>Core Slot</th>
          </tr>
        </thead>
        <tbody>
          {player.progression.operators.map((operator) => (
            <tr key={operator.operatorId}>
              <td>{operator.name}</td>
              <td>{operator.effectiveLevel}</td>
              <td>{operator.levelSource}</td>
              <td>{operator.trainedLevel}</td>
              <td>{operator.skillTraining}</td>
              <td>{operator.promotion}</td>
              <td>{operator.coreSlot ?? "-"}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <Link to={`/runs?accountId=${player.accountId}`}>View runs</Link>
    </main>
  );
}

