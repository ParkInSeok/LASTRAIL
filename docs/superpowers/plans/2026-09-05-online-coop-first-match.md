# Online Cooperative First Match Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development or superpowers:executing-plans when implementing this plan. Checkboxes represent work not yet executed.

**Goal:** Verify one isolated local World Raid attempt with two independently authenticated Unity clients, authoritative server commands/snapshots, and identical finalized team score without granting real rewards.

**Architecture:** Separate pure policy, protocol, local session authority, and Unity presentation. Reuse the existing Hono/TypeScript and Unity HTTP/auth components where their contracts fit; do not route World Raid through story run result submission. A local serialized room authority must own the timer/state and reject client-declared damage/score.

**Tech Stack:** Existing TypeScript, Hono, Zod, Vitest, Wrangler; Unity 6000.3.22f1, C#, UnityWebRequest, TMP, Input System. A local Durable Object-backed room is a candidate for serialized authority, not an already installed binding. Validate local runtime support before selecting it; do not use an unprotected Worker global as durable shared authority.

**Spec:** [Mode policy](../../design/online-coop-mode-policy.md), [World Raid rules](../../design/world-raid-rules.md).

## Global Constraints

- Runtime repository: `E:/Projects/LASTRAIL`; server root `server`; Unity root `unity-project`. Planning repository is `C:/Users/dlstj/OneDrive/문서/ChatGPT/LASTRAIL`.
- Planning does not authorize remote infrastructure, paid services, production deployment, remote seed/migration, real currency grants, APK signing, or video capture.
- Preserve unrelated dirty changes. Use an isolated execution worktree only when needed and verify Unity asset/reference paths before copying anything.
- Story is solo-only. World Raid has unlimited boss HP, no difficulty selector, one train role/one squad role, server-owned 180-second first-test deadline and phases at 120/60/0 seconds.
- No new art is necessary. Preserve allied left-region 3/2 and enemy right-region positions.
- Four operation category IDs are not verified. Do not identify them with the first four `opn_` story missions. Ordinary-operation routing cannot be marked complete before obtaining authoritative IDs.
- Existing skill/ultimate definitions and live damage producers are not established. A deterministic local fixture must be explicitly labeled test combat, not production skill implementation.
- Pure policy evidence, two HTTP test clients, a Unity fixture screenshot, and two actual Unity processes are distinct milestones. Report each separately.

## Existing Files Verified

| Path relative to runtime repository | What it actually supplies |
| --- | --- |
| `server/apps/game-api/src/index.ts` | Hono HTTP auth, profile, story/run and reward endpoints; no observed room authority |
| `server/apps/game-api/wrangler.jsonc` | D1/local/dev/prd config; no current Durable Object room binding |
| `server/package.json` | Vitest/typecheck/lint scripts; no confirmed cooperative transport |
| `unity-project/Assets/_LASTRAIL/Scripts/Run/UnityWebRequestRunApiHttpAdapter.cs` | Base URL and HTTP endpoint transport |
| `unity-project/Assets/_LASTRAIL/Scripts/Auth/GameApiAuthenticatedHttpTransport.cs` | Bearer application; not full session orchestration |
| `unity-project/Assets/_LASTRAIL/Scripts/Run/RunApiFlowFacade.cs` | Act 1 local wave/run flow; not a cooperative authority |
| `unity-project/Assets/_LASTRAIL/Scripts/Battle/BattleSceneRoot.cs` | Local SimulationClock tick; not authoritative online World Raid time |
| `unity-project/Assets/_LASTRAIL/Scripts/Presentation/Battle/FullyAnimatedCombatHudViewState.cs` | Presentation-only state; hard-coded story boss defeat/refund labels |
| `unity-project/Assets/_LASTRAIL/Scripts/Run/RunEncounterType.cs` | Ordinary15Wave/RaidBoss only; do not reinterpret RaidBoss as World Raid |
| `unity-project/Assets/_LASTRAIL/Scripts/Presentation/Startup/StartupScreenRootBehaviour.cs` | RenderAuth and callbacks; not a confirmed runtime flow owner |
| `unity-project/Tools/run-editmode-tests.ps1` | No `-quit`, but currently includes `-nographics`; do not use unchanged for this project's graphics/license-sensitive checks |

