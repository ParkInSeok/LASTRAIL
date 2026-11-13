// Unity-owned diagnostic capture. Original server source and producer QA are read-only.
import {writeFile} from 'node:fs/promises';
import {strict as assert} from 'node:assert';
import {equipmentStatFixture} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/helpers/equipment-run-stat-fixture';
import {EquipmentMergeStore} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/equipment-merge';
import {equipmentCoreStatVersion,readPinnedRunEquipmentStats} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/equipment-run-stat-owner';
const qa='D:/Project/LASTRAIL/QA/CombatBMigration-20261004/RunEquipmentSource-20261005/',f=await equipmentStatFixture();
try {
 const threeSlots=(await f.owner.capture('a',f.runId,[{wearerKind:'operator',wearerId:'op_kyle',slots:[f.item('hp-1'),f.item('hp-2'),f.item('atk-1')]}])).snapshot;
 const twoWearers=(await f.owner.capture('a',f.runId,[{wearerKind:'operator',wearerId:'op_kyle',slots:[f.item('hp-1'),null,null]},{wearerKind:'operator',wearerId:'op_sable',slots:[f.item('atk-1'),null,null]}])).snapshot;
 const merge=new EquipmentMergeStore(f.env.DB,{prepare:async()=>({sourceVersion:equipmentCoreStatVersion,resolvedEffects:[],assertions:[f.env.DB.prepare('INSERT INTO equipment_merge_write_guard(valid) VALUES(1)')]})});
 await merge.merge('a',f.runId,{commandId:'UNITY_CONSUMER_LOCAL_STORED_MERGE',primaryInstanceId:'hp-1',secondaryInstanceId:'hp-2',expectedInventoryRevision:1,expectedPrimaryRevision:1,expectedSecondaryRevision:1,expectedPrimaryEffectRevision:1,expectedSecondaryEffectRevision:1,expectedSourceVersion:equipmentCoreStatVersion,confirmedLosses:true});
 const twoStar=(await f.owner.capture('a',f.runId,[{wearerKind:'operator',wearerId:'op_kyle',slots:[f.item('hp-1'),null,null]}])).snapshot;
 const failures:{case:string;errorCode:string;snapshot:null}[]=[];
 async function reject(label:string,instance:string,expected:string){try{await f.owner.capture('a',f.runId,[{wearerKind:'operator',wearerId:'op_kyle',slots:[f.item(instance),null,null]}]);assert.fail('Expected original producer rejection');}catch(error){const code=(error as {code?:string}).code;assert.equal(code,expected);failures.push({case:label,errorCode:code!,snapshot:null});}}
 await f.issue('unique-3','HP','SET04',3);await reject('original 3-star unique handler unavailable','unique-3','equipment_stat_effect_handler_unavailable');
 f.sql.run("INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision) VALUES('unbound',?,'LOCAL_UNBOUND','LOCAL_HP',?,'HP','SET04',1,0,'STORED',1)",[f.runId,equipmentCoreStatVersion]);
 await reject('original effect source absent','unbound','equipment_stat_effect_source_missing');
 f.sql.run("UPDATE run_equipment_instances SET revision=revision+1 WHERE instance_id='hp-1'");
 assert.deepEqual(await readPinnedRunEquipmentStats(twoStar,twoStar,twoStar.packetHash),twoStar);
 assert.equal(f.scalar('SELECT snapshot_json FROM run_snapshots'),f.originalSnapshot);
 await writeFile(qa+'original-producer-samples.json',JSON.stringify({evidence:'ACTUAL_ORIGINAL_SERVER_OWNER_WITH_ISOLATED_SQL_FIXTURE_SYNTHETIC_RUN_NOT_OPERATING_START',version:equipmentCoreStatVersion,threeSlots,twoWearers,twoStar,failures,storedMergeOnly:true,originalSnapshotRestoredAfterCurrentRevisionChange:true,originalRunSnapshotUnchanged:true,healthUnchanged:(await f.health.read('a',f.runId)).revision===1},null,2)+'\n');
} finally {f.sql.close();}
