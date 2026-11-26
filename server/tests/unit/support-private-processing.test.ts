import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { SupportTicketService, type SupportDatabase, type SupportStatement } from "../../packages/common/src/support-ticket-service";

type SqlValue = string | number | null;

class Statement implements SupportStatement {
  private values: SqlValue[] = [];
  constructor(private readonly db: initSqlJs.Database, private readonly sql: string) {}
  bind(...values: SqlValue[]): Statement { this.values = values; return this; }
  async first<T>(): Promise<T | null> {
    const stmt = this.db.prepare(this.sql);
    try { stmt.bind(this.values); return stmt.step() ? stmt.getAsObject() as T : null; }
    finally { stmt.free(); }
  }
  async all<T>(): Promise<{ results: T[] }> {
    const stmt = this.db.prepare(this.sql);
    const results: T[] = [];
    try { stmt.bind(this.values); while (stmt.step()) results.push(stmt.getAsObject() as T); return { results }; }
    finally { stmt.free(); }
  }
  async run(): Promise<unknown> { this.db.run(this.sql, this.values); return {}; }
}

class Database implements SupportDatabase {
  constructor(private readonly db: initSqlJs.Database) {}
  prepare(sql: string): Statement { return new Statement(this.db, sql); }
  async batch(statements: SupportStatement[]): Promise<unknown[]> {
    this.db.run("BEGIN;");
    try {
      const results = [];
      for (const statement of statements) results.push(await statement.run());
      this.db.run("COMMIT;");
      return results;
    } catch (error) {
      this.db.run("ROLLBACK;");
      throw error;
    }
  }
}

async function fixture() {
  const SQL = await initSqlJs();
  const db = new SQL.Database();
  db.run("PRAGMA foreign_keys = ON;");
  for (const name of ["0001_initial.sql", "0008_admin.sql", "0020_support_tickets.sql", "0072_support_web_answers.sql"]) {
    db.run(readFileSync(join(process.cwd(), "packages/db/migrations", name), "utf8"));
  }
  db.run("INSERT INTO accounts (id,status,created_at,updated_at) VALUES ('a1','ACTIVE',1,1),('a2','ACTIVE',1,1);");
  db.run("INSERT INTO admin_principals (id,access_subject,role,status,created_at,updated_at) VALUES ('p1','x1','OPERATOR','ACTIVE',1,1),('p2','x2','VIEWER','ACTIVE',1,1),('p3','x3','OPERATOR','ACTIVE',1,1);");
  db.run("INSERT INTO support_ticket_processor_grants (principal_id,permission,created_at) VALUES ('p1','SUPPORT_TICKET_PROCESS',1);");
  db.run(`INSERT INTO support_tickets (id,account_id,category,status,body_text,body_preview,body_length,server_received_at,idempotency_key,request_hash,created_at,updated_at)
    VALUES ('t1','a1','BUG_OTHER','OPEN','Original private body','Original private body',21,1,'original-key','hash',1,1);`);
  return { db, service: new SupportTicketService(new Database(db)) };
}

describe("private support processing", () => {
  it("lists only owned tickets and keeps receipts separate from replies", async () => {
    const { service } = await fixture();
    expect((await service.listOwnTickets("a2")).items).toEqual([]);
    const own = await service.listOwnTickets("a1");
    expect(own.items).toHaveLength(1);
    expect(own.items[0]).toMatchObject({ ticketId: "t1", publicReply: null });
    expect(await service.getOwnTicket("a2", "t1")).toBeNull();
  });

  it("writes a public answer once, replays the same command, and hides internal notes", async () => {
    const { db, service } = await fixture();
    const input = { ticketId: "t1", principalId: "p1", idempotencyKey: "answer-1", expectedRevision: 0, status: "IN_REVIEW" as const,
      publicReply: "We are checking the issue.", internalNote: "internal investigation", now: 100 };
    const first = await service.process(input);
    expect(first).toMatchObject({ kind: "applied", revision: 1 });
    expect(await service.process(input)).toMatchObject({ kind: "replay", revision: 1 });
    const owner = await service.getOwnTicket("a1", "t1");
    expect(owner).toMatchObject({ status: "IN_REVIEW", publicReply: "We are checking the issue." });
    expect(JSON.stringify(owner)).not.toContain("internal investigation");
    expect(await service.getOperatorTicket("p2", "t1")).toBeNull();
    expect(await service.getOperatorTicket("p1", "t1")).toMatchObject({
      ticketId: "t1", revision: 1, assigneePrincipalId: null,
      history: [{ principalId: "p1", publicReply: "We are checking the issue.", internalNote: "internal investigation" }],
    });
    expect(db.exec("SELECT body_text,idempotency_key FROM support_tickets")[0]?.values[0]).toEqual(["Original private body", "original-key"]);
    expect(db.exec("SELECT COUNT(*) FROM support_ticket_commands")[0]?.values[0]?.[0]).toBe(1);
  });

  it("rejects viewer writes, changed replay payloads, and stale revisions", async () => {
    const { service } = await fixture();
    const command = { ticketId: "t1", principalId: "p1", idempotencyKey: "one", expectedRevision: 0, status: "IN_REVIEW" as const, publicReply: "A public answer", now: 100 };
    expect(await service.process({ ...command, principalId: "p2" })).toMatchObject({ kind: "forbidden" });
    expect(await service.process({ ...command, principalId: "p3" })).toMatchObject({ kind: "forbidden" });
    expect(await service.process({ ...command, assigneePrincipalId: "p3" })).toMatchObject({ kind: "invalid" });
    expect(await service.process(command)).toMatchObject({ kind: "applied" });
    expect(await service.process({ ...command, publicReply: "Changed answer" })).toMatchObject({ kind: "idempotency_conflict" });
    expect(await service.process({ ...command, idempotencyKey: "two" })).toMatchObject({ kind: "revision_conflict", revision: 1 });
  });

  it("preserves an assigned operator when a later answer omits assignment", async () => {
    const { db, service } = await fixture();
    expect(await service.process({ ticketId: "t1", principalId: "p1", idempotencyKey: "assign", expectedRevision: 0,
      status: "IN_REVIEW", assigneePrincipalId: "p1", now: 100 })).toMatchObject({ kind: "applied" });
    expect(await service.process({ ticketId: "t1", principalId: "p1", idempotencyKey: "answer", expectedRevision: 1,
      status: "IN_REVIEW", publicReply: "The issue is resolved.", now: 101 })).toMatchObject({ kind: "applied" });
    expect(db.exec("SELECT assignee_principal_id FROM support_ticket_processing")[0]?.values[0]?.[0]).toBe("p1");
  });

  it("keeps a closed ticket closed until a reopen contract is defined", async () => {
    const { service } = await fixture();
    expect(await service.process({ ticketId: "t1", principalId: "p1", idempotencyKey: "close", expectedRevision: 0,
      status: "CLOSED", now: 100 })).toMatchObject({ kind: "applied" });
    expect(await service.process({ ticketId: "t1", principalId: "p1", idempotencyKey: "reopen", expectedRevision: 1,
      status: "OPEN", now: 101 })).toMatchObject({ kind: "invalid" });
  });
});
