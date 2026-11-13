import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { ticketCategories } from "../shared/support/tickets";
import { runTimeLabel } from "./RunsPage";
import {SupportProcessingPanel} from '../shared/support/SupportProcessingPanel';

function TicketError({error}: {error: unknown}) {
  const message = adminRequestFailureDisplay(error, { resourceLabel: "고객문의", resourceId: "/api/support/tickets" });
  return <p role="alert">{message.reason} · 문의 API 배포 및 접근 권한 확인 필요</p>;
}
export function SupportTicketsPage() {
  const [draft, setDraft] = useState({accountId:"", category:""});
  const [filters, setFilters] = useState(draft);
  const [cursors, setCursors] = useState([""]);
  const cursor = cursors[cursors.length - 1];
  const query = useQuery({ queryKey: ["support-tickets", filters, cursor], queryFn: () => adminApi.supportTickets({...filters, limit:"25", cursor}), retry: false });
  return <main className="content"><h1>CS 지원</h1>
    <form className="toolbar" onSubmit={e => {e.preventDefault(); setFilters({...draft, accountId:draft.accountId.trim()}); setCursors([""]);}}>
      <input aria-label="계정 ID" placeholder="계정 ID" value={draft.accountId} onChange={e => setDraft({...draft, accountId:e.target.value})} />
      <select aria-label="문의 카테고리" value={draft.category} onChange={e => setDraft({...draft, category:e.target.value})}><option value="">전체 카테고리</option>{Object.entries(ticketCategories).map(([code,label]) => <option key={code} value={code}>{label}</option>)}</select><button>조회</button>
    </form>
    {query.isLoading ? <p>조회 중</p> : query.error ? <TicketError error={query.error}/> : !query.data?.items.length ? <p>문의 없음</p> : <div style={{overflowX:"auto"}}><table><thead><tr>{["접수 시각 (KST)","문의 ID","계정 ID","카테고리","내용","앱 버전 / 코드"].map(label => <th key={label}>{label}</th>)}</tr></thead><tbody>{query.data.items.map(t => <tr key={t.ticketId}>
      <td>{runTimeLabel(t.serverReceivedAt)}</td><td><Link to={`/support/tickets/${encodeURIComponent(t.ticketId)}`}>{t.ticketId}</Link></td><td><Link to={`/support/accounts/${encodeURIComponent(t.accountId)}`}>{t.accountId || "미기록"}</Link></td><td>{ticketCategories[t.category] ?? t.category}</td><td style={{whiteSpace:"pre-wrap",overflowWrap:"anywhere",maxWidth:360}}>{t.bodyPreview || "내용 없음"}</td><td>{t.appVersion || "미기록"} / {t.appVersionCode || "미기록"}</td>
    </tr>)}</tbody></table></div>}
    <nav aria-label="문의 페이지"><button disabled={cursors.length === 1 || query.isFetching} onClick={() => setCursors(c => c.slice(0,-1))}>이전</button> <button disabled={!query.data?.nextCursor || query.isFetching} onClick={() => {if(query.data?.nextCursor) setCursors(c => [...c,query.data!.nextCursor!]);}}>다음</button></nav>
  </main>;
}
export function SupportTicketPage() {
  const {ticketId = ""} = useParams();
  const query = useQuery({queryKey:["support-ticket",ticketId],queryFn:()=>adminApi.supportTicket(ticketId),retry:false,enabled:!!ticketId});
  const t = query.data;
  return <main className="content"><Link to="/support/tickets">문의 목록</Link><h1>고객문의 상세</h1>
    {query.isLoading ? <p>조회 중</p> : query.error ? <TicketError error={query.error}/> : t ? <>
      <dl>{[["문의 ID",t.ticketId],["카테고리",ticketCategories[t.category] ?? t.category],["접수 시각 (KST)",runTimeLabel(t.serverReceivedAt)],["클라이언트 작성 시각 (KST)",runTimeLabel(t.clientComposedAt)],["클라이언트 제출 시각 (KST)",runTimeLabel(t.clientSubmittedAt)],["앱 버전",t.appVersion || "미기록"],["버전 코드",t.appVersionCode || "미기록"]].map(([label,value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>
      <Link to={`/support/accounts/${encodeURIComponent(t.accountId)}`}>계정 지원 기록 · {t.accountId}</Link>
      <h2>문의 내용</h2><p style={{whiteSpace:"pre-wrap",overflowWrap:"anywhere"}}>{t.body || "내용 없음"}</p>
      <SupportProcessingPanel ticketId={ticketId}/>
    </> : <p>문의 없음</p>}
  </main>;
}
