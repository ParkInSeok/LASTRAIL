import { describe, expect, it } from "vitest";
import {
  suspiciousPlayerHref,
  suspiciousPlayersFiltersFromSearchParams,
  suspiciousPlayersSearchParamsFromFilters
} from "./SuspiciousPlayersPage";

describe("SuspiciousPlayersPage helpers", () => {
  it("links suspicious player rows to the player investigation detail", () => {
    expect(suspiciousPlayerHref("account-1")).toBe("/integrity/suspicious-players/account-1");
  });

  it("reads suspicious player filters from URL search params", () => {
    const filters = suspiciousPlayersFiltersFromSearchParams(
      new URLSearchParams("reviewStatus=REVIEWING")
    );

    expect(filters).toEqual({ reviewStatus: "REVIEWING" });
  });

  it("writes only active suspicious player filters to URL search params", () => {
    const params = suspiciousPlayersSearchParamsFromFilters({ reviewStatus: "" });

    expect(params.toString()).toBe("");
  });
});
