# Act 1 Launch Scope

This document is the current planning source of truth for Act 1 launch scope.

## Fixed Launch Decisions

- Google login is required for launch, but real Google OAuth client ids, allowed audiences, secrets, and remote smoke are deferred until app registration / production-readiness work.
- Unity uses Google Sign-In Unity SDK.
- Guest play remains supported, and guest accounts must be linkable to Google later.
- Automatic account merge is not part of launch.
- Android token storage uses Android Keystore through a vetted Unity secure storage plugin when practical.
- Gacha UI and reveal animation are included in launch.
- Combat target is fully animated 2D sprite combat with readable HUD and touch UI.
- Act 1 visual scope includes 15 operators: 5 initial/free operators plus 10 gacha elite operators.
- Ordinary Act 1 operations use the 15-wave structure.
- Ordinary operation augments/tactics are granted or selected at run start, not mid-wave.
- `raid_raider_king_engine` is a boss-only raid.
- Raid starts from the same full augment/tactic package expected after an ordinary 15-wave build, then plays the boss encounter only.
- Story raid uses the same 5 operator slots as ordinary Act 1 operations.
- Story raid failure refunds stamina, while story raid success is based on verified boss defeat.
- World Raid is a separate future competitive mode, not the Act 1 story raid contract.
- `POST /v1/runs/start` should require `Idempotency-Key` before production traffic.
- Gacha launch uses one Act 1 pickup banner, `cur_command_ticket`, 1-pull and 10-pull buttons, rare 10 / elite 30 / pickup elite 60 pity thresholds, and limited ticket acquisition from Act 1 first clear plus launch login bonus. Normal offline repeat rewards should not grant command tickets at launch.
- Recommended launch command ticket quantity is 12 total from non-repeat sources: 5 from launch login bonus and 7 from Act 1 first clears.
- Codex/archive is required for launch.
- Localization uses string ids.
- Sound scope is SFX plus one BGM loop.
- Mobile target is portrait 9:16 with safe area support and vibration.
- Admin pages and nav stay visible, while Dashboard should be trimmed to useful launch summaries.
- CS templates are read-only suggestions in the support UI. Copy, send, and ticket actions are excluded.
- Ops error write integration should be minimal and limited to critical server failures.
- Rail Choice Events are excluded from Act 1 launch and remain backlog.

## Server Launch Work

- Done: Google auth sign-in/link/restore contracts, production ID token verifier adapter, JWKS failure safety, and raw token safety tests.
- Done: `POST /v1/runs/start` skeleton, server-authored metadata response, `Idempotency-Key` requirement, preview consume race guard, and consume-success failure recovery.
- Done: Raid boss-only run finish contract and story raid failure stamina refund commit for `raid_raider_king_engine`.
- Done: Ordinary 15-wave augment-at-start contract and start package refs echo.
- Done: Gacha launch draft data, response stability smoke, and 12-ticket non-repeat acquisition path candidate docs/tests.
- Done: Minimal ops error write integration for critical failures.
- Done: Login and session metrics read model for support/player detail.
- In progress/remaining: final start package/contentVersion source alignment, live balance seed promotion decision, Google PRD app values later, and final full regression after Unity/Admin/Narrative surfaces settle.

## Unity Launch Work

- Done: Google/auth contract flow models, auth gate UX state, auth-aware transport boundary, UnityWebRequest adapter boundary, and run start client wiring.
- Done: Run start response metadata, start package refs, raid finish payload, and stamina refund display marker compatibility.
- Done: 15-operator visual manifest/audit and initial 5 versus gacha elite 10 roster separation.
- Done: Mission shell, narrative/localization presentation state, codex/archive presentation state, gacha reveal/screen presentation state, reward/offline/story display state, combat presentation cue state, and mobile HUD policy models.
- Done: Android Keystore secure token storage spec; PlayerPrefs token storage is forbidden.
- Done: Mission shell presenter/view boundary for mission select, briefing, run start loading, and result placeholder.
- In progress/remaining: vetted secure storage plugin or first-party bridge integration, Google SDK preparation, concrete client injection beyond run start, actual Unity scene/prefab binding, 2D Animation/Sprite Atlas asset pipeline, fully animated combat scene, story raid boss UI, gacha reveal UI, reward/offline/story/codex UI, SFX/BGM/vibration implementation, and device performance QA.

