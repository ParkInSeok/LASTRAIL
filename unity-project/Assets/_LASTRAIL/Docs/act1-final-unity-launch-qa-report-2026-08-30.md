# Act 1 Final Unity Launch QA Report - 2026-08-30

This report summarizes the current Unity player-facing launch state for Act 1. It is intended for launch operators and device QA. It does not open new server, admin, narrative, reward, economy, gacha probability, Rail Choice Events, or World Raid scope.

## Completed Launch Path

| Area | Status | Commit | Verification |
| --- | --- | --- | --- |
| Unity Editor discovery and package readiness | GREEN | `6731f46`, `5f03139` | Unity `6000.3.22f1` discovered, `com.unity.2d.animation@13.0.0` resolved, EditMode `501/501` passed |
| Auth-aware transport and run start wiring | GREEN | `d8daf96`, `23829cd` | Covered by later full EditMode regression |
| Mission shell UI presenter | GREEN | `93d9719` | Covered by later presenter and screenshot QA |
| Gacha UI presenter | GREEN | `34b836f` | Covered by later presenter and screenshot QA |
| Combat HUD and story raid boss presenter | GREEN | `5be5e58` | Covered by later presenter and screenshot QA |
| Scene/prefab binding for seven screens | GREEN | `fd15fee` | Targeted binding tests `3/3`, full EditMode `504/504` passed |
| Seven-screen portrait screenshot QA | GREEN | `ec31069` | 7 PNGs at `1080x1920`, pass `7`, warning `0`, fail `0`, full EditMode `509/509` passed |
| Animation asset pipeline | GREEN | `3a5c5b9` | AssetDatabase verification passed: 10 atlases, 17 controllers, 123 clips |
| Android vibration bridge | CODE GREEN, DEVICE PENDING | `6a583e2` | EditMode `518/518` passed |
| Android Keystore token storage bridge | CODE GREEN, DEVICE PENDING | `4ccea46` | EditMode `521/521` passed |
| Google Sign-In placeholder integration | CONFIG SLOT GREEN, APP REGISTRATION PENDING | `ba7cd19` | Covered by subsequent full EditMode `526/526` passed |
| Android build smoke | GRADLE EXPORT GREEN, APK BLOCKED | `af5c94c` | Gradle export succeeded, full EditMode `526/526` passed; APK blocked by IL2CPP OOM |

## Pass / Fail / Blocker Table

| Gate | Current Status | Evidence | Remaining Action |
| --- | --- | --- | --- |
| Code readiness | PASS | Full EditMode `526/526` passed after Android smoke changes | Continue regression checks after blocker work |
| Editor QA | PASS | Seven bound screens captured at portrait `1080x1920`, pass `7`, warning `0`, fail `0` | Re-run after final art/config changes |
| Gradle export artifact | PASS | `E:\Projects\LASTRAIL\unity-project\Builds\AndroidSmoke\GradleProjectExportClean`, 2412 files | Use as build-only smoke artifact |
| APK artifact | BLOCKED | Direct APK reached IL2CPP native compile/link and failed with `LLVM ERROR: out of memory` / `ld.lld` OOM | Retry on higher-memory build host or approved adjusted Android build environment |
| Android device/emulator validation | BLOCKED | Embedded `adb devices` returned no attached devices | Connect Android device or start emulator |
| Android vibration device validation | BLOCKED | Native bridge compiles, no Android device available | Validate light/medium/heavy cues on device |
| Android Keystore device validation | BLOCKED | First-party bridge compiles, no Android device available | Validate save/load/overwrite/clear/logout on device |
| Google OAuth and app registration | BLOCKED BY PRODUCT SETUP | Placeholder slots exist, no real client id/audience/secret inserted | Register app and provide real values in PRD/setup stage |
| Production/deploy decisions | NOT STARTED | No production secrets or paid/live setup added | Approve production settings, signing, deploy, and store-track decisions separately |

## Android Artifact Details

- Unity Editor: `D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe`
- Android Gradle export: `E:\Projects\LASTRAIL\unity-project\Builds\AndroidSmoke\GradleProjectExportClean`
- Export file count: `2412`
- Unity build log evidence before cleanup: `Build Finished, Result: Success`; reported export size `566156593`
- Direct APK result: blocked by `android_apk_il2cpp_toolchain_oom`
- Device state: blocked by `android_device_or_emulator_unavailable`
- Android Keystore device validation: `android_keystore_device_validation_pending`
- Android vibration device validation: `android_vibration_device_validation_pending`

## Narrative Copy QA

No concrete narrative copy truncation, overlap, or readability issue was reported by the seven-screen screenshot QA pass. No Narrative copy change is currently requested from Unity QA.

## Boundary Audit

- No PlayerPrefs token storage.
- No account shortcut header, body, or query parameter.
- No raw token, production secret, auth header, device id, raw request body, raw RNG material, probability, full weight list, reward amount, ledger id, or server submit payload in debug output.
- No Unity-side reward, economy, story progress, unlock, offline reward, gacha probability, pity, guarantee, ledger, or grant Source of Truth calculation.
- No Rail Choice Events in Act 1 launch scope.
- No World Raid launch UI or flow.
- Google OAuth real client id, audience, secret, and production secrets remain deferred.

## Next Human / Device Actions

1. Connect an Android device or start an emulator visible to Unity embedded adb.
2. Retry direct APK build on a higher-memory build host or approve an adjusted Android build environment for the IL2CPP OOM blocker.
3. Install/run the Android smoke artifact path on device once an APK build succeeds.
4. Validate Android vibration cues on device: light hit, heavy hit, skill, ultimate, boss phase, mission start/result, gacha reveal/result.
5. Validate Android Keystore token storage on device: save, load, overwrite after refresh, write failure handling, clear/logout.
6. Complete Google app registration and provide real OAuth client id/audience only in the PRD/setup stage.
7. Run final Android build smoke and startup navigation pass: AuthGate, MissionShell, Gacha, Combat HUD, CodexArchive, RewardReview, OfflineReward, StoryProgressSummary.
8. Approve production signing, production secrets, live service, and deploy/store-track decisions in a separate release gate.
