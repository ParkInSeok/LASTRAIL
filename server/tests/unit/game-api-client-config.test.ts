import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";

describe("game-api client config", () => {
  it("returns public Unity client configuration without admin endpoints", async () => {
    const response = await app.request("/v1/client-config", {}, {
      ENVIRONMENT: "DEV",
      ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    });

    expect(response.status).toBe(200);

    const body = await response.json();
    expect(body).toEqual({
      environment: "DEV",
      apiBaseUrl: "https://dev-api.lastrail.app",
      balanceConfigEndpointTemplate: "/v1/balance-config/:version",
      minimumClientVersion: "0.1.0",
      maintenance: { enabled: false },
    });
    expect(JSON.stringify(body)).not.toContain("admin");
  });
});
