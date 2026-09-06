import { useState, type FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { opsSeverityLabel, opsErrorLogStatusLabel, opsErrorLogSourceLabel } from "../shared/labels/adminLabels";
import type { OpsErrorLogReadFiltersPreview } from "../shared/opsErrorLogs/opsErrorLogContract";
import "./errorLogs.css";

export type LogFilters = { q: string; environment: string; service: string; severity: string; status: string; from: string; to: string };
export const emptyLogFilters: LogFilters = { q: "", environment: "", service: "", severity: "", status: "", from: "", to: "" };
export function buildLogFilters(category: "server" | "frontend", filters: LogFilters, cursor = ""): Partial<OpsErrorLogReadFiltersPreview> {
  return { category, q: filters.q.trim(), environment: filters.environment, service: filters.service, severity: filters.severity, status: filters.status,
    occurredFrom: filters.from ? new Date(filters.from).toISOString() : "", occurredTo: filters.to ? new Date(filters.to).toISOString() : "", limit: "25", cursor };
}
export function logTime(value: string) {
  const date = new Date(/^\d+$/.test(value) ? Number(value) : value);
  if (Number.isNaN(date.getTime())) return value;
  const parts = Object.fromEntries(new Intl.DateTimeFormat("en-GB", { timeZone: "Asia/Seoul", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23" }).formatToParts(date).map((part) => [part.type, part.value]));
  return `${parts.year}-${parts.month}-${parts.day} ${parts.hour}:${parts.minute}:${parts.second}`;
}

export function ErrorLogsPage() {
  const [category, setCategory] = useState<"server" | "frontend">("server");
  const [draft, setDraft] = useState<LogFilters>({ ...emptyLogFilters });
  const [filters, setFilters] = useState<LogFilters>({ ...emptyLogFilters });
  const [cursors, setCursors] = useState<string[]>([""]);
  const [selected, setSelected] = useState<string | null>(null);
  const queryFilters = buildLogFilters(category, filters, cursors[cursors.length - 1]);
  const query = useQuery({ queryKey: ["ops-error-logs", queryFilters], queryFn: () => adminApi.opsErrorLogs(queryFilters), retry: false });
  const detail = useQuery({ queryKey: ["ops-error-log", selected], queryFn: () => adminApi.opsErrorLogDetail(selected!), enabled: selected !== null, retry: false });
  const error = query.error ? adminRequestFailureDisplay(query.error, { resourceLabel: "에러 로그", resourceId: "list" }) : null;
  function changeTab(next: "server" | "frontend") {
    setCategory(next); setCursors([""]); setSelected(null);
    setDraft({ ...emptyLogFilters }); setFilters({ ...emptyLogFilters });
  }
  function apply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setFilters({ ...draft }); setCursors([""]); setSelected(null);
  }
  const rangeInvalid = Boolean(draft.from && draft.to && draft.from > draft.to);
  function selectFilter(key: "environment" | "service" | "severity" | "status", label: string, codes: string[], labelFor: (code: string) => string = (code) => code) {
    return <label>{label}<select value={draft[key]} onChange={(e) => setDraft({ ...draft, [key]: e.target.value })}><option value="">전체</option>{codes.map((code) => <option key={code} value={code}>{labelFor(code)}</option>)}</select></label>;
  }
  return <main className="content error-log-page">
    <h1>시스템 에러 로그</h1>
    <div role="tablist" aria-label="에러 발생 영역" className="log-tabs">
      {(["server", "frontend"] as const).map((tab) => <button key={tab} id={`log-tab-${tab}`} role="tab" aria-selected={category === tab} aria-controls="log-panel" onClick={() => changeTab(tab)}>{tab === "server" ? "서버" : "프론트"}</button>)}
    </div>
    <section id="log-panel" role="tabpanel" aria-labelledby={`log-tab-${category}`}>
      <form onSubmit={apply} className="log-filters">
        <label className="log-search">검색<input aria-label="로그 검색" maxLength={200} placeholder="에러코드, 내용, 로그 ID, 요청 ID, 리소스 ID" value={draft.q} onChange={(e) => setDraft({ ...draft, q: e.target.value })} /></label>
        {selectFilter("environment", "환경", ["DEV", "PRD"])}
        {selectFilter("service", "발생 서비스", category === "server" ? ["admin_api", "game_api", "worker_cron", "migration_verify", "support_read_model", "integrity_pipeline"] : [], opsErrorLogSourceLabel)}
        {selectFilter("severity", "심각도", ["info", "warning", "error", "critical"], opsSeverityLabel)}
        {selectFilter("status", "상태", ["open", "acknowledged", "resolved", "ignored"], opsErrorLogStatusLabel)}
        <label>시작 시점 (현지 시간)<input aria-label="시작 시점" type="datetime-local" value={draft.from} onChange={(e) => setDraft({ ...draft, from: e.target.value })} /></label>
        <label>종료 시점 (현지 시간)<input aria-label="종료 시점" type="datetime-local" value={draft.to} onChange={(e) => setDraft({ ...draft, to: e.target.value })} /></label>
        <button type="submit" disabled={rangeInvalid}>검색</button>
        <button type="button" onClick={() => { setDraft({ ...emptyLogFilters }); setFilters({ ...emptyLogFilters }); setCursors([""]); setSelected(null); }}>초기화</button>
        <button type="button" disabled={query.isFetching} onClick={() => void query.refetch()}>새로고침</button>
        {rangeInvalid && <p role="alert">종료 시점은 시작 시점 이후로 선택하세요.</p>}
      </form>
      {query.data?.collectionAvailable === false && <p role="status">프론트 로그 수집 미연결. 서버 로그는 서버 탭에서 조회할 수 있습니다.</p>}
      {error && <p role="alert">{error.reason} · {error.nextStep}</p>}
      <div className="table-wrap"><table><thead><tr>{["발생 시점 (KST)", "에러코드", "에러내용", "발생 위치", "심각도 / 상태", "추적코드"].map((label) => <th key={label}>{label}</th>)}</tr></thead>
        <tbody>{query.isLoading ? <tr><td colSpan={6}>조회 중</td></tr> : !query.error && !query.data?.items.length ? <tr><td colSpan={6}>데이터 없음</td></tr> : query.data?.items.map((row) => <tr key={row.id}>
          <td>{logTime(row.occurredAt)}</td><td><code>{row.errorCode}</code></td><td>{row.messageSummary}</td>
          <td>{opsErrorLogSourceLabel(row.service)}<small>{row.environment} · {row.source}</small></td>
          <td>{opsSeverityLabel(row.severity)}<small>{opsErrorLogStatusLabel(row.status)}</small></td>
          <td><button className="log-id" onClick={() => setSelected(row.id)} aria-label={`로그 상세 ${row.id}`}>{row.id}</button><small>요청: {row.requestId}</small></td>
        </tr>)}</tbody></table></div>
      <nav className="log-paging" aria-label="로그 페이지"><button disabled={cursors.length === 1 || query.isFetching} onClick={() => { setCursors(cursors.slice(0, -1)); setSelected(null); }}>이전</button><span>{cursors.length} 페이지</span><button disabled={!query.data?.nextCursor || query.isFetching} onClick={() => { setCursors([...cursors, query.data!.nextCursor!]); setSelected(null); }}>다음</button></nav>
      {selected && <section className="log-detail" aria-label="로그 추적 상세"><h2>로그 추적 상세</h2><button onClick={() => setSelected(null)}>닫기</button>
        {detail.isLoading && <p>조회 중</p>}
        {detail.error && <p role="alert">{adminRequestFailureDisplay(detail.error, { resourceLabel: "로그 상세", resourceId: selected }).reason}</p>}
        {detail.data && <dl>{([ ["로그 ID", detail.data.item.id], ["요청 ID", detail.data.item.requestId], ["추적 참조", detail.data.item.traceRef], ["계정 ID", detail.data.item.accountId], ["출격 ID", detail.data.item.runId], ["보상 미리보기 ID", detail.data.item.rewardPreviewId], ["보상 수령 ID", detail.data.item.rewardClaimId], ["오프라인 보상 ID", detail.data.item.offlineRewardClaimId], ["모집 배치 ID", detail.data.item.gachaBatchId], ["마스킹", detail.data.item.masked === true ? "마스킹됨" : "확인 필요"] ]).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || "없음"}</dd></div>)}</dl>}
      </section>}
    </section>
  </main>;
}
