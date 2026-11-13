export type PolicyEnvironment = "DEV" | "PRD";
export type AdPolicy = {
  environment: PolicyEnvironment; policyStatus: "CONFIGURED" | "UNKNOWN";
  adsEnabled: boolean | null; runtimePolicyRevision: number | null; serverTime: number;
  canWrite: boolean; canEnable: boolean; enableBlockReason: string | null;
};
export type AdPolicyCommand = {environment:PolicyEnvironment;expectedRuntimePolicyRevision:number|null;adsEnabled:boolean;reason:string};
export type AdPolicyReceipt = {environment:PolicyEnvironment;adsEnabled:boolean;runtimePolicyRevision:number;auditId:string;requestId:string};
function record(value:unknown):Record<string,unknown> {
  if(!value || typeof value!=="object" || Array.isArray(value))throw Error("광고 정책 응답 형식 오류");
  return value as Record<string,unknown>;
}
const positive = (value:unknown):value is number => typeof value==="number" && Number.isSafeInteger(value) && value>0;
const nonempty = (value:unknown):value is string => typeof value==="string" && value.trim().length>0;
export function mapAdPolicy(value:unknown,environment:PolicyEnvironment):AdPolicy {
  const r=record(value), configured=r.policyStatus==="CONFIGURED";
  if(r.accepted!==true || r.contractVersion!=="admin-runtime-policy-v1" || r.environment!==environment ||
    r.permission!=="RUNTIME_POLICY_READ" || r.permissionMode!=="ROLE_PERMISSION" ||
    !["CONFIGURED","UNKNOWN"].includes(String(r.policyStatus)) ||
    (configured ? typeof r.adsEnabled!=="boolean" || !positive(r.runtimePolicyRevision) : r.adsEnabled!==null || r.runtimePolicyRevision!==null) ||
    typeof r.serverTime!=="number" || !Number.isSafeInteger(r.serverTime) || r.serverTime<0 || !Number.isFinite(new Date(r.serverTime).getTime()) ||
    typeof r.canWrite!=="boolean" || typeof r.canEnable!=="boolean" ||
    !(r.enableBlockReason===null || nonempty(r.enableBlockReason)) || (!r.canEnable && !nonempty(r.enableBlockReason)))throw Error("광고 정책 응답의 환경·권한·값을 확인할 수 없습니다.");
  return {environment,policyStatus:r.policyStatus as AdPolicy["policyStatus"],adsEnabled:r.adsEnabled as boolean|null,runtimePolicyRevision:r.runtimePolicyRevision as number|null,serverTime:r.serverTime,canWrite:r.canWrite,canEnable:r.canEnable,enableBlockReason:r.enableBlockReason as string|null};
}
export function mapAdPolicyWrite(value:unknown,command:AdPolicyCommand):AdPolicyReceipt {
  const r=record(value);
  if(r.accepted!==true || r.contractVersion!=="admin-runtime-policy-write-v1" || r.environment!==command.environment || r.adsEnabled!==command.adsEnabled ||
    !positive(r.runtimePolicyRevision) || r.runtimePolicyRevision!==(command.expectedRuntimePolicyRevision??0)+1 || !nonempty(r.auditId) || !nonempty(r.requestId))throw Error("저장 응답을 확인할 수 없습니다. 같은 요청으로 결과를 확인하세요.");
  return {environment:command.environment,adsEnabled:command.adsEnabled,runtimePolicyRevision:r.runtimePolicyRevision,auditId:r.auditId,requestId:r.requestId};
}
