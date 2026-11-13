import {useId} from "react";
import "./adPolicy.css";
import type {useAdPolicyController} from "./useAdPolicyController";
import type {PolicyEnvironment} from "./adPolicyContract";
type Props=ReturnType<typeof useAdPolicyController>&{environment:PolicyEnvironment;writeAllowed:boolean};
export function AdPolicyConnectedView({read,draft,errors,command,editable,review,reviewDraft,change,save,reload,retry,canRetry,environment,writeAllowed}:Props){
 const errorId=useId(),policy=read.kind==="ready"?read.policy:undefined;
 return <section className="policy-card" aria-label="광고 운영 정책" aria-busy={read.kind==="loading"||command.kind==="pending"}>
  <header className="policy-card-header"><h2>광고 정책 <span className="policy-environment">{environment}</span></h2><button type="button" disabled={read.kind==="loading"||command.kind==="pending"} onClick={()=>void reload()}>새로고침</button></header>
  {read.kind==="loading"&&<p role="status">광고 정책 조회 중</p>}{read.kind==="error"&&<p role="alert">{read.message}</p>}
  <div className="policy-overview"><div><p>현재 적용 상태</p><strong className="policy-current">{policy?.adsEnabled===true?"ON":policy?.adsEnabled===false?"OFF":"미확인"}</strong><p>{policy?.adsEnabled===true?"신규 광고 노출 허용":policy?.adsEnabled===false?"신규 광고 노출 중단":"현재 정책을 OFF로 간주하지 않습니다."}</p></div><dl>
   <dt>정책 버전</dt><dd>{policy?.runtimePolicyRevision??"미확인"}</dd><dt>조회 시각 · KST</dt><dd>{policy?new Date(policy.serverTime).toLocaleString("ko-KR",{timeZone:"Asia/Seoul"}):"미확인"}</dd>
   <dt>변경 권한</dt><dd>{environment==="PRD"?"PRD · 읽기 전용":writeAllowed&&policy?.canWrite?"OWNER · OFF 변경 가능":"저장 불가 · 권한 또는 환경 제한"}</dd></dl></div><hr/>
  <form onSubmit={event=>{event.preventDefault();reviewDraft();}}>
   <fieldset className="policy-fields" disabled={!editable} aria-describedby={errors.length?errorId:undefined}>
    <legend>정책 변경</legend>
    <div><label>변경할 정책<select aria-label="광고 ON/OFF 초안" value={draft.adsEnabled===null?"":String(draft.adsEnabled)} onChange={event=>change({...draft,adsEnabled:event.target.value===""?null:event.target.value==="true"})}><option value="">선택 필요</option><option value="false">OFF · 신규 광고 중단</option><option value="true" disabled={!policy?.canEnable}>ON · 준비 검증 필요</option></select></label>{!policy?.canEnable&&<p className="policy-hint">ON 전환은 준비 검증 전까지 사용할 수 없습니다.</p>}</div>
    <label>변경 사유<textarea aria-label="광고 정책 변경 사유" maxLength={500} value={draft.reason} placeholder="예: 광고 공급자 점검으로 신규 노출 중단" onChange={event=>change({...draft,reason:event.target.value})}/><span className="policy-count">{draft.reason.length} / 500</span></label>
   </fieldset>
   {errors.length>0&&<ul id={errorId} role="alert">{errors.map(error=><li key={error}>{error}</li>)}</ul>}
   <div className="policy-actions"><p className="policy-hint">화면 이동 시 미전송 초안은 폐기됩니다.</p><button type="submit" disabled={!editable}>변경 내용 검토</button></div>
  </form>
  {review&&editable&&<div className="policy-review"><h3>변경 요청 검토</h3><p>{environment} · {policy?.adsEnabled===true?"ON":policy?.adsEnabled===false?"OFF":"미확인"} → {review.draft.adsEnabled?"ON":"OFF"} · 기준 버전 {review.revision??"미설정"}</p><p className="policy-reason">{review.draft.reason}</p>{review.draft.adsEnabled===false&&<p>신규 광고만 중단하며, 기본 보상·영구권·기존 승인 거래는 유지됩니다.</p>}<button type="button" disabled={!editable} onClick={save}>정책 저장</button></div>}
  {command.kind==="pending"&&<p role="status">저장 결과 확인 중 · 중복 요청 차단</p>}
  {command.kind==="uncertain"&&<div role="alert"><p>저장 결과 미확인 · 새 요청을 만들지 말고 같은 요청의 결과를 확인하세요. 새로고침이나 앱 종료 시 복구 식별자는 보관되지 않습니다.</p><button type="button" disabled={!canRetry} onClick={retry}>같은 요청 결과 확인</button></div>}
  {command.kind==="rejected"&&<p role="alert">{command.message}</p>}
  {command.kind==="saved"&&<p role="status">저장 확인 · 요청 개정 {command.receipt.runtimePolicyRevision} · 감사 ID {command.receipt.auditId}. 현재 정책은 별도 조회 결과를 확인하세요.</p>}
 </section>;
}