## Checkpoint 1: Executable Server Policies

**Create:** `server/packages/common/src/coop/world-raid-policy.ts`, `ranking-settlement.ts`, `ticket-policy.ts` and `server/tests/unit/coop-policy.test.ts`.
**Boundary:** No routes, DB writes, exports into existing gameplay, or time obtained from client input. Tests pass explicit trusted clock values.

Proposed pure interfaces:

```ts
export type RaidPhase = 1 | 2 | 3 | "ended";
export function phaseAt(startMs: number, durationMs: number, nowMs: number): RaidPhase;
export function recoveryMs(previousDowns: number): number;
export type RankedTeam = {
  attemptId: string;
  members: readonly [string, string];
  eligible: readonly string[];
  score: bigint;
  completedAtMs: number;
};
export type RewardAssignment = { accountId: string; position: number; attemptId: string };
export function assignTeamRewards(rows: readonly RankedTeam[]): RewardAssignment[];
export function accrueTickets(stock: number, elapsedDailyBoundaries: number): number;
```

Contracts: reject negative/nonfinite clocks, invalid counts, self-pairs, duplicate members, eligible accounts outside the pair, and conflicting repeated attempt IDs. `assignTeamRewards` is scoped to one frozen board; sort by score descending, completion ascending, attempt ID ordinal. Deduplicate to the best row per unordered pair before assigning. Ineligible members neither receive nor occupy reward positions. `accrueTickets` treats stock above cap as a retained owed refund and adds no regeneration.

- [ ] Add failing Vitest assertions with actual boundary values:

```ts
expect(phaseAt(0, 180000, 59999)).toBe(1);
expect(phaseAt(0, 180000, 60000)).toBe(2);
expect(phaseAt(0, 180000, 120000)).toBe(3);
expect(phaseAt(0, 180000, 180000)).toBe("ended");
expect([0, 1, 2, 10].map(recoveryMs)).toEqual([8000, 12000, 16000, 16000]);
const row = (attemptId: string, a: string, b: string, score: bigint): RankedTeam => ({
  attemptId, members: [a, b], eligible: [a, b], score, completedAtMs: 180000
});
const assigned = assignTeamRewards([
  row("r1", "A", "B", 400n), row("r2", "A", "C", 300n),
  row("r3", "B", "C", 200n), row("r4", "D", "E", 100n)
]);
expect(assigned.map(x => [x.accountId, x.position])).toEqual([
  ["A", 1], ["B", 1], ["C", 2], ["D", 3], ["E", 3]
]);
expect(accrueTickets(9, 1)).toBe(10);
expect(accrueTickets(12, 3)).toBe(12);
```

- [ ] Run from `server`: `pnpm exec vitest run tests/unit/coop-policy.test.ts`; confirm relevant failures, then implement pure helpers only. Use integer/fixed-precision damage and `bigint` internally; never serialize bigint directly as JSON.
- [ ] Add permutation, duplicate-result, role-swap, ineligible-member, all-ineligible, daily reset/cap, and over-cap refund assertions. Two calls over the same frozen rows must produce the same assignments regardless of reward claim order.
- [ ] Verify score tiers use cumulative differences (Bronze to Silver grants 700 credits/50 parts/25 data), never all lower rows again. Better cross-mode ranking bundle pays once; selection cannot rerun cooperative promotion.
- [ ] Run focused tests, then `pnpm typecheck` and `pnpm lint`. Review/commit only these policy files and tests. Mark this as policy verification, not online gameplay.

## Checkpoint 2: Shared Protocol And Fixture Contract

