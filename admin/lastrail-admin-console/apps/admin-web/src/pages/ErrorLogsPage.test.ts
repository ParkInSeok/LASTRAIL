import { describe, expect, it } from "vitest";
import {
  errorLogDetailPlaceholders,
  errorLogFilterGroups,
  errorLogListColumns,
  errorLogSummaryCards
} from "./ErrorLogsPage";

describe("ErrorLogsPage configuration", () => {
  it("covers runtime error summary, filters, list columns, and detail placeholders", () => {
    expect(errorLogSummaryCards.map((card) => card.label)).toEqual([
      "최근 오류",
      "경고",
      "치명 오류",
      "재시도 가능 이벤트",
      "마스킹 필요 이벤트"
    ]);

    expect(errorLogFilterGroups.map((group) => group.label)).toContain("서비스");
    expect(errorLogListColumns).toContain("errorCode/message 요약");
    expect(errorLogDetailPlaceholders.map((placeholder) => placeholder.title)).toContain("민감정보 마스킹 상태");
  });

  it("keeps every displayed metric pending until the server log API is available", () => {
    expect(errorLogSummaryCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(errorLogDetailPlaceholders.every((placeholder) => placeholder.status === "연동 대기")).toBe(true);
  });

  it("uses Korean labels while keeping ops log stable codes visible", () => {
    const options = errorLogFilterGroups.flatMap((group) => group.options);

    expect(options).toContain("개발 (DEV)");
    expect(options).toContain("운영 (PRD)");
    expect(options).toContain("Unity 클라이언트 진단 (Unity Client Diagnostic)");
    expect(options).toContain("치명 오류 (fatal)");
    expect(options).toContain("보상 계획 불일치 (reward_plan_mismatch)");
    expect(options).toContain("위험도 보상 정책 누락 (risk_reward_policy_missing)");
    expect(options).toContain("무결성 판정으로 보상 제한 (reward_integrity_restricted)");
  });
});
