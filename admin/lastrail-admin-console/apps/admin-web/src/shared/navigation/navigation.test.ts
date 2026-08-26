import { describe, expect, it } from "vitest";
import { visibleNavigation } from "./navigation";

describe("admin navigation", () => {
  it("shows read-only player and run routes when the admin has read permissions", () => {
    const nav = visibleNavigation(["ACCOUNT_READ", "RUN_READ", "ECONOMY_READ"]);
    const labels = nav.flatMap((group) => group.items.map((item) => item.label));

    expect(labels).toContain("플레이어");
    expect(labels).toContain("출격");
    expect(labels).toContain("요원 모집");
    expect(labels).not.toContain("시스템 에러 로그");
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
});
