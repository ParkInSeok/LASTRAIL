export const packageName = "@lastrail/balance";

export type RunRiskTier = "I" | "II" | "III" | "IV" | "V";
export type RunOutcome = "VICTORY" | "FAILURE" | "ABANDONED";
export type IntegrityDisposition =
  | "ALLOW"
  | "ALLOW_PREVIEW_WITH_FLAG"
  | "INVALIDATE_PREVIEW";
export type RewardLineKind = "CURRENCY" | "ITEM";
export type OfflineStoryProgressKey =
  | "none"
  | "opn_01_last_departure"
  | "opn_02_first_signal"
  | "opn_03_water_stop"
  | "opn_04_scrapstar_show"
  | "opn_05_night_boarders"
  | "opn_06_broken_line"
  | "raid_raider_king_engine";

export type RunRewardLineConfig = {
  kind: RewardLineKind;
  id: string;
  quantity: number;
};

export type RunRewardLine = RunRewardLineConfig & {
  source: "RUN_CLEAR" | "FIRST_CLEAR";
};

export type RunRewardsConfig = {
  schemaVersion: number;
  riskMultipliersBps: Partial<Record<RunRiskTier, number>>;
  operationProgressionMultipliersBps: Record<string, number>;
  outcomePolicy: {
    victoryFullClearBps: number;
    failureBaseBps: number;
    failureWaveBps: number;
    failureCapBps: number;
    abandonedBps: number;
  };
  operations: Record<
    string,
    {
      baseRewards: RunRewardLineConfig[];
      firstClearRewards: RunRewardLineConfig[];
    }
  >;
  integrityPolicy: {
    softAnomaly: IntegrityDisposition;
    hardViolation: IntegrityDisposition;
    quarantinedRun: IntegrityDisposition;
  };
};

export type RunRewardPreviewCalculation = {
  operationId: string;
  actId: string;
  regionId: string;
  risk: RunRiskTier;
  outcome: RunOutcome;
  clearedWaves: number;
  operationProgressionMultiplierBps: number;
  riskMultiplierBps: number;
  outcomeMultiplierBps: number;
  waveCompletionMultiplierBps: number;
  firstClearEligible: boolean;
  firstClearApplied: boolean;
  integrityDisposition: IntegrityDisposition;
};

export type RunRewardPreviewPlan = {
  rewardLines: RunRewardLine[];
  calculation: RunRewardPreviewCalculation;
};

export type CalculateRunRewardPreviewOptions = {
  config: RunRewardsConfig;
  operationId: string;
  actId: string;
  regionId: string;
  risk: RunRiskTier;
  outcome: RunOutcome;
  clearedWaves: number;
  firstClearEligible: boolean;
  integrityDisposition: IntegrityDisposition;
};

export type OfflineRewardsConfig = {
  schemaVersion: number;
  minimumOfflineMinutes: number;
  storyProgressMultipliersBps: Record<OfflineStoryProgressKey, number>;
  storageCapHours: Record<OfflineStoryProgressKey, number>;
  baseHourlyRewards: RunRewardLineConfig[];
  blockedDefaultRewardIds: string[];
  maxClaimableHours: number;
  integrityPolicy: {
    clockTamper: "REJECT_CLAIM";
    staleClientState: "RECALCULATE_SERVER_SIDE";
    maintenanceMode: "DEFER_CLAIM";
  };
};

export type LaunchEconomyBalanceDraft = {
  status: "TESTING_TUNING_DRAFT";
  productionFinal: false;
  seedPromotionApproved: false;
  activeRunBaseRepeatRewards: Record<
    string,
    {
      cur_credits: number;
      cur_machine_parts: number;
      cur_tactical_data: number;
      cur_operator_exp: number;
    }
  >;
  activeRunFirstClearRewards: Record<
    string,
    {
      cur_credits: number;
      cur_machine_parts: number;
      cur_tactical_data: number;
      candidateRewards: Array<{ id: string; quantity: number }>;
    }
  >;
  offlineBaseHourlyRewards: {
    cur_credits: number;
    cur_machine_parts: number;
    cur_tactical_data: number;
    cur_operator_exp: number;
  };
  offlineRepeatBlockedRewardIds: string[];
  normalActOneRunBlockedRewardIds: string[];
  candidateOnlyRewardIds: string[];
  gachaTicketAcquisition: {
    status: "LAUNCH_CONFIG";
    totalNonRepeatTickets: 12;
    sources: Array<{
      sourceType: "LOGIN_BONUS" | "FIRST_CLEAR" | "STORY_RAID_FIRST_CLEAR";
      sourceId: string;
      itemId: "cur_command_ticket";
      quantity: number;
      repeatable: false;
    }>;
  };
};

