# Customer Support Workflow

## Purpose

This document defines the launch-scope customer support workflow for LAST RAIL.

The goal is to let support staff answer player reports from server-owned records without opening repair or mutation tools too early.

## Ownership

- Server owns account truth, run truth, reward truth, gacha truth, story progress truth, unlock truth, and idempotency records.
- Admin owns read-only operational visibility and support triage surfaces.
- Unity owns player-facing presentation and local debug context, but does not own durable truth.
- Customer support uses Admin read-only data first. Manual correction is a later controlled workflow.

## Launch Scope

Launch customer support should support read-only investigation for:

- Account/auth issues
- Run completion reports
- Run reward preview or claim reports
- Offline reward preview or claim reports
- First clear reward reports
- Story progress or unlock reports
- Gacha result or guarantee reports
- Integrity-restricted reward reports

Launch customer support should not include:

- Manual currency grants
- Reward repair buttons
- Offline reward timestamp reset
- Story progress mutation
- Unlock mutation
- Gacha result reroll
- Localization publishing
- Narrative editing
- Economy correction

## Support Case Types

### Account/Auth

Common reports:

- Guest account cannot continue
- Session expired
- Account appears reset
- Player cannot access reward or story state

Read-only evidence:

- `accountId`
- account status
- created time
- auth session status
- latest successful authenticated request
- device/session diagnostic context if available

Do not expose:

- access tokens
- refresh tokens
- raw auth headers
- secret material

### Run Completion

Common reports:

- Run finished but result did not update
- Victory did not unlock next operation
- Failure reward looked wrong
- Duplicate submit returned unexpected response

Read-only evidence:

- `runId`
- `previewId`
- operation id
- risk tier
- outcome
- cleared waves
- finished time
- idempotency operation/key presence
- story progress mutation presence
- reward preview id

Support interpretation:

- Only `VICTORY` with 15 cleared waves advances ordinary story progress.
- Duplicate finish replay should not reapply story progress, first clear, or rewards.
- Lower operation clear should not regress higher stored progress.

### Run Reward

Common reports:

- Reward missing after run
- First clear bonus missing
- Same risk appears to reward differently
- Claim failed after preview

Read-only evidence:

- `rewardPreviewId`
- reward claim id
- reward transaction id
- economy transaction id
- currency ledger entries
- first clear state for `accountId + operationId + risk`
- integrity disposition
- idempotency replay state

Support interpretation:

- First clear is risk-scoped.
- First clear is consumed at accepted run finish preview generation time.
- If a preview exists but claim failed, recovery should focus on stored preview/claim state, not recalculating first clear eligibility.
- Unity-supplied reward amount or multiplier fields are not trusted.

### Offline Reward

Common reports:

- Offline reward not claimable
- Offline reward was smaller than expected
- Offline claim duplicated
- Player returned after long absence and expected uncapped reward

Read-only evidence:

- `offlineRewardClaimId`
- `claimStatus`
- `last_offline_reward_claim_at`
- `last_story_progress_id`
- `revision`
- preview status
- claimable seconds/hours
- storage cap hours
- minimum claim seconds
- max claimable hours
- reward transaction id
- economy transaction id
- currency ledger entries
- idempotency operation/key presence

Support interpretation:

- Offline rewards use server-stored story progress.
- Risk tier does not directly multiply offline rewards.
- 10 minutes or less than the launch minimum is not claimable.
- Empty claims should not advance `last_offline_reward_claim_at`.
- Duplicate replay should not grant rewards again or increment state revision.
- Existing accounts without an offline state row may use account creation time fallback until claim creates state.

### Story Progress/Unlock

Common reports:

- Next operation did not unlock
- Story progress appears stuck
- Offline reward multiplier did not increase
- Narrative or briefing appears missing

Read-only evidence:

- highest story progress id
- highest cleared operation id
- cleared operation ids
- unlocked operations
- last progress run id
- narrative version
- narrative/localization content status
- progression ledger entries

Support interpretation:

- Server durable story progress is the source of truth.
- Client-supplied story progress, unlocked operations, and narrative flags are not trusted.
- Offline reward multiplier uses server-stored highest story progress.
- Narrative version is diagnostic metadata, not reward or unlock input.

### Gacha/Recruitment

Common reports:

- Guarantee did not trigger
- Duplicate pull looked different
- Player questions recruitment result
- Banner unavailable or inactive

Read-only evidence:

- gacha batch id
- banner id/status
- pull count
- guarantee type
- pity counter state
- idempotency replay state
- reward/account item ledger entries
- sanitized selection reason if available

Support interpretation:

