# CE03 local producer stage 1

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