export type GachaResultRarity = "COMMON" | "RARE" | "ELITE";
export type GachaResultType =
  | "ELITE_OPERATOR"
  | "RARE_OPERATOR"
  | "ELITE_OPERATOR_TOKEN"
  | "RARE_OPERATOR_TOKEN"
  | "TRAINING_MATERIAL"
  | "WORK_SKILL_MATERIAL"
  | "TACTICAL_MATERIAL"
  | "COMMAND_TICKET_FRAGMENT";
export type GachaGuaranteeType =
  | "NONE"
  | "RARE_OR_HIGHER_10"
  | "ELITE_30"
  | "PICKUP_ELITE_60";

export type LaunchGachaPoolResultDraft = {
  resultRarity: GachaResultRarity;
  resultType: GachaResultType;
  resultId: string;
  weight: number;
  quantity?: number;
  isPickup?: boolean;
};

export type LaunchGachaDataDraft = {
  status: "TESTING_TUNING_DRAFT";
  productionFinal: false;
  seedPromotionApproved: false;
  actOneEliteOperatorIds: string[];
  banners: Array<{
    bannerId: string;
    bannerType: "PICKUP";
    pickupOperatorId: string;
    balanceConfigVersionRef: "TESTING_TUNING_DRAFT";
    pityConfig: {
      rare: 10;
      elite: 30;
      pickup: 60;
    };
    cost: {
      itemId: "cur_command_ticket";
      quantityPerPull: 1;
    };
    allowedPullCounts: [1, 10];
    pool: {
      results: LaunchGachaPoolResultDraft[];
    };
  }>;
  supportedGuaranteeTypes: GachaGuaranteeType[];
  clientVisibleResultFields: string[];
  duplicateConversion: {
    eliteOperatorTokenQuantity: 50;
    rareOperatorTokenQuantity: 30;
    displayItemId: "cur_operator_token";
    persistedItemIdPattern: "cur_operator_token:<operatorId>";
  };
};

export type OfflineRewardPreviewCalculation = {
  storyProgressKey: OfflineStoryProgressKey;
  storyProgressMultiplierBps: number;
  offlineStorageCapHours: number;
  actualOfflineSeconds: number;
  claimableSeconds: number;
  minimumOfflineMinutes: number;
};

export type OfflineRewardPreviewPlan = {
  rewardLines: Array<RunRewardLineConfig & { source: "OFFLINE_REWARD" }>;
  calculation: OfflineRewardPreviewCalculation;
};

export type CalculateOfflineRewardPreviewOptions = {
  config: OfflineRewardsConfig;
  storyProgressKey: string;
  actualOfflineSeconds: number;
};

const basisPoints = 10000;
const maxRunWaves = 15;
const supportedRunRewardsSchemaVersion = 1;
const supportedOfflineRewardsSchemaVersion = 1;
const runRiskTiers: RunRiskTier[] = ["I", "II", "III", "IV", "V"];
const rewardLineKinds: RewardLineKind[] = ["CURRENCY", "ITEM"];
const offlineStoryProgressKeys: OfflineStoryProgressKey[] = [
  "none",
  "opn_01_last_departure",
  "opn_02_first_signal",
  "opn_03_water_stop",
  "opn_04_scrapstar_show",
  "opn_05_night_boarders",
  "opn_06_broken_line",
  "raid_raider_king_engine",
];
const integrityDispositions: IntegrityDisposition[] = [
  "ALLOW",
  "ALLOW_PREVIEW_WITH_FLAG",
  "INVALIDATE_PREVIEW",
];

