import { afterEach, expect, it, vi } from "vitest";
import configured from "./fixtures/configured-zero-and-owned-records-21dbd8f1-197f-4623-906e-b885dc205396.json";
import unconfigured from "./fixtures/unconfigured-3d75f579-a6b4-426c-8659-4951cb502510.json";
import unavailable from "./fixtures/unavailable-source-016a8cbf-6207-4af8-b081-cb2d83b15b87.json";
import monthly from "./fixtures/monthly-incomplete-binding-8dafc650-78b0-46c0-872d-20ce941ccf1a.json";
import { mapPlayerActivities } from "./playerActivities";
import { adminApi } from "../api/client";
afterEach(() => vi.unstubAllGlobals());
it("accepts all four actual SQLite fixtures and preserves independent read states", () => {
  for (const fixture of [configured, unconfigured, unavailable, monthly]) {
    expect(mapPlayerActivities(fixture, "a").sections).toHaveLength(5);
  }
  const result = mapPlayerActivities(configured, "a");
  expect(result.sections[0].fields.some(f => f.value === 0)).toBe(true);
  expect(result.sections[0].fields.some(f => f.value === "미관측")).toBe(true);
  expect(JSON.stringify(mapPlayerActivities(unconfigured, "a"))).toContain("정책 미설정");
  expect(JSON.stringify(mapPlayerActivities(unavailable, "a"))).toContain("조회 불가");
  expect(JSON.stringify(mapPlayerActivities(monthly, "a"))).toContain("elite_ticket_binding_missing");
});
it("rejects account/version/section violations and discards unknown sensitive fields", () => {
  expect(() => mapPlayerActivities(configured, "b")).toThrow();
  expect(() => mapPlayerActivities({ ...configured, version: "future" }, "a")).toThrow();
  expect(() => mapPlayerActivities({ ...configured, shop: { availability: "AVAILABLE", data: null } }, "a")).toThrow();
  const safe = mapPlayerActivities({ ...configured, rawPayload: "FORBIDDEN", shop: { ...configured.shop, data: { ...configured.shop.data, token: "FORBIDDEN" } } }, "a");
  expect(JSON.stringify(safe)).not.toContain("FORBIDDEN");
});
it("uses account-scoped GET with no body and rejects a different account", async () => {
  const fetch = vi.fn().mockResolvedValue(Response.json({ ...configured, accountId: "account/x" }));
  vi.stubGlobal("fetch", fetch);
  await adminApi.playerActivities("account/x");
  expect(fetch.mock.calls[0][0]).toBe("/api/players/account%2Fx/activity-summary");
  expect(fetch.mock.calls[0][1]?.body).toBeUndefined();
  await expect(adminApi.playerActivities("other")).rejects.toThrow();
});
