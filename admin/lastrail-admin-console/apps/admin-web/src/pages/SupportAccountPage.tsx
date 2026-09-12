import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { SupportPolicyReads } from "../shared/support/SupportPolicyReads";
import { adminApi } from "../shared/api/client";
import { adminReadStatusLabel, adminReadValueLabel } from "../shared/api/errorDisplay";
import {
  encounterTypeLabel,
  opsErrorCodeLabel,
  opsErrorLogSourceLabel,
  opsErrorLogStatusLabel,
  opsSeverityLabel,
  permissionLabel
} from "../shared/labels/adminLabels";
import {
  accountAuthVisibilityFields,
  accountAuthSessionFieldLabel,
  customerSupportCombatEvidenceFields,
  customerSupportGachaLaunchPolicy,
  customerSupportCaseTypeLabel,
  customerSupportCaseTypes,
  customerSupportContractPreview,
  customerSupportContextFieldLabel,
  customerSupportContextFields,
  customerSupportEscalationActionLabel,
  customerSupportEscalationActions,
  customerSupportEscalationCategories,
  customerSupportEscalationCategoryLabel,
  customerSupportEscalationEvidenceFields,
  customerSupportEscalationSeverityLabel,
  customerSupportEscalationSeverities,
  customerSupportMutationControlsOutOfScope,
  customerSupportRepairAuditFields,
  customerSupportRepairCategories,
  customerSupportRepairCategoryLabel,
  customerSupportSafeSnippetPlaceholders,
  customerSupportSuggestionCaseLabel,
  customerSupportSuggestionCases,
  customerSupportSummarySectionCodes,
  customerSupportSummarySectionLabel,
  emptySupportAccountSummary,
  customerSupportTemplateStatuses
} from "../shared/support/supportContract";
import type { SupportOpsErrorEvidence } from "../shared/support/supportContract";
import type { SupportLoginBonusSummary } from "../shared/support/supportContract";
import type { SupportRecentRunEvidence } from "../shared/support/supportContract";

type SupportCard = {
  code: string;
  field?: string;
  label: string;
  value: string;
  note: string;
};

type SupportOpsErrorEvidenceRow = {
  id: string;
  summary: string;
  occurredAt: string;
  service: string;
  status: string;
  source: string;
  requestId: string;
  messageSummary: string;
  correlations: string;
  links: string;
};

type SupportRecentRunEvidenceRow = {
  id: string;
  operation: string;
  riskOutcome: string;
  encounter: string;
  raidRefs: string;
  packageRefs: string;
  versions: string;
  storyRaidRefund: string;
  rewardPreviewId: string;
};

type SupportLoginBonusEvidenceRow = {
  bonusId: string;
  claimStatus: string;
  claimedAt: string;
  rewardTransactionId: string;
  expectedReward: string;
  rewardLineSummary: string;
  idempotencyReplayKnown: string;
  readOnly: "읽기 전용";
};

function labelWithCode(label: string, code: string): string {
  return label === code ? code : `${label} (${code})`;
}

function correlationSummary(correlationIds: SupportOpsErrorEvidence["correlationIds"]): string {
  const parts = Object.entries(correlationIds)
    .filter((entry): entry is [string, string] => typeof entry[1] === "string" && entry[1].length > 0)
    .map(([key, value]) => `${key}=${value}`);
  return parts.length > 0 ? parts.join(" / ") : "safe correlation ids 없음";
}

function linkSummary(links: SupportOpsErrorEvidence["links"]): string {
  return links.length > 0 ? links.map((link) => `${link.label}: ${link.href}`).join(" / ") : "없음";
}

