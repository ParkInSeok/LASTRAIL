import {
  BootstrapResponseSchema,
  PlayerDetailSchema,
  PlayerSearchResponseSchema,
  RunDetailSchema,
  RunSearchResponseSchema
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
  async searchRuns(accountId?: string) {
    const params = new URLSearchParams();
    if (accountId) params.set("accountId", accountId);
    return RunSearchResponseSchema.parse(await readJson(`/api/runs?${params.toString()}`));
  },
  async runDetail(runId: string) {
    return RunDetailSchema.parse(await readJson(`/api/runs/${encodeURIComponent(runId)}`));
  }
};

