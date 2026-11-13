import {useEffect,useRef,useState} from "react";
import {useQuery,useQueryClient} from "@tanstack/react-query";
import {adminApi,AdminApiError} from "../api/client";
import {newNoticeDocument,noticeCommandSchema,noticeIdSchema,type NoticeAction,type NoticeCommand,type NoticeDocument,type NoticeList} from "./noticeContract";
type Attempt={id:string;key:string;body:NoticeCommand};
type CommandState={kind:"idle"}|{kind:"pending"|"uncertain";attempt:Attempt}|{kind:"saved";revision:number;id:string};
export function useNoticeController(operator:string,environment:"DEV"|"PRD",selected:string|null,list:NoticeList|undefined,listHealthy:boolean){
 const client=useQueryClient(),mounted=useRef(false),reviewRef=useRef<Attempt|null>(null);
 const commandKey=["notice-command",operator,environment];
 const command=useQuery<CommandState>({queryKey:commandKey,queryFn:async()=>({kind:"idle"}),enabled:false,initialData:{kind:"idle"},gcTime:Infinity}).data;
 const state=()=>client.getQueryData<CommandState>(commandKey)??{kind:"idle"} as CommandState;
 const setCommand=(next:CommandState)=>client.setQueryData(commandKey,next);
 const [id,setId]=useState(selected??((command.kind==="pending"||command.kind==="uncertain")?command.attempt.id:`notice-${crypto.randomUUID()}`));
 const exists=!!selected||!!client.getQueryData(["notice",operator,environment,id])||(command.kind==="saved"&&command.id===id);
 const detail=useQuery({queryKey:["notice",operator,environment,selected??id],queryFn:()=>adminApi.notice(environment,selected??id),enabled:exists,retry:false,refetchOnWindowFocus:false});
 const [draft,setDraft]=useState<NoticeDocument|null>(null),[reason,setReason]=useState(""),[message,setMessage]=useState("");
 const [draftBase,setDraftBase]=useState<number|null>(null);
 const [review,setReview]=useState<Attempt|null>(null);
 const empty=useRef(newNoticeDocument());
 const document=draft??detail.data?.notice.document??empty.current;
 const busy=command.kind==="pending"||command.kind==="uncertain";
 const healthy=exists?!!detail.data&&!detail.isError&&!detail.isFetching:listHealthy&&!!list;
 const permissions=exists?detail.data?.permissions:list?.permissions;
 const write=environment==="DEV"&&healthy&&!busy;
 const dirty=draft!==null&&JSON.stringify(draft)!==JSON.stringify(detail.data?.notice.document);
 const hasConflict=draft!==null&&draftBase!==(detail.data?.notice.revision??null);
 function clearReview(){reviewRef.current=null;setReview(null);}
 function change(value:NoticeDocument){if(busy)return;clearReview();if(draft===null)setDraftBase(detail.data?.notice.revision??null);setDraft(value);setMessage("");}
 function resetDraft(){if(!write)return;clearReview();setDraft(null);setDraftBase(null);setMessage("");}
 function changeReason(value:string){clearReview();setReason(value);}
 function changeId(value:string){clearReview();setId(value);}
 function prepare(action:NoticeAction){
  clearReview();setMessage("");
  if(!write||!permissions||!(action==="SAVE_DRAFT"?permissions.canEdit:action==="WITHDRAW"?permissions.canWithdraw:permissions.canPublish))return;
  if(hasConflict){setMessage("다른 변경이 저장되었습니다. 최신 내용으로 다시 편집해 주세요.");return;}
  if(!noticeIdSchema.safeParse(id).success){setMessage("ID는 영문·숫자·하이픈·밑줄로 80자 이내여야 합니다.");return;}
  if(action!=="SAVE_DRAFT"&&(!detail.data||dirty)){setMessage("변경한 내용을 먼저 초안으로 저장해 주세요.");return;}
  if((action==="PUBLISH"||action==="SCHEDULE")&&detail.data){
   const now=detail.data.serverTime;
   if(detail.data.notice.status!=="DRAFT"||(document.endsAt!==null&&document.endsAt<=now)||(action==="PUBLISH"&&document.startsAt>now)||(action==="SCHEDULE"&&document.startsAt<=now)){setMessage("게시 시각을 확인해 주세요. 즉시 게시는 시작 시각이 지난 초안, 예약 게시는 미래 시각의 초안만 가능합니다.");return;}
  }
  const parsed=noticeCommandSchema.safeParse({environment,command:action,expectedRevision:detail.data?.notice.revision??null,reason,...(action==="SAVE_DRAFT"?{document}:{})});
  if(!parsed.success){setMessage(parsed.error.issues.map(i=>`${i.path.join(".")}: ${i.message}`).join(" / "));return;}
  const attempt={id,key:crypto.randomUUID(),body:parsed.data};reviewRef.current=attempt;setReview(attempt);
 }
 async function execute(attempt:Attempt,recovery=false){
  setCommand({kind:"pending",attempt});
  clearReview();
  try{
   const receipt=await adminApi.noticeCommand(attempt.id,attempt.body,attempt.key);
   setCommand({kind:"saved",revision:receipt.revision,id:receipt.noticeId});
   if(mounted.current&&attempt.id===id){setDraft(null);setReason("");setMessage(`처리 완료 · 변경 번호 ${receipt.revision}. 최신 상태를 다시 조회합니다.`);}
   await Promise.all([client.invalidateQueries({queryKey:["notices",operator,environment]}),client.invalidateQueries({queryKey:["notice",operator,environment]})]);
  }catch(error){
   const rejected=!recovery&&error instanceof AdminApiError&&error.status>=400&&error.status<500;
   setCommand(rejected?{kind:"idle"}:{kind:"uncertain",attempt});
   if(mounted.current)setMessage(rejected?`요청이 거절되었습니다 (${error.errorCode??error.status}). 새로고침 후 내용을 다시 검토해 주세요.`:"처리 결과를 확인하지 못했습니다. 같은 요청 확인으로 기존 요청만 다시 확인할 수 있습니다.");
   if(rejected)await client.invalidateQueries({queryKey:["notice",operator,environment]});
  }
 }
 function confirm(){const attempt=reviewRef.current;if(!attempt||state().kind==="pending"||state().kind==="uncertain"||!write)return;if(attempt.body.expectedRevision!==(detail.data?.notice.revision??null)){clearReview();setMessage("공지 내용이 변경되었습니다. 다시 검토해 주세요.");return;}void execute(attempt);}
 async function recover(){
  const current=state();if(current.kind!=="uncertain"||environment!=="DEV")return;
  // Claim synchronously before the permission read to prevent double recovery clicks.
  setCommand({kind:"pending",attempt:current.attempt});
  try{
   const latest=await adminApi.notices(environment);
   const action=current.attempt.body.command;
   if(!(action==="SAVE_DRAFT"?latest.permissions.canEdit:action==="WITHDRAW"?latest.permissions.canWithdraw:latest.permissions.canPublish))throw new Error("권한 없음");
   await execute(current.attempt,true);
  }catch{setCommand(current);if(mounted.current)setMessage("현재 권한을 확인하지 못했습니다. 기존 요청은 보존했습니다.");}
 }
 async function reload(){if(busy)return;clearReview();await detail.refetch();}
 useEffect(()=>{mounted.current=true;return()=>{mounted.current=false;reviewRef.current=null;};},[]);
 return {id,exists,changeId,document,change,reason,changeReason,message,review,prepare,confirm,cancel:clearReview,recover,command,detail,permissions,write,busy,dirty,hasConflict,resetDraft,reload};
}
