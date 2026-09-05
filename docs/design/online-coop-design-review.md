# Cooperative Design And Implementation Readiness Review

## Scope

Read-only runtime/source review and planning-document consolidation. Reviewed repositories: planning at `C:/Users/dlstj/OneDrive/문서/ChatGPT/LASTRAIL`, runtime at `E:/Projects/LASTRAIL`. No server, Unity, asset, migration, deployment, currency, or video operation was performed in this pass.

## Resolved Document Conflicts

| Earlier alternative | Consolidated policy |
| --- | --- |
| World Raid difficulty boards | No World Raid difficulty; separate solo/cooperative event/boss boards |
| Direct phase/survival/clear-time bonuses | Valid boss damage only; successful mechanics grant damage buffs |
| Three ranked partners per account | Unlimited teams; one account payout with duplicate-account promotion |
| Player rows for a cooperative score | One team row with both names and immutable score-producing loadout |
| Separate mode tickets/daily counters | Shared ticket stock; no additional daily attempt counter |
| Multiple mode/partner reward payouts | Shared score tiers and better ranking bundle once per account |
| Raw public rank determines reward | Cooperative promoted position; skip only rows without eligible unassigned accounts |
| Unspecified recovery deadline | Entity recovery deadline, never the attempt deadline; no recovery beyond attempt end |

## Confirmed Existing Boundaries

- `server/apps/game-api/src/index.ts` is an existing Hono HTTP API with guest/refresh/profile/run/reward routes. `server/apps/game-api/wrangler.jsonc` contains D1 configuration and no observed room authority binding. Generated `worker-configuration.d.ts` WebSocket/DurableObject declarations are SDK types, not implementation evidence.
- Unity's `RunEncounterType` has only ordinary and story-raid variants. `FullyAnimatedCombatHudViewState` embeds story defeat/refund labels and exposes presentation cues, not server-owned match time/score.
- `BattleSceneRoot` advances a local simulation clock. `RunApiFlowFacade`/story submission cannot be relabeled authoritative cooperative World Raid logic.
- Existing HTTP/Bearer components and HUD assets can be reused, but startup-to-lobby orchestration, membership, role command ownership, and reconnect controllers are missing from the inspected flow.
- Existing `Tools/run-editmode-tests.ps1` still passes `-nographics`. Future graphics/license-sensitive verification must add a reviewed graphics option or use a direct graphics batchmode invocation, with no `-quit` for `-runTests`.

## External Task Evidence

- Unity returned read-only source findings and a proposed minimum controller/transport plan; no execution or implementation was claimed.
- Server task requests returned completed with no readable report twice. This review relies on direct server source inspection and does not claim Server-owner sign-off.

## Explicit Engineering Gates

1. Recover the four basic operation category IDs from authoritative source before generic operation routing. The six story mission IDs are not the four categories.
2. Review a shared versioned room/command/snapshot contract before parallel server and Unity implementation.
3. Establish serialized local authority and an actual two-client transport; tests over fixtures alone cannot pass this gate.
4. Define real boss schedules, role actions, damage/skill producers, and activity instrumentation before public gameplay. A deterministic test encounter must remain labeled as such.
5. Design persistent cross-account ticket transactions and idempotent settlements before real account writes. Local dry-run ledgers are not sufficient production evidence.
6. Validate two phones/emulators and deployment readiness separately. No Android artifact, device connection, real service grant, or live smoke was produced here.

## Next Executable Slice

Follow [the staged first-match plan](../superpowers/plans/2026-09-05-online-coop-first-match.md), beginning with pure server policy checks for phases, down timers, ticket accrual, and reward promotion. The first slice does not require a final combat skill catalog or new remote service.

## Documentation Checks

Check local links, Markdown fences/tables, retained canonical policy values, no stale team cap/difficulty/bonus decisions, and path-scoped `git diff --check`. These are documentation checks, not gameplay tests or proof of balanced economy.
