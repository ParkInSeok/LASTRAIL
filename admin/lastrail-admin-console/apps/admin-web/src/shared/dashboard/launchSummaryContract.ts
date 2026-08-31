import { adminReadValueLabel } from "../api/errorDisplay";

export type DashboardLaunchSummary = {
  permission: string;
  permissionMode: string;
  activeSessions: string;
  concurrentUsers: string;
  recentActiveAccounts5m: string;
  recentActiveAccounts30m: string;
  recentActiveAccountsSource: string;
  lastLoginAtLatest: string;
  lastLoginAgeSeconds: string;
  supportReviewNeededCount: string;
  accountRecoveryCandidateCount: string;
  integrityHighRiskCount: string;
  rewardClaimFailureCount: string;
  offlineRewardIssueCount: string;
  gachaSupportIssueCount: string;
  duplicateReplayCount: string;
  criticalOpenOpsErrorCount: string;
  unacknowledgedCoreFailureCount: string;
  serverCalculatedAt: string;
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
    exactConcurrentUsersEnabled: false;
  };
};

export type DashboardLaunchMetric = {
  label: string;
  value: string;
  note: string;
};

export type DashboardLaunchSection = {
  title: string;
  description: string;
  metrics: DashboardLaunchMetric[];
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function readValue(value: unknown, emptyLabel = "없음"): string {
  return adminReadValueLabel({ kind: "success", value, emptyLabel });
}

function numberValue(value: unknown): string {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "0";
}

function exactConcurrentUsersLabel(value: unknown, precision: unknown): string {
  if ((value === null || value === undefined) && precision === "UNAVAILABLE") return "정밀 동접자 백로그 (UNAVAILABLE)";
  if (typeof value === "number" && Number.isFinite(value)) return `${value}`;
  return "정밀도 대기";
}

function secondsValue(value: unknown): string {
  return typeof value === "number" && Number.isFinite(value) ? `${value}초` : "없음";
}

export function mapDashboardLaunchSummaryResponse(response: unknown): DashboardLaunchSummary {
  const record = asRecord(response) ?? {};
  const buckets = asRecord(record.lastLoginAgeBuckets);
  const supportContext = asRecord(record.supportContext);

  return {
    permission: readValue(record.permission, "SUPPORT_READ"),
    permissionMode: readValue(record.permissionMode, "ROLE_PERMISSION"),
    activeSessions: numberValue(record.activeSessions),
    concurrentUsers: exactConcurrentUsersLabel(record.concurrentUsers, record.concurrentUsersPrecision),
    recentActiveAccounts5m: readValue(record.recentActiveAccounts5m),
    recentActiveAccounts30m: readValue(record.recentActiveAccounts30m),
    recentActiveAccountsSource: readValue(record.recentActiveAccountsSource),
    lastLoginAtLatest: readValue(record.lastLoginAtLatest),
    lastLoginAgeSeconds: secondsValue(buckets?.latestAgeSeconds),
    supportReviewNeededCount: numberValue(record.supportReviewNeededCount),
    accountRecoveryCandidateCount: numberValue(record.accountRecoveryCandidateCount),
    integrityHighRiskCount: numberValue(record.integrityHighRiskCount),
    rewardClaimFailureCount: numberValue(record.rewardClaimFailureCount),
    offlineRewardIssueCount: numberValue(record.offlineRewardIssueCount),
    gachaSupportIssueCount: numberValue(record.gachaSupportIssueCount),
    duplicateReplayCount: numberValue(record.duplicateReplayCount),
    criticalOpenOpsErrorCount: numberValue(record.criticalOpenOpsErrorCount),
    unacknowledgedCoreFailureCount: numberValue(record.unacknowledgedCoreFailureCount),
    serverCalculatedAt: readValue(record.serverCalculatedAt),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      exactConcurrentUsersEnabled: supportContext?.exactConcurrentUsersEnabled === true ? false : false
    }
  };
}

