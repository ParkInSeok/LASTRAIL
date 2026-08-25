import { FormEvent, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";

export function PlayersPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const queryText = searchParams.get("q") ?? "";
  const [draft, setDraft] = useState(queryText);
  const query = useQuery({
    queryKey: ["players", queryText],
    queryFn: () => adminApi.searchPlayers(queryText)
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    setSearchParams(draft ? { q: draft } : {});
  }

  return (
    <main className="content">
      <header>
        <h1>Players</h1>
        <p>Account search and read-only inspection</p>
      </header>
      <form className="toolbar" onSubmit={submit}>
        <input
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="Account ID or display name"
        />
        <button type="submit">Search</button>
      </form>
      {query.isLoading && <p>Loading players</p>}
      {query.error && <p className="error">Players request failed</p>}
      {query.data && (
        <table>
          <thead>
            <tr>
              <th>Account</th>
              <th>Status</th>
              <th>Sync</th>
              <th>Risk</th>
              <th>Review</th>
            </tr>
          </thead>
          <tbody>
            {query.data.items.map((player) => (
              <tr key={player.accountId}>
                <td>
                  <Link to={`/players/${player.accountId}`}>{player.displayName}</Link>
                  <small>{player.accountId}</small>
                </td>
                <td>{player.status}</td>
                <td>{player.syncLevel}</td>
                <td>{player.risk ?? "-"}</td>
                <td>{player.reviewStatus ?? "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}

