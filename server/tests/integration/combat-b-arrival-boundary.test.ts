import {expect,it,vi} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {setupBOperationFixture} from '../helpers/combat-b-operation-fixture';
import {setupBDeparturePinFixture} from '../helpers/combat-b-departure-pin-fixture';
import {prepareBDepartureCombatSnapshot} from '../../apps/game-api/src/combat-b-departure-combat-pin';
import {prepareBArrivalBoundarySource,readPinnedBArrivalBoundary,bArrivalReportV2Version,bArrivalBoundaryReportSchema,verifyBArrivalBoundaryReport} from '../../apps/game-api/src/combat-b-arrival-source';
import {BArrivalReportStore,BArrivalConsistencyOwner} from '../../apps/game-api/src/combat-b-arrival-report';
import {OperationMapStore} from '../../apps/game-api/src/operation-map-store';
import {OperationMapPublisher} from '../../apps/game-api/src/operation-map-publisher';
import {createBOperationBinding} from '../../apps/game-api/src/combat-b-operation-binding';
import {captureOperationMap} from '../../apps/game-api/src/operation-map-plan';
import {BOriginalAcceptedBroker} from '../../apps/game-api/src/combat-b-original-accepted-broker';
import {BRouteMapVerifier} from '../../apps/game-api/src/combat-b-route-adapter';
import {bRouteVersion} from '../../apps/game-api/src/combat-b-route-adapter';
import app from '../../apps/game-api/src/index';
import {BOriginalArrivalStore,type BOriginalArrivalVerifier} from '../../apps/game-api/src/combat-b-arrival-original';
import {BTravelCommandStore,bTravelCommandVersion,compactBTravelEvents} from '../../apps/game-api/src/combat-b-travel-command';
import {applyBTravelCommand} from '../../apps/game-api/src/combat-b-arrival-source';
import {destinationResponseVersion} from '../../apps/game-api/src/operation-map-destination-response';

it.each([null,destinationResponseVersion])('returns the original travel baseline through destination HTTP projection %s',async version=>{
 const f=await fixture(version);try{
  const receipt=(await f.map.lookup('a',f.runId,'choose-boundary'))!;
  expect(f.destinationReply).toMatchObject({travelInitial:receipt.travelInitial});
  const response=await app.request('/v1/runs/'+f.runId+'/operation-map/destination',{method:'POST',headers:{...f.headers,'Idempotency-Key':'choose-boundary',...(version?{'X-Map-Destination-Version':version}:{})},body:JSON.stringify({expectedRevision:1,edgeId:'entry:n1-1'})},f.env);
  expect(response.status).toBe(200);expect(await response.json()).toEqual(f.destinationReply);
 }finally{f.close();}
});

it.each([299,300,301])('preserves a pause accepted at %i ms and validates any already reached boundary',async elapsed=>{
 const f=await fixture();try{
  const origin=await f.env.DB.prepare('SELECT confirmed_at_ms FROM operation_map_destination_times WHERE run_id=?').bind(f.runId).first<{confirmed_at_ms:number}>();
  const store=new BTravelCommandStore(f.env.DB,()=>origin!.confirmed_at_ms+elapsed);
  const pause={version:bTravelCommandVersion,requestId:'boundary-pause',destinationEventId:'choose-boundary',nodeId:'n1-1',mapRevision:2,expectedRevision:0,kind:'PAUSE',value:true};
  const response=await store.submit('a',f.runId,pause);
  if(elapsed<300){expect(response.state.paused).toBe(true);expect(response).not.toHaveProperty('arrival');}
  else{expect(response).toMatchObject({pauseIntent:true,arrival:{arrivalAccepted:true}});expect(response.state.distanceMm).toBe(300);}
  expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');
  expect(await store.submit('a',f.runId,pause)).toEqual(response);
 }finally{f.close();}
});

it('accepts only one of pause and arrival at the same travel revision',async()=>{
 const f=await fixture();try{
  const origin=await f.env.DB.prepare('SELECT confirmed_at_ms FROM operation_map_destination_times WHERE run_id=?').bind(f.runId).first<{confirmed_at_ms:number}>();
  const store=new BTravelCommandStore(f.env.DB,()=>origin!.confirmed_at_ms+300);
  const base={version:bTravelCommandVersion,destinationEventId:'choose-boundary',nodeId:'n1-1',mapRevision:2,expectedRevision:0};
  const results=await Promise.allSettled([store.submit('a',f.runId,{...base,requestId:'race-pause',kind:'PAUSE',value:true}),store.submit('a',f.runId,{...base,requestId:'race-arrive',kind:'ARRIVE'})]);
  expect(results.filter(r=>r.status==='fulfilled')).toHaveLength(1);
  expect(f.scalar('SELECT COUNT(*) FROM b_travel_commands')).toBe(1);
  expect(f.scalar('SELECT COUNT(*) FROM b_arrival_originals')).toBe(1);
  expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');
 }finally{f.close();}
});

