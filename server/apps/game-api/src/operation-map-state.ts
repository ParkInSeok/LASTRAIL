import {z} from "zod";
import {ActivityError} from "./activity-policy";
import {exactCombatValueSchema} from './combat-exact-value';
import {ordinaryAutomationSchema,ordinaryActionCarryoverSchema} from './ordinary-run-automation';
import {captureOperationMap,projectOperationMap,requireDirectDestination,type OperationMapPlan} from "./operation-map-plan";
export const mapId=z.string().min(1).max(200);
const revision=z.number().int().positive().safe();
export const mapProgress=z.object({version:z.literal("operation.map-progress.v1"),traversedEdgeIds:z.array(mapId).max(12),pendingEdgeId:mapId.nullable(),phase:z.enum(["READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE"]),holdIds:z.array(mapId).max(1000).refine(x=>new Set(x).size===x.length)}).strict();
const base={eventId:mapId,expectedRevision:revision};
export const mapCommand=z.discriminatedUnion("kind",[
 z.object({...base,kind:z.literal("CONFIRM"),edgeId:mapId}).strict(),
 z.object({...base,kind:z.literal("ARRIVE"),edgeId:mapId,evidenceRef:mapId}).strict(),
 z.object({...base,kind:z.literal("COMPLETE"),nodeId:mapId,evidenceRef:mapId,attemptGeneration:revision.optional()}).strict(),
 z.object({...base,kind:z.literal("RELEASE_HOLD"),holdId:mapId,evidenceRef:mapId}).strict()
]);
export const mapProof=z.object({kind:z.enum(["ARRIVE","COMPLETE","RELEASE_HOLD"]),verificationId:mapId,holdIds:z.array(mapId).max(1000).refine(x=>new Set(x).size===x.length),attemptGeneration:revision.optional(),ultimateAuto:z.boolean().optional(),actions:ordinaryActionCarryoverSchema.optional()}).strict().refine(p=>(p.ultimateAuto===undefined&&p.actions===undefined)||p.kind==='COMPLETE');
export const destinationResponseVersion='operation.map-destination.v2.20261019';
const destinationId=mapId,destinationRevision=revision,destinationHash=z.string().regex(/^[a-f0-9]{64}$/u);
export const destinationResponseSchema=z.strictObject({
 version:z.literal(destinationResponseVersion),accountId:destinationId,runId:destinationId,requestId:destinationId,revision:destinationRevision,
 startSnapshotHash:destinationHash,planHash:destinationHash,
 map:z.strictObject({graphId:destinationId,graphRevision:destinationRevision,eventAssignmentVersion:destinationId,definitionVersion:destinationId}),
 state:z.strictObject({phase:z.literal('TRAVELLING'),currentNodeId:destinationId,pendingEdgeId:destinationId}),
 destination:z.strictObject({edgeId:destinationId,fromNodeId:destinationId,toNodeId:destinationId,node:z.strictObject({nodeId:destinationId,visitDepth:z.number().int().min(1).max(12),eventKind:z.enum(['COMBAT','ELITE','AUGMENT','EQUIPMENT','ENCHANT','MAINTENANCE','MERCHANT','SCOUT','BOSS']),publicInfo:z.strictObject({name:z.string().min(1).max(200),iconId:destinationId}),encounterProfileRef:destinationId,rewardPlanRef:destinationId})}),
 nextAction:z.literal('AWAIT_VERIFIED_ARRIVAL'),travelInitial:z.record(z.string(),z.json()).optional()
}).refine(v=>v.state.pendingEdgeId===v.destination.edgeId&&v.state.currentNodeId===v.destination.fromNodeId&&v.destination.toNodeId===v.destination.node.nodeId);

