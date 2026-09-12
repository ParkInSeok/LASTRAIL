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