**Create:** `server/packages/common/src/coop/protocol.ts`, `server/tests/unit/coop-protocol.test.ts`, `server/docs/api/coop-local.md` and `server/tests/fixtures/coop-local/` canonical request/response fixtures.

**Produces:** Versioned JSON parsed by both TypeScript and Unity; proposed names below are design interfaces, not existing endpoints.

```ts
type CoopRole = "train" | "squad";
type RoomState = "waiting" | "ready" | "running" | "ended";
type Command = {
  protocolVersion: 1; commandId: string; roomId: string;
  attemptId: string; expectedRevision: number;
  kind: "intercept" | "release_anchor" | "request_support";
};
type Snapshot = {
  protocolVersion: 1; roomId: string; attemptId: string;
  rulesVersion: string; sequence: number; serverNowMs: number;
  startedAtMs: number; deadlineMs: number; phase: 1 | 2 | 3 | "ended";
  assignedRole: CoopRole; members: { displayName: string; role: CoopRole }[];
  trainHp: number; operatorHp: { operatorId: string; hp: number }[];
  mechanic: null | { id: string; deadlineMs: number; state: "active" | "success" | "failure" };
  buff: null | { eventId: string; multiplierBps: number; expiresAtMs: number };
  scoreFixed: string; scoreScale: number; scoreFinal: string | null;
  connection: "connected" | "grace" | "partner_absent";
  finalReason: null | "time_limit" | "early_end" | "server_invalid";
};
```

Command identity and role come from authenticated membership, not a supplied accountId. Commands contain intentions, never damage/score/buff declarations. `scoreFixed` is a bounded decimal integer string and `scoreScale` is fixed by version; validate bounds in both clients without floating-point rounding. Member/loadout snapshot fields must include the established public assets required for rendering but no tokens/internal diagnostics.

- [ ] Add strict schema tests: preserve `"0037"`, reject numeric code `37`, unknown command kind, forged role fields, negative time, self-pair, oversized payload, unsupported version, and client score fields.
- [ ] Define route shapes for create room, code request, host approve/reject, role/loadout choice, ready, start, snapshot, command, result, and reconnect in `coop-local.md`. Use opaque room/request IDs and code generation to reject stale approvals. Start/approval/commands require idempotency keys.
- [ ] Publish paired fixtures for 120/60/0-second boundaries, active/success/failure mechanic, buff expiration, disconnected/ineligible member, and final combined score. Set `rulesVersion=coop-local-test-v1`, test-only account inventory, no real grants.
- [ ] Define exact server ordering: attempt deadline ends before incoming gameplay commands at that instant; phase boundary retires the old unfinished mechanic before new commands; expired buffs do not apply; valid pre-boundary failures resolve once. Do not leave ordering to client request arrival races.
- [ ] Both owners review names/required fields and fixture parsing before server endpoints or Unity DTO implementations begin. Commit protocol/spec/fixtures together; this is not an API deployment.

## Checkpoint 3: Local Serialized Room Authority

**Proposed files:** `server/apps/coop-local/src/index.ts`, `room-authority.ts`, `invite-registry.ts`, `wrangler.jsonc`, and `server/tests/integration/coop-local-smoke.test.ts`.
**Modify only if needed:** local test workspace/package wiring, not live `apps/game-api` routes or deployed bindings.

This checkpoint first validates the local transport/authority approach. Prefer a local serialized room object and separate serialized code registry consistent with the existing Workers stack. Read current official/runtime documentation during implementation. If the existing runtime cannot host it, record the exact blocker before changing architecture; do not pretend module-global memory is restart-safe authority.

