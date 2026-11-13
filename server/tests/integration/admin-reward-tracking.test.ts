import {afterEach,expect,it} from 'vitest';
import {Hono} from 'hono';
import {setup} from '../helpers/departure-fixture';
import {registerAdminRewardTrackingRoutes} from '../../apps/admin-api/src/admin-reward-tracking-routes';
import admin from '../../apps/admin-api/src/index';
const cleanup:(()=>void)[]=[];afterEach(()=>{for(const close of cleanup.splice(0))close();});
async function fixture(){
 const f=await setup(false,true,null);cleanup.push(()=>f.sql.close());
 f.sql.run("INSERT INTO admin_principals VALUES('reward-reader','reward-reader','VIEWER','ACTIVE',NULL,0,0)");
 const app=new Hono<{Bindings:Env}>();
 registerAdminRewardTrackingRoutes(app,async()=>({id:'reward-reader',role:'VIEWER'}));
 const proposal={environment:'DEV',batchId:'proposal',revision:1,title:'Bug thanks',type:'REPORTER_THANKS',incidentRef:'incident',
  policyVersion:null,targetAsOf:null,targetCondition:null,accountIds:['a','a','missing'],rewardLines:[{itemId:'cur_crystal',quantity:1500}]};
 const preview=(input:unknown=proposal)=>app.request('/v1/admin/reward-tracking/review-preview',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)},f.env);
 return {...f,app,proposal,preview};
}
it('previews explicit proposals without approving targets or issuing any reward',async()=>{
 const f=await fixture(),before=Buffer.from(f.sql.export()),response=await f.preview();
 expect(response.status).toBe(200);const body=await response.json();
 expect(body).toMatchObject({reviewApproved:false,executable:false,revision:1,targets:{requestedCount:3,uniqueProposedCount:2,duplicateInputCount:1,observedAccountCount:1,missingAccountCount:1,confirmedEligibleCount:null,unresolvedEligibilityCount:2},proposedTotals:[{itemId:'cur_crystal',quantity:'3000'}]});
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 expect(JSON.stringify(body)).not.toContain('displayName');
});
it('binds hashes to content/revision and does not silently default to all accounts',async()=>{
 const f=await fixture(),one=await(await f.preview()).json();
 expect((await(await f.preview()).json()).reviewHash).toBe(one.reviewHash);
 const changed=await(await f.preview({...f.proposal,revision:2})).json();expect(changed.reviewHash).not.toBe(one.reviewHash);
 expect((await f.preview({...f.proposal,accountIds:[]})).status).toBe(400);
 expect((await f.preview({...f.proposal,environment:'PRD'})).status).toBe(409);
});
it('uses current persisted SUPPORT_READ role/status and never trusts stale authentication role alone',async()=>{
 const f=await fixture();f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='reward-reader'");
 expect((await f.preview()).status).toBe(403);
});
it('separates approved write permission from unsupported payout commands and reuses the account receipt endpoint',async()=>{
 const f=await fixture(),response=await f.app.request('/v1/admin/reward-tracking/capabilities',{},f.env),body=await response.json();
 expect(response.status).toBe(200);
 expect(body).toMatchObject({permissions:{read:'SUPPORT_READ',saveDraft:'REWARD_MANAGEMENT_WRITE',review:'REWARD_MANAGEMENT_WRITE',
  execute:'REWARD_MANAGEMENT_WRITE',stop:'REWARD_MANAGEMENT_WRITE',delta:'REWARD_MANAGEMENT_WRITE'},
  writeRoles:['OWNER','OPERATOR'],executionSupported:false,reviewApprovalSupported:false,
  supportedWriteCommands:['SAVE_DRAFT'],originalAccountReceiptRoute:'/v1/admin/support/accounts/:accountId'});
 expect(body.missingSources).not.toContain('ORIGINAL_REVIEW_EXECUTE_STOP_DELTA_PERMISSION_MAPPING');
 expect((await f.app.request('/v1/admin/reward-tracking/execute',{method:'POST'},f.env)).status).toBe(404);
});
it('registers the real authenticated Admin routes and rechecks disabled principals',async()=>{
 const f=await fixture(),env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',
  ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const headers={'Cf-Access-Subject':'reward-reader','Content-Type':'application/json'};
 const capabilities=await admin.request('/v1/admin/reward-tracking/capabilities',{headers},env);
 expect(capabilities.status).toBe(200);
 expect(await capabilities.json()).toMatchObject({permissions:{read:'SUPPORT_READ'},executionSupported:false});
 const preview=await admin.request('/v1/admin/reward-tracking/review-preview',{method:'POST',headers,body:JSON.stringify(f.proposal)},env);
 expect(preview.status).toBe(200);expect(await preview.json()).toMatchObject({reviewApproved:false,executable:false});
 const unauthenticated=await admin.request('/v1/admin/reward-tracking/capabilities',{},env);
 expect(unauthenticated.status).not.toBe(200);
 f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='reward-reader'");
 expect((await admin.request('/v1/admin/reward-tracking/capabilities',{headers},env)).status).not.toBe(200);
});
