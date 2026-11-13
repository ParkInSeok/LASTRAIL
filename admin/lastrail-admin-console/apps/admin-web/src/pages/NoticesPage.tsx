import {useEffect,useMemo,useState,useSyncExternalStore} from 'react';
import {Link} from 'react-router-dom';
import {useAdmin} from '../shared/auth/AdminContext';
import type {AdminSessionResources} from '../shared/auth/AdminSessionResources';
import {NoticeManagement} from '../shared/liveops/noticeManagement/NoticeManagement';
import {noticeManagementPort} from '../shared/liveops/noticeManagement/noticeManagementPort';
import {ManagedNoticeEditor} from '../shared/liveops/noticeManagement/ManagedNoticeEditor';
import {noticeLabels} from '../shared/operations/noticeContract';
import '../shared/operations/notices.css';
export function NoticesPage(){
 const {bootstrap,isLoading,error,session,sessionActive}=useAdmin();
 return <main className="content"><header><h1>공지 관리</h1><p>초안을 저장한 뒤 게시 시점과 대상을 검토합니다. 초안 수정은 게시본에 바로 반영되지 않습니다.</p></header>
 {isLoading?<p role="status">운영자 정보를 확인 중입니다.</p>:error||!bootstrap||!session||sessionActive===false?<p role="alert">운영자 정보를 확인할 수 없습니다. 권한을 다시 확인해 주세요.</p>:<NoticeContent key={session.generation} session={session} operator={bootstrap.admin.id} environment={bootstrap.environment==='prd'?'PRD':'DEV'} auditAllowed={bootstrap.admin.permissions.includes('AUDIT_READ')}/>}
 </main>;
}
function NoticeContent({session,operator,environment,auditAllowed}:{session:AdminSessionResources;operator:string;environment:'DEV'|'PRD';auditAllowed:boolean}){
 const model=useMemo(()=>session.acquire('notice-management',()=>new NoticeManagement(operator,environment,noticeManagementPort())),[session,operator,environment]);
 const state=useSyncExternalStore(model.subscribe,model.snapshot,model.snapshot);
 const [after,setAfter]=useState(''),[selected,setSelected]=useState<string|null|undefined>(()=>model.snapshot().selected??undefined),[newVersion,setNewVersion]=useState(0);
 useEffect(()=>{if(model.snapshot().phase==='active')void Promise.all([model.capabilities(),model.list()]);},[model]);
 useEffect(()=>{if(state.phase==='revoked')session.revoke();},[state.phase,session]);
 if(state.phase!=='active')return <p role="alert">운영자 권한을 다시 확인해 주세요.</p>;
 const caps=state.capabilities?.domains.NOTICE,busy=state.write==='pending'||state.write==='outcomeUnknown';
 const page=(cursor:string)=>{setAfter(cursor);void model.list(cursor);};
 return <>{environment==='PRD'&&<p role="status">PRD에서는 조회만 가능합니다.</p>}{state.readError&&<p role="alert">{state.readError}. 표시 중인 값이 있으면 이전에 확인한 값입니다.</p>}
 <section className="admin-panel"><div className="notice-toolbar"><h2>공지 목록</h2><div><button disabled={busy} onClick={()=>void Promise.all([model.capabilities(true),model.list(after,true)])}>새로고침</button> <button disabled={!caps?.canEdit||!state.list||busy||environment!=='DEV'} onClick={()=>{model.close();setSelected(null);setNewVersion(v=>v+1);}}>새 공지</button></div></div>
 {!state.list?(state.listPhase==='failed'?<p>목록을 불러오지 못했습니다. 새로고침으로 다시 확인해 주세요.</p>:<p role="status">목록 조회 중…</p>):state.list.items.length===0?<p>등록된 공지가 없습니다.</p>:<table><thead><tr><th>제목</th><th>편집 상태</th><th>변경 번호</th><th>대상</th></tr></thead><tbody>{state.list.items.map(item=><tr key={item.noticeId}><td><button disabled={busy} onClick={()=>{setSelected(item.noticeId);void model.open(item.noticeId);}}>{item.document.title}</button><small className="notice-id">{item.noticeId}</small></td><td>{noticeLabels[item.status]}</td><td>{item.revision}</td><td>{item.document.surfaces} · {item.document.visibility}</td></tr>)}</tbody></table>}
 <div className="notice-toolbar"><small>목록 상태는 편집본 기준입니다. 실제 게시본은 공지를 선택해 확인하세요.</small><div><button disabled={!after||busy} onClick={()=>page('')}>처음</button> <button disabled={!state.list?.nextAfter||busy} onClick={()=>page(state.list!.nextAfter!)}>다음</button></div></div></section>
 {selected!==undefined?<><button disabled={busy} onClick={()=>{model.close();setSelected(undefined);}}>편집 닫기</button><ManagedNoticeEditor key={`${selected??'new'}:${newVersion}`} model={model} selected={selected} auditAllowed={auditAllowed}/></>:<p>목록에서 공지를 선택하거나 새 공지를 작성해 주세요.</p>}
 <p><Link to="/game">게임 설정</Link> · 서비스 점검은 공지 게시와 별도로 관리합니다.</p></>;
}
