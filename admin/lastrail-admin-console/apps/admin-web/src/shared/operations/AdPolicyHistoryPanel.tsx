import {useQuery} from "@tanstack/react-query";
import {Link} from "react-router-dom";
import {adminApi} from "../api/client";
import type {PolicyEnvironment} from "./adPolicyContract";
import {recentPolicyHistory} from "./adPolicyHistory";
export function AdPolicyHistoryPanel({operatorId,environment,allowed,receiptId,refreshVersion}:{operatorId:string;environment:PolicyEnvironment;allowed:boolean;receiptId?:string;refreshVersion:number}){
 const query=useQuery({queryKey:["policy-history",operatorId,environment,allowed,receiptId,refreshVersion],enabled:allowed,retry:false,
  queryFn:async()=>recentPolicyHistory((await adminApi.auditLogs({action:"RUNTIME_POLICY_UPDATE",resourceId:environment,result:"SUCCESS"})).items,environment)});
 const rows=query.data??[];
 return <section className="policy-history" aria-label="최근 변경 이력"><header className="policy-card-header"><div><h2>최근 변경 이력</h2><p>최근 성공한 변경 최대 5건 · {environment}</p></div>{allowed&&<Link to={`/audit?action=RUNTIME_POLICY_UPDATE&resourceId=${environment}&result=SUCCESS`}>전체 이력 보기 →</Link>}</header>
  {!allowed?<p>변경 이력 조회 권한이 없습니다.</p>:query.isPending?<p role="status">변경 이력을 불러오는 중입니다.</p>:query.isError?<div role="alert"><p>변경 이력 조회 실패 · 이력이 없는 상태와 다릅니다.</p><button type="button" onClick={()=>void query.refetch()}>이력 다시 조회</button></div>:rows.length===0?<p>변경 이력이 없습니다.</p>:<div className="policy-history-scroll"><table><thead><tr><th>변경 시각 · KST</th><th>변경자</th><th>정책</th><th>변경 사유</th></tr></thead><tbody>{rows.map(row=><tr key={row.id}><td>{new Date(row.createdAt).toLocaleString("ko-KR",{timeZone:"Asia/Seoul"})}</td><td>{row.actor}</td><td className="policy-transition">{row.before} → {row.after}</td><td>{row.reason}</td></tr>)}</tbody></table></div>}
 </section>;
}
