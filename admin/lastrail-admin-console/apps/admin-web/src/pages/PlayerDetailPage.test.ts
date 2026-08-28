import { describe, expect, it } from "vitest";
import { playerAuthEvidenceCards } from "./PlayerDetailPage";

describe("PlayerDetailPage configuration", () => {
  it("prepares read-only login provider evidence without account mutation controls", () => {
    expect(playerAuthEvidenceCards.map((card) => card.code)).toEqual([
      "providerNames",
      "linkedProviders",
      "accountState",
      "linkStatus",
      "lastLoginAt",
      "lastLoginAge",
      "activeSessionCount",
      "latestSessionAt",
      "sanitizedIdentityRef"
    ]);
    expect(playerAuthEvidenceCards.map((card) => card.label)).toContain("게스트");
    expect(playerAuthEvidenceCards.map((card) => card.label)).toContain("Google");
    expect(playerAuthEvidenceCards.every((card) => card.value === "연동 대기")).toBe(true);

    const serialized = JSON.stringify(playerAuthEvidenceCards);
    expect(serialized).not.toContain("link button");
    expect(serialized).not.toContain("unlink");
    expect(serialized).not.toContain("merge");
    expect(serialized).not.toContain("revoke");
    expect(serialized).not.toContain("Google ID token");
    expect(serialized).not.toContain("session secret");
  });
});
