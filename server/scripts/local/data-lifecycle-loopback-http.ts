import {createServer} from 'node:http';
import {randomBytes} from 'node:crypto';
import {setup} from '../../tests/helpers/departure-fixture';
import app from '../../apps/game-api/src/index';
import {createAccessToken,hashRefreshToken} from '../../apps/game-api/src/auth';
import {sha256Hex} from '@lastrail/common';
import economy from '../../packages/balance/src/launch-economy-v1.json';
import {objectivePeriods,objectiveDayMs} from '../../apps/game-api/src/recurring-policy';

/** Isolated memory-only trial, never a deployed binding. Same-process ACK recovery
 * is supported; process restart creates a different trial/account state. */
export async function startDataLifecycleLoopback({lifetimeMs=1800000,onRequest}:{lifetimeMs?:number;onRequest?:(entry:{method:string;path:string;status:number;bytes:number})=>void}={}){
 if(!Number.isSafeInteger(lifetimeMs)||lifetimeMs<100||lifetimeMs>1800000)throw Error('local_lifetime_invalid');
 const f=await setup();
 try{
  const now=Date.now(),secret=randomBytes(32).toString('hex'),configuration=JSON.stringify(economy);
  f.sql.run('INSERT INTO economy_versions VALUES(?,?,?,0)',[economy.version,await sha256Hex(configuration),configuration]);
  f.sql.run('INSERT INTO economy_version_bindings VALUES(1,?)',[economy.version]);
  for(const currency of ['CREDITS','MACHINE_PARTS','SPECIAL_CORE','OPERATOR_EXP','TACTICAL_DATA'])f.sql.run('INSERT INTO account_currencies(account_id,currency,balance,updated_at) VALUES(?,?,100000,0)',['a',currency]);
  const period=objectivePeriods(now);
  f.sql.run("INSERT INTO recurring_definitions VALUES('trial-quests','QUESTS',?,?,?)",[period.weekStart,period.weekStart+7*objectiveDayMs,JSON.stringify({version:'recurring-v1',cosmetics:{FRAME:null,OPERATOR_SKIN:null,TRAIN_SKIN:null},purchase:null})]);
  f.sql.run("INSERT INTO recurring_awards VALUES('trial-award','a','trial-quests','QUESTS',?,'D2',?,?,0,?)",[String(period.dayStart),now,period.dayEnd,JSON.stringify([{kind:'CURRENCY',id:'cur_credits',quantity:100}])]);
  f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'",[now+lifetimeMs+60000]);
  const refreshToken=randomBytes(32).toString('hex');
  f.sql.run("UPDATE auth_sessions SET refresh_token_hash=? WHERE id='s'",[await hashRefreshToken(refreshToken)]);
  f.sql.run("INSERT INTO auth_identities VALUES('trial-linked','a','GOOGLE','EXPLICIT_SYNTHETIC_NOT_REAL_GOOGLE',0)");
  f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('trial-b','b',?,'trial-family-b',0,'ACTIVE',?,?)",[randomBytes(32).toString('hex'),now,now+lifetimeMs]);
  const secondaryAccount={accountId:'b',accessToken:await createAccessToken({secret,accountId:'b',sessionId:'trial-b',nowMs:now,ttlMs:lifetimeMs})};
  const accessToken=await createAccessToken({secret,accountId:'a',sessionId:'s',nowMs:now,ttlMs:lifetimeMs});
  const env={...f.env,AUTH_SIGNING_KEY_CURRENT:secret};
  const getPaths=new Set(['/v1/account/state','/v1/recurring/state','/v1/attendance/monthly/current','/v1/gacha/banners','/v1/offline-rewards/preview','/v1/growth/config','/v1/trains/loadout','/v1/formation/ultimate']);
  const postPaths=new Set(['/v1/formation/presets/p/save','/v1/formation/presets/p/activate','/v1/trains/train_a/level-up','/v1/offline-rewards/claim','/v1/trains/loadout','/v1/formation/ultimate','/v1/auth/refresh','/v1/auth/logout','/v1/quests/trial-award/claim','/v1/recurring/bulk-claims']);
  let tail=Promise.resolve(),closing:Promise<void>|undefined,finish!:()=>void;
  const closed=new Promise<void>(r=>{finish=r;});
  const server=createServer((req,res)=>{
   const handle=async()=>{
    const address=server.address(),host=address&&typeof address!=='string'?'127.0.0.1:'+address.port:'';
    const reject=(status:number,error:string)=>{res.writeHead(status,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({error}));};
    if(closing){reject(503,'local_closing');return;}
    if(req.headers.host!==host||req.headers.origin){reject(403,'local_host_forbidden');return;}
    const method=req.method??'GET',path=req.url??'';
    if(!(method==='GET'?getPaths.has(path):method==='POST'&&postPaths.has(path))){reject(404,'local_scope_unavailable');return;}
    try{
     const chunks:Buffer[]=[];let size=0;
     for await(const chunk of req){size+=chunk.length;if(size>16384){reject(413,'local_body_limit');return;}chunks.push(Buffer.from(chunk));}
     const headers=new Headers();for(const [key,value] of Object.entries(req.headers))if(value!==undefined)headers.set(key,Array.isArray(value)?value.join(','):value);
     const response=await app.fetch(new Request('http://'+host+path,{method,headers,...(method==='GET'?{}:{body:Buffer.concat(chunks)})}),env);
     const bytes=Buffer.from(await response.arrayBuffer());onRequest?.({method,path,status:response.status,bytes:bytes.length});
     res.writeHead(response.status,Object.fromEntries(response.headers));res.end(bytes);
    }catch{if(!res.headersSent)reject(503,'local_transport_failed');else res.destroy();}
   };
   tail=tail.then(handle).catch(()=>{res.destroy();});
  });
  server.requestTimeout=10000;server.headersTimeout=10000;
  await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',()=>{server.off('error',no);ok();});});
  const address=server.address();if(!address||typeof address==='string')throw Error('local_bind_failed');
  const close=()=>closing??=(async()=>{clearTimeout(timer);await new Promise<void>(r=>{server.close(()=>r());server.closeAllConnections();});await tail;f.sql.close();finish();})();
  const timer=setTimeout(()=>{void close();},lifetimeMs);
  return {close,closed,bootstrap:{version:'data.lifecycle-loopback.v1.20261021',classification:'EXPLICIT_LOCAL_SYNTHETIC_NOT_OPERATING',baseUrl:'http://127.0.0.1:'+address.port,accessToken,refreshToken,secondaryAccount,tokenExpiresAt:now+lifetimeMs,accountId:'a',environment:'DEV',persistence:'MEMORY_ONLY_RESTART_IS_NEW_TRIAL',economyVersion:economy.version,
   formationCommand:{path:'/v1/formation/presets/p/save',body:{name:'Lifecycle trial',expectedRevision:1,operators:Array.from({length:5},(_,slot)=>({slot,operatorId:'op'+slot})),responseVersion:'formation.command.v2.20261021'}},
   questClaim:{path:'/v1/quests/trial-award/claim',awardId:'trial-award',rightKey:'quest:trial-award',directClaimEndsAt:period.dayEnd,classification:'EXPLICIT_SYNTHETIC_EARNED_RIGHT'},
   bulkClaim:{path:'/v1/recurring/bulk-claims',body:{kind:'QUEST',awardIds:['trial-award','not-earned']},recovery:'ORIGINAL_KEY_AND_BODY',classification:'EXPLICIT_SYNTHETIC_ONE_EARNED_ONE_UNEARNED'},
   allowedGetPaths:[...getPaths],allowedPostPaths:[...postPaths],actualPlayerVerified:false}};
 }catch(error){f.sql.close();throw error;}
}
