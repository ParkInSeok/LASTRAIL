import { useParams } from "react-router-dom";
import { permissionLabel } from "../shared/labels/adminLabels";
import {
  customerSupportCaseTypeLabel,
  customerSupportCaseTypes,
  customerSupportContractPreview,
  customerSupportContextFieldLabel,
  customerSupportContextFields,
  customerSupportMutationControlsOutOfScope,
  customerSupportSafeSnippetPlaceholders,
  customerSupportTemplateStatuses
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

export const supportResponseTemplateCards: SupportCard[] = customerSupportTemplateStatuses.map((status) => ({
  code: status,
  label:
    {
      internalChecks: "내부 확인",
      playerFacingTemplate: "플레이어 안내 템플릿",
      escalateWhen: "운영 검토 조건"
    }[status] ?? status,
  value: "연동 대기",
  note: `${status} read-only template 상태`
}));

export const supportSafeSnippetCards: SupportCard[] = customerSupportSafeSnippetPlaceholders.map((snippet) => ({
  code: snippet,
  label:
    {
      server_record_checked: "서버 기록 확인",
      duplicate_replay_existing_result: "중복 요청 기존 결과",
      offline_claimable_cap_applied: "오프라인 보관 한도 적용",
      first_clear_finish_time_basis: "첫 클리어 완료 시점 기준",
      progress_and_text_are_separate: "진행도와 표시 문구 분리",
      rng_probability_not_disclosed: "확률·난수 비공개",
      escalated_for_ops_review: "운영 검토 전달"
    }[snippet] ?? snippet,
  value: "연동 대기",
  note: `${snippet} safe explanation 후보`
}));

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
      <section className="admin-panel">
        <header>
          <h2>응답 템플릿 상태</h2>
          <p>internal checks / player-facing template / escalate when 구조의 read-only placeholder입니다</p>
        </header>
        <div className="metric-grid">
          {supportResponseTemplateCards.map((card) => (
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
          <h2>안전 설명 snippet 후보</h2>
          <p>전체 문구 하드코딩 없이 지원 안내 가능 여부만 표시합니다</p>
        </header>
        <div className="metric-grid">
          {supportSafeSnippetCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
