import {BRefitStore} from '../../apps/game-api/src/combat-b-refit-store';
import {LocalRefitPersistenceSource} from '../../apps/game-api/src/combat-b-refit-persistence-source';
import {describe,expect,it} from 'vitest';
import {readFileSync} from 'node:fs';
import {BSelectionStore,selectionVersion} from '../../apps/game-api/src/combat-b-selection-store';
import {LocalFixtureBSelectionCatalog} from '../../apps/game-api/src/combat-b-selection-catalog';
import {prepareBPlacementDeparture,readBPlacementDeparture} from '../../apps/game-api/src/combat-b-departure-placement';
import {setupBDeparturePinFixture} from '../helpers/combat-b-departure-pin-fixture';
const selection={version:'combat.b-placement.v2.20261002',mountedTurrets:[],placements:[{slot:0,carriageId:'LOCAL_KIND',occupant:{kind:'OPERATOR',operatorId:'LOCAL_OP'}},{slot:4,carriageId:'LOCAL_KIND',occupant:{kind:'CARGO'}}]};
async function fixture(){
 const f=await setupBDeparturePinFixture(),now=Date.now();
 f.sql.run(readFileSync('apps/game-api/local/combat-b-selection.sql','utf8'));
 f.sql.run("INSERT INTO account_carriages(account_id,carriage_id) VALUES('a','LOCAL_KIND'); INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a','LOCAL_OP',0)");
 f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('placement-session','a','LOCAL_SECRET_HASH','placement-family',0,'ACTIVE',?,?)",[now,now+600000]);
 f.sql.run('INSERT INTO combat_b_fixture_catalog VALUES(?,1,?)',['a',JSON.stringify({version:'LOCAL_PLACEMENT_CATALOG',status:'ISOLATED_LOCAL_FIXTURE',unlockedSlotCount:5,carriages:[{carriageId:'LOCAL_KIND',occupants:['OPERATOR','CARGO']}]})]);
 const auth={accountId:'a',sessionId:'placement-session',accessExpiresAt:now+600000},store=new BSelectionStore(f.env.DB,new LocalFixtureBSelectionCatalog());
 await store.save(auth,{version:selectionVersion,expectedRevision:0,catalogVersion:'LOCAL_PLACEMENT_CATALOG',expectedCatalogRevision:1,selection},'original-placement');
 return {...f,auth,store,prepare:()=>prepareBPlacementDeparture(store,auth,f.newRunId,f.baseJson,f.owner)};
}
describe('original persisted B placement departure boundary',()=>{
 it.each(['none','refit','rank','instance'])('pins original refit identity and fences late %s change',async change=>{const f=await fixture();try{
  for(const name of ['combat-b-refit.sql','combat-b-refit-http.sql'])f.sql.run(readFileSync('apps/game-api/local/'+name,'utf8'));
  f.sql.run('INSERT INTO combat_b_fixture_refits VALUES(?,1,1,?)',['a',JSON.stringify({status:'ISOLATED_LOCAL_FIXTURE',ranks:['BCB01','BCB02','BCB03','BCB04','BCB05','BCB06','BCB07'].map(type=>({type,rank:1})),instances:[{slot:0,instanceId:'physical-0',carriageId:'LOCAL_KIND'},{slot:4,instanceId:'physical-4',carriageId:'LOCAL_KIND'}]})]);
  const store=new BRefitStore(f.env.DB,new LocalRefitPersistenceSource(),new LocalFixtureBSelectionCatalog()),r=await store.read(f.auth);
  await store.save(f.auth,{version:r.version,expectedRevision:0,selectionRevision:1,rankRevision:1,definitionHash:r.definitionHash,sourceHash:r.sourceHash,refits:[{slot:0,type:'BCB01',rank:1},{slot:4,type:'BCB06',rank:1}]},'original-refit');
  const p=await prepareBPlacementDeparture(f.store,f.auth,f.newRunId,f.baseJson,f.owner,store),read=await readBPlacementDeparture(p.snapshotJson,{accountId:'a',runId:f.newRunId});
  expect(read?.refitSnapshot).toMatchObject({revision:1,refit:{rankRevision:1,cars:[{slot:0,instanceId:'physical-0'},{slot:4,instanceId:'physical-4',occupant:{kind:'CARGO'}}]}});
  expect(read?.missingSources).toEqual(['ORIGINAL_OCCUPANT_EFFECTIVE_PROFILE_BINDING']);
  if(change==='refit')f.sql.run('UPDATE combat_b_refit_selections SET revision=2');if(change==='rank')f.sql.run('UPDATE combat_b_fixture_refits SET revision=2');if(change==='instance')f.sql.run("UPDATE combat_b_fixture_refits SET source_json=json_set(source_json,'$.instances[0].instanceId','different-original-instance')");
  if(change==='none'){await f.commit(p);const frozen=String(f.scalar('SELECT snapshot_json FROM run_snapshots WHERE run_id=?',[f.newRunId]));f.sql.run('DELETE FROM combat_b_refit_selections; DELETE FROM combat_b_fixture_refits');expect((await readBPlacementDeparture(frozen,{accountId:'a',runId:f.newRunId}))?.refitSnapshot).toEqual(read?.refitSnapshot);}else{await expect(f.commit(p)).rejects.toThrow();expect(f.scalar('SELECT COUNT(*) FROM runs WHERE id=?',[f.newRunId])).toBe(0);}
 }finally{f.close();}});
 it('freezes saved B choices, preserves holes/CARGO and restores original pin after source disappears',async()=>{const f=await fixture();try{const prepared=await f.prepare();await f.commit(prepared);const frozen=String(f.scalar('SELECT snapshot_json FROM run_snapshots WHERE run_id=?',[f.newRunId]));f.sql.run('DELETE FROM combat_b_fixture_catalog; DELETE FROM combat_b_selections');const read=await readBPlacementDeparture(frozen,{accountId:'a',runId:f.newRunId});expect(read).toMatchObject({selectionRevision:1,selection,canStart:false,runtimeOccupantBindingIncluded:false,effectiveCombatStatsIncluded:false});expect(read?.selection.placements.map(p=>p.slot)).toEqual([0,4]);expect(read?.runtimeProfileHash).toMatch(/^[a-f0-9]{64}$/u);expect(JSON.stringify(JSON.parse(f.baseJson))).not.toContain('bDeparturePlacement');}finally{f.close();}});
 it.each(['selection','catalog','ownership','session'])('rejects %s mutation at original transaction without creating run',async change=>{const f=await fixture();try{const prepared=await f.prepare();if(change==='selection')f.sql.run('UPDATE combat_b_selections SET revision=2');if(change==='catalog')f.sql.run('UPDATE combat_b_fixture_catalog SET revision=2');if(change==='ownership')f.sql.run("DELETE FROM account_operators WHERE operator_id='LOCAL_OP'");if(change==='session')f.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE id='placement-session'");await expect(f.commit(prepared)).rejects.toThrow();expect(f.scalar('SELECT COUNT(*) FROM runs WHERE id=?',[f.newRunId])).toBe(0);}finally{f.close();}});
 it('does not upgrade old snapshots or unsupported profiles',async()=>{const f=await fixture();try{expect(await readBPlacementDeparture(f.baseJson,{accountId:'a',runId:f.newRunId})).toBeNull();await expect(prepareBPlacementDeparture(f.store,f.auth,f.newRunId,f.baseJson,{...f.owner,supports:async()=>false})).rejects.toThrow('combat_b_departure_profile_unsupported');}finally{f.close();}});
 it('rejects wrong account/run and tampered frozen choices',async()=>{const f=await fixture();try{const prepared=await f.prepare(),raw=JSON.parse(prepared.snapshotJson);await expect(readBPlacementDeparture(prepared.snapshotJson,{accountId:'b',runId:f.newRunId})).rejects.toThrow();raw.admission.bDeparturePlacement.selection.placements[1].slot=3;await expect(readBPlacementDeparture(JSON.stringify(raw),{accountId:'a',runId:f.newRunId})).rejects.toThrow();}finally{f.close();}});
});