import {canonicalJson,sha256Hex} from '@lastrail/common';
import {setupRiskRunFixture} from './combat-b-risk-run-fixture';
import {operationRiskVersion} from '../../apps/game-api/src/combat-b-operation-risk';
import {operationMapStartStatements} from '../../apps/game-api/src/operation-map-departure';
import type {EquipmentDepartureSource} from '../../apps/game-api/src/equipment-departure-source';
import type {BDepartureCombatSourceOwner,PreparedBDepartureSnapshot} from '../../apps/game-api/src/combat-b-departure-combat-pin';
/** Private LOCAL transaction mechanics only. No approved/production profile, START or economic command. */
export async function setupBDeparturePinFixture(){
 const f=await setupRiskRunFixture(),newRunId='LOCAL_NEW_B_PIN_19',newPreviewId='LOCAL_NEW_PREVIEW_19',base=structuredClone(f.snapshot);
 base.runId=newRunId;base.operationPreviewId=newPreviewId;base.departureResponse.runId=newRunId;base.departureResponse.operationPreviewId=newPreviewId;
 const baseJson=JSON.stringify(base);
 f.sql.run('CREATE TABLE local_b_source_guard_19(account_id TEXT PRIMARY KEY,revision INTEGER NOT NULL)');f.sql.run("INSERT INTO local_b_source_guard_19 VALUES('a',1)");
 const source={version:'combat.b-departure-source.v1.20261003',binding:{accountId:'a',runId:newRunId,operationPreviewId:newPreviewId,operationId:base.operationId,selectedRisk:base.risk,operationDefinitionVersion:base.admission.operationMapPlan.definitionVersion,operationPlanHash:await sha256Hex(canonicalJson(base.admission.operationMapPlan)),operationPublicationHash:base.operationMapContent!.contentHash},contextId:'LOCAL_SOURCE_OWNER_CONTEXT_NOT_RUN_ID_19',profile:{id:'LOCAL_TX_PROFILE_NOT_RUNTIME_APPROVAL_19',version:'LOCAL_TX_ONLY_v1',executorVersion:'LOCAL_CONTRACT_TEST_NOT_EXECUTOR',definition:{purpose:'TRANSACTION_TEST_ONLY_NOT_APPROVED_GAME_DATA'}},riskApplicationVersion:operationRiskVersion,rawProfiles:[{actorRef:'LOCAL_TX_ACTOR_NO_SPAWN',hp:{sourceId:'LOCAL_TEST_HP_SOURCE_NOT_GAME_BASE',kind:'BASE_HP',value:{numerator:'17',denominator:'1'},riskAppliedVersion:null},actions:[{sourceId:'LOCAL_TEST_RAW_ACTION_NOT_SCOUT',kind:'RAW_ACTION_DAMAGE',value:{numerator:'13',denominator:'7'},riskAppliedVersion:null,progressAppliedVersion:null}]}]};
 const owner:BDepartureCombatSourceOwner={prepare:async()=>({source:structuredClone(source),assertions:[f.env.DB.prepare("INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_b_source_guard_19 WHERE account_id='a' AND revision=1) THEN 1 ELSE 0 END")]}),supports:async s=>s.profile.executorVersion==='LOCAL_CONTRACT_TEST_NOT_EXECUTOR'&&s.profile.definition.purpose==='TRANSACTION_TEST_ONLY_NOT_APPROVED_GAME_DATA'};
 async function commit(prepared:PreparedBDepartureSnapshot){return f.env.DB.batch([
  ...prepared.atomicAssertions,
  f.env.DB.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES('local-b-pin-mut19','a',101,'RUN_START','local-b-tx19',0)"),
  f.env.DB.prepare("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) VALUES(?,'a','local-b-pin-mut19',?,'V',1,?,'CONSUMED',0,0,'departure-v4')").bind(newPreviewId,base.operationId,baseJson),
  f.env.DB.prepare("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,'a',?,'local-b-pin-mut19',1,'ACTIVE',0)").bind(newRunId,newPreviewId),
  f.env.DB.prepare('INSERT INTO run_snapshots VALUES(?,?,0)').bind(newRunId,prepared.snapshotJson),
  ...operationMapStartStatements(f.env.DB,'a',newRunId,prepared.snapshotJson),
  f.env.DB.prepare("INSERT INTO idempotency_records VALUES('local-b-replay19','a','local-b-tx19','run_start',?,0)").bind(newRunId),
  f.env.DB.prepare('DELETE FROM departure_write_guard')
 ]);}
 const scalar=(query:string,values:unknown[]=[])=>{const statement=f.sql.prepare(query);try{statement.bind(values as (string|number)[]);if(!statement.step())return null;return Object.values(statement.getAsObject())[0];}finally{statement.free();}};
 return {...f,scalar,newRunId,newPreviewId,baseJson,source,owner,commit};
}
/** Exercises the actual original builder/transaction in a private synthetic LOCAL database.
 * Its original fuel ledger writes are toy fixture writes, never operating economy or real admission.
 */
