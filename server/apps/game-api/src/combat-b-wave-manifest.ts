import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {captureOperationMap,type OperationMapPlan} from './operation-map-plan';
import {readDepartureMap} from './operation-map-departure';
import {operationRiskRawSchema,operationRiskVersion} from './combat-b-operation-risk';
import {bDepartureCombatSourceVersion,readBDepartureCombatPin,readBDepartureCombatPinForRun,type BDepartureCombatSourceOwner,type BDepartureCombatSource,type BDepartureCombatContext} from './combat-b-departure-combat-pin';
export const bWaveManifestVersion='combat.b-wave-manifest.v1.20261003';
export const bWaveManifestLimits={templates:32,nodes:64,waves:128,instancesPerWave:16,actionsPerTemplate:32,utf8Bytes:524288} as const;
const pressure={version:'combat.b-wave-pressure.v3.20261003',battlefield:'ROOF_EXTERIOR',stages:[
 {fromDepth:1,toDepth:4,minimumCount:6,maximumCount:8,aliveCap:4,firstGroup:4,nextGroupMaximum:2,entryIntervalMs:1800,attackSlots:3},
 {fromDepth:5,toDepth:8,minimumCount:9,maximumCount:12,aliveCap:6,firstGroup:4,nextGroupMaximum:2,entryIntervalMs:1500,attackSlots:4},
 {fromDepth:9,toDepth:11,minimumCount:13,maximumCount:16,aliveCap:8,firstGroup:6,nextGroupMaximum:2,entryIntervalMs:1200,attackSlots:5}],
 perTargetAttackSlots:2,targetPreparationIntervalMs:600,entranceGraceMs:600,
 basicTrial:{rawDamage:24,periodMs:1600,preparationMs:500,recoveryMs:300},namedEnemyMappingApproved:false,
 executionRules:{aliveIncludesEntranceAndClimbing:true,skipFullEntryOpportunity:true,noCatchupEntries:true,combatClockOnly:true,entryGraceIsNotClimbingTime:true,slotCancellationDoesNotAdvanceReuse:true,targetGenerationLocked:true,completionRequiresPlannedAndLivingAndResidualZero:true,rewardCountIndependent:true}} as const;
export function bWavePressureDefinition(){return structuredClone(pressure);}
const json=(v:unknown)=>canonicalJson(v as Parameters<typeof canonicalJson>[0]);
const digest=(v:unknown)=>sha256Hex(json(v));
const fail=(code:string):never=>{throw new ActivityError(409,'combat_b_wave_'+code);};
const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u),nat=z.number().int().nonnegative().safe();
const reference=z.strictObject({templateId:id,version:id,templateHash:hash});
const template=z.strictObject({templateId:id,version:id,definitionId:id,definition:z.record(z.string(),z.json()),hp:operationRiskRawSchema.shape.hp,actions:z.array(operationRiskRawSchema.shape.damage).min(1).max(bWaveManifestLimits.actionsPerTemplate),templateHash:hash});
const wave=z.strictObject({nodeId:id,visitDepth:z.number().int().min(1).max(11),waveOrdinal:z.union([z.literal(1),z.literal(2)]),instances:z.array(z.strictObject({instanceId:id,ordinal:nat,templateRef:reference,entryGroup:nat,entryNotBeforeMs:nat})).min(1).max(bWaveManifestLimits.instancesPerWave)});
const manifestSchema=z.strictObject({version:z.literal(bWaveManifestVersion),contextId:id,templates:z.array(template).min(1).max(bWaveManifestLimits.templates),waves:z.array(wave).min(2).max(bWaveManifestLimits.waves)});
const departureOrigin=z.strictObject({accountId:id,runId:id,operationPreviewId:id,operationId:id,selectedRisk:z.enum(['I','II','III','IV','V']),departureVersion:id,baseSnapshotHash:hash,definitionVersion:id,planHash:hash,publicationHash:hash});
const origin=z.strictObject({departure:departureOrigin,contextId:id,profileRef:z.strictObject({id,version:id,executorVersion:id}),parentDefinitionHash:hash,parentRawProfilesHash:hash,riskApplicationVersion:z.literal(operationRiskVersion)});
const pinSchema=manifestSchema.extend({origin,pressurePolicy:z.json(),manifestHash:hash});
export type BWaveManifestPin=z.infer<typeof pinSchema>;
/** Internal source/executor ownership only. All template/manifest/support revisions MUST be
 * fenced in the SAME original departure batch. This port installs no production profile.
 */
