import {it,expect} from 'vitest';
import {capturePierceLocalTrialPolicy,readPierceLocalTrialPolicy} from '../../apps/game-api/src/operator-pierce-trial-policy';
it('publishes a separate LOCAL policy without modifying the old effect definition or inventing critical chance',async()=>{
 const policy=await capturePierceLocalTrialPolicy();
 expect(await readPierceLocalTrialPolicy(JSON.parse(JSON.stringify(policy)))).toEqual(policy);
 expect(policy.profile).toBe('mira-pierce-comparison-local.v1.20261016');
 expect(policy.research.status).toBe('NOT_APPLICABLE');
 expect(policy.critical).toEqual({status:'NOT_APPLICABLE',chanceBps:null,rngDraws:0});
 expect(policy.groups.map(g=>[g.key,g.pierceSource,g.en17CoefficientBps])).toEqual([
  ['A','VERIFIED_ABSENT',null],['B','VERIFIED_FORMAL_OWNED_SELECTION',null],
  ['C','VERIFIED_ABSENT',2500],['D','VERIFIED_FORMAL_OWNED_SELECTION',2500]]);
 expect(policy.formalAugmentId).toBeNull();expect(policy.canStart).toBe(false);
});
it.each(['DEV','CRIT','ID','OLD_PROFILE'])('rejects rehashed %s policy drift',async kind=>{
 const policy=await capturePierceLocalTrialPolicy() as unknown as Record<string,unknown>;
 if(kind==='DEV')policy.environment='DEV';if(kind==='CRIT')policy.critical={status:'NOT_APPLICABLE',chanceBps:0,rngDraws:0};
 if(kind==='ID')policy.formalAugmentId='O21';if(kind==='OLD_PROFILE')policy.profile='mira-equipped-multishot-local.v1.20261015';
 const {policyHash:_,...body}=policy;void _;const {canonicalJson,sha256Hex}=await import('@lastrail/common');policy.policyHash=await sha256Hex(canonicalJson(body));
 await expect(readPierceLocalTrialPolicy(policy)).rejects.toThrow('operator_pierce_trial_policy_mismatch');
});
