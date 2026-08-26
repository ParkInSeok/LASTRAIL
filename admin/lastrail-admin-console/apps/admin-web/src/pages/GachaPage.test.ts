import { describe, expect, it } from "vitest";
import { gachaReadOnlySections } from "./GachaPage";

describe("GachaPage configuration", () => {
  it("covers banner, pity, pull history, guarantee, errors, conversion, and ledger read-only needs", () => {
    const sectionTitles = gachaReadOnlySections.map((section) => section.title);

    expect(sectionTitles).toContain("배너 상태");
    expect(sectionTitles).toContain("계정별 천장 상태");
    expect(sectionTitles).toContain("뽑기 이력");
    expect(sectionTitles).toContain("보장 발동");
    expect(sectionTitles).toContain("거절/오류 코드");
    expect(sectionTitles).toContain("중복/조각 변환");
    expect(sectionTitles).toContain("아이템 원장 연결");
  });

  it("marks every section as pending server query API integration", () => {
    expect(gachaReadOnlySections.every((section) => section.status === "조회 API 대기")).toBe(true);
  });

  it("uses Korean labels while keeping stable backend codes visible", () => {
    const fields = gachaReadOnlySections.flatMap((section) => section.fields);

    expect(fields).toContain("60뽑 픽업 정예 보장 (PICKUP_ELITE_60)");
    expect(fields).toContain("이전 결과 재조회 (DUPLICATE_REPLAY)");
    expect(fields).toContain("지휘권 부족 (insufficient_command_ticket)");
  });
});
