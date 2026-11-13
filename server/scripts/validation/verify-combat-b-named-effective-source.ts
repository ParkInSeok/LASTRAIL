import {writeFile} from 'node:fs/promises';
import {strict as assert} from 'node:assert';
import {Buffer} from 'node:buffer';
import {Hono} from 'hono';
import app,{resolveAuthContext} from '../../apps/game-api/src/index';
import {namedFixture,namedRequest} from '../../tests/helpers/combat-b-named-operator-fixture';
import {bNamedEffectiveSourceVersion,readPinnedBNamedEffectiveSource,type BNamedEffectiveSource} from '../../apps/game-api/src/combat-b-named-effective-source';
import {registerBNamedEffectiveSourceRoutes} from '../../apps/game-api/src/combat-b-named-effective-source-routes';
import {startWorldBossLoopback} from '../local/world-boss-loopback';
const qa='D:/Project/LASTRAIL/QA/CommanderS1-20260922/',f=await namedFixture(true),local=new Hono<{Bindings:Env}>();
registerBNamedEffectiveSourceRoutes(local,resolveAuthContext,{forAuthenticatedContext:async(_c,accountId)=>{assert.equal(accountId,'a');return f.refitOwner!;}});
const h=await startWorldBossLoopback(async req=>await app.fetch(req,f.env)),bound=await startWorldBossLoopback(async req=>await local.fetch(req,f.env));
const trace:{case:string;status:number;errorCode:string|null}[]=[],headers={...f.headers,'X-B-Named-Effective-Version':bNamedEffectiveSourceVersion};
async function post(label:string,status:number,extra:Record<string,string>={},body:unknown=namedRequest,origin=h.origin,path='/v1/combat/b-named-effective-source'){
 const r=await fetch(origin+path,{method:'POST',headers:{...headers,...extra},body:JSON.stringify(body)}),b=await r.json() as Record<string,unknown>;assert.equal(r.status,status,label);trace.push({case:label,status:r.status,errorCode:typeof b.errorCode==='string'?b.errorCode:null});return b;
}
let publicPacket:BNamedEffectiveSource|undefined,localPacket:BNamedEffectiveSource|undefined;
try{
 const before=f.sql.export();publicPacket=await post('signed additive original applicability read',200) as unknown as BNamedEffectiveSource;localPacket=await post('server composed original persisted LOCAL refit provider',200,{},namedRequest,bound.origin) as unknown as BNamedEffectiveSource;
 assert.equal(publicPacket.refitProviderStatus,'OPERATING_PROVIDER_UNAVAILABLE');assert.equal(localPacket.refitProviderStatus,'ORIGINAL_PERSISTED_PROVIDER_BOUND');assert.ok(Buffer.from(f.sql.export()).equals(Buffer.from(before)));
 await post('same If-Match',200,{'If-Match':publicPacket.packetHash});await post('wrong version',409,{'X-B-Named-Effective-Version':'old'});await post('wrong hash',409,{'If-Match':'0'.repeat(64)});await post('claimed effective maxima',400,{}, {...namedRequest,effectiveStats:{maxHp:1000000}});await post('source effect hash stale',409,{}, {...namedRequest,expectedEffectSourceHash:'0'.repeat(64)});await post('query account claim',400,{},namedRequest,h.origin,'/v1/combat/b-named-effective-source?accountId=b');await post('missing original bearer',401,{Authorization:''});
 f.sql.run("INSERT INTO player_profiles(account_id,commander_xp,created_at,updated_at) VALUES('a',0,0,0)");await post('changed commander authority old If-Match',409,{'If-Match':publicPacket.packetHash});assert.deepEqual(await readPinnedBNamedEffectiveSource(localPacket,localPacket,localPacket.packetHash),localPacket);
 f.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE id='s'");await post('revoked session original provider',401,{},namedRequest,bound.origin);
}finally{await bound.close();await h.close();f.sql.close();}
assert.ok(publicPacket&&localPacket);
for(const [kind,packet]of [['public',publicPacket],['local-refit',localPacket]] as const)await writeFile(qa+'combat-b-named-effective-source-'+kind+'-sample-20261004.json',JSON.stringify({evidence:'SYNTHETIC_SQL_ACCOUNT_APPROVED_LV1_BASES_NO_BOOST_NOT_OPERATING_START',transport:'actual signed loopback TCP; local provider bound only in isolated verification app',packet},null,2)+'\n');
await writeFile(qa+'combat-b-named-effective-source-tcp-20261004.json',JSON.stringify({trace,publicHash:publicPacket.packetHash,localRefitHash:localPacket.packetHash,noReadWrites:true,originalSnapshotRestoredAfterEffectSourceChange:true,listenerClosed:true,databaseClosed:true,runtimeSupported:false,canStart:false},null,2)+'\n');process.stdout.write(JSON.stringify({tcpCases:trace.length,publicHash:publicPacket.packetHash,localRefitHash:localPacket.packetHash})+'\n');