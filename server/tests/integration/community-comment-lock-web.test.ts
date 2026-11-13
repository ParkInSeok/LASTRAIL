import {describe,it,expect} from 'vitest';
import game from '../../apps/game-api/src/index';
import admin from '../../apps/admin-api/src/index';
import {communityWebFixture} from '../helpers/community-web-fixture';
import {installSyntheticCommunityConsent} from '../helpers/community-consent-fixture';
async function fixture(){
 const f=await communityWebFixture('edit');
 f.sql.run("DELETE FROM community_contents WHERE id='p'");
 const r=await game.request('/v1/web/community/posts',{method:'POST',headers:{...f.headers,'X-Community-Content-Version':'community.post-create.v1.20261008'},body:JSON.stringify({commandId:'parent',boardId:'free',title:'Title',markdown:'Body',spoiler:false,wikiRefs:[],sourceBalanceVersion:null})},f.env);
 expect(r.status).toBe(200);const {contentId}=await r.json() as {contentId:string};
 f.sql.run("INSERT INTO admin_principals VALUES('owner','owner','OWNER','ACTIVE',NULL,0,0),('operator','operator','OPERATOR','ACTIVE',NULL,0,0)");
 const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 expect((await admin.request('/v1/admin/community/moderator-grants/operator',{method:'POST',headers:{'Cf-Access-Subject':'owner','Content-Type':'application/json','X-Community-Moderation-Grant-Version':'community.moderation-grants.v1.20261008','Idempotency-Key':'grant'},body:JSON.stringify({action:'GRANT',permission:'COMMUNITY_MODERATE',expectedRevision:0,reason:'Synthetic local test grant'})},env)).status).toBe(200);
 const lock=(action:string,expectedRevision:number)=>admin.request('/v1/admin/community/contents/'+contentId+'/comment-lock',{method:'POST',headers:{'Cf-Access-Subject':'operator','Content-Type':'application/json','X-Community-Comment-Lock-Version':'community.comment-lock.v1.20261009','Idempotency-Key':action},body:JSON.stringify({action,expectedRevision,reason:'Synthetic local test reason'})},env);
 const comment=(commandId='comment',extra:Record<string,string>={})=>game.request('/v1/web/community/contents/'+contentId+'/comments',{method:'POST',headers:{...f.headers,'X-Community-Content-Version':'community.comment-create.v1.20261008',...extra},body:JSON.stringify({commandId,markdown:'New comment'})},f.env);
 return {...f,lock,comment};
}
describe('actual WEB creation after actual Admin comment unlock',()=>{
 it('keeps lock failure quota-free and resumes normal rate and original receipt',async()=>{const f=await fixture();try{
 expect((await f.lock('LOCK_COMMENTS',1)).status).toBe(200);
 const denied=await f.comment();expect(denied.status).toBe(409);expect(await denied.json()).toEqual({error:'community_comments_locked'});
 expect(f.scalar("SELECT COUNT(*) FROM community_content_creation WHERE kind='COMMENT'")).toBe(0);
 expect((await f.lock('UNLOCK_COMMENTS',2)).status).toBe(200);
 const r=await f.comment();expect(r.status).toBe(200);const receipt=await r.json();expect(await(await f.comment()).json()).toEqual(receipt);
 expect((await f.comment('second')).status).toBe(429);
 expect(f.scalar("SELECT COUNT(*) FROM community_content_creation WHERE kind='COMMENT'")).toBe(1);
 }finally{f.sql.close();}});
 it.each([{'X-CSRF-Token':''},{Cookie:''},{'X-LastRail-App-Version-Code':'124'}])('does not bypass WEB authorization %j',async extra=>{const f=await fixture();try{
 expect((await f.lock('LOCK_COMMENTS',1)).status).toBe(200);expect((await f.lock('UNLOCK_COMMENTS',2)).status).toBe(200);
 expect((await f.comment('invalid',extra)).status).toBe(403);expect(f.scalar("SELECT COUNT(*) FROM community_content_creation WHERE kind='COMMENT'")).toBe(0);
 }finally{f.sql.close();}});
 it('does not bypass newly published unaccepted policies after unlock',async()=>{const f=await fixture();try{
 expect((await f.lock('LOCK_COMMENTS',1)).status).toBe(200);expect((await f.lock('UNLOCK_COMMENTS',2)).status).toBe(200);
 await installSyntheticCommunityConsent(f.sql,f.env.DB,2,false);
 expect((await f.comment()).status).toBe(403);expect(f.scalar("SELECT COUNT(*) FROM community_content_creation WHERE kind='COMMENT'")).toBe(0);
 }finally{f.sql.close();}});
 it('does not release account suspension after unlock',async()=>{const f=await fixture();try{
 expect((await f.lock('LOCK_COMMENTS',1)).status).toBe(200);expect((await f.lock('UNLOCK_COMMENTS',2)).status).toBe(200);
 f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");expect((await f.comment()).status).toBe(403);
 expect(f.scalar("SELECT COUNT(*) FROM community_content_creation WHERE kind='COMMENT'")).toBe(0);
 }finally{f.sql.close();}});
});
