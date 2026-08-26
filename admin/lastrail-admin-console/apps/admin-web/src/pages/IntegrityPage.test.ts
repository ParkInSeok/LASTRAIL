import { describe, expect, it } from "vitest";
import {
  integrityEventHref,
  integrityFiltersFromSearchParams,
  integritySearchParamsFromFilters
} from "./IntegrityPage";

describe("IntegrityPage helpers", () => {
  it("links integrity events to player and run investigation targets", () => {
    expect(integrityEventHref("account-1", "run-1")).toEqual({
      playerHref: "/players/account-1",
      runHref: "/runs/run-1"
    });
    expect(integrityEventHref("account-1", null)).toEqual({
      playerHref: "/players/account-1",
      runHref: null
    });
  });

  it("reads integrity filters from URL search params", () => {
    const filters = integrityFiltersFromSearchParams(
      new URLSearchParams(
        "accountId=account-1&classification=SOFT_ANOMALY&reviewState=OPEN"
      )
    );

    expect(filters).toEqual({
      accountId: "account-1",
      classification: "SOFT_ANOMALY",
      reviewState: "OPEN"
    });
  });

  it("writes only active integrity filters to URL search params", () => {
    const params = integritySearchParamsFromFilters({
      accountId: " account-1 ",
      classification: "",
      reviewState: "IN_REVIEW"
    });

    expect(params.toString()).toBe("accountId=account-1&reviewState=IN_REVIEW");
  });
});
