import { z } from "zod";
import { canonicalJson } from "@lastrail/common";
import { frontlineExecutionPolicy, frontlinePolicyId, frontlinePolicySha256, frontlineSimulationVersion } from "./ce03-targeting-policy";

// Isolated r4 wire projection. This module is not a shipping readiness contract.
export const id = z.string().regex(/^[A-Za-z0-9_.:-]{1,128}$/u);
export const sha = z.string().regex(/^[a-f0-9]{64}$/u);
export const nat = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
export const pos = nat.min(1), bps = nat.max(10000);
const obj = z.strictObject;
const gcd = (a: number, b: number): number => b === 0 ? a : gcd(b, a % b);
const ratio = obj({ numerator: pos, denominator: pos }).refine(r => gcd(r.numerator, r.denominator) === 1);
export const operatorIds = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"] as const;
export const enemyIds = ["enemy_raider_scout", "enemy_spike_cart_gunner", "enemy_railbike_marauder"] as const;
const operatorId = z.enum(operatorIds), enemyId = z.enum(enemyIds);
export const target = z.enum(["ally_lowest_hp", "all_allies", "enemy_focus_any", "enemies_front_2", "train", "self", "EXTERIOR_ENEMY_FOCUS"]);
const override = target.nullable();
export const effectSchema = z.discriminatedUnion("type", [
  obj({ type: z.literal("damage"), basis: z.enum(["CASTER_ATK", "TRAIN_ATK"]), coefficient: ratio, targetOverride: override }),
  obj({ type: z.literal("heal"), basis: z.literal("CASTER_ATK"), coefficient: ratio, targetOverride: override }),
  obj({ type: z.literal("shield"), basis: z.literal("CASTER_MAX_HP"), coefficient: ratio, durationMs: pos, targetOverride: override }),
  obj({ type: z.literal("train_shield"), basis: z.literal("TRAIN_MAX_HP"), coefficient: ratio, durationMs: pos, targetOverride: override }),
  obj({ type: z.literal("repair"), basis: z.literal("TRAIN_MAX_HP"), coefficient: ratio, targetOverride: override }),
  obj({ type: z.literal("taunt"), durationMs: pos, targetOverride: target }),
  obj({ type: z.literal("interrupt"), targetOverride: override }),
  obj({ type: z.literal("mark"), valueBps: bps, durationMs: pos, targetOverride: override }),
  obj({ type: z.literal("train_damage_buff"), valueBps: bps, durationMs: pos, targetOverride: override }),
]);
const ability = obj({ id, kind: z.enum(["ACTIVE", "ULTIMATE", "TRAIN_MANUAL"]), firstReadyMs: nat,
  cooldownMs: pos, windupMs: nat, recoveryMs: nat, target, effects: z.array(effectSchema).min(1) });
const passive = z.union([
  obj({ type: z.literal("armor_when_low_hp"), belowHpBps: bps, valueBps: bps }),
  obj({ type: z.enum(["damage_to_marked", "healing_done", "damage_to_saboteur", "repair_done"]), valueBps: bps }),
]);
const operator = obj({ operatorId, slot: nat.max(4), level: pos.max(50), promotion: nat.max(3), skillRank: pos.max(5),
  stats: obj({ maxHp: pos, atk: pos, armorBps: bps, attackMs: pos, reach: z.enum(["front", "any"]) }),
  passive, active: ability, ultimate: ability, presentation: obj({ controller: id, portrait: id.nullable(), skillIcon: id }) });
const train = obj({ level: pos.max(10), stats: obj({ maxHp: pos, atk: pos, armorBps: bps, attackMs: pos,
  engineOutput: pos, totalWeight: pos, speedNumerator: pos, speedDenominator: pos }),
  manual: z.array(ability).length(3), presentation: obj({ sprite: id.nullable() }) });
const car = obj({ type: z.enum(["Combat", "Gun"]), slot: nat.max(1), level: pos.max(10),
  stats: obj({ maxHp: pos, armorBps: bps, weight: pos }),
  device: obj({ type: z.literal("DAMAGE"), periodMs: pos, target: z.enum(["enemy_focus_any", "EXTERIOR_ENEMY_FOCUS"]), amount: pos }),
  presentation: obj({ sprite: id.nullable() }) });
