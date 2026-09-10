import type { Context, Hono } from "hono";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError } from "./activity-policy";
import { walletId } from "./crystal-wallet";
import { formationSelection } from "./formation";
import { ultimateSelection, type UltimateFormation } from "./ultimate-selection";
import { describeCombatRequirements } from "./combat-readiness";
import { parseRunRewardsConfig, type RunRiskTier } from "../../../packages/balance/src/index";
import { readTrainContext, parseSavedTrain, trainProgress, validateTrainSelection, trainOwnershipGuard } from "./train-loadout";

const operations = ["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop", "opn_04_scrapstar_show", "opn_05_night_boarders", "opn_06_broken_line", "raid_raider_king_engine"] as const;
const revision = z.number().int().min(1).max(Number.MAX_SAFE_INTEGER);
const requestSchema = z.strictObject({ operationId: z.enum(operations), risk: z.enum(["I", "II", "III", "IV", "V"]), expectedFormationRevision: revision, expectedLoadoutRevision: revision, expectedUltimateRevision: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER - 1) });
const version = "departure-v2";
type JsonValue = Parameters<typeof canonicalJson>[0];
type Preview = { id: string; account_id: string; objective: string; risk: string; balance_config_version: number; preview_json: string; status: string; expires_at: number; admission_version?: string | null };
type Formation = UltimateFormation;
// Selected ownership and persisted growth facts, not effective combat stats.
const entitiesSql = `SELECT json_object(
 'operators',json((SELECT json_group_array(json_object('operatorId',operator_id,'skillTraining',skill_training,'promotion',promotion,'revision',revision)) FROM
   (SELECT * FROM account_operators WHERE account_id=?1 AND operator_id IN (SELECT json_extract(value,'$.operatorId') FROM json_each((SELECT formation_json FROM account_formation WHERE account_id=?1),'$.operators')) ORDER BY operator_id))),
 'trainingCoreSlots',json((SELECT json_group_array(json_object('slot',slot_index,'operatorId',operator_id,'trainedLevel',trained_level,'revision',revision)) FROM
   (SELECT * FROM operator_training_core_slots WHERE account_id=?1 AND operator_id IN (SELECT json_extract(value,'$.operatorId') FROM json_each((SELECT formation_json FROM account_formation WHERE account_id=?1),'$.operators')) ORDER BY slot_index))),
 'train',(SELECT json_object('trainId',train_id,'level',level,'revision',revision) FROM account_train WHERE account_id=?1),
 'carriages',json((SELECT json_group_array(json_object('carriageId',carriage_id,'level',level,'revision',revision)) FROM
   (SELECT * FROM account_carriages WHERE account_id=?1 AND carriage_id IN (SELECT json_extract(value,'$.carriageId') FROM json_each((SELECT loadout_json FROM account_train_loadout WHERE account_id=?1),'$.carriages')) ORDER BY carriage_id)))
) AS value`;

