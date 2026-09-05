# Solo And Online Cooperative Mode Policy

## Status

Consolidated user decisions; this is not implemented networking or release approval. World Raid mechanics, tickets, and settlement are in [world-raid-rules.md](world-raid-rules.md).

## Content Matrix

| Content | Solo | Online two-player | Difficulty |
| --- | --- | --- | --- |
| Story and story raid | Yes | No | Existing story rules |
| Four basic operation types | Yes | Yes | Existing operation difficulty tiers |
| World Raid | Yes | Yes | None; event boss and mode only |

The exact four operation type IDs must be recovered from authoritative design/source before routing or migrations are written. The six Act 1 mission IDs are missions, not evidence of four operation categories. Do not invent names or use Rail Choice Events as a category.

## Roles And Assets

- Each participant uses their own phone/account: one train role and one squad role.
- Train player uses their own train, parts, and weapons. Squad player uses their own five operators with their upgrades/equipment.
- Preserve the external/internal split; role-specific emphasis and controls may differ. Inside the train, allies remain in the left region with the accepted 3/2 arrangement and enemies on the right.
- Solo focuses on squad control plus train support commands. Solo mechanics must be sequentially solvable; cooperative versions may require complementary actions within a shared window.
- Changing role/operation/difficulty/loadout before departure clears both ready states and triggers eligibility/ownership revalidation. No role or asset exchange during combat.
- No asset ownership transfer. Historical ranked builds show only the score-producing public loadout, never current inventory presented as history.

## Invitation Codes

- Server issues four numeric digits, including leading zeroes, unique among active code-bearing lobbies within the lookup routing scope.
- A code discovers a lobby and submits a join request; it does not grant membership. Require login and host approval before admission.
- Show only safe display names before approval. Provide copying/sharing, request rejection, pre-combat removal, code regeneration, expiry, and server rate limits.
- Retire the code on start, closure, or expiry. Code exhaustion must return controlled unavailability, never overwrite another active lobby.
- Bind approvals to immutable room ID and code generation so a stale request cannot enter a different room that later reuses the digits.
- Reconnection uses original membership, not the code. No new participant joins an active attempt. Host disconnection does not terminate a server-owned fight.
- Exact code TTL/rate-limit configuration requires implementation measurement. Local test constants must not silently become production policy.

## Random Matching And Carry

- Random matches require the same operation ID, selected difficulty, mode/rules version, complementary roles, and independent eligibility for both accounts. Combat power is not a filter.
- Waiting longer cannot relax content/difficulty eligibility. A player choosing either role can be assigned one role before readiness, not two roles at once.
- World Raid matches the same event/boss/mode/rules version and complementary roles with common content eligibility. It has no difficulty filter.
- Invited ordinary-operation parties may carry a basic-content-eligible player below the top two difficulty tiers if at least one participant has the selected difficulty unlocked.
- At the top two ordinary-operation tiers, both participants must qualify independently. Random matching has no carry-admission exception at any tier.
- Low power/damage does not reduce a valid player's reward. Do not require both roles, individual last hits, or minimum damage to complete quests.
- Carry completion counts for eligible rewards/quests, not automatic story or chained-difficulty unlocks. Existing unlock authority remains intact.
- Recheck all entry constraints at start. An invite exception cannot be carried into a random match by changing queue or role.

## Recurring Quests

- Solo-only, cooperative-train-only, cooperative-squad-only, and mixed play can each obtain the full recurring reward track.
- Share progress and claim state across modes. Do not create separately required mode objectives or doubled payouts.
- No recurring requirement to invite a friend, play both roles, clear solo-only story, meet personal damage/kill quotas, or complete both modes.
- Use equivalent completion units: two complementary actions of one cooperative mechanic count as one completion.
- World Boss has one weekly participation objective, no daily duplicate, no win requirement, and no mechanic-success quota. Its direct reward is the World Raid Participation tier, not another duplicate bundle.
- Server validates events, reset boundaries, progress, and grants idempotently. The World Raid ticket budget does not replace ordinary-operation entry costs.

## Implementation And Release Boundaries

- First engineering target: two distinct authenticated local clients, real command/snapshot transport, authoritative server state, and existing Unity HUD assets in an isolated test encounter.
- Deterministic fixture combat is evidence of protocol/state flow, not a finalized skill engine, real player inventory, public matchmaking, or production account progression.
- No new art/video is required for this proof. Screenshot fixtures alone do not prove actual controller/transport wiring.
- Remote migrations/seeds, live infrastructure, secrets/OAuth registration, production deployment, store/signing, and video remain subject to their existing separate approvals.
