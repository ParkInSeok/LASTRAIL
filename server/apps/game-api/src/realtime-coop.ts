export type RealtimeCoopRoomNamespace = {
  getByName: (name: string) => {
    createRoom: (input: RealtimeCoopRoomCreateInput) => Promise<RealtimeCoopRoomResponse>;
    fetch: (request: Request) => Promise<Response>;
  };
};

export type RealtimeCoopRoomCreateInput = {
  runId: string;
  hostAccountId: string;
  idempotencyKey: string;
  requestId: string;
};

export type RealtimeCoopRoomResponse = {
  accepted: boolean;
  roomStatus: "CREATED" | "JOINED" | "ALREADY_JOINED" | "REJECTED";
  realtimeMode: "COOP_2P";
  roomId: string | null;
  runId: string;
  maxPlayers: 2;
  transport: {
    kind: "WEBSOCKET";
    connectPath: string | null;
  };
  serverAuthority: {
    combatResult: "SERVER_VALIDATED";
    reward: "SERVER_SOURCE_OF_TRUTH";
  };
  errorCode: string | null;
  message: string | null;
};

type RoomRow = {
  id: string;
  run_id: string;
  status: string;
};

type ParticipantRow = {
  account_id: string;
};

const maxCoopPlayers = 2;

export function createRealtimeCoopRejected(errorCode: string, message: string): RealtimeCoopRoomResponse {
  return {
    accepted: false,
    roomStatus: "REJECTED",
    realtimeMode: "COOP_2P",
    roomId: null,
    runId: "",
    maxPlayers: maxCoopPlayers,
    transport: {
      kind: "WEBSOCKET",
      connectPath: null,
    },
    serverAuthority: {
      combatResult: "SERVER_VALIDATED",
      reward: "SERVER_SOURCE_OF_TRUTH",
    },
    errorCode,
    message,
  };
}

export function isSafeRealtimeId(value: string): boolean {
  return /^[A-Za-z0-9_-]{3,96}$/u.test(value);
}

export class RealtimeCoopRoom {
  constructor(
    private readonly ctx: DurableObjectState,
    private readonly env: Env,
  ) {
    void this.env;
    this.ctx.blockConcurrencyWhile(async () => {
      this.migrate();
    });
  }

  async createRoom(input: RealtimeCoopRoomCreateInput): Promise<RealtimeCoopRoomResponse> {
    if (!isSafeRealtimeId(input.runId) || !isSafeRealtimeId(input.hostAccountId)) {
      return createRealtimeCoopRejected("contract_violation", "Invalid realtime coop room identity.");
    }

    const now = Date.now();
    const roomId = `coop_room_${input.runId}`;
    const existingRoom = this.ctx.storage.sql
      .exec<RoomRow>("SELECT id, run_id, status FROM rooms WHERE id = ?", roomId)
      .toArray()[0];

    if (existingRoom === undefined) {
      this.ctx.storage.sql.exec(
        `INSERT INTO rooms (id, run_id, status, max_players, created_by_account_id, created_at, updated_at)
         VALUES (?, ?, 'WAITING', ?, ?, ?, ?)`,
        roomId,
        input.runId,
        maxCoopPlayers,
        input.hostAccountId,
        now,
        now,
      );
    }

    const currentParticipant = this.ctx.storage.sql
      .exec<ParticipantRow>("SELECT account_id FROM participants WHERE room_id = ? AND account_id = ?", roomId, input.hostAccountId)
      .toArray()[0];

    const participantCount = this.ctx.storage.sql
      .exec<{ count: number }>("SELECT COUNT(*) AS count FROM participants WHERE room_id = ?", roomId)
      .one().count;

    if (currentParticipant === undefined && participantCount >= maxCoopPlayers) {
      return {
        ...createRealtimeCoopRejected("realtime_coop_room_full", "Realtime coop room is full."),
        runId: input.runId,
      };
    }

    if (currentParticipant === undefined) {
      this.ctx.storage.sql.exec(
        `INSERT INTO participants (room_id, account_id, role, joined_at, last_seen_at)
         VALUES (?, ?, 'HOST', ?, ?)`,
        roomId,
        input.hostAccountId,
        now,
        now,
      );
    }

    return this.createAcceptedResponse({
      roomStatus: currentParticipant === undefined && existingRoom === undefined ? "CREATED" : "ALREADY_JOINED",
      roomId,
      runId: input.runId,
    });
  }

  async fetch(request: Request): Promise<Response> {
    if (request.headers.get("Upgrade")?.toLowerCase() !== "websocket") {
      return Response.json({ accepted: false, connectionStatus: "REJECTED", errorCode: "realtime_websocket_required", message: "WebSocket upgrade is required." }, { status: 400 });
    }

    const accountId = request.headers.get("X-Lastrail-Realtime-Account-Id");
    const runId = request.headers.get("X-Lastrail-Realtime-Run-Id");
    const roomId = request.headers.get("X-Lastrail-Realtime-Room-Id");
    if (accountId === null || runId === null || roomId === null) {
      return Response.json({ accepted: false, connectionStatus: "REJECTED", errorCode: "contract_violation", message: "Realtime coop connection context is required." }, { status: 400 });
    }

    const webSocketPair = new WebSocketPair();
    const client = webSocketPair[0];
    const server = webSocketPair[1];
    this.ctx.acceptWebSocket(server, [`room:${roomId}`, `account:${accountId}`]);
    server.serializeAttachment({
      accountId,
      runId,
      roomId,
      connectedAt: Date.now(),
    });

    return new Response(null, { status: 101, webSocket: client });
  }

