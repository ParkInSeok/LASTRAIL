import {createServer} from 'node:http';
import {randomUUID} from 'node:crypto';
import {isDeepStrictEqual} from 'node:util';
import {pathToFileURL} from 'node:url';
import app from '../../apps/game-api/src/index';
import {hashPassword,validatePassword} from '../../apps/game-api/src/password-auth';
import {CommunityConsentService} from '../../apps/game-api/src/community-consent';
import {CommunityContentCreator} from '../../apps/game-api/src/community-content-creator';
import {setup} from '../../tests/helpers/departure-fixture';
import {installSyntheticCommunityConsent} from '../../tests/helpers/community-consent-fixture';
export const communityLoopbackVersion='community.loopback-http.v1.20261008';
export const communityLoopbackApiOrigin='https://community-loopback.invalid';
export const communityLoopbackWebOrigin='https://dev.lastrail.app';
const maxLifetime=30*60*1000;
/** Local verification only: actual production Hono routes, existing in-memory sql.js/D1 adapter.
 * No production registration, real DB binding, secret export, or persistent fixture DB. */
export async function startCommunityLoopback({password,lifetimeMs=maxLifetime}:{password:string;lifetimeMs?:number}){
 if(!validatePassword(password)||!Number.isSafeInteger(lifetimeMs)||lifetimeMs<100||lifetimeMs>maxLifetime)throw new Error('local_fixture_options_invalid');
 const f=await setup(false,true,null);
 try{
  const hash=await hashPassword(password),expires=Date.now()+lifetimeMs;
  for(const account of ['a','b']){
   f.sql.run('INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES(?,?,?,?,?,?,0,0)',[account,'community-local-'+account,hash.passwordHash,hash.algorithm,hash.paramsJson,'synthetic-'+account+'@example.invalid']);
   f.sql.run("INSERT INTO account_tester_entitlements VALUES(?,'DEV','123','ACTIVE',?,NULL,'SYNTHETIC_LOOPBACK_ONLY',1,0,0)",[account,expires]);
  }
  await installSyntheticCommunityConsent(f.sql,f.env.DB,1,false);
  const consent=new CommunityConsentService(f.env.DB,'DEV','ko-KR');
  await consent.accept('b',{commandId:'synthetic_other_accept',...(await consent.current('b')).policy});
  const source=await new CommunityContentCreator(f.env.DB,()=>Date.now()-120000,undefined,undefined,undefined,actor=>consent.creationEligibility(actor)).createPost('b',{commandId:'synthetic_other_post',boardId:'free',title:'SYNTHETIC LOOPBACK POST',markdown:'Synthetic fixture only.'});
  const env={...f.env,WEB_ALLOWED_ORIGIN:communityLoopbackWebOrigin,CLIENT_ADMISSION_MODE:'ENFORCED' as const};
  let databaseClosed=false,closing:Promise<void>|undefined,finish!:()=>void;
  const closed=new Promise<void>(resolve=>{finish=resolve;}),pending=new Set<Promise<void>>();
  const server=createServer((req,res)=>{
   const task=(async()=>{
    const address=server.address(),host=address&&typeof address!=='string'?'127.0.0.1:'+address.port:'';
    const reject=(status:number,code:string)=>{res.writeHead(status,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({error:code}));};
    if(req.headers.host!==host){reject(403,'local_host_forbidden');return;}
    if(!req.url||!req.url.startsWith('/')||req.url.startsWith('//')||req.url.length>4096){reject(400,'local_request_invalid');return;}
    const url=new URL(req.url,'http://'+host);
    if(!/^\/v1\/web\/(?:sessions\/(?:password|me|csrf|logout)|community(?:\/|$))/u.test(url.pathname)){reject(404,'local_scope_unavailable');return;}
    try{
     const chunks:Buffer[]=[];let bytes=0;
     for await(const chunk of req){bytes+=chunk.length;if(bytes>256000){reject(413,'local_transport_body_limit');return;}chunks.push(Buffer.from(chunk));}
     const headers=new Headers();for(const [key,value] of Object.entries(req.headers))if(value!==undefined)headers.set(key,Array.isArray(value)?value.join(','):value);
     const response=await app.fetch(new Request(url,{method:req.method??'GET',headers,...(['GET','HEAD'].includes(req.method??'GET')?{}:{body:Buffer.concat(chunks)})}),env);
     res.writeHead(response.status,Object.fromEntries(response.headers.entries()));res.end(Buffer.from(await response.arrayBuffer()));
    }catch{if(!res.headersSent)reject(503,'local_bridge_unavailable');else res.end();}
   })();pending.add(task);void task.finally(()=>pending.delete(task));
  });
  server.requestTimeout=10000;server.headersTimeout=10000;
  await new Promise<void>((resolve,reject)=>{server.once('error',reject);server.listen(0,'127.0.0.1',()=>{server.off('error',reject);resolve();});});
  const address=server.address();if(!address||typeof address==='string')throw new Error('local_bind_failed');
  const close=()=>closing??=(async()=>{clearTimeout(timer);await new Promise<void>(resolve=>{server.close(()=>resolve());server.closeAllConnections();});await Promise.allSettled([...pending]);f.sql.close();databaseClosed=true;finish();})();
  const timer=setTimeout(()=>{void close();},lifetimeMs);
  return {baseUrl:'http://127.0.0.1:'+address.port,origin:communityLoopbackWebOrigin,apiOrigin:communityLoopbackApiOrigin,versionCode:123,fixturePostId:source.contentId,lifetimeMs,closed,close,databaseClosed:()=>databaseClosed};
 }catch(error){f.sql.close();throw error;}
}
/** Explicit Node test transport, not browser cookie/CORS/HTTPS emulation.
 * It maps only one test HTTPS label to real loopback HTTP, carries server-issued cookie in memory,
 * and forwards actual requests/responses without mocked endpoints or auth bypass. */
