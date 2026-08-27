import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
};

const authSecret = "test-secret-minimum-32-characters";

async function createAuthorizationHeader(accountId = "account_1", sessionId = "session_1"): Promise<string> {
  const token = await createAccessToken({
    secret: authSecret,
    accountId,
    sessionId,
    nowMs: Date.now(),
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

function createEnv(options: {
  accountId?: string;
  sessionId?: string;
  storyProgress?: {
    act_id: string;
    highest_cleared_operation_id: string;
    highest_story_progress_id: string;
    cleared_operations_json: string;
    story_flags_json: string;
    narrative_version: string | null;
    last_progress_run_id: string | null;
    updated_at: number;
  } | null;
  unlockRows?: Array<{ unlock_id: string }>;
} = {}): TestEnv {
  const accountId = options.accountId ?? "account_1";
  const sessionId = options.sessionId ?? "session_1";
  const storyProgress = options.storyProgress ?? null;
  const unlockRows = options.unlockRows ?? [];

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
                    session_expires_at: Date.now() + 900000,
                    account_status: "ACTIVE",
                  };
                }
                if (sql.includes("FROM account_story_progress")) {
                  expect(values).toEqual([accountId]);
                  return storyProgress;
                }
                return null;
              },
              all: async () => {
                if (sql.includes("FROM account_unlocks")) {
                  expect(values).toEqual([accountId]);
                  return { results: unlockRows };
                }
                return { results: [] };
              },
            };
          },
        };
      },
      async batch() {
        throw new Error("GET /v1/story-progress must be read-only.");
      },
    } as unknown as D1Database,
  };
}

describe("game-api story progress contract", () => {
  it("requires player bearer auth", async () => {
    const response = await app.request("/v1/story-progress", { method: "GET" }, createEnv());

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      errorCode: "auth_required",
    });
  });

  it("returns the default Act 1 none progress for an authenticated account without a progress row", async () => {
    const response = await app.request(
      "/v1/story-progress",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv(),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      accepted: true,
      actId: "act_01_wasteland_railway",
      operationId: null,
      storyProgress: {
        highestStoryProgressId: "none",
        highestClearedOperationId: null,
        order: 0,
      },
      firstClear: null,
      clearedOperationIds: [],
      narrativeFlags: [],
      storyFlags: [],
      unlockedOperations: ["opn_01_last_departure"],
      narrativeVersion: "act1.2026-08-27.001",
      narrative: {
        narrativeVersion: "act1.2026-08-27.001",
        contentStatus: "CANDIDATE",
        localizationStatus: "PENDING",
        fixtureStatus: "PARTIAL",
        missingContentStatus: "UNKNOWN",
      },
      lastProgressRunId: null,
      updatedAt: null,
      errorCode: null,
      message: null,
    });
  });

  it("returns stored story progress and operation unlocks from the authenticated account context", async () => {
    const response = await app.request(
      "/v1/story-progress",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv({
        storyProgress: {
          act_id: "act_01_wasteland_railway",
          highest_cleared_operation_id: "opn_03_water_stop",
          highest_story_progress_id: "opn_03_water_stop",
          cleared_operations_json: JSON.stringify([
            "opn_01_last_departure",
            "opn_02_first_signal",
            "opn_03_water_stop",
          ]),
          story_flags_json: JSON.stringify(["operation_cleared:opn_03_water_stop"]),
          narrative_version: "act1.2026-08-27.001",
          last_progress_run_id: "run_story_1",
          updated_at: Date.parse("2026-08-27T00:00:00.000Z"),
        },
        unlockRows: [
          { unlock_id: "operation:opn_04_scrapstar_show" },
          { unlock_id: "feature:train_slot_3" },
        ],
      }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      actId: "act_01_wasteland_railway",
      operationId: "opn_03_water_stop",
      storyProgress: {
        highestStoryProgressId: "opn_03_water_stop",
        highestClearedOperationId: "opn_03_water_stop",
        order: 3,
      },
      clearedOperationIds: [
        "opn_01_last_departure",
        "opn_02_first_signal",
        "opn_03_water_stop",
      ],
      narrativeFlags: ["operation_cleared:opn_03_water_stop"],
      storyFlags: ["operation_cleared:opn_03_water_stop"],
      unlockedOperations: ["opn_01_last_departure", "opn_04_scrapstar_show"],
      lastProgressRunId: "run_story_1",
      updatedAt: "2026-08-27T00:00:00.000Z",
    });
  });

  it("rejects caller-supplied account id query parameters", async () => {
    const response = await app.request(
      "/v1/story-progress?accountId=account_other",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv(),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      errorCode: "contract_violation",
    });
  });

  it("does not return localized dialogue, token, account shortcut, RNG, or reward calculation internals", async () => {
    const response = await app.request(
      "/v1/story-progress",
      {
        method: "GET",
        headers: { Authorization: await createAuthorizationHeader() },
      },
      createEnv({
        storyProgress: {
          act_id: "act_01_wasteland_railway",
          highest_cleared_operation_id: "opn_01_last_departure",
          highest_story_progress_id: "opn_01_last_departure",
          cleared_operations_json: JSON.stringify(["opn_01_last_departure"]),
          story_flags_json: JSON.stringify(["operation_cleared:opn_01_last_departure"]),
          narrative_version: null,
          last_progress_run_id: "run_story_2",
          updated_at: Date.parse("2026-08-27T00:00:00.000Z"),
        },
      }),
    );

    expect(response.status).toBe(200);
    const serialized = JSON.stringify(await response.json());
    expect(serialized).not.toContain("localizedDialogue");
    expect(serialized).not.toContain("dialogueText");
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("secret");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("rewardMultiplier");
    expect(serialized).not.toContain("rewardCalculation");
    expect(serialized).not.toContain("accountId");
    expect(serialized).not.toContain("X-Account-Id");
  });
});
