import { describe, expect, it } from "vitest";
import {
  auditDecisionLabel,
  reviewStatusLabel,
  runHref,
  runTypeLabel,
  signalEvidenceSummary
} from "./SuspiciousPlayerDetailPage";

describe("SuspiciousPlayerDetailPage helpers", () => {
  it("uses confirmed Korean operations labels for review states", () => {
    expect(reviewStatusLabel("NEW")).toBe("신규");
    expect(reviewStatusLabel("REVIEWING")).toBe("검토 중");
    expect(reviewStatusLabel("REWARD_HELD")).toBe("보상 보류");
  });

  it("labels suspicious run types using the confirmed 출격 and raid wording", () => {
    expect(runTypeLabel("RUN")).toBe("출격");
    expect(runTypeLabel("RAID_RUN")).toBe("레이드 출격");
  });

  it("links suspicious run rows to run detail while keeping decisions read-only", () => {
    expect(runHref("run-1")).toBe("/runs/run-1");
    expect(auditDecisionLabel("INTEGRITY_REVIEW_MARK")).toBe("판정 기록");
  });

  it("formats evidence as compact read-only JSON", () => {
    expect(signalEvidenceSummary({ maxDamageRatio: 12.8 })).toBe('{"maxDamageRatio":12.8}');
  });
});
