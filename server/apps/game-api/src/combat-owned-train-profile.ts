import { canonicalJson, sha256Hex } from "@lastrail/common";
import { readOwnedCombatProfile } from "./combat-owned-profile";
import { captureOwnedTrainBaseDefinitions } from "./combat-owned-base-definitions";
import { resolveCarriageType } from "./train-loadout";
import { z } from 'zod';

export function calculateOwnedTrainPermanentStats(levelInput: unknown) {
  const level = z.number().int().min(1).max(10).parse(levelInput);
  const definition = captureOwnedTrainBaseDefinitions();
  return { maxHp: Number(BigInt(definition.train.hp) * BigInt(10000 + definition.growth.hpPerLevelBps * (level - 1)) / 10000n),
    attack: Number(BigInt(definition.train.attack) * BigInt(10000 + definition.growth.outputPerLevelBps * (level - 1)) / 10000n),
    defense: definition.train.defense, basicIntervalMs: definition.train.basicIntervalMs };
}

/** Approved train extension over the same owner-scoped SQL intake. Does not save,
 * infer turret installations, rewrite previous intake versions or start a run.
 */
export async function readOwnedTrainCombatProfile(db: D1Database, accountId: string, input: unknown) {
  const original = await readOwnedCombatProfile(db, accountId, input);
  const definition = captureOwnedTrainBaseDefinitions();
  const scaleHp = (base: number, level: number) => Number(BigInt(base)
    * BigInt(10000 + definition.growth.hpPerLevelBps * (level - 1)) / 10000n);
  const scaleOutput = (base: number, level: number) => Number(BigInt(base)
    * BigInt(10000 + definition.growth.outputPerLevelBps * (level - 1)) / 10000n);
  // Prior output is historical evidence. Only this distinct contract corrects the
  // lifecycle diagnosis; no prerequisite is inferred from a missing optional system.
  const missing = original.missing.filter(reason => ![
    "predeparture_equipment_binding_unavailable", "account_research_ledger_unavailable",
    "train_carriage_base_stat_binding_unavailable", "turret_installation_binding_unavailable",
  ].includes(reason));
  const selectedTrain = { ...original.selectedTrain, role: "TRAIN",
    permanentStats: calculateOwnedTrainPermanentStats(original.selectedTrain.level) };
  const selectedCarriages = original.selectedCarriages.map(c => {
    const role = resolveCarriageType(c.carriageId);
    const base = role !== null && Object.hasOwn(definition.carriages, role)
      ? definition.carriages[role as keyof typeof definition.carriages] : null;
    if (!base) missing.push(`carriage_base_definition_missing:${c.carriageId}`);
    const device = base?.device;
    const deviceInput = device ? { ...device, ...("amount" in device ? { amount: scaleOutput(device.amount, c.level) } : {}) } : null;
    return { ...c, role, ownerAccountId: accountId,
      permanentStats: base ? { maxHp: scaleHp(base.hp, c.level), defense: base.defense } : null,
      deviceInput, ownsIndependentBasicFire: role === "GUN" ? false : null };
  });
  const turretSlots = selectedCarriages.filter(c => c.role === "GUN").flatMap(c => {
    // The actual legacy loadout has no saved slot-kind mapping. Absence is NOT
    // proof of a deliberately empty slot; preserve that distinction.
    missing.push(`turret_slot_selection_not_persisted:${c.carriageId}`);
    return [1, 2].map(slot => ({ carriageId: c.carriageId, slot, ownerAccountId: accountId,
      turretId: null, entityId: null, status: "SELECTION_UNBOUND" }));
  });
  const ownedTurrets = original.ownedTurrets.map(t => {
    missing.push(`turret_kind_definition_binding_missing:${t.turretId}`);
    return { ...t, ownerAccountId: accountId, permanentStats: null };
  });
  const { contentHash, ...source } = original;
  const payload = { ...source, schemaVersion: "combat.owned-train-intake.v1",
    ownedProfileContentHash: contentHash,
    trainDefinitionVersion: definition.version,
    trainDefinitionHash: `sha256:${await sha256Hex(canonicalJson(definition))}`,
    trainDefinitionReferences: definition.references,
    selectedTrain, selectedCarriages, ownedTurrets, turretSlots, missing,
    deferredInputs: [{ code: "run_equipment_effect_input_binding_unbound", stage: "RUN_ITEM_EFFECT_CONSUMPTION",
      requiredWhen: "ACTUAL_EQUIPPED_RUN_ITEMS_EXIST" }],
    applicability: {
      ...capturePredepartureEffectApplicability(),

      turretInstallation: { requiredMinimum: 0, perGunCarriageSlots: 2, status: "SELECTION_UNBOUND",
        source: "04_COMBAT_RUN.md#turret-ownership-loadout-v02" },
    },
  };
  return { ...payload, contentHash: `sha256:${await sha256Hex(canonicalJson(payload))}` };
}

/** Approved lifecycle policy shared by legacy intake and B independent preparation. */
export function capturePredepartureEffectApplicability(){return {
 equipment:{predepartureRequired:false,lifecycle:'RUN_OWNED',status:'NOT_APPLICABLE_BEFORE_RUN',source:'13_EQUIPMENT_ENCHANTMENTS.md#1 / equipment-reward-resolution-v02'},
 research:{predepartureRequired:false,status:'NO_APPROVED_ACTIVE_NODE_CATALOG',source:'35_CURRENT_ECONOMY_ANNEX.md#7'},
};}
