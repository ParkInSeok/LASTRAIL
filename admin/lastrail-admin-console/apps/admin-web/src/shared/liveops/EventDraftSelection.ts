import type {EventDraftModel} from './eventDraft/eventDraftModel';import type {EventScheduleDraft,EventDefinition} from './eventScheduleDraft';import {eventDefinitionToLocal} from './eventDraft/eventDraftAdapter';
/** Selection lifetime only. Current saved state stays with EventDraftModel; editable input stays with EventScheduleDraft. */
export class EventDraftSelection {
 private epoch=0;private disposed=false;private off:()=>void;private source='';
 constructor(private model:EventDraftModel,private editor:EventScheduleDraft,private invalidate:()=>void,private initial:EventDefinition){this.off=model.subscribe(()=>{const s=model.snapshot(),identity=JSON.stringify([s.roundId,s.current?.revision,s.current?.definitionHash,s.phase==='unauthorized']);if(identity!==this.source){this.source=identity;this.invalidate();}});}
 async open(id:string,refresh=false){if(this.disposed)return;const epoch=++this.epoch,editingRevision=this.editor.snapshot().revision;this.invalidate();const saved=await this.model.open(id,refresh);if(this.disposed||epoch!==this.epoch)return;const state=this.model.snapshot();if(state.roundId!==id||['unauthorized','disposed'].includes(state.phase)||this.editor.snapshot().revision!==editingRevision)return;if(saved&&state.current?.revision===saved.revision&&state.current.definitionHash===saved.definitionHash)this.editor.reset(this.editor.snapshot().scope,eventDefinitionToLocal(saved.definition));else if(!saved&&!state.current)this.editor.reset(this.editor.snapshot().scope,{...this.initial,roundId:id});}
 dispose(){this.disposed=true;this.epoch++;this.off();}
}
