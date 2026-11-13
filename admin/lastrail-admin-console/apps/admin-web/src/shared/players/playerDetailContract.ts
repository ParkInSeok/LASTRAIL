import type { PlayerLaunchEvidence } from "./playerLaunchEvidenceContract";

export type PlayerTrainingCoreSlot = {
  slot: number;
  operatorId: string;
  name: string;
  trainedLevel: number;
};

export type PlayerOperatorProgression = {
  operatorId: string;
  name: string;
  effectiveLevel: number;
  levelSource: "TRAINED" | "SYNC";
  trainedLevel: number;
  skillTraining: number;
  promotion: string;
  coreSlot?: number;
};

export type PlayerDetailReadModel = {
  accountId: string;
  displayName: string;
  status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  createdAt: string;
  progression: {
    operatorExp: number;
    syncLevel: number;
    coreLevelSpread: number;
    trainingCore: PlayerTrainingCoreSlot[];
    operators: PlayerOperatorProgression[];
  };
  launchEvidence: PlayerLaunchEvidence;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function nonEmptyString(value: unknown, fallback: string): string {
  return typeof value === "string" && value.length > 0 ? value : fallback;
}

function nonNegativeInt(value: unknown, fallback: number): number {
  return typeof value === "number" && Number.isInteger(value) && value >= 0 ? value : fallback;
}

function positiveInt(value: unknown, fallback: number): number {
  return typeof value === "number" && Number.isInteger(value) && value >= 1 ? value : fallback;
}

function accountStatus(value: unknown): PlayerDetailReadModel["status"] {
  if (value === "ACTIVE" || value === "SUSPENDED" || value === "BANNED" || value === "DELETED") return value;
  return "ACTIVE";
}

function levelSource(value: unknown): PlayerOperatorProgression["levelSource"] {
  return value === "TRAINED" || value === "SYNC" ? value : "SYNC";
}

function optionalCoreSlot(value: unknown): number | undefined {
  return typeof value === "number" && Number.isInteger(value) && value >= 1 ? value : undefined;
}

function trainingCoreSlot(value: unknown): PlayerTrainingCoreSlot | null {
  const record = asRecord(value);
  if (!record) return null;
  return {
    slot: positiveInt(record.slot, 1),
    operatorId: nonEmptyString(record.operatorId, "unknown_operator"),
    name: nonEmptyString(record.name, nonEmptyString(record.operatorId, "unknown_operator")),
    trainedLevel: positiveInt(record.trainedLevel, 1)
  };
}

function operatorProgression(value: unknown): PlayerOperatorProgression | null {
  const record = asRecord(value);
  if (!record) return null;
  return {
    operatorId: nonEmptyString(record.operatorId, "unknown_operator"),
    name: nonEmptyString(record.name, nonEmptyString(record.operatorId, "unknown_operator")),
    effectiveLevel: positiveInt(record.effectiveLevel, 1),
    levelSource: levelSource(record.levelSource),
    trainedLevel: positiveInt(record.trainedLevel, 1),
    skillTraining: nonNegativeInt(record.skillTraining, 0),
    promotion: nonEmptyString(record.promotion, "없음"),
    coreSlot: optionalCoreSlot(record.coreSlot)
  };
}

function arrayMap<T>(value: unknown, mapper: (item: unknown) => T | null): T[] {
  if (!Array.isArray(value)) return [];
  return value.map(mapper).filter((item): item is T => item !== null);
}

export function mapPlayerDetailResponse(
  response: unknown,
  requestedAccountId: string,
  launchEvidence: PlayerLaunchEvidence
): PlayerDetailReadModel {
  const record = asRecord(response) ?? {};
  const progression = asRecord(record.progression) ?? {};

  return {
    accountId: nonEmptyString(record.accountId, requestedAccountId),
    displayName: nonEmptyString(record.displayName, nonEmptyString(record.accountId, requestedAccountId)),
    status: accountStatus(record.status),
    createdAt: nonEmptyString(record.createdAt, ""),
    progression: {
      operatorExp: nonNegativeInt(progression.operatorExp, 0),
      syncLevel: positiveInt(progression.syncLevel, 1),
      coreLevelSpread: nonNegativeInt(progression.coreLevelSpread, 0),
      trainingCore: arrayMap(progression.trainingCore, trainingCoreSlot),
      operators: arrayMap(progression.operators, operatorProgression)
    },
    launchEvidence
  };
}
