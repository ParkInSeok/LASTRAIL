import type { Context, Hono } from 'hono';
import { z } from 'zod';
import { ActivityError, activityId } from './activity-policy';
import { readBoundedBody, type ActivityAuthResult } from './activity-routes';
import { OrdinaryCombatLifecycle } from './combat-ordinary-combat-lifecycle';

/** Initializes an existing owned START, never authorizes admission or replaces live checkpoint state. */
export function registerOrdinaryInitializationRoutes(app: Hono<{ Bindings: Env }>,
  authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  app.post('/v1/runs/:runId/ordinary-initialization', async c => {
    c.header('Cache-Control', 'no-store');
    const auth = await authenticate(c);
    if ('errorCode' in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    const runId = c.req.param('runId');
    const key = z.string().regex(/^[\x21-\x7e]{1,128}$/u).safeParse(c.req.header('Idempotency-Key'));
    if (!activityId.safeParse(runId).success || !key.success || new URL(c.req.url).search)
      return c.json({ accepted: false, errorCode: 'contract_violation' }, 400);
    try { z.strictObject({}).parse(JSON.parse(await readBoundedBody(c, 256))); }
    catch { return c.json({ accepted: false, errorCode: 'contract_violation' }, 400); }
    try {
      const initializationReceipt = await new OrdinaryCombatLifecycle(c.env.DB).activate(auth.accountId, runId,
        { requestId: key.data, source: 'ORIGINAL_START' });
      // This immutable receipt is initial HP/stats, not a current-state recovery response.
      return c.json({ accepted: true, initializationReceipt, runtimeExecutionVerified: false });
    } catch (error) {
      if (error instanceof ActivityError) {
        if (error.code === 'ordinary_stat_run_not_found') return c.json({ accepted: false, errorCode: 'run_not_found' }, 404);
        return c.json({ accepted: false, errorCode: error.code }, error.status);
      }
      return c.json({ accepted: false, errorCode: 'ordinary_combat_initialization_outcome_unknown' }, 503);
    }
  });
}