const enemy = obj({ definitionId: enemyId, behavior: z.enum(["assault", "ranged", "exterior"]), atk: pos, armorBps: bps,
  attackMs: pos, windupMs: nat, target: z.enum(["front_ally", "rear_ally", "train"]), presentation: obj({ controller: id, sprite: id }) });
const spawn = obj({ spawnId: id, definitionId: enemyId, lane: z.enum(["INTERIOR", "EXTERIOR"]), logicalSlot: nat,
  atTick: nat, resolvedMaxHp: pos, requiredForCompletion: z.literal(true) });
const wave = obj({ number: pos.max(15), spawns: z.array(spawn).min(1), completion: z.literal("ALL_REQUIRED_ACTORS_AND_TERMINAL_IMPACTS_RESOLVED") });
const packageEffect = obj({ id, category: z.enum(["SQUAD", "TRAIN", "SHIELD", "TACTIC"]), valueBps: bps,
  eventFilter: z.enum(["OPERATOR_DAMAGE", "LOCOMOTIVE_GUN_DAMAGE_INCLUDING_SUPPORT_EXCLUDING_COMBAT", "SHIELD_ISSUED", "SINGLE_TARGET_OPERATOR_ACTIVE_DIRECT_DAMAGE"]) });
export const rulesSchema = obj({ armorCapBps: bps, outgoingBuffCapBps: bps, magnitudeBonusCapBps: bps,
  attackSpeedBonusCapBps: bps, normalShieldCapMaxHpBps: bps, attackSlowCapBps: bps, markCapBps: bps, armorBreakCapBps: bps,
  trainRepairCapPerEffectMaxHpBps: bps, operatorRecoveryHpBps: bps, carRecoveryHpBps: bps, carriageTransferBps: bps,
  basicWindupRatioBps: bps, normalControlImmunityMs: nat, operatorRecoveryProtectionMs: nat, carRecoveryProtectionMs: nat,
  basicRecoveryMs: nat, skillRecoveryMs: nat, waveGapMs: nat, operatorDownMs: z.array(pos).length(3), carDownMs: z.array(pos).length(3) });
export const policySchema = obj({ version: id, orderedStages: z.array(id).min(1), targetTiePolicy: id, sameTickTerminalPolicy: id,
  interruptPolicy: id, statusReapplicationPolicy: id, shieldLayerPolicy: id, packageCombinationPolicy: id,
  damageRoundingPolicy: id, recoveryPolicy: id, waveActivationPolicy: id }).refine(p =>
  (p.version !== frontlineSimulationVersion && p.targetTiePolicy !== frontlinePolicyId) ||
  canonicalJson(p) === canonicalJson(frontlineExecutionPolicy), "frontline execution policy identity");
