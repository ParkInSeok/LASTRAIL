import type {Hono} from 'hono';
import {verifyAccessToken} from './auth';
import {readBoundedBody} from './activity-routes';
import {ActivityError} from './activity-policy';

export const isGameLogoutRoute=(method:string,path:string)=>method==='POST'&&path==='/v1/auth/logout';
/** A signed original session may only end its own family, including a retry
 * after revocation. It never regains gameplay authority or ends a later login. */
export function registerGameLogoutRoute(app:Hono<{Bindings:Env}>,secretFor:(env:Env)=>string|null){
 app.post('/v1/auth/logout',async c=>{
  c.header('Cache-Control','no-store');
  try{
   if(new URL(c.req.url).search||(await readBoundedBody(c,1024)).length)return c.json({accepted:false,errorCode:'contract_violation'},400);
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);throw error;}
  const secret=secretFor(c.env);if(!secret?.trim())return c.json({accepted:false,errorCode:'auth_not_configured'},500);
  const authorization=c.req.header('Authorization');if(!authorization?.startsWith('Bearer '))return c.json({accepted:false,errorCode:'auth_required'},401);
  const now=Date.now(),verified=await verifyAccessToken({secret,token:authorization.slice(7).trim(),nowMs:now});
  if(!verified)return c.json({accepted:false,errorCode:'access_token_invalid'},401);
  const row=await c.env.DB.prepare(`SELECT s.family_id,s.expires_at,
   EXISTS(SELECT 1 FROM auth_identities WHERE account_id=s.account_id AND provider='GOOGLE')
   OR EXISTS(SELECT 1 FROM auth_password_credentials WHERE account_id=s.account_id) AS recoverable
   FROM auth_sessions s WHERE s.id=? AND s.account_id=?`).bind(verified.sessionId,verified.accountId)
   .first<{family_id:string;expires_at:number;recoverable:number}>();
  if(!row||row.expires_at<=now)return c.json({accepted:false,errorCode:'auth_session_invalid'},401);
  if(!row.recoverable)return c.json({accepted:false,errorCode:'guest_recovery_required'},409);
  // Existing rotation trigger requires a ROTATED predecessor. Revocation of
  // all generations prevents a concurrent refresh from resurrecting this family.
  await c.env.DB.batch([c.env.DB.prepare("UPDATE auth_sessions SET status='REVOKED' WHERE account_id=? AND family_id=? AND status IN ('ACTIVE','ROTATED')")
   .bind(verified.accountId,row.family_id)]);
  return c.json({accepted:true,version:'data.account-exit.v1.20261021',accountId:verified.accountId,sessionId:verified.sessionId,status:'SESSION_FAMILY_ENDED'});
 });
}
