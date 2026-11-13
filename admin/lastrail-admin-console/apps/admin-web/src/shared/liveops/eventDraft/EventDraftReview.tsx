import type {EventDefinition,EventRewardRow,EventScheduleDraft} from '../eventScheduleDraft';
import type {EventDraftSnapshot} from './eventDraftModel';
export type EventDraftReviewProps={state:EventDraftSnapshot;localEditor:ReturnType<EventScheduleDraft['snapshot']>;reason:string;canWrite:boolean;onDefinitionChange:(value:EventDefinition)=>void;onReasonChange:(value:string)=>void;onSave:()=>void;onRecover:()=>void;onRefresh:()=>void};
const schedules={publicAt:'공개시각',start:'참여 시작',end:'참여 종료',claimEnd:'수동 수령 마감',giftOpen:'기념선물 개방'};
const kinds={participation:'참여',story:'스토리',cumulative:'누적',anniversary:'기념선물'};
/** Controlled View: local editor and save authority remain in their existing owners. */
export function EventDraftReview({state,localEditor,reason,canWrite,onDefinitionChange,onReasonChange,onSave,onRecover,onRefresh}:EventDraftReviewProps){
 const d=localEditor.definition,terminal=state.phase==='unauthorized'||state.phase==='disposed',busy=!!state.pending,locked=terminal||busy||!canWrite;
 if(terminal)return <section aria-label="이벤트 초안 접근 제한"><p role="alert">접근 권한을 다시 확인하세요.</p></section>;
 const change=(patch:Partial<EventDefinition>)=>onDefinitionChange({...d,...patch});
 const rowChange=(index:number,patch:Partial<EventRewardRow>)=>change({rows:d.rows.map((row,i)=>i===index?{...row,...patch}:row)});
 return <section aria-label="이벤트 초안 저장 검토"><h2>이벤트 정의 초안</h2><p>현재 저장 revision: {state.current?.revision??'미확인'} · 회차 {state.roundId??'미선택'} · {state.current?.state??'저장 원문 미확인'}</p>
 <p>초안 저장은 검토 승인·예약·발행·실적·권리 생성·지급·정산 실행이 아닙니다. 예약·발행 실행 불가</p>
 {state.error&&<p role="alert">조회·저장 확인 필요: {state.error}</p>}{state.phase==='conflict'&&<p role="alert">현재 저장값과 입력을 비교한 뒤 다음 revision으로 다시 검토하세요.</p>}
 <fieldset disabled={locked}><legend>로컬 편집 · KST</legend><label>회차 ID<input value={d.roundId} readOnly/></label><label>환경<input value={d.environment} readOnly/></label>
 {(['title','theme','policyVersion'] as const).map(key=><label key={key}>{{title:'제목',theme:'테마',policyVersion:'정책 버전'}[key]}<input value={d[key]} maxLength={key==='policyVersion'?128:200} onChange={e=>change({[key]:e.target.value})}/></label>)}
 {(Object.keys(schedules) as Array<keyof typeof schedules>).map(key=><label key={key}>{schedules[key]}<input type="datetime-local" step="0.001" value={d.schedule[key]} onChange={e=>change({schedule:{...d.schedule,[key]:e.target.value}})}/></label>)}
 <p>최초 참여기간 14~28일 · G=T+24시간 · C=T+7일 · 각 마감 정각 제외</p><h3>보상 권리</h3>
 {d.rows.map((row,index)=><fieldset key={index}><legend>보상 {index+1}</legend>
 <label>종류<select value={row.kind} onChange={e=>rowChange(index,{kind:e.target.value as EventRewardRow['kind']})}>{Object.entries(kinds).map(([key,label])=><option key={key} value={key}>{label}</option>)}</select></label>
 {(['id','condition','sourceId','itemId','policySource'] as const).map(key=><label key={key}>{{id:'권리 ID',condition:'달성 조건',sourceId:'원천 ID',itemId:'품목 ID',policySource:'보상 정책 버전'}[key]}<input value={row[key]} maxLength={key==='condition'?1000:128} onChange={e=>rowChange(index,{[key]:e.target.value})}/></label>)}
 {(['quantity','perAccount','threshold'] as const).map(key=><label key={key}>{{quantity:'수량',perAccount:'계정당 횟수',threshold:'누적 문턱'}[key]}<input type="number" min="1" step="1" value={row[key]===undefined||Number.isNaN(row[key])?'':row[key]} onChange={e=>rowChange(index,{[key]:key==='threshold'&&e.target.value===''?undefined:e.target.valueAsNumber})}/></label>)}
 <button type="button" onClick={()=>change({rows:d.rows.filter((_,i)=>i!==index)})}>보상 {index+1} 삭제</button></fieldset>)}
 <button type="button" disabled={d.rows.length>=100} onClick={()=>change({rows:[...d.rows,{id:'',kind:'participation',condition:'',sourceId:'',itemId:'',quantity:1,perAccount:1,policySource:'',catalogReady:false,ledgerReady:false,clientReady:false,assetReady:false,unownedHandlingReady:false,duplicateHandlingReady:false}]})}>보상행 추가</button>
 <label>변경 사유<textarea maxLength={1000} value={reason} onChange={e=>onReasonChange(e.target.value)}/></label></fieldset>
 <button type="button" disabled={locked||!!state.pending||!reason.trim()} onClick={onSave}>초안 저장</button><button type="button" disabled={terminal||busy} onClick={onRefresh}>저장 원문 새로고침</button>
 {state.pending?.phase==='uncertain'&&<div role="alert"><p>저장 결과가 불명확합니다. 새 저장을 중단하고 원요청으로 복구하세요.</p><button type="button" disabled={!canWrite||terminal} onClick={onRecover}>같은 원요청으로 저장 결과 복구</button></div>}
 {state.lastReceipt&&<p>마지막 원요청 영수증: {state.lastReceipt.roundId} · revision {state.lastReceipt.revision}. 현재 저장 revision과 다를 수 있습니다.</p>}
 {state.current&&<section aria-label="확정 초안 응답"><h3>저장된 정의 검증</h3><p>{state.current.definitionValid?'정의·시간 검증 통과':'정의·시간 검증 항목 확인 필요'} · 검토 승인 안 됨 · 예약 미지원</p><p>정의 해시 {state.current.definitionHash}</p><ul>{state.current.issues.map((value,i)=><li key={i}>{value}</li>)}</ul><h4>미연결·차단 항목</h4><ul>{state.current.blockers.map((value,i)=><li key={i}>{value}</li>)}</ul><h4>계정당 제안 최대 총량</h4><ul>{state.current.proposedTotals.map(row=><li key={row.itemId}>{row.itemId}: {row.quantity}</li>)}</ul><p>확정 권리·실제 지급 총량이 아닙니다.</p></section>}
 </section>;
}


