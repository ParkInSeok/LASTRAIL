# Community report review and content moderation v1

Contract `community.moderation.v1.20261008`; source `10_LIVEOPS_NOTICES.md#community-moderation-grant-approved-20261008`. Implemented in the canonical Server Admin app, distinct from member WEB routes and the already supplied grant-management version.

## Authentication and environment

All five routes require existing Admin authentication AND current ACTIVE OWNER/OPERATOR with explicit `COMMUNITY_MODERATE`. OWNER role or inquiry permission alone is insufficient; VIEWER and member WEB cookies cannot authorize them. Exact header `X-Community-Moderation-Version: community.moderation.v1.20261008` is required. DEV/PRD are server-binding DB environments; client environment overrides are forbidden. All responses are `Cache-Control: no-store`. Reads and receipt replay recheck current permission; cursor is never an access capability.

No standing DEV URL, new browser credential flow or operating migration is supplied. Local evidence uses actual registered Admin handlers, explicit LOCAL_HEADER synthetic principals, disposable SQL and real existing domain/source services. It is not new Access JWT, TCP, browser TLS/CORS or remote D1 evidence. Consumers may run an isolated handler harness now; live Access/BFF checks remain a separate environment task.

## Read routes

- GET `/v1/admin/community/reports`: optional `status=OPEN|IN_REVIEW|ACTIONED|DISMISSED`, `limit` 1–50 (default20), `cursor` up to1024. Default queue includes OPEN and IN_REVIEW. Unknown/repeated query fields or nonempty body are rejected. Sorted `submittedAt DESC, caseId COLLATE BINARY DESC`. Opaque cursor binds version, moderator identity hash, environment and exact status filter. Response `{contractVersion,environment,items,nextCursor}`; each item `{caseId,targetType,targetId,targetRevision,status,revision,submittedAt}`. No reporter identity, report text or evidence body in the list. Current page and final authority are rechecked before return.
- GET `/v1/admin/community/reports/:id`: no query/body. Response `{contractVersion,environment,report,evidence}`. `report` has the same item fields. Private `evidence` is `{snapshot,reasonCode,optionalDescription}`; snapshot `{targetType,targetId,revision,title,sanitizedBody,rendererVersion,observedAt}` is the original necessary report capture. Exact snapshot schema/hash/target/revision/time are checked, then current evidence/case and permission are rechecked. Missing evidence is a source failure, not a fabricated empty capture. No current target body is reconstructed from evidence.
- GET `/v1/admin/community/contents/:id`: no query/body. Response `{contractVersion,environment,content:{id,kind,parentId,revision,visibility,commentLocked}}`; no body, title, internal account identity or report reason. Use the returned current revision for an explicitly new content command.

Path IDs are ASCII alphanumeric/underscore/hyphen, 1–128 characters.

## Write routes and original-result recovery

JSON and printable ASCII `Idempotency-Key` of1–128 characters are required; shared bounded reader caps bytes at18,000. Unknown JSON fields are rejected; reason is trimmed1–500 characters and expectedRevision is an integer1..MAX_SAFE_INTEGER−1.

POST `/v1/admin/community/reports/:id/review`:

```json
{"status":"IN_REVIEW","expectedRevision":1,"reason":"Necessary review reason"}
```

Only OPEN→IN_REVIEW and IN_REVIEW→ACTIONED/DISMISSED are accepted. Same-status commands, direct OPEN→terminal and terminal reopening are conflicts. Receipt `{contractVersion,environment,resourceType:"REPORT",resourceId,revision,status}`. Success increments revision once. This command neither hides/restores content nor sets final notification/evidence expiry.

POST `/v1/admin/community/contents/:id/moderate`:

```json
{"action":"HIDE","expectedRevision":1,"reason":"Necessary content reason"}
```

HIDE accepts currently VISIBLE/REVIEW_PENDING only when current stored renderer provenance is verified; RESTORE accepts HIDDEN only. Receipt `{contractVersion,environment,resourceType:"CONTENT",resourceId,revision,visibility,rendererVersion}`. AUTHOR_DELETED cannot be hidden/restored or reconstructed from report evidence. Restore requires current ACTIVE author, exact public-author mapping, admitted original creation/descriptor metadata, current verified rendered body, and for comments a verified currently public eligible parent. Existing state model owns the visibility change. Hidden author edits remain the current verified body when restored.

Authority revision, content/case source, required parent/account/publication facts, receipt and minimal audit share one DB batch. Grant revoked before commit yields no moderation change; rollback covers content/case, success audit, receipt and proof. Exact prior command/revision/hash/time/receipt and source uniqueness are fenced, so late proof mutation/duplication cannot be promoted into a new valid proof. No title/body copy is added to moderation audit/proof/history.

