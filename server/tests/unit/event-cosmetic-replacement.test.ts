import {expect,it} from 'vitest';
import {captureEventCosmeticReplacement,eventCosmeticReplacementVersion,eventCosmeticReplacementBundle,inspectEventReplacementRows,inspectEventReplacementRecovery} from '../../apps/game-api/src/event-cosmetic-replacement';
import {inspectEventRerunOwnership,prepareEventRerunFirstGrant} from '../../apps/game-api/src/event-rerun-ownership';
const rows=()=>({roundId:'synthetic-round',tableVersion:eventCosmeticReplacementVersion,durationDays:14,
 rows:(['OPERATOR_SKIN','TRAIN_APPEARANCE','PROFILE_FRAME','TITLE'] as const).map((kind,i)=>({rewardRowId:'row'+i,cosmeticId:'synthetic-cosmetic'+i,cosmeticKind:kind,conditionRef:'original-condition'+i,componentOfCosmeticId:null}))});
it('exports each approved whole bundle with existing direct growth units and no invented crystal wallet ID',async()=>{
 const d=await captureEventCosmeticReplacement();
 const amounts=Object.fromEntries(d.rows.map(r=>[r.cosmeticKind,Object.fromEntries(r.lines.map(l=>[l.resource,l.quantity]))]));
 expect(amounts).toEqual({
  OPERATOR_SKIN:{FREE_CRYSTAL:1500,cur_credits:10000,cur_tactical_data:250,cur_operator_exp:5000},
  TRAIN_APPEARANCE:{FREE_CRYSTAL:1000,cur_credits:10000,cur_machine_parts:500},
  PROFILE_FRAME:{FREE_CRYSTAL:300,cur_credits:3000,cur_tactical_data:50},
  TITLE:{FREE_CRYSTAL:300,cur_credits:3000,cur_tactical_data:50}
 });
 expect(d.rows[0]!.lines[0]).toMatchObject({kind:'FREE_CRYSTAL',currencyId:null});
 expect(d.rows[0]!.lines[2]).toMatchObject({resource:'cur_tactical_data',currencyId:'TACTICAL_DATA'});
 const hash=d.definitionHash;d.rows[0]!.lines[0]!.quantity=1;
 expect((await captureEventCosmeticReplacement()).definitionHash).toBe(hash);
 expect((await eventCosmeticReplacementBundle(eventCosmeticReplacementVersion,'OPERATOR_SKIN')).lines[0]!.quantity).toBe(1500);
});
it('matches approved combined totals without multiplying for 14/21/28 days',async()=>{
 for(const durationDays of [14,21,28]){
  const all=await inspectEventReplacementRows({...rows(),durationDays});
  expect(all.maximumReplacementTotals).toEqual({FREE_CRYSTAL:3100,cur_credits:26000,cur_tactical_data:350,cur_operator_exp:5000,cur_machine_parts:500});
  expect(all).toMatchObject({sourceAuthorityVerified:false,publicationSupported:false,executeSupported:false,durationMultiplier:1});
 }
 const subset=rows();subset.rows=subset.rows.filter(r=>r.cosmeticKind!=='TRAIN_APPEARANCE');
 expect((await inspectEventReplacementRows(subset)).maximumReplacementTotals).toEqual({FREE_CRYSTAL:2100,cur_credits:16000,cur_tactical_data:350,cur_operator_exp:5000,cur_machine_parts:0});
});
it('rejects duplicate same-cosmetic rows, component splitting and unapproved table versions',async()=>{
 const duplicate=rows();duplicate.rows[1]!.cosmeticId=duplicate.rows[0]!.cosmeticId;
 await expect(inspectEventReplacementRows(duplicate)).rejects.toThrow('duplicate_cosmetic');
 await expect(inspectEventReplacementRows({...rows(),rows:[{...rows().rows[0],componentOfCosmeticId:'parent-cosmetic'}]})).rejects.toThrow('whole_cosmetic_required');
 await expect(inspectEventReplacementRows({...rows(),tableVersion:'future-or-caller-table'})).rejects.toThrow('table_version_unsupported');
 await expect(inspectEventReplacementRows({...rows(),durationDays:42})).rejects.toThrow();
});
async function receipt(){
 const bundle=await eventCosmeticReplacementBundle(eventCosmeticReplacementVersion,'OPERATOR_SKIN');
 return {environment:'DEV',roundId:'synthetic-round',rewardRowId:'row0',accountId:'account',cosmeticId:'synthetic-cosmetic0',
  cosmeticKind:'OPERATOR_SKIN',branch:'REPLACEMENT',tableVersion:bundle.tableVersion,tableHash:bundle.tableHash,originalGrantReceiptRef:'synthetic-original-grant',
  lines:bundle.lines.map((l,i)=>({resource:l.resource,quantity:l.quantity,status:i===0?'SUCCEEDED':i===1?'RESULT_UNKNOWN':i===2?'FAILED_RETRYABLE':'PENDING',originalLineReceiptRef:i===0?'synthetic-wallet-receipt':null}))};
}
it('preserves successful lines and recovers unknown outcomes without reissuing the whole bundle',async()=>{
 const original=await receipt(),view=await inspectEventReplacementRecovery(original);
 expect(view.lines.map(l=>l.nextAction)).toEqual(['NONE','RECOVER_ORIGINAL','RETRY_ORIGINAL_LINE','RETRY_ORIGINAL_LINE']);
 expect(view).toMatchObject({branch:'REPLACEMENT',allLinesReportedSucceeded:false,executeSupported:false,sourceAuthorityVerified:false,receiptWritten:false});
 expect(await inspectEventReplacementRecovery(original)).toEqual(view);
 const final=structuredClone(original);final.lines[2]!.status='FAILED_FINAL';
 const next=await inspectEventReplacementRecovery(final);
 expect(next.lines[2]!.nextAction).toBe('REQUIRES_ORIGINAL_OWNER');
 expect(next.rightIdentity).toBe(view.rightIdentity);expect(next.lines[2]!.lineIdentity).toBe(view.lines[2]!.lineIdentity);
});
it('rejects missing/duplicated/changed bundle lines and unverified success-shaped data',async()=>{
 const original=await receipt();
 await expect(inspectEventReplacementRecovery({...original,lines:original.lines.slice(1)})).rejects.toThrow('whole_bundle_required');
 await expect(inspectEventReplacementRecovery({...original,lines:[original.lines[0],original.lines[0],...original.lines.slice(2)]})).rejects.toThrow('whole_bundle_required');
 const wrong=structuredClone(original);wrong.lines[0]!.quantity++;await expect(inspectEventReplacementRecovery(wrong)).rejects.toThrow('line_conflict');
 const missing=structuredClone(original);missing.lines[0]!.originalLineReceiptRef=null;await expect(inspectEventReplacementRecovery(missing)).rejects.toThrow('successful_line_receipt_required');
 await expect(inspectEventReplacementRecovery({...original,tableHash:'b'.repeat(64)})).rejects.toThrow('table_hash_conflict');
 await expect(inspectEventReplacementRecovery({...original,branch:'COSMETIC'})).rejects.toThrow();
 await expect(inspectEventReplacementRecovery({...original,currentOwnership:'UNOWNED'})).rejects.toThrow();
});
it('binds approved replacement preview to rerun policy while original first-grant producer remains unavailable',async()=>{
 const input={environment:'DEV',roundId:'new',previousRoundId:'old',rewardRowId:'skin',accountId:'a',cosmeticId:'synthetic-skin',
  definitionVersion:'definition',definitionHash:'a'.repeat(64),replacementTableVersion:eventCosmeticReplacementVersion,cosmeticKind:'OPERATOR_SKIN',
  ownership:{accountId:'a',cosmeticId:'synthetic-skin',state:'OWNED'},originalRequestState:'NOT_OBSERVED'};
 const result=await inspectEventRerunOwnership(input);
 expect(result).toMatchObject({branchPreview:'REPLACEMENT',replacementPolicyReady:true,publicationSupported:false,executeSupported:false,sourceAuthorityVerified:false,
  replacementPreview:{tableVersion:eventCosmeticReplacementVersion,lines:[{quantity:1500},{quantity:10000},{quantity:250},{quantity:5000}]}});
 expect(result.missingSources).not.toContain('APPROVED_EXACT_REPLACEMENT_TABLE');
 await expect(prepareEventRerunFirstGrant(input)).rejects.toMatchObject({status:503,code:'event_rerun_original_grant_owner_unavailable'});
 expect((await inspectEventRerunOwnership({...input,originalRequestState:'RESULT_UNKNOWN'})).branchPreview).toBe('RECOVER_ORIGINAL');
});
