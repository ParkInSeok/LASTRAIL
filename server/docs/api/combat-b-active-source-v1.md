# B Mira ACTIVE LOCAL source v1 — S-B-ACTIVE-SOURCE-14

Version `combat.b-active-source.v1.20261003`. **Partial task: approved source supply complete; flight/impact source and ongoing ACTIVE restore/effect adapter are not supplied.** No START or execution capability. Existing Eve/basic/refit/rank versions remain unchanged.

## Environment and authentication

Creator-only disposable supplier: `startBActiveSocket` in `scripts/local/combat-b-active-socket.ts`. Run from this Server checkout with explicit D project TEMP. Uses existing SQL.js D1 adapter, all 98 local SQL migrations and original isolated owned-profile fixtures. `origin` is the actual assigned 127.0.0.1 TCP address, never a fabricated DEV URL. Credentials live only in `intake('a'|'b')` process memory. No operational migration, rank route, credential file or public service registration.

`GET /_local/combat-b/active-source`, signed game-session Bearer from the private LOCAL intake, exact `X-Combat-B-Active-Version` above plus original `X-Combat-B-Refit-Version: combat.b-refit-http.v1.20261002`. Existing selected source must be eligible, freshly saved refit with exactly one actual `op_mira` occupant. Choose via original selection HTTP then save current refit; the supplier does not replace Eve, grant operators or mutate source on GET. No query/body/other methods. Same-origin optional Origin, LOCAL gate and current account/session/expiry checked through original refit owner. Before/after source, profile double capture and final auth/source checks precede publication. All responses no-store; read retries at most4 then409. No session logging.

## Exact source fields

| Fields | Source/meaning |
|---|---|
| accountId/selectionRevision/refitRevision/rankRevision/sourceHash/refitDefinitionHash/departure | Existing authoritative B selection/refit/physical/shared ranks; both current eligibility and immutable departure |
| actor.actorId/operatorId/instanceId/carriageId/slot/stats | Actual current saved Mira placement, original physical identity and readOwnedCombatProfile; NOT old five-person slots |
| ownedProfile/ownedProfileHash, definition | Existing approved publication and actual DB profile; generic profile canExecute remains false |
| ability | Original `op_mira.active`: ACTIVE/TIME_COOLDOWN, CD10000, windup300, recovery300,45%×6,interval200; no BASIC passive import |
| timing | Original1ms B reference step (10000 TimeSpan ticks), firstReady0, original Unity Ready/FixedStepDebt; explicit empty LOCAL generalHaste/CDR, EN08null; BCB02 sourced from original captured carriage at each actual acceptance |
| runtimeInput/runtimeInputHash | Actual actor plus original LOCAL train/car/enemy stats, external geometry/shields; no operational enemy generation/AI |
| target | Exactly the existing single LOCAL enemy, original eligible geometry and approved Mira own/adjacent2 basic range; acceptance lock, per-shot alive/range/battlefield recheck, no retarget |
| phase | Approved six-skill detail: shot offsets300/500/700/900/1100/1300ms, occupancy1600ms, fixed intervals, first-effect ATK snapshot once; invalid target cancels all unissued reservations then300ms recovery; committed shots preserve target; no damage/shield double-application, no per-shot cast/basic charge; no free BASIC in recovery |
| initial | Only genuine initial IDLE/tick0/ready0/empty commands/shots/cursor0; GET creates no command, receipt or acceptance |
| runtimeOwnership | Original Unity commandId/actor/ability/tick/sequence/due/Ready ownership; required cross-bound restore fields, ongoingSnapshotSupplied=false. It is a consumer contract requirement, NOT an already generated running checkpoint |
| contextId/version/packetHash/sourceEvidence | Canonical SHA256 and explicit original approved references; LOCAL reference byte+canonical pins enforced before publishing |

Original accepted command/sequence/Ready/cooldown receipt must be generated together by the actual Unity owner. BCB timing acceptance (`target:NONE,direct:false`) is a modifier receipt; shot damage (`target:ENEMY,direct:true`) belongs to its separate approved direct-skill event. Do not relabel one as both events or repeat CD consumption each shot.

`readPinnedBActiveSource(input, original)` is an internal frozen-original restore port. It requires exact original packet equality, rejects even rehashed mutations and returns an owned clone. `original` must come from a trusted captured departure; never replace it with GET current publication. Current source/refit changes and old receipt replay do not update an in-progress source. This validates the supplied original only; it does not claim full Unity phase checkpoint restore.

## Errors

| Status/code | Meaning |
|---|---|
|401 original auth errors | Missing/expired/revoked/old restored session |
|403 combat_b_origin_forbidden | Foreign Origin |
|400 combat_b_active_request_invalid | Method/query/body boundary |
|409 combat_b_active_version_conflict | Exact ACTIVE header mismatch |
|409 combat_b_active_refit_ineligible | Unsaved or stale current departure |
|409 combat_b_active_geometry_unavailable/target_unavailable/read_conflict | Unsupported placement/target or source read race |
|409 combat_b_active_original_packet_conflict | Frozen original tamper or foreign packet |
|503 combat_b_active_actor_unsupported/owned_stats_unavailable/definition_unsupported/reference_unsupported/modifiers_unavailable | Exact source unavailable; no fallback |
|503 combat_b_active_source_unavailable | Unexpected source/storage failure, logged without credentials |

Existing refit/auth error bodies are preserved. Success is immutable source data, never an acceptance or combat result.

## Verification and actual remaining fields

QA root `D:/Project/LASTRAIL/QA/CommanderS1-20260922`: active-source RED, review-RED, final2, actual11 TCP traces and validation manifest. Run `pnpm exec vitest run tests/integration/combat-b-active-source.test.ts tests/integration/combat-b-rank-fixture.test.ts tests/integration/combat-b-source-binding.test.ts tests/integration/combat-b-source-packet.test.ts tests/integration/combat-b-refit-http.test.ts`. Exact type project: `scripts/validation/combat-b-active-source-tsconfig.json`. Actual producer command: `pnpm exec tsx --tsconfig scripts/validation/combat-b-active-source-tsconfig.json D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-active-source-traces-20261003.mts`.

**Required decision/source:** 44 section5 explicitly distinguishes firing from later travel/impact. No supplied approved B missile type/speed/path/impact-time producer maps to Mira ordinary6-shot. These cannot be invented from 200ms shot interval or old CE03 ability. Plan should either identify the existing approved missile/impact source and stable effect IDs, or explicitly authorize the bounded LOCAL direct-impact kernel at each shot emission for this consumption test; recommend the latter only as an explicit LOCAL decision, never operating ballistics policy. No velocity invented, no assumption that target lock is initial damage execution.

**Required Unity implementation once exact source boundary is accepted:** actual B actor ACTIVE acceptance/burst/recovery/HP/effect event adapter; original queue/Ready/clock and versioned running checkpoint with accepted command/due/target/snapshot/reservation+committed cursors. U13 supplies timing in CE03 validation host only; its177PASS is not this adapter. Original script skill configuration's five-operator requirement cannot be bypassed by supplying an Eve heal kit. Full operating profile/multi-target battlefield/long-run/Player/View remain outside this LOCAL source.

Therefore `U-B-ACTIVE-CONSUME-14` is **not registered** yet. S14 remains partial for exact missing impact boundary, not user approval or unrelated whole-server completion. No repeat U13 or older completed source jobs.
