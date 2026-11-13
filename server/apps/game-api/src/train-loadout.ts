import type { Context, Hono } from "hono";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError, activityId } from "./activity-policy";
import { walletId } from "./crystal-wallet";

export const trainLoadoutRulesVersion = "train-loadout-v1";
const water = "opn_03_water_stop", night = "opn_05_night_boarders";
const types: Record<string, string> = { car_combat: "COMBAT", car_gun: "GUN", car_cargo: "CARGO", car_defense: "DEFENSE", car_medical: "MEDICAL" };
export function resolveCarriageType(id: string): string | null {
  return Object.hasOwn(types, id) ? types[id]! : null;
}
const slots = z.array(z.strictObject({ slot: z.number().int().min(0).max(3), carriageId: activityId })).min(2).max(4)
  .refine(rows => new Set(rows.map(r => r.carriageId)).size === rows.length && [...rows].sort((a, b) => a.slot - b.slot).every((r, i) => r.slot === i));
export const trainSelection = z.strictObject({ rulesVersion: z.literal(trainLoadoutRulesVersion), trainId: activityId, carriages: slots });
const changeBody = trainSelection.extend({ expectedRevision: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER - 1),responseVersion:z.literal('train-loadout.command.v2.20261021').optional() });
type Selection = z.infer<typeof trainSelection>;
type OwnedCarriage = { carriageId: string; level: number; revision: number };
export type TrainContext = {
  train: { trainId: string; level: number; revision: number } | null;
  carriages: OwnedCarriage[];
  turrets: { turretId: string; level: number; revision: number }[];
  saved: { definition: string; revision: number; updatedAt: number } | null;
  progress: string | null;
  openRun: number;
};
export const openTrainRunSql = "SELECT 1 FROM active_combat_runs WHERE account_id=? AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED')";
const ownedCountSql = `SELECT COUNT(*) FROM json_each(?) j JOIN account_carriages c
  ON c.account_id=? AND c.carriage_id=json_extract(j.value,'$.carriageId')`;

// One SQLite statement makes inventory, milestones and selection a consistent view.
export async function readTrainContext(db: D1Database, accountId: string): Promise<TrainContext> {
  const row = await db.prepare(`SELECT json_object(
    'train',(SELECT json_object('trainId',train_id,'level',level,'revision',revision) FROM account_train WHERE account_id=?1),
    'carriages',json((SELECT json_group_array(json_object('carriageId',carriage_id,'level',level,'revision',revision)) FROM (SELECT * FROM account_carriages WHERE account_id=?1 ORDER BY carriage_id))),
    'turrets',json((SELECT json_group_array(json_object('turretId',turret_id,'level',level,'revision',revision)) FROM (SELECT * FROM account_turrets WHERE account_id=?1 ORDER BY turret_id))),
    'saved',(SELECT json_object('definition',loadout_json,'revision',revision,'updatedAt',updated_at) FROM account_train_loadout WHERE account_id=?1),
    'progress',(SELECT cleared_operations_json FROM account_story_progress WHERE account_id=?1),
    'openRun',EXISTS(SELECT 1 FROM active_combat_runs WHERE account_id=?1 AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED'))
  ) AS value`).bind(accountId).first<{ value: string }>();
  return JSON.parse(row!.value) as TrainContext;
}

export function trainProgress(context: TrainContext) {
  let cleared: string[] = [];
  try {
    if (context.progress !== null) cleared = z.array(z.string()).parse(JSON.parse(context.progress));
  } catch { return { cleared, unlockedSlotCount: null, progressError: "story_progress_invalid" }; }
  if (cleared.includes(night) && !cleared.includes(water)) return { cleared, unlockedSlotCount: null, progressError: "story_progress_inconsistent" };
  return { cleared, unlockedSlotCount: cleared.includes(night) ? 4 : cleared.includes(water) ? 3 : 2, progressError: null };
}

function capability(id: string, progress: ReturnType<typeof trainProgress>) {
  const type = resolveCarriageType(id);
  const supported = type !== null && id !== "car_medical";
  const reason = !supported ? (id === "car_medical" ? "medical_unlock_unapproved" : "carriage_definition_unsupported")
    : progress.progressError ?? ((id === "car_cargo" && !progress.cleared.includes(water)) || (id === "car_defense" && !progress.cleared.includes(night)) ? "story_locked" : null);
  return { type, installationSupported: supported, installable: reason === null, reason };
}

