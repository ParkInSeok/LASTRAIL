import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureOperationMap,projectOperationMap} from './operation-map-plan';
import {readDepartureMap} from './operation-map-departure';
import {mapId,readMapProgress} from './operation-map-state';
import {capturePublishedOperationMap} from './operation-map-publisher';
export const riskRunBindingVersion='combat.b-risk-run-binding.v1.20261003';
const hash=z.string().regex(/^[a-f0-9]{64}$/u);
const positive=z.number().int().positive().safe();
const risk=z.enum(['I','II','III','IV','V']);
const status=z.enum(['ACTIVE','FINISH_PENDING','COMPLETED','FAILED','QUARANTINED','EXPIRED','ABANDONED']);
const missingSources=['ORIGINAL_PINNED_RISK_APPLICATION_VERSION','ORIGINAL_NATIVE_COMBAT_CONTEXT','ORIGINAL_UNSCALED_HP_OR_EHP','ORIGINAL_RAW_ACTION_DAMAGE','ORIGINAL_RISK_PROGRESS_APPLIED_FLAGS','SUPPORTED_B_RUNTIME_PROFILE'] as const;
const packetSchema=z.strictObject({version:z.literal(riskRunBindingVersion),frozen:z.strictObject({accountId:mapId,runId:mapId,operationPreviewId:mapId,operationId:mapId,selectedRisk:risk,balanceVersion:mapId,contentVersion:mapId.nullable(),departureVersion:mapId,startSnapshotHash:hash,originalCombatContextId:z.null(),riskApplicationVersion:z.null()}),
 progress:z.strictObject({revision:positive,stateHash:hash,phase:z.enum(['READY','TRAVELLING','ACTIVE','FINAL_NODE_COMPLETE']),currentNodeId:mapId,visitDepth:z.number().int().min(0).max(12),band:z.enum(['EARLY','MIDDLE','LATE']).nullable(),bandStatus:z.enum(['NO_VISITED_NODE','VISITED_NODE','BOSS_SEPARATE_PROFILE']),pendingEdgeId:mapId.nullable(),blockedByPendingTransactions:z.boolean()}),
 originalProfile:z.strictObject({profile:z.literal('ORDINARY_12_NODE'),definitionVersion:mapId,planHash:hash,publicationVersion:mapId.nullable(),publicationHash:hash.nullable(),currentEncounterRef:mapId.nullable(),currentEncounterVersion:mapId.nullable(),currentEncounterDefinitionHash:hash.nullable()}),runStatus:status,
 support:z.strictObject({metadata:z.literal(true),activeRun:z.boolean(),scaling:z.literal(false),missionStart:z.literal(false)}),missingSources:z.array(z.enum(missingSources)).length(missingSources.length),packetHash:hash});
