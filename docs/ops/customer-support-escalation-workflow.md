# Customer Support Escalation Workflow

## Purpose

This document defines when LAST RAIL customer support should resolve a report with a safe response, escalate it for review, or block action until better evidence exists.

Launch support remains read-only. Escalation creates an evidence package and review state, not a repair or mutation.

## Severity

| Severity | Korean Label | Meaning | Launch Action |
| --- | --- | --- | --- |
| `info` | 안내 | Server records show expected behavior. | Respond with an approved template. |
| `needs_review` | 검토 필요 | Records are incomplete or may disagree. | Escalate to operator review. |
| `high_risk` | 고위험 | Economy, reward, integrity, or account ownership impact is possible. | Escalate to owner or senior operator review. |
| `blocked` | 처리 보류 | Support cannot safely decide from available records. | Do not promise repair; request more context or wait for tooling. |

## Escalation Categories

| Category | Korean Label | Default Severity |
| --- | --- | --- |
| `reward_claim_missing_ledger` | 보상 수령 장부 누락 | `high_risk` |
| `reward_preview_without_claim` | 보상 미리보기 후 수령 누락 | `needs_review` |
| `offline_state_conflict` | 오프라인 보상 상태 충돌 | `high_risk` |
| `offline_claim_duplicate_mismatch` | 오프라인 중복 수령 불일치 | `high_risk` |
| `story_unlock_missing` | 스토리 해금 누락 | `needs_review` |
| `gacha_guarantee_conflict` | 모집 보장 상태 충돌 | `high_risk` |
| `integrity_restricted_reward` | 무결성 보상 제한 | `high_risk` |
| `account_identity_conflict` | 계정 식별 충돌 | `blocked` |
| `narrative_content_missing` | 내러티브 콘텐츠 누락 | `needs_review` |
| `expected_behavior` | 정상 동작 안내 | `info` |

## Evidence Package

Every escalation should collect a compact evidence package.

Required fields:

```text
caseType:
severity:
category:
accountId:
primaryReferenceId:
observedStatus:
idempotencyStatus:
serverEvidence:
playerFacingSummary:
requestedAction:
```

Optional reference fields:

- `runId`
- `rewardPreviewId`
- `rewardClaimId`
- `offlineRewardClaimId`
- `gachaBatchId`
- `economyTransactionId`
- `rewardTransactionId`
- `accountMutationId`
- `currencyLedgerEntryIds`
- `storyProgressId`
- `lastProgressRunId`
- `integrityEventId`
- `opsErrorLogId`
- `supportTicketId`

Do not include:

- access tokens
- refresh tokens
- raw auth headers
- raw request bodies
- raw RNG material
- RNG seed or seed preimage
- per-candidate probability
- full weight lists
- raw multiplier bps
- internal reward calculation details
- localized dialogue full text

## Launch Actions

Allowed launch actions:

- `respond_with_template`
- `request_more_player_context`
- `escalate_to_operator_review`
- `escalate_to_owner_review`
- `mark_as_known_issue_candidate`
- `attach_read_only_evidence`

Not allowed at launch:

- `manual_reward_grant`
- `currency_correction`
- `offline_reward_timestamp_reset`
- `story_progress_mutation`
- `unlock_mutation`
- `gacha_reroll`
- `reward_repair`
- `economy_repair`
- `narrative_editing`
- `localization_publishing`

## Case Rules

### Expected Behavior

Use severity `info` when server records show expected behavior.

Examples:

- Offline reward is below minimum claim time.
- Offline reward is capped by storage limit.
- Duplicate replay returned the stored result.
- First clear reward was already consumed for the same operation and risk.
- Lower operation clear did not regress higher story progress.

Action:

- Use an approved response template.
- Do not escalate unless the player provides new evidence.

### Reward Claim Missing Ledger

Use category `reward_claim_missing_ledger` when a successful reward claim exists but currency or economy ledger evidence is missing.

Required evidence:

- reward claim id
- reward transaction id
- economy transaction id
- expected ledger presence status
- idempotency status
- affected currency codes without raw internal formulas

Action:

- Escalate to owner or senior operator review.
- Do not issue manual compensation from launch support tools.

### Reward Preview Without Claim

Use category `reward_preview_without_claim` when a reward preview exists but no claim record exists.

Required evidence:

- reward preview id
- run id
- preview status
- claim status if any
- idempotency status
- integrity disposition

Action:

- Escalate to operator review if the player cannot retry claim safely.
- If a supported retry path exists, guide the player to retry.

### Offline State Conflict

Use category `offline_state_conflict` when offline reward state and claim records disagree.

Examples:

