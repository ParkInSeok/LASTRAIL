/** Local review projections only. Original event/run/rights owners remain authoritative; no Server DTO or command protocol. */
export type OutageFeature='PARTICIPATION'|'COMPLETION'|'CLAIM'|'AGGREGATION';
export type OutageSchedule=Readonly<{environment:'DEV'|'PRD';roundId:string;revision:number;start:number;T:number;G:number;C:number}>;
export type OutageEvidence=Readonly<{incidentId:string;environment:'DEV'|'PRD';roundId:string;feature:OutageFeature;startedAt:number|null;restoredAt:number|null;confirmed:boolean;targetConfirmed:boolean}>;
const day=86400000,features:OutageFeature[]=['PARTICIPATION','COMPLETION','CLAIM','AGGREGATION'];
const at=(v:unknown):v is number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&Number.isFinite(new Date(v).getTime());
function union(intervals:[number,number][]){const sorted=[...intervals].sort((a,b)=>a[0]-b[0]);let total=0,start:number|undefined,end=0;for(const [a,b] of sorted){if(start===undefined){start=a;end=b;}else if(a<=end)end=Math.max(end,b);else{total+=end-start;start=a;end=b;}}return total+(start===undefined?0:end-start);}
export function reviewOutageExtension(original:OutageSchedule,proposal:OutageSchedule,evidence:readonly OutageEvidence[]){
 const issues:string[]=[],duration={} as Record<OutageFeature,number|null>;
 if(proposal.environment!==original.environment||proposal.roundId!==original.roundId||proposal.revision!==original.revision)issues.push('원일정 환경·회차·revision을 확인해 주세요.');
 if(![original.start,original.T,original.G,original.C,proposal.start,proposal.T,proposal.G,proposal.C].every(at))issues.push('일정 절대시각을 확인해 주세요.');
 if(proposal.start!==original.start)issues.push('연장 검토에서 원래 참여 시작시각을 변경할 수 없습니다.');
 if(proposal.T<original.T||proposal.G<original.G||proposal.C<original.C)issues.push('기존 마감을 앞당길 수 없습니다.');
 if(proposal.T>original.T&&(proposal.G<proposal.T+day||proposal.C<proposal.T+7*day))issues.push('새 참여 종료 기준 24시간 유예와 7일 수령을 보장해야 합니다.');
 if(proposal.G>original.G&&proposal.C<proposal.G+6*day)issues.push('유예 종료 이후 최소 6일의 수령 기간이 필요합니다.');
 if(proposal.T<original.start||proposal.G<proposal.T||proposal.C<proposal.G)issues.push('참여·유예·수령 마감 순서를 확인해 주세요.');
 if(evidence.some(v=>v.environment!==original.environment||v.roundId!==original.roundId))issues.push('다른 환경 또는 회차의 장애 근거입니다.');
 for(const feature of features){const relevant=evidence.filter(v=>v.feature===feature&&v.environment===original.environment&&v.roundId===original.roundId);const complete=relevant.length>0&&relevant.every(v=>v.incidentId.trim()&&v.confirmed&&v.targetConfirmed&&at(v.startedAt)&&at(v.restoredAt)&&v.restoredAt>=v.startedAt);duration[feature]=complete?union(relevant.map(v=>[v.startedAt!,v.restoredAt!])):null;}
 if(evidence.some(v=>!v.confirmed||!v.targetConfirmed||!at(v.startedAt)||!at(v.restoredAt)||v.restoredAt<v.startedAt))issues.push('실제 차단 시각·대상 근거가 미확인입니다.');
 if(proposal.T!==original.T&&!evidence.some(v=>v.feature==='PARTICIPATION'))issues.push('참여 재개방·연장에 필요한 실제 영향 검토가 필요합니다.');
 const confirmed=(feature:OutageFeature)=>duration[feature]!==null&&duration[feature]!>0;
 if(proposal.G>original.G&&!confirmed('COMPLETION')&&!(proposal.T>original.T&&confirmed('PARTICIPATION')&&proposal.G===Math.max(original.G,proposal.T+day)))issues.push('완료 유예 연장에 필요한 실제 영향 검토가 필요합니다.');
 const derivedClaim=Math.max(original.C,proposal.T>original.T&&confirmed('PARTICIPATION')?proposal.T+7*day:0,proposal.G>original.G&&confirmed('COMPLETION')?proposal.G+6*day:0);
 if(proposal.C>original.C&&!confirmed('CLAIM')&&proposal.C!==derivedClaim)issues.push('수령 마감 연장에 필요한 실제 영향 검토가 필요합니다.');
 return Object.freeze({issues:Object.freeze(issues),duration:Object.freeze(duration),requiresContributionRecovery:evidence.some(v=>v.feature==='AGGREGATION'),preserveOriginalRuns:true as const,preserveConfirmedRights:true as const,compensationSeparate:true as const,executionAvailable:false as const});
}
function immutable<T>(v:T):T{if(v&&typeof v==='object'){Object.freeze(v);for(const child of Object.values(v))immutable(child);}return v;}
export class OutageExtensionDraft {
 private state:{phase:'active'|'disposed';generation:number;original:OutageSchedule;proposal:OutageSchedule;evidence:readonly OutageEvidence[];reviewed:boolean};private listeners=new Set<()=>void>();
 constructor(original:OutageSchedule){this.state=immutable({phase:'active',generation:0,original:{...original},proposal:{...original},evidence:[],reviewed:false});}
 snapshot=()=>this.state;
 subscribe=(fn:()=>void)=>{if(this.state.phase!=='active')return()=>{};this.listeners.add(fn);return()=>{this.listeners.delete(fn);};};
 private set(next:typeof this.state){if(this.state.phase!=='active')return;this.state=immutable(next);for(const fn of this.listeners)fn();}
 setEvidence(evidence:readonly OutageEvidence[]){this.set({...this.state,evidence:evidence.map(v=>({...v})),reviewed:false});}
 setProposal(proposal:OutageSchedule){this.set({...this.state,proposal:{...proposal},reviewed:false});}
 markReviewed(){if(reviewOutageExtension(this.state.original,this.state.proposal,this.state.evidence).issues.length)throw new Error('미확인·충돌 내용을 먼저 확인해 주세요.');this.set({...this.state,reviewed:true});}
 invalidate(){this.set({...this.state,generation:this.state.generation+1,evidence:[],reviewed:false});}
 dispose(){this.listeners.clear();this.state=immutable({...this.state,phase:'disposed',generation:this.state.generation+1,evidence:[],reviewed:false});}
}

