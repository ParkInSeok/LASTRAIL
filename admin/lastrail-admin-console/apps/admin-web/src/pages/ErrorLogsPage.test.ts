import { describe, expect, it } from "vitest";
import {
  errorLogDetailPlaceholders,
  errorLogEndpointCandidates,
  errorLogFilterGroups,
  errorLogListColumns,
  errorLogReadOnlyFilters,
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
    expect(options).toContain("게임 API (game_api)");
    expect(options).toContain("고객지원 조회 모델 (support_read_model)");
    expect(options).toContain("열림 (open)");
    expect(options).toContain("확인됨 (acknowledged)");
    expect(options).toContain("치명 (critical)");
    expect(options).toContain("Unity 클라이언트 진단 (Unity Client Diagnostic)");
    expect(options).toContain("치명 오류 (fatal)");
    expect(options).toContain("Google 인증 설정 누락 (google_auth_config_missing)");
    expect(options).toContain("Google 인증 검증기 사용 불가 (google_auth_verifier_unavailable)");
    expect(options).toContain("보상 계획 불일치 (reward_plan_mismatch)");
    expect(options).toContain("위험도 보상 정책 누락 (risk_reward_policy_missing)");
    expect(options).toContain("무결성 판정으로 보상 제한 (reward_integrity_restricted)");
    expect(options).toContain("오프라인 보상 정책 누락 (offline_reward_policy_missing)");
    expect(options).toContain("오프라인 보상 상태 충돌 (offline_reward_state_conflict)");
    expect(options).toContain("모집 상태 충돌 (gacha_state_conflict)");
    expect(options).toContain("스토리 진행 정책 누락 (story_progress_policy_missing)");
    expect(options).toContain("스토리 진행 상태 충돌 (story_progress_state_conflict)");
    expect(options).toContain("내러티브 콘텐츠 누락 (narrative_content_missing)");
    expect(options).toContain("gachaBannerId");
  });

  it("keeps ops error logs as read-only support evidence before endpoints are implemented", () => {
    expect(errorLogEndpointCandidates).toEqual([
      "GET /v1/admin/ops/error-logs",
      "GET /v1/admin/ops/error-logs/:id"
    ]);
    expect(errorLogDetailPlaceholders.map((placeholder) => placeholder.title)).toContain("지원 근거");
    expect(JSON.stringify(errorLogDetailPlaceholders)).not.toContain("resolve button");
    expect(JSON.stringify(errorLogDetailPlaceholders)).not.toContain("repair action");
    expect(JSON.stringify(errorLogDetailPlaceholders)).toContain("Google JWKS/fetch/parse");
    expect(JSON.stringify(errorLogDetailPlaceholders)).toContain("retryable=true");
    expect(JSON.stringify(errorLogDetailPlaceholders)).toContain("masked=true");
    expect(JSON.stringify(errorLogDetailPlaceholders)).not.toContain("JWKS raw dump");
    expect(JSON.stringify(errorLogDetailPlaceholders)).not.toContain("full claims");
    expect(JSON.stringify(errorLogDetailPlaceholders)).not.toContain("stack dump");
  });

  it("prepares actual read-only Admin API filters and list columns", () => {
    expect(errorLogReadOnlyFilters).toEqual([
      "environment",
      "severity",
      "status",
      "source",
      "service",
      "errorCode",
      "requestId",
      "accountId",
      "runId",
      "rewardPreviewId",
      "rewardClaimId",
      "offlineRewardClaimId",
      "gachaBatchId",
      "gachaBannerId",
      "storyProgressId",
      "integrityEventId",
      "supportTicketId",
      "limit",
      "cursor"
    ]);
    expect(errorLogListColumns).toEqual([
      "발생 시각",
      "소스/서비스",
      "심각도",
      "상태",
      "errorCode/message 요약",
      "requestId",
      "관련 리소스",
      "masked"
    ]);
  });
});
