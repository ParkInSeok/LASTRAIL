// Isolated original auth/routes/SQL and LOCAL original map fixture. No operating grant.
import {writeFile,readFile,unlink} from 'node:fs/promises';
import {setTimeout as delay} from 'node:timers/promises';
import {strict as assert} from 'node:assert';
import app from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/index';
import {createAccessToken} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/auth';
import {safeTransitionFixture} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/helpers/equipment-safe-transition-fixture';
import {startWorldBossLoopback} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/scripts/local/world-boss-loopback';
const connection=process.env.LASTRAIL_SAFE_CONNECTION!,qa=process.env.LASTRAIL_SAFE_QA!;
assert.ok(connection?.startsWith('D:/Project/LASTRAIL/LocalTemp/'));assert.ok(qa?.startsWith('D:/Project/LASTRAIL/QA/'));
const f=await safeTransitionFixture(),now=Date.now();f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'",[now+900000]);
const token=await createAccessToken({secret:'departure-test-secret-at-least-32-chars',accountId:'a',sessionId:'s',nowMs:now,ttlMs:900000});
const protectedBefore=f.sql.exec('SELECT snapshot_json FROM run_snapshots; SELECT state_json FROM operation_map_states; SELECT * FROM equipment_safe_states');
const trace:unknown[]=[],listeners:Awaited<ReturnType<typeof startWorldBossLoopback>>[]=[];
async function handle(req:Request,delayed=false){if(delayed)await delay(700);const request=req.method==='GET'?null:await req.clone().json();const response=await app.fetch(req,f.env);const p=await response.clone().json() as Record<string,unknown>;trace.push({method:req.method,path:new URL(req.url).pathname,status:response.status,request,etag:response.headers.get('ETag'),errorCode:p.errorCode??null,accepted:p.accepted??null});return response;}
try{
 const normal=await startWorldBossLoopback(r=>handle(r));listeners.push(normal);
 const late=await startWorldBossLoopback(r=>handle(r,true));listeners.push(late);
 await writeFile(connection,JSON.stringify({origin:normal.origin,delayedOrigin:late.origin,token,accountId:'a',runId:f.runId}));
 const deadline=Date.now()+900000;while(Date.now()<deadline){try{await readFile(connection+'.stop');break;}catch(e){if((e as NodeJS.ErrnoException).code!=='ENOENT')throw e;}await delay(200);}
}finally{
 try{
  const closed=await Promise.allSettled(listeners.map(l=>l.close()));
  let protectedUnchanged=false,diagnosticReceipts:unknown;
  try{const protectedAfter=f.sql.exec('SELECT snapshot_json FROM run_snapshots; SELECT state_json FROM operation_map_states; SELECT * FROM equipment_safe_states');protectedUnchanged=JSON.stringify(protectedBefore)===JSON.stringify(protectedAfter);diagnosticReceipts=f.scalar('SELECT COUNT(*) FROM equipment_safe_transition_receipts');}finally{f.sql.close();}
  await writeFile(qa+'/server-trace.json',JSON.stringify({evidence:'REAL_LOOPBACK_ORIGINAL_AUTH_ROUTE_SQL_LOCAL_MAP_FIXTURE',trace,protectedUnchanged,diagnosticReceipts,listenerClosed:closed.every(r=>r.status==='fulfilled'),databaseClosed:true},null,2));
  assert.ok(protectedUnchanged);assert.ok(closed.every(r=>r.status==='fulfilled'));
 }finally{await unlink(connection).catch(e=>{if(e.code!=='ENOENT')throw e;});}
}
