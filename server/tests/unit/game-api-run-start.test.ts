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

function createEnv(previewRow: Record<string, unknown> | null) {
  const batchSql: string[] = [];
  const batchBinds: unknown[][] = [];
  const env: TestEnv & { __batchSql: string[]; __batchBinds: unknown[][] } = {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    DB: {
      prepare(sql: string) {
        return {
          bind(...values: unknown[]) {
            return {
              sql,
              values,
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
        return statements.map(() => ({ success: true }));
      },
    } as unknown as D1Database,
    __batchSql: batchSql,
    __batchBinds: batchBinds,
  };
  return env;
}

describe("game-api run start contract", () => {
  it("starts an ordinary 15-wave run from server-authored preview metadata", async () => {
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
        allowedOperatorSlots: 4,
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
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ operationPreviewId: "preview_ordinary_1" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      accepted: true,
      startStatus: "STARTED",
      operationPreviewId: "preview_ordinary_1",
      operationId: "opn_03_water_stop",
      encounterType: "ORDINARY_15_WAVE",
      riskLevel: "III",
      risk: "III",
      balanceVersion: "501",
      contentVersion: "act1.launch.content.v1",
      ordinary: {
        totalWaves: 15,
        startWaveNumber: 1,
      },
      raid: null,
      raidId: null,
      bossId: null,
      bossPhaseRefs: [],
      allowedOperatorSlots: 4,
      startAugmentPackageRef: "act1_full_start_package_v1",
      startTacticPackageRef: "act1_full_start_package_v1",
      errorCode: null,
      message: null,
    });
    expect(body.runId).toMatch(/^run_/u);
    expect(JSON.stringify(body)).not.toContain("should_not_leak");
    expect(JSON.stringify(body)).not.toContain("rawRng");
    expect(JSON.stringify(body)).not.toContain("probability");
    expect(JSON.stringify(body)).not.toContain("animationTimeline");
    expect(env.__batchSql.join("\n")).toContain("INSERT INTO runs");
    expect(env.__batchSql.join("\n")).toContain("UPDATE operation_previews");
  });

  it("starts a raid boss run with raid-only metadata refs", async () => {
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
        allowedOperatorSlots: 4,
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
        headers: { Authorization: await createAuthorizationHeader() },
        body: JSON.stringify({ operationPreviewId: "preview_raid_1" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      startStatus: "STARTED",
      operationPreviewId: "preview_raid_1",
      operationId: "raid_raider_king_engine",
      encounterType: "RAID_BOSS",
      riskLevel: "V",
      balanceVersion: "501",
      ordinary: null,
      raid: {
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
      },
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossPhaseRefs: ["crown_chase", "trophy_break", "engine_stop"],
      startAugmentPackageRef: "act1_full_start_package_v1",
      startTacticPackageRef: "act1_full_start_package_v1",
      errorCode: null,
      message: null,
    });
  });

  it("rejects client-supplied source-of-truth fields", async () => {
    const response = await app.request(
      "/v1/runs/start?accountId=account_other",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
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
