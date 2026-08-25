# Game API

Base domains:

- DEV: `https://dev-api.lastrail.app`
- STAGING: `https://staging-api.lastrail.app`
- PRODUCTION: `https://api.lastrail.app`

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
