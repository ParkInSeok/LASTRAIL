import {afterEach,expect,it} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {CombatDamageStore} from '../../apps/game-api/src/combat-damage-store';
import {captureLifestealDamageEvidence} from '../../apps/game-api/src/lifesteal-damage-evidence';
const cleanups:(()=>void)[]=[];afterEach(()=>{for(const close of cleanups.splice(0))close();});
async function fixture(){const f=await setup(true);cleanups.push(()=>f.sql.close());const q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();
 const store=new CombatDamageStore(f.env.DB);await store.initialize('a',r.runId,[{id:'eve',hp:10,maxHp:10},{id:'enemy',hp:100,maxHp:100}],{operators:[{actorId:'eve',expanded:false}],enemyTargetIds:['enemy']});
 return {...f,store,runId:r.runId};}
const hit=(eventId='hit',expectedRevision=1)=>({eventId,expectedRevision,targetId:'enemy',sourceActorId:'eve',actionId:eventId,hitOrdinal:0,sourceKind:'SUMMON',damage:{base:{numerator:'1000',denominator:'1'},effects:[],defenseBps:0,incomingReductionBps:0}});
it('projects original committed loss without overkill, writes, or treating actor charge as full attribution proof',async()=>{
 const f=await fixture(),receipt=await f.store.commitOperatorHit('a',f.runId,hit()),before=Buffer.from(f.sql.export());
 const context={accountId:'a',runId:f.runId};const evidence=await captureLifestealDamageEvidence(context,receipt);
 expect(evidence).toMatchObject({eventId:'hit',targetId:'enemy',confirmedHpLoss:100,recordedSourceActorId:'eve',
  attributionStatus:'RECORDED_ACTOR_REQUIRES_LOGICAL_OWNER_PROOF',canApplyHealing:false,receipt});
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 expect(await captureLifestealDamageEvidence(context,await f.store.commitOperatorHit('a',f.runId,hit()))).toEqual(evidence);
 const zero=await captureLifestealDamageEvidence(context,await f.store.commitOperatorHit('a',f.runId,hit('later',2)));
 expect(zero).toMatchObject({confirmedHpLoss:0,canApplyHealing:false});
});
it('does not reinterpret missing attribution as legitimately shared damage',async()=>{
 const f=await fixture();const receipt=await f.store.commit('a',f.runId,{eventId:'raw',expectedRevision:1,targetId:'enemy',hpLoss:1});
 expect(await captureLifestealDamageEvidence({accountId:'a',runId:f.runId},receipt)).toMatchObject({recordedSourceActorId:null,
  attributionStatus:'MISSING_ATTRIBUTION_NOT_SHARED_PROOF',canApplyHealing:false});
});
it('rejects malformed loss and client-added ownership flags instead of approving healing',async()=>{
 const f=await fixture(),receipt=await f.store.commitOperatorHit('a',f.runId,hit()),context={accountId:'a',runId:f.runId};
 await expect(captureLifestealDamageEvidence(context,{...receipt,hpLoss:101})).rejects.toThrow();
 await expect(captureLifestealDamageEvidence(context,{...receipt,hasO22:true})).rejects.toThrow();
 await expect(captureLifestealDamageEvidence({...context,operatorId:'other'},receipt)).rejects.toThrow();
});
