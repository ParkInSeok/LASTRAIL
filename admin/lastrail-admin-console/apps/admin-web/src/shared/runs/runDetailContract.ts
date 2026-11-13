import type { RunLaunchEvidence } from "./runLaunchEvidenceContract";
import { mapRunVersions, type RunVersionEvidence } from "./runVersions";

export type RunOperatorSnapshot = {
  operatorId: string;
  effectiveLevel: number;
  levelSource: "TRAINED" | "SYNC";
  syncLevelAtStart: number;
  trainedLevelAtStart?: number;
  skillTraining: number;
  promotion: string;
};

export type RunDetailReadModel = {
  runId: string;
  accountId: string;
  startedAt: string;
  completedAt?: string;
  result: "IN_PROGRESS" | "CLEARED" | "FAILED" | "INVALIDATED";
  riskTier: "I" | "II" | "III" | "IV" | "V";
  balanceVersion: string;
  validatorVersion?: string;
  currentActiveBalanceVersion: string;
  clientBuild: string;
  contentVersion: string;
  validatorRuntimeRevision: string;
  snapshot: {
    operatorIds: string[];
    ultimateOperatorId?: string;
    operators: RunOperatorSnapshot[];
  };
  launchEvidence: RunLaunchEvidence;
  versionEvidence: RunVersionEvidence;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function nonEmptyString(value: unknown, fallback: string): string {
  return typeof value === "string" && value.length > 0 ? value : fallback;
}

function optionalString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function nonNegativeInt(value: unknown, fallback: number): number {
  return typeof value === "number" && Number.isInteger(value) && value >= 0 ? value : fallback;
}

function positiveInt(value: unknown, fallback: number): number {
  return typeof value === "number" && Number.isInteger(value) && value >= 1 ? value : fallback;
}

function runResult(value: unknown): RunDetailReadModel["result"] {
  if (value === "IN_PROGRESS" || value === "CLEARED" || value === "FAILED" || value === "INVALIDATED") return value;
  if (value === "VICTORY" || value === "SUCCESS") return "CLEARED";
  if (value === "FAILURE" || value === "ABANDONED") return "FAILED";
  return "IN_PROGRESS";
}

function riskTier(value: unknown): RunDetailReadModel["riskTier"] {
  if (value === "I" || value === "II" || value === "III" || value === "IV" || value === "V") return value;
  return "I";
}

function levelSource(value: unknown): RunOperatorSnapshot["levelSource"] {
  return value === "TRAINED" || value === "SYNC" ? value : "SYNC";
}

function stringList(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === "string" && item.length > 0);
}

function operatorSnapshot(value: unknown): RunOperatorSnapshot | null {
  const record = asRecord(value);
  if (!record) return null;
  return {
    operatorId: nonEmptyString(record.operatorId, "unknown_operator"),
    effectiveLevel: positiveInt(record.effectiveLevel, 1),
    levelSource: levelSource(record.levelSource),
    syncLevelAtStart: positiveInt(record.syncLevelAtStart, 1),
    trainedLevelAtStart:
      typeof record.trainedLevelAtStart === "number" && Number.isInteger(record.trainedLevelAtStart) && record.trainedLevelAtStart >= 1
        ? record.trainedLevelAtStart
        : undefined,
    skillTraining: nonNegativeInt(record.skillTraining, 0),
    promotion: nonEmptyString(record.promotion, "없음")
  };
}

function operatorSnapshots(value: unknown): RunOperatorSnapshot[] {
  if (!Array.isArray(value)) return [];
  return value.map(operatorSnapshot).filter((item): item is RunOperatorSnapshot => item !== null);
}

function snapshotOperatorIds(snapshot: Record<string, unknown>, operators: RunOperatorSnapshot[]): string[] {
  const explicitIds = stringList(snapshot.operatorIds);
  if (explicitIds.length > 0) return explicitIds;
  return operators.map((operator) => operator.operatorId);
}

export function mapRunDetailResponse(
  response: unknown,
  requestedRunId: string,
  launchEvidence: RunLaunchEvidence
): RunDetailReadModel {
  const record = asRecord(response) ?? {};
  const snapshot = asRecord(record.snapshot) ?? {};
  const operators = operatorSnapshots(snapshot.operators);

  return {
    runId: nonEmptyString(record.runId, requestedRunId),
    accountId: nonEmptyString(record.accountId, "없음"),
    startedAt: nonEmptyString(record.startedAt, ""),
    completedAt: optionalString(record.completedAt),
    result: runResult(record.result),
    riskTier: riskTier(record.riskTier),
    balanceVersion: nonEmptyString(record.balanceVersion, "없음"),
    validatorVersion: optionalString(record.validatorVersion),
    currentActiveBalanceVersion: nonEmptyString(record.currentActiveBalanceVersion, "없음"),
    clientBuild: nonEmptyString(record.clientBuild, "없음"),
    contentVersion: nonEmptyString(record.contentVersion, "없음"),
    validatorRuntimeRevision: nonEmptyString(record.validatorRuntimeRevision, "없음"),
    snapshot: {
      operatorIds: snapshotOperatorIds(snapshot, operators),
      ultimateOperatorId: optionalString(snapshot.ultimateOperatorId),
      operators
    },
    launchEvidence,
    versionEvidence: mapRunVersions(record)
  };
}
