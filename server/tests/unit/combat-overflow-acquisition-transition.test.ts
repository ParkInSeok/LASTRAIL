import {expect,it} from 'vitest';
import {OverflowPulseBudget} from '../../apps/game-api/src/combat-overflow-pulse-budget';

// Boundary fixtures supply an independently APPLIED original parent. They do not
// simulate SELECT becoming APPLIED, an HP owner, or a live projectile/shield owner.
const parent=(parentId='cast-before',o02ApplicationId:string|null=null)=>({
 runId:'run',nodeId:'node',actorId:'healer',actorGeneration:1,parentId,
 definitionVersion:'augment.utility-definitions.v8.20261020',applicationId:'o23-original',
 attackAtCast:{numerator:1000,denominator:1},o02ApplicationId
});
const pulse=(p:ReturnType<typeof parent>,tick:number,effectiveAmount=500,beforeHp=1000,targetId='ally')=>{
 const {attackAtCast,o02ApplicationId,...origin}=p;void attackAtCast;void o02ApplicationId;
 return {eventId:`pulse-${tick}`,parentId:p.parentId,origin,tick,sequence:tick,
  heals:[{eventId:`heal-${tick}`,targetId,targetGeneration:1,sourceKind:'ORIGINAL_SKILL_HEAL_OVER_TIME_TICK',
   outcome:'APPLIED',effectiveAmount,beforeHp,maxHp:1000,afterHp:Math.min(1000,beforeHp+effectiveAmount),redistribution:null}],
  launch:{kind:'RESERVED',projectileId:`projectile-${p.parentId}-${tick}`,targetId:'enemy',targetGeneration:1}};
};

it('keeps an ongoing parent at 60% after O02 acquisition and uses 25%/30% only for a new parent',()=>{
 const prior=parent(),ongoing=new OverflowPulseBudget(prior);
 const fired=ongoing.allocateFromOriginalReceipts(pulse(prior,1));
 const checkpoint=ongoing.snapshot(),firedOriginal=structuredClone(fired);
 const acquired=parent('cast-after','o02-applied');
 const next=new OverflowPulseBudget(acquired).allocateFromOriginalReceipts(pulse(acquired,1));
 expect(next.healing[0]?.o02ShieldRequest).toBe(125);
 expect(next.allocatedDamage).toBe(150);
 const restored=OverflowPulseBudget.restore(prior,checkpoint);
 const continued=restored.allocateFromOriginalReceipts(pulse(prior,2));
 expect(continued.allocatedDamage).toBe(300);
 expect(continued.healing[0]?.o02ShieldRequest).toBe(0);
 expect(continued.remainingParentBudget).toBe(900);
 expect(fired).toEqual(firedOriginal);
 expect(restored.snapshot().journal[0]?.receipt.projectile).toEqual(fired.projectile);
});

it('rejects rebinding a saved parent to newly acquired O02, application, or attack pins',()=>{
 const prior=parent(),owner=new OverflowPulseBudget(prior);
 owner.allocateFromOriginalReceipts(pulse(prior,1));const saved=owner.snapshot();
 for(const changed of [{...prior,o02ApplicationId:'o02-new'},
  {...prior,applicationId:'o23-new'},{...prior,attackAtCast:{numerator:2000,denominator:1}}]){
  expect(()=>OverflowPulseBudget.restore(changed,saved)).toThrow('restore_parent_conflict');
 }
 expect(OverflowPulseBudget.restore(prior,saved).snapshot()).toEqual(saved);
});

it('preserves original definition while consuming new recipient deficits and effective heal amounts each tick',()=>{
 const prior=parent(),owner=new OverflowPulseBudget(prior);
 expect(owner.allocateFromOriginalReceipts(pulse(prior,1)).allocatedDamage).toBe(300);
 const changedRecipient=owner.allocateFromOriginalReceipts(pulse(prior,2,800,500,'another-ally'));
 expect(changedRecipient.healing[0]).toMatchObject({targetId:'another-ally',actualHeal:500,remainingExcess:300});
 expect(changedRecipient.allocatedDamage).toBe(180);
 const reducedHeal=owner.allocateFromOriginalReceipts(pulse(prior,3,200,900));
 expect(reducedHeal.healing[0]?.actualHeal).toBe(100);
 expect(reducedHeal.allocatedDamage).toBe(60);
 expect(reducedHeal.remainingParentBudget).toBe(960);
});

it('does not commit a staged new pulse or recharge a restored parent on duplicate recovery',()=>{
 const prior=parent(),owner=new OverflowPulseBudget(prior),first=pulse(prior,1);
 owner.allocateFromOriginalReceipts(first);const committed=owner.snapshot();
 const staged=owner.prepareFromOriginalReceipts(pulse(prior,2));
 expect(owner.snapshot()).toEqual(committed);
 const recovered=OverflowPulseBudget.restore(prior,committed);
 expect(recovered.allocateFromOriginalReceipts(first)).toMatchObject({replayed:true,allocatedDamage:300,remainingParentBudget:1200});
 expect(recovered.snapshot()).toEqual(committed);
 expect(OverflowPulseBudget.restore(prior,staged.checkpoint).snapshot().spent).toBe(600);
});
