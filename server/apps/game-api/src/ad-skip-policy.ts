import { z } from "zod";
import { ActivityError } from "./activity-policy";
const id=z.string().min(1).max(200);
export const adSkipCatalogSchema=z.strictObject({version:id,provider:z.enum(["GOOGLE_PLAY","APP_STORE"]),appId:id,productId:id,
  environment:z.enum(["LOCAL","DEV","PRD"]),checkoutLifetimeMs:z.number().int().positive().max(Number.MAX_SAFE_INTEGER)});
export type AdSkipCatalog=z.infer<typeof adSkipCatalogSchema>;
export async function readAdSkipCatalog(db:D1Database,environment:string):Promise<AdSkipCatalog|null>{
  const row=await db.prepare("SELECT config_json FROM ad_skip_catalogs WHERE environment=?").bind(environment).first<{config_json:string}>();
  if(!row)return null;
  let input:unknown;try{input=JSON.parse(row.config_json);}catch{throw new ActivityError(503,"ad_skip_purchase_unconfigured");}
  const p=adSkipCatalogSchema.safeParse(input);
  if(!p.success||p.data.environment!==environment)throw new ActivityError(503,"ad_skip_purchase_unconfigured");
  return p.data;
}
