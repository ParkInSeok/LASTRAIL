import {writeFileSync,mkdtempSync,rmSync,rmdirSync} from 'node:fs';
import {spawnSync} from 'node:child_process';
import {join} from 'node:path';
import {createServer} from 'node:http';
import {z} from 'zod';
import {sha256Hex} from '@lastrail/common';
import economy from '../../packages/balance/src/launch-economy-v1.json';
import {createAccessToken} from '../../apps/game-api/src/auth';
import {it,expect,afterEach,vi} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import app from '../../apps/game-api/src/index';
const closes:(()=>void)[]=[];afterEach(()=>{vi.restoreAllMocks();for(const close of closes.splice(0))close();});
const version='run.reward-claim.v2.20261019';
it('supplies nonempty progress projection definitions and its independent source version without rewriting the claim',async()=>{
 const f=await fixture(),sequence=Number(f.scalar("SELECT max(sequence) FROM account_mutations WHERE account_id='a'"))+1;
 const delta=(operation:string)=>JSON.stringify({runId:f.runId,actId:'act_01_wasteland_railway',highestStoryProgressId:operation,highestClearedOperationId:operation});
 f.sql.run("INSERT INTO account_mutations VALUES('progress-observed','a',?,'STORY_PROGRESS',NULL,3)",[sequence]);
 f.sql.run("INSERT INTO account_story_progress VALUES('a','act_01_wasteland_railway','opn_02_first_signal','opn_02_first_signal',?,'[\"signal-found\"]','act1.2026-08-27.001',?,3)",[JSON.stringify(['opn_01_last_departure','opn_02_first_signal']),f.runId]);
 f.sql.run("INSERT INTO progression_ledger VALUES('progress-observed-ledger','a','progress-observed','STORY_PROGRESS','opn_02_first_signal',?,3)",[delta('opn_02_first_signal')]);
 f.sql.run("INSERT INTO account_unlocks VALUES('a','operation:opn_03_water_stop',3)");
 const claim=await f.claim();expect(claim.status).toBe(200);const original=await claim.json();
 const get=(v='story.progress-observation.v1.20261022')=>app.request('/v1/story-progress',{headers:{...f.headers,'X-Story-Progress-Version':v}},f.env);
 const response=await get();expect(response.status).toBe(200);const current=await response.json();
 expect(current.sourceOwner).toEqual({accountId:'a',...original.owners.progress});
 expect(current.storyProgress).toMatchObject({order:2,highestStoryProgressId:'opn_02_first_signal'});
 expect(current.storyFlags).toEqual(['signal-found']);expect(current.unlockedOperations).toContain('opn_03_water_stop');
 expect(current.projectionDefinition).toMatchObject({version:'story.progress-projection.v1.20261022',narrativeVersion:'act1.2026-08-27.001',initialOperationId:'opn_01_last_departure'});
 expect(current.projectionDefinition.storyProgressOrder[2]).toBe('opn_02_first_signal');
 expect(current.projectionDefinition.metadata).toEqual(current.narrative);
 f.sql.run("INSERT INTO account_mutations VALUES('progress-later','a',?,'STORY_PROGRESS',NULL,4)",[sequence+2]);
 f.sql.run("INSERT INTO progression_ledger VALUES('progress-later-ledger','a','progress-later','STORY_PROGRESS','opn_03_water_stop',?,4)",[delta('opn_03_water_stop')]);
 f.sql.run("UPDATE account_story_progress SET highest_cleared_operation_id='opn_03_water_stop',highest_story_progress_id='opn_03_water_stop',updated_at=4 WHERE account_id='a'");
 const newer=await(await get()).json();expect(newer.sourceOwner.mutationSequence).toBe(sequence+2);expect(newer.storyProgress.order).toBe(3);
 const before=f.sql.export();expect(await(await f.claim()).json()).toEqual(original);expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
 const legacy=await(await app.request('/v1/story-progress',{headers:f.headers},f.env)).json();expect(legacy.sourceOwner).toBeUndefined();expect(legacy.projectionDefinition).toBeUndefined();
 expect((await get('unknown')).status).toBe(409);
 f.sql.run("UPDATE account_story_progress SET narrative_version='future-version' WHERE account_id='a'");
 const unavailable=await get();expect(unavailable.status).toBe(503);expect(await unavailable.json()).toMatchObject({errorCode:'story_progress_projection_unavailable'});
 f.sql.run("UPDATE account_story_progress SET narrative_version='act1.2026-08-27.001',cleared_operations_json='{}' WHERE account_id='a'");
 const invalid=await get();expect(invalid.status).toBe(503);expect(await invalid.json()).toMatchObject({errorCode:'story_progress_source_unavailable'});
 f.sql.run("UPDATE account_story_progress SET cleared_operations_json='[\"unknown-operation\"]' WHERE account_id='a'");
 expect((await get()).status).toBe(503);
 f.sql.run("UPDATE account_story_progress SET cleared_operations_json='[]' WHERE account_id='a'");
 f.sql.run("INSERT INTO account_unlocks VALUES('a','operation:unknown-operation',4)");
 expect((await get()).status).toBe(503);
 f.sql.run("DELETE FROM account_unlocks WHERE unlock_id='operation:unknown-operation'");
 f.sql.run("UPDATE account_story_progress SET highest_cleared_operation_id='unknown-operation' WHERE account_id='a'");
 f.sql.run("UPDATE progression_ledger SET delta_json=json_set(delta_json,'$.highestClearedOperationId','unknown-operation') WHERE id='progress-later-ledger'");
 expect((await get()).status).toBe(503);
});
it('does not label unversioned nonempty progress as verified empty',async()=>{
 const f=await fixture();f.sql.run("INSERT INTO account_unlocks VALUES('a','operation:opn_03_water_stop',3)");
 const before=f.sql.export(),response=await app.request('/v1/story-progress',{headers:{...f.headers,'X-Story-Progress-Version':'story.progress-observation.v1.20261022'}},f.env);
 expect(response.status).toBe(503);expect(await response.json()).toMatchObject({errorCode:'story_progress_source_unavailable'});
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
});
it('orders a current wallet observation against a changed claim wallet without using an unversioned balance',async()=>{
 const f=await fixture();
 f.sql.run("INSERT INTO crystal_policies(id,policy_json) VALUES('fixture-crystal-policy',?)",[JSON.stringify({currencyId:'fixture_crystal',ticketExchange:null,purchase:null})]);
 f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'fixture-crystal-policy')");
 f.sql.run('UPDATE run_reward_previews SET reward_plan_json=?',[JSON.stringify({rewardLines:[{kind:'CRYSTAL',id:'fixture_crystal',quantity:7,source:'RUN_CLEAR',launchSourceKey:'LOCAL_crystal_source'}]})]);
 const read=async()=>{const r=await app.request('/v1/wallet/crystals',{headers:f.headers},f.env);expect(r.status).toBe(200);return r.json();};
 const before=await read(),response=await f.claim();expect(response.status).toBe(200);const receipt=await response.json(),after=await read();
 expect(before.stateVersion).toMatchObject({version:'crystal-wallet.snapshot.v1.20261022',accountId:'a',coverage:['BALANCES']});
 expect(before.stateVersion.mutationSequence).toBeLessThan(receipt.owners.wallet.sourceMutation.mutationSequence);
 expect(after.stateVersion.mutationSequence).toBe(receipt.owners.wallet.sourceMutation.mutationSequence);
 expect(after).toMatchObject({freeBalance:7,paidBalance:0,totalBalance:7,policyVersion:'fixture-crystal-policy',spendOrder:['FREE','PAID'],ticketExchange:null,purchaseConfigured:false});
 f.sql.run("INSERT INTO account_mutations VALUES('later-wallet','a',?,'FIXTURE',NULL,9)",[after.stateVersion.mutationSequence+1]);
 f.sql.run("UPDATE crystal_lots SET remaining=2 WHERE account_id='a'");
 const newer=await read();expect(newer.totalBalance).toBe(2);expect(newer.stateVersion.mutationSequence).toBeGreaterThan(after.stateVersion.mutationSequence);
 const bytes=f.sql.export();expect(await(await f.claim()).json()).toEqual(receipt);expect(Buffer.from(f.sql.export()).equals(Buffer.from(bytes))).toBe(true);
});
it('preserves a committed claim through expired authentication and recovers it under a new same-account session',async()=>{
 const f=await fixture(),first=await f.claim();expect(first.status).toBe(200);const original=await first.json(),now=Date.now();
 const expired=await createAccessToken({secret:f.env.AUTH_SIGNING_KEY_CURRENT,accountId:'a',sessionId:'s',nowMs:now-120000,ttlMs:60000});
 const post=(token:string)=>app.request(`/v1/runs/${f.runId}/reward-preview/${f.previewId}/claim`,{method:'POST',headers:{...f.headers,Authorization:'Bearer '+token,'Idempotency-Key':'complete-key','X-Run-Claim-Version':version},body:JSON.stringify({runId:f.runId,rewardPreviewId:f.previewId})},f.env);
 let before=f.sql.export();const denied=await post(expired);expect(denied.status).toBe(401);expect(await denied.json()).toMatchObject({errorCode:'access_token_invalid'});
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
 f.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE id='s'");
 f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('relogin-a','a','new-h','new-f',0,'ACTIVE',?,?)",[now,now+900000]);
 const renewed=await createAccessToken({secret:f.env.AUTH_SIGNING_KEY_CURRENT,accountId:'a',sessionId:'relogin-a',nowMs:now,ttlMs:900000});
 before=f.sql.export();const recovered=await post(renewed);expect(recovered.status).toBe(200);expect(await recovered.json()).toEqual(original);
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
 expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);expect(f.scalar('SELECT count(*) FROM run_complete_claim_receipts')).toBe(1);
});
it.each(['truncated','hash-mismatch','unsupported-version'] as const)('preserves a %s server receipt and refuses to reconstruct or regrant it',async fault=>{
 const f=await fixture(),initial=await f.claim();expect(initial.status).toBe(200);
 const receipt=await initial.json(),stored=String(f.scalar('SELECT payload_json FROM run_complete_claim_receipts'));
 // Explicit disk-corruption simulation bypasses write guards only in this test DB.
 f.sql.run('DROP TRIGGER run_complete_claim_no_update');
 f.sql.run('PRAGMA ignore_check_constraints=ON');
 const damaged=fault==='truncated'?stored.slice(0,-5):fault==='unsupported-version'?stored.replace(version,'run.reward-claim.future'):stored;
 f.sql.run('UPDATE run_complete_claim_receipts SET payload_json=?,payload_hash=?',[damaged,fault==='hash-mismatch'?'invalid-hash':await sha256Hex(damaged)]);
 f.sql.run('PRAGMA ignore_check_constraints=OFF');
 const before=f.sql.export(),recovered=await f.claim();
 expect(recovered.status).toBe(503);expect(await recovered.json()).toMatchObject({errorCode:'run_claim_complete_storage_invalid'});
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
 expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
 expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(receipt.owners.account.currencies[0].balance);
 expect(f.scalar('SELECT payload_json FROM run_complete_claim_receipts')).toBe(damaged);
});
it('restores the exact claim from a disk database in fresh processes without network, regrant or latest-state rollback',async()=>{
 const f=await fixture(),response=await f.claim();expect(response.status).toBe(200);
 const original=await response.json();
 const directory=mkdtempSync('D:/Project/LASTRAIL/LocalTemp/claim-restart-'),path=join(directory,'server.sqlite');
 const probe=()=>{
  const result=spawnSync(process.execPath,['--import','tsx','tests/helpers/claim-receipt-restart-probe.ts',path,f.runId,f.previewId],{encoding:'utf8',timeout:20000,env:{...process.env,TSX_TSCONFIG_PATH:'tsconfig.base.json'}});
  expect(result.error).toBeUndefined();expect(result.status,result.stderr).toBe(0);
  return JSON.parse(result.stdout);
 };
 try{
  writeFileSync(path,f.sql.export());const afterSave=probe();
  expect(afterSave).toMatchObject({receipt:original,foreign:null,conflict:true,httpCalls:0,unchanged:true,credits:110,grants:1,receipts:1});
  expect(afterSave.receipt).toEqual(original);
  expect(afterSave.pid).not.toBe(process.pid);
  f.sql.run("UPDATE account_currencies SET balance=999 WHERE account_id='a' AND currency='CREDITS'");
  f.sql.run("INSERT INTO account_mutations VALUES('restart-later','a',?,'FIXTURE',NULL,9)",[original.owners.account.mutationSequence+1]);
  writeFileSync(path,f.sql.export());const afterLaterMutation=probe();
  expect(afterLaterMutation).toMatchObject({receipt:original,foreign:null,conflict:true,httpCalls:0,unchanged:true,credits:999,grants:1,receipts:1});
  expect(afterLaterMutation.receipt).toEqual(original);
  expect(afterLaterMutation.pid).not.toBe(process.pid);expect(afterLaterMutation.pid).not.toBe(afterSave.pid);
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-receipt-restart-server-process-20261022.json',JSON.stringify({scope:'DISK_SQLJS_FRESH_NODE_PROCESSES_NOT_UNITY_OWNER_APPLY_OR_DEVICE_CRYPTO',parentPid:process.pid,afterSave,afterLaterMutation},null,2));
 }finally{rmSync(path,{force:true});rmdirSync(directory);}
},60000);
async function fixture(withGrowth=false){
 const f=await setup(true);closes.push(()=>f.sql.close());
 if(withGrowth){const config=JSON.stringify(economy);
  f.sql.run('INSERT INTO economy_versions VALUES(?,?,?,0)',[economy.version,await sha256Hex(config),config]);
  f.sql.run('INSERT INTO economy_version_bindings VALUES(1,?)',[economy.version]);
 }
 const p=await(await f.preview()).json(),r=await(await f.start(p.operationPreviewId)).json(),runId=r.runId as string,previewId='complete-preview';
 f.sql.run("UPDATE runs SET status='COMPLETED' WHERE id=?",[runId]);
 f.sql.run("INSERT INTO run_final_results VALUES(?,'VICTORY',15,'{}',1)",[runId]);
 f.sql.run("INSERT INTO account_currencies VALUES('a','CREDITS',100,0)");f.sql.run("INSERT INTO account_items VALUES('a','item-fixture',2,0)");
 const plan=JSON.stringify({rewardLines:[{kind:'CURRENCY',id:'cur_credits',quantity:10,source:'RUN_CLEAR'},{kind:'ITEM',id:'item-fixture',quantity:3,source:'RUN_CLEAR'}]});
 f.sql.run("INSERT INTO run_reward_previews(id,account_id,run_id,balance_config_version,status,reward_plan_json,expires_at,created_at) VALUES(?,'a',?,1,'ACTIVE',?,?,1)",[previewId,runId,plan,Date.now()+900000]);
 const claim=(key='complete-key',v:string|null=version)=>app.request('/v1/runs/'+runId+'/reward-preview/'+previewId+'/claim',{method:'POST',headers:{...f.headers,'Idempotency-Key':key,...(v?{'X-Run-Claim-Version':v}:{})},body:JSON.stringify({runId,rewardPreviewId:previewId})},f.env);
 return {...f,runId,previewId,claim};
}
it('supplies lobby balances over real HTTP and preserves unclaimed rights and original receipts without follow-up GETs',async()=>{
 const f=await fixture(),claimPath=`/v1/runs/${f.runId}/reward-preview/${f.previewId}/claim`,lifecyclePath=`/v1/runs/${f.runId}/lifecycle`;
 const trace:{method:string;path:string;status:number;bytes:number}[]=[];
 const server=createServer(async(req,res)=>{
  if(!(req.method==='POST'&&req.url===claimPath)&&!(req.method==='GET'&&req.url===lifecyclePath)){res.writeHead(404);res.end();return;}
  try{
   const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));
   const headers=new Headers();for(const [key,value]of Object.entries(req.headers))if(typeof value==='string')headers.set(key,value);
   const response=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:req.method,headers,...(req.method==='POST'?{body:Buffer.concat(chunks)}:{})}),f.env);
   const bytes=Buffer.from(await response.arrayBuffer());trace.push({method:req.method!,path:req.url!,status:response.status,bytes:bytes.length});res.writeHead(response.status,Object.fromEntries(response.headers));res.end(bytes);
  }catch{res.writeHead(500);res.end();}
 });
 try{
  await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',()=>{server.off('error',no);ok();});});
  const address=server.address();if(!address||typeof address==='string')throw Error('bind');const base='http://127.0.0.1:'+address.port;
  const before=f.sql.export(),unclaimed=await fetch(base+lifecyclePath,{headers:f.headers});expect(unclaimed.status).toBe(200);
  expect(await unclaimed.json()).toMatchObject({finish:{status:'RECORDED'},reward:{status:'CLAIMABLE'},nextAction:'REWARD_REVIEW',newAttemptAuthorized:false,grantsOnRead:false});
  expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  const post=()=>fetch(base+claimPath,{method:'POST',headers:{...f.headers,'Idempotency-Key':'lobby-original','X-Run-Claim-Version':version},body:JSON.stringify({runId:f.runId,rewardPreviewId:f.previewId})});
  const response=await post();expect(response.status).toBe(200);
  const original=z.object({owners:z.object({account:z.object({mutationSequence:z.number()}).passthrough()}).passthrough()}).passthrough().parse(await response.json());
  expect(original).toMatchObject({version,status:'CLAIMED',nextAction:'RETURN_TO_LOBBY',owners:{account:{currencies:[{currency:'CREDITS',balance:110}],items:[{itemId:'item-fixture',quantity:5}]}}});
  f.sql.run("UPDATE account_currencies SET balance=999 WHERE currency='CREDITS'");
  f.sql.run("INSERT INTO account_mutations VALUES('later-lobby','a',?,'FIXTURE',NULL,2)",[original.owners.account.mutationSequence+1]);
  for(let i=0;i<2;i++){const replay=await post();expect(replay.status).toBe(200);expect(await replay.json()).toEqual(original);}
  expect(trace.slice(1).every(row=>row.method==='POST')).toBe(true);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(999);
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-lobby-return-http-20261021.json',JSON.stringify({classification:'ACTUAL_AUTHENTICATED_TCP_CLAIM_WITH_SYNTHETIC_TERMINAL_RUN_MEMORY_ONLY',trace,diagnosticPreClaimGets:1,normalPostClaimGets:0,originalReplayPreserved:true,grants:1,laterSyntheticBalancePreserved:999,actualFinish:'NOT_RUN_SQL_FINAL_RESULT_FIXTURE',sceneReturn:'NOT_RUN',player:'NOT_RUN',memory:'NOT_MEASURED',processRestart:'NOT_RUN'},null,2));
 }finally{server.closeAllConnections();if(server.listening)await new Promise<void>(ok=>server.close(()=>ok()));}
});
it('keeps terminal outcome separate from reward expiry and recovers an expired-window committed claim over HTTP',async()=>{
 const f=await fixture(),path=`/v1/runs/${f.runId}/lifecycle`,claimPath=`/v1/runs/${f.runId}/reward-preview/${f.previewId}/claim`;
 const trace:Array<{method:string;status:number;bytes:number}>=[];
 const server=createServer(async(req,res)=>{
  if(!(req.method==='GET'&&req.url===path)&&!(req.method==='POST'&&req.url===claimPath)){res.writeHead(404);res.end();return;}
  try{
   const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));
   const headers=new Headers();for(const [key,value]of Object.entries(req.headers))if(typeof value==='string')headers.set(key,value);
   const response=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:req.method,headers,...(req.method==='POST'?{body:Buffer.concat(chunks)}:{})}),f.env);
   const bytes=Buffer.from(await response.arrayBuffer());trace.push({method:req.method!,status:response.status,bytes:bytes.length});
   res.writeHead(response.status,Object.fromEntries(response.headers));res.end(bytes);
  }catch{res.writeHead(500);res.end();}
 });
 try{
  await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',()=>{server.off('error',no);ok();});});
  const address=server.address();if(!address||typeof address==='string')throw Error('bind');const base='http://127.0.0.1:'+address.port;
  // Only the reward window changes. The confirmed victory remains authoritative.
  f.sql.run('UPDATE run_reward_previews SET expires_at=2');
  const before=f.sql.export();
  for(let i=0;i<2;i++){
   const response=await fetch(base+path,{headers:f.headers});expect(response.status).toBe(200);
   const state=await response.json();
   expect(state).toMatchObject({runStatus:'COMPLETED',finish:{status:'RECORDED',outcome:'VICTORY'},reward:{status:'EXPIRED',expiresAt:2},
    disposition:'EXPIRED_UNCLAIMED',augmentRecovery:{state:'TERMINAL'},newAttemptAuthorized:false,grantsOnRead:false});
   expect(state).not.toHaveProperty('operationExpiresAt');
  }
  expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
  // Fixture reset permits a commit; then the client loses its response and the window expires.
  f.sql.run('UPDATE run_reward_previews SET expires_at=?',[Date.now()+900000]);
  const post=()=>fetch(base+claimPath,{method:'POST',headers:{...f.headers,'Idempotency-Key':'terminal-original','X-Run-Claim-Version':version},body:JSON.stringify({runId:f.runId,rewardPreviewId:f.previewId})});
  const committed=await post();expect(committed.status).toBe(200);const original=await committed.json();
  f.sql.run('UPDATE run_reward_previews SET expires_at=2');const afterExpiry=f.sql.export();
  for(let i=0;i<2;i++){const recovered=await post();expect(recovered.status).toBe(200);expect(await recovered.json()).toEqual(original);}
  expect(Buffer.from(f.sql.export()).equals(Buffer.from(afterExpiry))).toBe(true);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
  expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(110);
  expect(trace.slice(2).every(row=>row.method==='POST')).toBe(true);
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-restore-terminal-http-20261021.json',JSON.stringify({
   classification:'ACTUAL_HTTP_SYNTHETIC_TERMINAL_AND_REWARD_WINDOW_SQL_MEMORY_ONLY',trace,readWrites:0,grants:1,
   terminalOutcomePreserved:true,originalClaimRecoveredAfterRewardExpiry:true,normalPostClaimGets:0,
   operationExpirySource:'NOT_SUPPLIED',player:'NOT_RUN',processRestart:'NOT_RUN',memory:'NOT_MEASURED'
  },null,2));
 }finally{server.closeAllConnections();if(server.listening)await new Promise<void>(ok=>server.close(()=>ok()));}
});
it('authorizes a separate post-claim spend against real balances and preserves later spend through old claim replay over HTTP',async()=>{
 const f=await fixture(true),cost=economy.growth.trainCoreStepsCreditsPartsCores[0]!;
 f.sql.run("UPDATE account_currencies SET balance=? WHERE currency='CREDITS'",[cost[0]!-10]);
 f.sql.run("INSERT INTO account_currencies VALUES('a','MACHINE_PARTS',10000,0),('a','SPECIAL_CORE',100,0)");
 const claimPath=`/v1/runs/${f.runId}/reward-preview/${f.previewId}/claim`,growthPath='/v1/trains/train_a/level-up';
 const trace:Array<{path:string;status:number;bytes:number}>=[];
 const server=createServer(async(req,res)=>{
  if(req.method!=='POST'||![claimPath,growthPath].includes(req.url??'')){res.writeHead(404);res.end();return;}
  try{const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));const headers=new Headers();
   for(const [key,value]of Object.entries(req.headers))if(typeof value==='string')headers.set(key,value);
   const response=await app.fetch(new Request('http://127.0.0.1'+req.url,{method:'POST',headers,body:Buffer.concat(chunks)}),f.env);
   const bytes=Buffer.from(await response.arrayBuffer());trace.push({path:req.url!,status:response.status,bytes:bytes.length});res.writeHead(response.status,Object.fromEntries(response.headers));res.end(bytes);
  }catch{res.writeHead(500);res.end();}
 });
 try{
  await new Promise<void>((ok,no)=>{server.once('error',no);server.listen(0,'127.0.0.1',()=>{server.off('error',no);ok();});});
  const address=server.address();if(!address||typeof address==='string')throw Error('bind');const base='http://127.0.0.1:'+address.port;
  const spend=(key:string)=>fetch(base+growthPath,{method:'POST',headers:{...f.headers,'Idempotency-Key':key},body:JSON.stringify({economyVersion:economy.version,responseVersion:'growth.command.v2.20261021'})});
  const claim=()=>fetch(base+claimPath,{method:'POST',headers:{...f.headers,'Idempotency-Key':'post-claim-original','X-Run-Claim-Version':version},body:JSON.stringify({runId:f.runId,rewardPreviewId:f.previewId})});
  const early=await spend('early-explicit');expect(early.status).toBe(409);expect(await early.json()).toMatchObject({errorCode:'insufficient_balance'});
  const paid=await claim();expect(paid.status).toBe(200);const original=await paid.json();
  expect(original.owners.account.currencies).toContainEqual({currency:'CREDITS',balance:cost[0]});
  const used=await spend('new-explicit-spend');expect(used.status).toBe(200);const spent=await used.json();
  expect(spent.owners.account.mutationSequence).toBeGreaterThan(original.owners.account.mutationSequence);
  expect(spent).toMatchObject({owners:{account:{train:{level:2,revision:2}}}});
  expect(spent.owners.account.currencies).toContainEqual({currency:'CREDITS',balance:0});
  const before=f.sql.export();expect(await(await claim()).json()).toEqual(original);
  expect(await(await spend('new-explicit-spend')).json()).toEqual(spent);
  expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  const insufficient=await spend('another-explicit-spend');expect(insufficient.status).toBe(409);
  expect(await insufficient.json()).toMatchObject({errorCode:'insufficient_balance'});
  expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(0);
  expect(f.scalar('SELECT level FROM account_train')).toBe(2);expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
  writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/data-post-claim-command-http-20261021.json',JSON.stringify({classification:'ACTUAL_AUTH_HTTP_SYNTHETIC_TERMINAL_MEMORY_ONLY',trace,
   claimGrants:1,successfulGrowthSpends:1,followupGets:0,finalCredits:0,finalTrainLevel:2,oldReceiptPreserved:true,
   clientStorageGate:'NOT_RUN',queuedInput:'NOT_RUN',player:'NOT_RUN',memory:'NOT_MEASURED',processRestart:'NOT_RUN'},null,2));
 }finally{server.closeAllConnections();if(server.listening)await new Promise<void>(ok=>server.close(()=>ok()));}
});
it('keeps the awarded stack ID and historical amount distinct from a later current quantity and other item IDs',async()=>{
 const f=await fixture();f.sql.run("INSERT INTO account_items VALUES('a','item-fixture-copy',9,0)");
 const response=await f.claim();expect(response.status).toBe(200);const receipt=await response.json();
 expect(receipt.rewardLines).toContainEqual(expect.objectContaining({kind:'ITEM',id:'item-fixture',quantity:3}));
 expect(receipt.owners.account.items).toEqual([{itemId:'item-fixture',quantity:5},{itemId:'item-fixture-copy',quantity:9}]);
 // Synthetic later inventory mutation, not a supported item-use command or UI action.
 f.sql.run("UPDATE account_items SET quantity=1 WHERE item_id='item-fixture'");
 f.sql.run("INSERT INTO account_mutations VALUES('later-stack','a',?,'FIXTURE',NULL,2)",[receipt.owners.account.mutationSequence+1]);
 const before=f.sql.export();
 const currentResponse=await app.request('/v1/account/state',{headers:f.headers},f.env);expect(currentResponse.status).toBe(200);
 const current=await currentResponse.json();
 expect(current.items).toContainEqual({itemId:'item-fixture',quantity:1});
 expect(current.items).toContainEqual({itemId:'item-fixture-copy',quantity:9});
 expect(current.mutationSequence).toBeGreaterThan(receipt.owners.account.mutationSequence);
 expect(await(await f.claim()).json()).toEqual(receipt);
 expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
 expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
});
it('returns a durable complete response with actual balances and owner versions, without client follow-up reads',async()=>{
 const f=await fixture(),before=Number(f.scalar("SELECT max(sequence) FROM account_mutations WHERE account_id='a'"));
 const wire=vi.spyOn(app,'request'),response=await f.claim();expect(response.status).toBe(200);const body=await response.json();
 expect(wire.mock.calls).toHaveLength(1);expect(wire.mock.calls[0]![1]!.method).toBe('POST');
 if(process.env.LASTRAIL_EXPORT_CLAIM_V2_EVIDENCE==='1')writeFileSync('D:/Project/LASTRAIL/QA/CommanderS1-20260922/run-complete-claim-example-20261019.json',JSON.stringify({scope:'IN_PROCESS_HONO_SQLJS_FIXTURE_NOT_EXTERNAL_NETWORK_OR_UNITY',honoRequests:wire.mock.calls.length,followupGets:0,payloadBytes:Buffer.byteLength(JSON.stringify(body)),response:body},null,2));
 expect(body).toMatchObject({version,status:'CLAIMED',runId:f.runId,rewardPreviewId:f.previewId,nextAction:'RETURN_TO_LOBBY',owners:{account:{kind:'SNAPSHOT',beforeMutationSequence:before,mutationSequence:before+1,currencies:[{currency:'CREDITS',balance:110}],items:[{itemId:'item-fixture',quantity:5}]},wallet:{kind:'UNCHANGED'},progress:{kind:'SNAPSHOT'}}});
 expect(body.rewardClaimId).toBe(body.transactionReceipt.rewardTransactionId);
 expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(110);
 expect(f.scalar('SELECT count(*) FROM run_complete_claim_receipts')).toBe(1);
});
it('replays original complete receipt after later mutations without taking a new snapshot or regranting',async()=>{
 const f=await fixture(),first=await(await f.claim()).json();
 f.sql.run("UPDATE account_currencies SET balance=999 WHERE currency='CREDITS'");
 f.sql.run("INSERT INTO account_mutations VALUES('later','a',?,'FIXTURE',NULL,2)",[first.owners.account.mutationSequence+1]);
 const replay=await f.claim();expect(replay.status).toBe(200);expect(await replay.json()).toEqual(first);
 expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(999);expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
});
it('rolls back grant, versions and receipt together if complete-response persistence fails',async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER fail_complete BEFORE INSERT ON run_complete_claim_receipts BEGIN SELECT RAISE(ABORT,'fixture-response-fault'); END");
 expect((await f.claim()).status).toBe(500);
 expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(100);
 expect(f.scalar('SELECT status FROM run_reward_previews')).toBe('ACTIVE');expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
});
it('keeps legacy claim/replay unchanged and refuses to invent v2 completion for a legacy receipt',async()=>{
 const f=await fixture(),old=await(await f.claim('old-key',null)).json();expect(old.status).toBe('CLAIMED');expect(old.version).toBeUndefined();
 const result=await f.claim('old-key');expect(result.status).toBe(409);expect(await result.json()).toMatchObject({errorCode:'run_claim_complete_unavailable'});
 expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
});
it('rejects unsupported versions and incomplete final state before granting',async()=>{
 const f=await fixture();expect((await f.claim('x','unknown')).status).toBe(409);
 f.sql.run("UPDATE runs SET status='ACTIVE'");expect((await f.claim()).status).toBe(409);
 expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
});