export interface BWaveManifestSourceOwner{
 prepare(source:Readonly<BDepartureCombatSource>,context:Readonly<BDepartureCombatContext>):Promise<{manifest:unknown;assertions:readonly D1PreparedStatement[]}>;
 supports(pin:Readonly<BWaveManifestPin>,source:Readonly<BDepartureCombatSource>,context:Readonly<BDepartureCombatContext>):Promise<boolean>;
}
function bounded(input:unknown){if(new TextEncoder().encode(json(input)).byteLength>bWaveManifestLimits.utf8Bytes)return fail('capacity');}
async function captureManifest(input:unknown,plan:OperationMapPlan,contextId:string){
 bounded(input);const parsed=manifestSchema.safeParse(structuredClone(input));if(!parsed.success)return fail('manifest_invalid');const m=parsed.data;
 if(m.contextId!==contextId)return fail('context_conflict');
 const refs=new Map<string,typeof m.templates[number]>();
 for(const t of m.templates){if(refs.has(t.templateId)||BigInt(t.hp.value.numerator)<=0||new Set(t.actions.map(a=>a.sourceId)).size!==t.actions.length)return fail('template_invalid');const {templateHash,...base}=t;if(await digest(base)!==templateHash)return fail('template_hash_conflict');refs.set(t.templateId,t);}
 const instanceIds=new Set<string>(),waves=new Set<string>(),nodes=new Map<string,Set<number>>();
 let lastNode=-1,lastWave=0;
 for(const w of m.waves){const index=plan.nodes.findIndex(n=>n.nodeId===w.nodeId),node=plan.nodes[index];if(!node||node.eventKind!=='COMBAT'||node.visitDepth!==w.visitDepth)return fail('node_conflict');
  if(index<lastNode||index===lastNode&&w.waveOrdinal<=lastWave)return fail('wave_order');lastNode=index;lastWave=w.waveOrdinal;
  const key=json([w.nodeId,w.waveOrdinal]);if(waves.has(key))return fail('wave_duplicate');waves.add(key);const ordinals=nodes.get(w.nodeId)??new Set<number>();ordinals.add(w.waveOrdinal);nodes.set(w.nodeId,ordinals);
  const stage=pressure.stages.find(s=>w.visitDepth>=s.fromDepth&&w.visitDepth<=s.toDepth)!;if(w.instances.length<stage.minimumCount||w.instances.length>stage.maximumCount)return fail('count_conflict');
  const groups=new Map<number,number>();let previousGroup=0;
  for(const [index,i] of w.instances.entries()){if(instanceIds.has(i.instanceId)||i.ordinal!==index)return fail('instance_conflict');instanceIds.add(i.instanceId);
   const t=refs.get(i.templateRef.templateId);if(!t||t.version!==i.templateRef.version||t.templateHash!==i.templateRef.templateHash)return fail('reference_conflict');
   if(index===0&&i.entryGroup!==0||i.entryGroup<previousGroup||i.entryGroup>previousGroup+1||i.entryNotBeforeMs!==i.entryGroup*stage.entryIntervalMs)return fail('entry_conflict');previousGroup=i.entryGroup;groups.set(i.entryGroup,(groups.get(i.entryGroup)??0)+1);
  }
  if(groups.get(0)!==stage.firstGroup||[...groups].some(([g,n])=>g>0&&n>stage.nextGroupMaximum))return fail('entry_conflict');
 }
 if(nodes.size>bWaveManifestLimits.nodes||[...nodes.values()].some(w=>w.size!==2||!w.has(1)||!w.has(2)))return fail('node_capacity');return m;
}
const parentHeader=z.object({version:z.literal(bDepartureCombatSourceVersion),contextId:id,profile:z.object({id,version:id,executorVersion:id,definition:z.record(z.string(),z.json())}),riskApplicationVersion:z.literal(operationRiskVersion),rawProfiles:z.array(z.json()).min(1).max(128)});
function parentSource(input:unknown){const captured=structuredClone(input);if(!parentHeader.safeParse(captured).success)return fail('parent_invalid');return captured as BDepartureCombatSource;}
async function manifestOrigin(source:BDepartureCombatSource,context:BDepartureCombatContext){const definition=structuredClone(source.profile.definition);delete definition.bWaveManifest;
 return origin.parse({departure:context,contextId:source.contextId,profileRef:{id:source.profile.id,version:source.profile.version,executorVersion:source.profile.executorVersion},parentDefinitionHash:await digest(definition),parentRawProfilesHash:await digest(source.rawProfiles),riskApplicationVersion:source.riskApplicationVersion});
}
async function capturePin(input:unknown,source:BDepartureCombatSource,context:BDepartureCombatContext,plan:OperationMapPlan){bounded(input);const parsed=pinSchema.safeParse(structuredClone(input));if(!parsed.success)return fail('pin_invalid');const pin=parsed.data;
 const {origin:storedOrigin,pressurePolicy,manifestHash,...manifest}=pin;await captureManifest(manifest,plan,source.contextId);
 if(source.profile.definition.waveManifestVersion!==bWaveManifestVersion||await digest(plan)!==context.planHash||json(storedOrigin)!==json(await manifestOrigin(source,context))||json(pressurePolicy)!==json(pressure))return fail('pin_conflict');
 const {manifestHash:ignored,...payload}=pin;void ignored;if(await digest(payload)!==manifestHash)return fail('pin_hash_conflict');return pin;
}
/** Explicit opt-in S19 composition. Does not change S19 actorRef/rawProfiles semantics,
 * create a seed, sample an unapproved enemy pool, start a run or install support.
 * Pass this owner to the existing startDeparture eighth argument ONLY at an approved composition root.
 */
