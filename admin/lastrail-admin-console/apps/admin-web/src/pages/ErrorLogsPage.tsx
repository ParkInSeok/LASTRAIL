import {opsReadRequest} from '../shared/opsErrorLogs/opsReadRequest';
import type {BootstrapResponse} from '@lastrail/admin-contracts';
import type {AdminSessionResources} from '../shared/auth/AdminSessionResources';
import {OpsErrorLogsController} from '../shared/opsErrorLogs/opsErrorLogsController';
import {ReviewLifetime} from '../shared/liveops/ReviewLifetime';
import {OpsInvestigationReadPanel} from '../shared/opsErrorLogs/OpsInvestigationReadPanel';
import { useState,useEffect,useMemo, type FormEvent } from "react";
import { useQuery,useQueryClient } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { useAdmin } from "../shared/auth/AdminContext";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import { opsSeverityLabel, opsErrorLogStatusLabel, opsErrorLogSourceLabel } from "../shared/labels/adminLabels";
import type { OpsErrorLogReadFiltersPreview } from "../shared/opsErrorLogs/opsErrorLogContract";
import "./errorLogs.css";

export type LogFilters = { q: string; serverVersion: string; appVersion: string; appVersionCode: string; service: string; severity: string; status: string; from: string; to: string;phase:string;contractVersion:string };
export const emptyLogFilters: LogFilters = { q: "", serverVersion: "", appVersion: "", appVersionCode: "", service: "", severity: "", status: "", from: "", to: "",phase:"",contractVersion:"" };
export function buildLogFilters(category: "server" | "frontend", filters: LogFilters, cursor = ""): Partial<OpsErrorLogReadFiltersPreview> {
  return { category, q: filters.q.trim(),phase:filters.phase?.trim(),contractVersion:filters.contractVersion?.trim(), ...(category === "server" ? { serverVersion: filters.serverVersion.trim() } : { appVersion: filters.appVersion.trim(), appVersionCode: filters.appVersionCode.trim() }), service: filters.service, severity: filters.severity, status: filters.status,
    occurredFrom: filters.from ? new Date(filters.from).toISOString() : "", occurredTo: filters.to ? new Date(filters.to).toISOString() : "", limit: "25", cursor };
}
export function logTime(value: string) {
  const date = new Date(/^\d+$/.test(value) ? Number(value) : value);
  if (Number.isNaN(date.getTime())) return value;
  const parts = Object.fromEntries(new Intl.DateTimeFormat("en-GB", { timeZone: "Asia/Seoul", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23" }).formatToParts(date).map((part) => [part.type, part.value]));
  return `${parts.year}-${parts.month}-${parts.day} ${parts.hour}:${parts.minute}:${parts.second}`;
}

export function ErrorLogsPage(){const {bootstrap,isLoading,error,session,sessionActive}=useAdmin();if(isLoading)return <main className="content"><p>운영자 확인 중</p></main>;if(!bootstrap||error||sessionActive===false||!bootstrap.admin.permissions.includes('AUDIT_READ'))return <main className="content"><p role="alert">운영자와 오류 조회 권한을 확인하세요.</p></main>;return <ScopedErrorLogsPage key={`${bootstrap.admin.id}:${bootstrap.admin.role}:${bootstrap.environment}:${session?.generation??'local'}`} bootstrap={bootstrap} session={session}/>;}
function ScopedErrorLogsPage({bootstrap,session}:{bootstrap:BootstrapResponse;session?:AdminSessionResources}){
 const queryClient=useQueryClient();const [generation]=useState(()=>session?.generation??crypto.randomUUID());
 const scope={principalId:bootstrap.admin.id,environment:bootstrap.environment==='prd'?'PRD':'DEV',authorizationRevision:generation};
 const prefix=['ops-mounted',generation];const create=()=>{const controller=new OpsErrorLogsController(scope,bootstrap.admin.permissions,adminApi);return {controller,dispose(){controller.dispose();queryClient.removeQueries({queryKey:prefix});}};};
 const [resource]=useState(()=>session?session.acquire('ops-errors',create):create()),controller=resource.controller;
 const [lifetime]=useState(()=>new ReviewLifetime(()=>{if(!session)resource.dispose();}));const [,changed]=useState(0);
 useEffect(()=>{const release=lifetime.enter(),off=controller.subscribe(()=>{changed(v=>v+1);if(!controller.matchesScope(scope))session?.revoke();});return()=>{off();release();};},[controller,lifetime]);
 const active=controller.matchesScope(scope);const options={retry:false,staleTime:Infinity,refetchOnWindowFocus:false,refetchOnMount:false};

  const [category, setCategory] = useState<"server" | "frontend">("server");
  const [draft, setDraft] = useState<LogFilters>({ ...emptyLogFilters });
  const [filters, setFilters] = useState<LogFilters>({ ...emptyLogFilters });
  const [cursors, setCursors] = useState<string[]>([""]);
  const [selected, setSelected] = useState<string | null>(null);
  const queryFilters = buildLogFilters(category, filters, cursors[cursors.length - 1]);
  const listRead=useMemo(()=>opsReadRequest((force)=>controller.reads.list(queryFilters,force)),[controller,JSON.stringify(queryFilters)]);
 const summaryRead=useMemo(()=>opsReadRequest((force)=>controller.reads.summary(queryFilters,force)),[controller,JSON.stringify(queryFilters)]);
 const investigationRead=useMemo(()=>opsReadRequest((force)=>controller.investigations.load(selected!,force)),[controller,selected]);
  const query = useQuery({queryKey:[...prefix,'list',queryFilters],queryFn:async()=>{const value=await listRead.query();if(!value)throw Error('오류 조회 수명 종료');return value;},enabled:active,...options});
 const summary=useQuery({queryKey:[...prefix,'summary',queryFilters],queryFn:async()=>{const value=await summaryRead.query();if(!value)throw Error('집계 조회 수명 종료');return value;},enabled:active&&category==='server',...options});
  const detail = useQuery({queryKey:[...prefix,'detail',selected],queryFn:async()=>{const value=await controller.reads.detail(selected!);if(!value)throw Error('상세 조회 수명 종료');return value;},enabled:active&&selected!==null,...options});
 const investigation=useQuery({queryKey:[...prefix,'investigation',selected],queryFn:async()=>{const value=await investigationRead.query();if(!value)throw Error('조사 조회 수명 종료');return value;},enabled:active&&selected!==null,...options});
 const refresh=async()=>{await Promise.allSettled([listRead.refresh(()=>query.refetch({throwOnError:true})),...(category==='server'?[summaryRead.refresh(()=>summary.refetch({throwOnError:true}))]:[])]);};
  const error = query.error ? adminRequestFailureDisplay(query.error, { resourceLabel: "에러 로그", resourceId: "list" }) : null;
  function changeTab(next: "server" | "frontend") {
    setCategory(next); setCursors([""]); setSelected(null);
    setDraft({ ...emptyLogFilters }); setFilters({ ...emptyLogFilters });
  }
  function apply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setFilters({ ...draft }); setCursors([""]); setSelected(null);
  }
  const rangeInvalid = Boolean(draft.from && draft.to && draft.from > draft.to);
  function selectFilter(key: "service" | "severity" | "status", label: string, codes: string[], labelFor: (code: string) => string = (code) => code) {
    return <label>{label}<select value={draft[key]} onChange={(e) => setDraft({ ...draft, [key]: e.target.value })}><option value="">전체</option>{codes.map((code) => <option key={code} value={code}>{labelFor(code)}</option>)}</select></label>;
  }
  if(!active)return <main className="content"><p role="alert">조회 권한이 변경되어 기존 오류 정보를 닫았습니다.</p></main>;
  return <main className="content error-log-page">
    <header className="log-heading"><h1>시스템 에러 로그</h1><span>{bootstrap ? `${bootstrap.environment.toUpperCase()} Server` : "Server 확인 중"}</span></header>
    <div role="tablist" aria-label="에러 발생 영역" className="log-tabs">
      {(["server", "frontend"] as const).map((tab) => <button key={tab} id={`log-tab-${tab}`} role="tab" aria-selected={category === tab} aria-controls="log-panel" onClick={() => changeTab(tab)}>{tab === "server" ? "서버" : "프론트"}</button>)}
    </div>
    <section id="log-panel" role="tabpanel" aria-labelledby={`log-tab-${category}`}>
      <form onSubmit={apply} className="log-filters">
        <label className="log-search">검색<input aria-label="로그 검색" maxLength={200} placeholder="에러코드, 내용, 로그 ID, 요청 ID, 리소스 ID" value={draft.q} onChange={(e) => setDraft({ ...draft, q: e.target.value })} /></label>
        {selectFilter("service", "발생 서비스", category === "server" ? ["admin_api", "game_api", "worker_cron", "migration_verify", "support_read_model", "integrity_pipeline"] : [], opsErrorLogSourceLabel)}
        {selectFilter("severity", "심각도", ["info", "warning", "error", "critical"], opsSeverityLabel)}
        {category === "server" && <label>서버 버전<input aria-label="서버 버전" placeholder="배포 ID 전체 일치" maxLength={100} pattern="[A-Za-z0-9._-]+" value={draft.serverVersion} onChange={(e) => setDraft({ ...draft, serverVersion: e.target.value })} /></label>}
        {category === "frontend" && <>
          <label>앱 버전<input aria-label="앱 버전" placeholder="예: 0.1.0" maxLength={100} value={draft.appVersion} onChange={(e) => setDraft({ ...draft, appVersion: e.target.value })} /></label>
          <label>앱 버전코드<input aria-label="앱 버전코드" placeholder="예: 1" inputMode="numeric" pattern="[0-9]+" maxLength={10} value={draft.appVersionCode} onChange={(e) => setDraft({ ...draft, appVersionCode: e.target.value })} /></label>
        </>}
        <label>발생 단계<input value={draft.phase} maxLength={100} onChange={e=>setDraft({...draft,phase:e.target.value})}/></label><label>발생 계약 버전<input value={draft.contractVersion} maxLength={100} onChange={e=>setDraft({...draft,contractVersion:e.target.value})}/></label>
        {selectFilter("status", "상태", ["open", "acknowledged", "resolved", "ignored"], opsErrorLogStatusLabel)}
        <label>시작 시점 (현지 시간)<input aria-label="시작 시점" type="datetime-local" value={draft.from} onChange={(e) => setDraft({ ...draft, from: e.target.value })} /></label>
        <label>종료 시점 (현지 시간)<input aria-label="종료 시점" type="datetime-local" value={draft.to} onChange={(e) => setDraft({ ...draft, to: e.target.value })} /></label>
        <button type="submit" disabled={rangeInvalid}>검색</button>
        <button type="button" onClick={() => { setDraft({ ...emptyLogFilters }); setFilters({ ...emptyLogFilters }); setCursors([""]); setSelected(null); }}>초기화</button>
        <button type="button" disabled={query.isFetching} onClick={() => void refresh()}>새로고침</button>
        {rangeInvalid && <p role="alert">종료 시점은 시작 시점 이후로 선택하세요.</p>}
      </form>
      {query.data?.collectionAvailable === false && <p role="status">프론트 로그 수집 미연결. 서버 로그는 서버 탭에서 조회할 수 있습니다.</p>}
      {error && <p role="alert">{error.reason} · {error.nextStep}</p>}
      {category==='server'?<section className="admin-panel" aria-label="필터 전체 오류 집계"><h2>조건 전체 집계</h2><p>원로그 상태 기준 · 현재 목록 페이지와 다른 전체 범위</p>{summary.error?<p role="alert">전체 집계 조회 실패 · 확인되지 않은 수치를 0으로 표시하지 않습니다.</p>:<dl>{([['사건 수',summary.data?.eventCount],['알려진 발생 횟수 합',summary.data?.occurrenceCount],['발생 횟수 미확인 사건',summary.data?.occurrenceUnknownEventCount],['영향 계정',summary.data?.affectedAccountCount],['계정 미귀속 사건',summary.data?.unattributedEventCount]] as const).map(([label,value])=><div key={label}><dt>{label}</dt><dd>{value===null||value===undefined?'미확인':value.toLocaleString('ko-KR')}</dd></div>)}</dl>}</section>:null}
      <div className="table-wrap"><table><thead><tr>{["발생 시점 (KST)", "에러코드", "에러내용", "발생 위치", "심각도 / 상태", "로그 ID"].map((label) => <th key={label}>{label}</th>)}</tr></thead>
        <tbody>{query.isLoading ? <tr><td colSpan={6}>조회 중</td></tr> : !query.error && !query.data?.items.length ? <tr><td colSpan={6}>데이터 없음</td></tr> : query.data?.items.map((row) => <tr key={row.id}>
          <td>{logTime(row.occurredAt)}</td><td><code>{row.errorCode}</code></td><td>{row.messageSummary}</td>
          <td>{opsErrorLogSourceLabel(row.service)}<small>{row.environment} · {row.source}</small>{category === "server" ? <small>서버 버전: {row.serverVersion ?? "미기록"}</small> : <><small>앱 버전: {row.appVersion ?? "미기록"}</small><small>버전코드: {row.appVersionCode ?? "미기록"}</small></>}</td>
          <td>{opsSeverityLabel(row.severity)}<small>{opsErrorLogStatusLabel(row.status)}</small></td>
          <td><button className="log-id" onClick={() => setSelected(row.id)} aria-label={`로그 상세 ${row.id}`}>{row.id}</button><small>요청: {row.requestId}</small></td>
        </tr>)}</tbody></table></div>
      <nav className="log-paging" aria-label="로그 페이지"><button disabled={cursors.length === 1 || query.isFetching} onClick={() => { setCursors(cursors.slice(0, -1)); setSelected(null); }}>이전</button><span>{cursors.length} 페이지</span><button disabled={!query.data?.nextCursor || query.isFetching} onClick={() => { setCursors([...cursors, query.data!.nextCursor!]); setSelected(null); }}>다음</button></nav>
      {selected && <section className="log-detail" aria-label="로그 추적 상세"><h2>로그 추적 상세</h2><button onClick={() => setSelected(null)}>닫기</button>
        {detail.isLoading && <p>조회 중</p>}
        {detail.error && <p role="alert">{adminRequestFailureDisplay(detail.error, { resourceLabel: "로그 상세", resourceId: selected }).reason}</p>}
        <OpsInvestigationReadPanel originalStatus={detail.data?.item.status??'미확인'} record={investigation.isError?undefined:controller.investigations.get(selected)} loading={investigation.isFetching} error={investigation.error?'조사 원본 조회 실패 · 명시 새로고침으로 확인하세요.':null} onRefresh={()=>void investigationRead.refresh(()=>investigation.refetch({throwOnError:true})).catch(()=>{})}/>
        {detail.data && <dl>{([ ["로그 ID", detail.data.item.id], ["요청 ID", detail.data.item.requestId], ["추적 참조", detail.data.item.traceRef], ["계정 ID", detail.data.item.accountId], ["출격 ID", detail.data.item.runId], ["보상 미리보기 ID", detail.data.item.rewardPreviewId], ["보상 수령 ID", detail.data.item.rewardClaimId], ["오프라인 보상 ID", detail.data.item.offlineRewardClaimId], ["모집 배치 ID", detail.data.item.gachaBatchId], ["마스킹", detail.data.item.masked === true ? "마스킹됨" : "확인 필요"] ]).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || "없음"}</dd></div>)}</dl>}
      </section>}
    </section>
  </main>;
}
