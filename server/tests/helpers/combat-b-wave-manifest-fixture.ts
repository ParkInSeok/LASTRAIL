import {canonicalJson,sha256Hex} from '@lastrail/common';
import type {BWaveManifestSourceOwner} from '../../apps/game-api/src/combat-b-wave-manifest';
import {setupBDeparturePinFixture} from './combat-b-departure-pin-fixture';
import type {BDepartureCombatContext,BDepartureCombatSource} from '../../apps/game-api/src/combat-b-departure-combat-pin';
export const waveVersion='combat.b-wave-manifest.v1.20261003';
export async function setupWaveManifestFixture(){
 const f=await setupBDeparturePinFixture(),plan=JSON.parse(f.baseJson).admission.operationMapPlan;
 f.sql.run('CREATE TABLE local_wave_owner21(revision INTEGER,supported INTEGER)');f.sql.run('INSERT INTO local_wave_owner21 VALUES(1,1)');
 const source={...structuredClone(f.source),profile:{...structuredClone(f.source.profile),definition:{...f.source.profile.definition,waveManifestVersion:waveVersion}}} as unknown as BDepartureCombatSource;
 const parent={...f.owner,prepare:async(c:Readonly<BDepartureCombatContext>)=>({...await f.owner.prepare(c),source:structuredClone(source)})};
 const templates=await Promise.all(['a','b'].map(async key=>{const raw=f.source.rawProfiles[0]!,base={templateId:'LOCAL_SYNTHETIC_TEMPLATE_'+key,version:'LOCAL_TRANSACTION_ONLY_v1',definitionId:'LOCAL_SYNTHETIC_ENEMY_'+key,definition:{purpose:'SCHEMA_TEST_ONLY_NO_GAME_PROFILE'},hp:structuredClone(raw.hp),actions:structuredClone(raw.actions)};return {...base,templateHash:await sha256Hex(canonicalJson(base))};}));
 function wave(nodeId:string,depth:number,count:number,waveOrdinal:number){const first=depth<=8?4:6,interval=depth<=4?1800:depth<=8?1500:1200;return {nodeId,visitDepth:depth,waveOrdinal,instances:Array.from({length:count},(_,ordinal)=>{const t=templates[ordinal%2]!,group=ordinal<first?0:1+Math.floor((ordinal-first)/2);return {instanceId:`LOCAL_INSTANCE_${nodeId}_${waveOrdinal}_${ordinal}`,ordinal,templateRef:{templateId:t.templateId,version:t.version,templateHash:t.templateHash},entryGroup:group,entryNotBeforeMs:group*interval};})};}
 const proposal={version:waveVersion,contextId:source.contextId,templates,waves:[wave('n1-0',1,6,1),wave('n1-0',1,6,2)]};
 const owner:BWaveManifestSourceOwner={prepare:async()=>({manifest:structuredClone(proposal),assertions:[f.env.DB.prepare('INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_wave_owner21 WHERE revision=1 AND supported=1) THEN 1 ELSE 0 END')]}),supports:async()=>f.scalar('SELECT supported FROM local_wave_owner21')===1};
 return {...f,plan,source,parent,templates,proposal,waveOwner:owner,wave};
}
/** Private synthetic LOCAL actual-original-builder fixture; toy fuel ledger is not operating economy. */
export async function setupOriginalWaveManifestFixture(){
 const {setupOriginalBDepartureFixture}=await import('./combat-b-departure-pin-fixture'),{startDeparture}=await import('../../apps/game-api/src/departure'),{createBWaveDepartureSourceOwner}=await import('../../apps/game-api/src/combat-b-wave-manifest');
 const sample=await setupWaveManifestFixture(),raw=structuredClone(sample.source),proposal=structuredClone(sample.proposal);sample.close();
 const f=await setupOriginalBDepartureFixture(),pinned=JSON.parse(String(f.scalar('SELECT preview_json FROM operation_previews'))),plan=pinned.admission.operationMapPlan;
 f.sql.run('CREATE TABLE local_wave_owner21(revision INTEGER,supported INTEGER)');f.sql.run('INSERT INTO local_wave_owner21 VALUES(1,1)');
 const parent={prepare:async(c:Readonly<BDepartureCombatContext>)=>({source:{...structuredClone(raw),binding:{accountId:c.accountId,runId:c.runId,operationPreviewId:c.operationPreviewId,operationId:c.operationId,selectedRisk:c.selectedRisk,operationDefinitionVersion:c.definitionVersion,operationPlanHash:c.planHash,operationPublicationHash:c.publicationHash}},assertions:[f.env.DB.prepare('INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_original_b_owner19 WHERE revision=1 AND supported=1) THEN 1 ELSE 0 END')]}),supports:async()=>f.scalar('SELECT supported FROM local_original_b_owner19')===1};
 const owner={prepare:async()=>({manifest:structuredClone(proposal),assertions:[f.env.DB.prepare('INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM local_wave_owner21 WHERE revision=1 AND supported=1) THEN 1 ELSE 0 END')]}),supports:async()=>f.scalar('SELECT supported FROM local_wave_owner21')===1};
 async function startWave(){const row=await f.env.DB.prepare('SELECT * FROM operation_previews WHERE id=?').bind(f.id).first<Parameters<typeof startDeparture>[2]>();return startDeparture(f.env.DB,'a',row!,'local-original-wave-builder21',async(row,runId)=>({accepted:true,startStatus:'STARTED',runId,operationPreviewId:row.id,operationId:row.objective,risk:row.risk,balanceVersion:String(row.balance_config_version),encounterType:'ORDINARY_12_NODE'}),undefined,f.env.ENVIRONMENT,createBWaveDepartureSourceOwner(parent,owner,plan));}
 function failWaveAtCommit(kind:'source'|'support'){f.adapter.beforeBatch=()=>{delete f.adapter.beforeBatch;f.sql.run(kind==='source'?'UPDATE local_wave_owner21 SET revision=2':'UPDATE local_wave_owner21 SET supported=0');};}
 const restoreWave=()=>f.sql.run('UPDATE local_wave_owner21 SET revision=1,supported=1'),withdrawWave=()=>{f.sql.run('UPDATE local_wave_owner21 SET revision=9,supported=0');f.changeSource();};
 return {...f,startWave,failWaveAtCommit,restoreWave,withdrawWave};
}