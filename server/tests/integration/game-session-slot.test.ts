import { readFileSync } from 'node:fs';
import initSqlJs from 'sql.js';
import { describe, expect, it } from 'vitest';
import app from '../../apps/game-api/src/index';
import { hashRefreshToken } from '../../apps/game-api/src/auth';
import { setup } from '../helpers/departure-fixture';

async function fixture() {
  const SQL = await initSqlJs(), db = new SQL.Database();
  db.run(readFileSync('packages/db/migrations/0001_initial.sql', 'utf8'));
  db.run("INSERT INTO accounts(id,status,created_at,updated_at) VALUES('a','ACTIVE',0,0),('b','ACTIVE',0,0)");
  const insert = (id: string, family = id, generation = 0, at = 10, account = 'a') =>
    db.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES(?,?,?,?,?,'ACTIVE',?,9999999999999)",
      [id, account, id, family, generation, at]);
  insert('old'); insert('older', 'other', 0, 5);
  db.run(readFileSync('packages/db/migrations/0095_game_session_slot.sql', 'utf8'));
  const rows = () => db.exec("SELECT id,status FROM auth_sessions ORDER BY id")[0]!.values;
  return { db, insert, rows };
}

describe('one active game session, new verified login wins', () => {
  it('reconciles legacy duplicates deterministically and replaces the previous session', async () => {
    const f = await fixture();
    expect(f.rows()).toEqual([['old','ACTIVE'],['older','REVOKED']]);
    f.insert('new');
    expect(f.rows()).toEqual([['new','ACTIVE'],['old','REVOKED'],['older','REVOKED']]);
    f.db.close();
  });
  it('keeps other accounts and rolls back takeover when a login transaction fails', async () => {
    const f = await fixture(); f.insert('peer','peer',0,10,'b');
    f.db.run('BEGIN'); f.insert('new');
    expect(() => f.insert('new')).toThrow(); f.db.run('ROLLBACK');
    expect(f.rows()).toContainEqual(['old','ACTIVE']);
    expect(f.rows()).toContainEqual(['peer','ACTIVE']); f.db.close();
  });
  it('allows valid rotation and rejects a refresh prepared before a new login', async () => {
    const f = await fixture();
    f.db.run("UPDATE auth_sessions SET status='ROTATED',rotated_at=20 WHERE id='old'");
    f.insert('rotated','old',1,20);
    f.insert('new','new',0,30);
    f.db.run("UPDATE auth_sessions SET status='ROTATED',rotated_at=40 WHERE id='rotated' AND status='ACTIVE'");
    expect(() => f.insert('stale','old',2,40)).toThrow('game_session_rotation_stale');
    expect(f.rows()).toContainEqual(['new','ACTIVE']); f.db.close();
  });
  it('rejects suspended account issuance before revoking the existing game session', async () => {
    const f = await fixture(); f.db.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
    expect(() => f.insert('new')).toThrow('game_session_account_inactive');
    expect(f.rows()).toContainEqual(['old','ACTIVE']); f.db.close();
  });
  it('returns an authentication rejection when login replaces a refresh already being prepared', async () => {
    const f = await setup(false, true, null), refreshToken = 'test-only-refresh-token-'.repeat(3);
    f.sql.run("UPDATE auth_sessions SET refresh_token_hash=? WHERE id='s'", [await hashRefreshToken(refreshToken)]);
    f.env.DB.beforeBatch = () => {
      f.env.DB.beforeBatch = undefined;
      f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('new-login','a','new-hash','new-family',0,'ACTIVE',?,?)", [Date.now(), Date.now()+60000]);
    };
    const response = await app.request('/v1/auth/refresh', { method: 'POST',
      headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refreshToken }) }, f.env);
    expect(response.status).toBe(401);
    expect(f.scalar("SELECT COUNT(*) FROM auth_sessions WHERE status='ACTIVE'")).toBe(1);
    expect(f.scalar("SELECT id FROM auth_sessions WHERE status='ACTIVE'")).toBe('new-login');
    f.sql.close();
  });
  it('replays the committed same-key refresh when the other request used an earlier timestamp', async () => {
    const f = await setup(false, true, null), refreshToken = 'test-only-concurrent-refresh-token';
    f.sql.run("UPDATE auth_sessions SET refresh_token_hash=? WHERE id='s'", [await hashRefreshToken(refreshToken)]);
    f.adapter.beforeBatch = () => {
      f.adapter.beforeBatch = undefined;
      const earlier = Date.now() - 100;
      f.sql.run("UPDATE auth_sessions SET status='ROTATED',rotated_at=? WHERE id='s'", [earlier]);
      f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('first-refresh','a','first-refresh-hash','f',1,'ACTIVE',?,?)", [earlier, Date.now()+60000]);
      f.sql.run("INSERT INTO idempotency_records VALUES('first-refresh-key','a','same-refresh-key-20261001','auth_refresh','first-refresh',?)", [earlier]);
    };
    const response = await app.request('/v1/auth/refresh', { method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': 'same-refresh-key-20261001' },
      body: JSON.stringify({ refreshToken }) }, f.env);
    expect(response.status).toBe(200);
    expect(f.scalar("SELECT id FROM auth_sessions WHERE status='ACTIVE'")).toBe('first-refresh');
    expect(f.scalar("SELECT COUNT(*) FROM idempotency_records WHERE operation='auth_refresh'")).toBe(1);
    f.sql.close();
  });
});
