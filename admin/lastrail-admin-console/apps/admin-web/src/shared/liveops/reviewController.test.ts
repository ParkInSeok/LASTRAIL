import {describe,it,expect} from 'vitest';
import {parseOptionalKst} from './reviewInput';
import {EventDraftSelection} from './EventDraftSelection';
import {EventDraftModel} from './eventDraft/eventDraftModel';import {EventScheduleDraft} from './eventScheduleDraft';import {eventDefinitionToLocal} from './eventDraft/eventDraftAdapter';import {definition,saved} from './eventDraft/eventDraft.fixture';
function fixture(){let resolveA:(value:unknown)=>void=()=>{};const port={list:async()=>({}),read:(id:string)=>id==='A'?new Promise<unknown>(r=>{resolveA=r;}):Promise.resolve(saved(1,id)),save:async()=>({})};const model=new EventDraftModel({principalId:'actor',environment:'DEV',authorizationGeneration:'g'},['EVENT_CONFIG_WRITE'],port);const editor=new EventScheduleDraft('scope',eventDefinitionToLocal(definition()));let invalidations=0;const selection=new EventDraftSelection(model,editor,()=>{invalidations++;},editor.snapshot().definition);return {selection,model,editor,resolveA:()=>resolveA(saved(1,'A')),invalidations:()=>invalidations};}
describe('review controller boundaries',()=>{
 it('blank KST is absent while malformed nonempty KST is rejected',()=>{expect(parseOptionalKst('')).toBeNull();expect(()=>parseOptionalKst('wrong')).toThrow();});
 it('late round A cannot replace the already selected B editor',async()=>{const f=fixture(),a=f.selection.open('A');await f.selection.open('B');f.resolveA();await a;expect(f.editor.snapshot().definition.roundId).toBe('B');f.selection.dispose();f.model.dispose();});
 it('new current revision invalidates previous outage preview and disposal prevents late editor reset',async()=>{const f=fixture(),a=f.selection.open('A');f.selection.dispose();f.resolveA();await a;expect(f.editor.snapshot().definition.roundId).not.toBe('A');expect(f.invalidations()).toBeGreaterThan(0);f.model.dispose();});
});