export function validateTrainSelection(context: TrainContext, selection: Selection) {
  if (context.train?.trainId !== selection.trainId) throw new ActivityError(403, "train_not_owned");
  const progress = trainProgress(context);
  if (progress.progressError) throw new ActivityError(409, progress.progressError);
  if (selection.carriages.length !== progress.unlockedSlotCount) throw new ActivityError(400, "train_slot_count_invalid");
  const counts: Record<string, number> = {};
  for (const row of selection.carriages) {
    if (!context.carriages.some(c => c.carriageId === row.carriageId)) throw new ActivityError(403, "carriage_not_owned");
    const eligible = capability(row.carriageId, progress);
    if (eligible.reason) throw new ActivityError(409, eligible.reason);
    counts[eligible.type!] = (counts[eligible.type!] ?? 0) + 1;
  }
  if (counts.COMBAT !== 1 || (counts.CARGO ?? 0) > 1 || ["GUN", "MEDICAL", "DEFENSE"].some(type => (counts[type] ?? 0) > 2)) throw new ActivityError(409, "train_type_limit_invalid");
}

export function parseSavedTrain(context: TrainContext): Selection | null {
  if (!context.saved) return null;
  try { return trainSelection.parse(JSON.parse(context.saved.definition)); }
  catch { throw new ActivityError(409, "train_loadout_schema_unsupported"); }
}

function view(context: TrainContext) {
  const progress = trainProgress(context);
  let loadout: Selection | null = null, loadoutError: string | null = null;
  try {
    loadout = parseSavedTrain(context);
    if (loadout) validateTrainSelection(context, loadout);
  } catch (error) {
    if (!(error instanceof ActivityError)) throw error;
    loadoutError = error.code;
  }
  return { rulesVersion: trainLoadoutRulesVersion, train: context.train,
    ownedCarriages: context.carriages.map(c => ({ ...c, ...capability(c.carriageId, progress) })),
    ownedTurrets: context.turrets.map(t => ({ ...t, installationSupported: false, reason: "turret_installation_undefined" })),
    unlockedSlotCount: progress.unlockedSlotCount, progressError: progress.progressError,
    typeLimits: { COMBAT: { min: 1, max: 1 }, CARGO: { min: 0, max: 1 }, GUN: { min: 0, max: 2 }, MEDICAL: { min: 0, max: 2 }, DEFENSE: { min: 0, max: 2 } },
    revision: context.saved?.revision ?? 0, loadout, loadoutError, openRun: Boolean(context.openRun),
    updatedAt: context.saved ? new Date(context.saved.updatedAt).toISOString() : null };
}

export function trainOwnershipGuard(db: D1Database, accountId: string, selection: Selection, progress: string | null, expectedRevision: number) {
  return db.prepare(`INSERT INTO train_loadout_write_guard(valid) SELECT CASE WHEN
    EXISTS(SELECT 1 FROM account_train WHERE account_id=? AND train_id=?)
    AND (${ownedCountSql})=?
    AND (SELECT cleared_operations_json FROM account_story_progress WHERE account_id=?) IS ?
    AND COALESCE((SELECT revision FROM account_train_loadout WHERE account_id=?),0)=?
    AND NOT EXISTS(${openTrainRunSql}) THEN 1 ELSE 0 END`)
    .bind(accountId, selection.trainId, JSON.stringify(selection.carriages), accountId, selection.carriages.length,
      accountId, progress, accountId, expectedRevision, accountId);
}

