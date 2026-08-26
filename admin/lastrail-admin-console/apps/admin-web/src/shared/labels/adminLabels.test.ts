import { describe, expect, it } from "vitest";
import {
  accountStatusLabel,
  auditActionLabel,
  auditResultLabel,
  categoryLabel,
  classificationLabel,
  enforcementLabel,
  evidenceCodeLabel,
  levelSourceLabel,
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
});
