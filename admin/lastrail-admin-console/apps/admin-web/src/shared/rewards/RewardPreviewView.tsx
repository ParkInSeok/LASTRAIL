import {Link} from 'react-router-dom';
import type {RewardPreviewState} from './RewardPreviewModel';
/** Existing host owns Model and input/preview callbacks; no executing or receipt-fetching View. */
export function RewardPreviewView({state,onPreview,onRefresh}:{state:RewardPreviewState;onPreview:()=>void;onRefresh:()=>void}){
 const r=state.preview.data;
 return <section aria-label="보상 제안 미리보기"><h2>보상 제안 미리보기 · 검토용 구성안</h2><p>SUPPORT_READ 읽기 전용 · 검토승인·지급 실행 불가</p>
 {state.phase!=='active'?<p role="alert">운영자 권한을 다시 확인하세요.</p>:<><button disabled={!state.proposal||state.preview.phase==='loading'} onClick={onPreview}>제안 미리보기</button><button onClick={onRefresh}>명시적 갱신</button>
 {state.capabilities.data?<p>제안 대상 최대 {state.capabilities.data.maximumExplicitPreviewTargets} · 지급/검토승인 미지원</p>:null}
 {state.preview.phase==='loading'?<p>조회 중</p>:state.preview.phase==='error'?<p role="alert">제안 미리보기를 확인할 수 없습니다.</p>:r?<><p>제안 revision {r.revision} · hash {r.reviewHash} · 관측시각 {r.observedAt}</p><p>명시 제안 대상만 관측 · 확정 대상 snapshot 아님 · 실행 작업 미생성</p>
 <dl>{[['요청',r.targets.requestedCount],['고유 제안',r.targets.uniqueProposedCount],['중복 입력',r.targets.duplicateInputCount],['관측 계정',r.targets.observedAccountCount],['없는 계정',r.targets.missingAccountCount],['확정 적격',r.targets.confirmedEligibleCount??'미판정'],['미판정 자격',r.targets.unresolvedEligibilityCount],['기존 권리 중복',r.targets.duplicateEntitlementCount??'미판정'],['정책 제외',r.targets.policyExcludedCount??'미판정']].map(([label,value])=><div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>
 <h3>고유 제안 계정 기준 예상 총량 · 확정 지급 아님</h3><ul>{r.proposedTotals.map(line=><li key={line.itemId}>{line.itemId}: {line.quantity}</li>)}</ul>
 <ul>{r.observations.map(row=><li key={row.accountId}><Link to={`/support/accounts/${encodeURIComponent(row.accountId)}`}>기존 계정 지원 기록 · {row.accountId}</Link> · {row.account?.status??'계정 미관측'} · 자격 미판정 · 기존권리/지급/수령 미관측</li>)}</ul><ul>{r.unavailable.map(reason=><li key={reason}>{reason}</li>)}</ul></>:<p>명시 제안을 선택하세요. 미리보기는 승인이나 지급이 아닙니다.</p>}</>}
 </section>;
}
