# Unity Editor Environment Discovery - 2026-08-30

This launch package pass resolved only the approved Unity 2D Animation package and the built-in UnityWebRequest module needed by existing client adapter code. It did not install SDKs, configure OAuth values, install secure storage plugins, or run Android device operations.

## Project Hints

- Project path checked: `unity-project`
- Project editor version: `6000.3.22f1`
- Project editor revision: `6000.3.22f1 (1c726e1fb402)`
- `Packages/manifest.json` exists.
- Declared packages include `com.unity.2d.animation`, `com.unity.2d.sprite`, `com.unity.inputsystem`, `com.unity.modules.unitywebrequest`, `com.unity.test-framework`, `com.unity.textmeshpro`, `com.unity.ugui`, and `com.unity.visualscripting`.
- `com.unity.2d.animation` is resolved as `13.0.0` for Unity `6000.3.22f1`.

## Local Editor Hints

- `D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe` was discovered.
- `C:\Program Files\Unity\Hub\Editor` was not found.
- `Unity`, `unity`, and `UnityHub` were not found on PATH.
- Registry version hints exist for `2021.3.20f1`, `6000.0.68f1`, and `6000.3.22f1`, but no executable location was available from those keys.
- `C:\Users\dlstj\AppData\Local\Unity\Editor` contains logs only; no `Unity.exe` was discovered there.
- Unity Hub application data exists under `C:\Users\dlstj\AppData\Roaming\UnityHub`, but no `editors.json` installation list was discovered by the read-only search.

## Dry-run Result

- Unity batchmode compile smoke was run with `6000.3.22f1`.
- Current dry-run status: `BATCHMODE_COMPILE_SMOKE_PASSED`.
- Unity EditMode tests were run with `6000.3.22f1`: `501` total, `501` passed, `0` failed.

## Editor Warnings To Check During Binding Pass

- `Assets/_LASTRAIL/Scripts/Presentation/MissionShell/MissionShellScreenRootBehaviour.cs.meta` reported an invalid GUID and should be regenerated or repaired before scene/prefab binding.
- Input System emitted `Invalid value of 'activeInputHandler' setting: -1`; confirm Player Settings input handling during the next Editor configuration pass.
- TextMeshPro package is deprecated in Unity `6000.3`; keep the existing dependency for compatibility until UI package migration is intentionally scheduled.

## Remaining Handoff Blockers

- `editor_device_screenshot_qa_unavailable`
- Existing launch checklist blockers still apply for native vibration, Android Keystore secure storage plugin, Google SDK/OAuth app registration, and Android build smoke.
- `scene_prefab_binding_pending`
- `native_vibration_bridge_pending`
- `android_keystore_device_validation_pending`
- `google_sdk_oauth_app_registration_pending`
- `android_build_smoke_pending`

## Boundaries

- Approved launch package resolve only: `com.unity.2d.animation` and built-in `com.unity.modules.unitywebrequest`.
- No Google SDK or OAuth values.
- No secure storage plugin install.
- No Android device operation.
- No PlayerPrefs token storage.
- No account shortcut headers/body/query.
- No reward, economy, story, offline reward, gacha probability, ledger, or server truth calculation.
- No raw token, account id, device id, raw asset path, RNG/probability, reward amount, ledger id, full localized copy, or server submit payload in debug output.
