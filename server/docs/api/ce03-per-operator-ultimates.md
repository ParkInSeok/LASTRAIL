# Individual ultimates and manual/AUTO control

## Current approved extension: general ACTIVE always automatic (.003)

Select all three trusted options: `targetingPolicyId=ce03.frontline-first-v1`, `ultimatePolicyId=ce03.per-operator-ultimate-v1`, and `activePolicyId=ce03.general-active-auto-v1`. The third option requires the first two. This creates execution `ce03.execution.2026-09-13.003`, SHA256 `266d1e98ef39c5596f0f8cf9f1bd78ce77207c943d1f3192fe7e5b3fd2c9e8d4`, command `ce03-command-contract-v3`, and future accepted wrapper `ce03-isolated-command-profile-r9`. The prior .002/v2 contract below is retained unchanged for old preparations.

Command-v3 keeps v2 `support`, `supportChannel` and `ultimates`, and adds the closed `actives` payload from `generalActiveAutoPolicy` in `ce03-ultimate-policy.ts`. Its `enabled=ALWAYS` has no UI toggle. The upper `AUTO` controls only the five ultimates and starts OFF. Train skills remain manual.

The new command stage is `A1.manual-ultimate-auto-active-auto-commands`: queued manual commands first, then eligible ultimate AUTO when ON, then eligible general ACTIVE, followed by existing basic scheduling. Each automatic phase uses formation slot then ordinal actor ID order. Shared actor queue, casting, basic reservation and recovery locks prevent duplicate reservations. Preflight living/available caster, authored readiness and valid targets before enqueueing; skipped candidates consume nothing and emit no rejected journal entries. Earlier accepted casts remain intact. Manual ultimates received before the decision boundary take priority over both automatic phases; this does not preempt an already accepted cast.

General ACTIVE effects, first-ready/cooldown values, target checks, windup, recovery and CAST_ACCEPTANCE consumption stay authored. Actual cooldown timestamps persist across down/recovery and restore. Pause/terminal makes no automatic decisions. Share a deterministic checkpointed automatic ID sequence between both automatic phases. A separate .003 checkpoint identity must reject legacy/.002 restore; do not reinterpret prior saved state. Remove general manual buttons and display actual remaining cooldown immediately below world HP. Target absence must not invent a cooldown. Unity verifies target absence, down/recovery, pause/restore, manual and ultimate-AUTO contention separately from server contract tests.

New source pin `generalActiveAutoPolicyV1` is the UTF-8/LF `generalActiveAutoSource` text; it explicitly supersedes only the earlier source sentence keeping general ACTIVE manual. Other individual-ultimate rules remain unchanged. Generate a fresh read-only/no-run fixture with `scripts/validation/verify-ce03-general-active-auto.ts`.

For both new command generations, consumer report version is `ce03-consumer-runner-report-v4` without presentation or `ce03-consumer-runner-report-v5` with presentation. Existing report v1/v2/v3 remains for legacy drafts. Required coverage is the eight base categories plus `SUPPORT_SELECTION`, `INDIVIDUAL_ULTIMATES`, `ULTIMATE_AUTO`, `LEGACY_POLICY_BOUNDARY`; command-v3 additionally requires `GENERAL_ACTIVE_AUTO`, and presentation requires `PRESENTATION_BINDINGS`. Report shape/hashes/labels alone never prove execution: the trusted verifier must authenticate actual runner evidence for the exact draft, source, checkpoint and vectors. Existing verifier implementations do not automatically support these new reports.

## Retained ultimate-only preparation (.002)

Explicitly opt new preparations into `ultimatePolicyId: "ce03.per-operator-ultimate-v1"` together with `targetingPolicyId: "ce03.frontline-first-v1"`. Both fields belong in the third producer argument or trusted local-host construction options; they cannot be set by a prepare request. Omitted options preserve legacy behavior. `leaderPolicy` is unnecessary for the new host.