- [ ] Prove local authentication with two separately provisioned test accounts. Reuse existing server verifier helpers where accessible without exposing secrets. No production tokens, query-string bearer tokens, or unrestricted debug impersonation endpoint.
- [ ] Implement authenticated HTTP command + revisioned snapshot transport for the first pass using existing Unity HTTP capability. Start with 250ms snapshot polling as a local test parameter; cap one in-flight poll per client, rate-limit, measure latency, and use a transport interface so later streaming is possible. This is not final real-time responsiveness approval.
- [ ] Implement four-digit discovery with randomized allocation/retry, explicit exhaustion, host acceptance, immutable request/room/generation IDs, expiry, and membership max two. Local TTL/rate-limit constants are test settings documented in the contract, not production decisions.
- [ ] Serialize ready/asset/role/start transitions. Simulate per-account ticket debit in a local test ledger with a single start transaction; repeat starts return the original attempt, never an extra debit. Real multi-account inventory transaction design is a later production gate.
- [ ] Run one test mechanic: train `intercept`, squad `release_anchor`; accept each only in its role/window. Server-owned configured fixture actions produce deterministic damage; successful combined actions produce the configured shared buff once. Never accept a client-reported success or damage amount.
- [ ] Advance time with trusted clock; reads/ticks catch up missed deadlines once. Store revision/event IDs so two clients cannot duplicate penalties or rewards. Test controlled local authority restart and reject obsolete attempt/rules versions.
- [ ] Implement original-member reconnect, latest snapshot, 30/60-second grace, no timer/buff reset, and early-end vs voluntary-abandon distinction. Eligibility is recorded from explicit test evidence; it is not a released anti-AFK classifier.
- [ ] At deadline write one deterministic local result and dry-run reward assignments only. Assert real wallet/claim writes are zero and no production DB binding exists in this test worker.
- [ ] Run two independent HTTP clients against the local endpoint: request/approve/ready/start, wrong-role rejection, duplicate command, skewed client clock, disconnect/reconnect, exact final score. Capture machine-readable request IDs/revisions/results with auth material redacted. This passes protocol integration only.

## Checkpoint 4: Actual Unity Controllers

**Create:** `unity-project/Assets/_LASTRAIL/Scripts/Coop/{CoopProtocol,ICoopTransport,CoopHttpTransport,CoopSessionController,CoopLocalBootstrap}.cs`.
**Create:** `Scripts/Presentation/Coop/{CoopLobbyView,WorldRaidHudViewState,WorldRaidHudPresenter}.cs` under the same scripts root and `Tests/EditMode/CoopSessionTests.cs`.
**Modify narrowly:** existing `FullyAnimatedCombatHudRootBehaviour.cs`/prefab builder only to expose reusable view/input surfaces; preserve existing story adapter and labels. Use a separate local entry prefab/scene, not global startup build-scene replacement.

Proposed client interface:

```csharp
public interface ICoopTransport
{
    System.Threading.Tasks.Task<CoopSnapshot> ReadAsync(
        string roomId, System.Threading.CancellationToken cancel);
    System.Threading.Tasks.Task<CoopSnapshot> SendAsync(
        CoopCommand command, System.Threading.CancellationToken cancel);
}
// CoopSnapshot and CoopCommand mirror the reviewed protocol, not story RunStartRequest.
```

- [ ] Parse the canonical fixtures from Checkpoint 2; add NUnit failures for unknown protocol, decimal score overflow, stale snapshots, previous attempt snapshot, and lost leading zeroes. Copy no private server account identifiers into public UI.
- [ ] Add controller state transitions for idle/requested/admitted/ready/running/reconnecting/ended. Use two isolated test token stores. Reuse Bearer transport without rewriting login/OAuth/token storage.
- [ ] Wire create/code request/host approve/role/readiness to server acknowledgments. Do not mark ready/start locally before acceptance. Reject role changes during running on both UI and server.
- [ ] Replace fixture-only icon actions with local test command callbacks owned by the controller. Authority responses update HUD; local click/VFX cannot resolve a mechanic, alter HP, or add score.
- [ ] Add World Raid view state with remaining time, phase, score, HP, mechanic/buff deadlines, and role. Do not cast it to story `RaidBoss` or show boss-defeat/refund labels. Do not run `BattleSceneRoot` local simulation as online authority.
- [ ] Test snapshot revision ordering, cancellation on disable, lost response/retry with same commandId, phase transitions without repeated cues, buff expiry during reconnect, and identical terminal result parsing.
- [ ] Render 1080x1920 and 360x640 with actual authority snapshots; label screenshots by their real input source. Preserve formation and battle background. No video without a separate user request.
- [ ] Run focused tests with graphics batchmode, no `-nographics` and no `-quit` when using `-runTests`. Require fresh XML and failed count zero, not only process exit 0. Existing wrapper currently needs a graphics option or a direct command.

