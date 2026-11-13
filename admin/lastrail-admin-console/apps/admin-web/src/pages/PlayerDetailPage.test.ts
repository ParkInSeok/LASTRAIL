import { describe, expect, it } from "vitest";
import {
  playerAuthEvidenceCards,
  playerAuthEvidenceCardsForLaunchEvidence,
  playerRecentOpsErrorEvidenceCards,
  playerRecentOpsErrorEvidenceCardsForLaunchEvidence
} from "./PlayerDetailPage";

describe("PlayerDetailPage configuration", () => {
  it("prepares read-only login provider evidence without account mutation controls", () => {
    expect(playerAuthEvidenceCards.map((card) => card.code)).toEqual([
      "identityProviders",
      "providerNames",
      "linkedProviders",
      "primaryProvider",
      "hasGoogleLink",
      "activeSessionCount",
      "latestSessionCreatedAt",
      "latestSessionAt",
      "latestSessionExpiresAt",
      "latestAuthAt",
      "lastLoginAt",
      "lastLoginAgeSeconds",
      "lastSeenAt",
      "lastSeenSource",
      "concurrentUsers",
      "concurrentUsersPrecision"
    ]);
    expect(playerAuthEvidenceCards.map((card) => card.label).join(" ")).toContain("게스트");
    expect(playerAuthEvidenceCards.map((card) => card.label).join(" ")).toContain("Google");
    expect(playerAuthEvidenceCards.map((card) => card.label)).toContain("주 제공자");
    expect(playerAuthEvidenceCards.find((card) => card.code === "concurrentUsers")?.note).toContain(
      "UNAVAILABLE/null은 확정 동접자 수 아님"
    );
    expect(playerAuthEvidenceCards.every((card) => card.value === "연동 대기")).toBe(true);

    const serialized = JSON.stringify(playerAuthEvidenceCards);
    expect(serialized).not.toContain("link button");
    expect(serialized).not.toContain("unlink");
    expect(serialized).not.toContain("merge");
    expect(serialized).not.toContain("revoke");
    expect(serialized).not.toContain("Google ID token");
    expect(serialized).not.toContain("session secret");
    expect(serialized).not.toContain("refresh token hash");
  });

  it("prepares recent ops error evidence placeholders without resolution actions", () => {
    expect(playerRecentOpsErrorEvidenceCards.map((card) => card.code)).toEqual([
      "recentOpsErrorLogs",
      "opsErrorSeverity",
      "opsErrorStatus",
      "opsErrorSource",
      "opsErrorCorrelationIds"
    ]);
    expect(playerRecentOpsErrorEvidenceCards.every((card) => card.value === "연동 대기")).toBe(true);
    expect(playerRecentOpsErrorEvidenceCards.find((card) => card.code === "recentOpsErrorLogs")?.note).toContain(
      "latest 5"
    );
    expect(playerRecentOpsErrorEvidenceCards.find((card) => card.code === "opsErrorCorrelationIds")?.note).toContain(
      "accountId / runId / gachaBannerId"
    );

    const serialized = JSON.stringify(playerRecentOpsErrorEvidenceCards);
    expect(serialized).not.toContain("resolve button");
    expect(serialized).not.toContain("acknowledge button");
    expect(serialized).not.toContain("stack dump");
  });

  it("maps expanded player auth/session read-only evidence into values", () => {
    const cards = playerAuthEvidenceCardsForLaunchEvidence({
      authSession: {
        items: [
          { field: "providerNames", label: "Google/게스트 제공자", value: "GOOGLE / GUEST", note: "providerNames" },
          { field: "hasGoogleLink", label: "Google 연동 여부", value: "연동됨", note: "hasGoogleLink" },
          { field: "lastLoginAt", label: "마지막 로그인 시각", value: "2026-08-31T06:10:00.000Z", note: "lastLoginAt" },
          {
            field: "concurrentUsers",
            label: "동접자 집계 상태",
            value: "정밀 동접자 백로그 (UNAVAILABLE)",
            note: "UNAVAILABLE/null은 확정 동접자 수 아님"
          }
        ]
      },
      opsErrors: [],
      supportContext: { readOnly: true, mutationEndpointsEnabled: false, source: "admin_player_read_model" }
    });

    expect(cards.find((card) => card.code === "providerNames")?.value).toBe("GOOGLE / GUEST");
    expect(cards.find((card) => card.code === "hasGoogleLink")?.value).toBe("연동됨");
    expect(cards.find((card) => card.code === "lastLoginAt")?.value).toBe("2026-08-31T06:10:00.000Z");
    expect(cards.find((card) => card.code === "concurrentUsers")?.value).toContain("UNAVAILABLE");
  });

  it("maps expanded player ops error evidence and empty state", () => {
    expect(
      playerRecentOpsErrorEvidenceCardsForLaunchEvidence({
        authSession: { items: [] },
        opsErrors: [],
        supportContext: { readOnly: true, mutationEndpointsEnabled: false, source: "admin_player_read_model" }
      }).find((card) => card.code === "recentOpsErrorLogs")?.value
    ).toBe("데이터 없음");

    const cards = playerRecentOpsErrorEvidenceCardsForLaunchEvidence({
      authSession: { items: [] },
      opsErrors: [
        {
          errorLogId: "err-1",
          occurredAt: "2026-08-31T06:11:00.000Z",
          environment: "DEV",
          service: "game_api",
          severity: "critical",
          errorCode: "run_start_state_conflict",
          messageSummary: "safe summary",
          status: "open",
          source: "game_api",
          requestId: "req-1",
          correlationIds: {
            accountId: "account-1",
            runId: "run-1",
            rewardPreviewId: null,
            rewardClaimId: null,
            offlineRewardClaimId: null,
            gachaBatchId: null,
            gachaBannerId: null,
            storyProgressId: null,
            integrityEventId: null,
            supportTicketId: null
          },
          links: []
        }
      ],
      supportContext: { readOnly: true, mutationEndpointsEnabled: false, source: "admin_player_read_model" }
    });

    expect(cards.find((card) => card.code === "recentOpsErrorLogs")?.value).toBe("1건");
    expect(cards.find((card) => card.code === "opsErrorSeverity")?.value).toBe("치명 / critical");
    expect(cards.find((card) => card.code === "opsErrorSource")?.value).toBe("게임 API / game_api");
    expect(cards.find((card) => card.code === "opsErrorCorrelationIds")?.value).toContain("runId=run-1");

    const serialized = JSON.stringify(cards);
    expect(serialized).not.toContain("raw auth header");
    expect(serialized).not.toContain("stack dump");
  });
});
