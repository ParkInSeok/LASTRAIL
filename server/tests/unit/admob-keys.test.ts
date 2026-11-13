import { describe, expect, it, vi } from "vitest";
import { createAdmobKeyResolver } from "../../apps/game-api/src/admob-keys";

describe("trusted AdMob key retrieval", () => {
  const body = { keys: [{ keyId: 7, base64: "AQID" }] };
  it("uses fixed Google origin and shares a fresh lookup", async () => {
    const fetcher = vi.fn(async () => Response.json(body));
    const resolve = createAdmobKeyResolver(fetcher, () => 100);
    expect(await resolve("7")).toBe("AQID");
    expect(await resolve("7")).toBe("AQID");
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(fetcher.mock.calls[0]?.[0]).toBe("https://www.gstatic.com/admob/reward/verifier-keys.json");
  });
  it("refreshes expired keys and never uses stale data on failure", async () => {
    let now = 0;
    const fetcher = vi.fn(async () => Response.json(body));
    const resolve = createAdmobKeyResolver(fetcher, () => now);
    await resolve("7");
    now = 86400000;
    fetcher.mockImplementationOnce(async () => new Response("failure", { status: 503 }));
    await expect(resolve("7")).rejects.toThrow("ad_ssv_keys_unavailable");
    expect(await resolve("7")).toBe("AQID");
    expect(fetcher).toHaveBeenCalledTimes(3);
  });
  it("discovers an unknown rotated key inside the provider retry window", async () => {
    let now = 0;
    const fetcher = vi.fn(async () => Response.json(body));
    const resolve = createAdmobKeyResolver(fetcher, () => now);
    await resolve("7");
    now = 1000;
    fetcher.mockImplementationOnce(async () => Response.json({ keys: [...body.keys, { keyId: 8, base64: "BAUG" }] }));
    expect(await resolve("8")).toBe("BAUG");
    expect(fetcher).toHaveBeenCalledTimes(2);
  });
  it.each([
    { keys: [] }, { keys: [{ keyId: 7, base64: "AQID" }, { keyId: 7, base64: "BAUG" }] },
    { keys: [{ keyId: "7", base64: "AQID" }] }, { keys: [{ keyId: 7, base64: "invalid!" }] },
  ])("rejects invalid key documents", async (data) => {
    await expect(createAdmobKeyResolver(async () => Response.json(data))("7")).rejects.toThrow("ad_ssv_keys_unavailable");
  });
  it("bounds an oversized streamed document", async () => {
    await expect(createAdmobKeyResolver(async () => new Response("x".repeat(65537)))("7")).rejects.toThrow("ad_ssv_keys_unavailable");
  });
  it("coalesces concurrent refresh without shared user data", async () => {
    const fetcher = vi.fn(async () => Response.json(body));
    const resolve = createAdmobKeyResolver(fetcher);
    expect(await Promise.all([resolve("7"), resolve("7"), resolve("8")])).toEqual(["AQID", "AQID", null]);
    expect(fetcher).toHaveBeenCalledTimes(1);
  });
});
