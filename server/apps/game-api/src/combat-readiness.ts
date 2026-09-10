import type { Context, Hono } from "hono";
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
export function registerRunRecoveryRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>,
  restore: (row: RecoveryRow) => Record<string, unknown> | null) {
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
