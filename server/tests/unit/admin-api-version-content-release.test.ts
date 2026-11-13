import { describe, expect, it, vi } from "vitest";
import app from "../../apps/admin-api/src/index";

const contentRelease = {
  status: "APPROVED",
  releaseId: "content_act1_001",
  contentVersion: "act1.2026-09-06.001",
  catalogUrl: "https://assets.lastrail.app/addressables/catalog.json",
  catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  minimumVersionCode: "90",
  maximumVersionCode: "100",
};

function createEnv(initialVersion: unknown): Env & { VERSION_BUCKET: { __readStored(): unknown } } {
  let stored: string | null = initialVersion === null ? null : JSON.stringify(initialVersion);
  let revision = 1;
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    VERSION_BUCKET: {
      get: vi.fn(async () => {
        const snapshot = stored;
        if (snapshot === null) return null;
        return { httpEtag: `"${revision}"`, json: async () => JSON.parse(snapshot) as unknown };
      }),
      put: vi.fn(async (_key: string, value: string, options?: { onlyIf?: Headers }) => {
        if (options?.onlyIf?.get("If-None-Match") === "*" && stored !== null) return null;
        if (options?.onlyIf?.has("If-Match") && options.onlyIf.get("If-Match") !== `"${revision}"`) return null;
        stored = value;
        revision++;
        return { etag: String(revision) };
      }),
      __readStored: () => stored === null ? null : JSON.parse(stored) as unknown,
    },
    DB: {
      prepare(sql: string) {
        expect(sql).toContain("admin_principals");
        return {
          bind(accessSubject: string) {
            expect(accessSubject).toBe("access|owner-1");
            return {
              first: async () => ({
                id: "principal-owner",
                access_subject: "access|owner-1",
                role: "OWNER",
                status: "ACTIVE",
                display_name: "Owner",
              }),
            };
          },
        };
      },
    } as unknown as D1Database,
  } as unknown as Env & { VERSION_BUCKET: { __readStored(): unknown } };
}