export const launchRunRewardsConfig: RunRewardsConfig = {
  schemaVersion: 1,
  riskMultipliersBps: {
    I: 8000,
    II: 10000,
    III: 12000,
    IV: 14500,
    V: 17500,
  },
  operationProgressionMultipliersBps: {
    opn_01_last_departure: 10000,
    opn_02_first_signal: 11000,
    opn_03_water_stop: 12000,
    opn_04_scrapstar_show: 13000,
    opn_05_night_boarders: 14000,
    opn_06_broken_line: 15500,
    raid_raider_king_engine: 18000,
  },
  outcomePolicy: {
    victoryFullClearBps: 10000,
    failureBaseBps: 2500,
    failureWaveBps: 5500,
    failureCapBps: 8000,
    abandonedBps: 0,
  },
  operations: {
    opn_01_last_departure: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 300 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 700 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_machine_parts", quantity: 100 },
      ],
    },
    opn_02_first_signal: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 330 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 760 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_machine_parts", quantity: 120 },
      ],
    },
    opn_03_water_stop: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 360 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 820 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_machine_parts", quantity: 160 },
        { kind: "ITEM", id: "cur_command_ticket", quantity: 1 },
      ],
    },
    opn_04_scrapstar_show: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 390 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 900 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_tactical_data", quantity: 120 },
      ],
    },
    opn_05_night_boarders: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 430 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 980 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_tactical_data", quantity: 160 },
        { kind: "ITEM", id: "cur_command_ticket", quantity: 1 },
      ],
    },
    opn_06_broken_line: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 480 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 1100 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_special_core", quantity: 1 },
        { kind: "ITEM", id: "cur_command_ticket", quantity: 2 },
      ],
    },
    raid_raider_king_engine: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 720 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 1600 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_special_core", quantity: 2 },
        { kind: "ITEM", id: "cur_command_ticket", quantity: 3 },
      ],
    },
  },
  integrityPolicy: {
    softAnomaly: "ALLOW_PREVIEW_WITH_FLAG",
    hardViolation: "INVALIDATE_PREVIEW",
    quarantinedRun: "INVALIDATE_PREVIEW",
  },
};

export const launchOfflineRewardsConfig: OfflineRewardsConfig = {
  schemaVersion: 1,
  minimumOfflineMinutes: 10,
  storyProgressMultipliersBps: {
    none: 10000,
    opn_01_last_departure: 11500,
    opn_02_first_signal: 13200,
    opn_03_water_stop: 15200,
    opn_04_scrapstar_show: 17500,
    opn_05_night_boarders: 20500,
    opn_06_broken_line: 24000,
    raid_raider_king_engine: 30000,
  },
  storageCapHours: {
    none: 4,
    opn_01_last_departure: 5,
    opn_02_first_signal: 6,
    opn_03_water_stop: 7,
    opn_04_scrapstar_show: 8,
    opn_05_night_boarders: 9,
    opn_06_broken_line: 10,
    raid_raider_king_engine: 12,
  },
  baseHourlyRewards: [
    { kind: "CURRENCY", id: "cur_credits", quantity: 100 },
    { kind: "CURRENCY", id: "cur_machine_parts", quantity: 50 },
    { kind: "CURRENCY", id: "cur_tactical_data", quantity: 30 },
    { kind: "CURRENCY", id: "cur_operator_exp", quantity: 20 },
  ],
  blockedDefaultRewardIds: [
    "cur_special_core",
    "cur_command_ticket",
    "cur_command_ticket_fragment",
  ],
  maxClaimableHours: 12,
  integrityPolicy: {
    clockTamper: "REJECT_CLAIM",
    staleClientState: "RECALCULATE_SERVER_SIDE",
    maintenanceMode: "DEFER_CLAIM",
  },
};

