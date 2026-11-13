import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {WebApi} from '../src/web-api.mjs';
import {WebSessionModel} from '../src/web-session-model.mjs';
import {SupportReadModel} from '../src/support-read-model.mjs';
import {MaintenanceLoginModel} from '../src/maintenance-login-model.mjs';
// Test-only explicit Cookie/Origin transport; production HTTPS/Secure remains unchanged.
const root='D:/Project/LASTRAIL',checks=[],events=[],models=[];let stage='private-input',status='FAIL';
const output=root+'/QA/Homepage/support-socket-20261001.json';
const check=(name,ok)=>{stage=name;if(!ok)throw Error('CHECK_FAILED');checks.push({name,result:'PASS'});};
const credentials=JSON.parse(await readFile(process.argv[2],'utf8'));
const supplierEvidence=process.argv[3],sourcePins=[];
function client(){
 let cookie='';const control={origin:credentials.webOrigin,omitCsrf:false};
 const transport=async(url,init)=>{
  const logical=new URL(url);if(logical.origin!==credentials.webOrigin)throw Error('TEST_ORIGIN_MISMATCH');
  const headers=new Headers(init.headers),anonymous=init.credentials==='omit';
  if(!anonymous){headers.set('Origin',control.origin);if(cookie)headers.set('Cookie',cookie);}
  if(control.omitCsrf)headers.delete('X-CSRF-Token');
  const response=await fetch(credentials.origin+logical.pathname+logical.search,{...init,headers});
  const setCookie=response.headers.get('set-cookie');
  if(setCookie){check('cookie-security-'+events.length,/; Secure; HttpOnly/i.test(setCookie)&&/SameSite=Lax/i.test(setCookie));cookie=setCookie.split(';')[0];}
  events.push({path:logical.pathname,method:init.method,status:response.status,noStore:response.headers.get('cache-control')?.includes('no-store')===true,anonymous,cookieSent:headers.has('Cookie'),devHeaderSent:headers.has('X-LastRail-App-Version-Code')});
  return response;
 };
 const api=new WebApi({origin:credentials.webOrigin,environment:'DEV',versionCode:123,fetch:transport}),session=new WebSessionModel(api);
 models.push(session);return {api,session,control};
}
async function rejected(name,fn,status,kind){stage=name;let error;try{await fn();}catch(e){error=e;}check(name,error?.status===status&&error?.kind===kind);}
try{
 const pinned=JSON.parse(await readFile(root+'/QA/CommanderS1-20260922/support-socket-validation-20261001.json','utf8'));
 for(const pin of pinned.dirtySourcePins){const actual=createHash('sha256').update(await readFile(pinned.sourceRoot+'/'+pin.path)).digest('hex');check('source-pin-'+pin.path,actual===pin.sha256.toLowerCase());sourcePins.push({path:pin.path,sha256:actual});}
 const a=client(),b=client();
 stage='password-A';await a.session.loginPassword(credentials.accounts[0].loginId,credentials.accounts[0].password);
 check('password-and-me-A',a.session.state.phase==='authenticated'&&a.session.state.accountId===credentials.accounts[0].id);
 await a.session.restore();const token=await a.session.csrfToken();check('csrf-contract',typeof token==='string'&&token.length>0&&events.some(e=>e.path==='/v1/web/sessions/csrf'&&e.status===200));
 const read=new SupportReadModel(a.api,a.session);models.push(read);
 await read.openList();check('initial-empty-list',read.state.phase==='empty');
 const input={category:'BUG_OTHER',body:'Homepage disposable HTTP verification inquiry.'},key='homepage-socket-original-20261001';
 stage='submit';const submitted=await a.api.submitTicket(input,{key,csrfToken:token});
 check('submit-original',submitted.accepted&&submitted.ticketStatus==='RECEIVED');
 const receipt=await a.api.receipt(key);check('original-key-receipt',receipt.ticketId===submitted.ticketId&&receipt.receiptStatus==='FOUND');
 check('receipt-hides-account-id',!Object.hasOwn(receipt,'accountId'));
 const replay=await a.api.submitTicket(input,{key,csrfToken:token});check('same-key-replay',replay.ticketId===submitted.ticketId&&replay.ticketStatus==='DUPLICATE_REPLAY');
 await rejected('same-key-conflict',()=>a.api.submitTicket({...input,body:'Changed disposable inquiry body.'},{key,csrfToken:token}),409,'conflict');
 await read.openList();check('own-list',read.state.phase==='ready'&&read.state.items.some(t=>t.ticketId===submitted.ticketId));
 await read.openTicket(submitted.ticketId);check('own-detail',read.state.phase==='ready'&&read.state.ticket.body===input.body);
 check('detail-no-internal-data',!Object.hasOwn(read.state.ticket,'internalNote')&&!Object.hasOwn(read.state.ticket,'accountId'));
 stage='password-B';await b.session.loginPassword(credentials.accounts[1].loginId,credentials.accounts[1].password);
 check('password-and-me-B',b.session.state.phase==='authenticated'&&b.session.state.accountId===credentials.accounts[1].id);
 await rejected('other-account-detail-404',()=>b.api.ticket(submitted.ticketId),404,'not-found');
 await rejected('other-account-receipt-404',()=>b.api.receipt(key),404,'not-found');
 check('other-account-empty',(await b.api.tickets()).items.length===0);
 a.control.origin='https://wrong-origin.invalid';await rejected('wrong-origin-403',()=>a.api.tickets(),403,'forbidden');a.control.origin=credentials.webOrigin;
 a.control.omitCsrf=true;await rejected('missing-csrf-403',()=>a.api.submitTicket(input,{key:'homepage-missing-csrf-20261001',csrfToken:token}),403,'forbidden');a.control.omitCsrf=false;
 await rejected('wrong-csrf-403',()=>a.api.submitTicket(input,{key:'homepage-wrong-csrf-20261001',csrfToken:'wrong-test-csrf'}),403,'forbidden');
 check('rejected-posts-no-extra-ticket',(await a.api.tickets()).items.length===1);
 const publicModel=new MaintenanceLoginModel(a.api,{session:a.session});models.push(publicModel);await publicModel.refresh();
 check('public-maintenance',publicModel.state.snapshot?.contractVersion==='maintenance-login-v1'&&publicModel.state.phase==='allowed'&&publicModel.state.confirmed===false);
 const publicEvent=events.findLast(e=>e.path==='/v1/public/maintenance/login');check('public-anonymous',publicEvent.anonymous&&!publicEvent.cookieSent&&!publicEvent.devHeaderSent&&publicEvent.noStore);
 await a.session.restore();check('session-restore',a.session.state.phase==='authenticated');await a.session.csrfToken();check('csrf-after-restore',events.filter(e=>e.path==='/v1/web/sessions/csrf'&&e.status===200).length>=2);
 await a.session.logout();check('logout',a.session.state.phase==='anonymous'&&a.session.state.logoutConfirmed===true);
 await rejected('me-after-logout-401',()=>a.api.me(),401,'unauthenticated');
 check('private-read-cleared',read.state.phase==='authentication-required'&&read.state.ticket===null&&read.state.items.length===0);
 check('all-responses-no-store',events.every(e=>e.noStore));status='PASS';
}catch{checks.push({name:stage,result:'FAIL',reason:'Sanitized failure; credentials and response bodies withheld'});process.exitCode=1;}
finally{
 for(const model of models.reverse())model.dispose();
 await writeFile(output,JSON.stringify({taskId:'H-03',unit:'homepage-support-socket-20261001',contractVersions:['support-socket-local-v1','web-session-v1','private-support-list-v1','private-support-ticket-v1','maintenance-login-v1'],status,passed:checks.filter(c=>c.result==='PASS').length,failed:checks.filter(c=>c.result==='FAIL').length,checks,transportEvents:events,sourcePins,supplierEvidence,scope:'Actual Node fetch loopback HTTP with original Server Hono handlers, isolated SQL.js and real homepage WebApi/Models; explicit test-only Cookie/Origin transport',limitations:['Not browser cookie jar/CORS/HTTPS','Not remote Cloudflare/D1/Google','Synthetic123 is not approved registry','No maintenance mutation; confirmed503 covered by prior unit','All View/bootstrap deferred'],secretsLogged:false},null,2)+'\n');
 console.log(JSON.stringify({status,passed:checks.filter(c=>c.result==='PASS').length,failed:checks.filter(c=>c.result==='FAIL').length,evidence:output}));
}

