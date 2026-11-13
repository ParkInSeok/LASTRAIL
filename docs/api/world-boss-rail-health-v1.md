# World boss single-intent LOCAL health adapter v1

Version `world-boss.rail-health.v1.20261002`; personal layer `world-boss.rail-personal-shield.v1.20261002`; explicit local original start `world-boss.rail-health-start.v1.20261002`. Scope LOCAL_TEST_PROFILE/TRUSTED_LOCAL_TRIAL, phase1 TRAIN direct hit, one personal shield. Not an operational world profile or HTTP route.

Source policy: 44_COMBAT_DETAIL_CONTRACTS.md#numeric-defense-damage-pipeline-v06 (top approval correction) and #world-boss-rail-action-v3-20261002. New rail v3 discards old maxHP35%/DEF/shield bypass. This adapter reuses approved K1000 arithmetic and existing HP owner; local attack magnitude/DEF/effect/shield test data does not approve operational coefficients or effect mappings.

## Ownership and prerequisites

HP is exclusively `CombatHealthStore` / existing `combat_damage_states` v2 / `HealthRebaseState`. No second HP row/cache or replacement kernel. Current actual initialized v2 owner must already exist. Its account/run and immutable original start must explicitly match LOCAL marker, attempt, content and initial target. Legacy v1, actor-charge state, missing HP owner and nonlocal/original ordinary snapshots are not initialized/migrated by this adapter. Original run snapshot is never changed.

There is no existing Server personal shield absorption owner to reuse. Minimum `WorldBossRailShield` owns one resolved personal layer, protection deadline and logical last time; it never owns HP, recovery, charge, score or wallet. Layer issuance magnitude is a frozen trusted trial input; no ability cast/production eligibility is proved. Multiple layers/shared TC02/special pools and effects requiring an absent producer are unsupported rather than given invented order.

Frozen `WorldBossRailJournal` reads and validates the actual persisted LOCAL rail/frame/receipt/geometry owner. Normal phase1 result requires original bound ATTACK/result/failure intent; cancellation consumes the original authoritative cancellation frame without inventing impact. Operational world movement/tick/health chronology and full-entity/round lifecycle remain missing; these LOCAL tests do not represent gameplay balance or admission readiness.

## Internal contracts

`attach(accountId, runId, profile)` requires original owner account == train participant, ACTIVE account/run, existing supported v2 health owner, frozen local start/initial targets and matching source round/content/target/attack. Before actual impact only. Profile strict fields: version/status/attemptId/contentVersion/roundId/targetId/attackRef, existing `numericDefenseInputSchema` damage input, optional single `{sourceId,recipientId,amount,expiresAtMs}`, protectedUntilMs. Only phase1 and targetRef/train are supported. Profile and original shield seed are immutable. Duplicate attach returns existing state; raced duplicate/lost acknowledgement recover original binding. No client attach/profile/HP upload API exists.

`apply(accountId, {version,attemptId,contentVersion,roundId})` is trusted internal producer invocation, not a gameplay client damage command. Owner account required; target/damage/availability/HP cannot be supplied. Reuse source original failure intent and geometry evidence. It creates one processed receipt per attempt/round. Event key for existing HP owner is `wb:` + SHA256 of adapter/attempt/round/original intent, avoiding lossy truncation of escaped source IDs. Original event IDs remain in receipt.

`read(accountId, attemptId)` checks source participation (operator may read shared train health) and validates original owner/start/HP checkpoint and personal layer provenance. Return `{version,status,inputProvenance,runId,healthRevision,health,shieldRevision,shield,processingStatus,receipt}`. Read status is PENDING_SOURCE before the original result, PENDING_HEALTH until its processing transaction, APPLIED with original receipt after commit. V3 logical revision, source journal revision, health revision and shield revision are distinct; consumers must not interchange them.

## Calculation and original timing

Source result carries original logical resolvedAtMs; physical ATTACK preserves its own observedAtMs. No source timing is changed. Local damage applies once at original result logical time. Existing calculator resolves offense groups → effective DEF (strongest weakening capped30%, summed penetration capped60%, K1000/(1000+DEF)) → eligible reductions (EQ06 sum cap50%, strongest guard stance, independent groups multiplied) → one final floor. EQ06 eligibility uses authoritative pre-hit HP<=50% exactly once. Other source/effect eligibility comes from frozen explicit TRIAL inputs, not operational equipment/ability mapping.

