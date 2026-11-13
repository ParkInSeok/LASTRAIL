import { describe, expect, it } from 'vitest';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
import { setup } from '../helpers/departure-fixture';
import { readFileSync } from 'node:fs';

async function fixture() {
  const f = await setup(false, true, null);
  f.sql.run("INSERT INTO admin_principals(id,access_subject,role,status,created_at,updated_at) VALUES('operator','access|operator','OPERATOR','ACTIVE',0,0),('viewer','access|viewer','VIEWER','ACTIVE',0,0)");
  f.sql.run(`INSERT INTO support_tickets
    (id,account_id,category,status,body_text,body_preview,body_length,server_received_at,idempotency_key,request_hash,created_at,updated_at)
    VALUES ('ticket-1','a','BUG_OTHER','OPEN','Private original body','Private preview',21,100,'original-key','hash',100,100)`);
  const env = { ...f.env, ADMIN_AUTH_MODE: 'LOCAL_HEADER', ALLOWED_ADMIN_ORIGINS: 'http://localhost:5173',
    ADMIN_ACCESS_ISSUER: '', ADMIN_ACCESS_AUD: '', ADMIN_DELEGATED_ACCESS_ISSUER: '', ADMIN_DELEGATED_ACCESS_AUD: '' };
  const command = (key: string, body: object, subject = 'access|operator') => admin.request(
    '/v1/admin/support/tickets/ticket-1/processing', {
      method: 'POST', headers: { 'Cf-Access-Subject': subject, 'Content-Type': 'application/json', 'Idempotency-Key': key },
      body: JSON.stringify(body),
    }, env);
  const detail = (subject = 'access|operator') => admin.request('/v1/admin/support/tickets/ticket-1/processing',
    { headers: { 'Cf-Access-Subject': subject } }, env);
  return { ...f, env, command, detail };
}

