# Community author deletion v1

Contract: `community.content-store.v1.20261008`; state model `community.content-state.v1` unchanged. S-16 partial implementation, not community publication readiness.

## Request and response

`POST /v1/web/community/contents/:contentId/delete`

Headers: `Content-Type: application/json`, `Origin` matching configured `WEB_ALLOWED_ORIGIN`, separate web session cookie, `X-CSRF-Token`, `X-Community-Content-Version: community.content-store.v1.20261008`. DEV additionally requires `X-LastRail-App-Version-Code` with the exact active tester entitlement. Game Bearer and Admin credentials grant no access here.

Body: `{"commandId":"delete-1","expectedRevision":1}`. Unknown fields, query parameters and invalid IDs/revisions are rejected. Content IDs use 1–128 ASCII letters/digits/underscore/hyphen. Existing finite request transport limit is 18,000 bytes. The server derives the account from its authenticated browser session.

200: `{"contractVersion":"community.content-store.v1.20261008","commandId":"delete-1","contentId":"p","revision":2,"visibility":"AUTHOR_DELETED"}`. No owner account, original title/body, CSRF secret or internal notes are returned. All responses `Cache-Control: no-store`; exact origin credential CORS. Matching OPTIONS supports POST and the listed headers.

The same account/command ID and exact payload replay the original receipt, including after response loss; changed target/revision with the same key is 409. Session and tester rights are rechecked on retries. Current deleted state and linked original audit/receipt are validated before replay; corrupted state or republished active body cannot be reported as successful deletion.

## Atomicity and ownership

`CommunityContent` owns author/deletion state rules. `CommunityContentStore` owns active-content persistence and exact old state JSON/revision CAS. One DB batch fences active browser session/CSRF, active owning account, DEV tester and original content; then changes the active state, writes a body-free receipt and minimal action audit. Audit insertion failure rolls back body removal and receipt together. Simultaneous identical requests yield one receipt/audit. Owned comments may be deleted under hidden or locked parents without modifying the parent.

Migration `0108_community_content.sql` creates independent active-content, receipt, audit and transaction-guard tables. No account catalog seed, publication or live migration was performed. The audit records the approved community v3 action-based maximum of 180 days with a policy version, distinct from legal security logs. Periodic purge, receipt retirement/reference lifetime, backup/search/cache/evidence deletion and actual storage policy activation remain unimplemented. Active-row body removal is not a declaration that every copy has been destroyed.

## Errors

- 400 `community_command_invalid` / `contract_violation`: malformed or unsupported input.
- Existing web core: 403 `web_origin_forbidden`, `web_csrf_forbidden`, `tester_forbidden`; missing/revoked mutation cookies use the core's CSRF failure contract. Unavailable origin/environment: 503 `web_support_unavailable` (OPTIONS: `community_unavailable`).
- 403 `community_forbidden`: another author's object.
- 404 `community_content_unavailable`: missing target for a fresh command.
- 409 `community_version_conflict`, `community_request_conflict`, `community_revision_conflict`, `community_deleted`.
- 503 `community_storage_invalid`: damaged row/receipt/audit or deleted-state mismatch; `community_storage_unavailable` for a failed transaction without a competing revision. Unexpected route failures: `community_unavailable`.

No list/detail/create/edit/renderer/sanitize/report/block/moderation APIs are supplied by this unit. In-process API tests use real existing password/web-cookie/CSRF and tester core with isolated SQLite; no persistent DEV URL, browser or remote D1 validation is claimed. Verification: QA/CommanderS1-20260922/community-delete-final-20261008.json, 43 tests passing across five files; scoped type/lint exit 0. Initial route tests failed before implementation; review regressions failed 3/13 before the fixes.


## Nonpublic target correction — 2026-10-08

The shared author-command pipeline now follows approved11 §5: another author's HIDDEN/REVIEW_PENDING/AUTHOR_DELETED target returns404 community_content_unavailable, identical to a missing target; another author's visible target remains403. Original deletion receipt/hash and18KB transport semantics are unchanged. The completed edit renderer is a separate contract at community-author-edit-v1.md; this deletion API does not accept body/title edits.

