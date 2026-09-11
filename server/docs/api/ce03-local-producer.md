# CE03 local producer stage 1

The later isolated host and support/leader command contract are implemented in
[ce03-local-host.md](ce03-local-host.md). This document retains the standalone
stage1/r4 boundary; actual full-consumer evidence is still required for admission.

Implements the adopted `ce03.local.qa.20260911.001` selection using the consolidated
r4.1 field contract. This is local preparation, not accepted combat admission.

`ce03-local-producer.ts` verifies the exact raw SHA256 of the authored combat data,
Direction's adopted selection overlay and Unity's typed binding evidence. It
projects only the closed r4 fields, validates explicit actor/render roles and
dependency closure, resolves all15 mission1 waves and the complete selected kits,
train/manual/car devices, package and policy settings. No live catalog is changed.
Raw paths, importer metadata and code dependency evidence remain provenance through
the pinned source file; they are not extra executable Binding fields.

The source fixtures are immutable input copies under `tests/fixtures/ce03`:

| File | Raw SHA256 |
| --- | --- |
| combat-stats.json | 66451f6948acc0d112864aaad0d95a3f0673993612ce691825727d1c305e263f |
| selection.json | 5418b60c50d07b2ffdd1c9ae9616f27a1e27bac58d15e3f8263d7b7ccbc7fa91 |
| typed-bindings.json | 15560bb7cb54198cd282ed999ff62dc2d22bd07b44d8355a9664a624e5f4efec |

## Authenticated disposable preparation

`registerLocalCombatProducer` is deliberately NOT called by the shipping app.
Its sole endpoint is `POST /__local/ce03/prepare` on a separately composed Hono
instance. Caller composition must supply the exact disposable DB object, an
account allowlist and the existing authentication resolver. It verifies loopback
URL, DB object identity, actual token/session/account and allowlist. This is not a
network sandbox or authorization to mount the endpoint publicly. No persistent
listener or wrangler configuration is created by this slice.

The request is the existing closed departure-preview selection body:
`operationId`, `risk`, `expectedFormationRevision`, `expectedLoadoutRevision`,
`expectedUltimateRevision`. No client account ID, effective stats or ready flag.
`readLocalDepartureSelection` reuses existing ownership/loadout/open-run/balance/
fuel checks without creating a preview. The producer enforces the adopted actual
L1/P0/skill1/train1 fixture, five unique owned initial operators, five distinct
training core rows and ordered Combat/Gun. `skillRank` is exactly `skillTraining`.
An explicit ultimate is required. Before returning it rereads the selection and
rejects any changed source/revision; future start still needs its own atomic check.

Response status is `MISSING_EXECUTION_EVIDENCE`, `ready:false`, with `draft` and
the original `ownedSelection` separately. The draft's template intentionally
contains no run identity or ownership revisions; actual owner facts are retained
separately for the future accepted snapshot. Nothing here debits fuel, generates
seed, creates a run, returns a successful admission or grants a reward.

## Execution evidence and serialization boundary

`finalizeLocalCombatArtifacts(draft, undefined)` fails with
`ce03_execution_evidence_missing`. No code/conformance digest is filled from
candidate markdown or invented constants. Actual Unity code/runner/vector
attestation is still unavailable and `LOCAL_CONSUMER` evidence is not accepted by
this stage. This is the precise remaining integration input, not a renewed policy
approval wait.

The fixture-only positive path accepts executed test algorithm source bytes,
vector JSON and a zero-failure count under `DISPOSABLE_FIXTURE`. It computes
digests from those bytes and validates closed Profile/ContentManifest serialization.
Returned artifacts explicitly have `evidenceScope:DISPOSABLE_FIXTURE`,
`ready:false`, `frozen:false`, and **candidate** digest names. They cannot attest
to Unity combat or be used as actual consumer evidence. This function is not
exposed as an HTTP request and is not connected to start.

No seed/S snapshot is generated. Real P/C/S freeze and start/debit integration
remain the later stage after actual consumer evidence and atomicity verification.
Proposal policy references pin the selected semantics, not executable code.

### Server-owned carriage type projection

The authenticated isolated prepare response additionally contains
`carriageOwnership:{rulesVersion:"train-loadout-v1",loadoutRevision,carriages}`.
Each selected carriage has `{carriageId,slot,level,revision,type}` with the local
closed type `Combat` or `Gun`. The producer joins the rechecked owned rows by ID
to the selected loadout and uses `train-loadout.ts`'s authoritative
`resolveCarriageType`, also used by existing loadout validation. It explicitly
converts `COMBAT`/`GUN` and checks the corresponding draft slot's type. No client
map, name parsing or array-position type inheritance is involved. Unknown types,
missing ownership, changed revisions and exchanged selected IDs are rejected.

