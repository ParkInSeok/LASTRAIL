# Community moderation grants v1

Contract: `community.moderation-grants.v1.20261008`. Source: 10_LIVEOPS_NOTICES.md#community-moderation-grant-approved-20261008. Permission: `COMMUNITY_MODERATE`, independent of `SUPPORT_TICKET_PROCESS`.

## Available API

GET and POST `/v1/admin/community/moderator-grants/:principalId`, registered in the actual Admin app. Admin authentication and current ACTIVE OWNER are required for both. Existing Access authentication is reused; member WEB cookies do not authenticate these routes. DEV and PRD bind their own DB; no client environment selector. Required exact header: `X-Community-Moderation-Grant-Version: community.moderation-grants.v1.20261008`. No query parameters; principal ID is ASCII alphanumeric, underscore or hyphen, 1–128 characters. All responses use `Cache-Control: no-store`.

GET requires an empty body and returns `{contractVersion, environment, state}`. State contains `principalId`, nullable `displayName`, current `role`/`status`, `permission`, `granted`, monotonic `revision`, nullable `lastChange: {actorId, at, reason, outcome, environment}`. Final current OWNER and target checks reject stale access/state.

POST requires JSON and printable ASCII `Idempotency-Key` of 1–128 characters. Strict body:

```json
{"action":"GRANT","permission":"COMMUNITY_MODERATE","expectedRevision":0,"reason":"Approved moderation duty"}
```

`action` is GRANT or REVOKE; revision is a nonnegative safe integer; reason is trimmed 1–500 characters. Shared bounded body reader limits the body to 18,000 bytes. Receipt: `{contractVersion,environment,result:{principalId,permission,granted,revision,outcome}}`; outcome APPLIED or NO_CHANGE. Changed state increments revision once; NO_CHANGE does not. Matching original key/payload replays its original receipt without restoring the current grant; changed payload conflicts. Current OWNER authority remains required for replay.

Only ACTIVE OWNER/OPERATOR targets can receive grants. OWNER self grant/revoke and last processor revoke are permitted. Role changes, disablement and deletion invalidate grants; reactivation does not restore them. Deletion/recreation preserves revision tombstones. Grant, CAS revision, immutable command receipt and successful audit commit in one DB batch with trigger checks of current owner/target. Inquiry grant and OWNER role alone confer no community moderation permission. `CommunityModerationGrantService.canModerate` is the explicit current-authority boundary for future moderation commands, not proof that those commands are supplied.

Errors return `{error}`: authentication 401; `ADMIN_PERMISSION_DENIED` 403; `community_grant_target_not_found` 404; malformed body/path/query 400 `contract_violation`; missing/invalid key 400 `idempotency_key_required`; wrong version 409 `community_grant_version_conflict`; stale revision 409 `community_grant_revision_conflict`; ineligible target 409 `community_grant_target_ineligible`; changed original payload 409 `idempotency_conflict`; unavailable DB/source/invalid stored receipt 503 `community_grant_unavailable`. After revision conflict read current state, preserve an uncertain command's original key for recovery, and use a new key only for an explicitly new command.

## Schema, compatibility and evidence

Empty migration `0117_community_moderation_grants.sql` follows 0001–0116. No grant seeds or operating migration were run. Community audit actions CHANGE/REJECT/INVALIDATE are OWNER-only in general audit reads. Existing support grant facade reuses the same scoped owner; its permission, contract and hash/receipt semantics remain separate.

Final local QA: `D:/Project/LASTRAIL/QA/CommanderS1-20260922/community-moderation-grants-final-20261008.json`, SHA256 `362702fae96aba52d3a44b8dac1319f929ddc7f23097716dda80a7a1318b8e96`, 31 PASS / 0 FAIL / 0 pending (12 new, 19 existing support). Common/Admin/Game type checks and changed-source ESLint exit 0. Admin composite include added only 26 actual imported dependencies after TS6307; no type checking disabled. Static review found no Critical/Important issue and executed no tests.

Tests use actual Admin handlers plus explicitly LOCAL_HEADER synthetic principals and disposable migrated SQL. They cover separate permissions, WEB denial, self/last revoke, NO_CHANGE/replay, role/status/delete invalidation, concurrent CAS, late authority loss and private audit filtering. This is not actual Access JWT/TCP/browser/remote-D1 proof. No standing DEV URL is supplied; consumer can use the registered handler in an isolated harness. No real grants, deployment or UI work.

## Ready consumer scope and remaining work

A-03 may implement non-screen typed grant management with current-read, reason/revision/key lifetime, recovery and private OWNER errors using this exact contract. Actual Access/BFF environment validation is separate. Approved design remains required before screen connection. Report review/processing and content hide/eligible restore API are not supplied by this grant unit; those remain in existing S16. No sanction type, period, permanent ban or game penalty was invented.

## Subsequent approved API supplied — 2026-10-08
The report review and content hide/eligible restore boundary is now supplied under the separate community.moderation.v1.20261008 contract. See community-moderation-v1.md and Server/WAIT#s16-moderation-api-result-20261008 for347PASS/type/lint and exact remaining scope. The earlier grant-only remaining-work paragraph describes its historical completion boundary; it is not a current blocker for the supplied moderation unit. Grant v1 fields and permission rules remain unchanged. No live migration/grants/deploy/UI were performed.
