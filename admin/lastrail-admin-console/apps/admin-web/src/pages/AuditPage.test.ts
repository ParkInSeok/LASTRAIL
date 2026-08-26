import { describe, expect, it } from "vitest";
import {
  auditFiltersFromSearchParams,
  auditResourceHref,
  auditSearchParamsFromFilters,
  compactAuditSummary
} from "./AuditPage";

describe("AuditPage helpers", () => {
  it("links account and run audit resources to their detail pages", () => {
    expect(auditResourceHref("account", "account-1")).toBe("/players/account-1");
    expect(auditResourceHref("run", "run-1")).toBe("/runs/run-1");
    expect(auditResourceHref("incident", "incident-1")).toBeNull();
    expect(auditResourceHref("account", null)).toBeNull();
  });

  it("formats audit summaries as compact readable text", () => {
    expect(compactAuditSummary({ note: "Reviewed abnormal Training Core spread." })).toBe(
      '{"note":"Reviewed abnormal Training Core spread."}'
    );
    expect(compactAuditSummary(null)).toBe("null");
  });

  it("reads audit filters from URL search params", () => {
    const filters = auditFiltersFromSearchParams(
      new URLSearchParams(
        "action=PLAYER_NOTE_CREATE&resourceId=account-1&result=SUCCESS"
      )
    );

    expect(filters).toEqual({
      action: "PLAYER_NOTE_CREATE",
      resourceId: "account-1",
      result: "SUCCESS"
    });
  });

  it("ignores invalid audit result filters from URL search params", () => {
    const filters = auditFiltersFromSearchParams(
      new URLSearchParams("action=PLAYER_NOTE_CREATE&result=ERROR")
    );

    expect(filters).toEqual({
      action: "PLAYER_NOTE_CREATE",
      resourceId: "",
      result: ""
    });
  });

  it("writes only active audit filters to URL search params", () => {
    const params = auditSearchParamsFromFilters({
      action: " PLAYER_NOTE_CREATE ",
      resourceId: "account-1",
      result: ""
    });

    expect(params.toString()).toBe("action=PLAYER_NOTE_CREATE&resourceId=account-1");
  });
});