export function createBWaveDepartureSourceOwner(parent:BDepartureCombatSourceOwner,waveOwner:BWaveManifestSourceOwner|undefined,planInput:unknown):BDepartureCombatSourceOwner{
 const plan=captureOperationMap(structuredClone(planInput));
 return {prepare:async context=>{
  if(!waveOwner)return fail('source_unavailable');if(await digest(plan)!==context.planHash)return fail('plan_conflict');
  const resolved=await parent.prepare(structuredClone(context)),source=parentSource(resolved.source),parentAssertions=Array.from(resolved.assertions??[]);
  if(source.profile.definition.waveManifestVersion!==bWaveManifestVersion||Object.hasOwn(source.profile.definition,'bWaveManifest'))return fail('opt_in_required');
  if(!parentAssertions.length||parentAssertions.some(s=>typeof s.bind!=='function'))return fail('source_fence_missing');
  const proposal=await waveOwner.prepare(structuredClone(source),structuredClone(context)),assertions=Array.from(proposal.assertions??[]);
  if(!assertions.length||assertions.some(s=>typeof s.bind!=='function')||parentAssertions.length+assertions.length>128)return fail('source_fence_missing');
  const manifest=await captureManifest(proposal.manifest,plan,source.contextId),payload={...manifest,origin:await manifestOrigin(source,context),pressurePolicy:bWavePressureDefinition()},pin=pinSchema.parse({...payload,manifestHash:await digest(payload)});bounded(pin);
  return {source:{...source,profile:{...source.profile,definition:{...source.profile.definition,bWaveManifest:pin}}},assertions:[...parentAssertions,...assertions]};
 },supports:async(source,context)=>{
  if(!waveOwner)return false;const pin=await capturePin(source.profile.definition.bWaveManifest,source,context,plan);
  return await parent.supports(structuredClone(source),structuredClone(context))===true&&await waveOwner.supports(structuredClone(pin),structuredClone(source),structuredClone(context))===true;
 }};
}
/** Trusted original SQL/START bytes; untrusted consumers need independent expected
 * parent pin + manifest hashes. Restore never consults current templates/support or rerolls.
 */
export async function readBWaveManifest(snapshotJson:string,trust:{accountId:string;runId:string;expectedManifestHash?:string;expectedParentPinHash?:string}){
 const t=z.strictObject({accountId:id,runId:id,expectedManifestHash:hash.optional(),expectedParentPinHash:hash.optional()}).parse(structuredClone(trust));
 const parent=await readBDepartureCombatPin(snapshotJson,{accountId:t.accountId,runId:t.runId,...(t.expectedParentPinHash?{expectedPinHash:t.expectedParentPinHash}:{})});
 if(!parent){if(t.expectedParentPinHash!==undefined||t.expectedManifestHash!==undefined)return fail('pin_hash_conflict');return null;}const d=parent.source.profile.definition,marker=d.waveManifestVersion,input=d.bWaveManifest;
 if(marker===undefined&&(input===undefined||input===null)){if(t.expectedManifestHash!==undefined)return fail('pin_hash_conflict');return null;}
 const plan=readDepartureMap(JSON.parse(snapshotJson));if(!plan)return fail('plan_conflict');const pin=await capturePin(input,parent.source,parent.origin,plan);
 if(t.expectedManifestHash!==undefined&&pin.manifestHash!==t.expectedManifestHash)return fail('pin_hash_conflict');return pin;
}
export async function readBWaveManifestForRun(db:D1Database,accountId:string,runId:string){
 const parent=await readBDepartureCombatPinForRun(db,accountId,runId),row=await db.prepare('SELECT s.snapshot_json FROM run_snapshots s JOIN runs r ON r.id=s.run_id WHERE r.id=? AND r.account_id=?').bind(runId,accountId).first<{snapshot_json:string}>();
 if(!row||await sha256Hex(row.snapshot_json)!==parent.startSnapshotHash)return fail('stale');
 const manifest=await readBWaveManifest(row.snapshot_json,{accountId,runId,...(parent.pin?{expectedParentPinHash:parent.pin.pinHash}:{})});
 const payload={version:bWaveManifestVersion,accountId,runId,runStatus:parent.runStatus,startSnapshotHash:parent.startSnapshotHash,parentPinHash:parent.pin?.pinHash??null,status:manifest?'PINNED_MANIFEST' as const:'HISTORICAL_NO_MANIFEST' as const,manifest,canStart:false,newProfileAdmission:false,fullCombatSupported:false,manifestPinned:manifest!==null};
 const packet={...payload,packetHash:await digest(payload)},fenced=await readBDepartureCombatPinForRun(db,accountId,runId);if(fenced.packetHash!==parent.packetHash)return fail('stale');return packet;
}