export const launchEconomyBalanceDraft: LaunchEconomyBalanceDraft = {
  status: "TESTING_TUNING_DRAFT",
  productionFinal: false,
  seedPromotionApproved: false,
  activeRunBaseRepeatRewards: {
    opn_01_last_departure: {
      cur_credits: 120,
      cur_machine_parts: 8,
      cur_tactical_data: 4,
      cur_operator_exp: 700,
    },
    opn_02_first_signal: {
      cur_credits: 135,
      cur_machine_parts: 9,
      cur_tactical_data: 5,
      cur_operator_exp: 780,
    },
    opn_03_water_stop: {
      cur_credits: 150,
      cur_machine_parts: 11,
      cur_tactical_data: 6,
      cur_operator_exp: 860,
    },
    opn_04_scrapstar_show: {
      cur_credits: 170,
      cur_machine_parts: 13,
      cur_tactical_data: 7,
      cur_operator_exp: 950,
    },
    opn_05_night_boarders: {
      cur_credits: 195,
      cur_machine_parts: 15,
      cur_tactical_data: 8,
      cur_operator_exp: 1050,
    },
    opn_06_broken_line: {
      cur_credits: 225,
      cur_machine_parts: 18,
      cur_tactical_data: 10,
      cur_operator_exp: 1180,
    },
    raid_raider_king_engine: {
      cur_credits: 360,
      cur_machine_parts: 28,
      cur_tactical_data: 16,
      cur_operator_exp: 1600,
    },
  },
  activeRunFirstClearRewards: {
    opn_01_last_departure: {
      cur_credits: 250,
      cur_machine_parts: 15,
      cur_tactical_data: 8,
      candidateRewards: [],
    },
    opn_02_first_signal: {
      cur_credits: 300,
      cur_machine_parts: 18,
      cur_tactical_data: 12,
      candidateRewards: [],
    },
    opn_03_water_stop: {
      cur_credits: 360,
      cur_machine_parts: 24,
      cur_tactical_data: 14,
      candidateRewards: [],
    },
    opn_04_scrapstar_show: {
      cur_credits: 430,
      cur_machine_parts: 30,
      cur_tactical_data: 18,
      candidateRewards: [{ id: "cur_command_ticket_fragment", quantity: 1 }],
    },
    opn_05_night_boarders: {
      cur_credits: 520,
      cur_machine_parts: 38,
      cur_tactical_data: 22,
      candidateRewards: [],
    },
    opn_06_broken_line: {
      cur_credits: 650,
      cur_machine_parts: 48,
      cur_tactical_data: 28,
      candidateRewards: [{ id: "cur_special_core", quantity: 1 }],
    },
    raid_raider_king_engine: {
      cur_credits: 1000,
      cur_machine_parts: 75,
      cur_tactical_data: 45,
      candidateRewards: [{ id: "cur_special_core", quantity: 2 }],
    },
  },
  offlineBaseHourlyRewards: {
    cur_credits: 24,
    cur_machine_parts: 2,
    cur_tactical_data: 1,
    cur_operator_exp: 80,
  },
  offlineRepeatBlockedRewardIds: [
    "cur_special_core",
    "cur_command_ticket",
    "cur_command_ticket_fragment",
  ],
  normalActOneRunBlockedRewardIds: ["cur_command_ticket"],
  candidateOnlyRewardIds: ["cur_command_ticket_fragment", "cur_special_core"],
  gachaTicketAcquisition: {
    status: "LAUNCH_CONFIG",
    totalNonRepeatTickets: 12,
    sources: [
      {
        sourceType: "LOGIN_BONUS",
        sourceId: "launch_login_bonus",
        itemId: "cur_command_ticket",
        quantity: 5,
        repeatable: false,
      },
      {
        sourceType: "FIRST_CLEAR",
        sourceId: "opn_03_water_stop",
        itemId: "cur_command_ticket",
        quantity: 1,
        repeatable: false,
      },
      {
        sourceType: "FIRST_CLEAR",
        sourceId: "opn_05_night_boarders",
        itemId: "cur_command_ticket",
        quantity: 1,
        repeatable: false,
      },
      {
        sourceType: "FIRST_CLEAR",
        sourceId: "opn_06_broken_line",
        itemId: "cur_command_ticket",
        quantity: 2,
        repeatable: false,
      },
      {
        sourceType: "STORY_RAID_FIRST_CLEAR",
        sourceId: "raid_raider_king_engine",
        itemId: "cur_command_ticket",
        quantity: 3,
        repeatable: false,
      },
    ],
  },
};

