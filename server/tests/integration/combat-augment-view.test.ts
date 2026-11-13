import {it,expect} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {RunAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {CombatHostReportStore} from '../../apps/game-api/src/combat-host-report-store';
import {captureCombatAuthority} from '../../apps/game-api/src/combat-authority';
import {readAugmentView} from '../../apps/game-api/src/combat-augment-view';
import app from '../../apps/game-api/src/index';
it.each(['runId','eventId','stageId','catalogVersion','accountId','environment'])('rejects mismatched stored %s instead of displaying a different original grant',async field=>{
 const f=await fixture();try{
  const checkpoint=JSON.parse(String(f.scalar('SELECT checkpoint_json FROM solo_augment_grants')));
  checkpoint.issue[field]='other-source';
  if(field in checkpoint.snapshot)checkpoint.snapshot[field]='other-source';
  if(field==='accountId')checkpoint.issue.selectionOwner='other-source';
  f.sql.run('UPDATE solo_augment_grants SET checkpoint_json=?',[JSON.stringify(checkpoint)]);
  const before=f.sql.export(),response=await app.request(`/v1/runs/${f.runId}/augment-grants/grant`,{headers:f.headers},f.env);
  expect(response.status).toBe(503);expect(await response.json()).toEqual({accepted:false,errorCode:'augment_view_source_conflict'});
  expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
 }finally{f.sql.close();}
});
it('preserves unknown definition and target fields rather than fabricating empty equipment or effect recipients',async()=>{
 const f=await fixture();try{const view=await readAugmentView(f.env.DB,'a','DEV',f.runId,'grant',1000);
  expect(view.targetReadout.cards).toHaveLength(3);
  for(const card of view.targetReadout.cards)expect(card).toMatchObject({definition:null,scope:null,definitionStatus:'SOURCE_NOT_SUPPLIED',targets:null,targetResolution:'SOURCE_NOT_SUPPLIED'});
 }finally{f.sql.close();}
});
async function fixture(coop=false){const f=await setup(true),p=await(await f.preview()).json(),r=await(await f.start(p.operationPreviewId)).json();if(coop){const original=JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));original.admission.combatAuthority=captureCombatAuthority('ORDINARY_COOP');original.departureResponse.admission.combatAuthority=original.admission.combatAuthority;f.sql.run('UPDATE run_snapshots SET snapshot_json=?',[JSON.stringify(original)]);await new CombatHostReportStore(f.env.DB).initialize({authority:captureCombatAuthority('ORDINARY_COOP'),runId:r.runId,participants:[{accountId:'a',role:'TRAIN'},{accountId:'b',role:'OPERATOR'}],activeAccountIds:['a','b'],hostAccountId:'a',hostGeneration:1,definitionVersion:original.admission.combatPolicyRequirements.version,snapshotRevision:1,lastStateSequence:0,logicalTimeMs:0});}await new RunAugmentStore(f.env.DB,()=>0,coop?'ORDINARY_COOP':'ORDINARY_SOLO').issue({accountId:'a',environment:'DEV',selectionOwner:'a',runId:r.runId,eventId:'grant',stageId:'stage',catalogVersion:'v',eligibleIds:['A','B','C','D']});return {...f,runId:r.runId};}
it('exposes an authenticated read-only display contract',async()=>{const f=await fixture();const url=`/v1/runs/${f.runId}/augment-grants/grant`;expect((await app.request(url,{},f.env)).status).toBe(401);const before=f.sql.export();const response=await app.request(url,{headers:f.headers},f.env);expect(response.status).toBe(200);expect(response.headers.get('Cache-Control')).toBe('no-store');const body=await response.json();expect(body).toMatchObject({version:'combat.augment-view.v1',selectionControl:'SELF',pendingAd:null,grant:{grantRevision:0,state:'SELECTING'}});expect(body.grant.slots.map((s:{freeRemaining:number})=>s.freeRemaining)).toEqual([3,3,3]);expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);expect((await app.request(url+'?account=b',{headers:f.headers},f.env)).status).toBe(400);});
it('denies another account in solo and any outsider or wrong environment',async()=>{const f=await fixture();for(const [account,env] of [['b','DEV'],['a','PRD'],['outsider','DEV']])await expect(readAugmentView(f.env.DB,account!,env!,f.runId,'grant',1000)).rejects.toThrow('augment_view_forbidden');});
it('shows shared selection ownership and recovery pause to original participants',async()=>{const f=await fixture(true);expect(await readAugmentView(f.env.DB,'b','DEV',f.runId,'grant',1000)).toMatchObject({selectionControl:'PARTNER'});await new CombatHostReportStore(f.env.DB).recordDisconnect(f.runId,{requestId:'away',expectedRevision:1,disconnectedAccountId:'a',observedAt:1000});expect(await readAugmentView(f.env.DB,'b','DEV',f.runId,'grant',1001)).toMatchObject({selectionControl:'PAUSED'});});
it('projects pending slot and deadline without advertising credentials or purchaser identity',async()=>{const f=await fixture(true);f.sql.run("INSERT INTO augment_ad_sessions VALUES('intent',?,'grant',?,2000,'AD_PENDING')",[f.runId,JSON.stringify({target:{accountId:'a',slotIndex:1},binding:{customData:'secret',userId:'private',adUnit:'internal'}})]);const own=await readAugmentView(f.env.DB,'a','DEV',f.runId,'grant',1000),peer=await readAugmentView(f.env.DB,'b','DEV',f.runId,'grant',2001);expect(own.pendingAd).toEqual({slotIndex:1,callbackDeadlineAt:2000,ownedByViewer:true,phase:'PENDING'});expect(peer.pendingAd).toEqual({slotIndex:1,callbackDeadlineAt:2000,ownedByViewer:false,phase:'AWAITING_RESOLUTION'});expect(JSON.stringify(peer)).not.toMatch(/secret|private|internal|accountId|intentId/);expect(f.scalar('SELECT status FROM augment_ad_sessions')).toBe('AD_PENDING');});
it('keeps completed and selected grants closed for display',async()=>{const f=await fixture();f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await readAugmentView(f.env.DB,'a','DEV',f.runId,'grant',1000)).toMatchObject({selectionControl:'CLOSED'});});