describe("admin-api version content release safety", () => {
  const headers = { "Cf-Access-Subject": "access|owner-1", "Content-Type": "application/json" };
  const base = { version: "1.0.0", versionCode: "100", minimumVersionCode: "90", contentRelease };
  const draft = { version: "1.0.1", versionCode: "101", minimumVersionCode: "90" };
  const read = async (env: Env) => (await app.request("/v1/admin/versions", { headers }, env)).json() as Promise<{ revision: string | null }>;
  const save = (env: Env, body: unknown) => app.request("/v1/admin/versions/export", { method: "POST", headers, body: JSON.stringify(body) }, env);

  it("rejects a stale browser form after another editor saves, then permits reviewed fresh revision", async () => {
    const env = createEnv(base);
    const viewed = await read(env);
    expect(viewed.revision).toBe('"1"');
    expect((await save(env, { ...draft, expectedRevision: viewed.revision })).status).toBe(200);
    const before = env.VERSION_BUCKET.__readStored();
    const stale = await save(env, { ...base, expectedRevision: viewed.revision });
    expect(stale.status).toBe(409);
    expect(await stale.json()).toEqual({ error: { code: "version_current_pointer_conflict" } });
    expect(env.VERSION_BUCKET.__readStored()).toEqual(before);
    const refreshed = await read(env);
    expect(refreshed.revision).not.toBe(viewed.revision);
    expect((await save(env, { ...draft, expectedRevision: refreshed.revision })).status).toBe(200);
  });
  it("requires an explicit browser precondition", async () => {
    const env = createEnv(base);
    const response = await save(env, draft);
    expect(response.status).toBe(428);
    expect(await response.json()).toEqual({ error: { code: "version_precondition_required" } });
    expect(env.VERSION_BUCKET.put).not.toHaveBeenCalled();
  });
  it.each(["", "*", "unquoted", 1, {}, []].map(value => [value]))("rejects invalid revision %j without storage write", async expectedRevision => {
    const env = createEnv(base);
    const response = await save(env, { ...draft, expectedRevision });
    expect(response.status).toBe(400);
    expect(await response.json()).toEqual({ error: { code: "invalid_version_revision" } });
    expect(env.VERSION_BUCKET.put).not.toHaveBeenCalled();
  });
  it("returns null revision for absence and allows only the first creation", async () => {
    const env = createEnv(null);
    expect((await read(env)).revision).toBeNull();
    const responses = await Promise.all([save(env, { ...draft, expectedRevision: null }), save(env, { ...draft, expectedRevision: null })]);
    expect(responses.map(r => r.status).sort()).toEqual([200, 409]);
  });
  it("returns no usable revision when storage read fails", async () => {
    const env = createEnv(base);
    env.VERSION_BUCKET.get = async () => { throw new Error("offline"); };
    const response = await app.request("/v1/admin/versions", { headers }, env);
    expect(response.status).toBe(502);
    expect(await response.json()).toEqual({ error: { code: "version_storage_read_failed" } });
  });
  it("keeps store errors as 502 and never returns accepted", async () => {
    const env = createEnv({ version: "1.0.0", versionCode: "100", minimumVersionCode: "90", contentRelease });
    const before = env.VERSION_BUCKET.__readStored();
    env.VERSION_BUCKET.put = async () => { throw new Error("store unavailable"); };
    const response = await app.request("/v1/admin/versions/export", {
      method: "POST", headers: { "Cf-Access-Subject": "access|owner-1", "Content-Type": "application/json" },
      body: JSON.stringify({ version: "1.0.1", versionCode: "101", minimumVersionCode: "90", expectedRevision: '"1"' }),
    }, env);
    expect(response.status).toBe(502);
    expect(await response.json()).toEqual({ error: { code: "version_storage_write_failed" } });
    expect(env.VERSION_BUCKET.__readStored()).toEqual(before);
  });
  it("rejects a legacy export racing a content change and preserves the new pointer", async () => {
    const env = createEnv({ version: "1.0.0", versionCode: "100", minimumVersionCode: "90", contentRelease });
    const originalGet = env.VERSION_BUCKET.get;
    const next = { version: "1.0.0", versionCode: "100", minimumVersionCode: "90", contentRelease: { ...contentRelease, releaseId: "new-content" } };
    env.VERSION_BUCKET.get = async (key: string) => {
      const snapshot = await originalGet(key);
      await env.VERSION_BUCKET.put(key, JSON.stringify(next));
      return snapshot;
    };
    const response = await app.request("/v1/admin/versions/export", {
      method: "POST", headers: { "Cf-Access-Subject": "access|owner-1", "Content-Type": "application/json" },
      body: JSON.stringify({ version: "1.0.1", versionCode: "101", minimumVersionCode: "90", expectedRevision: '"1"' }),
    }, env);
    expect(response.status).toBe(409);
    expect(await response.json()).toEqual({ error: { code: "version_current_pointer_conflict" } });
    expect(env.VERSION_BUCKET.__readStored()).toEqual(next);
  });
  it("returns sanitized content release status with the current version policy", async () => {
    const env = createEnv({
      version: "1.0.0",
      versionCode: "100",
      minimumVersionCode: "90",
      contentRelease,
    });

    const response = await app.request(
      "/v1/admin/versions",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      version: "1.0.0",
      versionCode: "100",
      minimumVersionCode: "90",
      contentRelease,
      admissionPolicy: {
        status: "CONFIGURED",
        gameApiEnforcement: "CLIENT_CONFIG_AND_PROTECTED_ROUTES",
      },
    });
  });

  it("preserves an existing approved content release when legacy minimum-version export omits it", async () => {
    const env = createEnv({
      version: "1.0.0",
      versionCode: "100",
      minimumVersionCode: "90",
      contentRelease,
    });

    const response = await app.request(
      "/v1/admin/versions/export",
      {
        method: "POST",
        headers: { "Cf-Access-Subject": "access|owner-1", "Content-Type": "application/json" },
        body: JSON.stringify({ version: "1.0.1", versionCode: "101", minimumVersionCode: "90", expectedRevision: '"1"' }),
      },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      version: "1.0.1",
      versionCode: "101",
      minimumVersionCode: "90",
      contentRelease,
    });
    expect(env.VERSION_BUCKET.__readStored()).toMatchObject({
      version: "1.0.1",
      versionCode: "101",
      minimumVersionCode: "90",
      contentRelease,
    });
  });
});
