import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import type { IntegrityEventsResponse } from "@lastrail/admin-contracts";
import { adminApi } from "../shared/api/client";
import {
  categoryLabel,
  classificationLabel,
  evidenceCodeLabel,
  reviewStateLabel
} from "../shared/labels/adminLabels";

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
        <h1>무결성</h1>
        <p>조사 신호와 검증 증거</p>
      </header>
      <form className="toolbar" onSubmit={submitFilters}>
        <input
          aria-label="계정 ID"
          placeholder="계정 ID"
          value={draft.accountId}
          onChange={(event) => setDraft({ ...draft, accountId: event.target.value })}
        />
        <select
          aria-label="분류"
          value={draft.classification}
          onChange={(event) =>
            setDraft({
              ...draft,
              classification: event.target.value as IntegrityFilters["classification"]
            })
          }
        >
          <option value="">전체 분류</option>
          <option value="SOFT_ANOMALY">경미 이상</option>
          <option value="HARD_VIOLATION">중대 위반</option>
        </select>
        <select
          aria-label="검토 상태"
          value={draft.reviewState}
          onChange={(event) =>
            setDraft({ ...draft, reviewState: event.target.value as IntegrityFilters["reviewState"] })
          }
        >
          <option value="">전체 상태</option>
          <option value="OPEN">신규</option>
          <option value="IN_REVIEW">검토 중</option>
          <option value="RESOLVED">처리 완료</option>
        </select>
        <button type="submit">적용</button>
      </form>
      {query.isLoading ? <p>무결성 이벤트를 불러오는 중</p> : null}
      {query.error ? <p className="error">무결성 이벤트 요청 실패</p> : null}
      {query.data?.items.length === 0 ? <p>무결성 이벤트가 없습니다</p> : null}
      {query.data && query.data.items.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>시각</th>
              <th>계정</th>
              <th>출격</th>
              <th>분류</th>
              <th>증거</th>
              <th>검토</th>
              <th>신호</th>
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
                  <td>{classificationLabel(event.classification)}</td>
                  <td>
                    {evidenceCodeLabel(event.evidenceCode)}
                    <small className="audit-summary">{compactEvidence(event)}</small>
                  </td>
                  <td>
                    {reviewStateLabel(event.reviewState)}
                    <small>{event.assignedTo ?? "미배정"}</small>
                  </td>
                  <td>
                    {categoryLabel(event.category)}
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