describe('admin support processing HTTP boundary', () => {
  it('keeps historical reply-free close receipts replayable after the policy migration', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    f.sql.run('DROP TRIGGER support_ticket_transition_policy_guard');
    const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(JSON.stringify([
      'ticket-1',0,'CLOSED',{unchanged:true},null,null,
    ])));
    const hash = Array.from(new Uint8Array(digest), byte=>byte.toString(16).padStart(2,'0')).join('');
    f.sql.run(`INSERT INTO support_ticket_commands
      (id,ticket_id,principal_id,idempotency_key,request_hash,expected_revision,resulting_revision,next_status,created_at)
      VALUES('legacy','ticket-1','operator','legacy',?,0,1,'CLOSED',101)`, [hash]);
    f.sql.run("INSERT INTO support_ticket_processing VALUES('ticket-1',1,NULL,101)");
    f.sql.run("UPDATE support_tickets SET status='CLOSED',updated_at=101 WHERE id='ticket-1'");
    f.sql.run(readFileSync('packages/db/migrations/0097_support_processing_policy.sql','utf8'));
    const replay = await f.command('legacy', {expectedRevision:0,status:'CLOSED'});
    expect(replay.status).toBe(200);
    expect(replay.headers.get('X-Support-Processing-Policy')).toBe('support-processing-flow-v2-20261001');
    expect(await replay.json()).toEqual({contractVersion:'support-ticket-process-v1',revision:1});
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(1);
    expect(f.scalar('SELECT public_reply_text FROM support_ticket_commands')).toBeNull();
    expect(() => f.sql.run(`INSERT INTO support_ticket_commands
      (id,ticket_id,principal_id,idempotency_key,request_hash,expected_revision,resulting_revision,next_status,created_at)
      VALUES('reopen','ticket-1','operator','reopen','hash',1,2,'OPEN',102)`)).toThrow('support_closed_reopen_forbidden');
  });

  it('accepts only one concurrent close/return and preserves its final answer or private reason', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    expect((await f.command('review', {expectedRevision:0,status:'IN_REVIEW'})).status).toBe(200);
    const results = await Promise.all([
      f.command('close', {expectedRevision:1,status:'CLOSED',publicReply:'Final explanation'}),
      f.command('return', {expectedRevision:1,status:'OPEN',internalNote:'Reclassification reason'}),
    ]);
    expect(results.map(response=>response.status).sort()).toEqual([200,409]);
    expect(f.scalar('SELECT revision FROM support_ticket_processing')).toBe(2);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(2);
    const status = f.scalar("SELECT status FROM support_tickets WHERE id='ticket-1'");
    expect(f.scalar('SELECT next_status FROM support_ticket_commands WHERE resulting_revision=2')).toBe(status);
    expect(f.scalar(status==='CLOSED'
      ? 'SELECT public_reply_text FROM support_ticket_commands WHERE resulting_revision=2'
      : 'SELECT internal_note_text FROM support_ticket_commands WHERE resulting_revision=2')).toBe(
      status==='CLOSED' ? 'Final explanation' : 'Reclassification reason');
  });

  it('requires a final reply in this close command, even after an earlier public answer', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    expect((await f.command('missing-final', { expectedRevision: 0, status: 'CLOSED' })).status).toBe(400);
    expect((await f.command('middle', { expectedRevision: 0, status: 'IN_REVIEW', publicReply: 'Still investigating.' })).status).toBe(200);
    expect((await f.command('old-reply-is-not-final', { expectedRevision: 1, status: 'CLOSED', internalNote: 'Internal conclusion' })).status).toBe(400);
    const final = { expectedRevision: 1, status: 'CLOSED', publicReply: 'Final explanation.', internalNote: 'Private evidence' };
    const response = await f.command('final', final);
    expect(response.status).toBe(200);
    expect(await (await f.command('final', final)).json()).toEqual(await response.json());
    expect((await f.command('closed-note', { expectedRevision: 2, status: 'CLOSED', internalNote: 'Later private note' })).status).toBe(200);
    expect((await f.command('correction', { expectedRevision: 3, status: 'CLOSED', publicReply: 'Corrected final explanation.' })).status).toBe(200);
    expect((await f.command('reopen-closed', { expectedRevision: 4, status: 'OPEN', internalNote: 'Reopen reason' })).status).toBe(400);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(4);
    const own = await game.request('/v1/support/tickets/ticket-1', { headers: { Authorization: f.headers.Authorization } }, f.env);
    const ownBody = JSON.stringify(await own.json());
    expect(ownBody).toContain('Corrected final explanation.');
    expect(ownBody).not.toContain('Private evidence');
    expect(ownBody).not.toContain('Later private note');
  });

  it('requires a private reason to return IN_REVIEW to OPEN', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    expect((await f.command('review', { expectedRevision: 0, status: 'IN_REVIEW' })).status).toBe(200);
    expect((await f.command('no-reason', { expectedRevision: 1, status: 'OPEN', publicReply: 'Public explanation' })).status).toBe(400);
    expect((await f.command('null-reason', { expectedRevision: 1, status: 'OPEN', internalNote: null })).status).toBe(400);
    expect((await f.command('reason', { expectedRevision: 1, status: 'OPEN', internalNote: 'Wrong category; reassess.' })).status).toBe(200);
    expect(f.scalar("SELECT status FROM support_tickets WHERE id='ticket-1'")).toBe('OPEN');
    expect(f.scalar('SELECT revision FROM support_ticket_processing')).toBe(2);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(2);
  });

  it('guards invalid transitions at storage and rolls a final reply back with a failed state write', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    const insert = (status: string, reply: string | null, note: string | null) => f.sql.run(`INSERT INTO support_ticket_commands
      (id,ticket_id,principal_id,idempotency_key,request_hash,expected_revision,resulting_revision,next_status,public_reply_text,internal_note_text,created_at)
      VALUES('raw','ticket-1','operator','raw','hash',0,1,?,?,?,101)`, [status,reply,note]);
    expect(() => insert('CLOSED', null, 'Internal note')).toThrow('support_final_reply_required');
    expect(() => insert('CLOSED', ' \t\n ', null)).toThrow('support_final_reply_required');
    for (const whitespace of ['\u00a0', '\u3000', '\ufeff', '\u2000\u2028\u202f']) {
      expect(() => insert('CLOSED', whitespace, null)).toThrow('support_final_reply_required');
    }
    f.sql.run("UPDATE support_tickets SET status='IN_REVIEW' WHERE id='ticket-1'");
    expect(() => insert('OPEN', 'Public explanation', null)).toThrow('support_return_reason_required');
    for (const whitespace of ['\u00a0', '\u3000', '\ufeff', '\u2000\u2028\u202f']) {
      expect(() => insert('OPEN', null, whitespace)).toThrow('support_return_reason_required');
    }
    f.sql.run("CREATE TRIGGER fail_support_status BEFORE UPDATE ON support_tickets BEGIN SELECT RAISE(ABORT,'synthetic state storage failure'); END");
    expect((await f.command('atomic-close', { expectedRevision: 0, status: 'CLOSED', publicReply: 'Final answer' })).status).toBe(503);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(0);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_processing')).toBe(0);
    expect(f.scalar("SELECT status FROM support_tickets WHERE id='ticket-1'")).toBe('IN_REVIEW');
  });

  it('requires a separate active grant and replays the exact response after a lost answer', async () => {
    const f = await fixture();
    const body = { expectedRevision: 0, status: 'IN_REVIEW', publicReply: 'We are reviewing this.', internalNote: 'Private investigation.' };
    expect((await f.command('answer-1', body)).status).toBe(403);
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    expect((await f.command('answer-1', body, 'access|viewer')).status).toBe(403);
    const first = await f.command('answer-1', body);
    expect(first.status).toBe(200);
    expect(first.headers.get('Cache-Control')).toBe('no-store');
    const firstBody = await first.json();
    expect(firstBody).toMatchObject({ contractVersion: 'support-ticket-process-v1', revision: 1 });
    expect(await (await f.command('answer-1', body)).json()).toEqual(firstBody);
    expect((await f.command('answer-1', { ...body, publicReply: 'Changed' })).status).toBe(409);
    const operator = await f.detail();
    expect(await operator.json()).toMatchObject({ ticket: { ticketId: 'ticket-1', history: [{ internalNote: 'Private investigation.' }] } });
    const player = await game.request('/v1/support/tickets/ticket-1', { headers: { Authorization: f.headers.Authorization } }, f.env);
    expect(JSON.stringify(await player.json())).not.toContain('Private investigation.');
    f.sql.run("DELETE FROM support_ticket_processor_grants WHERE principal_id='operator'");
    expect((await f.command('answer-2', { ...body, expectedRevision: 1 })).status).toBe(403);
    expect((await f.detail()).status).toBe(403);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(1);
  });

  it('rejects malformed commands and revision races without creating an answer', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    expect((await f.command('bad', { expectedRevision: 0, status: 'IN_REVIEW', extra: true })).status).toBe(400);
    expect((await f.command('bad', { expectedRevision: -1, status: 'IN_REVIEW' })).status).toBe(400);
    expect((await f.command('oversized', { expectedRevision: 0, status: 'IN_REVIEW', internalNote: 'x'.repeat(18001) })).status).toBe(400);
    expect((await f.command('good', { expectedRevision: 0, status: 'IN_REVIEW', publicReply: 'Answer' })).status).toBe(200);
    expect((await f.command('stale', { expectedRevision: 0, status: 'CLOSED' })).status).toBe(409);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(1);
  });

  it('blocks a grant revoked between preflight and the atomic command batch', async () => {
    const f = await fixture();
    f.sql.run("INSERT INTO support_ticket_processor_grants VALUES('operator','SUPPORT_TICKET_PROCESS',0)");
    f.adapter.beforeBatch = () => {
      f.adapter.beforeBatch = undefined;
      f.sql.run("DELETE FROM support_ticket_processor_grants WHERE principal_id='operator'");
    };
    expect((await f.command('race', { expectedRevision: 0, status: 'IN_REVIEW', publicReply: 'Answer' })).status).toBe(403);
    expect(f.scalar('SELECT COUNT(*) FROM support_ticket_commands')).toBe(0);
    expect(f.scalar("SELECT status FROM support_tickets WHERE id='ticket-1'")).toBe('OPEN');
  });
});
