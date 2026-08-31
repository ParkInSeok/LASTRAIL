import { describe, expect, it } from "vitest";
import {
  dashboardAggregateApiWait,
  dashboardLaunchSectionsForReadState,
  dashboardSections,
  dashboardShortcuts,
  dashboardDetailSections,
  launchDetailSupportCandidates,
  launchDashboardSections,
  offlineRewardClaimDashboardFields,
  offlineRewardDashboardContractFields,
  offlineRewardBlockedDefaultRewards,
  offlineRewardFormulaPlaceholders,
  offlineRewardStateDashboardFields,
  offlineRewardSourceCandidates,
  offlineRewardStoryProgressPolicy
} from "./DashboardPage";

describe("DashboardPage configuration", () => {
  it("keeps the first dashboard screen focused on launch operations summary", () => {
    expect(launchDashboardSections.map((section) => section.title)).toEqual([
      "인증/세션",
      "CS/계정 이슈",
      "무결성 리스크",
      "보상/모집 이슈",
      "치명 시스템 오류"
    ]);
    expect(launchDashboardSections.flatMap((section) => section.metrics.map((metric) => metric.label))).toEqual([
      "활성 세션",
      "마지막 로그인 시각",
      "마지막 로그인 경과",
      "Google 연동 상태",
      "지원 확인 필요 계정",
      "계정 복구 후보",
      "Google 연동 계정",
      "긴급/높음 의심 계정",
      "체크섬 불일치",
      "불가능한 피해량/점수",
      "보상 수령 실패",
      "오프라인 보상 상태",
      "모집 문의 후보",
      "중복 재생 확인",
      "치명 오류",
      "미확인 핵심 실패"
    ]);
    expect(launchDashboardSections.flatMap((section) => section.metrics).every((metric) => metric.value === dashboardAggregateApiWait)).toBe(true);
    expect(JSON.stringify(launchDashboardSections)).toContain("activeSessionCount");
    expect(JSON.stringify(launchDashboardSections)).toContain("lastLoginAt");
    expect(JSON.stringify(launchDashboardSections)).not.toContain("concurrentUsers 후보");
  });

  it("maps dashboard launch summary data into first-screen query state cards", () => {
    const sections = dashboardLaunchSectionsForReadState({
      data: {
        permission: "SUPPORT_READ",
        permissionMode: "ROLE_PERMISSION",
        activeSessions: "2",
        concurrentUsers: "정밀 동접자 백로그 (UNAVAILABLE)",
        recentActiveAccounts5m: "없음",
        recentActiveAccounts30m: "없음",
        recentActiveAccountsSource: "not_available_without_activity_source",
        lastLoginAtLatest: "2026-08-25T11:51:40.000Z",
        lastLoginAgeSeconds: "300초",
        supportReviewNeededCount: "3",
        accountRecoveryCandidateCount: "1",
        integrityHighRiskCount: "4",
        rewardClaimFailureCount: "1",
        offlineRewardIssueCount: "2",
        gachaSupportIssueCount: "1",
        duplicateReplayCount: "7",
        criticalOpenOpsErrorCount: "5",
        unacknowledgedCoreFailureCount: "6",
        serverCalculatedAt: "2026-08-25T11:56:40.000Z",
        supportContext: {
          readOnly: true,
          mutationEndpointsEnabled: false,
          exactConcurrentUsersEnabled: false
        }
      },
      isLoading: false,
      error: null
    });

    expect(sections.flatMap((section) => section.metrics.map((metric) => [metric.label, metric.value]))).toContainEqual([
      "활성 세션",
      "2"
    ]);
    expect(JSON.stringify(sections)).toContain("정밀 동접자 백로그 (UNAVAILABLE)");
    expect(JSON.stringify(sections)).not.toContain(dashboardAggregateApiWait);
  });

  it("moves long policy and placeholder-heavy metrics into detail candidates", () => {
    const launchTitles = launchDashboardSections.map((section) => section.title);
    const detailTitles = dashboardDetailSections.map((section) => section.title);

    expect(launchTitles).not.toContain("오프라인 보상 진행도");
    expect(launchTitles).not.toContain("위험도/보상 불균형");
    expect(detailTitles).toContain("오프라인 보상 진행도");
    expect(detailTitles).toContain("위험도/보상 불균형");
  });

  it("keeps detail-specific launch candidates out of the top dashboard metrics", () => {
    expect(launchDetailSupportCandidates.map((candidate) => candidate.label)).toEqual([
      "Google provider/link visibility",
      "Gacha support visibility",
      "Boss-only raid label/status",
      "Augment-at-start visibility",
      "Player/support login session detail"
    ]);
    expect(launchDetailSupportCandidates.map((candidate) => candidate.status)).toEqual([
      "Support/Player read model 연동됨",
      "Gacha read model 연동됨",
      "Run/Support 표시 준비",
      "Run/Support 표시 준비",
      "Support/Player read model 연동됨"
    ]);
    expect(JSON.stringify(launchDashboardSections)).not.toContain("Boss-only raid label/status");
    expect(JSON.stringify(launchDashboardSections)).not.toContain("Augment-at-start visibility");
  });

  it("includes ad, reward, and risk operations sections in Korean", () => {
    const sectionTitles = dashboardSections.map((section) => section.title);

    expect(sectionTitles).toContain("광고/보상형 광고");
    expect(sectionTitles).toContain("재화/보상");
    expect(sectionTitles).toContain("위험 신호");
    expect(sectionTitles).toContain("위험도/보상 불균형");
    expect(dashboardSections.flatMap((section) => section.metrics.map((metric) => metric.label))).toContain(
      "광고 완료율"
    );
  });

  it("prepares risk reward imbalance metrics without hard-coded economy amounts", () => {
    const riskRewardSection = dashboardSections.find((section) => section.title === "위험도/보상 불균형");
    const labels = riskRewardSection?.metrics.map((metric) => metric.label) ?? [];

    expect(labels).toEqual([
      "위험도별 평균 보상",
      "위험도별 클리어율",
      "위험도별 평균 실패 공세",
      "예상 보상 vs 실제 미리보기 불일치",
      "Risk IV/V 보상 수령 실패율",
      "first clear 보상 지급 상태"
    ]);
    expect(riskRewardSection?.metrics.every((metric) => metric.value === "연동 대기")).toBe(true);
  });

  it("prepares offline reward progression metrics without hard-coded reward amounts", () => {
    const offlineRewardSection = dashboardSections.find((section) => section.title === "오프라인 보상 진행도");
    const labels = offlineRewardSection?.metrics.map((metric) => metric.label) ?? [];

    expect(labels).toEqual([
      "최고 스토리 진행도",
      "공식 계산식",
      "수령 가능 시간",
      "최소 누적 시간",
      "Risk 직접 배율",
      "스토리 배율/보관 한도",
      "허용 반복 보상",
      "기본 제외 보상",
      "상태 row 관측 후보",
      "수령 응답 관측 후보",
      "오프라인 수령 상태"
    ]);
    expect(offlineRewardSection?.metrics.every((metric) => metric.value === "연동 대기")).toBe(true);
    expect(offlineRewardSection?.metrics.map((metric) => metric.note).join(" ")).not.toContain("지휘권");
  });

  it("aligns offline reward contract placeholders with server calculator labels", () => {
    expect(offlineRewardFormulaPlaceholders).toEqual([
      "baseHourlyReward × storyProgressMultiplier × cappedOfflineHours",
      "claimableHours = min(actualOfflineHours, offlineStorageCapHours, maxClaimableHours)",
      "10분 미만 launch 기본 0 reward",
      "Risk I~V는 오프라인 보상에 직접 곱하지 않음"
    ]);
    expect(offlineRewardStoryProgressPolicy[0]).toEqual({
      progressId: "none",
      label: "스토리 진행 없음",
      multiplier: "1.00x",
      capHours: "4h"
    });
    expect(offlineRewardStoryProgressPolicy.at(-1)).toEqual({
      progressId: "raid_raider_king_engine",
      label: "약탈왕 기관차",
      multiplier: "3.00x",
      capHours: "12h"
    });
    expect(offlineRewardSourceCandidates).toEqual([
      "cur_credits",
      "cur_machine_parts",
      "cur_tactical_data",
      "cur_operator_exp"
    ]);
    expect(offlineRewardBlockedDefaultRewards).toEqual([
      "cur_special_core",
      "cur_command_ticket",
      "cur_command_ticket_fragment"
    ]);
  });

  it("maps offline reward preview response fields to dashboard skeleton candidates", () => {
    expect(offlineRewardDashboardContractFields).toEqual([
      "previewStatus",
      "storyProgressId",
      "claimableSeconds",
      "claimableHours",
      "offlineStorageCapHours",
      "minimumClaimSeconds",
      "maxClaimableHours",
      "rewardLines.source",
      "calculation.storyProgressId",
      "calculation.storyProgressMultiplierLabel",
      "calculation.cappedOfflineSeconds",
      "calculation.capSource",
      "narrativeVersion",
      "serverCalculatedAt",
      "errorCode",
      "message"
    ]);
  });

  it("maps offline reward state fields without opening mutation controls", () => {
    const offlineRewardSection = dashboardSections.find((section) => section.title === "오프라인 보상 진행도");
    const stateMetric = offlineRewardSection?.metrics.find((metric) => metric.label === "상태 row 관측 후보");

    expect(offlineRewardStateDashboardFields).toEqual([
      "last_offline_reward_claim_at",
      "last_story_progress_id",
      "last_balance_config_version",
      "last_narrative_version",
      "last_previewed_at",
      "revision",
      "updated_at"
    ]);
    expect(stateMetric?.value).toBe("연동 대기");
    expect(stateMetric?.note).toContain("accounts.created_at lazy fallback");
    expect(stateMetric?.note).toContain("last_previewed_at route 미갱신");
    expect(stateMetric?.note).toContain("preview read-only");
    expect(stateMetric?.note).not.toContain("manual reset");
    expect(stateMetric?.note).not.toContain("state mutation");
  });

  it("maps offline reward claim response references without opening claim controls", () => {
    const offlineRewardSection = dashboardSections.find((section) => section.title === "오프라인 보상 진행도");
    const claimMetric = offlineRewardSection?.metrics.find((metric) => metric.label === "수령 응답 관측 후보");

    expect(offlineRewardClaimDashboardFields).toEqual([
      "claimStatus",
      "offlineRewardClaimId",
      "rewardTransactionId",
      "economyTransactionId",
      "accountMutationId",
      "serverClaimedAt",
      "revision",
      "last_offline_reward_claim_at"
    ]);
    expect(claimMetric?.value).toBe("연동 대기");
    expect(claimMetric?.note).toContain("offlineRewardClaimId");
    expect(claimMetric?.note).toContain("last claim baseline");
    expect(claimMetric?.note).not.toContain("claim button");
    expect(claimMetric?.note).not.toContain("manual reset");
    expect(claimMetric?.note).not.toContain("reward repair");
  });

  it("keeps dashboard shortcuts pointed at existing read-only queues", () => {
    expect(
      dashboardShortcuts.some(
        (shortcut) => shortcut.label === "의심 플레이어" && shortcut.href === "/integrity/suspicious-players"
      )
    ).toBe(true);
    expect(
      dashboardShortcuts.some((shortcut) => shortcut.label === "무결성 이벤트" && shortcut.href === "/integrity")
    ).toBe(true);
  });
});
