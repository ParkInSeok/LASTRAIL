export type RunLaunchEvidence = {
  operationId: string | null;
  outcome: string | null;
  clearedWaves: number | null;
  rewardPreviewId: string | null;
  rewardClaimId: string | null;
  appliedMultiplierLabel: string | null;
  calculationMetadataStatus: string | null;
  firstClearConsumed: boolean | null;
  integrityDisposition: {
    status: string | null;
    openSignals: number | null;
    hardViolations: number | null;
    primaryReason: string | null;
  };
  encounterType: string | null;
  raidId: string | null;
  bossId: string | null;
  bossPhaseRefs: string[];
  bossDefeated: boolean | null;
  completedPhaseCount: number | null;
  encounterDurationSeconds: number | null;
  allowedOperatorSlots: number | string | null;
  startAugmentPackageRef: string | null;
  startTacticPackageRef: string | null;
  selectedAugmentIds: string[];
  selectedTacticIds: string[];
  staminaRefund: {
    status: string | null;
    resource: string | null;
    reason: string | null;
    policyId: string | null;
    serverOwnedMutation: boolean | string | null;
    fuelLedgerReason: string | null;
    fuelLedgerRefs: string[];
  } | null;
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
    repairEndpointsEnabled: false;
    source: string | null;
  };
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function stringOrNull(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function booleanOrNull(value: unknown): boolean | null {
  return typeof value === "boolean" ? value : null;
}

function numberOrNull(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function stringList(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === "string" && item.length > 0);
}

export function mapRunLaunchEvidence(response: unknown): RunLaunchEvidence {
  const record = asRecord(response) ?? {};
  const integrity = asRecord(record.integrityDisposition);
  const staminaRefund = asRecord(record.staminaRefund);
  const supportContext = asRecord(record.supportContext);

  return {
    operationId: stringOrNull(record.operationId),
    outcome: stringOrNull(record.outcome),
    clearedWaves: numberOrNull(record.clearedWaves),
    rewardPreviewId: stringOrNull(record.rewardPreviewId),
    rewardClaimId: stringOrNull(record.rewardClaimId),
    appliedMultiplierLabel: stringOrNull(record.appliedMultiplierLabel),
    calculationMetadataStatus: stringOrNull(record.calculationMetadataStatus),
    firstClearConsumed: booleanOrNull(record.firstClearConsumed),
    integrityDisposition: {
      status: stringOrNull(integrity?.status),
      openSignals: numberOrNull(integrity?.openSignals),
      hardViolations: numberOrNull(integrity?.hardViolations),
      primaryReason: stringOrNull(integrity?.primaryReason)
    },
    encounterType: stringOrNull(record.encounterType),
    raidId: stringOrNull(record.raidId),
    bossId: stringOrNull(record.bossId),
    bossPhaseRefs: stringList(record.bossPhaseRefs),
    bossDefeated: booleanOrNull(record.bossDefeated),
    completedPhaseCount: numberOrNull(record.completedPhaseCount),
    encounterDurationSeconds: numberOrNull(record.encounterDurationSeconds),
    allowedOperatorSlots: numberOrNull(record.allowedOperatorSlots) ?? stringOrNull(record.allowedOperatorSlots),
    startAugmentPackageRef: stringOrNull(record.startAugmentPackageRef),
    startTacticPackageRef: stringOrNull(record.startTacticPackageRef),
    selectedAugmentIds: stringList(record.selectedAugmentIds),
    selectedTacticIds: stringList(record.selectedTacticIds),
    staminaRefund: staminaRefund
      ? {
          status: stringOrNull(staminaRefund.status),
          resource: stringOrNull(staminaRefund.resource),
          reason: stringOrNull(staminaRefund.reason),
          policyId: stringOrNull(staminaRefund.policyId),
          serverOwnedMutation:
            booleanOrNull(staminaRefund.serverOwnedMutation) ?? stringOrNull(staminaRefund.serverOwnedMutation),
          fuelLedgerReason: stringOrNull(staminaRefund.fuelLedgerReason),
          fuelLedgerRefs: stringList(staminaRefund.fuelLedgerRefs)
        }
      : null,
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: stringOrNull(supportContext?.source)
    }
  };
}
