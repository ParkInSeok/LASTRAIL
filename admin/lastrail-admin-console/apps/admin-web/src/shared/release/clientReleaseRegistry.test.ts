import { describe, expect, it } from "vitest";
import { mapClientRegistry, buildRegistryCommand } from "./clientReleaseRegistry";

export const content = { status: "APPROVED", releaseId: "local-release", contentVersion: "local-content", catalogUrl: "https://example.invalid/catalog.bin", catalogHash: "sha256:" + "a".repeat(64), minimumVersionCode: "2", maximumVersionCode: "2" };
export const registryFixture = { target: "DEV", registryStatus: "CONFIGURED", revision: '"r1"', minimumVersionCode: "1", auditHead: "local-history", canMutate: true, capabilities: { register: true, changeStatus: true, promote: true, changeMinimum: true, rollback: true, manageTesters: false }, rows: [{ version: "0.2.0", versionCode: "2", status: "INTERNAL", contentRelease: content, artifactIdentity: "sha256:" + "b".repeat(64), validation: { status: "STRUCTURAL_ONLY", artifactBytesVerified: false } }] };
const context = { target: "DEV" as const, editingRevision: '"r1"', readUnavailable: false, needsReview: false };
const promote = { operation: "promote" as const, versionCode: "2", reason: "local contract test", confirmed: true };

describe("actual local client registry contract", () => {
  it("whitelists response and preserves unknown statuses without byte-verification inference", () => {
    const value = mapClientRegistry({ ...registryFixture, secret: "discard", rows: [{ ...registryFixture.rows[0], status: "FUTURE" }] });
    expect(value.rows[0].status).toBe("FUTURE");
    expect(value.rows[0].validation.artifactBytesVerified).toBe(false);
    expect(JSON.stringify(value)).not.toContain("discard");
    expect(mapClientRegistry({ ...registryFixture, revision: null }).revision).toBeNull();
    expect(() => mapClientRegistry({ ...registryFixture, target: "UNKNOWN" })).toThrow();
  });
  it("builds exact unchanged-artifact promotion without minimum fields", () => {
    const command = buildRegistryCommand(mapClientRegistry(registryFixture), context, promote);
    expect(command.path).toBe("/api/client-releases/2/promote");
    expect(command.body).toEqual({ target: "DEV", expectedRevision: '"r1"', reason: "local contract test", confirmation: "PROMOTE_UNCHANGED_ARTIFACT", expectedArtifactIdentity: registryFixture.rows[0].artifactIdentity });
    expect(() => buildRegistryCommand(mapClientRegistry(registryFixture), context, { ...promote, confirmed: false })).toThrow();
  });
  it("fails closed on target, readonly, stale, unavailable and unregistered selections", () => {
    const state = mapClientRegistry(registryFixture);
    for (const ctx of [{ ...context, target: "PRD" as const }, { ...context, editingRevision: '"old"' }, { ...context, readUnavailable: true }, { ...context, needsReview: true }]) expect(() => buildRegistryCommand(state, ctx, promote)).toThrow();
    for (const override of [{ canMutate: false }, { revision: null }, { capabilities: {} }]) expect(() => buildRegistryCommand(mapClientRegistry({ ...registryFixture, ...override }), context, promote)).toThrow();
    expect(() => buildRegistryCommand(state, context, { ...promote, versionCode: "3" })).toThrow();
  });
  it("registers INTERNAL without artifact edits and blocks reused codes", () => {
    const state = mapClientRegistry({ ...registryFixture, registryStatus: "LEGACY", rows: [] });
    const input = { operation: "register" as const, version: "0.2.0", versionCode: "2", contentRelease: content, reason: "local registration", confirmed: true };
    expect(buildRegistryCommand(state, context, input).body).toEqual({ target: "DEV", expectedRevision: '"r1"', reason: input.reason, version: input.version, versionCode: "2", contentRelease: content, legacyTransitionConfirmation: "ENABLE_EXACT_LOOKUP" });
    expect(buildRegistryCommand({ ...state, registryStatus: "CONFIGURED" }, context, input).body).not.toHaveProperty("legacyTransitionConfirmation");
    expect(() => buildRegistryCommand(mapClientRegistry(registryFixture), context, input)).toThrow();
    expect(() => buildRegistryCommand(state, context, { ...input, confirmed: false })).toThrow();
  });
  it("requires independent minimum confirmation and a usable stable row", () => {
    const input = { operation: "minimum" as const, minimumVersionCode: "2", reason: "drop older support", confirmed: true };
    expect(() => buildRegistryCommand(mapClientRegistry(registryFixture), context, input)).toThrow();
    const state = mapClientRegistry({ ...registryFixture, rows: [{ ...registryFixture.rows[0], status: "STABLE" }] });
    expect(buildRegistryCommand(state, context, input).body).toEqual({ target: "DEV", expectedRevision: '"r1"', reason: input.reason, minimumVersionCode: "2", confirmMinimumChange: true });
    expect(() => buildRegistryCommand(state, context, { ...input, confirmed: false })).toThrow();
    expect(() => buildRegistryCommand(state, context, { ...input, minimumVersionCode: "3" })).toThrow();
  });
  it("rejects unsupported status edits and never infers STABLE from a higher code", () => {
    const state = mapClientRegistry(registryFixture);
    expect(() => buildRegistryCommand(state, context, { operation: "status", versionCode: "2", reason: "local test", status: "STABLE" as "INTERNAL" })).toThrow();
    expect(state.rows[0].status).toBe("INTERNAL");
    expect(buildRegistryCommand(state, context, { operation: "status", versionCode: "2", reason: "local test", status: "DISABLED" }).body).toEqual({ target: "DEV", expectedRevision: '"r1"', reason: "local test", status: "DISABLED" });
  });
});
