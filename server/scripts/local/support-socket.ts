import {createServer} from 'node:http';
import {randomBytes} from 'node:crypto';
import {Buffer} from 'node:buffer';
import {readFile,readdir} from 'node:fs/promises';
import {resolve} from 'node:path';
import initSqlJs from 'sql.js';
import game from '../../apps/game-api/src/index';
import admin from '../../apps/admin-api/src/index';
import {hashPassword} from '../../apps/game-api/src/password-auth';
import {SqlJsD1Database} from '../../tests/helpers/growth-d1';
import {testMaintenancePolicy} from '../../tests/helpers/maintenance-policy-fixture';

// Disposable synthetic LOCAL_SQLJS environment only. Never imported by Workers.
// Existing product handlers and authentication run unchanged; no remote bindings.
const versionCode='123',appVersion='1.0.0-api-qa';
const webOrigin='https://support.local.invalid';
const gameHeaders={'X-LastRail-App-Version-Code':versionCode,'X-LastRail-App-Version':appVersion,'X-LastRail-App-Platform':'android'};
function allowed(method:string,path:string){
 if(path==='/cdn-cgi/access/certs')return method==='GET';
 if(path==='/v1/auth/password/login'||path==='/v1/auth/refresh')return method==='POST';
 if(path==='/v1/public/maintenance/login')return ['GET','OPTIONS'].includes(method);
 if(['/v1/web/sessions/password','/v1/web/sessions/csrf','/v1/web/sessions/logout'].includes(path))return ['POST','OPTIONS'].includes(method);
 if(path==='/v1/web/sessions/me')return ['GET','OPTIONS'].includes(method);
 if(/^\/v1\/(?:web\/)?support\/tickets$/u.test(path))return ['GET','POST','OPTIONS'].includes(method);
 if(/^\/v1\/(?:web\/)?support\/tickets\/[A-Za-z0-9_-]{1,128}$/u.test(path))return ['GET','OPTIONS'].includes(method);
 if(/^\/v1\/admin\/support\/tickets\/[A-Za-z0-9_-]{1,128}\/processing$/u.test(path))return ['GET','POST'].includes(method);
 if(path==='/v1/admin/support/processor-grants/local_owner')return ['GET','POST'].includes(method);
 return false;
}
export async function startSupportSocket(){
 const SQL=await initSqlJs(),sql=new SQL.Database();
 let destroyed=false;
 const destroy=()=>{if(!destroyed){destroyed=true;sql.close();}};
 try{
  sql.create_function('unixepoch',()=>Date.now()/1000);sql.run('PRAGMA foreign_keys=ON');
  const migrations=(await readdir(resolve('packages/db/migrations'))).filter(file=>/^\d+_.+\.sql$/u.test(file)).sort();
  for(const file of migrations)sql.run(await readFile(resolve('packages/db/migrations',file),'utf8'));
  testMaintenancePolicy(sql);
  const expiresAt=Date.now()+3600000;
  const accounts=[];
  for(const index of [0,1]){
   const id='local_support_'+randomBytes(8).toString('hex'),loginId='socket'+index+randomBytes(4).toString('hex');
   const password='LocalOnly!'+randomBytes(24).toString('base64url'),hash=await hashPassword(password);
   sql.run('INSERT INTO accounts(id,created_at,updated_at) VALUES(?,?,?)',[id,Date.now(),Date.now()]);
   sql.run(`INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES(?,?,?,?,?,?,?,?)`,
    [id,loginId,hash.passwordHash,hash.algorithm,hash.paramsJson,loginId+'@example.invalid',Date.now(),Date.now()]);
   sql.run(`INSERT INTO account_tester_entitlements(account_id,environment,version_code,status,expires_at,granted_by,created_at,updated_at) VALUES(?,'DEV','123','ACTIVE',?,'LOCAL_TEST_ONLY',?,?)`,[id,expiresAt,Date.now(),Date.now()]);
   accounts.push({id,loginId,password,token:'',refreshToken:'',accessTokenExpiresAt:''});
  }
  sql.run("INSERT INTO admin_principals VALUES('local_owner','local|owner','OWNER','ACTIVE',NULL,0,0),('local_viewer','local|viewer','VIEWER','ACTIVE',NULL,0,0)");
  const signingSecret=randomBytes(48).toString('base64url');
  const keyPair=await crypto.subtle.generateKey({name:'RSASSA-PKCS1-v1_5',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'},true,['sign','verify']);
  const publicKey={...await crypto.subtle.exportKey('jwk',keyPair.publicKey),kid:'local-support',alg:'RS256',use:'sig'};
  // Synthetic legacy version document for the prepared Unity HTTP harness. It
  // is not a public release or current START-support declaration. Exact 123 is
  // additionally enforced by this local transport; all economy/START paths deny.
  const version={version:appVersion,versionCode,minimumVersionCode:versionCode,contentRelease:{status:'APPROVED',releaseId:'LOCAL_TEST_ONLY_SUPPORT',contentVersion:'LOCAL_TEST_ONLY',
   catalogUrl:'https://example.invalid/local-support-not-a-published-catalog',catalogHash:'sha256:'+'a'.repeat(64),minimumVersionCode:versionCode,maximumVersionCode:versionCode}};
  let origin='';
  const env={DB:new SqlJsD1Database(sql) as unknown as D1Database,ENVIRONMENT:'DEV',CLIENT_ADMISSION_MODE:'ENFORCED',AUTH_SIGNING_KEY_CURRENT:signingSecret,
   ALLOWED_ADMIN_ORIGINS:'',WEB_ALLOWED_ORIGIN:'',ADMIN_AUTH_MODE:'ACCESS_JWT_REQUIRED',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'local-support-only',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:'',
   VERSION_BUCKET:{get:async()=>({httpEtag:'"LOCAL_TEST_ONLY_SUPPORT"',json:async()=>version})}};
  // Serialize SQL.js handler access, matching an isolated DB owner. JWKS reads
  // bypass this queue so the unchanged Admin verifier can fetch its signing key.
  let pending=Promise.resolve();
  const server=createServer(async(req,res)=>{
   try{
    const url=new URL(req.url??'/',origin),method=req.method??'GET';
    if(url.origin!==origin||req.headers.host!==new URL(origin).host||!allowed(method,url.pathname)){res.writeHead(404);res.end();return;}
    if(url.pathname==='/cdn-cgi/access/certs'){res.writeHead(200,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({keys:[publicKey]}));return;}
    const chunks:Buffer[]=[];let size=0;
    for await(const chunk of req){const bytes=Buffer.from(chunk);size+=bytes.length;if(size>16384){res.writeHead(413);res.end();return;}chunks.push(bytes);}
    const headers=new Headers();for(const [name,value] of Object.entries(req.headers)){if(typeof value==='string')headers.set(name,value);}
    const execute=async()=>{
     if(url.pathname.startsWith('/v1/support/')||url.pathname.startsWith('/v1/auth/')){
      if(headers.get('X-LastRail-App-Version-Code')!==versionCode){res.writeHead(403,{'Content-Type':'application/json'});res.end('{"errorCode":"local_exact_version_required"}');return;}
     }
     const application=url.pathname.startsWith('/v1/admin/')?admin:game;
     const response=await application.request(url.href,{method,headers,...(method==='GET'||method==='HEAD'?{}:{body:Buffer.concat(chunks)})},env);
     res.writeHead(response.status,Object.fromEntries(response.headers));res.end(Buffer.from(await response.arrayBuffer()));
    };
    const task=pending.then(execute);pending=task.catch(()=>{});await task;
   }catch{res.writeHead(503,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end('{"error":"local_support_transport_unavailable"}');}
  });
  await new Promise<void>((done,reject)=>{server.once('error',reject);server.listen(0,'127.0.0.1',()=>{server.removeListener('error',reject);done();});});
  const address=server.address();if(!address||typeof address==='string')throw new Error('Local listener unavailable');
  origin=`http://127.0.0.1:${address.port}`;
  env.WEB_ALLOWED_ORIGIN=webOrigin;env.ALLOWED_ADMIN_ORIGINS=origin;env.ADMIN_ACCESS_ISSUER=origin;
  const close=async()=>{const stopped=new Promise<void>((done,reject)=>server.close(error=>error?reject(error):done()));server.closeAllConnections();await stopped;await pending;destroy();};
  try{
   for(const account of accounts){
    const response=await fetch(origin+'/v1/auth/password/login',{method:'POST',headers:{...gameHeaders,'Content-Type':'application/json'},body:JSON.stringify({loginId:account.loginId,password:account.password})});
    if(response.status!==200)throw new Error('Local password bootstrap failed');
    const bundle=await response.json() as {accessToken:string;refreshToken:string;accessTokenExpiresAt:string};
    account.token=bundle.accessToken;account.refreshToken=bundle.refreshToken;account.accessTokenExpiresAt=bundle.accessTokenExpiresAt;
   }
   const jwt=async(subject:string)=>{
    const header=Buffer.from(JSON.stringify({alg:'RS256',kid:'local-support'})).toString('base64url');
    const payload=Buffer.from(JSON.stringify({iss:origin,aud:'local-support-only',sub:subject,exp:Math.floor(expiresAt/1000)})).toString('base64url');
    const data=header+'.'+payload;
    return data+'.'+Buffer.from(await crypto.subtle.sign('RSASSA-PKCS1-v1_5',keyPair.privateKey,new TextEncoder().encode(data))).toString('base64url');
   };
   return {origin,webOrigin,gameHeaders,credentials:{accounts,adminOwnerJwt:await jwt('local|owner'),adminViewerJwt:await jwt('local|viewer')},close,
    metadata:{contractVersion:'support-socket-local-v1',origin,webOrigin,environment:'LOCAL_SQLJS',appVersionCode:versionCode,migrations,
     accountExpiresAt:expiresAt,authentication:'GAME real password login; WEB real password cookie+CSRF; ADMIN real RS256 verifier with local-only issuer',
     clientAdmission:'ENFORCED synthetic legacy version fixture plus exact 123 transport gate; no production registry/public catalog',
     supported:'GAME/WEB support submit,receipt,list,detail; WEB password/me/csrf/logout; ADMIN ticket processing + explicit local_owner grant/revoke; maintenance login status',
     limits:'loopback Node HTTP+SQL.js only; Secure cookie preserved (HTTP browser cookie QA NOT_SUPPORTED); no Cloudflare Access, remote D1, Google, public source, START, reward or operational maintenance',
     cleanup:'close listener and discard in-memory database; restart creates new accounts, keys and empty tickets'}};
  }catch(error){await close();throw error;}
 }catch(error){destroy();throw error;}
}
