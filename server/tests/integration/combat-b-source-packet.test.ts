import type {RefitRead,captureLocalFlowPacket} from '../../apps/game-api/src/combat-b-local-flow-source';
import type {BSelectionStore} from '../../apps/game-api/src/combat-b-selection-store';
type Packet=Awaited<ReturnType<typeof captureLocalFlowPacket>>;
import {expect,it} from 'vitest';
import {existsSync} from 'node:fs';
import {startBRefitSocket} from '../../scripts/local/combat-b-refit-socket';
import {refitHttpVersion} from '../../apps/game-api/src/combat-b-refit-http';
it('actual authenticated LOCAL supplier fails closed until saved eligible source, then supplies current Eve/physical frozen input',async()=>{
 const start=existsSync('scripts/local/combat-b-source-socket.ts')?(await import('../../scripts/local/combat-b-source-socket')).startBSourceSocket:startBRefitSocket;
 const h=await start();try{const a=h.intake('a'),url=h.origin+'/_local/combat-b/source';const early=await fetch(url,{headers:a.headers});expect(early.status).toBe(409);
 const read=await(await fetch(h.origin+'/_local/combat-b/refit',{headers:a.headers})).json() as RefitRead;
 const body={version:refitHttpVersion,expectedRevision:0,selectionRevision:read.selectionRevision,rankRevision:read.rankSource.revision,definitionHash:read.definitionHash,sourceHash:read.sourceHash,refits:read.rankSource.instances.map((i)=>({slot:i.slot,type:'BCB01',rank:1}))};
 expect((await fetch(h.origin+'/_local/combat-b/refit',{method:'POST',headers:{...a.headers,'Content-Type':'application/json','Idempotency-Key':'packet'},body:JSON.stringify(body)})).status).toBe(200);
 const packet=await fetch(url,{headers:a.headers});expect(packet.status).toBe(200);const input=await packet.json() as Packet;
 expect(input.attack.version).toBe('unity.combat-b.direct-local.v2');expect(input.loopBinding.version).toBe('unity.combat-b.basic-loop-binding-local.v1');expect(input.accountId).toBe('a');expect(input.departure.cars[0]!.instanceId).toBe('a_physical1');expect(input.attack.entities.find((x)=>x.id==='op_eve')).toMatchObject({definitionId:'op_eve',maxHp:1368,basicIntervalMicroseconds:2000000,attack:{numerator:'3878000000',denominator:'100000000'}});
 expect(input.attack.formation.slots[0]).toEqual({carriageId:'a_physical1',operatorId:'op_eve'});expect(input.startReady).toBe(false);expect(input.packetHash).toMatch(/^sha256:[a-f0-9]{64}$/);expect(await(await fetch(url,{headers:a.headers})).json()).toEqual(input);
 const b=h.intake('b');expect((await fetch(url,{headers:b.headers})).status).toBe(409);
 const sel=await(await fetch(h.origin+'/_local/combat-b/selection',{headers:a.headers})).json() as Awaited<ReturnType<BSelectionStore['read']>>;
 const changed={version:'combat.b-selection-http.v1.20261002',expectedRevision:1,catalogVersion:sel.catalog.version,expectedCatalogRevision:sel.catalog.revision,selection:{...sel.selection,placements:[{...sel.selection!.placements[0],occupant:{kind:'CARGO'}},{...sel.selection!.placements[1],occupant:{kind:'OPERATOR',operatorId:'op_eve'}}]}};
 expect((await fetch(h.origin+'/_local/combat-b/selection',{method:'POST',headers:{...a.headers,'Content-Type':'application/json','Idempotency-Key':'moveEve'},body:JSON.stringify(changed)})).status).toBe(200);
 expect((await fetch(url,{headers:a.headers})).status).toBe(409); // old saved refit remains historical, not auto-rebound
 const fresh=await(await fetch(h.origin+'/_local/combat-b/refit',{headers:a.headers})).json() as RefitRead;
 const next={...body,expectedRevision:1,selectionRevision:2,sourceHash:fresh.sourceHash,refits:[{slot:0,type:'BCB01',rank:1},{slot:1,type:'BCB04',rank:1}]};
 expect((await fetch(h.origin+'/_local/combat-b/refit',{method:'POST',headers:{...a.headers,'Content-Type':'application/json','Idempotency-Key':'nextPacket'},body:JSON.stringify(next)})).status).toBe(200);
 const current=await(await fetch(url,{headers:a.headers})).json() as Packet;expect(current.attack.formation.slots[1]).toEqual({carriageId:'a_physical2',operatorId:'op_eve'});expect(current.packetHash).not.toBe(input.packetHash);expect(input.attack.formation.slots[0]!.operatorId).toBe('op_eve');
 expect(await(await fetch(h.origin+'/_local/combat-b/refit',{method:'POST',headers:{...a.headers,'Content-Type':'application/json','Idempotency-Key':'packet'},body:JSON.stringify(body)})).json()).toMatchObject({revision:1});
 const bytes=h.checkpoint();let restored:Awaited<ReturnType<typeof startBRefitSocket>>|undefined;try{restored=await start({restore:bytes});expect((await fetch(restored.origin+'/_local/combat-b/source',{headers:a.headers})).status).toBe(401);expect(await(await fetch(restored.origin+'/_local/combat-b/source',{headers:restored.intake('a').headers})).json()).toEqual(current);}finally{bytes.fill(0);if(restored)expect(await restored.close()).toMatchObject({listenerClosed:true,databaseDiscarded:true,cleanupFailure:false});}
 expect((await fetch(url)).status).toBe(401);expect((await fetch(url+'?accountId=b',{headers:a.headers})).status).toBe(400);
 }finally{expect(await h.close()).toMatchObject({listenerClosed:true,databaseDiscarded:true,cleanupFailure:false});}
});
it('new source supplier lease and port collision dispose owned DB without closing another supplier',async()=>{const {startBSourceSocket}=await import('../../scripts/local/combat-b-source-socket');const h=await startBSourceSocket();try{await expect(startBSourceSocket({port:Number(new URL(h.origin).port)})).rejects.toMatchObject({cleanup:{listenerClosed:true,databaseDiscarded:true,cleanupFailure:false}});expect((await fetch(h.origin+'/_local/combat-b/refit',{headers:h.intake('a').headers})).status).toBe(200);const lease=await startBSourceSocket({lifetimeMs:100});expect(await lease.closed).toMatchObject({listenerClosed:true,databaseDiscarded:true,cleanupFailure:false});expect(()=>lease.intake('a')).toThrow();}finally{await h.close();}});