import {EventSettlementReview} from './eventSettlementReview';
import {type EventScheduleDraft,reviewEventDefinition} from './eventScheduleDraft';
type Props={state:ReturnType<EventScheduleDraft['snapshot']>};
/** Pure preview. Editor/controller binds its existing owner; no timers, requests or execution callbacks. */
export function EventScheduleReview({state}:Props){const d=state.definition,review=reviewEventDefinition(d);return <section aria-label="이벤트 일정 보상 검토"><h2>로컬 정의 검토</h2><p>{d.title} · {d.theme} · {d.environment} · 회차 {d.roundId} · 정책 {d.policyVersion} · revision {state.revision}</p>
 <h3>일정 · KST</h3><dl>{Object.entries(d.schedule).map(([key,value])=><div key={key}><dt>{({publicAt:'공개시각',start:'참여시작',end:'참여종료',claimEnd:'수령마감',giftOpen:'기념선물개방'} as Record<string,string>)[key]}</dt><dd>{value||'미입력'}</dd></div>)}</dl>
 <p>절대시각 변환 후 검토 · 참여 [시작, 종료) · 서버 시각/원상태가 권위 · 메인 회차 겹침은 검토 경고 대상</p>
 <h3>보상 조건 미리보기</h3><ul>{d.rows.map(r=><li key={r.id}>{r.kind} · 권리 {r.id} · {r.condition} · 원천 {r.sourceId} · {r.itemId} × {r.quantity} · 계정당 {r.perAccount}회 · 원정책 {r.policySource} · 자산/지원 {r.assetReady&&r.catalogReady&&r.clientReady&&r.ledgerReady&&r.unownedHandlingReady&&r.duplicateHandlingReady?'로컬 준비표시 (서버 검증 전)':'미준비'}</li>)}</ul>
 <h3>품목별 최대 정의 총량</h3><ul>{Object.entries(review.totals).map(([item,total])=><li key={item}>{item}: {total}</li>)}</ul><p>종류가 다른 재화 합산 없음 · 달성 가능성/지급 결과를 보장하지 않음</p>
 <h3>검토·예약</h3><p>{state.reviewedRevision===state.revision?'현재 revision 로컬 검토 표시':'새 검토 필요'} · 서버 정의hash/권한/지원상태 검증 미연결</p><ul>{review.issues.map((issue,i)=><li key={i}>{issue}</li>)}</ul><button type="button" disabled>예약 불가 · 확정 계약 미연결</button>
 <h3>연장·중단 영향</h3><p>원run·확정 권리 보존 · 연장은 기존 보상표 유지 · 변경사유/효력시점/구·신정의/대상권리 영향과 새 검토 필요</p><p>중단은 새참여·실적접수·수령 영향과 원거래 진행중 결과 처리를 각각 검토합니다. 종료 단축·권리회수·미수령 소멸 실행 없음.</p><p>신규/늦은참여·미보유요원 외형·지원모드·실적 producer·중복외형 정책 대조 필요. 실제 DEV 실적→권리→수령 검수 미수행.</p>
 {state.absoluteSchedule.end!==null&&<EventSettlementReview T={state.absoluteSchedule.end} observation={null}/>} </section>;}


