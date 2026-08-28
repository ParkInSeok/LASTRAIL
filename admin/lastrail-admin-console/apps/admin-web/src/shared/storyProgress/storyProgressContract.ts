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
  permission: "STORY_PROGRESS_READ" | "ROLE_READ_ONLY_INTERIM" | string;
  permissionMode: "ROLE_PERMISSION" | "ROLE_READ_ONLY_INTERIM" | string;
  readOnly: true;
};

export type StoryProgressListItem = {
  accountId: string;
  actId: string;
  highestClearedOperationId: string;
  highestStoryProgressId: string;
  order: number;
  clearedOperationIds: string[];
  unlockedOperations: string[];
  narrativeVersion: string;
  contentStatus: StoryProgressStatus | "연동 대기" | string;
  localizationStatus: StoryProgressStatus | "연동 대기" | string;
  fixtureStatus: StoryProgressStatus | "연동 대기" | string;
  missingContentStatus: StoryProgressStatus | "연동 대기" | string;
  supportContext: StoryProgressSupportContext;
};

export type StoryProgressListItemPreview = StoryProgressListItem;

export type StoryProgressDetail = StoryProgressListItem;

export type StoryProgressDetailPreview = StoryProgressDetail;

export type StoryProgressListResponse = {
  permission: "STORY_PROGRESS_READ" | string;
  permissionMode: "ROLE_PERMISSION" | "ROLE_READ_ONLY_INTERIM" | string;
  items: StoryProgressListItem[];
  nextCursor: string | null;
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
  permissionMode: "ROLE_PERMISSION",
  readOnly: true
};

export function emptyStoryProgressDetail(accountId = "연동 대기"): StoryProgressDetail {
  return {
    accountId,
    actId: "act_01_wasteland_railway",
    highestClearedOperationId: "none",
    highestStoryProgressId: "none",
    order: 0,
    clearedOperationIds: [],
    unlockedOperations: [],
    narrativeVersion: "연동 대기",
    contentStatus: "content_missing",
    localizationStatus: "localization_pending",
    fixtureStatus: "fixture_pending",
    missingContentStatus: "content_missing",
    supportContext: pendingSupportContext
  };
}

const listItem: StoryProgressListItemPreview = {
  accountId: "연동 대기",
  actId: "act_01_wasteland_railway",
  highestClearedOperationId: "연동 대기",
  highestStoryProgressId: "연동 대기",
  order: 0,
  clearedOperationIds: [],
  unlockedOperations: [],
  narrativeVersion: "연동 대기",
  contentStatus: "연동 대기",
  localizationStatus: "연동 대기",
  fixtureStatus: "연동 대기",
  missingContentStatus: "연동 대기",
  supportContext: pendingSupportContext
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function stringValue(value: unknown, fallback: string): string {
  return typeof value === "string" && value.length > 0 ? value : fallback;
}

function numberValue(value: unknown, fallback: number): number {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

function stringArrayValue(value: unknown): string[] {
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
}

function mapSupportContext(
  value: unknown,
  fallback: { lastProgressRunId?: unknown; updatedAt?: unknown; permission?: unknown; permissionMode?: unknown } = {}
): StoryProgressSupportContext {
  const record = asRecord(value);
  return {
    lastProgressRunId: stringValue(record?.lastProgressRunId ?? fallback.lastProgressRunId, "연동 대기"),
    updatedAt: stringValue(record?.updatedAt ?? fallback.updatedAt, "연동 대기"),
    permission: stringValue(record?.permission ?? fallback.permission, "STORY_PROGRESS_READ"),
    permissionMode: stringValue(record?.permissionMode ?? fallback.permissionMode, "ROLE_PERMISSION"),
    readOnly: true
  };
}

function mapStoryProgressItem(
  value: unknown,
  fallbackAccountId = "연동 대기",
  fallbackPermission: unknown = "STORY_PROGRESS_READ",
  fallbackPermissionMode: unknown = "ROLE_PERMISSION"
): StoryProgressListItem {
  const record = asRecord(value);
  if (!record) return emptyStoryProgressDetail(fallbackAccountId);
  const storyProgress = asRecord(record.storyProgress);
  const narrative = asRecord(record.narrative);

  return {
    accountId: stringValue(record.accountId, fallbackAccountId),
    actId: stringValue(record.actId, "act_01_wasteland_railway"),
    highestStoryProgressId: stringValue(storyProgress?.highestStoryProgressId ?? record.highestStoryProgressId, "none"),
    highestClearedOperationId: stringValue(
      storyProgress?.highestClearedOperationId ?? record.highestClearedOperationId,
      "none"
    ),
    order: numberValue(storyProgress?.order ?? record.order, 0),
    clearedOperationIds: stringArrayValue(record.clearedOperationIds),
    unlockedOperations: stringArrayValue(record.unlockedOperations),
    narrativeVersion: stringValue(narrative?.narrativeVersion ?? record.narrativeVersion, "연동 대기"),
    contentStatus: stringValue(narrative?.contentStatus ?? record.contentStatus, "content_missing"),
    localizationStatus: stringValue(narrative?.localizationStatus ?? record.localizationStatus, "localization_pending"),
    fixtureStatus: stringValue(narrative?.fixtureStatus ?? record.fixtureStatus, "fixture_pending"),
    missingContentStatus: stringValue(narrative?.missingContentStatus ?? record.missingContentStatus, "content_missing"),
    supportContext: mapSupportContext(record.supportContext, {
      lastProgressRunId: record.lastProgressRunId,
      updatedAt: record.updatedAt,
      permission: fallbackPermission,
      permissionMode: fallbackPermissionMode
    })
  };
}

export function mapStoryProgressListResponse(response: unknown): StoryProgressListResponse {
  const record = asRecord(response);
  const items = Array.isArray(record?.items)
    ? record.items.map((item) => mapStoryProgressItem(item, "연동 대기", record?.permission, record?.permissionMode))
    : [];

  return {
    permission: stringValue(record?.permission, "STORY_PROGRESS_READ"),
    permissionMode: stringValue(record?.permissionMode, "ROLE_PERMISSION"),
    items,
    nextCursor: typeof record?.nextCursor === "string" ? record.nextCursor : null
  };
}

export function mapStoryProgressDetailResponse(response: unknown, accountId: string): StoryProgressDetail {
  return response === null || response === undefined
    ? emptyStoryProgressDetail(accountId)
    : mapStoryProgressItem(response, accountId);
}

export const storyProgressContractPreview = {
  listItem,
  detail: {
    accountId: listItem.accountId,
    actId: listItem.actId,
    highestClearedOperationId: listItem.highestClearedOperationId,
    highestStoryProgressId: listItem.highestStoryProgressId,
    order: listItem.order,
    clearedOperationIds: listItem.clearedOperationIds,
    unlockedOperations: listItem.unlockedOperations,
    narrativeVersion: listItem.narrativeVersion,
    contentStatus: listItem.contentStatus,
    localizationStatus: listItem.localizationStatus,
    fixtureStatus: listItem.fixtureStatus,
    missingContentStatus: listItem.missingContentStatus,
    supportContext: listItem.supportContext
  },
  filters: Object.fromEntries(storyProgressReadContractFilterFields.map((field) => [field, ""])) as StoryProgressFiltersPreview
};
