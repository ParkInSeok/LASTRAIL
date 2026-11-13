import { describe, expect, it } from 'vitest';
import app from '../../apps/game-api/src/index';
import { hashPassword } from '../../apps/game-api/src/password-auth';
import { setup } from '../helpers/departure-fixture';

const origin = 'https://dev.lastrail.app';
async function fixture() {
  const f = await setup(false, true, null);
  const password = await hashPassword('TestOnlyStrongPassword-123!');
  f.sql.run(`INSERT INTO auth_password_credentials(account_id,login_id_normalized,password_hash,password_hash_algorithm,
    password_hash_params_json,recovery_email_normalized,created_at,updated_at) VALUES('a','tester',?,?,?,?,0,0)`,
    [password.passwordHash, password.algorithm, password.paramsJson, 'tester@example.invalid']);
  const env = { ...f.env, WEB_ALLOWED_ORIGIN: origin, CLIENT_ADMISSION_MODE: 'ENFORCED' as const };
  const login = (passwordValue = 'TestOnlyStrongPassword-123!', requestOrigin = origin, targetEnv = env) => app.request(
    '/v1/web/sessions/password', { method: 'POST', headers: { Origin: requestOrigin, 'Content-Type': 'application/json' },
      body: JSON.stringify({ loginId: 'tester', password: passwordValue }) }, targetEnv);
  return { ...f, env, login };
}

