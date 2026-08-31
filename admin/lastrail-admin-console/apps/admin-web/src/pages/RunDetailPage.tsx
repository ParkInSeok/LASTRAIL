import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { adminReadValueLabel, adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import {
  encounterTypeLabel,
  firstClearLabel,
  fuelLedgerReasonLabel,
  levelSourceLabel,
  permissionLabel,
  runResultLabel,
  serverOwnedMutationLabel,
  storyProgressLabel,
  storyProgressStatusLabel
} from "../shared/labels/adminLabels";
import type { RunLaunchEvidence } from "../shared/runs/runLaunchEvidenceContract";
import {
  storyProgressReadContractFilterFields,
  type StoryProgressDetail
} from "../shared/storyProgress/storyProgressContract";
import {
  narrativeCoverageRowsForReadState,
  type NarrativeCoverageRow
} from "../shared/narrative/narrativeCoverageContract";

type RunRiskRewardPlaceholder = {
  label: string;
  value: string;
  note: string;
};

type ActOneNarrativeCoverage = NarrativeCoverageRow;

type RaidNarrativeObjectivePlaceholder = {
  objectiveId: string;
  label: string;
  state: "연동 대기";
  note: string;
};

type StoryProgressReadContractPlaceholder = {
  label: string;
  field: string;
  value: string;
  note: string;
};

type RunEncounterPlaceholder = {
  code: "ORDINARY_15_WAVE" | "RAID_BOSS";
  label: string;
  value: "연동 대기";
  note: string;
};

type RunCombatEvidencePlaceholder = {
  field: string;
  label: string;
  value: string;
  note: string;
};

export const runRiskRewardPlaceholders: RunRiskRewardPlaceholder[] = [
  { label: "위험도", value: "연동 대기", note: "위험도 I~V 표시 후보" },
  { label: "작전 ID", value: "연동 대기", note: "operation progression 배율 후보" },
  { label: "결과", value: "연동 대기", note: "CLEARED/PARTIAL/ABANDONED 등 outcome" },
  { label: "클리어 공세", value: "연동 대기", note: "waveCompletionMultiplier 후보" },
  { label: "rewardPreviewId / rewardClaimId", value: "연동 대기", note: "보상 미리보기/수령 연결" },
  { label: "적용 배율", value: "연동 대기", note: "sanitized calculation metadata 후보" },
  { label: "first clear", value: "연동 대기", note: "첫 클리어 보너스 표시 후보" },
  { label: "무결성 판정", value: "연동 대기", note: "reward_integrity_restricted 연계 후보" },
  { label: "계산 메타데이터 상태", value: "연동 대기", note: "서버 보상 미리보기 / Unity 진단 참고 신호 분리" }
];

export const runStoryNarrativePlaceholders: RunRiskRewardPlaceholder[] = [
  { label: "작전 ID", value: "연동 대기", note: "stable operation id 표시 후보" },
  { label: "스토리 진행도", value: "연동 대기", note: "최고 클리어 진행도 표시 후보" },
  { label: "첫 클리어 상태", value: "연동 대기", note: "first clear visibility 후보" },
  { label: "미션 내러티브 상태", value: "연동 대기", note: "브리핑/콜아웃 데이터 상태 후보" },
  { label: "현지화 데이터 상태", value: "연동 대기", note: "localization missing-state 후보" },
  { label: "누락 데이터 상태", value: "연동 대기", note: "narrative_content_missing 연계 후보" },
  { label: "플레이어 문의 지원 문맥", value: "연동 대기", note: "read-only support context 후보" }
];

export const actOneNarrativeCoverage: ActOneNarrativeCoverage[] = [
  "opn_01_last_departure",
  "opn_02_first_signal",
  "opn_03_water_stop",
  "opn_04_scrapstar_show",
  "opn_05_night_boarders",
  "opn_06_broken_line",
  "raid_raider_king_engine"
].map((missionId) => ({
  missionId,
  displayName: storyProgressLabel(missionId),
  coverageState: "연동 대기",
  localizationState: "연동 대기",
  fixtureState: "연동 대기",
  narrativeVersionState: "연동 대기",
  statusHint: "초안 존재 / 현지화 대기 / fixture 대기 / 버전 대기 / 콘텐츠 누락 후보"
}));

export const raidNarrativeObjectivePlaceholders: RaidNarrativeObjectivePlaceholder[] = [
  { objectiveId: "prisoner_car_preserved", label: "포로칸 보존", state: "연동 대기", note: "read-only raid scoring 후보" },
  {
    objectiveId: "broadcast_relay_captured",
    label: "방송 중계차 확보",
    state: "연동 대기",
    note: "read-only raid scoring 후보"
  },
  { objectiveId: "supply_car_stabilized", label: "보급차 안정화", state: "연동 대기", note: "read-only raid scoring 후보" },
  { objectiveId: "fuel_core_stable", label: "연료 코어 안정", state: "연동 대기", note: "read-only raid scoring 후보" },
  { objectiveId: "engine_integrity", label: "엔진 무결성", state: "연동 대기", note: "read-only raid scoring 후보" }
];

export const encounterTypePlaceholders: RunEncounterPlaceholder[] = [
  {
    code: "ORDINARY_15_WAVE",
    label: encounterTypeLabel("ORDINARY_15_WAVE"),
    value: "연동 대기",
    note: "일반 Act 1 작전은 15공세 기준으로 판정합니다"
  },
  {
    code: "RAID_BOSS",
    label: encounterTypeLabel("RAID_BOSS"),
    value: "연동 대기",
    note: "레이드는 15공세가 아닌 boss-only encounter로 분리합니다"
  }
];

export const raidBossResultPlaceholders: RunCombatEvidencePlaceholder[] = [
  { field: "raidId", label: "레이드 ID", value: "연동 대기", note: "raid boss result reference 후보" },
  { field: "bossId", label: "보스 ID", value: "연동 대기", note: "boss-only encounter reference 후보" },
  { field: "bossDefeated", label: "보스 격파 여부", value: "연동 대기", note: "15공세 클리어와 분리 표시" },
  { field: "completedPhaseCount", label: "완료 페이즈 수", value: "연동 대기", note: "boss phase progression 후보" },
  { field: "encounterDurationSeconds", label: "전투 소요 시간", value: "연동 대기", note: "sanitized duration 후보" }
];

export const storyRaidLaunchResultPolicyPlaceholders: RunCombatEvidencePlaceholder[] = [
  {
    field: "allowedOperatorSlots",
    label: "허용 요원 슬롯",
    value: "5",
    note: "Act 1 story raid는 일반 작전과 동일하게 5 operators 기준"
  },
  {
    field: "failureStaminaRefund",
    label: "실패 stamina 환급",
    value: "연동 대기",
    note: "story raid 실패 시 stamina refund 후보, read-only evidence"
  },
  {
    field: "failureStaminaRefundStatus",
    label: "실패 환급 상태",
    value: "COMMITTED",
    note: "story raid failure stamina refund committed read-only marker"
  },
  {
    field: "failureStaminaRefundResource",
    label: "실패 환급 리소스",
    value: "STAMINA",
    note: "story raid failure stamina refund read-only marker"
  },
  {
    field: "failureStaminaRefundReason",
    label: "실패 환급 사유",
    value: "STORY_RAID_FAILURE",
    note: "story raid failure stamina refund read-only marker"
  },
  {
    field: "failureStaminaRefundPolicyId",
    label: "실패 환급 정책 ID",
    value: "act1_story_raid_failure_stamina_refund_v1",
    note: "story raid failure stamina refund read-only marker"
  },
  {
    field: "serverOwnedMutation",
    label: "서버 소유 mutation",
    value: "STORY_RAID_STAMINA_REFUND",
    note: "server-owned mutation label evidence only, Admin action 아님"
  },
  {
    field: "fuelLedgerReason",
    label: "fuel ledger 사유",
    value: "STORY_RAID_FAILURE_REFUND",
    note: "fuel ledger reason read-only evidence only, reward/economy correction 아님"
  },
  {
    field: "successCondition",
    label: "성공 기준",
    value: "boss_defeat",
    note: "boss defeat success 기준"
  },
  {
    field: "worldRaidScope",
    label: "World Raid 범위",
    value: "backlog",
    note: "World Raid와 분리, ranking/settlement/reward repair UI는 launch 제외"
  }
];

export const runStartPackageEvidencePlaceholders: RunCombatEvidencePlaceholder[] = [
  {
    field: "startAugmentPackageRef",
    label: "시작 증강 패키지 ref",
    value: "연동 대기",
    note:
      "server-authored run start package / ORDINARY_15_WAVE / RAID_BOSS 공통 후보 / reward/economy/story/offline/gacha Source of Truth 아님"
  },
  {
    field: "startTacticPackageRef",
    label: "시작 전술 패키지 ref",
    value: "연동 대기",
    note:
      "server-authored run start package / ORDINARY_15_WAVE / RAID_BOSS 공통 후보 / reward/economy/story/offline/gacha Source of Truth 아님"
  },
  {
    field: "selectedAugmentIds",
    label: "선택 증강 ID",
    value: "연동 대기",
    note: "run start 시점에 확정된 selected augment stable ids 후보"
  },
  {
    field: "selectedTacticIds",
    label: "선택 전술 ID",
    value: "연동 대기",
    note: "run start 시점에 확정된 selected tactic stable ids 후보"
  },
  {
    field: "source",
    label: "근거 소스",
    value: "run_start",
    note: "source=run_start read-only evidence"
  }
];

export const forbiddenRunCombatEvidenceExamplesOutOfScope = [
  "raw combat timeline",
  "animation frame",
  "VFX",
  "SFX",
  "camera",
  "hit-stop",
  "debug payload"
] as const;

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function stringValue(value: unknown): string {
  return adminReadValueLabel({ kind: "success", value });
}

function nullableStringValue(value: unknown): string {
  if (typeof value === "number") return String(value);
  return stringValue(value);
}

function stringListValue(value: unknown): string {
  if (!Array.isArray(value)) return "없음";
  const values = value.filter((item): item is string => typeof item === "string" && item.length > 0);
  return values.length > 0 ? values.join(" / ") : "없음";
}

function startMetadataRecord(source: unknown): Record<string, unknown> | null {
  const record = asRecord(source);
  return asRecord(record?.launchEvidence) ?? asRecord(record?.serverPreviewMetadata) ?? asRecord(record?.metadata) ?? record;
}

function booleanReadValue(value: boolean | null | undefined, trueLabel = "예", falseLabel = "아니오"): string {
  if (value === true) return trueLabel;
  if (value === false) return falseLabel;
  return "없음";
}

function maybeLabelWithCode(labeler: (value: string) => string, value: string | null | undefined, emptyLabel = "없음"): string {
  if (!value) return emptyLabel;
  return labelWithCode(labeler(value), value);
}

function rewardReferenceValue(evidence: RunLaunchEvidence): string {
  return `${stringValue(evidence.rewardPreviewId)} / ${stringValue(evidence.rewardClaimId)}`;
}

function integrityDispositionValue(evidence: RunLaunchEvidence): string {
  const disposition = evidence.integrityDisposition;
  const values = [
    disposition.status,
    typeof disposition.openSignals === "number" ? `open=${disposition.openSignals}` : null,
    typeof disposition.hardViolations === "number" ? `hard=${disposition.hardViolations}` : null,
    disposition.primaryReason
  ].filter((value): value is string => typeof value === "string" && value.length > 0);

  return values.length > 0 ? values.join(" / ") : "없음";
}

export function runRiskRewardCardsForLaunchEvidence(
  evidence: RunLaunchEvidence | null | undefined
): RunRiskRewardPlaceholder[] {
  if (!evidence) return runRiskRewardPlaceholders;

  return [
    { label: "위험도", value: "없음", note: "위험도 I~V 표시 후보" },
    {
      label: "작전 ID",
      value: maybeLabelWithCode(storyProgressLabel, evidence.operationId),
      note: "operation progression 배율 후보"
    },
    { label: "결과", value: maybeLabelWithCode((value) => runResultLabel(value as never), evidence.outcome), note: "CLEARED/PARTIAL/ABANDONED 등 outcome" },
    { label: "클리어 공세", value: stringValue(evidence.clearedWaves), note: "waveCompletionMultiplier 후보" },
    { label: "rewardPreviewId / rewardClaimId", value: rewardReferenceValue(evidence), note: "보상 미리보기/수령 연결" },
    {
      label: "적용 배율",
      value: stringValue(evidence.appliedMultiplierLabel),
      note: "sanitized calculation metadata 후보"
    },
    {
      label: "first clear",
      value: evidence.firstClearConsumed === null ? "없음" : firstClearLabel(evidence.firstClearConsumed),
      note: "첫 클리어 보너스 표시 후보"
    },
    { label: "무결성 판정", value: integrityDispositionValue(evidence), note: "reward_integrity_restricted 연계 후보" },
    {
      label: "계산 메타데이터 상태",
      value: maybeLabelWithCode(storyProgressStatusLabel, evidence.calculationMetadataStatus),
      note: "서버 보상 미리보기 / Unity 진단 참고 신호 분리"
    }
  ];
}

function serverOwnedMutationValue(value: string | boolean | null | undefined): string {
  if (value === true) return labelWithCode(serverOwnedMutationLabel("STORY_RAID_STAMINA_REFUND"), "STORY_RAID_STAMINA_REFUND");
  if (value === false || value === null || value === undefined) return "없음";
  return labelWithCode(serverOwnedMutationLabel(value), value);
}

export function storyRaidPolicyCardsForLaunchEvidence(
  evidence: RunLaunchEvidence | null | undefined
): RunCombatEvidencePlaceholder[] {
  if (!evidence) return storyRaidLaunchResultPolicyPlaceholders;
  const refund = evidence.staminaRefund;

  return storyRaidLaunchResultPolicyPlaceholders.map((placeholder) => {
    const valueByField: Record<string, string> = {
      allowedOperatorSlots: stringValue(evidence.allowedOperatorSlots),
      failureStaminaRefund: refund ? "있음" : "없음",
      failureStaminaRefundStatus: stringValue(refund?.status),
      failureStaminaRefundResource: stringValue(refund?.resource),
      failureStaminaRefundReason: stringValue(refund?.reason),
      failureStaminaRefundPolicyId: stringValue(refund?.policyId),
      serverOwnedMutation: serverOwnedMutationValue(refund?.serverOwnedMutation),
      fuelLedgerReason: refund?.fuelLedgerReason
        ? labelWithCode(fuelLedgerReasonLabel(refund.fuelLedgerReason), refund.fuelLedgerReason)
        : "없음",
      successCondition: "boss_defeat",
      worldRaidScope: "backlog"
    };

    return {
      ...placeholder,
      value: valueByField[placeholder.field] ?? placeholder.value
    };
  });
}

export function runStartMetadataEvidenceFrom(source: unknown): RunCombatEvidencePlaceholder[] {
  const metadata = startMetadataRecord(source);
  const encounterType = stringValue(metadata?.encounterType);
  const encounterValue =
    encounterType === "없음" ? encounterType : labelWithCode(encounterTypeLabel(encounterType), encounterType);

  return [
    {
      field: "encounterType",
      label: "Encounter 유형",
      value: encounterValue,
      note: `${encounterTypeLabel("ORDINARY_15_WAVE")} / ${encounterTypeLabel("RAID_BOSS")} / ORDINARY_15_WAVE / RAID_BOSS`
    },
    { field: "raidId", label: "레이드 ID", value: stringValue(metadata?.raidId), note: "boss-only raid reference" },
    { field: "bossId", label: "보스 ID", value: stringValue(metadata?.bossId), note: "boss-only encounter reference" },
    {
      field: "bossPhaseRefs",
      label: "보스 페이즈 ref",
      value: stringListValue(metadata?.bossPhaseRefs),
      note: "raid boss phase references"
    },
    {
      field: "startAugmentPackageRef",
      label: "시작 증강 패키지 ref",
      value: stringValue(metadata?.startAugmentPackageRef),
      note:
        "server-authored run start package / ORDINARY_15_WAVE / RAID_BOSS 공통 후보 / reward/economy/story/offline/gacha Source of Truth 아님"
    },
    {
      field: "startTacticPackageRef",
      label: "시작 전술 패키지 ref",
      value: stringValue(metadata?.startTacticPackageRef),
      note:
        "server-authored run start package / ORDINARY_15_WAVE / RAID_BOSS 공통 후보 / reward/economy/story/offline/gacha Source of Truth 아님"
    },
    { field: "contentVersion", label: "콘텐츠 버전", value: stringValue(metadata?.contentVersion), note: "server preview metadata" },
    { field: "balanceVersion", label: "밸런스 버전", value: stringValue(metadata?.balanceVersion), note: "server preview metadata" },
    {
      field: "allowedOperatorSlots",
      label: "허용 요원 슬롯",
      value: nullableStringValue(metadata?.allowedOperatorSlots),
      note: "nullable/candidate allowed operator slots"
    },
    {
      field: "selectedAugmentIds",
      label: "선택 증강 ID",
      value: stringListValue(metadata?.selectedAugmentIds),
      note: "run start 시점에 확정된 selected augment stable ids"
    },
    {
      field: "selectedTacticIds",
      label: "선택 전술 ID",
      value: stringListValue(metadata?.selectedTacticIds),
      note: "run start 시점에 확정된 selected tactic stable ids"
    },
    { field: "source", label: "근거 소스", value: "run_start", note: "source=run_start read-only evidence" }
  ];
}

export const storyProgressReadContractPlaceholders: StoryProgressReadContractPlaceholder[] = [
  { label: "계정 ID", field: "accountId", value: "연동 대기", note: "story progress detail 조회 기준" },
  { label: "Act ID", field: "actId", value: "연동 대기", note: "Act 1 read model 연결 후보" },
  { label: "최고 스토리 진행도", field: "highestStoryProgressId", value: "연동 대기", note: "Run detail story progress 연결 후보" },
  { label: "최고 클리어 작전", field: "highestClearedOperationId", value: "연동 대기", note: "highest cleared operation 연결 후보" },
  { label: "진행 순서", field: "order", value: "연동 대기", note: "Act 1 progression order 연결 후보" },
  { label: "클리어 작전 목록", field: "clearedOperationIds", value: "연동 대기", note: "cleared operation ids read-only 표시 후보" },
  { label: "해금 작전 목록", field: "unlockedOperations", value: "연동 대기", note: "unlocked operations read-only 표시 후보" },
  { label: "내러티브 콘텐츠 상태", field: "contentStatus", value: "연동 대기", note: "coverage status 연결 후보" },
  { label: "현지화 상태", field: "localizationStatus", value: "연동 대기", note: "localization status 연결 후보" },
  { label: "fixture 상태", field: "fixtureStatus", value: "연동 대기", note: "Unity fixture status 연결 후보" },
  { label: "내러티브 버전", field: "narrativeVersion", value: "연동 대기", note: "예: act1.2026-08-27.001 후보" },
  { label: "누락 콘텐츠 상태", field: "missingContentStatus", value: "연동 대기", note: "missing-state 연결 후보" },
  { label: "마지막 진행 출격", field: "lastProgressRunId", value: "연동 대기", note: "플레이어 문의 지원 문맥 후보" },
  { label: "진행 갱신 시각", field: "updatedAt", value: "연동 대기", note: "read-only updatedAt 후보" },
  {
    label: "조회 권한 후보",
    field: "permission",
    value: "연동 대기",
    note: `${permissionLabel("STORY_PROGRESS_READ")} / STORY_PROGRESS_READ`
  },
  {
    label: "권한 판정 모드",
    field: "permissionMode",
    value: "연동 대기",
    note: "ROLE_PERMISSION primary / ROLE_READ_ONLY_INTERIM legacy fallback"
  },
  {
    label: "읽기 전용",
    field: "readOnly",
    value: "연동 대기",
    note: "mutation control 금지 경계"
  }
];

export const storyProgressReadContractFilters = [...storyProgressReadContractFilterFields];

export { narrativeCoverageRowsForReadState };

function labelWithCode(label: string, code: string): string {
  return label === code ? code : `${label} / ${code}`;
}

function missionLabelWithCode(missionId: string): string {
  return labelWithCode(storyProgressLabel(missionId), missionId);
}

function statusLabelWithCode(status: string): string {
  return labelWithCode(storyProgressStatusLabel(status), status);
}

function missionListLabel(missionIds: string[]): string {
  return missionIds.length > 0 ? missionIds.map(missionLabelWithCode).join(", ") : "없음";
}

function storySupportContextValue(detail: StoryProgressDetail): string {
  const values = [detail.supportContext.lastProgressRunId, detail.supportContext.updatedAt].filter(
    (value) => value !== "없음" && value !== "연동 대기"
  );
  return values.length > 0 ? values.join(" / ") : "없음";
}

export function runStoryNarrativeCardsForStoryProgressDetail(
  detail: StoryProgressDetail,
  evidence?: RunLaunchEvidence | null
): RunRiskRewardPlaceholder[] {
  const operationId = evidence?.operationId ?? detail.highestClearedOperationId;

  return [
    { ...runStoryNarrativePlaceholders[0], value: missionLabelWithCode(operationId) },
    { ...runStoryNarrativePlaceholders[1], value: missionLabelWithCode(detail.highestStoryProgressId) },
    {
      ...runStoryNarrativePlaceholders[2],
      value: evidence?.firstClearConsumed === undefined || evidence.firstClearConsumed === null
        ? "없음"
        : firstClearLabel(evidence.firstClearConsumed)
    },
    { ...runStoryNarrativePlaceholders[3], value: statusLabelWithCode(detail.contentStatus) },
    { ...runStoryNarrativePlaceholders[4], value: statusLabelWithCode(detail.localizationStatus) },
    { ...runStoryNarrativePlaceholders[5], value: statusLabelWithCode(detail.missingContentStatus) },
    { ...runStoryNarrativePlaceholders[6], value: storySupportContextValue(detail) }
  ];
}

function runStoryNarrativeCardsForLoading(): RunRiskRewardPlaceholder[] {
  return runStoryNarrativePlaceholders.map((placeholder) => ({ ...placeholder, value: "조회 중" }));
}

function runStoryNarrativeCardsForError(error: unknown, accountId: string): RunRiskRewardPlaceholder[] {
  const failure = adminRequestFailureDisplay(error, { resourceLabel: "스토리 진행도", resourceId: accountId });

  return runStoryNarrativePlaceholders.map((placeholder, index) => ({
    ...placeholder,
    value: index === 0 ? accountId : failure.reason
  }));
}

export function storyProgressDetailToReadContractPlaceholders(
  detail: StoryProgressDetail
): StoryProgressReadContractPlaceholder[] {
  return [
    { label: "계정 ID", field: "accountId", value: detail.accountId, note: "story progress detail 조회 기준" },
    { label: "Act ID", field: "actId", value: detail.actId, note: "Act 1 read model 연결 후보" },
    {
      label: "최고 스토리 진행도",
      field: "highestStoryProgressId",
      value: missionLabelWithCode(detail.highestStoryProgressId),
      note: "Run detail story progress 연결 후보"
    },
    {
      label: "최고 클리어 작전",
      field: "highestClearedOperationId",
      value: missionLabelWithCode(detail.highestClearedOperationId),
      note: "highest cleared operation 연결 후보"
    },
    { label: "진행 순서", field: "order", value: String(detail.order), note: "Act 1 progression order 연결 후보" },
    {
      label: "클리어 작전 목록",
      field: "clearedOperationIds",
      value: missionListLabel(detail.clearedOperationIds),
      note: "cleared operation ids read-only 표시 후보"
    },
    {
      label: "해금 작전 목록",
      field: "unlockedOperations",
      value: missionListLabel(detail.unlockedOperations),
      note: "unlocked operations read-only 표시 후보"
    },
    {
      label: "내러티브 콘텐츠 상태",
      field: "contentStatus",
      value: statusLabelWithCode(detail.contentStatus),
      note: "coverage status 연결 후보"
    },
    {
      label: "현지화 상태",
      field: "localizationStatus",
      value: statusLabelWithCode(detail.localizationStatus),
      note: "localization status 연결 후보"
    },
    {
      label: "fixture 상태",
      field: "fixtureStatus",
      value: statusLabelWithCode(detail.fixtureStatus),
      note: "Unity fixture status 연결 후보"
    },
    {
      label: "내러티브 버전",
      field: "narrativeVersion",
      value: detail.narrativeVersion,
      note: "예: act1.2026-08-27.001 후보"
    },
    {
      label: "누락 콘텐츠 상태",
      field: "missingContentStatus",
      value: statusLabelWithCode(detail.missingContentStatus),
      note: "missing-state 연결 후보"
    },
    {
      label: "마지막 진행 출격",
      field: "lastProgressRunId",
      value: detail.supportContext.lastProgressRunId,
      note: "플레이어 문의 지원 문맥 후보"
    },
    {
      label: "진행 갱신 시각",
      field: "updatedAt",
      value: detail.supportContext.updatedAt,
      note: "read-only updatedAt 후보"
    },
    {
      label: "조회 권한 후보",
      field: "permission",
      value: labelWithCode(permissionLabel(detail.supportContext.permission), detail.supportContext.permission),
      note: `${permissionLabel("STORY_PROGRESS_READ")} / STORY_PROGRESS_READ`
    },
    {
      label: "권한 판정 모드",
      field: "permissionMode",
      value: detail.supportContext.permissionMode,
      note: "ROLE_PERMISSION primary / ROLE_READ_ONLY_INTERIM legacy fallback"
    },
    {
      label: "읽기 전용",
      field: "readOnly",
      value: detail.supportContext.readOnly ? "읽기 전용" : "확인 필요",
      note: "mutation control 금지 경계"
    }
  ];
}

function RunDetailFailure({ error, runId }: { error: unknown; runId: string }) {
  const failure = adminRequestFailureDisplay(error, { resourceLabel: "출격", resourceId: runId });

  return (
    <main className="content error">
      <header>
        <h1>{failure.title}</h1>
        <p>{failure.resourceId}</p>
      </header>
      <section className="summary-grid" aria-label="출격 요청 실패 분류">
        <div>
          <span>분류</span>
          <strong>{failure.reason}</strong>
          <small>{failure.nextStep}</small>
        </div>
        <div>
          <span>상태 코드</span>
          <strong>{failure.statusLabel}</strong>
          <small>Admin API response status</small>
        </div>
        <div>
          <span>errorCode</span>
          <strong>{failure.errorCode}</strong>
          <small>{failure.message}</small>
        </div>
        <div>
          <span>경계</span>
          <strong>읽기 전용</strong>
          <small>replay/reprocess/result edit/reward correction 없음</small>
        </div>
      </section>
    </main>
  );
}

export function RunDetailPage() {
  const { runId = "" } = useParams();
  const query = useQuery({
    queryKey: ["run", runId],
    queryFn: () => adminApi.runDetail(runId),
    enabled: runId.length > 0
  });
  const runDetail = query.data;
  const storyProgressAccountId = runDetail?.accountId ?? "";
  const storyProgressQuery = useQuery({
    queryKey: ["story-progress-detail", storyProgressAccountId],
    queryFn: () => adminApi.storyProgressDetail(storyProgressAccountId),
    enabled: storyProgressAccountId.length > 0,
    retry: false
  });
  const narrativeCoverageQuery = useQuery({
    queryKey: ["narrative-coverage", "act_01_wasteland_railway"],
    queryFn: () => adminApi.narrativeCoverage("act_01_wasteland_railway"),
    retry: false
  });

  if (query.isLoading) return <main className="content">출격을 불러오는 중</main>;
  if (query.error) return <RunDetailFailure error={query.error} runId={runId} />;
  if (!query.data) return <main className="content">선택된 출격이 없습니다</main>;

  const run = query.data;
  const storyProgressCards = storyProgressQuery.data
    ? storyProgressDetailToReadContractPlaceholders(storyProgressQuery.data)
    : storyProgressReadContractPlaceholders;
  const storyNarrativeCards = storyProgressQuery.data
    ? runStoryNarrativeCardsForStoryProgressDetail(storyProgressQuery.data, run.launchEvidence)
    : storyProgressQuery.isLoading
      ? runStoryNarrativeCardsForLoading()
      : storyProgressQuery.error
        ? runStoryNarrativeCardsForError(storyProgressQuery.error, storyProgressAccountId)
        : runStoryNarrativePlaceholders;
  const startMetadataCards = runStartMetadataEvidenceFrom(run);
  const riskRewardCards = runRiskRewardCardsForLaunchEvidence(run.launchEvidence);
  const storyRaidPolicyCards = storyRaidPolicyCardsForLaunchEvidence(run.launchEvidence);
  const narrativeCoverageRows =
    narrativeCoverageRowsForReadState({
      data: narrativeCoverageQuery.data,
      isLoading: narrativeCoverageQuery.isLoading,
      error: narrativeCoverageQuery.error
    }) ?? actOneNarrativeCoverage;

  return (
    <main className="content">
      <header>
        <h1>{run.runId}</h1>
        <p>{run.accountId}</p>
      </header>
      <section className="summary-grid">
        <div>
          <span>출격 밸런스 버전</span>
          <strong>{run.balanceVersion}</strong>
        </div>
        <div>
          <span>현재 게시 밸런스 버전</span>
          <strong>{run.currentActiveBalanceVersion}</strong>
        </div>
        <div>
          <span>클라이언트 빌드</span>
          <strong>{run.clientBuild}</strong>
        </div>
        <div>
          <span>검증 런타임 리비전</span>
          <strong>{run.validatorRuntimeRevision}</strong>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>위험도/보상 계산 기준</h2>
          <p>서버 reward preview generation 계약 확정 전까지 계산 구조만 준비합니다</p>
        </header>
        <div className="metric-grid">
          {riskRewardCards.map((placeholder) => (
            <div key={placeholder.label} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>{placeholder.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>스토리 진행도/내러티브 상태</h2>
          <p>스토리 진행과 현지화/내러티브 데이터 상태를 읽기 전용으로 볼 준비만 합니다</p>
        </header>
        <div className="metric-grid">
          {storyNarrativeCards.map((placeholder) => (
            <div key={placeholder.label} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>{placeholder.note}</small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {storyProgressCards.map((placeholder) => (
            <div key={placeholder.field} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>
                {placeholder.field} / {placeholder.note}
              </small>
            </div>
          ))}
        </div>
        <div className="metric-card">
          <span>필터 후보</span>
          <strong>연동 대기</strong>
          <small>{storyProgressReadContractFilters.join(" / ")}</small>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>미션 ID</th>
                <th>표시명</th>
                <th>coverage</th>
                <th>localization</th>
                <th>fixture</th>
                <th>narrative version</th>
                <th>상태 후보</th>
              </tr>
            </thead>
            <tbody>
              {narrativeCoverageRows.map((item) => (
                <tr key={item.missionId}>
                  <td>{item.missionId}</td>
                  <td>{item.displayName}</td>
                  <td>{item.coverageState}</td>
                  <td>{item.localizationState}</td>
                  <td>{item.fixtureState}</td>
                  <td>{item.narrativeVersionState}</td>
                  <td>{item.statusHint}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="metric-grid">
          {raidNarrativeObjectivePlaceholders.map((objective) => (
            <div key={objective.objectiveId} className="metric-card">
              <span>{objective.label}</span>
              <strong>{objective.state}</strong>
              <small>
                {objective.objectiveId} / {objective.note}
              </small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {encounterTypePlaceholders.map((placeholder) => (
            <div key={placeholder.code} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>
                {placeholder.code} / {placeholder.note}
              </small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {raidBossResultPlaceholders.map((placeholder) => (
            <div key={placeholder.field} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>
                {placeholder.field} / {placeholder.note}
              </small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {storyRaidPolicyCards.map((placeholder) => (
            <div key={placeholder.field} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>
                {placeholder.field} / {placeholder.note}
              </small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {startMetadataCards.map((placeholder) => (
            <div key={placeholder.field} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>
                {placeholder.field} / {placeholder.note}
              </small>
            </div>
          ))}
        </div>
      </section>
      <h2>스냅샷</h2>
      <table>
        <thead>
          <tr>
            <th>요원</th>
            <th>적용 레벨</th>
            <th>레벨 출처</th>
            <th>시작 시 동기화</th>
            <th>시작 시 훈련</th>
            <th>스킬</th>
            <th>승급</th>
          </tr>
        </thead>
        <tbody>
          {run.snapshot.operators.map((operator) => (
            <tr key={operator.operatorId}>
              <td>{operator.operatorId}</td>
              <td>{operator.effectiveLevel}</td>
              <td>{levelSourceLabel(operator.levelSource)}</td>
              <td>{operator.syncLevelAtStart}</td>
              <td>{operator.trainedLevelAtStart ?? "-"}</td>
              <td>{operator.skillTraining}</td>
              <td>{operator.promotion}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}
