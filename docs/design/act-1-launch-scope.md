# Act 1 Launch Scope

This document is the current planning source of truth for Act 1 launch scope.

## Fixed Launch Decisions

- Google login is required for launch.
- Unity uses Google Sign-In Unity SDK.
- Guest play remains supported, and guest accounts must be linkable to Google later.
- Automatic account merge is not part of launch.
- Android token storage should use Android Keystore through a vetted Unity secure storage plugin when practical.
- Gacha UI and reveal animation are included in launch.
- Combat target is fully animated 2D sprite combat with readable HUD and touch UI.
- Act 1 visual scope includes 10 elite operators.
- Ordinary Act 1 operations use the 15-wave structure.
- Ordinary operation augments/tactics are granted or selected at run start, not mid-wave.
- `raid_raider_king_engine` is a boss-only raid.
- Raid starts from the same full augment/tactic package expected after an ordinary 15-wave build, then plays the boss encounter only.
- Codex/archive is required for launch.
- Localization uses string ids.
- Sound scope is SFX plus one BGM loop.
- Mobile target is portrait 9:16 with safe area support and vibration.
- Admin pages and nav stay visible, while Dashboard should be trimmed to useful launch summaries.
- CS templates are read-only suggestions in the support UI. Copy, send, and ticket actions are excluded.
- Ops error write integration should be minimal and limited to critical server failures.
- Rail Choice Events are excluded from Act 1 launch and remain backlog.

## Server Launch Work

- Google auth sign-in endpoint and contract.
- Guest to Google link or upgrade contract.
- Existing Google identity sign-in and restore.
- Google auth security tests: no raw token storage, no raw token logs, no account id shortcuts.
- Raid boss-only run start and finish contract.
- Ordinary 15-wave augment-at-start contract.
- Gacha launch data and response stability smoke.
- Minimal ops error write integration for critical failures.
- Login and session metrics read model for Dashboard and support/player detail.
- Full launch regression for auth, run finish, reward, story progress, offline reward, gacha, Admin read-only, migrations, and secret scanning.

## Unity Launch Work

- Google SDK integration, ID token provider, social auth flow, guest upgrade, restore UX.
- Secure token storage and auth-aware Game API transport.
- Fully animated combat scene for ordinary 15-wave operations.
- Boss-only raid combat scene and boss HUD.
- 10 operator portraits, icons, combat sprites, and battle-ready animation minimum set.
- Enemy and raid boss animation minimum set.
- Gacha UI and reveal animation.
- Mission select, briefing, loading, battle, result, reward, offline reward, story progress, and codex/archive UI.
- String-id localization lookup, fallback, and missing-content handling.
- Mobile safe area, touch target, vibration, SFX, BGM, and performance pass.

## Admin Launch Work

- Google provider/link/account recovery read-only visibility.
- Dashboard trim while keeping all Admin pages visible.
- Login/session metrics on Dashboard and support/player detail.
- Gacha support visibility.
- Boss-only raid labels and run evidence.
- Augment-at-start read-only evidence.
- CS template read-only suggestions.
- Ops error logs read-only list/detail/filter aligned with minimal write sources.
- Forbidden-field and mutation-control absence verification.

## Decision Queue

These decisions should be surfaced to the user when they return, but they should not block unrelated launch-safe work:

- Google OAuth client ids and allowed audiences for Android and future platforms.
- Exact Google link endpoint shape and idempotency requirement.
- Whether Google email is stored at all; current recommendation is no email storage for launch.
- Animation toolchain: frame animation, skeletal/tween, Spine, or Unity 2D Animation package.
- Ultimate cue scope and per-character unique animation count.
- Target Android device class, target FPS, texture memory, and VFX budget.
- Raid contract shape: extend existing run lifecycle with `encounterType` or create a raid-specific contract.
- Augment-at-start exact schema and server validation boundary.
- Gacha launch banner/pool/weights/pity/duplicate conversion data.
- Economy draft promotion into live launch config or seed.
- DEV/internal-test deploy and smoke timing.

## Backlog

- Rail Choice Events.
- Account merge, social unlink, and additional social providers.
- Repair, manual compensation, reward repair, economy correction, story unlock mutation, gacha reroll.
- Full combat replay, frame timeline upload, server-owned animation timeline.
- Advanced boss telemetry and animation performance analytics.
- Queue/R2/Workers Logs ops error pipeline.
- Full localization publishing console and narrative editing.
- Dynamic music, voice acting, long cinematic cutscenes, 3D full models, skins, and equipment visual variants.
