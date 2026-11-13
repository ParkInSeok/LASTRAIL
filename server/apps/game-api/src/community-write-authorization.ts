import type {Context} from 'hono';
import {sha256Hex} from '@lastrail/common';
import {CommunityStoreError} from './community-content-store';
type Ctx=Context<{Bindings:Env}>;
export async function communityWriteAuthorization(c:Ctx,accountId:string,authorize:()=>Promise<string>){
 const csrfHash='sha256:'+await sha256Hex(c.req.header('X-CSRF-Token')!);
 return async()=>{
     if(await authorize()!==accountId)throw new CommunityStoreError(403,'community_forbidden');
     // The session core verified this CSRF against the exact browser cookie; fence current rights at commit too.
     const tester=c.env.ENVIRONMENT==='DEV'?` AND EXISTS(SELECT 1 FROM account_tester_entitlements
       WHERE account_id=? AND environment='DEV' AND version_code=? AND status='ACTIVE'
       AND revoked_at IS NULL AND expires_at>(julianday('now')-2440587.5)*86400000)`:'';
     const values:string[]=[accountId,csrfHash];if(tester)values.push(accountId,c.req.header('X-LastRail-App-Version-Code')!);
     return [c.env.DB.prepare(`INSERT INTO community_content_write_guard(valid) SELECT CASE WHEN EXISTS(
       SELECT 1 FROM web_sessions s JOIN accounts a ON a.id=s.account_id WHERE s.account_id=? AND s.csrf_hash=?
       AND s.revoked_at IS NULL AND s.expires_at>(julianday('now')-2440587.5)*86400000 AND a.status='ACTIVE')${tester} THEN 1 ELSE 0 END`).bind(...values)];
    };
}
