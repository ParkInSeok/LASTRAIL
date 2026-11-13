import {z} from 'zod';
import {ActivityError} from './activity-policy';

const storedPolicy=z.strictObject({revision:z.number().int().positive().safe(),ads_enabled:z.union([z.literal(0),z.literal(1)])});
/** Read-only current environment policy; this is not a build/SDK availability grant. */
export async function readAdRuntimePolicy(db:D1Database,environment:string) {
  const row=await db.prepare('SELECT revision,ads_enabled FROM ad_runtime_policies WHERE environment=?').bind(environment).first();
  const parsed=storedPolicy.safeParse(row);
  return {accepted:true,contractVersion:'runtime-policy-v1' as const,environment,
    policyStatus:parsed.success?'CONFIGURED' as const:'UNKNOWN' as const,
    adsEnabled:parsed.success?parsed.data.ads_enabled===1:null,
    runtimePolicyRevision:parsed.success?parsed.data.revision:null,
    serverTime:Date.now()};
}
export async function requireNewAd(db:D1Database,environment:string):Promise<void> {
  const policy=await readAdRuntimePolicy(db,environment);
  if(policy.adsEnabled===null)throw new ActivityError(503,'ad_runtime_policy_unknown');
  if(!policy.adsEnabled)throw new ActivityError(409,'ads_disabled_by_policy');
}
