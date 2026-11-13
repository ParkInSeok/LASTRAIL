/** Private support processing boundary. HTTP auth must resolve the account/principal first. */
export const SUPPORT_TICKET_PROCESS_PERMISSION = "SUPPORT_TICKET_PROCESS" as const;
export const SUPPORT_PROCESSING_POLICY_VERSION = "support-processing-flow-v2-20261001" as const;
export type SupportStatus = "OPEN" | "IN_REVIEW" | "CLOSED";
type SqlValue = string | number | null;

export interface SupportStatement {
  bind(...values: SqlValue[]): SupportStatement;
  first<T>(): Promise<T | null>;
  all<T>(): Promise<{ results: T[] }>;
  run(): Promise<unknown>;
}

export interface SupportDatabase {
  prepare(sql: string): SupportStatement;
  batch(statements: SupportStatement[]): Promise<unknown[]>;
}

export interface ProcessSupportTicket {
  ticketId: string;
  principalId: string;
  idempotencyKey: string;
  expectedRevision: number;
  status: SupportStatus;
  assigneePrincipalId?: string | null;
  publicReply?: string | null;
  internalNote?: string | null;
  now: number;
}

interface TicketRow {
  id: string;
  category: string;
  status: SupportStatus;
  body_text: string;
  body_preview: string;
  server_received_at: number;
  revision: number | null;
  public_reply_text: string | null;
}

interface CommandRow { request_hash: string; resulting_revision: number }
interface PrincipalRow { role: string; status: string; permission: string | null }

export type ProcessResult =
  | { kind: "applied" | "replay"; revision: number }
  | { kind: "forbidden" | "not_found" | "invalid" | "idempotency_conflict"; revision?: never }
  | { kind: "revision_conflict"; revision: number };

function ticketDto(row: TicketRow) {
  return {
    ticketId: row.id,
    category: row.category,
    status: row.status,
    bodyPreview: row.body_preview,
    serverReceivedAt: new Date(row.server_received_at).toISOString(),
    revision: row.revision ?? 0,
    publicReply: row.public_reply_text,
  };
}

const ownTicketColumns = `ticket.id, ticket.category, ticket.status, ticket.body_text, ticket.body_preview,
  ticket.server_received_at, processing.revision,
  (SELECT command.public_reply_text FROM support_ticket_commands command
   WHERE command.ticket_id = ticket.id AND command.public_reply_text IS NOT NULL
   ORDER BY command.resulting_revision DESC LIMIT 1) AS public_reply_text`;

export class SupportTicketService {
  constructor(private readonly db: SupportDatabase) {}

  async canProcess(principalId: string): Promise<boolean> {
    const principal = await this.db.prepare(`SELECT principal.role,principal.status,grant_row.permission
      FROM admin_principals principal
      LEFT JOIN support_ticket_processor_grants grant_row ON grant_row.principal_id = principal.id
      WHERE principal.id = ?`).bind(principalId).first<PrincipalRow>();
    return principal?.status === "ACTIVE" &&
      (principal.role === "OWNER" || principal.role === "OPERATOR") &&
      principal.permission === SUPPORT_TICKET_PROCESS_PERMISSION;
  }

  async listOwnTickets(accountId: string, limit = 50, before?: { serverReceivedAt: number; ticketId: string }) {
    if (!accountId || !Number.isSafeInteger(limit) || limit < 1 || limit > 50) throw new Error("invalid_support_read_scope");
    const rows = await this.db.prepare(`SELECT ${ownTicketColumns}
      FROM support_tickets ticket LEFT JOIN support_ticket_processing processing ON processing.ticket_id = ticket.id
      WHERE ticket.account_id = ? AND (? IS NULL OR ticket.server_received_at < ? OR
        (ticket.server_received_at = ? AND ticket.id < ?))
      ORDER BY ticket.server_received_at DESC, ticket.id DESC LIMIT ?`)
      .bind(accountId, before?.serverReceivedAt ?? null, before?.serverReceivedAt ?? null,
        before?.serverReceivedAt ?? null, before?.ticketId ?? null, limit + 1)
      .all<TicketRow>();
    const visible = rows.results.slice(0, limit);
    const last = visible.at(-1);
    return { items: visible.map(ticketDto), nextCursor: rows.results.length > limit && last
      ? { serverReceivedAt: last.server_received_at, ticketId: last.id } : null };
  }

  async getOwnTicket(accountId: string, ticketId: string) {
    if (!accountId || !ticketId) return null;
    const row = await this.db.prepare(`SELECT ${ownTicketColumns}
      FROM support_tickets ticket LEFT JOIN support_ticket_processing processing ON processing.ticket_id = ticket.id
      WHERE ticket.account_id = ? AND ticket.id = ?`)
      .bind(accountId, ticketId).first<TicketRow>();
    return row ? { ...ticketDto(row), body: row.body_text } : null;
  }

  async getOperatorTicket(principalId: string, ticketId: string) {
    if (!ticketId || !await this.canProcess(principalId)) return null;
    const row = await this.db.prepare(`SELECT ${ownTicketColumns},processing.assignee_principal_id
      FROM support_tickets ticket LEFT JOIN support_ticket_processing processing ON processing.ticket_id = ticket.id
      WHERE ticket.id = ?`).bind(ticketId).first<TicketRow & { assignee_principal_id: string | null }>();
    if (!row) return null;
    const history = await this.db.prepare(`SELECT principal_id,resulting_revision,next_status,
      public_reply_text,internal_note_text,created_at FROM support_ticket_commands
      WHERE ticket_id = ? ORDER BY resulting_revision DESC LIMIT 101`)
      .bind(ticketId).all<{ principal_id: string; resulting_revision: number; next_status: SupportStatus;
        public_reply_text: string | null; internal_note_text: string | null; created_at: number }>();
    return { ...ticketDto(row), body: row.body_text, assigneePrincipalId: row.assignee_principal_id,
      history: history.results.slice(0, 100).map((command) => ({ principalId: command.principal_id,
        revision: command.resulting_revision, status: command.next_status, publicReply: command.public_reply_text,
        internalNote: command.internal_note_text, createdAt: new Date(command.created_at).toISOString() })),
      historyTruncated: history.results.length > 100 };
  }

