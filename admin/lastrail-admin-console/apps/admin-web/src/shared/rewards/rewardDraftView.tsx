import type {RewardDraftState} from './rewardDraftModel';
type Props={state:RewardDraftState;reason:string;onReason:(reason:string)=>void;onSave:()=>void;onRecover:()=>void;onRead:()=>void};
/** Controlled candidate: parent owns original save key and binds existing Model methods. */
export function RewardDraftView({state,reason,onReason,onSave,onRecover,onRead}:Props){const inactive=['revoked','disposed'].includes(state.phase);return <section aria-label="보상 초안 저장"><h2>보상 초안 저장 · DRAFT</h2><p>초안 저장은 대상 검토승인·지급이 아닙니다. 실행 불가.</p>{inactive?<p role="alert">권한을 다시 확인하세요.</p>:<>
 <p>현재 저장 revision {state.current?.revision??'없음'} · 상태 {state.phase}</p>
 <label>초안 변경 사유<textarea value={reason} onChange={event=>onReason(event.target.value)} disabled={state.phase==='saving'||state.phase==='unknown'}/></label>
 <button onClick={onSave} disabled={!state.canSave||!state.proposal||!reason.trim()||['saving','unknown','conflict','loading'].includes(state.phase)}>초안 저장</button>
 <button onClick={onRead} disabled={['saving','unknown','loading'].includes(state.phase)}>저장 원본 조회</button>
 {state.phase==='unknown'?<p role="alert">저장 결과 확인 필요. 본문을 바꾸지 않고 원키·원본문으로 복구합니다.</p>:null}
 {state.phase==='unknown'?<button onClick={onRecover}>원저장 결과 복구</button>:null}
 {state.phase==='conflict'?<p role="alert">동시 수정 충돌 · 최신 저장 원본을 조회한 뒤 다시 검토하세요.</p>:null}
 {state.current?<p>저장 당시 관측값 · observationsCurrent:false · 현재 자격/지급/수령 확정 아님</p>:null}
 {state.preview?<ul>{state.preview.proposedTotals.map(line=><li key={line.itemId}>{line.itemId}: {line.quantity} · 제안 총량</li>)}</ul>:null}
 </>}</section>;}
