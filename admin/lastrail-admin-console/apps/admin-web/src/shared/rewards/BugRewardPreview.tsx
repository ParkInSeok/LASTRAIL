import {estimateBugReward,type BugRewardDraft} from './bugReward';
const amount=(value:number)=>`${value.toLocaleString('ko-KR')} FREE 크리스탈`;
/** Disconnected presentation: parent owns the review draft. No payment, permission or fetch effect. */
export function BugRewardPreview({draft}:{draft:BugRewardDraft}) {
 const estimate=estimateBugReward(draft);
 return <section aria-label="제보 보상 독립 검토"><h2>제보 보상 검토 · 연결 전</h2>
  <p>정책 계산 예시이며 자격 미확정입니다. 지급 권한과 서버 계약 확인 전에는 실행할 수 없습니다.</p>
  <dl><div><dt>감사 보상 예상 기준액</dt><dd>{amount(estimate.gratitude.target)}</dd></div>
   <div><dt>감사 보상 예상 상향 차액</dt><dd>{estimate.gratitude.difference===null?'기지급액 미확인':amount(estimate.gratitude.difference)}</dd></div>
   <div><dt>동일인·사건 감사 상한</dt><dd>{amount(estimate.gratitude.cap)}</dd></div>
   <div><dt>전원 보상 정책액</dt><dd>{estimate.allPlayer.policyAmount===null?'해당 등급 승인 없음':amount(estimate.allPlayer.policyAmount)} · 대상 기준·배치 계약 미확정</dd></div>
   <div><dt>피해 복구</dt><dd>원손실 근거와 기존 복구 계약으로 별도 검토</dd></div>
  </dl>
  {estimate.firstReporterCombinedIllustration!==null?<p>자격 있는 최초제보자 합계 예시 {amount(estimate.firstReporterCombinedIllustration)} · 감사 보상과 전원 보상은 각각 별도 지급 · 피해 복구 별도</p>:null}
  <p>심사 입력은 자격 확정 또는 지급 완료가 아닙니다. 결과 불명확 시 원요청·원키로 확인해야 합니다.</p>
  <small>{estimate.gratitude.policyVersion} / {estimate.allPlayer.policyVersion}</small>
 </section>;
}
