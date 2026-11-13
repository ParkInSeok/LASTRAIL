import {readFileSync} from 'node:fs';
import {canonicalJson,sha256Hex} from '@lastrail/common';
export const originalV1=JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BNativeTwoWave-20261005/two-wave-http-result.json','utf8'));
export const originalJournal=JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/CombatBMigration-20261004/BOriginalEventProducer-20261006/original-event-sample.json','utf8')).journal;
// Explicit transport compatibility composition from preserved capture + original journal.
// This is not a newly executed unchanged full Unity v2 export.
export async function v2Report(){const n=structuredClone(originalV1.runtimeSources.nodeSession);n.runtime.version='unity.combat-b.travel-native-local.v2.20261006';n.runtime.originalEvents=structuredClone(originalJournal);return {version:'combat.b-result-report.v2.20261006',requestId:'v2-original-journal',reportOriginMapRevision:2,checkpointHash:'sha256:'+await sha256Hex(canonicalJson(n.runtime)),nodeSession:n};}
export async function rehashV2(b:Awaited<ReturnType<typeof v2Report>>,rebind=false){if(rebind)for(const e of b.nodeSession.runtime.originalEvents.records){e.binding.checkpointHash='sha256:'+await sha256Hex(canonicalJson(e.checkpoint));e.eventRef='sha256:'+await sha256Hex(canonicalJson(e.binding));}b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(b.nodeSession.runtime));}
