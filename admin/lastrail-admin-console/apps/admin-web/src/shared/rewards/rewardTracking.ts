/** Admin-only display projection; NOT a Server wire, permission, receipt or execution contract. */
export type RightProcessing = 'PENDING'|'PROCESSING'|'PAYMENT_CONFIRMED'|'ALREADY_PAID'|'EXCLUDED'|'FAILED'|'UNKNOWN';
export type ReceiptState = 'UNRECEIVED'|'RECEIVED'|'EXPIRED'|'NOT_APPLICABLE'|'UNKNOWN';
export type RewardRightDisplay = Readonly<{reference:string;kind:'GRATITUDE'|'ALL_PLAYER'|'RESTORATION'|'EVENT';processing:RightProcessing;receipt:ReceiptState;retryable?:boolean}>;
export type RewardTrackingProjection = Readonly<{
 environment:string;batchReference:string;sourceVersion:string;revision:string;policyVersion:string;
 targetSnapshot:string|null;targetCount:number|null;targetCountKind:'ESTIMATED'|'CONFIRMED';totalQuantity:number|null;
 aggregateScope:'CURRENT_PAGE'|'ALL_TARGETS';aggregatedAt:string;rights:readonly RewardRightDisplay[];
}>;
export type ReviewInvalidation = 'LOCAL_EDIT'|'SOURCE_CHANGED'|'SOURCE_CONFLICT'|'PERMISSION_CHANGED'|'EXPLICIT_REFRESH'|'RESULT_UNKNOWN'|null;
type ReviewBinding = Readonly<{revision:string;sourceVersion:string;policyVersion:string;targetSnapshot:string}>;
export type RewardReviewState = Readonly<{generation:number;projection:RewardTrackingProjection|null;review:ReviewBinding|null;reviewInvalidation:ReviewInvalidation;executionAvailable:false}>;
const processing:readonly string[]=['PENDING','PROCESSING','PAYMENT_CONFIRMED','ALREADY_PAID','EXCLUDED','FAILED','UNKNOWN'];
const receipt:readonly string[]=['UNRECEIVED','RECEIVED','EXPIRED','NOT_APPLICABLE','UNKNOWN'];
function copyProjection(value:RewardTrackingProjection):RewardTrackingProjection {
 for(const count of [value.targetCount,value.totalQuantity])if(count!==null&&(!Number.isSafeInteger(count)||count<0))throw new Error('수량/대상수 확인 필요');
 for(const text of [value.environment,value.batchReference,value.sourceVersion,value.revision,value.policyVersion,value.aggregatedAt])if(typeof text!=='string'||!text.trim())throw new Error('원본 식별/버전 확인 필요');
 if(value.targetSnapshot!==null&&(typeof value.targetSnapshot!=='string'||!value.targetSnapshot.trim()))throw new Error('대상 snapshot 확인 필요');
 if(!['ESTIMATED','CONFIRMED'].includes(value.targetCountKind)||!['CURRENT_PAGE','ALL_TARGETS'].includes(value.aggregateScope))throw new Error('집계 범위 확인 필요');
 const references=new Set<string>();
 const rights=value.rights.map(row=>{
  if(!row.reference||references.has(row.reference)||!processing.includes(row.processing)||!receipt.includes(row.receipt)||!['GRATITUDE','ALL_PLAYER','RESTORATION','EVENT'].includes(row.kind)||(row.retryable!==undefined&&typeof row.retryable!=='boolean'))throw new Error('권리 표시값 확인 필요');
  references.add(row.reference);return Object.freeze({...row});
 });
 // Account/target count and entitlement-row count are different dimensions.
 return Object.freeze({...value,rights:Object.freeze(rights)});
}
export function summarizeRights(rights:readonly RewardRightDisplay[]) {
 const paid=(row:RewardRightDisplay)=>row.processing==='PAYMENT_CONFIRMED'||row.processing==='ALREADY_PAID';
 const terminal=(row:RewardRightDisplay)=>paid(row)||row.processing==='EXCLUDED'||row.processing==='FAILED';
 return Object.freeze({total:rights.length,paymentConfirmed:rights.filter(paid).length,unknown:rights.filter(row=>row.processing==='UNKNOWN').length,failed:rights.filter(row=>row.processing==='FAILED').length,
  allProcessed:rights.length>0&&rights.every(terminal),allPaid:rights.length>0&&rights.every(paid),
  recoveryRequired:Object.freeze(rights.filter(row=>row.processing==='UNKNOWN').map(row=>row.reference)),
  retryableFailed:Object.freeze(rights.filter(row=>row.processing==='FAILED'&&row.retryable===true).map(row=>row.reference))});
}
/** One context owns the immutable review state; caller supplies existing source data, never fetches here. */
export class RewardReviewOwner {
 #state:RewardReviewState=Object.freeze({generation:0,projection:null,review:null,reviewInvalidation:null,executionAvailable:false});
 snapshot=()=>this.#state;
 beginContext(_contextIdentity:string):number {
  if(!_contextIdentity.trim())throw new Error('검토 컨텍스트 필요');
  this.#state=Object.freeze({generation:this.#state.generation+1,projection:null,review:null,reviewInvalidation:null,executionAvailable:false});return this.#state.generation;
 }
 accept(value:RewardTrackingProjection,generation:number,expectedRevision:string|null):boolean {
  if(generation!==this.#state.generation)return false;
  if((this.#state.projection?.revision??null)!==expectedRevision){this.invalidate('SOURCE_CONFLICT');return false;}
  const current=this.#state.projection;
  if(current&&(value.environment!==current.environment||value.batchReference!==current.batchReference)){this.invalidate('SOURCE_CONFLICT');return false;}
  const projection=copyProjection(value);
  // Any accepted producer update requires explicit fresh review; old reviews cannot authorize it.
  this.#state=Object.freeze({...this.#state,projection,review:null,reviewInvalidation:this.#state.projection?'SOURCE_CHANGED':null});return true;
 }
 completeReview():void {
  const p=this.#state.projection;
  if(!p||this.#state.reviewInvalidation==='LOCAL_EDIT'||this.#state.reviewInvalidation==='PERMISSION_CHANGED'||this.#state.reviewInvalidation==='SOURCE_CONFLICT'||this.#state.reviewInvalidation==='RESULT_UNKNOWN'||this.#state.reviewInvalidation==='EXPLICIT_REFRESH'||p.targetCountKind!=='CONFIRMED'||p.targetCount===null||!p.targetSnapshot||p.totalQuantity===null)throw new Error('확정 원본과 권한 재확인 필요');
  this.#state=Object.freeze({...this.#state,review:Object.freeze({revision:p.revision,sourceVersion:p.sourceVersion,policyVersion:p.policyVersion,targetSnapshot:p.targetSnapshot}),reviewInvalidation:null});
 }
 edit():void {this.invalidate('LOCAL_EDIT');}
 invalidate(reason:Exclude<ReviewInvalidation,null>):void {this.#state=Object.freeze({...this.#state,review:null,reviewInvalidation:reason});}
}


