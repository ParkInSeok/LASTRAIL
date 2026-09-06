import { describe, expect, it } from "vitest";
import { bannerTypeLabel, historyFilters } from "./GachaHistory";

describe("gacha history", () => {
  it("labels banner types without guessing unknown types", () => {
    expect(bannerTypeLabel("PICKUP")).toBe("픽업");
    expect(bannerTypeLabel("STANDARD")).toBe("일반");
    expect(bannerTypeLabel("FUTURE")).toBe("FUTURE");
    expect(bannerTypeLabel(undefined)).toBe("유형 미기록");
  });
  it("keeps account and banner filters in paginated requests", () => {
    expect(historyFilters(" account-1 ", "banner-1", "25")).toEqual({accountId:"account-1", bannerId:"banner-1", cursor:"25", limit:"25"});
  });
});
