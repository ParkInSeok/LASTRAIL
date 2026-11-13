import type {Context,Hono} from "hono";
import {readBoundedBody,type ActivityAuthResult} from "./activity-routes";
import {ActivityError} from "./activity-policy";
import {readCoopStatus} from "./combat-coop-status";
export function registerCoopStatusRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
 app.get("/v1/runs/:runId/coop-status",async c=>{
  c.header("Cache-Control","no-store");
  const auth=await authenticate(c);
  if("errorCode" in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{
   if((await readBoundedBody(c)).length||new URL(c.req.url).search||c.req.param("runId").length>200)throw new ActivityError(400,"contract_violation");
   return c.json(await readCoopStatus(c.env.DB,auth.accountId,c.req.param("runId"),Date.now()));
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);throw error;}
 });
}
