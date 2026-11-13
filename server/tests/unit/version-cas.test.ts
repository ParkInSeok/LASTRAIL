import { describe, expect, it } from "vitest";
import { saveVersion, type VersionBucket } from "@lastrail/common";

const value = { version: "0.1.0", versionCode: "1", minimumVersionCode: "1" };
function store(initial: unknown = value) {
  let data = initial;
  let revision = initial === null ? 0 : 1;
  const bucket: VersionBucket = {
    async get() { const captured = data; const etag = `"${revision}"`; return captured === null ? null : { httpEtag: etag, json: async () => captured }; },
    async put(_key, body, options) {
      if (options.onlyIf?.get("If-None-Match") === "*" && data !== null) return null;
      if (options.onlyIf?.has("If-Match") && options.onlyIf.get("If-Match") !== `"${revision}"`) return null;
      data = JSON.parse(body); revision++; return { etag: String(revision) };
    },
  };
  return { bucket, read: () => data };
}
describe("version atomic writes", () => {
  it.each([value, null])("only one competing writer succeeds for existing/missing object %j", async (initial) => {
    const s = store(initial);
    const results = await Promise.allSettled([saveVersion(s.bucket, value), saveVersion(s.bucket, { ...value, version: "0.1.1" })]);
    expect(results.filter(r => r.status === "fulfilled")).toHaveLength(1);
    expect(results.filter(r => r.status === "rejected")).toHaveLength(1);
  });
  it("rejects stale expected etag without overwriting current bytes", async () => {
    const s = store();
    await saveVersion(s.bucket, value, { expectedEtag: '"1"' });
    await expect(saveVersion(s.bucket, { ...value, version: "stale" }, { expectedEtag: '"1"' })).rejects.toThrow("version_current_pointer_conflict");
    expect(s.read()).toEqual(value);
  });
  it("never reports a null write as success", async () => {
    const s = store(); s.bucket.put = async () => null;
    await expect(saveVersion(s.bucket, value)).rejects.toThrow("version_current_pointer_conflict");
  });
  it("fails closed when an existing object lacks an etag", async () => {
    const s = store(); s.bucket.get = async () => ({ json: async () => value });
    await expect(saveVersion(s.bucket, value)).rejects.toThrow("version_etag_unavailable");
  });
  it("propagates store failure", async () => {
    const s = store(); s.bucket.put = async () => { throw new Error("offline"); };
    await expect(saveVersion(s.bucket, value)).rejects.toThrow("offline");
  });
  it("does not create over an existing object when absence was expected", async () => {
    const s = store();
    await expect(saveVersion(s.bucket, { ...value, version: "new" }, { expectedEtag: null })).rejects.toThrow("version_current_pointer_conflict");
    expect(s.read()).toEqual(value);
  });
});
