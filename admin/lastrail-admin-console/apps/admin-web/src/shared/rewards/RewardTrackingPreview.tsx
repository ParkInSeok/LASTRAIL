import {summarizeRights,type RewardReviewState} from './rewardTracking';
const statuses={PENDING:'대기',PROCESSING:'처리중',PAYMENT_CONFIRMED:'지급확정',ALREADY_PAID:'기존지급확인',EXCLUDED:'정책상제외',FAILED:'실패확정',UNKNOWN:'결과불명확'};
const receipts={UNRECEIVED:'미수령',RECEIVED:'수령',EXPIRED:'만료',NOT_APPLICABLE:'해당 없음',UNKNOWN:'미확인'};
export function RewardTrackingPreview({state}:{state:RewardReviewState}){
 const p=state.projection;
 if(!p)return <section aria-label="보상 지급 추적 검토 후보"><h2>보상 지급 추적 · 연결 전</h2><p>원본 미공급 · 실행 불가</p></section>;
 const summary=summarizeRights(p.rights);
 return <section aria-label="보상 지급 추적 검토 후보"><h2>보상 지급 추적 · 연결 전</h2>
 <p>시각 승인·producer 계약 대기 중인 독립 검토 후보 · 실행 불가</p>
 <dl>{[['환경',p.environment],['원배치',p.batchReference],['원본 버전',p.sourceVersion],['검토 revision',p.revision],['정책 버전',p.policyVersion],['대상 snapshot',p.targetSnapshot??'미확정'],['대상수',`${p.targetCount??'미확인'} · ${p.targetCountKind==='CONFIRMED'?'확정':'추정'}`],['보상 총량',p.totalQuantity??'미확인'],['집계 범위',p.aggregateScope==='CURRENT_PAGE'?'현재 페이지':'전체 대상'],['집계시각',p.aggregatedAt],['검토 상태',state.review?'검토 완료 · 지급 실행 아님':`재검토 필요 · ${state.reviewInvalidation??'미검토'}`]].map(([label,value])=><div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>
 <p>표시 범위 처리 완료: {summary.allProcessed?'예':'아니오'} · 표시 범위 모든 대상 지급 성공: {summary.allPaid?'예':'아니오'}</p>
 <table><thead><tr><th>권리 참조</th><th>유형</th><th>지급 처리</th><th>수령</th></tr></thead><tbody>{p.rights.map(row=><tr key={row.reference}><td>{row.reference}</td><td>{row.kind}</td><td>{statuses[row.processing]}</td><td>{receipts[row.receipt]}</td></tr>)}</tbody></table>
 <p>결과불명확 {summary.unknown}건: 원거래·원키 복구 후 확정. 실패확정 {summary.failed}건: 재시도 가능 원인만 검토. 지급확정·기존지급확인은 재지급 대상이 아닙니다.</p>
 <p>감사·전원보상·피해복구는 별도 권리입니다. 합계·권리 생성·전달·실수령을 지급 성공으로 합치지 않습니다. 실행중 중지는 새 대상 착수만 중단하며 처리중 원거래 결과 확인이 필요합니다.</p>
 </section>;
}
