import type { Context, Hono } from "hono";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError, activityId } from "./activity-policy";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { readCrystalWallet, walletId } from "./crystal-wallet";
import { planActivityGrant } from "./activity-grant-plan";
import { attendanceMonth, monthlyCells, parseMonthlyPolicy } from "./monthly-attendance-policy";
import { objectiveLoginEvent } from "./objective-events";

const base = { policyVersion: activityId, cycleId: z.string().regex(/^\d{4}-\d{2}$/u), expectedRevision: z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER - 1) };
const naturalBody = z.strictObject(base);
const recoveryBody = z.strictObject({ ...base, walletPolicyVersion: activityId, confirmedPaid: z.number().int().min(0).max(100) });
type Body = z.infer<typeof naturalBody> | z.infer<typeof recoveryBody>;
type Options = { db: D1Database; accountId: string; activityId: string };
type Cell = { cell: number; source: "NATURAL" | "RECOVERY"; dayIndex: number; claimId: string };
type State = { registeredAt: number; revision: number; policyVersion: string | null; activeDefinitionCount: number; cells: Cell[]; visits: number[] };

export async function discoverMonthlyAttendance(db: D1Database, accountId: string) {
  // One DB snapshot supplies clock, this account's binding and all candidate
  // definitions. Never race multiple reads into an arbitrary first match.
  const row = await db.prepare(`SELECT json_object('serverTime',clock.now,
    'pinned',(SELECT activity_id FROM monthly_attendance_cycles WHERE account_id=?1
      AND cycle_id=strftime('%Y-%m',clock.now/1000,'unixepoch','+5 hours')),
    'definitions',json((SELECT json_group_array(json_object('id',id,'policy',policy_json))
      FROM (SELECT id,policy_json FROM activity_definitions WHERE json_extract(policy_json,'$.kind')='MONTHLY_ATTENDANCE' ORDER BY id)))) AS value
    FROM (SELECT CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER) AS now) clock`).bind(accountId).first<{ value: string }>();
  const snapshot = JSON.parse(row!.value) as { serverTime: number; pinned: string | null; definitions: Array<{ id: string; policy: string }> };
  const month = attendanceMonth(snapshot.serverTime);
  const empty = { accepted: true, version: "monthly-attendance-discovery-v1", ...month, serverTime: snapshot.serverTime,
    activityId: null as string | null, policyVersion: null as string | null, pinned: snapshot.pinned !== null, canAttend: false };
  let definitions: Array<{ id: string; policy: ReturnType<typeof parseMonthlyPolicy> }>;
  try { definitions = snapshot.definitions.map(d => ({ id: d.id, policy: parseMonthlyPolicy(d.policy) })); }
  catch { return { httpStatus: 503 as const, body: { ...empty, accepted: false, status: "INVALID_CONFIGURATION", errorCode: "monthly_attendance_policy_invalid" } }; }
  const active = definitions.filter(d => d.policy.startAt <= snapshot.serverTime && snapshot.serverTime < d.policy.endAt);
  const pinned = definitions.find(d => d.id === snapshot.pinned);
  const preserved = { ...empty, activityId: snapshot.pinned, policyVersion: snapshot.pinned };
  if (active.length > 1) return { httpStatus: 409 as const, body: { ...preserved, accepted: false, status: "AMBIGUOUS_CONFIGURATION", errorCode: "attendance_definition_ambiguous", candidateCount: active.length } };
  if (snapshot.pinned && (!pinned || !active.some(d => d.id === pinned.id))) return { httpStatus: 409 as const, body: { ...preserved, accepted: false, status: "PINNED_POLICY_UNAVAILABLE", errorCode: "attendance_cycle_policy_changed" } };
  const selected = pinned ?? active[0];
  if (!selected) return { httpStatus: 200 as const, body: { ...empty, status: definitions.length ? "OUTSIDE_WINDOW" : "NOT_CONFIGURED", errorCode: null } };
  const bound = selected.policy.eliteTicketItemId !== null;
  return { httpStatus: 200 as const, body: { ...empty, activityId: selected.id, policyVersion: selected.id,
    definitionStartAt: selected.policy.startAt, definitionEndAt: selected.policy.endAt,
    canAttend: bound, status: bound ? "AVAILABLE" : "REWARD_BINDING_UNAVAILABLE", errorCode: bound ? null : "elite_ticket_binding_missing" } };
}

