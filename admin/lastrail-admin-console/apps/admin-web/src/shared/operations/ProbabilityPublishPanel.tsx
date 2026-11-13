import {Link} from "react-router-dom";
import type {ProbabilitySource} from "./probabilityDisclosure";
import {useProbabilityPublish} from "./useProbabilityPublish";
export function ProbabilityPublishPanel({operator,environment,owner,auditAllowed,source}:{operator:string;environment:"DEV"|"PRD";owner:boolean;auditAllowed:boolean;source:ProbabilitySource|undefined}){
 const c=useProbabilityPublish(operator,environment,owner,source);
 const historyId=source?.disclosureId??(c.command.kind==="saved"?c.command.id:undefined);
 return <section className="admin-panel"><h2>공시 게시</h2><p>DEV의 OWNER만 게시할 수 있습니다. 게시본은 해당 원본을 보존하며 광고 ON이나 보상 정책을 변경하지 않습니다.</p>
 {source?<p>{source.publicationStatus==="PUBLISHED"?"이미 게시된 원본입니다.":source.sourceStatus==="RETIRED"?"퇴역한 광고 정책은 게시할 수 없습니다.":!source.canPublish?"현재 게시 권한 또는 게시 조건을 충족하지 않습니다.":"게시할 원본을 확인하고 사유를 입력해 주세요."}</p>:<p>게시하려는 정확한 원본을 먼저 조회해 주세요.</p>}
 <label>게시 사유 <textarea maxLength={500} disabled={!c.canPublish} value={c.reason} onChange={e=>c.changeReason(e.target.value)}/></label> <button disabled={!c.canPublish} onClick={c.prepare}>게시 내용 검토</button>
 {c.review?<section className="notice-review" aria-label="확률 공시 게시 검토"><h3>공시 게시 확인</h3><p>{environment} · {c.review.body.consumer==="OFFLINE"?"오프라인 보상":"전투 보상"} · {c.review.body.sourceKind==="AD_POLICY"?"광고 정책":"구매 정책"} · 버전 {c.review.body.policyVersion}</p><table><thead><tr><th>배율</th><th>확률</th></tr></thead><tbody>{c.review.source.distribution.map(row=><tr key={row.multiplier}><td>{row.multiplier}배</td><td>{row.percentage}%</td></tr>)}</tbody></table><p>사유: {c.review.body.reason}</p><p style={{overflowWrap:"anywhere"}}>게시 대상: {c.review.source.disclosureId}</p><p>게시 직전에 같은 원본인지 다시 확인합니다.</p><button disabled={!c.canPublish} onClick={()=>void c.confirm()}>공시 게시 실행</button> <button onClick={c.cancel}>취소</button></section>:null}
 {c.command.kind==="pending"?<p role="status">원본 확인·게시 처리 중…</p>:c.command.kind==="uncertain"?<p role="alert">이전 공시 요청 결과 미확인 <button disabled={!owner||environment!=="DEV"} onClick={()=>void c.recover()}>같은 요청 확인</button></p>:null}
 {c.message?<p role="status">{c.message}</p>:null}{historyId&&auditAllowed?<Link to={`/audit?action=PROBABILITY_PUBLISH&resourceId=${encodeURIComponent(historyId)}&result=SUCCESS`}>공시 게시 이력</Link>:null}</section>;
}