export function supportOpsErrorEvidenceRows(
  items: SupportOpsErrorEvidence[],
  integrationPending = true
): SupportOpsErrorEvidenceRow[] {
  if (items.length === 0) {
    if (!integrationPending) {
      return [
        {
          id: "데이터 없음",
          summary: "없음",
          occurredAt: "없음",
          service: "없음",
          status: "없음",
          source: "없음",
          requestId: "없음",
          messageSummary: "최근 시스템 에러 근거 없음",
          correlations: "safe correlation ids 없음",
          links: "없음"
        }
      ];
    }
    return [
      {
        id: "연동 대기",
        summary: "연동 대기",
        occurredAt: "연동 대기",
        service: "연동 대기",
        status: "연동 대기",
        source: "연동 대기",
        requestId: "연동 대기",
        messageSummary: "latest 5 / critical first read-only evidence 대기",
        correlations: "safe correlation ids 연동 대기",
        links: "연동 대기"
      }
    ];
  }

  return items.map((item) => ({
    id: adminReadValueLabel({ kind: "success", value: item.errorLogId }),
    summary: `${labelWithCode(opsSeverityLabel(item.severity), item.severity)} / ${labelWithCode(
      opsErrorCodeLabel(item.errorCode),
      item.errorCode
    )}`,
    occurredAt: adminReadValueLabel({ kind: "success", value: item.occurredAt }),
    service: labelWithCode(opsErrorLogSourceLabel(item.service), item.service),
    status: labelWithCode(opsErrorLogStatusLabel(item.status), item.status),
    source: labelWithCode(opsErrorLogSourceLabel(item.source), item.source),
    requestId: adminReadValueLabel({ kind: "success", value: item.requestId }),
    messageSummary: adminReadValueLabel({ kind: "success", value: item.messageSummary }),
    correlations: correlationSummary(item.correlationIds),
    links: linkSummary(item.links)
  }));
}

function listSummary(values: string[]): string {
  return values.length > 0 ? values.join(" / ") : "없음";
}

function rewardLineSummary(
  lines: SupportLoginBonusSummary["expectedRewardLines"] | SupportLoginBonusSummary["rewardLineSummary"],
  emptyLabel = "없음"
): string {
  if (lines.length === 0) return emptyLabel;
  return lines
    .map((line) =>
      [
        `${line.kind} ${line.id} x${line.quantity}`,
        `source=${line.source}`,
        line.balanceAfter ? `balanceAfter=${line.balanceAfter}` : null
      ]
        .filter(Boolean)
        .join(" ")
    )
    .join(" / ");
}

function loginBonusClaimStatusLabel(status: string): string {
  if (status === "CLAIMED") return "수령 완료";
  if (status === "UNCLAIMED") return "미수령";
  return status;
}

export function supportAccountReadStatusLabel(state: {
  hasData: boolean;
  isError: boolean;
  isLoading: boolean;
  isPlaceholderAccountId?: boolean;
}): string {
  if (state.isPlaceholderAccountId) return "계정 ID 선택 필요";
  return adminReadStatusLabel(state);
}

export function supportLoginBonusEvidenceRows(summary: SupportLoginBonusSummary): SupportLoginBonusEvidenceRow[] {
  const isPending = summary.bonusId === "연동 대기" && summary.claimStatus === "연동 대기";
  return [
    {
      bonusId: summary.bonusId,
      claimStatus: labelWithCode(loginBonusClaimStatusLabel(summary.claimStatus), summary.claimStatus),
      claimedAt: summary.claimedAt,
      rewardTransactionId: summary.rewardTransactionId,
      expectedReward: rewardLineSummary(summary.expectedRewardLines, isPending ? "연동 대기" : "없음"),
      rewardLineSummary: rewardLineSummary(summary.rewardLineSummary, isPending ? "연동 대기" : "없음"),
      idempotencyReplayKnown: summary.idempotencyReplayKnown
        ? "재생 기록 확인됨"
        : adminReadValueLabel({ kind: "success", value: isPending ? null : "재생 기록 없음", emptyLabel: "연동 대기" }),
      readOnly: "읽기 전용"
    }
  ];
}

