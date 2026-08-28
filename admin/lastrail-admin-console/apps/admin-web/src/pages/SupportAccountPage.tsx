import { useParams } from "react-router-dom";
import { permissionLabel } from "../shared/labels/adminLabels";
import {
  customerSupportCaseTypeLabel,
  customerSupportCaseTypes,
  customerSupportContractPreview,
  customerSupportContextFieldLabel,
  customerSupportContextFields,
  customerSupportMutationControlsOutOfScope
} from "../shared/support/supportContract";

type SupportCard = {
  code: string;
  field?: string;
  label: string;
  value: "연동 대기";
  note: string;
};

export const supportCaseTypeCards: SupportCard[] = customerSupportCaseTypes.map((caseType) => ({
  code: caseType,
  label: customerSupportCaseTypeLabel(caseType),
  value: "연동 대기",
  note: `${caseType} read-only case 후보`
}));

export const supportAccountContextCards: SupportCard[] = customerSupportContextFields.map((field) => ({
  code: field,
  field,
  label: customerSupportContextFieldLabel(field),
  value: "연동 대기",
  note: `${field} sanitized reference 후보`
}));

export const supportOutOfScopeControls = [...customerSupportMutationControlsOutOfScope];

export function SupportAccountPage() {
  const { accountId = "연동 대기" } = useParams();

  return (
    <main className="content">
      <header>
        <h1>CS 지원</h1>
        <p>{accountId}</p>
      </header>
      <section className="summary-grid" aria-label="CS 지원 요약">
        <div>
          <span>권한 후보</span>
          <strong>{permissionLabel(customerSupportContractPreview.permission)}</strong>
          <small>{customerSupportContractPreview.permission}</small>
        </div>
        <div>
          <span>권한 모드 후보</span>
          <strong>{customerSupportContractPreview.permissionMode}</strong>
          <small>서버 route 구현 전 contract preview</small>
        </div>
        <div>
          <span>endpoint 후보</span>
          <strong>연동 대기</strong>
          <small>{customerSupportContractPreview.endpointCandidate}</small>
        </div>
        <div>
          <span>mutation</span>
          <strong>비활성</strong>
          <small>launch 1차 read-only</small>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>문의 유형</h2>
          <p>지원 응답을 분류하기 위한 stable case type 후보입니다</p>
        </header>
        <div className="metric-grid">
          {supportCaseTypeCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>계정 지원 문맥</h2>
          <p>correlation/reference id와 sanitized status 중심의 읽기 전용 후보입니다</p>
        </header>
        <div className="metric-grid">
          {supportAccountContextCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
