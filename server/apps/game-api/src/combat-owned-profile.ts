import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { formationSelection } from "./formation";
import { trainSelection, validateTrainSelection } from "./train-loadout";
import { captureCombatPolicyForFormation, nyxPolicyVersion } from "./combat-policy-catalog";
import { captureOwnedCombatBaseDefinitions, captureNyxOwnedCombatBaseDefinitions } from "./combat-owned-base-definitions";

const id = z.string().min(1).max(128), revision = z.number().int().min(1).max(Number.MAX_SAFE_INTEGER);
const operator = z.object({ operatorId: id, skillTraining: z.number().int().min(1).max(5),
  promotion: z.number().int().min(0).max(3), revision });
const core = z.object({ slot: z.number().int().min(0).max(4), operatorId: id,
  level: z.number().int().min(1).max(50), revision });
const owned = z.object({ formation: z.object({ json: z.string(), revision }),
  loadout: z.object({ json: z.string(), revision }), train: z.object({ trainId: id, level: z.number().int().min(1).max(10), revision }),
  operators: z.array(operator), cores: z.array(core),
  carriages: z.array(z.object({ carriageId: id, level: z.number().int().min(1).max(10), revision })),
  turrets: z.array(z.object({ turretId: id, level: z.number().int().min(1).max(10), revision })),
  progress: z.string().nullable(), openRun: z.number().int().min(0).max(1) });
// One SQL snapshot; no mixed-time read of formation, ownership, growth or loadout.
const query = `SELECT json_object(
 'formation',(SELECT json_object('json',formation_json,'revision',revision) FROM account_formation WHERE account_id=?1),
 'loadout',(SELECT json_object('json',loadout_json,'revision',revision) FROM account_train_loadout WHERE account_id=?1),
 'train',(SELECT json_object('trainId',train_id,'level',level,'revision',revision) FROM account_train WHERE account_id=?1),
 'operators',json((SELECT json_group_array(json_object('operatorId',operator_id,'skillTraining',skill_training,'promotion',promotion,'revision',revision)) FROM (SELECT * FROM account_operators WHERE account_id=?1 ORDER BY operator_id))),
 'cores',json((SELECT json_group_array(json_object('slot',slot_index,'operatorId',operator_id,'level',trained_level,'revision',revision)) FROM (SELECT * FROM operator_training_core_slots WHERE account_id=?1 ORDER BY slot_index))),
 'carriages',json((SELECT json_group_array(json_object('carriageId',carriage_id,'level',level,'revision',revision)) FROM (SELECT * FROM account_carriages WHERE account_id=?1 ORDER BY carriage_id))),
 'turrets',json((SELECT json_group_array(json_object('turretId',turret_id,'level',level,'revision',revision)) FROM (SELECT * FROM account_turrets WHERE account_id=?1 ORDER BY turret_id))),
 'progress',(SELECT cleared_operations_json FROM account_story_progress WHERE account_id=?1),
 'openRun',EXISTS(SELECT 1 FROM active_combat_runs WHERE account_id=?1 AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED'))
 ) AS value`;

/** Account ID comes from the caller's authenticated principal, never a client body.
 * Read-only intake, not an atomic START quote and not an active-run restoration path.
 */
