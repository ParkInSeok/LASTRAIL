import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { adminApi } from "../shared/api/client";

type SuspiciousPlayersFilters = {
  reviewStatus: "" | "UNREVIEWED" | "IN_REVIEW" | "CONFIRMED_VIOLATION";
};

function reviewStatusFromSearchParam(value: string | null): SuspiciousPlayersFilters["reviewStatus"] {
  if (value === "UNREVIEWED" || value === "IN_REVIEW" || value === "CONFIRMED_VIOLATION") {
    return value;
  }
  return "";
}

export function suspiciousPlayersFiltersFromSearchParams(searchParams: URLSearchParams): SuspiciousPlayersFilters {
  return {
    reviewStatus: reviewStatusFromSearchParam(searchParams.get("reviewStatus"))
  };
}

export function suspiciousPlayersSearchParamsFromFilters(filters: SuspiciousPlayersFilters): URLSearchParams {
  const searchParams = new URLSearchParams();
  if (filters.reviewStatus) searchParams.set("reviewStatus", filters.reviewStatus);
  return searchParams;
}

export function suspiciousPlayerHref(accountId: string): string {
  return `/players/${encodeURIComponent(accountId)}`;
}

export function SuspiciousPlayersPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const currentFilters = suspiciousPlayersFiltersFromSearchParams(searchParams);
  const [draft, setDraft] = useState<SuspiciousPlayersFilters>(currentFilters);
  const [filters, setFilters] = useState<SuspiciousPlayersFilters>(currentFilters);
  const query = useQuery({
    queryKey: ["suspicious-players", filters],
    queryFn: () =>
      adminApi.suspiciousPlayers({
        reviewStatus: filters.reviewStatus || undefined
      })
  });

  function submitFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFilters(draft);
    setSearchParams(suspiciousPlayersSearchParamsFromFilters(draft));
  }

  return (
    <main className="content">
      <header>
        <h1>Suspicious Players</h1>
        <p>Account-level investigation queue from open integrity signals</p>
      </header>
      <form className="toolbar" onSubmit={submitFilters}>
        <select
          aria-label="Review Status"
          value={draft.reviewStatus}
          onChange={(event) =>
            setDraft({
              reviewStatus: event.target.value as SuspiciousPlayersFilters["reviewStatus"]
            })
          }
        >
          <option value="">Any status</option>
          <option value="UNREVIEWED">Unreviewed</option>
          <option value="IN_REVIEW">In review</option>
          <option value="CONFIRMED_VIOLATION">Confirmed violation</option>
        </select>
        <button type="submit">Apply</button>
      </form>
      {query.isLoading ? <p>Loading suspicious players</p> : null}
      {query.error ? <p className="error">Suspicious players request failed</p> : null}
      {query.data?.items.length === 0 ? <p>No suspicious players found</p> : null}
      {query.data && query.data.items.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>Account</th>
              <th>Priority</th>
              <th>Signals</th>
              <th>Suspicious Runs</th>
              <th>Last Detection</th>
              <th>Primary Reason</th>
              <th>Review</th>
              <th>Enforcement</th>
            </tr>
          </thead>
          <tbody>
            {query.data.items.map((player) => (
              <tr key={player.accountId}>
                <td>
                  <Link to={suspiciousPlayerHref(player.accountId)}>{player.displayName}</Link>
                  <small>{player.accountId}</small>
                </td>
                <td>{player.priority}</td>
                <td>
                  {player.openSignals}
                  <small>{player.hardViolations} hard</small>
                </td>
                <td>{player.suspiciousRuns}</td>
                <td>{new Date(player.lastDetectionAt).toLocaleString()}</td>
                <td>{player.primaryReason}</td>
                <td>
                  {player.reviewStatus}
                  <small>{player.reviewOwner ?? "Unassigned"}</small>
                </td>
                <td>{player.currentEnforcement}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </main>
  );
}
