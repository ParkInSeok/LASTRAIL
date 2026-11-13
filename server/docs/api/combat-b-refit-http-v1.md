# B refit selection HTTP v1 — S-B-REFIT-HTTP-08

Transport version `combat.b-refit-http.v1.20261002`, approved effect version `combat.b-carriage-buffs.v1.20261002`, original placement `combat.b-placement.v2.20261002`. This new LOCAL factory/persistence adapter supplies authenticated next-selection refit GET/POST. It does not replace old selection/buff files, receipts, investments or fixed departure snapshots. No operating route/deployment, rank issuance/upgrade/costs, START or full-run execution is supplied. Source policy: WAIT#combat-b-continuous-dispatch-20261002 and WORK_QUEUE_SERVER S-B-REFIT-HTTP-08; approved carriage policy44 remains unchanged.

## Available environment and authentication

`createCombatBRefitHttp(refitSource,catalogSource)` is unregistered in operating worker. Missing suppliers explicitly503; no fabricated default operating catalog/physical instance/rank. Enabled only `ENVIRONMENT=LOCAL`, explicit `LOCAL_B_REFIT=ISOLATED_LOCAL_FIXTURE`, HTTP loopback host and same-origin browser requests. Native consumer uses signed game Authorization header privately; homepage/admin credentials are not involved. Existing `resolveAuthContext` and `verifyAccessToken` validate current account/session/signature/expiry. Store validates account/session/access deadline again, and at millisecond SQL transaction commit. Replay still requires current authorization.

Actual route: `GET /_local/combat-b/refit`, `POST /_local/combat-b/refit`, on producer's currently returned `origin` only. `Cache-Control:no-store`, response/request header `X-Combat-B-Refit-Version:combat.b-refit-http.v1.20261002`. Query string and GET body forbidden. POST application/json; body limit18000 bytes via existing bounded reader; required Idempotency-Key printable ASCII1..128. Account identity is resolved from authorization, never body/query. Unknown fields reject.

## GET

Returns `{version,buffVersion,definitionHash,sourceHash,definitions,revision,refit,savedDefinitionHash,savedSourceHash,selectionRevision,rankSource,eligibilityError,startReady:false}`. Empty new refit row has revision0/refitnull; current base selection and rank/physical source must actually exist. `rankSource` contains actual LOCAL source revision/status and seven shared rank entries/physical instances. Definition arrays use original S04 detached reader, not a copied table.

`definitionHash` is SHA256 of canonical JSON `{version:buffVersion,definitions:captureDefinitions()}`. `sourceHash` is an opaque per-account SHA256 of canonical `{version:transportVersion,accountId,source:validatedSourceCapture,catalog:parsedCurrentCatalogWithoutRawDocument}`. Validated source capture includes selected slots, rank revision, selection revision, explicit physical instances and current ownership. Catalog includes version/revision/status/unlocked count/eligible cars. Because the fingerprint covers the complete ownership capture, even an unrelated inventory acquisition/removal changes it; fresh GET/new command is required. This does not rewrite historical refit/receipt/departure ranks or invalidate an already running snapshot. Never recompute sourceHash from UI guesses; consume actual GET. It catches same-revision physical/catalog/ownership data changes and prevents foreign-account source reuse. Semantic object key order is canonical, no raw-document key-order false conflict.

Two consistent captures of source/ownership/catalog/saved state prevent mixed GET output; conflicting capture retries up to4, then409. Authorization is rechecked after capture. Stored original refit is separately returned with current `eligibilityError`: changed rank/selection/definition/physical/ownership does not silently upgrade or rewrite historical selection. This is not an executable START snapshot. Missing current source503 remains explicit; original POST receipt can still replay without current source.

## POST

Example (take both hashes from current GET):
```json
{
 "version":"combat.b-refit-http.v1.20261002",
 "expectedRevision":0,
 "selectionRevision":1,
 "rankRevision":1,
 "definitionHash":"<64 lowercase hex from GET>",
 "sourceHash":"<64 lowercase hex from GET>",
 "refits":[{"slot":0,"type":"BCB01","rank":1},{"slot":1,"type":"BCB06","rank":1}]
}
```

Refits cover exactly every selected slot0..4, combined operator+cargo maximum5. One type per cell, same type in multiple cells allowed. Claimed rank1..5 must equal authoritative account-shared type rank. Existing B selection validator/refit source validator own placement/ownership/physical bindings. Body cannot supply instanceId, carriageId, operatorId, arbitrary growth or account identity. SQL LOCAL source explicitly provisions physical IDs and ranks; absence does not grant R1/R5.

Accepted response: `{accepted:true,version,receiptId,revision,changed,definitionHash,sourceHash,refit,startReady:false}`. `refit` is original validated S04 candidate `{version,accountId,selectionRevision,rankRevision,sourceStatus,cars:[{slot,instanceId,carriageId,occupant,type,rank}]}`. Changed full candidate/hash increments refit revision1; same original candidate/hash with new key is no-op but records its own immutable receipt. This operation is free selection only, not a rank upgrade, spending, reward or refund.

One D1 batch transaction verifies current authenticated account/session/access deadline, expected refit revision, current exact base selection/raw rank/instance documents, current ownership and current catalog document/revision; the complete owned car/operator sets included in the fingerprint are guarded by count AND membership, so unselected asset additions/removals also cannot commit a stale sourceHash; writes only new refit selection and its receipt. Failure rolls back both. Late competing same-key commit is rechecked before declaring a revision conflict; two identical concurrent requests share original receipt. Distinct changed-key requests obey CAS. No writes to original placement, shared ranks, source rows, old receipts, A loadout/old turret investments, run/START/economy rows.