it('runs three accepted input requests through product auth and recovers a lost resume response without new time',async()=>{
 const f=await fixture();const realClock=Date.now;
 try{
  const origin=await f.env.DB.prepare('SELECT confirmed_at_ms FROM operation_map_destination_times WHERE run_id=?').bind(f.runId).first<{confirmed_at_ms:number}>();
  let now=origin!.confirmed_at_ms;vi.spyOn(Date,'now').mockImplementation(()=>now);
  let inputRequests=0,retries=0;
  const send=async(requestId:string,expectedRevision:number,kind:string,extra={},retry=false)=>{
   if(kind!=='ARRIVE'){if(retry)retries++;else inputRequests++;}
   const body={version:bTravelCommandVersion,requestId,expectedRevision,kind,...extra,destinationEventId:'choose-boundary',nodeId:'n1-1',mapRevision:2};
   const response=await app.request('/v1/runs/'+f.runId+'/b-arrival-reports',{method:'POST',headers:{...f.headers,'X-B-Arrival-Version':bTravelCommandVersion,'Idempotency-Key':requestId},body:JSON.stringify(body)},f.env);
   const result=await response.json();expect(response.status,JSON.stringify(result)).toBe(200);return result;
  };
  now+=100;await send('http-speed',0,'TRAVEL_THROTTLE',{bps:11000});
  now+=50;await send('http-pause',1,'PAUSE',{value:true});
  now+=10000;const lost=await send('http-resume',2,'PAUSE',{value:false});
  now+=50;expect(await send('http-resume',2,'PAUSE',{value:false},true)).toEqual(lost);
  expect({inputRequests,retries}).toEqual({inputRequests:3,retries:1});
  now+=300;const arrival=await send('http-arrival',3,'ARRIVE');expect(arrival.arrival.arrivalAccepted).toBe(true);
  expect(f.scalar('SELECT COUNT(*) FROM b_travel_commands')).toBe(4);
 }finally{vi.restoreAllMocks();Date.now=realClock;f.close();}
});

it('preserves recovery resume and arrival capacity at the bounded replay limit',async()=>{
 const f=await fixture();try{
  const origin=await f.env.DB.prepare('SELECT confirmed_at_ms FROM operation_map_destination_times WHERE run_id=?').bind(f.runId).first<{confirmed_at_ms:number}>();
  let now=origin!.confirmed_at_ms+1;
  const store=new BTravelCommandStore(f.env.DB,()=>now);
  const c=(requestId:string,expectedRevision:number,kind:string,extra={})=>({version:bTravelCommandVersion,requestId,expectedRevision,kind,...extra,destinationEventId:'choose-boundary',nodeId:'n1-1',mapRevision:2});
  const first=await store.submit('a',f.runId,c('initial',0,'TRAVEL_THROTTLE',{bps:11000}));
  const initial={tick:0,distanceMm:0,remainderUnits:0,debtTicks:0,paused:false,throttleBps:10000};
  let state={...initial};const events:Parameters<typeof compactBTravelEvents>[1][number][]=[];
  const append=(command:typeof events[number]['command'])=>{state=applyBTravelCommand(f.source,state,command);events.push({command,state:{...state}});};
  // Synthetic equivalent replay projection stresses the storage boundary; not a Player input history.
  for(let i=0;i<498;i++){append({kind:'ADVANCE',eventId:'a'+i,elapsed:20,maximumSteps:10000});append({kind:'TRAVEL_THROTTLE',eventId:'s'+i,bps:i%2?11000:12000});}
  append({kind:'ADVANCE',eventId:'last',elapsed:40,maximumSteps:10000});
  expect(state).toEqual(first.state);
  f.sql.run('UPDATE b_travel_heads SET journal_json=?',[JSON.stringify(events)]);
  now+=1;await expect(store.submit('a',f.runId,c('capacity',1,'TRAVEL_THROTTLE',{bps:12000}))).rejects.toThrow('history_capacity');
  await store.submit('a',f.runId,c('recover-limit',1,'RECOVER'));
  now+=10000;await store.submit('a',f.runId,c('recover-again',2,'RECOVER'));
  await store.submit('a',f.runId,c('resume-limit',3,'PAUSE',{value:false}));
  now+=300;expect((await store.submit('a',f.runId,c('arrive-limit',4,'ARRIVE'))).arrival).toMatchObject({arrivalAccepted:true});
 }finally{f.close();}
});

