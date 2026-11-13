import {canonicalJson} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readDisclosureSource,disclosureSourcePredicate} from './probability-disclosure-source';
import {readProbabilityDisclosure} from './probability-disclosure-store';
import {parseOfflineRewardPolicy} from './offline-ad-policy';
import {parseCombatRewardPolicy} from './combat-ad-policy';
export type RewardDisclosurePinRow={probability_disclosure_id:string|null;probability_source_digest:string|null};
export function rewardDisclosureView(row:RewardDisclosurePinRow){
 if(row.probability_disclosure_id===null&&row.probability_source_digest===null)
  return {status:'LEGACY_UNLINKED' as const,disclosureId:null,sourceDigest:null,publicPath:null};
 if(!row.probability_disclosure_id||!row.probability_source_digest)throw new ActivityError(503,'reward_disclosure_invalid');
 return {status:'PINNED' as const,disclosureId:row.probability_disclosure_id,sourceDigest:row.probability_source_digest,
  publicPath:'/v1/public/probability-disclosures/'+row.probability_disclosure_id};
}
export async function prepareRewardDisclosure(db:D1Database,environment:string,consumer:'OFFLINE'|'COMBAT',input:unknown,origin:'AD_REQUEST'|'PURCHASE_ONLY'){
 if(environment!=='DEV'&&environment!=='PRD')throw new ActivityError(503,'reward_disclosure_unavailable');
 const parse=consumer==='OFFLINE'?parseOfflineRewardPolicy:parseCombatRewardPolicy;
 const policy=parse(input),sourceKind=origin==='AD_REQUEST'?'AD_POLICY':'PURCHASE_POLICY';
 try{
  const source=await readDisclosureSource(db,environment,consumer,policy.version,sourceKind);
  if(!source.publishable)throw new ActivityError(503,'reward_disclosure_unavailable');
  if(canonicalJson(parse(JSON.parse(source.rawConfig)))!==canonicalJson(policy))throw new ActivityError(409,'reward_disclosure_policy_changed');
  const published=await readProbabilityDisclosure(db,environment,source.disclosureId);
  if(!published)throw new ActivityError(503,'reward_disclosure_unavailable');
  const condition=disclosureSourcePredicate({environment,consumer,sourceKind,policyVersion:policy.version,
    expectedSourceDigest:source.projection.sourceDigest,reason:'internal readiness'},source.rawConfig);
  return {disclosureId:source.disclosureId,sourceDigest:source.projection.sourceDigest,
   guard:db.prepare('INSERT INTO reward_disclosure_guard SELECT CASE WHEN EXISTS('+condition.sql+') THEN 1 ELSE 0 END').bind(...condition.bindings)};
 }catch(error){
  if(error instanceof ActivityError)throw error;
  throw new ActivityError(503,'reward_disclosure_unavailable');
 }
}