Key is account scoped. Same normalized body/key returns EXACT original response, even after later selection/rank/catalog changes or source absence. Same key/different body409. Current session revocation/account inactive/token expiry still prevents replay. Stored receipt UPDATE is rejected by SQL trigger. Malformed/invalid bound receipt503. Loss of response AFTER commit may503 at transport; retry ORIGINAL key and body, never assume rollback or invent a new command. GET can reconcile current selection; it does not replace the original receipt.

## Errors and retry

Errors are `{accepted:false,errorCode}` with same transport/no-store headers.400: malformed strict request/JSON/key/content type/body/query/GET-body.401: original auth error/access_token_invalid/auth_session_invalid.403: account_not_active, current original ownership failure or cross-origin.404: LOCAL gate/unknown route.409: transport version, definition/source hash, refit/selection/rank/catalog revision, duplicate/ineligible/missing selected cells, rank claim, idempotency conflict, bounded read/write concurrency.503: unavailable/invalid source, invalid stored state/receipt or generic storage failure; sanitized stage-only log, no secret/body logging.

Important codes include `combat_b_refit_transport_version_conflict`, `combat_b_refit_definition_conflict`, `combat_b_refit_source_hash_conflict`, `combat_b_refit_revision_conflict`, `combat_b_rank_revision_conflict`, `combat_b_refit_selection_conflict`, `combat_b_rank_conflict`, `combat_b_refit_ownership_invalid`, `combat_b_refit_source_unavailable`, `combat_b_refit_source_invalid`, `combat_b_refit_state_invalid`, `combat_b_refit_receipt_invalid`, `combat_b_refit_read_conflict`, `combat_b_refit_write_conflict`, `idempotency_conflict`; unchanged original B catalog/selection errors propagate. Failclosed malformed source is503, not synthetic success.

After409 reread GET and reconcile actual source/refit; a NEW intended command needs fresh hashes/revisions and a NEW key.503/timeout/cancel may have committed: retain original command/key/body until exact replay or reconciliation. Do not auto-repeat new writes or use consumer cache as rank authority. No automatic retry loop in consumer View.

## Actual private LOCAL supplier

`startBRefitSocket({port?:0..65535,lifetimeMs?:1..900000 default600000,restore?:privateUint8Array})` allocates actual SQL.js SQLite using all98 current existing local migration files plus separate LOCAL selection/refit/refit-HTTP DDL. This says nothing about remote DEV schema or operational migration status. Explicit fixture accounts a/b each own an operator+one cargo placement, distinct physical instances and R1 type ranks. This is trusted isolated supplier setup, not game password login, free operational rank issuance or a default public database.

Returns `{origin,metadata,intake('a'|'b'),checkpoint(),close(),closed}`. Private memory intake contains signed ephemeral authorization header/version header/expiry/account and must go only to trusted consumer supervisor's private env or IPC; never save to files, command argv, logs, stdout, Pages or public endpoints. Existing running Unity credentials are not changed automatically. Supervisor starts its own test child, controls cancellation/signals and `finally` cleanup. Metadata alone is public/credential-free. Memory-only checkpoint is caller-owned; await request completion, consume trusted bytes once and fill(0), no saved DB export. Restore preserves original source/selection/receipt but revokes old fixture sessions and issues fresh signed credentials/secret; old credentials401.

Lease/close stops accepting requests, drains existing handler promises through existing loopback owner, then closes DB and clears owned private bundles. Repeated close idempotent. Request Abort after server acceptance does not roll back an already valid command; actual TCP cancellation test waits handler completion, then GET/replay proves one receipt. Port failure disposes new DB/bundles and leaves existing listener alone. Fixed lease is a LOCAL test-lifecycle bound, not a game session policy.

Use canonical cwd `D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`; TEMP/TMP/TMPDIR under D project. Actual runnable trace command: `pnpm exec tsx --tsconfig scripts/validation/combat-b-refit-http-tsconfig.json D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-refit-http-traces-20261003.mts`. This tsconfig supplies existing @lastrail/common path mapping; direct unconfigured tsx may fail module resolution. It exports no credentials; actual TCP supplier is private import, no standalone public CLI.

## Validation and remaining dependencies

New tests exercise real Hono auth HTTP+SQLite transaction and real127.0.0.1 TCP supplier, including CAS, original response, ownership/account/rank/physical/catalog changes, millisecond access deadline, receipt rollback, commit-then-ACK-loss, reopen/fresh auth, source missing/default gate, request Abort and all cleanup. Existing frozen source/selection/buff pins and old run rows are preserved. QA actual TCP responses are credential-free.

Unity U-B-REFIT-HTTP-09 can consume actual next-refit authentication/save/read/original-result/recovery contract once S08 and its U-B-SELECTION-03/U-B-BUFF-05 predecessors are evidenced. No final View/bootstrap before existing art/resource/intake AND. Operational physical/rank issuance, initial unlock/base HP+DEF/costs/cargo, original START/snapshot transaction, live actor/geometry/effect/hasteclock, full-run node/retention/1024 journal rollover and cooperative/world profiles remain separate policy/implementation prerequisites. Do not classify this new persisted next-selection as gameplay/START/production readiness.