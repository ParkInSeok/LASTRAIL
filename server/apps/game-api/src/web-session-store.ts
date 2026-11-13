import type { WebSessionRecord, WebSessionStore } from "./web-session";
import {commitLoginSession} from './maintenance-login';

type WebSessionRow = {
  token_hash: string;
  csrf_hash: string;
  account_id: string;
  created_at: number;
  expires_at: number;
  revoked_at: number | null;
};

/** D1 adapter for migration 0070. It never reads or changes game auth_sessions. */
export function createD1WebSessionStore(db: D1Database, environment?: unknown): WebSessionStore {
  return {
    async create(record) {
      await commitLoginSession(db,environment,'WEB',[db.prepare(`INSERT INTO web_sessions
        (token_hash, csrf_hash, account_id, created_at, expires_at, revoked_at)
        VALUES (?, ?, ?, ?, ?, NULL)`)
        .bind(record.tokenHash, record.csrfHash, record.accountId, record.createdAt, record.expiresAt)]);
    },
    async findActive(tokenHash, now): Promise<WebSessionRecord | null> {
      const row = await db.prepare(`SELECT token_hash, csrf_hash, account_id, created_at, expires_at, revoked_at
        FROM web_sessions WHERE token_hash=? AND revoked_at IS NULL AND expires_at>?`)
        .bind(tokenHash, now).first<WebSessionRow>();
      if (!row) return null;
      return { tokenHash: row.token_hash, csrfHash: row.csrf_hash, accountId: row.account_id,
        createdAt: row.created_at, expiresAt: row.expires_at, revokedAt: row.revoked_at };
    },
    async revoke(tokenHash, now) {
      const result = await db.prepare(`UPDATE web_sessions SET revoked_at=?
        WHERE token_hash=? AND revoked_at IS NULL AND expires_at>?`)
        .bind(now, tokenHash, now).run();
      if (result?.meta?.changes !== undefined) return result.meta.changes === 1;
      const row = await db.prepare('SELECT revoked_at FROM web_sessions WHERE token_hash=?')
        .bind(tokenHash).first<{ revoked_at: number | null }>();
      return row?.revoked_at === now;
    },
    async rotateCsrf(tokenHash, csrfHash, now) {
      const result = await db.prepare(`UPDATE web_sessions SET csrf_hash=?
        WHERE token_hash=? AND revoked_at IS NULL AND expires_at>?`)
        .bind(csrfHash, tokenHash, now).run();
      if (result?.meta?.changes !== undefined) return result.meta.changes === 1;
      const row = await db.prepare('SELECT csrf_hash FROM web_sessions WHERE token_hash=?')
        .bind(tokenHash).first<{ csrf_hash: string }>();
      return row?.csrf_hash === csrfHash;
    },
    async accountIsActive(accountId) {
      const row = await db.prepare(`SELECT id FROM accounts WHERE id=? AND status='ACTIVE'`)
        .bind(accountId).first();
      return row !== null;
    },
    async hasExactActiveTesterEntitlement(accountId, versionCode, now) {
      const row = await db.prepare(`SELECT account_id FROM account_tester_entitlements
        WHERE account_id=? AND environment='DEV' AND version_code=?
          AND status='ACTIVE' AND revoked_at IS NULL AND expires_at>?`)
        .bind(accountId, versionCode, now).first();
      return row !== null;
    },
  };
}
