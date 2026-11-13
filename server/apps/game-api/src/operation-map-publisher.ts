import {captureMapGenerationRequest,generateOperationMap,mapGenerationRequestSchema,mapGenerationVersion} from "./operation-map-generator";
import {z} from "zod";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {captureOperationMap} from "./operation-map-plan";
import {mapFail,mapId} from "./operation-map-state";
const eventKind=z.enum(["COMBAT","ELITE","AUGMENT","EQUIPMENT","ENCHANT","MAINTENANCE","MERCHANT","SCOUT","BOSS"]);
const entry=z.strictObject({ref:mapId,version:mapId,profile:z.literal("ORDINARY_12_NODE"),eventKind,definition:z.record(z.string(),z.json())});
const catalogSchema=z.strictObject({version:z.literal("operation.map-catalog.v1"),catalogId:mapId,revision:z.number().int().positive().safe(),definitionVersion:mapId,encounters:z.array(entry).min(1).max(1000),rewards:z.array(entry).min(1).max(1000)});
export interface OperationMapCatalog {
 /** One consistent server-owned catalog snapshot. Adapter MUST resolve only definitions
  * supported by actual executors/reward validators, with their immutable versions.
  * Never implement this by echoing a client's catalog or a supported=true flag.
  * No production adapter is installed; generic definition validation is adapter-owned. */
 read():Promise<unknown>;
}
export type OperationMapCatalogEntry=z.infer<typeof entry>;
/** Server adapter bound to real execution and reward validation. The adapter must inspect
 * immutable definition payload/version, not a catalog-supported boolean or client echo.
 * No production implementation exists until both sides have actual runtime handlers. */
export interface OperationMapContentSupport {
 supportsEncounter(entry:Readonly<OperationMapCatalogEntry>):Promise<boolean>;
 supportsReward(entry:Readonly<OperationMapCatalogEntry>):Promise<boolean>;
}
/** Internal authored-plan publisher. Does not generate a map, enable admission, or prove gameplay.
 * Pins full resolved payloads: later catalog changes cannot replace this run's definitions.
 */
export class OperationMapPublisher {
 constructor(private readonly catalog?:OperationMapCatalog,private readonly support?:OperationMapContentSupport){}
 /** Server-owned seed and eligible candidates only. Validate ALL candidates before drawing;
  * one snapshot prevents a publication from mixing catalog revisions. */
 async generate(input:unknown){
  const request=captureMapGenerationRequest(input);
  if(request.version!==mapGenerationVersion)return mapFail("generation_version_retired");
  if(!this.catalog)return mapFail("catalog_unavailable");
  const parsed=catalogSchema.safeParse(await this.catalog.read());if(!parsed.success)return mapFail("catalog_invalid");const c=parsed.data;
  if(c.definitionVersion!==request.definitionVersion)return mapFail("definition_version_conflict");
  const encounters=new Map(c.encounters.map(e=>[e.ref,e])),rewards=new Map(c.rewards.map(e=>[e.ref,e]));
  if(encounters.size!==c.encounters.length||rewards.size!==c.rewards.length)return mapFail("catalog_invalid");
  for(const node of request.candidates){const e=encounters.get(node.encounterProfileRef),r=rewards.get(node.rewardPlanRef);
   if(!e||!r)return mapFail("content_unresolved");if(e.eventKind!==node.eventKind||r.eventKind!==node.eventKind)return mapFail("content_kind_conflict");}
  if(!this.support)return mapFail("support_unavailable");
  // Resolve every eligible definition, including candidates the seed will not draw.
  // This is a new-publication gate. Restore uses pinned bytes without today's handlers.
  const verifiedEncounters=new Set<string>(),verifiedRewards=new Set<string>();
  for(const candidate of request.candidates){
   const encounter=encounters.get(candidate.encounterProfileRef)!;
   const reward=rewards.get(candidate.rewardPlanRef)!;
   if(!verifiedEncounters.has(encounter.ref)){
    if(!await this.support.supportsEncounter(structuredClone(encounter)))return mapFail("content_unsupported");
    verifiedEncounters.add(encounter.ref);
   }
   if(!verifiedRewards.has(reward.ref)){
    if(!await this.support.supportsReward(structuredClone(reward)))return mapFail("content_unsupported");
    verifiedRewards.add(reward.ref);
   }
  }
  const generated=await generateOperationMap(request);
  const publication=await new OperationMapPublisher({read:async()=>c}).publish(generated.plan);
  const {contentHash,...base}=publication;void contentHash;
  const content={...base,version:"operation.map-content.v2" as const,generation:generated.generation};
  return {...content,contentHash:await sha256Hex(canonicalJson(content))};
 }
 async publish(input:unknown){
  const plan=captureOperationMap(input);
  if(!this.catalog)return mapFail("catalog_unavailable");
  const parsed=catalogSchema.safeParse(await this.catalog.read());if(!parsed.success)return mapFail("catalog_invalid");
  const c=parsed.data;
  if(c.definitionVersion!==plan.definitionVersion)return mapFail("definition_version_conflict");
  const encounters=new Map(c.encounters.map(e=>[e.ref,e])),rewards=new Map(c.rewards.map(e=>[e.ref,e]));
  if(encounters.size!==c.encounters.length||rewards.size!==c.rewards.length)return mapFail("catalog_invalid");
  const usedEncounters=new Set<string>(),usedRewards=new Set<string>();
  const bindings=plan.nodes.map(node=>{
   const encounter=encounters.get(node.encounterProfileRef),reward=rewards.get(node.rewardPlanRef);
   if(!encounter||!reward)return mapFail("content_unresolved");
   if(encounter.eventKind!==node.eventKind||reward.eventKind!==node.eventKind)return mapFail("content_kind_conflict");
   usedEncounters.add(encounter.ref);usedRewards.add(reward.ref);
   return {nodeId:node.nodeId,encounterRef:encounter.ref,encounterVersion:encounter.version,rewardRef:reward.ref,rewardVersion:reward.version};
  });
  const content={version:"operation.map-content.v1" as const,catalogId:c.catalogId,catalogRevision:c.revision,definitionVersion:c.definitionVersion,plan,bindings,
   encounters:[...usedEncounters].sort().map(ref=>encounters.get(ref)!),rewards:[...usedRewards].sort().map(ref=>rewards.get(ref)!)};
  return {...content,contentHash:await sha256Hex(canonicalJson(content))};
 }
}

