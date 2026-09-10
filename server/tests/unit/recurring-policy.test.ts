import { describe, expect, it } from "vitest";
import { qualifyObjectiveFinish, objectivePeriods, pass50Rewards } from "../../apps/game-api/src/recurring-policy";
describe("approved shared objective policy", () => {
  it("credits ordinary normal defeats only after completed waves and keeps unsupported story defeats explicit", () => {
    expect(qualifyObjectiveFinish("ORDINARY_15", "FAILURE", 0)).toMatchObject({ ends: 0, waves: 0 });
    for (const wave of [1, 7, 14]) expect(qualifyObjectiveFinish("ORDINARY_15", "FAILURE", wave)).toMatchObject({ ends: 1, waves: wave });
    expect(qualifyObjectiveFinish("ORDINARY_15", "FAILURE", 15).status).toBe("INVALID");
    expect(qualifyObjectiveFinish("STORY_15", "FAILURE", 5).status).toBe("UNSUPPORTED_STORY_DEFEAT");
    expect(qualifyObjectiveFinish("STORY_BOSS", "VICTORY", 0, true)).toMatchObject({ ends: 1, waves: 0 });
    expect(qualifyObjectiveFinish("STORY_BOSS", "VICTORY", 0, false)).toMatchObject({ ends: 0, waves: 0 });
    expect(qualifyObjectiveFinish("STORY_BOSS", "FAILURE", 0)).toMatchObject({ ends: 0, waves: 0 });
    expect(qualifyObjectiveFinish("ORDINARY_15", "ABANDONED", 7)).toMatchObject({ ends: 0, waves: 0 });
    expect(qualifyObjectiveFinish("WORLD", "VICTORY", 15)).toMatchObject({ ends: 0, waves: 0 });
  });
  it("keeps Monday weeks separate from four season-relative blocks and disables week5", () => {
    const p = objectivePeriods(Date.parse("2026-09-07T00:00:00Z"));
    expect(p.passWeek).toBe(0); expect(p.weekStart).toBe(Date.parse("2026-09-06T19:00:00Z"));
    expect(objectivePeriods(Date.parse("2026-09-08T00:00:00Z")).passWeek).toBe(1);
    expect(objectivePeriods(Date.parse("2026-09-29T00:00:00Z")).passWeek).toBe(null);
  });
  it("keeps 50 levels and exact fivefold premium materials without invented cosmetics", () => {
    const rows = pass50Rewards(); expect(rows).toHaveLength(50);
    expect(rows[49]!.xp).toBe(5000);
    for (const row of rows) expect(row.premium[0]!.quantity).toBe(row.free[0]!.quantity * 5);
    expect(rows[29]!.cosmeticSlot).toBe("FRAME");
  });
});
