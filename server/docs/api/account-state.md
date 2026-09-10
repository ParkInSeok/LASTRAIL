# Account state — S01 / S02 read contract

Local S03 contract, 2026-09-10. Not deployed. This document and its implementation
are committed together. Requires migration0028, which adds active squad selection
and offline receipt references; it grants no inventory. Do not use the moving
owner worktree as the earlier S01-only verification reference.

`GET /v1/account/state` uses the existing bearer-token/session/account verifier and
client admission middleware. No query parameters or body. No idempotency key is
required. All responses from the route use `Cache-Control: no-store`.

The server selects the authenticated account only. One SQLite SELECT contains
all holdings subqueries, so they share a database snapshot. GET does not grant,
consume, regenerate fuel, change presets or initialize missing records.

| Field | Contract |
| --- | --- |
| accepted, accountId | true; authenticated immutable account ID |
| serverTime | UTC ISO-8601 response time; not a client clock or revision |
| mutationSequence | highest committed account mutation sequence, or 0 |
| currencies | CREDITS, MACHINE_PARTS, TACTICAL_DATA, SPECIAL_CORE, OPERATOR_EXP numeric balances; absent rows read as 0 |
| items[] | itemId, quantity, ordered by itemId |
| fuel | null if not initialized; otherwise current, max, lastRegenAt, updatedAt |
| operators[] | operatorId, skillTraining, promotion, revision, acquiredAt; ordered by operatorId |
| trainingCoreSlots[] | slot (0–4), operatorId, trainedLevel, investedOperatorExp, revision; only occupied slots, ordered by slot |
| train | null if absent; trainId, level, revision, updatedAt |
| carriages[] / turrets[] | carriageId / turretId, level, revision; owned records, not equipped positions |
| formation | object; activationSupported=true, activePresetId:string or null, activePresetRevision:number or null, revision:number, operators:array, updatedAt:ISO string or null, presets:array |
| formation.operators[] | slot:number (0–4), operatorId:string; copied active squad ordered by slot; [] before activation |
| formation.presets[] | id, name, definitionJson (opaque existing saved JSON string), revision, updatedAt |
| unlocks[] | unlockId, unlockedAt |

All timestamp fields are UTC ISO-8601 strings. Empty collections are `[]`, not
sample inventory. No numeric combat stats or effective squad levels are invented.
Training cores are progression slots; they are not an active combat formation.
S03 adds a separately stored active squad selection. It does not add train/equipment
installation positions or connect this squad to the existing run-start snapshot.
activationSupported=true means the server implements the operator-only save/activate
contract, not that this account already owns a valid squad or may depart. Per-account
eligibility is checked by POST. See [formation.md](formation.md).

## S01 compatibility and exact S03 nesting

S03 preserves all previous top-level S01 fields and formation.presets. Within
formation it changes activationSupported from false to true, allows activePresetId
to become a string, and adds activePresetRevision, revision, operators, updatedAt.
No new field is placed at the response root. A strict S01 parser should remain
pinned to the S01 snapshot until it explicitly adopts this S03 contract.

Never-activated account (formation portion of an otherwise normal state response):

```json
{"formation":{"activePresetId":null,"activePresetRevision":null,"revision":0,"operators":[],"updatedAt":null,"activationSupported":true,"presets":[]}}
```

After saving and activating preset p at revision1 (fixture IDs/time, not grants):

```json
{"formation":{"activePresetId":"p","activePresetRevision":1,"revision":1,"operators":[{"slot":0,"operatorId":"op0"},{"slot":1,"operatorId":"op1"},{"slot":2,"operatorId":"op2"},{"slot":3,"operatorId":"op3"},{"slot":4,"operatorId":"op4"}],"updatedAt":"2026-09-10T05:00:00.000Z","activationSupported":true,"presets":[{"id":"p","name":"Squad","definitionJson":"{\"operators\":[{\"operatorId\":\"op0\",\"slot\":0},{\"operatorId\":\"op1\",\"slot\":1},{\"operatorId\":\"op2\",\"slot\":2},{\"operatorId\":\"op3\",\"slot\":3},{\"operatorId\":\"op4\",\"slot\":4}],\"schemaVersion\":1}","revision":1,"updatedAt":"2026-09-10T05:00:00.000Z"}]}}
```

Saved preset edits do not alter formation.operators or activePresetRevision until
explicit reactivation. Missing ownership later does not fabricate replacements;
the next activation/departure must revalidate its selection.

S01-only reconstructed verification snapshot:
`C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/release-packages/screen-s01-20260910/server`.
Its parent source-manifest.json SHA256 is
`0764f2dbf34fd50367beef6b76754b993a77f47a65698c9461880eb7e4c2316f`.
It uses archived904ce840 + S01 only, migration0027 maximum, old false/null capability,
and its own original S01 document. Five state integration tests executed there PASS.
This is a verified reconstruction, not an asserted historic original build artifact.

Reuse `GET /v1/profile` for display name/commander XP,
`GET /v1/wallet/crystals` for the existing policy-scoped free/paid wallet contract,
and `GET /v1/growth/config` for verified economy version/hash and growth costs.
These are separate reads and do not share the account-state snapshot. No second
crystal balance calculation or duplicate growth catalog was added.

## Refresh and error handling

After accepted growth, claim, recruitment or other holdings mutation, schedule a
fresh account-state GET and refresh the relevant existing wallet/profile query.
For an unknown POST outcome, retry that POST with its original idempotency key,
then read state. A GET never establishes whether an unknown POST was committed.

Unity captures its session/account generation and local refresh request counter.
Discard responses from an old generation or superseded refresh request, including
failures. Replace the snapshot as a whole. Do not add the GET balances to cached
balances. Do not discard a refresh solely because mutationSequence is equal:
legacy/admin writers may update records without incrementing this counter.
The counter is diagnostic, not an ETag or universal concurrency token.

Authentication errors preserve existing 401/403/503 codes. Admission errors also
preserve existing middleware semantics and required content/version headers.
Any query/body returns 400 `contract_violation`. If the account disappears after
authentication, return 503 `account_state_unavailable`, never fabricate holdings.
Keep the previous snapshot visibly stale on failure; never replace it with zeros.

## Executed local verification

Five new real-SQLite integration tests cover account isolation (currency, item,
operator, carriage and turret), explicit absent state, authentication/query
rejection, disappearance, read-only refresh, and the actual game entry point:
signed active session -> growth POST -> new balances/levels -> same-key retry ->
unchanged mutation sequence -> revoked-session rejection. Test accounts and
policies are fixtures, not a DEV/Unity device validation.

The initial S01 full suite was71files446tests. Later S03/S10 validation is recorded
in screen-integration-status-20260910.md and the matching result report.
No deployment, remote migration, OneDrive synchronization or Unity runtime claim.
