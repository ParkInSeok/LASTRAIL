import { canonicalJson,sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
export type AdSkipActor={db:D1Database;accountId:string;environment:"LOCAL"|"DEV"|"PRD"};
export const adSkipHash=(value:Parameters<typeof canonicalJson>[0])=>sha256Hex(canonicalJson(value));
export async function adSkipReplay(o:AdSkipActor,operation:string,key:string,hash:string){
  const r=await o.db.prepare(`SELECT request_hash,response_json FROM ad_skip_request_receipts WHERE account_id=? AND environment=? AND operation=? AND request_key=?`)
    .bind(o.accountId,o.environment,operation,key).first<{request_hash:string;response_json:string}>();
  if(!r)return null;if(r.request_hash!==hash)throw new ActivityError(409,"idempotency_conflict");
  return JSON.parse(r.response_json) as Record<string,unknown>;
}
export function adSkipReceipt(o:AdSkipActor,operation:string,key:string,hash:string,response:unknown){
  return o.db.prepare("INSERT INTO ad_skip_request_receipts VALUES(?,?,?,?,?,?)").bind(o.accountId,o.environment,operation,key,hash,JSON.stringify(response));
}
export async function adSkipMutation(o:AdSkipActor,operation:string){
  const row=await o.db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS n FROM account_mutations WHERE account_id=?").bind(o.accountId).first<{n:number}>();
  return o.db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,?,?,?,?)")
    .bind('ad_skip_'+crypto.randomUUID(),o.accountId,row!.n,operation,Date.now());
}
export async function adSkipRetry<T>(action:()=>Promise<T>):Promise<T>{
  for(let n=0;n<4;n++)try{return await action();}catch(e){
    if(!/UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(e)))throw e;
    if(n===3)throw new ActivityError(409,"ad_skip_state_changed");
  }
  throw new ActivityError(409,"ad_skip_state_changed");
}
