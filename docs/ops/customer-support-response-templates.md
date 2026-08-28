# Customer Support Response Templates

## Purpose

This document defines launch-scope customer support response templates for LAST RAIL.

The templates are written for support staff. They separate:

- Internal support checks: what the operator verifies in Admin.
- Player-facing response: safe wording that can be sent to the player.
- Escalation criteria: when the case should move beyond read-only support.

Support staff should not expose raw security, economy, probability, RNG, or token details.

## Global Rules

Use these rules for every support response:

- Base answers on server records, not screenshots or client-local state.
- Do not promise manual compensation unless an approved repair workflow exists.
- Do not expose access tokens, refresh tokens, raw auth headers, raw request bodies, RNG seeds, seed preimages, per-candidate probabilities, full weight tables, raw multiplier bps, or internal exploit-sensitive calculations.
- Keep stable backend ids in internal notes. Share them with players only when needed as a support reference.
- Describe recruited operators as rescued specialists, defectors, or signal responders. Do not describe them as purchased goods.
- If the evidence is incomplete, say the case is being escalated for record review instead of guessing.

## Case Type Labels

| Code | Korean Label | Support Use |
| --- | --- | --- |
| `account_auth` | 계정/인증 | Session, guest account, account status reports |
| `run_completion` | 출격 완료 | Finish, outcome, cleared waves, unlock reports |
| `run_reward` | 출격 보상 | Reward preview, claim, first clear reports |
| `offline_reward` | 오프라인 보상 | Offline preview/claim/cap reports |
| `story_progress` | 스토리 진행 | Operation unlock and story progress reports |
| `gacha_recruitment` | 모집/합류 | Gacha pull, guarantee, pity reports |
| `integrity_restriction` | 무결성 제한 | Restricted reward or suspicious activity reports |

## Account/Auth

### Internal Checks

- Confirm account exists and is active.
- Check account created time and latest authenticated activity.
- Check whether the report is about guest continuation, session expiry, or account reset.
- Check whether related story/reward records belong to the same authenticated account context.

### Player-Facing Template

서버 기록 기준으로 계정 상태를 확인했습니다. 현재 확인된 접속 기록과 계정 상태를 기준으로 이어서 조사하겠습니다. 계정 보상이나 진행도는 기기 화면이 아니라 서버에 저장된 계정 기록을 기준으로 확인됩니다.

### Escalate When

- Account is missing but player has purchase or platform proof.
- Multiple account records appear to represent one player.
- Auth session state conflicts with account status.
- Support cannot verify account ownership from available records.

## Run Completion

### Internal Checks

- Confirm `runId`, operation id, risk tier, outcome, cleared waves, and finished time.
- Check idempotency record for duplicate finish submit.
- Check whether `VICTORY` with 15 cleared waves occurred.
- Check story progress mutation and unlock records.
- Check reward preview id created at finish time.

### Player-Facing Template

출격 결과는 서버 완료 기록을 기준으로 확인됩니다. 일반 스토리 진행은 승리와 전체 공세 클리어가 함께 기록된 경우에 갱신됩니다. 중복 제출된 완료 요청은 같은 결과를 다시 확인하는 방식이며, 진행도나 보상을 다시 적용하지 않습니다.

### Escalate When

- Full victory exists but no story progress or unlock record exists.
- Finish record exists but reward preview was not created.
- Run status, final result, and idempotency records disagree.
- Integrity disposition blocks completion but no support-visible reason exists.

## Run Reward

### Internal Checks

- Confirm reward preview id and reward claim id.
- Check reward transaction, economy transaction, and currency ledger entries.
- Check first clear state for `accountId + operationId + risk`.
- Check whether the reward claim was a duplicate replay.
- Check integrity disposition.

### Player-Facing Template

출격 보상은 서버가 완료 기록, 위험도, 클리어 상태, 첫 클리어 여부를 기준으로 계산한 보상 계획을 따릅니다. 첫 클리어 보상은 완료 처리 시점에 확인되며, 같은 요청이 다시 들어온 경우에는 기존 처리 결과를 다시 보여줍니다.

### First Clear Template

첫 클리어 보상은 작전과 위험도 단위로 서버에서 확인됩니다. 이미 해당 작전/위험도의 첫 완료 기록이 있으면 같은 첫 클리어 보상이 다시 생성되지 않습니다.

### Escalate When

- Reward claim exists but ledger entries are missing.
- Reward preview exists but claim cannot be recovered or retried.
- First clear state conflicts with stored run completion.
- Integrity restriction requires manual review.

## Offline Reward

### Internal Checks

- Confirm offline reward state row source.
- Check `last_offline_reward_claim_at`, `last_story_progress_id`, revision, and updated time.
- Check preview status and claim status.
- Check claimable seconds/hours, storage cap hours, minimum claim seconds, and max claimable hours.
- Check latest offline reward transaction and economy ledger.
- Check idempotency record for duplicate claim.

### Not Claimable Template