const publishedSchema=z.strictObject({version:z.literal("operation.map-content.v1"),catalogId:mapId,catalogRevision:z.number().int().positive().safe(),definitionVersion:mapId,plan:z.json(),
 bindings:z.array(z.strictObject({nodeId:mapId,encounterRef:mapId,encounterVersion:mapId,rewardRef:mapId,rewardVersion:mapId})).min(1).max(78),
 encounters:z.array(entry).min(1).max(1000),rewards:z.array(entry).min(1).max(1000),contentHash:z.string().regex(/^[a-f0-9]{64}$/u)});
/** Validate a SERVER-OWNED pinned publication, not a client capability or proof of support.
 * Rebuilding checks every binding, referenced payload and hash using the same publication rules.
 * This intentionally does not fetch today's catalog when restoring an original run.
 */
export async function capturePublishedOperationMap(input:unknown,expectedPlan:unknown){
 if(input && typeof input==="object" && "version" in input && input.version==="operation.map-content.v2"){
  const parsed=generatedPublishedSchema.safeParse(input);if(!parsed.success)return mapFail("content_invalid");const p=parsed.data;
  const generated=await generateOperationMap(p.generation.request);
  if(canonicalJson(generated.generation)!==canonicalJson(p.generation)||canonicalJson(generated.plan)!==canonicalJson(p.plan))return mapFail("content_invalid");
  const publication=await new OperationMapPublisher({read:async()=>({version:"operation.map-catalog.v1",catalogId:p.catalogId,revision:p.catalogRevision,definitionVersion:p.definitionVersion,encounters:p.encounters,rewards:p.rewards})}).publish(p.plan);
  const {contentHash,...base}=publication;void contentHash;
  const content={...base,version:"operation.map-content.v2" as const,generation:p.generation};
  const rebuilt={...content,contentHash:await sha256Hex(canonicalJson(content))};
  if(canonicalJson(rebuilt)!==canonicalJson(p))return mapFail("content_invalid");
  if(canonicalJson(rebuilt.plan)!==canonicalJson(captureOperationMap(expectedPlan)))return mapFail("content_plan_conflict");return rebuilt;
 }
 const parsed=publishedSchema.safeParse(input);if(!parsed.success)return mapFail("content_invalid");const p=parsed.data;
 const rebuilt=await new OperationMapPublisher({read:async()=>({version:"operation.map-catalog.v1",catalogId:p.catalogId,revision:p.catalogRevision,definitionVersion:p.definitionVersion,encounters:p.encounters,rewards:p.rewards})}).publish(p.plan);
 if(canonicalJson(rebuilt)!==canonicalJson(p))return mapFail("content_invalid");
 if(canonicalJson(rebuilt.plan)!==canonicalJson(captureOperationMap(expectedPlan)))return mapFail("content_plan_conflict");
 return rebuilt;
}

const generatedPublishedSchema=publishedSchema.extend({version:z.literal("operation.map-content.v2"),generation:z.strictObject({request:mapGenerationRequestSchema,attemptsPerDepth:z.array(z.number().int().min(1).max(256)).length(11)})});

/** Revalidate the original private publication when it exists. Older authored snapshots
 * without a content envelope remain readable; only a new publication can claim support. */
export async function verifyPinnedOperationMapContent(snapshot:unknown,expectedPlan:unknown){
 if(!snapshot||typeof snapshot!=="object")return mapFail("content_invalid");
 if(Object.hasOwn(snapshot,"operationMapContent"))
  await capturePublishedOperationMap((snapshot as {operationMapContent:unknown}).operationMapContent,expectedPlan);
}
