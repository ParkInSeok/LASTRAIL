import { ticketList, ticketDetail } from "../support/tickets";
import { mapDeployment, type DeploymentService } from "../release/serverDeployment";
import {
  mapDashboardLaunchSummaryResponse
} from "../dashboard/launchSummaryContract";

import {
  mapGachaBannerListResponse,
  mapGachaPityResponse,
  mapGachaPullBatchDetailResponse,
  mapGachaPullBatchListResponse
} from "../gacha/gachaContract";

import {
  mapOfflineRewardDetailResponse,
  mapOfflineRewardListResponse,
  type OfflineRewardReadFiltersPreview
} from "../offlineRewards/offlineRewardContract";

import { mapNarrativeCoverageResponse } from "../narrative/narrativeCoverageContract";

import {
  mapOpsErrorLogDetailResponse,
  mapOpsErrorLogListResponse,
  mapOpsErrorLogSummaryResponse,
  type OpsErrorLogReadFiltersPreview
} from "../opsErrorLogs/opsErrorLogContract";

import { mapPlayerLaunchEvidence } from "../players/playerLaunchEvidenceContract";
import { mapPlayerDetailResponse } from "../players/playerDetailContract";
import { mapRunDetailResponse } from "../runs/runDetailContract";
import { mapRunVersions } from "../runs/runVersions";
import { mapRunLaunchEvidence } from "../runs/runLaunchEvidenceContract";
import { mapCurrentVersion, mapVersionExportResult, type VersionExportRequest } from "../release/releaseManagementContract";

import {
  mapStoryProgressDetailResponse,
  mapStoryProgressListResponse,
  type StoryProgressFiltersPreview
} from "../storyProgress/storyProgressContract";

import { mapSupportAccountSummaryResponse } from "../support/supportContract";

import {
  AuditLogsResponseSchema,
  BootstrapResponseSchema,
  CreatePlayerNoteRequestSchema,
  CreatePlayerNoteResponseSchema,
  IntegrityEventsResponseSchema,
  PlayerNotesResponseSchema,
  PlayerSearchResponseSchema,
  RunSearchResponseSchema,
  SuspiciousPlayerDetailSchema,
  SuspiciousPlayersResponseSchema
} from "@lastrail/admin-contracts";

type AdminApiErrorOptions = {
  status: number;
  path: string;
  errorCode?: string;
  upstreamMessage?: string;
};

export class AdminApiError extends Error {
  readonly name = "AdminApiError";
  readonly status: number;
  readonly path: string;
  readonly errorCode?: string;
  readonly upstreamMessage?: string;

  constructor(options: AdminApiErrorOptions) {
    const suffix = options.errorCode ? ` (${options.errorCode})` : "";
    super(`Admin request failed with ${options.status}${suffix}`);
    this.status = options.status;
    this.path = options.path;
    this.errorCode = options.errorCode;
    this.upstreamMessage = options.upstreamMessage;
  }
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function errorDetailsFrom(payload: unknown): Pick<AdminApiErrorOptions, "errorCode" | "upstreamMessage"> {
  const root = record(payload);
  const nested = record(root?.error);

  if (nested) {
    return {
      errorCode: typeof nested.code === "string" ? nested.code : undefined,
      upstreamMessage: typeof nested.message === "string" ? nested.message : undefined
    };
  }

  return {
    errorCode: typeof root?.error === "string" ? root.error : typeof root?.errorCode === "string" ? root.errorCode : undefined,
    upstreamMessage: typeof root?.message === "string" ? root.message : undefined
  };
}

async function adminApiErrorFrom(response: Response, path: string): Promise<AdminApiError> {
  try {
    const details = errorDetailsFrom(await response.clone().json());
    return new AdminApiError({ status: response.status, path, ...details });
  } catch {
    return new AdminApiError({ status: response.status, path });
  }
}

async function readJson(path: string): Promise<unknown> {
  const response = await fetch(path, {
    headers: {
      accept: "application/json"
    }
  });

  if (!response.ok) {
    throw await adminApiErrorFrom(response, path);
  }

  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.toLowerCase().includes("application/json")) {
    throw new AdminApiError({
      status: response.status,
      path,
      errorCode: "ADMIN_API_NON_JSON_RESPONSE",
      upstreamMessage: "Admin API returned a non-JSON response."
    });
  }

