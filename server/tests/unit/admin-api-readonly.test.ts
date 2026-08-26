import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";

type QueryResult = Record<string, unknown> | Record<string, unknown>[] | null;

function createEnv(results: QueryResult[], onStatement?: (sql: string, values: unknown[]) => void): Env {
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
          bind(...values: unknown[]) {
            onStatement?.(sql, values);
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
              run: async () => ({ success: true }),
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

  it("creates a player note as an audit log entry", async () => {
    const writes: Array<{ sql: string; values: unknown[] }> = [];
    const response = await app.request(
      "/v1/admin/players/account-1/notes",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Cf-Access-Subject": "access|owner-1",
          "x-request-id": "req-note-1",
        },
        body: JSON.stringify({ note: "Reviewed abnormal Training Core spread." }),
      },
      createEnv([{ id: "account-1" }], (sql, values) => {
        if (sql.includes("admin_audit_logs")) writes.push({ sql, values });
      }),
    );

    expect(response.status).toBe(201);
    await expect(response.json()).resolves.toMatchObject({
      note: {
        accountId: "account-1",
        action: "PLAYER_NOTE_CREATE",
        result: "SUCCESS",
      },
    });
    expect(writes).toHaveLength(1);
    expect(writes[0]?.values).toEqual([
      expect.stringMatching(/^audit_/u),
      "principal-1",
      "OWNER",
      "ACCOUNT_NOTE_WRITE",
      "PLAYER_NOTE_CREATE",
      "account",
      "account-1",
      "req-note-1",
      expect.stringMatching(/^sha256:[a-f0-9]{64}$/u),
      '{"note":"Reviewed abnormal Training Core spread."}',
      "SUCCESS",
      expect.any(Number),
    ]);
  });

  it("lists player notes from audit logs newest first", async () => {
    const response = await app.request(
      "/v1/admin/players/account-1/notes",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv([
        [
          {
            id: "audit-newer",
            principal_id: "principal-1",
            display_name: "Owner",
            action: "PLAYER_NOTE_CREATE",
            resource_id: "account-1",
            request_id: "req-note-2",
            change_summary_json: '{"note":"Second note."}',
            created_at: 1787715000000,
          },
          {
            id: "audit-older",
            principal_id: "principal-2",
            display_name: null,
            action: "PLAYER_NOTE_CREATE",
            resource_id: "account-1",
            request_id: "req-note-1",
            change_summary_json: '{"note":"First note."}',
            created_at: 1787714000000,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      items: [
        {
          id: "audit-newer",
          accountId: "account-1",
          note: "Second note.",
          authorId: "principal-1",
          authorDisplayName: "Owner",
          requestId: "req-note-2",
          createdAt: "2026-08-26T03:30:00.000Z",
        },
        {
          id: "audit-older",
          accountId: "account-1",
          note: "First note.",
          authorId: "principal-2",
          authorDisplayName: "principal-2",
          requestId: "req-note-1",
          createdAt: "2026-08-26T03:13:20.000Z",
        },
      ],
    });
  });

  it("lists audit log entries with action and resource filters", async () => {
    const response = await app.request(
      "/v1/admin/audit?action=PLAYER_NOTE_CREATE&resourceId=account-1",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv([
        [
          {
            id: "audit-1",
            principal_id: "principal-1",
            display_name: "Owner",
            role: "OWNER",
            permission: "ACCOUNT_NOTE_WRITE",
            action: "PLAYER_NOTE_CREATE",
            resource_type: "account",
            resource_id: "account-1",
            request_id: "req-note-1",
            confirmation_id: null,
            payload_hash: "sha256:abc",
            change_summary_json: '{"note":"Reviewed abnormal Training Core spread."}',
            result: "SUCCESS",
            error_code: null,
            created_at: 1787715000000,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      items: [
        {
          id: "audit-1",
          principalId: "principal-1",
          principalDisplayName: "Owner",
          role: "OWNER",
          permission: "ACCOUNT_NOTE_WRITE",
          action: "PLAYER_NOTE_CREATE",
          resourceType: "account",
          resourceId: "account-1",
          requestId: "req-note-1",
          confirmationId: null,
          payloadHash: "sha256:abc",
          changeSummary: { note: "Reviewed abnormal Training Core spread." },
          result: "SUCCESS",
          errorCode: null,
          createdAt: "2026-08-26T03:30:00.000Z",
        },
      ],
    });
  });

  it("lists integrity events with review state and filters", async () => {
    const response = await app.request(
      "/v1/admin/integrity/events?accountId=account-1&classification=SOFT_ANOMALY&reviewState=OPEN",
      { headers: { "Cf-Access-Subject": "access|owner-1" } },
      createEnv([
        [
          {
            id: "event-1",
            account_id: "account-1",
            run_id: "run-1",
            classification: "SOFT_ANOMALY",
            category: "DAMAGE_ANALYSIS",
            evidence_code: "DAMAGE_SPIKE",
            evidence_json: '{"maxDamageRatio":2.4}',
            review_state: "OPEN",
            assigned_to: null,
            created_at: 1787716800000,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toEqual({
      items: [
        {
          id: "event-1",
          accountId: "account-1",
          runId: "run-1",
          classification: "SOFT_ANOMALY",
          category: "DAMAGE_ANALYSIS",
          evidenceCode: "DAMAGE_SPIKE",
          evidence: { maxDamageRatio: 2.4 },
          reviewState: "OPEN",
          assignedTo: null,
          createdAt: "2026-08-26T04:00:00.000Z",
        },
      ],
    });
  });
});
