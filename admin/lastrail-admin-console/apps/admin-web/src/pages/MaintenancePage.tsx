import {useState} from "react";
import {useQuery} from "@tanstack/react-query";
import {Link} from "react-router-dom";
import {useAdmin} from "../shared/auth/AdminContext";
import {adminApi} from "../shared/api/client";
import {MaintenanceDraftEditor} from "../shared/operations/MaintenanceDraftEditor";
import {maintenanceBlocks,maintenanceModes,maintenanceScopes,parseMaintenanceTime,type MaintenanceEnvironment} from "../shared/operations/maintenanceContract";
const date=(value:number|null)=>value===null?"미지정":new Date(value).toLocaleString("ko-KR",{timeZone:"Asia/Seoul"});
export function MaintenancePage(){
 const {bootstrap,isLoading,error}=useAdmin();
 return <main className="content"><header><h1>점검 계획</h1><p>저장된 계획과 특정 시각의 예상 상태를 조회합니다.</p></header>
 <section className="admin-panel" role="status"><h2>실제 점검 적용 미연결</h2><p>계획과 미리보기는 서비스 상태를 변경하지 않습니다. 실제 점검 중인지 여부는 이 화면에서 확인할 수 없습니다.</p></section>
 {isLoading?<p>운영자 정보를 확인 중입니다.</p>:error||!bootstrap?<p role="alert">운영자 정보를 확인할 수 없습니다.</p>:<PlanList key={`${bootstrap.admin.id}:${bootstrap.environment}`} operator={bootstrap.admin.id} environment={bootstrap.environment==="prd"?"PRD":"DEV"} auditAllowed={bootstrap.admin.permissions.includes("AUDIT_READ")}/>}
 <p><Link to="/notices">공지 관리</Link> · 점검 공지 게시와 실제 서비스 점검은 별도입니다.</p></main>;
}
function PlanList({operator,environment,auditAllowed}:{operator:string;environment:MaintenanceEnvironment;auditAllowed:boolean}){
 const [cursors,setCursors]=useState([""]),[selected,setSelected]=useState<string|null>(null);
 const [editing,setEditing]=useState(false),[editorVersion,setEditorVersion]=useState(0);
 const after=cursors.at(-1)!;
 const query=useQuery({queryKey:["maintenance-plans",operator,environment,after],queryFn:()=>adminApi.maintenancePlans(environment,after),retry:false,refetchOnWindowFocus:false});
 return <><section className="admin-panel"><h2>계획 목록 · {environment}</h2><button disabled={query.isFetching} onClick={()=>void query.refetch()}>목록 새로고침</button> <button disabled={environment!=="DEV"||!query.data?.permissions.canSaveDraft||query.isError||query.isFetching} onClick={()=>{setSelected(null);setEditing(true);setEditorVersion(v=>v+1);}}>새 점검 계획</button>
 {query.isFetching?<p role="status">계획 조회 중…</p>:query.isError?<p role="alert">점검 계획을 불러오지 못했습니다. 다시 조회해 주세요.</p>:query.data?.items.length===0?<p>등록된 점검 계획이 없습니다.</p>:<table><thead><tr><th>제목</th><th>대상</th><th>시작 예정 · 한국 시간</th><th>상태</th></tr></thead><tbody>{query.data?.items.map(p=><tr key={p.maintenanceId}><td><button onClick={()=>setSelected(p.maintenanceId)}>{p.document.title}</button></td><td>{maintenanceScopes[p.document.serviceScope]}</td><td>{date(p.document.startsAt)}</td><td>초안 · 미적용</td></tr>)}</tbody></table>}
 <nav aria-label="점검 계획 페이지"><button disabled={cursors.length===1||query.isFetching} onClick={()=>setCursors(v=>v.slice(0,-1))}>이전</button> <span>{cursors.length} 페이지</span> <button disabled={!query.data?.nextAfter||query.isFetching||query.isError} onClick={()=>{if(query.data?.nextAfter)setCursors(v=>[...v,query.data!.nextAfter!]);}}>다음</button></nav>
 </section>{selected?<><PlanDetail key={selected} operator={operator} environment={environment} id={selected}/><button disabled={environment!=="DEV"||!query.data?.permissions.canSaveDraft||query.isError} onClick={()=>setEditing(true)}>선택한 계획 편집</button></>:null}{editing?<MaintenanceDraftEditor key={`${selected??"new"}:${editorVersion}`} operator={operator} environment={environment} selected={selected} auditAllowed={auditAllowed} onSaved={id=>setSelected(id)}/>:null}</>;
}
export function PlanDetail({operator,environment,id}:{operator:string;environment:MaintenanceEnvironment;id:string}){
 const detail=useQuery({queryKey:["maintenance-plan",operator,environment,id],queryFn:()=>adminApi.maintenancePlan(environment,id),retry:false,refetchOnWindowFocus:false});
 const [input,setInput]=useState(""),[selection,setSelection]=useState<{at:number;revision:number}|null>(null),[error,setError]=useState("");
 const [requestVersion,setRequestVersion]=useState(0);
 const preview=useQuery({queryKey:["maintenance-preview",operator,environment,id,selection?.revision,selection?.at,requestVersion],queryFn:()=>adminApi.maintenancePreview(environment,id,selection!.revision,selection!.at),enabled:!!selection&&!detail.isError&&!detail.isFetching,retry:false,refetchOnWindowFocus:false});
 function inspect(){const at=parseMaintenanceTime(input);setSelection(null);if(at===null){setError("한국 시간으로 날짜와 시각을 입력해 주세요.");return;}if(!detail.data||detail.isError||detail.isFetching)return;setError("");setRequestVersion(v=>v+1);setSelection({at,revision:detail.data.plan.draftRevision});}
 const plan=detail.data?.plan,p=plan?.document;
 return <section className="admin-panel"><h2>계획 상세</h2><button disabled={detail.isFetching} onClick={()=>{setSelection(null);void detail.refetch();}}>최신 계획 조회</button>
 {detail.isFetching?<p role="status">계획 조회 중…</p>:detail.isError?<p role="alert">계획 조회 실패. 미리보기도 사용할 수 없습니다.</p>:p&&plan?<><h3>{p.title}</h3><p style={{whiteSpace:"pre-wrap"}}>{p.summary}</p><dl><dt>계획 ID / 변경 번호</dt><dd>{id} / {plan.draftRevision}</dd><dt>애플리케이션 / 대상</dt><dd>{p.applicationId} / {maintenanceScopes[p.serviceScope]}</dd><dt>출격 정리 시작 · 한국 시간</dt><dd>{date(p.drainStartsAt)}</dd><dt>점검 시작 / 예상 종료 · 한국 시간</dt><dd>{date(p.startsAt)} / {date(p.estimatedEndAt)}</dd><dt>진행 중 출격 처리 계획</dt><dd>{p.existingRunPolicy==="DRAIN_THEN_PRESERVE"?"출격 정리 후 보존":p.existingRunPolicy==="EMERGENCY_PRESERVE"?"긴급 보존":"해당 없음"}</dd><dt>웹 처리 계획</dt><dd>{p.webMode?maintenanceModes[p.webMode]:"해당 없음"}</dd></dl>
 <p>예상 종료 시각이 지나도 자동으로 점검이 종료되는 계획은 아닙니다.</p>
 <details><summary>적용되지 않는 이유</summary><ul>{detail.data!.application.blockReasons.map(reason=><li key={reason}>{maintenanceBlocks[reason]??`미확인 적용 조건 (${reason})`}</li>)}</ul></details>
 <label>예상 상태를 확인할 시각 · 한국 시간 <input type="datetime-local" value={input} onChange={e=>{setInput(e.target.value);setSelection(null);setError("");}}/></label> <button onClick={inspect}>예상 상태 조회</button>
 {error?<p role="alert">{error}</p>:null}
 {selection?(preview.isFetching?<p role="status">예상 상태 조회 중…</p>:preview.isError?<p role="alert">예상 상태를 확인하지 못했습니다. 계획 변경 여부를 확인하고 다시 조회해 주세요.</p>:preview.data?<section aria-label="가상 점검 예상 상태"><h3>가상 예상 상태 · 미적용</h3><p>{date(preview.data.preview.at)} (한국 시간) / 계획 변경 번호 {preview.data.preview.draftRevision}</p><p>게임: {maintenanceModes[preview.data.preview.effectiveGameMode]} · 웹: {maintenanceModes[preview.data.preview.effectiveWebMode]}</p><p>이 결과는 저장된 계획의 예상값이며 현재 서비스 상태가 아닙니다.</p></section>:null):null}
 </>:null}</section>;
}
