import { describe, expect, it } from "vitest";
import { runTimeLabel } from "./RunsPage";

describe("run list timestamps", () => {
  it("shows server timestamps in KST", () => {
    expect(runTimeLabel("2026-09-06T01:02:03Z")).toBe("2026-09-06 10:02:03");
  });
  it("distinguishes ongoing runs from missing historical timestamps", () => {
    expect(runTimeLabel(undefined, "IN_PROGRESS")).toBe("진행 중");
    expect(runTimeLabel(undefined, "FAILED")).toBe("미기록");
    expect(runTimeLabel(undefined)).toBe("미기록");
    expect(runTimeLabel("invalid")).toBe("미기록");
  });
});