export const firstPlayableScalingSchema=z.strictObject({
 version:z.enum(['combat.first-playable-scaling.v1.20261027','combat.first-playable-scaling.v2.20261028']),accountId:mapId,runId:mapId,encounterId:mapId,
 nodeId:mapId,visitDepth:z.number().int().min(1).max(11),progressRevision:revision,progressStateHash:destinationHash,
 startSnapshotHash:destinationHash,initializationResponseHash:destinationHash,sourceHash:destinationHash,
 profileId:mapId,profileVersion:mapId,profileHash:destinationHash,
 riskDefinitionVersion:mapId,riskDefinitionHash:z.string().regex(/^sha256:[a-f0-9]{64}$/u),
 applied:z.strictObject({risk:z.enum(['I','II','III','IV','V']),progress:z.enum(['EARLY','MIDDLE','LATE']),
  hpMultiplier:exactCombatValueSchema,riskDamageMultiplier:exactCombatValueSchema,progressDamageMultiplier:exactCombatValueSchema,
  gunnerHp:exactCombatValueSchema,launcherHp:exactCombatValueSchema,gunnerRawDamage:exactCombatValueSchema,artilleryRawDamage:exactCombatValueSchema}),
 hpRiskApplications:z.literal(1),damageRiskApplications:z.literal(1),damageProgressApplications:z.literal(1),bindingHash:destinationHash,
 enemySpatial:z.strictObject({version:mapId,definitionHash:destinationHash,geometryDefinitionHash:destinationHash,physicalInstanceId:z.string().regex(/^[a-f0-9]{32}$/u)}).optional()
}).refine(v=>(v.version==='combat.first-playable-scaling.v2.20261028')===(v.enemySpatial!==undefined));
const nodeStartSchema=z.strictObject({version:z.literal('ordinary.node-start.v1.20261028'),accountId:mapId,runId:mapId,nodeId:mapId,
 generation:z.literal(1),entryMapRevision:revision,snapshot:z.record(z.string(),z.json()),snapshotHash:destinationHash});
export const mapReceipt=z.object({version:z.literal("operation.map-receipt.v1"),eventId:mapId,revision,state:mapProgress,verificationId:mapId.nullable(),destination:destinationResponseSchema.optional(),firstPlayableScaling:firstPlayableScalingSchema.optional(),nodeStart:nodeStartSchema.optional(),travelInitial:z.record(z.string(),z.json()).optional(),ordinaryAutomation:ordinaryAutomationSchema.optional()}).strict();
export const mapFail=(code:string):never=>{throw new ActivityError(409,`operation_map_${code}`);};
export function initialMapProgress(){return mapProgress.parse({version:"operation.map-progress.v1",traversedEdgeIds:[],pendingEdgeId:null,phase:"READY",holdIds:[]});}
export function readMapProgress(plan:OperationMapPlan,input:unknown){
 const s=mapProgress.parse(input);projectOperationMap(plan,s.traversedEdgeIds);
 if((s.phase==="TRAVELLING")!==(s.pendingEdgeId!==null))return mapFail("state_invalid");
 if(s.pendingEdgeId)requireDirectDestination(plan,s.traversedEdgeIds,s.pendingEdgeId);
 if((s.phase==="ACTIVE"||s.phase==="FINAL_NODE_COMPLETE")&&s.traversedEdgeIds.length===0)return mapFail("state_invalid");
 if((s.phase==="READY"||s.phase==="TRAVELLING")&&s.traversedEdgeIds.length===12)return mapFail("state_invalid");
 if(s.phase==="FINAL_NODE_COMPLETE"&&s.traversedEdgeIds.length!==12)return mapFail("state_invalid");
 if(s.holdIds.length&&(s.phase==="TRAVELLING"||s.phase==="ACTIVE"||!s.traversedEdgeIds.length))return mapFail("state_invalid");return s;
}
/** Progress rules only. Arrival/objective/transaction facts must be verified by the mode adapter. */
export function transitionMap(planInput:unknown,stateInput:unknown,commandInput:unknown,proofInput:unknown){
 const plan=captureOperationMap(planInput),s=readMapProgress(plan,stateInput),c=mapCommand.parse(commandInput);
 if(c.kind!=="CONFIRM"){const p=mapProof.safeParse(proofInput);if(!p.success||p.data.kind!==c.kind||(c.kind!=="COMPLETE"&&p.data.holdIds.length))return mapFail("verification_invalid");}
 if(c.kind==="CONFIRM"){
  if(s.phase!=="READY")return mapFail("phase_invalid");if(s.holdIds.length)return mapFail("hold_active");requireDirectDestination(plan,s.traversedEdgeIds,c.edgeId);s.pendingEdgeId=c.edgeId;s.phase="TRAVELLING";
 }else if(c.kind==="ARRIVE"){
  if(s.phase!=="TRAVELLING"||s.pendingEdgeId!==c.edgeId)return mapFail("phase_invalid");s.traversedEdgeIds.push(c.edgeId);s.pendingEdgeId=null;s.phase="ACTIVE";
 }else if(c.kind==="COMPLETE"){
  if(s.phase!=="ACTIVE"||projectOperationMap(plan,s.traversedEdgeIds).currentNodeId!==c.nodeId)return mapFail("phase_invalid");s.holdIds=mapProof.parse(proofInput).holdIds;s.phase=s.traversedEdgeIds.length===12?"FINAL_NODE_COMPLETE":"READY";
 }else{
  if(!s.holdIds.includes(c.holdId))return mapFail("hold_missing");s.holdIds=s.holdIds.filter(id=>id!==c.holdId);
 }
 return readMapProgress(plan,s);
}