export async function readOwnedCombatProfile(db: D1Database, accountId: string, input: unknown) {
  const request = z.strictObject({ expectedFormationRevision: revision, expectedLoadoutRevision: revision,
    expectedSourceHash: z.string().regex(/^sha256:[a-f0-9]{64}$/u).optional(),
    expectedDefinitionVersion: id.optional(), expectedPolicyVersion: id.optional() }).safeParse(input);
  if (!request.success || !id.safeParse(accountId).success) throw new ActivityError(400, "combat_owned_request_invalid");
  const row = await db.prepare(query).bind(accountId).first<{ value: string }>();
  let raw: unknown;
  try { raw = row ? JSON.parse(row.value) : null; }
  catch { throw new ActivityError(503, "combat_owned_source_invalid"); }
  const selection = z.object({ formation: z.unknown().nullable(), loadout: z.unknown().nullable(), train: z.unknown().nullable() }).safeParse(raw);
  if (!selection.success || !selection.data.formation || !selection.data.loadout || !selection.data.train) {
    throw new ActivityError(409, "combat_owned_selection_missing");
  }
  const parsed = owned.safeParse(raw);
  if (!parsed.success) throw new ActivityError(503, "combat_owned_source_invalid");
  const s = parsed.data;
  if (s.formation.revision !== request.data.expectedFormationRevision || s.loadout.revision !== request.data.expectedLoadoutRevision) {
    throw new ActivityError(409, "combat_owned_selection_changed");
  }
  if (s.openRun) throw new ActivityError(409, "combat_owned_open_run");
  let formation: z.infer<typeof formationSelection>, loadout: z.infer<typeof trainSelection>;
  try { formation = formationSelection.parse(JSON.parse(s.formation.json)); loadout = trainSelection.parse(JSON.parse(s.loadout.json)); }
  catch { throw new ActivityError(503, "combat_owned_selection_invalid"); }
  validateTrainSelection({ train: s.train, carriages: s.carriages, turrets: s.turrets,
    saved: { definition: s.loadout.json, revision: s.loadout.revision, updatedAt: 0 }, progress: s.progress, openRun: s.openRun }, loadout);
  const byId = new Map(s.operators.map(o => [o.operatorId, o]));
  if (formation.operators.some(o => !byId.has(o.operatorId))) throw new ActivityError(403, "combat_owned_operator_not_owned");
  if (s.cores.some(c => !byId.has(c.operatorId))) throw new ActivityError(503, "combat_owned_core_owner_mismatch");
  const completeCores = s.cores.length === 5 && new Set(s.cores.map(c => c.slot)).size === 5 && new Set(s.cores.map(c => c.operatorId)).size === 5;
  const policy = captureCombatPolicyForFormation(formation.operators);
  const base = policy.version === nyxPolicyVersion ? captureNyxOwnedCombatBaseDefinitions() : captureOwnedCombatBaseDefinitions();
  const sourceHash = `sha256:${await sha256Hex(canonicalJson({ accountId, state: s }))}`;
  if ((request.data.expectedSourceHash !== undefined && request.data.expectedSourceHash !== sourceHash)
    || (request.data.expectedDefinitionVersion !== undefined && request.data.expectedDefinitionVersion !== base.version)
    || (request.data.expectedPolicyVersion !== undefined && request.data.expectedPolicyVersion !== policy.version)) {
    throw new ActivityError(409, "combat_owned_source_changed");
  }
  const missing = ["predeparture_equipment_binding_unavailable", "account_research_ledger_unavailable",
    "commander_and_run_modifier_binding_unavailable", "entity_placement_and_capabilities_unbound", "start_checkpoint_binding_unbound"];
  if (!completeCores) missing.push("training_core_incomplete");
  const operators = [...formation.operators].sort((a, b) => a.slot - b.slot).map(selected => {
    const o = byId.get(selected.operatorId)!;
    const ownCore = s.cores.find(c => c.operatorId === o.operatorId);
    const level = completeCores ? ownCore?.level ?? Math.min(...s.cores.map(c => c.level)) : null;
    const b = Object.hasOwn(base.operators, o.operatorId) ? base.operators[o.operatorId as keyof typeof base.operators] : null;
    const defense = Object.hasOwn(policy.numericDefense.operatorBase, o.operatorId)
      ? policy.numericDefense.operatorBase[o.operatorId as keyof typeof policy.numericDefense.operatorBase] : null;
    if (!b || defense === null) missing.push(`operator_base_definition_missing:${o.operatorId}`);
    const permanentStats = b && defense !== null && level !== null ? calculateOwnedOperatorStats(b,base.growth,level,o.promotion,defense) : null;
    const sourceId = `account_operators:${accountId}:${o.operatorId}`;
    return { ...selected, level, promotion: o.promotion, skillTraining: o.skillTraining,
      permanentStats, effectiveStats: null,
      skillMagnitude: { numerator: String(10000 + base.growth.skillPerRankBps * (o.skillTraining - 1)), denominator: "10000" },
      sources: { operator: { sourceId, revision: o.revision }, training: s.cores.map(c => ({
        sourceId: `operator_training_core_slots:${accountId}:${c.slot}`, revision: c.revision, operatorId: c.operatorId, level: c.level })) },
      // Stat-input provenance groups, NOT runtime outgoing/heal/mark effect groups.
      statInputs: { sourceId, sourceOwner: accountId, targetDefinitionId: o.operatorId, group: "PERMANENT_STAT_GROWTH",
        promotionHpBps: base.growth.promotionHpBps[o.promotion]!, promotionAttackBps: base.growth.promotionAttackBps[o.promotion]! },
      effectModifiers: null,
    };
  });
  const payload = { schemaVersion: "combat.owned-profile-intake.v1", accountId,
    definitionVersion: base.version, definitionHash: `sha256:${await sha256Hex(canonicalJson(base))}`,
    policyVersion: policy.version, policyHash: `sha256:${await sha256Hex(canonicalJson(policy))}`,
    sourceHash, formationRevision: s.formation.revision, loadoutRevision: s.loadout.revision,
    binding: "READ_ONLY_CANDIDATE_NOT_ATOMIC_START", canExecute: false, missing, operators,
    selectedTrain: { ...s.train, sourceId: `account_train:${accountId}`, effectiveStats: null },
    selectedCarriages: loadout.carriages.map(c => ({ ...c, ...s.carriages.find(o => o.carriageId === c.carriageId)!,
      sourceId: `account_carriages:${accountId}:${c.carriageId}`, effectiveStats: null })),
    ownedTurrets: s.turrets.map(t => ({ ...t, sourceId: `account_turrets:${accountId}:${t.turretId}` })),
    selectedTurrets: null, equipment: null, research: null, startSnapshot: null, checkpoint: null };
  payload.missing.push("train_carriage_base_stat_binding_unavailable", "turret_installation_binding_unavailable");
  return { ...payload, contentHash: `sha256:${await sha256Hex(canonicalJson(payload))}` };
}
export type OwnedCombatProfile = Awaited<ReturnType<typeof readOwnedCombatProfile>>;
export function requireCompleteOwnedCombatProfile(profile: OwnedCombatProfile): never {
  void profile;
  throw new ActivityError(409, "combat_owned_profile_incomplete");
}

