import { describe, expect, it } from "vitest";
import { navigationItemUsesExactActiveMatch, visibleNavigation } from "./navigation";

describe("admin navigation", () => {
  it("shows read-only player and run routes when the admin has read permissions", () => {
    const nav = visibleNavigation(["ACCOUNT_READ", "RUN_READ", "ECONOMY_READ"]);
    const labels = nav.flatMap((group) => group.items.map((item) => item.label));

    expect(labels).toContain("플레이어");
    expect(labels).toContain("출격");
    expect(labels).toContain("요원 모집");
    expect(labels).toContain("CS 지원");
    expect(labels).not.toContain("시스템 에러 로그");
    expect(labels).not.toContain("버전 관리");
    expect(labels).not.toContain("밸런스 게시");
  });

  it("shows system error logs for admins with audit read permission", () => {
    const nav = visibleNavigation(["AUDIT_READ"]);
    const items = nav.flatMap((group) => group.items);

    expect(items).toContainEqual({
      label: "시스템 에러 로그",
      path: "/ops/error-logs",
      permission: "AUDIT_READ"
    });
  });

  it("hides the removed offline reward page even with read permission", () => {
    const nav = visibleNavigation(["OFFLINE_REWARD_READ"]);
    const items = nav.flatMap((group) => group.items);

    expect(items).not.toContainEqual({
      label: "오프라인 보상",
      path: "/offline-rewards",
      permission: "OFFLINE_REWARD_READ"
    });
  });

  it("shows version management for admins with balance read permission without finalizing a release permission enum", () => {
    const nav = visibleNavigation(["BALANCE_READ"]);
    const items = nav.flatMap((group) => group.items);

    expect(items).toContainEqual({
      label: "버전 관리",
      path: "/versions",
      permission: "BALANCE_READ"
    });
  });

  it("uses exact active matching for parent routes that also have child navigation entries", () => {
    const nav = visibleNavigation(["INTEGRITY_READ"]);
    const items = nav.flatMap((group) => group.items);

    expect(navigationItemUsesExactActiveMatch("/integrity", items)).toBe(true);
    expect(navigationItemUsesExactActiveMatch("/integrity/suspicious-players", items)).toBe(false);
  });
});
