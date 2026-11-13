import { z } from "zod";
import { maintenanceContextSchema, maintenanceImmediateCommandSchema, type MaintenanceContext } from "../../../packages/common/src/maintenance-runtime-policy";
import { MaintenancePlanError, maintenanceDocumentSchema, type MaintenancePrincipal } from "../../../packages/common/src/maintenance-plan-policy";
import { maintenanceLoginReadStatement } from "../../game-api/src/maintenance-admission";

const revision = z.number().int().positive().safe();
const time = z.number().int().nonnegative().safe();
const commandSchema = maintenanceImmediateCommandSchema;
const rowSchema = z.object({ revision, stored_state: z.enum(["NORMAL", "SCHEDULED", "ACTIVE", "COMPLETED", "CANCELLED", "IMMEDIATE"]),
  document_json: z.string().nullable(), maintenance_id: z.string().nullable(), draft_revision: revision.nullable(), checked_at: time,
  game_code: z.enum(["OK", "BLOCKED"]), web_code: z.enum(["OK", "BLOCKED"]),
  immediate_game_mode: z.enum(["NORMAL", "DRAINING", "MAINTENANCE"]).nullable(), immediate_web_mode: z.literal("NORMAL").nullable(),
  estimated_end_at: time.nullable() });

/** Read/review boundary only. A valid request is not an applied policy, a saved
 * draft, an idempotency receipt, or permission to synthesize a scheduled plan.
 * The runtime supports immediate GAME modes with WEB NORMAL only. */
export class MaintenanceImmediateReview {
  private readonly context: MaintenanceContext;
  constructor(private readonly db: D1Database, context: MaintenanceContext) { this.context = maintenanceContextSchema.parse(context); }
  private async authorize(actor: MaintenancePrincipal, owner = false) {
    if (owner && actor.role !== "OWNER") throw new MaintenancePlanError(403, "ADMIN_PERMISSION_DENIED");
    const row = await this.db.prepare(`SELECT role FROM admin_principals WHERE id=? AND status='ACTIVE'
      AND role IN (${owner ? "'OWNER'" : "'OWNER','OPERATOR','VIEWER'"})`).bind(actor.id).first<{ role: string }>();
    if (!row) throw new MaintenancePlanError(403, "ADMIN_PERMISSION_DENIED");
    return row.role;
  }
  async read(actor: MaintenancePrincipal) {
    await this.authorize(actor);
    try {
      const row = rowSchema.parse(await maintenanceLoginReadStatement(this.db, this.context).first());
      const document = row.document_json === null ? null : maintenanceDocumentSchema.parse(JSON.parse(row.document_json));
      const active = ["SCHEDULED", "ACTIVE"].includes(row.stored_state), gameTarget = document?.serviceScope !== "WEB";
      const gameMode = row.stored_state === "IMMEDIATE" ? row.immediate_game_mode! : !active || !gameTarget || !document ? "NORMAL" : row.checked_at >= document.startsAt ? "MAINTENANCE"
        : document.drainStartsAt !== null && row.checked_at >= document.drainStartsAt ? "DRAINING" : "NORMAL";
      const policyWebMode = active && document && document.serviceScope !== "GAME" && row.checked_at >= document.startsAt ? document.webMode! : "NORMAL";
      const role = await this.authorize(actor);
      return { contractVersion: "admin-maintenance-current-state-v2", ...this.context, serverTime: row.checked_at, policyRevision: row.revision,
        storedState: row.stored_state, gameMode, webMode: policyWebMode === "NORMAL" ? "NORMAL" : null, policyWebMode,
        webModeStatus: policyWebMode === "NORMAL" ? "NORMAL_POLICY" : "ENFORCEMENT_UNVERIFIED",
        gameLoginAllowed: row.game_code === "OK", webLoginAllowed: row.web_code === "OK",
        estimatedEndAt: row.estimated_end_at, automaticCompletion: false,
        activeReservation: row.stored_state === "SCHEDULED" || (row.stored_state === "ACTIVE" && document && row.checked_at < document.startsAt)
          ? { maintenanceId: row.maintenance_id!, draftRevision: row.draft_revision!,
          policyRevision: row.revision, startsAt: document!.startsAt, drainStartsAt: document!.drainStartsAt } : null,
        capabilities: { canReview: role === "OWNER", canApply: false, canApplyAllModes: false, canApplySupportedCombinations: role === "OWNER",
          supportedImmediateCombinations: ["NORMAL", "DRAINING", "MAINTENANCE"].map(gameMode => ({ gameMode, webMode: "NORMAL" })),
          canCancelReservationViaHttp: false,
          gameLoginControl: true, webLoginControl: true, existingWebModeControl: false } };
    } catch (error) {
      if (error instanceof MaintenancePlanError) throw error;
      throw new MaintenancePlanError(503, "maintenance_policy_unavailable");
    }
  }
  async review(actor: MaintenancePrincipal, raw: unknown) {
    await this.authorize(actor, true);
    const parsed = commandSchema.safeParse(raw);
    if (!parsed.success) throw new MaintenancePlanError(400, "contract_violation");
    const requested = parsed.data, current = await this.read(actor);
    await this.authorize(actor, true);
    if (current.policyRevision !== requested.expectedRevision) throw new MaintenancePlanError(409, "maintenance_revision_conflict");
    if (current.activeReservation) throw new MaintenancePlanError(409, "maintenance_schedule_conflict");
    if (requested.estimatedEndAt !== null && requested.estimatedEndAt <= current.serverTime) throw new MaintenancePlanError(400, "contract_violation");
    return { contractVersion: "admin-maintenance-immediate-review-v2", applied: false, canApply: requested.webMode === "NORMAL",
      current, requested, automaticCompletion: false,
      loginIfApplied: { gameLoginAllowed: requested.gameMode !== "MAINTENANCE",
        webLoginAllowed: requested.gameMode !== "MAINTENANCE" && requested.webMode !== "MAINTENANCE" },
      blockReasons: requested.webMode === "NORMAL" ? [] : ["web_existing_session_mode_enforcement_unavailable"] };
  }
}
