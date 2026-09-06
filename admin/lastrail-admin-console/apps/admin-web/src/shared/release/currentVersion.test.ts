import { describe, expect, it } from "vitest";
import { mapCurrentVersion } from "./releaseManagementContract";

describe("current version response", () => {
  it("preserves empty storage and owner permission without inventing a version", () => {
    expect(mapCurrentVersion({ server: "dev-admin.lastrail.app", canExport: true, version: null, versionCode: null })).toEqual({ server: "dev-admin.lastrail.app", canExport: true, version: null, versionCode: null });
  });
  it("whitelists saved values and preserves denied export permission", () => {
    expect(mapCurrentVersion({ server: "dev-admin.lastrail.app", canExport: false, version: "1.2.3", versionCode: "100", extra: "discard" })).toEqual({ server: "dev-admin.lastrail.app", canExport: false, version: "1.2.3", versionCode: "100" });
  });
  it("rejects malformed or error responses instead of showing successful empty storage", () => {
    expect(() => mapCurrentVersion({ error: "unavailable" })).toThrow();
    expect(() => mapCurrentVersion({ server: "DEV", canExport: "true", version: null, versionCode: null })).toThrow();
  });
});
