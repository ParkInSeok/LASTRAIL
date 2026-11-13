import {it,expect} from 'vitest';
import {Hono} from 'hono';
import {setup} from '../helpers/departure-fixture';
import {AdminEventManagement} from '../../apps/admin-api/src/admin-event-management';
import {registerAdminEventManagementRoutes} from '../../apps/admin-api/src/admin-event-management-routes';
import admin from '../../apps/admin-api/src/index';
const actor={id:'OPERATOR',role:'OPERATOR' as const};
const definition={environment:'DEV',roundId:'round',revision:1,policyVersion:null,title:'Synthetic event',theme:'Synthetic',
 schedule:{publishAt:1000,startAt:1000,endAt:2000,claimUntil:null,giftOpenAt:null},
 rewards:[{rightId:'right',kind:'PARTICIPATION',condition:'Unresolved',producerId:null,threshold:null,itemId:null,quantity:1,perAccountCount:1,policyVersion:null}]};
const input={operation:'SAVE_DRAFT',reason:'Synthetic draft change',expectedRevision:null,definition};
async function fixture(){
 const f=await setup(false,true,null);
 for(const role of ['OWNER','OPERATOR','VIEWER'])f.sql.run("INSERT INTO admin_principals VALUES(?,?,?,'ACTIVE',NULL,0,0)",[role,role,role]);
 return {...f,service:new AdminEventManagement(f.env.DB,'DEV')};
}
it('saves an issue-bearing structural draft, returns committed state and replays original revision after later edits',async()=>{
 const f=await fixture();try{
  const first=await f.service.save(actor,'round',input,'save');
  expect(first).toMatchObject({persisted:true,state:'DRAFT',revision:1,reviewApproved:false,scheduleSupported:false,definitionValid:false});
  expect(first.reason).toBe(input.reason);
  await expect(f.service.save(actor,'round',{...input,reason:' '},'empty-reason')).rejects.toThrow('contract_violation');
  expect(first.issues).toContain('INITIAL_DURATION_OUTSIDE_14_TO_28_DAYS');
  const next={...input,expectedRevision:1,definition:{...definition,revision:2,title:'Updated'}};
  await f.service.save(actor,'round',next,'edit');
  expect((await f.service.read('round')).revision).toBe(2);
  expect(await f.service.save(actor,'round',input,'save')).toEqual(first);
  await expect(f.service.save(actor,'round',{...input,reason:'Different reason'},'save')).rejects.toThrow('idempotency_conflict');
  await expect(f.service.save(actor,'round',{...input,definition:{...definition,title:'changed'}},'save')).rejects.toThrow('idempotency_conflict');
  await expect(f.service.save(actor,'round',input,'stale')).rejects.toThrow('event_revision_conflict');
  await expect(f.service.save(actor,'round',{...next,definition:{...definition,revision:9}},'wrong')).rejects.toThrow('event_revision_conflict');
  expect(f.scalar('SELECT count(*) FROM admin_audit_logs')).toBe(2);
  expect(f.scalar("SELECT json_extract(change_summary_json,'$.reason') FROM admin_audit_logs LIMIT 1")).toBe(input.reason);
 }finally{f.sql.close();}
});
it('rolls back draft/receipt on audit failure and rejects revoked write permission at commit',async()=>{
 const f=await fixture();try{
  f.sql.run("CREATE TRIGGER fail_event_audit BEFORE INSERT ON admin_audit_logs BEGIN SELECT RAISE(ABORT,'test'); END");
  await expect(f.service.save(actor,'round',input,'save')).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM admin_event_drafts')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM admin_event_draft_receipts')).toBe(0);
  f.sql.run('DROP TRIGGER fail_event_audit');
  const batch=f.env.DB.batch.bind(f.env.DB);
  f.env.DB.batch=async statements=>{f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OPERATOR'");return batch(statements);};
  await expect(f.service.save(actor,'round',input,'race')).rejects.toMatchObject({status:403,code:'admin_permission_denied'});
  expect(f.scalar('SELECT count(*) FROM admin_event_drafts')).toBe(0);
 }finally{f.sql.close();}
});
it('converges same-key commits and isolates environment and current reader permissions through routes',async()=>{
 const f=await fixture();try{
  const both=await Promise.all([f.service.save(actor,'round',input,'same'),f.service.save(actor,'round',input,'same')]);
  expect(both[0]).toEqual(both[1]);expect(f.scalar('SELECT count(*) FROM admin_event_drafts')).toBe(1);
  await expect(new AdminEventManagement(f.env.DB,'PRD').read('round')).rejects.toThrow('event_draft_not_found');
  let role:'OWNER'|'OPERATOR'|'VIEWER'='VIEWER';
  const app=new Hono<{Bindings:Env}>();
  registerAdminEventManagementRoutes(app,async()=>({id:role,role}),(_p,permission)=>permission==='SUPPORT_READ'||role!=='VIEWER');
  const path='/v1/admin/events/definitions/round';
  expect((await app.request(path,{},f.env)).status).toBe(200);
  const send=()=>app.request(path+'/commands',{method:'POST',headers:{'Content-Type':'application/json','Idempotency-Key':'route'},body:JSON.stringify(input)},f.env);
  expect((await send()).status).toBe(403);
  role='OPERATOR';expect((await send()).status).toBe(409);
  f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='OPERATOR'");
  expect((await app.request(path,{},f.env)).status).toBe(403);
 }finally{f.sql.close();}
});
it('actual Admin authentication exposes saved drafts to readers and only OWNER/OPERATOR may write',async()=>{
 const f=await fixture();try{
  const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
  const path='/v1/admin/events/definitions';
  expect([401,403]).toContain((await admin.request(path,{},env)).status);
  for(const role of ['OWNER','OPERATOR','VIEWER']){
   const body={...input,definition:{...definition,roundId:role}},headers={'Cf-Access-Subject':role,'Content-Type':'application/json','Idempotency-Key':'auth-save'};
   const response=await admin.request(`${path}/${role}/commands`,{method:'POST',headers,body:JSON.stringify(body)},env);
   expect(response.status).toBe(role==='VIEWER'?403:200);
   if(role!=='VIEWER'){
    expect(await response.json()).toMatchObject({roundId:role,state:'DRAFT',revision:1,persisted:true,scheduleSupported:false});
    expect(response.headers.get('Cache-Control')).toBe('no-store');
   }
  }
  const read=await admin.request(path+'?limit=1', {headers:{'Cf-Access-Subject':'VIEWER'}},env);
  expect(read.status).toBe(200);const list=await read.json() as {items:Record<string,unknown>[]};
  expect(list).toMatchObject({items:[{state:'DRAFT',title:'Synthetic event',revision:1}],nextAfter:'OPERATOR'});
  expect(list.items[0]).not.toHaveProperty('definition');expect(list.items[0]).not.toHaveProperty('rewards');
  const malformed=await admin.request(`${path}/bad/commands`,{method:'POST',headers:{'Cf-Access-Subject':'OPERATOR','Content-Type':'application/json','Idempotency-Key':'bad'},
   body:JSON.stringify({...input,definition:{...definition,roundId:'bad'},reviewApproved:true})},env);
  expect(malformed.status).toBe(400);expect(f.scalar('SELECT count(*) FROM admin_event_drafts')).toBe(2);
  f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='OPERATOR'");
  expect([401,403]).toContain((await admin.request(`${path}/OPERATOR`,{headers:{'Cf-Access-Subject':'OPERATOR'}},env)).status);
 }finally{f.sql.close();}
});
it('recovers an original receipt committed after its first lookup and preserves immutable revision history',async()=>{
 const f=await fixture();try{
  const prepare=f.env.DB.prepare.bind(f.env.DB);let once=true,winner:unknown;
  f.env.DB.prepare=(sql:string)=>{
   const statement=prepare(sql);
   if(sql.startsWith('SELECT payload_hash,response_json FROM admin_event_draft_receipts')&&once){
    const first=statement.first.bind(statement);
    statement.first=async<T>()=>{
     const row=await first<T>();
     if(once){once=false;winner=await f.service.save(actor,'round',input,'race');}
     return row;
    };
   }
   return statement;
  };
  expect(await f.service.save(actor,'round',input,'race')).toEqual(winner);
  expect(f.scalar('SELECT count(*) FROM admin_event_drafts')).toBe(1);
  expect(()=>f.sql.run("UPDATE admin_event_drafts SET revision=5")).toThrow('immutable_event_draft_revision');
  expect(()=>f.sql.run("DELETE FROM admin_event_draft_receipts")).toThrow('immutable_event_draft_receipt');
 }finally{f.sql.close();}
});
