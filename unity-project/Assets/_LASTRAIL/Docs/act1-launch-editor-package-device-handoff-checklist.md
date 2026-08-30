# Act 1 Launch Editor, Package, and Device Handoff Checklist

This checklist moves the Unity code-readiness slices into Editor/package/device QA without opening new server, admin, narrative, reward, economy, gacha probability, or world raid scope.

## Required Order

1. Resolve `com.unity.2d.animation` in Unity Package Manager.
2. Bind MissionShell, Gacha, Combat HUD, Codex/archive, RewardReview, OfflineReward, and StoryProgressSummary scene or prefab roots.
3. Run portrait 9:16 screenshot QA for all 7 player-facing screens.
4. Validate Android native vibration bridge on device.
5. Select/import Android Keystore-backed secure storage plugin and validate save/load/overwrite/clear.
6. After app registration, import/configure Google Sign-In SDK and OAuth client settings.
7. Run Android build smoke on a target device.

## Current Code Readiness

- Presenter/view-state boundaries exist for the 7 player-facing screens.
- Pure readiness/debug gates exist for UI safe area, touch targets, text overflow, VFX budgets, audio cues, vibration cues, secure storage, Google SDK readiness, and Android vibration bridge readiness.
- Debug outputs are ids, status, counts, and blockers only.

## Approval Or Device Blockers

- `com_unity_2d_animation_package_resolve_pending`
- `scene_prefab_binding_pending`
- `editor_device_screenshot_qa_unavailable`
- `native_vibration_bridge_pending`
- `android_keystore_secure_storage_plugin_pending`
- `google_sdk_oauth_app_registration_pending`
- `android_build_smoke_pending`

## Boundaries

- Do not add Google OAuth values until app registration/PRD setup.
- Do not store tokens in PlayerPrefs.
- Do not add account shortcut headers, body fields, or query parameters.
- Do not include raw token values, account ids, device ids, raw asset paths, RNG/probability data, reward amounts, ledger ids, full localized copy, or server submit payloads in debug output.
- Do not add Rail Choice Events or World Raid UI to Act 1 launch.
