import type { Context, Hono } from "hono";
import { z } from "zod";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError, activityId } from "./activity-policy";

// No approved operation-content binding or combat/stat/ultimate protocol exists
// in the inspected server source. These are explicit unavailable capabilities,
// not a catalog containing invented defaults, nor a client-controlled flag.
export function describeCombatRequirements(previewJson: string, risk: string) {
  let source: Record<string, unknown>;
  try {
    const parsed: unknown = JSON.parse(previewJson);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error("invalid source");
    source = parsed as Record<string, unknown>;
  } catch { throw new ActivityError(503, "combat_requirement_source_invalid"); }
  const text = (key: string) => typeof source[key] === "string" && source[key] !== "" ? source[key] as string : null;
  const riskIndex = ["I", "II", "III", "IV", "V"].indexOf(risk);
  return {
    version: "run-combat-requirements-v1", status: "BLOCKED", canStart: false,
    blockingReasons: ["enemy_faction_binding_missing", "wave_plan_binding_missing", "reward_table_binding_missing",
      "combat_seed_contract_missing", "engine_output_profile_unapproved", "ultimate_selection_contract_missing", "effective_stats_profile_unapproved"],
    riskTier: riskIndex >= 0 ? risk : null, riskLevel: riskIndex >= 0 ? riskIndex + 1 : null,
    operatorSlotCount: source.allowedOperatorSlots === 5 ? 5 : null,
    firstClearEligible: typeof source.firstClearEligible === "boolean" ? source.firstClearEligible : null,
    enemyFactionId: null, wavePlanId: null, rewardTableId: null, seed: null, engineOutput: null, ultimateOperatorId: null,
    requiredContent: {
      operationId: text("operationId"), contentVersion: text("contentVersion"),
      startAugmentPackageRef: text("startAugmentPackageRef"), startTacticPackageRef: text("startTacticPackageRef"),
      encounterType: text("encounterType"), raidId: text("raidId"), bossId: text("bossId"),
      bossPhaseRefs: Array.isArray(source.bossPhaseRefs) ? source.bossPhaseRefs.filter((v): v is string => typeof v === "string" && v.length > 0) : [],
      bindingVersion: null, catalogHash: null,
    },
  };
}

type RecoveryRow = { id: string; preview_id: string | null; status: string; started_at: number; snapshot_json: string | null };
const startLookupBody = z.strictObject({ startIdempotencyKey: z.string().regex(/^[\x21-\x7e]{1,128}$/u), operationPreviewId: activityId });
export function registerRunRecoveryRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>,
  restore: (row: RecoveryRow) => Record<string, unknown> | null) {
  // POST transports the original key in a bounded body, never a URL or echoed
  // lookup header. This route only SELECTs; it does not replay the start command.
  app.post("/v1/runs/start-result", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    const empty = (lookupStatus: string, errorCode: string | null, accepted = false) => ({ accepted, lookupStatus, runId: null, runStatus: null, startResponse: null, errorCode });
    let body: z.infer<typeof startLookupBody>;
    try {
      if (new URL(c.req.url).search) throw new Error("query forbidden");
      body = startLookupBody.parse(JSON.parse(await readBoundedBody(c)));
    } catch { return c.json(empty("INVALID_REQUEST", "contract_violation"), 400); }
    try {
      // Keep the receipt when the run/snapshot is unavailable, and account-scope
      // the JOIN too so a corrupt foreign result_ref cannot reveal another run.
      const row = await c.env.DB.prepare(`SELECT r.id,r.preview_id,r.status,r.started_at,s.snapshot_json
        FROM idempotency_records i
        LEFT JOIN runs r ON r.id=i.result_ref AND r.account_id=i.account_id
        LEFT JOIN run_snapshots s ON s.run_id=r.id
        WHERE i.account_id=? AND i.operation='run_start' AND i.idempotency_key=?`)
        .bind(auth.accountId, body.startIdempotencyKey).first<Omit<RecoveryRow, "id" | "status" | "started_at"> & { id: string | null; status: string | null; started_at: number | null }>();
      if (!row) return c.json(empty("NOT_FOUND", null, true));
      if (row.id && row.preview_id !== null && row.preview_id !== body.operationPreviewId) return c.json(empty("CONFLICT", "start_preview_mismatch"), 409);
      const response = row.id && row.status && row.started_at !== null && row.preview_id === body.operationPreviewId
        ? restore({ ...row, id: row.id, status: row.status, started_at: row.started_at }) : null;
      const startResponse = response?.runId === row.id && response?.operationPreviewId === body.operationPreviewId ? response : null;
      return c.json({ accepted: true, lookupStatus: startResponse ? "COMMITTED" : "SNAPSHOT_UNAVAILABLE",
        runId: row.id, runStatus: row.status, startResponse, errorCode: null });
    } catch {
      // Do not log exception text, SQL binds, original key or request body. A
      // transport/database failure is not proof that the start never committed.
      return c.json(empty("TEMPORARILY_UNAVAILABLE", "start_lookup_unavailable"), 503);
    }
  });
  app.get("/v1/runs/:runId/start-state", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      const id = c.req.param("runId");
      if (!activityId.safeParse(id).success || new URL(c.req.url).search || (await readBoundedBody(c)).length) throw new ActivityError(400, "contract_violation");
      const row = await c.env.DB.prepare(`SELECT r.id,r.preview_id,r.status,r.started_at,s.snapshot_json
        FROM runs r LEFT JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?`).bind(id, auth.accountId).first<RecoveryRow>();
      if (!row) return c.json({ accepted: false, errorCode: "run_not_found" }, 404);
      const startResponse = restore(row);
      return c.json({ accepted: true, runId: row.id, runStatus: row.status,
        recoveryStatus: startResponse ? "AVAILABLE" : "SNAPSHOT_UNAVAILABLE", startResponse });
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  });
}