it('supplies untouched balances and fuel too, so accepting a newer financial snapshot does not need a matching old patch basis',async()=>{
 const f=await fixture();f.sql.run("INSERT INTO account_currencies VALUES('a','MACHINE_PARTS',55,0)");
 const body=await(await f.claim()).json();
 expect(body.owners.account.kind).toBe('SNAPSHOT');
 expect(body.owners.account.currencies).toContainEqual({currency:'MACHINE_PARTS',balance:55});
 expect(body.owners.account.fuel.current).toBe(Number(f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'")));
 expect(body.owners.account.coverage).toEqual(['CURRENCIES','ITEMS','FUEL']);
});


it('replays the winner if the same key commits after initial receipt lookup but before preview lookup',async()=>{
 const f=await fixture(),prepare=f.adapter.prepare.bind(f.adapter);let fired=false;let winner:unknown;
 f.adapter.prepare=(sql:string)=>{const statement=prepare(sql);
  if(!fired&&sql.includes('SELECT id, account_id, run_id, status, balance_config_version, reward_plan_json')){
   const original=statement.first.bind(statement);statement.first=async<T>()=>{if(!fired){fired=true;winner=await(await f.claim()).json();}return original<T>();};
  }return statement;
 };
 const response=await f.claim();expect(response.status).toBe(200);expect(await response.json()).toEqual(winner);expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
});
it('replays a same-key winner when a prepared loser reaches its first source fence',async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);let fired=false;let winner:unknown;
 f.env.DB.batch=async statements=>{if(!fired&&statements.length>3){fired=true;winner=await(await f.claim()).json();}return batch(statements);};
 const response=await f.claim();expect(response.status).toBe(200);expect(await response.json()).toEqual(winner);expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
});
it('orders progress by its actual STORY_PROGRESS mutation, independent from the later financial claim sequence',async()=>{
 const f=await fixture(),progressSequence=Number(f.scalar("SELECT max(sequence) FROM account_mutations WHERE account_id='a'"))+1;
 f.sql.run("INSERT INTO account_mutations VALUES('story-update','a',?,'STORY_PROGRESS',NULL,3)",[progressSequence]);
 f.sql.run("INSERT INTO account_story_progress(account_id,act_id,highest_cleared_operation_id,highest_story_progress_id,cleared_operations_json,story_flags_json,narrative_version,last_progress_run_id,updated_at) VALUES('a','act_01','opn_01_last_departure','sp1','[\"opn_01_last_departure\"]','[]','narrative-fixture',?,3)",[f.runId]);
 f.sql.run("INSERT INTO progression_ledger(id,account_id,mutation_id,progression_type,target_id,delta_json,created_at) VALUES('story-ledger','a','story-update','STORY_PROGRESS','sp1',?,3)",[JSON.stringify({runId:f.runId,actId:'act_01',highestStoryProgressId:'sp1',highestClearedOperationId:'opn_01_last_departure'})]);
 const first=await(await f.claim()).json();expect(first).toMatchObject({errorCode:null});expect(first.owners.progress).toMatchObject({mutationSequence:progressSequence,beforeMutationSequence:progressSequence,versionSource:'STORY_PROGRESS_MUTATION',state:{story:{highestStoryProgressId:'sp1'}}});
 expect(first.owners.account.mutationSequence).toBe(progressSequence+1);
 f.sql.run("INSERT INTO account_mutations VALUES('later-story','a',?,'STORY_PROGRESS',NULL,4)",[progressSequence+2]);f.sql.run("UPDATE account_story_progress SET highest_story_progress_id='sp2',updated_at=4");
 const replay=await(await f.claim()).json();expect(replay).toEqual(first);expect(replay.owners.progress.mutationSequence).toBeLessThan(progressSequence+2);
});



