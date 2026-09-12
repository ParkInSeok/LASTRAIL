# Admin player activity summary v1

`GET /v1/admin/players/:accountId/activity-summary`

Uses existing Admin authentication and `SUPPORT_READ` (OWNER/OPERATOR/VIEWER).
It does not impersonate a game account or accept game bearer tokens as Admin
authority. No query parameters or request body are accepted. Missing account is
404; invalid input is400. Responses use `Cache-Control: no-store`.

Top-level fields:

| Field | Contract |
| --- | --- |
| version | `admin-player-activities-v1` |
| accountId | Exact account selected by the authenticated Admin caller |
| observedAt | Server timestamp in milliseconds |
| permission / permissionMode | `SUPPORT_READ` / `ROLE_PERMISSION` |
| readOnly | true |
| consistency | `INDEPENDENT_LIVE_READS`; not a transaction-wide snapshot |
| shop / monthlyAttendance / pass / ordinaryQuests / mail | Sections below |

Every section has `{availability, reason, data}`. `AVAILABLE` has `reason:null`
and an object in `data`; its known zero, false, empty arrays and nullable fields
must remain distinct. `UNCONFIGURED` means the current policy is absent and has
`data:null`. `UNAVAILABLE` means invalid configuration, conflicting policy, or a
failed source read and also has `data:null`. A failed section does not erase
unaffected sections. Unknown errors use `source_read_failed`, never raw SQL/error
text. An unavailable section must not be shown as an empty successful result.

## Section data

`shop`: policyVersion, currencyId, wallet{freeBalance,paidBalance,totalBalance},
dayStart, nextResetAt, ticketOffersConfigured, dailyRemaining,
ticketUnavailableReason, purchaseConfigured, configuredProductCount,
purchaseVerificationAvailable, purchaseVerificationReason, cashPrice,
cashPriceSource, materialShopAvailability. Daily remaining is0 or1 only when the
existing launch100-crystal exchange is configured; otherwise it isnull.
purchaseVerificationAvailable isnull with GAME_RUNTIME_NOT_OBSERVED because this
Admin read cannot establish the game Worker's live verifier. cashPrice isnull,
STORE_REQUIRED; no price is fabricated. The material shop remains UNCONFIGURED.

`monthlyAttendance`: activityId, policyVersion, cycleId, active, canAttend,
unavailableReason, revision, completedCells, todayAttended, recoveryUsed,
recoverableCount, cells[]. Each cell exposes only cell, claimed, source,
dayIndex, unavailableReason. No claim receipt or reward payload is copied. The
existing monthly discovery/pinning/conflict rules are reused. A valid policy
with missing elite-ticket binding can be read but has canAttend=false and
elite_ticket_binding_missing; this is not absent configuration or a zero reward.

`pass`: current PASS50 policyId/startAt/endAt, earnedXp, xp(capped5000), level
(capped50), premium, purchaseConfigured, cosmeticBindingsComplete,
progressSyncPending, progressSource. Premium uses the existing account-bound,
non-revoked verified-order entitlement relation without returning order IDs.
XP comes from persisted awards; no synchronization or mutation is invoked.

`ordinaryQuests`: current QUESTS policyId/startAt/endAt, limit100, hasMore,
progressSyncPending, progressSource, items[]. Items contain awardId, objectiveId,
periodId, earnedAt, periodEnd and status CLAIMED/MAILED/MAIL_PENDING/EARNED.
This is a summary of recorded awards, not an assertion that all original events
have already been synchronized. progressSource is
PERSISTED_AWARDS_NO_SYNC_ON_ADMIN_READ for both pass and quests.

`mail`: limit100, hasMore, claimabilitySource
RECORDED_RIGHT_AND_EXPIRY_NOT_MUTATION_PREFLIGHT, items[]. Each has awardId,
policyId, objectiveId, sentAt, expiresAt, and CLAIMED/EXPIRED/CLAIMABLE. The query
requires matching ownership on both the mail and award records. Historical mail
can be available even when no current quest policy exists. CLAIMABLE describes
the recorded right and expiry; it is not a transaction preflight or permission
to grant. Neither source response JSON nor raw rewards are exposed.

Quest and mail summaries return the latest100 (timestamp descending, stable ID
tie order), probing101 to set hasMore. They are intentionally not exhaustive
history endpoints and do not supply a pagination cursor. No total is inferred
from the truncated array length.

## Implementation and verification

Projection: `apps/admin-api/src/player-activities.ts`; route registration in
`apps/admin-api/src/index.ts`. Read-only policy helpers are reused from game-api;
Admin's composite TypeScript project explicitly lists their transitive source
dependencies, following the existing async-worker project pattern. This does not
mount game routes or grant mutation permissions.

Actual migrated SQLite regression covers existing Admin authentication, account
existence, query/body refusal, no-store, missing configuration versus empty mail,
known0 balances/XP, owner-isolated records, contradictory mail/award ownership,
100-row caps, absent source tables, invalid policies, and monthly missing elite
binding. SELECT-only interception plus before/after serialized-DB SHA256 verifies
that reads do not synchronize or write. Raw payload canaries remain absent.

The tests export four uniquely named actual response fixtures under
`audit-results/admin-player-activities`: unconfigured, configured-zero-and-owned-
records, unavailable-source, monthly-incomplete-binding. These are synthetic
SQLite account data for UI regression, not live account observations. Fixture
filenames are never overwritten by later test runs.

Admin must verify version and exact accountId before showing sections. Preserve
availability, progressSyncPending, hasMore and unknown null values. Endpoint
completion does not establish browser responsiveness or a remote deployment;
those are the Admin caller's next integration/QA work.
