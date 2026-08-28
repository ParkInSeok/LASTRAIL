# Repair and Compensation Policy

## Purpose

This document defines the conditions required before LAST RAIL opens manual repair or compensation tools.

Launch support remains read-only. Repair and compensation are future controlled workflows that require explicit permission, audit records, evidence, and limits.

## Current Launch Position

Manual repair is not available at launch.

Support staff may:

- Review server-owned records.
- Use approved response templates.
- Attach read-only evidence.
- Escalate cases for review.

Support staff may not:

- Grant currency manually.
- Grant items manually.
- Reset offline reward timestamps.
- Mutate story progress.
- Mutate unlocks.
- Reroll gacha results.
- Edit reward transactions.
- Edit economy ledger entries.
- Publish localization or narrative changes.

## Repair Principles

Any future repair workflow must follow these principles:

- Server records are the source of truth.
- Repair must reference the original affected record.
- Repair must create a new audited correction record, not rewrite history.
- Repair must be idempotent where replay is possible.
- Repair must have an explicit reason code.
- Repair must have an operator id.
- High-impact repairs require owner approval.
- Player-facing messaging must not expose exploit-sensitive internals.

## Permission Model

Launch permission:

- `SUPPORT_READ`: read-only support investigation only.

Future repair permissions:

| Permission | Purpose | Suggested Roles |
| --- | --- | --- |
| `SUPPORT_ESCALATION_READ` | Read escalation status and evidence package. | VIEWER, OPERATOR, OWNER |
| `SUPPORT_ESCALATION_WRITE` | Mark case review status or attach notes. | OPERATOR, OWNER |
| `SUPPORT_REPAIR_PREVIEW` | Preview a proposed repair without committing it. | OPERATOR, OWNER |
| `SUPPORT_REPAIR_APPROVE` | Approve high-impact repair. | OWNER |
| `SUPPORT_REPAIR_EXECUTE` | Execute an approved repair transaction. | OWNER or restricted OPERATOR |

No future permission should imply unrestricted economy mutation.

## Repair Categories

| Category | Allowed Future Repair Type | Launch Status |
| --- | --- | --- |
| `reward_claim_missing_ledger` | Replay stored reward plan into ledger through a correction transaction. | Disabled |
| `reward_preview_without_claim` | Reopen or retry claim against stored reward preview. | Disabled |
| `offline_state_conflict` | Correct offline state timestamp/revision with evidence. | Disabled |
| `offline_claim_duplicate_mismatch` | Reconcile idempotency and ledger records. | Disabled |
| `story_unlock_missing` | Apply missing unlock/progress from verified full victory. | Disabled |
| `gacha_guarantee_conflict` | Escalate for manual review; direct reroll discouraged. | Disabled |
| `integrity_restricted_reward` | Owner review after integrity disposition. | Disabled |
| `account_identity_conflict` | Account recovery or merge workflow. | Disabled |
| `narrative_content_missing` | Content/localization fix, not player-state repair. | Disabled |

## Required Evidence

Every future repair request must include:

- support ticket id
- account id
- case type
- escalation category
- severity
- primary reference id
- affected transaction ids
- observed failure
- proposed repair action
- player-facing summary
- operator id
- approval status

Category-specific evidence:

- Run reward repair: run id, reward preview id, reward claim id if any, reward transaction id if any.
- Offline reward repair: offline reward claim id, state revision, last claim timestamp, story progress id.
- Story unlock repair: run id, operation id, outcome, cleared waves, progression ledger state.
- Gacha repair: gacha batch id, banner id, guarantee type, pity state, ledger state.
- Account recovery: candidate account ids, non-secret auth identity references, ownership proof status.

Do not include:

- tokens
- raw auth headers
- raw request bodies
- raw RNG material
- seed preimages
- per-candidate probabilities
- full weight lists
- raw multiplier bps
- internal reward calculation details
- localized dialogue full text

## Audit Requirements

Future repair execution must create immutable audit records.

Required audit fields:

- audit id
- support ticket id
- account id
- operator id
- approver id if required
- permission used
- repair category
- reason code
- affected reference ids
- before snapshot hash or sanitized before summary
- after snapshot hash or sanitized after summary
- idempotency key if applicable
- created at

Audit records should not store secrets, raw request bodies, raw RNG, probability tables, or exploit-sensitive calculations.

## Transaction Rules

Future repair transactions must:

- Be atomic.
- Avoid direct edits to historical ledger rows.
- Create correction ledger rows instead of rewriting prior ledger.
- Attach reason code and support ticket id.
- Be idempotency-protected when retried.
- Fail closed if evidence does not match current server state.
- Return a sanitized result with reference ids only.

## Compensation Limits

Future compensation limits should be conservative.

Launch recommendation:

- No manual compensation.

Future limit candidates:

- Low-impact compensation can be previewed by OPERATOR and approved by OWNER.
- High-impact currency or rare item compensation always requires OWNER approval.
- `cur_special_core`, `cur_command_ticket`, and `cur_command_ticket_fragment` require explicit allowlist.
- Gacha reroll should remain prohibited unless a separate incident policy approves it.
- Compensation should not exceed the verified missing value from server records unless incident policy allows it.

## Player-Facing Messaging

Use:

- "서버 기록 검토 후 필요한 경우 별도 기준에 따라 보정됩니다."
- "현재는 임의 지급 없이 저장된 기록을 기준으로 확인합니다."
- "보정이 필요한 경우 승인 절차와 기록 확인을 거쳐 처리됩니다."

Avoid:

- "바로 지급해 드리겠습니다" before approval.
- "확률 오류였습니다" without verified incident policy.
- "기록을 수정했습니다" unless an audited repair actually committed.
- "다시 뽑아 드리겠습니다" for gacha reports.

## Non-Goals

This policy does not implement:

- Repair APIs
- Admin repair buttons
- Ticket system integration
- Economy correction jobs
- Account merge tooling
- Gacha reroll tooling
- Localization publishing

## Rollout Order

1. Keep launch support read-only.
2. Add support account summary read-only endpoint and Admin page.
3. Add escalation status read-only placeholders.
4. Define audit table and permission model.
5. Add repair preview endpoint.
6. Add owner approval workflow.
7. Add narrow repair execution endpoint for one category.
8. Expand only after production incidents prove the need.

## Open Decisions

- Whether repair approval should be in Admin or an external ticketing system.
- Whether support ticket id should be required for all repair previews.
- Whether OPERATOR can execute low-impact repairs after OWNER approval.
- Which currency/item ids are eligible for compensation.
- Maximum daily compensation per account and per operator.
- Whether repair audit data needs a separate retention policy.
- Whether account recovery belongs in support repair or a separate identity workflow.
