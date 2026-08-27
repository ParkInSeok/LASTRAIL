import {
  opsEnvironmentLabel,
  opsErrorCodeLabel,
  opsServiceLabel,
  opsSeverityLabel
} from "../shared/labels/adminLabels";

type ErrorLogSummaryCard = {
  label: string;
  value: "연동 대기";
  note: string;
};

type ErrorLogFilterGroup = {
  label: string;
  options: string[];
};

type ErrorLogDetailPlaceholder = {
  title: string;
  status: "연동 대기";
  description: string;
};

const labeledCode = (label: string, code: string) => `${label} (${code})`;

export const errorLogSummaryCards: ErrorLogSummaryCard[] = [
  { label: "최근 오류", value: "연동 대기", note: "최근 런타임 error 이벤트" },
  { label: "경고", value: "연동 대기", note: "warn 심각도 이벤트" },
  { label: "치명 오류", value: "연동 대기", note: "fatal 심각도 이벤트" },
  { label: "재시도 가능 이벤트", value: "연동 대기", note: "retry later 후보" },
  { label: "마스킹 필요 이벤트", value: "연동 대기", note: "민감정보 보호 검토" }
];

export const errorLogFilterGroups: ErrorLogFilterGroup[] = [
  { label: "환경", options: ["DEV", "PRD"].map((code) => labeledCode(opsEnvironmentLabel(code), code)) },
  {
    label: "서비스",
    options: ["Admin API", "Game API", "Async Worker", "Pages Function", "Unity Client Diagnostic"].map((code) =>
      labeledCode(opsServiceLabel(code), code)
    )
  },
  { label: "심각도", options: ["warn", "error", "fatal"].map((code) => labeledCode(opsSeverityLabel(code), code)) },
  {
    label: "보상/위험도 오류",
    options: [
      "reward_plan_mismatch",
      "reward_multiplier_missing",
      "reward_plan_unavailable",
      "risk_reward_policy_missing",
      "reward_integrity_restricted",
      "offline_reward_policy_missing",
      "offline_reward_claim_unavailable",
      "offline_reward_state_conflict",
      "offline_reward_integrity_restricted"
    ].map((code) => labeledCode(opsErrorCodeLabel(code), code))
  },
  { label: "식별자", options: ["errorCode", "requestId", "accountId", "runId", "rewardPreviewId", "gachaBatchId"] },
  { label: "시간 범위", options: ["최근 1시간", "최근 24시간", "최근 7일", "사용자 지정"] }
];

export const errorLogListColumns = [
  "발생 시각",
  "서비스/환경",
  "심각도",
  "errorCode/message 요약",
  "requestId",
  "관련 리소스",
  "재시도 가능 여부"
];

export const errorLogDetailPlaceholders: ErrorLogDetailPlaceholder[] = [
  {
    title: "stack/trace 요약",
    status: "연동 대기",
    description: "서버 로그 수집 API 확정 후 마스킹된 trace 요약을 표시합니다."
  },
  {
    title: "request metadata",
    status: "연동 대기",
    description: "method, route, status, requestId 등 민감정보를 제외한 요청 메타데이터를 표시합니다."
  },
  {
    title: "관련 운영 링크",
    status: "연동 대기",
    description: "audit log, ledger, reward claim, gacha batch 연결 조회 후보입니다."
  },
  {
    title: "민감정보 마스킹 상태",
    status: "연동 대기",
    description: "token, secret, raw request body, raw RNG material 저장/표시 금지 기준을 확인합니다."
  },
  {
    title: "클라이언트 진단 payload",
    status: "연동 대기",
    description: "Unity diagnostic snapshot은 fail-soft/non-blocking 별도 소스 후보로만 취급합니다."
  }
];

export function ErrorLogsPage() {
  return (
    <main className="content">
      <header>
        <h1>시스템 에러 로그</h1>
        <p>Worker, API, Pages Function, 클라이언트 진단 실패를 관측하는 읽기 전용 준비 화면</p>
      </header>
      <section className="summary-grid" aria-label="시스템 에러 로그 요약">
        {errorLogSummaryCards.map((card) => (
          <div key={card.label}>
            <span>{card.label}</span>
            <strong>{card.value}</strong>
            <small>{card.note}</small>
          </div>
        ))}
      </section>
      <section className="admin-panel">
        <header>
          <h2>필터</h2>
          <p>로그 수집 API 확정 전까지 조건만 고정합니다</p>
        </header>
        <div className="metric-grid">
          {errorLogFilterGroups.map((group) => (
            <div key={group.label} className="metric-card">
              <span>{group.label}</span>
              <strong>연동 대기</strong>
              <small>{group.options.join(" / ")}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>목록</h2>
          <p>시스템 런타임 실패 관측용이며 운영자 작업 이력인 감사 로그와 분리합니다</p>
        </header>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                {errorLogListColumns.map((column) => (
                  <th key={column}>{column}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              <tr>
                <td colSpan={errorLogListColumns.length}>로그 조회 API 연동 대기</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>상세/가이드</h2>
          <p>D1/R2/remote log transport 연결 없이 표시 구조만 준비합니다</p>
        </header>
        <div className="metric-grid">
          {errorLogDetailPlaceholders.map((placeholder) => (
            <div key={placeholder.title} className="metric-card">
              <span>{placeholder.title}</span>
              <strong>{placeholder.status}</strong>
              <small>{placeholder.description}</small>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