function storyRaidRefundSummary(marker: SupportRecentRunEvidence["startMetadata"]["failureStaminaRefundMarker"]): string {
  if (
    marker.status === "연동 대기" &&
    marker.resource === "연동 대기" &&
    marker.reason === "연동 대기" &&
    marker.policyId === "연동 대기" &&
    marker.serverOwnedMutation === "연동 대기" &&
    marker.fuelLedgerReason === "연동 대기"
  ) {
    return "연동 대기";
  }
  if (
    marker.status === "없음" &&
    marker.resource === "없음" &&
    marker.reason === "없음" &&
    marker.policyId === "없음" &&
    marker.serverOwnedMutation === "없음" &&
    marker.fuelLedgerReason === "없음"
  ) {
    return "없음";
  }
  return `status=${marker.status} / resource=${marker.resource} / reason=${marker.reason} / policyId=${marker.policyId} / mutation=${marker.serverOwnedMutation} / ledgerReason=${marker.fuelLedgerReason}`;
}

export function supportRecentRunEvidenceRows(
  items: SupportRecentRunEvidence[],
  integrationPending = true
): SupportRecentRunEvidenceRow[] {
  if (items.length === 0) {
    if (!integrationPending) {
      return [
        {
          id: "데이터 없음",
          operation: "없음",
          riskOutcome: "없음",
          encounter: "없음",
          raidRefs: "없음",
          packageRefs: "없음",
          versions: "없음",
          storyRaidRefund: "없음",
          rewardPreviewId: "없음"
        }
      ];
    }
    return [
      {
        id: "연동 대기",
        operation: "연동 대기",
        riskOutcome: "연동 대기",
        encounter: "연동 대기",
        raidRefs: "연동 대기",
        packageRefs: "server-authored run start package 연동 대기",
        versions: "연동 대기",
        storyRaidRefund: "연동 대기",
        rewardPreviewId: "연동 대기"
      }
    ];
  }

  return items.map((item) => {
    const metadata = item.startMetadata;
    return {
      id: item.runId,
      operation: item.operationId,
      riskOutcome: `${item.risk} / ${item.outcome}`,
      encounter:
        metadata.encounterType === "연동 대기"
          ? "연동 대기"
          : labelWithCode(encounterTypeLabel(metadata.encounterType), metadata.encounterType),
      raidRefs: `raidId=${metadata.raidId} / bossId=${metadata.bossId} / bossPhaseRefs=${listSummary(
        metadata.bossPhaseRefs
      )}`,
      packageRefs: `startAugmentPackageRef=${metadata.startAugmentPackageRef} / startTacticPackageRef=${metadata.startTacticPackageRef} / source=${metadata.source}`,
      versions: `contentVersion=${metadata.contentVersion} / balanceVersion=${metadata.balanceVersion} / allowedOperatorSlots=${metadata.allowedOperatorSlots}`,
      storyRaidRefund: storyRaidRefundSummary(metadata.failureStaminaRefundMarker),
      rewardPreviewId: item.rewardPreviewId
    };
  });
}

export const supportCaseTypeCards: SupportCard[] = customerSupportCaseTypes.map((caseType) => ({
  code: caseType,
  label: customerSupportCaseTypeLabel(caseType),
  value: "분류 기준",
  note: `${caseType} read-only case 후보`
}));

export const supportAccountContextCards: SupportCard[] = customerSupportContextFields.map((field) => ({
  code: field,
  field,
  label: customerSupportContextFieldLabel(field),
  value: "지원 문맥 후보",
  note:
    field === "recentOpsErrorLogs"
      ? "recent open/acknowledged latest 5, critical first, source/correlation ids read-only compose 후보"
      : `${field} sanitized reference 후보`
}));

export const supportSummarySectionCards: SupportCard[] = customerSupportSummarySectionCodes.map((section) => ({
  code: section,
  label: customerSupportSummarySectionLabel(section),
  value: "support summary 계약",
  note: `${section} sanitized read-only section`
}));

export const supportAccountAuthEvidenceCards: SupportCard[] = [
  ...accountAuthVisibilityFields.map((field) => ({
    code: field,
    label: accountAuthSessionFieldLabel(field),
    value: "Support summary 연동됨",
    note:
      field === "concurrentUsers"
        ? "UNAVAILABLE/null은 확정 동접자 수 아님"
        : `${field} read-only auth/session evidence 후보`
  }))
];

