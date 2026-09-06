import { describe, expect, it } from "vitest";
import { mapCurrentVersion } from "./releaseManagementContract";

describe("current version response", () => {
  it("reads safe policy metadata without treating it as runtime enforcement proof", () => {
    const result = mapCurrentVersion({server:"DEV",canExport:true,version:"1",versionCode:"1",contentRelease:{releaseId:"r1",contentVersion:"c1",status:"APPROVED",secret:"drop"},admissionPolicy:{status:"CONFIGURED",gameApiEnforcement:"CLIENT_CONFIG_AND_PROTECTED_ROUTES"}});
    expect(result.contentRelease).toEqual({releaseId:"r1",contentVersion:"c1",status:"APPROVED"});
    expect(result.admissionPolicy?.status).toBe("CONFIGURED");
  });
  it("maps minimum policy and rejects a minimum above the current version", () => {
    const base = { server: "DEV", canExport: true, version: "1.0", versionCode: "10" };
    expect(mapCurrentVersion({ ...base, minimumVersionCode: "2" }).minimumVersionCode).toBe("2");
    expect(mapCurrentVersion({ ...base, minimumVersionCode: null }).minimumVersionCode).toBeUndefined();
    expect(() => mapCurrentVersion({ ...base, minimumVersionCode: "11" })).toThrow();
  });
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