it('supplies the actual changed crystal wallet without changing the independent progress owner version',async()=>{
 const f=await fixture();f.sql.run("INSERT INTO crystal_policies(id,policy_json) VALUES('fixture-crystal-policy',?)",[JSON.stringify({currencyId:'fixture_crystal',ticketExchange:null,purchase:null})]);f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'fixture-crystal-policy')");
 f.sql.run('UPDATE run_reward_previews SET reward_plan_json=?',[JSON.stringify({rewardLines:[{kind:'CRYSTAL',id:'fixture_crystal',quantity:7,source:'RUN_CLEAR',launchSourceKey:'LOCAL_crystal_source'}]})]);
 const response=await f.claim();expect(response.status).toBe(200);const body=await response.json();
 expect(body.owners.wallet).toMatchObject({kind:'SNAPSHOT',coverage:['BALANCES'],currencyId:'fixture_crystal',policyVersion:'fixture-crystal-policy',freeBalance:7,paidBalance:0,totalBalance:7});
 expect(body.owners.progress).toMatchObject({versionSource:'VERIFIED_EMPTY',mutationSequence:0});
 expect(f.scalar('SELECT sum(remaining) FROM crystal_lots')).toBe(7);
});
it('rejects another authenticated account without exposing or granting the original complete receipt',async()=>{
 const f=await fixture(),now=Date.now();f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('b-session','b','h-b','f-b',0,'ACTIVE',?,?)",[now,now+900000]);
 const token=await createAccessToken({secret:f.env.AUTH_SIGNING_KEY_CURRENT,accountId:'b',sessionId:'b-session',nowMs:now,ttlMs:900000});
 const response=await app.request('/v1/runs/'+f.runId+'/reward-preview/'+f.previewId+'/claim',{method:'POST',headers:{...f.headers,Authorization:'Bearer '+token,'X-Run-Claim-Version':version,'Idempotency-Key':'b-key'},body:JSON.stringify({runId:f.runId,rewardPreviewId:f.previewId})},f.env);
 expect(response.status).toBe(404);expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
});
it('recovers a lost batch acknowledgement with the same original response and cannot mutate that stored response',async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);let lost=true;
 f.env.DB.batch=async statements=>{const result=await batch(statements);if(lost){lost=false;throw Error('fixture-lost-ack');}return result;};
 expect((await f.claim()).status).toBe(200);const original=JSON.parse(String(f.scalar('SELECT payload_json FROM run_complete_claim_receipts')));
 expect(await(await f.claim()).json()).toEqual(original);expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
 expect(()=>f.sql.run("UPDATE run_complete_claim_receipts SET payload_json='{}'")).toThrow('immutable_complete_claim');
});

it('replays the complete winner when it commits between ACTIVE preview and the later idempotency lookup',async()=>{
 const f=await fixture(),prepare=f.adapter.prepare.bind(f.adapter);let fired=false;let winner:unknown;
 f.adapter.prepare=(sql:string)=>{const statement=prepare(sql);
  if(!fired&&sql.includes('LEFT JOIN reward_transactions reward ON reward.id = idem.result_ref')){
   const first=statement.first.bind(statement);statement.first=async<T>()=>{if(!fired){fired=true;winner=await(await f.claim()).json();}return first<T>();};
  }return statement;
 };
 const response=await f.claim();expect(response.status).toBe(200);expect(await response.json()).toEqual(winner);
});

it.each([['FAILED','FAILURE'],['ABANDONED','ABANDONED']] as const)('supplies complete receipts for valid terminal %s results too',async(status,outcome)=>{
 const f=await fixture();f.sql.run('UPDATE runs SET status=?',[status]);f.sql.run('UPDATE run_final_results SET outcome=?',[outcome]);
 const response=await f.claim();expect(response.status).toBe(200);expect(await response.json()).toMatchObject({version,status:'CLAIMED',nextAction:'RETURN_TO_LOBBY'});
});

