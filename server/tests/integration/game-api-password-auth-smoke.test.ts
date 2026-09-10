import { describe, expect, it, vi } from "vitest";
import initSqlJs from "sql.js";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import app from "../../apps/game-api/src/index";
import { GmailRecoveryEmailSendError } from "../../apps/game-api/src/password-auth";
import type { RecoveryEmailMessage, RecoveryEmailSender } from "../../apps/game-api/src/password-auth";

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
      while (statement.step()) results.push(statement.getAsObject() as T);
      return { results };
    } finally {
      statement.free();
    }
  }

  async run(): Promise<{ success: true; meta: { changes: number } }> {
    this.database.run(this.sql, this.values);
    return { success: true, meta: { changes: this.database.getRowsModified() } };
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

  async batch(statements: SqlJsD1Statement[]): Promise<Array<{ success: true }>> {
    this.database.run("BEGIN;");
    try {
      for (const statement of statements) {
        await statement.run();
      }
      this.database.run("COMMIT;");
      return statements.map(() => ({ success: true }));
    } catch (error) {
      this.database.run("ROLLBACK;");
      throw error;
    }
  }
}

type PasswordAuthEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
  RECOVERY_EMAIL_SENDER?: RecoveryEmailSender;
};

const authSecret = "test-secret-minimum-32-characters";
const fixedNow = Date.parse("2026-09-06T00:00:00.000Z");

async function createMigratedDb(): Promise<initSqlJs.Database> {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");

  const migrationDir = join(process.cwd(), "packages/db/migrations");
  const migrations = readdirSync(migrationDir).filter((file) => /^00(0[1-9]|[123][0-9])_.+\.sql$/u.test(file)).sort();
  expect(migrations).toHaveLength(30);

  for (const migration of migrations) {
    db.run(readFileSync(join(migrationDir, migration), "utf8"));
  }

  return db;
}

function createEnv(db: initSqlJs.Database, messages: RecoveryEmailMessage[] = []): PasswordAuthEnv {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    RECOVERY_EMAIL_SENDER: {
      sendRecoveryEmail: async (message) => {
        messages.push(message);
      },
    },
    DB: new SqlJsD1Database(db) as unknown as D1Database,
  };
}

function scalar(db: initSqlJs.Database, sql: string): number {
  const result = db.exec(sql);
  return Number(result[0]?.values[0]?.[0] ?? 0);
}

function firstRow<T extends Record<string, SqlValue>>(db: initSqlJs.Database, sql: string): T | null {
  const result = db.exec(sql);
  if (result.length === 0 || result[0]?.values.length === 0) return null;
  const columns = result[0]?.columns ?? [];
  const values = result[0]?.values[0] ?? [];
  return Object.fromEntries(columns.map((column, index) => [column, values[index] ?? null])) as T;
}

