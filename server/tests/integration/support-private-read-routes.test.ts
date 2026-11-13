import { describe, expect, it } from 'vitest';
import app from '../../apps/game-api/src/index';
import { SupportTicketService, type SupportDatabase } from '../../packages/common/src/support-ticket-service';
import { setup } from '../helpers/departure-fixture';

describe('private support ticket reads', () => {
  it('returns only owned tickets and never exposes internal processing notes', async () => {
    const f = await setup(false, true, null);
    f.sql.run(`INSERT INTO support_tickets
      (id,account_id,category,status,body_text,body_preview,body_length,server_received_at,idempotency_key,request_hash,created_at,updated_at)
      VALUES ('t-a','a','BUG_OTHER','OPEN','Account A private body','Account A preview',22,100,'key-a','hash-a',100,100),
             ('t-old','a','BUG_OTHER','OPEN','Older private body','Older preview',18,99,'key-old','hash-old',99,99),
             ('t-b','b','BUG_OTHER','OPEN','Account B private body','Account B preview',22,101,'key-b','hash-b',101,101)`);
    f.sql.run("INSERT INTO admin_principals(id,access_subject,role,status,created_at,updated_at) VALUES('p1','p1','OPERATOR','ACTIVE',0,0)");
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('p1','SUPPORT_TICKET_PROCESS',0)");
    const service = new SupportTicketService(f.env.DB as unknown as SupportDatabase);
    expect(await service.process({ ticketId: 't-a', principalId: 'p1', idempotencyKey: 'answer',
      expectedRevision: 0, status: 'IN_REVIEW', publicReply: 'Public answer', internalNote: 'Secret note', now: 101 }))
      .toMatchObject({ kind: 'applied' });
    const headers = { Authorization: f.headers.Authorization };
    const list = await app.request('/v1/support/tickets?limit=1', { headers }, f.env);
    expect(list.status).toBe(200);
    expect(list.headers.get('Cache-Control')).toBe('no-store');
    const body = await list.json() as { items: { ticketId: string }[]; nextCursor: string };
    expect(body.items.map((item) => item.ticketId)).toEqual(['t-a']);
    expect(body.nextCursor).toBeTruthy();
    const next = await app.request(`/v1/support/tickets?limit=1&cursor=${encodeURIComponent(body.nextCursor)}`, { headers }, f.env);
    expect((await next.json() as { items: { ticketId: string }[] }).items.map((item) => item.ticketId)).toEqual(['t-old']);
    expect(JSON.stringify(body)).not.toContain('Account B private body');
    expect(JSON.stringify(body)).not.toContain('Secret note');
    const own = await app.request('/v1/support/tickets/t-a', { headers }, f.env);
    const ownBody = await own.json();
    expect(ownBody).toMatchObject({ ticket: { ticketId: 't-a', body: 'Account A private body', publicReply: 'Public answer' } });
    expect(JSON.stringify(ownBody)).not.toContain('Secret note');
    expect((await app.request('/v1/support/tickets/t-b', { headers }, f.env)).status).toBe(404);
    expect((await app.request('/v1/support/tickets', {}, f.env)).status).toBe(401);
  });

  it('rejects malformed paging and query without returning an empty success', async () => {
    const f = await setup(false, true, null);
    const headers = { Authorization: f.headers.Authorization };
    expect((await app.request('/v1/support/tickets?limit=0', { headers }, f.env)).status).toBe(400);
    expect((await app.request('/v1/support/tickets?cursor=invalid', { headers }, f.env)).status).toBe(400);
    expect((await app.request('/v1/support/tickets/t-a?unused=1', { headers }, f.env)).status).toBe(400);
    expect((await app.request('/v1/support/tickets?unused=1', { headers }, f.env)).status).toBe(400);
  });
});
