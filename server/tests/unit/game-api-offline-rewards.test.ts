import { describe, expect, it, vi } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
};

const authSecret = "test-secret-minimum-32-characters";
const fixedNow = Date.parse("2026-08-28T00:00:00.000Z");

async function createAuthorizationHeader(accountId = "account_1", sessionId = "session_1"): Promise<string> {
  const token = await createAccessToken({
    secret: authSecret,
    accountId,
    sessionId,
    nowMs: fixedNow,
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

function createEnv(options: {
  accountId?: string;
  sessionId?: string;
  accountCreatedAt?: number;
  offlineRewardState?: {
    last_offline_reward_claim_at: number;
    last_story_progress_id: string;
    last_balance_config_version: number | null;
    last_narrative_version: string | null;
    last_previewed_at: number | null;
    revision: number;
    updated_at: number;
  } | null;
  storyProgress?: {
    highest_story_progress_id: string;
    narrative_version: string | null;
  } | null;
} = {}): TestEnv {
  const accountId = options.accountId ?? "account_1";
  const sessionId = options.sessionId ?? "session_1";
  const accountCreatedAt = options.accountCreatedAt ?? fixedNow - 4 * 60 * 60 * 1000;
  const offlineRewardState = options.offlineRewardState ?? null;
  const storyProgress = options.storyProgress ?? null;

  return {
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
                  expect(values).toEqual([sessionId, accountId]);
                  return {
                    account_id: accountId,
                    session_id: sessionId,
                    session_status: "ACTIVE",
                    session_expires_at: fixedNow + 900000,
                    account_status: "ACTIVE",
                    account_created_at: accountCreatedAt,
                  };
                }
                if (sql.includes("FROM account_story_progress")) {
                  expect(values).toEqual([accountId]);
                  return storyProgress;
                }
                if (sql.includes("FROM account_offline_reward_state")) {
                  expect(values).toEqual([accountId]);
                  return offlineRewardState;
                }
                return null;
              },
              all: async () => ({ results: [] }),
            };
          },
        };
      },
      async batch() {
        throw new Error("GET /v1/offline-rewards/preview must be read-only.");
      },
    } as unknown as D1Database,
  };
}

