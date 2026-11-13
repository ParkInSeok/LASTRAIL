import './draftReview.css';
import {ReviewLifetime} from './ReviewLifetime';
import {EventDraftSelection} from './EventDraftSelection';
import {parseOptionalKst} from './reviewInput';
import {useEffect,useMemo,useState,useRef} from 'react';
import type {BootstrapResponse} from '@lastrail/admin-contracts';
import {adminApi} from '../api/client';
import {EventScheduleDraft,type EventDefinition} from './eventScheduleDraft';
import {EventDraftModel} from './eventDraft/eventDraftModel';
import {eventDraftCanSaveRole,eventDraftDefinitionFromLocal,eventDefinitionToLocal} from './eventDraft/eventDraftAdapter';
import {EventDraftReview} from './eventDraft/EventDraftReview';
import {RewardDraftModel} from '../rewards/rewardDraftModel';
import {RewardDraftView} from '../rewards/rewardDraftView';
import {RewardDraftProposalForm} from '../rewards/rewardDraftProposalForm';
import {parseRewardDraftForm,rewardDraftFormInput,type RewardDraftFormInput} from '../rewards/rewardDraftForm';
import type {RewardProposal} from '../rewards/rewardPreviewContract';
import {OutagePreviewModel} from './outagePreview/OutagePreviewModel';
import {OutagePreviewReview} from './outagePreview/OutagePreviewReview';
import type {OutagePreviewInput} from './outagePreview/outagePreviewContract';
import {CosmeticReplacementModel} from './cosmeticReplacement/CosmeticReplacementModel';
import {CosmeticReplacementReview} from './cosmeticReplacement/CosmeticReplacementReview';

type Observable<T>={snapshot:()=>T;subscribe:(fn:()=>void)=>()=>void};
function useOwnedSnapshot<T>(owner:Observable<T>){const [state,setState]=useState(()=>owner.snapshot());useEffect(()=>{setState(owner.snapshot());return owner.subscribe(()=>setState(owner.snapshot()));},[owner]);return state;}
type Props={authorizationGeneration:string;bootstrap:BootstrapResponse;initialEvent:EventDefinition;initialReward:RewardProposal};
/** Composes existing owners for review. The production App is intentionally unchanged until final art acceptance.
 * The supplied Server contract gates EVENT_CONFIG_WRITE locally by the actual principal role;
 * it is not a bootstrap/global grant. The Server independently authorizes the write transaction.
 */
