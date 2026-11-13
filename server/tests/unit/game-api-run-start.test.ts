import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
};

const authSecret = "test-secret-minimum-32-characters";

async function createAuthorizationHeader(accountId = "account_1"): Promise<string> {
  const token = await createAccessToken({
    secret: authSecret,
    accountId,
    sessionId: "session_1",
    nowMs: Date.now(),
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

function createEnv(previewRow: Record<string, unknown> | null, options: {
  consumeChanges?: number;
  idempotencyRow?: Record<string, unknown> | null;
  failStartBatch?: boolean;
  rollbackChanges?: number;
  versionDocument?: unknown;
  clientAdmissionMode?: "DISABLED" | "ENFORCED";
  runtimeDeploymentId?: string;
} = {}) {
  const batchSql: string[] = [];
  const batchBinds: unknown[][] = [];
  const runSql: string[] = [];
  const runBinds: unknown[][] = [];
  const env: TestEnv & {
    __batchSql: string[];
    __batchBinds: unknown[][];
    __runSql: string[];
    __runBinds: unknown[][];
  } = {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    CLIENT_ADMISSION_MODE: options.clientAdmissionMode,
    VERSION_BUCKET: options.versionDocument === undefined
      ? undefined
      : {
          get: async (key: string) => {
            expect(key).toBe("versions/current.json");
            return { json: async () => options.versionDocument };
          },
          put: async () => {
            throw new Error("run start must not write version policy.");
          },
        },
    CF_VERSION_METADATA: options.runtimeDeploymentId === undefined
      ? undefined
      : { id: options.runtimeDeploymentId },
    DB: {
      prepare(sql: string) {
        return {
          first: async () => sql.includes("balance_config_active") ? {version:501,config_json:"{}"} : null,
          bind(...values: unknown[]) {
            return {
              sql,
              values,
              run: async () => {
                runSql.push(sql);
                runBinds.push(values);
                if (sql.includes("UPDATE operation_previews") && sql.includes("SET status = 'ACTIVE'")) {
                  return { success: true, meta: { changes: options.rollbackChanges ?? 1 } };
                }
                return { success: true, meta: { changes: options.consumeChanges ?? 1 } };
              },
              all: async () => ({results:[]}),
              first: async () => {
                if (sql.includes("FROM auth_sessions")) {
                  return {
                    account_id: values[1],
                    session_id: "session_1",
                    session_status: "ACTIVE",
                    session_expires_at: Date.now() + 900000,
                    account_status: "ACTIVE",
                  };
                }
                if (sql.includes("FROM idempotency_records") && sql.includes("run_start")) {
                  return options.idempotencyRow ?? null;
                }
                if (sql.includes("FROM operation_previews")) {
                  return previewRow;
                }
                return null;
              },
            };
          },
        };
      },
      async batch(statements: Array<{ sql?: string; values?: unknown[] }>) {
        batchSql.push(...statements.map((statement) => String(statement.sql)));
        batchBinds.push(...statements.map((statement) => statement.values ?? []));
        if (options.consumeChanges === 0 && statements.some(statement => String(statement.sql).includes("INSERT INTO departure_write_guard"))) throw new Error("CHECK constraint failed: valid=1");
        if (options.failStartBatch) {
          throw new Error("simulated run start persistence failure");
        }
        return statements.map(() => ({ success: true }));
      },
    } as unknown as D1Database,
    __batchSql: batchSql,
    __batchBinds: batchBinds,
    __runSql: runSql,
    __runBinds: runBinds,
  };
  return env;
}

describe("game-api run start contract", () => {
  it("rejects an unversioned ordinary preview without starting a legacy run", async () => {
    const env = createEnv({
      id: "preview_ordinary_1",
      account_id: "account_1",
      mutation_id: "mutation_preview_1",
      objective: "opn_03_water_stop",
      risk: "III",
      balance_config_version: 501,
      preview_json: JSON.stringify({
        operationId: "opn_03_water_stop",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
        encounterType: "ORDINARY_15_WAVE",
        raidId: "should_not_leak",
        bossId: "should_not_leak",
        bossPhaseRefs: ["should_not_leak"],
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
        contentVersion: "act1.launch.content.v1",
      }),
      status: "ACTIVE",
      expires_at: Date.now() + 900000,
      created_at: Date.now() - 1000,
    });

    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-1",
        },
        body: JSON.stringify({ operationPreviewId: "preview_ordinary_1" }),
      },
      env,
    );

    expect(response.status).toBe(409);
    expect(await response.json()).toMatchObject({ accepted: false, errorCode: "departure_quote_changed" });
    expect(env.__batchSql).toEqual([]);
    expect(env.__runSql).toEqual([]);
  });

  it("does not admit an unversioned quote even with valid client deployment admission", async () => {
    const env = createEnv({
      id: "preview_versioned_1",
      account_id: "account_1",
      mutation_id: "mutation_preview_1",
      objective: "opn_03_water_stop",
      risk: "III",
      balance_config_version: 501,
      preview_json: JSON.stringify({
        operationId: "opn_03_water_stop",
        encounterType: "ORDINARY_15_WAVE",
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
        contentVersion: "act1.launch.content.v1",
      }),
      status: "ACTIVE",
      expires_at: Date.now() + 900000,
      created_at: Date.now() - 1000,
    }, {
      clientAdmissionMode: "ENFORCED",
      runtimeDeploymentId: "cf-runtime-deploy-123",
      versionDocument: {
        version: "1.0.0",
        versionCode: "100",
        minimumVersionCode: "90",
        contentRelease: {
          status: "APPROVED",
          releaseId: "content_act1_001",
          contentVersion: "act1.2026-09-06.001",
          catalogUrl: "https://assets.lastrail.app/addressables/catalog.json",
          catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
          minimumVersionCode: "90",
          maximumVersionCode: "100",
        },
      },
    });

    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-versioned",
          "X-LastRail-App-Version-Code": "100",
          "X-LastRail-App-Platform": "android",
          "X-LastRail-App-Channel": "internal",
        },
        body: JSON.stringify({ operationPreviewId: "preview_versioned_1" }),
      },
      env,
    );

    expect(response.status).toBe(409);
    expect(await response.json()).toMatchObject({ accepted: false, errorCode: "departure_quote_changed" });
    expect(env.__batchSql).toEqual([]);
    expect(env.__runSql).toEqual([]);
  });

  it("requires Idempotency-Key header before starting a run", async () => {
    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ operationPreviewId: "preview_ordinary_1" }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      startStatus: "REJECTED",
      operationPreviewId: "preview_ordinary_1",
      errorCode: "contract_violation",
    });
  });

  it("replays a prior successful run start without consuming the preview again", async () => {
    const env = createEnv(null, {
      idempotencyRow: {
        result_ref: "run_previous_1",
        created_at: 1798444800000,
        preview_id: "preview_ordinary_1",
        snapshot_json: JSON.stringify({
          runId: "run_previous_1",
          operationPreviewId: "preview_ordinary_1",
          operationId: "opn_03_water_stop",
          actId: "act_01_wasteland_railway",
          regionId: "region_wasteland_railway",
          encounterType: "ORDINARY_15_WAVE",
          riskLevel: "III",
          risk: "III",
          balanceVersion: "501",
          contentVersion: "act1.launch.content.v1",
          ordinary: { totalWaves: 15, startWaveNumber: 1 },
          raid: null,
          raidId: null,
          bossId: null,
          bossPhaseRefs: [],
          allowedOperatorSlots: 5,
          startAugmentPackageRef: "act1_full_start_package_v1",
          startTacticPackageRef: "act1_full_start_package_v1",
          serverStartedAt: "2026-12-28T00:00:00.000Z",
          runVersionSnapshot: {
            source: "server_accepted_run_start",
            clientInstalled: { versionCode: "100", platform: "android", channel: "internal" },
            clientAdmission: { status: "ACCEPTED", minimumVersionCode: "90", updateRequired: false },
            serverAcceptedDeployment: { environment: "DEV", runtimeDeploymentId: "cf-runtime-deploy-123" },
            contentRelease: { status: "APPROVED_COMPATIBLE", releaseId: "content_act1_001", contentVersion: "act1.2026-09-06.001", catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
            capturedAt: "2026-12-28T00:00:00.000Z",
          },
        }),
      },
    });

    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-1",
        },
        body: JSON.stringify({ operationPreviewId: "preview_ordinary_1" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      startStatus: "STARTED",
      runId: "run_previous_1",
      operationPreviewId: "preview_ordinary_1",
      encounterType: "ORDINARY_15_WAVE",
      allowedOperatorSlots: 5,
      runVersionSnapshot: {
        source: "server_accepted_run_start",
        clientInstalled: { versionCode: "100", platform: "android", channel: "internal" },
        clientAdmission: { status: "ACCEPTED", minimumVersionCode: "90", updateRequired: false },
      },
      errorCode: null,
    });
    expect(env.__runSql.join("\n")).not.toContain("UPDATE operation_previews");
    expect(env.__batchSql.join("\n")).not.toContain("INSERT INTO runs");
  });

  it("rejects Idempotency-Key reuse for a different operation preview", async () => {
    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-1",
        },
        body: JSON.stringify({ operationPreviewId: "preview_other_1" }),
      },
      createEnv(null, {
        idempotencyRow: {
          result_ref: "run_previous_1",
          created_at: 1798444800000,
          preview_id: "preview_ordinary_1",
          snapshot_json: "{}",
        },
      }),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      startStatus: "REJECTED",
      operationPreviewId: "preview_other_1",
      errorCode: "contract_violation",
    });
  });

  it("rejects an obsolete quote before attempting any consume race", async () => {
    const env = createEnv({
      id: "preview_ordinary_1",
      account_id: "account_1",
      mutation_id: "mutation_preview_1",
      objective: "opn_03_water_stop",
      risk: "III",
      balance_config_version: 501,
      preview_json: JSON.stringify({
        operationId: "opn_03_water_stop",
        encounterType: "ORDINARY_15_WAVE",
      }),
      status: "ACTIVE",
      expires_at: Date.now() + 900000,
      created_at: Date.now() - 1000,
    }, { consumeChanges: 0 });

    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-1",
        },
        body: JSON.stringify({ operationPreviewId: "preview_ordinary_1" }),
      },
      env,
    );

    expect(response.status).toBe(409);
    expect(await response.json()).toMatchObject({ accepted: false, errorCode: "departure_quote_changed" });
    expect(env.__batchSql).toEqual([]);
    expect(env.__runSql).toEqual([]);
  });

  it("does not attempt legacy persistence or emit a persistence error for an obsolete quote", async () => {
    const env = createEnv({
      id: "preview_ordinary_1",
      account_id: "account_1",
      mutation_id: "mutation_preview_1",
      objective: "opn_03_water_stop",
      risk: "III",
      balance_config_version: 501,
      preview_json: JSON.stringify({
        operationId: "opn_03_water_stop",
        encounterType: "ORDINARY_15_WAVE",
        allowedOperatorSlots: 5,
      }),
      status: "ACTIVE",
      expires_at: Date.now() + 900000,
      created_at: Date.now() - 1000,
    }, { failStartBatch: true });

    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-failed-batch",
        },
        body: JSON.stringify({ operationPreviewId: "preview_ordinary_1" }),
      },
      env,
    );

    expect(response.status).toBe(409);
    expect(await response.json()).toMatchObject({ accepted: false, errorCode: "departure_quote_changed" });
    expect(env.__batchSql).toEqual([]);
    expect(env.__runSql).toEqual([]);
  });

  it("rejects an unversioned raid preview without starting a legacy run", async () => {
    const env = createEnv({
      id: "preview_raid_1",
      account_id: "account_1",
      mutation_id: "mutation_preview_raid_1",
      objective: "raid_raider_king_engine",
      risk: "V",
      balance_config_version: 501,
      preview_json: JSON.stringify({
        operationId: "raid_raider_king_engine",
        actId: "act_01_wasteland_railway",
        regionId: "region_wasteland_railway",
        encounterType: "RAID_BOSS",
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "act1_full_start_package_v1",
        startTacticPackageRef: "act1_full_start_package_v1",
        contentVersion: "act1.launch.content.v1",
      }),
      status: "ACTIVE",
      expires_at: Date.now() + 900000,
      created_at: Date.now() - 1000,
    });

    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-1",
        },
        body: JSON.stringify({ operationPreviewId: "preview_raid_1" }),
      },
      env,
    );

    expect(response.status).toBe(409);
    expect(await response.json()).toMatchObject({ accepted: false, errorCode: "departure_quote_changed" });
    expect(env.__batchSql).toEqual([]);
    expect(env.__runSql).toEqual([]);
  });

  it("rejects client-supplied source-of-truth fields", async () => {
    const response = await app.request(
      "/v1/runs/start?accountId=account_other",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "run-start-idem-1",
        },
        body: JSON.stringify({
          operationPreviewId: "preview_ordinary_1",
          accountId: "account_other",
          riskLevel: "V",
          rewardAmount: 999999,
          storyProgress: "raid_raider_king_engine",
        }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      startStatus: "REJECTED",
      errorCode: "contract_violation",
    });
  });

  it("requires player bearer auth", async () => {
    const response = await app.request(
      "/v1/runs/start",
      {
        method: "POST",
        headers: { "Idempotency-Key": "run-start-idem-1" },
        body: JSON.stringify({ operationPreviewId: "preview_ordinary_1" }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      startStatus: "REJECTED",
      errorCode: "auth_required",
    });
  });
});
