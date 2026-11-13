import {guardedRestoreFixture} from "../helpers/combat-restore-fixture";
import {setCombatSqlClock} from "../helpers/combat-sql-clock";
import {expect,it} from "vitest";
import {setup} from "../helpers/departure-fixture";
import {CombatHostReportStore} from "../../apps/game-api/src/combat-host-report-store";
import {captureCombatAuthority} from "../../apps/game-api/src/combat-authority";
async function fixture(coop=true){
 const f=await setup(true),q=await(await f.preview()).json(),r=await(await f.start(q.operationPreviewId)).json();
 // Test-only future coop START fixture; public coop admission remains unsupported.
 const snapshot=JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots")));
 if(coop){snapshot.admission.combatAuthority=captureCombatAuthority("ORDINARY_COOP");snapshot.departureResponse.admission.combatAuthority=snapshot.admission.combatAuthority;f.sql.run("UPDATE run_snapshots SET snapshot_json=?",[JSON.stringify(snapshot)]);}
 const context={authority:captureCombatAuthority("ORDINARY_COOP"),runId:r.runId,participants:[{accountId:"a",role:"TRAIN"},{accountId:"b",role:"OPERATOR"}],activeAccountIds:["a","b"],hostAccountId:"a",hostGeneration:1,definitionVersion:snapshot.admission.combatPolicyRequirements.version,snapshotRevision:1,lastStateSequence:0,logicalTimeMs:0};
 const store=new CombatHostReportStore(f.env.DB);return {...f,store,context,runId:r.runId};
}

async function paused(retained:boolean){const f=await fixture();await f.store.initialize(f.context);await f.store.recordDisconnect(f.runId,{requestId:"away",expectedRevision:1,disconnectedAccountId:"a",observedAt:1000});if(retained)await f.store.recordSuspend(f.runId,{requestId:"store",expectedRevision:2,observedAt:100000});return {...f,revision:retained?3:2,deadline:retained?86500000:61000};}
function restorer(f:Awaited<ReturnType<typeof paused>>,verifiedAt:number){return new CombatHostReportStore(f.env.DB,guardedRestoreFixture(f.env.DB,{verify:async c=>({checkpointId:"cp",transactionCursor:"tx",verifiedAt,restoredAccountIds:JSON.parse(c.nextContextJson).activeAccountIds})}));}
it("rejects publication at exact expiry even when verification finished in time",async()=>{for(const retained of [false,true]){const f=await paused(retained);let now=f.deadline-1;setCombatSqlClock(f.sql,()=>now);f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;now=f.deadline;};await expect(restorer(f,f.deadline-1).resolveRecovery("a",f.runId,{requestId:"late",expectedRevision:f.revision,mode:"REJOIN"})).rejects.toThrow("combat_host_state_conflict");expect(f.scalar("SELECT count(*) FROM combat_host_resume_receipts")).toBe(0);expect((await f.store.read("a",f.runId)).revision).toBe(f.revision);}});
it("also fences a suspended solo transition and rejects a database clock behind verification",async()=>{for(const regressed of [false,true]){const f=await paused(true);setCombatSqlClock(f.sql,()=>regressed?100000:f.deadline);await expect(restorer(f,100001).resolveRecovery("b",f.runId,{requestId:"solo",expectedRevision:f.revision,mode:"SOLO"})).rejects.toThrow("combat_host_state_conflict");expect(f.scalar("SELECT count(*) FROM combat_host_lineage")).toBe(1);}});
it("commits one millisecond before expiry and replays the original receipt after expiry",async()=>{const f=await paused(true);let now=f.deadline-1;setCombatSqlClock(f.sql,()=>now);const store=restorer(f,now),c={requestId:"resume",expectedRevision:f.revision,mode:"REJOIN"};const first=await store.resolveRecovery("a",f.runId,c);now=f.deadline+100;expect(await store.resolveRecovery("a",f.runId,c)).toEqual(first);expect(f.scalar("SELECT count(*) FROM combat_host_resume_receipts")).toBe(1);});
it("does not impose the elapsed short rejoin window on an explicit unsuspended solo choice",async()=>{const f=await paused(false);setCombatSqlClock(f.sql,()=>100000);expect(await restorer(f,99999).resolveRecovery("b",f.runId,{requestId:"solo",expectedRevision:f.revision,mode:"SOLO"})).toMatchObject({activeAccountIds:["b"]});});
