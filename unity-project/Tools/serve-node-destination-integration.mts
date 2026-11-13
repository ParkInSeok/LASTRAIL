// Original public destination route, isolated fixture only. No arrival API invented.
import {writeFile,readFile,unlink} from 'node:fs/promises';
import {setTimeout as delay} from 'node:timers/promises';
import {strict as assert} from 'node:assert';
import app from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/index';
import {createAccessToken} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/auth';
import {safeTransitionFixture} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/helpers/equipment-safe-transition-fixture';
import {startWorldBossLoopback} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/scripts/local/world-boss-loopback';
const connection=process.env.LASTRAIL_NODE_CONNECTION!,qa=process.env.LASTRAIL_NODE_QA!;
assert.ok(connection?.startsWith('D:/Project/LASTRAIL/LocalTemp/'));assert.ok(qa?.startsWith('D:/Project/LASTRAIL/QA/'));
const f=await safeTransitionFixture(),listeners:Awaited<ReturnType<typeof startWorldBossLoopback>>[]=[],trace:unknown[]=[];
try{
 // The original fixture's internal LOCAL verifier settles its own synthetic reward.
 // This is test setup, not a public hold-release command or operating authority.
 await f.map.commit('a',f.runId,{kind:'RELEASE_HOLD',eventId:'local-fixture-settled',expectedRevision:4,holdId:'reward',evidenceRef:'LOCAL'});
 const now=Date.now();f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'",[now+900000]);
 const token=await createAccessToken({secret:'departure-test-secret-at-least-32-chars',accountId:'a',sessionId:'s',nowMs:now,ttlMs:900000});
 const protectedBefore=JSON.stringify(f.sql.exec('SELECT snapshot_json FROM run_snapshots; SELECT * FROM equipment_safe_states'));
 async function handle(req:Request,delayed=false){if(delayed)await delay(700);const body=req.method==='GET'?null:await req.clone().json();const r=await app.fetch(req,f.env);const p=await r.clone().json() as Record<string,unknown>;trace.push({method:req.method,path:new URL(req.url).pathname,body,status:r.status,errorCode:p.errorCode??null});return r;}
 const normal=await startWorldBossLoopback(r=>handle(r));listeners.push(normal);const late=await startWorldBossLoopback(r=>handle(r,true));listeners.push(late);
 await writeFile(connection,JSON.stringify({origin:normal.origin,delayedOrigin:late.origin,token,accountId:'a',runId:f.runId,edgeId:'n1-0:n2-0'}));
 const deadline=Date.now()+900000;while(Date.now()<deadline){try{await readFile(connection+'.stop');break;}catch(e){if((e as NodeJS.ErrnoException).code!=='ENOENT')throw e;}await delay(200);}
 const current=await f.map.read('a',f.runId);const protectedUnchanged=protectedBefore===JSON.stringify(f.sql.exec('SELECT snapshot_json FROM run_snapshots; SELECT * FROM equipment_safe_states'));
 await writeFile(qa+'/destination-server-state.json',JSON.stringify({evidence:'ORIGINAL_AUTH_SQL_LOOPBACK_LOCAL_MAP_SETUP',trace,protectedUnchanged,current,localFixtureHoldReleaseOnly:true,operatingArrival:false},null,2));assert.ok(protectedUnchanged);assert.equal(current.revision,6);assert.equal(current.state.phase,'TRAVELLING');assert.equal(current.state.pendingEdgeId,'n1-0:n2-0');
}finally{
 try{const closed=await Promise.allSettled(listeners.map(l=>l.close()));try{assert.ok(closed.every(r=>r.status==='fulfilled'));}finally{f.sql.close();}await writeFile(qa+'/destination-cleanup.json',JSON.stringify({listenerClosed:true,databaseClosed:true}));}finally{await unlink(connection).catch(e=>{if(e.code!=='ENOENT')throw e;});}
}