Original keys are scoped to environment+moderator across report/content commands. Hash includes contract/environment/resource/body. Matching original request returns its original receipt even after later legitimate changes, without changing current status/visibility. Different payload/resource with the same key is409. Original replay still requires current moderator authority. After uncertain transport failure keep original body/key to recover its receipt; after revision conflict read current state and use a new key only for a new deliberate command. No new command should replay an old receipt as current truth.

## Errors and public/member compatibility

Error shape `{error}`. Existing authentication401;403 `ADMIN_PERMISSION_DENIED`;400 `contract_violation`, `idempotency_key_required`, `community_cursor_invalid`;404 `community_report_unavailable` or `community_content_unavailable`;409 `community_moderation_version_conflict`, `idempotency_conflict`, `community_revision_conflict`, `community_review_transition_invalid`, `community_moderation_transition_invalid`, `community_deleted`, `community_parent_unavailable`, `community_restore_ineligible`, `community_moderation_authority_stale`, `community_moderation_stale`;503 `community_moderation_unavailable`, `community_moderation_storage_invalid`, `community_report_evidence_unavailable` and existing renderer/publication source failures. Do not automatically retry invalid transitions/permissions; transient unavailable reads may use bounded consumer retries. No private exception/body is returned in an error.

Public/own read versions, original creation order/cursors and all old author/report receipt bytes remain unchanged. HIDDEN post disappears from public list/detail and blocks its comments. Eligible RESTORE makes current VISIBLE revision readable through the same renderer/source verifier. `updatedAt` reflects that current revision's validated moderation proof time; createdAt/order remains the original creation. Nonpublic owner body/time remain masked. Personal blocks still apply after restore. Moderator proof time participates in final public/own page source comparison. Member own report result remains only RECEIVED/PROCESSED; actual terminal review changes it to PROCESSED without exposing ACTIONED/DISMISSED, reasons or evidence. Original member submission receipt remains its original RECEIVED result.

General Admin audit hides community moderation reasons from readers without the current dedicated permission, including a final post-query permission check. Grant management audits remain OWNER-only under their separate contract.

## Schema, verification and remaining dependencies

`0118_community_moderation.sql` is empty additive schema after0001–0117, containing immutable command receipts, body-free current-revision moderation proof and report queue index. Apply schema before any updated reader/Admin build in a separately authorized environment rollout. No operating SQL/migration/grants/deploy were performed. Updated read code requires this schema; do not claim an old remote DB is compatible without checking it.

QA `D:/Project/LASTRAIL/QA/CommanderS1-20260922/community-moderation-api-verified-20261008.json`, SHA256 `b55cee313d5a2a63214dad12141bc97b8ad6f44541209867b55a82a62e8068af`:347PASS/0FAIL/0pending (new27 plus affected community, grant and inquiry/Admin regression). Full common/Game/Admin/async-worker type checks and changed-file ESLint exit0. Actual late audit permission loss RED→GREEN and proof previous-revision/duplicate+case source races RED3→GREEN were exercised. Focused static review found one Important proof-fence issue, fixed with executed regression; delta review found no further Critical/Important issue. Reviewer ran no tests/edits. Initial `community-moderation-api-final-20261008.json`331PASS/16FAIL is preserved as failed evidence, not a completion result; three older isolated fixtures omitted the new schema, corrected by including actual0116/0118 migrations rather than relaxing product checks.

Ready A-03 scope is non-screen typed private queue/detail/review and HIDE/eligible RESTORE, composing existing grant management/BFF with current read, revision/key lifetime, error/recovery and reason isolation. Existing H-04 can consume limited own result and public visibility/current proof effects under unchanged DTOs. Screen connection still waits for approved design.

Not supplied by this unit: comment lock writer, sanctions/types/period/permanent ban/game penalty cascade, actual notice/purge/withdrawal/backup lifecycle, descriptor editing policy, published operating terms, live Access/browser/remote environment, or UI. ACTIVE account status and authoritative parent/publication source are checked, but no new community sanction policy/source was invented. Active renderer source retirement remains subject to the existing S16 retention/receipt separation decision; no deletion job or fake retention completion was introduced.
## Comment-lock follow-up supplied — 2026-10-09

The separately approved actual writer is now supplied by [community-comment-lock-v1.md](community-comment-lock-v1.md), contract community.comment-lock.v1.20261009. The earlier remaining-writer paragraph is historical to this moderation unit; it is no longer the current lock API blocker. Other scope and environment limitations remain.