async function context(o: Options, now: number) {
  const row = await o.db.prepare("SELECT policy_json FROM activity_definitions WHERE id=?").bind(o.activityId).first<{ policy_json: string }>();
  if (!row) throw new ActivityError(503, "activity_not_configured");
  const policy = parseMonthlyPolicy(row.policy_json), month = attendanceMonth(now);
  const raw = await o.db.prepare(`SELECT json_object('registeredAt',a.created_at,
    'activeDefinitionCount',(SELECT COUNT(*) FROM activity_definitions WHERE json_extract(policy_json,'$.kind')='MONTHLY_ATTENDANCE'
      AND json_extract(policy_json,'$.startAt')<=?3 AND json_extract(policy_json,'$.endAt')>?3),
    'revision',COALESCE((SELECT revision FROM monthly_attendance_cycles WHERE account_id=a.id AND cycle_id=?2),0),
    'policyVersion',(SELECT activity_id FROM monthly_attendance_cycles WHERE account_id=a.id AND cycle_id=?2),
    'cells',json((SELECT json_group_array(json_object('cell',cell,'source',source,'dayIndex',day_index,'claimId',claim_id)) FROM monthly_attendance_cells WHERE account_id=a.id AND cycle_id=?2)),
    'visits',json((SELECT json_group_array(day_index) FROM monthly_attendance_visits WHERE account_id=a.id AND cycle_id=?2))) AS value
    FROM accounts a WHERE a.id=?1`).bind(o.accountId, month.cycleId, now).first<{ value: string }>();
  if (!raw) throw new ActivityError(403, "account_not_found");
  const state = JSON.parse(raw.value) as State;
  if (state.activeDefinitionCount > 1) throw new ActivityError(409, "attendance_definition_ambiguous");
  if (state.policyVersion !== null && state.policyVersion !== o.activityId) throw new ActivityError(409, "attendance_cycle_policy_changed");
  // A partially elapsed registration service day is not a fully missed day.
  const firstRecoverableDay = Math.max(0, Math.ceil((state.registeredAt - month.startAt) / 86400000));
  const used = new Set([...state.visits, ...state.cells.map(c => c.dayIndex)]);
  const missed = Array.from({ length: Math.max(0, month.dayIndex - firstRecoverableDay) }, (_, i) => firstRecoverableDay + i).filter(d => !used.has(d));
  const recovered = state.cells.filter(c => c.source === "RECOVERY").length;
  const recoverable = Math.max(0, Math.min(missed.length, 7 - recovered, 28 - state.cells.length));
  return { policy, month, state, missed, recovered, recoverable, todayAttended: state.visits.includes(month.dayIndex) };
}
export async function readMonthlyAttendance(o: Options) {
  const now = Date.now(), c = await context(o, now);
  let wallet: Awaited<ReturnType<typeof readCrystalWallet>> | null = null;
  try { wallet = await readCrystalWallet(o.db, o.accountId); } catch (error) { if (!(error instanceof ActivityError)) throw error; }
  const active = now >= c.policy.startAt && now < c.policy.endAt;
  return { accepted: true, activityId: o.activityId, policyVersion: o.activityId, version: c.policy.version,
    ...c.month, serverTime: now, active, revision: c.state.revision, completedCells: c.state.cells.length,
    complete: c.state.cells.length === 28, todayAttended: c.todayAttended,
    unavailableReason: c.policy.eliteTicketItemId === null ? "elite_ticket_binding_missing" : null,
    cells: monthlyCells(c.policy).map(cell => ({ ...cell, receipt: c.state.cells.find(row => row.cell === cell.cell) ?? null })),
    recovery: { price: 100, maxPerCycle: 7, used: c.recovered, recoverableCount: c.recoverable,
      todayAttendanceRequired: !c.todayAttended,
      quote: wallet ? { walletPolicyVersion: wallet.policyVersion, currencyId: wallet.currencyId,
        freeSpent: Math.min(100, wallet.freeBalance), paidSpent: Math.max(0, 100 - wallet.freeBalance),
        sufficientFunds: wallet.totalBalance >= 100 } : null },
  };
}
export async function mutateMonthlyAttendance(o: Options, body: Body, kind: "NATURAL" | "RECOVERY", key: string) {
  const { db, accountId, activityId: policyVersion } = o;
  const operation = `monthly_attendance_${kind.toLowerCase()}`;
  const hash = await sha256Hex(canonicalJson({ policyVersion, kind, body }));
  const replay = async () => {
    const r = await db.prepare(`SELECT r.request_hash,c.response_json FROM monthly_attendance_requests r JOIN activity_claims c ON c.id=r.claim_id AND c.account_id=r.account_id
      WHERE r.account_id=? AND r.operation=? AND r.idempotency_key=?`).bind(accountId, operation, key).first<{ request_hash: string; response_json: string }>();
    if (!r) return null;
    if (r.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
    return JSON.parse(r.response_json) as Record<string, unknown>;
  };
  for (let attempt = 0; attempt < 4; attempt++) {
    const original = await replay(); if (original) return original;
    try {
      const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?").bind(accountId).first<{ value: number }>())!.value;
      const now = Date.now(), c = await context(o, now);
      if (body.policyVersion !== policyVersion || body.cycleId !== c.month.cycleId) throw new ActivityError(409, "attendance_cycle_changed");
      if (now < c.policy.startAt || now >= c.policy.endAt) throw new ActivityError(409, "activity_outside_window");
      // No partially configured 27-cell launch and no substitute ordinary ticket.
      if (!c.policy.eliteTicketItemId) throw new ActivityError(503, "attendance_reward_binding_unavailable");
      if (kind === "NATURAL" && c.todayAttended) {
        const old = await db.prepare(`SELECT a.id,a.response_json FROM monthly_attendance_visits v JOIN activity_claims a ON a.id=v.claim_id
          WHERE v.account_id=? AND v.cycle_id=? AND v.day_index=?`).bind(accountId, c.month.cycleId, c.month.dayIndex).first<{ id: string; response_json: string }>();
        await db.batch([db.prepare("INSERT INTO monthly_attendance_requests(account_id,operation,idempotency_key,request_hash,claim_id) VALUES(?,?,?,?,?)").bind(accountId, operation, key, hash, old!.id)]);
        return JSON.parse(old!.response_json) as Record<string, unknown>;
      }
      if (body.expectedRevision !== c.state.revision) throw new ActivityError(409, "attendance_revision_conflict");
      if (kind === "RECOVERY" && !c.todayAttended) throw new ActivityError(409, "attendance_today_required");
      if (kind === "RECOVERY" && c.recoverable === 0) throw new ActivityError(409, "attendance_not_recoverable");
      const day = kind === "NATURAL" ? c.month.dayIndex : c.missed[0]!;
      const nextCell = c.state.cells.length < 28 ? c.state.cells.length + 1 : null;
      const rewards = nextCell === null ? [] : monthlyCells(c.policy)[nextCell - 1]!.rewards;
      const claimId = walletId("monthly_attendance"), mutationId = walletId("monthly_mutation"), economyId = walletId("economy");
      const plan = await planActivityGrant({ db, accountId, mutationId, claimId, economyId, now, rewards, operation,
        ...(kind === "RECOVERY" && "confirmedPaid" in body ? { recovery: { confirmedPaid: body.confirmedPaid, walletPolicyVersion: body.walletPolicyVersion } } : {}),
      });
      if (kind === "RECOVERY" && !("confirmedPaid" in body)) throw new ActivityError(400, "contract_violation");
      const claimKey = `${c.month.cycleId}:${kind}:${day}`;
      const response = { accepted: true, version: c.policy.version, policyVersion, cycleId: c.month.cycleId, kind,
        revision: c.state.revision + 1, claimId, rewardTransactionId: claimId, accountMutationId: mutationId, economyTransactionId: economyId,
        cell: nextCell, sourceDayIndex: day, completedCells: c.state.cells.length + (nextCell === null ? 0 : 1),
        rewardLines: plan.lines, wallet: plan.wallet, serverClaimedAt: now, refreshRequired: true };
      const json = JSON.stringify(response);
      const writes = [
        db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,?,?,?)").bind(mutationId, accountId, sequence, operation, key, now),
        db.prepare(`INSERT INTO monthly_attendance_guard(valid) SELECT CASE WHEN
          clock.now>=?1 AND clock.now<?2 AND clock.now>=?3 AND clock.now<?4
          AND COALESCE((SELECT revision FROM monthly_attendance_cycles WHERE account_id=?5 AND cycle_id=?6),0)=?7
          AND NOT EXISTS(SELECT 1 FROM monthly_attendance_cycles WHERE account_id=?5 AND cycle_id=?6 AND activity_id<>?8)
          AND (SELECT created_at FROM accounts WHERE id=?5)=?9
          AND (SELECT COUNT(*) FROM activity_definitions WHERE json_extract(policy_json,'$.kind')='MONTHLY_ATTENDANCE'
            AND json_extract(policy_json,'$.startAt')<=clock.now AND json_extract(policy_json,'$.endAt')>clock.now)=1
          THEN 1 ELSE 0 END FROM (SELECT CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER) AS now) clock`)
          .bind(c.month.startAt + c.month.dayIndex * 86400000, c.month.startAt + (c.month.dayIndex + 1) * 86400000,
            c.policy.startAt, c.policy.endAt, accountId, c.month.cycleId, c.state.revision, policyVersion, c.state.registeredAt),
        db.prepare(`INSERT INTO monthly_attendance_cycles(account_id,cycle_id,activity_id,revision) VALUES(?,?,?,?)
          ON CONFLICT(account_id,cycle_id) DO UPDATE SET revision=excluded.revision`).bind(accountId, c.month.cycleId, policyVersion, c.state.revision + 1),
        db.prepare("INSERT INTO economy_transactions(id,account_id,mutation_id,reason,source_type,source_id,created_at) VALUES(?,?,?,?,'ACTIVITY',?,?)").bind(economyId, accountId, mutationId, operation, claimId, now),
        ...plan.statements,
        db.prepare("INSERT INTO reward_transactions(id,account_id,run_id,mutation_id,balance_config_version,reward_json,created_at) VALUES(?,?,NULL,?,NULL,?,?)").bind(claimId, accountId, mutationId, json, now),
        db.prepare("INSERT INTO activity_claims(id,account_id,activity_id,claim_key,operation,idempotency_key,request_hash,response_json,created_at) VALUES(?,?,?,?,?,?,?,?,?)").bind(claimId, accountId, policyVersion, claimKey, operation, key, hash, json, now),
        db.prepare("INSERT INTO monthly_attendance_requests(account_id,operation,idempotency_key,request_hash,claim_id) VALUES(?,?,?,?,?)").bind(accountId, operation, key, hash, claimId),
      ];
      if (kind === "NATURAL") writes.push(db.prepare("INSERT INTO monthly_attendance_visits(account_id,cycle_id,day_index,claim_id) VALUES(?,?,?,?)").bind(accountId, c.month.cycleId, day, claimId), objectiveLoginEvent(db, accountId));
      if (nextCell !== null) writes.push(db.prepare("INSERT INTO monthly_attendance_cells(account_id,cycle_id,cell,source,day_index,claim_id) VALUES(?,?,?,?,?,?)").bind(accountId, c.month.cycleId, nextCell, kind, day, claimId));
      writes.push(db.prepare("DELETE FROM monthly_attendance_guard"));
      await db.batch(writes);
      return response;
    } catch (error) {
      const completed = await replay(); if (completed) return completed;
      if (/UNIQUE constraint failed|CHECK constraint failed: valid=1|CHECK constraint failed.*singleton_id|monthly request immutable/iu.test(String(error))) {
        if (attempt < 3) continue;
        throw new ActivityError(409, "attendance_state_changed");
      }
      throw error;
    }
  }
  throw new ActivityError(409, "attendance_state_changed");
}
export function registerMonthlyAttendanceRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  app.get("/v1/attendance/monthly/current", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    if (new URL(c.req.url).search || (await readBoundedBody(c)).length) return c.json({ accepted: false, errorCode: "contract_violation" }, 400);
    const found = await discoverMonthlyAttendance(c.env.DB, auth.accountId);
    return c.json(found.body, found.httpStatus);
  });
  const handler = (kind?: "NATURAL" | "RECOVERY") => async (c: Context<{ Bindings: Env }>) => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      const activityIdValue = c.req.param("activityId");
      if (!activityId.safeParse(activityIdValue).success || new URL(c.req.url).search) throw new ActivityError(400, "contract_violation");
      const raw = await readBoundedBody(c), options = { db: c.env.DB, accountId: auth.accountId, activityId: activityIdValue! };
      if (!kind) { if (raw.length) throw new ActivityError(400, "contract_violation"); return c.json(await readMonthlyAttendance(options)); }
      const key = c.req.header("Idempotency-Key") ?? "";
      let body: Body;
      try { body = (kind === "NATURAL" ? naturalBody : recoveryBody).parse(JSON.parse(raw)); } catch { throw new ActivityError(400, "contract_violation"); }
      if (!/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "contract_violation");
      return c.json(await mutateMonthlyAttendance(options, body, kind, key));
    } catch (error) { if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status); throw error; }
  };
  app.get("/v1/attendance/:activityId/monthly", handler());
  app.post("/v1/attendance/:activityId/auto-attend", handler("NATURAL"));
  app.post("/v1/attendance/:activityId/recover-next", handler("RECOVERY"));
}
