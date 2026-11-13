import { describe, expect, it } from 'vitest';
import app from '../../apps/game-api/src/index';
import { hashPassword } from '../../apps/game-api/src/password-auth';
import { setup } from '../helpers/departure-fixture';

const origin = 'https://dev.lastrail.app';

describe('web private support reads', () => {
  it('uses an owned cookie and exact DEV tester version without exposing another account', async () => {
    const f = await setup(false, true, null);
    const hash = await hashPassword('TestOnlyStrongPassword-123!');
    f.sql.run(`INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,
      password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES('a','tester',?,?,?,?,0,0)`,
      [hash.passwordHash, hash.algorithm, hash.paramsJson, 'tester@example.invalid']);
    f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)", [Date.now() + 60000]);
    f.sql.run(`INSERT INTO support_tickets
      (id,account_id,category,status,body_text,body_preview,body_length,server_received_at,idempotency_key,request_hash,created_at,updated_at)
      VALUES ('t-a','a','BUG_OTHER','OPEN','Account A private body','Account A preview',22,100,'key-a','hash-a',100,100),
             ('t-b','b','BUG_OTHER','OPEN','Account B private body','Account B preview',22,101,'key-b','hash-b',101,101)`);
    const env = { ...f.env, WEB_ALLOWED_ORIGIN: origin, CLIENT_ADMISSION_MODE: 'ENFORCED' as const };
    const login = await app.request('/v1/web/sessions/password', { method: 'POST',
      headers: { Origin: origin, 'Content-Type': 'application/json' },
      body: JSON.stringify({ loginId: 'tester', password: 'TestOnlyStrongPassword-123!' }) }, env);
    expect(login.status).toBe(200);
    const cookie = login.headers.get('Set-Cookie')!.split(';')[0]!;
    const headers = { Origin: origin, Cookie: cookie, 'X-LastRail-App-Version-Code': '123' };
    const list = await app.request('/v1/web/support/tickets', { headers }, env);
    expect(list.status).toBe(200);
    expect(list.headers.get('Access-Control-Allow-Origin')).toBe(origin);
    expect(list.headers.get('Cache-Control')).toBe('no-store');
    expect((await list.json() as { items: { ticketId: string }[] }).items.map(item => item.ticketId)).toEqual(['t-a']);
    expect((await app.request('/v1/web/support/tickets/t-a', { headers }, env)).status).toBe(200);
    expect((await app.request('/v1/web/support/tickets/t-b', { headers }, env)).status).toBe(404);
    expect((await app.request('/v1/web/support/tickets', { headers: { Origin: origin } }, env)).status).toBe(401);
    expect((await app.request('/v1/web/support/tickets', { headers: { ...headers, Origin: 'https://evil.example' } }, env)).status).toBe(403);
    expect((await app.request('/v1/web/support/tickets', { headers: { ...headers, 'X-LastRail-App-Version-Code': '124' } }, env)).status).toBe(403);
    expect((await app.request('/v1/web/support/tickets', { headers }, { ...env, WEB_ALLOWED_ORIGIN: undefined })).status).toBe(503);
    f.sql.run("UPDATE account_tester_entitlements SET revoked_at=1 WHERE account_id='a'");
    expect((await app.request('/v1/web/support/tickets', { headers }, env)).status).toBe(403);
  });

  it('requires CSRF for web submission and replays the existing idempotent ticket result', async () => {
    const f = await setup(false, true, null);
    const hash = await hashPassword('TestOnlyStrongPassword-123!');
    f.sql.run(`INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,
      password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES('a','tester',?,?,?,?,0,0)`,
      [hash.passwordHash, hash.algorithm, hash.paramsJson, 'tester@example.invalid']);
    f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'test',1,0,0)", [Date.now() + 60000]);
    const env = { ...f.env, WEB_ALLOWED_ORIGIN: origin, CLIENT_ADMISSION_MODE: 'ENFORCED' as const };
    const login = await app.request('/v1/web/sessions/password', { method: 'POST',
      headers: { Origin: origin, 'Content-Type': 'application/json' },
      body: JSON.stringify({ loginId: 'tester', password: 'TestOnlyStrongPassword-123!' }) }, env);
    const cookie = login.headers.get('Set-Cookie')!.split(';')[0]!;
    const csrfToken = (await login.json() as { csrfToken: string }).csrfToken;
    const headers = { Origin: origin, Cookie: cookie, 'X-LastRail-App-Version-Code': '123',
      'Idempotency-Key': 'web-ticket-request-0001', 'Content-Type': 'application/json' };
    const body = JSON.stringify({ category: 'BUG_OTHER', body: 'A private web support ticket.' });
    expect((await app.request('/v1/web/support/tickets', { method: 'POST', headers, body }, env)).status).toBe(403);
    const accepted = await app.request('/v1/web/support/tickets', { method: 'POST',
      headers: { ...headers, 'X-CSRF-Token': csrfToken }, body }, env);
    expect(accepted.status).toBe(200);
    expect(accepted.headers.get('Cache-Control')).toBe('no-store');
    const result = await accepted.json() as { ticketId: string };
    expect(result.ticketId).toBeTruthy();
    const replay = await app.request('/v1/web/support/tickets', { method: 'POST',
      headers: { ...headers, 'X-CSRF-Token': csrfToken }, body }, env);
    expect(replay.status).toBe(200);
    expect((await replay.json() as { ticketId: string }).ticketId).toBe(result.ticketId);
    expect((await app.request('/v1/web/support/tickets', { method: 'POST',
      headers: { ...headers, 'X-CSRF-Token': csrfToken },
      body: JSON.stringify({ category: 'BUG_OTHER', body: 'A different private support ticket.' }) }, env)).status).toBe(409);
    expect((await app.request('/v1/web/support/tickets', { method: 'POST',
      headers: { ...headers, 'Idempotency-Key': 'short', 'X-CSRF-Token': csrfToken }, body }, env)).status).toBe(400);
    const receipt = await app.request('/v1/web/support/tickets/receipt', { headers: {
      Origin: origin, Cookie: cookie, 'X-LastRail-App-Version-Code': '123', 'Idempotency-Key': 'web-ticket-request-0001' } }, env);
    expect(receipt.status).toBe(200);
    expect((await receipt.json() as { ticketId: string }).ticketId).toBe(result.ticketId);
    const preflight = await app.request('/v1/web/support/tickets', { method: 'OPTIONS', headers: {
      Origin: origin, 'Access-Control-Request-Method': 'POST',
      'Access-Control-Request-Headers': 'x-csrf-token,idempotency-key,content-type,x-lastrail-app-version-code' } }, env);
    expect(preflight.status).toBe(204);
    expect(preflight.headers.get('Access-Control-Allow-Headers')).toContain('X-CSRF-Token');
    expect(f.sql.exec('SELECT id FROM support_tickets').length).toBe(1);
  });
});
