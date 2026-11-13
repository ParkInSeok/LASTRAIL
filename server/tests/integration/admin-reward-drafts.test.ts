import {afterEach,expect,it,vi} from 'vitest';
import initSqlJs from 'sql.js';
import {readFileSync,readdirSync} from 'node:fs';
import {SqlJsD1Database} from '../helpers/growth-d1';
import {AdminRewardDrafts} from '../../apps/admin-api/src/admin-reward-drafts';
import {registerAdminRewardDraftRoutes} from '../../apps/admin-api/src/admin-reward-draft-routes';
import {Hono} from 'hono';
const cleanup:(()=>void)[]=[];afterEach(()=>{vi.restoreAllMocks();for(const close of cleanup.splice(0))close();});
const actor={id:'writer',role:'OPERATOR' as const};
const proposal=(revision=1)=>({environment:'DEV',batchId:'batch',revision,title:'Review draft',type:'EVENT',incidentRef:'incident',
 policyVersion:null,targetAsOf:null,targetCondition:null,accountIds:['a'],rewardLines:[{itemId:'cur_crystal',quantity:10}]});
const command=(expectedRevision:number|null=null)=>({command:'SAVE_DRAFT',expectedRevision,reason:'Prepare review',proposal:proposal((expectedRevision??0)+1)});
async function fixture(){
 const SQL=await initSqlJs(),sql=new SQL.Database();cleanup.push(()=>sql.close());
 sql.run('PRAGMA foreign_keys=ON');
 for(const f of readdirSync('packages/db/migrations').filter(f=>/^\d+_.+\.sql$/u.test(f)).sort())sql.run(readFileSync('packages/db/migrations/'+f,'utf8'));
 sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0)");
 sql.run("INSERT INTO admin_principals VALUES('writer','writer','OPERATOR','ACTIVE',NULL,0,0)");
 const db=new SqlJsD1Database(sql),service=new AdminRewardDrafts(db as unknown as D1Database,'DEV');
 return {sql,db,service};
}
it('saves a complete DRAFT response and immutable revision/receipt/audit, without granting rewards',async()=>{
 const f=await fixture(),result=await f.service.save(actor,'batch',command(),'key1');
 expect(result).toMatchObject({accepted:true,status:'DRAFT',revision:1,reviewApproved:false,executable:false,
  preview:{proposal:proposal(),executable:false}});
 expect(f.sql.exec('SELECT COUNT(*) FROM reward_transactions')[0]!.values[0]![0]).toBe(0);
 expect(f.sql.exec("SELECT permission FROM admin_audit_logs WHERE resource_type='REWARD_DRAFT'")[0]!.values).toEqual([['REWARD_MANAGEMENT_WRITE']]);
 expect(()=>f.sql.run("UPDATE admin_reward_draft_revisions SET document_json='{}'")).toThrow();
});
it('replays original bytes after a later revision and refuses same-key payload changes',async()=>{
 const f=await fixture(),first=await f.service.save(actor,'batch',command(),'key1');
 await f.service.save(actor,'batch',command(1),'key2');
 expect(await f.service.save(actor,'batch',command(),'key1')).toEqual(first);
 await expect(f.service.save(actor,'batch',{...command(),reason:'changed'},'key1')).rejects.toMatchObject({code:'idempotency_conflict'});
 await expect(f.service.save(actor,'batch',command(),'newkey')).rejects.toMatchObject({code:'reward_draft_revision_conflict'});
});
it('denies VIEWER and fences permission revocation before the atomic batch',async()=>{
 const f=await fixture();
 f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='writer'");
 await expect(f.service.save({...actor,role:'VIEWER'},'batch',command(),'viewer')).rejects.toMatchObject({status:403});
 f.sql.run("UPDATE admin_principals SET role='OPERATOR' WHERE id='writer'");
 f.db.beforeBatch=()=>f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='writer'");
 await expect(f.service.save(actor,'batch',command(),'revoked')).rejects.toMatchObject({status:403});
 expect(f.sql.exec('SELECT COUNT(*) FROM admin_reward_draft_revisions')[0]!.values[0]![0]).toBe(0);
});
it('rolls back all draft changes when audit persistence fails and recovers an ACK-lost committed original',async()=>{
 const f=await fixture();
 f.sql.run("CREATE TRIGGER fail_reward_audit BEFORE INSERT ON admin_audit_logs BEGIN SELECT RAISE(ABORT,'audit unavailable'); END");
 await expect(f.service.save(actor,'batch',command(),'key')).rejects.toMatchObject({status:503});
 expect(f.sql.exec('SELECT COUNT(*) FROM admin_reward_draft_revisions')[0]!.values[0]![0]).toBe(0);
 f.sql.run('DROP TRIGGER fail_reward_audit');
 const batch=f.db.batch.bind(f.db);vi.spyOn(f.db,'batch').mockImplementationOnce(async statements=>{await batch(statements);throw Error('ACK lost');});
 const result=await f.service.save(actor,'batch',command(),'key');
 expect(await f.service.save(actor,'batch',command(),'key')).toEqual(result);
 expect(f.sql.exec('SELECT COUNT(*) FROM admin_reward_draft_receipts')[0]!.values[0]![0]).toBe(1);
});
it('preserves a competing newer revision and never writes the stale command receipt',async()=>{
 const f=await fixture();await f.service.save(actor,'batch',command(),'initial');
 f.db.beforeBatch=()=>{
  f.db.beforeBatch=undefined;
  f.sql.run("INSERT INTO admin_reward_draft_revisions SELECT environment,batch_id,2,status,document_json,created_by,created_at FROM admin_reward_draft_revisions WHERE revision=1");
  f.sql.run("UPDATE admin_reward_draft_heads SET revision=2");
 };
 await expect(f.service.save(actor,'batch',command(1),'stale')).rejects.toMatchObject({code:'reward_draft_revision_conflict'});
 expect(f.sql.exec("SELECT COUNT(*) FROM admin_reward_draft_receipts WHERE request_key='stale'")[0]!.values[0]![0]).toBe(0);
 expect(f.sql.exec('SELECT revision FROM admin_reward_draft_heads')[0]!.values[0]![0]).toBe(2);
});
it('exposes only the approved draft command and requires an original request key',async()=>{
 const f=await fixture(),app=new Hono<{Bindings:Env}>();
 f.sql.run("UPDATE admin_principals SET role='OWNER' WHERE id='writer'");
 registerAdminRewardDraftRoutes(app,async()=>({...actor,role:'OWNER'}));
 const env={DB:f.db as unknown as D1Database,ENVIRONMENT:'DEV'} as Env;
 const request=(key?:string,input:unknown=command())=>app.request('/v1/admin/reward-tracking/drafts/batch/commands',{
  method:'POST',headers:{'Content-Type':'application/json',...(key?{'Idempotency-Key':key}:{})},body:JSON.stringify(input)},env);
 expect((await request()).status).toBe(400);
 expect((await request('route')).status).toBe(200);
 expect((await request('execute',{...command(),command:'EXECUTE'})).status).toBe(400);
});
it('returns the latest stored draft on reentry under SUPPORT_READ without refreshing frozen target observations',async()=>{
 const f=await fixture();await f.service.save(actor,'batch',command(),'first');
 await f.service.save(actor,'batch',command(1),'second');
 f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='writer'");
 f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
 const before=Buffer.from(f.sql.export()),result=await f.service.read('writer','batch');
 expect(result).toMatchObject({revision:2,status:'DRAFT',observationsCurrent:false,reviewApproved:false,executable:false});
 expect(result.preview.observations[0]!.account!.status).toBe('ACTIVE');
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 await expect(f.service.read('writer','missing')).rejects.toMatchObject({status:404});
});
it('uses real Admin authentication for OWNER/OPERATOR draft writes and VIEWER reads only',async()=>{
 const f=await fixture(),{default:admin}=await import('../../apps/admin-api/src/index');
 const env={DB:f.db as unknown as D1Database,ENVIRONMENT:'DEV',ADMIN_AUTH_MODE:'LOCAL_HEADER',
  ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',
  ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''} as Env;
 const headers={'Cf-Access-Subject':'writer','Content-Type':'application/json','Idempotency-Key':'real-route'};
 const path='/v1/admin/reward-tracking/drafts/batch';
 expect((await admin.request(path+'/commands',{method:'POST',headers,body:JSON.stringify(command())},env)).status).toBe(200);
 const read=await admin.request(path,{headers},env);expect(read.status).toBe(200);
 expect(await read.json()).toMatchObject({revision:1,status:'DRAFT',observationsCurrent:false});
 expect([401,403]).toContain((await admin.request(path,{},env)).status);
 f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='writer'");
 expect((await admin.request(path,{headers},env)).status).toBe(200);
 expect((await admin.request(path+'/commands',{method:'POST',headers:{...headers,'Idempotency-Key':'viewer'},body:JSON.stringify(command(1))},env)).status).toBe(403);
 f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='writer'");
 expect([401,403]).toContain((await admin.request(path,{headers},env)).status);
});
