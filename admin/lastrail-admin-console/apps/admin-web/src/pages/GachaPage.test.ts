import { describe, expect, it } from "vitest";
import { gachaReadOnlySections } from "./GachaPage";

describe("GachaPage configuration", () => {
  it("covers banner, pity, pull history, conversion, and ledger read-only needs", () => {
    const sectionTitles = gachaReadOnlySections.map((section) => section.title);

    expect(sectionTitles).toContain("배너 상태");
    expect(sectionTitles).toContain("계정별 천장 상태");
    expect(sectionTitles).toContain("뽑기 이력");
    expect(sectionTitles).toContain("중복/조각 변환");
    expect(sectionTitles).toContain("아이템 원장 연결");
  });

  it("marks every section as pending server query API integration", () => {
    expect(gachaReadOnlySections.every((section) => section.status === "조회 API 대기")).toBe(true);
  });
});
