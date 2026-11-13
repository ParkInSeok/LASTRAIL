import {ProbabilityDisclosureError} from '../../../packages/common/src/probability-disclosure-policy';
import {readDisclosureSource,disclosureSourcePredicate} from '../../game-api/src/probability-disclosure-source';
import {readProbabilityDisclosure} from '../../game-api/src/probability-disclosure-store';
type Consumer='OFFLINE'|'COMBAT';
type State='NO_ACTIVE_POLICY'|'UNPUBLISHED'|'PUBLISHED'|'INVALID_SOURCE'|'INVALID_DISCLOSURE'|'SOURCE_CHANGED'|'UNAVAILABLE';
type Item={consumer:Consumer;sourceKind:'AD_POLICY';state:State;policyVersion:string|null;sourceDigest:string|null;disclosureId:string|null;publicPath:string|null};
const empty=(consumer:Consumer,state:State):Item=>({consumer,sourceKind:'AD_POLICY',state,policyVersion:null,sourceDigest:null,disclosureId:null,publicPath:null});
async function inspectPublication(db:D1Database,environment:'DEV'|'PRD',consumer:Consumer):Promise<Item>{
 try{
  const table=consumer==='OFFLINE'?'offline_ad_policies':'combat_ad_policies';
  const active=await db.prepare("SELECT version FROM "+table+" WHERE environment=? AND status='ACTIVE'").bind(environment).first<{version:string}>();
  if(!active)return empty(consumer,'NO_ACTIVE_POLICY');
  let source:Awaited<ReturnType<typeof readDisclosureSource>>;
  try{source=await readDisclosureSource(db,environment,consumer,active.version);}
  catch(error){
   if(error instanceof ProbabilityDisclosureError){
    if(error.code==='probability_source_not_found')return empty(consumer,'SOURCE_CHANGED');
    if(error.code==='probability_source_invalid')return empty(consumer,'INVALID_SOURCE');
   }
   throw error;
  }
  if(!source.publishable)return empty(consumer,'SOURCE_CHANGED');
  let published:Awaited<ReturnType<typeof readProbabilityDisclosure>>;
  try{published=await readProbabilityDisclosure(db,environment,source.disclosureId);}
  catch(error){
   if(error instanceof ProbabilityDisclosureError&&error.code==='probability_disclosure_storage_invalid')return empty(consumer,'INVALID_DISCLOSURE');
   throw error;
  }
  const current=disclosureSourcePredicate({environment,consumer,policyVersion:active.version,expectedSourceDigest:source.projection.sourceDigest,reason:'read-only inspection'},source.rawConfig);
  if(!await db.prepare(current.sql).bind(...current.bindings).first())return empty(consumer,'SOURCE_CHANGED');
  return {...empty(consumer,published?'PUBLISHED':'UNPUBLISHED'),policyVersion:source.projection.policyVersion,
   sourceDigest:source.projection.sourceDigest,disclosureId:published?source.disclosureId:null,
   publicPath:published?'/v1/public/probability-disclosures/'+source.disclosureId:null};
 }catch{return empty(consumer,'UNAVAILABLE');}
}
/** Advisory publication check only; never grants SDK/provider/privacy/store approval or changes the runtime policy. */
export async function readAdDisclosureReadiness(db:D1Database,environment:'DEV'|'PRD'){
 const items=await Promise.all((['OFFLINE','COMBAT'] as const).map(consumer=>inspectPublication(db,environment,consumer)));
 return {contractVersion:'admin-ad-disclosure-readiness-v1',scope:'PUBLICATION_ONLY',environment,checkedAt:Date.now(),
  canEnable:false,enableBlockReason:'ad_enable_readiness_unavailable',items};
}
