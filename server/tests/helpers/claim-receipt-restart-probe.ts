import {readFileSync} from 'node:fs';
import initSqlJs from 'sql.js';
import {readCompleteRunClaim} from '../../apps/game-api/src/run-complete-claim';
import {SqlJsD1Database} from './growth-d1';

// Test-only fresh-process reader: no client owner implementation or HTTP fixture.
const [path,runId,previewId]=process.argv.slice(2);
if(!path||!runId||!previewId)throw Error('missing probe arguments');
let httpCalls=0;
globalThis.fetch=async()=>{httpCalls++;throw Error('unexpected network');};
const SQL=await initSqlJs(),sql=new SQL.Database(readFileSync(path));
try{
 const before=Buffer.from(sql.export()),db=new SqlJsD1Database(sql) as unknown as D1Database;
 const receipt=await readCompleteRunClaim(db,'a',runId,previewId,'complete-key');
 const foreign=await readCompleteRunClaim(db,'b',runId,previewId,'complete-key');
 let conflict=false;
 try{await readCompleteRunClaim(db,'a',runId+'-other',previewId,'complete-key');}
 catch(error){conflict=error instanceof Error&&error.message==='run_claim_complete_request_conflict';}
 const scalar=(query:string)=>sql.exec(query)[0]?.values[0]?.[0];
 process.stdout.write(JSON.stringify({pid:process.pid,receipt,foreign,conflict,httpCalls,
  unchanged:before.equals(Buffer.from(sql.export())),
  credits:scalar("SELECT balance FROM account_currencies WHERE account_id='a' AND currency='CREDITS'"),
  grants:scalar('SELECT count(*) FROM reward_transactions'),receipts:scalar('SELECT count(*) FROM run_complete_claim_receipts')}));
}finally{sql.close();}
