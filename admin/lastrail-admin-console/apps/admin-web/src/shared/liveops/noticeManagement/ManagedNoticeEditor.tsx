import {useEffect,useMemo,useSyncExternalStore} from 'react';
import {Link} from 'react-router-dom';
import {NoticeEditorFields,NoticePreview} from '../../operations/NoticeEditor';
import {noticeLabels} from '../../operations/noticeContract';
import {MountedNoticeController} from './MountedNoticeController';
import type {NoticeManagement} from './NoticeManagement';
const time=(v:number|null)=>v===null?'종료 없음':v===0?'즉시':new Date(v).toLocaleString('ko-KR',{timeZone:'Asia/Seoul'});
/** Uses the original editor template with one management state owner. */
export function ManagedNoticeEditor({model,selected,auditAllowed}:{model:NoticeManagement;selected:string|null;auditAllowed:boolean}){
 const c=useMemo(()=>new MountedNoticeController(model,selected),[model,selected]);
 const view=useSyncExternalStore(c.subscribe,c.snapshot,c.snapshot),state=model.snapshot();
 useEffect(()=>{c.connect();return()=>c.disconnect();},[c]);
 if(!view.active)return <p role="alert">운영자 권한을 다시 확인해 주세요.</p>;
 const d=view.document,current=view.current,published=current?.publication;
 return <section className="admin-panel notice-editor"><div className="notice-toolbar"><h2>{view.exists?'공지 편집':'새 공지'}</h2>{view.exists&&<button disabled={view.busy} onClick={()=>void c.reload()}>최신 상태 조회</button>}</div>
 {view.exists&&!current&&<p role="status">공지 원문을 확인 중입니다. 원문 확인 전에는 변경할 수 없습니다.</p>}
 {current&&<section className="notice-publication"><strong>연결된 게시본</strong>{published?<><p>{published.document.title} · 변경 번호 {published.revision} · {noticeLabels[published.status]}</p><p>{published.document.surfaces} · {published.document.visibility} · {time(published.document.startsAt)} ~ {time(published.document.endsAt)} (한국 시간)</p><details><summary>게시 원문 보기</summary><NoticePreview document={published.document}/></details></>:<p>현재 연결된 게시본이 없습니다.</p>}<small>편집본: {noticeLabels[current.notice.status]} / 변경 번호 {current.notice.revision} · 실제 노출은 원서버 정책을 따릅니다.</small></section>}
 <NoticeEditorFields document={d} id={view.id} exists={view.exists} onIdChange={id=>c.changeId(id)} onChange={doc=>c.change(doc)} canEdit={view.canEdit} baselinePresentationRevision={current?.notice.document.presentationRevision??1}/>
 <details><summary>작성 내용 미리보기</summary><NoticePreview document={d}/></details>
 <label>변경 사유 (필수)<textarea disabled={!view.active||view.busy||model.environment!=='DEV'} maxLength={500} value={view.reason} onChange={e=>c.changeReason(e.target.value)}/></label>
 <div className="notice-actions"><button disabled={!view.canEdit||view.conflict} onClick={()=>c.prepare('SAVE_DRAFT')}>초안 저장 검토</button><button disabled={!view.canReview||view.conflict} onClick={()=>c.prepare('REVIEW')}>현재 revision 검토</button><button disabled={!view.canExecute||current?.notice.status!=='DRAFT'} onClick={()=>c.prepare('PUBLISH')}>즉시 게시 검토</button><button disabled={!view.canExecute||current?.notice.status!=='DRAFT'} onClick={()=>c.prepare('SCHEDULE')}>예약 게시 검토</button><button disabled={!view.canExecute||!published} onClick={()=>c.prepare('WITHDRAW')}>게시 철회 검토</button></div>
 {current?.review?<p>서버 검토 {current.review.reviewId} · 변경 번호 {current.review.reviewedRevision} · 게시 실행 아님</p>:<p>저장된 현재 변경 번호의 서버 검토가 필요합니다. 새 초안 저장 후 다시 검토하세요.</p>}
 {view.dirty&&<small>아직 저장하지 않은 내용이 있습니다. 다른 공지를 선택하면 편집 내용은 사라집니다.</small>}
 {view.conflict&&<p role="alert">다른 변경이 저장되었습니다. 입력을 보관한 뒤 최신 원문을 확인하세요. <button disabled={view.busy} onClick={()=>c.resetDraft()}>편집 내용 버리고 최신본 사용</button></p>}
 {view.prepared&&<section className="notice-review" aria-label="공지 변경 검토"><h3>{view.prepared.action==='REVIEW'?'현재 revision 서버 검토':noticeLabels[view.prepared.action]} 확인</h3><p>{model.environment} · {view.prepared.id} · 기준 변경 번호 {current?.notice.revision??'신규'}</p><p>사유: {view.reason}</p><NoticePreview document={d}/><button disabled={view.busy} onClick={()=>void c.confirm()}>{view.prepared.action==='REVIEW'?'서버 검토 확정':noticeLabels[view.prepared.action]+' 실행'}</button> <button onClick={()=>c.cancel()}>취소</button></section>}
 {view.message&&<p role="status">{view.message}</p>}{state.write==='pending'&&<p role="status">요청 처리 중…</p>}{state.write==='outcomeUnknown'&&<p role="alert">이전 요청 결과가 미확인입니다. 입력을 고정하고 원요청으로 복구합니다. <button onClick={()=>void c.recover()}>같은 요청 확인</button></p>}
 {current&&<details onToggle={e=>{if(e.currentTarget.open)void model.history(current.notice.noticeId);}}><summary>원본 변경 이력</summary>{state.history?<><ul>{state.history.items.map(row=><li key={row.revision}>변경 번호 {row.revision} · {noticeLabels[row.status]} · {row.principalId??'작성자 미공급'}</li>)}</ul>{state.history.nextBefore!==null&&<button onClick={()=>void model.history(current.notice.noticeId,state.history!.nextBefore!)}>이전 이력</button>}</>:<p>이력 확인 중…</p>}</details>}
 {auditAllowed&&view.exists&&<Link to={`/audit?resourceId=${encodeURIComponent(view.id)}&result=SUCCESS`}>이 공지의 변경 이력</Link>}
 </section>;
}
