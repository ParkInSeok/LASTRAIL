# Act 1 Launch Editor, Package, and Device Handoff Checklist

This checklist moves the Unity code-readiness slices into Editor/package/device QA without opening new server, admin, narrative, reward, economy, gacha probability, or world raid scope.

## Required Order

1. Resolve `com.unity.2d.animation` in Unity Package Manager. Completed for Unity `6000.3.22f1` with package version `13.0.0`.
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

## Unity Test Runner CLI Rule

- For Unity `6000.3.22f1`, do not combine `-runTests` with `-quit`.
- Unity Test Framework exits the Editor after completing command-line tests. Adding `-quit` can stop the process after compilation before `CommandLineTest.Executer.InitializeAndExecuteRun` starts, leaving no requested XML result.
- Use `Tools/run-editmode-tests.ps1` or the documented no-quit command in `Assets/_LASTRAIL/Docs/unity-test-runner-cli-readiness-2026-09-01.md`.
- ExecuteMethod validation and screenshot QA commands may still use `-quit`; this rule is specific to `-runTests`.

## Approval Or Device Blockers

- `scene_prefab_binding_pending`
- `editor_device_screenshot_qa_unavailable`
- `native_vibration_bridge_pending`
- `android_keystore_device_validation_pending`
- `google_sdk_oauth_app_registration_pending`
- `android_build_smoke_pending`

## Editor Warnings To Clear

- Repair or regenerate `Assets/_LASTRAIL/Scripts/Presentation/MissionShell/MissionShellScreenRootBehaviour.cs.meta`, which Unity reports as an invalid GUID.
- Confirm Player Settings input handling because Unity Input System reports `activeInputHandler = -1` during batchmode test startup.

## Boundaries

- Do not add Google OAuth values until app registration/PRD setup.
- Do not store tokens in PlayerPrefs.
- Do not add account shortcut headers, body fields, or query parameters.
- Do not include raw token values, account ids, device ids, raw asset paths, RNG/probability data, reward amounts, ledger ids, full localized copy, or server submit payloads in debug output.
- Do not add Rail Choice Events or World Raid UI to Act 1 launch.
