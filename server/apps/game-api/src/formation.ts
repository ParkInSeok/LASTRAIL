import type { Context, Hono } from "hono";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError, activityId } from "./activity-policy";
import { walletId } from "./crystal-wallet";

const revision = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER - 1);
const operatorSlots = z.array(z.strictObject({ slot: z.number().int().min(0).max(4), operatorId: activityId })).length(5)
  .refine(rows => new Set(rows.map(r => r.slot)).size === 5 && new Set(rows.map(r => r.operatorId)).size === 5);
const selection = z.strictObject({ schemaVersion: z.literal(1), operators: operatorSlots });
const saveBody = z.strictObject({ expectedRevision: revision, name: z.string().trim().min(1).max(80), operators: operatorSlots });
const activateBody = z.strictObject({ expectedRevision: revision, expectedFormationRevision: revision });
type Save = z.infer<typeof saveBody> & { kind: "SAVE"; presetId: string };
type Activate = z.infer<typeof activateBody> & { kind: "ACTIVATE"; presetId: string };
type Receipt = { request_hash: string; response_json: string };
const openRun = "SELECT 1 FROM runs WHERE account_id=? AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED')";
const ownedCount = `SELECT COUNT(*) AS count FROM json_each(?) j JOIN account_operators o
  ON o.account_id=? AND o.operator_id=json_extract(j.value,'$.operatorId')`;

