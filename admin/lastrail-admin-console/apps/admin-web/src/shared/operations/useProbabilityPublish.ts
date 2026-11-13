import {useEffect,useRef,useState} from "react";
import {useQuery,useQueryClient} from "@tanstack/react-query";
import {adminApi,AdminApiError} from "../api/client";
import {probabilityPublishSchema,type ProbabilityPublish,type ProbabilitySource} from "./probabilityDisclosure";
type Attempt={body:ProbabilityPublish;key:string;source:ProbabilitySource};
type State={kind:"idle"}|{kind:"pending"|"uncertain";attempt:Attempt}|{kind:"saved";id:string};
export function useProbabilityPublish(operator:string,environment:"DEV"|"PRD",owner:boolean,source:ProbabilitySource|undefined){
 const client=useQueryClient(),alive=useRef(false),reviewRef=useRef<Attempt|null>(null);
 const key=["probability-publish",operator,environment];
 const command=useQuery<State>({queryKey:key,queryFn:async()=>({kind:"idle"}),enabled:false,initialData:{kind:"idle"},gcTime:Infinity}).data;
 const state=()=>client.getQueryData<State>(key)??{kind:"idle"} as State;
 const setCommand=(next:State)=>client.setQueryData(key,next);
 const [reason,setReason]=useState(""),[review,setReview]=useState<Attempt|null>(null),[message,setMessage]=useState("");
 const busy=command.kind==="pending"||command.kind==="uncertain";
 const canPublish=owner&&environment==="DEV"&&!!source?.canPublish&&!busy;
 function cancel(){reviewRef.current=null;setReview(null);}
 function changeReason(value:string){cancel();setReason(value);}
 function prepare(){cancel();setMessage("");if(!canPublish||!source)return;
  const parsed=probabilityPublishSchema.safeParse({environment,consumer:source.consumer,sourceKind:source.publicSchemaVersion===2?"PURCHASE_POLICY":"AD_POLICY",policyVersion:source.policyVersion,expectedSourceDigest:source.sourceDigest,reason});
  if(!parsed.success){setMessage("게시 사유를 1~500자로 입력해 주세요.");return;}
  const attempt={body:parsed.data,key:crypto.randomUUID(),source:structuredClone(source)};reviewRef.current=attempt;setReview(attempt);
 }
 async function refresh(){await client.invalidateQueries({queryKey:["probability-source",operator,environment]});}
 async function send(attempt:Attempt,recovery:boolean){
  try{const receipt=await adminApi.publishProbability(attempt.body,attempt.key,attempt.source.disclosureId);setCommand({kind:"saved",id:receipt.disclosureId});if(alive.current){setReason("");setMessage("공시 게시가 처리되었습니다. 현재 원본의 게시 상태를 다시 조회합니다.");}await refresh();}
  catch(error){const definitive=error instanceof AdminApiError&&((!recovery&&error.status>=400&&error.status<500)||(error.status===409&&["probability_source_conflict","probability_disclosure_exists"].includes(error.errorCode??""))||(error.status===404&&error.errorCode==="probability_source_not_found"));
   setCommand(definitive?{kind:"idle"}:{kind:"uncertain",attempt});if(alive.current)setMessage(definitive?`게시 요청 거절 (${(error as AdminApiError).errorCode??"요청 확인 필요"}). 원본을 다시 조회하고 검토해 주세요.`:"게시 결과를 확인하지 못했습니다. 같은 요청 확인으로 기존 게시 요청만 재확인해 주세요.");if(definitive)await refresh();
  }
 }
 async function confirm(){const attempt=reviewRef.current;if(!attempt||!canPublish||state().kind==="pending"||state().kind==="uncertain"||source?.disclosureId!==attempt.source.disclosureId||source.sourceDigest!==attempt.body.expectedSourceDigest)return;
  setCommand({kind:"pending",attempt});cancel();
  try{const latest=await adminApi.probabilitySource(environment,{consumer:attempt.body.consumer,sourceKind:attempt.body.sourceKind,policyVersion:attempt.body.policyVersion});
   if(!latest.canPublish||latest.disclosureId!==attempt.source.disclosureId||latest.sourceDigest!==attempt.body.expectedSourceDigest){setCommand({kind:"idle"});if(alive.current)setMessage("원본이나 게시 상태가 변경되었습니다. 다시 조회한 내용을 검토해 주세요.");await refresh();return;}
  }catch{setCommand({kind:"idle"});if(alive.current)setMessage("게시 직전 원본 확인에 실패했습니다. 게시 요청은 보내지 않았습니다.");return;}
  await send(attempt,false);
 }
 async function recover(){const current=state();if(current.kind!=="uncertain"||!owner||environment!=="DEV")return;setCommand({kind:"pending",attempt:current.attempt});
  try{const identity=await adminApi.bootstrap();if(identity.admin.id!==operator||identity.admin.role!=="OWNER"||!(identity.environment==="dev"||identity.environment==="local"))throw new Error("권한 확인 실패");}
  catch{setCommand(current);if(alive.current)setMessage("현재 게시 권한을 확인하지 못했습니다. 기존 요청을 보존했습니다.");return;}
  // Replay must remain possible after publication/retirement/deletion, without a new source precondition.
  await send(current.attempt,true);
 }
 useEffect(()=>{cancel();},[source]);
 useEffect(()=>{alive.current=true;return()=>{alive.current=false;reviewRef.current=null;};},[]);
 return {reason,changeReason,review,prepare,confirm,cancel,recover,command,canPublish,busy,message};
}
