import {describe,expect,it,vi} from 'vitest';
import {startSupportSocket} from '../../scripts/local/support-socket';

describe('Admin JWT socket error boundary',()=>{
 it('keeps normal, malformed, bad signature, expiry and missing credentials distinct',async()=>{
  const host=await startSupportSocket();
  const path=host.origin+'/v1/admin/support/processor-grants/local_owner';
  const request=(token?:string)=>fetch(path,{headers:token?{'Cf-Access-Jwt-Assertion':token}:{}});
  try{
   expect((await request()).status).toBe(401);
   expect((await request(host.credentials.adminOwnerJwt)).status).toBe(200);
   const parts=host.credentials.adminOwnerJwt.split('.');parts[2]=(parts[2]![0]==='A'?'B':'A')+parts[2]!.slice(1);
   for(const token of ['invalid.local.signature','bnVsbA.e30.AA',parts.join('.'),host.credentials.adminOwnerJwt+'.extra']){
    const response=await request(token);expect(response.status).toBe(403);
    expect(await response.json()).toEqual({error:'ADMIN_ACCESS_JWT_INVALID'});
   }
   const clock=vi.spyOn(Date,'now').mockReturnValue(Date.now()+3600001);
   try{
    const expired=await request(host.credentials.adminOwnerJwt);expect(expired.status).toBe(403);
    expect(await expired.json()).toEqual({error:'ADMIN_ACCESS_JWT_INVALID'});
   }finally{clock.mockRestore();}
   expect((await request(host.credentials.adminOwnerJwt)).status).toBe(200);
  }finally{await host.close();}
 },20000);
});
