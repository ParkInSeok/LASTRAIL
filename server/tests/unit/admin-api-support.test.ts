import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type QueryResult = Record<string, unknown> | Record<string, unknown>[] | null;

const principal = {
  id: "principal-1",
  access_subject: "access|viewer-1",
  role: "VIEWER",
  status: "ACTIVE",
  display_name: "Viewer",
};

function principalForRole(role: "OWNER" | "OPERATOR" | "VIEWER" = "VIEWER") {
  return { ...principal, role };
}

function createEnv(results: QueryResult[]): Env {
  const queue = [...results];

  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: {
      prepare() {
        return {
          bind() {
            return {
              first: async () => {
                const result = queue.shift();
                if (Array.isArray(result)) return result[0] ?? null;
                return result;
              },
              all: async () => {
                const result = queue.shift();
                return { results: Array.isArray(result) ? result : result === null ? [] : [result] };
              },
              run: async () => {
                throw new Error("Admin support account summary endpoint must be read-only.");
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

const accountRow = {
  account_id: "account-1",
  account_status: "ACTIVE",
  account_created_at: 1787650000000,
  account_updated_at: 1787655600000,
  display_name: "Conductor",
};

describe("admin-api support account summary contract", () => {
  it.each(["OWNER", "OPERATOR", "VIEWER"] as const)("allows %s to read support summaries through SUPPORT_READ", async (role) => {
    const response = await app.request(
      "/v1/admin/support/accounts/account-1",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([
        principalForRole(role),
        accountRow,
        [{ provider: "GUEST" }],
        { active_session_count: 1, latest_session_created_at: 1787651000000, latest_session_expires_at: 1788255800000 },
        [],
        [],
        null,
        null,
        [],
        [],
        [],
        { open_signals: 0, hard_violations: 0, latest_detection_at: null, primary_reason: null },
        null,
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      accountId: "account-1",
      account: {
        accountId: "account-1",
        accountStatus: "ACTIVE",
        displayName: "Conductor",
      },
      auth: {
        identityProviders: ["GUEST"],
        activeSessionCount: 1,
      },
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        repairEndpointsEnabled: false,
        source: "support_account_summary",
      },
    });
  });

  it("returns compact support sections with stable references and fallback read models", async () => {
    const response = await app.request(
      "/v1/admin/support/accounts/account-1",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([
        principal,
        accountRow,
        [{ provider: "GUEST" }, { provider: "GOOGLE" }],
        { active_session_count: 2, latest_session_created_at: 1787651000000, latest_session_expires_at: 1788255800000 },
        [
          {
            run_id: "run-1",
            status: "COMPLETED",
            started_at: 1787652000000,
            finished_at: 1787653000000,
            risk: "V",
            balance_config_version: 1,
            preview_json: JSON.stringify({ operationId: "opn_03_water_stop", actId: "act_01_wasteland_railway" }),
            outcome: "VICTORY",
            cleared_waves: 15,
            reward_preview_id: "reward_preview_1",
          },
        ],
        [
          {
            reward_preview_id: "reward_preview_1",
            run_id: "run-1",
            preview_status: "CLAIMED",
            reward_claim_id: "reward_claim_1",
            claimed_at: 1787654000000,
            balance_config_version: 1,
            expires_at: 1787740000000,
            reward_plan_json: JSON.stringify({
              rewardLines: [{ kind: "CURRENCY", id: "cur_credits", quantity: 100, source: "RUN_REWARD" }],
            }),
          },
        ],
        {
          account_id: "account-1",
          display_name: "Conductor",
          account_status: "ACTIVE",
          account_created_at: 1787650000000,
          highest_story_progress_id: "opn_03_water_stop",
          narrative_version: "act1.2026-08-27.001",
          last_offline_reward_claim_at: 1787655600000,
          last_story_progress_id: "opn_03_water_stop",
          last_balance_config_version: null,
          last_narrative_version: "act1.2026-08-27.001",
          last_previewed_at: null,
          revision: 2,
          state_updated_at: 1787655600000,
          last_claim_id: "offline_claim_1",
          last_claim_created_at: 1787655600000,
        },
        {
          account_id: "account-1",
          act_id: "act_01_wasteland_railway",
          highest_cleared_operation_id: "opn_03_water_stop",
          highest_story_progress_id: "opn_03_water_stop",
          cleared_operations_json: JSON.stringify(["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop"]),
          story_flags_json: JSON.stringify(["operation_cleared:opn_03_water_stop"]),
          narrative_version: null,
          last_progress_run_id: "run-1",
          updated_at: 1787655600000,
        },
        [{ unlock_id: "operation:opn_04_scrapstar_show" }],
        [
          {
            batch_id: "gacha_batch_1",
            banner_id: "banner-1",
            pull_count: 10,
            cost_item_id: "cur_command_ticket",
            cost_quantity: 10,
            result_summary_json: JSON.stringify({
              responseResults: [{ resultId: "operator_elite_1", pullIndex: 1, guaranteeType: "ELITE_30" }],
              postPityState: { bannerId: "banner-1", totalPullCount: 10 },
            }),
            created_at: 1787657000000,
          },
        ],
        [
          { operation: "run_finish", result_ref: "reward_preview_1", created_at: 1787653000000 },
          { operation: "gacha_pull", result_ref: "gacha_batch_1", created_at: 1787657000000 },
        ],
        { open_signals: 1, hard_violations: 0, latest_detection_at: 1787658000000, primary_reason: "ABNORMAL_CLEAR_TIME" },
        { status: "REVIEWING", reason: "manual review", updated_at: 1787658100000 },
      ]),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      accountId: "account-1",
      recentRuns: [
        {
          runId: "run-1",
          status: "COMPLETED",
          operationId: "opn_03_water_stop",
          actId: "act_01_wasteland_railway",
          outcome: "VICTORY",
          clearedWaves: 15,
          rewardPreviewId: "reward_preview_1",
        },
      ],
      runRewards: [
        {
          rewardPreviewId: "reward_preview_1",
          previewStatus: "CLAIMED",
          rewardClaimId: "reward_claim_1",
          rewardLineSummary: [{ kind: "CURRENCY", id: "cur_credits", quantity: 100, source: "RUN_REWARD" }],
        },
      ],
      offlineRewards: {
        offlineRewardState: {
          source: "account_offline_reward_state",
          revision: 2,
        },
        lastClaim: {
          offlineRewardClaimId: "offline_claim_1",
        },
      },
      storyProgress: {
        storyProgress: {
          highestStoryProgressId: "opn_03_water_stop",
          order: 3,
        },
        unlockedOperations: ["opn_01_last_departure", "opn_04_scrapstar_show"],
      },
      gacha: {
        recentBatches: [
          {
            batchId: "gacha_batch_1",
            guaranteeTypes: ["ELITE_30"],
          },
        ],
      },
      integrity: {
        openSignals: 1,
        hardViolations: 0,
        accountReviewStatus: "REVIEWING",
      },
      opsErrors: [],
    });
    expect(body.idempotency).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          operation: "run_finish",
          resultRef: "reward_preview_1",
          replaySafe: true,
        }),
      ]),
    );
  });

  it("rejects mutation-like request bodies", async () => {
    const response = await app.request(
      "/v1/admin/support/accounts/account-1",
      {
        headers: {
          "Cf-Access-Subject": "access|viewer-1",
          "Content-Length": "2",
        },
      },
      createEnv([principal]),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      error: "contract_violation",
      message: "Admin support account summary endpoints do not accept request bodies.",
    });
  });

  it("does not return tokens, raw request bodies, RNG, probability, localized dialogue, or reward internals", async () => {
    const response = await app.request(
      "/v1/admin/support/accounts/account-1",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([
        principal,
        accountRow,
        [{ provider: "GUEST" }],
        { active_session_count: 1, latest_session_created_at: 1787651000000, latest_session_expires_at: 1788255800000 },
        [],
        [],
        null,
        null,
        [],
        [],
        [],
        { open_signals: 0, hard_violations: 0, latest_detection_at: null, primary_reason: null },
        null,
      ]),
    );

    expect(response.status).toBe(200);
    const serialized = JSON.stringify(await response.json());
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("refresh_token_hash");
    expect(serialized).not.toContain("Authorization");
    expect(serialized).not.toContain("rawBody");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("rngSeed");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("fullWeight");
    expect(serialized).not.toContain("localizedDialogue");
    expect(serialized).not.toContain("storyProgressMultiplierBps");
    expect(serialized).not.toContain("rewardCalculation");
    expect(serialized).not.toContain("X-Account-Id");
  });
});
