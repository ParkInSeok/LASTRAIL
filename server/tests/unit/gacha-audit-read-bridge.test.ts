import { describe, expect, it } from "vitest";
import { startGachaAuditReadBridge } from "../helpers/gacha-audit-read-bridge";

describe("local audit GET-only bridge", () => {
  it("delegates allowed list/detail/banner reads and blocks mutation, unrelated paths and foreign origin", async () => {
    const calls: string[] = [];
    const bridge = await startGachaAuditReadBridge(async (path) => { calls.push(path); return Response.json({ path }); });
    try {
      for (const path of ["/v1/admin/gacha/pull-batches?limit=2", "/v1/admin/gacha/pull-batches/gacha_batch_a", "/v1/admin/gacha/banners"]) {
        const response = await fetch(bridge.url + path);
        expect(response.status).toBe(200);
        expect(await response.json()).toEqual({ path });
      }
      expect((await fetch(bridge.url + "/v1/admin/gacha/pull-batches", { method: "POST" })).status).toBe(405);
      expect((await fetch(bridge.url + "/v1/admin/versions")).status).toBe(404);
      expect((await fetch(bridge.url + "/v1/admin/gacha/banners", { headers: { Origin: "https://untrusted.invalid" } })).status).toBe(403);
      expect(calls).toHaveLength(3);
    } finally { bridge.close(); await bridge.closed; }
  });
});
