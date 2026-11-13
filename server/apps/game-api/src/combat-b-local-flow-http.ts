import {Hono} from 'hono';
import {canonicalJson} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {createCombatBSelectionHttp} from './combat-b-selection-http';
import {createCombatBRefitHttp} from './combat-b-refit-http';
import {LocalFixtureBSelectionCatalog} from './combat-b-selection-catalog';
import {LocalRefitPersistenceSource} from './combat-b-refit-persistence-source';
import {captureLocalFlowPacket,localSourceVersion,type RefitRead,type LocalReference} from './combat-b-local-flow-source';
export function createLocalFlowSourceHttp(reference:LocalReference){
 const catalog=new LocalFixtureBSelectionCatalog(),refit=createCombatBRefitHttp(new LocalRefitPersistenceSource(),catalog),selection=createCombatBSelectionHttp(catalog),app=new Hono<{Bindings:Env}>();
 app.all('*',async c=>{
  const url=new URL(c.req.url);
  if(url.pathname==='/_local/combat-b/selection')return selection.fetch(c.req.raw,c.env);
  if(url.pathname!=='/_local/combat-b/source')return refit.fetch(c.req.raw,c.env);
  c.header('Cache-Control','no-store');c.header('X-Combat-B-Source-Version',localSourceVersion);
  try{
   if(c.req.method!=='GET'||url.search||c.req.header('Content-Length')&&c.req.header('Content-Length')!=='0')throw new ActivityError(400,'combat_b_source_request_invalid');
   const capture=async()=>{const target=new URL('/_local/combat-b/refit',url);return refit.fetch(new Request(target,{headers:c.req.raw.headers}),c.env);};
   // Reuse existing real auth, origin, LOCAL gate, exact transport header and source consistency owner.
   for(let n=0;n<4;n++){
    const before=await capture();if(before.status!==200)return before;
    if(c.req.header('X-Combat-B-Source-Version')!==localSourceVersion)throw new ActivityError(409,'combat_b_source_version_conflict');
    const current=await before.json() as RefitRead,packet=await captureLocalFlowPacket(c.env.DB,current,reference);
    const after=await capture();if(after.status!==200)return after;
    if(canonicalJson(current as never)!==canonicalJson(await after.json() as never))continue;
    const check=await captureLocalFlowPacket(c.env.DB,current,reference);
    if(packet.packetHash!==check.packetHash)continue;
    // Final account/session/deadline check after owned source read, never publishes post-revocation data.
    const final=await capture();if(final.status!==200)return final;
    if(canonicalJson(current as never)!==canonicalJson(await final.json() as never))continue;
    return c.json(packet);
   }
   throw new ActivityError(409,'combat_b_source_read_conflict');
  }catch(e){if(e instanceof ActivityError)return c.json({accepted:false,errorCode:e.code},e.status);console.error('combat_b_local_source_unavailable');return c.json({accepted:false,errorCode:'combat_b_local_source_unavailable'},503);}
 });return app;
}