import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { createAccessToken } from "../../apps/game-api/src/auth";

type TestEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT: string;
  REALTIME_COOP_ROOMS?: {
    getByName: (name: string) => {
      createRoom: (input: Record<string, unknown>) => Promise<Record<string, unknown>>;
      fetch: (request: Request) => Promise<Response>;
    };
  };
};

const authSecret = "test-secret-minimum-32-characters";

async function createAuthorizationHeader(accountId = "account_1", sessionId = "session_1"): Promise<string> {
  const token = await createAccessToken({
    secret: authSecret,
    accountId,
    sessionId,
    nowMs: Date.now(),
    ttlMs: 900000,
  });
  return `Bearer ${token}`;
}

function createEnv(options: {
  realtimeRooms?: TestEnv["REALTIME_COOP_ROOMS"];
} = {}): TestEnv {
  return {
    ENVIRONMENT: "DEV",
    ALLOWED_ADMIN_ORIGINS: "https://dev-admin.lastrail.app",
    AUTH_SIGNING_KEY_CURRENT: authSecret,
    REALTIME_COOP_ROOMS: options.realtimeRooms,
    DB: {
      prepare(sql: string) {
        return {
          bind(...values: unknown[]) {
            return {
              sql,
              values,
              first: async () => {
                if (sql.includes("FROM auth_sessions")) {
                  return {
                    account_id: values[1],
                    session_id: "session_1",
                    session_status: "ACTIVE",
                    session_expires_at: Date.now() + 900000,
                    account_status: "ACTIVE",
                    account_created_at: Date.now() - 1000,
                  };
                }
                return null;
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

describe("game-api realtime coop contract", () => {
  it("fails closed when the Durable Object room binding is not configured", async () => {
    const response = await app.request(
      "/v1/realtime/coop/rooms",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "coop-room-idem-1",
        },
        body: JSON.stringify({ runId: "run_1" }),
      },
      createEnv(),
    );

    expect(response.status).toBe(503);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      roomStatus: "REJECTED",
      errorCode: "realtime_coop_unavailable",
    });
  });

  it("creates a two-player server-authoritative room through the room namespace", async () => {
    const calls: Array<Record<string, unknown>> = [];
    const env = createEnv({
      realtimeRooms: {
        getByName(name) {
          expect(name).toBe("coop:run_1");
          return {
            createRoom: async (input) => {
              calls.push(input);
              return {
                accepted: true,
                roomStatus: "CREATED",
                realtimeMode: "COOP_2P",
                roomId: "coop_room_run_1",
                runId: "run_1",
                maxPlayers: 2,
                transport: {
                  kind: "WEBSOCKET",
                  connectPath: "/v1/realtime/coop/rooms/coop_room_run_1/connect?runId=run_1",
                },
                serverAuthority: {
                  combatResult: "SERVER_VALIDATED",
                  reward: "SERVER_SOURCE_OF_TRUTH",
                },
                errorCode: null,
                message: null,
              };
            },
            fetch: async () => new Response("unused", { status: 500 }),
          };
        },
      },
    });

    const response = await app.request(
      "/v1/realtime/coop/rooms",
      {
        method: "POST",
        headers: {
          Authorization: await createAuthorizationHeader(),
          "Idempotency-Key": "coop-room-idem-2",
          "X-Request-Id": "req_coop_room_1",
        },
        body: JSON.stringify({ runId: "run_1" }),
      },
      env,
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      accepted: true,
      roomStatus: "CREATED",
      realtimeMode: "COOP_2P",
      roomId: "coop_room_run_1",
      runId: "run_1",
      maxPlayers: 2,
      transport: {
        kind: "WEBSOCKET",
        connectPath: "/v1/realtime/coop/rooms/coop_room_run_1/connect?runId=run_1",
      },
      serverAuthority: {
        combatResult: "SERVER_VALIDATED",
        reward: "SERVER_SOURCE_OF_TRUTH",
      },
      errorCode: null,
      message: null,
    });
    expect(calls).toEqual([
      expect.objectContaining({
        runId: "run_1",
        hostAccountId: "account_1",
        idempotencyKey: "coop-room-idem-2",
        requestId: "req_coop_room_1",
      }),
    ]);
    expect(JSON.stringify(calls)).not.toContain("Authorization");
  });

  it("rejects client-supplied combat, reward, RNG, or animation source-of-truth fields", async () => {
    const forbiddenBodies = [
      { runId: "run_1", accountId: "account_other" },
      { runId: "run_1", combatResult: "VICTORY" },
      { runId: "run_1", rewardGrant: [{ id: "cur_credits", quantity: 999999 }] },
      { runId: "run_1", rawRng: "seed" },
      { runId: "run_1", animationTimeline: [{ frame: 1 }] },
    ];

    for (const body of forbiddenBodies) {
      const response = await app.request(
        "/v1/realtime/coop/rooms",
        {
          method: "POST",
          headers: {
            Authorization: await createAuthorizationHeader(),
            "Idempotency-Key": `coop-room-forbidden-${Object.keys(body).join("-")}`,
          },
          body: JSON.stringify(body),
        },
        createEnv({
          realtimeRooms: {
            getByName: () => {
              throw new Error("room namespace should not be reached");
            },
          },
        }),
      );

      expect(response.status).toBe(400);
      await expect(response.json()).resolves.toMatchObject({
        accepted: false,
        roomStatus: "REJECTED",
        errorCode: "contract_violation",
      });
    }
  });

  it("requires websocket upgrade for room transport connections", async () => {
    const response = await app.request(
      "/v1/realtime/coop/rooms/coop_room_run_1/connect?runId=run_1",
      {
        method: "GET",
        headers: {
          Authorization: await createAuthorizationHeader(),
        },
      },
      createEnv({
        realtimeRooms: {
          getByName: () => {
            throw new Error("room namespace should not be reached");
          },
        },
      }),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      accepted: false,
      connectionStatus: "REJECTED",
      errorCode: "realtime_websocket_required",
    });
  });
});