  async process(input: ProcessSupportTicket): Promise<ProcessResult> {
    if (!await this.canProcess(input.principalId)) return { kind: "forbidden" };
    const publicReply = input.publicReply?.trim() || null;
    const internalNote = input.internalNote?.trim() || null;
    if (!input.ticketId || !input.principalId || !input.idempotencyKey || input.idempotencyKey.length > 128 ||
      !Number.isSafeInteger(input.expectedRevision) || input.expectedRevision < 0 ||
      !Number.isSafeInteger(input.now) || input.now < 0 ||
      !["OPEN", "IN_REVIEW", "CLOSED"].includes(input.status) ||
      (publicReply?.length ?? 0) > 2000 || (internalNote?.length ?? 0) > 2000 ||
      (input.publicReply !== undefined && input.publicReply !== null && !publicReply) ||
      (input.internalNote !== undefined && input.internalNote !== null && !internalNote)) return { kind: "invalid" };

    const payload = JSON.stringify([input.ticketId, input.expectedRevision, input.status,
      input.assigneePrincipalId === undefined ? { unchanged: true } : input.assigneePrincipalId, publicReply, internalNote]);
    const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(payload));
    const hash = Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, "0")).join("");
    const existing = await this.db.prepare(`SELECT request_hash,resulting_revision FROM support_ticket_commands
      WHERE ticket_id = ? AND principal_id = ? AND idempotency_key = ?`)
      .bind(input.ticketId, input.principalId, input.idempotencyKey).first<CommandRow>();
    if (existing) return existing.request_hash === hash
      ? { kind: "replay", revision: existing.resulting_revision } : { kind: "idempotency_conflict" };

    const ticket = await this.db.prepare(`SELECT ticket.status AS status, processing.revision AS revision,
      processing.assignee_principal_id AS assignee_principal_id
      FROM support_tickets ticket LEFT JOIN support_ticket_processing processing ON processing.ticket_id = ticket.id
      WHERE ticket.id = ?`).bind(input.ticketId).first<{ status: SupportStatus; revision: number | null; assignee_principal_id: string | null }>();
    if (!ticket) return { kind: "not_found" };
    const revision = ticket.revision ?? 0;
    if (revision !== input.expectedRevision) return { kind: "revision_conflict", revision };
    if (ticket.status === "CLOSED" && input.status !== "CLOSED") return { kind: "invalid" };
    // Historical receipts replay above; only newly accepted transitions use this policy.
    if (ticket.status !== "CLOSED" && input.status === "CLOSED" && !publicReply) return { kind: "invalid" };
    if (ticket.status === "IN_REVIEW" && input.status === "OPEN" && !internalNote) return { kind: "invalid" };
    const assignee = input.assigneePrincipalId === undefined ? ticket.assignee_principal_id : input.assigneePrincipalId;
    if (assignee !== null && !await this.canProcess(assignee)) return { kind: "invalid" };
    if (!publicReply && !internalNote && ticket.status === input.status && assignee === ticket.assignee_principal_id) return { kind: "invalid" };

    const nextRevision = revision + 1;
    const commandId = `support_command_${crypto.randomUUID()}`;
    try {
      await this.db.batch([
        this.db.prepare(`INSERT INTO support_ticket_commands
          (id,ticket_id,principal_id,idempotency_key,request_hash,expected_revision,resulting_revision,
           next_status,assignee_principal_id,public_reply_text,internal_note_text,created_at)
          VALUES (?,?,?,?,?,?,?,?,?,?,?,?)`)
          .bind(commandId,input.ticketId,input.principalId,input.idempotencyKey,hash,revision,nextRevision,
            input.status,assignee,publicReply,internalNote,input.now),
        this.db.prepare(`INSERT INTO support_ticket_processing (ticket_id,revision,assignee_principal_id,updated_at)
          VALUES (?,?,?,?) ON CONFLICT(ticket_id) DO UPDATE SET revision=excluded.revision,
          assignee_principal_id=excluded.assignee_principal_id,updated_at=excluded.updated_at`)
          .bind(input.ticketId,nextRevision,assignee,input.now),
        this.db.prepare("UPDATE support_tickets SET status = ?, updated_at = ? WHERE id = ?")
          .bind(input.status,input.now,input.ticketId),
      ]);
      return { kind: "applied", revision: nextRevision };
    } catch (error) {
      const raced = await this.db.prepare(`SELECT request_hash,resulting_revision FROM support_ticket_commands
        WHERE ticket_id = ? AND principal_id = ? AND idempotency_key = ?`)
        .bind(input.ticketId,input.principalId,input.idempotencyKey).first<CommandRow>();
      if (raced) return raced.request_hash === hash
        ? { kind: "replay", revision: raced.resulting_revision } : { kind: "idempotency_conflict" };
      if (!await this.canProcess(input.principalId)) return { kind: "forbidden" };
      const current = await this.db.prepare("SELECT revision FROM support_ticket_processing WHERE ticket_id = ?")
        .bind(input.ticketId).first<{ revision: number }>();
      if ((current?.revision ?? 0) !== revision) return { kind: "revision_conflict", revision: current?.revision ?? 0 };
      throw error;
    }
  }
}
