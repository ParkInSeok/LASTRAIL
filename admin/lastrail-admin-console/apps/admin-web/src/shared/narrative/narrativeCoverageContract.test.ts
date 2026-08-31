import { describe, expect, it } from "vitest";
import {
  narrativeCoverageRowsForReadState,
  mapNarrativeCoverageResponse
} from "./narrativeCoverageContract";

describe("narrativeCoverageContract", () => {
  it("maps Act 1 narrative coverage rows with label/code display", () => {
    const result = mapNarrativeCoverageResponse({
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          missionId: "opn_01_last_departure",
          displayName: "마지막 출발",
          contentStatus: "draft_exists",
          localizationStatus: "localization_pending",
          fixtureStatus: "fixture_pending",
          missingContentStatus: "version_pending",
          narrativeVersion: null,
          updatedAt: "2026-08-31T09:00:00.000Z",
          source: "narrative_coverage_read_model"
        }
      ],
      summary: {
        contentMissingCount: 0,
        localizationPendingCount: 1,
        fixturePendingCount: 1,
        versionMismatchCount: 0,
        latestNarrativeVersion: null
      }
    });

    expect(result.items[0]).toEqual({
      missionId: "opn_01_last_departure",
      displayName: "마지막 출발",
      contentStatus: "draft_exists",
      localizationStatus: "localization_pending",
      fixtureStatus: "fixture_pending",
      missingContentStatus: "version_pending",
      narrativeVersion: "없음",
      updatedAt: "2026-08-31T09:00:00.000Z",
      source: "narrative_coverage_read_model"
    });
    expect(result.summary.latestNarrativeVersion).toBe("없음");

    const rows = narrativeCoverageRowsForReadState({ data: result, isLoading: false, error: null });
    expect(rows[0]).toMatchObject({
      missionId: "opn_01_last_departure",
      coverageState: "초안 존재 / draft_exists",
      localizationState: "현지화 대기 / localization_pending",
      fixtureState: "fixture 대기 / fixture_pending",
      narrativeVersionState: "없음"
    });
  });

  it("maps empty coverage success to a data-empty row, not integration pending", () => {
    const result = mapNarrativeCoverageResponse({
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [],
      summary: {
        contentMissingCount: 0,
        localizationPendingCount: 0,
        fixturePendingCount: 0,
        versionMismatchCount: 0,
        latestNarrativeVersion: null
      }
    });

    const rows = narrativeCoverageRowsForReadState({ data: result, isLoading: false, error: null });

    expect(rows).toEqual([
      {
        missionId: "데이터 없음",
        displayName: "데이터 없음",
        coverageState: "데이터 없음",
        localizationState: "데이터 없음",
        fixtureState: "데이터 없음",
        narrativeVersionState: "없음",
        statusHint: "unknown act 또는 coverage row 없음"
      }
    ]);
  });
});
