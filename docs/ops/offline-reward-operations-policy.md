# Offline Reward Operations Policy

## Purpose

This document defines launch operations policy for LAST RAIL offline rewards.

The Game API already owns preview and claim calculation. This policy defines how support, Admin, and server operations should interpret edge cases without opening repair tools too early.

## Source of Truth

Server-owned records are authoritative:

- `account_story_progress.highest_story_progress_id`
- `account_offline_reward_state.last_offline_reward_claim_at`
- `account_offline_reward_state.revision`
- offline reward claim response records
- `reward_transactions`
- `economy_transactions`
- `currency_ledger`
- `idempotency_records`
- future `ops_error_logs`

Unity local state, screenshots, client timers, client-supplied story progress, risk tier, offline seconds, reward amounts, currency grants, item grants, and economy grants are not source of truth.

## Launch Rules

- Offline rewards use server-stored story progress.
- Risk I-V does not directly multiply offline rewards.
- Claimable time is capped by offline storage cap.
- Rewards below minimum claim time are not claimable.
- Empty claims must not advance `last_offline_reward_claim_at`.
- Duplicate replay must not grant rewards again.
- Duplicate replay must not increment offline state revision.
- Claim success must create auditable reward/economy/ledger references.
- Admin launch surfaces are read-only.
- Manual repair and compensation remain disabled.

## Long Absence And Cap

When a player is away longer than the storage cap:

- The server should calculate rewards only up to the applicable cap.
- Time beyond the cap is not claimable by default.
- Support should describe this as storage capacity, not lost server time.

Player-facing wording:

> 오프라인 보상은 서버에 저장된 마지막 수령 시각과 현재 스토리 진행도의 보관 한도를 기준으로 계산됩니다. 보관 한도를 초과한 시간은 기본 수령량에 포함되지 않을 수 있습니다.

Escalate only when:

- The stored cap does not match story progress policy.
- Claim result exceeds cap.
- Claim result is lower than cap without an explainable minimum, state, or policy reason.

## Minimum Claim Time

Launch policy:

- Offline reward below the minimum claim seconds is not claimable.
- The recommended player-facing state is `offline_reward_not_claimable`.
- The server should return rejection without consuming the claim baseline.

Player-facing wording:

> 현재 서버 기록 기준으로 수령 가능한 오프라인 보상이 없습니다. 오프라인 보상은 최소 누적 시간과 보관 한도 기준을 함께 적용합니다.

Support should not escalate when:

- The only reason is that elapsed offline time is below the minimum.
- No ledger or state mutation occurred.

Escalate when:

- The claim baseline advanced after a not-claimable result.
- The player received a success response without ledger references.

## Preview And Claim Mismatch

Preview is informational. Claim is the committed server transaction.

Expected differences:

- Time can advance between preview and claim.
- Story progress may advance before claim and change the calculation input.
- Offline reward state may change if another valid claim was committed first.

Unexpected mismatch candidates:

- Claim uses a story progress id not present in server state.
- Claim uses a baseline older than `last_offline_reward_claim_at`.
- Claim succeeds but reward/economy ledger is missing.
- Claim reward lines conflict with stored transaction payload.
- Duplicate replay returns a different claim id or references.

Operational response:

- Treat expected time drift as `info`.
- Treat missing ledger or duplicate mismatch as `high_risk`.
- Treat story policy missing as `needs_review` or `high_risk` depending on impact.
- Do not manually correct from launch Admin.

## Clock Tamper Policy

The server should not trust client clocks for offline reward duration.

Clock tamper suspicion candidates:

- Client diagnostic time is far ahead or behind server time.
- Repeated requests show impossible local elapsed time.
- Claim attempt cadence conflicts with server `last_offline_reward_claim_at`.
- Multiple devices produce contradictory local offline durations.

Launch handling:

- Server-calculated time remains authoritative.
- Support should not accuse the player.
- Use neutral wording and escalate only with server-side evidence.

Player-facing wording:

> 보상 수령 시간은 서버 기록을 기준으로 확인됩니다. 기기 시간과 서버 기록이 다를 수 있어 저장된 수령 기준으로 다시 확인하겠습니다.

Do not expose:

- raw client timestamps if sensitive
- fraud scoring internals
- device fingerprint internals
- anti-abuse thresholds

## State Revision Conflict

`account_offline_reward_state.revision` exists to help detect concurrent or inconsistent updates.

Conflict candidates:

- Revision increments without a matching offline reward claim.
- Two claims appear to commit from the same baseline.
- Duplicate replay increments revision.
- Empty claim increments revision.
- Latest reward transaction does not match state update.

Operational response:

