import { describe, expect, it } from "vitest";
import {
  actOneNarrativeCoverage,
  storyProgressDetailToReadContractPlaceholders,
  raidNarrativeObjectivePlaceholders,
  runRiskRewardPlaceholders,
  runStoryNarrativePlaceholders,
  storyProgressReadContractFilters,
  storyProgressReadContractPlaceholders
} from "./RunDetailPage";

describe("RunDetailPage risk reward skeleton", () => {
  it("prepares run risk reward read-only placeholders without economy amounts", () => {
    const labels = runRiskRewardPlaceholders.map((placeholder) => placeholder.label);

    expect(labels).toEqual([
      "위험도",
      "작전 ID",
      "결과",
      "클리어 공세",
      "rewardPreviewId / rewardClaimId",
      "적용 배율",
      "first clear",
      "무결성 판정",
      "계산 메타데이터 상태"
    ]);
    expect(runRiskRewardPlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
  });

  it("keeps risk and calculation placeholders aligned with display labels", () => {
    const notes = runRiskRewardPlaceholders.map((placeholder) => placeholder.note);

    expect(notes).toContain("위험도 I~V 표시 후보");
    expect(notes).toContain("첫 클리어 보너스 표시 후보");
    expect(notes).toContain("서버 보상 미리보기 / Unity 진단 참고 신호 분리");
  });

  it("prepares story progress and narrative status placeholders without narrative text", () => {
    const labels = runStoryNarrativePlaceholders.map((placeholder) => placeholder.label);

    expect(labels).toEqual([
      "작전 ID",
      "스토리 진행도",
      "첫 클리어 상태",
      "미션 내러티브 상태",
      "현지화 데이터 상태",
      "누락 데이터 상태",
      "플레이어 문의 지원 문맥"
    ]);
    expect(runStoryNarrativePlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
    expect(runStoryNarrativePlaceholders.map((placeholder) => placeholder.note).join(" ")).not.toContain("대사");
  });

  it("aligns story progress placeholders with read-only admin query contract fields", () => {
    expect(storyProgressReadContractPlaceholders.map((placeholder) => placeholder.field)).toEqual([
      "accountId",
      "actId",
      "highestStoryProgressId",
      "highestClearedOperationId",
      "order",
      "clearedOperationIds",
      "unlockedOperations",
      "contentStatus",
      "localizationStatus",
      "fixtureStatus",
      "narrativeVersion",
      "missingContentStatus",
      "lastProgressRunId",
      "updatedAt",
      "permission",
      "readOnly"
    ]);
    expect(storyProgressReadContractPlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
    expect(storyProgressReadContractPlaceholders.map((placeholder) => placeholder.note).join(" ")).toContain(
      "STORY_PROGRESS_READ"
    );
  });

  it("maps story progress detail data into run detail read-only cards", () => {
    const placeholders = storyProgressDetailToReadContractPlaceholders({
      accountId: "account-1",
      actId: "act_01_wasteland_railway",
      highestStoryProgressId: "opn_05_night_boarders",
      highestClearedOperationId: "opn_05_night_boarders",
      order: 5,
      clearedOperationIds: ["opn_01_last_departure", "opn_05_night_boarders"],
      unlockedOperations: ["opn_06_broken_line"],
      narrativeVersion: "act1.2026-08-27.001",
      contentStatus: "ready",
      localizationStatus: "localization_pending",
      fixtureStatus: "future_status",
      missingContentStatus: "content_missing",
      supportContext: {
        lastProgressRunId: "run-5",
        updatedAt: "2026-08-28T06:00:00.000Z",
        permission: "ROLE_READ_ONLY_INTERIM",
        readOnly: true
      }
    });

    expect(placeholders.map((placeholder) => [placeholder.field, placeholder.value])).toEqual([
      ["accountId", "account-1"],
      ["actId", "act_01_wasteland_railway"],
      ["highestStoryProgressId", "야간 승선전 / opn_05_night_boarders"],
      ["highestClearedOperationId", "야간 승선전 / opn_05_night_boarders"],
      ["order", "5"],
      ["clearedOperationIds", "마지막 출발 / opn_01_last_departure, 야간 승선전 / opn_05_night_boarders"],
      ["unlockedOperations", "끊어진 선로 / opn_06_broken_line"],
      ["contentStatus", "준비 완료 / ready"],
      ["localizationStatus", "현지화 대기 / localization_pending"],
      ["fixtureStatus", "future_status"],
      ["narrativeVersion", "act1.2026-08-27.001"],
      ["missingContentStatus", "콘텐츠 누락 / content_missing"],
      ["lastProgressRunId", "run-5"],
      ["updatedAt", "2026-08-28T06:00:00.000Z"],
      ["permission", "ROLE_READ_ONLY_INTERIM"],
      ["readOnly", "읽기 전용"]
    ]);
  });

  it("prepares story progress read-only filter skeleton fields", () => {
    expect(storyProgressReadContractFilters).toEqual([
      "accountId",
      "actId",
      "highestStoryProgressId",
      "highestClearedOperationId",
      "narrativeVersion",
      "contentStatus",
      "localizationStatus",
      "fixtureStatus",
      "missingContentStatus",
      "updatedFrom",
      "updatedTo",
      "limit",
      "cursor"
    ]);
  });

  it("lists Act 1 narrative coverage by stable mission id with placeholder states", () => {
    expect(actOneNarrativeCoverage.map((item) => item.missionId)).toEqual([
      "opn_01_last_departure",
      "opn_02_first_signal",
      "opn_03_water_stop",
      "opn_04_scrapstar_show",
      "opn_05_night_boarders",
      "opn_06_broken_line",
      "raid_raider_king_engine"
    ]);
    expect(actOneNarrativeCoverage.map((item) => item.displayName)).toEqual([
      "마지막 출발",
      "첫 신호",
      "급수 정차",
      "스크랩스타 쇼",
      "야간 승선전",
      "끊어진 선로",
      "약탈왕 기관차"
    ]);
    expect(actOneNarrativeCoverage.every((item) => item.coverageState === "연동 대기")).toBe(true);
    expect(actOneNarrativeCoverage.every((item) => item.localizationState === "연동 대기")).toBe(true);
    expect(actOneNarrativeCoverage.every((item) => item.narrativeVersionState === "연동 대기")).toBe(true);
    expect(actOneNarrativeCoverage.map((item) => item.statusHint).join(" ")).not.toContain("보상량");
  });

  it("prepares raid optional objectives as read-only scoring candidates", () => {
    expect(raidNarrativeObjectivePlaceholders.map((objective) => objective.objectiveId)).toEqual([
      "prisoner_car_preserved",
      "broadcast_relay_captured",
      "supply_car_stabilized",
      "fuel_core_stable",
      "engine_integrity"
    ]);
    expect(raidNarrativeObjectivePlaceholders.every((objective) => objective.state === "연동 대기")).toBe(true);
    expect(raidNarrativeObjectivePlaceholders.every((objective) => objective.note.includes("read-only"))).toBe(true);
  });
});