async function change(db: D1Database, accountId: string, body: z.infer<typeof changeBody>, key: string) {
  const selection: Selection = { rulesVersion: body.rulesVersion, trainId: body.trainId, carriages: [...body.carriages].sort((a, b) => a.slot - b.slot) };
  const definition = canonicalJson(selection), hash = await sha256Hex(canonicalJson({ ...selection, expectedRevision: body.expectedRevision,...(body.responseVersion?{responseVersion:body.responseVersion}:{}) }));
  const replay = async () => {
    const receipt = await db.prepare("SELECT request_hash,response_json FROM train_loadout_receipts WHERE account_id=? AND idempotency_key=?")
      .bind(accountId, key).first<{ request_hash: string; response_json: string }>();
    if (!receipt) return null;
    if (receipt.request_hash !== hash) throw new ActivityError(409, "idempotency_conflict");
    return JSON.parse(receipt.response_json) as Record<string, unknown>;
  };
  for (let attempt = 0; attempt < 4; attempt++) {
    const committed = await replay(); if (committed) return committed;
    try {
      const sequence = (await db.prepare("SELECT COALESCE(MAX(sequence),0)+1 AS value FROM account_mutations WHERE account_id=?").bind(accountId).first<{ value: number }>())!.value;
      const context = await readTrainContext(db, accountId);
      if ((context.saved?.revision ?? 0) !== body.expectedRevision) throw new ActivityError(409, "train_loadout_revision_conflict");
      if (context.openRun) throw new ActivityError(409, "train_loadout_open_run");
      validateTrainSelection(context, selection);
      const changed = context.saved?.definition !== definition;
      const nextRevision = body.expectedRevision + (changed ? 1 : 0);
      const now = Date.now(), receiptId = walletId("train_loadout"), mutationId = walletId("train_loadout_mut");
      const response = { accepted: true, receiptId, revision: nextRevision, changed, loadout: selection, refreshRequired: !body.responseVersion,
        ...(body.responseVersion?{contractVersion:body.responseVersion,accountMutation:{beforeMutationSequence:sequence-1,mutationSequence:sequence},
          owners:{trainLoadout:{kind:'SNAPSHOT',revision:nextRevision,loadout:selection}},invalidates:changed?['DEPARTURE_PREVIEW']:[]}: {}) };
      const writes = [
        db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES(?,?,?,'TRAIN_LOADOUT',?,?)").bind(mutationId, accountId, sequence, key, now),
        trainOwnershipGuard(db, accountId, selection, context.progress, body.expectedRevision),
      ];
      if (changed) writes.push(db.prepare(`INSERT INTO account_train_loadout(account_id,loadout_json,revision,updated_at) VALUES(?,?,?,?)
        ON CONFLICT(account_id) DO UPDATE SET loadout_json=excluded.loadout_json,revision=excluded.revision,updated_at=excluded.updated_at`).bind(accountId, definition, nextRevision, now));
      writes.push(db.prepare("DELETE FROM train_loadout_write_guard"),
        db.prepare("INSERT INTO train_loadout_receipts(id,account_id,idempotency_key,request_hash,mutation_id,response_json,created_at) VALUES(?,?,?,?,?,?,?)").bind(receiptId, accountId, key, hash, mutationId, JSON.stringify(response), now));
      try { await db.batch(writes); return response; }
      catch (error) {
        if (/UNIQUE constraint failed|CHECK constraint failed: valid=1|train loadout receipt immutable/iu.test(String(error))) {
          if (attempt < 3) continue;
          throw new ActivityError(409, "train_loadout_write_conflict");
        }
        throw error;
      }
    } catch (error) {
      if (error instanceof ActivityError) { const completed = await replay(); if (completed) return completed; }
      throw error;
    }
  }
  throw new ActivityError(409, "train_loadout_write_conflict");
}

export function registerTrainLoadoutRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  app.on(["GET", "POST"], "/v1/trains/loadout", async c => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search) throw new ActivityError(400, "contract_violation");
      if (c.req.method === "GET") {
        if ((await readBoundedBody(c)).length) throw new ActivityError(400, "contract_violation");
        return c.json(view(await readTrainContext(c.env.DB, auth.accountId)));
      }
      const key = c.req.header("Idempotency-Key") ?? "";
      if (!/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "contract_violation");
      let raw: unknown;
      try { raw = JSON.parse(await readBoundedBody(c)); } catch { throw new ActivityError(400, "contract_violation"); }
      const parsed = changeBody.safeParse(raw);
      if (!parsed.success) throw new ActivityError(400, "contract_violation");
      return c.json(await change(c.env.DB, auth.accountId, parsed.data, key));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  });
}