export const launchGachaDataDraft: LaunchGachaDataDraft = {
  status: "TESTING_TUNING_DRAFT",
  productionFinal: false,
  seedPromotionApproved: false,
  actOneEliteOperatorIds: [
    "op_vex",
    "op_nyx",
    "op_sable",
    "op_marrow",
    "op_iris",
    "op_rail",
    "op_anchor",
    "op_lumen",
    "op_cinder",
    "op_warden",
  ],
  banners: [
    {
      bannerId: "banner_act1_pickup_vex_testing",
      bannerType: "PICKUP",
      pickupOperatorId: "op_vex",
      balanceConfigVersionRef: "TESTING_TUNING_DRAFT",
      pityConfig: {
        rare: 10,
        elite: 30,
        pickup: 60,
      },
      cost: {
        itemId: "cur_command_ticket",
        quantityPerPull: 1,
      },
      allowedPullCounts: [1, 10],
      pool: {
        results: [
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_vex",
            weight: 120,
            isPickup: true,
          },
          { resultRarity: "ELITE", resultType: "ELITE_OPERATOR", resultId: "op_nyx", weight: 80 },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_sable",
            weight: 80,
          },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_marrow",
            weight: 80,
          },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_iris",
            weight: 80,
          },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_rail",
            weight: 80,
          },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_anchor",
            weight: 80,
          },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_lumen",
            weight: 80,
          },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_cinder",
            weight: 80,
          },
          {
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR",
            resultId: "op_warden",
            weight: 80,
          },
          {
            resultRarity: "RARE",
            resultType: "RARE_OPERATOR",
            resultId: "op_signal_runner",
            weight: 500,
          },
          {
            resultRarity: "RARE",
            resultType: "RARE_OPERATOR_TOKEN",
            resultId: "cur_operator_token",
            weight: 800,
            quantity: 10,
          },
          {
            resultRarity: "RARE",
            resultType: "TRAINING_MATERIAL",
            resultId: "mat_rare_manual",
            weight: 1200,
            quantity: 1,
          },
          {
            resultRarity: "COMMON",
            resultType: "TRAINING_MATERIAL",
            resultId: "mat_scrap",
            weight: 4200,
            quantity: 1,
          },
          {
            resultRarity: "COMMON",
            resultType: "WORK_SKILL_MATERIAL",
            resultId: "mat_work_skill_basic",
            weight: 2500,
            quantity: 1,
          },
          {
            resultRarity: "COMMON",
            resultType: "TACTICAL_MATERIAL",
            resultId: "mat_tactical_data_pack",
            weight: 1100,
            quantity: 1,
          },
        ],
      },
    },
  ],
  supportedGuaranteeTypes: [
    "NONE",
    "RARE_OR_HIGHER_10",
    "ELITE_30",
    "PICKUP_ELITE_60",
  ],
  clientVisibleResultFields: [
    "pullSequence",
    "resultRarity",
    "resultType",
    "resultId",
    "reward",
    "isPickup",
    "guaranteeType",
    "wasDuplicate",
    "duplicateConversion",
  ],
  duplicateConversion: {
    eliteOperatorTokenQuantity: 50,
    rareOperatorTokenQuantity: 30,
    displayItemId: "cur_operator_token",
    persistedItemIdPattern: "cur_operator_token:<operatorId>",
  },
};

export function parseRunRewardsConfig(value: unknown): RunRewardsConfig {
  const root = requireRecord(value, "risk_reward_policy_missing");
  if (root.schemaVersion !== supportedRunRewardsSchemaVersion) {
    throw new Error("risk_reward_policy_missing");
  }

  const riskMultipliersBps = parseRiskMultipliers(root.riskMultipliersBps);
  const operationProgressionMultipliersBps = parseOperationProgressionMultipliers(
    root.operationProgressionMultipliersBps,
  );
  const operations = parseRunRewardOperations(root.operations);

  return {
    schemaVersion: supportedRunRewardsSchemaVersion,
    riskMultipliersBps,
    operationProgressionMultipliersBps,
    outcomePolicy: parseOutcomePolicy(root.outcomePolicy),
    operations,
    integrityPolicy: parseIntegrityPolicy(root.integrityPolicy),
  };
}

export function parseOfflineRewardsConfig(value: unknown): OfflineRewardsConfig {
  const root = requireRecord(value, "offline_reward_policy_invalid");
  if (root.schemaVersion !== supportedOfflineRewardsSchemaVersion || "riskMultipliersBps" in root) {
    throw new Error("offline_reward_policy_invalid");
  }

  const blockedDefaultRewardIds = parseStringArray(root.blockedDefaultRewardIds, "offline_reward_policy_invalid");
  const baseHourlyRewards = parseRewardLines(root.baseHourlyRewards);
  if (baseHourlyRewards.some((line) => blockedDefaultRewardIds.includes(line.id))) {
    throw new Error("offline_reward_policy_invalid");
  }

  return {
    schemaVersion: supportedOfflineRewardsSchemaVersion,
    minimumOfflineMinutes: requirePositiveInteger(root.minimumOfflineMinutes, "offline_reward_policy_invalid"),
    storyProgressMultipliersBps: parseOfflineStoryProgressNumbers(root.storyProgressMultipliersBps),
    storageCapHours: parseOfflineStoryProgressNumbers(root.storageCapHours),
    baseHourlyRewards,
    blockedDefaultRewardIds,
    maxClaimableHours: requirePositiveInteger(root.maxClaimableHours, "offline_reward_policy_invalid"),
    integrityPolicy: parseOfflineIntegrityPolicy(root.integrityPolicy),
  };
}