it('accepts server-timed travel commands, discards recovery gaps and supplies actual arrival authority',async()=>{
 const f=await fixture();try{
  const row=await f.env.DB.prepare('SELECT confirmed_at_ms FROM operation_map_destination_times WHERE run_id=?').bind(f.runId).first<{confirmed_at_ms:number}>();
  let now=row!.confirmed_at_ms;
  expect((await f.map.lookup('a',f.runId,'choose-boundary'))!.travelInitial).toMatchObject({version:bTravelCommandVersion,revision:0,acceptedAtMs:now,state:{distanceMm:0,throttleBps:10000,paused:false}});
  const store=new BTravelCommandStore(f.env.DB,()=>now);
  const command=(requestId:string,expectedRevision:number,kind:string,extra={})=>({version:bTravelCommandVersion,requestId,expectedRevision,kind,...extra,destinationEventId:'choose-boundary',nodeId:'n1-1',mapRevision:2});
  now+=100;
  const speed=command('speed',0,'TRAVEL_THROTTLE',{bps:11000});
  const first=await store.submit('a',f.runId,speed);expect(first.state.distanceMm).toBe(100);expect(first.revision).toBe(1);
  now+=50;await store.submit('a',f.runId,command('pause',1,'PAUSE',{value:true}));
  now+=10000;const recovered=await store.submit('a',f.runId,command('recover',2,'RECOVER'));
  expect(recovered.state.distanceMm).toBe(100);expect(recovered.state.paused).toBe(true);
  expect(await store.submit('a',f.runId,speed)).toEqual(first);
  await expect(store.submit('a',f.runId,{...speed,bps:12000})).rejects.toThrow('request_conflict');
  await expect(store.submit('a',f.runId,command('stale',1,'PAUSE',{value:false}))).rejects.toThrow('revision_conflict');
  now-=1;await expect(store.submit('a',f.runId,command('backwards',3,'PAUSE',{value:false}))).rejects.toThrow('clock_regressed');now+=1;
  now+=50;await store.submit('a',f.runId,command('resume',3,'PAUSE',{value:false}));
  now+=300;const arrived=await store.submit('a',f.runId,command('arrival-server',4,'ARRIVE'));
  expect(arrived.arrival).toMatchObject({arrivalAccepted:true});
  expect(arrived.state.distanceMm).toBeGreaterThanOrEqual(300);
  const receipt=await store.submit('a',f.runId,command('arrival-server',4,'ARRIVE'));expect(receipt).toEqual(arrived);
  const response=await app.request('/v1/runs/'+f.runId+'/b-route/transition',{method:'POST',headers:{...f.headers,'X-B-Route-Version':bRouteVersion,'Idempotency-Key':'accept-server-arrival'},body:JSON.stringify({version:bRouteVersion,command:{kind:'ARRIVE',eventId:'accept-server-arrival',expectedRevision:2,edgeId:'entry:n1-1',evidenceRef:arrived.arrival!.evidenceRef}})},f.env);
  const body=await response.json();expect(body,JSON.stringify(body)).toMatchObject({committed:true,receipt:{state:{phase:'ACTIVE'}}});
  expect(await store.submit('a',f.runId,command('arrival-server',4,'ARRIVE'))).toEqual(arrived);
  expect(await store.submit('a',f.runId,command('recover-after-arrival',5,'RECOVER'))).toMatchObject({status:'CURRENT_MAP',mapState:{phase:'ACTIVE'}});
  const acceptedCount=f.scalar('SELECT COUNT(*) FROM b_travel_commands');
  await expect(store.submit('a',f.runId,command('late-pause',5,'PAUSE',{value:true}))).rejects.toThrow();
  expect(f.scalar('SELECT COUNT(*) FROM b_travel_commands')).toBe(acceptedCount);
  expect((await f.map.read('a',f.runId)).state.phase).toBe('ACTIVE');
 }finally{f.close();}
});

it('accepts travel recovery through the authenticated existing address and rejects client clock fields',async()=>{
 const f=await fixture();try{
  const body={version:bTravelCommandVersion,requestId:'http-recover',expectedRevision:0,kind:'RECOVER',destinationEventId:'choose-boundary',nodeId:'n1-1',mapRevision:2};
  const post=(input:unknown,headers=f.headers)=>app.request('/v1/runs/'+f.runId+'/b-arrival-reports',{method:'POST',headers:{...headers,'X-B-Arrival-Version':bTravelCommandVersion,'Idempotency-Key':body.requestId},body:JSON.stringify(input)},f.env);
  expect((await post({...body,elapsed:1000000})).status).toBe(400);
  const response=await post(body);expect(response.status).toBe(200);const saved=await response.json();
  expect(saved).toMatchObject({revision:1,state:{distanceMm:0,paused:true}});
  expect(await (await post(body)).json()).toEqual(saved);
  const stale={...body,requestId:'http-stale'};
  const conflict=await app.request('/v1/runs/'+f.runId+'/b-arrival-reports',{method:'POST',headers:{...f.headers,'X-B-Arrival-Version':bTravelCommandVersion,'Idempotency-Key':stale.requestId},body:JSON.stringify(stale)},f.env);
  expect(conflict.status).toBe(409);expect(await conflict.json()).toMatchObject({accepted:false,current:saved});
  expect((await post(body,{'Content-Type':'application/json'})).status).toBe(401);
  expect(f.scalar('SELECT COUNT(*) FROM b_travel_commands')).toBe(1);
  expect(f.scalar('SELECT COUNT(*) FROM b_arrival_originals')).toBe(0);
 }finally{f.close();}
});