The Unity caller can derive its required `trustedCarTypes` map from this response
by `carriageId`, matching the projection's loadout revision and each ID/slot/owned
level/revision to `ownedSelection`. Reject duplicates, missing rows or unknown
types. Preserve that association with the same response; a projection detached
from its authenticated response is not proof of ownership. The projection leaves
the existing draft and ownedSelection fields unchanged, and does not require a
second GET. Final atomic admission revalidation remains separate.

Projection verification: absent projection first failed the new regression;
focused producer/loadout/attestation24PASS; full88files593testsPASS20.02s;
game-api TypeScript and changed-file ESLint PASS. Distinct owned revisions7/9,
unchanged DB bytes, swapped selected IDs, missing ownership and caller-supplied
projection rejection are covered. Independent review found no high-impact issue.

### Full-consumer attestation interface

`ce03-consumer-attestation.ts` adds the internal
`attestLocalConsumer(draft, artifacts, verifier)` boundary. It does not register a
route or feed start/freeze. The trusted local composition must supply a
`ConsumerAttestationVerifier`; no default provider is installed. The provider must
verify the actual runner/code/vector/checkpoint evidence, not just return true
because JSON parses. HTTP clients cannot supply that verifier.

Required artifact strings are `algorithmSource`, `checkpointSchema`, `vectorsJson`
and `reportJson`. They are bounded and reject lone UTF16 surrogates. The algorithm,
vector and checkpoint digests are computed from their exact UTF8 bytes. A source
bundle may be represented as a stable textual archive; the real runner adapter
must validate its complete code identity. Markdown provenance is not execution.

The new runner handoff report is closed canonical JSON with fields:
`schemaVersion:ce03-consumer-runner-report-v1`, `scope:FULL_LOCAL_CONSUMER`, `runId`,
`simulationVersion`, `draftSha256`, `algorithmSourceSha256`, `vectorsSha256`,
`checkpointSchemaSha256`, `results:[{caseId,coverage,status:PASS}]`, `passed`,
`failed:0`, `skipped:0`. `draftSha256` covers the complete strict draft's canonical
JSON. Existing operation-journal keys/raw response strings are unaffected by this
new report serialization convention. Duplicate report keys reject.

The vector artifact must have `cases` with unique `id` and `coverage`. Every vector
must have one matching passed report result. Full coverage requires
`STRICT_PROFILE`, `FULL_EFFECTS`, `MANUAL_COMMANDS`, `FIFTEEN_WAVES`,
`CHECKPOINT_RESTORE`, `DETERMINISM`, `OWNER_ISOLATION`, `JOURNAL_RECOVERY`.
These categories describe the already-required complete consumer scope; counts
alone (including the current18 basic/vitals tests) cannot substitute for it.

After shape, complete-coverage and exact-hash checks, the injected verifier receives
one frozen request with the exact artifact strings and measured hashes. Rejection
or exceptions fail closed. A successful result still says `ready:false`,
`frozen:false`; it contains verified evidence digests, not a seed, accepted run,
frozen P/C/S or admission capability. No actual provider or full Unity evidence is
available yet. Test-only accepting/rejecting stubs verify this control flow only.

Errors distinguish absent artifacts, absent verifier, invalid report/draft,
incomplete coverage, changed evidence, rejected attestation and unavailable
verifier. Actual runner adapter/attestation and consumer integration remain later
work; this interface does not claim those have passed.

## Reproduction and verification

From `server`, run the package's Vitest runner on
`tests/integration/ce03-local-producer.test.ts`. Seven tests cover real auth and
owner facts, no writes, explicit ultimate, stale/in-flight revisions, invalid
growth, wrong route host, missing execution evidence, altered sources, strict
numeric/effect schemas, signedInt64 locators and typed dependency/role mismatch.

Run `tsx --tsconfig tsconfig.base.json scripts/validation/verify-ce03-local-producer.ts`
to create a fresh in-memory DB with migrations0001–0035 and an explicitly named
Reina-ultimate automated fixture. It writes `audit-results/ce03-local-producer/`
`draft.json` and `verification.json`. No remote DB inputs, persistent account
state, remote credentials or real accounts are used. Missing execution evidence
and byte-identical DB state are asserted. No fixture execution-evidence hashes
are included in that actual draft export.

Verification for this slice: focused7PASS; full87files/586testsPASS20.45s;
game-api/async-worker TypeScript and source ESLint PASS. Independent bounded review
found no high-impact stage1 defect. No actual Unity battle/checkpoint/video test,
new migration, deployment, public-ready gate or real-account mutation.

Attestation interface verification: focused6PASS1.76s;
full88files/592testsPASS17.24s; game-api/async-worker TypeScript and source
ESLint PASS. Six tests cover absent providers/artifacts, partial coverage,
tampered evidence, verifier rejection/errors, immutable exact requests and
malformed/ambiguous reports. A slow element-wise DB assertion was replaced by
byte comparison; the final full run passed without increasing its timeout.
Independent bounded review found no high-impact defect. Accepting providers in
these tests are synthetic fixtures; actual Unity conformance remains unverified.