export function calculateRunRewardPreview(
  options: CalculateRunRewardPreviewOptions,
): RunRewardPreviewPlan {
  assertClearedWaves(options.clearedWaves);
  const operation = options.config.operations[options.operationId];
  if (operation === undefined) {
    throw new Error("risk_reward_policy_missing");
  }

  const operationProgressionMultiplierBps =
    options.config.operationProgressionMultipliersBps[options.operationId];
  if (!isPositiveSafeInteger(operationProgressionMultiplierBps)) {
    throw new Error("risk_reward_policy_missing");
  }

  const riskMultiplierBps = options.config.riskMultipliersBps[options.risk];
  if (!isPositiveSafeInteger(riskMultiplierBps)) {
    throw new Error("reward_multiplier_missing");
  }

  const outcomeMultiplierBps = getOutcomeMultiplierBps(options.config, options.outcome);
  const waveCompletionMultiplierBps = getWaveCompletionMultiplierBps(
    options.config,
    options.outcome,
    options.clearedWaves,
  );
  const firstClearApplied =
    options.firstClearEligible &&
    options.outcome === "VICTORY" &&
    options.clearedWaves === maxRunWaves;

  return {
    rewardLines: [
      ...operation.baseRewards
        .map((line) =>
          scaleRewardLine(
            line,
            operationProgressionMultiplierBps,
            riskMultiplierBps,
            outcomeMultiplierBps,
            waveCompletionMultiplierBps,
          ),
        )
        .filter((line) => line.quantity > 0),
      ...(firstClearApplied
        ? operation.firstClearRewards.map((line) => ({
            ...line,
            source: "FIRST_CLEAR" as const,
          }))
        : []),
    ],
    calculation: {
      operationId: options.operationId,
      actId: options.actId,
      regionId: options.regionId,
      risk: options.risk,
      outcome: options.outcome,
      clearedWaves: options.clearedWaves,
      operationProgressionMultiplierBps,
      riskMultiplierBps,
      outcomeMultiplierBps,
      waveCompletionMultiplierBps,
      firstClearEligible: options.firstClearEligible,
      firstClearApplied,
      integrityDisposition: options.integrityDisposition,
    },
  };
}

export function calculateOfflineRewardPreview(
  options: CalculateOfflineRewardPreviewOptions,
): OfflineRewardPreviewPlan {
  if (
    !Number.isFinite(options.actualOfflineSeconds) ||
    options.actualOfflineSeconds < 0
  ) {
    throw new Error("contract_violation");
  }
  const config = parseOfflineRewardsConfig(options.config);
  const storyProgressKey = requireOfflineStoryProgressKey(options.storyProgressKey);
  const storyProgressMultiplierBps = config.storyProgressMultipliersBps[storyProgressKey];
  const offlineStorageCapHours = config.storageCapHours[storyProgressKey];
  const actualOfflineSeconds = Math.floor(options.actualOfflineSeconds);
  const minimumOfflineSeconds = config.minimumOfflineMinutes * 60;
  const claimableSeconds = actualOfflineSeconds < minimumOfflineSeconds
    ? 0
    : Math.min(
        actualOfflineSeconds,
        offlineStorageCapHours * 60 * 60,
        config.maxClaimableHours * 60 * 60,
      );

  return {
    rewardLines: claimableSeconds === 0
      ? []
      : config.baseHourlyRewards
          .map((line) => ({
            ...line,
            quantity: Math.floor(
              (line.quantity * storyProgressMultiplierBps * claimableSeconds) /
                basisPoints /
                3600,
            ),
            source: "OFFLINE_REWARD" as const,
          }))
          .filter((line) => line.quantity > 0),
    calculation: {
      storyProgressKey,
      storyProgressMultiplierBps,
      offlineStorageCapHours,
      actualOfflineSeconds,
      claimableSeconds,
      minimumOfflineMinutes: config.minimumOfflineMinutes,
    },
  };
}