- State revision changed without a matching claim.
- `last_offline_reward_claim_at` moved forward after an empty claim.
- Stored story progress id is unknown.
- Claim uses a baseline inconsistent with state.

Required evidence:

- offline reward state source
- last claim baseline
- state revision
- offline reward claim id if any
- reward/economy transaction ids
- idempotency status

Action:

- Escalate to owner review.
- Do not reset offline state at launch.

### Offline Duplicate Mismatch

Use category `offline_claim_duplicate_mismatch` when duplicate replay evidence does not match the stored claim.

Required evidence:

- idempotency operation
- duplicate replay status
- offline reward claim id
- ledger write count
- state revision before/after if visible

Action:

- Escalate as high risk.
- Do not advise the player to repeatedly retry if writes appear inconsistent.

### Story Unlock Missing

Use category `story_unlock_missing` when a full victory should advance story progress but no unlock/progress evidence exists.

Required evidence:

- run id
- operation id
- outcome
- cleared waves
- current story progress id
- unlocked operations
- progression ledger presence

Action:

- Escalate to operator review.
- Do not mutate story progress from support at launch.

### Gacha Guarantee Conflict

Use category `gacha_guarantee_conflict` when guarantee type and pity state appear inconsistent.

Required evidence:

- gacha batch id
- banner id
- pull count
- guarantee type
- pity counter status
- duplicate replay status
- ledger result presence

Action:

- Escalate to owner or senior operator review.
- Do not expose raw probability, RNG, seed, or full weight lists.
- Do not reroll or replace recruitment result at launch.

### Integrity Restricted Reward

Use category `integrity_restricted_reward` when a reward or progress action is blocked or limited by integrity disposition.

Required evidence:

- integrity disposition
- affected run/reward/offline/gacha reference ids
- ops error code if available
- whether restriction is soft anomaly or hard restriction

Action:

- Escalate to owner or integrity review.
- Use neutral player-facing language.

### Account Identity Conflict

Use category `account_identity_conflict` when support cannot safely determine account ownership.

Examples:

- Player claims progress belongs to another account.
- Guest continuation produced multiple candidate accounts.
- Platform proof and server account status disagree.

Required evidence:

- account ids involved
- public player display names if available
- account status
- created/last active timestamps
- non-secret auth identity references

Action:

- Mark as `blocked`.
- Request more account ownership context.
- Do not merge accounts or move rewards at launch.

### Narrative Content Missing

Use category `narrative_content_missing` when story progress exists but briefing, callout, codex, or localization content is missing.

Required evidence:

- mission id
- story progress id
- narrative version
- content status
- localization status
- fixture status

Action:

- Escalate to content/localization review.
- Do not treat narrative content status as reward or unlock truth.

## Admin Read-Only Fields

Future Admin support surfaces should expose:

- case type
- severity
- escalation category
- recommended launch action
- support permission
- primary reference id
- related reference ids
- idempotency status
- ledger presence status
- integrity disposition
- safe player-facing summary availability
- mutation controls disabled status

Suggested permission:

- `SUPPORT_READ`

Future permissions:

- `SUPPORT_ESCALATION_READ`
- `SUPPORT_ESCALATION_WRITE`
- `SUPPORT_REPAIR_APPROVE`

Only `SUPPORT_READ` is in launch read-only scope.

## Player-Facing Escalation Language

Use:

- "서버 기록 기준으로 추가 확인이 필요한 상태입니다."
- "기록 불일치 가능성이 있어 운영 검토로 전달하겠습니다."
- "현재 단계에서는 임의 보정 없이 저장된 기록을 기준으로 확인합니다."
- "보상이나 진행도 변경은 검토 절차가 끝난 뒤 별도 기준에 따라 처리됩니다."

Avoid:

- "보상해 드리겠습니다" before an approved repair workflow exists.
- "확률상 문제가 없습니다" with raw probability detail.
- "다시 뽑아 드리겠습니다" for gacha cases.
- "기록을 수정했습니다" unless an audited mutation actually happened.

## Rollout Order

1. Keep support escalation as documentation and Admin placeholder only.
2. Add Server read-only support account summary endpoint.
3. Add Admin read-only support account page.
4. Add support escalation status placeholders.
5. Add ops error log correlation.
6. Define audited repair policy.
7. Implement repair workflows only after approval, audit, and permission design.

## Open Decisions

- Whether escalation records need their own table or can start as support ticket metadata.
- Whether support ticket ids are required for every escalation.
- Whether `SUPPORT_ESCALATION_WRITE` should be available to OPERATOR or OWNER only.
- How long evidence package references should be retained.
- Whether player-facing response templates should be copied manually or sent through Admin.
- Which integrity cases require a separate security review workflow.
