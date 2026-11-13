import type { Context, Hono } from 'hono';
import {HTTPException} from 'hono/http-exception';
import { readBoundedBody } from './activity-routes';
import { ActivityError } from './activity-policy';
import { normalizeLoginId, validatePassword, verifyPasswordHash } from './password-auth';
import { createGoogleIdTokenVerifier, GoogleIdTokenVerificationError, type GoogleIdTokenVerifier } from './auth';
import { createWebSessionService, WEB_SESSION_CONTRACT_VERSION } from './web-session';
import { createD1WebSessionStore } from './web-session-store';

type Ctx = Context<{ Bindings: Env }>;
type WebEnv = Env & { WEB_ALLOWED_ORIGIN?: string; WEB_GOOGLE_AUTH_AUDIENCES?: string;
  GOOGLE_ID_TOKEN_VERIFIER?: GoogleIdTokenVerifier };
const paths = new Set(['/v1/web/sessions/password', '/v1/web/sessions/google', '/v1/web/sessions/me', '/v1/web/sessions/csrf', '/v1/web/sessions/logout']);

export function isWebSessionRoute(method: string, path: string): boolean {
  return paths.has(path) && ['GET', 'POST', 'OPTIONS'].includes(method);
}

export function registerWebSessionRoutes(app: Hono<{ Bindings: Env }>) {
  const handle = (action: 'password' | 'google' | 'me' | 'csrf' | 'logout' | 'options') => async (c: Ctx) => {
    c.header('Cache-Control', 'no-store');
    c.header('Vary', 'Origin');
    const origin = (c.env as WebEnv).WEB_ALLOWED_ORIGIN;
    if (!origin) return c.json({ error: 'web_origin_unavailable' }, 503);
    const requestOrigin = c.req.header('Origin');
    if (requestOrigin !== origin) return c.json({ error: 'web_origin_forbidden' }, 403);
    try {
      const store = createD1WebSessionStore(c.env.DB,c.env.ENVIRONMENT);
      const service = createWebSessionService({ store, origin, resolveOwnedAccount: async (proof) => {
        if (!proof || typeof proof !== 'object') return null;
        const input = proof as Record<string, unknown>;
        if (action === 'google') {
          const audiences = ((c.env as WebEnv).WEB_GOOGLE_AUTH_AUDIENCES ?? '').split(',')
            .map(value => value.trim()).filter(Boolean);
          if (audiences.length === 0) throw new Error('web_google_unavailable');
          const verifier = (c.env as WebEnv).GOOGLE_ID_TOKEN_VERIFIER ?? createGoogleIdTokenVerifier();
          let subject: string;
          try {
            const claims = await verifier.verifyIdToken(input.idToken as string, audiences);
            subject = claims.subject;
          } catch (error) {
            if (error instanceof GoogleIdTokenVerificationError && error.code.startsWith('google_jwks_'))
              throw new Error('web_google_unavailable');
            return null;
          }
          if (typeof subject !== 'string' || !subject.trim()) return null;
          const row = await c.env.DB.prepare(`SELECT identities.account_id,accounts.status AS account_status
            FROM auth_identities identities JOIN accounts ON accounts.id=identities.account_id
            WHERE identities.provider='GOOGLE' AND identities.provider_subject=?`).bind(subject)
            .first<{ account_id: string; account_status: string }>();
          return row?.account_status === 'ACTIVE' ? { accountId: row.account_id, identity: 'GOOGLE' as const } : null;
        }
        const login = normalizeLoginId(input.loginId);
        const password = validatePassword(input.password);
        if (!login || !password) return null;
        const row = await c.env.DB.prepare(`SELECT credentials.account_id,credentials.password_hash,
          credentials.password_hash_algorithm,accounts.status AS account_status
          FROM auth_password_credentials credentials JOIN accounts ON accounts.id=credentials.account_id
          WHERE credentials.login_id_normalized=?`).bind(login.normalized)
          .first<{ account_id: string; password_hash: string; password_hash_algorithm: string; account_status: string }>();
        if (!row || row.account_status !== 'ACTIVE' || row.password_hash_algorithm !== 'PBKDF2_SHA256' ||
            !await verifyPasswordHash(password, row.password_hash)) return null;
        return { accountId: row.account_id, identity: 'PASSWORD' as const };
      } });
      c.header('Access-Control-Allow-Origin', origin);
      c.header('Access-Control-Allow-Credentials', 'true');
      if (action === 'options') {
        c.header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
        c.header('Access-Control-Allow-Headers', 'Content-Type, X-CSRF-Token');
        c.header('Access-Control-Max-Age', '600');
        return c.body(null, 204);
      }
      const cookie = c.req.header('Cookie');
      if (action === 'password' || action === 'google') {
        const raw = await readBoundedBody(c);
        let proof: unknown;
        try { proof = JSON.parse(raw); } catch { return c.json({ error: 'contract_violation' }, 400); }
        if (!proof || typeof proof !== 'object' || Array.isArray(proof) ||
            Object.keys(proof).sort().join(',') !== (action === 'google' ? 'idToken' : 'loginId,password') ||
            (action === 'google' && (typeof (proof as { idToken?: unknown }).idToken !== 'string' ||
              !(proof as { idToken: string }).idToken.trim()))) return c.json({ error: 'contract_violation' }, 400);
        try {
          const issued = await service.issue(origin, proof);
          c.header('Set-Cookie', issued.setCookie);
          return c.json({ contractVersion: WEB_SESSION_CONTRACT_VERSION, csrfToken: issued.csrfToken,
            expiresAt: new Date(issued.expiresAt).toISOString() });
        } catch (error) {
          if (error instanceof Error && error.message === 'web_identity_unverified') return c.json({ error: 'web_login_invalid' }, 401);
          throw error;
        }
      }
      if ((await readBoundedBody(c)) !== '') return c.json({ error: 'contract_violation' }, 400);
      if (new URL(c.req.url).searchParams.size) return c.json({ error: 'contract_violation' }, 400);
      if (action === 'me') {
        const session = await service.verify(cookie);
        return session ? c.json({ contractVersion: WEB_SESSION_CONTRACT_VERSION, accountId: session.accountId,
          expiresAt: new Date(session.expiresAt).toISOString() }) : c.json({ error: 'web_session_required' }, 401);
      }
      if (action === 'csrf') {
        const csrfToken = await service.renewCsrf(cookie, origin);
        return csrfToken ? c.json({ contractVersion: WEB_SESSION_CONTRACT_VERSION, csrfToken }) : c.json({ error: 'web_session_required' }, 401);
      }
      if (!await service.logout(cookie, origin, c.req.header('X-CSRF-Token'))) return c.json({ error: 'web_csrf_forbidden' }, 403);
      c.header('Set-Cookie', service.clearCookie);
      return c.json({ contractVersion: WEB_SESSION_CONTRACT_VERSION, loggedOut: true });
    } catch (error) {
      if (error instanceof HTTPException) return error.getResponse();
      if (error instanceof ActivityError) return c.json({ error: error.code }, error.status);
      return c.json({ error: 'web_session_unavailable' }, 503);
    }
  };
  app.options('/v1/web/sessions/password', handle('options'));
  app.options('/v1/web/sessions/google', handle('options'));
  app.options('/v1/web/sessions/me', handle('options'));
  app.options('/v1/web/sessions/csrf', handle('options'));
  app.options('/v1/web/sessions/logout', handle('options'));
  app.post('/v1/web/sessions/password', handle('password'));
  app.post('/v1/web/sessions/google', handle('google'));
  app.get('/v1/web/sessions/me', handle('me'));
  app.post('/v1/web/sessions/csrf', handle('csrf'));
  app.post('/v1/web/sessions/logout', handle('logout'));
}
