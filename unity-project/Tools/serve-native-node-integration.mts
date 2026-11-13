import {writeFile,readFile,unlink} from 'node:fs/promises';
import {setTimeout as delay} from 'node:timers/promises';
import {strict as assert} from 'node:assert';
import app from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/index';
import {createAccessToken} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/auth';
import {bRouteFixture} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/helpers/combat-b-route-fixture';
import {startWorldBossLoopback} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/scripts/local/world-boss-loopback';
const connection=process.env.LASTRAIL_NATIVE_CONNECTION!,qa=process.env.LASTRAIL_NATIVE_QA!;
assert.ok(connection?.startsWith('D:/Project/LASTRAIL/LocalTemp/'));assert.ok(qa?.startsWith('D:/Project/LASTRAIL/QA/'));
const f=await bRouteFixture('NATIVE'),listeners:Awaited<ReturnType<typeof startWorldBossLoopback>>[]=[],trace:unknown[]=[];
try{
 const now=Date.now();f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'",[now+900000]);
 const token=await createAccessToken({secret:'departure-test-secret-at-least-32-chars',accountId:'a',sessionId:'s',nowMs:now,ttlMs:900000});
 const snapshotBefore=f.scalar('SELECT snapshot_json FROM run_snapshots');
 async function handle(req:Request,delayed=false){if(delayed)await delay(700);const response=await app.fetch(req,f.env);const p=await response.clone().json();trace.push({method:req.method,path:new URL(req.url).pathname,status:response.status,errorCode:p.errorCode??null,packetHash:p.packetHash??null,mapRevision:p.mapRevision??p.revision??null});return response;}
 const normal=await startWorldBossLoopback(r=>handle(r));listeners.push(normal);const late=await startWorldBossLoopback(r=>handle(r,true));listeners.push(late);
 await writeFile(connection,JSON.stringify({origin:normal.origin,delayedOrigin:late.origin,token,accountId:'a',runId:f.runId}));
 const deadline=Date.now()+900000;let stopped=false;while(Date.now()<deadline){try{await readFile(connection+'.stop');stopped=true;break;}catch(e){if((e as NodeJS.ErrnoException).code!=='ENOENT')throw e;}await delay(200);}assert.ok(stopped);
 const state=await f.map.read('a',f.runId),snapshotUnchanged=snapshotBefore===f.scalar('SELECT snapshot_json FROM run_snapshots');
 await writeFile(qa+'/native-server-state.json',JSON.stringify({evidence:'ORIGINAL_AUTH_HTTP_FROZEN_NATIVE_ORIGIN_LOCAL_FIXTURE',trace,state,snapshotUnchanged,operatingAcceptedProvider:false},null,2));assert.ok(snapshotUnchanged);assert.equal(state.revision,2);assert.equal(state.state.phase,'TRAVELLING');
}finally{try{const closed=await Promise.allSettled(listeners.map(l=>l.close()));try{assert.ok(closed.every(r=>r.status==='fulfilled'));}finally{f.sql.close();}await writeFile(qa+'/native-cleanup.json',JSON.stringify({listenersClosed:true,databaseClosed:true}));}finally{await unlink(connection).catch(e=>{if(e.code!=='ENOENT')throw e;});}}