The remaining integer budget goes through the valid personal shield then HP. Expiry equal to result time is expired; protection equal to result time is expired. No minimum1 damage. Absorption cannot exceed layer remaining; HP loss cannot exceed current HP; BigInt arithmetic is preserved until capped safe-integer outputs. Exact input range reuses the existing 256-digit schema. Shield-only/zero HP changes preserve the HP rebase anchor. TARGET_DOWN, protection, evasion and cancelled/skipped result cannot retarget or add damage. No carriage transfer, duplicate direct hit, maxHP35 penalty, world score, charge or ordinary train HP0 defeat rule is added.

Example trial: base1000, DEF1000, two independent eligible trial reductions10% and20% → floor(1000*0.5*0.9*0.8)=360; personal shield100 → absorbs100, HP1000→740. These are reproducible test numbers, not operating balance.

## Atomic storage and replay

Source journal is an immutable durable intent. HP application is a separately versioned processing transaction; journal result by itself does not claim HP has applied. Until its processed receipt exists, that intent is pending health and can be retried. Original journal/geometry/v3/HTTP files are unchanged.

Commit batch: assert exact source head version/hash/revision/rail/frame/geometry/binding and immutable source entry, exact link/profile/seed/current shield, actual ACTIVE run/account/original snapshot and current v2 HP/checksum/state; retain `CombatHealthStore.prepareChange` verifier/source statements; write HP/health receipt, shield checkpoint and processed world receipt together; clear guards. Intermediate failure rolls all HP/shield/receipt changes back, leaving original intent pending. Both health receipts are immutable. Failed/lost acknowledgement reads the exact original world receipt; concurrent copies apply damage once. Missing or detached partial HP history fails closed, not a backfill. New instance/SQLite reopen restores same owned states and original receipts.

Current shield is validated against immutable seed (revision1) or original processed receipt (revision2), and an HP receipt must exist in the validated existing HP ledger. There is no standalone shield setter/reissue supported by this contract. The existing HP owner may subsequently perform its own valid events; the original attack replay still returns its original receipt.

Receipt fields: version/resolutionId/attempt/content/round/target, sourceJournalRevision, sourceAttackEventId/sourceIntentEventId, resolvedAtMs/reason, calculation/resolvedDamage, shieldBefore/After/absorbed, hpBefore/loss/after, healthRevisionBefore/After and shieldRevisionBefore/After, original healthReceipt or null. It never claims source journal revision was incremented by damage processing.

## Errors, installation and followups

Internal LocalRailError: 403 owner/participant/inactive owner; 404 missing local run/attempt; 409 unsupported profile/intent/definition, changed attachment/event body, closed attach window, inactive run, CAS conflict; 503 missing link/result, corrupt/detached source/shield/HP/receipt, partial history, revision exhaustion. Existing CombatHealthStore schema/proof errors and numeric source/range conflicts are thrown. No HTTP mapping currently installed. Never return damage success on invalid source or storage failure.

Dedicated LOCAL database SQL: existing local rail SQL, frozen journal SQL, new `apps/game-api/local/world-boss-rail-health.sql`; existing operational health tables must already exist from schema. Never apply this local SQL to DEV/PRD or bootstrap operational accounts/runs from these fixtures.

Consumable now: explicit LOCAL HP/shield processed receipt/read fixtures and pending→applied/replayed semantics. Next authenticated HTTP adapter needs existing real token/session/participant auth, local-only isolation, original source version/header checks, these internal domain/repositories, processing readiness/error mapping and actual consumer request/restart verification. It must expose commands/read only, never arbitrary damage/profile/HP/frame/sample upload. Operational movement/event clock/health chronology, dynamic effect and shield producer, nonphase1/nontrain/multilayer/world down/fullround integration remain separate prerequisites. Neither Unity browser validation nor health View completion is a prerequisite for this LOCAL Server processing verification. Final approved art still gates View connection.