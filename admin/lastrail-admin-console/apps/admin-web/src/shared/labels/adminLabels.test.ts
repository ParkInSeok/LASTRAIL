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
  levelSourceLabel,
  opsEnvironmentLabel,
  opsErrorCodeLabel,
  opsServiceLabel,
  opsSeverityLabel,
  permissionLabel,
  priorityLabel,
  resourceTypeLabel,
  reviewStateLabel,
  reviewStatusLabel,
  roleLabel,
  runResultLabel
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
    expect(opsErrorCodeLabel("unknown_ops_error")).toBe("unknown_ops_error");
  });
});
