# CE03 support local v2 — first implementation unit

Version `ce03-support-local-v2.20260912.001`. Source: Plan §3.2 completion orders and combat detail §6, 2026-09-12. Local development only; the r6 producer, profile, START admission and Unity consumer are unchanged.

| Contract | Actual code in `apps/game-api/src/ce03-support-v2.ts` | Boundary |
| --- | --- | --- |
| Seven numeric definitions | `supportDefinitions` immutable rows | Data input for a future closed profile; not runtime capability evidence |
| Selected three from ten | `resolveSupportThree` | Exactly three unique IDs, selected order preserved. Legacy three supplied through the existing closed ability parser; unknown/missing keys reject. Does not insert ten rows into old template |
| TARGET_MAX_HP | `targetMaxHpAmount` | Exact BigInt intermediate, floor, heal 3/20 clamped to missing HP, shield 1/4; invalid HP rejects |
| Cast-start target snapshot | `pinSupportTargets` | Strict actor inputs and unique identity; returns copied entity/action pins, never mutates focus |
| Barrage | Interior enemies, focus then spawn order then ordinal ID, max three | Damage and stun application still requires new consumer |
| Medical/smoke | Living, non-returning operators | Medical rejects all-full; returned living protected actors are eligible because protection does not exclude them |
| Weakness/disruption | Explicit enemy ID in either lane | Disruption requires an interruptible, nonimmune skill/channel and pins action ID; basic attacks excluded |
| Protection | Exact HP ratio then slot then ordinal ID | Cast-start choice, no replacement |
| Commit pin check | `validSupportPins` | Removes invalid/dead/returning targets and replaced or immune actions. Does not cancel anything itself; input pins must be trusted checkpoint state |
| Damage source | `firepowerApplies` | Train basic and Combat/Gun device only; operator, support, heal, repair, fixed mechanic excluded |
| Effect lifecycle rules | `supportPolicies` | Versioned intended stage/cap/cooldown/refund policies; enforcement in the consumer is still pending |

Firepower pins the living TRAIN actor as the buff owner; the source predicate also covers carriage device damage. No separate carriage HP/target simulation is introduced here. World snapshots use strict `entityId,hp,maxHp,slot,spawnOrder,side,lane,returning,action`; action is null or `{id,kind,interruptible,immune}`. All integers are safe nonnegative integers (maxHp positive). Entity/action IDs must remain unique across the battle; do not recycle action IDs after restore.

Next unit must bind these helpers into a new closed full profile and command/checkpoint versions. It must define same-tick interrupt/pulse order, persist target pins and cooldown/channel/effect state, enforce deduplication and source-expiry/caps, then produce actual Unity conformance vectors. No provider may admit this module as an executable profile by its version or data rows alone. Existing r6 remains available only under its original proven contract.

Validation: `tests/integration/ce03-support-v2.test.ts` exercises target HP clamping/large-integer arithmetic, lane/focus selection, all-full/down/returning rejection, ratio ties, explicit action pins, changed actions, source filtering, and closed selection/world inputs.

## Second unit: closed full draft and command checkpoint

`ce03-support-profile.ts` creates/parses `ce03-support-full-draft-r7`. It includes the entire validated baseline draft and its canonical hash, full projected template with exactly three selected legacy/new manual rows, immutable new support policy, and canonical `profileSha256` (hash excludes itself). Baseline execution policy/hash and every projected field are rechecked on parse. The template's old execution block remains the **baseline core scheduler**; the outer simulationVersion plus supportPolicy describe the extension. `ready:false` and `executionEvidence:null` are mandatory. This is a draft, not an accepted r7 runtime profile. It deliberately does not reuse old algorithm/runner evidence.

`ce03-support-state.ts` supplies strict `ce03-support-command-v2` and `ce03-support-checkpoint-v2`. The initial isolated state currently accepts three of the **new seven** roles. The full draft can describe mixed legacy/new selections; execution dispatch to the existing legacy consumer is still pending and must not be inferred from draft parsing.

State is bound to the expected profile hash and ordered selection. Each accepted command records its original body/key, start tick, sequence and target/action pins. Restore recomputes all cooldowns, shared-channel release, pending cast and due outbox from receipt history and rejects mismatches. Input command keys cannot be reused with another body. Replaying the original body returns its state without revalidating current targets or spending again. Invalid/unavailable commands leave the original input untouched.

