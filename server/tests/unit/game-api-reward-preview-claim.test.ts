import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";

type RewardPreviewRow = {
  id: string;
  account_id: string;
  run_id: string;
  status: "ACTIVE" | "CLAIMED" | "EXPIRED" | "INVALIDATED";
  balance_config_version: number;
  reward_plan_json: string;
  expires_at: number;
  created_at: number;
  claimed_at: number | null;
  reward_claim_id: string | null;
};

function createEnv(row: RewardPreviewRow | null): Env {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    DB: {
      prepare(sql: string) {
        expect(sql).toContain("run_reward_previews");
        return {
          bind(runId: string, rewardPreviewId: string) {
            expect(runId).toBe("run_1");
            expect(rewardPreviewId).toBe("reward_preview_1");
            return {
              first: async () => row,
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

describe("game-api reward preview and claim contract", () => {
  it("returns a server-calculated reward preview for Unity reward review", async () => {
    const response = await app.request(
      "/v1/runs/run_1/reward-preview/reward_preview_1",
      {},
      createEnv({
        id: "reward_preview_1",
        account_id: "account_1",
        run_id: "run_1",
        status: "ACTIVE",
        balance_config_version: 1,
        reward_plan_json: JSON.stringify({
          rewardLines: [
            {
              kind: "CURRENCY",
              id: "cur_credits",
              quantity: 1200,
              source: "RUN_CLEAR",
              adBonusEligible: true,
            },
          ],
        }),
        expires_at: Date.parse("2026-08-27T00:00:00.000Z"),
        created_at: Date.parse("2026-08-26T00:00:00.000Z"),
        claimed_at: null,
        reward_claim_id: null,
      }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      runId: "run_1",
      rewardPreviewId: "reward_preview_1",
      status: "ACTIVE",
      claimability: "CLAIMABLE",
      balanceVersion: "1",
      expiresAt: "2026-08-27T00:00:00.000Z",
      serverCalculatedAt: "2026-08-26T00:00:00.000Z",
      rewardLines: [
        {
          kind: "CURRENCY",
          id: "cur_credits",
          quantity: 1200,
          source: "RUN_CLEAR",
          adBonusEligible: true,
        },
      ],
      claim: {
        claimable: true,
        rewardClaimId: null,
        claimedAt: null,
        errorCode: null,
      },
    });
  });

  it("returns 404 when a reward preview is not found", async () => {
    const response = await app.request(
      "/v1/runs/run_1/reward-preview/reward_preview_1",
      {},
      createEnv(null),
    );

    expect(response.status).toBe(404);
    await expect(response.json()).resolves.toEqual({
      errorCode: "reward_preview_not_found",
      message: "Reward preview was not found.",
    });
  });

  it("rejects reward claim requests without Idempotency-Key", async () => {
    const response = await app.request(
      "/v1/runs/run_1/reward-preview/reward_preview_1/claim",
      {
        method: "POST",
        body: JSON.stringify({ runId: "run_1", rewardPreviewId: "reward_preview_1" }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      runId: "run_1",
      rewardPreviewId: "reward_preview_1",
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    });
  });

  it("rejects reward claim requests when path and body identifiers differ", async () => {
    const response = await app.request(
      "/v1/runs/run_1/reward-preview/reward_preview_1/claim",
      {
        method: "POST",
        headers: { "Idempotency-Key": "claim-idem-1" },
        body: JSON.stringify({ runId: "run_other", rewardPreviewId: "reward_preview_1" }),
      },
      createEnv(null),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      runId: "run_1",
      rewardPreviewId: "reward_preview_1",
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Path and body runId/rewardPreviewId must match.",
    });
  });

  it("maps an already claimed preview to duplicate replay without granting again", async () => {
    const response = await app.request(
      "/v1/runs/run_1/reward-preview/reward_preview_1/claim",
      {
        method: "POST",
        headers: { "Idempotency-Key": "claim-idem-1" },
        body: JSON.stringify({ runId: "run_1", rewardPreviewId: "reward_preview_1" }),
      },
      createEnv({
        id: "reward_preview_1",
        account_id: "account_1",
        run_id: "run_1",
        status: "CLAIMED",
        balance_config_version: 1,
        reward_plan_json: JSON.stringify({ rewardLines: [] }),
        expires_at: Date.parse("2026-08-27T00:00:00.000Z"),
        created_at: Date.parse("2026-08-26T00:00:00.000Z"),
        claimed_at: Date.parse("2026-08-26T01:00:00.000Z"),
        reward_claim_id: "reward_claim_1",
      }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      runId: "run_1",
      rewardPreviewId: "reward_preview_1",
      rewardClaimId: "reward_claim_1",
      status: "DUPLICATE_REPLAY",
      serverClaimedAt: "2026-08-26T01:00:00.000Z",
      balanceVersion: "1",
      rewardLines: [],
      errorCode: "duplicate_replay",
      message: "Reward claim was already committed.",
    });
  });

  it("commits an active reward claim and writes economy ledger statements in one batch", async () => {
    const batchSql: string[] = [];
    const env = {
      ENVIRONMENT: "DEV",
      ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
      DB: {
        prepare(sql: string) {
          return {
            bind() {
              return {
                sql,
                first: async () => {
                  if (sql.includes("FROM run_reward_previews")) {
                    return {
                      id: "reward_preview_1",
                      account_id: "account_1",
                      run_id: "run_1",
                      status: "ACTIVE",
                      balance_config_version: 1,
                      reward_plan_json: JSON.stringify({
                        rewardLines: [
                          {
                            kind: "CURRENCY",
                            id: "cur_credits",
                            quantity: 1200,
                            source: "RUN_CLEAR",
                            adBonusEligible: true,
                          },
                        ],
                      }),
                      expires_at: Date.parse("2026-08-27T00:00:00.000Z"),
                      created_at: Date.parse("2026-08-26T00:00:00.000Z"),
                      claimed_at: null,
                      reward_claim_id: null,
                    } satisfies RewardPreviewRow;
                  }
                  if (sql.includes("MAX(sequence)")) {
                    return { sequence: 2 };
                  }
                  if (sql.includes("FROM account_currencies")) {
                    return { balance: 100 };
                  }
                  return null;
                },
              };
            },
          };
        },
        async batch(statements: Array<{ sql?: string }>) {
          batchSql.push(...statements.map((statement) => String(statement.sql)));
          return statements.map(() => ({ success: true }));
        },
      } as unknown as D1Database,
    };

    const response = await app.request(
      "/v1/runs/run_1/reward-preview/reward_preview_1/claim",
      {
        method: "POST",
        headers: { "Idempotency-Key": "claim-idem-1" },
        body: JSON.stringify({ runId: "run_1", rewardPreviewId: "reward_preview_1" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      runId: "run_1",
      rewardPreviewId: "reward_preview_1",
      status: "CLAIMED",
      balanceVersion: "1",
      rewardLines: [
        {
          kind: "CURRENCY",
          id: "cur_credits",
          quantity: 1200,
          source: "RUN_CLEAR",
          balanceAfter: 1300,
        },
      ],
      errorCode: null,
      message: null,
    });
    expect(body.rewardClaimId).toMatch(/^reward_claim_/u);
    expect(body.serverClaimedAt).toMatch(/^\d{4}-\d{2}-\d{2}T/u);
    expect(batchSql.join("\n")).toContain("INSERT INTO account_mutations");
    expect(batchSql.join("\n")).toContain("INSERT INTO reward_transactions");
    expect(batchSql.join("\n")).toContain("INSERT INTO economy_transactions");
    expect(batchSql.join("\n")).toContain("INSERT INTO currency_ledger");
    expect(batchSql.join("\n")).toContain("UPDATE run_reward_previews");
    expect(batchSql.join("\n")).toContain("INSERT INTO idempotency_records");
  });
});
