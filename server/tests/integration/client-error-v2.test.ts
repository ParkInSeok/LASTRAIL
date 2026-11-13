import {afterEach,expect,it} from 'vitest';
import initSqlJs from 'sql.js';
import app from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';
import {createAccessToken} from '../../apps/game-api/src/auth';
const dbs:initSqlJs.Database[]=[];afterEach(()=>{for(const db of dbs.splice(0))db.close();});
const version='client-errors.v2.20261019';
const event={kind:'UNITY_ERROR',occurredAt:'2026-10-19T00:00:00.000Z',lastOccurredAt:'2026-10-19T00:00:01.000Z',occurrenceCount:2,
 appVersion:'1.2.3',appVersionCode:'123',scope:'AUTHENTICATED_CURRENT_SESSION',errorCode:'REQUIRED_STATE_SAVE_FAILED',phase:'STATE_SAVE',platform:'WINDOWS',operationKeyHash:null};
async function fixture(){const f=await setup(true);dbs.push(f.sql);return {...f,send:(events:unknown[]=[event],key='safe-v2-original-key')=>f.post('/v1/client-errors',{version,events},key),
 receipt:(key='safe-v2-original-key')=>app.request('/v1/client-errors/receipt',{headers:{Authorization:f.headers.Authorization,'Idempotency-Key':key}},f.env)};}
