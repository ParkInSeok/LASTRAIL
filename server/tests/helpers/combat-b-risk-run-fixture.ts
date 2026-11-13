import {setup} from './departure-fixture';
import {plan} from './operation-map-fixture';
import {OperationMapPublisher} from '../../apps/game-api/src/operation-map-publisher';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {captureCombatAuthority} from '../../apps/game-api/src/combat-authority';
import {departureVersion} from '../../apps/game-api/src/departure';
import {initialMapProgress} from '../../apps/game-api/src/operation-map-state';
/** Persisted retained-run metadata in isolated SQL only. No START, fuel debit, reward or HP seed. */
export async function setupRiskRunFixture(includePublication=true){const f=await setup(true),p=plan(),runId='LOCAL_RETAINED_RISK_RUN',previewId='LOCAL_RETAINED_PREVIEW';
 for(const n of p.nodes){n.encounterProfileRef='local-encounter-'+n.eventKind;n.rewardPlanRef='local-reward-'+n.eventKind;}
 const kinds=[...new Set(p.nodes.map(n=>n.eventKind))];
 const catalog={read:async()=>({version:'operation.map-catalog.v1',catalogId:'LOCAL_METADATA_ONLY_ORIGINAL_CATALOG',revision:1,definitionVersion:p.definitionVersion,encounters:kinds.map(eventKind=>({ref:'local-encounter-'+eventKind,version:'original-local-v1',profile:'ORDINARY_12_NODE',eventKind,definition:{executor:'UNSUPPORTED_ORIGINAL_LOCAL_METADATA_ONLY'}})),rewards:kinds.map(eventKind=>({ref:'local-reward-'+eventKind,version:'original-local-v1',profile:'ORDINARY_12_NODE',eventKind,definition:{}}))})};
 const publication=await new OperationMapPublisher(catalog).publish(p);
 const response={runId,operationPreviewId:previewId,operationId:'opn_01_last_departure',risk:'V',balanceVersion:'1',contentVersion:'LOCAL_ORIGINAL_METADATA_v1',encounterType:'ORDINARY_12_NODE',admission:{version:departureVersion,combatAuthority:captureCombatAuthority('ORDINARY_SOLO'),operationMapPlan:p}};
 const snapshot={...response,...(includePublication?{operationMapContent:publication}:{}),departureResponse:structuredClone(response)},text=JSON.stringify(snapshot);
 f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES('local-retained-mutation','a',100,'RUN_START','local-retained',0)");
 f.sql.run("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) VALUES(?,'a','local-retained-mutation','opn_01_last_departure','V',1,?,'CONSUMED',0,0,?)",[previewId,JSON.stringify(response),departureVersion]);
 f.sql.run("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,'a',?,'local-retained-mutation',1,'ACTIVE',0)",[runId,previewId]);
 f.sql.run('INSERT INTO run_snapshots VALUES(?,?,0)',[runId,text]);await new OperationMapStore(f.env.DB).initialize('a',runId);
 function setProgress(depth:number,phase='ACTIVE',pendingEdgeId:string|null=null){let current=p.entryNodeId;const traversedEdgeIds:string[]=[];for(let i=0;i<depth;i++){const e=p.edges.find(e=>e.fromNodeId===current)!;traversedEdgeIds.push(e.edgeId);current=e.toNodeId;}const state={...initialMapProgress(),traversedEdgeIds,phase,pendingEdgeId};f.sql.run('UPDATE operation_map_states SET revision=revision+1,state_json=? WHERE run_id=?',[JSON.stringify(state),runId]);return current;}
 return {...f,runId,previewId,plan:p,snapshot,snapshotText:text,setProgress,close:()=>f.sql.close()};}