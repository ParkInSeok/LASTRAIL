import {readFileSync} from 'node:fs';
import {createAccessToken} from '../../apps/game-api/src/auth';
import {worldHealthFixture} from './world-boss-health-fixture';
import {createWorldBossAuthorityLocalHttp,authorityHttpVersion} from '../../apps/game-api/src/world-boss-authority-local-http';
import {railVersion} from '../../apps/game-api/src/world-boss-rail-contract';
import {journalVersion} from '../../apps/game-api/src/world-boss-rail-journal';
import {railHealthVersion} from '../../apps/game-api/src/world-boss-rail-health';
export const authorityPath='/_local/world-boss/attempts/trial/authority';
export async function authorityHttpFixture(){
 const f=await worldHealthFixture();f.sql.run(readFileSync('apps/game-api/local/world-boss-authority-http.sql','utf8'));
 for(const a of ['a','b','outsider'])f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES(?,?,?, ?,0,'ACTIVE',0,1000000)",['s_'+a,a,'h_'+a,'f_'+a]);
 const secret='local-authority-test-key-not-operational';const env={DB:f.db,ENVIRONMENT:'LOCAL' as const,ALLOWED_ADMIN_ORIGINS:'http://localhost:5173' as const,AUTH_SIGNING_KEY_CURRENT:secret,LOCAL_WORLD_RAIL_PROFILE:'LOCAL_TEST_PROFILE' as const};const app=createWorldBossAuthorityLocalHttp();
 const headers=async(account='a')=>({Authorization:'Bearer '+await createAccessToken({secret,accountId:account,sessionId:'s_'+account,nowMs:10000,ttlMs:1000000}),'Content-Type':'application/json','X-World-Authority-Version':authorityHttpVersion,'X-World-Rail-Version':railVersion,'X-World-Journal-Version':journalVersion,'X-World-Health-Version':railHealthVersion,'X-World-Content-Version':'LOCAL_GEO'});
 const get=async(account='a',extra:Record<string,string>={},path=authorityPath)=>app.request('http://127.0.0.1'+path,{headers:{...await headers(account),...extra}},env);
 const post=async(body:unknown=f.command,account='a',extra:Record<string,string>={},path=authorityPath+'/commands')=>app.request('http://127.0.0.1'+path,{method:'POST',headers:{...await headers(account),'Idempotency-Key':(body as {commandId?:string})?.commandId??'route','X-World-Journal-Revision':'1',...extra},body:typeof body==='string'?body:JSON.stringify(body)},env);
 const counts=()=>['world_boss_rail_geometry_journal','world_boss_authority_http_commands','world_boss_rail_health_receipts'].map(t=>f.sql.exec('SELECT count(*) FROM '+t)[0]!.values[0]![0]);
 return {...f,app,env,headers,get,post,counts,secret};
}