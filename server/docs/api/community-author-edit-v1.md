# Community author edit v1

S-16 partial contract: `community.content-edit.v1.20261008`; renderer `community.markdown.v1.20261008`; unchanged state model `community.content-state.v1`. Publication, browser integration, deployment and the full community feature are not completed by this unit.

## HTTP contract

`POST /v1/web/community/contents/:contentId/edit`

Required headers: `Content-Type: application/json`, exact configured `Origin`, separate web cookie, `X-CSRF-Token`, `X-Community-Content-Version: community.content-edit.v1.20261008`; DEV also requires `X-LastRail-App-Version-Code` matching the exact active tester entitlement. Game Bearer/Admin credentials provide no authority. Same Origin credential CORS/OPTIONS and all responses `Cache-Control: no-store` reuse the deletion route boundary.

Post body: `{"commandId":"edit-1","expectedRevision":1,"title":"New title","markdown":"**New body**"}`. Comments use `title:null`; post titles require 1–100 Unicode code points after domain trimming. Body is 1–10,000 Unicode code points for posts, 1–1,000 for comments; whitespace-only input is rejected. Counts use the submitted body, including whitespace. Clients cannot supply account/kind/visibility/sanitizedBody/renderer policy. Unknown fields or query parameters are rejected.

The finite edit transport cap is 128,000 UTF-8 bytes, allowing the approved maximum even when surrogate pairs are JSON-escaped (120,000 bytes for the body plus title/envelope). Existing default callers and deletion remain capped at 18,000 bytes. Invalid server limits fail closed. This transport limit does not increase approved content lengths.

200 example: `{"contractVersion":"community.content-edit.v1.20261008","rendererVersion":"community.markdown.v1.20261008","commandId":"edit-1","contentId":"p","revision":2,"visibility":"VISIBLE"}`. Receipt has no title/body/private owner or audit notes. It acknowledges the **original command result**, not the latest content projection: after subsequent edits/hide/delete, retries return this original metadata without restoring body/visibility. Consumers must not republish or replace current state using a historical receipt. Read/detail APIs are a separate unfinished unit.

Same account/key/exact parsed command returns the original receipt; changed title/body/target/revision or cross-operation key reuse is409. Deletion v1 receipt bytes and hashes remain unchanged. Missing/revoked session, CSRF rotation, account suspension and DEV tester revocation are rechecked on retries and fenced again in the mutation batch.

## Renderer and persistence ownership

Only paragraphs/line breaks, flat ordered/unordered lists, emphasis, inline code, fenced code and explicitly approved HTTPS links produce HTML. Unsupported Markdown constructs remain escaped text. The output uses fixed allowlisted tags and escaped text/URL attributes; no raw HTML passthrough, image rendering, embedding or external fetch. HTML examples inside code are escaped literals. Normal raw HTML tags outside code are rejected. Link labels are escaped text.

The route currently supplies **no approved link origins**, so new link input is rejected while text/code editing works. Renderer tests use an explicit synthetic approved origin only to verify exact-origin/credentials/scheme/attribute escaping. No real allowlist, new-account link eligibility or new policy setting was invented. Any later link activation must supply the actual approved server policy; it is not a client field.

`CommunityContent` remains the domain owner and preserves HIDDEN/REVIEW_PENDING on author edits; comments cannot acquire a title. `CommunityContentStore` uses one shared author-command pipeline for revision/full-state JSON CAS, authorization assertions, body-free immutable-result receipts and minimum audit. Server-rendered HTML replaces the active body; prior body/title and submitted Markdown are not copied into receipt/audit history. A new `0109_community_content_edits.sql` audit records revisions, renderer output state hash and action-based community v3 maximum180-day metadata, preserving0108 deletion structures. No operating migrations were executed. Periodic purge, receipt retirement/reference policy, evidence/search/cache/backups and account deletion remain unfinished.

Replay validates the original audit and receipt plus structurally valid current state. If the active revision still equals this edit, its full state hash must match the original edit; later legitimate revisions are not overwritten. Deleted content cannot be edited with a fresh command. A final audit failure rolls back the content and receipt. The renderer avoids repeated unclosed-label scans; local cost checks are not Workers CPU/concurrency guarantees.

## Errors and consumer scope

400: `community_command_invalid`, `community_content_invalid`, `community_body_invalid`, `community_markdown_invalid`, `community_html_forbidden`, `community_image_forbidden`, `community_link_forbidden`, `contract_violation`.

