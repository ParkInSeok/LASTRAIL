import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type QueryResult = Record<string, unknown> | Record<string, unknown>[] | null;

function createEnv(results: QueryResult[]): Env {
  const queue = [...results];

  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: {
      prepare(sql: string) {
        return {
          bind() {
            return {
              first: async () => {
                if (sql.includes("admin_principals")) {
                  return {
                    id: "principal-1",
                    access_subject: "access|owner-1",
                    role: "OWNER",
                    status: "ACTIVE",
                    display_name: "Owner",
                  };
                }

                const result = queue.shift();
                return Array.isArray(result) ? (result[0] ?? null) : result;
              },
              all: async () => {
                const result = queue.shift();
                return { results: Array.isArray(result) ? result : result === null ? [] : [result] };
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

describe("admin-api read-only operations", () => {
  it("lists players with computed sync level", async () => {
    const response = await app.request(
      "/v1/admin/players?q=Kyle",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv([
        [
          {
            account_id: "account-1",
            display_name: "Kyle Account",
            status: "ACTIVE",
            updated_at: 1787659200000,
            sync_level: 30,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      items: [
        {
          accountId: "account-1",
          displayName: "Kyle Account",
          status: "ACTIVE",
          syncLevel: 30,
          lastSeenAt: "2026-08-25T12:00:00.000Z",
        },
      ],
    });
  });

  it("returns player progression using Training Core and Sync", async () => {
    const response = await app.request(
      "/v1/admin/players/account-1",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv([
        {
          account_id: "account-1",
          display_name: "Kyle Account",
          status: "ACTIVE",
          created_at: 1787659200000,
          operator_exp: 1840,
        },
        [
          { slot_index: 0, operator_id: "kyle", trained_level: 32 },
          { slot_index: 1, operator_id: "reina", trained_level: 31 },
          { slot_index: 2, operator_id: "eve", trained_level: 30 },
          { slot_index: 3, operator_id: "sera", trained_level: 30 },
          { slot_index: 4, operator_id: "mira", trained_level: 33 },
        ],
        [
          { operator_id: "kyle", skill_training: 3, promotion: 1 },
          { operator_id: "vex", skill_training: 1, promotion: 0 },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body.progression.syncLevel).toBe(30);
    expect(body.progression.coreLevelSpread).toBe(3);
    expect(body.progression.trainingCore).toHaveLength(5);
    expect(body.progression.operators).toEqual([
      {
        operatorId: "kyle",
        name: "kyle",
        effectiveLevel: 32,
        levelSource: "TRAINED",
        trainedLevel: 32,
        skillTraining: 3,
        promotion: "1",
        coreSlot: 1,
      },
      {
        operatorId: "vex",
        name: "vex",
        effectiveLevel: 30,
        levelSource: "SYNC",
        trainedLevel: 1,
        skillTraining: 1,
        promotion: "0",
      },
    ]);
  });

  it("lists runs with historical balance context", async () => {
    const response = await app.request(
      "/v1/admin/runs?accountId=account-1",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv([
        [
          {
            run_id: "run-1",
            account_id: "account-1",
            status: "COMPLETED",
            started_at: 1787659200000,
            finished_at: 1787660100000,
            risk: "III",
            balance_config_version: 7,
            validator_version: "validator-1",
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      items: [
        {
          runId: "run-1",
          accountId: "account-1",
          result: "CLEARED",
          riskTier: "III",
          balanceVersion: "7",
          validatorVersion: "validator-1",
        },
      ],
    });
  });

  it("returns run detail with frozen operator snapshot", async () => {
    const snapshot = {
      operatorIds: ["kyle", "reina", "eve", "sera", "mira"],
      ultimateOperatorId: "kyle",
      operators: [
        {
          operatorId: "kyle",
          effectiveLevel: 32,
          levelSource: "TRAINED",
          syncLevelAtStart: 30,
          trainedLevelAtStart: 32,
          skillTraining: 3,
          promotion: "1",
        },
        {
          operatorId: "reina",
          effectiveLevel: 31,
          levelSource: "TRAINED",
          syncLevelAtStart: 30,
          trainedLevelAtStart: 31,
          skillTraining: 2,
          promotion: "0",
        },
        {
          operatorId: "eve",
          effectiveLevel: 30,
          levelSource: "TRAINED",
          syncLevelAtStart: 30,
          trainedLevelAtStart: 30,
          skillTraining: 2,
          promotion: "0",
        },
        {
          operatorId: "sera",
          effectiveLevel: 30,
          levelSource: "TRAINED",
          syncLevelAtStart: 30,
          trainedLevelAtStart: 30,
          skillTraining: 2,
          promotion: "0",
        },
        {
          operatorId: "mira",
          effectiveLevel: 33,
          levelSource: "TRAINED",
          syncLevelAtStart: 30,
          trainedLevelAtStart: 33,
          skillTraining: 2,
          promotion: "0",
        },
      ],
    };

    const response = await app.request(
      "/v1/admin/runs/run-1",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv([
        {
          run_id: "run-1",
          account_id: "account-1",
          status: "COMPLETED",
          started_at: 1787659200000,
          finished_at: 1787660100000,
          risk: "III",
          balance_config_version: 7,
          current_active_balance_version: 8,
          build_id: "build-9",
          content_version: 4,
          validator_version: "validator-1",
          validator_runtime_revision: 12,
          snapshot_json: JSON.stringify(snapshot),
        },
      ]),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body.balanceVersion).toBe("7");
    expect(body.currentActiveBalanceVersion).toBe("8");
    expect(body.snapshot.operatorIds).toEqual(snapshot.operatorIds);
  });
});