| Field | Value |
| --- | --- |
| Simulation/execution policy version | `ce03.execution.2026-09-13.002` |
| Execution policy SHA256 | `68beb942a695ea761aa0f2aaf5677d7eee603aeeb3de078ffca148a27d824b50` |
| Command stage | `U1.manual-auto-commands` |
| Command contract | `ce03-command-contract-v2` |
| Command policy | `ultimates.version=per-operator-basic-hp-hit-v1` |
| Template selected ultimate | `selectedUltimateOperatorId=null` |
| Future accepted profile wrapper | `ce03-isolated-command-profile-r8` (legacy remains r5) |

Command-v2 retains `support` and `supportChannel`; it has an `ultimates` object instead of `leader`. The complete closed payload is defined in `ce03-ultimate-policy.ts`. A v2 command cannot be attested with an old draft, or a new draft with missing/v1 commands. The full prepare hash binds all ownership, draft and commands. Existing profile/catalog/effective-stat identifiers and all authored ultimate effect/cooldown values are unchanged.

The normal new host `/__local/ce03/prepare` request contains `selection` with operation/risk, `expectedFormationRevision`, `expectedLoadoutRevision`, and the outer `expectedSupportRevision`. Omit `selection.expectedUltimateRevision`: old selected leader is only a nullable historical revision fact in the returned ownership snapshot. A null leader therefore succeeds only for the explicitly configured new preparation. Public/default departure gates and old START receipt replay are unchanged. Future admission still requires the appropriate versioned departure/consumer proof; this preparation does not establish START readiness or enable the existing public departure path for null-leader runs.

## Effective combat rules

Every formation operator has its own 0–100 gauge. Only that surviving operator's resolved basic hit with actual `HpLoss>0` grants4; shield-only hits, zero damage, skills, train/device/enemy attacks and dead-source committed hits grant nothing. Preserve charge across down/recovery. Do not clone the former shared gauge into every operator.

All five ultimate abilities already exist in the template. Enable each for its own operator; preserve coefficients, targeting, windup and recovery. Existing charged mode replaces first-ready/cooldown checks: the authored15s/45s metadata remains, but **no new45-second AND gate** is introduced. Spend only the accepted caster's gauge once at `CAST_ACCEPTANCE`. Rejection does not spend; later cancellation follows the old accepted-cast behavior.

AUTO starts off for every new run. On each executed fixed tick, process queued manual commands first, then evaluate AUTO in formation slot/ordinal actor-ID order before basic scheduling. Each candidate must be alive and available, have100 charge and valid targets, and have no queued/casting command, basic reservation or recovery lock. Skip ineligible candidates without emitting rejected commands. Actor queue occupancy prevents duplicate manual/AUTO reservations. Use a deterministic checkpointed sequence for automatic command IDs. A manual input received after the current tick belongs to the next decision boundary.

Turning AUTO off stops future automatic choices and leaves accepted casts intact. Paused or terminal combat never auto-casts; changing the toggle does not execute a tick. General active skills and train manual skills remain manual.

## Checkpoint and UI implementation boundary

Unity must bind the complete static policy into configuration identity and use a separate new checkpoint schema. Persist an exact actor-ID charge map, current AUTO flag and automatic command-ID sequence; validate numeric ranges, roster identity and old/new restore incompatibility. Do not reinterpret old shared `leaderCharge` or old accepted records. The UI displays each operator's gauge below HP and a distinct ultimate command affordance, removes the leader-only panel and exposes a dedicated ultimate AUTO toggle.

`individualUltimateSource` exports the exact UTF-8/LF normative override. New `sourceFiles.individualUltimatePolicyV1` hashes it; existing frontline/R1/R2 pins remain. Run `tsx --tsconfig tsconfig.base.json scripts/validation/verify-ce03-individual-ultimates.ts` to create a separate disposable fixture with no leader selected, no presentation manifest and no execution/admission assertion. It writes only to a fresh audit directory, checks legacy draft identity and byte-identical DB before/after prepare, and creates no run. Server tests verify the contract; Unity separately verifies gauge arithmetic, double clicks, AUTO/manual ordering, pause/restore, target loss/down recovery and legacy playback.
