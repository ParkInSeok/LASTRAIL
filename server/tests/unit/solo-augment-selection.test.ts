import {describe, expect, it} from 'vitest';
import {SoloAugmentSelection, type ExtraApprovalPort, type ExtraTarget} from '../../apps/game-api/src/solo-augment-selection';

const issue = {accountId:'test-account', environment:'test', runId:'test-run', eventId:'test-event', stageId:'test-stage',
  catalogVersion:'TEST_ONLY', selectionOwner:'test-account', eligibleIds:['test-a','test-b','test-c','test-d','test-e']};
const actor = {accountId:issue.accountId, environment:issue.environment};
const allowed: ExtraApprovalPort = {authorize: async target => ({target, approvalId:'test-approved', cause:'PURCHASE_ENTITLEMENT'})};
const create = (pool=issue.eligibleIds, port=allowed) => SoloAugmentSelection.issue({...issue, eligibleIds:pool}, {drawIndex:()=>0, extraApproval:port});
const command = (g:SoloAugmentSelection, mode:'FREE'|'EXTRA'='FREE', slotIndex=0) => {
  const s=g.read(); return {eventId:s.eventId, expectedRevision:s.grantRevision, slotIndex, candidateId:s.slots[slotIndex].candidateId, mode};
};
async function exhaust(g:SoloAugmentSelection) {for(let i=0;i<3;i++) await g.reroll(actor,`free-${i}`,command(g));}

