import { describe, expect, it } from "vitest";
import { canonicalJson } from "@lastrail/common";
import { ce03Inputs, ce03ProducerFixture } from "../helpers/ce03-producer-fixture";
import { prepareLocalCombatDraft } from "../../apps/game-api/src/ce03-local-producer";
import { templateSchema, enemyIds } from "../../apps/game-api/src/ce03-local-schema";
import { describeCombatRequirements } from "../../apps/game-api/src/combat-readiness";

async function preparedDraft() {
  const f = await ce03ProducerFixture();
  try {
    const before = f.sql.export();
    const prepared = await (await f.request()).json();
    const draft = await prepareLocalCombatDraft(ce03Inputs(), prepared.ownedSelection, {
      targetingPolicyId: "ce03.frontline-first-v1", ultimatePolicyId: "ce03.per-operator-ultimate-v1", activePolicyId: "ce03.general-active-auto-v1"
    });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    return draft;
  } finally { f.sql.close(); }
}

describe("second-final integration boundary on existing .003 contract", () => {
  it("retains the existing fixed proposal package, not branch offers", async () => {
    const draft = await preparedDraft();
    const source = JSON.parse(ce03Inputs().selectionJson);
    expect(draft.template.package.scope).toBe("ISOLATED_PROPOSAL_KEYS");
    expect(draft.template.package.augments).toEqual(source.package.augments);
    expect(draft.template.package.tactic).toEqual(source.package.tactic);
    expect(draft.template.package).not.toHaveProperty("offers");
    expect(draft.template.package).not.toHaveProperty("selectedAugmentId");
  });
  it.each(["route", "branchChoices", "throttle", "distance", "controlEvents"])("does not silently extend .003 with %s", async field => {
    const draft = await preparedDraft(), before = canonicalJson(draft);
    expect(templateSchema.safeParse({ ...draft.template, [field]: null }).success).toBe(false);
    expect(canonicalJson(draft)).toBe(before);
  });
  it("keeps actor/spawn identity and completion gates distinct from artillery passage", async () => {
    const { template } = await preparedDraft();
    expect(template.enemyDefinitions.map(e => e.definitionId)).toEqual([...enemyIds]);
    expect(template.cars.map(c => c.type)).toEqual(["Combat", "Gun"]);
    expect(template.waves).toHaveLength(15);
    for (const wave of template.waves) for (const spawn of wave.spawns) {
      expect(spawn.requiredForCompletion).toBe(true);
      expect(spawn.resolvedMaxHp).toBeGreaterThan(0);
      expect(spawn).not.toHaveProperty("exposureDistance");
    }
    expect(template.train.stats.speedNumerator).toBeGreaterThan(0);
    expect(template.train.stats.speedDenominator).toBeGreaterThan(0);
    expect(template.execution).toMatchObject({ stepMs: 50, clock: "INTEGER_FIXED_STEP", noWallClockCatchup: true });
  });
  it("does not turn package references into an approved branch catalog or readiness", () => {
    const requirements = describeCombatRequirements(JSON.stringify({ operationId: "opn_01_last_departure", contentVersion: "act1.launch.content.v1",
      startAugmentPackageRef: "act1_full_start_package_v1", startTacticPackageRef: "act1_full_start_package_v1", allowedOperatorSlots: 5 }), "I");
    expect(requirements).toMatchObject({ status: "BLOCKED", canStart: false,
      requiredContent: { startAugmentPackageRef: "act1_full_start_package_v1", bindingVersion: null, catalogHash: null } });
    expect(requirements).not.toHaveProperty("branchChoices");
  });
});