it('stores fixed v2 diagnostics and preserves the original receipt after log retention',async()=>{
 const f=await fixture(),r=await f.send();expect(r.status).toBe(200);const received=await r.json();expect(received).toMatchObject({accepted:true,version,acceptedEventCount:1});
 expect(f.scalar('SELECT error_code FROM ops_error_logs')).toBe('frontend_required_state_save_failed');
 const context=JSON.parse(String(f.scalar('SELECT sanitized_context_json FROM ops_error_logs')));expect(context).toMatchObject({phase:'STATE_SAVE',platform:'WINDOWS',occurrenceCount:2});
 f.sql.run('DELETE FROM ops_error_logs');
 expect(await(await f.receipt()).json()).toMatchObject({accepted:true,version,acceptedEventCount:1,ingestId:received.ingestId});
 expect(await(await f.send()).json()).toMatchObject({accepted:true,version,ingestStatus:'DUPLICATE_REPLAY',ingestId:received.ingestId});
 expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(0);
});
it.each([{stack:'private/raw/path'}, {errorCode:'ARBITRARY_TEXT'}, {phase:'RUNTIME'}, {occurrenceCount:61},
 {lastOccurredAt:'2026-10-18T00:00:00.000Z'}, {operationKeyHash:'raw-secret'}, {accountId:'other'}, {environment:'PRD'}])('rejects unsafe or inconsistent v2 event %j',async change=>{
 const f=await fixture();expect((await f.send([{...event,...change}])).status).toBe(400);expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(0);
});
it('preserves the v1 event contract and rejects silently adding v2 fields',async()=>{
 const f=await fixture(),legacy={kind:event.kind,occurredAt:event.occurredAt,occurrenceCount:1,appVersion:'1.0',appVersionCode:'100',scope:event.scope};
 const r=await f.post('/v1/client-errors',{events:[legacy]},'legacy-compatible-key');expect(r.status).toBe(200);expect((await r.json()).version).toBeUndefined();
 expect((await f.post('/v1/client-errors',{events:[event]},'legacy-invalid-key')).status).toBe(400);
 expect(f.scalar('SELECT count(*) FROM client_error_receipts')).toBe(0);
});
it('enforces the existing batch and byte limits before storing diagnostics',async()=>{
 const f=await fixture();expect((await f.send(Array(26).fill(event))).status).toBe(400);
 const r=await app.request('/v1/client-errors',{method:'POST',headers:{...f.headers,'Idempotency-Key':'oversize-original-key'},body:' '.repeat(18001)+JSON.stringify({version,events:[event]})},f.env);
 expect(r.status).toBe(400);expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(0);
});
it('recovers lost acknowledgments with the same key and rejects a changed transaction correlation',async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);
 f.env.DB.batch=async statements=>{await batch(statements);f.env.DB.batch=batch;throw new Error('lost diagnostic acknowledgment');};
 const r=await f.send();expect(r.status).toBe(200);expect(await r.json()).toMatchObject({ingestStatus:'DUPLICATE_REPLAY',version});
 expect((await f.send([{...event,operationKeyHash:'sha256:'+'a'.repeat(64)}])).status).toBe(409);
 expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(1);expect(f.scalar('SELECT count(*) FROM client_error_receipts')).toBe(1);
});
it('enforces the last available rate slot atomically for concurrent v2 batches',async()=>{
 const f=await fixture();for(let n=0;n<11;n++)f.sql.run("INSERT INTO idempotency_records(id,account_id,idempotency_key,operation,result_ref,created_at) VALUES(?,'a',?,'client_error_ingest','fixture-prior',?)",['prior-'+n,'prior-key-'+n,Date.now()]);
 const responses=await Promise.all([f.send([event],'concurrent-rate-key-1'),f.send([event],'concurrent-rate-key-2')]);
 expect(responses.map(r=>r.status).sort()).toEqual([200,429]);expect(responses.find(r=>r.status===429)!.headers.get('Retry-After')).toBe('600');
 expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(1);
});
it('records deployment environment and never accepts anonymous diagnostics',async()=>{
 const f=await fixture(),request={method:'POST',headers:{...f.headers,'Idempotency-Key':'prd-synthetic-key-1'},body:JSON.stringify({version,events:[event]})};
 const r=await app.request('/v1/client-errors',request,{...f.env,ENVIRONMENT:'PRD'});expect(r.status).toBe(200);expect(f.scalar('SELECT environment FROM ops_error_logs')).toBe('PRD');
 const unauthorized=await app.request('/v1/client-errors',{...request,headers:{'Content-Type':'application/json','Idempotency-Key':'anonymous-original-key'}},f.env);expect(unauthorized.status).toBe(401);
});
it('rolls back all log and receipt writes when authority is revoked at commit',async()=>{
 const f=await fixture();f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE accounts SET status='BANNED'");};
 expect((await f.send()).status).toBe(403);expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(0);expect(f.scalar('SELECT count(*) FROM client_error_receipts')).toBe(0);
});
it('keeps an unavailable write recoverable with the original key',async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER fail_error_receipt BEFORE INSERT ON client_error_receipts BEGIN SELECT RAISE(ABORT,'fixture-storage-down'); END");
 expect((await f.send()).status).toBe(503);expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(0);
 f.sql.run('DROP TRIGGER fail_error_receipt');expect((await f.send()).status).toBe(200);expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(1);
});
it('accepts the full bounded batch without persisting an arbitrary request header',async()=>{
 const f=await fixture();const r=await app.request('/v1/client-errors',{method:'POST',headers:{...f.headers,'Idempotency-Key':'full-batch-original-key','X-Request-Id':'private/raw/path'},body:JSON.stringify({version,events:Array(25).fill({...event,occurrenceCount:60})})},f.env);
 expect(r.status).toBe(200);expect(await r.json()).toMatchObject({acceptedEventCount:25});expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(25);
 expect(f.scalar('SELECT request_id FROM ops_error_logs LIMIT 1')).toBeNull();
});
it('does not expose another account original diagnostic receipt',async()=>{
 const f=await fixture();expect((await f.send()).status).toBe(200);const now=Date.now();
 f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('bs','b','hash-b','family-b',0,'ACTIVE',?,?)",[now,now+60000]);
 const token=await createAccessToken({secret:f.env.AUTH_SIGNING_KEY_CURRENT,accountId:'b',sessionId:'bs',nowMs:now,ttlMs:60000});
 const r=await app.request('/v1/client-errors/receipt',{headers:{Authorization:'Bearer '+token,'Idempotency-Key':'safe-v2-original-key'}},f.env);
 expect(r.status).toBe(404);expect((await r.json()).ingestId).toBeNull();
});
it('rejects pre-epoch diagnostic time as a permanent input error without writes',async()=>{
 const f=await fixture();const r=await f.send([{...event,occurredAt:'1969-12-31T23:59:59.000Z',lastOccurredAt:'1969-12-31T23:59:59.000Z'}]);
 expect(r.status).toBe(400);expect(f.scalar('SELECT count(*) FROM ops_error_logs')).toBe(0);
});
