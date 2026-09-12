# Effect feedback: proposed integration contract

Status: coordination input for Unity, not an implemented wire version or readiness claim. Existing r6 evidence must not be reused after producer/renderer/resource changes. The W01/W02 source freeze starts only when a new complete packet and canonical hash are explicitly handed over.

## Observed producer boundary

Unity `BasicCombatSimulation.ResolveBasic` and `CombatSkillCommands.ApplySkillEffect` append `BasicCombatHit` with Tick, Source, Target, HpLoss, RemainingHp and Absorbed. `CombatLocalCheckpoint` restores those fields. Those records cannot independently identify ability/effect index or distinguish heal, repair, shield issue, status refresh and interruption. Node `ce03-support-effects.ts` currently saves command-level APPLIED/FIZZLED/CANCELLED receipts with resulting state; APPLIED also includes effects with zero realized amount. Neither source authorizes inferred positive feedback from HUD deltas or command status alone.

## Proposed minimum result envelope

Bind results to the accepted run and immutable profile identity. A stable identity is the tuple `(runId, profileSha256, globalSequence, commandId, abilityId, effectIndex, targetId, pulseIndex, resultKind)`. Preserve tuple fields or encode them canonically without ambiguous delimiter concatenation. Global sequence comes from the engine allocator; do not substitute the support-local receipt counter. One result identity must never acquire another payload after replay.

Include simulation tick, sourceId and a discriminated result payload emitted where the effect commits. For basic attacks, use an explicit basic-attack source kind/identity; do not invent an ability catalog ID. Final exact field names and schema version require Unity producer alignment.

| Result kind | Actual commit evidence | Presentation gate |
| --- | --- | --- |
| DAMAGE | hpLoss and absorbed, separately nonnegative | HP damage feedback only for hpLoss>0; shield absorption feedback only for absorbed>0 |
| HEAL / REPAIR | actualHpGain after missing-HP/per-effect clamp | Positive gain only, keeping healing and train repair distinct |
| SHIELD_ISSUE | previousRemaining, resultingRemaining, actual added amount, layer key, expiry | Do not label remaining total as newly granted shield; distinguish amount increase from expiry-only refresh |
| STATUS_ADD / STATUS_REFRESH | category, source key, actual magnitude, previous/new expiry, accepted state change | No positive feedback for ignored weaker replacement or unchanged state |
| INTERRUPT | actual cancelled action ID and actual cancellation result | No success feedback for immunity, disappeared action or replacement action |
| STUN | accepted control-state change and expiry | No success feedback when immunity rejects the control component; barrage damage remains separate |

CAST_STARTED and cooldown/channel updates can drive preparation feedback. They do not prove an effect landed. Cancellation, fizzle, full HP, complete damage protection and zero shield room must not generate fabricated positive effects. An absorbed hit may have hpLoss=0 and still legitimately produce shield feedback.

## Persistence and presentation

Save authoritative results with effect changes and gameplay applied receipts in the same battle checkpoint transaction. Presentation maintains its own last-consumed result identities/cursor so restore does not replay past one-shot audio/VFX. Presentation acknowledgment must never claim gameplay effects or resources. Skip/volume/mute/full-versus-simplified modes may change presentation only; they must not alter gameplay event order, damage, cooldown, seed, finish or claim.

Project resultKind plus approved ability/effect identity to the actual effect sound/visual mapping. Do not pass credentials, player account data or arbitrary asset paths through this envelope. Audio duration, gain and visual timing are presentation inputs, not gameplay durations. Missing mappings remain unconnected; they do not authorize guessing effect type from a display name.

## Required comparison cases

Positive/zero heal; repair versus heal; hpLoss-only, absorption-only and fully protected damage; shield addition versus refresh versus cap rejection; accepted status versus ignored weaker effect; actual interrupt versus immune/disappeared/replaced action; barrage damage with immune stun; duplicate/restart replay with no repeated one-shot; identical gameplay results across display/skip/audio modes.

Node effect vectors from commit 2a39ef6 remain NODE_EFFECT_PHASE_ONLY/unityExecuted=false. This document does not change them or claim they contain the proposed typed envelopes. Unity implementation, serialized schema tests and actual result comparison are still required.
