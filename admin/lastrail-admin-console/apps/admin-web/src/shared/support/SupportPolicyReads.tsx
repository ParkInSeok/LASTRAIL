import { adminRequestFailureDisplay } from "../api/errorDisplay";
import "./supportPolicyReads.css";

// Presentation only; the API adapter owns account and wire-shape validation.
export type PolicyReadState =
  | { kind: "select_account" | "loading" | "not_connected" }
  | { kind: "error"; error: unknown }
  | { kind: "ready"; sections: { label: string; fields: { label: string; value: string | number | null }[] }[] };
export function SupportPolicyReads({ state }: { state: PolicyReadState }) {
  return <section className="support-policy-reads" aria-label="정책 및 보상 조회">
    <h2>정책 및 보상 조회</h2>
    {state.kind === "ready" && <p>영역별 개별 조회 · 동일 시점 스냅샷 아님 · 임무/우편 최신 100건, 전체 건수 아님</p>}
    {state.kind === "error" ? <p role="alert">조회 실패 · {adminRequestFailureDisplay(state.error, { resourceLabel: "지원 계정", resourceId: "/api/support/accounts" }).reason}</p>
      : state.kind === "ready" ? state.sections.length ? state.sections.map((section, i) => <div key={i}><h3>{section.label}</h3><dl>{section.fields.map((field, j) => <div key={j}><dt>{field.label}</dt><dd>{field.value === null || field.value === "" ? "없음" : field.value}</dd></div>)}</dl></div>) : <p>데이터 없음</p>
      : <dl>{["상점", "월간 출석", "패스", "일반임무 · 우편"].map(label => <div key={label}><dt>{label}</dt><dd>{state.kind === "loading" ? "조회 중" : state.kind === "select_account" ? "계정 선택 필요" : "조회 계약 대기"}</dd></div>)}</dl>}
  </section>;
}
