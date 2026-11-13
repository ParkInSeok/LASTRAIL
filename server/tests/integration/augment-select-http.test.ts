import {afterEach,expect,it} from 'vitest';
import {createAugmentSelectFixture} from '../helpers/augment-select-fixture';

import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {augmentUtilityDefinitionVersion} from '../../apps/game-api/src/augment-utility-definition';
import app from '../../apps/game-api/src/index';
import {SoloAugmentSelection} from '../../apps/game-api/src/solo-augment-selection';
import {AugmentNodeIssuer,type AugmentEligibleCatalogSource} from '../../apps/game-api/src/augment-node-issuer';
import {issueAugmentForArrival} from '../../apps/game-api/src/augment-arrival-issuer';
const cleanup:(()=>void)[]=[];afterEach(()=>{for(const close of cleanup.splice(0))close();});
async function fixture(...args:Parameters<typeof createAugmentSelectFixture>){
 const f=await createAugmentSelectFixture(...args);cleanup.push(()=>f.sql.close());return f;
}
it('supplies current candidate conditions in the existing view without modifying grant or receipt state',async()=>{
 const f=await fixture(),before=Buffer.from(f.sql.export());
 const response=await app.request(f.url.replace(/\/select$/u,''),{headers:f.headers},f.env);
 expect(response.status).toBe(200);const view=await response.json();
 expect(view.targetReadout).toMatchObject({runId:f.runId,eventId:'grant',grantRevision:view.grant.grantRevision,catalogVersion:view.grant.catalogVersion,
  cards:[{candidateId:'O22',scope:'OPERATOR',targets:null,definition:{rules:{recipient:'SOLE_ATTRIBUTED_OPERATOR',damageToHealBps:1200}}}]});
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
});
it('does not confirm O22 without its original supported definition and application response',async()=>{
 const f=await fixture(true,'O22',true,'augment.utility-definitions.v7.20261019');
 const response=await f.select();
 expect(response.status).toBe(409);
 expect(await response.json()).toMatchObject({accepted:false,errorCode:'augment_store_application_definition_unsupported'});
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(0);
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
 expect(await new SoloAugmentStore(f.env.DB).read({accountId:'a',environment:'DEV'},f.runId,'grant')).toMatchObject({state:'SELECTING'});
});
it('returns the original application in one authenticated SELECT and replays after leaving the node',async()=>{
 const f=await fixture(),response=await f.select();expect(response.status).toBe(200);const r=await response.json();
 expect(r).toMatchObject({version:f.body.version,committed:true,result:{outcome:'SELECTED',applicationOriginal:{effectId:'O22',applied:false}}});
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(1);
 f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await(await f.select()).json()).toEqual(r);
 expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
});
it('recovers a lost SELECT commit acknowledgement with the same original and no map resume',async()=>{
 const f=await fixture(),before=String(f.scalar('SELECT state_json FROM operation_map_states'));
 const batch=f.env.DB.batch.bind(f.env.DB);let lost=true;
 f.env.DB.batch=async statements=>{const result=await batch(statements);if(lost){lost=false;throw Error('SELECT ACK lost');}return result;};
 const response=await f.select();expect(response.status).toBe(200);
 const saved=await response.json();
 expect(saved).toMatchObject({committed:true,result:{applicationOriginal:{applied:false,status:'SERVER_CONFIRMED_RUNTIME_APPLY_REQUIRED'}}});
 expect(await(await f.select()).json()).toEqual(saved);
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(1);
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(1);
 expect(f.scalar('SELECT state_json FROM operation_map_states')).toBe(before);
});
it('issues from an authenticated catalog source once and recovers without consulting changed catalog',async()=>{
 const f=await fixture(true,'O22',false),context={accountId:'a',environment:'DEV',runId:f.runId,stageId:'n1-1'};
 const source:AugmentEligibleCatalogSource={prepare:async()=>({catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['O22','T05'],assertions:[f.env.DB.prepare('INSERT INTO solo_augment_write_guard(valid) VALUES(1)')]})};
 const first=await new AugmentNodeIssuer(f.env.DB,source).issue(context);
 if('outcome' in first)throw Error('expected grant');
 expect(first.slots).toHaveLength(2);expect(first.slots.map(s=>s.candidateId).sort()).toEqual(['O22','T05']);
 expect(await new AugmentNodeIssuer(f.env.DB).issue(context)).toEqual(first);
 f.sql.run("UPDATE runs SET status='COMPLETED'");
 expect(await new AugmentNodeIssuer(f.env.DB).issue(context)).toEqual(first);
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants')).toBe(1);
});
it('rejects missing proof and completes a verified empty catalog without an empty grant',async()=>{
 const f=await fixture(true,'O22',false),context={accountId:'a',environment:'DEV',runId:f.runId,stageId:'n1-1'};
 await expect(new AugmentNodeIssuer(f.env.DB).issue(context)).rejects.toThrow('catalog_source_required');
 const source:AugmentEligibleCatalogSource={prepare:async()=>({catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['O22'],assertions:[]})};
 await expect(new AugmentNodeIssuer(f.env.DB,source).issue(context)).rejects.toThrow('catalog_proof_required');
 source.prepare=async()=>({catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:[],assertions:[f.env.DB.prepare('INSERT INTO solo_augment_write_guard(valid) VALUES(1)')]});
 const empty=await new AugmentNodeIssuer(f.env.DB,source).issue(context);
 expect(empty).toMatchObject({outcome:'NO_AWARD',catalogVersion:augmentUtilityDefinitionVersion,mapReceipt:{state:{phase:'READY'}}});
 expect(await new AugmentNodeIssuer(f.env.DB).issue(context)).toEqual(empty);
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants')).toBe(0);
 const before=await fixture(false,'O22',false);
 await expect(new AugmentNodeIssuer(before.env.DB,source).issue({...context,runId:before.runId})).rejects.toThrow('node_selection_unavailable');
});
it('filters actual owned singleton cards and rolls back a revoked catalog at issuance',async()=>{
 const f=await fixture(true,'O22',false),store=new SoloAugmentStore(f.env.DB,()=>0);
 await store.issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId:f.runId,eventId:'past',stageId:'past',catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['T05']});
 await store.execute({accountId:'a',environment:'DEV'},f.runId,'past','choose',{operation:'SELECT',command:{eventId:'past',expectedRevision:0,slotIndex:0,candidateId:'T05'}});
 f.sql.run('CREATE TABLE fixture_catalog(valid INTEGER)');f.sql.run('INSERT INTO fixture_catalog VALUES(1)');
 const source:AugmentEligibleCatalogSource={prepare:async()=>({catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['T05','O22'],assertions:[f.env.DB.prepare('INSERT INTO solo_augment_write_guard(valid) SELECT valid FROM fixture_catalog')]})};
 const context={accountId:'a',environment:'DEV',runId:f.runId,stageId:'n1-1'},issuer=new AugmentNodeIssuer(f.env.DB,source);
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run('UPDATE fixture_catalog SET valid=0');};
 await expect(issuer.issue(context)).rejects.toThrow();expect(f.scalar('SELECT count(*) FROM solo_augment_grants')).toBe(1);
 f.sql.run('UPDATE fixture_catalog SET valid=1');const selected=await issuer.issue(context);if('outcome' in selected)throw Error('expected grant');expect(selected.slots.map(s=>s.candidateId)).toEqual(['O22']);
});
it('commits no-award map state and receipt together and recovers ACK loss after completion',async()=>{
 const f=await fixture(true,'O22',false),context={accountId:'a',environment:'DEV',runId:f.runId,stageId:'n1-1'};
 const source:AugmentEligibleCatalogSource={prepare:async()=>({catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:[],assertions:[f.env.DB.prepare('INSERT INTO solo_augment_write_guard(valid) VALUES(1)')]})};
 const issuer=new AugmentNodeIssuer(f.env.DB,source);
 f.sql.run("CREATE TRIGGER reject_empty_receipt BEFORE INSERT ON operation_map_receipts WHEN NEW.event_id LIKE 'node:%' BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(issuer.issue(context)).rejects.toThrow('injected');
 expect((await new OperationMapStore(f.env.DB).read('a',f.runId)).state.phase).toBe('ACTIVE');
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants')).toBe(0);
 f.sql.run('DROP TRIGGER reject_empty_receipt');const batch=f.env.DB.batch.bind(f.env.DB);let lost=true;
 f.env.DB.batch=async statements=>{const result=await batch(statements);if(lost){lost=false;throw Error('ACK lost');}return result;};
 const saved=await issuer.issue(context);expect(saved).toMatchObject({outcome:'NO_AWARD'});
 f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await new AugmentNodeIssuer(f.env.DB).issue(context)).toEqual(saved);
 expect(f.scalar("SELECT count(*) FROM operation_map_receipts WHERE event_id LIKE 'node:%'")).toBe(1);
 expect(f.scalar('SELECT count(*) FROM solo_augment_write_guard')).toBe(0);
});
it('fences a result review inserted immediately before no-award completion',async()=>{
 const f=await fixture(true,'O22',false),context={accountId:'a',environment:'DEV',runId:f.runId,stageId:'n1-1'};
 // Minimal synthetic review owner row; the race concerns its existence only.
 f.sql.run('INSERT INTO combat_host_report_states(run_id,revision,initial_hash,context_json,start_snapshot_json) VALUES(?,1,?,?,?)',[f.runId,'fixture','{}',String(f.scalar('SELECT snapshot_json FROM run_snapshots'))]);
 const source:AugmentEligibleCatalogSource={prepare:async()=>({catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:[],assertions:[f.env.DB.prepare('INSERT INTO solo_augment_write_guard(valid) VALUES(1)')]})};
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run('INSERT INTO combat_result_reviews(run_id,state_json) VALUES(?,?)',[f.runId,'{}']);};
 await expect(new AugmentNodeIssuer(f.env.DB,source).issue(context)).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM combat_result_reviews')).toBe(1);
 expect((await new OperationMapStore(f.env.DB).read('a',f.runId)).state.phase).toBe('ACTIVE');
 expect(f.scalar("SELECT count(*) FROM operation_map_receipts WHERE event_id LIKE 'node:%'")).toBe(0);
});
it('calls the existing issuer from a committed arrival and reuses its grant on arrival retry',async()=>{
 const f=await fixture(true,'O22',false),receipt=await new OperationMapStore(f.env.DB).lookup('a',f.runId,'arrive');
 let calls=0;
 const catalog:AugmentEligibleCatalogSource={prepare:async()=>{calls++;return {catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['O22'],assertions:[f.env.DB.prepare('INSERT INTO solo_augment_write_guard(valid) VALUES(1)')]};}};
 const first=await issueAugmentForArrival(f.env.DB,'a',f.runId,'DEV',receipt,catalog);
 expect(first).toMatchObject({state:'SELECTING',stageId:'n1-1'});
 expect(await issueAugmentForArrival(f.env.DB,'a',f.runId,'DEV',receipt,catalog)).toEqual(first);
 expect(calls).toBe(1);expect(f.scalar('SELECT count(*) FROM solo_augment_grants')).toBe(1);
 await expect(issueAugmentForArrival(f.env.DB,'a',f.runId,'DEV',{...receipt,eventId:'invented'},catalog)).rejects.toThrow('arrival_receipt_invalid');
});
it('rejects unarrived nodes and changed map state at commit',async()=>{
 const before=await fixture(false);expect((await before.select()).status).toBe(409);
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run('UPDATE operation_map_states SET revision=revision+1');};
 expect((await f.select()).status).toBe(409);expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
});
it('rejects body authority injection and changed idempotent command',async()=>{
 const f=await fixture();expect((await f.select({...f.body,accountId:'b'})).status).toBe(400);
 expect((await f.select({...f.body,command:{...f.body.command,slotIndex:2}})).status).toBe(409);
 expect((await f.select()).status).toBe(200);
 expect((await f.select({...f.body,command:{...f.body.command,candidateId:'O23'}})).status).toBe(409);
});
it('requires authentication, grant ownership and the server environment',async()=>{
 const f=await fixture();expect((await app.request(f.url,{method:'POST',body:JSON.stringify(f.body),headers:{'Content-Type':'application/json','Idempotency-Key':'x'}},f.env)).status).toBe(401);
 f.env.ENVIRONMENT='PRD';expect((await f.select()).status).toBe(403);f.env.ENVIRONMENT='DEV';
 f.sql.run("UPDATE solo_augment_grants SET account_id='b'");expect((await f.select()).status).toBe(403);
 expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(0);
});
it('converges duplicate HTTP requests on one original selection and one application',async()=>{
 const f=await fixture(),responses=await Promise.all([f.select(),f.select()]);
 expect(responses.map(r=>r.status)).toEqual([200,200]);expect(await responses[0]!.json()).toEqual(await responses[1]!.json());
 expect(f.scalar('SELECT count(*) FROM solo_augment_receipts')).toBe(1);expect(f.scalar('SELECT count(*) FROM run_augment_effect_originals')).toBe(1);
});
it('rejects a second single-owned utility from the actual confirmed grant history',async()=>{
 const f=await fixture(true,'T05'),store=new SoloAugmentStore(f.env.DB,()=>0),eventId='earlier-grant';
 await store.issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId:f.runId,eventId,stageId:'earlier-node',catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['T05']});
 await store.execute({accountId:'a',environment:'DEV'},f.runId,eventId,'earlier-select',{operation:'SELECT',command:{eventId,expectedRevision:0,slotIndex:0,candidateId:'T05'}});
 expect((await f.select()).status).toBe(409);
 expect((await store.read({accountId:'a',environment:'DEV'},f.runId,'grant')).state).toBe('SELECTING');
});
it('fences a different grant becoming owned between validation and commit',async()=>{
 const f=await fixture(true,'T05'),store=new SoloAugmentStore(f.env.DB,()=>0),eventId='earlier-grant';
 await store.issue({accountId:'a',selectionOwner:'a',environment:'DEV',runId:f.runId,eventId,stageId:'earlier-node',catalogVersion:augmentUtilityDefinitionVersion,eligibleIds:['T05']});
 const model=SoloAugmentSelection.restore(String(f.scalar("SELECT checkpoint_json FROM solo_augment_grants WHERE event_id='earlier-grant'")),{extraApproval:{authorize:async()=>{throw Error('unused');}}});
 model.select({accountId:'a',environment:'DEV'},'other-choice',{eventId,expectedRevision:0,slotIndex:0,candidateId:'T05'});
 // Inject another transaction's changed original checkpoint at the batch boundary.
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE solo_augment_grants SET checkpoint_json=?,write_revision=write_revision+1 WHERE event_id='earlier-grant'",[model.checkpoint()]);};
 expect((await f.select()).status).toBe(409);
 expect((await store.read({accountId:'a',environment:'DEV'},f.runId,'grant')).state).toBe('SELECTING');
});
