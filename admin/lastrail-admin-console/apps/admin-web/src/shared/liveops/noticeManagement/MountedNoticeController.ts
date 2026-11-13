import {newNoticeDocument,noticeIdSchema,noticeCommandSchema,type NoticeDocument,type NoticeAction} from "../../operations/noticeContract";
import {liveOpsNoticeCommandSchema,type LiveOpsNoticeCommand} from "./noticeManagementContract";
import type {NoticeManagement} from "./NoticeManagement";
type Prepared={id:string;input:LiveOpsNoticeCommand;key:string;action:NoticeAction|"REVIEW"};
export class MountedNoticeController{
 private id:string;private draft:NoticeDocument|null=null;private base:number|null=null;private reason="";private message="";private prepared:Prepared|null=null;private disposed=false;private listeners=new Set<()=>void>();private unbind:()=>void=()=>{};private connected=false;
 private view!:ReturnType<MountedNoticeController["project"]>;
 constructor(private model:NoticeManagement,private selected:string|null,private makeKey:()=>string=()=>crypto.randomUUID(),newId?:string){this.id=selected??newId??"notice-"+crypto.randomUUID();this.view=this.project();}
 connect(){if(this.disposed||this.connected)return;this.connected=true;this.unbind=this.model.subscribe(()=>{if(this.model.snapshot().phase!=="active"){this.draft=null;this.reason="";this.prepared=null;this.message="";}this.emit();});this.emit();}
 disconnect(){this.connected=false;this.unbind();this.unbind=()=>{};}
 private project(){const source=this.model.snapshot(),active=!this.disposed&&source.phase==="active",current=source.detail?.notice.noticeId===this.id?source.detail:null,busy=source.write==="pending"||source.write==="outcomeUnknown",caps=source.capabilities?.domains.NOTICE,document=active?(this.draft??current?.notice.document??newNoticeDocument()):newNoticeDocument(),dirty=!!this.draft&&JSON.stringify(this.draft)!==JSON.stringify(current?.notice.document),conflict=this.draft!==null&&this.base!==(current?.notice.revision??null),healthy=!!current||this.selected===null&&!!source.list;
 return {active,id:active?this.id:"",document,reason:active?this.reason:"",message:this.message,prepared:active?this.prepared:null,current:active?current:null,exists:!!current||this.selected!==null,busy,dirty,conflict,reviewed:!!current?.review&&current.review.reviewedRevision===current.notice.revision,canEdit:active&&this.model.environment==="DEV"&&healthy&&!busy&&!!caps?.canEdit,canReview:active&&this.model.environment==="DEV"&&!!current&&!busy&&!dirty&&!!caps?.canReview,canExecute:active&&this.model.environment==="DEV"&&!!current?.review&&!busy&&!dirty&&!!caps?.canExecute};}
 snapshot=()=>this.view;subscribe=(fn:()=>void)=>{this.listeners.add(fn);return()=>{this.listeners.delete(fn);};};
 private emit(){if(this.disposed)return;this.view=this.project();for(const fn of this.listeners)fn();}
 change(document:NoticeDocument){if(!this.view.canEdit)return;if(this.draft===null)this.base=this.view.current?.notice.revision??null;this.draft=structuredClone(document);this.prepared=null;this.message="";this.emit();}
 changeId(id:string){if(!this.view.canEdit||this.view.exists)return;this.id=id;this.prepared=null;this.emit();}
 changeReason(reason:string){if(!this.view.active||this.view.busy)return;this.reason=reason;this.prepared=null;this.emit();}
 resetDraft(){if(this.view.busy)return;this.draft=null;this.base=null;this.prepared=null;this.emit();}
 prepare(action:NoticeAction|"REVIEW"){
  this.prepared=null;this.message="";const view=this.project(),revision=view.current?.notice.revision??null;
  try{if(!view.active||view.busy)throw new Error("notice_session_not_ready");if(action==="SAVE_DRAFT"?!view.canEdit:action==="REVIEW"?!view.canReview:!view.canExecute)throw new Error("server_capability_denied");if(view.conflict)throw new Error("current_revision_required");noticeIdSchema.parse(this.id);
   const input=action==="REVIEW"?{kind:"REVIEW" as const,expectedRevision:revision,reason:this.reason}:{kind:"DOMAIN_COMMAND" as const,reviewId:action==="SAVE_DRAFT"?null:view.current?.review?.reviewId,command:noticeCommandSchema.parse({environment:this.model.environment,command:action,expectedRevision:revision,reason:this.reason,...(action==="SAVE_DRAFT"?{document:view.document}:{})})};
   this.prepared={id:this.id,input:liveOpsNoticeCommandSchema.parse(input),key:this.makeKey(),action};
  }catch(error){this.message=error instanceof Error?error.message:"notice_input_invalid";}this.emit();
 }
 cancel(){this.prepared=null;this.emit();}
 async confirm(){const prepared=this.prepared;if(!prepared||this.view.busy||!this.view.active)return;this.prepared=null;this.emit();try{await this.model.execute(prepared.id,prepared.input,prepared.key);if(this.disposed||!this.connected)return;if(this.model.snapshot().write==="confirmed"){if(prepared.action==="SAVE_DRAFT"){this.draft=null;this.base=null;}await this.model.open(prepared.id);this.message="처리 완료 · 현재 변경 번호를 응답으로 반영했습니다.";}else this.message=this.model.snapshot().write==="outcomeUnknown"?"이전 요청 결과를 같은 요청으로 확인해 주세요.":"요청이 확정되지 않았습니다. 현재 상태를 확인하세요.";}catch(error){this.message=error instanceof Error?error.message:"notice_request_failed";}this.emit();}
 async recover(){if(!this.view.active||this.model.snapshot().write!=="outcomeUnknown")return;await this.model.recover();if(this.disposed||!this.connected)return;if(this.model.snapshot().write==="confirmed"){this.draft=null;this.base=null;await this.model.open(this.id);}this.emit();}
 async reload(){if(this.view.busy||!this.view.active)return;this.prepared=null;await this.model.open(this.id,true);this.emit();}
 dispose(){if(this.disposed)return;this.disposed=true;this.disconnect();this.listeners.clear();this.draft=null;this.reason="";this.prepared=null;this.view=this.project();}
}
