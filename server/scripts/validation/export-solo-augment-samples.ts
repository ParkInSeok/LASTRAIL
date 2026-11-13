import {writeFileSync} from 'node:fs';
import {SoloAugmentSelection, type RerollCommand} from '../../apps/game-api/src/solo-augment-selection';

// Explicitly synthetic INTERNAL-domain samples. No HTTP/Store/SSV/DB/START.
const actor={accountId:'TEST_ACCOUNT',environment:'TEST_ONLY'};
const dependencies={drawIndex:()=>0,extraApproval:{authorize:async(target:Parameters<import('../../apps/game-api/src/solo-augment-selection').ExtraApprovalPort['authorize']>[0])=>
  ({target,approvalId:'TEST_APPROVAL',cause:'PURCHASE_ENTITLEMENT' as const})}};
const g=SoloAugmentSelection.issue({...actor,selectionOwner:actor.accountId,runId:'TEST_RUN',eventId:'TEST_EVENT',stageId:'TEST_STAGE',
  catalogVersion:'TEST_CATALOG_ONLY',eligibleIds:['TEST_A','TEST_B','TEST_C','TEST_D','TEST_E']},dependencies);
const samples:Record<string,unknown>={initial:g.read()};
function command(mode:'FREE'|'EXTRA'):RerollCommand {const s=g.read();return {eventId:s.eventId,expectedRevision:s.grantRevision,
  slotIndex:0,candidateId:s.slots[0].candidateId,mode};}
for(let i=0;i<3;i++) {const input=command('FREE');samples[`free${i+1}`]={key:`free-${i}`,input,result:await g.reroll(actor,`free-${i}`,input)};}
const extra=command('EXTRA');samples.extra={key:'extra',input:extra,result:await g.reroll(actor,'extra',extra)};
const current=g.read();const select={eventId:current.eventId,expectedRevision:current.grantRevision,slotIndex:1,candidateId:current.slots[1].candidateId};
samples.selected={key:'selected',input:select,result:g.select(actor,'selected',select)};
const restored=SoloAugmentSelection.restore(g.checkpoint(),dependencies);
samples.oldExtraReplay={result:await restored.reroll(actor,'extra',extra),current:restored.read()};
try {await restored.reroll(actor,'late',command('FREE'));}catch(error){samples.closedError={code:(error as Error).message};}
try {await restored.reroll(actor,'extra',{...extra,slotIndex:1});}catch(error){samples.keyConflictError={code:(error as Error).message};}
const depleted=SoloAugmentSelection.issue({...actor,selectionOwner:actor.accountId,runId:'TEST_RUN',eventId:'TEST_DEPLETED',stageId:'TEST_STAGE_2',
  catalogVersion:'TEST_CATALOG_ONLY',eligibleIds:['TEST_A','TEST_B','TEST_C']},dependencies);
const none={eventId:'TEST_DEPLETED',expectedRevision:0,slotIndex:0,candidateId:'TEST_A',mode:'FREE' as const};
samples.noCandidate={key:'none',input:none,result:await depleted.reroll(actor,'none',none),current:depleted.read()};
writeFileSync('D:/Project/LASTRAIL/QA/AdSkipRerollServer-20260917/internal-domain-fixtures.json',JSON.stringify({
  contract:'solo-augment-selection-v1',scope:'TEST_ONLY_INTERNAL_DOMAIN_NO_HTTP_NO_RUNTIME_ISSUER',samples},null,2)+'\n');
