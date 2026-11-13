import {useSyncExternalStore} from 'react';
import {type LiveOpsKind,type LiveOpsSnapshot,LiveOpsWorkspace} from './LiveOpsWorkspace';
const status:Record<string,string>={DRAFT:'초안',PUBLISHED:'게시 중',SCHEDULED:'예약',WITHDRAWN:'철회',EXPIRED:'게시 종료',DRAFT_NOT_APPLIED:'점검 계획 초안 · 미적용'};
export type LiveOpsActions={open:(kind:LiveOpsKind)=>void;select:(id:string)=>void;close:()=>void;refresh:()=>void;next:(after:string)=>void;review:(kind:LiveOpsKind,id:string)=>void;history:(id:string)=>void};
/** Disconnected review candidate: host owns authenticated Model lifetime and existing edit/history controllers. */
export function LiveOpsWorkspacePanel({model,review,history,auditAllowed}:{model:LiveOpsWorkspace;review:LiveOpsActions['review'];history:LiveOpsActions['history'];auditAllowed:boolean}){
 const state=useSyncExternalStore(model.subscribe,model.snapshot,model.snapshot);
 return <LiveOpsWorkspaceView state={state} environment={model.environment} auditAllowed={auditAllowed} actions={{open:kind=>{void model.open(kind);},select:id=>{void model.select(id);},close:()=>model.closeDetail(),refresh:()=>{void model.open(state.kind,true,state.after);},next:after=>{void model.open(state.kind,false,after);},review,history}}/>;
}
export function LiveOpsWorkspaceView({state,environment,actions,auditAllowed}:{state:LiveOpsSnapshot;environment:'DEV'|'PRD';actions:LiveOpsActions;auditAllowed:boolean}){
 const detail=state.detail.data;
 return <section className="admin-panel" aria-label="운영 관리 검토안"><header><h1>운영 관리</h1><p>{environment} · 기존 공지와 점검 계획을 선택해 검토와 이력으로 연결합니다.</p></header>
 <p role="note">검토용 구성안입니다. 실제 메뉴 연결과 시각 승인은 별도입니다.</p>
 {state.phase!=='active'?<p role="alert">운영자 권한을 다시 확인해 주세요.</p>:<>
 {state.confirmed?.kind===state.kind?<p role="status">확인된 원요청: {state.confirmed.id} · 변경 번호 {state.confirmed.revision} · {status[state.confirmed.status]??state.confirmed.status}. 문서에 없는 값은 다시 선택해 확인해 주세요.</p>:null}<nav aria-label="운영 영역"><button aria-pressed={state.kind==='NOTICE'} onClick={()=>actions.open('NOTICE')}>공지</button> <button aria-pressed={state.kind==='MAINTENANCE_PLAN'} onClick={()=>actions.open('MAINTENANCE_PLAN')}>점검 계획</button></nav>
 <button disabled={state.list.phase==='loading'} onClick={actions.refresh}>목록 새로고침</button>
 {state.list.phase==='idle'?<p>조회할 운영 영역을 선택해 주세요.</p>:state.list.phase==='loading'?<p role="status">목록 조회 중…</p>:state.list.phase==='error'?<p role="alert">목록을 확인할 수 없습니다. 다시 조회해 주세요.</p>:<>
 {state.list.data?.items.length===0?<p>등록된 항목이 없습니다.</p>:<table><thead><tr><th>제목</th><th>상태</th><th>변경 번호</th></tr></thead><tbody>{state.list.data?.items.map(row=><tr key={row.id}><td><button onClick={()=>actions.select(row.id)}>{row.title}</button></td><td>{status[row.status]??row.status}</td><td>{row.revision}</td></tr>)}</tbody></table>}
 {state.list.data?.nextAfter?<button onClick={()=>actions.next(state.list.data!.nextAfter!)}>다음 목록</button>:null}</>}
 {state.selected?<section aria-label="선택한 운영 항목"><button onClick={actions.close}>상세 닫기</button>{state.detail.phase==='loading'?<p role="status">상세 조회 중…</p>:state.detail.phase==='error'?<p role="alert">상세를 확인할 수 없습니다.</p>:state.detail.phase==='idle'?<p>관련 내용이 변경되었습니다. 항목을 다시 선택해 확인해 주세요.</p>:detail?<>
 <h2>{detail.title}</h2><p>{status[detail.status]??detail.status} · 변경 번호 {detail.revision}</p><p>{detail.document.summary}</p>
 {detail.publication!==null?<p>실제 게시본: {detail.publication}</p>:<p>이 계획은 초안이며 서비스에 적용되지 않았습니다.</p>}
 <button onClick={()=>actions.review(detail.kind,detail.id)}>기존 편집·변경 검토 열기</button> {auditAllowed?<button onClick={()=>actions.history(detail.id)}>변경 이력 열기</button>:null}
 </>:null}</section>:null}
 <aside><h2>연결 준비</h2><p>기간 한정 이벤트·회차 보상은 검증된 서버 계약 공급 후 연결합니다.</p><p>제보 감사·전원 보상·피해복구는 별도 권리로 관리합니다. 미공급 지급을 완료로 표시하지 않습니다.</p></aside>
 </>}
 </section>;
}

