import { Hono } from "hono";
import { createRequestId } from "@lastrail/common";

const app = new Hono<{ Bindings: Env }>();

const apiBaseUrls = {
  LOCAL: "http://localhost:8787",
  DEV: "https://dev-api.lastrail.app",
  STAGING: "https://staging-api.lastrail.app",
  PRODUCTION: "https://api.lastrail.app",
} as const;

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
app.get("/v1/balance-config/:version", (c) => c.json({ error: "BALANCE_CONFIG_NOT_PUBLISHED", version: c.req.param("version") }, 404));

export default app;


