import {expect,it} from 'vitest';
import {OverflowPulseBudget} from '../../apps/game-api/src/combat-overflow-pulse-budget';
const parent=(actorId='healer-a',attack=1000)=>({runId:'run',nodeId:'node',actorId,actorGeneration:1,parentId:'cast-'+actorId,definitionVersion:'augment.utility-definitions.v8.20261020',applicationId:'owned-'+actorId,attackAtCast:{numerator:attack,denominator:1},o02ApplicationId:null as string|null});
const heal=(eventId:string,effectiveAmount=1500,beforeHp=1000)=>({eventId,targetId:'ally',targetGeneration:1,sourceKind:'ORIGINAL_ACTIVE_DIRECT_HP_HEAL',outcome:'APPLIED',effectiveAmount,beforeHp,maxHp:1000,afterHp:Math.min(1000,beforeHp+effectiveAmount),redistribution:null as null|{eventId:string;actualHeal:number}});
const pulse=(p:ReturnType<typeof parent>,tick:number,heals=[heal('heal-'+tick)])=>{const {attackAtCast,o02ApplicationId,...origin}=p;void attackAtCast;void o02ApplicationId;return {eventId:'pulse-'+tick,parentId:p.parentId,origin,tick,sequence:tick,heals,launch:{kind:'RESERVED',projectileId:'shot-'+p.actorId+'-'+tick,targetId:'enemy',targetGeneration:1} as {kind:string;projectileId?:string;targetId?:string;targetGeneration?:number}};};
it('charges 900/600/0 on pulse allocation even with no target or failed launch; no future excess means no shot',()=>{
 const p=parent(),o=new OverflowPulseBudget(p),one=pulse(p,1);one.launch={kind:'NO_TARGET'};
 const a=o.allocateFromOriginalReceipts(one),two=pulse(p,2);two.launch={kind:'CANNOT_FIRE'};
 const b=o.allocateFromOriginalReceipts(two),c=o.allocateFromOriginalReceipts(pulse(p,3));
 expect([a.allocatedDamage,b.allocatedDamage,c.allocatedDamage]).toEqual([900,600,0]);expect([a.discardedDamage,b.discardedDamage,c.projectile]).toEqual([900,600,null]);
 const fresh=new OverflowPulseBudget(p);fresh.allocateFromOriginalReceipts(one);expect(fresh.allocateFromOriginalReceipts(pulse(p,2,[heal('no-new-excess',0)])).projectile).toBeNull();expect(fresh.snapshot().spent).toBe(900);
});
it('aggregates recipients once per closed parent tick and uses application-time deficit',()=>{
 const p=parent(),o=new OverflowPulseBudget(p);
 const r=o.allocateFromOriginalReceipts(pulse(p,1,[heal('one',1000,900),heal('two',1000,900),heal('three',1000,900)]));
 expect(r.healing.map(h=>h.actualHeal)).toEqual([100,100,100]);expect(r.requestedDamage).toBe(1620);expect(r.projectile?.damageBeforeDefense).toBe(1500);
 expect(()=>o.allocateFromOriginalReceipts({...pulse(p,1),eventId:'another-pulse-same-tick'})).toThrow('pulse_order_conflict');
 const invalid=heal('stale-deficit',1000,900);invalid.afterHp=999;expect(()=>new OverflowPulseBudget(p).allocateFromOriginalReceipts(pulse(p,1,[invalid]))).toThrow('effect_deficit_conflict');
});
it('invalid/down or forbidden healing contributes zero, legitimate retarget uses its actual receipt without a new parent',()=>{
 const p=parent(),o=new OverflowPulseBudget(p),down={...heal('down'),outcome:'INVALID_TARGET',beforeHp:0,afterHp:0},forbidden={...heal('forbidden'),outcome:'HEAL_FORBIDDEN'};
 const r=o.allocateFromOriginalReceipts(pulse(p,1,[down,forbidden]));expect(r.allocatedDamage).toBe(0);expect(r.healing.map(h=>h.actualHeal)).toEqual([0,0]);
 const reselected={...heal('legitimate-original-retarget',1000,900),targetId:'other-ally'};
 expect(o.allocateFromOriginalReceipts(pulse(p,2,[reselected]))).toMatchObject({parentId:p.parentId,allocatedDamage:540,remainingParentBudget:960});
 expect(()=>new OverflowPulseBudget(p).allocateFromOriginalReceipts(pulse(p,1,[{...down,afterHp:100}]))).toThrow('invalid_heal_receipt');
 expect(()=>new OverflowPulseBudget(p).allocateFromOriginalReceipts(pulse(p,1,[{...heal('derived'),sourceKind:'O06_REDISTRIBUTION'}]))).toThrow();
});
it('keeps original healing, O06 and O02 allocations after O23 cap exhaustion and does not refund fired allocation',()=>{
 const p=parent('a',100);p.o02ApplicationId='o02-original';const o=new OverflowPulseBudget(p);
 const a=o.allocateFromOriginalReceipts(pulse(p,1));expect(a.allocatedDamage).toBe(150);expect(a.projectile).not.toBeNull();
 // There is no impact/refund command. Later missing target cannot change spent.
 const h=heal('ongoing-heal',1000,900);h.redistribution={eventId:'o06-positive',actualHeal:300};
 const b=o.allocateFromOriginalReceipts(pulse(p,2,[h]));expect(b.allocatedDamage).toBe(0);expect(b.healing[0]).toMatchObject({actualHeal:100,actualRedistribution:300,remainingExcess:600,o02ShieldRequest:150});expect(o.snapshot().spent).toBe(150);
});
it('restores original budget without recharge, returns duplicate once and rejects changed/reused receipts',()=>{
 const p=parent(),o=new OverflowPulseBudget(p),one=pulse(p,1);const first=o.allocateFromOriginalReceipts(one),snapshot=o.snapshot(),restored=OverflowPulseBudget.restore(p,snapshot);
 expect(restored.allocateFromOriginalReceipts(one)).toEqual({...first,replayed:true});expect(restored.snapshot()).toEqual(snapshot);
 expect(()=>restored.allocateFromOriginalReceipts({...one,launch:{kind:'NO_TARGET'}})).toThrow('event_conflict');
 expect(()=>restored.allocateFromOriginalReceipts(pulse(p,2,[heal('heal-1')]))).toThrow('heal_event_conflict');
 snapshot.spent=0;expect(()=>OverflowPulseBudget.restore(p,snapshot)).toThrow('restore_state_conflict');
 expect(()=>OverflowPulseBudget.restore({...p,actorGeneration:2},o.snapshot())).toThrow('restore_parent_conflict');
});
it.each([false,true])('separate healers consume current original HP receipts in either authority order (reverse=%s)',reverse=>{
 const pa=parent('a'),pb=parent('b',600),a=new OverflowPulseBudget(pa),b=new OverflowPulseBudget(pb);
 const ordered=reverse?[[pb,b],[pa,a]] as const:[[pa,a],[pb,b]] as const;
 const [first,second]=ordered.map(([p,o],i)=>o.allocateFromOriginalReceipts(pulse(p,1,[heal('original-'+p.actorId,800,i===0?500:1000)])));
 expect(first!.healing[0]!.actualHeal).toBe(500);expect(second!.healing[0]!.actualHeal).toBe(0);expect([first!.allocatedDamage,second!.allocatedDamage]).toEqual([180,480]);
 expect(first!.projectile?.projectileId).not.toBe(second!.projectile?.projectileId);expect(first!.origin.actorId).toBe(ordered[0][0].actorId);
 expect(a.snapshot().cap).toBe(1500);expect(b.snapshot().cap).toBe(900);
 const bs=b.snapshot(),ar=OverflowPulseBudget.restore(pa,a.snapshot());ar.allocateFromOriginalReceipts(pulse(pa,2));expect(b.snapshot()).toEqual(bs);
 expect(()=>b.allocateFromOriginalReceipts(pulse(pa,3))).toThrow('parent_conflict');
});
it('stages a candidate checkpoint without mutating live budget for the original authority transaction',()=>{
 const p=parent(),o=new OverflowPulseBudget(p),before=o.snapshot(),prepared=o.prepareFromOriginalReceipts(pulse(p,1));
 expect(o.snapshot()).toEqual(before);expect(prepared.receipt.allocatedDamage).toBe(900);
 const committed=OverflowPulseBudget.restore(p,prepared.checkpoint);expect(committed.allocateFromOriginalReceipts(pulse(p,2)).allocatedDamage).toBe(600);
});
it('splits E800 into shield request200/attack240 with independent cap and no target compensation',()=>{
 const p=parent('split',100);p.o02ApplicationId='original-o02';const o=new OverflowPulseBudget(p),command=pulse(p,1,[heal('split-heal',1000,800)]);
 const r=o.allocateFromOriginalReceipts(command);expect(r.healing[0]).toMatchObject({actualHeal:200,remainingExcess:800,o02ShieldRequest:200});expect(r).toMatchObject({requestedDamage:240,allocatedDamage:150,remainingParentBudget:0});
 expect(r.healing[0]).not.toHaveProperty('actualShield'); // Only original O02 may accept e.g.100 or consume it via O15.
 const saved=o.snapshot(),restored=OverflowPulseBudget.restore(p,saved);
 expect(restored.allocateFromOriginalReceipts(command)).toEqual({...r,replayed:true});expect(restored.snapshot()).toEqual(saved);
 const noTarget=pulse(p,2,[heal('fresh-after-shield-consumption',1000,800)]);noTarget.launch={kind:'NO_TARGET'};
 const next=restored.allocateFromOriginalReceipts(noTarget);expect(next.allocatedDamage).toBe(0);expect(next.healing[0]!.o02ShieldRequest).toBe(200);
 const uncappedParent=parent('split-uncapped');uncappedParent.o02ApplicationId='o02';const missing=pulse(uncappedParent,1,[heal('no-enemy',1000,800)]);missing.launch={kind:'NO_TARGET'};
 const absent=new OverflowPulseBudget(uncappedParent).allocateFromOriginalReceipts(missing);expect(absent).toMatchObject({allocatedDamage:240,discardedDamage:240});expect(absent.healing[0]!.o02ShieldRequest).toBe(200);
});
it.each([[300,500,125,150],[100,700,175,210]])('deducts O06 actual=%i only before O02/O23 split', (actual,remaining,shield,damage)=>{
 const p=parent('redistribution');p.o02ApplicationId='o02';
 // The original O06 owner requested300; its receipt confirms only actual HP.
 const originalO06={requested:300,actual};const h=heal('redistributed-original',1000,800);h.redistribution={eventId:'o06-receipt',actualHeal:originalO06.actual};
 const r=new OverflowPulseBudget(p).allocateFromOriginalReceipts(pulse(p,1,[h]));expect(r.healing[0]).toMatchObject({actualHeal:200,actualRedistribution:actual,remainingExcess:remaining,o02ShieldRequest:shield});expect(r.allocatedDamage).toBe(damage);
});