it('serializes concurrent travel inputs and leaves rejected or premature arrival without authority',async()=>{
 const f=await fixture();try{
  const origin=await f.env.DB.prepare('SELECT confirmed_at_ms FROM operation_map_destination_times WHERE run_id=?').bind(f.runId).first<{confirmed_at_ms:number}>();
  const store=new BTravelCommandStore(f.env.DB,()=>origin!.confirmed_at_ms);
  const command={version:bTravelCommandVersion,requestId:'one',expectedRevision:0,kind:'PAUSE',value:true,destinationEventId:'choose-boundary',nodeId:'n1-1',mapRevision:2};
  const both=await Promise.allSettled([store.submit('a',f.runId,command),store.submit('a',f.runId,{...command,requestId:'two'})]);
  expect(both.filter(r=>r.status==='fulfilled')).toHaveLength(1);
  expect(f.scalar('SELECT COUNT(*) FROM b_travel_commands')).toBe(1);
  const {value,...arrival}=command;void value;
  await expect(store.submit('a',f.runId,{...arrival,kind:'ARRIVE',requestId:'early',expectedRevision:1})).rejects.toThrow('boundary_not_reached');
  expect(f.scalar('SELECT COUNT(*) FROM b_arrival_originals')).toBe(0);
  expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');
 }finally{f.close();}
});

