import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import type {BCombatExecutableAdapter} from './combat-runtime-registry';
import type {BDepartureCombatSource,BDepartureCombatContext} from './combat-b-departure-combat-pin';
import {prepareBNativeNodeOrigin} from './combat-b-native-node-source';
import {bNamedOperatorDefinition} from './combat-b-named-operator-source';
import {captureOperationMap} from './operation-map-plan';
import {captureAugmentUtilityDefinitions} from './augment-utility-definition';

export const bNativeO22ExecutorVersion='combat.b-native-instant-basic-o22.v1.20261019';
// Explicit Unity CombatUtilityCatalogVersions contract; preserve legacy pins.
// New definition versions/hashes require consumer review, never a string upgrade.
export const bNativeO22ConsumerCatalogVersion='augment.utility-definitions.v7.20261019';
export const bNativeO22CurrentConsumerCatalogVersion='augment.utility-definitions.v8.20261020';
const currentConsumerCatalogHash='5eb2c9d669f7fb43ca5955371ced5d9d6581df1b533ecf35049dfc0d773f1813';
const currentConsumerO22Hash='2e089220f84434bd20da82d2877fd2cd02ec82a8fd60c1ef27076c4eed20e5c0';
const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const profileSchema=z.strictObject({id,version:id,executorVersion:z.literal(bNativeO22ExecutorVersion),definition:z.record(z.string(),z.json())});
const compositionSchema=z.strictObject({mode:z.literal('ORDINARY_SOLO'),factory:z.literal('BDirectAttackBasicLoop.CreateOrdinaryTravelEventRuntime'),activeOwner:z.null(),miraFirst:z.null(),healingModifiers:z.array(z.never()).length(0),operatorReplacement:z.literal(false),revival:z.literal(false),
 operators:z.array(z.strictObject({operatorId:z.literal('op_kyle'),actorRef:id,attackKind:z.literal('INSTANT_BASIC'),hpSourceId:id,actionSourceId:id})).length(1)});
const fenceKinds=['profile','rawSources','formation','geometry','nativeSeed','execution'] as const;
type FenceKind=typeof fenceKinds[number];
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const digest=(value:unknown)=>sha256Hex(json(value));
const fail=(reason:string):never=>{throw new ActivityError(409,'combat_b_native_o22_'+reason);};

/** Required actual original producer, NOT request JSON or a client capability.
 * It must validate complete effective/raw profiles, every published candidate's
 * executable support, original named placement/geometry and real native factory
 * composition. Each dependency is fenced in original START's transaction.
 * Existing named sources with effectiveStats=null/nativeSupported=false do not
 * implement this interface. No such producer is fabricated by this module. */
export interface BNativeO22OriginalSourceOwner {
 prepare(context:Readonly<BDepartureCombatContext>):Promise<{
  capture:{source:unknown;context:unknown;plan:unknown;namedDefinition:unknown;composition:unknown};
  assertions:Record<FenceKind,readonly D1PreparedStatement[]>;
 }>;
}

/** Specific executable boundary supported by Unity's new real HP/journal code.
 * Missing independent producer is an error, never a constant support proof.
 * This validates Kyle instant-basic only, not delayed operator projectiles,
 * activeOwner, Mira no-effect, modifiers, replacement, revival or other modes. */
