import {HTTPException} from 'hono/http-exception';
import {ActivityError} from './activity-policy';
import {maintenanceContext,maintenanceLoginStatement,maintenanceAdmissionError} from './maintenance-admission';

/** All statements roll back together, including the old GAME slot replacement. */
export async function commitLoginSession(db:D1Database,environment:unknown,channel:'GAME'|'WEB',statements:D1PreparedStatement[]){
 try{
  return await db.batch([maintenanceLoginStatement(db,maintenanceContext(environment),channel),...statements]);
 }catch(error){
  const missingSchema=error instanceof Error&&/no such table: (?:maintenance_admission_guard|maintenance_runtime_heads|maintenance_runtime_revisions|maintenance_plan_revisions|maintenance_immediate_modes)\b/u.test(error.message);
  const classified=error instanceof ActivityError?error:maintenanceAdmissionError(error)
   ??(missingSchema?new ActivityError(503,'maintenance_policy_unavailable'):null);
  if(!classified)throw error;
  const code=classified.code==='maintenance_new_request_blocked'?'maintenance_login_blocked':classified.code;
  throw new HTTPException(503,{res:Response.json({error:code,contractVersion:'maintenance-login-v1',
   confirmed:code==='maintenance_login_blocked'},{status:503,headers:{'Cache-Control':'no-store'}})});
 }
}