describe('internal solo augment selection (not a runtime issuer)', () => {
  it.each([1,2])('keeps %i eligible cards unique with original slot budgets and recoverable selection',async count=>{
    let approvals=0;
    const g=create(issue.eligibleIds.slice(0,count),{authorize:async()=>{approvals++;throw Error('unexpected approval');}});
    const before=g.read();expect(before.slots).toHaveLength(count);
    expect(new Set(before.slots.map(s=>s.candidateId)).size).toBe(count);
    expect(before.slots.map(s=>[s.freeRemaining,s.extraRemaining])).toEqual(Array.from({length:count},()=>[3,1]));
    const result=await g.reroll(actor,'no-replacement',command(g));expect(result.outcome).toBe('NO_CANDIDATE');
    expect(g.read()).toEqual(before);expect(approvals).toBe(0);
    const select=command(g);const selected=g.select(actor,'select',select);
    const restored=SoloAugmentSelection.restore(g.checkpoint(),{drawIndex:()=>{throw Error('redraw');},extraApproval:allowed});
    expect(restored.select(actor,'select',select)).toEqual(selected);
    expect(()=>restored.select(actor,'empty',{...select,slotIndex:count})).toThrow();
  });
  it('does not turn empty or missing issuer data into an invented successful node completion',()=>{
    expect(()=>create([])).toThrow();
    expect(()=>SoloAugmentSelection.issue({...issue,eligibleIds:undefined},{extraApproval:allowed})).toThrow();
    const corrupt=JSON.parse(create().checkpoint());corrupt.snapshot.slots.pop();
    expect(()=>SoloAugmentSelection.restore(JSON.stringify(corrupt),{extraApproval:allowed})).toThrow();
  });
  it('issues three distinct candidates with 3+1; grants reset independently', async()=>{
    const g=create(); expect(g.read().slots.map(s=>s.candidateId)).toEqual(['test-a','test-b','test-c']);
    await exhaust(g); expect(g.read().slots[0].freeRemaining).toBe(0);
    expect(create().read().slots.map(s=>[s.freeRemaining,s.extraRemaining])).toEqual([[3,1],[3,1],[3,1]]);
  });
  it('only replaces selected slot, excludes current three, permits a previously discarded card',async()=>{
    const g=create(); await g.reroll(actor,'one',command(g)); expect(g.read().slots.map(s=>s.candidateId)).toEqual(['test-d','test-b','test-c']);
    await g.reroll(actor,'two',command(g)); expect(g.read().slots[0].candidateId).toBe('test-a');
  });
  it('uses a uniform index over eligible IDs without type weighting',()=>{
    const counts=new Map<string,number>();
    for(let i=0;i<5;i++) {
      const g=SoloAugmentSelection.issue(issue,{drawIndex:n=>i%n,extraApproval:allowed});
      const id=g.read().slots[0].candidateId; counts.set(id,(counts.get(id)??0)+1);
    }
    expect([...counts.values()]).toEqual([1,1,1,1,1]);
  });
  it('does not consume or seek approval when no replacement exists',async()=>{
    let calls=0; const g=create(issue.eligibleIds.slice(0,3),{authorize:async()=>{calls++;throw Error('unexpected');}});
    const before=g.read(); expect((await g.reroll(actor,'none',command(g))).outcome).toBe('NO_CANDIDATE');
    expect(g.read()).toEqual(before); expect(calls).toBe(0);
  });
  it('replays original result after later changes and checkpoint reload; rejects changed request',async()=>{
    const g=create(); const c=command(g); const first=await g.reroll(actor,'same',c);
    await g.reroll(actor,'later',command(g));
    const restored=SoloAugmentSelection.restore(g.checkpoint(),{drawIndex:()=>0,extraApproval:allowed});
    expect(await restored.reroll(actor,'same',c)).toEqual(first);
    expect(restored.read()).toEqual(g.read());
    await expect(restored.reroll(actor,'same',{...c,slotIndex:1})).rejects.toThrow('request_conflict');
  });
  it('rejects wrong actor/event, stale revisions and extra before free exhausted',async()=>{
    const g=create(); const c=command(g);
    await expect(g.reroll({...actor,accountId:'other'},'a',c)).rejects.toThrow('owner_mismatch');
    await expect(g.reroll(actor,'b',{...c,eventId:'other'})).rejects.toThrow('event_mismatch');
    await expect(g.reroll(actor,'c',{...c,mode:'EXTRA'})).rejects.toThrow('free_remaining');
    await g.reroll(actor,'d',c); await expect(g.reroll(actor,'e',c)).rejects.toThrow('revision_conflict');
  });
  it('consumes the single shared extra after free and restores approved result without rechecking revoked authority',async()=>{
    let active=true; let calls=0;
    const g=create(issue.eligibleIds,{authorize:async target=>{calls++; if(!active)throw Error('revoked');return {target,approvalId:'proof',cause:'AD_SSV'};}});
    await exhaust(g); const c=command(g,'EXTRA'); const result=await g.reroll(actor,'extra',c); active=false;
    expect(result.snapshot.slots[0]).toMatchObject({freeRemaining:0,extraRemaining:0});
    expect(await g.reroll(actor,'extra',c)).toEqual(result); expect(calls).toBe(1);
    await expect(g.reroll(actor,'another',command(g,'EXTRA'))).rejects.toThrow('extra_exhausted');
  });
  it('approval failure or wrong binding leaves card and counters intact',async()=>{
    for(const port of [{authorize:async()=>{throw Error('revoked');}}, {authorize:async(target:ExtraTarget)=>({target:{...target,eventId:'other'},approvalId:'x',cause:'AD_SSV' as const})}]) {
      const g=create(issue.eligibleIds,port); await exhaust(g); const before=g.read();
      await expect(g.reroll(actor,'extra',command(g,'EXTRA'))).rejects.toThrow(); expect(g.read()).toEqual(before);
    }
  });
  it('restores approved extra and confirmed receipts without new authority calls',async()=>{
    const g=create(); await exhaust(g); const extra=command(g,'EXTRA'); const approved=await g.reroll(actor,'extra',extra);
    const select=command(g); const selected=g.select(actor,'selected',select);
    const restored=SoloAugmentSelection.restore(g.checkpoint(),{drawIndex:()=>{throw Error('no redraw');},
      extraApproval:{authorize:async()=>{throw Error('revoked after approval');}}});
    expect(await restored.reroll(actor,'extra',extra)).toEqual(approved);
    expect(restored.select(actor,'selected',select)).toEqual(selected);
    expect(restored.read().state).toBe('CONFIRMED');
    await expect(restored.reroll(actor,'late',command(restored))).rejects.toThrow('selection_closed');
  });
  it('selection wins over in-flight approval, preventing late reroll',async()=>{
    let release!:()=>void; const wait=new Promise<void>(r=>{release=r;});
    const g=create(issue.eligibleIds,{authorize:async target=>{await wait; return {target,approvalId:'ok',cause:'PURCHASE_ENTITLEMENT'};}});
    await exhaust(g); const c=command(g,'EXTRA'); const pending=g.reroll(actor,'extra',c); await Promise.resolve();
    g.select(actor,'select',{eventId:c.eventId,expectedRevision:c.expectedRevision,slotIndex:1,candidateId:g.read().slots[1].candidateId});
    release(); await expect(pending).rejects.toThrow('selection_closed');
    expect(g.read().slots[0].extraRemaining).toBe(1); expect(g.read().state).toBe('CONFIRMED');
  });
  it('two concurrent approvals consume only once; same request converges',async()=>{
    let calls=0;
    const g=create(issue.eligibleIds,{authorize:async target=>{calls++;return {target,approvalId:'same-proof',cause:'AD_SSV'};}}); await exhaust(g); const c=command(g,'EXTRA');
    const results=await Promise.all([g.reroll(actor,'same',c),g.reroll(actor,'same',c)]);
    expect(results[0]).toEqual(results[1]); expect(g.read().slots[0].extraRemaining).toBe(0);
    expect(calls).toBe(1);
  });
  it('different-key concurrent extras have one winner and one stale rejection',async()=>{
    const g=create(); await exhaust(g); const c=command(g,'EXTRA');
    const results=await Promise.allSettled([g.reroll(actor,'one',c),g.reroll(actor,'two',c)]);
    expect(results.filter(x=>x.status==='fulfilled')).toHaveLength(1);
    expect(results.filter(x=>x.status==='rejected')).toHaveLength(1);
    expect(g.read().slots[0].extraRemaining).toBe(0);
  });
  it('reserves an in-flight key across reroll bodies and selection',async()=>{
    let release!:()=>void; const wait=new Promise<void>(r=>{release=r;});
    const g=create(issue.eligibleIds,{authorize:async target=>{await wait;return {target,approvalId:'proof',cause:'AD_SSV'};}});
    await exhaust(g); const c=command(g,'EXTRA'); const pending=g.reroll(actor,'pending',c);
    expect(()=>g.select(actor,'pending',c)).toThrow('request_conflict');
    await expect(g.reroll(actor,'pending',{...c,slotIndex:1})).rejects.toThrow('request_conflict');
    release(); await pending;
  });
  it('reroll winner invalidates old selection and repeated select is stable',async()=>{
    const g=create(); const c=command(g); await g.reroll(actor,'free',c);
    expect(()=>g.select(actor,'stale',c)).toThrow();
    const select=command(g); const selected=g.select(actor,'select',select);
    expect(g.select(actor,'select',select)).toEqual(selected);
    await expect(g.reroll(actor,'closed',command(g))).rejects.toThrow('selection_closed');
  });
  it('rejects malformed issue/checkpoint/random and does not expose mutable state',()=>{
    expect(()=>create(['same','same','third'])).toThrow();
    expect(()=>SoloAugmentSelection.issue(issue,{drawIndex:()=>NaN,extraApproval:allowed})).toThrow();
    expect(()=>SoloAugmentSelection.restore('{}',{drawIndex:()=>0,extraApproval:allowed})).toThrow();
    const g=create(); const s=g.read(); s.slots[0].freeRemaining=999; expect(g.read().slots[0].freeRemaining).toBe(3);
  });
});
