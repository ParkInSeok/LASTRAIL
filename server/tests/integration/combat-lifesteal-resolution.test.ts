import {afterEach,expect,it} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {CombatHealthStore} from '../../apps/game-api/src/combat-health-store';
import {CombatLifestealResolution,type LifestealResolutionSource} from '../../apps/game-api/src/combat-lifesteal-resolution';
const cleanups:(()=>void)[]=[];afterEach(()=>{for(const close of cleanups.splice(0))close();});
async function fixture(hpLoss=1000,actorHp=100,forbidden=false,actorMaxHp=20000){
 const f=await setup(true);cleanups.push(()=>f.sql.close());const q=await(await f.preview()).json(),runId=(await(await f.start(q.operationPreviewId)).json()).runId as string;
 // Synthetic source proof for transactional tests, not an operating issuer/profile.
 f.sql.run('CREATE TABLE fixture_lifesteal_proof(valid INTEGER NOT NULL)');f.sql.run('INSERT INTO fixture_lifesteal_proof VALUES(1)');
 f.sql.run('CREATE TABLE fixture_lifesteal_reservations(event_id TEXT PRIMARY KEY)');
 const assertions=()=>[f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) SELECT valid FROM fixture_lifesteal_proof')];
 const health=new CombatHealthStore(f.env.DB,{prepare:async()=>({assertions:assertions()})});
 await health.initialize('a',runId,[{id:'op_eve',hp:actorHp,maxHp:actorMaxHp},{id:'enemy',hp:20000,maxHp:20000}]);
 let calls=0;
 const source:LifestealResolutionSource={prepare:async context=>{calls++;return {source:{nodeId:'fixture-node',actorGeneration:1,targetGeneration:1,parentId:'fixture-parent',sequence:1,
  actorId:'op_eve',targetId:'enemy',hpLoss,appliedEffect:{applicationId:'fixture-application',definitionVersion:'augment.utility-definitions.v7.20261019',definitionHash:'a'.repeat(64)},
  healingForbidden:forbidden,healingMultiplier:{numerator:'1',denominator:'1'}},assertions:[...assertions(),f.env.DB.prepare('INSERT INTO fixture_lifesteal_reservations(event_id) VALUES(?)').bind(context.eventId)]};}};
 return {...f,runId,health,source,calls:()=>calls,resolver:new CombatLifestealResolution(f.env.DB,source)};
}
it.each([1000,10000])('atomically saves %i damage and uncapped 12 percent recovery in the original HP row',async loss=>{
 const f=await fixture(loss);const result=await f.resolver.resolve('a',f.runId,'damage-1');
 expect(result).toMatchObject({hpLoss:loss,actualRecovery:loss*0.12,outcome:'HEALED',healthRevision:3});
 const state=await f.health.read('a',f.runId);
 expect(state.state.targets.find(t=>t.id==='op_eve')?.health.hp).toBe(100+loss*0.12);
 expect(state.state.targets.find(t=>t.id==='enemy')?.health.hp).toBe(20000-loss);
 expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(1);
 expect(f.scalar('SELECT count(*) FROM fixture_lifesteal_reservations')).toBe(1);
 expect(await new CombatLifestealResolution(f.env.DB,{prepare:async()=>{throw Error('must replay');}}).resolve('a',f.runId,'damage-1')).toEqual(result);
 expect(f.calls()).toBe(1);
});
it('accepts v8 original application without changing uncapped recovery or replaying its source',async()=>{
 const f=await fixture(10000);
 const source:LifestealResolutionSource={prepare:async c=>{const p=await f.source.prepare(c);p.source.appliedEffect.definitionVersion='augment.utility-definitions.v8.20261020';return p;}};
 const resolver=new CombatLifestealResolution(f.env.DB,source),receipt=await resolver.resolve('a',f.runId,'v8-original');
 expect(receipt).toMatchObject({hpLoss:10000,actualRecovery:1200,outcome:'HEALED'});
 expect(await resolver.resolve('a',f.runId,'v8-original')).toEqual(receipt);expect(f.calls()).toBe(1);
});
it.each([true,false])('preserves original hit order (outgoing first=%s) without post-down revival',async outgoingFirst=>{
 const f=await fixture(1000,100,false,1000);
 const incoming=async()=>{
  const state=await f.health.read('a',f.runId),hp=state.state.targets.find(t=>t.id==='op_eve')!.health.hp;
  return f.health.commit('a',f.runId,{eventId:'incoming',expectedRevision:state.revision,targetId:'op_eve',change:{kind:'ACTUAL_HP',reason:'DAMAGE',hp:Math.max(0,hp-150)}});
 };
 // Explicit source order, not packet/frame order. Projectile lifetime permission
 // is supplied by the synthetic source; no production lifetime claim is made.
 if(!outgoingFirst)await incoming();
 const result=await f.resolver.resolve('a',f.runId,'outgoing');
 if(outgoingFirst)await incoming();
 expect(result.actualRecovery).toBe(outgoingFirst?120:0);
 expect(result.outcome).toBe(outgoingFirst?'HEALED':'DOWN');
 expect((await f.health.read('a',f.runId)).state.targets.find(t=>t.id==='op_eve')!.health.hp).toBe(outgoingFirst?70:0);
 expect(await f.resolver.resolve('a',f.runId,'outgoing')).toEqual(result);
});
it('does not accumulate fractional recovery across damage events or award shield-only damage',async()=>{
 const f=await fixture(5);
 for(const eventId of ['hit-1','hit-2','hit-3'])expect((await f.resolver.resolve('a',f.runId,eventId)).actualRecovery).toBe(0);
 expect((await f.health.read('a',f.runId)).state.targets.find(t=>t.id==='op_eve')!.health.hp).toBe(100);
 const shieldOnly:LifestealResolutionSource={prepare:async c=>{const p=await f.source.prepare(c);p.source.hpLoss=0;return p;}};
 expect(await new CombatLifestealResolution(f.env.DB,shieldOnly).resolve('a',f.runId,'absorbed')).toMatchObject({actualRecovery:0,outcome:'DAMAGE_ZERO'});
});
it.each([{losses:[500,300,0],total:96},{losses:[100,100,100,100,100],total:60},{losses:[300],total:36}])('commits each confirmed loss separately: $losses',async({losses,total})=>{
 const f=await fixture();let index=0;
 const source:LifestealResolutionSource={prepare:async c=>{const p=await f.source.prepare(c);p.source.hpLoss=losses[index]!;p.source.sequence=index+1;return p;}};
 const resolver=new CombatLifestealResolution(f.env.DB,source),receipts=[];
 for(index=0;index<losses.length;index++)receipts.push(await resolver.resolve('a',f.runId,'child-'+index));
 expect(receipts.reduce((sum,r)=>sum+r.actualRecovery,0)).toBe(total);
 expect((await f.health.read('a',f.runId)).state.targets.find(t=>t.id==='op_eve')!.health.hp).toBe(100+total);
 // Restore earlier original events after later ones: no modifier reread or pay.
 for(index=0;index<losses.length;index++)expect(await f.resolver.resolve('a',f.runId,'child-'+index)).toEqual(receipts[index]);
});
it('rejects a stale prepared hit after an original lethal event commits',async()=>{
 const f=await fixture();let interleaved=false;
 const source:LifestealResolutionSource={prepare:async c=>{
  const p=await f.source.prepare(c);
  if(!interleaved){interleaved=true;await f.health.commit('a',f.runId,{eventId:'lethal',expectedRevision:c.health.revision,targetId:'op_eve',change:{kind:'ACTUAL_HP',reason:'DAMAGE',hp:0}});}
  return p;
 }};
 const resolver=new CombatLifestealResolution(f.env.DB,source);
 await expect(resolver.resolve('a',f.runId,'late-hit')).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM fixture_lifesteal_reservations')).toBe(0);
 expect(await resolver.resolve('a',f.runId,'late-hit')).toMatchObject({actualRecovery:0,outcome:'DOWN'});
});
it('keeps a down-time zero receipt after fixture-authorized return and heals only a new event',async()=>{
 const f=await fixture(1000,0),zero=await f.resolver.resolve('a',f.runId,'while-down');
 expect(zero).toMatchObject({actualRecovery:0,outcome:'DOWN'});
 // Synthetic original-owner authorization models a supported return. This does
 // not install a resurrection ability, generation map or projectile adapter.
 await f.health.commit('a',f.runId,{eventId:'fixture-return',expectedRevision:3,targetId:'op_eve',change:{kind:'ACTUAL_HP',reason:'HEAL',hp:100}});
 expect(await f.resolver.resolve('a',f.runId,'while-down')).toEqual(zero);
 const source:LifestealResolutionSource={prepare:async c=>{const p=await f.source.prepare(c);p.source.hpLoss=500;p.source.sequence=3;return p;}};
 expect(await new CombatLifestealResolution(f.env.DB,source).resolve('a',f.runId,'new-after-return')).toMatchObject({actualRecovery:60});
 expect((await f.health.read('a',f.runId)).state.targets.find(t=>t.id==='op_eve')!.health.hp).toBe(160);
});
it('rolls back both HP changes if any receipt fails and retries the original event',async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER reject_receipt BEFORE INSERT ON combat_damage_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
 await expect(f.resolver.resolve('a',f.runId,'damage-1')).rejects.toThrow();
 expect((await f.health.read('a',f.runId)).revision).toBe(1);
 expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(0);
 f.sql.run('DROP TRIGGER reject_receipt');expect(await f.resolver.resolve('a',f.runId,'damage-1')).toMatchObject({actualRecovery:120});
});
it('recovers an ACK loss with no second heal',async()=>{
 const f=await fixture(),batch=f.env.DB.batch.bind(f.env.DB);let lost=true;
 f.env.DB.batch=async statements=>{const r=await batch(statements);if(lost){lost=false;throw Error('ACK lost');}return r;};
 expect(await f.resolver.resolve('a',f.runId,'damage-1')).toMatchObject({actualRecovery:120});
 expect((await f.health.read('a',f.runId)).revision).toBe(3);
});
it('fences revoked transaction proof and foreign owner without modifying HP',async()=>{
 const f=await fixture();await expect(f.resolver.resolve('other',f.runId,'damage-1')).rejects.toThrow();
 f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run('UPDATE fixture_lifesteal_proof SET valid=0');};
 await expect(f.resolver.resolve('a',f.runId,'damage-1')).rejects.toThrow();expect((await f.health.read('a',f.runId)).revision).toBe(1);
});
it.each([{hp:20000,forbidden:false,reason:'FULL_HP'},{hp:0,forbidden:false,reason:'DOWN'},{hp:100,forbidden:true,reason:'HEAL_FORBIDDEN'}])('persists a zero result for $reason instead of paying it later',async c=>{
 const f=await fixture(1000,c.hp,c.forbidden);const first=await f.resolver.resolve('a',f.runId,'damage-1');
 expect(first).toMatchObject({actualRecovery:0,outcome:c.reason});
 expect(await f.resolver.resolve('a',f.runId,'damage-1')).toEqual(first);expect(f.calls()).toBe(1);
});
it.each([{hp:100,expected:60},{hp:19950,expected:50}])('requires proof and applies modifier then deficit for $hp',async c=>{
 const f=await fixture(1000,c.hp);
 await expect(new CombatLifestealResolution(f.env.DB).resolve('a',f.runId,'damage-1')).rejects.toThrow('source_required');
 const noProof:LifestealResolutionSource={prepare:async context=>({...await f.source.prepare(context),assertions:[]})};
 await expect(new CombatLifestealResolution(f.env.DB,noProof).resolve('a',f.runId,'damage-1')).rejects.toThrow('proof_required');
 const modified:LifestealResolutionSource={prepare:async context=>{const p=await f.source.prepare(context);p.source.healingMultiplier={numerator:'1',denominator:'2'};return p;}};
 expect(await new CombatLifestealResolution(f.env.DB,modified).resolve('a',f.runId,'damage-1')).toMatchObject({actualRecovery:c.expected,outcome:'HEALED'});
});
it('replays a winner committed during source preparation',async()=>{
 const f=await fixture();let winner:unknown;
 const racing:LifestealResolutionSource={prepare:async context=>{winner=await f.resolver.resolve('a',f.runId,'damage-1');return f.source.prepare(context);}};
 expect(await new CombatLifestealResolution(f.env.DB,racing).resolve('a',f.runId,'damage-1')).toEqual(winner);
 expect((await f.health.read('a',f.runId)).revision).toBe(3);
});
it('keeps fractional lifesteal until the final HP boundary and never re-evaluates a replay',async()=>{
 const f=await fixture(5);
 const doubled:LifestealResolutionSource={prepare:async context=>{const p=await f.source.prepare(context);p.source.healingMultiplier={numerator:'2',denominator:'1'};return p;}};
 const result=await new CombatLifestealResolution(f.env.DB,doubled).resolve('a',f.runId,'fractional');
 expect(result.actualRecovery).toBe(1);expect(await f.resolver.resolve('a',f.runId,'fractional')).toEqual(result);
});
it.each(['change','anchor'])('rejects corrupted combined receipt %s without a second HP write',async kind=>{
 const f=await fixture();await f.resolver.resolve('a',f.runId,'damage-1');
 const saved=JSON.parse(String(f.scalar("SELECT response_json FROM combat_damage_receipts WHERE event_id LIKE 'lifesteal:%'")));
 // Isolated corrupt-storage injection; production immutability remains enabled.
 f.sql.run('DROP TRIGGER combat_damage_receipt_immutable_update');
 if(kind==='change')saved.healthReceipts[1].change.hp++;else saved.healthReceipts[1].after.anchorHp++;
 f.sql.run("UPDATE combat_damage_receipts SET response_json=? WHERE event_id LIKE 'lifesteal:%'",[JSON.stringify(saved)]);
 await expect(f.resolver.resolve('a',f.runId,'damage-1')).rejects.toThrow();
 expect((await f.health.read('a',f.runId)).revision).toBe(3);
});
it('rolls back the final combined receipt failure and never publishes partial HP',async()=>{
 const f=await fixture();f.sql.run("CREATE TRIGGER reject_final BEFORE INSERT ON combat_damage_receipts WHEN NEW.event_id LIKE 'lifesteal:%' BEGIN SELECT RAISE(ABORT,'final receipt'); END");
 await expect(f.resolver.resolve('a',f.runId,'damage-1')).rejects.toThrow('final receipt');
 expect((await f.health.read('a',f.runId)).revision).toBe(1);expect(f.scalar('SELECT count(*) FROM combat_damage_receipts')).toBe(0);
});
it('allows only one HP commit for a concurrent duplicate event',async()=>{
 const f=await fixture();const results=await Promise.allSettled([f.resolver.resolve('a',f.runId,'damage-1'),f.resolver.resolve('a',f.runId,'damage-1')]);
 expect(results.some(r=>r.status==='fulfilled')).toBe(true);
 expect((await f.health.read('a',f.runId)).revision).toBe(3);
 expect(await f.resolver.resolve('a',f.runId,'damage-1')).toMatchObject({actualRecovery:120});
});