function scaleRewardLine(
  line: RunRewardLineConfig,
  operationProgressionMultiplierBps: number,
  riskMultiplierBps: number,
  outcomeMultiplierBps: number,
  waveCompletionMultiplierBps: number,
): RunRewardLine {
  const quantity = Math.floor(
    (line.quantity *
      operationProgressionMultiplierBps *
      riskMultiplierBps *
      outcomeMultiplierBps *
      waveCompletionMultiplierBps) /
      basisPoints /
      basisPoints /
      basisPoints /
      basisPoints,
  );
  return {
    ...line,
    quantity,
    source: "RUN_CLEAR",
  };
}

function getOutcomeMultiplierBps(config: RunRewardsConfig, outcome: RunOutcome): number {
  if (outcome === "VICTORY") return config.outcomePolicy.victoryFullClearBps;
  if (outcome === "ABANDONED") return config.outcomePolicy.abandonedBps;
  return basisPoints;
}

function getWaveCompletionMultiplierBps(
  config: RunRewardsConfig,
  outcome: RunOutcome,
  clearedWaves: number,
): number {
  if (outcome === "VICTORY") return clearedWaves === maxRunWaves ? basisPoints : 0;
  if (outcome === "ABANDONED") return config.outcomePolicy.abandonedBps;
  return Math.min(
    config.outcomePolicy.failureCapBps,
    config.outcomePolicy.failureBaseBps +
      Math.floor((config.outcomePolicy.failureWaveBps * clearedWaves) / maxRunWaves),
  );
}

function assertClearedWaves(clearedWaves: number): void {
  if (!Number.isInteger(clearedWaves) || clearedWaves < 0 || clearedWaves > maxRunWaves) {
    throw new Error("contract_violation");
  }
}

function isPositiveSafeInteger(value: number | undefined): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value > 0;
}

function parseRiskMultipliers(value: unknown): Record<RunRiskTier, number> {
  const record = requireRecord(value, "reward_multiplier_missing");
  return Object.fromEntries(
    runRiskTiers.map((risk) => [
      risk,
      requirePositiveInteger(record[risk], "reward_multiplier_missing"),
    ]),
  ) as Record<RunRiskTier, number>;
}

function parseOperationProgressionMultipliers(value: unknown): Record<string, number> {
  const record = requireRecord(value, "risk_reward_policy_missing");
  const entries = Object.entries(record);
  if (entries.length === 0) {
    throw new Error("risk_reward_policy_missing");
  }

  return Object.fromEntries(
    entries.map(([operationId, multiplier]) => [
      requireNonEmptyString(operationId, "risk_reward_policy_missing"),
      requirePositiveInteger(multiplier, "risk_reward_policy_missing"),
    ]),
  );
}

function parseOutcomePolicy(value: unknown): RunRewardsConfig["outcomePolicy"] {
  const record = requireRecord(value, "risk_reward_policy_missing");
  return {
    victoryFullClearBps: requirePositiveInteger(
      record.victoryFullClearBps,
      "risk_reward_policy_missing",
    ),
    failureBaseBps: requireNonNegativeInteger(
      record.failureBaseBps,
      "risk_reward_policy_missing",
    ),
    failureWaveBps: requirePositiveInteger(
      record.failureWaveBps,
      "risk_reward_policy_missing",
    ),
    failureCapBps: requirePositiveInteger(
      record.failureCapBps,
      "risk_reward_policy_missing",
    ),
    abandonedBps: requireNonNegativeInteger(
      record.abandonedBps,
      "risk_reward_policy_missing",
    ),
  };
}

function parseRunRewardOperations(value: unknown): RunRewardsConfig["operations"] {
  const record = requireRecord(value, "risk_reward_policy_missing");
  const entries = Object.entries(record);
  if (entries.length === 0) {
    throw new Error("risk_reward_policy_missing");
  }

  return Object.fromEntries(
    entries.map(([operationId, operationValue]) => {
      const operation = requireRecord(operationValue, "risk_reward_policy_missing");
      return [
        requireNonEmptyString(operationId, "risk_reward_policy_missing"),
        {
          baseRewards: parseRewardLines(operation.baseRewards),
          firstClearRewards: parseRewardLines(operation.firstClearRewards),
        },
      ];
    }),
  );
}