export async function setupOriginalBDepartureFixture(){
 const {setup}=await import('./departure-fixture'),{plan}=await import('./operation-map-fixture'),{OperationMapPublisher}=await import('../../apps/game-api/src/operation-map-publisher'),{startDeparture}=await import('../../apps/game-api/src/departure');
 const template=await setupBDeparturePinFixture(),raw=structuredClone(template.source);template.close();
 const f=await setup(true),quote=await (await f.preview()).json() as {operationPreviewId:string},id=quote.operationPreviewId;
 const pinned=JSON.parse(String(f.scalar('SELECT preview_json FROM operation_previews'))),p=plan();
 for(const node of p.nodes){node.encounterProfileRef='local-b19-'+node.eventKind;node.rewardPlanRef='local-b19-reward-'+node.eventKind;}
 const kinds=[...new Set(p.nodes.map(n=>n.eventKind))];
 const publication=await new OperationMapPublisher({read:async()=>({version:'operation.map-catalog.v1',catalogId:'LOCAL_B19_TRANSACTION_CATALOG_NOT_RUNTIME',revision:1,definitionVersion:p.definitionVersion,encounters:kinds.map(eventKind=>({ref:'local-b19-'+eventKind,version:'LOCAL_ONLY_v1',profile:'ORDINARY_12_NODE',eventKind,definition:{purpose:'LOCAL_ORIGINAL_BUILDER_VERIFICATION_ONLY'}})),rewards:kinds.map(eventKind=>({ref:'local-b19-reward-'+eventKind,version:'LOCAL_ONLY_v1',profile:'ORDINARY_12_NODE',eventKind,definition:{}}))})}).publish(p);
 pinned.encounterType='ORDINARY_12_NODE';pinned.admission.operationMapPlan=p;pinned.operationMapContent=publication;f.sql.run('UPDATE operation_previews SET preview_json=? WHERE id=?',[JSON.stringify(pinned),id]);
 f.sql.run('CREATE TABLE local_original_b_owner19(account_id TEXT PRIMARY KEY,revision INTEGER,supported INTEGER)');f.sql.run("INSERT INTO local_original_b_owner19 VALUES('a',1,1)");
 const owner:BDepartureCombatSourceOwner={prepare:async c=>({source:{...structuredClone(raw),binding:{accountId:c.accountId,runId:c.runId,operationPreviewId:c.operationPreviewId,operationId:c.operationId,selectedRisk:c.selectedRisk,operationDefinitionVersion:c.definitionVersion,operationPlanHash:c.planHash,operationPublicationHash:c.publicationHash}},assertions:[f.env.DB.prepare("INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_original_b_owner19 WHERE account_id=? AND revision=1 AND supported=1) THEN 1 ELSE 0 END").bind(c.accountId)]}),supports:async source=>source.profile.executorVersion==='LOCAL_CONTRACT_TEST_NOT_EXECUTOR'&&f.scalar('SELECT supported FROM local_original_b_owner19')===1};
 async function start(equipmentSource?:EquipmentDepartureSource){const row=await f.env.DB.prepare('SELECT * FROM operation_previews WHERE id=?').bind(id).first<Parameters<typeof startDeparture>[2]>();return startDeparture(f.env.DB,'a',row!,'local-original-builder19',async(row,runId)=>({accepted:true,startStatus:'STARTED',runId,operationPreviewId:row.id,operationId:row.objective,risk:row.risk,balanceVersion:String(row.balance_config_version),encounterType:'ORDINARY_12_NODE'}),undefined,f.env.ENVIRONMENT,owner,equipmentSource);}
 const scalar=(query:string,values:unknown[]=[])=>{const statement=f.sql.prepare(query);try{statement.bind(values as (string|number)[]);if(!statement.step())return null;return Object.values(statement.getAsObject())[0];}finally{statement.free();}};
 function failAtCommit(kind:'source'|'support'){f.adapter.beforeBatch=()=>{delete f.adapter.beforeBatch;f.sql.run(kind==='source'?'UPDATE local_original_b_owner19 SET revision=2':'UPDATE local_original_b_owner19 SET supported=0');};}
 const restoreSource=()=>f.sql.run('UPDATE local_original_b_owner19 SET revision=1,supported=1'),changeSource=()=>f.sql.run('UPDATE local_original_b_owner19 SET revision=9,supported=0');
 return {...f,scalar,id,start,failAtCommit,restoreSource,changeSource,close:()=>f.sql.close()};
}