async function state(db: D1Database, accountId: string, operationId: string, risk: string) {
  const train = await readTrainContext(db, accountId), selection = parseSavedTrain(train);
  if (!selection || !train.saved) throw new ActivityError(409, "train_loadout_required");
  if (train.openRun) throw new ActivityError(409, "departure_open_run");
  validateTrainSelection(train, selection);
  const progress = trainProgress(train);
  const order = operations.indexOf(operationId as typeof operations[number]);
  if (order < 0) throw new ActivityError(400, "contract_violation");
  if (!operations.slice(0, order).every(id => progress.cleared.includes(id))) throw new ActivityError(409, "operation_locked");
  const formation = await db.prepare("SELECT revision,formation_json,ultimate_operator_id,ultimate_revision FROM account_formation WHERE account_id=?").bind(accountId).first<Formation>();
  if (!formation) throw new ActivityError(409, "formation_required");
  let squad: z.infer<typeof formationSelection>;
  try { squad = formationSelection.parse(JSON.parse(formation.formation_json)); }
  catch { throw new ActivityError(409, "formation_schema_unsupported"); }
  const entities = (await db.prepare(entitiesSql).bind(accountId).first<{ value: string }>())!.value;
  if ((JSON.parse(entities) as { operators: unknown[] }).operators.length !== 5) throw new ActivityError(403, "formation_operator_not_owned");
  const balance = await db.prepare("SELECT b.version,b.config_json FROM balance_config_active a JOIN balance_config_versions b ON b.version=a.version WHERE a.singleton_id=1").first<{ version: number; config_json: string }>();
  let fuelCost: number;
  try { fuelCost = z.number().int().positive().max(2147483647).parse(JSON.parse(balance!.config_json).fuel.runStartCost); }
  catch { throw new ActivityError(503, "departure_cost_policy_unavailable"); }
  try {
    const rewards = parseRunRewardsConfig(JSON.parse(balance!.config_json).runRewards);
    if (!Object.hasOwn(rewards.operations, operationId) || !rewards.operationProgressionMultipliersBps[operationId] || !rewards.riskMultipliersBps[risk as RunRiskTier]) throw new Error("missing operation reward");
  } catch { throw new ActivityError(503, "departure_reward_policy_unavailable"); }
  const fuel = await db.prepare("SELECT fuel FROM fuel_state WHERE account_id=?").bind(accountId).first<{ fuel: number }>();
  if (!fuel) throw new ActivityError(409, "fuel_state_unavailable");
  if (fuel.fuel < fuelCost) throw new ActivityError(409, "insufficient_fuel");
  const firstClearEligible = !(await db.prepare("SELECT 1 FROM account_first_clears WHERE account_id=? AND operation_key=? AND risk=?").bind(accountId, operationId, risk).first());
  return { train, selection, formation, squad, ultimate: ultimateSelection(formation), entities, balance: balance!, fuelCost, fuel: fuel.fuel, operationId, risk, firstClearEligible };
}
type State = Awaited<ReturnType<typeof state>>;
function admission(s: State) {
  return { version, formationRevision: s.formation.revision, loadoutRevision: s.train.saved!.revision,
    formation: s.squad, ultimateSelection: s.ultimate, loadout: s.selection, ownedState: JSON.parse(s.entities) as JsonValue, fuelCost: s.fuelCost, firstClearEligible: s.firstClearEligible };
}
function guards(db: D1Database, accountId: string, s: State) {
  return [trainOwnershipGuard(db, accountId, s.selection, s.train.progress, s.train.saved!.revision),
    db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN
      EXISTS(SELECT 1 FROM account_formation WHERE account_id=?1 AND revision=?2 AND formation_json=?3 AND ultimate_revision=?11 AND ultimate_operator_id IS ?12)
      AND (${entitiesSql})=?4
      AND EXISTS(SELECT 1 FROM balance_config_active a JOIN balance_config_versions b ON b.version=a.version WHERE a.singleton_id=1 AND b.version=?5 AND b.config_json=?6)
      AND EXISTS(SELECT 1 FROM fuel_state WHERE account_id=?1 AND fuel=?7)
      AND (NOT EXISTS(SELECT 1 FROM account_first_clears WHERE account_id=?1 AND operation_key=?8 AND risk=?9))=?10
      THEN 1 ELSE 0 END`).bind(accountId, s.formation.revision, s.formation.formation_json, s.entities, s.balance.version, s.balance.config_json, s.fuel, s.operationId, s.risk, s.firstClearEligible ? 1 : 0, s.ultimate.revision, s.ultimate.operatorId)];
}
async function nextSequence(db: D1Database, accountId: string) {
  return (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?").bind(accountId).first<{ value: number }>())!.value;
}
const retryable = (error: unknown) => /UNIQUE constraint failed|CHECK constraint failed: valid=1|departure receipt immutable/iu.test(String(error));

async function issue(db: D1Database, accountId: string, body: z.infer<typeof requestSchema>, key: string) {
  const hash = await sha256Hex(canonicalJson(body));
  const replay = async () => {
    const row = await db.prepare("SELECT request_hash,response_json FROM departure_preview_receipts WHERE account_id=? AND idempotency_key=?").bind(accountId, key).first<{ request_hash: string; response_json: string }>();
    if (!row) return null;
    if (row.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
    return JSON.parse(row.response_json) as Record<string, unknown>;
  };
  for (let attempt = 0; attempt < 4; attempt++) {
    const previous = await replay(); if (previous) return previous;
    try {
      const sequence = await nextSequence(db, accountId), s = await state(db, accountId, body.operationId, body.risk);
      if (s.formation.revision !== body.expectedFormationRevision || s.train.saved!.revision !== body.expectedLoadoutRevision || s.ultimate.revision !== body.expectedUltimateRevision) throw new ActivityError(409, "departure_selection_changed");
      const now = Date.now(), id = walletId("preview"), mutationId = walletId("departure_preview"), expiresAt = now + 300000;
      const raid = body.operationId === "raid_raider_king_engine";
      const metadata = { operationId: body.operationId, actId: "act_01_wasteland_railway", regionId: "region_wasteland_railway", encounterType: raid ? "RAID_BOSS" : "ORDINARY_15_WAVE",
        raidId: raid ? body.operationId : null, bossId: raid ? "boss_raider_king_engine" : null, bossPhaseRefs: raid ? ["crown_chase", "trophy_break", "engine_stop"] : [],
        allowedOperatorSlots: 5, startAugmentPackageRef: "act1_full_start_package_v1", startTacticPackageRef: "act1_full_start_package_v1", contentVersion: "act1.launch.content.v1", firstClearEligible: s.firstClearEligible };
      const a = admission(s);
      const combatContract = describeCombatRequirements(JSON.stringify({ ...metadata, admission: a }), body.risk);
      const response = { accepted: true, operationPreviewId: id, ...metadata, combatContract, risk: body.risk, expiresAt: new Date(expiresAt).toISOString(),
        formationRevision: s.formation.revision, loadoutRevision: s.train.saved!.revision, admission: a,
        quote: { fuelCost: s.fuelCost, availableFuel: s.fuel, balanceVersion: String(s.balance.version) } };
      await db.batch([
        db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'DEPARTURE_PREVIEW',?,?)").bind(mutationId, accountId, sequence, key, now),
        ...guards(db, accountId, s),
        db.prepare("INSERT INTO operation_previews(id,account_id,mutation_id,objective,risk,balance_config_version,preview_json,status,expires_at,created_at,admission_version) VALUES(?,?,?,?,?,?,?,'ACTIVE',?,?,?)")
          .bind(id, accountId, mutationId, body.operationId, body.risk, s.balance.version, JSON.stringify({ ...metadata, combatContract, admission: a, balanceConfigHash: await sha256Hex(s.balance.config_json) }), expiresAt, now, version),
        db.prepare("INSERT INTO departure_preview_receipts(account_id,idempotency_key,request_hash,preview_id,response_json) VALUES(?,?,?,?,?)").bind(accountId, key, hash, id, JSON.stringify(response)),
        db.prepare("DELETE FROM train_loadout_write_guard"), db.prepare("DELETE FROM departure_write_guard"),
      ]);
      return response;
    } catch (error) {
      const completed = await replay(); if (completed) return completed;
      if (retryable(error) && attempt < 3) continue;
      if (retryable(error)) throw new ActivityError(409, "departure_write_conflict");
      throw error;
    }
  }
  throw new ActivityError(409, "departure_write_conflict");
}

export async function startDeparture(db: D1Database, accountId: string, initial: Preview, key: string,
  createResponse: (row: Preview, runId: string, now: number) => Promise<Record<string, unknown>>) {
  if (initial.admission_version !== version) throw new ActivityError(409, "departure_preview_version_unsupported");
  const replay = async () => {
    const row = await db.prepare("SELECT r.preview_id,s.snapshot_json FROM idempotency_records i JOIN runs r ON r.id=i.result_ref JOIN run_snapshots s ON s.run_id=r.id WHERE i.account_id=? AND i.operation='run_start' AND i.idempotency_key=?")
      .bind(accountId, key).first<{ preview_id: string; snapshot_json: string }>();
    if (!row) return null;
    if (row.preview_id !== initial.id) throw new ActivityError(409, "idempotency_conflict");
    return (JSON.parse(row.snapshot_json) as { departureResponse: Record<string, unknown> }).departureResponse;
  };
  for (let attempt = 0; attempt < 4; attempt++) {
    const previous = await replay(); if (previous) return previous;
    try {
      const sequence = await nextSequence(db, accountId);
      const row = await db.prepare("SELECT * FROM operation_previews WHERE id=? AND account_id=?").bind(initial.id, accountId).first<Preview>();
      if (!row || row.status !== "ACTIVE" || row.expires_at <= Date.now()) throw new ActivityError(409, "operation_preview_not_startable");
      const s = await state(db, accountId, row.objective, row.risk);
      if (s.ultimate.operatorId === null) throw new ActivityError(409, "ultimate_selection_required");
      const a = admission(s), pinned = JSON.parse(row.preview_json) as { admission: JsonValue; balanceConfigHash: string };
      if (s.balance.version !== row.balance_config_version || await sha256Hex(s.balance.config_json) !== pinned.balanceConfigHash || canonicalJson(a) !== canonicalJson(pinned.admission)) throw new ActivityError(409, "departure_quote_changed");
      const now = Date.now(), runId = walletId("run"), mutationId = walletId("departure_start");
      const response = { ...await createResponse(row, runId, now), admission: a, ultimateOperatorId: s.ultimate.operatorId, firstClearEligible: s.firstClearEligible };
      await db.batch([
        db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'RUN_START',?,?)").bind(mutationId, accountId, sequence, key, now),
        ...guards(db, accountId, s),
        db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM operation_previews WHERE id=? AND account_id=? AND status='ACTIVE' AND expires_at>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) AND preview_json=? AND balance_config_version=? AND admission_version=? AND objective=? AND risk=?) THEN 1 ELSE 0 END`)
          .bind(row.id, accountId, row.preview_json, row.balance_config_version, version, row.objective, row.risk),
        db.prepare("UPDATE operation_previews SET status='CONSUMED' WHERE id=? AND account_id=?").bind(row.id, accountId),
        db.prepare("UPDATE fuel_state SET fuel=fuel-?,updated_at=? WHERE account_id=?").bind(s.fuelCost, now, accountId),
        db.prepare("INSERT INTO fuel_ledger(id,account_id,mutation_id,delta,fuel_after,reason,created_at) VALUES(?,?,?,?,?,'RUN_START',?)").bind(walletId("fuel"), accountId, mutationId, -s.fuelCost, s.fuel - s.fuelCost, now),
        db.prepare("INSERT INTO runs(id,account_id,preview_id,mutation_id,balance_config_version,status,started_at) VALUES(?,?,?,?,?,'ACTIVE',?)").bind(runId, accountId, row.id, mutationId, row.balance_config_version, now),
        db.prepare("INSERT INTO run_snapshots(run_id,snapshot_json,created_at) VALUES(?,?,?)").bind(runId, JSON.stringify({ ...response, departureResponse: response }), now),
        db.prepare("INSERT INTO idempotency_records(id,account_id,idempotency_key,operation,result_ref,created_at) VALUES(?,?,?,'run_start',?,?)").bind(walletId("idem"), accountId, key, runId, now),
        db.prepare("DELETE FROM train_loadout_write_guard"), db.prepare("DELETE FROM departure_write_guard"),
      ]);
      return response;
    } catch (error) {
      const completed = await replay(); if (completed) return completed;
      if (retryable(error) && attempt < 3) continue;
      if (error instanceof ActivityError) throw error;
      throw new ActivityError(409, "run_start_state_conflict");
    }
  }
  throw new ActivityError(409, "run_start_state_conflict");
}

export function registerDepartureRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  app.post("/v1/operations/preview", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      const key = c.req.header("Idempotency-Key") ?? "";
      if (new URL(c.req.url).search || !/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "contract_violation");
      let raw: unknown;
      try { raw = JSON.parse(await readBoundedBody(c)); } catch { throw new ActivityError(400, "contract_violation"); }
      const parsed = requestSchema.safeParse(raw);
      if (!parsed.success) throw new ActivityError(400, "contract_violation");
      return c.json(await issue(c.env.DB, auth.accountId, parsed.data, key));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  });
}