const json=(value:unknown)=>canonicalJson(value as Parameters<typeof canonicalJson>[0]);
const digest=(value:unknown)=>sha256Hex(json(value));
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_risk_run_'+code);};
const departure=z.object({runId:mapId,operationPreviewId:mapId,operationId:mapId,risk,balanceVersion:z.union([mapId,z.number().int().positive().safe()]),contentVersion:mapId.optional(),encounterType:z.literal('ORDINARY_12_NODE'),admission:z.object({version:mapId}).passthrough()}).passthrough();
const select=`SELECT r.status,r.preview_id,r.balance_config_version,p.account_id AS preview_account,p.objective,p.risk,p.balance_config_version AS preview_balance,p.status AS preview_status,p.admission_version,a.status AS account_status,m.revision,m.plan_json,m.state_json,m.start_snapshot_json,s.snapshot_json FROM runs r LEFT JOIN accounts a ON a.id=r.account_id LEFT JOIN operation_previews p ON p.id=r.preview_id LEFT JOIN operation_map_states m ON m.run_id=r.id LEFT JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?`;
type Row={status:string;preview_id:string;balance_config_version:number;preview_account:string|null;objective:string|null;risk:string|null;preview_balance:number|null;preview_status:string|null;admission_version:string|null;account_status:string|null;revision:number|null;plan_json:string|null;state_json:string|null;start_snapshot_json:string|null;snapshot_json:string|null};
/** Reads original owners only. No new departure, source upgrade, HP seed, resource write or catalog lookup. */
export async function readRiskRunBinding(db:D1Database,accountId:string,runId:string){
 mapId.parse(accountId);mapId.parse(runId);
 const read=()=>db.prepare(select).bind(runId,accountId).first<Row>();
 const row=await read();if(!row)return fail('run_not_found');if(row.account_status!=='ACTIVE')return fail('account_inactive');
 if(!row.snapshot_json||!row.start_snapshot_json||!row.plan_json||!row.state_json||row.revision===null)return fail('source_missing');
 if(row.snapshot_json!==row.start_snapshot_json)return fail('departure_conflict');
 const snapshot:unknown=JSON.parse(row.snapshot_json),d=departure.safeParse(snapshot);
 if(!d.success)return fail('profile_unsupported');const original=d.data;
 const response=departure.safeParse(original.departureResponse);
 if(!response.success||json(response.data)!==json(Object.fromEntries(Object.entries(original).filter(([key])=>key!=='departureResponse'&&Object.hasOwn(response.data,key)))))return fail('departure_conflict');
 if(original.runId!==runId||original.operationPreviewId!==row.preview_id||row.preview_account!==accountId||original.operationId!==row.objective||original.risk!==row.risk||String(original.balanceVersion)!==String(row.balance_config_version)||row.preview_balance!==row.balance_config_version||row.preview_status!=='CONSUMED'||original.admission.version!==row.admission_version)return fail('departure_conflict');
 const plan=captureOperationMap(JSON.parse(row.plan_json)),originalPlan=readDepartureMap(snapshot);
 if(!originalPlan||canonicalJson(plan)!==canonicalJson(originalPlan))return fail('plan_conflict');
 const publication=Object.hasOwn(original,'operationMapContent')?await capturePublishedOperationMap(original.operationMapContent,plan):null;
 const state=readMapProgress(plan,JSON.parse(row.state_json)),view=projectOperationMap(plan,state.traversedEdgeIds);
 const node=plan.nodes.find(n=>n.nodeId===view.currentNodeId),depth=node?.visitDepth??0;
 const binding=publication?.bindings.find(b=>b.nodeId===view.currentNodeId),entry=publication?.encounters.find(e=>e.ref===binding?.encounterRef);
 const frozen={accountId,runId,operationPreviewId:row.preview_id,operationId:original.operationId,selectedRisk:original.risk,balanceVersion:String(original.balanceVersion),contentVersion:original.contentVersion??null,departureVersion:original.admission.version,startSnapshotHash:await sha256Hex(row.snapshot_json),originalCombatContextId:null,riskApplicationVersion:null};
 const progress={revision:positive.parse(row.revision),stateHash:await sha256Hex(row.state_json),phase:state.phase,currentNodeId:view.currentNodeId,visitDepth:depth,band:depth===0||depth===12?null:depth<=4?'EARLY' as const:depth<=8?'MIDDLE' as const:'LATE' as const,bandStatus:depth===0?'NO_VISITED_NODE' as const:depth===12?'BOSS_SEPARATE_PROFILE' as const:'VISITED_NODE' as const,pendingEdgeId:state.pendingEdgeId,blockedByPendingTransactions:state.holdIds.length>0};
 const base={version:riskRunBindingVersion,frozen,progress,originalProfile:{profile:plan.profile,definitionVersion:plan.definitionVersion,planHash:await digest(plan),publicationVersion:publication?.version??null,publicationHash:publication?.contentHash??null,currentEncounterRef:node?.encounterProfileRef??null,currentEncounterVersion:entry?.version??null,currentEncounterDefinitionHash:entry?await digest(entry.definition):null},runStatus:status.parse(row.status),support:{metadata:true,activeRun:row.status==='ACTIVE',scaling:false,missionStart:false},missingSources:[...missingSources]};
 const packet=packetSchema.parse({...base,packetHash:await digest(base)});
 // Hashing/publication validation yields. Reject owner/progress/terminal changes during that interval.
 const latest=await read();if(!latest||canonicalJson(latest)!==canonicalJson(row))return fail('stale');return packet;
}
const trustSchema=z.strictObject({accountId:mapId,runId:mapId,startSnapshotHash:hash,progressRevision:positive,packetHash:hash});
/** The caller supplies trusted authenticated origin/hash/revision, never fields taken from the untrusted packet. */
export async function readPinnedRiskRunBinding(input:unknown,trust:unknown){
 const p=packetSchema.parse(input),t=trustSchema.parse(trust),{packetHash,...base}=p;
 if(p.frozen.accountId!==t.accountId||p.frozen.runId!==t.runId||p.frozen.startSnapshotHash!==t.startSnapshotHash||p.progress.revision!==t.progressRevision||packetHash!==t.packetHash||await digest(base)!==packetHash)return fail('pinned_conflict');
 return p;
}
/** Original retained snapshots have no supported B raw-profile contract; do not infer one from generic JSON. */
export function requireRiskRunScalingSource(input:unknown):never{packetSchema.parse(input);return fail('raw_profile_unavailable');}