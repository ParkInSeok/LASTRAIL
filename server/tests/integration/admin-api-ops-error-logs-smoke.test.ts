import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/admin-api/src/index";

type SqlValue = string | number | Uint8Array | null;

class SqlJsD1Statement {
  readonly sql: string;
  private readonly database: initSqlJs.Database;
  private values: SqlValue[] = [];

  constructor(database: initSqlJs.Database, sql: string) {
    this.database = database;
    this.sql = sql;
  }

  bind(...values: SqlValue[]): SqlJsD1Statement {
    this.values = values;
    return this;
  }

  async first<T>(): Promise<T | null> {
    const statement = this.database.prepare(this.sql);
    try {
      statement.bind(this.values);
      if (!statement.step()) return null;
      return statement.getAsObject() as T;
    } finally {
      statement.free();
    }
  }

  async all<T>(): Promise<{ results: T[] }> {
    const statement = this.database.prepare(this.sql);
    const results: T[] = [];
    try {
      statement.bind(this.values);
      while (statement.step()) {
        results.push(statement.getAsObject() as T);
      }
      return { results };
    } finally {
      statement.free();
    }
  }
}

class SqlJsD1Database {
  private readonly database: initSqlJs.Database;

  constructor(database: initSqlJs.Database) {
    this.database = database;
  }

  prepare(sql: string): SqlJsD1Statement {
    return new SqlJsD1Statement(this.database, sql);
  }
}

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[12][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(29);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(db: initSqlJs.Database): Env {
  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function scalar(db: initSqlJs.Database, sql: string): number {
  const result = db.exec(sql);
  return Number(result[0]?.values[0]?.[0] ?? 0);
}

function seedOpsErrorLog(db: initSqlJs.Database): void {
  const now = Date.parse("2026-08-27T00:00:00.000Z");
  db.run(
    `INSERT INTO admin_principals (
       id, access_subject, role, status, display_name, created_at, updated_at
     ) VALUES ('principal-ops-1', 'access|ops-operator', 'OPERATOR', 'ACTIVE', 'Ops Operator', ?, ?);`,
    [now, now],
  );
  db.run("INSERT INTO accounts (id, status, created_at, updated_at) VALUES ('account-ops-smoke-1', 'ACTIVE', ?, ?);", [
    now,
    now,
  ]);
  db.run(
    `INSERT INTO ops_error_logs (
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id, reward_claim_id,
       offline_reward_claim_id, gacha_batch_id, gacha_banner_id, story_progress_id,
       integrity_event_id, support_ticket_id, idempotency_operation, idempotency_key_hash,
       correlation_json, sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     ) VALUES (
       'ops_error_smoke_1', ?, 'DEV', 'game_api', 'critical', 'gacha_state_conflict',
       'Gacha state conflict.', 'open', 'game_api', 'req_ops_smoke_1', 'account-ops-smoke-1',
       NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'gacha_pull', 'sha256:idem',
       '{"accountId":"account-ops-smoke-1"}', '{"component":"gacha"}', 1, 1,
       NULL, NULL, NULL, NULL, NULL, ?
     );`,
    [now, now],
  );
}

describe("admin-api ops error logs local D1 smoke", () => {
  it("reads list and detail without mutating migrated D1 state", async () => {
    const db = await createMigratedDb();
    seedOpsErrorLog(db);
    const env = createEnv(db);
    const headers = { "Cf-Access-Subject": "access|ops-operator" };
    const beforeLogs = scalar(db, "SELECT COUNT(*) FROM ops_error_logs;");

    const listResponse = await app.request(
      "/v1/admin/ops/error-logs?accountId=account-ops-smoke-1&severity=critical",
      { headers },
      env,
    );
    expect(listResponse.status).toBe(200);
    await expect(listResponse.json()).resolves.toMatchObject({
      permission: "OPS_ERROR_LOG_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          id: "ops_error_smoke_1",
          severity: "critical",
          errorCode: "gacha_state_conflict",
          accountId: "account-ops-smoke-1",
          retryable: true,
          masked: true,
        },
      ],
      nextCursor: null,
    });

    const detailResponse = await app.request(
      "/v1/admin/ops/error-logs/ops_error_smoke_1",
      { headers },
      env,
    );
    expect(detailResponse.status).toBe(200);
    const detailBody = await detailResponse.json();
    expect(detailBody).toMatchObject({
      item: {
        id: "ops_error_smoke_1",
        correlation: { accountId: "account-ops-smoke-1" },
        sanitizedContext: { component: "gacha" },
      },
      detail: null,
    });

    const serialized = JSON.stringify(detailBody);
    expect(serialized).not.toContain("Authorization");
    expect(serialized).not.toContain("accessToken");
    expect(serialized).not.toContain("refreshToken");
    expect(serialized).not.toContain("rawBody");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("rngSeed");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("fullWeight");
    expect(serialized).not.toContain("localizedDialogue");
    expect(serialized).not.toContain("storyProgressMultiplierBps");
    expect(serialized).not.toContain("rewardCalculation");

    expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs;")).toBe(beforeLogs);
    expect(db.exec("PRAGMA foreign_key_check;")).toEqual([]);
    expect(db.exec("PRAGMA quick_check;")[0]?.values).toEqual([["ok"]]);
  });
});
