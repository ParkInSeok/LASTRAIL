import { describe, expect, it } from "vitest";
import { parseActivityPolicy, attendanceDay } from "../../apps/game-api/src/activity-policy";

const monday = Date.parse("2026-09-06T19:00:00Z");
const policy = { kind: "ATTENDANCE", startAt: monday, endAt: monday + 86400000, claimUntil: monday + 86400000,
  resetOffsetMinutes: 1140, counting: "DISTINCT_DAYS", dailyRewards: [], milestones: [], recovery: null };
describe("approved KST 04:00 activity clock", () => {
  it("increments at exactly 04:00 KST and rejects obsolete 05:00 config", () => {
    expect(attendanceDay(monday, 1140)).toBe(attendanceDay(monday - 1, 1140) + 1);
    expect(() => parseActivityPolicy(JSON.stringify({ ...policy, resetOffsetMinutes: 1200 }))).toThrow();
    expect(() => parseActivityPolicy(JSON.stringify(policy))).not.toThrow();
  });
  it("rejects pass dates not aligned to 04:00 KST and nonascending thresholds", () => {
    const pass = { kind: "PASS", startAt: monday, endAt: monday + 86400000, claimUntil: monday + 86400000,
      levels: [{ xp: 5, free: [], premium: [] }], runXp: {}, purchase: null };
    expect(() => parseActivityPolicy(JSON.stringify(pass))).not.toThrow();
    expect(() => parseActivityPolicy(JSON.stringify({ ...pass, endAt: monday + 86400000 - 1 }))).toThrow();
    expect(() => parseActivityPolicy(JSON.stringify({ ...pass, levels: [...pass.levels, ...pass.levels] }))).toThrow();
  });
});
