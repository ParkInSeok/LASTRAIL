import {expect,it} from 'vitest';
import {inspectEventRerunOwnership,prepareEventRerunFirstGrant,type EventRerunObservation} from '../../apps/game-api/src/event-rerun-ownership';
const observation=():EventRerunObservation=>({environment:'DEV',roundId:'new-round',previousRoundId:'old-round',rewardRowId:'skin-stage',
 accountId:'account',cosmeticId:'actual-skin-id',definitionVersion:'definition',definitionHash:'a'.repeat(64),
 replacementTableVersion:null,ownership:{accountId:'account',cosmeticId:'actual-skin-id',state:'UNOWNED'},
 originalRequestState:'NOT_OBSERVED'});
it('separates round-specific rights from permanent cosmetic identity without approving a first grant',async()=>{
 const input=observation(),result=await inspectEventRerunOwnership(input);
 expect(result).toMatchObject({branchPreview:'COSMETIC',executeSupported:false,applied:false,sourceAuthorityVerified:false,
  replacementPolicyReady:false,progressCopyAllowed:false,accountFirstClearRegrantAllowed:false});
 expect(result.missingSources).not.toContain('APPROVED_EXACT_REPLACEMENT_TABLE');
 expect(result).toMatchObject({replacementDefinitionAvailable:true,approvedReplacementTableVersion:'event.cosmetic-replacement.v1.20261020'});
 const newer=await inspectEventRerunOwnership({...input,roundId:'third-round'});
 expect(newer.proposedRightIdentity).not.toBe(result.proposedRightIdentity);
 expect(newer.permanentCosmeticIdentity).toBe(result.permanentCosmeticIdentity);
 const changed=await inspectEventRerunOwnership({...input,definitionVersion:'changed',cosmeticId:'another-skin',
  ownership:{...input.ownership,cosmeticId:'another-skin'}});
 expect(changed.proposedRightIdentity).toBe(result.proposedRightIdentity);
 expect(changed.permanentCosmeticIdentity).not.toBe(result.permanentCosmeticIdentity);
 await expect(inspectEventRerunOwnership({...input,previousRoundId:input.roundId})).rejects.toThrow();
});
it('treats a permanent skin right as owned independently of whether the operator is unlocked',async()=>{
 const input=observation();input.ownership.state='OWNED';
 const result=await inspectEventRerunOwnership(input);
 expect(result.branchPreview).toBe('REPLACEMENT');
 expect(result).toMatchObject({replacementPolicyReady:false,executeSupported:false});
 expect(result.requirements).toContain('PERMANENT_SKIN_RIGHT_COUNTS_WITHOUT_OPERATOR_UNLOCK');
});
it('never branches on an unknown original outcome and rejects cross-account or cosmetic observations',async()=>{
 const input=observation();
 expect((await inspectEventRerunOwnership({...input,originalRequestState:'RESULT_UNKNOWN'})).branchPreview).toBe('RECOVER_ORIGINAL');
 expect((await inspectEventRerunOwnership({...input,ownership:{...input.ownership,state:'RESULT_UNKNOWN'}})).branchPreview).toBe('RECOVER_ORIGINAL');
 expect((await inspectEventRerunOwnership({...input,ownership:{...input.ownership,state:'UNKNOWN'}})).branchPreview).toBe('UNDETERMINED');
 await expect(inspectEventRerunOwnership({...input,ownership:{...input.ownership,accountId:'other'}})).rejects.toThrow();
});
it('fails closed at the first-grant producer boundary, including when caller supplies a table version',async()=>{
 const input={...observation(),replacementTableVersion:'caller-supplied'};
 const result=await inspectEventRerunOwnership(input);
 expect(result.replacementPolicyReady).toBe(false);
 await expect(prepareEventRerunFirstGrant(input)).rejects.toMatchObject({status:503,code:'event_rerun_original_grant_owner_unavailable'});
 expect(result.requirements).toContain('FREEZE_BRANCH_AND_TABLE_IN_ORIGINAL_RECEIPT_REPLAY_WITHOUT_RESELECTION');
});
