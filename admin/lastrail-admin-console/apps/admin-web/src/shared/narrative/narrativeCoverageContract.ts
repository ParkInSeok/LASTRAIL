import { adminRequestFailureDisplay } from "../api/errorDisplay";
import { storyProgressLabel, storyProgressStatusLabel } from "../labels/adminLabels";

export type NarrativeCoverageItem = {
  missionId: string;
  displayName: string;
  contentStatus: string;
  localizationStatus: string;
  fixtureStatus: string;
  missingContentStatus: string;
  narrativeVersion: string;
  updatedAt: string;
  source: string;
};

export type NarrativeCoverageSummary = {
  contentMissingCount: string;
  localizationPendingCount: string;
  fixturePendingCount: string;
  versionMismatchCount: string;
  latestNarrativeVersion: string;
};

export type NarrativeCoverageResponse = {
  permission: string;
  permissionMode: string;
  items: NarrativeCoverageItem[];
  summary: NarrativeCoverageSummary;
};

export type NarrativeCoverageRow = {
  missionId: string;
  displayName: string;
  coverageState: string;
  localizationState: string;
  fixtureState: string;
  narrativeVersionState: string;
  statusHint: string;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function readString(value: unknown, emptyLabel = "없음"): string {
  return typeof value === "string" && value.length > 0 ? value : emptyLabel;
}

function numberString(value: unknown): string {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : "0";
}

function statusWithCode(status: string): string {
  const label = storyProgressStatusLabel(status);
  return label === status ? status : `${label} / ${status}`;
}

function mapNarrativeCoverageItem(value: unknown): NarrativeCoverageItem | null {
  const record = asRecord(value);
  if (!record) return null;
  const missionId = readString(record.missionId);

  return {
    missionId,
    displayName: readString(record.displayName, storyProgressLabel(missionId)),
    contentStatus: readString(record.contentStatus, "content_missing"),
    localizationStatus: readString(record.localizationStatus, "localization_pending"),
    fixtureStatus: readString(record.fixtureStatus, "fixture_pending"),
    missingContentStatus: readString(record.missingContentStatus, "content_missing"),
    narrativeVersion: readString(record.narrativeVersion),
    updatedAt: readString(record.updatedAt),
    source: readString(record.source)
  };
}

function mapSummary(value: unknown): NarrativeCoverageSummary {
  const record = asRecord(value);

  return {
    contentMissingCount: numberString(record?.contentMissingCount),
    localizationPendingCount: numberString(record?.localizationPendingCount),
    fixturePendingCount: numberString(record?.fixturePendingCount),
    versionMismatchCount: numberString(record?.versionMismatchCount),
    latestNarrativeVersion: readString(record?.latestNarrativeVersion)
  };
}

export function mapNarrativeCoverageResponse(response: unknown): NarrativeCoverageResponse {
  const record = asRecord(response) ?? {};
  const items = Array.isArray(record.items)
    ? record.items.map(mapNarrativeCoverageItem).filter((item): item is NarrativeCoverageItem => Boolean(item))
    : [];

  return {
    permission: readString(record.permission, "STORY_PROGRESS_READ"),
    permissionMode: readString(record.permissionMode, "ROLE_PERMISSION"),
    items,
    summary: mapSummary(record.summary)
  };
}

function itemToRow(item: NarrativeCoverageItem): NarrativeCoverageRow {
  return {
    missionId: item.missionId,
    displayName: item.displayName,
    coverageState: statusWithCode(item.contentStatus),
    localizationState: statusWithCode(item.localizationStatus),
    fixtureState: statusWithCode(item.fixtureStatus),
    narrativeVersionState: item.narrativeVersion,
    statusHint: `${statusWithCode(item.missingContentStatus)} / ${item.updatedAt} / ${item.source}`
  };
}

export function narrativeCoverageRowsForReadState(state: {
  data?: NarrativeCoverageResponse;
  isLoading: boolean;
  error: unknown;
}): NarrativeCoverageRow[] {
  if (state.data) {
    if (state.data.items.length === 0) {
      return [
        {
          missionId: "데이터 없음",
          displayName: "데이터 없음",
          coverageState: "데이터 없음",
          localizationState: "데이터 없음",
          fixtureState: "데이터 없음",
          narrativeVersionState: state.data.summary.latestNarrativeVersion,
          statusHint: "unknown act 또는 coverage row 없음"
        }
      ];
    }
    return state.data.items.map(itemToRow);
  }

  if (state.isLoading) {
    return [
      {
        missionId: "조회 중",
        displayName: "조회 중",
        coverageState: "조회 중",
        localizationState: "조회 중",
        fixtureState: "조회 중",
        narrativeVersionState: "조회 중",
        statusHint: "narrative coverage read model 조회 중"
      }
    ];
  }

  if (state.error) {
    const failure = adminRequestFailureDisplay(state.error, {
      resourceLabel: "내러티브 coverage",
      resourceId: "/api/narrative/coverage"
    });
    return [
      {
        missionId: "확인 필요",
        displayName: failure.title,
        coverageState: failure.reason,
        localizationState: failure.reason,
        fixtureState: failure.reason,
        narrativeVersionState: "확인 필요",
        statusHint: failure.nextStep
      }
    ];
  }

  return [];
}
