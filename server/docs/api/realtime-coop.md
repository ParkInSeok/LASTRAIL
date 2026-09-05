# Realtime Coop Combat Contract

Status: first server skeleton, local contract only.

## Launch Decision

The two-player mode is real-time cooperative combat. The server owns validation
and result judgment. Unity owns presentation, input capture, animation, VFX,
SFX, camera, and local interpolation.

Recommended Cloudflare shape:

- one Durable Object per coop combat room/run
- hibernatable WebSocket transport
- room state and accepted input references stored in the Durable Object's
  SQLite storage
- Game API resolves Bearer auth before routing to the room object

Cloudflare's current Durable Objects WebSocket guidance recommends the
hibernation API for WebSocket server use because the object can sleep while
connections stay attached. The room object therefore uses
`DurableObjectState.acceptWebSocket(...)` rather than keeping an always-hot
in-memory server.

## Current Endpoints

```text
POST /v1/realtime/coop/rooms
GET /v1/realtime/coop/rooms/:roomId/connect?runId=<runId>
```

`POST /v1/realtime/coop/rooms` requires:

- `Authorization: Bearer <access-token>`
- `Idempotency-Key`
- body `{ "runId": "run_..." }`

Response:

```json
{
  "accepted": true,
  "roomStatus": "CREATED",
  "realtimeMode": "COOP_2P",
  "roomId": "coop_room_run_...",
  "runId": "run_...",
  "maxPlayers": 2,
  "transport": {
    "kind": "WEBSOCKET",
    "connectPath": "/v1/realtime/coop/rooms/coop_room_run_.../connect?runId=run_..."
  },
  "serverAuthority": {
    "combatResult": "SERVER_VALIDATED",
    "reward": "SERVER_SOURCE_OF_TRUTH"
  },
  "errorCode": null,
  "message": null
}
```

If `REALTIME_COOP_ROOMS` is not configured, the route fails closed:

```json
{
  "accepted": false,
  "roomStatus": "REJECTED",
  "realtimeMode": "COOP_2P",
  "roomId": null,
  "runId": "",
  "maxPlayers": 2,
  "transport": {
    "kind": "WEBSOCKET",
    "connectPath": null
  },
  "serverAuthority": {
    "combatResult": "SERVER_VALIDATED",
    "reward": "SERVER_SOURCE_OF_TRUTH"
  },
  "errorCode": "realtime_coop_unavailable",
  "message": "Realtime coop room service is not configured."
}
```

`GET /v1/realtime/coop/rooms/:roomId/connect` requires a WebSocket upgrade and
Bearer auth. The Game API strips the raw `Authorization` header before
forwarding the request to the Durable Object and passes only server-resolved
internal connection context headers.

## Client Message Envelope

Current accepted client message types:

- `READY`
- `PLAYER_INPUT`
- `HEARTBEAT`
- `CLIENT_STATE_HINT`

Minimum client message:

```json
{
  "type": "PLAYER_INPUT",
  "clientMessageId": "input_001",
  "payload": {
    "inputRef": "move_slot_a"
  }
}
```

The first skeleton acknowledges accepted input as
`SERVER_INPUT_ACCEPTED`. It does not yet implement the full combat simulation,
matchmaking, reconnect resume model, or final result commit.

## Forbidden Client Fields

Realtime client payloads must not contain:

- `accountId` / `account_id`
- final combat result or outcome
- reward lines, currency grants, item grants, or economy mutation fields
- raw RNG, seed, probability, or weight internals
- animation/VFX/SFX/camera/hit-stop timeline payloads

Those remain server-owned or Unity-local presentation data and are not part of
the realtime server authority contract.

## Current Boundaries

Implemented in this slice:

- Game API room create route
- Game API WebSocket connect route
- `RealtimeCoopRoom` Durable Object skeleton
- Durable Object SQLite schema for rooms, participants, and accepted input refs
- fail-closed behavior when the room binding is unavailable
- public response shape with `maxPlayers = 2`

Not implemented yet:

- matchmaking queue
- invite code lifecycle
- second-player join route
- reconnect token/resume protocol
- authoritative combat simulation
- final coop result commit into run finish/reward/story/offline systems
- Admin read model
- remote deploy or production Durable Object migration

## Error Codes

- `auth_required`
- `access_token_invalid`
- `auth_session_invalid`
- `account_not_active`
- `idempotency_key_required`
- `contract_violation`
- `realtime_coop_unavailable`
- `realtime_websocket_required`
- `realtime_coop_room_full`

## Follow-Up Decision Queue

- Whether coop rooms are created from an existing solo `runId` or a dedicated
  coop run-start route.
- Whether joining uses invite codes, friend invites, or match queue.
- Reconnect TTL and stale input replay rules.
- Server simulation tick rate and accepted input batching interval.
- How coop final results map to existing `POST /v1/runs/:runId/finish`.
- Whether Act 1 launch includes coop or keeps it as post-launch while single
  player remains the first release path.
