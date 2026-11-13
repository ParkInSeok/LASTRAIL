# Community POST comment lock v1

Contract `community.comment-lock.v1.20261009`; approved source `docs/superpowers/plans/_categories/10_LIVEOPS_NOTICES.md#community-comment-lock-approved-20261009` and current `11_WEBSITE_COMMUNITY.md`. This is the approved comment-lock command under existing `COMMUNITY_MODERATE`. Existing moderation/grant versions and their request hash/receipts remain unchanged.

## Request and permission

POST `/v1/admin/community/contents/:id/comment-lock`

Existing Admin authentication plus current ACTIVE OWNER/OPERATOR with explicit COMMUNITY_MODERATE is required. OWNER role, inquiry permission, VIEWER or member WEB cookie alone cannot authorize. DEV/PRD come from the server binding; client environment/actor/permission overrides are rejected.

Headers:

- `X-Community-Comment-Lock-Version: community.comment-lock.v1.20261009`
- `Content-Type: application/json`
- `Idempotency-Key`: 1–128 printable ASCII characters, no whitespace.

Strict JSON `{action: "LOCK_COMMENTS" | "UNLOCK_COMMENTS", expectedRevision: number, reason: string}`. Revision is an integer 1–9007199254740990. Trimmed reason is 1–500 characters. Resource ID is ASCII alphanumeric/underscore/hyphen, 1–128 characters. No query parameters or additional JSON fields. Existing bounded body transport limit is 18,000 bytes. Responses use `Cache-Control: no-store`.

Only POST targets can change. AUTHOR_DELETED cannot change. HIDDEN or REVIEW_PENDING stays in its original visibility; unlock never restores a post or releases an account restriction. The existing model changes only commentLocked and revision (successful revision+1), retaining title/body and current descriptor.

## Receipt, atomicity and recovery

Response `{contractVersion,environment,resourceType:"CONTENT",resourceId,kind:"POST",revision,visibility,commentLocked,rendererVersion:"community.markdown.v1.20261008"}`. No body/author identity/reason is returned.

Already requested lock state with a new key returns409 `community_comment_lock_transition_invalid`, matching existing moderation transition conventions. This does not add a NO_CHANGE policy. Same original key/body returns its original receipt even after a subsequent unlock, without re-locking current content. Keys share environment+actor scope with moderation commands; same key with a different contract/resource/body conflicts409. Current permission is checked for replay, and a receipt is not current state. Existing GET `/v1/admin/community/contents/:id` with the moderation v1 header supplies current metadata/revision.

Current grant revision, exact parent row/revision/state, validated current renderer source, minimal audit, immutable receipt, content update and body-free lock proof share one DB batch. If source/grant/state changes first, the whole command is rejected. Proof persistence failure rolls back content, success audit and receipt. General community audit reason visibility remains protected by current dedicated permission. No title/body historical copy is introduced.

After uncertain transport failure retry the original body/key to recover the original result. After409 revision/transition conflict read current metadata and use a new key only for a new deliberate command. Do not automatically retry403 or invalid transitions. Refresh public/member state after success or original replay; historical receipt must not overwrite newer current state.

## Member effects and errors

New comments on a locked post return409 `community_comments_locked` with no new content/receipt/quota. Comments committed first remain and their original receipts replay. Existing post/child reads, own deletion and reporting retain their existing permissions. Public parent eligibility, personal blocks, account/session/exact DEV tester, CSRF, current published policy consent and rate limits still apply. Unlock does not bypass these gates. Creation/author/public/own versions and createdAt order/cursors remain unchanged. Existing commentLocked/revision fields reflect current verified source; updatedAt reflects the current lock proof time. Nonpublic body/time masking is retained.

Error shape `{error}`. Existing authentication401;403 `ADMIN_PERMISSION_DENIED`;400 `contract_violation`, `idempotency_key_required` and existing body-limit errors;404 `community_content_unavailable`;409 `community_comment_lock_version_conflict`, `community_comment_lock_target_invalid`, `community_comment_lock_transition_invalid`, `community_revision_conflict`, `community_deleted`, `idempotency_conflict`, `community_moderation_authority_stale`, `community_moderation_stale`;503 `community_moderation_unavailable`, `community_moderation_storage_invalid` and existing renderer/source failures. Unknown internal exceptions return a bounded unavailable error, never private data.

## Schema and evidence

Empty additive migration `0119_community_comment_lock.sql` follows0001–0118. Updated renderer/list readers and Admin writer require it before rollout; an old remote DB is not assumed compatible. It stores body-free current revision proof tied to immutable command receipt. Existing active renderer source/ordinary audit/receipt retirement dependencies remain; this adds no purge, seed, grant, publication or deployment.

QA `D:/Project/LASTRAIL/QA/CommanderS1-20260922/community-comment-lock-writer-verified-20261009.json`, SHA256 `29dc2d2b014442c96df222f1a17cbb35645291479a3102068f1fb02e577b4985`: 368PASS/0FAIL/0pending, including21 new actual handler tests (15 Admin/core and6 real WEB authentication path) plus347 affected prior regression. Initial missing route produced12 failing tests before implementation. All four server package type checks and changed-file ESLint exit0. Focused current-source review found no Critical/Important issue; reviewer ran no tests/edits and did not independently verify a Git diff.

Evidence uses actual registered Admin/game handlers, disposable migration-backed sql.js and explicit LOCAL_HEADER synthetic Admin principals. WEB evidence uses actual normal password login/cookie/CSRF/exact DEV tester and current consent services with clearly synthetic published policy sources. Core creation race tests use an explicit test eligibility port, not real WEB consent. No current Access JWT/TCP/browser/TLS/CORS/remote D1 or standing DEV URL was validated here. Previous preparation38 and moderation347 are historical evidence, not additive claims of extra tests.

A-03 can now consume typed/BFF lock commands with current permission/revision/key/error recovery. H-04 can consume current flag and new-comment errors under existing DTOs. Screen connection remains after approved design. Whole S16 retention/source retirement, notice/purge/withdrawal/backup, descriptor/sanction policy, actual published operating terms and live environment remain separate dependencies. No operating migration, real grants, posting, rewards or deployment occurred.
