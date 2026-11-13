# B wave manifest v1 — S-B-WAVE-MANIFEST-21

Contract `combat.b-wave-manifest.v1.20261003`. Pressure definition `combat.b-wave-pressure.v3.20261003`. Approval: `44_COMBAT_DETAIL_CONTRACTS.md#combat-b-wave-pressure-approved-v03-20261003`; battlefield correction: `#combat-b-wave-source-review-20261003`.

This implements immutable template/instance storage and authenticated original-source reads. No named production B enemy/source owner is registered. Every packet says `canStart:false`, `newProfileAdmission:false`, `fullCombatSupported:false`. These flags remain false even for a valid synthetic manifest. This is not a combat scheduler or an approved random pool generator. Pool weights, EHP/DEF, Gunner damage/ballistics, target selection and actual operation/story mapping in v04 are not published.

## Ownership and explicit composition

`createBWaveDepartureSourceOwner(parent, waveOwner, originalPlan)` returns a `BDepartureCombatSourceOwner` for the existing original `startDeparture` eighth parameter. The actual internal source owner must explicitly supply `profile.definition.waveManifestVersion` equal to the new contract. The adapter rejects existing `bWaveManifest`, missing opt-in, missing wave owner or missing parent/wave CAS statements. It compares the captured immutable original plan hash to the original departure context.

The new `BWaveManifestSourceOwner.prepare(source, context)` supplies an original manifest proposal plus D1 assertions. `supports(pin, source, context)` must inspect ALL original template/behavior versions, instance refs and the actual executor. The existing parent support check also runs; a client supported flag or fixture is not a replacement. All inspected source/template/pool/executor/support revisions must be fenced in the same original departure batch. Production composition is absent; no default implementation or fake profile is installed.

The prepared manifest is nested in `source.profile.definition.bWaveManifest`, with its own explicit version and hash. S19 then validates and pins the complete source in its original immutable snapshot. Parent `rawProfiles`, unique `actorRef` meaning and cap128 remain unchanged; template HP/actions belong to a separate schema and are never stuffed into old actorRef entries. The original parent/source/profile hashes cover the extension. The extension separately binds the original departure account/run/preview/operation/risk/definition/plan/publication and base snapshot hashes, native context, exact profile/executor ref, original parent rawProfiles hash and parent definition hash excluding the extension. This avoids a circular self hash.

No seed is generated or redrawn here. The source owner must tie its actual deterministic manifest result and original seed/algorithm/pool lineage to the original profile definition/publication before passing it to this seam. The original publication hash covers generated-map metadata when present. Map COMBAT40 is not an enemy weight; no map RNG counter or historical publication is changed. The storage schema does not attest random fairness or approved pool membership by itself.

## Immutable schemas and bounds

Proposal strict fields: `{version, contextId, templates, waves}`. Unknown keys are rejected.

Template: `{templateId, version, definitionId, definition, hp, actions, templateHash}`. `definition` contains the actual versioned behavior payload; the actual owner validates semantics. `hp` is the original S17 unscaled BASE_HP or EHP_BUDGET with explicit null risk flag and positive exact value. Actions use the S17 RAW_ACTION_DAMAGE exact rational schema with explicit null risk/progress flags and unique source IDs. The lowercase SHA256 template hash covers the canonical complete template excluding `templateHash`.

Wave: `{nodeId, visitDepth, waveOrdinal, instances}`. The referenced original map node must exist, be COMBAT, and match visit depth1..11. Each covered node must have exactly two waves, ordinals1/2, in original map node order then wave order. Elite/boss/other nodes are not silently converted. A bounded subset of COMBAT nodes may be supplied; this is not whole-map readiness.

Instance: `{instanceId, ordinal, templateRef:{templateId,version,templateHash}, entryGroup, entryNotBeforeMs}`. IDs are globally unique across the supplied manifest; instance ordinals are contiguous from zero. Exact ref/version/hash must match a pinned template. Group zero has the approved first-group count. Later nonempty contiguous groups contain at most2, and their minimum entry opportunity offset is `entryGroup × entryIntervalMs`. The offset is an earliest logical opportunity, NOT an actual spawn time, actual movement/climb duration or proof of empty capacity. The Unity wave owner must split/delay queued instances according to remaining live capacity, skip full opportunities and avoid catch-up; storage never clears a wave just because the current group is empty. Spatial geometry, placement, actual generation/HP and attack reservations are native execution responsibilities and are not invented by this manifest.

Technical limits:32 templates,32 actions/template,64 covered nodes,128 waves,16 instances/wave,200-character IDs,512KiB canonical UTF8 for proposal AND complete extension pin. These are storage limits, not a fixed256 balance cap. Larger original graphs/definitions are explicitly rejected or need a future approved version/partition contract, never silently truncated. A synthetic test demonstrates256 instances in22 waves referencing2 templates while the original parent rawProfiles has1 unchanged entry. This does not prove an actual256-enemy map.