async function mutateFormation(db: D1Database, accountId: string, action: Save | Activate, key: string) {
  const hash = await sha256Hex(canonicalJson(action));
  const replay = async () => {
    const receipt = await db.prepare("SELECT request_hash,response_json FROM formation_receipts WHERE account_id=? AND idempotency_key=?")
      .bind(accountId, key).first<Receipt>();
    if (!receipt) return null;
    if (receipt.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
    return JSON.parse(receipt.response_json) as Record<string, unknown>;
  };
  for (let attempt = 0; attempt < 4; attempt++) {
    const committed = await replay();
    if (committed) return committed;
    try {
      const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?")
        .bind(accountId).first<{ value: number }>())!.value;
      const preset = await db.prepare("SELECT revision,preset_json FROM commander_presets WHERE account_id=? AND id=?")
        .bind(accountId, action.presetId).first<{ revision: number; preset_json: string }>();
      if ((preset?.revision ?? 0) !== action.expectedRevision) throw new ActivityError(409, "preset_revision_conflict");
      let operators: z.infer<typeof operatorSlots>;
      let active: { revision: number; preset_id: string; preset_revision: number; formation_json: string } | null = null;
      if (action.kind === "SAVE") operators = action.operators;
      else {
        if (!preset) throw new ActivityError(409, "preset_not_found");
        let definition: unknown;
        try { definition = JSON.parse(preset.preset_json); } catch { throw new ActivityError(409, "preset_schema_unsupported"); }
        const parsed = selection.safeParse(definition);
        if (!parsed.success) throw new ActivityError(409, "preset_schema_unsupported");
        operators = parsed.data.operators;
        active = await db.prepare("SELECT revision,preset_id,preset_revision,formation_json FROM account_formation WHERE account_id=?")
          .bind(accountId).first();
        if ((active?.revision ?? 0) !== action.expectedFormationRevision) throw new ActivityError(409, "formation_revision_conflict");
        if (await db.prepare(openRun).bind(accountId).first()) throw new ActivityError(409, "formation_open_run");
      }
      operators = [...operators].sort((a, b) => a.slot - b.slot);
      const operatorsJson = JSON.stringify(operators);
      if ((await db.prepare(ownedCount).bind(operatorsJson, accountId).first<{ count: number }>())?.count !== 5) throw new ActivityError(403, "formation_operator_not_owned");
      const definitionJson = canonicalJson({ schemaVersion: 1, operators });
      const changed = active?.formation_json !== definitionJson;
      const unchanged = action.kind === "ACTIVATE" && !changed && active?.preset_id === action.presetId && active.preset_revision === action.expectedRevision;
      const now = Date.now(), mutationId = walletId("formation_mut"), receiptId = walletId("formation");
      const nextRevision = action.kind === "SAVE" ? action.expectedRevision + 1 : (active?.revision ?? 0) + (unchanged ? 0 : 1);
      const response = { accepted: true, receiptId, kind: action.kind, presetId: action.presetId,
        presetRevision: action.kind === "SAVE" ? nextRevision : action.expectedRevision,
        ...(action.kind === "ACTIVATE" ? { formationRevision: nextRevision, formationChanged: changed, operators } : {}),
        refreshRequired: true };
      const writes: D1PreparedStatement[] = [
        db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'FORMATION',?,?)")
          .bind(mutationId, accountId, sequence, key, now),
        db.prepare(`INSERT INTO formation_write_guard(valid) SELECT CASE WHEN
          COALESCE((SELECT revision FROM commander_presets WHERE account_id=? AND id=?),0)=?
          AND (${ownedCount})=5 THEN 1 ELSE 0 END`)
          .bind(accountId, action.presetId, action.expectedRevision, operatorsJson, accountId),
      ];
      if (action.kind === "SAVE") {
        writes.push(db.prepare(`INSERT INTO commander_presets(id,account_id,name,preset_json,revision,updated_at) VALUES(?,?,?,?,?,?)
          ON CONFLICT(id) DO UPDATE SET name=excluded.name,preset_json=excluded.preset_json,revision=excluded.revision,updated_at=excluded.updated_at
          WHERE commander_presets.account_id=excluded.account_id`)
          .bind(action.presetId, accountId, action.name, definitionJson, nextRevision, now));
        // A globally colliding foreign ID must not yield a successful no-op save.
        writes.push(db.prepare(`INSERT INTO formation_write_guard(valid) SELECT CASE WHEN EXISTS(
          SELECT 1 FROM commander_presets WHERE id=? AND account_id=? AND revision=? AND preset_json=?) THEN 1 ELSE 0 END`)
          .bind(action.presetId, accountId, nextRevision, definitionJson));
      } else {
        writes.push(db.prepare(`INSERT INTO formation_write_guard(valid) SELECT CASE WHEN
          COALESCE((SELECT revision FROM account_formation WHERE account_id=?),0)=?
          AND NOT EXISTS(${openRun}) THEN 1 ELSE 0 END`).bind(accountId, action.expectedFormationRevision, accountId));
        if (!unchanged) writes.push(db.prepare(`INSERT INTO account_formation(account_id,preset_id,preset_revision,formation_json,revision,updated_at) VALUES(?,?,?,?,?,?)
          ON CONFLICT(account_id) DO UPDATE SET preset_id=excluded.preset_id,preset_revision=excluded.preset_revision,
          formation_json=excluded.formation_json,revision=excluded.revision,updated_at=excluded.updated_at`)
          .bind(accountId, action.presetId, action.expectedRevision, definitionJson, nextRevision, now));
      }
      writes.push(db.prepare("DELETE FROM formation_write_guard"),
        db.prepare("INSERT INTO formation_receipts(id,account_id,idempotency_key,request_hash,mutation_id,response_json,created_at) VALUES(?,?,?,?,?,?,?)")
          .bind(receiptId, accountId, key, hash, mutationId, JSON.stringify(response), now));
      try { await db.batch(writes); return response; }
      catch (error) {
        if (/UNIQUE constraint failed|CHECK constraint failed: valid=1|formation receipt immutable/iu.test(String(error))) {
          if (attempt < 3) continue;
          throw new ActivityError(409, "formation_write_conflict");
        }
        throw error;
      }
    } catch (error) {
      if (error instanceof ActivityError) {
        const completed = await replay();
        if (completed) return completed;
      }
      throw error;
    }
  }
  throw new ActivityError(409, "formation_write_conflict");
}

export function registerFormationRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  const handler = (kind: "SAVE" | "ACTIVATE") => async (c: Context<{ Bindings: Env }>) => {
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      const presetId = c.req.param("presetId"), key = c.req.header("Idempotency-Key") ?? "";
      if (new URL(c.req.url).search || !activityId.safeParse(presetId).success || !/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "contract_violation");
      let body: unknown;
      try { body = JSON.parse(await readBoundedBody(c)); } catch { throw new ActivityError(400, "contract_violation"); }
      if (kind === "SAVE") {
        const parsed = saveBody.safeParse(body);
        if (!parsed.success) throw new ActivityError(400, "contract_violation");
        return c.json(await mutateFormation(c.env.DB, auth.accountId, { ...parsed.data, kind, presetId: presetId! }, key));
      }
      const parsed = activateBody.safeParse(body);
      if (!parsed.success) throw new ActivityError(400, "contract_violation");
      return c.json(await mutateFormation(c.env.DB, auth.accountId, { ...parsed.data, kind, presetId: presetId! }, key));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  };
  app.post("/v1/formation/presets/:presetId/save", handler("SAVE"));
  app.post("/v1/formation/presets/:presetId/activate", handler("ACTIVATE"));
}
