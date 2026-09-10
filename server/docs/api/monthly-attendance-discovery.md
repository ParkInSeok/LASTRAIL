# Current monthly attendance discovery

`GET /v1/attendance/monthly/current` requires existing bearer auth/admission,
rejects all query/body fields and sends no-store. It performs one DB snapshot
SELECT over immutable activity_definitions and the authenticated account's current
monthly_attendance_cycles binding, using DB time. No grant, row initialization,
policy creation, fallback legacy attendance or seed. No new migration.

```ts
type Discovery = {
 accepted: boolean;
 version: "monthly-attendance-discovery-v1";
 serverTime: number;
 cycleId: string; startAt: number; endAt: number; dayIndex: number;
 activityId: string | null; policyVersion: string | null;
 pinned: boolean; canAttend: boolean;
 status: "AVAILABLE" | "NOT_CONFIGURED" | "OUTSIDE_WINDOW" |
   "REWARD_BINDING_UNAVAILABLE" | "AMBIGUOUS_CONFIGURATION" |
   "PINNED_POLICY_UNAVAILABLE" | "INVALID_CONFIGURATION";
 errorCode: string | null;
 definitionStartAt?: number; definitionEndAt?: number;
 candidateCount?: number;
};
```

| HTTP/status | Meaning |
| --- | --- |
| 200 NOT_CONFIGURED | No monthly-kind definitions; legacy DISTINCT_DAYS never substitutes |
| 200 OUTSIDE_WINDOW | Monthly definitions exist but none covers DB now |
| 200 REWARD_BINDING_UNAVAILABLE | Exactly one current definition, missing elite binding; ID returned for blocked read UI, canAttend=false |
| 200 AVAILABLE | Exactly one current definition with configured binding; no claim/XP or completion implied |
| 409 AMBIGUOUS_CONFIGURATION | More than one active definition; no first-match selection. Only an existing account pin may be returned as identity, canAttend=false |
| 409 PINNED_POLICY_UNAVAILABLE | Current month already pinned but that policy is not active/available; identity retained, no switch |
| 503 INVALID_CONFIGURATION | Monthly definition fails strict policy parsing |

Auth400/401/403/etc retain standard auth envelopes; query/body400 contract_violation.
Database/transport failures are failures, never successful empty lists. Invalid
definitions are errors even if another valid definition exists. Candidate IDs are
not selected by lexical/creation order. On ambiguity, candidateCount is diagnostic.
No another-account cycle identity is returned.

The per-ID monthly read and mutation reject ambiguous active definitions too.
Mutation commit guards recheck the active definition count, so a second definition
inserted after discovery cannot race into a payout. A policy switch cannot create
another same-month ledger because the stable account+cycle binding is preserved.

Unity uses returned activityId/policyVersion/cycleId to read `/monthly`, then the
original strict auto-attend/recover-next protocol. All amounts and spending split
come from that read. Discovery is not a reusable grant authorization: refresh when
account or month changes, discard responses for the prior account/request context,
and retain original mutation requests only for receipt recovery. Old cycle requests
still409 without spending; old successful receipts still replay. Monthly KST04
rollover is independent from local midnight. Actual elite binding remains pending;
test AVAILABLE fixtures are not published/approved policies.

Six integration tests cover missing/legacy exclusion/outside/null binding, read-only
bytes, ambiguity with/without account pin, pinned-policy unavailability, DB-month
rollover and stale command, auth/query, and concurrent-definition commit rollback.