describe('browser-only web session HTTP boundary', () => {
  it('fails closed without an allowed website origin or owner proof', async () => {
    const f = await fixture();
    expect((await f.login('TestOnlyStrongPassword-123!', origin, { ...f.env, WEB_ALLOWED_ORIGIN: undefined })).status).toBe(503);
    expect((await f.login('bad password')).status).toBe(401);
    expect((await f.login('TestOnlyStrongPassword-123!', 'https://evil.example')).status).toBe(403);
    expect(f.scalar('SELECT COUNT(*) FROM web_sessions')).toBe(0);
    const preflight = await app.request('/v1/web/sessions/password', { method: 'OPTIONS',
      headers: { Origin: origin, 'Access-Control-Request-Method': 'POST',
        'Access-Control-Request-Headers': 'content-type' } }, f.env);
    expect(preflight.status).toBe(204);
    expect(preflight.headers.get('Access-Control-Allow-Credentials')).toBe('true');
    expect((await app.request('/v1/web/sessions/password', { method: 'OPTIONS',
      headers: { Origin: 'https://evil.example' } }, f.env)).status).toBe(403);
  });

  it('issues a separate cookie, supports CSRF recovery and revokes logout without occupying game slots', async () => {
    const f = await fixture();
    const login = await f.login();
    expect(login.status).toBe(200);
    expect(login.headers.get('Access-Control-Allow-Origin')).toBe(origin);
    const setCookie = login.headers.get('Set-Cookie')!;
    expect(setCookie).toContain('Secure; HttpOnly; SameSite=Lax');
    const cookie = setCookie.split(';')[0]!;
    const loginBody = await login.json() as { csrfToken: string };
    expect(loginBody.csrfToken).toHaveLength(43);
    expect(JSON.stringify(loginBody)).not.toContain('accessToken');
    expect(f.scalar("SELECT COUNT(*) FROM auth_sessions WHERE account_id='a'")).toBe(1);
    const headers = { Origin: origin, Cookie: cookie };
    expect((await app.request('/v1/web/sessions/me', { headers }, f.env)).status).toBe(200);
    const renew = await app.request('/v1/web/sessions/csrf', { method: 'POST', headers }, f.env);
    expect(renew.status).toBe(200);
    const csrf = (await renew.json() as { csrfToken: string }).csrfToken;
    expect(csrf).not.toBe(loginBody.csrfToken);
    expect((await app.request('/v1/web/sessions/logout', { method: 'POST', headers: { ...headers, 'X-CSRF-Token': loginBody.csrfToken } }, f.env)).status).toBe(403);
    const logout = await app.request('/v1/web/sessions/logout', { method: 'POST', headers: { ...headers, 'X-CSRF-Token': csrf } }, f.env);
    expect(logout.status).toBe(200);
    expect(logout.headers.get('Set-Cookie')).toContain('Max-Age=0');
    expect((await app.request('/v1/web/sessions/me', { headers }, f.env)).status).toBe(401);
    expect(f.scalar("SELECT COUNT(*) FROM auth_sessions WHERE account_id='a'")).toBe(1);
  });

  it('rechecks the owning account on every cookie read', async () => {
    const f = await fixture();
    const login = await f.login();
    const cookie = login.headers.get('Set-Cookie')!.split(';')[0]!;
    f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
    expect((await app.request('/v1/web/sessions/me', { headers: { Origin: origin, Cookie: cookie } }, f.env)).status).toBe(401);
    expect((await app.request('/v1/web/sessions/csrf', { method: 'POST', headers: { Origin: origin, Cookie: cookie } }, f.env)).status).toBe(401);
  });

  it('issues a browser cookie only for an already linked Google account and a web audience', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO auth_identities(id,account_id,provider,provider_subject,linked_at) VALUES('google-a','a','GOOGLE','subject-a',0)");
    const env = { ...f.env, WEB_GOOGLE_AUTH_AUDIENCES: 'web-client.apps.googleusercontent.com',
      GOOGLE_ID_TOKEN_VERIFIER: { verifyIdToken: async (_token: string, audiences: string[]) => {
        expect(audiences).toEqual(['web-client.apps.googleusercontent.com']);
        return { subject: 'subject-a' };
      } } };
    const google = (targetEnv = env) => app.request('/v1/web/sessions/google', { method: 'POST',
      headers: { Origin: origin, 'Content-Type': 'application/json' }, body: JSON.stringify({ idToken: 'test-id-token' }) }, targetEnv);
    const issued = await google();
    expect(issued.status).toBe(200);
    expect(issued.headers.get('Set-Cookie')).toContain('__Host-lr_web_session=');
    expect(f.scalar("SELECT COUNT(*) FROM auth_sessions WHERE account_id='a'")).toBe(1);
    expect((await google({ ...env, WEB_GOOGLE_AUTH_AUDIENCES: undefined })).status).toBe(503);
    f.sql.run("DELETE FROM auth_identities WHERE id='google-a'");
    expect((await google()).status).toBe(401);
    f.sql.run("INSERT INTO auth_identities(id,account_id,provider,provider_subject,linked_at) VALUES('google-a','a','GOOGLE','subject-a',0)");
    f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
    expect((await google()).status).toBe(401);
  });

  it('keeps two web sessions independent through a game password login and one web logout', async () => {
    const f = await fixture();
    const first = await f.login();
    const second = await f.login();
    expect(first.status).toBe(200);
    expect(second.status).toBe(200);
    const firstCookie = first.headers.get('Set-Cookie')!.split(';')[0]!;
    const secondCookie = second.headers.get('Set-Cookie')!.split(';')[0]!;
    expect(firstCookie).not.toBe(secondCookie);
    const gameLogin = await app.request('/v1/auth/password/login', { method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ loginId: 'tester', password: 'TestOnlyStrongPassword-123!' }) },
      { ...f.env, CLIENT_ADMISSION_MODE: undefined });
    expect(gameLogin.status).toBe(200);
    expect(f.scalar('SELECT COUNT(*) FROM web_sessions WHERE revoked_at IS NULL')).toBe(2);
    const gameSessionCount = f.scalar("SELECT COUNT(*) FROM auth_sessions WHERE account_id='a' AND status='ACTIVE'");
    expect(gameSessionCount).toBe(1);
    const headers = { Origin: origin, Cookie: firstCookie };
    const firstCsrf = (await first.json() as { csrfToken: string }).csrfToken;
    expect((await app.request('/v1/web/sessions/logout', { method: 'POST',
      headers: { ...headers, 'X-CSRF-Token': firstCsrf } }, f.env)).status).toBe(200);
    expect((await app.request('/v1/web/sessions/me', { headers }, f.env)).status).toBe(401);
    expect((await app.request('/v1/web/sessions/me', { headers: { Origin: origin, Cookie: secondCookie } }, f.env)).status).toBe(200);
    expect(f.scalar("SELECT COUNT(*) FROM auth_sessions WHERE account_id='a' AND status='ACTIVE'")).toBe(gameSessionCount);
  });
});
