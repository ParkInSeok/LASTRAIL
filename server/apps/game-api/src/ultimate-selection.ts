import type { Context, Hono } from "hono";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError, activityId } from "./activity-policy";
import { formationSelection } from "./formation";
import { walletId } from "./crystal-wallet";

const revision = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER - 1);
const schema = z.strictObject({ rulesVersion: z.literal("ultimate-selection-v1"), operatorId: activityId.nullable(), expectedRevision: revision, expectedFormationRevision: revision });
export type UltimateFormation = { revision: number; formation_json: string; ultimate_operator_id: string | null; ultimate_revision: number };
export function ultimateSelection(f: UltimateFormation) {
  let raw: unknown;
  try { raw = JSON.parse(f.formation_json); } catch { throw new ActivityError(409, "formation_schema_unsupported"); }
  const squad = formationSelection.safeParse(raw);
  if (!squad.success) throw new ActivityError(409, "formation_schema_unsupported");
  if (f.ultimate_operator_id !== null && !squad.data.operators.some(o => o.operatorId === f.ultimate_operator_id)) throw new ActivityError(409, "ultimate_selection_invalid");
  return { rulesVersion: "ultimate-selection-v1" as const, operatorId: f.ultimate_operator_id, revision: f.ultimate_revision };
}
const openRun = "SELECT 1 FROM runs WHERE account_id=?1 AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED')";
const owned = `SELECT COUNT(*) FROM json_each(?2,'$.operators') j JOIN account_operators o
  ON o.account_id=?1 AND o.operator_id=json_extract(j.value,'$.operatorId')`;
async function read(db: D1Database, accountId: string) {
  return db.prepare(`SELECT f.*, (${openRun}) AS open_run,
    (SELECT COUNT(*) FROM json_each(f.formation_json,'$.operators') j JOIN account_operators o
      ON o.account_id=f.account_id AND o.operator_id=json_extract(j.value,'$.operatorId')) AS owned_count
    FROM account_formation f WHERE f.account_id=?1`).bind(accountId).first<UltimateFormation & { open_run: number | null; owned_count: number }>();
}
async function save(db: D1Database, accountId: string, body: z.infer<typeof schema>, key: string) {
  const hash = await sha256Hex(canonicalJson(body));
  const replay = async () => {
    const receipt = await db.prepare("SELECT request_hash,response_json FROM ultimate_selection_receipts WHERE account_id=? AND idempotency_key=?").bind(accountId, key).first<{ request_hash: string; response_json: string }>();
    if (!receipt) return null;
    if (receipt.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
    return JSON.parse(receipt.response_json) as Record<string, unknown>;
  };
  for (let attempt = 0; attempt < 4; attempt++) {
    const previous = await replay(); if (previous) return previous;
    try {
      const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?").bind(accountId).first<{ value: number }>())!.value;
      const f = await read(db, accountId);
      if (!f) throw new ActivityError(409, "formation_required");
      ultimateSelection(f);
      if (f.revision !== body.expectedFormationRevision) throw new ActivityError(409, "formation_revision_conflict");
      if (f.ultimate_revision !== body.expectedRevision) throw new ActivityError(409, "ultimate_revision_conflict");
      if (f.open_run) throw new ActivityError(409, "ultimate_open_run");
      const squad = formationSelection.parse(JSON.parse(f.formation_json));
      if (f.owned_count !== 5 || (body.operatorId !== null && !squad.operators.some(o => o.operatorId === body.operatorId))) throw new ActivityError(403, "ultimate_operator_not_eligible");
      const next = f.ultimate_revision + (f.ultimate_operator_id === body.operatorId ? 0 : 1);
      const mutationId = walletId("ultimate"), now = Date.now();
      const response = { accepted: true, rulesVersion: body.rulesVersion, operatorId: body.operatorId, revision: next,
        formationRevision: f.revision, selectionStatus: body.operatorId === null ? "UNSELECTED" : "SELECTED", refreshRequired: true };
      await db.batch([
        db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'ULTIMATE_SELECTION',?,?)").bind(mutationId, accountId, sequence, key, now),
        db.prepare(`INSERT INTO ultimate_write_guard(valid) SELECT CASE WHEN EXISTS(
          SELECT 1 FROM account_formation WHERE account_id=?1 AND formation_json=?2 AND revision=?3
          AND ultimate_revision=?4 AND ultimate_operator_id IS ?5)
          AND (${owned})=5 AND NOT EXISTS(${openRun}) THEN 1 ELSE 0 END`)
          .bind(accountId, f.formation_json, f.revision, f.ultimate_revision, f.ultimate_operator_id),
        db.prepare("UPDATE account_formation SET ultimate_operator_id=?,ultimate_revision=?,updated_at=? WHERE account_id=?").bind(body.operatorId, next, now, accountId),
        db.prepare("INSERT INTO ultimate_selection_receipts(account_id,idempotency_key,request_hash,response_json,mutation_id) VALUES(?,?,?,?,?)").bind(accountId, key, hash, JSON.stringify(response), mutationId),
        db.prepare("DELETE FROM ultimate_write_guard"),
      ]);
      return response;
    } catch (error) {
      const completed = await replay(); if (completed) return completed;
      if (/UNIQUE constraint failed|CHECK constraint failed: valid=1|ultimate receipt immutable/iu.test(String(error))) {
        if (attempt < 3) continue;
        throw new ActivityError(409, "ultimate_write_conflict");
      }
      throw error;
    }
  }
  throw new ActivityError(409, "ultimate_write_conflict");
}
export function registerUltimateRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  app.on(["GET", "POST"], "/v1/formation/ultimate", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search) throw new ActivityError(400, "contract_violation");
      const raw = await readBoundedBody(c);
      if (c.req.method === "GET") {
        if (raw.length) throw new ActivityError(400, "contract_violation");
        const f = await read(c.env.DB, auth.accountId);
        const selected = f ? ultimateSelection(f) : { rulesVersion: "ultimate-selection-v1", operatorId: null, revision: 0 };
        const status = !f ? "FORMATION_REQUIRED" : f.owned_count !== 5 ? "OWNERSHIP_INVALID" : selected.operatorId === null ? "UNSELECTED" : "SELECTED";
        return c.json({ accepted: true, ...selected, formationRevision: f?.revision ?? 0, selectionStatus: status, changeLocked: Boolean(f?.open_run) });
      }
      const key = c.req.header("Idempotency-Key") ?? "";
      let body: z.infer<typeof schema>;
      try { body = schema.parse(JSON.parse(raw)); } catch { throw new ActivityError(400, "contract_violation"); }
      if (!/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "contract_violation");
      return c.json(await save(c.env.DB, auth.accountId, body, key));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  });
}
