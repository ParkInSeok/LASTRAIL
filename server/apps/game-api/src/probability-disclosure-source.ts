import {sha256Hex} from '@lastrail/common';
import {ProbabilityDisclosureError as Failure,disclosureProjectionSchema,disclosureId,percentage,type PublishDisclosure} from '../../../packages/common/src/probability-disclosure-policy';
import {parseOfflineRewardPolicy} from './offline-ad-policy';
import {parseCombatRewardPolicy} from './combat-ad-policy';
import {parsePurchaseRewardPolicy} from './purchase-reward-policy';

function adSourceTable(consumer:PublishDisclosure['consumer']){
  return consumer==='OFFLINE'?'offline_ad_policies':'combat_ad_policies';
}
/** Both the atomic guard and failure classification use the exact same source predicate. */
export function disclosureSourcePredicate(command:PublishDisclosure,rawConfig:string){
  if(command.sourceKind==='PURCHASE_POLICY')return {
    sql:'SELECT 1 FROM purchase_reward_policies WHERE environment=? AND kind=? AND config_json=?',
    bindings:[command.environment,command.consumer,rawConfig],
  };
  return {
    sql:`SELECT 1 FROM ${adSourceTable(command.consumer)} WHERE environment=? AND version=? AND status='ACTIVE' AND config_json=?`,
    bindings:[command.environment,command.policyVersion,rawConfig],
  };
}
export async function readDisclosureSource(db:D1Database,environment:PublishDisclosure['environment'],consumer:PublishDisclosure['consumer'],version:string,sourceKind:NonNullable<PublishDisclosure['sourceKind']>='AD_POLICY'){
  const statement=sourceKind==='PURCHASE_POLICY'
    ?db.prepare("SELECT NULL AS version,environment,'CONFIGURED' AS status,config_json FROM purchase_reward_policies WHERE environment=? AND kind=?").bind(environment,consumer)
    :db.prepare(`SELECT version,environment,status,config_json FROM ${adSourceTable(consumer)} WHERE version=? AND environment=?`).bind(version,environment);
  const row=await statement.first<{version:string|null;environment:string;status:string;config_json:string}>();
  if(!row)throw new Failure(404,'probability_source_not_found');
  try{
    if(row.config_json.length>16384||!['ACTIVE','RETIRED','CONFIGURED'].includes(row.status))throw new Error('invalid source');
    const value=JSON.parse(row.config_json);
    const policy=sourceKind==='PURCHASE_POLICY'?parsePurchaseRewardPolicy(value,environment,consumer)
      :consumer==='OFFLINE'?parseOfflineRewardPolicy(value):parseCombatRewardPolicy(value);
    if(sourceKind==='PURCHASE_POLICY'&&policy.version!==version)throw new Failure(404,'probability_source_not_found');
    if(policy.environment!==row.environment||(sourceKind==='AD_POLICY'&&policy.version!==row.version))throw new Error('invalid source');
    const projection=disclosureProjectionSchema.parse({environment,consumer,policyVersion:version,
      scope:'MULTIPLIER_DISTRIBUTION',
      ...(sourceKind==='PURCHASE_POLICY'?{publicSchemaVersion:2,sourceKind}:{publicSchemaVersion:1}),
      sourceDigest:'sha256:'+await sha256Hex(row.config_json),
      distribution:policy.weightsBps.map((n,i)=>({multiplier:i+2,numerator:n,denominator:10000,percentage:percentage(n)}))});
    return {projection,disclosureId:await disclosureId(projection),status:row.status,
      publishable:sourceKind==='PURCHASE_POLICY'||row.status==='ACTIVE',rawConfig:row.config_json};
  }catch(error){
    if(error instanceof Failure)throw error;
    throw new Failure(503,'probability_source_invalid');
  }
}