  async webSocketMessage(ws: WebSocket, message: string | ArrayBuffer): Promise<void> {
    const parsed = this.parseClientMessage(message);
    if (parsed === null) {
      ws.send(JSON.stringify({
        type: "SERVER_REJECTED",
        errorCode: "contract_violation",
        message: "Realtime coop message is invalid.",
      }));
      return;
    }

    const attachment = ws.deserializeAttachment() as { accountId?: string; roomId?: string; runId?: string } | null;
    const serverMessage = {
      type: "SERVER_INPUT_ACCEPTED",
      roomId: attachment?.roomId ?? null,
      runId: attachment?.runId ?? null,
      accountId: attachment?.accountId ?? null,
      clientMessageId: parsed.clientMessageId,
      serverReceivedAt: new Date().toISOString(),
    };

    for (const peer of this.ctx.getWebSockets(attachment?.roomId === undefined ? undefined : `room:${attachment.roomId}`)) {
      peer.send(JSON.stringify(serverMessage));
    }
  }

  async webSocketClose(ws: WebSocket): Promise<void> {
    const attachment = ws.deserializeAttachment() as { accountId?: string; roomId?: string } | null;
    if (attachment?.accountId !== undefined && attachment.roomId !== undefined) {
      this.ctx.storage.sql.exec(
        "UPDATE participants SET last_seen_at = ? WHERE room_id = ? AND account_id = ?",
        Date.now(),
        attachment.roomId,
        attachment.accountId,
      );
    }
  }

  private migrate(): void {
    this.ctx.storage.sql.exec(`
      CREATE TABLE IF NOT EXISTS _sql_schema_migrations (
        id INTEGER PRIMARY KEY,
        applied_at TEXT NOT NULL DEFAULT (datetime('now'))
      );
    `);

    const currentVersion = this.ctx.storage.sql
      .exec<{ version: number }>("SELECT COALESCE(MAX(id), 0) AS version FROM _sql_schema_migrations")
      .one().version;

    if (currentVersion < 1) {
      this.ctx.storage.sql.exec(`
        CREATE TABLE IF NOT EXISTS rooms (
          id TEXT PRIMARY KEY,
          run_id TEXT NOT NULL,
          status TEXT NOT NULL CHECK (status IN ('WAITING','ACTIVE','COMPLETED','CLOSED')),
          max_players INTEGER NOT NULL CHECK (max_players = 2),
          created_by_account_id TEXT NOT NULL,
          created_at INTEGER NOT NULL,
          updated_at INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS participants (
          room_id TEXT NOT NULL,
          account_id TEXT NOT NULL,
          role TEXT NOT NULL CHECK (role IN ('HOST','GUEST')),
          joined_at INTEGER NOT NULL,
          last_seen_at INTEGER NOT NULL,
          PRIMARY KEY (room_id, account_id)
        );
        CREATE TABLE IF NOT EXISTS accepted_inputs (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          room_id TEXT NOT NULL,
          account_id TEXT NOT NULL,
          client_message_id TEXT NOT NULL,
          input_type TEXT NOT NULL,
          created_at INTEGER NOT NULL,
          UNIQUE(room_id, account_id, client_message_id)
        );
        INSERT INTO _sql_schema_migrations (id) VALUES (1);
      `);
    }
  }

  private createAcceptedResponse(options: {
    roomStatus: "CREATED" | "JOINED" | "ALREADY_JOINED";
    roomId: string;
    runId: string;
  }): RealtimeCoopRoomResponse {
    return {
      accepted: true,
      roomStatus: options.roomStatus,
      realtimeMode: "COOP_2P",
      roomId: options.roomId,
      runId: options.runId,
      maxPlayers: maxCoopPlayers,
      transport: {
        kind: "WEBSOCKET",
        connectPath: `/v1/realtime/coop/rooms/${options.roomId}/connect?runId=${options.runId}`,
      },
      serverAuthority: {
        combatResult: "SERVER_VALIDATED",
        reward: "SERVER_SOURCE_OF_TRUTH",
      },
      errorCode: null,
      message: null,
    };
  }

  private parseClientMessage(message: string | ArrayBuffer): { clientMessageId: string } | null {
    if (typeof message !== "string") return null;

    let parsed: unknown;
    try {
      parsed = JSON.parse(message);
    } catch {
      return null;
    }

    if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) return null;
    if (containsForbiddenRealtimeCoopField(parsed)) return null;

    const record = parsed as Record<string, unknown>;
    if (typeof record.clientMessageId !== "string" || !isSafeRealtimeId(record.clientMessageId)) return null;
    if (record.type !== "PLAYER_INPUT" && record.type !== "READY" && record.type !== "HEARTBEAT" && record.type !== "CLIENT_STATE_HINT") {
      return null;
    }

    return { clientMessageId: record.clientMessageId };
  }
}

const forbiddenRealtimeCoopFields = new Set([
  "accountId",
  "account_id",
  "combatResult",
  "finalResult",
  "outcome",
  "rewardGrant",
  "rewardLines",
  "currencyGrants",
  "itemGrants",
  "economyMutation",
  "rawRng",
  "rngSeed",
  "probability",
  "weighted",
  "animationTimeline",
  "vfxTimeline",
  "sfxTimeline",
  "cameraTimeline",
  "hitStopTimeline",
]);

export function containsForbiddenRealtimeCoopField(value: unknown): boolean {
  if (Array.isArray(value)) {
    return value.some((item) => containsForbiddenRealtimeCoopField(item));
  }
  if (typeof value !== "object" || value === null) return false;

  for (const [key, nestedValue] of Object.entries(value)) {
    if (forbiddenRealtimeCoopFields.has(key)) return true;
    if (containsForbiddenRealtimeCoopField(nestedValue)) return true;
  }
  return false;
}
