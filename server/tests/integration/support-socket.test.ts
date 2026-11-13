import {describe,expect,it} from 'vitest';
import {startSupportSocket} from '../../scripts/local/support-socket';

describe('disposable support socket',()=>{
 it('uses actual HTTP auth, private receipts, scoped web CSRF and signed Admin processing',async()=>{
  const host=await startSupportSocket();
  try{
   const a=host.credentials.accounts[0]!,b=host.credentials.accounts[1]!;
   const headers={...host.gameHeaders,Authorization:`Bearer ${a.token}`,'Content-Type':'application/json','Idempotency-Key':'socket-original-request-0001'};
   const body=JSON.stringify({category:'BUG_OTHER',body:'Disposable socket support inquiry.'});
   const call=(path:string,init:RequestInit={})=>fetch(host.origin+path,init);
   expect((await call('/v1/support/tickets',{headers:host.gameHeaders})).status).toBe(401);
   expect((await call('/v1/support/tickets',{headers:{...headers,Authorization:'Bearer lst1.invalid-local-token'}})).status).toBe(401);
   expect((await call('/v1/support/tickets',{headers:{...headers,'X-LastRail-App-Version-Code':'124'}})).status).toBe(403);
   const accepted=await call('/v1/support/tickets',{method:'POST',headers,body});expect(accepted.status).toBe(200);
   const receipt=await accepted.json() as {ticketId:string};
   const receiptHeaders={...host.gameHeaders,Authorization:headers.Authorization,'Idempotency-Key':headers['Idempotency-Key']};
   expect((await (await call('/v1/support/tickets/receipt',{headers:receiptHeaders})).json()).ticketId).toBe(receipt.ticketId);
   expect((await (await call('/v1/support/tickets',{method:'POST',headers,body})).json()).ticketId).toBe(receipt.ticketId);
   expect((await call('/v1/support/tickets',{method:'POST',headers,body:JSON.stringify({category:'BUG_OTHER',body:'Different inquiry conflicts.'})})).status).toBe(409);
   expect((await call(`/v1/support/tickets/${receipt.ticketId}`,{headers:{...headers,Authorization:`Bearer ${b.token}`}})).status).toBe(404);
   const processing=`/v1/admin/support/tickets/${receipt.ticketId}/processing`;
   expect((await call(processing)).status).toBe(401);
   expect((await call(processing,{headers:{'Cf-Access-Subject':'local|owner'}})).status).toBe(401);
   const parts=host.credentials.adminOwnerJwt.split('.');parts[2]=(parts[2]![0]==='A'?'B':'A')+parts[2]!.slice(1);
   expect((await call(processing,{headers:{'Cf-Access-Jwt-Assertion':parts.join('.')}})).status).toBe(403);
   expect((await call(processing,{headers:{'Cf-Access-Jwt-Assertion':host.credentials.adminViewerJwt}})).status).toBe(403);
   const adminHeaders={'Cf-Access-Jwt-Assertion':host.credentials.adminOwnerJwt,'Content-Type':'application/json','Idempotency-Key':'socket-close'};
   expect((await call(processing,{headers:adminHeaders})).status).toBe(403);
   const grant=await call('/v1/admin/support/processor-grants/local_owner',{method:'POST',headers:{...adminHeaders,'Idempotency-Key':'socket-explicit-grant'},body:JSON.stringify({action:'GRANT',permission:'SUPPORT_TICKET_PROCESS',expectedRevision:0,reason:'LOCAL_TEST_ONLY explicit disposable approval'})});
   expect(grant.status).toBe(200);
   const closed=await call(processing,{method:'POST',headers:adminHeaders,body:JSON.stringify({expectedRevision:0,status:'CLOSED',publicReply:'Disposable final answer.',internalNote:'Private operator note.'})});
   expect(closed.status).toBe(200);expect(closed.headers.get('X-Support-Processing-Policy')).toBe('support-processing-flow-v2-20261001');
   const detail=await call(`/v1/support/tickets/${receipt.ticketId}`,{headers});expect(detail.status).toBe(200);
   const text=await detail.text();expect(text).toContain('Disposable final answer.');expect(text).not.toContain('Private operator note.');
   const web=await call('/v1/web/sessions/password',{method:'POST',headers:{Origin:host.webOrigin,'Content-Type':'application/json'},body:JSON.stringify({loginId:a.loginId,password:a.password})});
   expect(web.status).toBe(200);expect(web.headers.get('Set-Cookie')).toContain('Secure; HttpOnly');
   const cookie=web.headers.get('Set-Cookie')!.split(';')[0]!,csrf=(await web.json() as {csrfToken:string}).csrfToken;
   const webHeaders={Origin:host.webOrigin,Cookie:cookie,'X-LastRail-App-Version-Code':'123','Content-Type':'application/json','Idempotency-Key':'socket-web-request-0001'};
   expect((await call('/v1/web/support/tickets',{method:'POST',headers:webHeaders,body})).status).toBe(403);
   expect((await call('/v1/web/support/tickets',{method:'POST',headers:{...webHeaders,'X-CSRF-Token':csrf},body})).status).toBe(200);
   expect((await call('/v1/web/support/tickets',{headers:{...webHeaders,Origin:'https://evil.invalid'}})).status).toBe(403);
   const revoke=await call('/v1/admin/support/processor-grants/local_owner',{method:'POST',headers:{...adminHeaders,'Idempotency-Key':'socket-explicit-revoke'},body:JSON.stringify({action:'REVOKE',permission:'SUPPORT_TICKET_PROCESS',expectedRevision:1,reason:'LOCAL_TEST_ONLY cleanup permission'})});
   expect(revoke.status).toBe(200);expect((await call(processing,{headers:adminHeaders})).status).toBe(403);
   expect(await (await call(`/v1/support/tickets/${receipt.ticketId}`,{headers})).text()).toContain('Disposable final answer.');
   expect((await call('/v1/runs/start',{method:'POST',headers,body:'{}'})).status).toBe(404);
   expect((await call('/v1/admin/maintenance-runtime/initialize',{method:'POST',headers:adminHeaders,body:'{}'})).status).toBe(404);
   const metadata=JSON.stringify(host.metadata);expect(metadata).not.toContain(a.token);expect(metadata).not.toContain(a.password);
  }finally{await host.close();}
  await expect(fetch(host.origin+'/v1/support/tickets')).rejects.toThrow();
 },20000);
 it('starts with new empty private data after disposal',async()=>{
  const host=await startSupportSocket();
  try{
   const headers={...host.gameHeaders,Authorization:`Bearer ${host.credentials.accounts[0]!.token}`};
   const response=await fetch(host.origin+'/v1/support/tickets',{headers});expect(response.status).toBe(200);
   expect((await response.json() as {items:unknown[]}).items).toEqual([]);
  }finally{await host.close();}
 });
});
