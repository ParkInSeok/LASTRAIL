import type {AuditLogsResponse} from "@lastrail/admin-contracts";
import type {PolicyEnvironment} from "./adPolicyContract";
export type PolicyHistoryRow={id:string;createdAt:string;actor:string;before:string;after:string;reason:string};
function object(value:unknown):Record<string,unknown>{return value!==null&&typeof value==="object"&&!Array.isArray(value)?value as Record<string,unknown>:{};}
function status(value:unknown){const enabled=object(value).adsEnabled;return enabled===true?"ON":enabled===false?"OFF":"미확인";}
export function recentPolicyHistory(items:AuditLogsResponse["items"],environment:PolicyEnvironment):PolicyHistoryRow[]{
 return items.filter(item=>item.action==="RUNTIME_POLICY_UPDATE"&&item.resourceId===environment&&item.result==="SUCCESS")
  .sort((a,b)=>Date.parse(b.createdAt)-Date.parse(a.createdAt)||b.id.localeCompare(a.id)).slice(0,5)
  .map(item=>{const summary=object(item.changeSummary);return {id:item.id,createdAt:item.createdAt,actor:item.principalDisplayName,before:status(summary.before),after:status(summary.after),reason:typeof summary.reason==="string"&&summary.reason.trim()?summary.reason:"사유 미확인"};});
}
