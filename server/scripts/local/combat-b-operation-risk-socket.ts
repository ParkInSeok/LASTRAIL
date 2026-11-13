import {z} from 'zod';
import {createAccessToken,verifyAccessToken} from '../../apps/game-api/src/auth';
import {BOperationRiskFixture,operationRiskDefinition,operationRiskVersion} from '../../apps/game-api/src/combat-b-operation-risk';
import {canonicalJson} from '../../packages/common/src/index';
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
import {startWorldBossLoopback} from './world-boss-loopback';
/** Independent signed LOCAL projection; no database or mission admission is provisioned. */
export async function startBOperationRiskSocket(options:{port?:number;lifetimeMs?:number;restore?:unknown}={}){
 const port=options.port??0,lifetime=options.lifetimeMs??120000;if(!Number.isSafeInteger(port)||port<0||port>65535||!Number.isSafeInteger(lifetime)||lifetime<1||lifetime>900000)throw new Error('combat_b_risk_socket_options');
 const restore=options.restore===undefined?undefined:z.array(z.strictObject({accountId:z.enum(['a','b']),binding:z.unknown(),original:z.unknown()})).max(2).parse(structuredClone(options.restore));
 let secret=crypto.randomUUID()+crypto.randomUUID(),closing:Promise<{listenerClosed:boolean;credentialsDiscarded:boolean}>|undefined;
 const bundles=new Map<string,{sessionId:string;accountId:string;headers:Record<string,string>}>(),fixtures=new Map<string,BOperationRiskFixture>(),jobs=new Set<Promise<unknown>>();
 for(const role of ['a','b']){const sessionId=crypto.randomUUID(),token=await createAccessToken({secret,accountId:role,sessionId,nowMs:Date.now(),ttlMs:lifetime});bundles.set(role,{sessionId,accountId:role,headers:{Authorization:'Bearer '+token,'X-Combat-B-Risk-Version':operationRiskVersion}});}
 if(restore!==undefined){for(const row of restore){if(fixtures.has(row.accountId))throw new Error('combat_b_risk_restore_duplicate');fixtures.set(row.accountId,await BOperationRiskFixture.create(row.binding,row.original));}}
 const auth=async(request:Request)=>{const header=request.headers.get('Authorization')??'';if(!header.startsWith('Bearer '))return null;const verified=await verifyAccessToken({secret,token:header.slice(7),nowMs:Date.now()});if(!verified)return null;const bundle=bundles.get(verified.accountId);return bundle&&bundle.sessionId===verified.sessionId?bundle:null;};
 const response=(body:unknown,status=200)=>new Response(JSON.stringify(body),{status,headers:{'Content-Type':'application/json','Cache-Control':'no-store','X-Combat-B-Risk-Version':operationRiskVersion}});
 const listener=await startWorldBossLoopback(async request=>{const url=new URL(request.url),reject=(code:string,status:number)=>response({accepted:false,errorCode:code},status);
 try{
 if(url.protocol!=='http:'||!['127.0.0.1','localhost','[::1]'].includes(url.hostname)||url.pathname!=='/_local/combat-b/operation-risk')return reject('combat_b_risk_local_only',404);
 if(request.headers.get('Origin')&&request.headers.get('Origin')!==url.origin)return reject('combat_b_risk_origin_forbidden',403);
 if(request.method!=='GET'||url.search||request.headers.get('Content-Length')&&request.headers.get('Content-Length')!=='0')return reject('combat_b_risk_request_invalid',400);
 const account=await auth(request);if(!account)return reject('combat_b_risk_auth_invalid',401);
 if(request.headers.get('X-Combat-B-Risk-Version')!==operationRiskVersion)return reject('combat_b_risk_version_conflict',409);
 const fixture=fixtures.get(account.accountId),packet=fixture?await fixture.read():{version:operationRiskVersion,definition:await operationRiskDefinition(),support:{sourceBound:false,localProjection:true,missionStart:false},startReady:false};
 if(!await auth(request))return reject('combat_b_risk_auth_invalid',401);return response(packet);
 }catch{return reject('combat_b_risk_unavailable',503);}},port);
 const close=()=>closing??=(async()=>{clearTimeout(timer);await listener.close();await Promise.allSettled([...jobs]);bundles.clear();fixtures.clear();secret='';return {listenerClosed:true,credentialsDiscarded:true};})();
 const timer=setTimeout(()=>{void close().catch(()=>console.error('combat_b_risk_cleanup_failed'));},lifetime);
 return {origin:listener.origin,close,intake(role:'a'|'b'){if(closing)throw new Error('combat_b_risk_socket_closed');const b=bundles.get(role)!;return {accountId:b.accountId,headers:{...b.headers}};},
 capture(role:'a'|'b',binding:unknown,original:unknown){if(closing)throw new Error('combat_b_risk_socket_closed');const ownedBinding=structuredClone(binding),ownedRaw=structuredClone(original);const b=bundles.get(role);if(!b)throw new Error('combat_b_risk_account_invalid');const work=(async()=>{if(!await auth(new Request(listener.origin,{headers:b.headers})))throw new Error('combat_b_risk_auth_invalid');const fixture=await BOperationRiskFixture.create(ownedBinding,ownedRaw),packet=await fixture.read();if(closing||!await auth(new Request(listener.origin,{headers:b.headers})))throw new Error('combat_b_risk_auth_invalid');const old=fixtures.get(role);if(old){if(json(await old.read())!==json(packet))throw new Error('combat_b_risk_binding_conflict');return old.read();}fixtures.set(role,fixture);return packet;})();jobs.add(work);work.then(()=>jobs.delete(work),()=>jobs.delete(work));return work;},
 async checkpoint(){if(closing)throw new Error('combat_b_risk_socket_closed');return Promise.all([...fixtures].map(async([accountId,f])=>{const p=await f.read();return {accountId,binding:p.binding,original:p.original};}));}};
}