describe("game-api password auth and recovery email local D1 smoke", () => {
  it("fails closed when recovery email sending is not configured", async () => {
    const db = await createMigratedDb();
    const env = createEnv(db);
    delete env.RECOVERY_EMAIL_SENDER;

    const response = await app.request(
      "/v1/auth/password/recovery-email/verify/start",
      {
        method: "POST",
        body: JSON.stringify({ purpose: "PASSWORD_REGISTER", recoveryEmail: "commander@example.com" }),
      },
      env,
    );

    expect(response.status).toBe(503);
    await expect(response.json()).resolves.toEqual({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: null,
      retryAfterSeconds: null,
      errorCode: "recovery_email_service_unavailable",
      message: "Recovery email verification is not configured.",
    });
    expect(scalar(db, "SELECT COUNT(*) FROM auth_email_verification_challenges;")).toBe(0);
  });

  it("logs Gmail recovery email sender failures without exposing provider details to players", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const env = createEnv(db);
      env.RECOVERY_EMAIL_SENDER = {
        sendRecoveryEmail: async () => {
          throw Object.assign(new GmailRecoveryEmailSendError("gmail_send_unauthorized"), {
            failureStage: "send_response",
            upstreamStatus: 403,
          });
        },
      };

      const response = await app.request(
        "/v1/auth/password/recovery-email/verify/start",
        {
          method: "POST",
          headers: { "X-Request-Id": "req_recovery_gmail_failure_1" },
          body: JSON.stringify({ purpose: "PASSWORD_REGISTER", recoveryEmail: "Commander@Example.com" }),
        },
        env,
      );

      expect(response.status).toBe(503);
      await expect(response.json()).resolves.toEqual({
        accepted: false,
        verificationStatus: "REJECTED",
        challengeId: null,
        expiresAt: null,
        retryAfterSeconds: null,
        errorCode: "recovery_email_service_unavailable",
        message: "Recovery email verification is not configured.",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM auth_email_verification_challenges WHERE status = 'CANCELLED';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM ops_error_logs WHERE error_code = 'gmail_send_unauthorized';")).toBe(1);
      expect(firstRow(db, "SELECT environment, service, severity, error_code, status, source, request_id, masked, retryable FROM ops_error_logs WHERE error_code = 'gmail_send_unauthorized';")).toEqual({
        environment: "DEV",
        service: "game_api",
        severity: "error",
        error_code: "gmail_send_unauthorized",
        status: "open",
        source: "game_api",
        request_id: "req_recovery_gmail_failure_1",
        masked: 1,
        retryable: 0,
      });
      const opsLogContext = firstRow<{ sanitized_context_json: string }>(
        db,
        "SELECT sanitized_context_json FROM ops_error_logs WHERE error_code = 'gmail_send_unauthorized';",
      );
      expect(JSON.parse(opsLogContext?.sanitized_context_json ?? "{}")).toMatchObject({
        emailMasked: "c********@example.com",
        failureCode: "gmail_send_unauthorized",
        failureStage: "send_response",
        provider: "GMAIL",
        upstreamStatus: 403,
      });
      const serializedLog = JSON.stringify(db.exec("SELECT * FROM ops_error_logs;"));
      expect(serializedLog).toContain("c********@example.com");
      expect(serializedLog).not.toContain("Commander@Example.com");
      expect(serializedLog).not.toContain("commander@example.com");
      expect(serializedLog).not.toContain("verificationCode");
      expect(serializedLog).not.toContain("Authorization");
      expect(serializedLog).not.toContain("refresh_token");
      expect(serializedLog).not.toContain("access_token");
    } finally {
      vi.useRealTimers();
    }
  });

  it("registers, signs in, and resets password only after verified recovery email ownership", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const messages: RecoveryEmailMessage[] = [];
      const env = createEnv(db, messages);

      const rejectedRegister = await app.request(
        "/v1/auth/password/register",
        {
          method: "POST",
          body: JSON.stringify({
            loginId: "rail_commander",
            password: "CorrectHorseBattery1!",
            recoveryEmail: "Commander@Example.com",
          }),
        },
        env,
      );
      expect(rejectedRegister.status).toBe(409);
      await expect(rejectedRegister.json()).resolves.toMatchObject({
        accepted: false,
        registerStatus: "REJECTED",
        errorCode: "recovery_email_verification_required",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(0);

      const startResponse = await app.request(
        "/v1/auth/password/recovery-email/verify/start",
        {
          method: "POST",
          body: JSON.stringify({ purpose: "PASSWORD_REGISTER", recoveryEmail: "Commander@Example.com" }),
        },
        env,
      );
      expect(startResponse.status).toBe(200);
      const startBody = await startResponse.json() as { challengeId: string; expiresAt: string };
      expect(startBody).toMatchObject({
        accepted: true,
        verificationStatus: "PENDING",
        retryAfterSeconds: 60,
        errorCode: null,
        message: null,
      });
      expect(startBody.challengeId).toMatch(/^email_challenge_/u);
      expect(startBody.expiresAt).toBe("2026-09-06T00:15:00.000Z");
      expect(messages).toHaveLength(1);
      expect(messages[0]).toMatchObject({
        purpose: "PASSWORD_REGISTER",
        challengeId: startBody.challengeId,
        emailMasked: "c********@example.com",
      });

      const rateLimited = await app.request(
        "/v1/auth/password/recovery-email/verify/start",
        {
          method: "POST",
          body: JSON.stringify({ purpose: "PASSWORD_REGISTER", recoveryEmail: "commander@example.com" }),
        },
        env,
      );
      expect(rateLimited.status).toBe(429);
      await expect(rateLimited.json()).resolves.toMatchObject({
        accepted: false,
        verificationStatus: "REJECTED",
        errorCode: "recovery_email_resend_limited",
      });

      const confirmResponse = await app.request(
        "/v1/auth/password/recovery-email/verify/confirm",
        {
          method: "POST",
          body: JSON.stringify({
            challengeId: startBody.challengeId,
            verificationCode: messages[0]?.verificationCode,
          }),
        },
        env,
      );
      expect(confirmResponse.status).toBe(200);
      await expect(confirmResponse.json()).resolves.toMatchObject({
        accepted: true,
        verificationStatus: "VERIFIED",
        challengeId: startBody.challengeId,
        errorCode: null,
        message: null,
      });

      const secondConfirm = await app.request(
        "/v1/auth/password/recovery-email/verify/confirm",
        {
          method: "POST",
          body: JSON.stringify({
            challengeId: startBody.challengeId,
            verificationCode: messages[0]?.verificationCode,
          }),
        },
        env,
      );
      expect(secondConfirm.status).toBe(409);
      await expect(secondConfirm.json()).resolves.toMatchObject({
        accepted: false,
        verificationStatus: "REJECTED",
        errorCode: "recovery_email_challenge_invalid",
      });

      const registerResponse = await app.request(
        "/v1/auth/password/register",
        {
          method: "POST",
          body: JSON.stringify({
            loginId: "rail_commander",
            password: "CorrectHorseBattery1!",
            recoveryEmail: "Commander@Example.com",
            verificationChallengeId: startBody.challengeId,
          }),
        },
        env,
      );
      expect(registerResponse.status).toBe(200);
      const registerBody = await registerResponse.json() as { accessToken: string; refreshToken: string; account: { id: string } };
      expect(registerBody).toMatchObject({
        tokenType: "Bearer",
        registerStatus: "REGISTERED",
        account: { status: "ACTIVE" },
      });
      expect(registerBody.account.id).toMatch(/^account_/u);

      const rawDatabaseState = JSON.stringify(db.export());
      expect(rawDatabaseState).not.toContain("CorrectHorseBattery1!");
      expect(rawDatabaseState).not.toContain(messages[0]?.verificationCode);
      expect(rawDatabaseState).not.toContain("Commander@Example.com");
      expect(rawDatabaseState).not.toContain(registerBody.refreshToken);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_password_credentials WHERE login_id_normalized = 'rail_commander';")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_recovery_emails WHERE email_normalized = 'commander@example.com' AND status = 'VERIFIED';")).toBe(1);

      const reusedChallengeRegister = await app.request(
        "/v1/auth/password/register",
        {
          method: "POST",
          body: JSON.stringify({
            loginId: "second_commander",
            password: "SecondCorrectHorseBattery1!",
            recoveryEmail: "Commander@Example.com",
            verificationChallengeId: startBody.challengeId,
          }),
        },
        env,
      );
      expect(reusedChallengeRegister.status).toBe(409);
      await expect(reusedChallengeRegister.json()).resolves.toMatchObject({
        accepted: false,
        registerStatus: "REJECTED",
        errorCode: "recovery_email_verification_required",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(1);

      const loginResponse = await app.request(
        "/v1/auth/password/login",
        {
          method: "POST",
          body: JSON.stringify({ loginId: "RAIL_COMMANDER", password: "CorrectHorseBattery1!" }),
        },
        env,
      );
      expect(loginResponse.status).toBe(200);
      await expect(loginResponse.json()).resolves.toMatchObject({
        tokenType: "Bearer",
        signInStatus: "SIGNED_IN",
        account: { id: registerBody.account.id, status: "ACTIVE" },
      });

      const resetStart = await app.request(
        "/v1/auth/password/reset/start",
        {
          method: "POST",
          body: JSON.stringify({ loginId: "rail_commander", recoveryEmail: "commander@example.com" }),
        },
        env,
      );
      expect(resetStart.status).toBe(200);
      await expect(resetStart.json()).resolves.toEqual({
        accepted: true,
        resetStatus: "EMAIL_SENT_IF_AVAILABLE",
        errorCode: null,
        message: null,
      });
      expect(messages).toHaveLength(2);
      expect(messages[1]?.purpose).toBe("PASSWORD_RESET");
      expect(messages[1]?.challengeId).toMatch(/^email_challenge_/u);
      expect(messages[1]?.verificationCode).toMatch(/^\d{8}$/u);

      const resetConfirm = await app.request(
        "/v1/auth/password/reset/confirm",
        {
          method: "POST",
          body: JSON.stringify({
            loginId: "rail_commander",
            recoveryEmail: "commander@example.com",
            verificationCode: messages[1]?.verificationCode,
            newPassword: "NewCorrectHorseBattery1!",
          }),
        },
        env,
      );
      expect(resetConfirm.status).toBe(200);
      await expect(resetConfirm.json()).resolves.toEqual({
        accepted: true,
        resetStatus: "RESET_CONFIRMED",
        errorCode: null,
        message: "Password was reset. Please sign in again.",
      });
      expect(scalar(db, `SELECT COUNT(*) FROM auth_sessions WHERE account_id = '${registerBody.account.id}' AND status = 'ACTIVE';`)).toBe(0);

      const secondResetConfirm = await app.request(
        "/v1/auth/password/reset/confirm",
        {
          method: "POST",
          body: JSON.stringify({
            loginId: "rail_commander",
            recoveryEmail: "commander@example.com",
            verificationCode: messages[1]?.verificationCode,
            newPassword: "AnotherCorrectHorseBattery1!",
          }),
        },
        env,
      );
      expect(secondResetConfirm.status).toBe(409);
      await expect(secondResetConfirm.json()).resolves.toEqual({
        errorCode: "password_reset_challenge_invalid",
        message: "Password reset challenge is invalid or expired.",
      });

      const oldLogin = await app.request(
        "/v1/auth/password/login",
        {
          method: "POST",
          body: JSON.stringify({ loginId: "rail_commander", password: "CorrectHorseBattery1!" }),
        },
        env,
      );
      expect(oldLogin.status).toBe(401);
      await expect(oldLogin.json()).resolves.toEqual({
        errorCode: "password_login_invalid",
        message: "Login ID or password is invalid.",
      });

      const newLogin = await app.request(
        "/v1/auth/password/login",
        {
          method: "POST",
          body: JSON.stringify({ loginId: "rail_commander", password: "NewCorrectHorseBattery1!" }),
        },
        env,
      );
      expect(newLogin.status).toBe(200);
      await expect(newLogin.json()).resolves.toMatchObject({
        tokenType: "Bearer",
        signInStatus: "SIGNED_IN",
        account: { id: registerBody.account.id, status: "ACTIVE" },
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it("links a guest account to password auth only with a consumed same-account recovery email challenge", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
    try {
      const db = await createMigratedDb();
      const messages: RecoveryEmailMessage[] = [];
      const env = createEnv(db, messages);

      const guestResponse = await app.request("/v1/auth/guest", { method: "POST" }, env);
      expect(guestResponse.status).toBe(200);
      const guest = await guestResponse.json() as { accessToken: string; account: { id: string } };

      const startResponse = await app.request(
        "/v1/auth/password/recovery-email/verify/start",
        {
          method: "POST",
          headers: { Authorization: `Bearer ${guest.accessToken}` },
          body: JSON.stringify({ purpose: "PASSWORD_LINK", recoveryEmail: "guest@example.com" }),
        },
        env,
      );
      expect(startResponse.status).toBe(200);
      const startBody = await startResponse.json() as { challengeId: string };

      const rejectedLink = await app.request(
        "/v1/auth/password/link",
        {
          method: "POST",
          headers: { Authorization: `Bearer ${guest.accessToken}` },
          body: JSON.stringify({
            loginId: "guest_commander",
            password: "GuestPassword123!",
            recoveryEmail: "guest@example.com",
            verificationChallengeId: startBody.challengeId,
          }),
        },
        env,
      );
      expect(rejectedLink.status).toBe(409);
      await expect(rejectedLink.json()).resolves.toMatchObject({
        accepted: false,
        linkStatus: "REJECTED",
        errorCode: "recovery_email_verification_required",
      });
      expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_password_credentials;")).toBe(0);

      const confirm = await app.request(
        "/v1/auth/password/recovery-email/verify/confirm",
        {
          method: "POST",
          headers: { Authorization: `Bearer ${guest.accessToken}` },
          body: JSON.stringify({ challengeId: startBody.challengeId, verificationCode: messages[0]?.verificationCode }),
        },
        env,
      );
      expect(confirm.status).toBe(200);

      const link = await app.request(
        "/v1/auth/password/link",
        {
          method: "POST",
          headers: { Authorization: `Bearer ${guest.accessToken}` },
          body: JSON.stringify({
            loginId: "guest_commander",
            password: "GuestPassword123!",
            recoveryEmail: "guest@example.com",
            verificationChallengeId: startBody.challengeId,
          }),
        },
        env,
      );
      expect(link.status).toBe(200);
      await expect(link.json()).resolves.toMatchObject({
        tokenType: "Bearer",
        linkStatus: "LINKED",
        account: { id: guest.account.id, status: "ACTIVE" },
      });
      expect(scalar(db, "SELECT COUNT(*) FROM accounts;")).toBe(1);
      expect(scalar(db, "SELECT COUNT(*) FROM auth_password_credentials WHERE account_id = (SELECT id FROM accounts LIMIT 1);")).toBe(1);
    } finally {
      vi.useRealTimers();
    }
  });
});
