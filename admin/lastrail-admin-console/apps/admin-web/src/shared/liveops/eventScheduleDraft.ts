import {settlementBounds} from './eventSettlement';
/** Local editor definitions only. No Server DTO, grant, receipt, reservation or execution protocol. */
export type EventRewardRow={id:string;kind:'participation'|'story'|'cumulative'|'anniversary';condition:string;sourceId:string;itemId:string;quantity:number;perAccount:number;threshold?:number;policySource:string;catalogReady:boolean;ledgerReady:boolean;clientReady:boolean;assetReady:boolean;unownedHandlingReady:boolean;duplicateHandlingReady:boolean};
export type EventDefinition={roundId:string;policyVersion:string;title:string;theme:string;environment:'DEV'|'PRD'|'';schedule:{publicAt:string;start:string;end:string;claimEnd:string;giftOpen:string};rows:EventRewardRow[];producerPolicy:string|null;closePolicy:string|null;settlementPolicy:string|null};
const day=86400000;
export function parseKst(value:string):number|null{const match=/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})$/.exec(value);if(!match)return null;const [y,m,d,h,min,s]=match.slice(1).map(Number);if(y<1000||m<1||m>12||d<1||h>23||min>59||s>59)return null;const utc=Date.UTC(y,m-1,d,h,min,s),date=new Date(utc);return date.getUTCFullYear()===y&&date.getUTCMonth()===m-1&&date.getUTCDate()===d?utc-9*3600000:null;}
function frozenDefinition(input:EventDefinition):EventDefinition{const copy=structuredClone(input);Object.freeze(copy.schedule);for(const row of copy.rows)Object.freeze(row);Object.freeze(copy.rows);return Object.freeze(copy);}
export function reviewEventDefinition(d:EventDefinition){const issues:string[]=[],totals:Record<string,number>=Object.create(null);for(const field of ['roundId','policyVersion','title','theme'] as const)if(!d[field].trim())issues.push(`${field} 필요`);if(!['DEV','PRD'].includes(d.environment))issues.push('환경 선택 필요');
 const times=Object.fromEntries(Object.entries(d.schedule).map(([key,v])=>[key,parseKst(v)]));const {publicAt,start,end,claimEnd,giftOpen}=times;
 if([publicAt,start,end,claimEnd].some(v=>v===null))issues.push('일정은 유효한 KST 날짜와 시각이 필요합니다');else if(publicAt!>start!||start!>=end!||end!>claimEnd!)issues.push('공개≤시작<종료≤수령마감 순서 필요');
 if(start!==null&&end!==null&&(end-start<14*day||end-start>28*day))issues.push('최초 참여기간은 14~28일이어야 합니다');
 if((d.schedule.giftOpen||d.rows.some(r=>r.kind==='anniversary'))&&(giftOpen===null||start===null||end===null||giftOpen<start||giftOpen>=end))issues.push('기념선물 개방은 참여기간 [시작, 종료) 내여야 합니다');
 if(end!==null&&claimEnd!==null&&claimEnd!==settlementBounds(end).C)issues.push('수령마감은 승인된 T+7일과 일치해야 합니다');
 if(!d.rows.length)issues.push('보상행 필요');const ids=new Set<string>(),thresholds=new Map<string,number>();for(const r of d.rows){if(!r.id.trim()||ids.has(r.id))issues.push('권리ID 누락/중복');ids.add(r.id);if(!r.itemId.trim()||!r.sourceId.trim()||!r.condition.trim()||!r.policySource.trim())issues.push(`${r.id}: 원천/품목/조건/원정책 필요`);
 if(!Number.isSafeInteger(r.quantity)||r.quantity<=0||!Number.isSafeInteger(r.perAccount)||r.perAccount<=0)issues.push(`${r.id}: 양의 정수 수량/계정당 횟수 필요`);else{const total=(totals[r.itemId]??0)+r.quantity*r.perAccount;if(!Number.isSafeInteger(total))issues.push(`${r.id}: 총량 범위 초과`);else totals[r.itemId]=total;}
 if(![r.catalogReady,r.ledgerReady,r.clientReady,r.assetReady,r.unownedHandlingReady,r.duplicateHandlingReady].every(v=>v===true))issues.push(`${r.id}: 자산/카탈로그/원장/클라/보유처리 미준비`);
 if(r.kind==='cumulative'){const key=r.sourceId;const previous=thresholds.get(key)??0;if(!Number.isSafeInteger(r.threshold)||r.threshold!<=previous)issues.push(`${r.id}: 누적 문턱은 같은 실적원천 내 증가해야 합니다`);else thresholds.set(key,r.threshold!);}}
 if(!d.producerPolicy||!d.closePolicy||!d.settlementPolicy)issues.push('승인 정책·실적 producer 결속 미확인');return {issues,totals,reservationAvailable:false as const};}
/** Owns local definition/review only. Server remains authority for schedule state, earned rights and receipts. */
export class EventScheduleDraft{private definition:EventDefinition;private revision=0;private reviewedRevision:number|null=null;private generation=0;private listeners=new Set<()=>void>();constructor(private scope:string,definition:EventDefinition){this.definition=frozenDefinition(definition);}
 snapshot(){return Object.freeze({scope:this.scope,generation:this.generation,revision:this.revision,reviewedRevision:this.reviewedRevision,definition:this.definition,absoluteSchedule:Object.freeze(Object.fromEntries(Object.entries(this.definition.schedule).map(([key,value])=>[key,parseKst(value)])) as Record<keyof EventDefinition['schedule'],number|null>)});}
 replace(definition:EventDefinition){this.definition=frozenDefinition(definition);this.revision++;this.reviewedRevision=null;this.emit();}
 markReviewed(){this.reviewedRevision=this.revision;this.emit();}
 reset(scope:string,definition:EventDefinition){this.scope=scope;this.generation++;this.revision=0;this.reviewedRevision=null;this.definition=frozenDefinition(definition);this.emit();}
 subscribe(listener:()=>void){this.listeners.add(listener);return()=>{this.listeners.delete(listener);};}private emit(){for(const listener of this.listeners)listener();}
 review(){return reviewEventDefinition(this.definition);}
 copyForNewRound(roundId:string){return new EventScheduleDraft(this.scope,{...this.definition,roundId}).snapshot();}
 impact(kind:'extend'|'stop'){return {kind,preserveRewardRows:true as const,preserveEarnedRights:true as const,preserveOriginalRun:true as const,requiresReview:true as const,executionAvailable:false as const};}
}



