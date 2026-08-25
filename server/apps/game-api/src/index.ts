import { Hono } from "hono";
import { createRequestId } from "@lastrail/common";

const app = new Hono<{ Bindings: Env }>();

const apiBaseUrls = {
  LOCAL: "http://localhost:8787",
  DEV: "https://dev-api.lastrail.app",
  STAGING: "https://staging-api.lastrail.app",
  PRODUCTION: "https://api.lastrail.app",
} as const;

type BalanceConfigRow = {
  version: number;
  schema_version: number;
  required_content_version: number;
  checksum: string;
  config_json: string;
  published_at: number;
};

function parsePositiveInteger(value: string): number | null {
  if (!/^[1-9]\d*$/u.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

app.get("/status", (c) => c.json({ ok: true, service: "game-api", requestId: createRequestId() }));
app.get("/v1/client-config", (c) =>
  c.json({
    environment: c.env.ENVIRONMENT,
    apiBaseUrl: apiBaseUrls[c.env.ENVIRONMENT],
    balanceConfigEndpointTemplate: "/v1/balance-config/:version",
    minimumClientVersion: "0.1.0",
    maintenance: { enabled: false },
  }),
);
app.get("/v1/balance-config/:version", async (c) => {
  const rawVersion = c.req.param("version");
  const version = parsePositiveInteger(rawVersion);
  if (version === null) {
    return c.json({ error: "INVALID_BALANCE_CONFIG_VERSION", version: rawVersion }, 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT version, schema_version, required_content_version, checksum, config_json, published_at
     FROM balance_config_versions
     WHERE version = ?`,
  )
    .bind(version)
    .first<BalanceConfigRow>();

  if (row === null) {
    return c.json({ error: "BALANCE_CONFIG_NOT_PUBLISHED", version }, 404);
  }

  let payload: unknown;
  try {
    payload = JSON.parse(row.config_json) as unknown;
  } catch {
    return c.json({ error: "BALANCE_CONFIG_PAYLOAD_INVALID", version }, 500);
  }

  return c.json({
    version: row.version,
    schemaVersion: row.schema_version,
    requiredContentVersion: row.required_content_version,
    checksum: row.checksum,
    publishedAt: row.published_at,
    payload,
  });
});

export default app;


