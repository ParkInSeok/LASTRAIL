import {afterEach,expect,it} from 'vitest';
import {createAugmentSelectFixture} from '../helpers/augment-select-fixture';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import app from '../../apps/game-api/src/index';
const clean:(()=>void)[]=[];afterEach(()=>{for(const close of clean.splice(0))close();});
async function fixture(arrived=true){
 const f=await createAugmentSelectFixture(arrived,'O22',false);clean.push(()=>f.sql.close());
 const store=new SoloAugmentStore(f.env.DB,()=>0);
 const initial=await store.issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId:f.runId,eventId:'grant',stageId:'n1-1',catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['O22','O23','T05','T16']});
 const body={version:'combat.augment-reroll.v1.20261021',command:{eventId:'grant',expectedRevision:0,slotIndex:0,candidateId:'O22',mode:'FREE'}};
 const url=f.url.replace(/select$/u,'reroll');
 return {...f,store,initial,rollBody:body,rollUrl:url,roll:(value:object=body,key='roll')=>f.post(url,value,key)};
}
it('rerolls only the addressed card once and returns complete replacement definitions with original-key recovery',async()=>{
 const f=await fixture(),response=await f.roll();expect(response.status).toBe(200);const result=await response.json();
 expect(result.result).toMatchObject({outcome:'REROLLED',snapshot:{grantRevision:1,state:'SELECTING',selectedCandidateId:null}});
 expect(result.result.snapshot.slots[0]).toMatchObject({candidateId:'T16',freeRemaining:2,extraRemaining:1});
 expect(result.result.snapshot.slots.slice(1)).toEqual(f.initial.slots.slice(1));
 expect(result.targetReadout).toMatchObject({grantRevision:1,cards:[{candidateId:'T16',scope:'TRAIN',targets:null},{candidateId:'O23'},{candidateId:'T05'}]});
 expect(await(await f.roll()).json()).toEqual(result);
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(1);
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
 for(const expectedRevision of [0,1]){const stale=await f.select({...f.body,command:{...f.body.command,expectedRevision}},'stale'+expectedRevision);expect(stale.status).toBe(409);}
 expect((await f.store.read({accountId:'a',environment:'DEV'},f.runId,'grant')).state).toBe('SELECTING');
});
it('rejects unauthenticated, unarrived, pending-ad and extra requests without spending',async()=>{
 const f=await fixture(false);expect((await app.request(f.rollUrl,{method:'POST'},f.env)).status).toBe(401);
 expect((await f.roll()).status).toBe(409);expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(0);
 const arrived=await fixture();expect((await arrived.roll({...arrived.rollBody,command:{...arrived.rollBody.command,mode:'EXTRA'}})).status).toBe(409);
 arrived.sql.run("INSERT INTO augment_ad_sessions VALUES('intent',?,'grant','{}',2000,'AD_PENDING')",[arrived.runId]);
 expect((await arrived.roll()).status).toBe(409);expect(await arrived.store.read({accountId:'a',environment:'DEV'},arrived.runId,'grant')).toEqual(arrived.initial);
});
it('serializes competing SELECT and REROLL without replacement selection or double consumption',async()=>{
 const f=await fixture();const responses=await Promise.all([f.roll(),f.select()]);expect(responses.map(r=>r.status).sort()).toEqual([200,409]);
 const snapshot=await f.store.read({accountId:'a',environment:'DEV'},f.runId,'grant');
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(1);
 if(snapshot.state==='CONFIRMED'){expect(snapshot.selectedCandidateId).toBe('O22');expect(snapshot.slots[0]?.freeRemaining).toBe(3);}
 else{expect(snapshot.slots[0]?.candidateId).toBe('T16');expect(snapshot.slots[0]?.freeRemaining).toBe(2);}
});
it('enforces the per-card three-free limit and rejects caller approval fields',async()=>{
 const f=await fixture();let snapshot=f.initial;
 expect((await f.roll({...f.rollBody,approval:'PURCHASE_ENTITLEMENT'})).status).toBe(400);
 for(let n=0;n<3;n++){
  const response=await f.roll({...f.rollBody,command:{...f.rollBody.command,expectedRevision:snapshot.grantRevision,candidateId:snapshot.slots[0]!.candidateId}},'free'+n);
  expect(response.status).toBe(200);snapshot=(await response.json()).result.snapshot;
 }
 const exhausted=await f.roll({...f.rollBody,command:{...f.rollBody.command,expectedRevision:snapshot.grantRevision,candidateId:snapshot.slots[0]!.candidateId}},'free4');
 expect(exhausted.status).toBe(409);expect(await exhausted.json()).toMatchObject({errorCode:'augment_selection_free_exhausted'});
 expect(snapshot.slots.map(s=>s.freeRemaining)).toEqual([0,3,3]);expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(3);
});
it('fences node changes at commit and recovers a lost reroll acknowledgement by original key',async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run('UPDATE operation_map_states SET revision=revision+1');};
 expect((await f.roll()).status).toBe(409);expect(await f.store.read({accountId:'a',environment:'DEV'},f.runId,'grant')).toEqual(f.initial);
 const batch=f.env.DB.batch.bind(f.env.DB);let lost=true;f.env.DB.batch=async statements=>{const r=await batch(statements);if(lost){lost=false;throw Error('lost reroll ACK');}return r;};
 const response=await f.roll();expect(response.status).toBe(200);const original=await response.json();
 f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await(await f.roll()).json()).toEqual(original);
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(1);
});
it.each(['REROLL','SELECT'])('fences an ad that starts after the %s pending check but before commit',async operation=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;
  f.sql.run("INSERT INTO augment_ad_sessions VALUES('racing-ad',?,'grant','{}',2000,'AD_PENDING')",[f.runId]);};
 const response=await (operation==='REROLL'?f.roll():f.select());expect(response.status).toBe(409);
 expect(await f.store.read({accountId:'a',environment:'DEV'},f.runId,'grant')).toEqual(f.initial);
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(0);expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
 expect(f.scalar("SELECT status FROM augment_ad_sessions WHERE id='racing-ad'")).toBe('AD_PENDING');
});