export function LiveOpsDraftReviewHost(props:Props){const b=props.bootstrap;return <ScopedDraftReview key={`${props.authorizationGeneration}:${b.admin.id}:${b.admin.role}:${b.environment}:${props.initialEvent.roundId}:${props.initialReward.batchId}`} {...props}/>;}
function ScopedDraftReview({bootstrap,initialEvent,initialReward,authorizationGeneration}:Props){
 const actor=bootstrap.admin.id,role=bootstrap.admin.role,environment=bootstrap.environment==='prd'?'PRD':'DEV';
 const [owners]=useState(()=>({
  event:new EventDraftModel({principalId:actor,environment,authorizationGeneration},eventDraftCanSaveRole(role)?['EVENT_CONFIG_WRITE']:[],{list:adminApi.eventDrafts,read:adminApi.eventDraft,save:adminApi.saveEventDraft}),
  editor:new EventScheduleDraft(actor+':'+environment,initialEvent),
  reward:new RewardDraftModel(actor,environment,initialReward.batchId,role,{read:adminApi.rewardDraft,save:adminApi.saveRewardDraft}),
  outage:new OutagePreviewModel(actor,environment,adminApi.eventOutagePreview),
  replacement:new CosmeticReplacementModel(actor,environment,adminApi.eventCosmeticReplacementTable)
 }));
 const e=useOwnedSnapshot(owners.event),local=useOwnedSnapshot(owners.editor),r=useOwnedSnapshot(owners.reward),o=useOwnedSnapshot(owners.outage),c=useOwnedSnapshot(owners.replacement);
 const [tab,setTab]=useState('event'),[message,setMessage]=useState(''),[eventReason,setEventReason]=useState(''),[rewardReason,setRewardReason]=useState(''),[rewardForm,setRewardForm]=useState<RewardDraftFormInput>(()=>rewardDraftFormInput(initialReward));
 const [round,setRound]=useState(initialEvent.roundId),[target,setTarget]=useState(''),[impactStart,setImpactStart]=useState(''),[impactEnd,setImpactEnd]=useState(''),[impactStatus,setImpactStatus]=useState<'CONFIRMED'|'UNKNOWN'|'PLANNED'>('UNKNOWN'),[claimOpen,setClaimOpen]=useState(''),[outageInput,setOutageInput]=useState<OutagePreviewInput|null>(null),[ownership,setOwnership]=useState<'unknown'|'expectedOwned'|'expectedUnowned'>('unknown');
 const [impact,setImpact]=useState({id:'',incident:'',evidence:'',feature:'PARTICIPATION' as OutagePreviewInput['impacts'][number]['feature'],applied:false});
 const selection=useMemo(()=>new EventDraftSelection(owners.event,owners.editor,()=>{owners.outage.invalidate();setOutageInput(null);},initialEvent),[owners]);
 const initialLoads=useRef<{event:Promise<void>;reward:Promise<void>;capabilities:ReturnType<typeof adminApi.rewardTrackingCapabilities>;replacement:Promise<void>}|null>(null);
 const [lifetime]=useState(()=>new ReviewLifetime(()=>{selection.dispose();owners.event.dispose();owners.reward.dispose();owners.outage.dispose();owners.replacement.dispose();}));
 const act=(action:()=>unknown|Promise<unknown>)=>{setMessage('');Promise.resolve().then(action).catch(error=>setMessage(error instanceof Error?error.message:'원본 확인 필요'));};
 useEffect(()=>{let active=true;const release=lifetime.enter();const loads=initialLoads.current??{event:selection.open(initialEvent.roundId),reward:owners.reward.read(),capabilities:adminApi.rewardTrackingCapabilities(),replacement:owners.replacement.load()};initialLoads.current=loads;
 void loads.event.catch(()=>{});void loads.reward.then(()=>{const proposal=owners.reward.snapshot().proposal;if(active&&proposal)setRewardForm(rewardDraftFormInput(proposal));});void loads.capabilities.then(caps=>{if(active)owners.reward.setWriteCapability(caps);}).catch(error=>{if(active)setMessage(String(error));});void loads.replacement.catch(error=>{if(active)setMessage(String(error));});return()=>{active=false;release();};},[owners,lifetime]);
 const readEvent=()=>act(()=>selection.open(round,true));
 const inputChanged=(definition:EventDefinition)=>{owners.editor.replace(definition);owners.outage.invalidate();setOutageInput(null);};
 const saveEvent=()=>act(async()=>{owners.event.edit(eventDraftDefinitionFromLocal(owners.editor.snapshot(),owners.event.snapshot().current?.revision??null));await owners.event.save(eventReason,crypto.randomUUID());});
 let rewardProposal:RewardProposal|null=null;try{rewardProposal=parseRewardDraftForm(rewardForm,{environment,batchId:initialReward.batchId,revision:(r.current?.revision??0)+1});}catch{/* Intermediate form input is not an accepted domain proposal. */}
 const saveReward=()=>act(async()=>{owners.reward.edit(parseRewardDraftForm(rewardForm,{environment,batchId:initialReward.batchId,revision:(owners.reward.snapshot().current?.revision??0)+1}));await owners.reward.save(rewardReason,crypto.randomUUID());});
 const calculate=()=>act(async()=>{const saved=owners.event.snapshot().current;if(!saved)throw Error('저장된 이벤트 초안을 먼저 선택하세요.');const previous=owners.outage.snapshot().input;if(previous&&previous.roundId===saved.roundId&&previous.revision===saved.revision&&previous.definitionHash===saved.definitionHash){await owners.outage.preview(previous);return;}const d=saved.definition,T=d.schedule.endAt;
 const input:OutagePreviewInput={environment,roundId:saved.roundId,revision:saved.revision,expectedRevision:saved.revision,definitionHash:saved.definitionHash,targetScope:target,observedAt:Date.now(),claimOpensAt:parseOptionalKst(claimOpen),alreadyAppliedImpactIds:impact.applied?[impact.id]:[],schedule:{startAt:d.schedule.startAt,participationEndsAt:T,completionGraceEndsAt:saved.timePolicy.completionGraceEndsAt,manualClaimEndsAt:saved.timePolicy.manualClaimEndsAt},impacts:[{impactId:impact.id,incidentId:impact.incident,feature:impact.feature,status:impactStatus,startAt:parseOptionalKst(impactStart),endAt:parseOptionalKst(impactEnd),evidenceRef:impact.evidence.trim()||null}]};setOutageInput(input);await owners.outage.preview(input);});
 const denied=e.phase==='unauthorized'||r.phase==='revoked'||o.phase==='revoked'||c.phase==='revoked';
 useEffect(()=>{if(denied){setRewardForm({title:'',type:'EVENT',incidentRef:'',policyVersion:'',targetAsOf:'',targetCondition:'',accountIdsText:'',rewardLines:[]});setEventReason('');setRewardReason('');setTarget('');setImpactStart('');setImpactEnd('');setClaimOpen('');setOutageInput(null);setImpact({id:'',incident:'',evidence:'',feature:'PARTICIPATION',applied:false});owners.editor.reset('',{...initialEvent,title:'',theme:'',policyVersion:'',rows:[],schedule:{publicAt:'',start:'',end:'',claimEnd:'',giftOpen:''}});selection.dispose();owners.event.dispose();owners.reward.revoke();owners.outage.dispose();owners.replacement.dispose();}},[denied,owners,selection]);
 if(denied)return <main className="content draft-review-host"><p role="alert">접근 권한이 변경되어 저장값과 입력을 닫았습니다. 다시 로그인해 확인하세요.</p></main>;
 const ready=bootstrap.admin.permissions.includes('SUPPORT_READ');if(!ready)return <p role="alert">운영 조회 권한을 확인하세요.</p>;
 return <main className="content draft-review-host"><header><h1>이벤트·보상 초안 검토</h1><p>{actor} · {role} · {environment} · 저장과 계산만 지원 · 예약/연장/지급 실행 없음</p></header><nav aria-label="검토 영역">{[['event','이벤트 초안'],['reward','보상 초안'],['outage','장애 계산'],['replacement','외형 대체표']].map(([id,label])=><button key={id} aria-pressed={tab===id} onClick={()=>setTab(id)}>{label}</button>)}</nav>{message?<p role="alert">{message}</p>:null}
 <div hidden={tab!=='event'}><label>선택 회차 ID<input value={round} onChange={ev=>setRound(ev.target.value)}/></label><button disabled={e.phase==='saving'||!!e.pending} onClick={readEvent}>회차 원본 선택</button><EventDraftReview state={e} localEditor={local} reason={eventReason} canWrite={owners.event.canWrite&&e.phase!=='loading'} onDefinitionChange={inputChanged} onReasonChange={setEventReason} onSave={saveEvent} onRecover={()=>act(()=>owners.event.recover())} onRefresh={readEvent}/></div>
 <div hidden={tab!=='reward'}><h2>보상 배치 {initialReward.batchId}</h2>{!['revoked','disposed'].includes(r.phase)?<RewardDraftProposalForm value={rewardForm} onChange={setRewardForm} disabled={['loading','saving','unknown','revoked','disposed'].includes(r.phase)}/>:null}<RewardDraftView state={{...r,canSave:r.canSave&&!!rewardProposal,proposal:rewardProposal}} reason={rewardReason} onReason={setRewardReason} onSave={saveReward} onRecover={()=>act(()=>owners.reward.recover())} onRead={()=>act(async()=>{await owners.reward.read(true);const p=owners.reward.snapshot().proposal;if(p)setRewardForm(rewardDraftFormInput(p));})}/></div>
 <div hidden={tab!=='outage'}><section className="admin-panel"><h2>저장 초안 기준 장애 영향 제안</h2><p>원장으로 확인된 운영 일정이 아닙니다. 증거 분류는 운영자의 제안이며 실제 장애 권위 검증 전입니다.</p><fieldset disabled={['revoked','disposed','loading'].includes(o.phase)}><legend>장애 근거 제안</legend>{(['id','incident','evidence'] as const).map(key=><label key={key}>{{id:'영향 ID',incident:'사건 ID',evidence:'원본 근거 참조'}[key]}<input value={impact[key]} onChange={ev=>{setImpact({...impact,[key]:ev.target.value});owners.outage.invalidate();}}/></label>)}<label>영향 기능<select value={impact.feature} onChange={ev=>{setImpact({...impact,feature:ev.target.value as typeof impact.feature});owners.outage.invalidate();}}><option value="PARTICIPATION">참여·출격</option><option value="COMPLETION">완료 유예</option><option value="CLAIM">수령</option><option value="AGGREGATION">실적 집계</option></select></label><label><input type="checkbox" checked={impact.applied} onChange={ev=>{setImpact({...impact,applied:ev.target.checked});owners.outage.invalidate();}}/>이미 반영된 영향으로 제안 · 원장 검증 전</label><label>대상 범위<input value={target} onChange={ev=>{setTarget(ev.target.value);owners.outage.invalidate();}}/></label><label>영향 분류<select value={impactStatus} onChange={ev=>{setImpactStatus(ev.target.value as typeof impactStatus);owners.outage.invalidate();}}><option value="UNKNOWN">미확인</option><option value="CONFIRMED">확인 제안 · 원본 인증 전</option><option value="PLANNED">계획</option></select></label><label>차단 시작 KST<input placeholder="2026-10-05T10:00:00" value={impactStart} onChange={ev=>{setImpactStart(ev.target.value);owners.outage.invalidate();}}/></label><label>복구 시각 KST<input placeholder="2026-10-05T11:00:00" value={impactEnd} onChange={ev=>{setImpactEnd(ev.target.value);owners.outage.invalidate();}}/></label><label>수령 개방 KST · 미공급은 공란<input value={claimOpen} onChange={ev=>{setClaimOpen(ev.target.value);owners.outage.invalidate();}}/></label></fieldset><button onClick={calculate} disabled={!e.current||o.phase==='loading'}>장애 연장 계산</button></section><OutagePreviewReview state={o} onRefresh={()=>act(()=>outageInput&&owners.outage.preview(outageInput,true))}/></div>
 <div hidden={tab!=='replacement'}><label>예상 보유 상태<select value={ownership} onChange={ev=>setOwnership(ev.target.value as typeof ownership)}><option value="unknown">미확인</option><option value="expectedOwned">보유 가정 · 실제 조회 아님</option><option value="expectedUnowned">미보유 가정 · 실제 조회 아님</option></select></label><CosmeticReplacementReview state={c} ownership={ownership} onRefresh={()=>act(()=>owners.replacement.load(true))}/></div></main>;
}
