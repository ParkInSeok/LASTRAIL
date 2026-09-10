# S08 monthly sequential attendance

Local implementation only, migration0032. Existing DISTINCT_DAYS daily/milestone
service remains a legacy contract. New monthly routes reuse its immutable activity
definition registry, activity/reward receipts, economy transactions, real currency
and item ledgers, and FREE/PAID crystal wallet planning. No definition is seeded.

## Policy and unresolved binding

Definition JSON: `{kind:"MONTHLY_ATTENDANCE",version:"monthly-attendance-v1",
startAt:number,endAt:number,eliteTicketItemId:string|null}`. Times must be KST04
calendar-month starts. Definitions are immutable; ID is the policyVersion.
The server defines the approved28 cells, not client input: each week's50 FREE,
credits, parts, EXP, data, credits, milestone; credit per cell1000/1500/2000/2500,
parts50/75/100/125, EXP100/150/200/250, data25/40/55/70. Milestones100/150/200 FREE,
then one elite ticket. Totals650 FREE,14000credits,350parts,700EXP,190data,one ticket.
Credits/parts/EXP/data use account_currencies and currency_ledger, not account_items.

**Actual elite-ticket identifier and consumption policy are not approved/bound.**
Null exposes elite_ticket_binding_missing and every mutation fails503
attendance_reward_binding_unavailable. No partial27-cell launch, ordinary ticket
substitution or complete claim. cur_command_ticket as binding is rejected. Test
fixtures explicitly use TEST_ONLY_ELITE_TICKET to test generic atomic item grants;
this is not a production entitlement/usable elite ticket. Do not publish a policy
until the actual entitlement producer/consumer and pinned version contract exist.
See [elite-ticket evidence and proposal](attendance-elite-ticket-review.md).

## API

Bearer authentication and existing admission, no query, strict bounded JSON,
no-store for success/errors. Prefix `/v1/attendance/:activityId`:

| Route | Request | Behavior |
| --- | --- | --- |
| GET /monthly | No body | Read-only cycle/cells/recovery quote; never grants/initializes |
| POST /auto-attend | policyVersion, cycleId, expectedRevision | Automatic client boot/lobby mutation before confirmation popup, no manual claim gating |
| POST /recover-next | Same plus walletPolicyVersion, confirmedPaid | One next sequential cell,100crystals, requires today's auto-attend committed first |

POST Idempotency-Key:1–128 printable non-space ASCII. cycleId `YYYY-MM` and policy
ID come from GET; expectedRevision is nonnegative safe integer below MAX_SAFE_INTEGER.
confirmedPaid is exact quoted PAID spend0..100; a PAID remainder requires explicit
user confirmation in Unity. Zero prevents silently spending any PAID. If split
changes, return409 attendance_paid_confirmation_changed and show a refreshed quote.
No caller dayIndex/cell/XP/time/reward inputs. Auto-attend is a separate successful
mutation before obtaining the recovery quote; recovery never implicitly confirms
spending a balance that changed with today's attendance.

GET includes version, activityId/policyVersion, cycleId/startAt/endAt/dayIndex,
serverTime, active, revision, completedCells, complete (only28), todayAttended,
unavailableReason,28 cells with reward kind/ID/quantity/unavailableReason and
optional `{cell,source,dayIndex,claimId}` receipt. Recovery includes price100,
maxPerCycle7, used, recoverableCount, todayAttendanceRequired and quote (null if
wallet unavailable) with walletPolicyVersion/currencyId/freeSpent/paidSpent/
sufficientFunds. A count is eligibility, not a promise of wallet/binding readiness.

Success includes version/policyVersion/cycleId/kind, new revision, claimId,
rewardTransactionId/accountMutationId/economyTransactionId, cell (null after28),
sourceDayIndex, completedCells, rewardLines, wallet, serverClaimedAt, refreshRequired.
Wallet is null when no crystal action is needed. Natural days29–31 may record a
visit with no cell/reward. They never grant another elite ticket.

400 contract_violation;401 auth;503 activity_not_configured,
monthly_attendance_policy_invalid, attendance_reward_binding_unavailable or wallet
policy failures. Main409s: attendance_cycle_changed, attendance_cycle_policy_changed,
activity_outside_window, attendance_revision_conflict, attendance_today_required,
attendance_not_recoverable, attendance_paid_confirmation_changed,
crystal_insufficient_balance, crystal_policy_changed, attendance_state_changed,
idempotency_conflict. Preserve draft/body/key on unknown outcomes; never infer
failure from a lost response. Retry original exact request to retrieve its receipt.

## Calendar and atomicity

Cycle changes on month1 KST04, not midnight or every28days. Natural visit grants
next cell, never the calendar-day number. Recovery uses earliest unused fully
elapsed service day after registration: ceil((registeredAt-cycleStart)/day), so
a partially elapsed registration day is not sold as a fully missed day. No dates
before registration/current/future days. Count=min(unused missed days,7-used,28-filled).
One stable account+month ledger across activity IDs prevents policy-version double
claim; after first mutation another policy ID cannot take over that cycle.

Account mutation sequence precedes eligibility/balance reads. One batch guards
DB service-day/time window, registration, cycle policy/revision, debits existing
wallet lots before any same-command reward, grants, writes immutable receipts and
updates visits/cells. Failure rolls back all writes. Duplicate natural visits
return the original response; each new successful request key stores an alias to
that receipt so day/month rollover does not break response-loss recovery. Alias
storage grants nothing and does not change attendance revision.

Successful original-body/key replay wins before current cycle/eligibility checks.
Same-month day boundary retries re-evaluate the new day; month boundary rejects
the stale cycle without spending. Competing natural/recovery actions share revision
and cell/day uniqueness. No login quest/pass XP, shop quota, offline time or login
bonus is awarded by recovery. Natural visits provide a durable distinct-day source
for the future shared quest producer; this slice does not implement that producer.

Tests cover read-only and missing binding, exact cell economy and real wallets,
month lengths, no skip/extra reward, registration,7 cap, FREE-first/PAID confirmation,
no reward pre-spend, stale cycle/exact and alias replay, DB boundary retries,
concurrent commands, rollback, auth/account isolation and strict inputs.

Local verification2026-09-10:80 test files/527 tests passed (20.28s), including
11 monthly tests. TypeScript/source ESLint passed. Independent review caught
currency-wallet routing and new-key duplicate replay gaps; both were fixed and
covered by real-wallet and cross-month alias tests before this full run.
