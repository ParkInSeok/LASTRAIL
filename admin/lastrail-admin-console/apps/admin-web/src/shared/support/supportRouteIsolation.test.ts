import { afterEach, expect, it, vi } from "vitest";
import { QueryClient } from "@tanstack/react-query";
import { adminApi } from "../api/client";
import { createSupportRouteFixture } from "../../../scripts/support-route-fixtures";
afterEach(() => vi.unstubAllGlobals());
it("isolates actual client query keys when A finishes after B", async () => {
  const fixture = createSupportRouteFixture();
  vi.stubGlobal("fetch", fixture.fetch);
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const a = client.fetchQuery({ queryKey: ["player-activities", "account_a"], queryFn: () => adminApi.playerActivities("account_a") });
  const b = await client.fetchQuery({ queryKey: ["player-activities", "account_b"], queryFn: () => adminApi.playerActivities("account_b") });
  expect(b.accountId).toBe("account_b");
  fixture.releaseA();
  await a;
  expect(client.getQueryData(["player-activities", "account_b"])).toBe(b);
  expect(JSON.stringify(b)).not.toContain("A_SHOP");
  client.clear();
});
it("preserves denied and partial unavailable states with no remote fallback", async () => {
  const fixture = createSupportRouteFixture();
  vi.stubGlobal("fetch", fixture.fetch);
  await expect(adminApi.playerActivities("account_denied")).rejects.toMatchObject({ status: 403 });
  const partial = await adminApi.playerActivities("account_partial");
  expect(partial.sections[0].fields[0].value).toBe("조회 불가");
  expect(partial.sections.find(s => s.label === "패스")?.fields.some(f => f.value === 0)).toBe(true);
  await expect(fixture.fetch("https://outside.invalid/")).rejects.toThrow();
  await expect(fixture.fetch("/api/players/account_b/activity-summary", { method: "POST" })).rejects.toThrow();
});