- Hard guarantee is represented by `guaranteeType`.
- Duplicate replay should return stored result, not rerun selection.
- Raw RNG material, seed preimage, per-candidate probabilities, and full weight lists are not support-facing data.
- Recruits should be described as rescued specialists, defectors, or signal responders, not purchased goods.

## Support Decision Matrix

| Symptom | Primary Evidence | Likely Outcome |
| --- | --- | --- |
| Reward preview exists, claim missing | reward preview + idempotency + claim records | Reopen/retry claim path candidate |
| Claim exists, ledger missing | reward transaction + economy transaction + ledger | Escalate as economy mutation mismatch |
| Duplicate claim report | idempotency record + claim id | Explain duplicate replay if no extra ledger write |
| Offline claim too small | story progress + cap + last claim time | Explain cap/minimum/server baseline |
| First clear missing | first clear record + run finish timing | Check consumed-at-finish policy |
| Story unlock missing | run final result + story progress + unlocks | Escalate if full victory with no progress/update |
| Gacha guarantee dispute | batch + guarantee type + pity state | Explain stored result/guarantee state |
| Integrity restriction | integrity disposition + ops error code | Escalate with read-only evidence |

## Admin Surface Candidates

### Account Support Summary

Candidate route:

- `/support/accounts/:accountId`

Read-only sections:

- account status
- recent runs
- run reward previews/claims
- offline reward state/claims
- story progress/unlocks
- gacha batches
- idempotency records
- integrity restrictions
- recent ops error logs

### Run Support Context

Candidate fields:

- run id
- operation id
- risk tier
- outcome
- cleared waves
- reward preview id
- reward claim id
- first clear consumed status
- story progress mutation id
- idempotency replay status
- integrity disposition

### Offline Reward Support Context

Candidate fields:

- offline reward claim id
- claim status
- last claim baseline
- state revision
- story progress id
- claimable duration
- storage cap
- reward transaction id
- economy transaction id
- duplicate replay status

### Gacha Support Context

Candidate fields:

- gacha batch id
- banner id
- pull count
- guarantee type
- pity counters
- sanitized selection reason
- item/operator ledger references
- duplicate replay status

## Permissions

Launch recommended permissions:

- `SUPPORT_READ`: read-only support case context
- `PLAYERS_READ`: existing player lookup
- `RUNS_READ`: existing run visibility
- `STORY_PROGRESS_READ`: story progress visibility
- future `OFFLINE_REWARD_READ`: offline reward state/claim visibility
- future `GACHA_READ`: gacha batch/pity visibility

Launch should not grant support users:

- reward mutation
- economy mutation
- story progress mutation
- unlock mutation
- offline reward timestamp reset
- gacha reroll or repair

## Player-Facing Response Guidelines

Use:

- "서버 기록 기준으로 확인했습니다."
- "수령 가능 시간과 보관 한도 기준으로 계산되었습니다."
- "중복 요청은 같은 처리 결과를 다시 보여주는 방식입니다."
- "첫 클리어 보상은 완료 처리 시점 기준으로 확인됩니다."

Avoid:

- raw internal ids unless needed for support ticket reference
- raw probability or weight details
- RNG seed or seed preimage
- token/session details
- exact exploit-sensitive formulas beyond public policy language
- language implying characters or rescued people are purchased goods

## Ops Error Code Candidates

Support-facing labels should preserve stable backend codes and add Korean labels in Admin.

- `reward_plan_mismatch`
- `reward_multiplier_missing`
- `reward_plan_unavailable`
- `reward_integrity_restricted`
- `offline_reward_not_claimable`
- `offline_reward_state_conflict`
- `offline_reward_claim_conflict`
- `offline_reward_plan_mismatch`
- `economy_mutation_conflict`
- `story_progress_state_conflict`
- `narrative_content_missing`
- `narrative_version_mismatch`
- `gacha_temporarily_unavailable`
- `banner_inactive`
- `invalid_pull_count`
- `duplicate_replay`

## Rollout Order

1. Define CS read-only workflow and terminology.
2. Add Server support read model contract for account support summary.
3. Add Admin support skeleton with read-only placeholders.
4. Connect Admin to read-only Server support endpoints.
5. Add ops error log correlation for support case types.
6. Define audited repair workflow separately.
7. Only then consider controlled mutation tools.

## Open Decisions

- Whether support should have a dedicated `/support` Admin section or be integrated into player/run detail pages first.
- Whether `SUPPORT_READ` should be a new permission or composed from existing read permissions.
- How long support timelines and recent claim/run/gacha history should be retained.
- Whether support ticket ids should be stored in Admin audit logs.
- Which repair flows require OWNER approval.
- Whether player-facing explanations should be generated from templates or maintained manually.
