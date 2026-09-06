import { FormEvent, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { accountStatusLabel, priorityLabel, reviewStatusLabel } from "../shared/labels/adminLabels";

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
        <h1>플레이어</h1>
        <p>계정 검색과 읽기 전용 조회</p>
      </header>
      <form className="toolbar" onSubmit={submit}>
        <input
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="계정 ID 또는 닉네임"
        />
        <button type="submit">검색</button>
      </form>
      {query.isLoading && <p>플레이어를 불러오는 중</p>}
      {query.error && <p className="error">플레이어 요청 실패</p>}
      {query.data && (
        <table>
          <thead>
            <tr>
              <th>닉네임</th>
              <th>계정 ID</th>
              <th>상태</th>
              <th>동기화</th>
              <th>위험도</th>
              <th>검토</th>
            </tr>
          </thead>
          <tbody>
            {query.data.items.map((player) => (
              <tr key={player.accountId}>
                <td>
                  {player.displayName?.trim() || "닉네임 없음"}
                </td>
                <td><Link to={`/players/${encodeURIComponent(player.accountId)}`}>{player.accountId}</Link></td>
                <td>{accountStatusLabel(player.status)}</td>
                <td>{player.syncLevel}</td>
                <td>{player.risk ? priorityLabel(player.risk) : "-"}</td>
                <td>{player.reviewStatus ? reviewStatusLabel(player.reviewStatus) : "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
