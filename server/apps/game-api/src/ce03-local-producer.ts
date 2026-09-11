import type { Context, Hono } from "hono";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { readBoundedBody } from "./activity-routes";
import { readLocalDepartureSelection } from "./departure";
import { bindingSchema, contentSchema, effectSchema, enemyIds, id, nat, operatorIds, policySchema,
  pos, profileSchema, rulesSchema, sourceFileSchema, target, templateSchema } from "./ce03-local-schema";

const version = "ce03.local.qa.20260911.001", simulationVersion = "ce03.execution.2026-09-11.002";
const sourceHashes = {
  designJson: "66451f6948acc0d112864aaad0d95a3f0673993612ce691825727d1c305e263f",
  selectionJson: "5418b60c50d07b2ffdd1c9ae9616f27a1e27bac58d15e3f8263d7b7ccbc7fa91",
  bindingEvidenceJson: "15560bb7cb54198cd282ed999ff62dc2d22bd07b44d8355a9664a624e5f4efec",
} as const;
export type LocalCombatInputs = { designJson: string; selectionJson: string; bindingEvidenceJson: string };
const fail = (code: string): never => { throw new ActivityError(409, code); };
const object = z.object, closed = z.strictObject;
const sourceEffect = object({ type: z.string(), target: z.string().optional(), atkBps: pos.optional(), trainAtkBps: pos.optional(),
  casterHpBps: pos.optional(), trainHpBps: pos.optional(), durationMs: pos.optional(), valueBps: nat.optional() });
const sourceAbility = object({ id: id.optional(), target: z.string(), cooldownMs: pos.optional(), windupMs: nat, effects: z.array(sourceEffect).min(1) });
const sourceDesign = object({ operators: z.array(object({ id, hp: pos, atk: pos, armorBps: nat, attackMs: pos, reach: z.string(), passive: z.unknown() })),
  kits: z.array(object({ operatorId: id, active: sourceAbility, ultimate: sourceAbility })),
  train: object({ hp: pos, armorBps: nat, atk: pos, attackMs: pos, weight: pos, manual: z.array(sourceAbility) }),
  carriages: z.array(object({ type: z.string(), hp: pos, armorBps: nat, weight: pos,
    device: object({ type: z.string(), amount: pos.optional(), periodMs: pos, target: z.string() }) })),
  enemies: z.array(object({ id, behavior: z.string(), atk: pos, armorBps: nat, attackMs: pos, windupMs: nat, target: z.string() })),
  rules: z.record(z.string(), z.unknown()),
});
const selectionSchema = object({ version: z.literal(version), operationId: z.literal("opn_01_last_departure"), risk: z.literal("I"),
  factionRef: z.null(), factionDisposition: z.literal("UNSUPPORTED_IN_ISOLATED_PROFILE"), actorSelection: z.literal("EXPLICIT_IDS"),
  waves: z.array(object({ wave: pos.max(15), interiorEnemyIds: z.array(z.enum(enemyIds)), interiorMaxHp: z.array(pos),
    exteriorEnemyId: z.literal("enemy_railbike_marauder"), exteriorMaxHp: pos })).length(15),
  package: object({ scope: z.literal("ISOLATED_PROPOSAL_KEYS"), augments: z.array(z.unknown()).length(3), tactic: z.unknown() }) });
const ownedSchema = object({ accountId: id, operationId: z.literal("opn_01_last_departure"), risk: z.literal("I"),
  formationRevision: pos, loadoutRevision: pos,
  formation: object({ operators: z.array(object({ slot: nat.max(4), operatorId: z.enum(operatorIds) })).length(5) }),
  ultimateSelection: object({ operatorId: z.enum(operatorIds), revision: nat }),
  loadout: object({ trainId: id, carriages: z.array(object({ slot: nat.max(1), carriageId: z.enum(["car_combat", "car_gun"]) })).length(2) }),
  ownedState: object({ operators: z.array(object({ operatorId: z.enum(operatorIds), skillTraining: z.literal(1), promotion: z.literal(0), revision: pos })).length(5),
    trainingCoreSlots: z.array(object({ slot: nat.max(4), operatorId: z.enum(operatorIds), trainedLevel: z.literal(1), revision: pos })).length(5),
    train: object({ trainId: id, level: z.literal(1), revision: pos }),
    carriages: z.array(object({ carriageId: z.enum(["car_combat", "car_gun"]), level: z.literal(1), revision: pos })).length(2) }) });
