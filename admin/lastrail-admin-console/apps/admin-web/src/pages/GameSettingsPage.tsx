import {useAdmin} from "../shared/auth/AdminContext";
import {AdPolicyConnectedView} from "../shared/operations/AdPolicyConnectedView";
import {useAdPolicyController} from "../shared/operations/useAdPolicyController";
import type {PolicyEnvironment} from "../shared/operations/adPolicyContract";
import {AdPolicyHistoryPanel} from "../shared/operations/AdPolicyHistoryPanel";
import {useState} from "react";
import {Link} from "react-router-dom";
export function GameSettingsPage(){
 const {bootstrap,isLoading,error}=useAdmin();
 const allowed=bootstrap?.admin.permissions.includes("RUNTIME_POLICY_READ");
 const environment=bootstrap?.environment;
 const writeAllowed=!!bootstrap?.admin.permissions.includes("RUNTIME_POLICY_WRITE")&&(environment==="dev"||environment==="local");
 return <main className="content"><header><h1>게임 설정</h1></header>
  {isLoading?<p role="status">운영자 정보를 조회 중입니다.</p>:error||!bootstrap?<p role="alert">운영자 정보 조회 실패 · 권한과 환경을 확인할 수 없습니다.</p>:!allowed?<p role="alert">게임 설정 조회 권한이 없습니다.</p>:
   <PolicyContent key={`${bootstrap.admin.id}:${environment}:${writeAllowed}:${bootstrap.admin.permissions.includes("AUDIT_READ")}`} operatorId={bootstrap.admin.id} environment={environment==="prd"?"PRD":"DEV"} writeAllowed={writeAllowed} historyAllowed={bootstrap.admin.permissions.includes("AUDIT_READ")}/>}
  <section className="admin-panel"><h2>공지 · 서비스 점검</h2><p><Link to="/notices">공지 관리 열기</Link> · <Link to="/maintenance">점검 계획 조회</Link></p><p>공지 게시와 서비스 점검, 광고 OFF는 별도 정책입니다. 실제 점검 적용은 아직 연결되지 않았습니다.</p></section>
 </main>;
}
function PolicyContent({operatorId,environment,writeAllowed,historyAllowed}:{operatorId:string;environment:PolicyEnvironment;writeAllowed:boolean;historyAllowed:boolean}){
 const controller=useAdPolicyController(operatorId,environment,writeAllowed);
 const [refreshVersion,setRefreshVersion]=useState(0);
 async function reload(){setRefreshVersion(value=>value+1);await controller.reload();}
 return <><AdPolicyConnectedView {...controller} reload={reload} environment={environment} writeAllowed={writeAllowed}/><AdPolicyHistoryPanel operatorId={operatorId} environment={environment} allowed={historyAllowed} refreshVersion={refreshVersion} receiptId={controller.command.kind==="saved"?controller.command.receipt.auditId:undefined}/></>;
}