export const supportCombatEvidenceCards: SupportCard[] = customerSupportCombatEvidenceFields.map((field) => ({
  code: field,
  label:
    {
      encounterType: "Encounter 유형",
      raidId: "레이드 ID",
      bossId: "보스 ID",
      bossDefeated: "보스 격파 여부",
      completedPhaseCount: "완료 페이즈 수",
      encounterDurationSeconds: "전투 소요 시간",
      startAugmentPackageRef: "시작 증강 패키지 ref",
      startTacticPackageRef: "시작 전술 패키지 ref",
      selectedAugmentIds: "선택 증강 ID",
      selectedTacticIds: "선택 전술 ID",
      source: "근거 소스"
    }[field] ?? field,
  value: "Support recentRuns 연동됨",
  note:
    field === "encounterType"
      ? `${encounterTypeLabel("ORDINARY_15_WAVE")} / ${encounterTypeLabel("RAID_BOSS")} / ORDINARY_15_WAVE / RAID_BOSS`
      : field === "source"
        ? "source=run_start read-only evidence 후보"
        : field === "startAugmentPackageRef" || field === "startTacticPackageRef"
          ? "server-authored run start package / reward/economy/story/offline/gacha Source of Truth 아님"
        : `${field} sanitized run/raid reference 후보`
}));

export const supportGachaLaunchEvidenceCards: SupportCard[] = customerSupportGachaLaunchPolicy.evidenceFields.map((field) => ({
  code: field,
  label:
    {
      activeBannerId: "활성 배너 ID",
      bannerType: "배너 유형",
      pickupOperatorId: "픽업 요원 ID",
      pullCountVisibility: "1회/10회 모집",
      pityThresholds: "천장 기준",
      guaranteeType: "보장 발동",
      duplicateConversion: "중복 변환",
      resultFields: "결과 필드",
      ticketAcquisition: "지휘권 획득처",
      actOneEliteVisualScope: "Act 1 정예 요원 10인"
    }[field] ?? field,
  value:
    field === "actOneEliteVisualScope"
      ? customerSupportGachaLaunchPolicy.scopeStatus
      : field === "pityThresholds" || field === "ticketAcquisition"
        ? "정책 기준"
        : "Gacha read model 연동됨",
  note:
    field === "pityThresholds"
      ? "rare 10 / elite 30 / pickup 60 draft/contract candidate"
      : field === "ticketAcquisition"
        ? [
            `launch non-repeat total ${customerSupportGachaLaunchPolicy.ticketAcquisition.totalNonRepeatTickets}`,
            `first-clear ${customerSupportGachaLaunchPolicy.ticketAcquisition.firstClearTickets.quantity} ${customerSupportGachaLaunchPolicy.ticketAcquisition.firstClearTickets.state}`,
            `login bonus ${customerSupportGachaLaunchPolicy.ticketAcquisition.loginBonusTickets.quantity} ${customerSupportGachaLaunchPolicy.ticketAcquisition.loginBonusTickets.state}`,
            `cost=${customerSupportGachaLaunchPolicy.ticketAcquisition.costItemId}`,
            customerSupportGachaLaunchPolicy.ticketAcquisition.boundary,
            ...customerSupportGachaLaunchPolicy.ticketAcquisition.sources.map(
              (source) => `${source.label} ${source.quantity} repeatable=${source.repeatable}${"state" in source ? ` state=${source.state}` : ""}`
            ),
            `offline repeat reward excludes ${customerSupportGachaLaunchPolicy.ticketAcquisition.excludedOfflineRepeatRewards.join(
              " / "
            )}`
          ].join(" / ")
      : field === "actOneEliteVisualScope"
        ? [
            customerSupportGachaLaunchPolicy.scopeStatus,
            `productionFinal=${customerSupportGachaLaunchPolicy.productionFinal}`,
            `seedPromotionApproved=${customerSupportGachaLaunchPolicy.seedPromotionApproved}`,
            ...customerSupportGachaLaunchPolicy.actOneEliteOperatorIds
          ].join(" / ")
        : `${field} gacha launch read-only support evidence 후보`
}));

export const supportOutOfScopeControls = [...customerSupportMutationControlsOutOfScope];

