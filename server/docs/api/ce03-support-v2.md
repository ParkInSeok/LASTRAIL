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
