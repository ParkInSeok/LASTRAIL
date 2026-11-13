import {expect,it} from 'vitest';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {SoloAugmentSelection} from '../../apps/game-api/src/solo-augment-selection';
import {captureCombatAuthority} from '../../apps/game-api/src/combat-authority';
import {plan} from '../helpers/operation-map-fixture';
import {projectAugmentReconnectState,type AugmentReconnectInput} from '../../apps/game-api/src/augment-reconnect-state';
async function fixture(confirmed=false,candidate='T05'){
 const map=plan();map.enabledEventKinds.push('AUGMENT');map.nodes[1]!.eventKind='AUGMENT';
 const startSnapshot=JSON.stringify({encounterType:'ORDINARY_12_NODE',admission:{combatAuthority:captureCombatAuthority('ORDINARY_SOLO'),operationMapPlan:map}});
 const issue={accountId:'a',environment:'DEV',runId:'run',eventId:'grant',stageId:'n1-1',catalogVersion:'original-fixture',selectionOwner:'a',eligibleIds:[candidate,'O08','T16','O06']};
 const owner=SoloAugmentSelection.issue(issue,{drawIndex:()=>0,extraApproval:{authorize:async()=>{throw Error('no approval');}}});
 const initialJson=owner.checkpoint();
 await owner.reroll({accountId:'a',environment:'DEV'},'reroll',{eventId:'grant',expectedRevision:0,slotIndex:2,candidateId:'T16',mode:'FREE'});
 const command={eventId:'grant',expectedRevision:1,slotIndex:0,candidateId:candidate};
 const result=confirmed?owner.select({accountId:'a',environment:'DEV'},'select',command):null;
 const input:AugmentReconnectInput={accountId:'a',environment:'DEV',runId:'run',runStatus:'ACTIVE',startSnapshot,
  map:{revision:3,planJson:JSON.stringify(map),stateJson:JSON.stringify({version:'operation.map-progress.v1',traversedEdgeIds:['entry:n1-1'],pendingEdgeId:null,phase:'ACTIVE',holdIds:[]}),startSnapshot},
  grant:{accountId:'a',environment:'DEV',runId:'run',eventId:'grant',stageId:'n1-1',checkpointJson:owner.checkpoint(),initialJson,issueHash:await sha256Hex(canonicalJson(issue)),startSnapshot,writeRevision:confirmed?3:2},
  receipt:result?{requestId:'select',requestHash:await sha256Hex(canonicalJson({actor:{accountId:'a',environment:'DEV'},action:{operation:'SELECT',command}})),responseJson:JSON.stringify(result)}:null,
  applicationOriginal:null};
 return input;
}
it('restores original candidates and reroll counts without claiming no pending local request',async()=>{
 const result=await projectAugmentReconnectState(await fixture());
 expect(result.state).toBe('SELECTING');
 expect(result.selection?.slots[2]).toMatchObject({candidateId:'O06',freeRemaining:2});
 expect(result).toMatchObject({localCheckpointStatus:'NOT_OBSERVED',pendingOriginalRequestMustBeRecovered:true,releaseSelectionHold:false});
});
it('returns the original confirmed receipt for the local checkpoint owner, not APPLIED or combat resume',async()=>{
 const input=await fixture(true),result=await projectAugmentReconnectState(input);
 expect(result.state).toBe('CONFIRMED_RUNTIME_CHECKPOINT_REQUIRED');
 expect(result.selectionReceipt).toEqual(JSON.parse(input.receipt!.responseJson));
 expect(result.releaseSelectionHold).toBe(false);
});
it.each(['account','environment','start','receipt','hash'])('refuses mixed %s originals',async kind=>{
 const input=await fixture(true);
 if(kind==='account')input.grant!.accountId='other';
 if(kind==='environment')input.grant!.environment='PROD';
 if(kind==='start')input.grant!.startSnapshot='{}';
 if(kind==='receipt')input.receipt!.requestId='other';
 if(kind==='hash')input.receipt!.requestHash='0'.repeat(64);
 expect((await projectAugmentReconnectState(input)).state).toBe('REVIEW_REQUIRED');
});
it('distinguishes missing current grant from unavailable map and never infers command rejection',async()=>{
 const input=await fixture();input.grant=null;
 expect((await projectAugmentReconnectState(input)).state).toBe('NO_CURRENT_GRANT');
 input.map=null;
 expect((await projectAugmentReconnectState(input)).state).toBe('UNKNOWN');
});
it('does not reopen selection after node completion or terminal run',async()=>{
 const input=await fixture();const state=JSON.parse(input.map!.stateJson);state.phase='READY';input.map!.stateJson=JSON.stringify(state);
 expect(await projectAugmentReconnectState(input)).toMatchObject({state:'NODE_EXITED',selection:null});
 input.runStatus='COMPLETED';
 expect(await projectAugmentReconnectState(input)).toMatchObject({state:'TERMINAL',selection:null});
});
it('does not return an O22 confirmation as usable when its immutable application original is missing',async()=>{
 expect((await projectAugmentReconnectState(await fixture(true,'O22'))).state).toBe('REVIEW_REQUIRED');
});
it.each(['ORDINARY_COOP','WORLD_BOSS_SOLO','WORLD_BOSS_COOP'] as const)('does not apply solo recovery to %s',async mode=>{
 const input=await fixture(true,'O22'),start=JSON.parse(input.startSnapshot!);
 start.admission.combatAuthority=captureCombatAuthority(mode);input.startSnapshot=JSON.stringify(start);
 expect(await projectAugmentReconnectState(input)).toMatchObject({state:'UNKNOWN',reason:'MODE_NOT_APPLICABLE',selection:null,map:null});
});
it('rejects malformed authority instead of interpreting it as an unsupported legitimate mode',async()=>{
 const input=await fixture(),start=JSON.parse(input.startSnapshot!);
 start.admission.combatAuthority={mode:'ORDINARY_SOLO'};input.startSnapshot=JSON.stringify(start);
 expect(await projectAugmentReconnectState(input)).toMatchObject({state:'REVIEW_REQUIRED',reason:'AUTHORITY_INVALID',map:null});
});
it('returns only validated map navigation and holds for active or exited nodes',async()=>{
 const input=await fixture(),state=JSON.parse(input.map!.stateJson);
 input.map!.stateJson=JSON.stringify(state);
 expect(await projectAugmentReconnectState(input)).toMatchObject({map:{phase:'ACTIVE',currentNodeId:'n1-1',holdIds:state.holdIds}});
 state.phase='READY';state.holdIds=['pending-reward','other-transaction'];input.map!.stateJson=JSON.stringify(state);
 expect(await projectAugmentReconnectState(input)).toMatchObject({state:'NODE_EXITED',map:{phase:'READY',currentNodeId:'n1-1',holdIds:state.holdIds}});
 input.map!.startSnapshot='mismatch';
 expect(await projectAugmentReconnectState(input)).toMatchObject({state:'REVIEW_REQUIRED',map:null});
});
