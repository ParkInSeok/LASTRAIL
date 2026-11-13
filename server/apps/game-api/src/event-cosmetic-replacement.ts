import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {currencyIdToD1Currency} from './reward-plan-lines';

export const eventCosmeticReplacementVersion='event.cosmetic-replacement.v1.20261020';
export const eventCosmeticKind=z.enum(['OPERATOR_SKIN','TRAIN_APPEARANCE','PROFILE_FRAME','TITLE']);
const id=z.string().trim().min(1).max(128),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const resource=z.enum(['FREE_CRYSTAL','cur_credits','cur_tactical_data','cur_operator_exp','cur_machine_parts']);
type Resource=z.infer<typeof resource>;
type Kind=z.infer<typeof eventCosmeticKind>;
const fail=(code:string):never=>{throw new ActivityError(409,'event_replacement_'+code);};
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
function line(key:Resource,quantity:number){
 return key==='FREE_CRYSTAL'?{resource:key,quantity,kind:'FREE_CRYSTAL' as const,currencyId:null}:
  {resource:key,quantity,kind:'CURRENCY' as const,currencyId:currencyIdToD1Currency[key]};
}
/** Approved policy only. Growth IDs are the existing reward-plan-lines /
 * activity-grant-plan direct currency units. FREE_CRYSTAL deliberately does not
 * invent a wallet ID or misuse ATTENDANCE/QUEST as an event source.
 * No items, boxes, paid crystals or cosmetic catalog entries are created.
 */
export async function captureEventCosmeticReplacement(){
 const rows=[
  {cosmeticKind:'OPERATOR_SKIN' as const,lines:[line('FREE_CRYSTAL',1500),line('cur_credits',10000),line('cur_tactical_data',250),line('cur_operator_exp',5000)]},
  {cosmeticKind:'TRAIN_APPEARANCE' as const,lines:[line('FREE_CRYSTAL',1000),line('cur_credits',10000),line('cur_machine_parts',500)]},
  {cosmeticKind:'PROFILE_FRAME' as const,lines:[line('FREE_CRYSTAL',300),line('cur_credits',3000),line('cur_tactical_data',50)]},
  {cosmeticKind:'TITLE' as const,lines:[line('FREE_CRYSTAL',300),line('cur_credits',3000),line('cur_tactical_data',50)]}
 ];
 const definition={version:eventCosmeticReplacementVersion,approval:'10#event-cosmetic-replacement-table-approved-20261020',
  selection:'WHOLE_BUNDLE' as const,condition:'SAME_ORIGINAL_COSMETIC_ACHIEVEMENT' as const,durationMultiplier:1,rows};
 return {...definition,definitionHash:await sha256Hex(json(definition))};
}
async function table(version:string){
 if(version!==eventCosmeticReplacementVersion)return fail('table_version_unsupported');
 return captureEventCosmeticReplacement();
}
export async function eventCosmeticReplacementBundle(version:string,kind:Kind){
 const definition=await table(version),parsed=eventCosmeticKind.parse(kind);
 return {tableVersion:definition.version,tableHash:definition.definitionHash,...definition.rows.find(row=>row.cosmeticKind===parsed)!};
}
const rowsSchema=z.strictObject({roundId:id,tableVersion:id,durationDays:z.union([z.literal(14),z.literal(21),z.literal(28)]),
 rows:z.array(z.strictObject({rewardRowId:id,cosmeticId:id,cosmeticKind:eventCosmeticKind,conditionRef:id,
  componentOfCosmeticId:id.nullable()})).min(1).max(128)});
/** Structural review, never publication authority. Aliases/root assets and the
 * achievement condition still require the original cosmetic/event catalog.
 * Explicit component rewards and duplicate IDs cannot pass even this review. */
