import { describe, expect, it } from "vitest";
import { mutateClientReleaseRegistry, describeClientReleaseRegistry } from "../../apps/admin-api/src/client-release-registry";
import type { VersionBucket } from "../../packages/common/src/version-policy";
import app from "../../apps/admin-api/src/index";

function fixture(target: "DEV" | "PRD" = "PRD") {
  const objects = new Map<string, { text: string; revision: string }>();
  let sequence = 1;
  objects.set("versions/current.json", { text: JSON.stringify({ version: "0.1.0", versionCode: "1", minimumVersionCode: "1" }), revision: '"1"' });
  const bucket: VersionBucket = {
    get: async (key) => { const row = objects.get(key); return row ? { httpEtag: row.revision, json: async () => JSON.parse(row.text) } : null; },
    put: async (key, text, options) => {
      const row = objects.get(key);
      if (options.onlyIf.get("If-None-Match") === "*" ? !!row : options.onlyIf.get("If-Match") !== row?.revision) return null;
      const revision = `"${++sequence}"`; objects.set(key, { text, revision }); return { httpEtag: revision };
    },
  };
  const release = { status: "APPROVED", releaseId: "release2", contentVersion: "content2", catalogUrl: "https://example.test/release2/catalog.bin",
    catalogHash: `sha256:${"a".repeat(64)}`, minimumVersionCode: "2", maximumVersionCode: "2" };
  const mutate = (operation: string, body: Record<string, unknown>, code?: string) => mutateClientReleaseRegistry(bucket, target, "owner-test", operation,
    operation === "register" ? { legacyTransitionConfirmation: "ENABLE_EXACT_LOOKUP", ...body } : body, code);
  return { bucket, release, mutate, objects };
}
describe("registry mutation CAS and unchanged promotion", () => {
  it("DEV registers STABLE, advertises no promotion and forbids INTERNAL without changing minimum", async () => {
    const { bucket, release, mutate, objects } = fixture("DEV");
    const registered = await mutate("register", { target: "DEV", expectedRevision: '"1"', reason: "dev release", version: "0.2", versionCode: "2", contentRelease: release });
    expect(registered).toMatchObject({ minimumVersionCode: "1", capabilities: { promote: false, manageTesters: false },
      distributionPolicy: { supportedStatuses: ["STABLE", "DISABLED"], initialStatus: "STABLE", mutableStatuses: ["STABLE", "DISABLED"], testerGate: false } });
    expect(registered.rows[0].status).toBe("STABLE");
    await expect(mutate("status", { target: "DEV", expectedRevision: registered.revision, reason: "internal invalid", status: "INTERNAL" }, "2")).rejects.toThrow("registry_status_invalid");
    await expect(mutate("promote", { target: "DEV", expectedRevision: registered.revision, reason: "no dev channels", confirmation: "PROMOTE_UNCHANGED_ARTIFACT", expectedArtifactIdentity: registered.rows[0].artifactIdentity }, "2")).rejects.toThrow("registry_promotion_environment_disabled");
    const minimum = await mutate("minimum", { target: "DEV", expectedRevision: registered.revision, reason: "minimum explicit", minimumVersionCode: "2", confirmMinimumChange: true });
    expect(minimum.minimumVersionCode).toBe("2");
    const disabled = await mutate("status", { target: "DEV", expectedRevision: minimum.revision, reason: "disable", status: "DISABLED" }, "2");
    const restored = await mutate("status", { target: "DEV", expectedRevision: disabled.revision, reason: "restore same release", status: "STABLE" }, "2");
    expect(restored.rows[0].artifactIdentity).toBe(registered.rows[0].artifactIdentity);
    const stored = JSON.parse(objects.get("versions/current.json")!.text);
    stored.clientReleaseRegistry.entries[0].status = "INTERNAL";
    objects.set("versions/current.json", { text: JSON.stringify(stored), revision: restored.revision! });
    await expect(describeClientReleaseRegistry(bucket, "DEV", true)).rejects.toThrow("registry_document_environment_invalid");
  });
  it.each(["DEV", "PRD"] as const)("%s requires explicit acknowledgement before first registration cuts over legacy admission", async (target) => {
    const { bucket, release, objects } = fixture(target);
    await expect(mutateClientReleaseRegistry(bucket, target, "owner-test", "register", { target, expectedRevision: '"1"', reason: "ordinary row", version: "0.2", versionCode: "2", contentRelease: release }))
      .rejects.toThrow("registry_transition_confirmation_required");
    expect(objects.size).toBe(1);
  });
  it("DEV rollback keeps later codes reserved and reactivation advances latest without changing minimum", async () => {
    const { release, mutate, objects } = fixture("DEV");
    const first = await mutate("register", { target: "DEV", expectedRevision: '"1"', reason: "first", version: "0.2", versionCode: "2", contentRelease: release });
    const next = await mutate("register", { target: "DEV", expectedRevision: first.revision, reason: "next", version: "0.3", versionCode: "3", contentRelease: { ...release, releaseId: "release3", minimumVersionCode: "3", maximumVersionCode: "3" } });
    const rollback = await mutate("rollback", { target: "DEV", expectedRevision: next.revision, reason: "restore", historyId: next.auditHead, confirmation: "RESTORE_REGISTRY" });
    expect(rollback.rows.find((row) => row.versionCode === "3")?.status).toBe("DISABLED");
    expect(JSON.parse(objects.get("versions/current.json")!.text).versionCode).toBe("2");
    const restored = await mutate("status", { target: "DEV", expectedRevision: rollback.revision, reason: "reactivate", status: "STABLE" }, "3");
    expect(restored.minimumVersionCode).toBe("1");
    expect(restored.rows.find((row) => row.versionCode === "3")?.artifactIdentity).toBe(next.rows.find((row) => row.versionCode === "3")?.artifactIdentity);
    expect(JSON.parse(objects.get("versions/current.json")!.text).versionCode).toBe("3");
  });
  it("DEV rejects rollback to historical INTERNAL without writing history or the current pointer", async () => {
    const { release, mutate, objects } = fixture("DEV");
    const first = await mutate("register", { target: "DEV", expectedRevision: '"1"', reason: "first", version: "0.2", versionCode: "2", contentRelease: release });
    const next = await mutate("register", { target: "DEV", expectedRevision: first.revision, reason: "next", version: "0.3", versionCode: "3", contentRelease: { ...release, releaseId: "release3", minimumVersionCode: "3", maximumVersionCode: "3" } });
    const key = `versions/client-release-history/${next.auditHead}.json`;
    const historical = JSON.parse(objects.get(key)!.text);
    historical.before.clientReleaseRegistry.entries[0].status = "INTERNAL";
    historical.before.clientReleaseRegistry.entries.push(historical.after.clientReleaseRegistry.entries[1]);
    objects.set(key, { ...objects.get(key)!, text: JSON.stringify(historical) });
    const beforeAttempt = structuredClone([...objects]);
    await expect(mutate("rollback", { target: "DEV", expectedRevision: next.revision, reason: "invalid restore", historyId: next.auditHead, confirmation: "RESTORE_REGISTRY" })).rejects.toThrow("registry_document_environment_invalid");
    expect([...objects]).toEqual(beforeAttempt);
  });
  it("registers internal, promotes identical artifact, changes minimum separately, and rolls back without lowering minimum", async () => {
    const { bucket, release, mutate } = fixture();
    const registered = await mutate("register", { target: "PRD", expectedRevision: '"1"', reason: "test register", version: "0.2.0", versionCode: "2", contentRelease: release });
    expect(registered.rows[0].status).toBe("INTERNAL");
    expect(registered).toMatchObject({ capabilities: { promote: true }, distributionPolicy: { initialStatus: "INTERNAL", testerGate: true } });
    expect(registered.minimumVersionCode).toBe("1");
    await expect(mutate("promote", { target: "PRD", expectedRevision: '"1"', reason: "stale", confirmation: "PROMOTE_UNCHANGED_ARTIFACT", expectedArtifactIdentity: registered.rows[0].artifactIdentity }, "2")).rejects.toThrow("version_current_pointer_conflict");
    const promoted = await mutate("promote", { target: "PRD", expectedRevision: registered.revision, reason: "tested", confirmation: "PROMOTE_UNCHANGED_ARTIFACT", expectedArtifactIdentity: registered.rows[0].artifactIdentity }, "2");
    expect(promoted.rows[0]).toMatchObject({ status: "STABLE", contentRelease: release, artifactIdentity: registered.rows[0].artifactIdentity });
    expect(promoted.minimumVersionCode).toBe("1");
    const minimum = await mutate("minimum", { target: "PRD", expectedRevision: promoted.revision, reason: "minimum", minimumVersionCode: "2", confirmMinimumChange: true });
    expect(minimum.minimumVersionCode).toBe("2");
    const rollback = await mutate("rollback", { target: "PRD", expectedRevision: minimum.revision, reason: "restore previous registry", historyId: minimum.auditHead, confirmation: "RESTORE_REGISTRY" });
    expect(rollback.minimumVersionCode).toBe("2");
    expect((await describeClientReleaseRegistry(bucket, "PRD", true)).rows[0].status).toBe("STABLE");
  });
  it("rejects artifact replacement and cross-target writes", async () => {
    const { release, mutate } = fixture();
    await expect(mutate("register", { target: "DEV", expectedRevision: '"1"', reason: "wrong", version: "0.2", versionCode: "2", contentRelease: release })).rejects.toThrow("registry_target_mismatch");
    const first = await mutate("register", { target: "PRD", expectedRevision: '"1"', reason: "register", version: "0.2", versionCode: "2", contentRelease: release });
    await expect(mutate("register", { target: "PRD", expectedRevision: first.revision, reason: "replace", version: "0.2", versionCode: "2", contentRelease: release })).rejects.toThrow("version_code_already_registered");
    await expect(mutate("status", { target: "PRD", expectedRevision: first.revision, reason: "smuggled minimum", status: "DISABLED", minimumVersionCode: "2" }, "2")).rejects.toThrow("registry_body_invalid");
  });
  it("keeps rolled-back registrations reserved so installed codes cannot change artifacts", async () => {
    const { release, mutate } = fixture();
    const registered = await mutate("register", { target: "PRD", expectedRevision: '"1"', reason: "register", version: "0.2", versionCode: "2", contentRelease: release });
    const stable = await mutate("promote", { target: "PRD", expectedRevision: registered.revision, reason: "stable", confirmation: "PROMOTE_UNCHANGED_ARTIFACT", expectedArtifactIdentity: registered.rows[0].artifactIdentity }, "2");
    const next = await mutate("register", { target: "PRD", expectedRevision: stable.revision, reason: "next", version: "0.3", versionCode: "3", contentRelease: { ...release, releaseId: "release3", minimumVersionCode: "3", maximumVersionCode: "3" } });
    const restored = await mutate("rollback", { target: "PRD", expectedRevision: next.revision, reason: "rollback", historyId: next.auditHead, confirmation: "RESTORE_REGISTRY" });
    await expect(mutate("register", { target: "PRD", expectedRevision: restored.revision, reason: "reuse", version: "0.3", versionCode: "3", contentRelease: { ...release, releaseId: "replacement", minimumVersionCode: "3", maximumVersionCode: "3" } })).rejects.toThrow("version_code_already_registered");
    expect(restored.rows.find((row) => row.versionCode === "3")?.status).toBe("DISABLED");
  });
  it("rejects CAS-losing history candidates as rollback targets", async () => {
    const { release, mutate, objects } = fixture();
    const results = await Promise.allSettled(["2", "3"].map((code) => mutate("register", { target: "PRD", expectedRevision: '"1"', reason: "race", version: `0.${code}`, versionCode: code, contentRelease: { ...release, minimumVersionCode: code, maximumVersionCode: code } })));
    expect(results.filter((row) => row.status === "fulfilled")).toHaveLength(1);
    expect(results.filter((row) => row.status === "rejected")).toHaveLength(1);
    const current = JSON.parse(objects.get("versions/current.json")!.text);
    const orphan = [...objects.entries()].filter(([key]) => key.includes("history/")).map(([, row]) => JSON.parse(row.text)).find((row) => row.id !== current.clientReleaseRegistry.auditHead);
    expect(orphan).toBeDefined();
    await expect(mutate("rollback", { target: "PRD", expectedRevision: objects.get("versions/current.json")!.revision, reason: "orphan", historyId: orphan.id, confirmation: "RESTORE_REGISTRY" })).rejects.toThrow("registry_history_not_committed");
  });
  it("wires actual Admin routes with role checks, stale-write rejection, and legacy-export protection", async () => {
    const { bucket, release } = fixture();
    let role = "OWNER";
    const env = { ENVIRONMENT: "DEV", ADMIN_AUTH_MODE: "LOCAL_HEADER", VERSION_BUCKET: bucket,
      DB: { prepare: () => ({ bind: () => ({ first: async () => ({ id: "owner-test", role, status: "ACTIVE" }) }) }) } } as unknown as Env;
    const headers = { "Cf-Access-Subject": "test-owner", "Content-Type": "application/json" };
    const request = (path: string, body?: unknown) => app.request(path, { headers, ...(body ? { method: "POST", body: JSON.stringify(body) } : {}) }, env);
    const read = await request("/v1/admin/client-releases?target=DEV");
    expect(read.status).toBe(200);
    expect(await read.json()).toMatchObject({ registryStatus: "LEGACY", canMutate: true, capabilities: { manageTesters: false } });
    role = "VIEWER";
    const body = { target: "DEV", expectedRevision: '"1"', reason: "register", version: "0.2", versionCode: "2", contentRelease: release, legacyTransitionConfirmation: "ENABLE_EXACT_LOOKUP" };
    expect((await request("/v1/admin/client-releases/register", body)).status).toBe(403);
    role = "OWNER";
    const registered = await request("/v1/admin/client-releases/register", body);
    expect(registered.status).toBe(200);
    const dto = await registered.json() as { revision: string };
    expect((await request("/v1/admin/client-releases/register", body)).status).toBe(409);
    expect((await request("/v1/admin/versions/export", { version: "0.2", versionCode: "2", minimumVersionCode: "1", expectedRevision: dto.revision })).status).toBe(409);
    expect((await request("/v1/admin/client-releases?target=PRD")).status).toBe(400);
  });
});
