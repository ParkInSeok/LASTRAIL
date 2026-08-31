import { describe, expect, it } from "vitest";
import { AdminApiError } from "./client";
import { adminReadStatusLabel, adminReadValueLabel, adminRequestFailureDisplay } from "./errorDisplay";

describe("admin request failure display", () => {
  it("classifies missing detail ids as DEV fixture or id wait", () => {
    expect(
      adminRequestFailureDisplay(
        new AdminApiError({
          status: 404,
          path: "/api/players/missing-account",
          errorCode: "ADMIN_PLAYER_NOT_FOUND",
          upstreamMessage: "Player was not found."
        }),
        { resourceLabel: "플레이어", resourceId: "missing-account" }
      )
    ).toMatchObject({
      reason: "DEV fixture/ID 없음",
      statusLabel: "404",
      errorCode: "ADMIN_PLAYER_NOT_FOUND",
      nextStep: "목록/검색에서 실제 DEV fixture ID를 다시 선택하세요."
    });
  });

  it("classifies Access and admin credential failures separately", () => {
    expect(
      adminRequestFailureDisplay(
        new AdminApiError({
          status: 403,
          path: "/api/runs/run-1",
          errorCode: "ADMIN_ACCESS_JWT_INVALID"
        }),
        { resourceLabel: "출격", resourceId: "run-1" }
      ).reason
    ).toBe("Access/admin credential 확인 필요");
  });

  it("classifies response shape failures without exposing raw payloads", () => {
    const display = adminRequestFailureDisplay(new Error("ZodError: invalid_type"), {
      resourceLabel: "출격",
      resourceId: "run-1"
    });

    expect(display.reason).toBe("응답 형식 확인 필요");
    expect(display.errorCode).toBe("없음");
    expect(JSON.stringify(display)).not.toContain("raw payload");
    expect(JSON.stringify(display)).not.toContain("token");
  });

  it("labels read status separately from generic pending state", () => {
    expect(adminReadStatusLabel({ hasData: true, isError: false, isLoading: false })).toBe("조회 가능");
    expect(adminReadStatusLabel({ hasData: false, isError: true, isLoading: false })).toBe(
      "Access/admin credential 확인 필요"
    );
    expect(adminReadStatusLabel({ hasData: false, isError: false, isLoading: true })).toBe("조회 중");
    expect(adminReadStatusLabel({ hasData: false, isError: false, isLoading: false })).toBe("연동 대기");
  });

  it("labels field read values by query state instead of treating empty success as integration pending", () => {
    expect(adminReadValueLabel({ kind: "integration_pending" })).toBe("연동 대기");
    expect(adminReadValueLabel({ kind: "loading" })).toBe("조회 중");
    expect(adminReadValueLabel({ kind: "success", value: "account-1" })).toBe("account-1");
    expect(adminReadValueLabel({ kind: "success", value: "" })).toBe("없음");
    expect(adminReadValueLabel({ kind: "success", value: [] })).toBe("없음");
    expect(adminReadValueLabel({ kind: "success", value: null, emptyLabel: "데이터 없음" })).toBe("데이터 없음");
    expect(
      adminReadValueLabel({
        kind: "error",
        error: new AdminApiError({
          status: 404,
          path: "/api/support/accounts/missing",
          errorCode: "account_not_found"
        }),
        resourceLabel: "CS 계정",
        resourceId: "missing"
      })
    ).toBe("DEV fixture/ID 없음");
  });
});