  return response.json();
}

async function writeJson(path: string, body: unknown): Promise<unknown> {
  const response = await fetch(path, {
    method: "POST",
    headers: {
      accept: "application/json",
      "content-type": "application/json"
    },
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    throw await adminApiErrorFrom(response, path);
  }

  return response.json();
}

function storyProgressQuery(filters: Partial<StoryProgressFiltersPreview>): string {
  return queryString(filters);
}

function offlineRewardQuery(filters: Partial<OfflineRewardReadFiltersPreview>): string {
  return queryString(filters);
}

function opsErrorLogQuery(filters: Partial<OpsErrorLogReadFiltersPreview>): string {
  return queryString(filters);
}

function gachaQuery(filters: Record<string, string | undefined>): string {
  return queryString(filters);
}

function queryString(filters: Record<string, string | undefined>): string {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value) params.set(key, value);
  });
  const query = params.toString();
  return query ? `?${query}` : "";
}

export const adminApi = {
  async serverDeploymentCurrent(service: DeploymentService) {
    return mapDeployment(await readJson(`/api/server-deployments/current?service=${encodeURIComponent(service)}`), service, "current");
  },
  async serverDeploymentHistory(service: DeploymentService, version: string) {
    return mapDeployment(await readJson(`/api/server-deployments/history/${encodeURIComponent(version)}?service=${encodeURIComponent(service)}`), service, "history", version);
  },
  async bootstrap() {
    return BootstrapResponseSchema.parse(await readJson("/api/bootstrap"));
  },
  async exportVersionToR2(request: VersionExportRequest) {
    return mapVersionExportResult(await writeJson("/api/versions/export", request), request);
  },
  async currentVersion() {
    return mapCurrentVersion(await readJson("/api/versions"));
  },
  async dashboardLaunchSummary() {
    return mapDashboardLaunchSummaryResponse(await readJson("/api/dashboard/launch-summary"));
  },
  async searchPlayers(query: string) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    return PlayerSearchResponseSchema.parse(
      await readJson(`/api/players?${params.toString()}`)
    );
  },
  async playerDetail(accountId: string) {
    const raw = await readJson(`/api/players/${encodeURIComponent(accountId)}`);
    return mapPlayerDetailResponse(raw, accountId, mapPlayerLaunchEvidence(raw));
  },
  async createPlayerNote(accountId: string, note: string) {
    const body = CreatePlayerNoteRequestSchema.parse({ note });
    return CreatePlayerNoteResponseSchema.parse(
      await writeJson(`/api/players/${encodeURIComponent(accountId)}/notes`, body)
    );
  },
  async playerNotes(accountId: string) {
    return PlayerNotesResponseSchema.parse(
      await readJson(`/api/players/${encodeURIComponent(accountId)}/notes`)
    );
  },
  async auditLogs(filters: { action?: string; resourceId?: string; result?: "SUCCESS" | "FAILURE" }) {
    const params = new URLSearchParams();
    if (filters.action) params.set("action", filters.action);
    if (filters.resourceId) params.set("resourceId", filters.resourceId);
    if (filters.result) params.set("result", filters.result);
    const query = params.toString();
    return AuditLogsResponseSchema.parse(
      await readJson(`/api/audit${query ? `?${query}` : ""}`)
    );
  },
  async integrityEvents(filters: {
    accountId?: string;
    classification?: "SOFT_ANOMALY" | "HARD_VIOLATION";
    reviewState?: "OPEN" | "IN_REVIEW" | "RESOLVED";
  }) {
    const params = new URLSearchParams();
    if (filters.accountId) params.set("accountId", filters.accountId);
    if (filters.classification) params.set("classification", filters.classification);
    if (filters.reviewState) params.set("reviewState", filters.reviewState);
    const query = params.toString();
    return IntegrityEventsResponseSchema.parse(
      await readJson(`/api/integrity/events${query ? `?${query}` : ""}`)
    );
  },
  async suspiciousPlayers(filters: {
    reviewStatus?: "NEW" | "REVIEWING" | "CLEARED" | "SCORE_EXCLUDED" | "REWARD_HELD" | "ESCALATED" | "ACTIONED" | "FALSE_POSITIVE";
  }) {
    const params = new URLSearchParams();
    if (filters.reviewStatus) params.set("reviewStatus", filters.reviewStatus);
    const query = params.toString();
    return SuspiciousPlayersResponseSchema.parse(
      await readJson(`/api/integrity/suspicious-players${query ? `?${query}` : ""}`)
    );
  },
  async suspiciousPlayerDetail(accountId: string) {
    return SuspiciousPlayerDetailSchema.parse(
      await readJson(`/api/integrity/suspicious-players/${encodeURIComponent(accountId)}`)
    );
  },
  async searchRuns(accountId?: string) {
    const params = new URLSearchParams();
    if (accountId) params.set("accountId", accountId);
    const raw = await readJson(`/api/runs?${params.toString()}`);
    const parsed = RunSearchResponseSchema.parse(raw);
    const rows = (raw as { items: unknown[] }).items;
    return { ...parsed, items: parsed.items.map((item, index) => ({ ...item, versionEvidence: mapRunVersions(rows[index]) })) };
  },
  async runDetail(runId: string) {
    const raw = await readJson(`/api/runs/${encodeURIComponent(runId)}`);
    return mapRunDetailResponse(raw, runId, mapRunLaunchEvidence(raw));
  },
  async storyProgressList(filters: Partial<StoryProgressFiltersPreview>) {
    return mapStoryProgressListResponse(await readJson(`/api/story-progress${storyProgressQuery(filters)}`));
  },
  async storyProgressDetail(accountId: string) {
    return mapStoryProgressDetailResponse(
      await readJson(`/api/story-progress/${encodeURIComponent(accountId)}`),
      accountId
    );
  },
  async narrativeCoverage(actId: string) {
    return mapNarrativeCoverageResponse(await readJson(`/api/narrative/coverage${queryString({ actId })}`));
  },
  async offlineRewardList(filters: Partial<OfflineRewardReadFiltersPreview>) {
    return mapOfflineRewardListResponse(await readJson(`/api/offline-rewards${offlineRewardQuery(filters)}`));
  },
  async offlineRewardDetail(accountId: string) {
    return mapOfflineRewardDetailResponse(
      await readJson(`/api/offline-rewards/${encodeURIComponent(accountId)}`),
      accountId
    );
  },
  async supportAccountSummary(accountId: string) {
    return mapSupportAccountSummaryResponse(
      await readJson(`/api/support/accounts/${encodeURIComponent(accountId)}`),
      accountId
    );
  },
  async supportTickets(filters: Record<string, string>) {
    return ticketList(await readJson(`/api/support/tickets${queryString(filters)}`));
  },
  async supportTicket(ticketId: string) {
    return ticketDetail(await readJson(`/api/support/tickets/${encodeURIComponent(ticketId)}`));
  },
  async gachaBanners(filters: { status?: string } = {}) {
    return mapGachaBannerListResponse(await readJson(`/api/gacha/banners${gachaQuery(filters)}`));
  },
  async gachaAccountPity(accountId: string, bannerId?: string) {
    return mapGachaPityResponse(
      await readJson(`/api/gacha/accounts/${encodeURIComponent(accountId)}/pity${gachaQuery({ bannerId })}`)
    );
  },
  async gachaPullBatches(filters: { accountId?: string; bannerId?: string; limit?: string; cursor?: string }) {
    return mapGachaPullBatchListResponse(await readJson(`/api/gacha/pull-batches${gachaQuery(filters)}`));
  },
  async gachaPullBatchDetail(batchId: string) {
    return mapGachaPullBatchDetailResponse(await readJson(`/api/gacha/pull-batches/${encodeURIComponent(batchId)}`));
  },
  async opsErrorLogs(filters: Partial<OpsErrorLogReadFiltersPreview>) {
    return mapOpsErrorLogListResponse(await readJson(`/api/ops/error-logs${opsErrorLogQuery(filters)}`));
  },
  async opsErrorLogSummary() {
    return mapOpsErrorLogSummaryResponse(await readJson("/api/ops/error-logs/summary"));
  },
  async opsErrorLogDetail(errorLogId: string) {
    return mapOpsErrorLogDetailResponse(
      await readJson(`/api/ops/error-logs/${encodeURIComponent(errorLogId)}`),
      errorLogId
    );
  }
};
