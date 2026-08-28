import { describe, expect, it } from "vitest";
import {
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
      "clearedOperations",
      "storyFlags",
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
});
