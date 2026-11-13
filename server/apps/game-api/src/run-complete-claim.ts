import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {createRunProgressSnapshot} from './run-progress-snapshot';
export const completeRunClaimVersion='run.reward-claim.v2.20261019';
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe();
function fail(code:string):never{throw new ActivityError(409,'run_claim_complete_'+code);}
const sourceSql=`SELECT json_object('runStatus',r.status,'outcome',f.outcome,'finalPayload',f.payload_json,
 'currencies',json((SELECT json_group_array(json_object('currency',currency,'balance',balance)) FROM (SELECT * FROM account_currencies WHERE account_id=r.account_id ORDER BY currency))),
 'items',json((SELECT json_group_array(json_object('itemId',item_id,'quantity',quantity)) FROM (SELECT * FROM account_items WHERE account_id=r.account_id ORDER BY item_id))),
 'fuel',(SELECT json_object('current',fuel,'max',max_fuel,'lastRegenAt',last_regen_at,'updatedAt',updated_at) FROM fuel_state WHERE account_id=r.account_id),
 'progressVersion',json((SELECT json_object('sequence',m.sequence,'targetId',l.target_id,'deltaJson',l.delta_json) FROM progression_ledger l JOIN account_mutations m ON m.id=l.mutation_id AND m.account_id=l.account_id WHERE l.account_id=r.account_id AND l.progression_type='STORY_PROGRESS' AND m.mutation_type='STORY_PROGRESS' ORDER BY m.sequence DESC LIMIT 1)),
 'story',(SELECT json_object('actId',p.act_id,'highestClearedOperationId',p.highest_cleared_operation_id,'highestStoryProgressId',p.highest_story_progress_id,'clearedOperationsJson',p.cleared_operations_json,'storyFlagsJson',p.story_flags_json,'narrativeVersion',p.narrative_version,'lastProgressRunId',p.last_progress_run_id,'updatedAt',p.updated_at) FROM account_story_progress p WHERE p.account_id=r.account_id),
 'unlocks',json((SELECT json_group_array(json_object('unlockId',unlock_id,'unlockedAt',unlocked_at)) FROM (SELECT * FROM account_unlocks WHERE account_id=r.account_id ORDER BY unlock_id)))) AS payload
 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_final_results f ON f.run_id=r.id WHERE r.id=? AND r.account_id=? AND a.status='ACTIVE'`;
const sourceSchema=z.strictObject({runStatus:z.string(),outcome:z.string(),finalPayload:z.string(),currencies:z.array(z.strictObject({currency:id,balance:nat})),items:z.array(z.strictObject({itemId:id,quantity:nat})),fuel:z.json().nullable(),progressVersion:z.strictObject({sequence:nat,targetId:id,deltaJson:z.string()}).nullable(),story:z.json().nullable(),unlocks:z.array(z.json())});
const responseSchema=z.strictObject({version:z.literal(completeRunClaimVersion),accountId:id,runId:id,rewardPreviewId:id,rewardClaimId:id,commandId:id,
 status:z.literal('CLAIMED'),serverClaimedAt:z.string(),balanceVersion:z.string(),rewardLines:z.array(z.json()),transactionReceipt:z.json(),nextAction:z.literal('RETURN_TO_LOBBY'),
 owners:z.strictObject({account:z.strictObject({kind:z.literal('SNAPSHOT'),coverage:z.tuple([z.literal('CURRENCIES'),z.literal('ITEMS'),z.literal('FUEL')]),fuel:z.json().nullable(),beforeMutationSequence:nat,mutationSequence:nat,currencies:z.array(z.strictObject({currency:id,balance:nat})),items:z.array(z.strictObject({itemId:id,quantity:nat}))}),wallet:z.json(),progress:z.strictObject({kind:z.literal('SNAPSHOT'),beforeMutationSequence:nat,mutationSequence:nat,versionSource:z.enum(['STORY_PROGRESS_MUTATION','VERIFIED_EMPTY']),contentHash:id,state:z.json()})}),errorCode:z.null(),message:z.null()})
 .refine(r=>r.owners.account.mutationSequence===r.owners.account.beforeMutationSequence+1);
export type CompleteRunClaimResponse=z.infer<typeof responseSchema>;
/** Only composed by the existing authorized settlement owner, in its SAME atomic batch.
 * Financial-category snapshot applies only above the current account sequence; equal receipts are no-ops. Older replies never roll balances back.
 * Progress uses its original STORY_PROGRESS mutation joined to progression_ledger, independent of this financial claim. Apply only newer owner versions; equal version requires equal content hash.
 * No transport call or current-state read is needed to replay an already stored response. */