type Draft = {
  profileId: string; engineProfileVersion: string; effectiveStatsVersion: string; roundingVersion: string; simulationVersion: string;
  template: z.infer<typeof templateSchema>; executionPolicy: z.infer<typeof policySchema>;
  bindings: z.infer<typeof bindingSchema>[]; sourceFiles: z.infer<typeof sourceFileSchema>[];
};
const mapTarget = (value: string) => target.parse(value === "external_focus" || value === "exterior_focus" ? "EXTERIOR_ENEMY_FOCUS" : value);
function normalizeEffect(e: z.infer<typeof sourceEffect>) {
  const override = e.target === undefined ? null : mapTarget(e.target);
  if (["damage", "heal", "shield", "train_shield", "repair"].includes(e.type)) {
    const pairs = [["CASTER_ATK", e.atkBps], ["TRAIN_ATK", e.trainAtkBps], ["CASTER_MAX_HP", e.casterHpBps], ["TRAIN_MAX_HP", e.trainHpBps]] as const;
    const chosen = pairs.filter(p => p[1] !== undefined);
    if (chosen.length !== 1) return fail("ce03_effect_basis_invalid");
    let numerator = chosen[0]![1]!, denominator = 10000;
    let a = numerator, b = denominator; while (b) { const next = a % b; a = b; b = next; }
    numerator /= a; denominator /= a; // Fixed owned rank1: no rank offset or second growth.
    return effectSchema.parse({ type: e.type, basis: chosen[0]![0], coefficient: { numerator, denominator }, targetOverride: override,
      ...(["shield", "train_shield"].includes(e.type) ? { durationMs: e.durationMs } : {}) });
  }
  return effectSchema.parse({ type: e.type, targetOverride: override,
    ...(e.durationMs === undefined ? {} : { durationMs: e.durationMs }), ...(e.valueBps === undefined ? {} : { valueBps: e.valueBps }) });
}
function normalizeAbility(a: z.infer<typeof sourceAbility>, abilityId: string, kind: "ACTIVE" | "ULTIMATE" | "TRAIN_MANUAL") {
  return { id: abilityId, kind, firstReadyMs: kind === "ULTIMATE" ? 15000 : 0,
    cooldownMs: kind === "ULTIMATE" ? 45000 : a.cooldownMs, windupMs: a.windupMs, recoveryMs: 200,
    target: mapTarget(a.target), effects: a.effects.map(normalizeEffect) };
}
function projectBindings(raw: unknown) {
  const envelope = object({ schemaVersion: z.literal("ce03-editor-typed-binding-evidence-v1"), shippingReady: z.literal(false),
    actualBattleReady: z.literal(false), roundtripVerified: z.literal(226), missingRequiredRoles: z.array(z.never()).length(0),
    bindings: z.array(object({ bindingId: id, actorOrRoleId: id, locator: bindingSchema.shape.locator,
      assetType: bindingSchema.shape.assetType, assetRawSha256: bindingSchema.shape.assetRawSha256, metaRawSha256: bindingSchema.shape.metaRawSha256,
      dependencies: z.array(id), importValidation: object({ unityVersion: z.string(), importerVersion: z.string(), loadVerified: z.literal(true) }) })).length(226) }).parse(raw);
  // Intentional closed projection; evidence-only asset paths, importer metadata,
  // code dependencies and outer readiness flags do not become wire fields.
  return envelope.bindings.map(b => bindingSchema.parse(b)).sort((a, b) => a.bindingId < b.bindingId ? -1 : a.bindingId > b.bindingId ? 1 : 0);
}
export function validateLocalBindingClosure(bindings: z.infer<typeof bindingSchema>[], template: z.infer<typeof templateSchema>) {
  const byId = new Map(bindings.map(b => [b.bindingId, b]));
  if (byId.size !== bindings.length) fail("ce03_binding_duplicate");
  for (const b of bindings) {
    if (new Set(b.dependencies).size !== b.dependencies.length || b.dependencies.some((d, i) => !byId.has(d) || (i > 0 && b.dependencies[i - 1]! >= d)))
      fail("ce03_binding_dependency_invalid");
  }
  const required = (bindingId: string | null, type: string) => {
    if (bindingId !== null && byId.get(bindingId)?.assetType !== type) fail("ce03_binding_role_invalid");
  };
  for (const o of template.operators) { required(o.presentation.controller, "AnimatorController"); required(o.presentation.portrait, "Sprite"); required(o.presentation.skillIcon, "Sprite"); }
  for (const e of template.enemyDefinitions) { required(e.presentation.controller, "AnimatorController"); required(e.presentation.sprite, "Sprite"); }
  required(template.train.presentation.sprite, "Sprite"); template.cars.forEach(c => required(c.presentation.sprite, "Sprite"));
  const p = template.presentation;
  [p.interiorLane, p.exteriorLane, p.initialSquadInterior, ...Object.values(p.vfx), ...Object.values(p.transitions)].forEach(v => required(v, "Sprite"));
}
export async function prepareLocalCombatDraft(inputs: LocalCombatInputs, ownedInput: unknown): Promise<Draft> {
  for (const key of Object.keys(sourceHashes) as (keyof LocalCombatInputs)[]) {
    if (await sha256Hex(inputs[key]) !== sourceHashes[key]) fail("ce03_source_hash_mismatch");
  }
  const checked = ownedSchema.safeParse(ownedInput);
  if (!checked.success) return fail("ce03_owned_fixture_mismatch");
  const own = checked.data, rows = [...own.formation.operators].sort((a, b) => a.slot - b.slot);
  if (new Set(rows.map(o => o.operatorId)).size !== 5 || rows.some((o, i) => o.slot !== i) ||
      new Set(own.ownedState.operators.map(o => o.operatorId)).size !== 5 ||
      new Set(own.ownedState.trainingCoreSlots.map(c => c.slot)).size !== 5 ||
      new Set(own.ownedState.trainingCoreSlots.map(c => c.operatorId)).size !== 5 ||
      own.loadout.trainId !== own.ownedState.train.trainId ||
      [...own.loadout.carriages].sort((a, b) => a.slot - b.slot).some((c, i) => c.slot !== i || c.carriageId !== ["car_combat", "car_gun"][i]) ||
      new Set(own.ownedState.carriages.map(c => c.carriageId)).size !== 2) fail("ce03_owned_fixture_mismatch");
  const d = sourceDesign.parse(JSON.parse(inputs.designJson)), selected = selectionSchema.parse(JSON.parse(inputs.selectionJson));
  const bindings = projectBindings(JSON.parse(inputs.bindingEvidenceJson));
  const policy = policySchema.parse({ version: simulationVersion, orderedStages: ["P1.expire-recover-spawn", "P1.commands", "P1.schedule", "P1.sequential-effects", "P1.terminal-wave", "P1.checkpoint"],
    targetTiePolicy: "P2", sameTickTerminalPolicy: "P1.failure-priority", interruptPolicy: "P3", statusReapplicationPolicy: "P4.status",
    shieldLayerPolicy: "P4.shield", packageCombinationPolicy: "P5", damageRoundingPolicy: "P5.final-floor", recoveryPolicy: "P6", waveActivationPolicy: "P6.r2" });
  const ruleValues = { ...d.rules, magnitudeBonusCapBps: 3000 };
  const rules = rulesSchema.parse(Object.fromEntries(Object.keys(rulesSchema.shape).map(k => [k, ruleValues[k as keyof typeof ruleValues]])));
  const template = templateSchema.parse({
    operation: { id: selected.operationId, mode: "SOLO", risk: selected.risk, encounterType: "ORDINARY_15_WAVE", factionRef: null,
      factionDisposition: selected.factionDisposition, actorSelection: selected.actorSelection, raidId: null, bossId: null, bossPhaseRefs: [] },
    selectedUltimateOperatorId: own.ultimateSelection.operatorId,
    operators: rows.map(row => {
      const o = d.operators.find(o => o.id === row.operatorId)!, kit = d.kits.find(k => k.operatorId === row.operatorId)!;
      return { operatorId: row.operatorId, slot: row.slot, level: 1, promotion: 0,
        skillRank: own.ownedState.operators.find(o => o.operatorId === row.operatorId)!.skillTraining,
        stats: { maxHp: o.hp, atk: o.atk, armorBps: o.armorBps, attackMs: o.attackMs, reach: o.reach }, passive: o.passive,
        active: normalizeAbility(kit.active, `${row.operatorId}.active`, "ACTIVE"), ultimate: normalizeAbility(kit.ultimate, `${row.operatorId}.ultimate`, "ULTIMATE"),
        presentation: { controller: `binding.${row.operatorId}.controller`, portrait: null, skillIcon: `binding.${row.operatorId}.skillIcon` } };
    }),
    train: { level: 1, stats: { maxHp: d.train.hp, atk: d.train.atk, armorBps: d.train.armorBps, attackMs: d.train.attackMs,
      engineOutput: 65, totalWeight: 60, speedNumerator: 105, speedDenominator: 100 },
      manual: d.train.manual.map(a => normalizeAbility(a, a.id!, "TRAIN_MANUAL")), presentation: { sprite: null } },
    cars: ["Combat", "Gun"].map((type, slot) => {
      const c = d.carriages.find(c => c.type === type)!;
      if (c.device.type !== "damage") return fail("ce03_device_unsupported");
      return { type, slot, level: 1, stats: { maxHp: c.hp, armorBps: c.armorBps, weight: c.weight },
        device: { type: "DAMAGE", amount: c.device.amount, periodMs: c.device.periodMs, target: mapTarget(c.device.target) }, presentation: { sprite: null } };
    }),
    enemyDefinitions: enemyIds.map(definitionId => {
      const e = d.enemies.find(e => e.id === definitionId)!;
      return { definitionId, behavior: e.behavior, atk: e.atk, armorBps: e.armorBps, attackMs: e.attackMs, windupMs: e.windupMs, target: e.target,
        presentation: { controller: `binding.${definitionId}.controller`, sprite: `binding.${definitionId}.sprite` } };
    }),
    waves: selected.waves.map(w => ({ number: w.wave, completion: "ALL_REQUIRED_ACTORS_AND_TERMINAL_IMPACTS_RESOLVED",
      spawns: [...w.interiorEnemyIds.map((definitionId, logicalSlot) => ({ spawnId: `wave.${w.wave}.interior.${logicalSlot}`, definitionId,
        lane: "INTERIOR", logicalSlot, atTick: 0, resolvedMaxHp: w.interiorMaxHp[logicalSlot], requiredForCompletion: true })),
      { spawnId: `wave.${w.wave}.exterior.0`, definitionId: w.exteriorEnemyId, lane: "EXTERIOR", logicalSlot: 0, atTick: 0, resolvedMaxHp: w.exteriorMaxHp, requiredForCompletion: true }] })),
    package: { ...selected.package, version }, rules,
    execution: { stepMs: 50, clock: "INTEGER_FIXED_STEP", crit: false, dodge: false, policyVersion: simulationVersion, policySha256: await sha256Hex(canonicalJson(policy)),
      entranceMs: 600, frontSlots: [3, 4], rearSlots: [0, 1, 2], queue: { capacity: 1, expiryMs: 1000, secondCommand: "REJECT", cooldownAt: "CAST_ACCEPTANCE" },
      noWallClockCatchup: true, enemyBasicInterruptibleWindup: true, trainActionChannel: "SHARED_BASIC_AND_MANUAL", basicLogicalTravel: "DIRECT_IMPACT", carriageDamageEvents: "UNSUPPORTED" },
    presentation: { interiorLane: "binding.scene.interiorLane", exteriorLane: "binding.scene.exteriorLane", initialSquadInterior: "binding.scene.initialSquadInterior",
      vfx: Object.fromEntries(["basicHit", "skillActivation", "ultimateActivation", "shield", "heal"].map(k => [k, `binding.scene.vfx.${k}`])),
      transitions: { victory: "binding.scene.transitions.victory", defeat: "binding.scene.transitions.defeat" } },
    variants: { equipment: "OFF", research: "OFF", extraPressure: "OFF", factionBonuses: "UNSUPPORTED" },
  });
  validateLocalBindingClosure(bindings, template);
  return { profileId: version, engineProfileVersion: "train-combat-test-profile-v1", effectiveStatsVersion: "combat.design.2026-09-09.001",
    roundingVersion: "ce03-rational-final-floor-v1", simulationVersion, template, executionPolicy: policy, bindings,
    sourceFiles: Object.entries(sourceHashes).map(([logicalName, rawSha256]): z.infer<typeof sourceFileSchema> => ({ logicalName, rawSha256 })).concat([
      { logicalName: "executionPolicyR1", rawSha256: "7091e45209bce6fc7059f7e020a8d38fec19f2a50b60a00b7cb406984427bcec" },
      { logicalName: "executionPolicyR2", rawSha256: "b44d16bd48eafecb60d2c70ddb9a4e0a681868ccd73772f65d98a9378e9be9ec" },
    ]).sort((a, b) => a.logicalName < b.logicalName ? -1 : 1) };
}

