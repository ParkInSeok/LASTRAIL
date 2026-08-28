import {
  mapStoryProgressDetailResponse,
  mapStoryProgressListResponse,
  type StoryProgressFiltersPreview
} from "../storyProgress/storyProgressContract";

import {
  AuditLogsResponseSchema,
  BootstrapResponseSchema,
  CreatePlayerNoteRequestSchema,
  CreatePlayerNoteResponseSchema,
  IntegrityEventsResponseSchema,
  PlayerDetailSchema,
  PlayerNotesResponseSchema,
  PlayerSearchResponseSchema,
  RunDetailSchema,
  RunSearchResponseSchema,
  SuspiciousPlayerDetailSchema,
  SuspiciousPlayersResponseSchema
} from "@lastrail/admin-contracts";

async function readJson(path: string): Promise<unknown> {
  const response = await fetch(path, {
    headers: {
      accept: "application/json"
    }
  });

  if (!response.ok) {
    throw new Error(`Admin request failed with ${response.status}`);
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
    throw new Error(`Admin request failed with ${response.status}`);
  }

  return response.json();
}

function storyProgressQuery(filters: Partial<StoryProgressFiltersPreview>): string {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value) params.set(key, value);
  });
  const query = params.toString();
  return query ? `?${query}` : "";
}

export const adminApi = {
  async bootstrap() {
    return BootstrapResponseSchema.parse(await readJson("/api/bootstrap"));
  },
  async searchPlayers(query: string) {
    const params = new URLSearchParams();
    if (query) params.set("q", query);
    return PlayerSearchResponseSchema.parse(
      await readJson(`/api/players?${params.toString()}`)
    );
  },
  async playerDetail(accountId: string) {
    return PlayerDetailSchema.parse(
      await readJson(`/api/players/${encodeURIComponent(accountId)}`)
    );
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
    return RunSearchResponseSchema.parse(await readJson(`/api/runs?${params.toString()}`));
  },
  async runDetail(runId: string) {
    return RunDetailSchema.parse(await readJson(`/api/runs/${encodeURIComponent(runId)}`));
  },
  async storyProgressList(filters: Partial<StoryProgressFiltersPreview>) {
    return mapStoryProgressListResponse(await readJson(`/api/story-progress${storyProgressQuery(filters)}`));
  },
  async storyProgressDetail(accountId: string) {
    return mapStoryProgressDetailResponse(
      await readJson(`/api/story-progress/${encodeURIComponent(accountId)}`),
      accountId
    );
  }
};