export const templateSchema = obj({
  operation: obj({ id: z.literal("opn_01_last_departure"), mode: z.literal("SOLO"), risk: z.literal("I"),
    encounterType: z.literal("ORDINARY_15_WAVE"), factionRef: z.null(), factionDisposition: z.literal("UNSUPPORTED_IN_ISOLATED_PROFILE"),
    actorSelection: z.literal("EXPLICIT_IDS"), raidId: z.null(), bossId: z.null(), bossPhaseRefs: z.array(z.never()).length(0) }),
  selectedUltimateOperatorId: operatorId, operators: z.array(operator).length(5), train, cars: z.array(car).length(2),
  enemyDefinitions: z.array(enemy).length(3), waves: z.array(wave).length(15),
  package: obj({ scope: z.literal("ISOLATED_PROPOSAL_KEYS"), version: id, augments: z.array(packageEffect).length(3), tactic: packageEffect }),
  rules: rulesSchema,
  execution: obj({ stepMs: z.literal(50), clock: z.literal("INTEGER_FIXED_STEP"), crit: z.literal(false), dodge: z.literal(false),
    policyVersion: id, policySha256: sha, targetingPolicyId: z.literal(frontlinePolicyId).optional(),
    entranceMs: nat, frontSlots: z.array(nat.max(4)), rearSlots: z.array(nat.max(4)),
    queue: obj({ capacity: z.literal(1), expiryMs: pos, secondCommand: z.literal("REJECT"), cooldownAt: z.literal("CAST_ACCEPTANCE") }),
    noWallClockCatchup: z.literal(true), enemyBasicInterruptibleWindup: z.boolean(), trainActionChannel: z.literal("SHARED_BASIC_AND_MANUAL"),
    basicLogicalTravel: z.literal("DIRECT_IMPACT"), carriageDamageEvents: z.literal("UNSUPPORTED") })
    .transform(({ targetingPolicyId, ...execution }): typeof execution & { targetingPolicyId?: typeof frontlinePolicyId } =>
      targetingPolicyId === undefined ? execution : { ...execution, targetingPolicyId }),
  presentation: obj({ interiorLane: id, exteriorLane: id, initialSquadInterior: id,
    vfx: obj({ basicHit: id, skillActivation: id, ultimateActivation: id, shield: id, heal: id }),
    transitions: obj({ victory: id, defeat: id }) }),
  variants: obj({ equipment: z.literal("OFF"), research: z.literal("OFF"), extraPressure: z.literal("OFF"), factionBonuses: z.literal("UNSUPPORTED") }),
}).superRefine((t, c) => {
  const fail = (message: string) => c.addIssue({ code: "custom", message });
  const e = t.execution;
  if ((e.targetingPolicyId !== undefined || e.policyVersion === frontlineSimulationVersion || e.policySha256 === frontlinePolicySha256) &&
      (e.targetingPolicyId !== frontlinePolicyId || e.policyVersion !== frontlineSimulationVersion || e.policySha256 !== frontlinePolicySha256))
    fail("frontline targeting identity");
  if (new Set(t.operators.map(o => o.operatorId)).size !== 5 || t.operators.some((o, i) => o.slot !== i)) fail("operator ordering/identity");
  if (!t.operators.some(o => o.operatorId === t.selectedUltimateOperatorId)) fail("ultimate not selected");
  if (t.cars.some((car, i) => car.slot !== i || car.type !== ["Combat", "Gun"][i])) fail("car order");
  if (new Set(t.enemyDefinitions.map(e => e.definitionId)).size !== 3) fail("enemy identities");
  if (t.waves.some((w, i) => w.number !== i + 1)) fail("wave order");
  const spawns = t.waves.flatMap(w => w.spawns);
  if (new Set(spawns.map(s => s.spawnId)).size !== spawns.length) fail("duplicate spawn");
  const slots = [...t.execution.frontSlots, ...t.execution.rearSlots];
  if (slots.length !== 5 || new Set(slots).size !== 5) fail("front/rear partition");
});
const int64 = z.string().regex(/^(0|-[1-9][0-9]*|[1-9][0-9]*)$/u).refine(s => {
  try { const n = BigInt(s); return n >= -(2n ** 63n) && n <= 2n ** 63n - 1n; } catch { return false; }
});
export const bindingSchema = obj({ bindingId: id, actorOrRoleId: id,
  locator: obj({ kind: z.literal("UNITY_ASSET_GUID"), guid: z.string().regex(/^[0-9a-f]{32}$/u), localFileId: int64 }),
  assetType: z.enum(["Sprite", "AnimationClip", "AnimatorController"]), assetRawSha256: sha, metaRawSha256: sha,
  dependencies: z.array(id), importValidation: obj({ unityVersion: z.string().regex(/^[\x20-\x7e]{1,128}$/u),
    importerVersion: z.string().regex(/^[\x20-\x7e]{1,128}$/u), loadVerified: z.literal(true) }) });
export const sourceFileSchema = obj({ logicalName: id, rawSha256: sha });
export const profileSchema = obj({ schemaVersion: z.literal("ce03-isolated-profile-r4"), profileId: id,
  engineProfileVersion: id, effectiveStatsVersion: id, roundingVersion: id, simulationVersion: id,
  sourceFiles: z.array(sourceFileSchema).min(1), template: templateSchema, executionPolicy: policySchema,
  executionEvidence: obj({ algorithmSourceSha256: sha, conformanceVectorsSha256: sha }) }).refine(p =>
  p.simulationVersion === p.executionPolicy.version && p.template.execution.policyVersion === p.executionPolicy.version,
  "execution version mismatch");
export const contentSchema = obj({ schemaVersion: z.literal("ce03-isolated-content-r4"), version: id, profileSha256: sha,
  resolver: obj({ mode: z.literal("UNITY_EDITOR_ASSET_DATABASE"), playerBuildAllowed: z.literal(false) }), bindings: z.array(bindingSchema).min(1) });