Example direct verification command (execution phase only):

```powershell
& 'D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe' -batchmode -projectPath 'E:\Projects\LASTRAIL\unity-project' -runTests -testPlatform EditMode -testFilter 'LastRail.Tests.EditMode.CoopSessionTests' -testResults 'E:\Projects\LASTRAIL\unity-project\TestResults-CoopSession.xml' -logFile 'E:\Projects\LASTRAIL\unity-project\Unity-CoopSession.log'
```

## Checkpoint 5: One Real Two-Client Attempt

**Evidence:** `unity-project/QA/Act1Launch/Reports/coop-local-first-match.json` and optional user-requested screenshots; no video. A detailed log must omit tokens and raw auth headers.

- [ ] Run two independent Unity clients against the same local authority with distinct test accounts. Do not open two editors on one Unity project checkout; use an isolated test copy or an existing safe client build path. If neither is available, report this checkpoint unexecuted instead of relabeling HTTP clients.
- [ ] Client A creates a room, B requests by code, A approves, roles select their own fixture assets, both ready, server starts exactly one attempt.
- [ ] Verify both observe the same attempt/config/deadline/phase/score. Send a wrong-role command and confirm authoritative rejection without side effects.
- [ ] Complete one cooperative mechanic, then fail one. Verify one shared buff, one declared target penalty, down/recovery behavior, phase transition, and no post-deadline damage.
- [ ] Disconnect/reconnect one original account, restore current snapshot, and verify no second cost, phase rewind, role switch, or replayed buff. Separately exercise voluntary abandonment and permitted remaining-player early end.
- [ ] Compare finalized score/result IDs on both clients. Record local ledger debits, dry-run assignments, real reward writes=0, elapsed time, and whether the input was deterministic test combat.
- [ ] Review evidence against the spec before broadening. This proves one local networked loop, not phone-device readiness, production combat balance, public random matching, or full Act 1 integration.

## Follow-On Work Coverage

| Requirement | Gate before broad implementation |
| --- | --- |
| Four operation types | Obtain authoritative category IDs/definitions; add invitation/random admission matrix tests without substituting story missions |
| Solo mechanics | Add solo adapter for the same boss schedule; prove sequential inputs and no unavailable mandatory support |
| Public random queue | Exact operation/difficulty or world event/boss key, complementary roles, independent eligibility, no power matching; queue concurrency/cancel tests |
| Real inventory/tickets | Local schema/transaction plan, atomic two-account charge/refund and trusted ownership; no remote migration without approval |
| Real skill/damage/boost engine | Server-owned catalog and battle producer, including cross-role buff composition; fixture logic cannot substitute |
| Actual ranking/rewards | Persist frozen score/eligibility/loadout, execute promotion/idempotent grants, review post-settlement invalidation and privacy |
| Quest integration | One shared solo/cooperative progress track, weekly participation only, no duplicate direct reward; test all role-only paths |
| Phone latency/device QA | Two separate phones or approved emulators, measured network loss/clock skew/readability; do not infer from Editor |
| Production release | Explicit infrastructure/secret/deploy approval and release branch/tag procedure |

## Review Outcome And Current Status

All checkboxes are initially unchecked. This plan is a staged implementation order with concrete first-checkpoint interfaces/tests; transport selection and full gameplay definitions are engineering gates, not existing capabilities. Do not execute later checkpoints in parallel before their contract dependencies exist.

The planning pass reads source and creates documents only. It does not run a server, start Unity, record video, change assets, add packages, create live services, or grant rewards.
