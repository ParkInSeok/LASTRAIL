import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type QueryResult = Record<string, unknown> | Record<string, unknown>[] | null;

const principal = {
  id: "principal-1",
  access_subject: "access|operator-1",
  role: "OPERATOR",
  status: "ACTIVE",
  display_name: "Operator",
};

function principalForRole(role: "OWNER" | "OPERATOR" | "VIEWER" = "OPERATOR") {
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
                throw new Error("Admin ops error logs endpoints must be read-only.");
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

const opsErrorRow = {
  id: "ops_error_1",
  occurred_at: 1787650000000,
  environment: "DEV",
  service: "game_api",
  severity: "error",
  error_code: "reward_plan_mismatch",
  message_summary: "Reward plan mismatch.",
  status: "open",
  source: "game_api",
  request_id: "req_ops_1",
  account_id: "account-1",
  run_id: "run-1",
  reward_preview_id: "reward_preview_1",
  reward_claim_id: null,
  offline_reward_claim_id: null,
  gacha_batch_id: null,
  gacha_banner_id: null,
  story_progress_id: "opn_04_scrapstar_show",
  integrity_event_id: null,
  support_ticket_id: null,
  idempotency_operation: "run_finish",
  idempotency_key_hash: "sha256:abc123",
  correlation_json: JSON.stringify({ accountId: "account-1", runId: "run-1" }),
  sanitized_context_json: JSON.stringify({ safe: true }),
  retryable: 1,
  masked: 1,
  payload_ref: null,
  trace_ref: null,
  resolved_at: null,
  resolved_by: null,
  resolution_note: null,
  created_at: 1787650000001,
};

describe("admin-api ops error logs read-only contract", () => {
  it("requires an authorized admin principal", async () => {
    const response = await app.request("/v1/admin/ops/error-logs", {}, createEnv([]));

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" });
  });

  it.each(["OWNER", "OPERATOR"] as const)("allows %s to list ops error logs through OPS_ERROR_LOG_READ", async (role) => {
    const response = await app.request(
      "/v1/admin/ops/error-logs?environment=DEV&service=game_api&severity=error&status=open&accountId=account-1&limit=10",
      { headers: { "Cf-Access-Subject": "access|operator-1" } },
      createEnv([principalForRole(role), [opsErrorRow]]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "OPS_ERROR_LOG_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          id: "ops_error_1",
          occurredAt: "2026-08-25T09:26:40.000Z",
          environment: "DEV",
          service: "game_api",
          severity: "error",
          errorCode: "reward_plan_mismatch",
          messageSummary: "Reward plan mismatch.",
          status: "open",
          source: "game_api",
          requestId: "req_ops_1",
          accountId: "account-1",
          runId: "run-1",
          rewardPreviewId: "reward_preview_1",
          storyProgressId: "opn_04_scrapstar_show",
          idempotencyOperation: "run_finish",
          idempotencyKeyHash: "sha256:abc123",
          retryable: true,
          masked: true,
          links: {
            accountId: "account-1",
            runId: "run-1",
            rewardPreviewId: "reward_preview_1",
          },
        },
      ],
      nextCursor: null,
    });
  });

  it("denies VIEWER by default for ops error log summaries", async () => {
    const response = await app.request(
      "/v1/admin/ops/error-logs",
      { headers: { "Cf-Access-Subject": "access|operator-1" } },
      createEnv([principalForRole("VIEWER")]),
    );

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_PERMISSION_DENIED" });
  });

  it("returns detail with sanitized context but no raw payload detail", async () => {
    const response = await app.request(
      "/v1/admin/ops/error-logs/ops_error_1",
      { headers: { "Cf-Access-Subject": "access|operator-1" } },
      createEnv([principal, opsErrorRow]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      item: {
        id: "ops_error_1",
        correlation: { accountId: "account-1", runId: "run-1" },
        sanitizedContext: { safe: true },
      },
      detail: null,
    });
  });

  it("returns not found for unknown log detail", async () => {
    const response = await app.request(
      "/v1/admin/ops/error-logs/missing",
      { headers: { "Cf-Access-Subject": "access|operator-1" } },
      createEnv([principal, null]),
    );

    expect(response.status).toBe(404);
    await expect(response.json()).resolves.toEqual({ error: "ops_error_log_not_found", message: "Ops error log was not found." });
  });

  it("rejects mutation-like request bodies", async () => {
    const response = await app.request(
      "/v1/admin/ops/error-logs",
      {
        headers: {
          "Cf-Access-Subject": "access|operator-1",
          "Content-Length": "2",
        },
      },
      createEnv([principal]),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      error: "contract_violation",
      message: "Admin ops error log read endpoints do not accept request bodies.",
    });
  });

  it("does not return raw request bodies, tokens, RNG, probability, weights, dialogue, or raw multipliers", async () => {
    const response = await app.request(
      "/v1/admin/ops/error-logs/ops_error_1",
      { headers: { "Cf-Access-Subject": "access|operator-1" } },
      createEnv([principal, opsErrorRow]),
    );

    expect(response.status).toBe(200);
    const serialized = JSON.stringify(await response.json());
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
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
