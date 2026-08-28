import { describe, expect, it } from "vitest";
import { playerAuthEvidenceCards, playerRecentOpsErrorEvidenceCards } from "./PlayerDetailPage";

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
});
