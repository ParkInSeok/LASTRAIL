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
                throw new Error("Admin offline reward endpoints must be read-only.");
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

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

const offlineRewardRow = {
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
};

describe("admin-api offline reward read-only contract", () => {
  it("requires an authorized admin principal", async () => {
    const response = await app.request("/v1/admin/offline-rewards", {}, createEnv([]));

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" });
  });

  it.each(["OWNER", "OPERATOR", "VIEWER"] as const)("allows %s to read offline reward summaries", async (role) => {
    const response = await app.request(
      "/v1/admin/offline-rewards",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([principalForRole(role), []]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "OFFLINE_REWARD_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [],
      nextCursor: null,
    });
  });

  it("lists compact offline reward read models", async () => {
    const response = await app.request(
      "/v1/admin/offline-rewards?accountId=account-1&storyProgressId=opn_03_water_stop&limit=10",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([principal, [offlineRewardRow]]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "OFFLINE_REWARD_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          accountId: "account-1",
          displayName: "Conductor",
          accountStatus: "ACTIVE",
          storyProgressId: "opn_03_water_stop",
          narrativeVersion: "act1.2026-08-27.001",
          offlineRewardState: {
            source: "account_offline_reward_state",
            lastOfflineRewardClaimAt: "2026-08-25T11:00:00.000Z",
            lastStoryProgressId: "opn_03_water_stop",
            lastBalanceConfigVersion: null,
            lastNarrativeVersion: "act1.2026-08-27.001",
            lastPreviewedAt: null,
            revision: 2,
            updatedAt: "2026-08-25T11:00:00.000Z",
          },
          lastClaim: {
            offlineRewardClaimId: "offline_claim_1",
            claimedAt: "2026-08-25T11:00:00.000Z",
          },
          supportContext: {
            readOnly: true,
            claimMutationEnabled: false,
          },
        },
      ],
      nextCursor: null,
    });
  });

  it("returns detail with no-row lazy fallback context", async () => {
    const response = await app.request(
      "/v1/admin/offline-rewards/account-empty",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([
        principal,
        {
          account_id: "account-empty",
          display_name: null,
          account_status: "ACTIVE",
          account_created_at: 1787650000000,
          highest_story_progress_id: null,
          narrative_version: null,
          last_offline_reward_claim_at: null,
          last_story_progress_id: null,
          last_balance_config_version: null,
          last_narrative_version: null,
          last_previewed_at: null,
          revision: null,
          state_updated_at: null,
          last_claim_id: null,
          last_claim_created_at: null,
        },
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accountId: "account-empty",
      storyProgressId: "none",
      narrativeVersion: "act1.2026-08-27.001",
      offlineRewardState: {
        source: "accounts.created_at_fallback",
        lastOfflineRewardClaimAt: "2026-08-25T09:26:40.000Z",
        revision: 0,
      },
      lastClaim: null,
    });
  });

  it("rejects mutation-like request bodies", async () => {
    const response = await app.request(
      "/v1/admin/offline-rewards/account-1",
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
      message: "Admin offline reward read endpoints do not accept request bodies.",
    });
  });

  it("does not return tokens, raw request bodies, RNG, or reward calculation internals", async () => {
    const response = await app.request(
      "/v1/admin/offline-rewards/account-1",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([principal, offlineRewardRow]),
    );

    expect(response.status).toBe(200);
    const serialized = JSON.stringify(await response.json());
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("Authorization");
    expect(serialized).not.toContain("secret");
    expect(serialized).not.toContain("rawBody");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("storyProgressMultiplierBps");
    expect(serialized).not.toContain("rewardCalculation");
    expect(serialized).not.toContain("X-Account-Id");
  });
});
