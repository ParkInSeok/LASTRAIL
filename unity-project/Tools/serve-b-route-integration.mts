// Isolated ORIGINAL fixture only. Its accepted SQL events are LOCAL test evidence.
import {writeFile,readFile,unlink} from 'node:fs/promises';
import {setTimeout as delay} from 'node:timers/promises';
import {strict as assert} from 'node:assert';
import app,{resolveAuthContext} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/index';
import {createAccessToken} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/auth';
import {registerBRouteRoutes} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/combat-b-route-routes';
import {bRouteFixture} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/helpers/combat-b-route-fixture';
import {startWorldBossLoopback} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/scripts/local/world-boss-loopback';
const connection=process.env.LASTRAIL_B_ROUTE_CONNECTION!,qa=process.env.LASTRAIL_B_ROUTE_QA!;
assert.ok(connection?.startsWith('D:/Project/LASTRAIL/LocalTemp/'));assert.ok(qa?.startsWith('D:/Project/LASTRAIL/QA/'));
const f=await bRouteFixture('ORIGINAL'),listeners:Awaited<ReturnType<typeof startWorldBossLoopback>>[]=[],trace:unknown[]=[];
try{
 const local=new (app.constructor as any)();registerBRouteRoutes(local,resolveAuthContext,{forAuthenticatedContext:async()=>f.owner});
 await f.record('ARRIVE','arrival','n1-0',2);await f.record('WAVE_COMPLETE','wave1','n1-0',3,[],1);await f.record('WAVE_COMPLETE','wave2','n1-0',3,[],2);await f.record('COMPLETE','COMPLETE_LOCAL','n1-0',3,['LOCAL_REWARD_HOLD']);await f.record('RELEASE_HOLD','RELEASE_LOCAL','n1-0',4,[],null,'LOCAL_REWARD_HOLD');
 const now=Date.now();f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'",[now+900000]);
 const token=await createAccessToken({secret:'departure-test-secret-at-least-32-chars',accountId:'a',sessionId:'s',nowMs:now,ttlMs:900000});
 const before=JSON.stringify(f.sql.exec('SELECT snapshot_json FROM run_snapshots'));
 async function handle(req:Request,accepted=false,delayed=false){if(delayed)await delay(700);const body=req.method==='GET'?null:await req.clone().json();const r=await (accepted?local:app).fetch(req,f.env);const packet=await r.clone().json();trace.push({method:req.method,path:new URL(req.url).pathname,body,status:r.status,packet,localAcceptedOwner:accepted});return r;}
 const normal=await startWorldBossLoopback(r=>handle(r));listeners.push(normal);const accepted=await startWorldBossLoopback(r=>handle(r,true));listeners.push(accepted);const late=await startWorldBossLoopback(r=>handle(r,false,true));listeners.push(late);
 await writeFile(connection,JSON.stringify({origin:normal.origin,acceptedOrigin:accepted.origin,delayedOrigin:late.origin,token,accountId:'a',runId:f.runId}));
 const deadline=Date.now()+900000;let stopped=false;while(Date.now()<deadline){try{await readFile(connection+'.stop');stopped=true;break;}catch(e){if((e as NodeJS.ErrnoException).code!=='ENOENT')throw e;}await delay(200);}assert.ok(stopped,'Unity test did not finish');
 const current=await f.map.read('a',f.runId),snapshotUnchanged=before===JSON.stringify(f.sql.exec('SELECT snapshot_json FROM run_snapshots'));
 await writeFile(qa+'/route-server-state.json',JSON.stringify({evidence:'ORIGINAL_AUTH_SQL_LOOPBACK_LOCAL_ACCEPTED_EVENT_OWNER',trace,current,snapshotUnchanged,operatingEvidenceProviderInstalled:false},null,2));assert.ok(snapshotUnchanged);assert.equal(current.revision,5);assert.equal(current.state.phase,'READY');assert.deepEqual(current.state.holdIds,[]);
}finally{
 try{const closed=await Promise.allSettled(listeners.map(l=>l.close()));try{assert.ok(closed.every(r=>r.status==='fulfilled'));}finally{f.sql.close();}await writeFile(qa+'/route-cleanup.json',JSON.stringify({listenerClosed:true,databaseClosed:true}));}finally{await unlink(connection).catch(e=>{if(e.code!=='ENOENT')throw e;});}
}
