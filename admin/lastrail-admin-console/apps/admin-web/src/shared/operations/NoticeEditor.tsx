import {Link} from "react-router-dom";
import {useState} from "react";
import {useNoticeController} from "./useNoticeController";
import {noticeLabels,publicationStatus,type NoticeDocument,type NoticeList} from "./noticeContract";
const dateText=(value:number|null)=>value===null?"종료 없음":value===0?"즉시":new Date(value).toLocaleString("ko-KR",{timeZone:"Asia/Seoul"});
const inputTime=(value:number|null)=>!value?"":new Date(value+9*3600000).toISOString().slice(0,16);
const parseTime=(value:string)=>value?Date.parse(`${value}:00+09:00`):null;
export function NoticeEditor(props:{operator:string;environment:"DEV"|"PRD";selected:string|null;list:NoticeList|undefined;listHealthy:boolean;auditAllowed:boolean}){
 const c=useNoticeController(props.operator,props.environment,props.selected,props.list,props.listHealthy);
 const d=c.document,detail=c.detail.data;
 const edit=c.write&&!!c.permissions?.canEdit;
 const published=detail?.publication;
 return <section className="admin-panel notice-editor">
 <div className="notice-toolbar"><h2>{c.exists?"공지 편집":"새 공지"}</h2>{c.exists?<button disabled={c.busy||c.detail.isFetching} onClick={()=>void c.reload()}>최신 상태 조회</button>:null}</div>
 {c.detail.isFetching?<p role="status">최신 공지를 조회 중입니다.</p>:c.detail.isError?<p role="alert">공지 조회 실패. 상태를 확인할 때까지 변경할 수 없습니다.</p>:null}
 {detail?<section className="notice-publication"><strong>실제 게시본 · {publicationStatus(detail.publication,detail.serverTime)}</strong>{published?<><p>{published.document.title} · 변경 번호 {published.revision}</p><p>{published.document.surfaces} · {published.document.visibility} · {dateText(published.document.startsAt)} ~ {dateText(published.document.endsAt)} (한국 시간)</p><details><summary>게시 원문 보기</summary><NoticePreview document={published.document}/></details></>:<p>현재 연결된 게시본이 없습니다.</p>}<small>서버 조회 시각: {dateText(detail.serverTime)} · 편집본: {noticeLabels[detail.notice.effectiveStatus]} / 변경 번호 {detail.notice.revision}</small></section>:null}
 <NoticeEditorFields document={d} id={c.id} exists={c.exists} onIdChange={c.changeId} onChange={c.change} canEdit={edit} baselinePresentationRevision={detail?.notice.document.presentationRevision??1}/>
 <details><summary>작성 내용 미리보기</summary><NoticePreview document={d}/></details>
 <label>변경 사유 (필수)<textarea disabled={!c.write} maxLength={500} value={c.reason} onChange={e=>c.changeReason(e.target.value)}/></label>
 <div className="notice-actions"><button disabled={!edit} onClick={()=>c.prepare("SAVE_DRAFT")}>초안 저장 검토</button><button disabled={!c.write||!c.permissions?.canPublish||detail?.notice.status!=="DRAFT"||c.dirty} onClick={()=>c.prepare("PUBLISH")}>즉시 게시 검토</button><button disabled={!c.write||!c.permissions?.canPublish||detail?.notice.status!=="DRAFT"||c.dirty} onClick={()=>c.prepare("SCHEDULE")}>예약 게시 검토</button><button disabled={!c.write||!c.permissions?.canWithdraw||!published||c.dirty} onClick={()=>c.prepare("WITHDRAW")}>게시 철회 검토</button></div>
 {c.dirty?<small>아직 저장하지 않은 내용이 있습니다. 다른 공지를 선택하면 편집 내용은 사라집니다.</small>:null}
 {c.hasConflict?<p role="alert">다른 변경이 저장되어 이 편집본을 저장할 수 없습니다. 필요한 내용을 보관한 뒤 최신 내용으로 다시 편집해 주세요. <button disabled={!c.write} onClick={c.resetDraft}>편집 내용 버리고 최신본 사용</button></p>:null}
 {c.review?<section className="notice-review" aria-label="공지 변경 검토"><h3>{noticeLabels[c.review.body.command]} 확인</h3><p>{props.environment} · {c.review.id} · 기준 변경 번호 {c.review.body.expectedRevision??"신규"}</p><p>사유: {c.review.body.reason}</p><p>노출: {d.surfaces} · {d.visibility} / 시작: {dateText(d.startsAt)} / 종료: {dateText(d.endsAt)} (한국 시간)</p>{c.review.body.command==="WITHDRAW"?<p>게시본을 내립니다. 저장된 초안은 유지됩니다.</p>:<NoticePreview document={c.review.body.command==="SAVE_DRAFT"?c.review.body.document:d}/>}<button disabled={!c.write} onClick={c.confirm}>{noticeLabels[c.review.body.command]} 실행</button> <button onClick={c.cancel}>취소</button></section>:null}
 {c.message?<p role="status">{c.message}</p>:null}{c.command.kind==="pending"?<p role="status">요청 처리 중…</p>:c.command.kind==="uncertain"?<p role="alert">공지 {c.command.attempt.id}의 이전 요청 결과가 미확인입니다. <button onClick={()=>void c.recover()}>같은 요청 확인</button></p>:null}
 {props.auditAllowed&&c.exists?<Link to={`/audit?action=NOTICE_COMMAND&resourceId=${encodeURIComponent(c.id)}&result=SUCCESS`}>이 공지의 변경 이력</Link>:null}
 </section>;
}
export function NoticePreview({document:d}:{document:NoticeDocument}){return <article className="notice-preview"><h3>{d.title||"제목 없음"}</h3><p>{d.summary}</p>{Object.entries(d.localizedBody).map(([locale,body])=><section key={locale}><strong>{locale}</strong><p style={{whiteSpace:"pre-wrap",overflowWrap:"anywhere"}}>{body}</p></section>)}</article>;}

