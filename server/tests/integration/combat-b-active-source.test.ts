import type {BActiveSourcePacket} from '../../apps/game-api/src/combat-b-active-source';
import type {BSelectionStore} from '../../apps/game-api/src/combat-b-selection-store';
import {existsSync} from 'node:fs';
import {expect,it} from 'vitest';
import {startBRankFixtureSocket} from '../../scripts/local/combat-b-rank-socket';
import type {RefitRead} from '../../apps/game-api/src/combat-b-local-flow-source';
import {refitHttpVersion} from '../../apps/game-api/src/combat-b-refit-http';
const version='combat.b-active-source.v1.20261003';
const start=async(options:{restore?:Uint8Array;lifetimeMs?:number;port?:number}={})=>existsSync('scripts/local/combat-b-active-socket.ts')?(await import('../../scripts/local/combat-b-active-socket')).startBActiveSocket(options):startBRankFixtureSocket(options);
async function prepare(h:Awaited<ReturnType<typeof start>>,role:'a'|'b'='a'){
 const intake=h.intake(role),headers={...intake.headers,'X-Combat-B-Active-Version':version};
 const sel=await(await fetch(h.origin+'/_local/combat-b/selection',{headers})).json() as Awaited<ReturnType<BSelectionStore['read']>>;
 const body={version:sel.version,expectedRevision:sel.revision,catalogVersion:sel.catalog.version,expectedCatalogRevision:sel.catalog.revision,selection:{...sel.selection,placements:sel.selection!.placements.map((x)=>x.slot===0?{...x,occupant:{kind:'OPERATOR',operatorId:'op_mira'}}:x)}};
 expect((await fetch(h.origin+'/_local/combat-b/selection',{method:'POST',headers:{...headers,'Content-Type':'application/json','Idempotency-Key':'mira'},body:JSON.stringify(body)})).status).toBe(200);
 const current=await(await fetch(h.origin+'/_local/combat-b/refit',{headers})).json() as RefitRead;
 const save={version:refitHttpVersion,expectedRevision:current.revision,selectionRevision:current.selectionRevision,rankRevision:current.rankSource.revision,definitionHash:current.definitionHash,sourceHash:current.sourceHash,refits:current.rankSource.instances.map(i=>({slot:i.slot,type:i.slot===0?'BCB02':'BCB01',rank:1}))};
 expect((await fetch(h.origin+'/_local/combat-b/refit',{method:'POST',headers:{...headers,'Content-Type':'application/json','Idempotency-Key':'mira-refit'},body:JSON.stringify(save)})).status).toBe(200);
 return {headers,save};
}
it('actual selected Mira physical/profile/approved ACTIVE supplies immutable timing and six-shot phase, never a fake acceptance',async()=>{
 const h=await start();try{const {headers}=await prepare(h);const response=await fetch(h.origin+'/_local/combat-b/active-source',{headers});expect(response.status).toBe(200);
 const p=await response.json() as BActiveSourcePacket;expect(p.version).toBe(version);expect(p.actor).toMatchObject({actorId:'op_mira',operatorId:'op_mira',slot:0,instanceId:'a_physical1',stats:{maxHp:1520,attack:{numerator:'4365000000',denominator:'100000000'},defense:111,basicIntervalMs:1800}});
 expect(p.ability).toMatchObject({id:'op_mira.active',kind:'ACTIVE',timing:'TIME_COOLDOWN',effect:'FIXED_TARGET_BURST',cooldownMs:10000,windupMs:300,recoveryMs:300,shots:6,damageBps:4500,intervalMs:200});expect(p.timing.stepTimeSpanTicks).toBe(10000);expect(p.timing.firstReadyMs).toBe(0);expect(p.phase.shotOffsetsMs).toEqual([300,500,700,900,1100,1300]);expect(p.phase.occupancyMs).toBe(1600);expect(p.initial).toMatchObject({tick:0,readyTick:0,phase:'IDLE',commands:[],committedShots:[],reservationCursor:0});expect(p.canExecute).toBe(false);expect(p.packetHash).toMatch(/^sha256:[a-f0-9]{64}$/);expect(response.headers.get('cache-control')).toBe('no-store');expect(await(await fetch(h.origin+'/_local/combat-b/active-source',{headers})).json()).toEqual(p);
 }finally{expect(await h.close()).toMatchObject({listenerClosed:true,databaseDiscarded:true,cleanupFailure:false});}
});
it('signed boundary rejects unsupported actor, stale rank and wrong requests; frozen packet/receipt survive actual source change and reopen',async()=>{
 const h=await start();let r:Awaited<ReturnType<typeof start>>|undefined;try{
 const a={...h.intake('a').headers,'X-Combat-B-Active-Version':version},url=h.origin+'/_local/combat-b/active-source';
 expect((await fetch(url,{headers:a})).status).toBe(409);const {headers,save}=await prepare(h);const original=await(await fetch(url,{headers})).json() as BActiveSourcePacket;
 expect((await fetch(url)).status).toBe(401);expect((await fetch(url,{headers:h.intake('b').headers})).status).toBe(409);expect((await fetch(url,{headers:{...headers,'X-Combat-B-Active-Version':'wrong'}})).status).toBe(409);expect((await fetch(url+'?ability=op_eve.active',{headers})).status).toBe(400);expect((await fetch(url,{method:'POST',headers})).status).toBe(400);
 const read=await(await fetch(h.origin+'/_local/combat-b/refit',{headers})).json() as RefitRead;
 const result=await h.transitionRanks('a',{version:'combat.b-local-rank-fixture.v1.20261003',expectedRankRevision:read.rankSource.revision,expectedSourceHash:read.sourceHash,changes:[{type:'BCB02',rank:5}]},'mira-r5');expect(result.rankRevision).toBe(2);
 expect((await fetch(url,{headers})).status).toBe(409);const fresh=await(await fetch(h.origin+'/_local/combat-b/refit',{headers})).json() as RefitRead;
 const newSave={...save,expectedRevision:1,rankRevision:2,sourceHash:fresh.sourceHash,refits:save.refits.map(x=>({...x,rank:x.type==='BCB02'?5:1}))};expect((await fetch(h.origin+'/_local/combat-b/refit',{method:'POST',headers:{...headers,'Content-Type':'application/json','Idempotency-Key':'mira-r5-refit'},body:JSON.stringify(newSave)})).status).toBe(200);
 const next=await(await fetch(url,{headers})).json() as BActiveSourcePacket;expect(next.packetHash).not.toBe(original.packetHash);expect(original.departure.cars[0]!.rank).toBe(1);expect(next.departure.cars[0]!.rank).toBe(5);expect(next.actor.instanceId).toBe(original.actor.instanceId);
 expect(await(await fetch(h.origin+'/_local/combat-b/refit',{method:'POST',headers:{...headers,'Content-Type':'application/json','Idempotency-Key':'mira-refit'},body:JSON.stringify(save)})).json()).toMatchObject({revision:1});
 const bytes=h.checkpoint();try{r=await start({restore:bytes});expect((await fetch(r.origin+'/_local/combat-b/active-source',{headers})).status).toBe(401);expect(await(await fetch(r.origin+'/_local/combat-b/active-source',{headers:{...r.intake('a').headers,'X-Combat-B-Active-Version':version}})).json()).toEqual(next);}finally{bytes.fill(0);}
 }finally{if(r)await r.close();await h.close();}
});
it('frozen original restore rejects altered and rehashed foreign packets without consulting current source',async()=>{
 const {readPinnedBActiveSource}=await import('../../apps/game-api/src/combat-b-active-source');const {canonicalJson,sha256Hex}=await import('@lastrail/common');const h=await start();try{
 const {headers}=await prepare(h);const original=await(await fetch(h.origin+'/_local/combat-b/active-source',{headers})).json() as BActiveSourcePacket;
 const restored=readPinnedBActiveSource(original,original);expect(restored).toEqual(original);restored.actor.actorId='tampered';expect(original.actor.actorId).toBe('op_mira');
 for(const mutate of [(p:BActiveSourcePacket)=>p.ability.id='op_eve.active',(p:BActiveSourcePacket)=>p.actor.instanceId='b_physical1',(p:BActiveSourcePacket)=>p.phase.shotOffsetsMs[0]=1,(p:BActiveSourcePacket)=>p.initial.tick=10,(p:BActiveSourcePacket)=>Reflect.set(p.timing,'generalHaste',[{id:'invented',bps:20000}]),(p:BActiveSourcePacket)=>p.version='wrong']){
 const changed=structuredClone(original);mutate(changed);const {packetHash,...payload}=changed;void packetHash;changed.packetHash='sha256:'+await sha256Hex(canonicalJson(payload as Parameters<typeof canonicalJson>[0]));expect(()=>readPinnedBActiveSource(changed,original)).toThrow('combat_b_active_original_packet_conflict');
 }
 expect(()=>readPinnedBActiveSource(null,original)).toThrow();
 }finally{await h.close();}
});
it('new source preserves original Eve route, signed revocation and owned socket lease/port cleanup',async()=>{
 const h=await start();try{const a=h.intake('a');const read=await(await fetch(h.origin+'/_local/combat-b/refit',{headers:a.headers})).json() as RefitRead;
 const body={version:refitHttpVersion,expectedRevision:0,selectionRevision:read.selectionRevision,rankRevision:read.rankSource.revision,definitionHash:read.definitionHash,sourceHash:read.sourceHash,refits:read.rankSource.instances.map(i=>({slot:i.slot,type:'BCB01',rank:1}))};
 expect((await fetch(h.origin+'/_local/combat-b/refit',{method:'POST',headers:{...a.headers,'Content-Type':'application/json','Idempotency-Key':'eve-old'},body:JSON.stringify(body)})).status).toBe(200);
 expect((await fetch(h.origin+'/_local/combat-b/source',{headers:a.headers})).status).toBe(200);const unsupported=await fetch(h.origin+'/_local/combat-b/active-source',{headers:a.headers});expect(unsupported.status).toBe(503);expect(await unsupported.json()).toMatchObject({errorCode:'combat_b_active_actor_unsupported'});
 expect((await fetch(h.origin+'/_local/combat-b/active-source',{headers:{...a.headers,Origin:'http://foreign.invalid'}})).status).toBe(403);
 await expect(start({port:Number(new URL(h.origin).port)})).rejects.toMatchObject({cleanup:{listenerClosed:true,databaseDiscarded:true,cleanupFailure:false}});
 const lease=await start({lifetimeMs:100});expect(await lease.closed).toMatchObject({listenerClosed:true,databaseDiscarded:true,cleanupFailure:false});expect(()=>lease.intake('a')).toThrow();
 }finally{await h.close();}
});
it('unsupported and corrupted LOCAL reference cannot be rehashed into an accepted ACTIVE source',async()=>{
 const {captureBActiveSource}=await import('../../apps/game-api/src/combat-b-active-source');const {readFileSync}=await import('node:fs');const {createHash}=await import('node:crypto');const {default:initSqlJs}=await import('sql.js');const {SqlJsD1Database}=await import('../helpers/growth-d1');
 const h=await start();try{const {headers}=await prepare(h);const current=await(await fetch(h.origin+'/_local/combat-b/refit',{headers})).json() as RefitRead;const bytes=h.checkpoint(),SQL=await initSqlJs(),sql=new SQL.Database(bytes);bytes.fill(0);try{
 const db=new SqlJsD1Database(sql) as unknown as D1Database;const data=readFileSync('D:/Project/LASTRAIL/QA/CombatBMigration-20261002/AttackConsumer/local-original-input.json');const original={template:JSON.parse(data.toString('utf8')),sha256:createHash('sha256').update(data).digest('hex')};
 for(const mutate of [(x:typeof original)=>x.template.version='unsupported.v99',(x:typeof original)=>x.template.geometry[0].distanceMm=-50,(x:typeof original)=>x.template.geometry[0].validUntilTick=-1,(x:typeof original)=>x.template.entities[0].maxHp=-1,(x:typeof original)=>x.sha256='0'.repeat(64)]){
 const changed=structuredClone(original);mutate(changed);await expect(captureBActiveSource(db,current,changed)).rejects.toMatchObject({code:'combat_b_active_reference_unsupported'});
 }
 }finally{sql.close();}
 }finally{await h.close();}
});
