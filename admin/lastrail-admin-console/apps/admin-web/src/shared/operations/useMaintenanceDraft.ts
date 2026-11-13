import {useEffect,useRef,useState} from "react";
import {useQuery,useQueryClient} from "@tanstack/react-query";
import {adminApi,AdminApiError} from "../api/client";
import {maintenanceIdSchema,maintenanceSaveSchema,newMaintenanceDocument,type MaintenanceDocument,type MaintenanceEnvironment,type MaintenanceSave} from "./maintenanceContract";
type Attempt={id:string;key:string;body:MaintenanceSave};
type SaveState={kind:"idle"}|{kind:"pending"|"uncertain";attempt:Attempt}|{kind:"saved";id:string;revision:number};
export function useMaintenanceDraft(operator:string,environment:MaintenanceEnvironment,selected:string|null,onSaved:(id:string)=>void){
 const client=useQueryClient(),alive=useRef(false),reviewRef=useRef<Attempt|null>(null);
 const commandKey=["maintenance-save",operator,environment];
 const command=useQuery<SaveState>({queryKey:commandKey,queryFn:async()=>({kind:"idle"}),enabled:false,initialData:{kind:"idle"},gcTime:Infinity}).data;
 const state=()=>client.getQueryData<SaveState>(commandKey)??{kind:"idle"} as SaveState;
 const setCommand=(value:SaveState)=>client.setQueryData(commandKey,value);
 const [id,setId]=useState(selected??((command.kind==="pending"||command.kind==="uncertain")?command.attempt.id:`maintenance-${crypto.randomUUID()}`));
 const exists=!!selected||!!client.getQueryData(["maintenance-plan",operator,environment,id])||(command.kind==="saved"&&command.id===id);
 const detail=useQuery({queryKey:["maintenance-plan",operator,environment,id],queryFn:()=>adminApi.maintenancePlan(environment,id),enabled:exists,retry:false,refetchOnWindowFocus:false});
 const access=useQuery({queryKey:["maintenance-draft-access",operator,environment],queryFn:()=>adminApi.maintenancePlans(environment),retry:false,refetchOnWindowFocus:false});
 const [draft,setDraft]=useState<{document:MaintenanceDocument;revision:number|null}|null>(null),[reason,setReason]=useState(""),[review,setReview]=useState<Attempt|null>(null),[message,setMessage]=useState("");
 const empty=useRef(newMaintenanceDocument());
 const document=draft?.document??detail.data?.plan.document??empty.current;
 const busy=command.kind==="pending"||command.kind==="uncertain";
 const healthy=exists?!!detail.data&&!detail.isError&&!detail.isFetching:!!access.data&&!access.isError&&!access.isFetching;
 const permission=exists?detail.data?.permissions.canSaveDraft:access.data?.permissions.canSaveDraft;
 const canWrite=environment==="DEV"&&healthy&&!!permission&&!busy;
 const conflict=!!draft&&draft.revision!==(detail.data?.plan.draftRevision??null);
 function clearReview(){reviewRef.current=null;setReview(null);}
 function change(document:MaintenanceDocument){if(!canWrite)return;clearReview();setDraft({document,revision:draft?draft.revision:detail.data?.plan.draftRevision??null});setMessage("");}
 function changeReason(value:string){clearReview();setReason(value);}
 function changeId(value:string){if(exists||busy)return;clearReview();setId(value);}
 function reset(){if(!canWrite)return;clearReview();setDraft(null);setMessage("");}
 async function reload(){if(busy)return;clearReview();if(exists)await detail.refetch();else await access.refetch();}
 function prepare(){clearReview();if(!canWrite||conflict)return;const parsed=maintenanceSaveSchema.safeParse({environment,command:"SAVE_DRAFT",expectedDraftRevision:detail.data?.plan.draftRevision??null,reason,plan:document});
  if(!maintenanceIdSchema.safeParse(id).success){setMessage("계획 ID는 영문·숫자·하이픈·밑줄로 80자 이내여야 합니다.");return;}
  if(!parsed.success){setMessage("입력 내용을 확인해 주세요: "+parsed.error.issues.map(i=>i.path.join(".")+" "+i.message).join(" / "));return;}
  const attempt={id,key:crypto.randomUUID(),body:parsed.data};reviewRef.current=attempt;setReview(attempt);setMessage("");
 }
 async function execute(attempt:Attempt,recovery=false){setCommand({kind:"pending",attempt});if(alive.current)clearReview();
  try{const receipt=await adminApi.saveMaintenanceDraft(attempt.id,attempt.body,attempt.key);setCommand({kind:"saved",id:receipt.maintenanceId,revision:receipt.draftRevision});
   await Promise.all([client.invalidateQueries({queryKey:["maintenance-plan",operator,environment]}),client.invalidateQueries({queryKey:["maintenance-plans",operator,environment]}),client.invalidateQueries({queryKey:["maintenance-preview",operator,environment]})]);
   if(alive.current){if(attempt.id===id){setDraft(null);setReason("");}setMessage("초안 저장 완료 · 실제 점검은 적용되지 않았습니다.");onSaved(attempt.id);}
  }catch(error){const rejected=error instanceof AdminApiError&&((!recovery&&error.status>=400&&error.status<500)||(error.status===409&&error.errorCode==="maintenance_draft_revision_conflict"));setCommand(rejected?{kind:"idle"}:{kind:"uncertain",attempt});
   if(alive.current){setMessage(rejected?`저장 요청이 거절되었습니다 (${error.errorCode??error.status}). 최신 계획을 조회하고 다시 검토해 주세요.`:"저장 결과 미확인. 같은 요청 확인으로 기존 요청을 다시 확인해 주세요.");}
   if(rejected)await client.invalidateQueries({queryKey:["maintenance-plan",operator,environment]});
  }
 }
 function confirm(){const a=reviewRef.current;if(!a||!canWrite||state().kind==="pending"||state().kind==="uncertain")return;if(a.body.expectedDraftRevision!==(detail.data?.plan.draftRevision??null)){clearReview();setMessage("계획이 변경되었습니다. 다시 검토해 주세요.");return;}void execute(a);}
 async function recover(){const current=state();if(current.kind!=="uncertain"||environment!=="DEV")return;setCommand({kind:"pending",attempt:current.attempt});try{const latest=await adminApi.maintenancePlans(environment);if(!latest.permissions.canSaveDraft)throw new Error("권한 없음");await execute(current.attempt,true);}catch{setCommand(current);if(alive.current)setMessage("현재 저장 권한을 확인하지 못했습니다. 기존 요청은 보존했습니다.");}}
 useEffect(()=>{alive.current=true;return()=>{alive.current=false;reviewRef.current=null;};},[]);
 return {id,exists,changeId,document,change,reason,changeReason,canWrite,busy,conflict,reset,reload,prepare,confirm,recover,review,cancel:clearReview,message,command,detail,access};
}
