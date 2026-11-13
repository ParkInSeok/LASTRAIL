import {createServer} from 'node:http';
import {createServer as createOwnershipServer} from 'node:net';
import {randomBytes} from 'node:crypto';
import {closeSync,existsSync,fsyncSync,openSync,readFileSync,realpathSync,renameSync,writeFileSync,appendFileSync} from 'node:fs';
import {join,relative,resolve} from 'node:path';
import initSqlJs from 'sql.js';
import app from '../../apps/game-api/src/index';
import {createAccessToken} from '../../apps/game-api/src/auth';
import {sha256Hex} from '@lastrail/common';
import {SqlJsD1Database} from '../../tests/helpers/growth-d1';
import {createAugmentSelectFixture} from '../../tests/helpers/augment-select-fixture';
import {SoloAugmentStore} from '../../apps/game-api/src/solo-augment-store';
import {captureAugmentTargetReadout} from '../../apps/game-api/src/augment-target-readout';
const root='D:/Project/LASTRAIL/QA/CommanderS1-20260922';
const version='augment.loopback-http.v1.20261021';
type Identity={runId:string;eventId:string;selectPath:string;selectBody:unknown};
type Trace={method:string;path:string;status:number};
/** Explicit local transport trial. Reuses unchanged app authentication and SELECT.
 * Only its isolated synthetic DB is mutable; START/economy/other endpoints are
 * not exposed. Every response waits for durable DB replacement. The directory
 * has one exclusive owner; it must never be shared with a deployed D1 binding.
 */
