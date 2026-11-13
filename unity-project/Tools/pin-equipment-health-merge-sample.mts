// Pin preserved producer QA without rerunning or overwriting its SQL sample.
import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {strict as assert} from 'node:assert';
import {canonicalJson,sha256Hex} from '../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/packages/common/src/index';
const samplePath='D:/Project/LASTRAIL/QA/CommanderS1-20260922/equipment-health-merge-sql-sample-20261005.json';
const bytes=await readFile(samplePath),fileSha=createHash('sha256').update(bytes).digest('hex');
assert.equal(fileSha,'22585c1d01140d3f3a64edb50404964ab84001bc615b6e6e8c9b9e1a996df4f6');
const sample=JSON.parse(bytes.toString('utf8')),requestHash=await sha256Hex(canonicalJson(sample.command)),responseHash=await sha256Hex(canonicalJson(sample.committedReceipt));
for(const t of sample.committedReceipt.targets){const event='eqmergehp:'+await sha256Hex(canonicalJson({runId:sample.currentStatSnapshot.runId,requestHash,wearerKind:t.wearerKind,wearerId:t.wearerId,targetId:t.targetId}));assert.ok(sample.committedReceipt.health.some((h:{eventId:string})=>h.eventId===event));}
await writeFile('D:/Project/LASTRAIL/QA/CombatBMigration-20261004/RunEquipmentMergeSource-20261005/original-pins.json',JSON.stringify({samplePath,fileSha,requestHash,responseHash,accountId:sample.currentStatSnapshot.accountId,runId:sample.currentStatSnapshot.runId,originalContextHash:sample.currentStatSnapshot.originalContextHash,currentStatHash:sample.currentStatSnapshot.packetHash,evidence:'preserved actual producer SQL sample; canonical hashes from original server common functions; not a new receipt wire field or HTTP call'},null,2)+'\n');
