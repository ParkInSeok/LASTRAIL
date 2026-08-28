import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type QueryResult = Record<string, unknown> | Record<string, unknown>[] | null;

function createEnv(results: QueryResult[], options: {
  role?: "OWNER" | "OPERATOR" | "VIEWER";
  onStatement?: (sql: string, values: unknown[]) => void;
} = {}): Env {
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
      prepare(sql: string) {
        return {
          bind(...values: unknown[]) {
            options.onStatement?.(sql, values);
            return {
              first: async () => {
                if (sql.includes("admin_principals")) {
                  return {
                    id: "principal-1",
                    access_subject: "access|viewer-1",
                    role: options.role ?? "VIEWER",
                    status: "ACTIVE",
                    display_name: "Viewer",
                  };
                }

                const result = queue.shift();
                return Array.isArray(result) ? (result[0] ?? null) : result;
              },
              all: async () => {
                const result = queue.shift();
                return { results: Array.isArray(result) ? result : result === null ? [] : [result] };
              },
              run: async () => {
                throw new Error("Admin story progress endpoints must be read-only.");
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

describe("admin-api story progress read-only contract", () => {
  it("requires an authorized admin principal", async () => {
    const env = createEnv([]);
    const response = await app.request("/v1/admin/story-progress", {}, env);

    expect(response.status).toBe(401);
    await expect(response.json()).resolves.toEqual({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" });
  });

  it("lists compact story progress read models with stable ids and raw statuses", async () => {
    const response = await app.request(
      "/v1/admin/story-progress?highestStoryProgressId=opn_03_water_stop&contentStatus=CANDIDATE&limit=10",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([
        [
          {
            account_id: "account-1",
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
            last_progress_run_id: "run-story-1",
            updated_at: 1787659200000,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          accountId: "account-1",
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
          narrativeVersion: "act1.2026-08-27.001",
          narrative: {
            narrativeVersion: "act1.2026-08-27.001",
            contentStatus: "CANDIDATE",
            localizationStatus: "PENDING",
            fixtureStatus: "PARTIAL",
            missingContentStatus: "UNKNOWN",
          },
          supportContext: {
            source: "account_story_progress",
            readOnly: true,
          },
          updatedAt: "2026-08-25T12:00:00.000Z",
        },
      ],
      nextCursor: null,
    });
  });

  it.each(["OWNER", "OPERATOR", "VIEWER"] as const)("allows %s to read story progress through STORY_PROGRESS_READ", async (role) => {
    const response = await app.request(
      "/v1/admin/story-progress",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([[]], { role }),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [],
    });
  });

  it("returns detail from account_story_progress and operation account_unlocks", async () => {
    const response = await app.request(
      "/v1/admin/story-progress/account-1",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([
        {
          account_id: "account-1",
          act_id: "act_01_wasteland_railway",
          highest_cleared_operation_id: "opn_05_night_boarders",
          highest_story_progress_id: "opn_05_night_boarders",
          cleared_operations_json: JSON.stringify([
            "opn_01_last_departure",
            "opn_02_first_signal",
            "opn_03_water_stop",
            "opn_04_scrapstar_show",
            "opn_05_night_boarders",
          ]),
          story_flags_json: JSON.stringify(["operation_cleared:opn_05_night_boarders"]),
          narrative_version: null,
          last_progress_run_id: "run-story-5",
          updated_at: 1787659300000,
        },
        [
          { unlock_id: "operation:opn_06_broken_line" },
          { unlock_id: "feature:train_slot_4" },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accountId: "account-1",
      actId: "act_01_wasteland_railway",
      operationId: "opn_05_night_boarders",
      storyProgress: {
        highestStoryProgressId: "opn_05_night_boarders",
        highestClearedOperationId: "opn_05_night_boarders",
        order: 5,
      },
      unlockedOperations: ["opn_01_last_departure", "opn_06_broken_line"],
      narrativeVersion: "act1.2026-08-27.001",
      supportContext: {
        source: "account_story_progress",
        readOnly: true,
        mutationEndpointsEnabled: false,
      },
    });
  });

  it("returns a stable none default for no-row account detail", async () => {
    const response = await app.request(
      "/v1/admin/story-progress/account-empty",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([null]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accountId: "account-empty",
      actId: "act_01_wasteland_railway",
      operationId: null,
      storyProgress: {
        highestStoryProgressId: "none",
        highestClearedOperationId: null,
        order: 0,
      },
      clearedOperationIds: [],
      unlockedOperations: ["opn_01_last_departure"],
      supportContext: {
        source: "default_none_progress",
        readOnly: true,
      },
    });
  });

  it("rejects mutation-like request bodies", async () => {
    const response = await app.request(
      "/v1/admin/story-progress/account-1",
      {
        method: "GET",
        headers: {
          "Cf-Access-Subject": "access|viewer-1",
          "Content-Length": "2",
        },
      },
      createEnv([]),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toEqual({
      error: "contract_violation",
      message: "Admin story progress read endpoints do not accept request bodies.",
    });
  });

  it("does not return localized dialogue, token, shortcut account, RNG, or reward calculation internals", async () => {
    const response = await app.request(
      "/v1/admin/story-progress/account-1",
      { headers: { "Cf-Access-Subject": "access|viewer-1" } },
      createEnv([
        {
          account_id: "account-1",
          act_id: "act_01_wasteland_railway",
          highest_cleared_operation_id: "opn_01_last_departure",
          highest_story_progress_id: "opn_01_last_departure",
          cleared_operations_json: JSON.stringify(["opn_01_last_departure"]),
          story_flags_json: JSON.stringify(["operation_cleared:opn_01_last_departure"]),
          narrative_version: "act1.2026-08-27.001",
          last_progress_run_id: "run-story-1",
          updated_at: 1787659200000,
        },
        [],
      ]),
    );

    expect(response.status).toBe(200);
    const serialized = JSON.stringify(await response.json());
    expect(serialized).not.toContain("localizedDialogue");
    expect(serialized).not.toContain("dialogueText");
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("secret");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("rewardCalculation");
    expect(serialized).not.toContain("X-Account-Id");
  });
});
