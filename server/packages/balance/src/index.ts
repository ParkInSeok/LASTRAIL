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
const supportedRunRewardsSchemaVersion = 1;
const runRiskTiers: RunRiskTier[] = ["I", "II", "III", "IV", "V"];
const rewardLineKinds: RewardLineKind[] = ["CURRENCY", "ITEM"];
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

function requireRecord(
  value: unknown,
  errorCode: "risk_reward_policy_missing" | "reward_multiplier_missing" | "reward_plan_mismatch",
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
  errorCode: "risk_reward_policy_missing" | "reward_multiplier_missing" | "reward_plan_mismatch",
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

