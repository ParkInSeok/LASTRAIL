export const packageName = "@lastrail/balance";

export type RunRiskTier = "I" | "II" | "III" | "IV" | "V";
export type RunOutcome = "VICTORY" | "FAILURE" | "ABANDONED";
export type IntegrityDisposition =
  | "ALLOW"
  | "ALLOW_PREVIEW_WITH_FLAG"
  | "INVALIDATE_PREVIEW";
export type RewardLineKind = "CURRENCY" | "ITEM";

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

const basisPoints = 10000;
const maxRunWaves = 15;

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
      ],
    },
    opn_06_broken_line: {
      baseRewards: [
        { kind: "CURRENCY", id: "cur_credits", quantity: 480 },
        { kind: "CURRENCY", id: "cur_operator_exp", quantity: 1100 },
      ],
      firstClearRewards: [
        { kind: "CURRENCY", id: "cur_special_core", quantity: 1 },
      ],
    },
  },
  integrityPolicy: {
    softAnomaly: "ALLOW_PREVIEW_WITH_FLAG",
    hardViolation: "INVALIDATE_PREVIEW",
    quarantinedRun: "INVALIDATE_PREVIEW",
  },
};

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