Pin strict fields: proposal fields plus `{origin, pressurePolicy, manifestHash}`. The manifest hash covers the complete canonical extension excluding only its own hash. Restore validates all refs, hashes, original plan, original source binding and the fixed versioned pressure definition. It does not consult today's template registry, support flags or RNG.

## Approved pressure data, not named mapping

| Visit depth | Count/wave | Alive cap | First group | Next max | Opportunity interval | Attack slots |
|---|---|---|---|---|---|---|
|1..4|6..8|4|4|2|1800ms|3|
|5..8|9..12|6|4|2|1500ms|4|
|9..11|13..16|8|6|2|1200ms|5|

The battlefield is ROOF_EXTERIOR. Shared per-target slots2, same-target preparation interval600ms, entrance grace600ms. The approved baseline trial raw24/period1600/preparation500/recovery300 is supplied separately from templates and marked `namedEnemyMappingApproved:false`; it does not assign Scout ATK×1, Gunner damage, carriage damage or train damage. Risk HP/EHP once, raw damage risk/progress once and final floor remain S17; this storage does no damage calculation or HP spawn conversion.

Policy records alive entrance/climbing inclusion, combat clock only, no catch-up, entrance grace distinct from climb time, no cancellation-based slot acceleration, identity/generation lock, planned/living/residual zero for completion, and unchanged rewards. Supplying policy is not implementing the Unity scheduler, reservations or actual HP journal.

## Authenticated read API

Actual GET `/v1/runs/:runId/b-wave-manifest` is registered through the existing departure route composition. Headers:

- Existing game Bearer credential and original exact DEV tester/session rules.
- `X-Combat-B-Wave-Manifest-Version: combat.b-wave-manifest.v1.20261003`.
- Optional `If-Match`: exact bare lowercase packetHash, same convention as original B read routes.

No body or query parameters. Cookie/admin/public-web credentials do not replace game authentication. Responses and errors use `Cache-Control:no-store`; success ETag is the bare packetHash. Response fields: version, accountId, runId, runStatus, exact `startSnapshotHash`, parentPinHash or null, status PINNED_MANIFEST/HISTORICAL_NO_MANIFEST, manifest or null, false admission/support flags, manifestPinned, packetHash. The packet hash covers all fields except itself. Terminal run reads remain original metadata; they never authorize a new start or mutate combat.

401: absent/expired/revoked original credential.400: query/body contract violation.409: wave version conflict, exact hash stale, manifest/template/reference/count/context/plan/pin conflict, invalid shape or bounds. Existing parent-source/run/account errors retain `combat_b_departure_*` codes. Unexpected failures propagate and are not turned into success. The source is fenced before the final authentication check and again after it. Cross-account SQL reads cannot obtain the original source. API source/metadata and session/risk versions are not accepted from request claims.

`readBWaveManifest` accepts trusted original SQL/START snapshot bytes. Consumers of untrusted bytes MUST supply independently trusted `expectedParentPinHash` and `expectedManifestHash` as well as the original account/run/START anchor; internal self hashes alone do not establish authority. If an expected hash requires a manifest, missing legacy data is rejected rather than downgraded. Original missing/null/unextended S19 pins otherwise return null without modification or upgrade.

## Persistence and LOCAL verification

S19 migration0099 already prevents replacing/updating/deleting a non-null B snapshot and owner rebinding. The extension inherits those guards; no new migration is needed. Original snapshot, operation map copy, idempotency receipt and late parent/wave CAS assertions use the existing same D1 batch. Source/support failures roll back original run/map/snapshot/fuel ledger writes; no separate partial commit is introduced. Original replay returns the same response without invoking today's owner. SQL export/import restore keeps exact original manifests.

Validation fixture definitions LOCAL_SYNTHETIC_ENEMY_a/b, HP17 and raw13/7 are storage transaction test inputs, not Scout/Gunner/production stats. Metadata-only fixture has zero fuel writes. The private actual-original-builder fixture necessarily writes one toy LOCAL fuel ledger on success, while operating economy/admission/deployment remain zero. Public fresh START still rejects combat_not_ready. Existing S19 source modules and historical snapshot schemas are preserved; departure.ts only adds the new read-route import/registration.

QA producer and response samples: `D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-wave-manifest-traces-20261003.mts` / `.json`. Run the producer from the canonical server with `node node_modules/tsx/dist/cli.mjs --tsconfig tsconfig.base.json <absolute producer path>`. It starts a scoped ephemeral loopback server, uses a private signed fixture credential, saves no credentials, proves blocked public fresh START, late wave CAS rollback, exact original read/replay and session/stale errors, then closes listener/database.

Consumer release is limited to immutable wire/reader/source validation after Server implementation/verification AND U18/U19 existing prerequisite evidence. It does not release full multi-enemy execution. Actual named profile/pool/raw/geometry, B operation/story mapping, native context/source/support owner, Unity pending-entry/attack scheduler/multi-generation restore and Scene connection remain separate dependencies. v04 proposals are not implementation defaults.