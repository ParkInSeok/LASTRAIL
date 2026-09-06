import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { gachaGuaranteeTypeLabel } from "../shared/labels/adminLabels";
import "./gachaHistory.css";

export function bannerTypeLabel(value?: string) {
  return value ? ({ PICKUP: "픽업", STANDARD: "일반", NORMAL: "일반", NON_PICKUP: "일반" } as Record<string, string>)[value] ?? value : "유형 미기록";
}
export function historyFilters(accountId: string, bannerId: string, cursor = "") {
  return { accountId: accountId.trim(), bannerId, cursor, limit: "25" };
}
function failure(error: unknown) {
  return adminRequestFailureDisplay(error, { resourceLabel: "뽑기 이력", resourceId: "/api/gacha" }).reason;
}
function PullResults({ batchId }: { batchId: string }) {
  const query = useQuery({ queryKey: ["gacha-pull-batch-detail", batchId], queryFn: () => adminApi.gachaPullBatchDetail(batchId), retry: false });
  if (query.isLoading) return <>조회 중</>;
  if (query.error) return <span role="status">{failure(query.error)}</span>;
  const results = query.data?.item.results ?? [];
  if (!results.length) return <>결과 없음</>;
  const evidence = query.data?.item.causeEvidence;
  return <><ul className="gacha-results">{results.map((r, i) => <li key={`${r.resultId}-${i}`}>
    <span>{r.rewardId}</span> <small>{r.resultRarity}{r.isPickup === "예" ? " · 픽업" : ""}{r.wasDuplicate === "예" ? " · 중복" : ""}</small>
    {r.duplicateConversion !== "없음" && <small> → {r.duplicateConversion}</small>}
  </li>)}</ul>{evidence && <details><summary>서버 참조 기록</summary><dl>
    <dt>중복 결과 수</dt><dd>{evidence.duplicateResultCount ?? "미기록"}</dd>
    <dt>중복 요청 기록</dt><dd>{evidence.idempotencyReplayKnown === null ? "미기록" : evidence.idempotencyReplayKnown ? "있음" : "없음"}</dd>
    <dt>보장 유형</dt><dd>{evidence.guaranteeTypes.map(gachaGuaranteeTypeLabel).join(", ") || "없음"}</dd>
    <dt>원인 판정</dt><dd>미확정 · 저장된 서버 기록만 표시</dd>
  </dl></details>}</>;
}
export function GachaHistory() {
  const [params, setParams] = useSearchParams();
  const accountId = params.get("accountId") ?? "";
  const bannerId = params.get("bannerId") ?? "";
  const [account, setAccount] = useState(accountId);
  const [banner, setBanner] = useState(bannerId);
  const [cursors, setCursors] = useState([""]);
  const cursor = cursors[cursors.length - 1];
  const banners = useQuery({ queryKey: ["gacha-banners"], queryFn: () => adminApi.gachaBanners(), retry: false });
  const query = useQuery({ queryKey: ["gacha-history", accountId, bannerId, cursor], queryFn: () => adminApi.gachaPullBatches(historyFilters(accountId, bannerId, cursor)), retry: false });
  return <main className="content gacha-history">
    <h1>요원 모집</h1>
    <form className="gacha-filters" onSubmit={e => { e.preventDefault(); setCursors([""]); setParams({ accountId: account.trim(), bannerId: banner }); }}>
      <label>계정 ID<input value={account} onChange={e => setAccount(e.target.value)} /></label>
      <label>모집 배너<select value={banner} onChange={e => setBanner(e.target.value)}><option value="">전체</option>{banners.data?.items.map(b => <option key={b.bannerId} value={b.bannerId}>{bannerTypeLabel(b.bannerType)} · {b.bannerId}</option>)}</select></label>
      <button type="submit">조회</button>
    </form>
    {banners.error && <p role="status">배너 정보: {failure(banners.error)}</p>}
    {query.isLoading ? <p role="status">조회 중</p> : query.error ? <p role="alert">{failure(query.error)}</p> : !query.data?.items.length ? <p>뽑기 이력 없음</p> :
      <div className="gacha-table-scroll"><table><thead><tr><th>뽑기 시점 (KST)</th><th>계정</th><th>모집 종류 / 배너</th><th>횟수</th><th>획득 결과</th><th>뽑기 ID</th></tr></thead><tbody>
        {query.data.items.map(batch => <tr key={batch.batchId}>
          <td>{Number.isNaN(Date.parse(batch.createdAt)) ? batch.createdAt : new Date(batch.createdAt).toLocaleString("ko-KR", { timeZone: "Asia/Seoul", hour12: false })}</td>
          <td><Link to={`/players/${encodeURIComponent(batch.accountId)}`}>{batch.accountId}</Link></td>
          <td>{bannerTypeLabel(banners.data?.items.find(b => b.bannerId === batch.bannerId)?.bannerType)}<small>{batch.bannerId}</small></td>
          <td>{batch.pullCount}회</td><td><PullResults batchId={batch.batchId} /></td><td>{batch.batchId}</td>
        </tr>)}
      </tbody></table></div>}
    <nav aria-label="뽑기 이력 페이지" className="gacha-filters">
      <button disabled={cursors.length === 1 || query.isFetching} onClick={() => setCursors(c => c.slice(0, -1))}>이전</button>
      <button disabled={!query.data?.nextCursor || query.isFetching} onClick={() => { if (query.data?.nextCursor) setCursors(c => [...c, query.data!.nextCursor!]); }}>다음</button>
    </nav>
  </main>;
}
