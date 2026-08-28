import { describe, expect, it } from "vitest";
import {
  accountStatusLabel,
  auditActionLabel,
  auditResultLabel,
  categoryLabel,
  classificationLabel,
  enforcementLabel,
  evidenceCodeLabel,
  gachaErrorCodeLabel,
  gachaGuaranteeTypeLabel,
  gachaResultStatusLabel,
  firstClearLabel,
  levelSourceLabel,
  opsEnvironmentLabel,
  opsErrorCodeLabel,
  opsServiceLabel,
  opsSeverityLabel,
  offlineRewardClaimStatusLabel,
  offlineRewardPreviewStatusLabel,
  permissionLabel,
  priorityLabel,
  resourceTypeLabel,
  reviewStateLabel,
  reviewStatusLabel,
  rewardCalculationSourceLabel,
  riskLevelLabel,
  roleLabel,
  runResultLabel,
  storyProgressStatusLabel,
  storyProgressLabel
} from "./adminLabels";

describe("adminLabels", () => {
  it("translates visible operational enums into Korean labels", () => {
    expect(priorityLabel("CRITICAL")).toBe("긴급");
    expect(priorityLabel("HIGH")).toBe("높음");
    expect(classificationLabel("HARD_VIOLATION")).toBe("중대 위반");
    expect(reviewStateLabel("IN_REVIEW")).toBe("검토 중");
    expect(enforcementLabel("NONE")).toBe("조치 없음");
    expect(runResultLabel("CLEARED")).toBe("완료");
    expect(accountStatusLabel("ACTIVE")).toBe("활성");
    expect(levelSourceLabel("SYNC")).toBe("동기화");
    expect(auditResultLabel("SUCCESS")).toBe("성공");
    expect(roleLabel("OWNER")).toBe("소유자");
    expect(categoryLabel("DAMAGE_ANALYSIS")).toBe("피해량 분석");
    expect(evidenceCodeLabel("IMPOSSIBLE_DAMAGE")).toBe("불가능한 피해량");
    expect(auditActionLabel("PLAYER_NOTE_CREATE")).toBe("플레이어 노트 작성");
    expect(resourceTypeLabel("account")).toBe("계정");
    expect(permissionLabel("BALANCE_PUBLISH")).toBe("밸런스 게시");
    expect(permissionLabel("STORY_PROGRESS_READ")).toBe("스토리 진행 조회");
  });

  it("keeps unknown backend codes readable instead of hiding them", () => {
    expect(enforcementLabel("CUSTOM_MARK")).toBe("CUSTOM_MARK");
    expect(evidenceCodeLabel("UNKNOWN_SIGNAL")).toBe("UNKNOWN_SIGNAL");
  });

  it("translates gacha guarantee and result codes into Korean labels", () => {
    expect(gachaGuaranteeTypeLabel("RARE_OR_HIGHER_10")).toBe("10회 희귀 이상 보장");
    expect(gachaGuaranteeTypeLabel("ELITE_30")).toBe("30뽑 정예 확정");
    expect(gachaGuaranteeTypeLabel("PICKUP_ELITE_60")).toBe("60뽑 픽업 정예 확정");
    expect(gachaGuaranteeTypeLabel("NONE")).toBe("보장 없음");
    expect(gachaResultStatusLabel("ACCEPTED")).toBe("처리 완료");
    expect(gachaResultStatusLabel("DUPLICATE_REPLAY")).toBe("이전 결과 재조회");
    expect(gachaResultStatusLabel("REJECTED")).toBe("거절");
  });

  it("translates gacha error codes while preserving unknown fallback", () => {
    expect(gachaErrorCodeLabel("insufficient_command_ticket")).toBe("지휘권 부족");
    expect(gachaErrorCodeLabel("auth_required")).toBe("인증 필요");
    expect(gachaErrorCodeLabel("banner_inactive")).toBe("비활성 모집 배너");
    expect(gachaErrorCodeLabel("invalid_pull_count")).toBe("잘못된 모집 횟수");
    expect(gachaErrorCodeLabel("gacha_temporarily_unavailable")).toBe("모집 일시 사용 불가");
    expect(gachaErrorCodeLabel("unknown_gacha_code")).toBe("unknown_gacha_code");
  });

  it("translates ops error log environment, service, and severity labels", () => {
    expect(opsEnvironmentLabel("DEV")).toBe("개발");
    expect(opsEnvironmentLabel("PRD")).toBe("운영");
    expect(opsServiceLabel("Pages Function")).toBe("Pages Function");
    expect(opsServiceLabel("Unity Client Diagnostic")).toBe("Unity 클라이언트 진단");
    expect(opsSeverityLabel("warn")).toBe("경고");
    expect(opsSeverityLabel("error")).toBe("오류");
    expect(opsSeverityLabel("fatal")).toBe("치명 오류");
  });

  it("translates risk reward ops error codes while preserving unknown fallback", () => {
    expect(opsErrorCodeLabel("reward_plan_mismatch")).toBe("보상 계획 불일치");
    expect(opsErrorCodeLabel("reward_multiplier_missing")).toBe("보상 배율 누락");
    expect(opsErrorCodeLabel("reward_plan_unavailable")).toBe("보상 계획 사용 불가");
    expect(opsErrorCodeLabel("risk_reward_policy_missing")).toBe("위험도 보상 정책 누락");
    expect(opsErrorCodeLabel("reward_integrity_restricted")).toBe("무결성 판정으로 보상 제한");
    expect(opsErrorCodeLabel("offline_story_progress_missing")).toBe("오프라인 보상 스토리 진행도 누락");
    expect(opsErrorCodeLabel("offline_reward_policy_missing")).toBe("오프라인 보상 정책 누락");
    expect(opsErrorCodeLabel("offline_reward_claim_unavailable")).toBe("오프라인 보상 수령 불가");
    expect(opsErrorCodeLabel("offline_reward_state_conflict")).toBe("오프라인 보상 상태 충돌");
    expect(opsErrorCodeLabel("duplicate_replay")).toBe("중복 재생");
    expect(opsErrorCodeLabel("offline_reward_not_claimable")).toBe("오프라인 보상 수령 대상 없음");
    expect(opsErrorCodeLabel("offline_reward_claim_conflict")).toBe("오프라인 보상 수령 충돌");
    expect(opsErrorCodeLabel("offline_reward_plan_mismatch")).toBe("오프라인 보상 계획 불일치");
    expect(opsErrorCodeLabel("economy_mutation_conflict")).toBe("경제 변경 충돌");
    expect(opsErrorCodeLabel("offline_reward_integrity_restricted")).toBe(
      "무결성 판정으로 오프라인 보상 제한"
    );
    expect(opsErrorCodeLabel("story_progress_policy_missing")).toBe("스토리 진행 정책 누락");
    expect(opsErrorCodeLabel("story_progress_state_conflict")).toBe("스토리 진행 상태 충돌");
    expect(opsErrorCodeLabel("narrative_content_missing")).toBe("내러티브 콘텐츠 누락");
    expect(opsErrorCodeLabel("narrative_version_mismatch")).toBe("내러티브 버전 불일치");
    expect(opsErrorCodeLabel("contract_violation")).toBe("계약 위반");
    expect(opsErrorCodeLabel("unknown_ops_error")).toBe("unknown_ops_error");
  });

  it("translates offline reward preview status while preserving unknown fallback", () => {
    expect(offlineRewardPreviewStatusLabel("AVAILABLE")).toBe("수령 가능");
    expect(offlineRewardPreviewStatusLabel("EMPTY")).toBe("수령 없음");
    expect(offlineRewardPreviewStatusLabel("REJECTED")).toBe("거절");
    expect(offlineRewardPreviewStatusLabel("FUTURE_STATUS")).toBe("FUTURE_STATUS");
  });

  it("translates offline reward claim status while preserving unknown fallback", () => {
    expect(offlineRewardClaimStatusLabel("CLAIMED")).toBe("수령 완료");
    expect(offlineRewardClaimStatusLabel("DUPLICATE_REPLAY")).toBe("중복 재생");
    expect(offlineRewardClaimStatusLabel("REJECTED")).toBe("거절");
    expect(offlineRewardClaimStatusLabel("FUTURE_CLAIM_STATUS")).toBe("FUTURE_CLAIM_STATUS");
  });

  it("translates story progress stable ids while preserving unknown fallback", () => {
    expect(storyProgressLabel("none")).toBe("스토리 진행 없음");
    expect(storyProgressLabel("opn_01_last_departure")).toBe("마지막 출발");
    expect(storyProgressLabel("opn_02_first_signal")).toBe("첫 신호");
    expect(storyProgressLabel("opn_03_water_stop")).toBe("급수 정차");
    expect(storyProgressLabel("opn_04_scrapstar_show")).toBe("스크랩스타 쇼");
    expect(storyProgressLabel("opn_05_night_boarders")).toBe("야간 승선전");
    expect(storyProgressLabel("opn_06_broken_line")).toBe("끊어진 선로");
    expect(storyProgressLabel("raid_raider_king_engine")).toBe("약탈왕 기관차");
    expect(storyProgressLabel(null)).toBe("스토리 진행 없음");
    expect(storyProgressLabel("future_story_id")).toBe("future_story_id");
  });

  it("translates story progress and narrative status codes while preserving unknown fallback", () => {
    expect(storyProgressStatusLabel("draft_exists")).toBe("초안 존재");
    expect(storyProgressStatusLabel("localization_pending")).toBe("현지화 대기");
    expect(storyProgressStatusLabel("fixture_pending")).toBe("fixture 대기");
    expect(storyProgressStatusLabel("version_pending")).toBe("버전 대기");
    expect(storyProgressStatusLabel("content_missing")).toBe("콘텐츠 누락");
    expect(storyProgressStatusLabel("version_mismatch")).toBe("버전 불일치");
    expect(storyProgressStatusLabel("ready")).toBe("준비 완료");
    expect(storyProgressStatusLabel("future_status")).toBe("future_status");
  });

  it("translates risk reward display labels without deriving calculation values", () => {
    expect(riskLevelLabel(1)).toBe("위험도 I");
    expect(riskLevelLabel(5)).toBe("위험도 V");
    expect(riskLevelLabel(7)).toBe("위험도 7");
    expect(firstClearLabel(true)).toBe("첫 클리어 보너스");
    expect(firstClearLabel(false)).toBe("첫 클리어 보너스 없음");
    expect(rewardCalculationSourceLabel("SERVER_REWARD_PREVIEW")).toBe("서버 보상 미리보기");
    expect(rewardCalculationSourceLabel("UNITY_DIAGNOSTIC_HINT")).toBe("Unity 진단 참고 신호");
    expect(rewardCalculationSourceLabel("UNKNOWN_SOURCE")).toBe("UNKNOWN_SOURCE");
  });
});