export async function startAugmentLoopback({directory,lifetimeMs=1800000}:{directory:string;lifetimeMs?:number}){
 if(!Number.isSafeInteger(lifetimeMs)||lifetimeMs<100||lifetimeMs>1800000)throw Error('local_lifetime_invalid');
 const dir=realpathSync(resolve(directory)),rel=relative(realpathSync(root),dir);
 if(!rel||rel.startsWith('..')||rel.includes(':'))throw Error('local_directory_forbidden');
 // OS-owned loopback lease is released even on process death. Hash collisions
 // fail closed as EADDRINUSE; never pick another port and open the same DB twice.
 const ownershipPort=40000+parseInt((await sha256Hex(dir.toLowerCase())).slice(0,8),16)%20000;
 const ownership=createOwnershipServer(socket=>socket.destroy());
 await new Promise<void>((ok,no)=>{ownership.once('error',no);ownership.listen({host:'127.0.0.1',port:ownershipPort,exclusive:true},()=>{ownership.off('error',no);ok();});});
 const databasePath=join(dir,'fixture.sqlite'),temporaryPath=join(dir,'fixture.sqlite.next');
 let sql:Awaited<ReturnType<typeof initSqlJs>>['Database']['prototype']|undefined;
 let releaseDone=false;
 const release=async()=>{if(releaseDone)return;releaseDone=true;sql?.close();await new Promise<void>(r=>ownership.close(()=>r()));};
 try{
  let identity:Identity;
  if(existsSync(databasePath)){
   const SQL=await initSqlJs();sql=new SQL.Database(readFileSync(databasePath));
   sql.create_function('unixepoch',()=>Date.now()/1000);sql.run('PRAGMA foreign_keys=ON');
   const row=sql.exec('SELECT identity_json FROM local_augment_http_trial WHERE version=?',[version]);
   if(row.length!==1||row[0]!.values.length!==1)throw Error('local_database_identity_invalid');
   identity=JSON.parse(String(row[0]!.values[0]![0]));
  }else{
   const f=await createAugmentSelectFixture();sql=f.sql;
   identity={runId:f.runId,eventId:'grant',selectPath:f.url,selectBody:f.body};
   sql.run('CREATE TABLE local_augment_http_trial(version TEXT PRIMARY KEY,identity_json TEXT NOT NULL)');
   sql.run('INSERT INTO local_augment_http_trial VALUES(?,?)',[version,JSON.stringify(identity)]);
  }
  const database=sql;
  if(!/^\/v1\/runs\/[^/]+\/augment-grants\/grant\/select$/u.test(identity.selectPath)||identity.selectPath!==`/v1/runs/${identity.runId}/augment-grants/${identity.eventId}/select`)throw Error('local_database_identity_invalid');
  const persist=()=>{
   const bytes=database.export(),fd=openSync(temporaryPath,'w');
   try{writeFileSync(fd,bytes);fsyncSync(fd);}finally{closeSync(fd);}
   renameSync(temporaryPath,databasePath);
  };
  const now=Date.now(),secret=randomBytes(32).toString('hex');
  database.run("UPDATE auth_sessions SET expires_at=? WHERE id='s' AND account_id='a'",[now+lifetimeMs+60000]);
  const accessToken=await createAccessToken({secret,accountId:'a',sessionId:'s',nowMs:now,ttlMs:lifetimeMs});
  const env={DB:new SqlJsD1Database(database) as unknown as D1Database,ENVIRONMENT:'DEV',AUTH_SIGNING_KEY_CURRENT:secret,ALLOWED_ADMIN_ORIGINS:'http://localhost:5173'};
  const initialSelection=await new SoloAugmentStore(env.DB).read({accountId:'a',environment:'DEV'},identity.runId,identity.eventId);
  const startSnapshotJson=String(database.exec('SELECT snapshot_json FROM run_snapshots WHERE run_id=?',[identity.runId])[0]!.values[0]![0]);
  const startSnapshotHash=await sha256Hex(startSnapshotJson);
  persist();
  const trace:Trace[]=[];let tail=Promise.resolve(),failed=false,closing:Promise<void>|undefined;
  let finish!:()=>void;const closed=new Promise<void>(r=>{finish=r;});
  const server=createServer((req,res)=>{
   const handle=async()=>{
    const address=server.address(),host=address&&typeof address!=='string'?'127.0.0.1:'+address.port:'';
    const reject=(status:number,error:string)=>{res.writeHead(status,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({error}));};
    if(failed||closing){reject(503,'local_owner_unavailable');return;}
    if(req.headers.host!==host||req.headers.origin){reject(403,'local_host_forbidden');return;}
    const viewPath=identity.selectPath.slice(0,-7),method=req.method??'GET';
    if(!((method==='POST'&&(req.url===identity.selectPath||req.url===viewPath+'/reroll'))||(method==='GET'&&req.url===viewPath))){reject(404,'local_scope_unavailable');return;}
    try{
     const chunks:Buffer[]=[];let size=0;
     for await(const chunk of req){size+=chunk.length;if(size>16384){reject(413,'local_body_limit');return;}chunks.push(Buffer.from(chunk));}
     const headers=new Headers();for(const [k,v] of Object.entries(req.headers))if(v!==undefined)headers.set(k,Array.isArray(v)?v.join(','):v);
     const response=await app.fetch(new Request('http://'+host+req.url,{method,headers,...(method==='GET'?{}:{body:Buffer.concat(chunks)})}),env);
     const bytes=Buffer.from(await response.arrayBuffer());
     // Failure after RAM commit is unknown, never acknowledged as durable success.
     persist();
     const entry={method,path:req.url!,status:response.status};trace.push(entry);appendFileSync(join(dir,'requests.jsonl'),JSON.stringify(entry)+'\n');
     res.writeHead(response.status,Object.fromEntries(response.headers.entries()));res.end(bytes);
    }catch{failed=true;if(!res.headersSent)reject(503,'local_persistence_or_transport_failed');else res.destroy();}
   };
   tail=tail.then(handle).catch(()=>{failed=true;res.destroy();});
  });
  server.requestTimeout=10000;server.headersTimeout=10000;
  await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',()=>{server.off('error',no);ok();});});
  const address=server.address();if(!address||typeof address==='string')throw Error('local_bind_failed');
  const close=()=>closing??=(async()=>{clearTimeout(timer);await new Promise<void>(r=>{server.close(()=>r());server.closeAllConnections();});await tail;await release();finish();})();
  const timer=setTimeout(()=>{void close();},lifetimeMs);
  const bootstrap={version,classification:'EXPLICIT_LOCAL_SYNTHETIC_NOT_OPERATING',baseUrl:'http://127.0.0.1:'+address.port,accountId:'a',environment:'DEV',...identity,
   accessToken,tokenExpiresAt:now+lifetimeMs,initialSelection,targetReadout:await captureAugmentTargetReadout(initialSelection),rerollPath:identity.selectPath.slice(0,-7)+'/reroll',rerollVersion:'combat.augment-reroll.v1.20261021',startSnapshotJson,startSnapshotHash,
   supportedMode:'ORDINARY_SOLO',effectId:'O22',runtimeApplyRequired:true,independentArrivalVerified:false,actualPlayerVerified:false};
  const count=(table:string)=>Number(database.exec('SELECT count(*) FROM '+table)[0]!.values[0]![0]);
  return {bootstrap,close,closed,requests:()=>structuredClone(trace),inspect:()=>({receipts:count('solo_augment_receipts'),originals:count('run_augment_effect_originals')})};
 }catch(error){await release();throw error;}
}
