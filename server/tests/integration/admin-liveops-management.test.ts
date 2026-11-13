import {expect,it} from 'vitest';
import admin from '../../apps/admin-api/src/index';
import {setup} from '../helpers/departure-fixture';

async function fixture(){
 const f=await setup(false,true,null);
 for(const role of ['OWNER','OPERATOR','VIEWER'])f.sql.run("INSERT INTO admin_principals VALUES(?,?,?,'ACTIVE',NULL,0,0)",[role,role,role]);
 const env={...f.env,ENVIRONMENT:'DEV',ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const document={presentationRevision:1,type:'EVENT',title:'TEST_ONLY',summary:'Notice only; no reward issuance',localizedBody:{ko:'시험 공지'},bodyFormat:'PLAIN_TEXT',surfaces:'WEB',visibility:'PUBLIC',channel:null,platform:null,minVersionCode:null,maxVersionCode:null,startsAt:0,endsAt:null,imageAssetId:null,action:null,popupMode:'NONE',priority:0,pinned:false};
 const get=(path:string,role='OWNER')=>admin.request('/v1/admin/liveops'+path,{headers:{'Cf-Access-Subject':role}},env);
 const command=(body:unknown,key:string,role='OWNER')=>admin.request('/v1/admin/liveops/notices/n1/commands',{method:'POST',headers:{'Cf-Access-Subject':role,'Content-Type':'application/json','Idempotency-Key':key},body:JSON.stringify(body)},env);
 const draft=(revision:number|null=null,doc=document)=>({kind:'DOMAIN_COMMAND',reviewId:null,command:{command:'SAVE_DRAFT',environment:'DEV',expectedRevision:revision,reason:'TEST_ONLY draft',document:doc}});
 const review=(revision=1)=>({kind:'REVIEW',expectedRevision:revision,reason:'TEST_ONLY reviewed'});
 const execute=(reviewId:string,revision=1)=>({kind:'DOMAIN_COMMAND',reviewId,command:{command:'PUBLISH',environment:'DEV',expectedRevision:revision,reason:'TEST_ONLY apply'}});
 return {...f,env,document,get,command,draft,review,execute};
}
it('exposes only source-backed support and reuses notice draft, review and original execution receipts',async()=>{
 const f=await fixture();
 const support=await f.get('/capabilities');expect(support.status).toBe(200);
 expect(await support.json()).toMatchObject({contractVersion:'admin-liveops-management.v1.20261020',domains:{NOTICE:{supported:true},EVENT_REWARDS:{supported:false}}});
 const draft=await f.command(f.draft(),'draft','OPERATOR');expect(draft.status).toBe(200);expect(await draft.json()).toMatchObject({notice:{revision:1,status:'DRAFT',document:f.document}});
 const reviewed=await f.command(f.review(),'review');expect(reviewed.status).toBe(200);const r=await reviewed.json();
 const applied=await f.command(f.execute(r.reviewId),'apply');expect(applied.status).toBe(200);const receipt=await applied.json();expect(receipt).toMatchObject({notice:{revision:2,status:'PUBLISHED'},command:'PUBLISH'});
 expect(await (await f.command(f.execute(r.reviewId),'apply')).json()).toEqual(receipt);
 expect((await f.get('/notices/n1/history')).status).toBe(200);
 expect(f.scalar('SELECT count(*) FROM notice_revisions')).toBe(2);
 expect(f.scalar('SELECT count(*) FROM notice_write_guard')).toBe(0);
});
it('invalidates review on edits, preserves original publication and recovers old execution after later changes',async()=>{
 const f=await fixture();await f.command(f.draft(),'d1');const review=await (await f.command(f.review(),'r1')).json();
 await f.command(f.draft(1,{...f.document,title:'Changed'}),'d2');
 expect((await f.command(f.execute(review.reviewId,2),'stale')).status).toBe(409);
 expect(f.scalar("SELECT count(*) FROM notice_revisions WHERE status='PUBLISHED'")).toBe(0);
 const r2=await (await f.command(f.review(2),'r2')).json(),body=f.execute(r2.reviewId,2);
 const original=await (await f.command(body,'publish')).json();
 await f.command(f.draft(3,{...f.document,title:'New pending draft'}),'d3');
 expect(await (await f.command(body,'publish')).json()).toEqual(original);
 expect(await (await f.get('/notices/n1')).json()).toMatchObject({notice:{revision:4,status:'DRAFT'},publication:{revision:3,status:'PUBLISHED'},review:null});
});
it('uses existing permissions, forbids missing reviews and rejects reused keys across commands',async()=>{
 const f=await fixture();
 expect((await f.command(f.draft(),'denied','VIEWER')).status).toBe(403);
 await f.command(f.draft(),'draft','OPERATOR');
 expect((await f.command(f.review(),'denied-review','OPERATOR')).status).toBe(403);
 const review=await (await f.command(f.review(),'review')).json();
 expect((await f.command(f.execute(review.reviewId),'denied-execution','OPERATOR')).status).toBe(403);
 expect((await f.command({...f.execute(review.reviewId),reviewId:null},'no-review')).status).toBe(409);
 expect((await f.command(f.draft(1),'review')).status).toBe(409);
 await f.command(f.draft(1),'owner-draft');
 expect((await f.command(f.review(2),'owner-draft')).status).toBe(409);
 expect((await f.command({...f.review(2),reason:'changed'},'review')).status).toBe(409);
});
it('fences revoked authority and source changes at review and execution commit',async()=>{
 const f=await fixture();await f.command(f.draft(),'draft');
 f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OWNER'");};
 expect((await f.command(f.review(),'review-denied')).status).toBe(403);
 expect(f.scalar('SELECT count(*) FROM liveops_notice_reviews')).toBe(0);
 f.sql.run("UPDATE admin_principals SET role='OWNER' WHERE id='OWNER'");
 const review=await (await f.command(f.review(),'review')).json();
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='OWNER'");};
 expect((await f.command(f.execute(review.reviewId),'execute-denied')).status).toBe(403);
 expect(f.scalar('SELECT count(*) FROM notice_revisions')).toBe(1);
});
it('rolls back review on audit failure and keeps original-key review recovery stable',async()=>{
 const f=await fixture();await f.command(f.draft(),'draft');
 f.sql.run("CREATE TRIGGER test_liveops_audit_failure BEFORE INSERT ON admin_audit_logs WHEN NEW.action='LIVEOPS_NOTICE_REVIEW' BEGIN SELECT RAISE(ABORT,'TEST_FAILURE'); END");
 expect((await f.command(f.review(),'review')).status).toBe(503);
 expect(f.scalar('SELECT count(*) FROM liveops_notice_reviews')).toBe(0);
 f.sql.run('DROP TRIGGER test_liveops_audit_failure');
 const original=await (await f.command(f.review(),'review')).json();
 await f.command(f.draft(1),'edit');
 expect(await (await f.command(f.review(),'review')).json()).toEqual(original);
 expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE action='LIVEOPS_NOTICE_REVIEW'")).toBe(1);
 expect(f.scalar('SELECT count(*) FROM notice_write_guard')).toBe(0);
});
it('recovers a concurrent same-key review committed between replay miss and changed source read',async()=>{
 const f=await fixture();await f.command(f.draft(),'draft');
 const prepare=f.adapter.prepare.bind(f.adapter);let interleaved=false,committed:unknown;
 f.adapter.prepare=(sql:string)=>{
  const statement=prepare(sql);
  if(sql.startsWith('SELECT payload_hash,response_json FROM liveops_notice_reviews')){
   const first=statement.first.bind(statement);
   statement.first=async<T>()=>{
    const result=await first<T>();
    if(!interleaved&&result===null){
     interleaved=true;
     const winner=await f.command(f.review(),'same-review');expect(winner.status).toBe(200);committed=await winner.json();
     expect((await f.command(f.draft(1,{...f.document,title:'Later draft'}),'later-draft')).status).toBe(200);
    }
    return result;
   };
  }
  return statement;
 };
 try{
  const retry=await f.command(f.review(),'same-review');expect(retry.status).toBe(200);
  expect(await retry.json()).toEqual(committed);
  expect(f.scalar('SELECT count(*) FROM liveops_notice_reviews')).toBe(1);
  expect(f.scalar("SELECT count(*) FROM admin_audit_logs WHERE action='LIVEOPS_NOTICE_REVIEW'")).toBe(1);
  expect(await (await f.get('/notices/n1')).json()).toMatchObject({notice:{revision:2,document:{title:'Later draft'}}});
 }finally{f.adapter.prepare=prepare;}
});
it('rejects a source revision changed after review validation but before publication batch',async()=>{
 const f=await fixture();await f.command(f.draft(),'draft');
 const review=await (await f.command(f.review(),'review')).json();
 f.adapter.beforeBatch=()=>{
  f.adapter.beforeBatch=undefined;
  f.sql.run("INSERT INTO notice_revisions SELECT environment,notice_id,revision+1,status,document_json,updated_at FROM notice_revisions WHERE environment='DEV' AND notice_id='n1' AND revision=1");
  f.sql.run("UPDATE notice_heads SET revision=2 WHERE environment='DEV' AND notice_id='n1'");
 };
 expect((await f.command(f.execute(review.reviewId),'publication-race')).status).toBe(409);
 expect(f.scalar("SELECT count(*) FROM notice_revisions WHERE status='PUBLISHED'")).toBe(0);
 expect(f.scalar("SELECT count(*) FROM notice_command_receipts WHERE request_key='publication-race'")).toBe(0);
 expect(f.scalar('SELECT count(*) FROM notice_write_guard')).toBe(0);
});
it('schedules through original owner and pages immutable execution history without inventing rewards',async()=>{
 const f=await fixture();await f.command(f.draft(null,{...f.document,startsAt:Date.now()+60000}),'draft');
 const review=await (await f.command(f.review(),'review')).json(),body=f.execute(review.reviewId);body.command.command='SCHEDULE';
 expect((await f.command(body,'schedule')).status).toBe(200);
 const first=await (await f.get('/notices/n1/history?limit=1')).json();
 expect(first).toMatchObject({items:[{revision:2,status:'SCHEDULED',receipt:{command:'SCHEDULE'}}],nextBefore:2});
 const next=await (await f.get('/notices/n1/history?before=2&limit=1')).json();
 expect(next).toMatchObject({items:[{revision:1,status:'DRAFT'}],nextBefore:null});
 expect((await f.get('/notices?limit=1')).status).toBe(200);
 expect((await f.get('/notices?environment=PRD')).status).toBe(400);
 expect((await f.command({kind:'PAYOUT',amount:1500},'payout')).status).toBe(400);
});
