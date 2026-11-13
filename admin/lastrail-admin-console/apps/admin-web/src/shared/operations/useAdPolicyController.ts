import {useEffect,useRef,useState} from "react";
import {useQuery,useQueryClient} from "@tanstack/react-query";
import {adminApi,AdminApiError} from "../api/client";
import type {AdPolicy,AdPolicyCommand,AdPolicyReceipt,PolicyEnvironment} from "./adPolicyContract";
import {validateAdPolicyDraft,type AdPolicyDraft} from "./adPolicyDraft";
type Attempt={key:string;body:AdPolicyCommand};
type CommandState = {kind:"idle"} | {kind:"pending"|"uncertain";attempt:Attempt} | {kind:"rejected";message:string} | {kind:"saved";receipt:AdPolicyReceipt};
type ReadState={kind:"loading"|"error";message?:string}|{kind:"ready";policy:AdPolicy};
const emptyDraft:AdPolicyDraft={adsEnabled:null,reason:""};
// QueryClient owns pending/ambiguous commands across route remounts. The mounted
// controller owns the current read and unsent draft. Nothing is stored on disk.
export function useAdPolicyController(operatorId:string,environment:PolicyEnvironment,writeAllowed:boolean) {
 const client=useQueryClient(),commandKey=["ad-policy-command",operatorId,environment];
 const commandQuery=useQuery<CommandState>({queryKey:commandKey,queryFn:async()=>({kind:"idle"}),enabled:false,initialData:{kind:"idle"},gcTime:Infinity});
 const command=commandQuery.data;
 const [read,setRead]=useState<ReadState>({kind:"loading"});
 const [draft,setDraft]=useState<AdPolicyDraft>(emptyDraft),[errors,setErrors]=useState<string[]>([]);
 const [review,setReview]=useState<{draft:AdPolicyDraft;revision:number|null}|null>(null);
 const reviewed=useRef<typeof review>(null);
 const clearReview=()=>{reviewed.current=null;setReview(null);};
 const mounted=useRef(false),generation=useRef(0),reading=useRef(false);
 const state=()=>client.getQueryData<CommandState>(commandKey)??{kind:"idle"} as CommandState;
 const setCommand=(next:CommandState)=>client.setQueryData(commandKey,next);
 async function reload() {
  if(!mounted.current || reading.current || state().kind==="pending")return;
  clearReview();
  const token=++generation.current;reading.current=true;setRead({kind:"loading"});
  try {
   const policy=await adminApi.adPolicy(environment);
   if(!mounted.current || token!==generation.current)return;
   setRead({kind:"ready",policy});
   if(state().kind==="rejected")setCommand({kind:"idle"});
  }catch(error){if(mounted.current && token===generation.current)setRead({kind:"error",message:error instanceof AdminApiError&&error.status===403?"광고 정책 조회 권한이 없습니다.":"광고 정책 조회 실패 · 현재 값은 미확인입니다."});}
  finally{if(token===generation.current)reading.current=false;}
 }
 useEffect(()=>{mounted.current=true;void reload();return()=>{mounted.current=false;generation.current++;reading.current=false;};},[]);
 // A new observer can mount while another controller's request owns the cache.
 // Resume its deferred read once that request settles; never issue another POST.
 useEffect(()=>{if(command.kind!=="pending" && read.kind==="loading" && !reading.current)void reload();},[command.kind]);
 const busy=command.kind==="pending"||command.kind==="uncertain";
 const editable=writeAllowed&&read.kind==="ready"&&read.policy.canWrite&&!busy&&command.kind!=="rejected";
 function change(next:AdPolicyDraft){if(!editable)return;clearReview();setDraft(next);setErrors([]);if(command.kind==="saved")setCommand({kind:"idle"});}
 function reviewDraft(){
  if(!editable||reading.current||read.kind!=="ready")return;
  const issues=validateAdPolicyDraft(draft,read.policy.canEnable);setErrors(issues);
  if(issues.length){clearReview();return;}
  const next={draft:{...draft},revision:read.policy.runtimePolicyRevision};reviewed.current=next;setReview(next);
 }
 async function execute(attempt:Attempt,recovery=false) {
  if(!mounted.current || state().kind==="pending")return;
  setCommand({kind:"pending",attempt});
  try {
   const receipt=await adminApi.updateAdPolicy(attempt.body,attempt.key);
   setCommand({kind:"saved",receipt});
   if(!mounted.current)return;
   setDraft(emptyDraft);setErrors([]);await reload();
  }catch(error){
   // Rejected authentication/validation of a replay says nothing about the
   // original transaction. Keep its identity until a valid receipt is restored.
   if(recovery){
    setCommand({kind:"uncertain",attempt});
    if(mounted.current && error instanceof AdminApiError && (error.status===401||error.status===403))setRead({kind:"error",message:"요청 결과 미확인 · 권한 복구 후 최신 정책을 조회하고 같은 요청을 확인하세요."});
    return;
   }
   // Transport/server/invalid-response failures cannot establish rollback.
   if(error instanceof AdminApiError && error.status>=400 && error.status<500){
    const message=error.errorCode==="runtime_policy_revision_conflict"?"정책 개정 충돌 · 초안은 유지했습니다. 최신 정책을 조회하고 다시 검토하세요.":error.status===403?"저장 권한이 없습니다. 최신 권한과 정책을 다시 확인하세요.":error.errorCode==="idempotency_conflict"?"요청 식별자 충돌 · 최신 정책을 확인하고 변경 사유를 다시 검토하세요.":"저장 요청이 거절되었습니다. 환경과 입력을 확인하고 최신 정책을 조회하세요.";
    setCommand({kind:"rejected",message});
   }else setCommand({kind:"uncertain",attempt});
  }
 }
 function save(){
  const current=state();
  if(!mounted.current || reading.current || !editable || current.kind==="pending" || current.kind==="uncertain" || read.kind!=="ready")return;
  const issues=validateAdPolicyDraft(draft,read.policy.canEnable);
  setErrors(issues);if(issues.length)return;
  const approved=reviewed.current;
  if(!approved||approved.revision!==read.policy.runtimePolicyRevision||approved.draft.adsEnabled!==draft.adsEnabled||approved.draft.reason!==draft.reason){setErrors(["변경 내용을 먼저 검토하세요."]);return;}
  clearReview();
  void execute({key:crypto.randomUUID(),body:{environment,expectedRuntimePolicyRevision:read.policy.runtimePolicyRevision,adsEnabled:draft.adsEnabled!,reason:draft.reason.trim()}});
 }
 function retry(){const current=state();if(!reading.current&&writeAllowed&&read.kind==="ready"&&read.policy.canWrite&&current.kind==="uncertain")void execute(current.attempt,true);}
 return {read,draft,errors,command,editable,review,reviewDraft,change,save,reload,retry,canRetry:writeAllowed&&read.kind==="ready"&&read.policy.canWrite&&command.kind==="uncertain"};
}
