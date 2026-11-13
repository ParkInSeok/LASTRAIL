import { z } from 'zod';
import type { Context, Hono } from 'hono';
import { SupportTicketService, SUPPORT_PROCESSING_POLICY_VERSION, type ProcessSupportTicket, type SupportDatabase } from '../../../packages/common/src/support-ticket-service';
import { readBoundedBody } from '../../game-api/src/activity-routes';
import { ActivityError } from '../../game-api/src/activity-policy';

type Ctx = Context<{ Bindings: Env }>;
type Principal = { id: string; role: 'OWNER' | 'OPERATOR' | 'VIEWER' };
const ticketId = /^[A-Za-z0-9_-]{1,128}$/u;
const command = z.strictObject({
  expectedRevision: z.number().int().nonnegative().safe(),
  status: z.enum(['OPEN', 'IN_REVIEW', 'CLOSED']),
  assigneePrincipalId: z.string().regex(ticketId).nullable().optional(),
  publicReply: z.string().trim().min(1).max(2000).nullable().optional(),
  internalNote: z.string().trim().min(1).max(2000).nullable().optional(),
});

export function registerAdminSupportProcessingRoutes(
  app: Hono<{ Bindings: Env }>, authenticate: (c: Ctx) => Promise<Principal | Response>,
) {
  const handle = (write: boolean) => async (c: Ctx) => {
    c.header('Cache-Control', 'no-store');
    c.header('X-Support-Processing-Policy', SUPPORT_PROCESSING_POLICY_VERSION);
    const principal = await authenticate(c);
    if (principal instanceof Response) return principal;
    try {
      const service = new SupportTicketService(c.env.DB as unknown as SupportDatabase);
      if (!await service.canProcess(principal.id)) return c.json({ error: 'ADMIN_PERMISSION_DENIED' }, 403);
      const id = c.req.param('ticketId');
      if (!id || !ticketId.test(id) || new URL(c.req.url).searchParams.size) return c.json({ error: 'contract_violation' }, 400);
      const raw = await readBoundedBody(c);
      if (!write) {
        if (raw !== '') return c.json({ error: 'contract_violation' }, 400);
        const ticket = await service.getOperatorTicket(principal.id, id);
        return ticket ? c.json({ contractVersion: 'admin-support-processing-v1', ticket }) : c.json({ error: 'support_ticket_not_found' }, 404);
      }
      const key = c.req.header('Idempotency-Key') ?? '';
      if (!/^[\x21-\x7e]{1,128}$/u.test(key)) return c.json({ error: 'idempotency_key_required' }, 400);
      let body: unknown;
      try { body = JSON.parse(raw); } catch { return c.json({ error: 'contract_violation' }, 400); }
      const parsed = command.safeParse(body);
      if (!parsed.success) return c.json({ error: 'contract_violation' }, 400);
      const input: ProcessSupportTicket = { ticketId: id, principalId: principal.id, idempotencyKey: key,
        expectedRevision: parsed.data.expectedRevision, status: parsed.data.status, now: Date.now() };
      if (parsed.data.assigneePrincipalId !== undefined) input.assigneePrincipalId = parsed.data.assigneePrincipalId;
      if (parsed.data.publicReply !== undefined) input.publicReply = parsed.data.publicReply;
      if (parsed.data.internalNote !== undefined) input.internalNote = parsed.data.internalNote;
      const result = await service.process(input);
      if (result.kind === 'forbidden') return c.json({ error: 'ADMIN_PERMISSION_DENIED' }, 403);
      if (result.kind === 'not_found') return c.json({ error: 'support_ticket_not_found' }, 404);
      if (result.kind === 'invalid') return c.json({ error: 'contract_violation' }, 400);
      if (result.kind === 'idempotency_conflict') return c.json({ error: 'idempotency_conflict' }, 409);
      if (result.kind === 'revision_conflict') return c.json({ error: 'revision_conflict', revision: result.revision }, 409);
      // Applied and replayed commands return the same stable result, even after later changes.
      return c.json({ contractVersion: 'support-ticket-process-v1', revision: result.revision });
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ error: error.code }, error.status);
      return c.json({ error: 'support_processing_unavailable' }, 503);
    }
  };
  app.get('/v1/admin/support/tickets/:ticketId/processing', handle(false));
  app.post('/v1/admin/support/tickets/:ticketId/processing', handle(true));
}
