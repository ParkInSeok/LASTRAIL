# Game API

Base domains:

- DEV: `https://dev-api.lastrail.app`
- PRD: `https://api.lastrail.app`

The server currently uses `dev` and `prd` environment axes only. PRD resources and deploys remain approval-gated.

Unversioned: `GET /status`.

Versioned routes use `/v1/*`.

## Client Config

`GET /v1/client-config` returns public Unity client configuration:

- `environment`
- `apiBaseUrl`
- `balanceConfigEndpointTemplate`
- `minimumClientVersion`
- `maintenance`

This response must not include secrets, tokens, Cloudflare credentials, Admin API URLs, or Access credentials. API domains are public client configuration, not a secret. If tamper resistance is needed later, add a signed config payload instead of encrypting a value that the Unity client must decrypt.

## Balance Config

Balance payloads remain on Game API: `GET /v1/balance-config/:version`.

The endpoint reads published versions from D1 `balance_config_versions` and returns:

- `version`
- `schemaVersion`
- `requiredContentVersion`
- `checksum`
- `publishedAt`
- `payload`

Only positive integer versions are accepted. Missing/unpublished versions return `BALANCE_CONFIG_NOT_PUBLISHED`. Invalid stored JSON returns `BALANCE_CONFIG_PAYLOAD_INVALID`.