## Admin Launch Work

- Done: Google provider/link/account recovery read-only visibility.
- Done: Dashboard trim while keeping Admin pages/nav visible.
- Done: Login/session metrics on support/player detail, with exact concurrent users kept as backlog/unavailable.
- Done: Gacha support visibility, 15 visual versus 10 gacha elite separation, and 12-ticket acquisition policy display.
- Done: Boss-only story raid labels, run start metadata evidence, start package refs evidence, and stamina refund read-only evidence.
- Done: CS template read-only suggestions without copy/send/ticket actions.
- Done: Ops error logs read-only list/detail/filter aligned with minimal write sources, including `run_start_state_conflict`.
- Done: Story raid stamina refund evidence aligned to the committed `COMMITTED` marker and read-only mutation/ledger labels.
- Done: Forbidden-field and mutation-control absence guard plus preview smoke checklist.
- Remaining: final preview/browser smoke when an authenticated preview session is available, and small label corrections only when new server/narrative contract fields land.

## Narrative And Direction Launch Work

- Done: Act 1 story raid direction corrected to boss-only, 5 operators, failure stamina refund, and boss defeat success.
- Done: 15-operator voice guide.
- Done: Act 1 localization string-id map draft.
- Done: `opn_01` through `opn_03` dialogue polish and early mission coverage pass.
- Done: `opn_04` through `opn_06` dialogue polish and late Act 1 tension pass.
- Done: Story raid boss callout/failure refund wording polish.
- Done: Act 1 narrative launch checklist and battle callout density rules.
- Remaining: codex/archive copy final pass and reward framing localization candidate selection.

## Launch Progress Board

| Area | Status | Notes |
| --- | --- | --- |
| Server contracts/source of truth | 85% | Core launch contracts are green locally; remaining risk is final config/source promotion and PRD-only OAuth/deploy work. |
| Admin read-only ops/support | 90% | Main surfaces and regression guards are in place; authenticated preview smoke remains environment-dependent. |
| Unity client/presentation | 62% | Mission shell presenter work has started actual UI wiring; scene/prefab binding, assets, combat, SDK/storage, and device QA remain the main bottleneck. |
| Narrative/direction | 82% | Ordinary missions, story raid copy, localization map, operator voice guide, and checklist are aligned; codex final pass and reward framing selection remain. |
| Overall Act 1 launch readiness | 71% | Source-of-truth/backend is ahead; launch readiness now depends mostly on Unity implementation and final narrative/UI integration. |

## Decision Queue

These decisions should be surfaced to the user when they return, but they should not block unrelated launch-safe work:

- Google OAuth client ids and allowed audiences for Android and future platforms are deferred until app registration / production-readiness work.
- Exact Google link endpoint shape and idempotency requirement.
- Whether Google email is stored at all; current recommendation is no email storage for launch.
- Specific secure token storage plugin approval within the Android Keystore requirement, or approval to build a first-party Android native bridge.
- Unity launch version, Android min SDK, target SDK, ARM64, IL2CPP, and managed stripping baseline.
- Ultimate cue scope and per-character unique animation count.
- Final `startAugmentPackageRef`, `startTacticPackageRef`, and `contentVersion` metadata source.
- Economy draft promotion into live launch config or seed.
- DEV/internal-test deploy and smoke timing.
- JWKS failure retry/backoff/alerting thresholds.
- Cloudflare Access policy for hashed preview deployment URLs versus alias-only protection.
- Whether story raid legacy accounts without `fuel_state` need a bootstrap/repair policy, or should remain rejected with state conflict.
- Whether `opn_01` Commander direct speech and optional objective callouts stay internal/presentation-only or enter launch localization fixtures.
- Operator display names and final narrative version revision.
- World Raid implementation plan and live-service schedule remain future/backlog.

## Backlog

- Rail Choice Events.
- Account merge, social unlink, and additional social providers.
- Repair, manual compensation, reward repair, economy correction, story unlock mutation, gacha reroll.
- Full combat replay, frame timeline upload, server-owned animation timeline.
- Advanced boss telemetry and animation performance analytics.
- World Raid live-service implementation, ranking settlement, and season operations.
- Queue/R2/Workers Logs ops error pipeline.
- Full localization publishing console and narrative editing.
- Dynamic music, voice acting, long cinematic cutscenes, 3D full models, skins, and equipment visual variants.