describe("game-api offline reward preview contract", () => {
  it("requires player bearer auth", async () => {
    const response = await app.request("/v1/offline-rewards/preview", { method: "GET" }, createEnv());

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      previewStatus: "REJECTED",
      errorCode: "auth_required",
    });
  });

  it("returns a server-calculated none progress preview for an account without story progress", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const response = await app.request(
        "/v1/offline-rewards/preview",
        {
          method: "GET",
          headers: { Authorization: await createAuthorizationHeader() },
        },
        createEnv({ accountCreatedAt: fixedNow - 4 * 60 * 60 * 1000 }),
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toEqual({
        accepted: true,
        previewStatus: "AVAILABLE",
        storyProgressId: "none",
        claimableSeconds: 14400,
        claimableHours: 4,
        offlineStorageCapHours: 4,
        minimumClaimSeconds: 600,
        maxClaimableHours: 12,
        rewardLines: [
          { kind: "CURRENCY", id: "cur_credits", quantity: 400, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_machine_parts", quantity: 200, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_tactical_data", quantity: 120, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_operator_exp", quantity: 80, source: "OFFLINE_REWARD" },
        ],
        calculation: {
          storyProgressId: "none",
          storyProgressMultiplierLabel: "1.00x",
          cappedOfflineSeconds: 14400,
          capSource: "STORY_PROGRESS_STORAGE_CAP",
        },
        narrativeVersion: "act1.2026-08-27.001",
        serverCalculatedAt: "2026-08-28T00:00:00.000Z",
        errorCode: null,
        message: null,
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it("uses stored story progress for cap and multiplier without trusting risk", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const response = await app.request(
        "/v1/offline-rewards/preview",
        {
          method: "GET",
          headers: { Authorization: await createAuthorizationHeader() },
        },
        createEnv({
          accountCreatedAt: fixedNow - 20 * 60 * 60 * 1000,
          storyProgress: {
            highest_story_progress_id: "raid_raider_king_engine",
            narrative_version: "act1.2026-08-27.001",
          },
        }),
      );

      expect(response.status).toBe(200);
      const body = await response.json();
      expect(body).toMatchObject({
        accepted: true,
        previewStatus: "AVAILABLE",
        storyProgressId: "raid_raider_king_engine",
        claimableSeconds: 43200,
        claimableHours: 12,
        offlineStorageCapHours: 12,
        rewardLines: [
          { kind: "CURRENCY", id: "cur_credits", quantity: 3600, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_machine_parts", quantity: 1800, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_tactical_data", quantity: 1080, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_operator_exp", quantity: 720, source: "OFFLINE_REWARD" },
        ],
        calculation: {
          storyProgressId: "raid_raider_king_engine",
          storyProgressMultiplierLabel: "3.00x",
          cappedOfflineSeconds: 43200,
          capSource: "STORY_PROGRESS_STORAGE_CAP",
        },
      });
      expect(JSON.stringify(body)).not.toContain("risk");
    } finally {
      vi.useRealTimers();
    }
  });

  it("uses offline reward state last claim time before account creation bootstrap baseline", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const response = await app.request(
        "/v1/offline-rewards/preview",
        {
          method: "GET",
          headers: { Authorization: await createAuthorizationHeader() },
        },
        createEnv({
          accountCreatedAt: fixedNow - 20 * 60 * 60 * 1000,
          offlineRewardState: {
            last_offline_reward_claim_at: fixedNow - 2 * 60 * 60 * 1000,
            last_story_progress_id: "opn_03_water_stop",
            last_balance_config_version: null,
            last_narrative_version: "act1.2026-08-27.001",
            last_previewed_at: fixedNow - 60 * 60 * 1000,
            revision: 1,
            updated_at: fixedNow - 2 * 60 * 60 * 1000,
          },
          storyProgress: {
            highest_story_progress_id: "opn_03_water_stop",
            narrative_version: "act1.2026-08-27.001",
          },
        }),
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toMatchObject({
        accepted: true,
        previewStatus: "AVAILABLE",
        storyProgressId: "opn_03_water_stop",
        claimableSeconds: 7200,
        claimableHours: 2,
        offlineStorageCapHours: 7,
        rewardLines: [
          { kind: "CURRENCY", id: "cur_credits", quantity: 304, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_machine_parts", quantity: 152, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_tactical_data", quantity: 91, source: "OFFLINE_REWARD" },
          { kind: "CURRENCY", id: "cur_operator_exp", quantity: 60, source: "OFFLINE_REWARD" },
        ],
        calculation: {
          storyProgressId: "opn_03_water_stop",
          storyProgressMultiplierLabel: "1.52x",
          cappedOfflineSeconds: 7200,
          capSource: "ACTUAL_OFFLINE_DURATION",
        },
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it("returns a zero reward preview below the minimum offline duration", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const response = await app.request(
        "/v1/offline-rewards/preview",
        {
          method: "GET",
          headers: { Authorization: await createAuthorizationHeader() },
        },
        createEnv({ accountCreatedAt: fixedNow - 9 * 60 * 1000 }),
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toMatchObject({
        accepted: true,
        previewStatus: "EMPTY",
        storyProgressId: "none",
        claimableSeconds: 0,
        claimableHours: 0,
        rewardLines: [],
        errorCode: null,
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it("rejects caller-supplied account, story progress, risk, duration, or reward fields", async () => {
    const forbiddenQueries = [
      "accountId=account_other",
      "storyProgress=opn_06_broken_line",
      "risk=V",
      "offlineSeconds=43200",
      "rewardAmount=9999",
      "currencyGrants=cur_credits",
    ];

    for (const query of forbiddenQueries) {
      const response = await app.request(
        `/v1/offline-rewards/preview?${query}`,
        {
          method: "GET",
          headers: { Authorization: await createAuthorizationHeader() },
        },
        createEnv(),
      );

      expect(response.status).toBe(400);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        previewStatus: "REJECTED",
        errorCode: "contract_violation",
      });
    }

    const bodyLikeResponse = await app.request(
      "/v1/offline-rewards/preview",
      {
        method: "GET",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "content-length": "2",
        },
      },
      createEnv(),
    );
    expect(bodyLikeResponse.status).toBe(400);
    await expect(bodyLikeResponse.json()).resolves.toMatchObject({
      accepted: false,
      previewStatus: "REJECTED",
      errorCode: "contract_violation",
    });
  });

  it("does not return blocked reward lines or sensitive/internal fields", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const response = await app.request(
        "/v1/offline-rewards/preview",
        {
          method: "GET",
          headers: { Authorization: await createAuthorizationHeader() },
        },
        createEnv(),
      );

      expect(response.status).toBe(200);
      const serialized = JSON.stringify(await response.json());
      expect(serialized).not.toContain("cur_special_core");
      expect(serialized).not.toContain("cur_command_ticket");
      expect(serialized).not.toContain("cur_command_ticket_fragment");
      expect(serialized).not.toContain("accessToken");
      expect(serialized).not.toContain("refreshToken");
      expect(serialized).not.toContain("secret");
      expect(serialized).not.toContain("rawRng");
      expect(serialized).not.toContain("rewardCalculation");
      expect(serialized).not.toContain("storyProgressMultiplierBps");
      expect(serialized).not.toContain("accountId");
      expect(serialized).not.toContain("X-Account-Id");
    } finally {
      vi.useRealTimers();
    }
  });

  it("requires Idempotency-Key for offline reward claim", async () => {
    const response = await app.request(
      "/v1/offline-rewards/claim",
      {
        method: "POST",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv(),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      claimStatus: "REJECTED",
      offlineRewardClaimId: null,
      errorCode: "contract_violation",
    });
  });

  it("rejects caller-supplied offline claim query or body fields", async () => {
    const queryResponse = await app.request(
      "/v1/offline-rewards/claim?storyProgress=opn_06_broken_line",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "offline-claim-key-1",
        },
      },
      createEnv(),
    );

    expect(queryResponse.status).toBe(400);
    await expect(queryResponse.json()).resolves.toMatchObject({
      accepted: false,
      claimStatus: "REJECTED",
      errorCode: "contract_violation",
    });

    const bodyResponse = await app.request(
      "/v1/offline-rewards/claim",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "offline-claim-key-2",
          "content-length": "28",
        },
        body: JSON.stringify({ rewardAmount: 9999 }),
      },
      createEnv(),
    );

    expect(bodyResponse.status).toBe(400);
    await expect(bodyResponse.json()).resolves.toMatchObject({
      accepted: false,
      claimStatus: "REJECTED",
      errorCode: "contract_violation",
    });
  });
});