export const supportResponseTemplateCards: SupportCard[] = customerSupportTemplateStatuses.map((status) => ({
  code: status,
  label:
    {
      internalChecks: "내부 확인",
      playerFacingTemplate: "플레이어 안내 템플릿",
      escalateWhen: "운영 검토 조건"
    }[status] ?? status,
  value: "추천 문구 준비",
  note: `${status} read-only template 상태`
}));

export const supportSafeSnippetCards: SupportCard[] = customerSupportSafeSnippetPlaceholders.map((snippet) => ({
  code: snippet,
  label:
    {
      server_record_checked: "서버 기록 확인",
      duplicate_replay_existing_result: "중복 요청 기존 결과",
      offline_claimable_cap_applied: "오프라인 보관 한도 적용",
      first_clear_finish_time_basis: "첫 클리어 완료 시점 기준",
      progress_and_text_are_separate: "진행도와 표시 문구 분리",
      rng_probability_not_disclosed: "확률·난수 비공개",
      escalated_for_ops_review: "운영 검토 전달"
    }[snippet] ?? snippet,
  value: "안전 설명 준비",
  note: `${snippet} safe explanation 후보`
}));

export const supportSuggestionCards: SupportCard[] = customerSupportSuggestionCases.map((suggestion) => ({
  code: suggestion.caseType,
  label: customerSupportSuggestionCaseLabel(suggestion.caseType),
  value: "추천 문구 준비",
  note: suggestion.safeSnippet
}));

export const supportEscalationSeverityCards: SupportCard[] = customerSupportEscalationSeverities.map((severity) => ({
  code: severity,
  label: customerSupportEscalationSeverityLabel(severity),
  value: "분류 기준",
  note: `${severity} escalation severity 후보`
}));

export const supportEscalationCategoryCards: SupportCard[] = customerSupportEscalationCategories.map((category) => ({
  code: category,
  label: customerSupportEscalationCategoryLabel(category),
  value: "분류 기준",
  note: `${category} escalation category 후보`
}));

export const supportEscalationActionCards: SupportCard[] = customerSupportEscalationActions.map((action) => ({
  code: action,
  label: customerSupportEscalationActionLabel(action),
  value: "권장 처리 후보",
  note: `${action} recommended action 후보`
}));

export const supportEscalationEvidenceCards: SupportCard[] = customerSupportEscalationEvidenceFields.map((field) => ({
  code: field,
  label:
    {
      severity: "심각도",
      category: "분류",
      recommendedAction: "권장 처리",
      primaryReferenceId: "주요 참조 ID",
      readOnlyEvidencePackage: "읽기 전용 근거 묶음"
    }[field] ?? field,
  value: "근거 묶음 후보",
  note: `${field} evidence package 후보`
}));

export const supportRepairPolicyCards: SupportCard[] = customerSupportRepairCategories.map((category) => ({
  code: category,
  label: customerSupportRepairCategoryLabel(category),
  value: "비활성",
  note: `${category} repair/compensation launch disabled`
}));

export const supportRepairAuditCards: SupportCard[] = customerSupportRepairAuditFields.map((field) => ({
  code: field,
  label:
    {
      supportTicketId: "지원 티켓 ID",
      accountId: "계정 ID",
      operatorId: "처리자 ID",
      approverId: "승인자 ID",
      permissionUsed: "사용 권한",
      reasonCode: "사유 코드",
      affectedIds: "영향 ID",
      beforeAfterSanitizedSummary: "전후 sanitized 요약"
    }[field] ?? field,
  value: "backlog",
  note: `${field} future repair audit 후보`
}));

