export type StoryProgressStatus =
  | "draft_exists"
  | "localization_pending"
  | "fixture_pending"
  | "version_pending"
  | "content_missing"
  | "version_mismatch"
  | "ready";

export type StoryProgressSupportContext = {
  lastProgressRunId: string;
  updatedAt: string;
  permission: "STORY_PROGRESS_READ";
  readOnly: true;
};

export type StoryProgressListItemPreview = {
  accountId: string;
  actId: string;
  highestClearedOperationId: string;
  highestStoryProgressId: string;
  narrativeVersion: string;
  contentStatus: StoryProgressStatus | "연동 대기";
  localizationStatus: StoryProgressStatus | "연동 대기";
  fixtureStatus: StoryProgressStatus | "연동 대기";
  missingContentStatus: StoryProgressStatus | "연동 대기";
  supportContext: StoryProgressSupportContext;
};

export type StoryProgressDetailPreview = StoryProgressListItemPreview & {
  clearedOperations: string[];
  storyFlags: Record<string, string>;
};

export type StoryProgressFiltersPreview = {
  accountId: string;
  actId: string;
  highestStoryProgressId: string;
  highestClearedOperationId: string;
  narrativeVersion: string;
  contentStatus: string;
  localizationStatus: string;
  fixtureStatus: string;
  missingContentStatus: string;
  updatedFrom: string;
  updatedTo: string;
  limit: string;
  cursor: string;
};

export const storyProgressReadContractFilterFields = [
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
] as const;

export type StoryProgressReadContractFilterField = (typeof storyProgressReadContractFilterFields)[number];

export function storyProgressContractFieldLabel(field: StoryProgressReadContractFilterField | string): string {
  return (
    {
      accountId: "계정 ID",
      actId: "Act ID",
      highestStoryProgressId: "최고 스토리 진행도",
      highestClearedOperationId: "최고 클리어 작전",
      narrativeVersion: "내러티브 버전",
      contentStatus: "콘텐츠 상태",
      localizationStatus: "현지화 상태",
      fixtureStatus: "fixture 상태",
      missingContentStatus: "누락 콘텐츠 상태",
      updatedFrom: "갱신 시작 시각",
      updatedTo: "갱신 종료 시각",
      limit: "조회 제한",
      cursor: "커서"
    }[field] ?? field
  );
}

const pendingSupportContext: StoryProgressSupportContext = {
  lastProgressRunId: "연동 대기",
  updatedAt: "연동 대기",
  permission: "STORY_PROGRESS_READ",
  readOnly: true
};

const listItem: StoryProgressListItemPreview = {
  accountId: "연동 대기",
  actId: "act_01_wasteland_railway",
  highestClearedOperationId: "연동 대기",
  highestStoryProgressId: "연동 대기",
  narrativeVersion: "연동 대기",
  contentStatus: "연동 대기",
  localizationStatus: "연동 대기",
  fixtureStatus: "연동 대기",
  missingContentStatus: "연동 대기",
  supportContext: pendingSupportContext
};

export const storyProgressContractPreview = {
  listItem,
  detail: {
    accountId: listItem.accountId,
    actId: listItem.actId,
    highestClearedOperationId: listItem.highestClearedOperationId,
    highestStoryProgressId: listItem.highestStoryProgressId,
    clearedOperations: [],
    storyFlags: {},
    narrativeVersion: listItem.narrativeVersion,
    contentStatus: listItem.contentStatus,
    localizationStatus: listItem.localizationStatus,
    fixtureStatus: listItem.fixtureStatus,
    missingContentStatus: listItem.missingContentStatus,
    supportContext: listItem.supportContext
  },
  filters: Object.fromEntries(storyProgressReadContractFilterFields.map((field) => [field, ""])) as StoryProgressFiltersPreview
};
