// U30 diagnostic owner only. Original server modules and SQL/auth fixtures remain unchanged.
import {writeFile,readFile,unlink} from 'node:fs/promises';
import {setTimeout as delay} from 'node:timers/promises';
import {Buffer} from 'node:buffer';
import {strict as assert} from 'node:assert';
import app,{resolveAuthContext} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/index';
import {createAccessToken} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/auth';
import {namedFixture} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/helpers/combat-b-named-operator-fixture';
import {BRefitStore} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/combat-b-refit-store';
import {LocalRefitPersistenceSource} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/combat-b-refit-persistence-source';
import {LocalFixtureBSelectionCatalog} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/combat-b-selection-catalog';
import {registerBNamedEffectiveSourceRoutes} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/combat-b-named-effective-source-routes';
import {startWorldBossLoopback} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/scripts/local/world-boss-loopback';
const connection=process.env.LASTRAIL_U30_CONNECTION!,qa=process.env.LASTRAIL_U30_QA!;
assert.ok(connection?.startsWith('D:/Project/LASTRAIL/LocalTemp/'));assert.ok(qa?.startsWith('D:/Project/LASTRAIL/QA/'));
const f=await namedFixture(true),local=new (app.constructor as new()=>typeof app)();
const now=Date.now();f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'",[now+900000]);
const token=await createAccessToken({secret:'departure-test-secret-at-least-32-chars',accountId:'a',sessionId:'s',nowMs:now,ttlMs:900000});
// The fixture's captured 60-second principal expires during Unity boot. Read its
// same persisted refit using the original store and the longer isolated session.
const refitStore=new BRefitStore(f.env.DB,new LocalRefitPersistenceSource(),new LocalFixtureBSelectionCatalog());
registerBNamedEffectiveSourceRoutes(local,resolveAuthContext,{forAuthenticatedContext:async(_c,account)=>{assert.equal(account,'a');return {read:accountId=>{assert.equal(accountId,'a');return refitStore.read({accountId,sessionId:'s',accessExpiresAt:now+900000});}};}});
const before=Buffer.from(f.sql.export()),trace:unknown[]=[],listeners:Awaited<ReturnType<typeof startWorldBossLoopback>>[]=[];
async function handle(req:Request,privateProvider:boolean,delayed=false){
 if(delayed)await delay(700);
 const body=await req.clone().json();const response=await (privateProvider?local:app).fetch(req,f.env);
 const packet=await response.clone().json() as Record<string,unknown>;
 trace.push({path:new URL(req.url).pathname,privateProvider,delayed,version:req.headers.get(privateProvider?'X-B-Named-Effective-Version':new URL(req.url).pathname.endsWith('effective-source')?'X-B-Named-Effective-Version':'X-B-Named-Operator-Version'),request:body,ifMatch:req.headers.get('If-Match'),status:response.status,errorCode:packet.errorCode??null,etag:response.headers.get('ETag'),packetHash:packet.packetHash??null});
 return response;
}
try{
 const publicListener=await startWorldBossLoopback(req=>handle(req,false));listeners.push(publicListener);
 const privateListener=await startWorldBossLoopback(req=>handle(req,true));listeners.push(privateListener);
 const delayedListener=await startWorldBossLoopback(req=>handle(req,true,true));listeners.push(delayedListener);
 // Credentials live only in the project-local temporary connection file; never stdout/QA.
 await writeFile(connection,JSON.stringify({publicOrigin:publicListener.origin,privateOrigin:privateListener.origin,delayedOrigin:delayedListener.origin,token}));
 const deadline=Date.now()+900000;
 while(Date.now()<deadline){try{await readFile(connection+'.stop');break;}catch(error){if((error as NodeJS.ErrnoException).code!=='ENOENT')throw error;}await delay(200);}
}finally{
 try{
  const closed=await Promise.allSettled(listeners.map(l=>l.close()));
  let noReadWrites=false;try{noReadWrites=before.equals(Buffer.from(f.sql.export()));}finally{f.sql.close();}
  const listenerClosed=closed.every(result=>result.status==='fulfilled');
  await writeFile(qa+'/server-trace.json',JSON.stringify({trace,noReadWrites,listenerClosed,databaseClosed:true,scope:'isolated synthetic account; original routes/auth/SQL; original refit store with 15-minute test principal; no START'},null,2)+'\n');
  assert.ok(listenerClosed,'Isolated listener cleanup failed');assert.ok(noReadWrites,'Read-only HTTP verification changed isolated database');
 }finally{await unlink(connection).catch(error=>{if(error.code!=='ENOENT')throw error;});}
}
