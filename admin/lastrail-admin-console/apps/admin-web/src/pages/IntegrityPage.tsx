import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import type { IntegrityEventsResponse } from "@lastrail/admin-contracts";
import { adminApi } from "../shared/api/client";

type IntegrityFilters = {
  accountId: string;
  classification: "" | "SOFT_ANOMALY" | "HARD_VIOLATION";
  reviewState: "" | "OPEN" | "IN_REVIEW" | "RESOLVED";
};

type IntegrityEventItem = IntegrityEventsResponse["items"][number];

function classificationFromSearchParam(value: string | null): IntegrityFilters["classification"] {
  if (value === "SOFT_ANOMALY" || value === "HARD_VIOLATION") return value;
  return "";
}

function reviewStateFromSearchParam(value: string | null): IntegrityFilters["reviewState"] {
  if (value === "OPEN" || value === "IN_REVIEW" || value === "RESOLVED") return value;
  return "";
}

export function integrityFiltersFromSearchParams(searchParams: URLSearchParams): IntegrityFilters {
  return {
    accountId: searchParams.get("accountId")?.trim() ?? "",
    classification: classificationFromSearchParam(searchParams.get("classification")),
    reviewState: reviewStateFromSearchParam(searchParams.get("reviewState"))
  };
}

export function integritySearchParamsFromFilters(filters: IntegrityFilters): URLSearchParams {
  const searchParams = new URLSearchParams();
  const accountId = filters.accountId.trim();
  if (accountId) searchParams.set("accountId", accountId);
  if (filters.classification) searchParams.set("classification", filters.classification);
  if (filters.reviewState) searchParams.set("reviewState", filters.reviewState);
  return searchParams;
}

export function integrityEventHref(accountId: string, runId: string | null) {
  return {
    playerHref: `/players/${encodeURIComponent(accountId)}`,
    runHref: runId ? `/runs/${encodeURIComponent(runId)}` : null
  };
}

function compactEvidence(event: IntegrityEventItem): string {
  return JSON.stringify(event.evidence);
}

export function IntegrityPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const currentFilters = integrityFiltersFromSearchParams(searchParams);
  const [draft, setDraft] = useState<IntegrityFilters>(currentFilters);
  const [filters, setFilters] = useState<IntegrityFilters>(currentFilters);
  const query = useQuery({
    queryKey: ["integrity-events", filters],
    queryFn: () =>
      adminApi.integrityEvents({
        accountId: filters.accountId.trim() || undefined,
        classification: filters.classification || undefined,
        reviewState: filters.reviewState || undefined
      })
  });

  function submitFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFilters(draft);
    setSearchParams(integritySearchParamsFromFilters(draft));
  }

  return (
    <main className="content">
      <header>
        <h1>Integrity</h1>
        <p>Investigation signals and validator evidence</p>
      </header>
      <form className="toolbar" onSubmit={submitFilters}>
        <input
          aria-label="Account ID"
          placeholder="Account ID"
          value={draft.accountId}
          onChange={(event) => setDraft({ ...draft, accountId: event.target.value })}
        />
        <select
          aria-label="Classification"
          value={draft.classification}
          onChange={(event) =>
            setDraft({
              ...draft,
              classification: event.target.value as IntegrityFilters["classification"]
            })
          }
        >
          <option value="">Any classification</option>
          <option value="SOFT_ANOMALY">Soft anomaly</option>
          <option value="HARD_VIOLATION">Hard violation</option>
        </select>
        <select
          aria-label="Review State"
          value={draft.reviewState}
          onChange={(event) =>
            setDraft({ ...draft, reviewState: event.target.value as IntegrityFilters["reviewState"] })
          }
        >
          <option value="">Any state</option>
          <option value="OPEN">Open</option>
          <option value="IN_REVIEW">In review</option>
          <option value="RESOLVED">Resolved</option>
        </select>
        <button type="submit">Apply</button>
      </form>
      {query.isLoading ? <p>Loading integrity events</p> : null}
      {query.error ? <p className="error">Integrity request failed</p> : null}
      {query.data?.items.length === 0 ? <p>No integrity events found</p> : null}
      {query.data && query.data.items.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>Time</th>
              <th>Account</th>
              <th>Run</th>
              <th>Classification</th>
              <th>Evidence</th>
              <th>Review</th>
              <th>Signal</th>
            </tr>
          </thead>
          <tbody>
            {query.data.items.map((event) => {
              const href = integrityEventHref(event.accountId, event.runId);
              return (
                <tr key={event.id}>
                  <td>{new Date(event.createdAt).toLocaleString()}</td>
                  <td>
                    <Link to={href.playerHref}>{event.accountId}</Link>
                  </td>
                  <td>{href.runHref ? <Link to={href.runHref}>{event.runId}</Link> : "-"}</td>
                  <td>{event.classification}</td>
                  <td>
                    {event.evidenceCode}
                    <small className="audit-summary">{compactEvidence(event)}</small>
                  </td>
                  <td>
                    {event.reviewState}
                    <small>{event.assignedTo ?? "Unassigned"}</small>
                  </td>
                  <td>
                    {event.category}
                    <small>{event.id}</small>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      ) : null}
    </main>
  );
}
