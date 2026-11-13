import {writeFile} from 'node:fs/promises';
import {strict as assert} from 'node:assert';
import app from '../../apps/game-api/src/index';
import {namedFixture,namedRequest} from '../../tests/helpers/combat-b-named-operator-fixture';
import {readPinnedBNamedOperatorSource,type BNamedOperatorSource} from '../../apps/game-api/src/combat-b-named-operator-source';
import {startWorldBossLoopback} from '../local/world-boss-loopback';
const qa='D:/Project/LASTRAIL/QA/CommanderS1-20260922/',f=await namedFixture(true);
const h=await startWorldBossLoopback(async req=>await app.fetch(req,f.env)),trace:{case:string;status:number;errorCode:string|null}[]=[];
async function post(label:string,status:number,extra:Record<string,string>={},path='/v1/combat/b-named-operator-source'){
 const response=await fetch(h.origin+path,{method:'POST',headers:{...f.headers,...extra},body:JSON.stringify(namedRequest)}),body=await response.json() as Record<string,unknown>;assert.equal(response.status,status,label);trace.push({case:label,status:response.status,errorCode:typeof body.errorCode==='string'?body.errorCode:null});return body;
}
try{
 const before=f.sql.export(),publicPacket=await post('signed owned approved base read',200) as unknown as BNamedOperatorSource;
 assert.equal(publicPacket.refit,null);const localPacket=await f.capture();assert.ok(localPacket.refit);assert.deepEqual(f.sql.export(),before);
 await post('same independent If-Match',200,{'If-Match':publicPacket.packetHash});await post('wrong hash',409,{'If-Match':'0'.repeat(64)});await post('old version',409,{'X-B-Named-Operator-Version':'old'});await post('account query injection',400,{},'/v1/combat/b-named-operator-source?accountId=b');await post('missing bearer',401,{Authorization:''});
 f.sql.run("UPDATE account_operators SET promotion=1,revision=2 WHERE account_id='a' AND operator_id='op_kyle'");await post('changed original growth old hash',409,{'If-Match':publicPacket.packetHash});assert.deepEqual(await readPinnedBNamedOperatorSource(localPacket,localPacket,localPacket.packetHash),localPacket);
 f.sql.run("UPDATE auth_sessions SET status='REVOKED' WHERE id='s'");await post('revoked original session',401);
 await writeFile(qa+'combat-b-named-operator-source-public-sample-20261004.json',JSON.stringify({evidence:'SYNTHETIC_SQL_OWNERSHIP_APPROVED_LV1_BASES_NO_BOOST_NOT_OPERATING_START',transport:'signed loopback TCP original game app',packet:publicPacket},null,2)+'\n');
 await writeFile(qa+'combat-b-named-operator-source-local-refit-sample-20261004.json',JSON.stringify({evidence:'SYNTHETIC_SQL_OWNERSHIP_ORIGINAL_BREFITSTORE_LOCAL_SOURCE_NOT_OPERATING_START',packet:localPacket},null,2)+'\n');
 await writeFile(qa+'combat-b-named-operator-source-tcp-20261004.json',JSON.stringify({transport:'127.0.0.1 ephemeral signed TCP',trace,publicHash:publicPacket.packetHash,localRefitHash:localPacket.packetHash,noReadWrites:true,restoredOriginalAfterGrowthChange:true,fuel:f.scalar("SELECT fuel FROM fuel_state WHERE account_id='a'"),runs:f.scalar('SELECT count(*) FROM runs'),runtimeSupported:false,canStart:false,closed:true},null,2)+'\n');
 process.stdout.write(JSON.stringify({tcpCases:trace.length,publicHash:publicPacket.packetHash,localRefitHash:localPacket.packetHash})+'\n');
}finally{await h.close();f.sql.close();}