function parseRewardLines(value: unknown): RunRewardLineConfig[] {
  if (!Array.isArray(value)) {
    throw new Error("reward_plan_mismatch");
  }

  return value.map((lineValue) => {
    const line = requireRecord(lineValue, "reward_plan_mismatch");
    return {
      kind: requireOneOf(line.kind, rewardLineKinds, "reward_plan_mismatch"),
      id: requireNonEmptyString(line.id, "reward_plan_mismatch"),
      quantity: requirePositiveInteger(line.quantity, "reward_plan_mismatch"),
    };
  });
}

function parseIntegrityPolicy(value: unknown): RunRewardsConfig["integrityPolicy"] {
  const record = requireRecord(value, "risk_reward_policy_missing");
  return {
    softAnomaly: requireOneOf(
      record.softAnomaly,
      integrityDispositions,
      "risk_reward_policy_missing",
    ),
    hardViolation: requireOneOf(
      record.hardViolation,
      integrityDispositions,
      "risk_reward_policy_missing",
    ),
    quarantinedRun: requireOneOf(
      record.quarantinedRun,
      integrityDispositions,
      "risk_reward_policy_missing",
    ),
  };
}

function parseOfflineIntegrityPolicy(value: unknown): OfflineRewardsConfig["integrityPolicy"] {
  const record = requireRecord(value, "offline_reward_policy_invalid");
  if (
    record.clockTamper !== "REJECT_CLAIM" ||
    record.staleClientState !== "RECALCULATE_SERVER_SIDE" ||
    record.maintenanceMode !== "DEFER_CLAIM"
  ) {
    throw new Error("offline_reward_policy_invalid");
  }
  return {
    clockTamper: "REJECT_CLAIM",
    staleClientState: "RECALCULATE_SERVER_SIDE",
    maintenanceMode: "DEFER_CLAIM",
  };
}

function parseOfflineStoryProgressNumbers(value: unknown): Record<OfflineStoryProgressKey, number> {
  const record = requireRecord(value, "offline_reward_policy_invalid");
  return Object.fromEntries(
    offlineStoryProgressKeys.map((key) => [
      key,
      requirePositiveInteger(record[key], "offline_reward_policy_invalid"),
    ]),
  ) as Record<OfflineStoryProgressKey, number>;
}

function parseStringArray(value: unknown, errorCode: "offline_reward_policy_invalid"): string[] {
  if (!Array.isArray(value) || value.some((item) => typeof item !== "string" || item.length === 0)) {
    throw new Error(errorCode);
  }
  return value;
}

function requireOfflineStoryProgressKey(value: string): OfflineStoryProgressKey {
  if (!offlineStoryProgressKeys.includes(value as OfflineStoryProgressKey)) {
    throw new Error("offline_story_progress_missing");
  }
  return value as OfflineStoryProgressKey;
}

function requireRecord(
  value: unknown,
  errorCode:
    | "risk_reward_policy_missing"
    | "reward_multiplier_missing"
    | "reward_plan_mismatch"
    | "offline_reward_policy_invalid",
): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new Error(errorCode);
  }
  return value as Record<string, unknown>;
}

function requireNonEmptyString(
  value: unknown,
  errorCode: "risk_reward_policy_missing" | "reward_plan_mismatch",
): string {
  if (typeof value !== "string" || value.trim().length === 0) {
    throw new Error(errorCode);
  }
  return value;
}

function requirePositiveInteger(
  value: unknown,
  errorCode:
    | "risk_reward_policy_missing"
    | "reward_multiplier_missing"
    | "reward_plan_mismatch"
    | "offline_reward_policy_invalid",
): number {
  if (typeof value !== "number" || !Number.isSafeInteger(value) || value <= 0) {
    throw new Error(errorCode);
  }
  return value;
}

function requireNonNegativeInteger(
  value: unknown,
  errorCode: "risk_reward_policy_missing",
): number {
  if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 0) {
    throw new Error(errorCode);
  }
  return value;
}

function requireOneOf<T extends string>(
  value: unknown,
  allowed: readonly T[],
  errorCode: "risk_reward_policy_missing" | "reward_plan_mismatch",
): T {
  if (typeof value !== "string" || !allowed.includes(value as T)) {
    throw new Error(errorCode);
  }
  return value as T;
}
export { launchEconomySchema, parseLaunchEconomy, trainingCoreExpCost, ordinaryVictoryExp } from "./launch-economy";
export type { LaunchEconomy } from "./launch-economy";
