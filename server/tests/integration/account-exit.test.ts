import {expect,it} from 'vitest';
import app from '../../apps/game-api/src/index';
import {setup} from '../helpers/departure-fixture';
import {createAccessToken,hashRefreshToken} from '../../apps/game-api/src/auth';
const logout='/v1/auth/logout';
it('rejects oversized logout input at the request boundary',async()=>{
 const f=await setup();try{expect((await app.request(logout,{method:'POST',headers:f.headers,body:'x'.repeat(1025)},f.env)).status).toBe(400);}finally{f.sql.close();}
});
it('ends only the original family, blocks its refresh, and recovers the original command only as its owner',async()=>{
 const f=await setup();try{
  f.sql.run("INSERT INTO auth_identities VALUES('google-a','a','GOOGLE','synthetic-google-a',0)");
  f.sql.run("UPDATE auth_sessions SET refresh_token_hash=? WHERE id='s'",[await hashRefreshToken('original-refresh')]);
  const command={name:'A saved',expectedRevision:1,operators:Array.from({length:5},(_,slot)=>({slot,operatorId:'op'+slot})),responseVersion:'formation.command.v2.20261021'};
  const save=(headers:Record<string,string>)=>app.request('/v1/formation/presets/p/save',{method:'POST',headers:{...headers,'Idempotency-Key':'owner-key'},body:JSON.stringify(command)},f.env);
  const original=await(await save(f.headers)).json();
  const ended=await app.request(logout,{method:'POST',headers:f.headers},f.env);expect(ended.status).toBe(200);const receipt=await ended.json();
  expect(receipt).toMatchObject({version:'data.account-exit.v1.20261021',accepted:true,accountId:'a',sessionId:'s',status:'SESSION_FAMILY_ENDED'});
  expect(await(await app.request(logout,{method:'POST',headers:f.headers},f.env)).json()).toEqual(receipt);
  expect((await save(f.headers)).status).toBe(401);
  expect((await app.request('/v1/auth/refresh',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({refreshToken:'original-refresh'})},f.env)).status).toBe(401);
  const now=Date.now();
  for(const account of ['a','b'])f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES(?,?,?,?,0,'ACTIVE',?,?)",['new-'+account,account,'refresh-'+account,'family-'+account,now,now+60000]);
  const headers=async(account:string)=>({'Content-Type':'application/json',Authorization:'Bearer '+await createAccessToken({secret:f.env.AUTH_SIGNING_KEY_CURRENT,accountId:account,sessionId:'new-'+account,nowMs:now,ttlMs:60000})});
  expect((await save(await headers('b'))).status).not.toBe(200);
  expect(await(await save(await headers('a'))).json()).toEqual(original);
  expect(await(await app.request(logout,{method:'POST',headers:f.headers},f.env)).json()).toEqual(receipt);
  expect((await app.request('/v1/account/state',{headers:await headers('a')},f.env)).status).toBe(200);
  expect(f.scalar("SELECT COUNT(*) FROM formation_receipts WHERE account_id='a' AND idempotency_key='owner-key'")).toBe(1);
 }finally{f.sql.close();}
});
it('distinguishes same-account rotation and prevents a stale refresh from resurrecting an ended family',async()=>{
 const f=await setup();try{
  f.sql.run("INSERT INTO auth_identities VALUES('google-a','a','GOOGLE','synthetic-google-a',0)");
  f.sql.run("UPDATE auth_sessions SET refresh_token_hash=? WHERE id='s'",[await hashRefreshToken('rotate-me')]);
  const request=()=>app.request('/v1/auth/refresh',{method:'POST',headers:{'Content-Type':'application/json','Idempotency-Key':'account-exit-refresh-0001'},body:JSON.stringify({refreshToken:'rotate-me'})},f.env);
  const refresh=await request();expect(refresh.status).toBe(200);expect(await refresh.json()).toMatchObject({account:{id:'a'}});
  const ended=await app.request(logout,{method:'POST',headers:f.headers},f.env);expect(ended.status).toBe(200);
  expect((await request()).status).toBe(401);expect(f.scalar("SELECT COUNT(*) FROM auth_sessions WHERE family_id='f' AND status='ACTIVE'")).toBe(0);
 }finally{f.sql.close();}
 const race=await setup();try{
  race.sql.run("UPDATE auth_sessions SET refresh_token_hash=? WHERE id='s'",[await hashRefreshToken('race-refresh')]);
  race.adapter.beforeBatch=()=>{delete race.adapter.beforeBatch;race.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE family_id='f'");};
  const reply=await app.request('/v1/auth/refresh',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({refreshToken:'race-refresh'})},race.env);
  expect(reply.status).toBe(401);expect(race.scalar("SELECT COUNT(*) FROM auth_sessions WHERE family_id='f' AND status='ACTIVE'")).toBe(0);
 }finally{race.sql.close();}
});
it('does not report success when durable revocation fails',async()=>{
 const f=await setup();try{
  f.sql.run("INSERT INTO auth_identities VALUES('google-a','a','GOOGLE','synthetic-google-a',0)");
  f.adapter.beforeBatch=()=>{throw Error('synthetic persistence failure');};
  expect((await app.request(logout,{method:'POST',headers:f.headers},f.env)).status).toBe(500);
  expect(f.scalar("SELECT status FROM auth_sessions WHERE id='s'")).toBe('ACTIVE');
 }finally{f.sql.close();}
});
it('preserves unlinked guest recovery and rejects forged scope/body without mutation',async()=>{
 const f=await setup();try{
  f.sql.run("INSERT INTO auth_identities VALUES('guest-a','a','GUEST','synthetic-guest-a',0)");
  const denied=await app.request(logout,{method:'POST',headers:f.headers},f.env);expect(denied.status).toBe(409);expect(await denied.json()).toMatchObject({errorCode:'guest_recovery_required'});
  expect((await app.request(logout,{method:'POST',headers:f.headers,body:JSON.stringify({accountId:'b'})},f.env)).status).toBe(400);
  expect((await app.request(logout,{method:'POST'},f.env)).status).toBe(401);
  expect(f.scalar("SELECT status FROM auth_sessions WHERE id='s'")).toBe('ACTIVE');
 }finally{f.sql.close();}
});
