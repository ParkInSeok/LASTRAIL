import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {ExactCombatValue,exactCombatValueSchema} from './combat-exact-value';
import {numericDefenseInputSchema,resolveNumericDefenseBudget} from './combat-numeric-defense';
export const operationRiskVersion='combat.b-operation-risk.v1.20261003';
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
const digest=async(x:unknown)=>'sha256:'+await sha256Hex(json(x));
const ratio=(n:string,d='1')=>({numerator:n,denominator:d});
const risks=[{id:'I',hp:ratio('1'),damage:ratio('1')},{id:'II',hp:ratio('5','4'),damage:ratio('23','20')},{id:'III',hp:ratio('31','20'),damage:ratio('13','10')},{id:'IV',hp:ratio('19','10'),damage:ratio('3','2')},{id:'V',hp:ratio('23','10'),damage:ratio('7','4')}];
const progress=[{id:'EARLY',damage:ratio('1')},{id:'MIDDLE',damage:ratio('11','10')},{id:'LATE',damage:ratio('6','5')}];
export async function operationRiskDefinition(){const payload={version:operationRiskVersion,approval:'44#combat-b-operation-risk-approved-20261003',risks:structuredClone(risks),progress:structuredClone(progress),policy:{hp:'RISK_ONCE_ONLY_HP_OR_EHP_NOT_BOTH',damage:'RAW_ACTION_RISK_ONCE_PROGRESS_ONCE_NOT_ACTOR_ATK',precision:'EXACT_RATIONAL_ORIGINAL_PIPELINE_FINAL_FLOOR',unchanged:['ATK','ATTACK_INTERVAL','WINDUP','RECOVERY','WARNINGS','REWARDS','UNLOCKS','FACILITIES','WORLD'],historical:'PIN_ORIGINAL_RUN_VERSION_NO_RETROACTIVE_UPGRADE'}};return {...payload,definitionHash:await digest(payload)};}
const id=z.string().min(1).max(128),hash=z.string().regex(/^sha256:[a-f0-9]{64}$/);
export const operationRiskBindingSchema=z.strictObject({version:z.literal(operationRiskVersion),operationId:id,runId:id,contextId:id,risk:z.enum(['I','II','III','IV','V']),progress:z.enum(['EARLY','MIDDLE','LATE']),rawSourceHash:hash,revision:z.literal(0),provenance:z.literal('BOUNDED_LOCAL_EXPLICIT_NOT_MISSION_START')});
export const operationRiskRawSchema=z.strictObject({hp:z.strictObject({sourceId:id,kind:z.enum(['BASE_HP','EHP_BUDGET']),value:exactCombatValueSchema,riskAppliedVersion:z.null()}),damage:z.strictObject({sourceId:id,kind:z.literal('RAW_ACTION_DAMAGE'),value:exactCombatValueSchema,riskAppliedVersion:z.null(),progressAppliedVersion:z.null()})});
export type OperationRiskBinding=z.infer<typeof operationRiskBindingSchema>;
export type OperationRiskRaw=z.infer<typeof operationRiskRawSchema>;
/** Immutable LOCAL source. Only its producer can attest an original unscaled HP/budget and raw action.
 * Marker stripping by an untrusted caller does not establish that attestation.
 */
export class BOperationRiskFixture {
 private constructor(private readonly binding:OperationRiskBinding,private readonly original:OperationRiskRaw){}
 static async create(bindingInput:unknown,rawInput:unknown){
 const b=operationRiskBindingSchema.safeParse(structuredClone(bindingInput)),r=operationRiskRawSchema.safeParse(structuredClone(rawInput));
 if(!b.success||!r.success)throw new ActivityError(400,'combat_b_risk_source_invalid');
 if(await digest(r.data)!==b.data.rawSourceHash)throw new ActivityError(409,'combat_b_risk_source_conflict');
 const fixture=new BOperationRiskFixture(b.data,r.data);fixture.scaleHp();fixture.scaleDamage();return fixture;
 }
 private risk(){return risks.find(r=>r.id===this.binding.risk)!;}
 scaleHp(){return {sourceId:this.original.hp.sourceId,kind:this.original.hp.kind,exact:ExactCombatValue.from(this.original.hp.value).multiply(ExactCombatValue.from(this.risk().hp)).snapshot(),riskAppliedVersion:operationRiskVersion,progressAppliedVersion:null,spawnConversion:'NOT_PERFORMED' as const};}
 scaleDamage(){return {sourceId:this.original.damage.sourceId,kind:this.original.damage.kind,exact:ExactCombatValue.from(this.original.damage.value).multiply(ExactCombatValue.from(this.risk().damage)).multiply(ExactCombatValue.from(progress.find(p=>p.id===this.binding.progress)!.damage)).snapshot(),riskAppliedVersion:operationRiskVersion,progressAppliedVersion:operationRiskVersion};}
 resolveDamage(input:unknown){
 const parsed=z.strictObject({pipeline:numericDefenseInputSchema,riskAppliedVersion:z.null(),progressAppliedVersion:z.null()}).safeParse(input);
 if(!parsed.success)throw new ActivityError(400,'combat_b_risk_pipeline_invalid');
 if(json(ExactCombatValue.from(parsed.data.pipeline.base).snapshot())!==json(ExactCombatValue.from(this.original.damage.value).snapshot()))throw new ActivityError(409,'combat_b_risk_pipeline_source_conflict');
 return resolveNumericDefenseBudget({...parsed.data.pipeline,base:this.scaleDamage().exact});
 }
 async read(){const payload={version:operationRiskVersion,definition:await operationRiskDefinition(),binding:structuredClone(this.binding),original:structuredClone(this.original),scaled:{hp:this.scaleHp(),damage:this.scaleDamage()},schemas:{binding:z.toJSONSchema(operationRiskBindingSchema),original:z.toJSONSchema(operationRiskRawSchema)},support:{sourceBound:true,localProjection:true,missionStart:false,enemyAuto:false,hpSpawnConversion:false},ownership:'TRUSTED_LOCAL_RAW_SOURCE_ONLY_UNITY_SOLO_HP_OWNER',missingMissionFields:['AUTHENTICATED_MISSION_DEPARTURE_RUN_ID','ORIGINAL_OPERATION_PROFILE_VERSION_HASH','SERVER_SELECTED_UNLOCK_ELIGIBLE_RISK','PINNED_UNSCALED_HP_OR_EHP_AND_RAW_ACTION_PROFILE','ACTUAL_PROGRESS_BAND_AND_SOURCE_APPLIED_FLAGS'],startReady:false};return {...payload,packetHash:await digest(payload)};}
}
export async function readPinnedOperationRiskPacket(input:unknown,trustedBinding:unknown,expectedPacketHash:string){
 const owned=structuredClone(input),binding=operationRiskBindingSchema.safeParse(structuredClone(trustedBinding));
 const p=z.object({version:z.literal(operationRiskVersion),packetHash:hash,binding:operationRiskBindingSchema,original:operationRiskRawSchema}).safeParse(owned);
 if(!p.success||!binding.success)throw new ActivityError(400,'combat_b_risk_packet_invalid');
 if(p.data.packetHash!==expectedPacketHash||json(p.data.binding)!==json(binding.data))throw new ActivityError(409,'combat_b_risk_packet_conflict');
 const fixture=await BOperationRiskFixture.create(binding.data,p.data.original),expected=await fixture.read();
 if(expected.packetHash!==expectedPacketHash||json(expected)!==json(owned))throw new ActivityError(409,'combat_b_risk_packet_conflict');return fixture;
}