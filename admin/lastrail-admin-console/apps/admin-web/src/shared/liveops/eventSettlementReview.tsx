import {settlementBounds,projectSettlement,type LocalSettlementObservation} from './eventSettlement';
const time=(value:number)=>new Intl.DateTimeFormat('ko-KR',{timeZone:'Asia/Seoul',dateStyle:'medium',timeStyle:'long',hour12:false}).format(value);
export function EventSettlementReview({T,observation}:{T:number;observation:LocalSettlementObservation|null}){const bounds=settlementBounds(T),p=projectSettlement(observation);return <section aria-label="이벤트 종료 수령 정산 검토"><h3>종료·수령 정산 로컬 관찰 · 서버 DTO 아님</h3><p>T 참여종료: {time(bounds.T)} · G=T+24시간: {time(bounds.G)} · C=T+7일: {time(bounds.C)} (KST)</p>
 <p>원참여시작≤서버확정 START&lt;T, 원회차/적격모드/정의 결속 및 권위 완료시각&lt;G 필요. 원run 만료가 빠르면 그 시각을 유지합니다. 종료후 신규 START·준비버튼·클라시각은 유예 자격이 아닙니다.</p>
 <p>시작 전 기존run의 참여기간 실적은 원 producer 계약 검토 필요 · 종료후 유예 자동 확대 없음 · 동일 run 같은 조건 재생은 추가기여 없음</p>
 <p>적격 원run: {p.eligibleOriginalRuns??'미확인'} · 미집계 기여: {p.unaggregatedContributions??'미확인'} · 미수령 권리: {p.unclaimed??'미확인'} · 지급 대기: {p.pending??'미확인'} · 확인된 지급: {p.confirmed??'미확인'} · 결과불명확: {p.unknown??'미확인'}</p>
 <p>{p.phase==='knownEmpty'?'제공된 관찰 범위 내 빈 결과':'관찰/미확인 범위를 전체 회차 완료로 판단하지 않습니다'}</p>
 <p>T에서 신규 참여/선물 자격 종료 · G에서 신규 실적 종료 · C는 수동 버튼 마감이며 확정권리 소멸이 아닙니다. 수동·자동은 같은 원권리/멱등 원장 경계를 사용합니다.</p>
 <p>C 이후 적격 원사건 누락기여 복구→원권리 생성→같은 자동정산 경로 검토. 권위 기록 없는 승리 생성 없음. 실패/공간부족/부분실패 권리는 지급대기로 보존하며 결과불명확은 원키로 복구합니다.</p>
 <ul>{p.rights.map(r=><li key={r.rightId}>{r.rightId} · {r.state} · 원권리 보존 · {r.recovery==='originalKey'?'원키 복구 필요':'같은 원권리 경계'} · 항목별 원영수증 확인 필요</li>)}</ul>
 <p>원사건 처리완결/권리정산 근거 미연결 · 장애 연장 제안은 자동 적용하지 않습니다</p><button type="button" disabled>지급 실행 불가 · 확정 계약 미연결</button></section>;}
