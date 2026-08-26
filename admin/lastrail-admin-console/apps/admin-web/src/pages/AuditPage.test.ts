import { describe, expect, it } from "vitest";
import { auditResourceHref, compactAuditSummary } from "./AuditPage";

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
});