export function emptyDashboardLaunchSummary(): DashboardLaunchSummary {
  return {
    permission: "SUPPORT_READ",
    permissionMode: "ROLE_PERMISSION",
    activeSessions: "연동 대기",
    concurrentUsers: "정밀 동접자 백로그 (UNAVAILABLE)",
    recentActiveAccounts5m: "백로그",
    recentActiveAccounts30m: "백로그",
    recentActiveAccountsSource: "activity source 대기",
    lastLoginAtLatest: "연동 대기",
    lastLoginAgeSeconds: "연동 대기",
    supportReviewNeededCount: "연동 대기",
    accountRecoveryCandidateCount: "연동 대기",
    integrityHighRiskCount: "연동 대기",
    rewardClaimFailureCount: "연동 대기",
    offlineRewardIssueCount: "연동 대기",
    gachaSupportIssueCount: "연동 대기",
    duplicateReplayCount: "연동 대기",
    criticalOpenOpsErrorCount: "연동 대기",
    unacknowledgedCoreFailureCount: "연동 대기",
    serverCalculatedAt: "연동 대기",
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      exactConcurrentUsersEnabled: false
    }
  };
}

export function dashboardLaunchSummaryMetricCards(summary: DashboardLaunchSummary): DashboardLaunchSection[] {
  return [
    {
      title: "인증/세션",
      description: "Google 로그인과 활성 세션 상태를 보는 launch 핵심 요약",
      metrics: [
        { label: "활성 세션", value: summary.activeSessions, note: "activeSessions" },
        { label: "마지막 로그인 시각", value: summary.lastLoginAtLatest, note: "lastLoginAtLatest" },
        { label: "마지막 로그인 경과", value: summary.lastLoginAgeSeconds, note: "lastLoginAgeBuckets.latestAgeSeconds" },
        { label: "정밀 동접자", value: summary.concurrentUsers, note: "exact concurrent users는 launch backlog" }
      ]
    },
    {
      title: "CS/계정 이슈",
      description: "지원 문의와 계정 복구 확인이 필요한 항목",
      metrics: [
        { label: "지원 확인 필요 계정", value: summary.supportReviewNeededCount, note: "supportReviewNeededCount" },
        { label: "계정 복구 후보", value: summary.accountRecoveryCandidateCount, note: "accountRecoveryCandidateCount" },
        { label: "Google 연동 계정", value: summary.accountRecoveryCandidateCount, note: "guest to Google recovery evidence" }
      ]
    },
    {
      title: "무결성 리스크",
      description: "launch 운영자가 먼저 봐야 하는 계정/출격 위험 신호",
      metrics: [
        { label: "긴급/높음 의심 계정", value: summary.integrityHighRiskCount, note: "integrityHighRiskCount" },
        { label: "체크섬 불일치", value: summary.integrityHighRiskCount, note: "integrity high-risk summary" },
        { label: "불가능한 피해량/점수", value: summary.integrityHighRiskCount, note: "integrity high-risk summary" }
      ]
    },
    {
      title: "보상/모집 이슈",
      description: "보상 수령, 오프라인 보상, 모집 문의 대응 요약",
      metrics: [
        { label: "보상 수령 실패", value: summary.rewardClaimFailureCount, note: "rewardClaimFailureCount" },
        { label: "오프라인 보상 상태", value: summary.offlineRewardIssueCount, note: "offlineRewardIssueCount" },
        { label: "모집 문의 후보", value: summary.gachaSupportIssueCount, note: "gachaSupportIssueCount" },
        { label: "중복 재생 확인", value: summary.duplicateReplayCount, note: "duplicateReplayCount" }
      ]
    },
    {
      title: "치명 시스템 오류",
      description: "핵심 서버 실패와 운영 확인이 필요한 런타임 오류",
      metrics: [
        { label: "치명 오류", value: summary.criticalOpenOpsErrorCount, note: "criticalOpenOpsErrorCount" },
        { label: "미확인 핵심 실패", value: summary.unacknowledgedCoreFailureCount, note: "unacknowledgedCoreFailureCount" }
      ]
    }
  ];
}
