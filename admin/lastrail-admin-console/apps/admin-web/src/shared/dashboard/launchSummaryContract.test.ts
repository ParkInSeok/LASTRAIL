import { describe, expect, it } from "vitest";
import { mapDashboardLaunchSummaryResponse, dashboardLaunchSummaryMetricCards } from "./launchSummaryContract";

describe("launch dashboard summary contract", () => {
  it("maps server launch summary values and unavailable concurrent users without generic pending", () => {
    const summary = mapDashboardLaunchSummaryResponse({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      activeSessions: 2,
      concurrentUsers: null,
      concurrentUsersPrecision: "UNAVAILABLE",
      recentActiveAccounts5m: null,
      recentActiveAccounts30m: null,
      recentActiveAccountsSource: "not_available_without_activity_source",
      lastLoginAtLatest: "2026-08-25T11:51:40.000Z",
      lastLoginAgeBuckets: { latestAgeSeconds: 300, exactConcurrentUsersAvailable: false },
      supportReviewNeededCount: 3,
      accountRecoveryCandidateCount: 1,
      integrityHighRiskCount: 4,
      rewardClaimFailureCount: 1,
      offlineRewardIssueCount: 2,
      gachaSupportIssueCount: 1,
      duplicateReplayCount: 7,
      criticalOpenOpsErrorCount: 5,
      unacknowledgedCoreFailureCount: 6,
      serverCalculatedAt: "2026-08-25T11:56:40.000Z",
      supportContext: { readOnly: true, mutationEndpointsEnabled: false, exactConcurrentUsersEnabled: false }
    });

    expect(summary.activeSessions).toBe("2");
    expect(summary.concurrentUsers).toBe("정밀 동접자 백로그 (UNAVAILABLE)");
    expect(summary.lastLoginAtLatest).toBe("2026-08-25T11:51:40.000Z");
    expect(summary.lastLoginAgeSeconds).toBe("300초");
    expect(summary.supportContext.mutationEndpointsEnabled).toBe(false);

    const cards = dashboardLaunchSummaryMetricCards(summary);
    expect(cards.flatMap((section) => section.metrics.map((metric) => [metric.label, metric.value]))).toContainEqual([
      "활성 세션",
      "2"
    ]);
    expect(JSON.stringify(cards)).not.toContain("대시보드 집계 API 대기");
  });

  it("keeps success empty summary as zero/none semantics", () => {
    const summary = mapDashboardLaunchSummaryResponse({
      activeSessions: 0,
      concurrentUsers: null,
      concurrentUsersPrecision: "UNAVAILABLE",
      lastLoginAtLatest: null,
      supportReviewNeededCount: 0,
      accountRecoveryCandidateCount: 0,
      integrityHighRiskCount: 0,
      rewardClaimFailureCount: 0,
      offlineRewardIssueCount: 0,
      gachaSupportIssueCount: 0,
      duplicateReplayCount: 0,
      criticalOpenOpsErrorCount: 0,
      unacknowledgedCoreFailureCount: 0
    });

    expect(summary.activeSessions).toBe("0");
    expect(summary.lastLoginAtLatest).toBe("없음");
    expect(summary.supportReviewNeededCount).toBe("0");
  });
});