export function SupportAccountPage() {
  const { accountId = "pending" } = useParams();
  const isPlaceholderAccountId = accountId === "pending";
  const query = useQuery({
    queryKey: ["support-account", accountId],
    queryFn: () => adminApi.supportAccountSummary(accountId),
    retry: false,
    enabled: !isPlaceholderAccountId
  });
  const safeSummary = !query.isError && query.data?.account.accountId === accountId ? query.data : undefined;
  const activities = useQuery({
    queryKey: ["player-activities", accountId],
    queryFn: () => adminApi.playerActivities(accountId),
    enabled: !isPlaceholderAccountId,
    retry: false
  });
  const summary = safeSummary ?? emptySupportAccountSummary(isPlaceholderAccountId ? "계정 ID 선택 필요" : accountId);
  const hasSummaryData = Boolean(safeSummary);
  const opsErrorRows = supportOpsErrorEvidenceRows(summary.opsErrors, !hasSummaryData);
  const recentRunRows = supportRecentRunEvidenceRows(summary.recentRuns, !hasSummaryData);
  const loginBonusRows = supportLoginBonusEvidenceRows(summary.loginBonus);

  return (
    <main className="content">
      <header>
        <h1>CS 지원</h1>
        <p>{summary.account.displayName === "연동 대기" ? accountId : summary.account.displayName}</p>
      </header>
      <SupportPolicyReads state={isPlaceholderAccountId ? { kind: "select_account" } : activities.isError ? { kind: "error", error: activities.error } : activities.data?.accountId === accountId ? { kind: "ready", sections: activities.data.sections } : { kind: "loading" }} />
      <section className="summary-grid" aria-label="CS 지원 요약">
        <div>
          <span>권한</span>
          <strong>{permissionLabel(summary.permission)}</strong>
          <small>{summary.permission}</small>
        </div>
        <div>
          <span>권한 모드</span>
          <strong>{summary.permissionMode}</strong>
          <small>ROLE_PERMISSION</small>
        </div>
        <div>
          <span>연동 상태</span>
          <strong>
            {supportAccountReadStatusLabel({
              hasData: hasSummaryData,
              isError: query.isError,
              isLoading: query.isLoading,
              isPlaceholderAccountId
            })}
          </strong>
          <small>{customerSupportContractPreview.endpointCandidate}</small>
        </div>
        <div>
          <span>mutation</span>
          <strong>비활성</strong>
          <small>launch 1차 read-only</small>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>지원 요약 섹션</h2>
          <p>Admin API support summary section을 sanitized read-only로 표시합니다</p>
        </header>
        <div className="metric-grid">
          {summary.sections.map((section) => (
            <div key={section.code} className="metric-card">
              <span>{section.label}</span>
              <strong>{section.value}</strong>
              <small>{section.code} / {section.referenceCount}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>Google 연동 근거</h2>
          <p>provider/link/account recovery 상태를 token 없이 읽기 전용으로 확인하는 후보입니다</p>
        </header>
        <div className="metric-grid">
          {summary.authSession.items.map((item) => (
            <div key={item.field} className="metric-card">
              <span>{item.label}</span>
              <strong>{item.value}</strong>
              <small>{item.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>문의 유형</h2>
          <p>지원 응답을 분류하기 위한 stable case type 후보입니다</p>
        </header>
        <div className="metric-grid">
          {supportCaseTypeCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>계정 지원 문맥</h2>
          <p>correlation/reference id와 sanitized status 중심의 읽기 전용 후보입니다</p>
        </header>
        <div className="metric-grid">
          {supportAccountContextCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>최근 시스템 에러 근거</h2>
          <p>support summary에 compose된 latest 5 / critical first 시스템 에러를 읽기 전용으로 표시합니다</p>
        </header>
        <table className="data-table">
          <thead>
            <tr>
              <th>발생 시각</th>
              <th>요약</th>
              <th>서비스/소스</th>
              <th>상태</th>
              <th>requestId</th>
              <th>관련 식별자</th>
              <th>링크</th>
            </tr>
          </thead>
          <tbody>
            {opsErrorRows.map((row) => (
              <tr key={row.id}>
                <td>{row.occurredAt}</td>
                <td>
                  <strong>{row.summary}</strong>
                  <small>{row.messageSummary}</small>
                </td>
                <td>
                  {row.service}
                  <small>{row.source}</small>
                </td>
                <td>{row.status}</td>
                <td>{row.requestId}</td>
                <td>{row.correlations}</td>
                <td>{row.links}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      <section className="admin-panel">
        <header>
          <h2>출격/레이드 전투 근거</h2>
          <p>일반 15공세와 보스 전용 레이드를 분리하고 run start 패키지 근거만 읽기 전용으로 표시합니다</p>
        </header>
        <table className="data-table">
          <thead>
            <tr>
              <th>runId</th>
              <th>작전</th>
              <th>위험도/결과</th>
              <th>encounter</th>
              <th>레이드 ref</th>
              <th>시작 패키지 ref</th>
              <th>버전/슬롯</th>
              <th>스토리 레이드 환급</th>
              <th>rewardPreviewId</th>
            </tr>
          </thead>
          <tbody>
            {recentRunRows.map((row) => (
              <tr key={row.id}>
                <td>{row.id}</td>
                <td>{row.operation}</td>
                <td>{row.riskOutcome}</td>
                <td>{row.encounter}</td>
                <td>{row.raidRefs}</td>
                <td>{row.packageRefs}</td>
                <td>{row.versions}</td>
                <td>{row.storyRaidRefund}</td>
                <td>{row.rewardPreviewId}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="metric-grid">
          {supportCombatEvidenceCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>모집 문의 근거</h2>
          <p>launch 포함 gacha 흐름을 batch/result/보장/중복 변환 기준으로 읽기 전용 확인하는 후보입니다</p>
        </header>
        <div className="metric-grid">
          {supportGachaLaunchEvidenceCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>로그인 보너스 근거</h2>
          <p>launch login bonus 5장 수령 상태를 서버 support summary 기준으로 읽기 전용 표시합니다</p>
        </header>
        <table className="data-table">
          <thead>
            <tr>
              <th>bonusId</th>
              <th>수령 상태</th>
              <th>수령 시각</th>
              <th>rewardTransactionId</th>
              <th>예상 보상</th>
              <th>저장 보상 요약</th>
              <th>idempotency</th>
              <th>권한</th>
            </tr>
          </thead>
          <tbody>
            {loginBonusRows.map((row) => (
              <tr key={row.bonusId}>
                <td>{row.bonusId}</td>
                <td>{row.claimStatus}</td>
                <td>{row.claimedAt}</td>
                <td>{row.rewardTransactionId}</td>
                <td>{row.expectedReward}</td>
                <td>{row.rewardLineSummary}</td>
                <td>{row.idempotencyReplayKnown}</td>
                <td>{row.readOnly}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      <section className="admin-panel">
        <header>
          <h2>응답 템플릿 상태</h2>
          <p>internal checks / player-facing template / escalate when 구조의 read-only placeholder입니다</p>
        </header>
        <div className="metric-grid">
          {supportResponseTemplateCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>안전 설명 snippet 후보</h2>
          <p>전체 문구 하드코딩 없이 지원 안내 가능 여부만 표시합니다</p>
        </header>
        <div className="metric-grid">
          {supportSafeSnippetCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>CS 추천 문구 후보</h2>
          <p>case type별 짧은 안전 설명을 읽기 전용 상태로만 표시합니다</p>
        </header>
        <div className="metric-grid">
          {supportSuggestionCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>Escalation 분류</h2>
          <p>launch 범위에서는 SUPPORT_READ 기반의 읽기 전용 판단 후보만 표시합니다</p>
        </header>
        <div className="metric-grid">
          {supportEscalationSeverityCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {supportEscalationCategoryCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>권장 처리와 근거 묶음</h2>
          <p>write/ticket/repair 없이 추천 action과 evidence package 필드만 준비합니다</p>
        </header>
        <div className="metric-grid">
          {supportEscalationActionCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
        <div className="metric-grid">
          {supportEscalationEvidenceCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>Repair/Compensation 정책</h2>
          <p>launch에서는 수동 복구와 보상 처리를 제공하지 않는 read-only 정책 상태입니다</p>
        </header>
        <div className="metric-grid">
          {supportRepairPolicyCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>Repair audit 후보</h2>
          <p>future repair 승인 흐름이 열릴 때 필요한 sanitized audit 필드 후보입니다</p>
        </header>
        <div className="metric-grid">
          {supportRepairAuditCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.code}</small>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
