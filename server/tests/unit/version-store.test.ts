import { describe, expect, it, vi } from "vitest";
import { parseVersion, readVersion, saveVersion, type VersionBucket } from "../../apps/admin-api/src/version-store";

describe("DEV current version storage", () => {
  it("accepts inclusive minimum and rejects invalid or above-current minimum", () => {
    expect(parseVersion({ version: "1.0", versionCode: "10", minimumVersionCode: "10" })).toEqual({ version: "1.0", versionCode: "10", minimumVersionCode: "10" });
    for (const minimumVersionCode of ["0", "-1", "11", "1.5", "", null]) {
      expect(parseVersion({ version: "1.0", versionCode: "10", minimumVersionCode })).toBeNull();
    }
    expect(parseVersion({ version: "1.0", versionCode: "10", minimumVersionCode: "2" })?.minimumVersionCode).toBe("2");
  });
  it("validates version codes and strips unknown fields", () => {
    expect(parseVersion({ version: "1.2.3", versionCode: "100", extra: "discard" })).toEqual({ version: "1.2.3", versionCode: "100" });
    for (const versionCode of ["0", "-1", "1.5", "2147483648", "1e3"]) {
      expect(parseVersion({ version: "1.2.3", versionCode })).toBeNull();
    }
    expect(parseVersion({ version: "../x", versionCode: "1" })).toBeNull();
  });
  it("writes the fixed JSON key and reads back saved values", async () => {
    let stored: string | null = null;
    const put = vi.fn(async (_key: string, value: string) => { stored = value; });
    const bucket = { put, get: vi.fn(async () => stored === null ? null : { json: async () => JSON.parse(stored!) }) } as unknown as VersionBucket;
    expect(await readVersion(bucket)).toBeNull();
    const value = { version: "1.2.3", versionCode: "100", minimumVersionCode: "10" };
    await saveVersion(bucket, value);
    expect(put).toHaveBeenCalledWith("versions/current.json", JSON.stringify(value), expect.objectContaining({ httpMetadata: { contentType: "application/json", cacheControl: "no-store" } }));
    expect(await readVersion(bucket)).toEqual(value);
  });
  it("does not treat corrupted data or failed writes as success", async () => {
    const bucket = { get: async () => ({ json: async () => ({}) }), put: async () => { throw new Error("unavailable"); } } as unknown as VersionBucket;
    await expect(readVersion(bucket)).rejects.toThrow("version_document_invalid");
    await expect(saveVersion(bucket, { version: "1", versionCode: "1" })).rejects.toThrow("unavailable");
  });
});
