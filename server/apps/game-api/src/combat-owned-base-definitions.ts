import { nyxApprovedDefinitionV2 } from './combat-nyx-approved-source';
/** Only base HP/ATK/basic timing from preserved 44 base table, retained by approved F2.
 * Roles, old armor, selected-ultimate and old kits from that table are NOT imported.
 * Current numeric DEF and skills remain owned by combat-policy-catalog/binding.
 */
const definition = {
  version: "combat.owned-base.2026-10-01.1",
  references: ["source-5bcf7c7f9134d9bc-operator-base-table", "combat-closeout-f1-f3-v01",
    "source-5bcf7c7f9134d9bc-persistent-stat-growth", "operator-promotion-effects-v01",
    "operator-skill-training-effects-v01"],
  operators: {
    op_kyle: { hp: 1400, attack: 36, basicIntervalMs: 1800 },
    op_reina: { hp: 900, attack: 50, basicIntervalMs: 1400 },
    op_eve: { hp: 950, attack: 28, basicIntervalMs: 2000 },
    op_sera: { hp: 920, attack: 40, basicIntervalMs: 1200 },
    op_mira: { hp: 1000, attack: 30, basicIntervalMs: 1800 },
  },
  growth: { hpPerLevelBps: 400, attackPerLevelBps: 350, skillPerRankBps: 500,
    // Latest 35 approved cumulative promotion supersedes historical +5% per tier.
    promotionHpBps: [0, 500, 500, 1000], promotionAttackBps: [0, 0, 500, 1000] },
};
export function captureOwnedCombatBaseDefinitions() { return structuredClone(definition); }
export function captureNyxOwnedCombatBaseDefinitions() {
 const previous=captureOwnedCombatBaseDefinitions(), source=nyxApprovedDefinitionV2();
 return {...previous,version:'combat.owned-base.nyx.2026-11-07.1',references:[...previous.references,...source.references],
  operators:{...previous.operators,op_nyx:{hp:source.base.hp,attack:source.base.attack,basicIntervalMs:source.base.basicIntervalMs}}};
}

// Separate immutable extension: never changes the operator intake's published v1/hash.
// HP/device bases retained by approved44 F2; DEF is approved v12, not an armor conversion.
const trainDefinition = {
  version: "combat.owned-train-base.2026-10-01.1",
  references: ["combat-closeout-f1-f3-v01", "source-836281eb9f35fa72-level-table-no-equipment-or-research",
    "source-836281eb9f35fa72-carriage-and-device-scaling", "def-shield-effect-mapping-v12",
    "gun-carriage-two-turrets-v01", "turret-ownership-loadout-v02", "turret-permanent-progression-v01"],
  train: { hp: 5000, attack: 60, defense: 111, basicIntervalMs: 1000 },
  carriages: {
    COMBAT: { hp: 1100, defense: 111, device: { amount: 24, intervalMs: 1600 } },
    GUN: { hp: 1000, defense: 87, device: null },
    CARGO: { hp: 900, defense: 53, device: { speedBps: 1000, intervalMs: 12000, durationMs: 6000 } },
    MEDICAL: { hp: 950, defense: 53, device: { amount: 80, intervalMs: 8000 } },
  },
  growth: { hpPerLevelBps: 800, outputPerLevelBps: 600 },
  // No stable turret-kind ID binding exists in current account catalog. Numeric
  // test-turret40/600 is not a definition for every owned kind.
  turretKinds: null,
};
export function captureOwnedTrainBaseDefinitions() { return structuredClone(trainDefinition); }
