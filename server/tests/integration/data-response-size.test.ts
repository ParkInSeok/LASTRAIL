import { expect, it } from 'vitest';
import { readFileSync, writeFileSync } from 'node:fs';
import { z } from 'zod';
import { startDataLifecycleLoopback } from '../../scripts/local/data-lifecycle-loopback-http';

const root = 'D:/Project/LASTRAIL/';
const call = z.object({ method:z.string(), path:z.string(), status:z.number(), responseBytes:z.number() });
const player = z.object({ calls:z.array(call), localRetryHttpCalls:z.number(), forcedGC:z.boolean() });
const memory = z.object({ managedUsedBytes:z.number(), managedReservedBytes:z.number(), gcCollections:z.array(z.number()), elapsedSeconds:z.number() });
const bytes = (value:unknown) => Buffer.byteLength(JSON.stringify(value), 'utf8');
function historical(folder:string) {
 const base = root+'QA/OperatorInformation-20261019/'+folder+'/evidence/';
 const result = player.parse(JSON.parse(readFileSync(base+'result.json','utf8')));
 return { source:base, calls:result.calls, totalResponseBytes:result.calls.reduce((n,c)=>n+c.responseBytes,0),
  saveCalls:result.calls.filter(c=>c.path==='/v1/formation/presets/p/save'),
  localRetryHttpCalls:result.localRetryHttpCalls, forcedGC:result.forcedGC,
  afterSave:memory.parse(JSON.parse(readFileSync(base+'07-confirmed-save.json','utf8'))),
  afterRelease:memory.parse(JSON.parse(readFileSync(base+'09-references-released-normal-gc.json','utf8'))) };
}
it('measures the real formation response and preserves the original Player comparison without interpreting wire bytes as heap',async()=>{
 const baseline=historical('DataSaveFailureBaselinePlayer267'), current=historical('DataSaveFailureCurrentPlayer268');
 expect(baseline.saveCalls.map(c=>c.responseBytes)).toEqual([834,834,834]);
 expect(current.saveCalls.map(c=>c.responseBytes)).toEqual([834]);
 const host=await startDataLifecycleLoopback({lifetimeMs:60000});
 try {
  const b=host.bootstrap, command=b.formationCommand;
  const send=()=>fetch(b.baseUrl+command.path,{method:'POST',headers:{Authorization:'Bearer '+b.accessToken,'Idempotency-Key':'response-size-original','Content-Type':'application/json'},body:JSON.stringify(command.body)});
  const response=await send();expect(response.status).toBe(200);const text=await response.text();
  const parsed=z.object({receiptId:z.string(),contractVersion:z.string(),owners:z.object({account:z.object({beforeMutationSequence:z.number(),mutationSequence:z.number(),preset:z.object({operators:z.array(z.object({slot:z.number(),operatorId:z.string()})),definitionJson:z.string()}).passthrough()}).passthrough()})}).passthrough().parse(JSON.parse(text));
  const account=parsed.owners.account,preset=account.preset;
  const definition=z.object({schemaVersion:z.literal(1),operators:z.array(z.object({slot:z.number(),operatorId:z.string()}))}).parse(JSON.parse(preset.definitionJson));
  expect(definition.operators).toEqual(preset.operators);
  expect(account.mutationSequence).toBe(account.beforeMutationSequence+1);
  const replays=[];for(let i=0;i<2;i++){const r=await send();expect(r.status).toBe(200);const replay=await r.text();expect(replay).toBe(text);replays.push(Buffer.byteLength(replay,'utf8'));}
  writeFileSync(root+'QA/CommanderS1-20260922/data-memory-cause-response-20261021.json',JSON.stringify({
   classification:'HISTORICAL_ACTUAL_PLAYER_RECORDS_PLUS_NEW_SYNTHETIC_AUTHENTICATED_TCP_FIELD_MEASUREMENT',
   baseline,current,
   freshResponse:{utf8Bytes:Buffer.byteLength(text,'utf8'),jsStringCodeUnits:text.length,replayBytes:replays,
    topLevelSerializedValueBytes:Object.fromEntries(Object.entries(parsed).map(([key,value])=>[key,bytes(value)])),
    accountPatchSerializedBytes:bytes(account),presetSerializedBytes:bytes(preset),
    structuredOperatorsSerializedBytes:bytes(preset.operators),definitionJsonSerializedStringBytes:bytes(preset.definitionJson),
    definitionContainsEquivalentOperators:true,slotCount:preset.operators.length,
    note:'Nested byte figures overlap; values exclude property names/delimiters. String code units are not measured managed allocations.'},
   requirements:{receiptId:'original transaction identity and replay',contractVersion:'consumer schema compatibility',
    beforeMutationSequence:'patch continuity fence',mutationSequence:'reject stale owner rollback',
    definitionJson:'current Unity snapshot/draft persisted definition input',operators:'current Unity request/receipt selection validation'},
   conclusions:{responseGrowthExplainsObservedManagedIncrease:false,
    meaning:'Observed save response bytes are unchanged; this does not exclude parsing, retention, GC timing or instrumentation effects.',
    serverProductionChange:false,retainedReferenceChain:'NOT_MEASURED',heapAllocation:'NOT_MEASURED',textureCpuGpu:'NOT_MEASURED',
    processPrivate:'UNAVAILABLE_IN_PLAYER_RECORDS',forcedGc:'NOT_RUN',playerRerun:'NOT_RUN'}
  },null,2));
 }finally{await host.close();}
});