export async function createLoopbackBrowserTransport({baseUrl,origin}:{baseUrl:string;origin:string}){
 const base=new URL(baseUrl);if(base.protocol!=='http:'||base.hostname!=='127.0.0.1'||!base.port||base.pathname!=='/'||base.username||base.password||base.search||base.hash||origin!==communityLoopbackWebOrigin)throw new Error('local_transport_target_invalid');
 let cookie='',calls=0;
 const request:typeof fetch=async(input,init={})=>{
  if(typeof input!=='string')throw new Error('local_transport_request_invalid');const url=new URL(input);if(url.origin!==communityLoopbackApiOrigin)throw new Error('local_transport_origin_invalid');
  const headers=new Headers(init.headers);headers.set('Origin',origin);headers.delete('Cookie');if(init.credentials==='include'&&cookie)headers.set('Cookie',cookie);
  calls++;const response=await fetch(base.origin+url.pathname+url.search,{...init,headers});
  const issued=response.headers.get('Set-Cookie');if(issued){if(!issued.startsWith('__Host-lr_web_session='))throw new Error('local_cookie_contract_invalid');cookie=issued.split(';')[0]!;if(cookie==='__Host-lr_web_session=')cookie='';}
  return response;
 };
 return {fetch:request,calls:()=>calls,clear:()=>{cookie='';}};
}
async function loadHomepageClient(){return import(pathToFileURL('D:/Project/LASTRAIL/homepage/src/web-api.mjs').href);}
export async function verifyCommunityLoopback({password,loadHomepage=loadHomepageClient}:{password:string;loadHomepage?:(fixture:Awaited<ReturnType<typeof startCommunityLoopback>>)=>ReturnType<typeof loadHomepageClient>}){
 const fixture=await startCommunityLoopback({password});let transport:Awaited<ReturnType<typeof createLoopbackBrowserTransport>>|undefined;
 const checks:{name:string;status:number}[]=[];
 const pass=(name:string)=>checks.push({name,status:200});
 const failure=async(name:string,status:number,action:()=>Promise<unknown>)=>{try{await action();throw new Error('expected_error_missing');}catch(error){if(!(error instanceof Error)||!('status' in error)||error.status!==status)throw new Error('local_check_failed_'+name);checks.push({name,status});}};
 let caught:unknown;
 try{
  transport=await createLoopbackBrowserTransport(fixture);
  const {WebApi}=await loadHomepage(fixture);
  const api=new WebApi({origin:fixture.apiOrigin,environment:'DEV',versionCode:123,fetch:transport.fetch});
  await failure('before_login_denied',401,()=>api.me());
  const login=await api.loginPassword('community-local-a',password),options={csrfToken:login.csrfToken};pass('real_password_login');
  if((await api.me()).accountId!=='a')throw new Error('local_identity_invalid');pass('web_session_me');
  const initial=await api.communityConsent();if(initial.accepted)throw new Error('automatic_consent');pass('synthetic_terms_read_not_accepted');
  const command={commandId:'tcp_post',boardId:'free',title:'TCP synthetic',markdown:'**Synthetic actual API**'};
  await failure('creation_requires_explicit_consent',403,()=>api.createCommunityPost(command,options));
  await api.acceptCommunityConsent({commandId:'tcp_accept',...initial.policy},options);pass('explicit_consent');
  const created=await api.createCommunityPost(command,options);pass('post_create');
  if(!isDeepStrictEqual(created,await api.createCommunityPost(command,options)))throw new Error('local_replay_changed');pass('post_original_receipt');
  const edited=await api.editCommunityContent({commandId:'tcp_edit',contentId:created.contentId,expectedRevision:1,title:'TCP edited',markdown:'Edited'},options);pass('owner_edit');
  const publicContent=await api.publicCommunityContent(created.contentId);if(publicContent.content.revision!==edited.revision||publicContent.content.updatedAt<publicContent.content.createdAt)throw new Error('local_time_invalid');pass('public_detail_updated_at');
  await api.ownCommunityPosts();pass('own_post_list');
  const comment=await api.createCommunityComment({commandId:'tcp_comment',postId:created.contentId,markdown:'TCP comment'},options);pass('comment_create');
  await api.publicCommunityComments(created.contentId);pass('public_comment_list');await api.ownCommunityComments();pass('own_comment_list');
  const other=await api.publicCommunityContent(fixture.fixturePostId),author=other.content.publicAuthorId;
  await api.setCommunityBlock({commandId:'tcp_block',publicAuthorId:author,blocked:true,expectedRevision:0},options);pass('block_other_author');
  await failure('blocked_public_target_hidden',404,()=>api.publicCommunityContent(fixture.fixturePostId));
  const reportCommand={commandId:'tcp_report',targetType:'POST',targetId:fixture.fixturePostId,expectedRevision:1,reasonCode:'SPAM',optionalDescription:null};
  const report=await api.submitCommunityReport(reportCommand,options);pass('report_blocked_target_without_terms_gate');
  if(!isDeepStrictEqual(report,await api.submitCommunityReport(reportCommand,options)))throw new Error('local_report_replay_changed');pass('report_original_receipt');
  await api.ownCommunityReport(report.caseId);pass('own_limited_report');
  await api.communityBlocks();pass('own_block_list');
  await api.setCommunityBlock({commandId:'tcp_unblock',publicAuthorId:author,blocked:false,expectedRevision:1},options);pass('unblock_other_author');
  await api.publicCommunityContent(fixture.fixturePostId);pass('current_public_source_restored_by_unblock');
  await failure('bad_csrf_denied',403,()=>api.deleteCommunityContent({commandId:'bad_delete',contentId:created.contentId,expectedRevision:2},{csrfToken:'invalid'}));
  await api.deleteCommunityContent({commandId:'tcp_delete_comment',contentId:comment.contentId,expectedRevision:1},options);pass('delete_comment');
  const deletion={commandId:'tcp_delete',contentId:created.contentId,expectedRevision:2};const deleted=await api.deleteCommunityContent(deletion,options);pass('delete_post');
  if(!isDeepStrictEqual(deleted,await api.deleteCommunityContent(deletion,options)))throw new Error('local_delete_replay_changed');pass('delete_original_receipt');
  await failure('deleted_public_target_hidden',404,()=>api.publicCommunityContent(created.contentId));
  const own=await api.ownCommunityContent(created.contentId);if(own.content.sanitizedBody!==null||own.content.updatedAt!==null)throw new Error('local_private_projection_invalid');pass('deleted_own_body_masked');
  // Raw transport calls still hit actual Hono boundaries; no client-side fabricated status.
  for(const [name,extra,status] of [['wrong_contract',{'X-Community-Public-Version':'old'},409],['wrong_exact_tester',{'X-LastRail-App-Version-Code':'124'},403]] as const){
   const raw=await transport.fetch(fixture.apiOrigin+'/v1/web/community/contents/'+fixture.fixturePostId,{credentials:'include',headers:{'X-Community-Public-Version':'community.public-read.v1.20261008','X-LastRail-App-Version-Code':'123',...extra}});
   if(raw.status!==status)throw new Error('local_check_failed_'+name);checks.push({name,status});
  }
  const originDenied=await fetch(fixture.baseUrl+'/v1/web/sessions/password',{method:'POST',headers:{Origin:'https://other.invalid','Content-Type':'application/json'},body:JSON.stringify({loginId:'community-local-a',password})});if(originDenied.status!==403)throw new Error('local_origin_not_enforced');checks.push({name:'wrong_origin',status:403});
  await api.logout(login.csrfToken);pass('web_logout');await failure('logout_session_denied',401,()=>api.me());
 }catch(error){caught=error;}finally{transport?.clear();await fixture.close();}
 let listenerClosed=false;try{await fetch(fixture.baseUrl+'/v1/web/sessions/me');}catch{listenerClosed=true;}
 if(caught)throw caught;if(!listenerClosed||!fixture.databaseClosed())throw new Error('local_shutdown_failed');
 return {contractVersion:communityLoopbackVersion,actualTcp:true,baseUrlUsed:fixture.baseUrl,listenerActiveAtReport:false,homepageClient:'D:/Project/LASTRAIL/homepage/src/web-api.mjs',environment:'ISOLATED_DEV_FIXTURE',versionCode:123,checks,transportCalls:transport?.calls()??0,listenerClosed,databaseClosed:fixture.databaseClosed(),actualBrowser:false,actualHttps:false,actualRemoteD1:false,operatingDeploy:false,operatingDataChanged:false};
}
export function syntheticLocalPassword(){return 'Synthetic-'+randomUUID();}