403: existing `web_origin_forbidden`, `web_csrf_forbidden`, `tester_forbidden`, or `community_forbidden` for another author's visible target. Missing/revoked mutation cookie follows existing CSRF403 semantics. **404 `community_content_unavailable`** is shared by missing and another author's nonpublic targets, including on deletion; no private-object existence is disclosed.

409: `community_version_conflict`, `community_request_conflict`, `community_revision_conflict`, `community_deleted`. 503: web feature/environment unavailable, `community_storage_invalid`, `community_storage_unavailable`, or unexpected `community_unavailable`. No failure is converted into published success.

H-04 can implement only the nonvisual typed edit command/receipt/error handling, shared in-flight key, session/account-change cancellation and historical-receipt discipline. Screen binding still requires approved design and separate registration. Existing delete contract remains usable. Admin review/create/list/report/block are not supplied here.

## Verification

QA/CommanderS1-20260922/community-edit-expiry-final-20261008.json: seven files75PASS0FAIL0pending (new55 plus affected existing delete20). SHA256 `23a2e5593f0391799441b33d33a0acda3c8a57edf773206e89ba12ab88092f18`. Game-api type and scoped lint exit0. Review P2 cases were reproduced (four failures) then fixed: pathological delimiter scaling and private target404. The changed nonpublic deletion expectation follows approved11 §5; original receipts/CAS behavior is preserved. Tests use isolated SQLite and in-process HTTP with the real existing browser-auth core; no actual browser, TCP endpoint, remote D1, persistent DEV URL or operating publication is claimed. The prior43-test deletion bundle is reused as history; only its affected20 tests were rerun.


The final commit-time authority check compares database time at millisecond scale using julianday, avoiding integer-second truncation. Two subsecond session/tester expiry regressions were RED (writes incorrectly accepted) before the fix and GREEN after it. Earlier73-test evidence remains in community-edit-final-20261008.json as history; totals overlap and must not be added. No remote database clock/concurrency claim is made.


<a id="community-updated-at-v1-20261008"></a>
## updatedAt additive projection — community.updated-at.v1.20261008
Existing read paths, required headers, primary contractVersion and cursor scope stay unchanged. Post/Comment content and list items now include updatedAt:number|null (server UTC epoch milliseconds). This version identifies this additive field contract; it is not a new required HTTP header. Existing create/edit/delete JSON and request hashes/original receipt JSON are unchanged. Consumers with strict response schemas must include the new field.
For VISIBLE current revision, updatedAt comes from the exact current renderer CREATE/EDIT audit and original receipt/state-hash proof, canonical ISO timestamp converted to safe nonnegative milliseconds. At creation updatedAt=createdAt. Editing changes updatedAt for that committed revision; receipt replay does not touch the latest revision/time. An edited legacy own content may have an actual verified edit time without any known creation date; no createdAt backfill is performed.
HIDDEN/REVIEW_PENDING/AUTHOR_DELETED own projections return updatedAt:null because this read contract masks private content and does not verify/offer its revision timestamp. Null is not a claim that a private audit record is absent or that deletion time is unknown everywhere. For VISIBLE missing/corrupt provenance, read fails503; it does not return an invented time or silently null. Public nonvisible access remains404.
Invalid/noncanonical audit timestamps or times before known creation fail503 community_storage_invalid. Detail final authority/state/current timestamp proof recheck and list final page edit-time fence reject stale metadata409 community_content_stale. Lists still sort/cursor only immutable createdAt and BINARY id; updatedAt does not change paging position. No operating moderator restore timestamp source supplied. No visibility restoration or new terms gate.
Current author edit supports title/body only. Board/spoiler/wikiRefs/sourceBalanceVersion editing is not newly supplied. Creation descriptor/detailsHash and source binding remain unchanged; adding fields to existing strict edit command is400. Board move or wiki-clear/current-vs-fixed policies require explicit decision and a separate descriptor revision contract that preserves original creation fact. Current plain body edit remains available.
Validation: 9 new focused tests RED then GREEN; current community27files277PASS0FAIL0pending, game-api type/scoped lint exit0; QA/CommanderS1-20260922/community-update-time-final-20261008.json SHA256 8f351b508dda755b91fc3eec67ead6266e9838e71c018412dff94f3a355858f8. Independent static reviewer found no Critical/Important gap; no tests executed by reviewer. Synthetic Hono/sql.js only, no actual DEV browser/remoteD1/deployment/operating migration. No proof-retirement or physical purge implementation.