import { describe, expect, it } from "vitest";
import { parseVersion } from "../../packages/common/src/version-policy";

const release = { status: "APPROVED", releaseId: "test-release", contentVersion: "test-content",
  catalogUrl: "https://example.test/releases/test-release/catalog.bin", catalogHash: `sha256:${"a".repeat(64)}`,
  minimumVersionCode: "2", maximumVersionCode: "2" };
const entry = { version: "0.2.0", versionCode: "2", status: "INTERNAL", contentRelease: release };
const version = { version: "0.1.0", versionCode: "1", minimumVersionCode: "1", clientReleaseRegistry: { schemaVersion: 1, entries: [entry] } };

describe("exact version registry wire compatibility", () => {
  it("preserves explicit registry metadata while retaining the legacy format", () => {
    expect(parseVersion(version)).toEqual(version);
    expect(parseVersion({ version: "0.1.0", versionCode: "1", minimumVersionCode: "1" }))
      .toEqual({ version: "0.1.0", versionCode: "1", minimumVersionCode: "1" });
  });
  it("rejects duplicated codes, unknown status and incompatible artifacts", () => {
    expect(parseVersion({ ...version, clientReleaseRegistry: { schemaVersion: 1, entries: [entry, entry] } })).toBeNull();
    expect(parseVersion({ ...version, clientReleaseRegistry: { schemaVersion: 1, entries: [{ ...entry, status: "HIGHER_IS_INTERNAL" }] } })).toBeNull();
    expect(parseVersion({ ...version, clientReleaseRegistry: { schemaVersion: 1, entries: [{ ...entry, versionCode: "3" }] } })).toBeNull();
  });
  it("rejects different artifact metadata sharing a release pin", () => {
    expect(parseVersion({ ...version, clientReleaseRegistry: { schemaVersion: 1, entries: [entry,
      { ...entry, versionCode: "3", contentRelease: { ...release, catalogHash: `sha256:${"b".repeat(64)}`, minimumVersionCode: "3", maximumVersionCode: "3" } },
    ] } })).toBeNull();
  });
});