export async function createBNativeO22ExecutorAdapter(originalProfile:unknown,owner?:BNativeO22OriginalSourceOwner):Promise<BCombatExecutableAdapter>{
 const profile=profileSchema.parse(structuredClone(originalProfile));
 const catalog=z.object({catalogVersion:id,complete:z.literal(true)}).parse(profile.definition.runtimeAugmentCatalog);
 if(catalog.catalogVersion===bNativeO22CurrentConsumerCatalogVersion){
  const definitions=await captureAugmentUtilityDefinitions();
  if(definitions.version!==catalog.catalogVersion||definitions.definitionHash!==currentConsumerCatalogHash
   ||definitions.definitions.find(d=>d.id==='O22')?.definitionHash!==currentConsumerO22Hash)return fail('consumer_definition_conflict');
 }else if(catalog.catalogVersion!==bNativeO22ConsumerCatalogVersion)return fail('consumer_catalog_unsupported');
 const profileHash=await digest(profile);
 return {profileId:profile.id,profileVersion:profile.version,executorVersion:profile.executorVersion,profileHash,
  prepare:async(source:Readonly<BDepartureCombatSource>,context:Readonly<BDepartureCombatContext>)=>{
   if(!owner)return fail('original_source_unavailable');
   const p=await owner.prepare(structuredClone(context));
   if(json(source.profile)!==json(profile)||json(source)!==json(p.capture.source)||json(context)!==json(p.capture.context))return fail('original_conflict');
   const b=source.binding;
   if(b.accountId!==context.accountId||b.runId!==context.runId||b.operationPreviewId!==context.operationPreviewId||b.operationId!==context.operationId||b.selectedRisk!==context.selectedRisk||b.operationDefinitionVersion!==context.definitionVersion||b.operationPlanHash!==context.planHash||b.operationPublicationHash!==context.publicationHash||source.contextId===context.runId)return fail('original_conflict');
   const plan=captureOperationMap(p.capture.plan);
   if(await digest(plan)!==context.planHash||plan.definitionVersion!==context.definitionVersion)return fail('plan_conflict');
   const native=z.object({seed:hash}).parse(source.profile.definition.bNativeNodeOrigin);
   if(json(await prepareBNativeNodeOrigin(context,source.contextId,native.seed,plan))!==json(source.profile.definition.bNativeNodeOrigin))return fail('native_origin_conflict');
   if(json(await bNamedOperatorDefinition())!==json(p.capture.namedDefinition))return fail('named_definition_conflict');
   const composition=compositionSchema.parse(p.capture.composition);
   for(const actor of composition.operators){
    const raw=source.rawProfiles.find(r=>r.actorRef===actor.actorRef);
    if(!raw||raw.hp.sourceId!==actor.hpSourceId||!raw.actions.some(a=>a.sourceId===actor.actionSourceId)||raw.hp.riskAppliedVersion!==null||raw.actions.some(a=>a.riskAppliedVersion!==null||a.progressAppliedVersion!==null))return fail('raw_source_conflict');
   }
   const assertions:D1PreparedStatement[]=[];
   for(const kind of fenceKinds){const guards=p.assertions[kind];if(!guards?.length||guards.some(g=>typeof g?.bind!=='function'))return fail(kind+'_fence_missing');assertions.push(...guards);}
   if(assertions.length>120)return fail('fence_limit');
   return assertions;
  }};
}

/** Actual registry-composition lookup. No record means no adapter; an existing
 * record with the new executor must satisfy its exact consumer version. The
 * independent original producer remains mandatory when prepare is called.
 * The outer CombatRuntimeRegistry owns source-row CAS and original-preview auth. */
export async function loadPersistedBNativeO22Executors(db:D1Database,environment:string,accountId:string,previewId:string,owner?:BNativeO22OriginalSourceOwner):Promise<readonly BCombatExecutableAdapter[]>{
 for(const value of [environment,accountId,previewId])id.parse(value);
 const row=await db.prepare(`SELECT c.source_json,c.source_hash,c.profile_hash FROM combat_runtime_sources c
  JOIN operation_previews p ON p.id=c.preview_id AND p.account_id=c.account_id JOIN accounts a ON a.id=c.account_id
  WHERE c.environment=? AND c.account_id=? AND c.preview_id=? AND c.enabled=1 AND p.status='ACTIVE' AND a.status='ACTIVE'`)
  .bind(environment,accountId,previewId).first<{source_json:string;source_hash:string;profile_hash:string}>();
 if(!row)return [];
 const source=z.object({binding:z.object({accountId:id,operationPreviewId:id}),profile:z.object({executorVersion:id}).passthrough()}).passthrough().parse(JSON.parse(row.source_json));
 if(await sha256Hex(row.source_json)!==row.source_hash||await digest(source.profile)!==row.profile_hash||source.binding.accountId!==accountId||source.binding.operationPreviewId!==previewId)return fail('registry_conflict');
 if(source.profile.executorVersion!==bNativeO22ExecutorVersion)return [];
 return [await createBNativeO22ExecutorAdapter(source.profile,owner)];
}