One step is 50 simulation milliseconds. Medical starts a 600-tick cooldown, becomes due at tick7, and releases the shared channel at tick12; smoke may then start while medical remains on cooldown. The persistent outbox retains a single intent per accepted command even after restore. Its sequence is currently a **support-local sequence**; it must be mapped to the unified engine sequence before merging with basic attacks or pulses. Outbox presence is not effect application or acknowledgment. The downstream consumer must atomically save effect changes and an applied-intent cursor in the battle checkpoint; never apply the full historical outbox each frame.

Bounds: 10,000 accepted command receipts, 4,096 unique pins per command, and tick <=1,000,000,000. History exhaustion rejects further commands without cost. Checkpoint reconstruction is linear in history; this reference implementation has no production performance claim. Actor/action history is trusted battle data, not a client authorization payload. Profile binding alone does not authenticate a checkpoint.

Still pending: unified sequence integration and same-tick interrupt/pulse application, actual new effect/state expiry/cap execution, atomic applied cursor, Unity parser/consumer and conformance vectors, and executable profile attestation. No HTTP registration, START acceptance, remote deployment or r6 modification is included.

## Third unit: Node effect consumer and reference vectors

`ce03-support-effects.ts` implements the **Node effect phase**, not the full Unity battle. `consumeSupportOutbox` binds the exact draft, support checkpoint and effect-frame profile hash/tick, requires an explicit command-key → engine-global-sequence mapping, and combines due supports with damage events in `(dueTick,sequence)` order. Mappings must come from the trusted unified engine queue and be saved when commands are accepted; this function never substitutes the support-local sequence. Missing mapping, late unprocessed effects, reordered/conflicting identities and mismatched clocks fail closed.

Actual execution now changes target HP, source-keyed shield/status layers and action identity for all seven roles. Each result frame includes the applied original event payload/identity and outcome in the same copied value as those changes. Serialized restore/replay cannot apply the same event twice. A failing batch leaves its input untouched. **The storage owner must atomically persist this whole value**; physical Unity checkpoint/CAS integration and whole-process durability are not established by this Node return-value test.

Damage uses BigInt through outgoing/mark/armor, then floors; smoke adds one post-armor floor before shields. Outgoing cap3000, strongest mark cap2000, effective armor cap6000, strongest smoke cap3000. BigInt remains in use through absorption and HP clamping, including maximum-safe input values. Firepower sums source values only for TRAIN_BASIC/COMBAT_DEVICE/GUN_DEVICE, then shares the outgoing cap; SUPPORT is excluded. Input `outgoingBps`/`armorBps` are already-resolved baseline core values, so importing package/passive modifiers remains the full-engine adapter's responsibility. `MECHANIC_FIXED` means a pre-shield fixed input that bypasses these numeric modifiers, not a new authored mechanic policy.

Shield layers preserve source/ability/effect-index identity, ignore weaker replacement, refresh equal/stronger, share target maxHP40% cap and absorb expiry-first then ordinal source/ability/index. Source status expiry is independent. Medical uses target maxHP15% with missing-HP clamp; protection uses25%. Barrage uses train ATK6/5, interior pins only/max3, and applies stun10ticks only to living nonimmune enemies. In this comparison kernel STUN gates uncommitted due damage during its active window; it does not retime/restart an action or implement actor command scheduling. That scheduling adapter still needs Unity integration.

Disruption clears only a matching interruptible skill/channel action ID. An earlier same-tick pulse stays applied; subsequent uncommitted pulses with that cancelled ID are cancelled. Explicitly committed impacts survive, including after source action/death changes. A replacement action cannot be cancelled by the old pin. All events for a simulation tick must reach this stage in global order; a late lower sequence is rejected.

Effect frame inputs are trusted simulation snapshots, not client-authorized stats or ownership. Full baseline operator/passive/device state import, mixed legacy ability dispatch, the actual Unity scheduler, Unity checkpoint store and new 15-wave conformance remain pending. No provider/readiness promotion is included; full draft still requires readyfalse/null execution evidence.

`ce03-support-effects.test.ts` exports seven reproducible input/event/expected-frame cases under `audit-results/ce03-support-effects/vectors-<uuid>.json`, with four module SHA256s. The artifact is marked `NODE_EFFECT_PHASE_ONLY`, `unityExecuted:false`. These are actual Node outputs for Unity to compare, not a Unity PASS report. Cases cover all seven roles; separate tests cover bound producer→state→effects, expiry/caps, replay/order conflicts, max-safe damage and untouched input on batch failure.