현재 서버 기록 기준으로 수령 가능한 오프라인 보상이 없습니다. 오프라인 보상은 최소 누적 시간과 보관 한도 기준을 함께 적용하며, 기준 시간보다 짧은 경우에는 수령 처리되지 않습니다.

### Smaller Than Expected Template

오프라인 보상은 서버에 저장된 스토리 진행도와 마지막 수령 시각, 보관 한도를 기준으로 계산됩니다. 장시간 미접속하더라도 보관 한도를 초과한 시간은 기본 수령량에 포함되지 않을 수 있습니다.

### Duplicate Claim Template

같은 수령 요청이 중복으로 들어온 경우에는 서버가 기존 처리 결과를 다시 확인합니다. 이 경우 보상이 한 번 더 지급되거나 수령 기준 시간이 다시 변경되지 않습니다.

### Escalate When

- Successful claim exists but currency ledger is missing.
- State revision changes without a matching claim transaction.
- Empty claim advanced `last_offline_reward_claim_at`.
- Stored story progress is unknown or policy is missing.
- Clock tamper or state conflict is suspected.

## Story Progress/Unlock

### Internal Checks

- Confirm highest story progress id and highest cleared operation id.
- Check cleared operation ids and unlocked operations.
- Check last progress run id.
- Check progression ledger entries.
- Check narrative version and missing content status.

### Player-Facing Template

스토리 진행도와 작전 해금은 서버 완료 기록을 기준으로 확인됩니다. 이미 더 높은 진행도가 저장된 경우, 낮은 작전의 재클리어로 진행도가 되돌아가지는 않습니다.

### Missing Narrative Template

진행도 기록과 내러티브 표시 데이터는 서로 분리되어 있습니다. 진행도는 서버 기록으로 확인하고, 표시 문구나 브리핑 누락은 콘텐츠 상태로 별도 확인하겠습니다.

### Escalate When

- Full victory should advance progress but no update exists.
- Unlock record is missing for the next expected operation.
- Narrative content is missing for an unlocked operation.
- Server progress and progression ledger disagree.

## Gacha/Recruitment

### Internal Checks

- Confirm gacha batch id, banner id, pull count, and banner status.
- Check guarantee type and pity counters.
- Check idempotency record for duplicate pull.
- Check account item/operator ledger references.
- Check sanitized selection reason if available.

### Player-Facing Template

모집 결과는 서버에 저장된 모집 기록을 기준으로 확인됩니다. 중복 요청은 같은 모집 결과를 다시 보여주는 방식이며, 결과를 새로 뽑거나 다시 계산하지 않습니다.

### Guarantee Template

보장 여부는 서버의 보장 상태와 모집 기록을 기준으로 확인됩니다. 내부 확률표나 난수 정보는 공개하지 않지만, 보장 조건이 적용되었는지는 기록으로 확인할 수 있습니다.

### Escalate When

- Guarantee type conflicts with pity counter state.
- Pull batch exists but item/operator ledger is missing.
- Duplicate replay returned a different result.
- Banner state changed during a claim window.

## Integrity Restriction

### Internal Checks

- Confirm integrity disposition and related ops error code.
- Check affected run/reward/offline/gacha reference ids.
- Check whether the restriction is soft anomaly or hard restriction.
- Check suspicious replay or tamper signals only through sanitized status.

### Player-Facing Template

일부 기록은 서버 무결성 확인 절차에 따라 추가 검토가 필요할 수 있습니다. 보상이나 진행도 처리에 제한이 적용된 경우, 서버 기록을 기준으로 확인 후 안내드리겠습니다.

### Escalate When

- Hard restriction blocks reward or progress.
- Integrity status is missing but reward is restricted.
- Suspicious replay conflicts with normal duplicate replay records.
- Operator decision is needed before a player response.

## Internal Note Format

Support notes should use this compact structure:

```text
caseType:
accountId:
primaryReferenceId:
observedStatus:
serverEvidence:
playerFacingSummary:
escalationNeeded:
```

Do not paste raw auth headers, raw request bodies, tokens, RNG material, or full probability tables into support notes.

## Safe Explanation Snippets

Use these short snippets when a full template is too long:

- 서버 기록 기준으로 확인했습니다.
- 중복 요청은 기존 처리 결과를 다시 보여주는 방식입니다.
- 수령 가능 시간과 보관 한도가 함께 적용됩니다.
- 첫 클리어 보상은 완료 처리 시점 기준으로 확인됩니다.
- 진행도와 표시 문구는 분리되어 확인됩니다.
- 내부 확률표와 난수 정보는 공개하지 않습니다.
- 추가 검토가 필요한 기록으로 확인되어 운영 검토로 전달하겠습니다.

## Open Decisions

- Whether response templates should be stored in Admin as static content or generated from a backend support contract.
- Whether support case notes should require a support ticket id.
- Which escalation types require OWNER approval.
- Whether support can send templated responses directly from Admin or only copy reviewed text.
- Whether each template needs separate Korean, English, and Japanese localization keys before launch.
