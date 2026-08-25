import { describe, expect, it } from "vitest";
import { visibleNavigation } from "./navigation";

describe("admin navigation", () => {
  it("shows read-only player and run routes when the admin has read permissions", () => {
    const nav = visibleNavigation(["ACCOUNT_READ", "RUN_READ"]);
    const labels = nav.flatMap((group) => group.items.map((item) => item.label));

    expect(labels).toContain("Players");
    expect(labels).toContain("Runs");
    expect(labels).not.toContain("Balance Publish");
  });
});