// Explicit synthetic AUTHORING fixture. These numeric values are NOT operating B defaults.
async function fixture(destinationVersion?:string|null){
 const f=await setupBOperationFixture(false),template=await setupBDeparturePinFixture(),raw=structuredClone(template.source);template.close();
 f.p.nodes.find(n=>n.nodeId==='n1-1')!.eventKind='AUGMENT';f.p.enabledEventKinds.push('AUGMENT');
 const replace=<T extends {eventKind:string}>(e:T)=>({...e,eventKind:e.eventKind==='MERCHANT'?'AUGMENT':e.eventKind});
 const publication=await new OperationMapPublisher({read:async()=>({version:'operation.map-catalog.v1',catalogId:'LOCAL_BOUNDARY',revision:1,definitionVersion:f.p.definitionVersion,encounters:f.publication.encounters.map(replace),rewards:f.publication.rewards.map(replace)})}).publish(f.p);
 const binding=await createBOperationBinding(publication,f.snapshot.rewardReference,1),response={...f.snapshot.departureResponse,balanceVersion:'1',bOperationBinding:binding,admission:{...f.snapshot.admission,operationMapPlan:f.p}};
 const base={...response,operationMapContent:publication,departureResponse:response};
 const prepared=await prepareBDepartureCombatSnapshot('a',f.runId,JSON.stringify(base),{prepare:async c=>({source:{...raw,profile:{...raw.profile,definition:{...raw.profile.definition,bArrivalBoundarySource:await prepareBArrivalBoundarySource(c,captureOperationMap(f.p),{
  definitionId:'LOCAL_BOUNDARY_FIXTURE_ONLY',definitionVersion:'local1',stepTicks:1000000,executorVersion:'combat.b-arrival-linear-fixed-step.v1.20261020',speedHalfUnits:2,
  configuration:{version:'rail-journey-v1',selectionScaleBps:100,recoveryMs:600,baseSpeedMillimetersPerSecond:1000,throttleMinimumBps:7000,throttleCruiseBps:10000,throttleMaximumBps:13000,offers:[]},
  boundaries:f.p.edges.map(e=>({...e,distanceMm:250}))})}},binding:{accountId:c.accountId,runId:c.runId,operationPreviewId:c.operationPreviewId,operationId:c.operationId,selectedRisk:c.selectedRisk,operationDefinitionVersion:c.definitionVersion,operationPlanHash:c.planHash,operationPublicationHash:c.publicationHash}},assertions:[f.env.DB.prepare('INSERT INTO departure_write_guard VALUES(1)')]}),supports:async()=>true});
 f.sql.run("INSERT INTO account_mutations VALUES('arrival-fixture-start','a',100,'RUN_START','LOCAL_BOUNDARY_INIT',0)");
 f.sql.run("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) VALUES('LOCAL_B12_PREVIEW','a','arrival-fixture-start','opn_01_last_departure','I',1,?,'CONSUMED',0,0,'departure-v4')",[JSON.stringify(response)]);
 f.sql.run("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,'a','LOCAL_B12_PREVIEW','arrival-fixture-start',1,'ACTIVE',0)",[f.runId]);f.sql.run('INSERT INTO run_snapshots VALUES(?,?,0)',[f.runId,prepared.snapshotJson]);
 // Explicit LOCAL test authority; never installed in the real app.
 const authority={prepare:async (binding:Parameters<import('../../apps/game-api/src/combat-b-arrival-report').BArrivalOriginalAuthority['prepare']>[0])=>({binding,assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard VALUES(1)')]})};
 const map=new OperationMapStore(f.env.DB,new BRouteMapVerifier(f.env.DB,new BOriginalAcceptedBroker(f.env.DB,new BArrivalConsistencyOwner(f.env.DB,authority))));await map.initialize('a',f.runId);
 let destinationReply:unknown;
 if(destinationVersion===undefined)await map.commit('a',f.runId,{kind:'CONFIRM',eventId:'choose-boundary',expectedRevision:1,edgeId:'entry:n1-1'});
 else{const response=await app.request('/v1/runs/'+f.runId+'/operation-map/destination',{method:'POST',headers:{...f.headers,'Idempotency-Key':'choose-boundary',...(destinationVersion?{'X-Map-Destination-Version':destinationVersion}:{})},body:JSON.stringify({expectedRevision:1,edgeId:'entry:n1-1'})},f.env);expect(response.status).toBe(200);destinationReply=await response.json();}
 const source=await readPinnedBArrivalBoundary(f.env.DB,'a',f.runId,'n1-1');
 const originalEventId='B_ARRIVE:'+await sha256Hex(canonicalJson({runId:f.runId,nodeId:'n1-1',destinationEventId:'choose-boundary',boundarySourceHash:source.source.sourceHash,commandId:'travel-original',tick:3}));
 const travel={version:'combat.b-arrival-travel-observation.v1.20261020',accountId:'a',runId:f.runId,nodeId:'n1-1',nativeContextId:source.nativeContextId,boundarySourceHash:source.source.sourceHash,nodeSourceHash:source.nodeSourceHash,
  commands:[{kind:'ADVANCE',eventId:'travel-original',elapsed:5000000,maximumSteps:5}],results:[{tick:3,distanceMm:300,remainderUnits:0,debtTicks:2000000,paused:false,throttleBps:10000}],arrival:{eventId:originalEventId,commandId:'travel-original',commandSequence:1,tick:3}};
 const body={version:bArrivalReportV2Version,requestId:'arrival-v2',reportOriginMapRevision:2,destinationEventId:'choose-boundary',checkpointHash:'sha256:'+await sha256Hex(canonicalJson(travel)),travel};
 return {...f,map,body,source,authority,destinationReply};
}
it('validates original pinned geometry/commands for a noncombat AUGMENT node and emits a dedicated arrival observation',async()=>{
 const f=await fixture();try{const store=new BArrivalReportStore(f.env.DB,f.authority),r=await store.submit('a',f.runId,f.body);expect(r).toMatchObject({arrivalAccepted:true,combatTruth:'NOT_PROVEN',verificationLevel:'ORIGINAL_PINNED_TRAVEL_COMMAND_REPLAY'});
 const p=await new BArrivalConsistencyOwner(f.env.DB,f.authority).prepare({accountId:'a',runId:f.runId,nodeId:'n1-1',mapRevision:2,kind:'ARRIVE',evidenceRef:r.evidenceRef});expect(p.status).toBe('VERIFIED');if(p.status==='VERIFIED'){expect(p.observation).not.toHaveProperty('waves');expect(p.observation).not.toHaveProperty('hazards');expect(p.assertions.length).toBeGreaterThan(0);}
 expect(await store.submit('a',f.runId,f.body)).toEqual(r);expect(f.scalar('SELECT count(*) FROM operation_map_write_guard')).toBe(0);
 // Explicit synthetic authority exercises the installed broker/map boundary.
 // The authenticated app test below proves this authority is not installed there.
 await f.map.commit('a',f.runId,{kind:'ARRIVE',eventId:'accept-boundary',expectedRevision:2,edgeId:'entry:n1-1',evidenceRef:r.evidenceRef});
 expect((await f.map.read('a',f.runId)).state.phase).toBe('ACTIVE');
 expect(f.scalar('SELECT count(*) FROM solo_augment_grants')).toBe(0);
 }finally{f.close();}
});
it('actual authenticated app rejects replay-only arrival without original elapsed and event authority',async()=>{
 const f=await fixture();try{
 const response=await app.request('/v1/runs/'+f.runId+'/b-arrival-reports',{method:'POST',headers:{...f.headers,'X-B-Arrival-Version':bArrivalReportV2Version,'Idempotency-Key':f.body.requestId},body:JSON.stringify(f.body)},f.env);
 expect(response.status).toBe(409);expect(await response.json()).toMatchObject({accepted:false,errorCode:'combat_b_arrival_original_time_credit_and_accepted_event_owner_missing'});
 expect((await f.map.read('a',f.runId)).state.phase).toBe('TRAVELLING');expect(f.scalar('SELECT count(*) FROM b_arrival_reports')).toBe(0);
 }finally{f.close();}
});
it('requires exact original authority binding and fences its loss atomically',async()=>{
 const f=await fixture();try{
 const wrong={prepare:async (binding:Parameters<typeof f.authority.prepare>[0])=>({binding:{...binding,tick:binding.tick+1},assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard VALUES(1)')]})};
 await expect(new BArrivalReportStore(f.env.DB,wrong).submit('a',f.runId,f.body)).rejects.toThrow('original_authority_conflict');
 const revoked={prepare:async (binding:Parameters<typeof f.authority.prepare>[0])=>({binding,assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard VALUES(0)')]})};
 await expect(new BArrivalReportStore(f.env.DB,revoked).submit('a',f.runId,f.body)).rejects.toThrow('state_or_original_event_conflict');
 expect(f.scalar('SELECT count(*) FROM b_arrival_reports')).toBe(0);
 const receipt=await new BArrivalReportStore(f.env.DB,f.authority).submit('a',f.runId,f.body);
 await expect(new BArrivalConsistencyOwner(f.env.DB).prepare({accountId:'a',runId:f.runId,nodeId:'n1-1',mapRevision:2,kind:'ARRIVE',evidenceRef:receipt.evidenceRef})).rejects.toThrow('original_time_credit_and_accepted_event_owner_missing');
 }finally{f.close();}
});
it('rejects a valid digest around invented distance, skipped first boundary, paused arrival or wrong destination',async()=>{
 const f=await fixture();try{const store=new BArrivalReportStore(f.env.DB,f.authority);
 for(const edit of [(b:typeof f.body)=>b.travel.results[0]!.distanceMm=250,(b:typeof f.body)=>b.travel.results[0]!.tick=5,(b:typeof f.body)=>b.travel.results[0]!.paused=true,(b:typeof f.body)=>b.destinationEventId='other-original',(b:typeof f.body)=>b.travel.commands[0]!.elapsed=1000000]){
 const b=structuredClone(f.body);edit(b);b.checkpointHash='sha256:'+await sha256Hex(canonicalJson(b.travel));await expect(store.submit('a',f.runId,b)).rejects.toThrow();}
 expect(f.scalar('SELECT count(*) FROM b_arrival_reports')).toBe(0);
 }finally{f.close();}
});
it('fences map changes during ingestion and revocation during consumption',async()=>{
 const f=await fixture();try{const store=new BArrivalReportStore(f.env.DB,f.authority),r=await store.submit('a',f.runId,f.body),p=await new BArrivalConsistencyOwner(f.env.DB,f.authority).prepare({accountId:'a',runId:f.runId,nodeId:'n1-1',mapRevision:2,kind:'ARRIVE',evidenceRef:r.evidenceRef});
 f.sql.run('INSERT INTO b_arrival_report_revocations VALUES(?,?,?)',[f.runId,r.evidenceRef,'withdrawn']);if(p.status==='VERIFIED')await expect(f.env.DB.batch([...p.assertions])).rejects.toThrow();await expect(store.submit('a',f.runId,f.body)).rejects.toThrow('revoked');
 }finally{f.close();}
 const g=await fixture();try{g.adapter.beforeBatch=()=>{delete g.adapter.beforeBatch;g.sql.run('UPDATE operation_map_states SET revision=revision+1');};await expect(new BArrivalReportStore(g.env.DB,g.authority).submit('a',g.runId,g.body)).rejects.toThrow('state_or_original_event_conflict');expect(g.scalar('SELECT count(*) FROM b_arrival_reports')).toBe(0);}finally{g.close();}
});
it('deduplicates the original destination across different upload keys without authorizing wave/safe completion',async()=>{
 const f=await fixture();try{const store=new BArrivalReportStore(f.env.DB,f.authority),r=await store.submit('a',f.runId,f.body);await expect(store.submit('a',f.runId,{...f.body,requestId:'different-key'})).rejects.toThrow('state_or_original_event_conflict');
 expect(await new BArrivalConsistencyOwner(f.env.DB,f.authority).prepare({accountId:'a',runId:f.runId,nodeId:'n1-1',mapRevision:2,kind:'COMPLETE',evidenceRef:r.evidenceRef})).toMatchObject({status:'UNSUPPORTED'});
 }finally{f.close();}
});
async function seal(body:ReturnType<typeof bArrivalBoundaryReportSchema.parse>){
 const t=body.travel,commandId=t.commands.at(-1)!.eventId,tick=t.results.at(-1)!.tick;
 t.arrival={eventId:'B_ARRIVE:'+await sha256Hex(canonicalJson({runId:t.runId,nodeId:t.nodeId,destinationEventId:body.destinationEventId,boundarySourceHash:t.boundarySourceHash,commandId,tick})),commandId,commandSequence:t.commands.length,tick};
 body.checkpointHash='sha256:'+await sha256Hex(canonicalJson(t));return body;
}
async function originalFixture(){
 const f=await fixture(),v=await verifyBArrivalBoundaryReport(f.source,f.body);
 const binding={accountId:'a',runId:f.runId,nodeId:'n1-1',mapRevision:2,checkpointHash:f.body.checkpointHash,originalEventId:v.originalEventId,sequence:v.sequence,tick:v.tick,destinationEventId:'choose-boundary',departurePinHash:f.source.departurePinHash,nodeSourceHash:f.source.nodeSourceHash};
 // A separate LOCAL source journal stands in for the independently verified
 // original combat-clock producer. Production never installs this verifier.
 const proof={binding,sourceRef:'LOCAL_ORIGINAL_CLOCK_RECEIPT',sourceRevision:1,clockCheckpointRef:'LOCAL_CLOCK_CHECKPOINT',
  clock:{authority:'LOCAL_LOGICAL',stepTicks:1000000,debtHalfTicks:4000000,completedSteps:3,speedHalfUnits:2,paused:false}};
 f.sql.run('CREATE TABLE local_original_clock_source(source_ref TEXT PRIMARY KEY,proof_json TEXT NOT NULL)');
 f.sql.run('INSERT INTO local_original_clock_source VALUES(?,?)',[proof.sourceRef,JSON.stringify(proof)]);
 const verifier:BOriginalArrivalVerifier={prepare:async input=>{
  const row=await f.env.DB.prepare('SELECT proof_json FROM local_original_clock_source WHERE source_ref=?').bind(input.sourceRef).first<{proof_json:string}>();
  if(!row)throw Error('LOCAL original source missing');
  return {proof:JSON.parse(row.proof_json),assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM local_original_clock_source WHERE source_ref=? AND proof_json=?) THEN 1 ELSE 0 END').bind(input.sourceRef,row.proof_json)]};
 }};
 return {...f,binding,proof,verifier,input:{binding,sourceRef:proof.sourceRef}};
}
it('captures independently verified original time receipts and consumes persisted authority through report and ARRIVE',async()=>{
 const f=await originalFixture();try{
 const original=new BOriginalArrivalStore(f.env.DB,f.verifier),receipt=await original.capture(f.input);
 expect(receipt).toMatchObject({sourceRef:f.proof.sourceRef,combatTruth:'NOT_PROVEN'});
 const authority=new BOriginalArrivalStore(f.env.DB);
 const uploaded=await app.request('/v1/runs/'+f.runId+'/b-arrival-reports',{method:'POST',headers:{...f.headers,'X-B-Arrival-Version':bArrivalReportV2Version,'Idempotency-Key':f.body.requestId},body:JSON.stringify(f.body)},f.env);
 expect(uploaded.status).toBe(200);const report=await uploaded.json();
 const arrive=()=>app.request('/v1/runs/'+f.runId+'/b-route/transition',{method:'POST',headers:{...f.headers,'X-B-Route-Version':bRouteVersion,'Idempotency-Key':'persisted-original-arrival'},body:JSON.stringify({version:bRouteVersion,command:{kind:'ARRIVE',eventId:'persisted-original-arrival',expectedRevision:2,edgeId:'entry:n1-1',evidenceRef:report.evidenceRef}})},f.env);
 const arrived=await arrive();expect(arrived.status).toBe(409);
 const result=await arrived.json();expect(result).toMatchObject({committed:true,augmentPending:true,errorCode:'augment_runtime_catalog_registry_required'});
 expect((await f.map.read('a',f.runId)).state.phase).toBe('ACTIVE');
 expect(await (await arrive()).json()).toEqual(result);
 expect(await original.capture(f.input)).toEqual(receipt);
 expect(await new BArrivalReportStore(f.env.DB,authority).submit('a',f.runId,f.body)).toEqual(report);
 expect(f.scalar('SELECT count(*) FROM b_arrival_originals')).toBe(1);
 expect(f.scalar('SELECT count(*) FROM operation_map_write_guard')).toBe(0);
 }finally{f.close();}
});
it('refuses original capture without independent verifier and rejects different binding or clock source',async()=>{
 const f=await originalFixture();try{
 await expect(new BOriginalArrivalStore(f.env.DB).capture(f.input)).rejects.toThrow('original_verifier_missing');
 for(const edit of [(p:typeof f.proof)=>p.binding.tick++,(p:typeof f.proof)=>p.clock.completedSteps++,(p:typeof f.proof)=>p.clock.stepTicks++,(p:typeof f.proof)=>p.clock.authority='SERVER_COMBAT']){
  const p=structuredClone(f.proof);edit(p);f.sql.run('UPDATE local_original_clock_source SET proof_json=?',[JSON.stringify(p)]);
  await expect(new BOriginalArrivalStore(f.env.DB,f.verifier).capture(f.input)).rejects.toThrow();
 }
 expect(f.scalar('SELECT count(*) FROM b_arrival_originals')).toBe(0);
 }finally{f.close();}
});
it('fences original source changes during capture and converges concurrent exact-key capture',async()=>{
 const f=await originalFixture();try{
 const original=new BOriginalArrivalStore(f.env.DB,f.verifier);
 f.adapter.beforeBatch=()=>{delete f.adapter.beforeBatch;f.sql.run("DELETE FROM local_original_clock_source");};
 await expect(original.capture(f.input)).rejects.toThrow('original_capture_conflict');
 expect(f.scalar('SELECT count(*) FROM b_arrival_originals')).toBe(0);
 f.sql.run('INSERT INTO local_original_clock_source VALUES(?,?)',[f.proof.sourceRef,JSON.stringify(f.proof)]);
 const both=await Promise.all([original.capture(f.input),original.capture(f.input)]);expect(both[0]).toEqual(both[1]);
 expect(f.scalar('SELECT count(*) FROM b_arrival_originals')).toBe(1);
 await expect(original.capture({...f.input,binding:{...f.binding,sequence:2}})).rejects.toThrow('original_source_conflict');
 }finally{f.close();}
});
it('revokes original evidence between prepare and map commit without publishing arrival',async()=>{
 const f=await originalFixture();try{
 const authority=new BOriginalArrivalStore(f.env.DB,f.verifier);await authority.capture(f.input);
 const report=await new BArrivalReportStore(f.env.DB,authority).submit('a',f.runId,f.body);
 const map=new OperationMapStore(f.env.DB,new BRouteMapVerifier(f.env.DB,new BOriginalAcceptedBroker(f.env.DB,new BArrivalConsistencyOwner(f.env.DB,authority))));
 f.adapter.beforeBatch=()=>{delete f.adapter.beforeBatch;f.sql.run('INSERT INTO b_arrival_original_revocations VALUES(?,?,?)',[f.runId,f.input.sourceRef,'SOURCE_WITHDRAWN']);};
 await expect(map.commit('a',f.runId,{kind:'ARRIVE',eventId:'revoked-arrival',expectedRevision:2,edgeId:'entry:n1-1',evidenceRef:report.evidenceRef})).rejects.toThrow();
 expect((await map.read('a',f.runId)).state.phase).toBe('TRAVELLING');
 expect(f.scalar("SELECT count(*) FROM operation_map_receipts WHERE event_id='revoked-arrival'")).toBe(0);
 await expect(authority.prepare(f.binding)).rejects.toThrow('original_revoked');
 await expect(authority.capture(f.input)).rejects.toThrow('original_revoked');
 expect(await authority.revoke('a',f.runId,f.input.sourceRef,'SOURCE_WITHDRAWN')).toEqual({revoked:true,sourceRef:f.input.sourceRef});
 }finally{f.close();}
});
it('preserves native B maximumSteps debt, zero-elapsed catchup and pause without borrowing CE03 clamp',async()=>{
 const f=await fixture();try{const b=bArrivalBoundaryReportSchema.parse(f.body);
 b.travel.commands=[{kind:'ADVANCE',eventId:'credit',elapsed:5000000,maximumSteps:0},{kind:'PAUSE',eventId:'pause',value:true},{kind:'ADVANCE',eventId:'paused-no-credit',elapsed:9000000,maximumSteps:5},{kind:'PAUSE',eventId:'resume',value:false},{kind:'ADVANCE',eventId:'travel-original',elapsed:0,maximumSteps:5}];
 const base={tick:0,distanceMm:0,remainderUnits:0,debtTicks:5000000,paused:false,throttleBps:10000};
 b.travel.results=[base,{...base,paused:true},{...base,paused:true},base,{...base,tick:3,distanceMm:300,debtTicks:2000000}];
 expect((await verifyBArrivalBoundaryReport(f.source,await seal(b))).tick).toBe(3);
 const forged=structuredClone(b);forged.travel.results[2]!.debtTicks+=9000000;await expect(verifyBArrivalBoundaryReport(f.source,await seal(forged))).rejects.toThrow('travel_result_conflict');
 const throttle=structuredClone(b);throttle.travel.commands[2]={kind:'TRAVEL_THROTTLE',eventId:'paused-throttle',bps:13000};await expect(verifyBArrivalBoundaryReport(f.source,await seal(throttle))).rejects.toThrow('throttle_paused');
 }finally{f.close();}
});
it('preserves fractional-step partitioning and validates bounded throttle distance exactly',async()=>{
 const f=await fixture();try{const b=bArrivalBoundaryReportSchema.parse(f.body);
 b.travel.commands=[{kind:'ADVANCE',eventId:'fraction',elapsed:500000,maximumSteps:5},{kind:'TRAVEL_THROTTLE',eventId:'slow',bps:7000},{kind:'ADVANCE',eventId:'travel-original',elapsed:4500000,maximumSteps:5}];
 b.travel.results=[{tick:0,distanceMm:0,remainderUnits:0,debtTicks:500000,paused:false,throttleBps:10000},{tick:0,distanceMm:0,remainderUnits:0,debtTicks:500000,paused:false,throttleBps:7000},{tick:4,distanceMm:280,remainderUnits:0,debtTicks:1000000,paused:false,throttleBps:7000}];
 expect((await verifyBArrivalBoundaryReport(f.source,await seal(b))).tick).toBe(4);
 const wrong=structuredClone(b);wrong.travel.commands[1]={kind:'TRAVEL_THROTTLE',eventId:'illegal',bps:6000};await expect(verifyBArrivalBoundaryReport(f.source,await seal(wrong))).rejects.toThrow('throttle_conflict');
 }finally{f.close();}
});
