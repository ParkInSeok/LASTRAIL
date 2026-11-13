import type { Context, Hono } from 'hono';
import { SupportTicketService, type SupportDatabase } from '../../../packages/common/src/support-ticket-service';
import { readBoundedBody, type ActivityAuthResult } from './activity-routes';
import { createWebSessionService } from './web-session';
import { createD1WebSessionStore } from './web-session-store';

type GameContext = Context<{ Bindings: Env }>;
const ticketIdPattern = /^[A-Za-z0-9_-]{1,128}$/u;
type Cursor = { serverReceivedAt: number; ticketId: string };

function parseCursor(value: string): Cursor | null {
  if (!/^[A-Za-z0-9_-]{1,256}$/u.test(value)) return null;
  try {
    const raw = JSON.parse(atob(value.replaceAll('-', '+').replaceAll('_', '/'))) as unknown;
    if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return null;
    const row = raw as Record<string, unknown>;
    if (Object.keys(row).sort().join(',') !== 'serverReceivedAt,ticketId' ||
        !Number.isSafeInteger(row.serverReceivedAt) || (row.serverReceivedAt as number) < 0 ||
        typeof row.ticketId !== 'string' || !ticketIdPattern.test(row.ticketId)) return null;
    return row as Cursor;
  } catch { return null; }
}

function encodeCursor(cursor: Cursor | null): string | null {
  return cursor ? btoa(JSON.stringify(cursor)).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/u, '') : null;
}

type Authenticate = (c: GameContext) => Promise<ActivityAuthResult>;
type WebEnv = Env & { WEB_ALLOWED_ORIGIN?: string };

export function isWebSupportRoute(method: string, path: string): boolean {
  if (path === '/v1/web/support/tickets') return ['GET', 'POST', 'OPTIONS'].includes(method);
  return (method === 'GET' || method === 'OPTIONS') && /^\/v1\/web\/support\/tickets\/[^/]+$/u.test(path);
}

export async function authenticateWebSupport(c: GameContext, mutation = false): Promise<ActivityAuthResult> {
  c.header('Vary', 'Origin');
  const origin = (c.env as WebEnv).WEB_ALLOWED_ORIGIN;
  if (!origin || (c.env.ENVIRONMENT !== 'DEV' && c.env.ENVIRONMENT !== 'PRD'))
    return { status: 503, errorCode: 'web_support_unavailable', message: 'Web support is unavailable.' };
  if (c.req.header('Origin') !== origin)
    return { status: 403, errorCode: 'web_origin_forbidden', message: 'Web origin is forbidden.' };
  c.header('Access-Control-Allow-Origin', origin);
  c.header('Access-Control-Allow-Credentials', 'true');
  let service: ReturnType<typeof createWebSessionService>;
  try { service = createWebSessionService({ store: createD1WebSessionStore(c.env.DB), origin }); }
  catch { return { status: 503, errorCode: 'web_support_unavailable', message: 'Web support is unavailable.' }; }
  const cookie = c.req.header('Cookie');
  const session = mutation
    ? await service.authorizeMutation(cookie, c.req.header('Origin'), c.req.header('X-CSRF-Token'))
    : await service.verify(cookie);
  if (mutation && !session)
    return { status: 403, errorCode: 'web_csrf_forbidden', message: 'Web mutation authorization is required.' };
  if (!session) return { status: 401, errorCode: 'web_session_required', message: 'Web session is required.' };
  if (c.env.ENVIRONMENT === 'DEV' &&
      !await service.verifyDevPublicRead(cookie, c.req.header('X-LastRail-App-Version-Code')))
    return { status: 403, errorCode: 'tester_forbidden', message: 'Tester access is required.' };
  return { accountId: session.accountId };
}

function registerReadRoutes(app: Hono<{ Bindings: Env }>, authenticate: Authenticate, basePath: string) {
  const handle = (detail: boolean) => async (c: GameContext) => {
    c.header('Cache-Control', 'no-store');
    try {
      const auth = await authenticate(c);
      if ('errorCode' in auth) return c.json({ error: auth.errorCode }, auth.status);
      if ((await readBoundedBody(c)) !== '') return c.json({ error: 'contract_violation' }, 400);
      const params = new URL(c.req.url).searchParams;
      const service = new SupportTicketService(c.env.DB as unknown as SupportDatabase);
      if (detail) {
        const id = c.req.param('ticketId');
        if (params.size || !id || !ticketIdPattern.test(id)) return c.json({ error: 'contract_violation' }, 400);
        const ticket = await service.getOwnTicket(auth.accountId, id);
        return ticket ? c.json({ contractVersion: 'private-support-ticket-v1', ticket }) : c.json({ error: 'support_ticket_not_found' }, 404);
      }
      if (params.size > 2 || [...params.keys()].some((key) => key !== 'limit' && key !== 'cursor') ||
          params.getAll('limit').length > 1 || params.getAll('cursor').length > 1) return c.json({ error: 'contract_violation' }, 400);
      const rawLimit = params.get('limit');
      const limit = rawLimit === null ? 20 : Number(rawLimit);
      if (rawLimit !== null && !/^[1-9][0-9]?$/u.test(rawLimit) || !Number.isInteger(limit) || limit > 50)
        return c.json({ error: 'contract_violation' }, 400);
      const rawCursor = params.get('cursor');
      const before = rawCursor === null ? undefined : parseCursor(rawCursor);
      if (rawCursor !== null && !before) return c.json({ error: 'contract_violation' }, 400);
      const result = await service.listOwnTickets(auth.accountId, limit, before ?? undefined);
      return c.json({ contractVersion: 'private-support-list-v1', items: result.items, nextCursor: encodeCursor(result.nextCursor) });
    } catch {
      return c.json({ error: 'support_ticket_unavailable' }, 503);
    }
  };
  app.get(basePath, handle(false));
  app.get(`${basePath}/:ticketId`, handle(true));
}

export function registerSupportPrivateReadRoutes(app: Hono<{ Bindings: Env }>, authenticate: Authenticate) {
  registerReadRoutes(app, authenticate, '/v1/support/tickets');
  registerReadRoutes(app, (c) => authenticateWebSupport(c), '/v1/web/support/tickets');
  const preflight = (c: GameContext) => {
    c.header('Cache-Control', 'no-store');
    c.header('Vary', 'Origin');
    const origin = (c.env as WebEnv).WEB_ALLOWED_ORIGIN;
    if (!origin) return c.json({ error: 'web_support_unavailable' }, 503);
    if (c.req.header('Origin') !== origin) return c.json({ error: 'web_origin_forbidden' }, 403);
    const method = c.req.header('Access-Control-Request-Method');
    if (method !== 'GET' && !(method === 'POST' && new URL(c.req.url).pathname === '/v1/web/support/tickets'))
      return c.json({ error: 'contract_violation' }, 400);
    c.header('Access-Control-Allow-Origin', origin);
    c.header('Access-Control-Allow-Credentials', 'true');
    c.header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    c.header('Access-Control-Allow-Headers', 'Content-Type, Idempotency-Key, X-CSRF-Token, X-LastRail-App-Version-Code');
    c.header('Access-Control-Max-Age', '600');
    return c.body(null, 204);
  };
  app.options('/v1/web/support/tickets', preflight);
  app.options('/v1/web/support/tickets/:ticketId', preflight);
}
