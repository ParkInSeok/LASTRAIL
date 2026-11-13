import {describeBDepartureCombatPinRequirements} from './combat-b-departure-combat-pin';
import { readCombatAuthority } from "./combat-authority";
import { bindCombatPolicyRequirements } from "./combat-policy-binding";
import type { Context, Hono } from "hono";
import { z } from "zod";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError, activityId } from "./activity-policy";
import {projectAugmentReconnectState} from './augment-reconnect-state';
import { projectOrdinaryInitialization } from './combat-ordinary-combat-lifecycle';

// Latest policy requirements are bound separately from execution capabilities.
// Catalog presence or a client flag never grants permission to start combat.
export function describeCombatRequirements(previewJson: string, risk: string) {
  let source: Record<string, unknown>;
  try {
    const parsed: unknown = JSON.parse(previewJson);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error("invalid source");
    source = parsed as Record<string, unknown>;
  } catch { throw new ActivityError(503, "combat_requirement_source_invalid"); }
  const text = (key: string) => typeof source[key] === "string" && source[key] !== "" ? source[key] as string : null;
  const riskIndex = ["I", "II", "III", "IV", "V"].indexOf(risk);
  const pinned = z.object({ version: z.enum(["departure-v3", "departure-v4"]), ultimateSelection: z.object({ rulesVersion: z.literal("ultimate-selection-v1"), operatorId: activityId.nullable(), revision: z.number().int().nonnegative() }), formation: z.object({ operators: z.array(z.object({ operatorId: activityId })).length(5) }) }).safeParse(source.admission);
  const ultimate = pinned.success && pinned.data.ultimateSelection.operatorId !== null && pinned.data.formation.operators.some(o => o.operatorId === pinned.data.ultimateSelection.operatorId) ? pinned.data.ultimateSelection : null;
  const policyBinding = bindCombatPolicyRequirements(source.admission, risk);
  const authoritySource=z.object({combatAuthority:z.unknown().optional()}).safeParse(source.admission);
  const authority=readCombatAuthority(authoritySource.success?authoritySource.data.combatAuthority:undefined);
  return {
    bDepartureCombatPin:describeBDepartureCombatPinRequirements(), authority, policyBinding, version: "run-combat-requirements-v3", status: "BLOCKED", canStart: false,
    blockingReasons: [...(authority.status==="INVALID"?["combat_authority_contract_invalid"]:[]), "enemy_faction_binding_missing", "wave_plan_binding_missing", "reward_table_binding_missing",
      "combat_seed_contract_missing", "engine_output_profile_unapproved", ...(!ultimate && policyBinding.status === "HISTORICAL" ? ["ultimate_selection_required"] : []), ...policyBinding.blockingReasons, "effective_stats_profile_unapproved"],
    riskTier: riskIndex >= 0 ? risk : null, riskLevel: riskIndex >= 0 ? riskIndex + 1 : null,
    operatorSlotCount: source.allowedOperatorSlots === 5 ? 5 : null,
    firstClearEligible: typeof source.firstClearEligible === "boolean" ? source.firstClearEligible : null,
    enemyFactionId: null, wavePlanId: null, rewardTableId: null, seed: null, engineOutput: null, ultimateOperatorId: ultimate?.operatorId ?? null,
    ultimateSelectionRevision: ultimate?.revision ?? (pinned.success ? pinned.data.ultimateSelection.revision : null),
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
type AugmentRecoveryRow = {
 map_revision:number|null;map_plan_json:string|null;map_state_json:string|null;map_start_snapshot:string|null;
 grant_run_id:string|null;grant_event_id:string|null;grant_stage_id:string|null;grant_account_id:string|null;grant_environment:string|null;
 grant_checkpoint_json:string|null;grant_initial_json:string|null;grant_issue_hash:string|null;grant_start_snapshot:string|null;grant_write_revision:number|null;
 selection_request_id:string|null;selection_request_hash:string|null;selection_response_json:string|null;application_json:string|null;application_hash:string|null;
};
const startLookupBody = z.strictObject({ startIdempotencyKey: z.string().regex(/^[\x21-\x7e]{1,128}$/u), operationPreviewId: activityId });
export function registerRunRecoveryRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>,
  restore: (row: RecoveryRow) => Record<string, unknown> | null) {
  app.get("/v1/runs/:runId/lifecycle", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c); if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    const id = c.req.param("runId");
    if (!activityId.safeParse(id).success || new URL(c.req.url).search || (await readBoundedBody(c)).length) return c.json({ accepted: false, errorCode: "contract_violation" }, 400);
    try {
    const row = await c.env.DB.prepare(`SELECT r.id,r.preview_id,r.status,r.started_at,s.snapshot_json,
      f.outcome,f.submitted_at,p.id AS reward_id,p.status AS reward_status,p.expires_at,p.reward_claim_id,t.reward_json,t.created_at AS reward_claimed_at,
      CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER) AS now,
      m.revision AS map_revision,m.plan_json AS map_plan_json,m.state_json AS map_state_json,m.start_snapshot_json AS map_start_snapshot,
      g.run_id AS grant_run_id,g.event_id AS grant_event_id,g.stage_id AS grant_stage_id,g.account_id AS grant_account_id,g.environment AS grant_environment,
      g.checkpoint_json AS grant_checkpoint_json,g.initial_json AS grant_initial_json,g.issue_hash AS grant_issue_hash,g.start_snapshot_json AS grant_start_snapshot,g.write_revision AS grant_write_revision,
      ar.request_id AS selection_request_id,ar.request_hash AS selection_request_hash,ar.response_json AS selection_response_json,
      ao.original_json AS application_json,ao.original_hash AS application_hash,
      oc.account_id AS ordinary_account_id,oc.phase AS ordinary_phase,oc.revision AS ordinary_revision,
      oc.origin_hash AS ordinary_origin_hash,oc.start_snapshot_json AS ordinary_start_snapshot
      FROM runs r LEFT JOIN run_snapshots s ON s.run_id=r.id LEFT JOIN run_final_results f ON f.run_id=r.id
      LEFT JOIN run_reward_previews p ON p.id=(SELECT id FROM run_reward_previews WHERE run_id=r.id AND account_id=r.account_id ORDER BY created_at DESC,id DESC LIMIT 1)
      LEFT JOIN reward_transactions t ON t.id=p.reward_claim_id AND t.account_id=r.account_id AND t.run_id=r.id
      LEFT JOIN operation_map_states m ON m.run_id=r.id
      LEFT JOIN ordinary_combat_phases oc ON oc.run_id=r.id
      LEFT JOIN solo_augment_grants g ON g.run_id=r.id AND g.stage_id=(
        SELECT json_extract(CASE WHEN e.type='object' THEN e.value ELSE '{}' END,'$.toNodeId') FROM json_each(m.plan_json,'$.edges') e
        WHERE json_extract(CASE WHEN e.type='object' THEN e.value ELSE '{}' END,'$.edgeId')=json_extract(m.state_json,'$.traversedEdgeIds[#-1]') LIMIT 1)
      LEFT JOIN solo_augment_receipts ar ON ar.run_id=g.run_id AND ar.event_id=g.event_id AND ar.request_id=(
        SELECT json_extract(CASE WHEN j.type='object' THEN j.value ELSE '{}' END,'$.key') FROM json_each(g.checkpoint_json,'$.receipts') j
        WHERE json_extract(CASE WHEN j.type='object' THEN j.value ELSE '{}' END,'$.result.outcome')='SELECTED' LIMIT 1)
      LEFT JOIN run_augment_effect_originals ao ON ao.run_id=g.run_id AND ao.grant_event_id=g.event_id AND ao.effect_id='O22'
      WHERE r.id=? AND r.account_id=?`).bind(id, auth.accountId).first<RecoveryRow & {
        outcome: string | null; submitted_at: number | null; reward_id: string | null; reward_status: string | null;
        expires_at: number | null; reward_claim_id: string | null; reward_json: string | null; reward_claimed_at: number | null; now: number;
        ordinary_account_id: string | null; ordinary_phase: string | null; ordinary_revision: number | null;
        ordinary_origin_hash: string | null; ordinary_start_snapshot: string | null } & AugmentRecoveryRow>();
    if (!row) return c.json({ accepted: false, errorCode: "run_not_found" }, 404);
    const startResponse = restore(row);
    const ordinaryInitialization = await projectOrdinaryInitialization(auth.accountId, row.id, row.snapshot_json,
      row.ordinary_phase === null ? null : { accountId: row.ordinary_account_id!, phase: row.ordinary_phase,
        revision: row.ordinary_revision!, originHash: row.ordinary_origin_hash!, startSnapshotJson: row.ordinary_start_snapshot! });
    let receipt: unknown = null;
    try {
      const rewardPlan: unknown = row.reward_json ? JSON.parse(row.reward_json) : null;
      if (rewardPlan && typeof rewardPlan === "object" && !Array.isArray(rewardPlan) && Array.isArray((rewardPlan as { rewardLines?: unknown }).rewardLines)) {
        receipt = { rewardTransactionId: row.reward_claim_id, runId: row.id, serverClaimedAt: row.reward_claimed_at, rewardPlan };
      }
    } catch { /* Keep missing/corrupt receipt explicitly unavailable. */ }
    const rewardStatus = !row.reward_id ? "NOT_AVAILABLE" : row.reward_status === "CLAIMED" ? receipt ? "CLAIMED" : "UNAVAILABLE"
      : row.reward_status === "EXPIRED" || (row.reward_status === "ACTIVE" && row.expires_at! <= row.now) ? "EXPIRED"
      : row.reward_status === "ACTIVE" ? "CLAIMABLE" : "UNAVAILABLE";
    const terminal = ["COMPLETED", "FAILED", "ABANDONED"].includes(row.status) && row.outcome !== null;
    // All mutable recovery facts are read in the same SQL snapshot. This adds no
    // request, re-grant or runtime apply; the original local checkpoint owner is
    // still responsible for its pending command, APPLIED and hold lifecycle.
    const augmentRecovery=await projectAugmentReconnectState({
      accountId:auth.accountId,environment:c.env.ENVIRONMENT,runId:row.id,runStatus:row.status,startSnapshot:row.snapshot_json,
      map:row.map_revision===null?null:{revision:row.map_revision,planJson:row.map_plan_json!,stateJson:row.map_state_json!,startSnapshot:row.map_start_snapshot!},
      grant:row.grant_event_id===null?null:{accountId:row.grant_account_id!,environment:row.grant_environment!,runId:row.grant_run_id!,eventId:row.grant_event_id,stageId:row.grant_stage_id!,checkpointJson:row.grant_checkpoint_json!,initialJson:row.grant_initial_json!,issueHash:row.grant_issue_hash!,startSnapshot:row.grant_start_snapshot!,writeRevision:row.grant_write_revision!},
      receipt:row.selection_request_id===null?null:{requestId:row.selection_request_id,responseJson:row.selection_response_json!,requestHash:row.selection_request_hash!},
      applicationOriginal:row.application_json===null?null:{originalJson:row.application_json,originalHash:row.application_hash!}
    });
    const nextAction = !startResponse || row.status === "QUARANTINED" || augmentRecovery.state==='REVIEW_REQUIRED' ? "REVIEW_REQUIRED"
      : terminal && rewardStatus === "CLAIMED" ? "RETURN_TO_LOBBY" : !row.outcome && !row.reward_id && row.status === "ACTIVE" ? "RECOVER_COMBAT_CONSUMER"
      : !row.outcome && !row.reward_id && row.status === "FINISH_PENDING" ? "RESUME_FINISH" : terminal && rewardStatus === "CLAIMABLE" ? "REWARD_REVIEW" : "REVIEW_REQUIRED";
    return c.json({ accepted: true, version: "run-lifecycle-v1", serverTime: row.now, runId: row.id, runStatus: row.status, startResponse,
      finish: { status: row.outcome ? "RECORDED" : "NOT_RECORDED", outcome: row.outcome, submittedAt: row.submitted_at, rewardPreviewId: row.reward_id },
      reward: { status: rewardStatus, rewardPreviewId: row.reward_id, expiresAt: row.expires_at, claimId: row.reward_claim_id, receipt },
      nextAction, augmentRecovery, ordinaryInitialization, grantsOnRead: false, newAttemptAuthorized: false, uiReturnAllowed: true,
      disposition: rewardStatus === "CLAIMED" ? "REWARDED" : rewardStatus === "EXPIRED" ? "EXPIRED_UNCLAIMED" : nextAction === "REVIEW_REQUIRED" ? "REVIEW_REQUIRED" : "PENDING" });
    } catch {
      // A failed read cannot establish termination, lost rights, or a new attempt.
      // Keep a diagnostic code without logging SQL, stored receipts, or error text.
      console.error("run_lifecycle_unavailable");
      return c.json({ accepted: false, errorCode: "run_lifecycle_unavailable" }, 503);
    }
  });
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
      console.error("run_start_state_unavailable");
      return c.json({ accepted: false, errorCode: "run_start_state_unavailable" }, 503);
    }
  });
}