export type NoticeEditorFieldsProps={document:NoticeDocument;id:string;exists:boolean;onIdChange:(id:string)=>void;onChange:(document:NoticeDocument)=>void;canEdit:boolean;baselinePresentationRevision:number};
/** Shared original input template; no API, domain command or parent lookup. */
export function NoticeEditorFields({document:d,id,exists,onIdChange,onChange,canEdit:edit,baselinePresentationRevision}:NoticeEditorFieldsProps){
 const [locale,setLocale]=useState("");
 function field<K extends keyof NoticeDocument>(key:K,value:NoticeDocument[K]){onChange({...d,[key]:value});}
 return ( <fieldset disabled={!edit}><legend>편집할 내용</legend>
 <label>공지 ID<input value={id} disabled={exists} onChange={e=>onIdChange(e.target.value)}/></label>
 <div className="notice-grid"><label>유형<select value={d.type} onChange={e=>field("type",e.target.value as NoticeDocument["type"])}>{Object.entries({GENERAL:"일반",UPDATE:"업데이트",MAINTENANCE:"점검 안내",EVENT:"이벤트",KNOWN_ISSUE:"알려진 문제"}).map(([v,l])=><option key={v} value={v}>{l}</option>)}</select></label><label>노출 위치<select value={d.surfaces} onChange={e=>field("surfaces",e.target.value as NoticeDocument["surfaces"])}><option value="BOTH">앱 · 웹</option><option value="APP">앱</option><option value="WEB">웹</option></select></label><label>공개 범위<select value={d.visibility} onChange={e=>field("visibility",e.target.value as NoticeDocument["visibility"])}><option value="PUBLIC">공개</option><option value="AUTHENTICATED">인증 사용자</option><option value="INTERNAL">내부</option></select></label></div>
 <label>제목 (200자)<input maxLength={200} value={d.title} onChange={e=>field("title",e.target.value)}/></label>
 <label>요약 (500자)<textarea maxLength={500} value={d.summary} onChange={e=>field("summary",e.target.value)}/></label>
 {Object.entries(d.localizedBody).map(([key,body])=><div key={key}><label>본문 · {key} (6,000자)<textarea rows={8} maxLength={6000} value={body} onChange={e=>field("localizedBody",{...d.localizedBody,[key]:e.target.value})}/></label><button disabled={Object.keys(d.localizedBody).length<=1} onClick={()=>{const next={...d.localizedBody};delete next[key];field("localizedBody",next);}}>{key} 삭제</button></div>)}
 <div className="notice-toolbar"><label>추가할 언어 코드<input placeholder="en 또는 ko-KR" value={locale} onChange={e=>setLocale(e.target.value)}/></label><button disabled={Object.keys(d.localizedBody).length>=8||!/^[a-z]{2,3}(?:-[A-Z]{2})?$/.test(locale)||locale in d.localizedBody} onClick={()=>{field("localizedBody",{...d.localizedBody,[locale]:""});setLocale("");}}>언어 추가</button></div>
 <div className="notice-grid"><label>시작 (한국 시간 · 비우면 즉시)<input type="datetime-local" value={inputTime(d.startsAt)} onChange={e=>field("startsAt",parseTime(e.target.value)??0)}/></label><label>종료 (한국 시간 · 비우면 무기한)<input type="datetime-local" value={inputTime(d.endsAt)} onChange={e=>field("endsAt",parseTime(e.target.value))}/></label></div>
 <details><summary>추가 노출 조건</summary><div className="notice-grid">{(["channel","platform"] as const).map(key=><label key={key}>{key==="channel"?"배포 채널":"플랫폼"} (빈 값: 전체)<input value={d[key]??""} onChange={e=>field(key,e.target.value||null)}/></label>)}{(["minVersionCode","maxVersionCode"] as const).map(key=><label key={key}>{key==="minVersionCode"?"최소 버전":"최대 버전"}<input type="number" min={1} value={d[key]??""} onChange={e=>field(key,e.target.value===""?null:Number(e.target.value))}/></label>)}<label>우선순위 (-1000~1000)<input type="number" min={-1000} max={1000} value={d.priority} onChange={e=>field("priority",Number(e.target.value))}/></label><label>표시 개정 번호<input type="number" min={baselinePresentationRevision??1} max={(baselinePresentationRevision??1)+1} value={d.presentationRevision} onChange={e=>field("presentationRevision",Number(e.target.value))}/></label></div><label><input type="checkbox" checked={d.pinned} onChange={e=>field("pinned",e.target.checked)}/> 상단 고정</label></details>
 </fieldset>);
}
