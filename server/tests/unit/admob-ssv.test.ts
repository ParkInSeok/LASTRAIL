import { generateKeyPairSync, sign } from "node:crypto";
import { describe, expect, it } from "vitest";
import { verifyAdmobCallback } from "../../apps/game-api/src/admob-ssv";

// Synthetic local key, never a Google key or a production trust configuration.
const pair = generateKeyPairSync("ec", { namedCurve: "prime256v1" });
const publicKey = pair.publicKey.export({ type: "spki", format: "der" }).toString("base64");
const content = "ad_network=1&ad_unit=unit&custom_data=challenge%2Bopaque&reward_amount=1&reward_item=reward&timestamp=1800000000000&transaction_id=abc123&user_id=owner";
const binding = { adUnit: "unit", customData: "challenge+opaque", userId: "owner", rewardAmount: "1", rewardItem: "reward" };
function callback(data = content): string {
  return `${data}&signature=${sign("sha256", Buffer.from(data), pair.privateKey).toString("base64url")}&key_id=7`;
}
const key = async (id: string) => id === "7" ? publicKey : null;

describe("AdMob SSV cryptographic and intent boundary", () => {
  it("verifies exact raw bytes and binds decoded custom data without paying", async () => {
    expect(await verifyAdmobCallback(callback(), key, binding)).toEqual({
      provider: "ADMOB", transactionId: "abc123", timestamp: 1800000000000,
      adUnit: "unit", userId: "owner", customData: "challenge+opaque",
    });
  });
  it.each([
    (q: string) => q.replace("unit", "evil"),
    (q: string) => q.replace("%2B", "%2b"),
    (q: string) => q.replace("ad_network=1&ad_unit=unit", "ad_unit=unit&ad_network=1"),
    (q: string) => q.replace("key_id=7", "key_id=8"),
    (q: string) => q.replace(/signature=[^&]+/, "signature=AAAA"),
    (q: string) => `${q}&user_id=evil`,
  ])("rejects modified wire or untrusted key (%#)", async (modify) => {
    await expect(verifyAdmobCallback(modify(callback()), key, binding)).rejects.toThrow();
  });
  it.each(["user_id=evil&", "custom_data=evil&", "%75ser_id=evil&", "key_id=7&", "signature=AAAA&"])("rejects signed duplicate field %s", async (prefix) => {
    await expect(verifyAdmobCallback(callback(prefix + content), key, binding)).rejects.toThrow("ad_ssv_malformed");
  });
  it.each(["adUnit", "customData", "userId", "rewardAmount", "rewardItem"] as const)("rejects authentic callback for another %s", async (field) => {
    await expect(verifyAdmobCallback(callback(), key, { ...binding, [field]: "other" })).rejects.toThrow("ad_ssv_binding_mismatch");
  });
  it.each(["-1", "1.2", "9007199254740992", "NaN"])("rejects invalid signed timestamp %s", async (timestamp) => {
    await expect(verifyAdmobCallback(callback(content.replace("1800000000000", timestamp)), key, binding)).rejects.toThrow("ad_ssv_malformed");
  });
  it("rejects malformed escape rather than silently replacing it", async () => {
    await expect(verifyAdmobCallback(callback(content.replace("%2B", "%XX")), key, binding)).rejects.toThrow("ad_ssv_malformed");
  });
  it("does not hide key-provider failure as valid verification", async () => {
    await expect(verifyAdmobCallback(callback(), async () => { throw new Error("unavailable"); }, binding)).rejects.toThrow("unavailable");
  });
});
