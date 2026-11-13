# Original B departure combat source pin

Contract `combat.b-departure-combat-pin.v1.20261003`; source `combat.b-departure-source.v1.20261003` (S-B-DEPARTURE-COMBAT-PIN-19).

## Creation and authority

`prepareBDepartureCombatSnapshot(accountId,runId,baseSnapshotJson,sourceOwner)` is an internal, immutable preparation boundary. It does not run SQL, create a run, spend resources, register a profile or authorize START. Original authentication/run/preview/operation/selected risk and admission come from the existing departure owner, not an extra request body. Original map/publication are validated with their existing owner kernels.

The injected `BDepartureCombatSourceOwner` must resolve actual original native context, supported runtime profile/definition/executor version, original unscaled actor HP or EHP and raw actions with explicit null applied-version flags. It must validate every resolved profile against actual executors through `supports`, and return CAS assertions for every inspected source/support version. Context ID cannot equal run ID; the builder never derives a context from run ID. All account/run/preview/operation/risk/operation-definition/plan/publication identities must match the original prepared departure. Unsupported/missing owners or empty source fences are rejected before pin publication.

There is **no production owner/registry installed**. Existing `OperationMapContentSupport` is itself a port without production implementation. A client capability/supported boolean or catalog presence never replaces executor support. Pure S17 approved risk definition/precision/raw schemas are reused, not reimplemented. No real raw HP/DEF/spawn or Scout targeting/coefficient is invented.

The independent immutable pin contains:

- origin: authenticated account/run/preview/operation/selectedRisk/departureVersion, canonical complete baseSnapshotHash, original definitionVersion/planHash/publicationHash;
- source: independently versioned native context ID and exact original runtime profile ID/version/executorVersion/definition, original actor raw HP/EHP and raw action arrays, existing approved riskApplicationVersion and null original risk/progress applied flags;
- sourceHash: canonical full original source hash; runtimeProfileHash: canonical exact profile hash (independent from run/account binding); original approved full riskDefinition and complete pinHash.

A source is captured and copied before support/hashing awaits; support receives isolated copies. Original response and private snapshot contain identical pins. The existing `startDeparture` accepts an optional eighth **server composition** argument, builds the pin after its original snapshot builder, inserts the final snapshot/map copy in its existing atomic batch, and appends source/support assertions in that same batch. Late failure rolls back all original writes. Public fresh `/v1/runs/start` remains combat_not_ready; the original index and S18 source contracts are unchanged. A supplied pin without a trusted owner is rejected. Existing successful replay runs first, validates the original stored pin and returns its original response without contacting today's source/support owner.

## Storage and restore

Migration `0099_b_departure_combat_pin.sql` protects only snapshots with non-null B pins: UPDATE, DELETE, INSERT OR REPLACE and pinned run account/preview/balance identity changes are rejected. Original legacy/null rows are not upgraded or rewritten. Existing status transitions remain available. All migrations were verified in isolated LOCAL; migration099 has not been applied to DEV/production by this task.

`readBDepartureCombatPin(snapshotJson, trustedOwner)` validates the stored original complete base, repeated response pin, original map/publication, all bindings and hashes, exact profile/source and immutable original risk definition. Trusted account/run are required. An optional expectedPinHash is for independently anchored callers; untrusted consumer bytes require an independent expected hash and original authenticated START anchor, never the packet's own hash as authority. This server function accepts trusted original SQL bytes; it is not a public upload or authorization API. Legacy missing/null pins remain null. Restore does not consult current settings or rerun support; changes to current source/support cannot replace an original pin.

## Read-only API

`GET /v1/runs/{runId}/b-combat-source`

Required: existing signed game Bearer/active session/account; exact `X-Combat-B-Departure-Pin-Version: combat.b-departure-combat-pin.v1.20261003`. Query/body forbidden. Optional `If-Match` is the exact bare lowercase packetHash. Response `Cache-Control:no-store`, bare ETag=packetHash. Single owned SQL join plus async source fences and final authentication/source check reject mixed-time or terminal changes.

Response: version/accountId/runId/runStatus/exact startSnapshotHash/status (`PINNED_SOURCE` or `HISTORICAL_UNPINNED`)/pin-or-null/packetHash; `canStart:false`, `newProfileAdmission:false`. `sourcePinned` means stored source presence only, not combat authorization. No private reward/map definition payload is exposed. Terminal original pins remain readable; consumers cannot use that as fresh execution authority. S18 v1 still retains its own null/metadata semantics rather than inferring this new B contract.

Existing auth errors401/403/500 are reused. Bad query/body is400. `combat_b_departure_*` source_unavailable/profile_unsupported/source_invalid/source_fence_missing/source_conflict/snapshot_invalid/response_conflict/already_pinned/pin_invalid/pin_conflict/run_not_found/account_inactive/snapshot_missing/version_conflict/stale are409; original invalid map/content errors retain original409 codes. Unknown storage/owner errors are not returned as success. Stale is a reread of metadata, not a retried START or source rewrite.

## Verification and current dependency

QA/CommanderS1-20260922/combat-b-departure-combat-pin-validation-20261003.json: 56 PASS (new10 + related46), type/lint exit0, signed TCP11, source/support late rollback, exact retry/replay/export/restore/source-setting changes, original map/snapshot byte agreement, DB immutability and legacy/null preservation. One read-only independent reviewer identified an actual-builder coverage gap (not a product defect); the missing private verification fixture was observed RED then the actual original function/batch was exercised GREEN, without a second review.

The LOCAL fixtures explicitly use synthetic transaction-only profile/value identifiers. They do not register approved production profiles. Actual original builder success exercises **toy LOCAL fuel ledger writes**; rollback cases leave no committed toy economic rows. Operating START/economic writes/deployments remain zero. The recipe below restricts ephemeral loopback to the original signed readonly API plus a test that confirms the original public fresh START stays blocked; it owns and closes its listener/SQL and does not persist credentials.

From canonical server: `node node_modules/tsx/dist/cli.mjs --tsconfig tsconfig.base.json D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-departure-combat-pin-traces-20261003.mts`. Sanitized exact responses are the matching .json. No DEV/public service URL or deployment is established.

Independent supplier contract/builder/store/reader boundary is complete. Actual B runtime source remains unavailable: original native context producer, approved executor/profile/definition, unscaledHP/EHP/rawACTION and applied-flag owners, and original new-departure risk-version binding require an actual supported source owner. Recommended next boundary: approve one precise B operation/enemy execution definition and its immutable raw-source lineage; implement the real native-context/source/support CAS owner; then wire it through this existing pin boundary and verify before releasing combat consumption. Scout same-car targeting/ATK24→raw60 remain separate unapproved decisions, not defaults. Current risk-table approval needs no reapproval. Legacy snapshots and approved art remain unchanged.

No Unity full-combat queue is released solely from a synthetic pin fixture. Pending consumer AND is S19 immutable boundary + actual approved supported source owner/profile + U18 verified consumer. Screen connection remains separately gated by approved art.

Implementation consulted current [D1 batch transaction documentation](https://developers.cloudflare.com/d1/worker-api/d1-database/#batch) and [Workers best practices](https://developers.cloudflare.com/workers/best-practices/workers-best-practices/). It reuses prepared bound statements and generated project Env; no new Workers config, global mutable registry, remote API or secret provisioning was added.