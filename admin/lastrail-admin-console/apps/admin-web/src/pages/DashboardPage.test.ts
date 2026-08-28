import { describe, expect, it } from "vitest";
import {
  dashboardSections,
  dashboardShortcuts,
  offlineRewardDashboardContractFields,
  offlineRewardBlockedDefaultRewards,
  offlineRewardFormulaPlaceholders,
  offlineRewardStateDashboardFields,
  offlineRewardSourceCandidates,
  offlineRewardStoryProgressPolicy
} from "./DashboardPage";

describe("DashboardPage configuration", () => {
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
