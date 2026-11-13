import {readFileSync} from 'node:fs';
import {expect,it,afterEach} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {EquipmentEffectOriginalStore} from '../../apps/game-api/src/equipment-effect-original';
import {EquipmentEffectStateStore} from '../../apps/game-api/src/equipment-effect-state';
import {EquipmentEnchantStore,type EquipmentEnchantSource} from '../../apps/game-api/src/equipment-enchant';
import {EquipmentMergeStore} from '../../apps/game-api/src/equipment-merge';
import type initSqlJs from 'sql.js';
const dbs:initSqlJs.Database[]=[];afterEach(()=>{for(const db of dbs.splice(0))db.close();});
const enchantment={effectId:'EN17',appliedStar:1,definitionVersion:'equipment.multishot-catalog.v1.20261015',definitionSnapshot:{version:'multishot-enchant-v1.20261015',appliedStar:1,coefficientBps:1500,maxSecondaryTargets:2,radiusL:{numerator:1,denominator:2},attackEvaluation:'AT_FIRE',targetDefenseEvaluation:'AT_IMPACT'}};
async function fixture(legacy=false){const f=await setup(true,true,true,legacy?'0120':undefined);dbs.push(f.sql);const q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId,'en17-start')).json();return {...f,runId:r.runId as string};}
async function issue(f:Awaited<ReturnType<typeof fixture>>,id:string,v2:boolean,enchanted=false){
 const snapshot={version:v2?'equipment.effect-original.v2.20261015':'equipment.effect-original.v1',equipment:{definitionId:'gear',definitionVersion:'gear-v1',star:1},uniqueEffects:[],enchantment:enchanted?enchantment:null};
 const store=new EquipmentEffectOriginalStore(f.env.DB,{prepare:async()=>({snapshot,assertions:[f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard VALUES(1)')]})});
 const prepared=await store.prepare('a',f.runId,{instanceId:id,sourceItemId:'source-'+id,expectedInventoryRevision:1});
 await f.env.DB.batch([f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision) VALUES(?,?,?,'gear','gear-v1','ATK','SET01',1,0,'STORED',1)`).bind(id,f.runId,'source-'+id),...prepared.statements]);return snapshot;
}
function command(){return {commandId:'en17-apply',instanceId:'a-gear',sourceRef:'trial-right',expectedSourceVersion:'trial-pool-v1',replaceExisting:false,expectedInventoryRevision:1,expectedInstanceRevision:1,expectedEffectRevision:1};}
function source(f:Awaited<ReturnType<typeof fixture>>){
 // Synthetic event-right adapter. Real catalog/transaction readers and DB guards are exercised;
 // no operating event, pool exposure, grant, economic consumption or RNG adapter is asserted.
 f.sql.run('CREATE TABLE test_en17_right(available INTEGER NOT NULL)');f.sql.run('INSERT INTO test_en17_right VALUES(1)');let draws=0;
 const adapter:EquipmentEnchantSource={prepare:async()=>{draws++;return {sourceVersion:'trial-pool-v1',enchantment,assertions:[f.env.DB.prepare('INSERT INTO equipment_enchant_write_guard SELECT CASE WHEN (SELECT available FROM test_en17_right)=1 THEN 1 ELSE 0 END')],consumeStatements:[f.env.DB.prepare('UPDATE test_en17_right SET available=0')]};}};return {adapter,draws:()=>draws};
}
it('persists v2 issuance and leaves immutable original/hash readable',async()=>{const f=await fixture(),snapshot=await issue(f,'a-gear',true,true);expect(await new EquipmentEffectOriginalStore(f.env.DB).read('a',f.runId,'a-gear')).toEqual(snapshot);expect((await new EquipmentEffectStateStore(f.env.DB).read('a',f.runId,'a-gear'))?.snapshot).toEqual({...snapshot,version:'equipment.effect-state.v2.20261015'});expect(f.scalar('PRAGMA foreign_key_check')).toBeUndefined();});
it('upgrades a v1 current state through one authorized EN17 procedure and replays without another draw',async()=>{const f=await fixture();const original=await issue(f,'a-gear',false);const before=f.scalar('SELECT payload_hash FROM run_equipment_effect_originals');const s=source(f),store=new EquipmentEnchantStore(f.env.DB,s.adapter);const result=await store.apply('a',f.runId,command());expect(result.version).toBe('equipment.enchant-receipt.v2.20261015');expect(result.enchantment).toEqual(enchantment);expect((await new EquipmentEffectStateStore(f.env.DB).read('a',f.runId,'a-gear'))?.snapshot.version).toBe('equipment.effect-state.v2.20261015');expect(await new EquipmentEffectOriginalStore(f.env.DB).read('a',f.runId,'a-gear')).toEqual(original);expect(f.scalar('SELECT payload_hash FROM run_equipment_effect_originals')).toBe(before);f.sql.run("UPDATE runs SET status='COMPLETED'");expect(await store.apply('a',f.runId,command())).toEqual(result);expect(s.draws()).toBe(1);await expect(store.apply('a',f.runId,{...command(),replaceExisting:true})).rejects.toThrow('equipment_request_conflict');});
it.each(['run_equipment_effect_history','run_equipment_receipts'])('rolls back EN17 version, right and revisions if %s fails',async table=>{const f=await fixture();await issue(f,'a-gear',false);const s=source(f);f.sql.run(`CREATE TRIGGER injected_en17 BEFORE INSERT ON ${table} BEGIN SELECT RAISE(ABORT,'en17-injected'); END`);await expect(new EquipmentEnchantStore(f.env.DB,s.adapter).apply('a',f.runId,command())).rejects.toThrow('en17-injected');expect(f.scalar('SELECT available FROM test_en17_right')).toBe(1);expect(f.scalar('SELECT revision FROM run_equipment_inventory')).toBe(1);expect(f.scalar('SELECT revision FROM run_equipment_effect_heads')).toBe(1);expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);});
it('merges an actual v2 primary and legacy auxiliary preserving appliedStar=1 after currentStar=2',async()=>{const f=await fixture();await issue(f,'a-gear',true,true);await issue(f,'b-gear',false);const store=new EquipmentMergeStore(f.env.DB,{prepare:async()=>({sourceVersion:'test-merge',resolvedEffects:[],assertions:[f.env.DB.prepare('INSERT INTO equipment_merge_write_guard VALUES(1)')]})});await store.merge('a',f.runId,{commandId:'en17-merge',primaryInstanceId:'a-gear',secondaryInstanceId:'b-gear',expectedInventoryRevision:1,expectedPrimaryRevision:1,expectedSecondaryRevision:1,expectedPrimaryEffectRevision:1,expectedSecondaryEffectRevision:1,expectedSourceVersion:'test-merge',confirmedLosses:true});const result=await new EquipmentEffectStateStore(f.env.DB).read('a',f.runId,'a-gear');expect(result?.snapshot).toMatchObject({version:'equipment.effect-state.v2.20261015',equipment:{star:2},enchantment});expect(f.scalar('SELECT count(*) FROM run_equipment_effect_originals')).toBe(2);});

it('migrates a real pre-0121 schema preserving original/history/receipt bytes and then writes EN17',async()=>{
 const f=await fixture(true);await issue(f,'a-gear',false);const s=source(f);
 const legacySource:EquipmentEnchantSource={prepare:async context=>({...await s.adapter.prepare(context),enchantment:{effectId:'EN02',appliedStar:1,definitionVersion:'test-old',definitionSnapshot:{fixtureOnly:true,fixed:123}}})};
 const oldStore=new EquipmentEnchantStore(f.env.DB,legacySource),oldReceipt=await oldStore.apply('a',f.runId,command());
 const before=f.sql.exec('SELECT snapshot_json,payload_hash FROM run_equipment_effect_originals'),history=f.sql.exec('SELECT snapshot_json,payload_hash FROM run_equipment_effect_history'),receipts=f.sql.exec('SELECT request_hash,response_json FROM run_equipment_receipts');
 f.sql.run(readFileSync('packages/db/migrations/0121_equipment_multishot_en17.sql','utf8'));
 expect(f.sql.exec('SELECT snapshot_json,payload_hash FROM run_equipment_effect_originals')).toEqual(before);expect(f.sql.exec('SELECT snapshot_json,payload_hash FROM run_equipment_effect_history')).toEqual(history);expect(f.sql.exec('SELECT request_hash,response_json FROM run_equipment_receipts')).toEqual(receipts);expect(await oldStore.apply('a',f.runId,command())).toEqual(oldReceipt);
 f.sql.run('UPDATE test_en17_right SET available=1');const p=await new EquipmentEnchantStore(f.env.DB,s.adapter).apply('a',f.runId,{...command(),commandId:'new-en17',sourceRef:'new-right',replaceExisting:true,expectedInventoryRevision:2,expectedInstanceRevision:2,expectedEffectRevision:2});expect(p.enchantment.effectId).toBe('EN17');expect(f.scalar('PRAGMA foreign_key_check')).toBeUndefined();
});
it('database guards reject v1 EN17, v2 coefficient corruption and immutable rewriting',async()=>{
 const f=await fixture();await issue(f,'a-gear',false);
 const bad={version:'equipment.effect-original.v1',equipment:{definitionId:'gear',definitionVersion:'gear-v1',star:1},uniqueEffects:[],enchantment};
 f.sql.run("INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision) VALUES('raw',?,'raw-source','gear','gear-v1','ATK','SET01',1,0,'STORED',1)",[f.runId]);
 const insert=(snapshot:unknown)=>f.sql.run('INSERT INTO run_equipment_effect_originals(instance_id,run_id,source_item_id,snapshot_json,payload_hash) VALUES(?,?,?,?,?)',['raw',f.runId,'raw-source',JSON.stringify(snapshot),'0'.repeat(64)]);
 expect(()=>insert(bad)).toThrow('equipment_effect_envelope_invalid');
 expect(()=>insert({...bad,version:'equipment.effect-original.v2.20261015',enchantment:{...enchantment,definitionSnapshot:{...enchantment.definitionSnapshot,coefficientBps:2500}}})).toThrow('equipment_effect_envelope_invalid');
 expect(()=>f.sql.run("UPDATE run_equipment_effect_originals SET payload_hash=?",['0'.repeat(64)])).toThrow('equipment_effect_original_immutable');
});

it('replaces EN17 by an old enchantment without downgrading the v2 state/receipt',async()=>{
 const f=await fixture();await issue(f,'a-gear',true,true);const s=source(f),old={effectId:'EN02',appliedStar:1,definitionVersion:'test-legacy',definitionSnapshot:{fixed:123}};
 const store=new EquipmentEnchantStore(f.env.DB,{prepare:async context=>({...await s.adapter.prepare(context),enchantment:old})});const c={...command(),replaceExisting:true};const result=await store.apply('a',f.runId,c);
 expect(result).toMatchObject({version:'equipment.enchant-receipt.v2.20261015',enchantment:old});expect((await new EquipmentEffectStateStore(f.env.DB).read('a',f.runId,'a-gear'))?.snapshot.version).toBe('equipment.effect-state.v2.20261015');expect(await store.apply('a',f.runId,c)).toEqual(result);expect(s.draws()).toBe(1);
});
