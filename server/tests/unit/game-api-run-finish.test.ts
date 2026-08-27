import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { launchRunRewardsConfig } from "../../packages/balance/src/index";

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
};

const authSecret = "test-secret-minimum-32-characters";

async function createAuthorizationHeader(): Promise<string> {
  const token = await createAccessToken({
    secret: authSecret,
    accountId: "account_1",
    sessionId: "session_1",
    nowMs: Date.now(),
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

describe("game-api run finish contract", () => {
  it("accepts a completed run finish and creates a reward preview from frozen run state", async () => {
    const batchSql: string[] = [];
    const batchBinds: unknown[][] = [];
    const env: TestEnv = {
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
                    expect(values).toEqual(["session_1", "account_1"]);
                    return {
                      account_id: "account_1",
                      session_id: "session_1",
                      session_status: "ACTIVE",
                      session_expires_at: Date.now() + 900000,
                      account_status: "ACTIVE",
                    };
                  }
                  if (sql.includes("FROM idempotency_records")) {
                    expect(values).toEqual(["account_1", "run-finish-idem-1"]);
                    return null;
                  }
                  if (sql.includes("FROM account_first_clears")) {
                    expect(values).toEqual(["account_1", "opn_03_water_stop", "III"]);
                    return null;
                  }
                  if (sql.includes("FROM runs")) {
                    expect(values).toEqual(["run_1"]);
                    return {
                      id: "run_1",
                      account_id: "account_1",
                      status: "ACTIVE",
                      balance_config_version: 1,
                      objective: "opn_03_water_stop",
                      risk: "III",
                      preview_json: JSON.stringify({
                        operationId: "opn_03_water_stop",
                        actId: "act_01_wasteland_railway",
                        regionId: "region_wasteland_railway",
                      }),
                      config_json: JSON.stringify({
                        runRewards: launchRunRewardsConfig,
                      }),
                    };
                  }
                  return null;
                },
              };
            },
          };
        },
        async batch(statements: Array<{ sql?: string }>) {
          batchSql.push(...statements.map((statement) => String(statement.sql)));
          batchBinds.push(...statements.map((statement) => (statement as { values?: unknown[] }).values ?? []));
          return statements.map(() => ({ success: true }));
        },
      } as unknown as D1Database,
    };

    const response = await app.request(
      "/v1/runs/run_1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-idem-1",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({
          runId: "run_1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      runId: "run_1",
      accepted: true,
      resultStatus: "ACCEPTED",
      rewardClaimId: null,
      balanceVersion: "1",
      message: null,
      errorCode: null,
    });
    expect(body.rewardPreviewId).toMatch(/^reward_preview_/u);
    expect(body.serverReceivedAt).toMatch(/^\d{4}-\d{2}-\d{2}T/u);
    expect(batchSql.join("\n")).toContain("INSERT INTO run_final_results");
    expect(batchSql.join("\n")).toContain("INSERT INTO run_reward_previews");
    expect(batchSql.join("\n")).toContain("INSERT INTO account_first_clears");
    expect(batchSql.join("\n")).toContain("INSERT INTO idempotency_records");
    expect(batchSql.join("\n")).not.toContain("reward amount");
    const rewardPreviewInsertIndex = batchSql.findIndex((sql) => sql.includes("INSERT INTO run_reward_previews"));
    expect(rewardPreviewInsertIndex).toBeGreaterThanOrEqual(0);
    const rewardPlan = JSON.parse(String(batchBinds[rewardPreviewInsertIndex]?.[4])) as {
      rewardLines: Array<{ source?: string }>;
      calculation: { firstClearEligible: boolean; firstClearApplied: boolean };
    };
    expect(rewardPlan.rewardLines.some((line) => line.source === "FIRST_CLEAR")).toBe(true);
    expect(rewardPlan.calculation.firstClearEligible).toBe(true);
    expect(rewardPlan.calculation.firstClearApplied).toBe(true);
  });

  it("replays accepted run finish with the stored reward preview id", async () => {
    const env: TestEnv = {
      ENVIRONMENT: "DEV",
      ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
      AUTH_SIGNING_KEY_CURRENT: authSecret,
      DB: {
        prepare(sql: string) {
          return {
            bind(...values: unknown[]) {
              return {
                first: async () => {
                  if (sql.includes("FROM auth_sessions")) {
                    expect(values).toEqual(["session_1", "account_1"]);
                    return {
                      account_id: "account_1",
                      session_id: "session_1",
                      session_status: "ACTIVE",
                      session_expires_at: Date.now() + 900000,
                      account_status: "ACTIVE",
                    };
                  }
                  if (sql.includes("FROM idempotency_records")) {
                    expect(values).toEqual(["account_1", "run-finish-idem-1"]);
                    return {
                      result_ref: "reward_preview_1",
                      created_at: Date.parse("2026-08-27T00:00:00.000Z"),
                      reward_created_at: null,
                      balance_config_version: 1,
                      preview_run_id: "run_1",
                    };
                  }
                  throw new Error(`duplicate replay must not query finishable run: ${sql}`);
                },
              };
            },
          };
        },
        async batch() {
          throw new Error("duplicate replay must not write a second finish batch");
        },
      } as unknown as D1Database,
    };

    const response = await app.request(
      "/v1/runs/run_1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-idem-1",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({
          runId: "run_1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      runId: "run_1",
      accepted: false,
      resultStatus: "DUPLICATE_REPLAY",
      rewardClaimId: null,
      rewardPreviewId: "reward_preview_1",
      serverReceivedAt: "2026-08-27T00:00:00.000Z",
      balanceVersion: "1",
      message: "Run finish was already accepted.",
      errorCode: "duplicate_replay",
    });
  });

  it("rejects idempotency replay when the stored reward preview belongs to another run", async () => {
    const env: TestEnv = {
      ENVIRONMENT: "DEV",
      ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
      AUTH_SIGNING_KEY_CURRENT: authSecret,
      DB: {
        prepare(sql: string) {
          return {
            bind() {
              return {
                first: async () => {
                  if (sql.includes("FROM auth_sessions")) {
                    return {
                      account_id: "account_1",
                      session_id: "session_1",
                      session_status: "ACTIVE",
                      session_expires_at: Date.now() + 900000,
                      account_status: "ACTIVE",
                    };
                  }
                  if (sql.includes("FROM idempotency_records")) {
                    return {
                      result_ref: "reward_preview_other",
                      created_at: Date.parse("2026-08-27T00:00:00.000Z"),
                      reward_created_at: null,
                      balance_config_version: 1,
                      preview_run_id: "run_other",
                    };
                  }
                  return null;
                },
              };
            },
          };
        },
        async batch() {
          throw new Error("idempotency run mismatch must not write");
        },
      } as unknown as D1Database,
    };

    const response = await app.request(
      "/v1/runs/run_1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-idem-reused-for-another-run",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({
          runId: "run_1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      env,
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      runId: "run_1",
      accepted: false,
      resultStatus: "REJECTED",
      rewardPreviewId: null,
      errorCode: "contract_violation",
    });
  });

  it("does not apply first-clear rewards when the account already cleared the operation risk", async () => {
    const batchSql: string[] = [];
    const batchBinds: unknown[][] = [];
    const env: TestEnv = {
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
                      account_id: "account_1",
                      session_id: "session_1",
                      session_status: "ACTIVE",
                      session_expires_at: Date.now() + 900000,
                      account_status: "ACTIVE",
                    };
                  }
                  if (sql.includes("FROM idempotency_records")) {
                    return null;
                  }
                  if (sql.includes("FROM account_first_clears")) {
                    expect(values).toEqual(["account_1", "opn_03_water_stop", "III"]);
                    return { cleared_at: Date.parse("2026-08-26T00:00:00.000Z") };
                  }
                  if (sql.includes("FROM runs")) {
                    return {
                      id: "run_1",
                      account_id: "account_1",
                      status: "ACTIVE",
                      balance_config_version: 1,
                      objective: "opn_03_water_stop",
                      risk: "III",
                      preview_json: JSON.stringify({
                        operationId: "opn_03_water_stop",
                        actId: "act_01_wasteland_railway",
                        regionId: "region_wasteland_railway",
                      }),
                      config_json: JSON.stringify({
                        runRewards: launchRunRewardsConfig,
                      }),
                    };
                  }
                  return null;
                },
              };
            },
          };
        },
        async batch(statements: Array<{ sql?: string }>) {
          batchSql.push(...statements.map((statement) => String(statement.sql)));
          batchBinds.push(...statements.map((statement) => (statement as { values?: unknown[] }).values ?? []));
          return statements.map(() => ({ success: true }));
        },
      } as unknown as D1Database,
    };

    const response = await app.request(
      "/v1/runs/run_1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-idem-existing-first-clear",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({
          runId: "run_1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
        }),
      },
      env,
    );

    expect(response.status).toBe(200);
    expect(batchSql.join("\n")).not.toContain("INSERT INTO account_first_clears");
    const rewardPreviewInsertIndex = batchSql.findIndex((sql) => sql.includes("INSERT INTO run_reward_previews"));
    const rewardPlan = JSON.parse(String(batchBinds[rewardPreviewInsertIndex]?.[4])) as {
      rewardLines: Array<{ source?: string }>;
      calculation: { firstClearEligible: boolean; firstClearApplied: boolean };
    };
    expect(rewardPlan.rewardLines.some((line) => line.source === "FIRST_CLEAR")).toBe(false);
    expect(rewardPlan.calculation.firstClearEligible).toBe(false);
    expect(rewardPlan.calculation.firstClearApplied).toBe(false);
  });

  it("rejects client-supplied reward fields before using them as source of truth", async () => {
    const env: TestEnv = {
      ENVIRONMENT: "DEV",
      ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
      AUTH_SIGNING_KEY_CURRENT: authSecret,
      DB: {
        prepare(sql: string) {
          throw new Error(`reward field contract violation must not touch DB: ${sql}`);
        },
      } as unknown as D1Database,
    };

    const response = await app.request(
      "/v1/runs/run_1/finish",
      {
        method: "POST",
        headers: {
          "Idempotency-Key": "run-finish-idem-client-reward-field",
          Authorization: await createAuthorizationHeader(),
        },
        body: JSON.stringify({
          runId: "run_1",
          currentStage: "Complete",
          currentWaveNumber: 0,
          completedWaveCount: 15,
          outcome: "VICTORY",
          runtimeStateJson: "{}",
          tacticSelections: [],
          clientCompletedAt: "2026-08-27T00:00:00.000Z",
          rewardAmount: 999999,
        }),
      },
      env,
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      runId: "run_1",
      accepted: false,
      resultStatus: "REJECTED",
      rewardPreviewId: null,
      errorCode: "contract_violation",
    });
  });
});