export async function prepareCompleteRunClaim(db:D1Database,input:{accountId:string;runId:string;previewId:string;transactionId:string;commandKey:string;sequence:number;at:number;balanceVersion:number;lines:unknown[];currencies:Map<string,number>;items:Map<string,number>;wallet:unknown;rewardPlan:unknown}){
 const {accountId,runId}=input;
 const row=await db.prepare(sourceSql).bind(runId,accountId).first<{payload:string}>();
 if(!row)return fail('source_unavailable');
 const parsed=sourceSchema.safeParse(JSON.parse(row.payload));if(!parsed.success)return fail('source_invalid');
 const source=parsed.data;const terminal=(source.runStatus==='COMPLETED'&&source.outcome==='VICTORY')||(source.runStatus==='FAILED'&&source.outcome==='FAILURE')||(source.runStatus==='ABANDONED'&&source.outcome==='ABANDONED');if(!terminal)return fail('final_state_unavailable');
 const progress=await createRunProgressSnapshot(source,input.sequence),at=new Date(input.at).toISOString();
 const currencies=new Map(source.currencies.map(row=>[row.currency,row.balance])),items=new Map(source.items.map(row=>[row.itemId,row.quantity]));
 for(const [currency,balance] of input.currencies)currencies.set(currency,balance);
 for(const [itemId,quantity] of input.items)items.set(itemId,quantity);
 const fuel=source.fuel===null?null:z.object({current:nat,max:nat,lastRegenAt:nat,updatedAt:nat}).parse(source.fuel);
 const finalFuel=fuel===null?null:{...fuel,lastRegenAt:new Date(fuel.lastRegenAt).toISOString(),updatedAt:new Date(fuel.updatedAt).toISOString()};
 const response=responseSchema.parse({version:completeRunClaimVersion,accountId,runId,rewardPreviewId:input.previewId,rewardClaimId:input.transactionId,commandId:input.commandKey,status:'CLAIMED',serverClaimedAt:at,balanceVersion:String(input.balanceVersion),rewardLines:input.lines,
  transactionReceipt:{rewardTransactionId:input.transactionId,runId,serverClaimedAt:at,rewardPlan:input.rewardPlan},nextAction:'RETURN_TO_LOBBY',
  owners:{account:{kind:'SNAPSHOT',coverage:['CURRENCIES','ITEMS','FUEL'],fuel:finalFuel,beforeMutationSequence:input.sequence-1,mutationSequence:input.sequence,
   currencies:[...currencies].sort(([a],[b])=>a<b?-1:a>b?1:0).map(([currency,balance])=>({currency,balance})),items:[...items].sort(([a],[b])=>a<b?-1:a>b?1:0).map(([itemId,quantity])=>({itemId,quantity}))},
   wallet:input.wallet,progress},errorCode:null,message:null});
 const serialized=json(response);if(new TextEncoder().encode(serialized).length>512*1024)return fail('capacity');const digest=await sha256Hex(serialized);
 return {response,assertion:db.prepare(`INSERT INTO run_complete_claim_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM (${sourceSql}) WHERE payload=?) THEN 1 ELSE 0 END`).bind(runId,accountId,row.payload),
  insert:db.prepare('INSERT INTO run_complete_claim_receipts VALUES(?,?,?,?,?,?,?,?)').bind(input.transactionId,accountId,runId,input.previewId,input.commandKey,completeRunClaimVersion,serialized,digest),
  cleanup:db.prepare('DELETE FROM run_complete_claim_guard')};
}
/** Original stored reply only. Legacy claims have no v2 row and remain explicitly unavailable. */
export async function readCompleteRunClaim(db:D1Database,accountId:string,runId:string,previewId:string,key:string):Promise<CompleteRunClaimResponse|null>{
 const row=await db.prepare('SELECT run_id,preview_id,payload_json,payload_hash FROM run_complete_claim_receipts WHERE account_id=? AND command_key=?').bind(accountId,key).first<{run_id:string;preview_id:string;payload_json:string;payload_hash:string}>();
 if(!row)return null;if(row.run_id!==runId||row.preview_id!==previewId)return fail('request_conflict');
 try{const response=responseSchema.parse(JSON.parse(row.payload_json));
  if(response.accountId!==accountId||response.runId!==runId||response.rewardPreviewId!==previewId||response.commandId!==key||await sha256Hex(row.payload_json)!==row.payload_hash||json(response)!==row.payload_json)throw Error('invalid');
  return response;
 }catch{throw new ActivityError(503,'run_claim_complete_storage_invalid');}
}




