import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {captureCombatAuthority} from "../../apps/game-api/src/combat-authority";
async function fixture(){const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();return {...f,runId:r.runId};}
const body=(runId:string,outcome="VICTORY")=>({runId,currentStage:"Complete",currentWaveNumber:0,completedWaveCount:15,outcome,runtimeStateJson:"{}",tacticSelections:[],clientCompletedAt:new Date().toISOString()});
it.each(["ORDINARY_COOP","WORLD_BOSS_SOLO","WORLD_BOSS_COOP"])("blocks legacy finish mutations for pinned %s regardless of reported outcome",async mode=>{
 const f=await fixture(),snapshot=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));snapshot.admission.combatAuthority=captureCombatAuthority(mode);snapshot.departureResponse.admission.combatAuthority=snapshot.admission.combatAuthority;
 f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(snapshot)]);
 for(const outcome of ["VICTORY","FAILURE","ABANDONED"]){const r=await f.post(`/v1/runs/${f.runId}/finish`,body(f.runId,outcome));expect(r.status).toBe(409);expect(await r.json()).toMatchObject({errorCode:"combat_finish_verification_required"});}
 expect(f.scalar("SELECT status FROM runs")).toBe("ACTIVE");expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);expect(f.scalar("SELECT count(*) FROM run_reward_previews")).toBe(0);
});
it("rejects malformed authority without treating it as a historical run",async()=>{
 const f=await fixture();f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.admission.combatAuthority.version','unknown')");
 const r=await f.post(`/v1/runs/${f.runId}/finish`,body(f.runId));expect(r.status).toBe(409);expect(await r.json()).toMatchObject({errorCode:"combat_authority_contract_invalid"});expect(f.scalar("SELECT count(*) FROM run_final_results")).toBe(0);
});
it("keeps ordinary solo finish and replays the committed receipt before current authority checks",async()=>{
 const f=await fixture(),request=body(f.runId),first=await f.post(`/v1/runs/${f.runId}/finish`,request,"original-finish");expect(first.status).toBe(200);
 const result=await first.json();expect(f.scalar("SELECT count(*) FROM run_reward_previews")).toBe(1);
 f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.admission.combatAuthority.version','unknown')");
 const replay=await f.post(`/v1/runs/${f.runId}/finish`,request,"original-finish");expect(replay.status).toBe(200);expect((await replay.json()).rewardPreviewId).toBe(result.rewardPreviewId);expect(f.scalar("SELECT count(*) FROM run_reward_previews")).toBe(1);
});
it("does not bypass a pinned departure response by omitting the outer authority",async()=>{
 const f=await fixture(),snapshot=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));
 delete snapshot.admission.combatAuthority;snapshot.departureResponse.admission.combatAuthority=captureCombatAuthority("ORDINARY_COOP");f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(snapshot)]);
 const r=await f.post(`/v1/runs/${f.runId}/finish`,{...body(f.runId),combatAuthority:captureCombatAuthority("ORDINARY_SOLO")});expect(r.status).toBe(409);expect(await r.json()).toMatchObject({errorCode:"combat_finish_verification_required"});expect(f.scalar("SELECT count(*) FROM run_reward_previews")).toBe(0);
});

it.each(['ordinaryRoofSource','ordinaryRailSource','ordinaryTrainSource','ordinaryStatSource','ordinaryInitialModifiers','ordinaryKitSource','ordinaryBasicSource','ordinaryRelocationSource','firstBattleEncounterSource'])(
 'does not downgrade an explicit %s to legacy finish, including nested null sources',async key=>{
 const f=await fixture();
 try{
  const original=JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  for(const nested of [false,true]){
   const snapshot=structuredClone(original);(nested?snapshot.departureResponse:snapshot)[key]=null;
   f.sql.run('UPDATE run_snapshots SET snapshot_json=?',[JSON.stringify(snapshot)]);
   const response=await f.post(`/v1/runs/${f.runId}/finish`,body(f.runId),`new-source-${nested}`);
   expect(response.status).toBe(409);expect(await response.json()).toMatchObject({errorCode:'combat_finish_verification_required'});
   expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(0);
  }
 }finally{f.sql.close();}
});

it('replays an already committed legacy finish before later five-actor source presence',async()=>{
 const f=await fixture();
 try{
  const request=body(f.runId),first=await f.post(`/v1/runs/${f.runId}/finish`,request,'stored-finish');
  expect(first.status).toBe(200);const original=await first.json();
  f.sql.run("UPDATE run_snapshots SET snapshot_json=json_set(snapshot_json,'$.ordinaryStatSource',null)");
  const replay=await f.post(`/v1/runs/${f.runId}/finish`,request,'stored-finish');
  expect(replay.status).toBe(200);expect((await replay.json()).rewardPreviewId).toBe(original.rewardPreviewId);
  expect(f.scalar('SELECT count(*) FROM run_final_results')).toBe(1);
 }finally{f.sql.close();}
});