// Caller-owned executed evidence is required. No fallback to proposal markdown or
// arbitrary supplied digest. Candidate hashes are never a frozen admitted P/C/S.
export async function finalizeLocalCombatArtifacts(draft: Draft, evidence: unknown) {
  if (evidence === undefined || evidence === null) return fail("ce03_execution_evidence_missing");
  const parsed = closed({ scope: z.enum(["DISPOSABLE_FIXTURE", "LOCAL_CONSUMER"]), algorithmSource: z.string().min(1), vectorsJson: z.string().min(1),
    verification: closed({ passed: pos, failed: z.literal(0) }) }).safeParse(evidence);
  if (!parsed.success) return fail("ce03_execution_evidence_invalid");
  const e = parsed.data;
  // This stage supports fixture serialization only. Actual consumer attestation
  // needs its runner/code report binding and is deliberately not accepted yet.
  if (e.scope !== "DISPOSABLE_FIXTURE" || /^\s*#/u.test(e.algorithmSource)) return fail("ce03_execution_evidence_invalid");
  const vectors = object({ cases: z.array(z.unknown()).min(1) }).safeParse(JSON.parse(e.vectorsJson));
  if (!vectors.success || vectors.data.cases.length !== e.verification.passed) return fail("ce03_execution_evidence_invalid");
  const { bindings, ...profileData } = draft;
  const profile = profileSchema.parse({ schemaVersion: "ce03-isolated-profile-r4", ...profileData,
    executionEvidence: { algorithmSourceSha256: await sha256Hex(e.algorithmSource), conformanceVectorsSha256: await sha256Hex(e.vectorsJson) } });
  if (profile.template.execution.policySha256 !== await sha256Hex(canonicalJson(profile.executionPolicy))) return fail("ce03_policy_hash_mismatch");
  validateLocalBindingClosure(bindings.map(b => bindingSchema.parse(b)), profile.template);
  const candidateProfileSha256 = await sha256Hex(canonicalJson(profile));
  const contentManifest = contentSchema.parse({ schemaVersion: "ce03-isolated-content-r4", version,
    profileSha256: candidateProfileSha256, resolver: { mode: "UNITY_EDITOR_ASSET_DATABASE", playerBuildAllowed: false }, bindings });
  return { ready: false as const, frozen: false as const, evidenceScope: e.scope, profile, contentManifest,
    candidateProfileSha256, candidateContentSha256: await sha256Hex(canonicalJson(contentManifest)) };
}

type LocalAuth = { accountId: string } | { status: 400 | 401 | 403 | 409 | 500 | 503; errorCode: string; message: string };
export function registerLocalCombatProducer(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<LocalAuth>,
  options: { inputs: LocalCombatInputs; disposableDatabase: D1Database; allowedAccountIds: readonly string[] }) {
  // NOT mounted in index.ts, no wrangler/service configuration. Explicit DB
  // identity + owner allowlist + loopback checked separately from authentication.
  app.post("/__local/ce03/prepare", async c => {
    c.header("Cache-Control", "no-store");
    if (!["127.0.0.1", "localhost", "[::1]"].includes(new URL(c.req.url).hostname) || c.env.DB !== options.disposableDatabase)
      return c.json({ ready: false, errorCode: "ce03_isolated_context_required" }, 403);
    const auth = await authenticate(c);
    if (!("accountId" in auth)) return c.json({ ready: false, errorCode: auth.errorCode }, auth.status);
    if (!options.allowedAccountIds.includes(auth.accountId)) return c.json({ ready: false, errorCode: "ce03_account_not_allowed" }, 403);
    try {
      if (new URL(c.req.url).search) throw new ActivityError(400, "contract_violation");
      const request = JSON.parse(await readBoundedBody(c));
      const ownedSelection = await readLocalDepartureSelection(c.env.DB, auth.accountId, request);
      const draft = await prepareLocalCombatDraft(options.inputs, ownedSelection);
      const rechecked = await readLocalDepartureSelection(c.env.DB, auth.accountId, request);
      if (canonicalJson(ownedSelection) !== canonicalJson(rechecked)) throw new ActivityError(409, "ce03_selection_changed_during_prepare");
      return c.json({ ready: false, status: "MISSING_EXECUTION_EVIDENCE", missing: ["executionEvidence"], ownedSelection, draft });
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ ready: false, errorCode: error.code }, error.status);
      if (error instanceof z.ZodError || error instanceof SyntaxError) return c.json({ ready: false, errorCode: "ce03_input_invalid" }, 400);
      throw error;
    }
  });
}