- Mark as `offline_state_conflict`.
- Severity should default to `high_risk`.
- Support should attach read-only evidence.
- Manual timestamp reset is disabled at launch.

## No-Row Fallback

Existing accounts may not have an offline reward state row.

Launch policy:

- Preview may use `accounts.created_at` lazy fallback.
- Claim may lazily prepare/upsert state during a successful claim transaction.
- Admin should show the fallback source clearly.
- Fallback itself is not an error.

Escalate when:

- A new guest account lacks state immediately after creation.
- Fallback is used after a successful claim should have created state.
- Fallback produces a claim inconsistent with account creation time.

## Duplicate Replay

Duplicate replay is expected when the same idempotency key is submitted again.

Expected behavior:

- Return stored claim/reference ids.
- Do not recalculate reward.
- Do not write additional ledger entries.
- Do not increment state revision.
- Reward lines may be empty in duplicate replay response.

Player-facing wording:

> 같은 수령 요청이 중복으로 들어온 경우에는 서버가 기존 처리 결과를 다시 확인합니다. 이 경우 보상이 한 번 더 지급되거나 수령 기준 시간이 다시 변경되지 않습니다.

Escalate when:

- Duplicate replay returns a different claim id.
- Duplicate replay creates additional ledger entries.
- Duplicate replay changes offline reward state.

## Reward Line Policy

Launch allowed recurring offline reward lines:

- `cur_credits`
- `cur_machine_parts`
- `cur_tactical_data`
- `cur_operator_exp`

Launch blocked default offline reward lines:

- `cur_special_core`
- `cur_command_ticket`
- `cur_command_ticket_fragment`

Support interpretation:

- Blocked default rewards should not appear in normal offline reward claims.
- If blocked rewards appear, treat as `offline_reward_plan_mismatch` or `economy_mutation_conflict`.
- Do not promise rare currency or ticket compensation from support.

## Admin Read-Only Fields

Admin should prioritize these fields:

- story progress id
- preview status
- claim status
- offline reward claim id
- last claim baseline
- state source
- state revision
- claimable seconds/hours
- storage cap hours
- minimum claim seconds
- max claimable hours
- reward transaction id
- economy transaction id
- account mutation id
- ledger presence status
- idempotency replay status
- error code
- support severity/category recommendation

Admin should not expose:

- repair buttons
- manual grant controls
- timestamp reset controls
- raw request body
- token/session secrets
- raw RNG
- raw multiplier bps
- internal exploit-sensitive formulas

## Ops Error Code Mapping

Expected/no-escalation:

- `offline_reward_not_claimable`: use `info` when caused only by minimum time.
- `duplicate_replay`: use `info` when references match stored claim.

Needs review:

- `offline_story_progress_missing`
- `offline_reward_policy_missing`
- `offline_reward_policy_invalid`

High risk:

- `offline_reward_state_conflict`
- `offline_reward_clock_tamper`
- `offline_reward_claim_conflict`
- `offline_reward_plan_mismatch`
- `economy_mutation_conflict`

## Support Decision Matrix

| Situation | Severity | Category | Launch Action |
| --- | --- | --- | --- |
| Below minimum claim time | `info` | `expected_behavior` | Respond with template |
| Capped long absence | `info` | `expected_behavior` | Explain cap |
| Duplicate replay matches stored claim | `info` | `expected_behavior` | Explain duplicate replay |
| State row missing for legacy account before first claim | `info` | `expected_behavior` | Explain fallback if needed |
| Successful claim but ledger missing | `high_risk` | `reward_claim_missing_ledger` | Escalate to owner review |
| Empty claim advanced baseline | `high_risk` | `offline_state_conflict` | Attach evidence, no repair |
| Duplicate replay wrote extra ledger | `high_risk` | `offline_claim_duplicate_mismatch` | Attach evidence, no retry advice |
| Unknown stored story progress | `needs_review` | `offline_state_conflict` | Escalate to operator review |
| Clock tamper suspected | `high_risk` | `offline_state_conflict` | Escalate with neutral wording |

## Rollout Order

1. Keep preview/claim support read-only.
2. Show fallback source and state revision in Admin.
3. Add ops error logs for real state conflicts.
4. Add support summary integration for recent offline errors.
5. Define repair preview separately.
6. Only then consider audited timestamp correction.

## Open Decisions

- Whether long absence cap overflow should ever be partially restored during special incidents.
- Whether clock tamper thresholds should be documented internally or kept in security-only docs.
- Whether no-row fallback should expire after a migration backfill.
- Whether offline reward state conflicts require OWNER review or a dedicated integrity queue.
- Whether `cur_operator_exp` should remain in offline rewards after economy balancing.
