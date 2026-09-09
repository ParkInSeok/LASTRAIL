import { z } from "zod";

// This schema validates an unpublished planning baseline; parsing never activates it.
const quantity = z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const positive = quantity.min(1);
const pair = z.tuple([quantity, quantity]);
const triple = z.tuple([quantity, quantity, quantity]);
const resources = z.tuple([quantity, quantity, quantity, quantity]);
const increasing = (length: number) => z.array(positive).length(length).refine(
  values => values.every((value, index) => index === 0 || value >= values[index - 1]!),
  "Values must not decrease",
);

export const launchEconomySchema = z.strictObject({
  version: z.string().regex(/^act1\.economy\.\d{4}-\d{2}-\d{2}\.\d{3}$/u),
  status: z.literal("NUMERIC_BASELINE_FOR_IMPLEMENTATION"),
  deployed: z.literal(false),
  resourceOrder: z.tuple([z.literal("credits"), z.literal("parts"), z.literal("data"), z.literal("operatorExp")]),
  offline: z.strictObject({
    hourly: resources, progressBps: increasing(8), capHours: increasing(8),
    minimumSeconds: positive, crystals: z.literal(0),
  }),
  ordinaryRun: z.strictObject({
    baseMaterials: z.array(triple).length(6), operationBps: increasing(6), riskBps: increasing(5),
    expByRisk: increasing(5), expUsesOperationMultiplier: z.literal(false),
    failureExpBpsPerClearedFraction: quantity.max(10000),
    firstClearMaterials: z.array(triple).length(6), firstClearSpecialCores: z.array(quantity).length(6),
    ordinaryWaves: z.literal(15),
  }),
  storyRaid: z.strictObject({
    repeatMaterials: triple, repeatExp: quantity, firstClearMaterials: triple,
    firstClearSpecialCores: quantity,
    failureRewards: z.tuple([z.literal(0), z.literal(0), z.literal(0), z.literal(0)]),
    failureStaminaRefund: z.literal(true),
  }),
  activities: z.strictObject({
    dailyMaterials: resources, weeklyMaterials: resources, dailyCrystals: quantity, weeklyCrystals: quantity,
    attendanceCreditsPerGrantByWeek: z.array(quantity).length(4),
    attendancePartsByWeek: z.array(quantity).length(4), attendanceDataByWeek: z.array(quantity).length(4),
    attendanceExpByWeek: z.array(quantity).length(4), attendanceCrystalsBySeventhCell: z.array(quantity).length(4),
    attendanceCrystalsFirstCellEachWeek: quantity, attendanceEliteTickets: quantity,
    passPatternMaterials: z.array(resources).length(10), passPatternCrystals: z.array(quantity).length(10),
    passLevels: z.literal(50), premiumExtraMultiplier: z.literal(5),
    passDailyXp: z.array(positive).length(3), passWeeklyXp: z.array(positive).length(3), passXpPerLevel: positive,
  }),
  growth: z.strictObject({
    operatorExpSteps1to10: z.array(positive).length(9), operatorExpStep10to20: positive,
    operatorExpStep20to30: positive, operatorExpStep30to40: positive, operatorExpStep40to50: positive,
    operatorMaxLevel: z.literal(50), operatorSlots: z.literal(5), refundActualInvestedExpBps: z.literal(10000),
    trainCoreStepsCreditsPartsCores: z.array(triple).length(9),
    equipmentStepsCreditsParts: z.array(pair).length(4), equipmentMaxLevel: z.literal(5),
    skillStepsCreditsData: z.array(pair).length(4), promotionStepsCreditsDataCores: z.array(triple).length(3),
    researchStepsCreditsData: z.array(pair).length(5),
    unknownTargetPolicy: z.literal("REJECT_WITHOUT_SPENDING"),
    researchEnabledWithoutApprovedNodeCatalog: z.literal(false),
  }),
  worldBoss: z.strictObject({
    scoreTierMaterials: z.array(triple).length(5), rankMaterials: z.array(triple).length(7),
    oneScoreTrackAndBestRankPerAccountEvent: z.literal(true),
    crystals: z.literal(0), exp: z.literal(0), specialCores: z.literal(0),
  }),
  shop: z.strictObject({
    ordinaryTicketCrystals: positive, dailyTicketCrystals: positive, dailyTicketLimit: z.literal(1),
    paidCrystalsSpentAfterFree: z.literal(true), creditToRecruitmentExchange: z.literal(false),
  }),
});

export type LaunchEconomy = z.infer<typeof launchEconomySchema>;

export function parseLaunchEconomy(value: unknown, expectedVersion: string): LaunchEconomy {
  const config = launchEconomySchema.parse(value);
  if (config.version !== expectedVersion) throw new Error("economy_version_mismatch");
  return config;
}

/** Quotes only. Does not mutate a wallet, activate targets, or calculate refunds. */
export function trainingCoreExpCost(config: LaunchEconomy, fromLevel: number, toLevel: number): number {
  if (!Number.isInteger(fromLevel) || !Number.isInteger(toLevel)
    || fromLevel < 1 || toLevel > config.growth.operatorMaxLevel || toLevel < fromLevel) {
    throw new Error("training_core_level_invalid");
  }
  const growth = config.growth;
  let total = 0;
  for (let level = fromLevel; level < toLevel; level++) {
    total += level < 10 ? growth.operatorExpSteps1to10[level - 1]!
      : level < 20 ? growth.operatorExpStep10to20
        : level < 30 ? growth.operatorExpStep20to30
          : level < 40 ? growth.operatorExpStep30to40 : growth.operatorExpStep40to50;
    if (!Number.isSafeInteger(total)) throw new Error("economy_cost_overflow");
  }
  return total;
}

/** Ordinary victory EXP is risk-only; operation/material multipliers do not apply. */
export function ordinaryVictoryExp(config: LaunchEconomy, risk: number): number {
  if (!Number.isInteger(risk) || risk < 1 || risk > 5) throw new Error("ordinary_risk_invalid");
  return config.ordinaryRun.expByRisk[risk - 1]!;
}
