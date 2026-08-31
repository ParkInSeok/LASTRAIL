import { describe, expect, it } from "vitest";
import {
  mapStoryProgressDetailResponse,
  mapStoryProgressListResponse,
  storyProgressContractFieldLabel,
  storyProgressContractPreview,
  storyProgressReadContractFilterFields
} from "./storyProgressContract";

describe("storyProgressContract", () => {
  it("prepares read-only list, detail, filter, and support context preview shapes", () => {
    expect(Object.keys(storyProgressContractPreview.listItem)).toEqual([
      "accountId",
      "actId",
      "highestClearedOperationId",
      "highestStoryProgressId",
      "order",
      "clearedOperationIds",
      "unlockedOperations",
      "narrativeVersion",
      "contentStatus",
      "localizationStatus",
      "fixtureStatus",
      "missingContentStatus",
      "supportContext"
    ]);
    expect(Object.keys(storyProgressContractPreview.detail)).toEqual([
      "accountId",
      "actId",
      "highestClearedOperationId",
      "highestStoryProgressId",
      "order",
      "clearedOperationIds",
      "unlockedOperations",
      "narrativeVersion",
      "contentStatus",
      "localizationStatus",
      "fixtureStatus",
      "missingContentStatus",
      "supportContext"
    ]);
    expect(storyProgressContractPreview.listItem.supportContext).toEqual({
      lastProgressRunId: "연동 대기",
      updatedAt: "연동 대기",
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_PERMISSION",
      readOnly: true
    });
  });

  it("keeps filter preview aligned with future Admin query fields", () => {
    expect(storyProgressReadContractFilterFields).toEqual([
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
    expect(Object.keys(storyProgressContractPreview.filters)).toEqual(storyProgressReadContractFilterFields);
  });

  it("uses display labels while preserving unknown field fallback", () => {
    expect(storyProgressContractFieldLabel("highestStoryProgressId")).toBe("최고 스토리 진행도");
    expect(storyProgressContractFieldLabel("narrativeVersion")).toBe("내러티브 버전");
    expect(storyProgressContractFieldLabel("missingContentStatus")).toBe("누락 콘텐츠 상태");
    expect(storyProgressContractFieldLabel("futureField")).toBe("futureField");
  });

  it("does not include mutation controls or sensitive/raw diagnostic examples", () => {
    const serialized = JSON.stringify(storyProgressContractPreview);

    expect(serialized).not.toContain("mutation");
    expect(serialized).not.toContain("repair");
    expect(serialized).not.toContain("token");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("raw body");
  });

  it("maps story progress list responses to the read-only contract shape", () => {
    const result = mapStoryProgressListResponse({
      items: [
        {
          accountId: "account-1",
          actId: "act_01_wasteland_railway",
          operationId: "opn_03_water_stop",
          storyProgress: {
            highestStoryProgressId: "opn_03_water_stop",
            highestClearedOperationId: "opn_03_water_stop",
            order: 3
          },
          clearedOperationIds: ["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop"],
          unlockedOperations: ["opn_04_scrapstar_show"],
          narrative: {
            narrativeVersion: "act1.2026-08-27.001",
            contentStatus: "CANDIDATE",
            localizationStatus: "PENDING",
            fixtureStatus: "PARTIAL",
            missingContentStatus: "UNKNOWN"
          },
          supportContext: {
            source: "account_story_progress",
            readOnly: true
          },
          updatedAt: "2026-08-28T03:00:00.000Z"
        }
      ],
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_PERMISSION",
      nextCursor: "cursor-2"
    });

    expect(result.items[0]).toEqual({
      accountId: "account-1",
      actId: "act_01_wasteland_railway",
      highestStoryProgressId: "opn_03_water_stop",
      highestClearedOperationId: "opn_03_water_stop",
      order: 3,
      clearedOperationIds: ["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop"],
      unlockedOperations: ["opn_04_scrapstar_show"],
      narrativeVersion: "act1.2026-08-27.001",
      contentStatus: "CANDIDATE",
      localizationStatus: "PENDING",
      fixtureStatus: "PARTIAL",
      missingContentStatus: "UNKNOWN",
      supportContext: {
        lastProgressRunId: "없음",
        updatedAt: "2026-08-28T03:00:00.000Z",
        permission: "STORY_PROGRESS_READ",
        permissionMode: "ROLE_PERMISSION",
        readOnly: true
      }
    });
    expect(result.permissionMode).toBe("ROLE_PERMISSION");
    expect(result.nextCursor).toBe("cursor-2");
  });

  it("keeps legacy read-only interim permission mode as a fallback", () => {
    const result = mapStoryProgressListResponse({
      items: [],
      permission: "STORY_PROGRESS_READ",
      permissionMode: "ROLE_READ_ONLY_INTERIM",
      nextCursor: null
    });

    expect(result.permissionMode).toBe("ROLE_READ_ONLY_INTERIM");
  });

  it("maps no-row story progress detail to a safe none default", () => {
    const result = mapStoryProgressDetailResponse(null, "account-no-row");

    expect(result).toEqual({
      accountId: "account-no-row",
      actId: "act_01_wasteland_railway",
      highestStoryProgressId: "none",
      highestClearedOperationId: "none",
      order: 0,
      clearedOperationIds: [],
      unlockedOperations: [],
      narrativeVersion: "없음",
      contentStatus: "content_missing",
      localizationStatus: "localization_pending",
      fixtureStatus: "fixture_pending",
      missingContentStatus: "content_missing",
      supportContext: {
        lastProgressRunId: "없음",
        updatedAt: "없음",
        permission: "STORY_PROGRESS_READ",
        permissionMode: "ROLE_PERMISSION",
        readOnly: true
      }
    });
  });
});