/** Original C3 growth arithmetic reused by explicit additive named definitions.
 * No equipment/research/buff or native action support is implied by permanent stats. */
export function calculateOwnedOperatorStats(base:{hp:number;attack:number;basicIntervalMs:number},growth:ReturnType<typeof captureOwnedCombatBaseDefinitions>['growth'],level:number,promotion:number,defense:number){
 z.strictObject({hp:z.number().int().positive().safe(),attack:z.number().int().positive().safe(),basicIntervalMs:z.number().int().positive().safe()}).parse(base);
 z.number().int().min(1).max(50).parse(level);z.number().int().min(0).max(3).parse(promotion);z.number().int().nonnegative().safe().parse(defense);
 return {maxHp:Number(BigInt(base.hp)*BigInt(10000+growth.hpPerLevelBps*(level-1))*BigInt(10000+growth.promotionHpBps[promotion]!)/100000000n),attack:{numerator:(BigInt(base.attack)*BigInt(10000+growth.attackPerLevelBps*(level-1))*BigInt(10000+growth.promotionAttackBps[promotion]!)).toString(),denominator:'100000000'},defense,basicIntervalMs:base.basicIntervalMs};
}
/** New internal departure composition: original read/calculation plus exact original
 * SQL snapshot fencing. No public response changes or new owned-state authority. */
export async function prepareOwnedCombatProfile(db:D1Database,accountId:string,input:unknown){
 const profile=await readOwnedCombatProfile(db,accountId,input);
 const row=await db.prepare(query).bind(accountId).first<{value:string}>();
 if(!row)throw new ActivityError(409,'combat_owned_source_changed');
 const original=owned.parse(JSON.parse(row.value));
 if('sha256:'+await sha256Hex(canonicalJson({accountId,state:original}))!==profile.sourceHash)throw new ActivityError(409,'combat_owned_source_changed');
 return {profile,assertions:[db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN json((SELECT value FROM (${query})))=json(?2) THEN 1 ELSE 0 END`).bind(accountId,row.value)]};
}