export async function inspectEventReplacementRows(input:unknown){
 const c=rowsSchema.parse(input),definition=await table(c.tableVersion);
 if(new Set(c.rows.map(r=>r.rewardRowId)).size!==c.rows.length)return fail('duplicate_reward_row');
 if(new Set(c.rows.map(r=>r.cosmeticId)).size!==c.rows.length)return fail('duplicate_cosmetic');
 if(c.rows.some(r=>r.componentOfCosmeticId!==null))return fail('whole_cosmetic_required');
 const rows=c.rows.map(row=>({...row,lines:structuredClone(definition.rows.find(d=>d.cosmeticKind===row.cosmeticKind)!.lines)}));
 const totals:Record<Resource,number>={FREE_CRYSTAL:0,cur_credits:0,cur_tactical_data:0,cur_operator_exp:0,cur_machine_parts:0};
 for(const row of rows)for(const entry of row.lines)totals[entry.resource]+=entry.quantity;
 return {contractVersion:eventCosmeticReplacementVersion,roundId:c.roundId,tableHash:definition.definitionHash,
  rows,maximumReplacementTotals:totals,totalsMeaning:'ALL_LISTED_ORIGINAL_CONDITIONS_EARNED_AND_ALREADY_OWNED_UPPER_BOUND',
  durationMultiplier:1,sourceAuthorityVerified:false,publicationSupported:false,executeSupported:false,
  missingSources:['ORIGINAL_WHOLE_COSMETIC_CATALOG_AND_ALIAS_BINDING','ORIGINAL_EVENT_ACHIEVEMENT_AND_FIRST_GRANT_OWNER',
   'ORIGINAL_EVENT_FREE_CRYSTAL_SOURCE_AND_ACTIVE_WALLET_POLICY','ORIGINAL_REWARD_ISSUERS_AND_ATOMIC_ITEM_RESULT_RECOVERY']};
}
const receiptSchema=z.strictObject({environment:z.enum(['DEV','PRD']),roundId:id,rewardRowId:id,accountId:id,cosmeticId:id,
 cosmeticKind:eventCosmeticKind,branch:z.literal('REPLACEMENT'),tableVersion:id,tableHash:hash,originalGrantReceiptRef:id,
 lines:z.array(z.strictObject({resource,quantity:z.number().int().positive().safe(),
  status:z.enum(['PENDING','SUCCEEDED','RESULT_UNKNOWN','FAILED_RETRYABLE','FAILED_FINAL']),originalLineReceiptRef:id.nullable()})).min(1).max(5)});
/** Validates a supplied ORIGINAL receipt projection; it does not authenticate
 * that receipt, write progress, or issue any item. No current ownership input:
 * retries use the frozen branch/table and cannot choose a cosmetic again.
 * Actual issuers must verify each line receipt before executing these actions.
 */
export async function inspectEventReplacementRecovery(input:unknown){
 const c=receiptSchema.parse(input),bundle=await eventCosmeticReplacementBundle(c.tableVersion,c.cosmeticKind);
 if(c.tableHash!==bundle.tableHash)return fail('table_hash_conflict');
 if(c.lines.length!==bundle.lines.length||new Set(c.lines.map(l=>l.resource)).size!==c.lines.length)return fail('whole_bundle_required');
 for(const l of c.lines){
  if(!bundle.lines.some(expected=>expected.resource===l.resource&&expected.quantity===l.quantity))return fail('line_conflict');
  if(l.status==='SUCCEEDED'&&l.originalLineReceiptRef===null)return fail('successful_line_receipt_required');
 }
 const right={environment:c.environment,roundId:c.roundId,rewardRowId:c.rewardRowId,accountId:c.accountId};
 const rightIdentity=await sha256Hex(json(right));
 const lines=await Promise.all(c.lines.map(async l=>({...l,lineIdentity:await sha256Hex(json({...right,resource:l.resource})),
  nextAction:l.status==='SUCCEEDED'?'NONE':l.status==='RESULT_UNKNOWN'?'RECOVER_ORIGINAL':l.status==='FAILED_FINAL'?'REQUIRES_ORIGINAL_OWNER':'RETRY_ORIGINAL_LINE'})));
 return {contractVersion:eventCosmeticReplacementVersion,rightIdentity,branch:c.branch,tableVersion:c.tableVersion,tableHash:c.tableHash,
  originalGrantReceiptRef:c.originalGrantReceiptRef,lines,allLinesReportedSucceeded:c.lines.every(l=>l.status==='SUCCEEDED'),
  sourceAuthorityVerified:false,executeSupported:false,applied:false,receiptWritten:false};
}
