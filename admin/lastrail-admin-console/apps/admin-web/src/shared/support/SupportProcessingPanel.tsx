import {useEffect,useState,useSyncExternalStore} from 'react';
import {useQuery,useQueryClient} from '@tanstack/react-query';
import {Link} from 'react-router-dom';
import {useAdmin} from '../auth/AdminContext';
import {adminApi,AdminApiError} from '../api/client';
import {SupportWriteSession} from './SupportWriteSession';
import {ownSupportSession} from './supportSessionOwner';
import {buildProcessingCommand,processingCommandSchema,grantCommandSchema,supportId,statusLabels,type ProcessingTicket} from './supportProcessing';
import './supportProcessing.css';
type Context={actor:string;profile:'local'|'dev'|'prd';owner:boolean};
const permissionFailure=(error:unknown)=>error instanceof AdminApiError&&[401,403].includes(error.status);
function useWriteSession(context:Context){
 const client=useQueryClient(),environment=context.profile==='prd'?'PRD':'DEV';
 const [session]=useState(()=>ownSupportSession(client,context.actor,context.profile,()=>new SupportWriteSession({verify:async()=>{
    const current=await adminApi.bootstrap();
    if(current.admin.id!==context.actor||current.environment!==context.profile||(current.admin.role==='OWNER')!==context.owner)throw new AdminApiError({status:403,path:'/bootstrap',errorCode:'ADMIN_IDENTITY_CHANGED'});
    if(current.environment==='prd')throw new AdminApiError({status:403,path:'/bootstrap',errorCode:'PRD_READ_ONLY'});
   },write:async(path,body,key)=>{
    const [kind,id]=path.split('/');
    let receipt:unknown;
    if(kind==='grant')receipt=await adminApi.changeSupportGrant(environment,id,grantCommandSchema.parse(body),key);
    else receipt=await adminApi.supportProcess(id,processingCommandSchema.parse(body),key);
    return receipt;
   }})));
 const state=useSyncExternalStore(session.subscribe,session.snapshot,session.snapshot);
 useEffect(()=>{if(state.phase==='saved'||state.phase==='rejected')void client.invalidateQueries({queryKey:['support-operation',context.actor,context.profile]});},[state,client,context.actor,context.profile]);
 useEffect(()=>()=>{if(session.snapshot().phase==='review')session.cancel();},[session]);
 return {session,state};
}
export function SupportProcessingPanel({ticketId}:{ticketId:string}){
 const {bootstrap,isLoading,error}=useAdmin();
 if(isLoading)return <p>처리 권한 확인 중…</p>;
 if(error||!bootstrap)return <p role="alert">운영자 정보를 확인할 수 없습니다.</p>;
 const context={actor:bootstrap.admin.id,profile:bootstrap.environment,owner:bootstrap.admin.role==='OWNER'};
 return <Operations key={`${context.actor}:${context.profile}:${ticketId}`} context={context} ticketId={ticketId}/>;
}
function Operations({context,ticketId}:{context:Context;ticketId:string}){
 const {session,state}=useWriteSession(context);
 const [ticketDirty,setTicketDirty]=useState(false),[grantDirty,setGrantDirty]=useState(false);
 useDraftProtection(ticketDirty||grantDirty);
 return <section className="support-operations"><h2>문의 처리</h2><p>고객 답변과 내부 메모는 각각 저장됩니다. 답변 작성만으로 문의가 종료되지 않습니다.</p>
  {context.profile==='prd'?<p>PRD에서는 조회만 가능합니다.</p>:null}
  <ProcessingForm context={context} ticketId={ticketId} session={session} onDirty={setTicketDirty}/>
  {context.owner?<GrantManager context={context} session={session} onDirty={setGrantDirty}/>:null}
  {state.message?<p role="status">{state.message}</p>:null}
  {state.phase==='review'&&state.attempt?<section className="support-review" aria-label="문의 명령 검토"><h3>저장 전 확인</h3><p>대상: {state.attempt.path} · 원요청 변경 번호 {String((state.attempt.body as {expectedRevision:number}).expectedRevision)}</p>
   <CommandReview body={state.attempt.body}/><button onClick={()=>void session.confirm()}>검토한 내용 저장</button> <button onClick={()=>session.cancel()}>취소</button></section>:null}
  {state.phase==='uncertain'?<button onClick={()=>void session.retry()}>같은 요청 확인</button>:null}
  {state.phase==='saved'?<details><summary>원요청 처리 결과</summary><pre>{JSON.stringify(state.receipt,null,2)}</pre></details>:null}
 </section>;
}
function CommandReview({body}:{body:unknown}){
 const command=body as {status?:ProcessingTicket['status'];publicReply?:string;internalNote?:string;action?:string;reason?:string};
 return <>{command.status?<p>고객 표시 상태: {statusLabels[command.status]}</p>:null}{command.publicReply?<div className="support-public"><h4>고객에게 표시할 답변</h4><p>{command.publicReply}</p></div>:null}{command.internalNote?<div className="support-internal"><h4>운영자 전용 메모</h4><p>{command.internalNote}</p></div>:null}{command.action?<><p>문의 처리 권한: {command.action==='GRANT'?'부여':'회수'}</p><p>사유: {command.reason}</p></>:null}</>;
}
function ProcessingForm({context,ticketId,session,onDirty}:{context:Context;ticketId:string;session:SupportWriteSession;onDirty:(dirty:boolean)=>void}){
 const client=useQueryClient();
 const [accessDenied,setAccessDenied]=useState(false);
 const query=useQuery({queryKey:['support-operation',context.actor,context.profile,'ticket',ticketId],queryFn:()=>adminApi.supportProcessing(ticketId),enabled:!accessDenied&&session.snapshot().phase!=='revoked',retry:false,refetchOnWindowFocus:false});
 const state=useSyncExternalStore(session.subscribe,session.snapshot,session.snapshot);
 const [draft,setDraft]=useState<{revision:number;status:ProcessingTicket['status'];reply:string;note:string}|null>(null),[message,setMessage]=useState('');
 const ticket=query.data?.ticket;
 const current=draft??{revision:ticket?.revision??0,status:ticket?.status??'OPEN',reply:'',note:''};
 const revoked=state.phase==='revoked'||accessDenied||permissionFailure(query.error)||state.deniedPath===`ticket/${ticketId}`;
 const dirty=!!draft&&(!!draft.reply||!!draft.note||draft.status!==ticket?.status);
 useEffect(()=>{onDirty(dirty);return()=>onDirty(false);},[dirty,onDirty]);
 useEffect(()=>{if(revoked){setAccessDenied(true);setDraft(null);setMessage('');client.removeQueries({queryKey:['support-operation',context.actor,context.profile,'ticket',ticketId]});}},[revoked,client,context.actor,context.profile,ticketId]);
 useEffect(()=>{if(state.phase==='saved'&&state.completed?.path===`ticket/${ticketId}`){
  const completed=state.completed,revision=(state.receipt as {revision:number}).revision;
  setDraft(previous=>{if(!previous)return null;const reply=completed.reply?'':previous.reply,note=completed.note?'':previous.note;return reply||note?{...previous,revision,reply,note}:null;});setMessage('');
 }},[state,ticketId]);
 const conflict=!!draft&&!!ticket&&draft.revision!==ticket.revision;
 const enabled=!!ticket&&!query.isError&&!query.isFetching&&!session.locked&&state.phase!=='review'&&context.profile!=='prd';
 function change(field:'status'|'reply'|'note',value:string){setDraft({...current,[field]:value});setMessage('');}
 function prepare(action:'reply'|'note'|'status'){
  if(!enabled||!ticket||conflict)return;
  try{const body=buildProcessingCommand(ticket,action,current.status,current.reply,current.note);session.review(`ticket/${ticketId}`,body);}catch(error){setMessage(error instanceof Error?error.message:'입력을 확인해 주세요.');}
 }
 if(revoked)return <div role="alert"><p>문의 처리 권한이 없습니다. 민감한 처리 내용과 초안을 제거했습니다.</p><button disabled={session.locked} onClick={()=>{session.cancel();setAccessDenied(false);}}>현재 처리 권한 다시 확인</button></div>;
 return <><button disabled={session.locked} onClick={()=>{session.cancel();void query.refetch();}}>처리 권한·최신 문의 조회</button>
  {query.isFetching?<p>처리 정보를 조회 중…</p>:query.isError?<p role="alert">처리 정보 조회 실패. 읽기 권한과 답변 처리 권한은 별도입니다.</p>:ticket?<>
   <p>현재 상태: {statusLabels[ticket.status]} · 변경 번호 {ticket.revision}</p>
   <div className="support-public"><h3>현재 고객 답변</h3><p>{ticket.publicReply??'아직 등록된 답변이 없습니다.'}</p></div>
   {conflict?<p role="alert">문의가 변경되었습니다. 초안을 유지했습니다. 최신 상태를 확인하고 <button onClick={()=>{setDraft(null);session.cancel();}}>초안 버리기</button> 후 다시 작성해 주세요.</p>:null}
   <fieldset disabled={!enabled||conflict}><legend>답변 작성</legend>
    <label>고객 답변 · 문의 작성자에게 표시됩니다<textarea maxLength={2000} value={current.reply} onChange={e=>change('reply',e.target.value)}/></label><small>{current.reply.length}/2000</small>
    <label>처리 상태<select value={current.status} onChange={e=>change('status',e.target.value)}>{Object.entries(statusLabels).map(([value,label])=><option key={value} value={value} disabled={ticket.status==='CLOSED'&&value!=='CLOSED'}>{label}</option>)}</select></label>
    <div className="support-public"><h3>고객 표시 미리보기</h3><p>{statusLabels[current.status]}</p><p>{current.reply.trim()||ticket.publicReply||'아직 등록된 답변이 없습니다.'}</p></div>
    <label>내부 메모 · 고객에게 표시되지 않습니다<textarea maxLength={2000} value={current.note} onChange={e=>change('note',e.target.value)}/></label><small>{current.note.length}/2000</small>
    <p>종료에는 이번 고객 답변이 필요합니다. 검토 중에서 접수로 되돌릴 때는 내부 사유가 필요합니다.</p>
    <button onClick={()=>prepare('reply')}>답변 등록 검토</button> <button onClick={()=>prepare('note')}>내부 메모 저장 검토</button> <button onClick={()=>prepare('status')}>상태 저장 검토</button>
   </fieldset>
   <details><summary>운영 처리 이력</summary>{ticket.history.map(h=><article key={h.revision}><p>{h.createdAt} · 변경 번호 {h.revision} · {statusLabels[h.status]} · {h.principalId}</p>{h.publicReply?<p>고객 답변: {h.publicReply}</p>:null}{h.internalNote?<p>내부 메모: {h.internalNote}</p>:null}</article>)}{ticket.historyTruncated?<p>최근 100건만 표시합니다.</p>:null}</details>
  </>:null}{message?<p role="alert">{message}</p>:null}
 </>;
}
function GrantManager({context,session,onDirty}:{context:Context;session:SupportWriteSession;onDirty:(dirty:boolean)=>void}){
 const client=useQueryClient();
 const environment=context.profile==='prd'?'PRD':'DEV';
 const [input,setInput]=useState(''),[target,setTarget]=useState(''),[reason,setReason]=useState(''),[message,setMessage]=useState('');
 const state=useSyncExternalStore(session.subscribe,session.snapshot,session.snapshot);
 const query=useQuery({queryKey:['support-operation',context.actor,context.profile,'grant',target],queryFn:()=>adminApi.supportGrant(environment,target),enabled:!!target,retry:false,refetchOnWindowFocus:false});
 useEffect(()=>{onDirty(!!reason);return()=>onDirty(false);},[reason,onDirty]);
 useEffect(()=>{if(permissionFailure(query.error)){setReason('');setTarget('');session.revoke();}},[query.error,session]);
 useEffect(()=>{if(state.phase==='saved'&&state.completed?.path===`grant/${target}`)setReason('');if(state.phase==='revoked'||state.deniedPath===`grant/${target}`){setReason('');setTarget('');client.removeQueries({queryKey:['support-operation',context.actor,context.profile,'grant',target]});}},[state,client,context.actor,context.profile,target]);
 const grant=query.data?.state;
 function prepare(action:'GRANT'|'REVOKE'){
  if(!grant||query.isError||query.isFetching||session.locked||state.phase==='review'||context.profile==='prd')return;
  try{session.review(`grant/${target}`,grantCommandSchema.parse({action,permission:'SUPPORT_TICKET_PROCESS',expectedRevision:grant.revision,reason}));setMessage('');}catch{setMessage('변경 사유를 1~500자로 입력해 주세요.');}
 }
 return <section className="support-grants"><h2>문의 처리 권한 관리 · OWNER</h2><p>답변 처리 권한과 별도입니다. 자신과 마지막 처리자의 권한도 회수할 수 있습니다.</p>
  <form onSubmit={e=>{e.preventDefault();if(session.locked||state.phase==='review')return;const id=supportId.safeParse(input.trim());if(!id.success){setMessage('정확한 운영자 ID를 입력해 주세요.');return;}session.cancel();if(target===id.data)void query.refetch();else setTarget(id.data);setReason('');setMessage('');}}>
   <label>대상 운영자 ID<input value={input} disabled={session.locked||state.phase==='review'} onChange={e=>setInput(e.target.value)}/></label><button disabled={session.locked||state.phase==='review'}>권한 조회</button>
  </form>
  {query.isFetching?<p>권한 조회 중…</p>:query.isError?<p role="alert">권한 조회 실패. 대상 없음·접근 거절·서버 오류는 권한 없음으로 대체하지 않습니다.</p>:grant&&state.phase!=='revoked'?<>
   <p>{grant.displayName??grant.principalId} · {grant.role} / {grant.status}</p><p>현재 처리 권한: {grant.granted?'부여됨':'없음'} · 변경 번호 {grant.revision}</p>
   {grant.lastChange?<p>최근 변경: {new Date(grant.lastChange.at).toLocaleString('ko-KR',{timeZone:'Asia/Seoul'})} · {grant.lastChange.outcome} · {grant.lastChange.reason??'사유 미기록'}</p>:<p>최근 변경 기록 없음</p>}
   <fieldset disabled={session.locked||state.phase==='review'||context.profile==='prd'}><label>권한 변경 사유<textarea maxLength={500} value={reason} onChange={e=>setReason(e.target.value)}/></label><button disabled={grant.status!=='ACTIVE'||!['OWNER','OPERATOR'].includes(grant.role)} onClick={()=>prepare('GRANT')}>부여 검토</button> <button onClick={()=>prepare('REVOKE')}>회수 검토</button></fieldset>
   <Link to={`/audit?resourceId=${encodeURIComponent(target)}`}>권한 변경 감사 이력</Link>
  </>:null}{message?<p role="alert">{message}</p>:null}
 </section>;
}
function useDraftProtection(dirty:boolean){
 useEffect(()=>{
  if(!dirty)return;
  const unload=(event:BeforeUnloadEvent)=>{event.preventDefault();event.returnValue='';};
  const navigate=(event:MouseEvent)=>{
   if(event.defaultPrevented||event.button!==0||event.ctrlKey||event.metaKey||event.shiftKey||event.altKey)return;
   const anchor=(event.target as Element).closest('a');
   if(anchor&&anchor.target!=='_blank'&&anchor.href!==location.href&&!window.confirm('저장하지 않은 작성 내용을 버리고 이동할까요?')){event.preventDefault();event.stopPropagation();}
  };
  window.addEventListener('beforeunload',unload);document.addEventListener('click',navigate,true);
  return()=>{window.removeEventListener('beforeunload',unload);document.removeEventListener('click',navigate,true);};
 },[dirty]);
}
