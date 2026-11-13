import { describe, expect, it } from "vitest";
import app from "../../apps/admin-api/src/index";
import { ce03ProducerFixture } from "../helpers/ce03-producer-fixture";
import { attendanceMonth } from "../../apps/game-api/src/monthly-attendance-policy";
import { objectivePeriods } from "../../apps/game-api/src/recurring-policy";
import { mkdir, writeFile } from "node:fs/promises";
import { createHash } from "node:crypto";

async function exportFixture(name: string, value: unknown) {
  await mkdir("audit-results/admin-player-activities", { recursive: true });
  await writeFile(`audit-results/admin-player-activities/${name}-${crypto.randomUUID()}.json`, JSON.stringify(value, null, 2) + "\n");
}

async function fixture() {
  const f = await ce03ProducerFixture();
  f.sql.run("INSERT INTO admin_principals(id,access_subject,role,status,display_name,created_at,updated_at) VALUES('viewer','access|viewer','VIEWER','ACTIVE','Viewer',0,0)");
  const env = { ...f.env, ENVIRONMENT: "LOCAL", ADMIN_AUTH_MODE: "LOCAL_HEADER" } as Env;
  const headers = { "Cf-Access-Subject": "access|viewer" };
  const get = (path = "/v1/admin/players/a/activity-summary", extra = {}) => app.request(path, { headers: { ...headers, ...extra } }, env);
  return { ...f, env, headers, get };
}
describe("Admin player activity summary uses existing admin auth and safe read projections", () => {
  it("distinguishes missing configuration from available empty records without writes", async () => {
    const f = await fixture(), prepare = f.adapter.prepare.bind(f.adapter);
    f.adapter.prepare = sql => { if (!/^\s*SELECT\b/iu.test(sql)) throw new Error("Admin read attempted a write"); return prepare(sql); };
    const databaseHash = () => createHash("sha256").update(f.sql.export()).digest("hex");
    const before = databaseHash(); const response = await f.get();
    expect(response.status).toBe(200); expect(response.headers.get("Cache-Control")).toBe("no-store");
    const result = await response.json();
    expect(result).toMatchObject({ version: "admin-player-activities-v1", accountId: "a", permission: "SUPPORT_READ",
      shop: { availability: "UNCONFIGURED", data: null }, monthlyAttendance: { availability: "UNCONFIGURED", data: null },
      pass: { availability: "UNCONFIGURED", data: null }, ordinaryQuests: { availability: "UNCONFIGURED", data: null },
      mail: { availability: "AVAILABLE", data: { items: [], hasMore: false } } });
    expect(databaseHash()).toBe(before);
    await exportFixture("unconfigured", result);
  });
  it("rejects unauthenticated, unknown account, query and body requests", async () => {
    const f = await fixture();
    expect((await app.request("/v1/admin/players/a/activity-summary", {}, f.env)).status).toBe(401);
    expect((await f.get("/v1/admin/players/missing/activity-summary")).status).toBe(404);
    expect((await f.get("/v1/admin/players/a/activity-summary?accountId=b")).status).toBe(400);
    expect((await f.get(undefined, { "Content-Length": "1" })).status).toBe(400);
  });
  it("returns known zero values and only the selected account's materialized records", async () => {
    const f = await fixture(), now = Date.now(), month = attendanceMonth(now), period = objectivePeriods(now);
    f.sql.run("INSERT INTO crystal_policies VALUES('wallet',?)", [JSON.stringify({ currencyId: "cur_crystal", ticketExchange: { ticketItemId: "cur_command_ticket", crystalsPerTicket: 100, maxTickets: 10 }, purchase: null })]);
    f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'wallet')");
    const policy = JSON.stringify({ version: "recurring-v1", cosmetics: { FRAME: null, OPERATOR_SKIN: null, TRAIN_SKIN: null }, purchase: null });
    f.sql.run("INSERT INTO recurring_definitions VALUES('pass','PASS50',?,?,?)", [month.startAt, month.endAt, policy]);
    f.sql.run("INSERT INTO recurring_definitions VALUES('quests','QUESTS',?,?,?)", [period.weekStart, period.weekStart + 7 * 86400000, policy]);
    for (const account of ["a", "b"]) {
      f.sql.run("INSERT INTO recurring_awards VALUES(?,?,?,'QUESTS','period','D2',?,?,0,?)", ["award-" + account, account, "quests", now - 10, now + 86400000, '{"secret":"RAW_REWARD_MUST_NOT_LEAK"}']);
      f.sql.run("INSERT INTO quest_mail VALUES(?,?,?,?)", ["award-" + account, account, now, now + 1209600000]);
    }
    const response = await f.get(), raw = await response.text(); expect(response.status).toBe(200);
    expect(raw).not.toContain("RAW_REWARD_MUST_NOT_LEAK"); expect(raw).not.toContain("award-b");
    expect(JSON.parse(raw)).toMatchObject({ shop: { availability: "AVAILABLE", data: { wallet: { freeBalance: 0, paidBalance: 0, totalBalance: 0 }, dailyRemaining: 1, purchaseVerificationAvailable: null } },
      pass: { availability: "AVAILABLE", data: { earnedXp: 0, level: 0, premium: false, progressSyncPending: false } },
      ordinaryQuests: { availability: "AVAILABLE", data: { items: [{ awardId: "award-a" }], hasMore: false } },
      mail: { availability: "AVAILABLE", data: { items: [{ awardId: "award-a" }], hasMore: false } } });
    await exportFixture("configured-zero-and-owned-records", JSON.parse(raw));
  });
  it("keeps an unreadable section unknown instead of converting it to zero or empty", async () => {
    const f = await fixture(); f.sql.run("DROP TABLE quest_mail");
    const response = await f.get(); expect(response.status).toBe(200);
    const result = await response.json();
    expect(result).toMatchObject({ mail: { availability: "UNAVAILABLE", reason: "source_read_failed", data: null } });
    await exportFixture("unavailable-source", result);
  });
  it("retains monthly incomplete reward binding and zero progress without inventing attendance eligibility", async () => {
    const f = await fixture(), month = attendanceMonth(Date.now());
    f.sql.run("INSERT INTO activity_definitions VALUES('monthly',?)", [JSON.stringify({ kind: "MONTHLY_ATTENDANCE", version: "monthly-attendance-v1", startAt: month.startAt, endAt: month.endAt, eliteTicketItemId: null })]);
    const response = await f.get(), result = await response.json(); expect(response.status).toBe(200);
    expect(result.monthlyAttendance).toMatchObject({ availability: "AVAILABLE", data: { activityId: "monthly", canAttend: false,
      unavailableReason: "elite_ticket_binding_missing", revision: 0, completedCells: 0, todayAttended: false } });
    expect(result.monthlyAttendance.data.cells).toHaveLength(28);
    expect(result.monthlyAttendance.data.cells[0]).toEqual({ cell: 1, claimed: false, source: null, dayIndex: null, unavailableReason: null });
    await exportFixture("monthly-incomplete-binding", result);
  });
  it("caps mail at 100 and ignores rows whose award belongs to another account", async () => {
    const f = await fixture(), now = Date.now(), period = objectivePeriods(now);
    f.sql.run("INSERT INTO recurring_definitions VALUES('quests','QUESTS',?,?,?)", [period.weekStart, period.weekStart + 7 * 86400000,
      JSON.stringify({ version: "recurring-v1", cosmetics: { FRAME: null, OPERATOR_SKIN: null, TRAIN_SKIN: null }, purchase: null })]);
    for (let i = 0; i < 102; i++) {
      const accountId = i === 101 ? "b" : "a", id = `award-${i}`;
      f.sql.run("INSERT INTO recurring_awards VALUES(?,?,?,'QUESTS',?,'D2',?,?,0,'[]')", [id, accountId, "quests", `period-${i}`, now + i, now + 86400000]);
      // Last row deliberately has contradictory owner foreign keys; projection must not leak it.
      f.sql.run("INSERT INTO quest_mail VALUES(?,'a',?,?)", [id, now + i, now + i + 1209600000]);
    }
    const result = await (await f.get()).json();
    expect(result.mail.data.items).toHaveLength(100); expect(result.mail.data.hasMore).toBe(true);
    expect(result.ordinaryQuests.data.items).toHaveLength(100); expect(result.ordinaryQuests.data.hasMore).toBe(true);
    expect(JSON.stringify(result)).not.toContain('"award-101"');
  });
  it("reports invalid configured policy as unavailable without leaking its raw payload", async () => {
    const f = await fixture(), month = attendanceMonth(Date.now());
    f.sql.run("INSERT INTO recurring_definitions VALUES('bad-pass','PASS50',?,?,?)", [month.startAt, month.endAt, '{"secret":"PRIVATE_BAD_POLICY"}']);
    const raw = await (await f.get()).text(); expect(raw).not.toContain("PRIVATE_BAD_POLICY");
    expect(JSON.parse(raw).pass).toEqual({ availability: "UNAVAILABLE", reason: "recurring_policy_invalid", data: null });
  });
});
