import { afterEach, expect, it, vi } from "vitest";
import { adminApi } from "../api/client";

afterEach(() => vi.unstubAllGlobals());
it("refuses a different or missing account before consuming support records", async () => {
  for (const account of [{ accountId: "other" }, {}]) {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json({ account })));
    await expect(adminApi.supportAccountSummary("requested")).rejects.toMatchObject({ errorCode: "ADMIN_ACCOUNT_RESPONSE_MISMATCH" });
  }
});
it("uses only the encoded read-only support path and preserves matching empty data", async () => {
  const fetcher = vi.fn().mockResolvedValue(Response.json({ account: { accountId: "account/x" } }));
  vi.stubGlobal("fetch", fetcher);
  const result = await adminApi.supportAccountSummary("account/x");
  expect(result.account.accountId).toBe("account/x");
  expect(fetcher.mock.calls[0][0]).toBe("/api/support/accounts/account%2Fx");
  expect(fetcher.mock.calls[0][1]?.method ?? "GET").toBe("GET");
});
