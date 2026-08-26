import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { adminApi } from "../shared/api/client";

type SuspiciousPlayersFilters = {
  reviewStatus: "" | "NEW" | "REVIEWING" | "CLEARED" | "SCORE_EXCLUDED" | "REWARD_HELD" | "ESCALATED" | "ACTIONED" | "FALSE_POSITIVE";
};

function reviewStatusFromSearchParam(value: string | null): SuspiciousPlayersFilters["reviewStatus"] {
  if (
    value === "NEW" ||
    value === "REVIEWING" ||
    value === "CLEARED" ||
    value === "SCORE_EXCLUDED" ||
    value === "REWARD_HELD" ||
    value === "ESCALATED" ||
    value === "ACTIONED" ||
    value === "FALSE_POSITIVE"
  ) {
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
  return `/integrity/suspicious-players/${encodeURIComponent(accountId)}`;
}

function reviewStatusLabel(status: Exclude<SuspiciousPlayersFilters["reviewStatus"], "">): string {
  const labels: Record<Exclude<SuspiciousPlayersFilters["reviewStatus"], "">, string> = {
    NEW: "신규",
    REVIEWING: "검토 중",
    CLEARED: "정상 처리",
    SCORE_EXCLUDED: "순위 제외",
    REWARD_HELD: "보상 보류",
    ESCALATED: "제재 검토",
    ACTIONED: "조치 완료",
    FALSE_POSITIVE: "오탐"
  };
  return labels[status];
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
        <h1>의심 플레이어</h1>
        <p>열린 무결성 신호를 계정 단위로 묶은 검토 큐</p>
      </header>
      <form className="toolbar" onSubmit={submitFilters}>
        <select
          aria-label="리뷰 상태"
          value={draft.reviewStatus}
          onChange={(event) =>
            setDraft({
              reviewStatus: event.target.value as SuspiciousPlayersFilters["reviewStatus"]
            })
          }
        >
          <option value="">전체 상태</option>
          <option value="NEW">신규</option>
          <option value="REVIEWING">검토 중</option>
          <option value="CLEARED">정상 처리</option>
          <option value="SCORE_EXCLUDED">순위 제외</option>
          <option value="REWARD_HELD">보상 보류</option>
          <option value="ESCALATED">제재 검토</option>
          <option value="ACTIONED">조치 완료</option>
          <option value="FALSE_POSITIVE">오탐</option>
        </select>
        <button type="submit">적용</button>
      </form>
      {query.isLoading ? <p>의심 플레이어를 불러오는 중</p> : null}
      {query.error ? <p className="error">의심 플레이어 요청 실패</p> : null}
      {query.data?.items.length === 0 ? <p>의심 플레이어가 없습니다</p> : null}
      {query.data && query.data.items.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>계정</th>
              <th>우선순위</th>
              <th>신호</th>
              <th>의심 출격</th>
              <th>마지막 탐지</th>
              <th>주요 사유</th>
              <th>리뷰</th>
              <th>운영 조치</th>
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
                  {reviewStatusLabel(player.reviewStatus)}
                  <small>{player.reviewOwner ?? "미배정"}</small>
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
