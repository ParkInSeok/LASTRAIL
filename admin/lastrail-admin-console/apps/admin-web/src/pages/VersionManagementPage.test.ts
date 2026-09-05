import { describe, expect, it } from "vitest";
import {
  versionManagementActionCards,
  versionManagementBoundaryCards,
  versionManagementCandidateFormFields,
  versionManagementStatusCards,
  versionManagementTargetCards
} from "./VersionManagementPage";

describe("VersionManagementPage configuration", () => {
  it("prepares target server cards across target/environment/platform/channel axes", () => {
    expect(versionManagementTargetCards.map((card) => card.label)).toEqual([
      "대상 서버",
      "환경",
      "플랫폼",
      "채널",
      "활성 release/revision",
      "대상 registry"
    ]);
    expect(versionManagementTargetCards.every((card) => card.value === "서버 계약 대기")).toBe(true);
  });

  it("uses field forms for release candidate editing instead of raw JSON editing", () => {
    expect(versionManagementCandidateFormFields.map((field) => field.label)).toEqual([
      "Release ID",
      "앱 최소 빌드",
      "앱 권장 빌드",
      "콘텐츠 버전",
      "콘텐츠 catalog ID",
      "콘텐츠 catalog hash",
      "프로토콜 버전",
      "업데이트 안내",
      "스토어 업데이트 URL",
      "플랫폼",
      "채널"
    ]);
    expect(versionManagementCandidateFormFields.every((field) => field.value === "서버 계약 대기")).toBe(true);
    expect(JSON.stringify(versionManagementCandidateFormFields)).not.toContain("raw JSON");
  });

  it("keeps save, promote, open, and rollback actions disabled until a server contract and target permissions exist", () => {
    expect(versionManagementActionCards.map((card) => [card.code, card.status])).toEqual([
      ["save_draft", "비활성"],
      ["apply_internal_testing", "비활성"],
      ["approve_candidate", "비활성"],
      ["prestage_production", "비활성"],
      ["promote_pointer", "비활성"],
      ["open_release", "비활성"],
      ["rollback_release", "비활성"]
    ]);
    expect(versionManagementActionCards.every((card) => card.note.includes("targetId"))).toBe(true);
  });

  it("surfaces release read-state distinctions and target safety boundaries", () => {
    expect(versionManagementStatusCards.map((card) => card.label)).toContain("충돌");
    expect(versionManagementStatusCards.map((card) => card.label)).toContain("검증 진행 중");
    expect(versionManagementBoundaryCards.map((card) => card.value)).toContain("스프레드시트 제외");
    expect(versionManagementBoundaryCards.map((card) => card.value)).toContain("원격 실행 보류");
  });
});
