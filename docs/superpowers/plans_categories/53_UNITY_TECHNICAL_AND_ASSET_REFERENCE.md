# 53. Unity 기술·음원 제작 참조

2026-09-21 통합. Assets 안에 흩어진 기술 문서와 음원 제작 기록 15개를 옮겼다. **각 본문은 작성 당시의 구현·제안·검수 기록**이며 현재 구현 상태는 33번, 정책은 00~29와 34~37번이 우선한다. 과거 15웨이브/전송 미구현/작업 금지 문구를 현재 정책이나 작업 제한으로 되살리지 않는다. 신규 API·비밀 저장소·실기기 검증을 이번에 완료한 것이 아니다. 코드와 바이너리 자산은 변경하지 않았다.

라이선스·저작자·가공 내역은 자산 옆 SOURCE-LICENSES/폰트 고지에 유지한다. 이 문서는 해당 고지를 대체하지 않는다. 오래된 코드 예시의 상대 경로는 원출처 기준이다.

## 목차

- [전투 AV 수정 공급 — 2026-09-12](#source-b842008a798e0837)
- [효과 발생음 v02 — 2026-09-12](#source-e26a925cab299273)
- [Act 1 Final Unity Launch QA Report - 2026-08-30](#source-67ea8a0a64f9b140)
- [Act 1 Launch Editor, Package, and Device Handoff Checklist](#source-145cd41ad44f4963)
- [Addressables Runtime Resources](#source-b9ff709061f6c715)
- [LAST RAIL Unity Android Keystore Secure Token Storage Spec](#source-8e0feb1c851094e2)
- [Android Keystore Token Storage Readiness](#source-78c0c67d9491657d)
- [Animation Asset Pipeline Readiness](#source-eecc19cb170d2d90)
- [Network-Aware Startup](#source-7c8ed103523fdc24)
- [Player Authentication Owner Design](#source-4cac60290354068c)
- [LAST RAIL Unity Run API / Submit / Diagnostic TODO](#source-00b48c6b15bfd4a4)
- [Safe Local Client Error Capture](#source-3c79b42e9b6284cc)
- [Local Support Inquiry Slice](#source-e305b417d6634f60)
- [Unity Editor Environment Discovery - 2026-08-30](#source-deb3c8a26944fb44)
- [Unity Test Runner CLI Readiness - 2026-09-01](#source-ad6c999fb5f40078)

<a id="source-b842008a798e0837"></a>
## 전투 AV 수정 공급 — 2026-09-12

원출처: `unity-project/Assets/_LASTRAIL/Audio/Provenance/AbilityAudio-20260912/DELIVERY.md`

<!-- preserved:source-b842008a798e0837:start -->
# 전투 AV 수정 공급 — 2026-09-12

## 네온 기준

NEON-REFERENCE.md의 승인 A 원본과 픽셀 측정을 사용한다. 현재 금속 polish를 기준으로 삼지 않는다. 스킬 이름/N%/실제 상태는 최신 기획을 유지한다.

## 음원 적용

audio-manifest.json에 초기13종+차기7종 각각 cast/effect, 총40 독립WAV와편집내역이 있다. audio/의 `{abilityId}.cast.wav`를 Unity가 현재 준비한 `Resources/CombatPresentation/Audio/{abilityId}.wav`에 매핑할 수 있다. 13개 cast만 기존 fresh durable CASTING에 사용 가능, bind/restore에서는 재생하지 않는다. 자동 공용 fallback·pitch변형은 없다. cast gain 시작권고0.65, effect0.70이며 실제 믹스로조정할 후보값이다. 컷인 skip은 성공효과 취소가 아니며 무음/저장설정을 따른다. （2026-10-29 대조: Unity 논리/직렬화 참조 확인, 파일 유지. 물리 경로는 169-decisions.json의 항목 109 참조.）

`{abilityId}.effect.wav`는 별도 파일이다. manifest의 positive typed effect 조건이 확인될 때만 재생한다. APPLIED, HP증가추측, 준비완료 타이머에서 성공효과를발사하지 않는다. Sera active/ultimate는 현재 후보가 primary damage 결과용이며 interrupt/mark 성공을함께확정하지 않는다. Mira ultimate repair도 damage buff성공과구별한다. 하나의 group cue를여러대상만큼중첩하지말고 command/effect identity로중복제거한다. 실제 interrupt/mark 등 부가효과독립큐는typedprojection확정후추가편집범위이며, 현재자료만으로모든효과별큐완료를주장하지않는다.

신규7개는 구현버전후보. 내부검토label은정식명칭채택아님, ID/condition은실제producer확정과대조후연결. 현재r6에등록하지않는다. 40파일+13종26파일청음reel+전체40파일reel은제작후보이지사용자품질합격아님. SOURCE-LICENSES.md의CCBY귀속문구유지. 새구매없음. 전체실청음과게임내중첩/무음/배속검수미완료를명시한다.

## 태양 분리

`wasteland-sunless-v01.png`는 원본map 1672x941 구성을 참조한 imagegen edit. 원래태양중심(1319,317),반경약18px,태양UV(.789,.663)과국소halo제거. ruinedtower/교량/철로/아래금속벽배치시각유지,이미지전체pixel동일은아님. 따뜻한전역노을은남지만원형태양은없다. 원본보존,정확prompt첨부. Mirror 반복으로검수할공급후보이며 left/right경계RGB는같지않으므로 Repeat방식seamless완료아님. Unity는현재Mirror경계2회통과의실제영상으로확인한다.

단일천체projection은Unity소유: source y≈.9의높은하늘위치,기존태양y를그대로써서교량앞에붙이는오류금지. 외부/창밖같은world좌표/원경속도,타일반복과독립된한개천체. cloudocclusion/건물mask미구현이면이를최종천체합성완료로표시하지않는다. alpha태양후보검수는별도기록;실패시Unity가준비한단일radialmesh선택가능.

실제추가생성 `sun-alpha-v01.png`: 1254×1254 RGBA, 중심대략(627,627), 원반반경약200/넓은halo약560. 원본반경18px를원하는경우전체스프라이트폭약113px(1254×18/200), Unity720화면에는map투영배율을추가적용한다. alpha0픽셀499324,최대253,테두리654픽셀alpha1이라완전zero-border조건은미달. alpha>16 bbox=[134,133,1124,1133]. 하드검정체크배경이아닌실제alpha는확인했지만halo폭/밝기는원본보다강해런타임합성검수필수. clean-edge/최종미술PASS아님. Unity radialmesh 대안과비교하여선택,원본alpha를임의재가공하지않았다. 이미지생성정확prompt2개와원본SHA/검증JSON첨부.

<!-- preserved:source-b842008a798e0837:end -->


<a id="source-e26a925cab299273"></a>
## 효과 발생음 v02 — 2026-09-12

원출처: `unity-project/Assets/_LASTRAIL/Audio/Provenance/EffectAudio-20260912/DELIVERY.md`

<!-- preserved:source-e26a925cab299273:start -->
# 효과 발생음 v02 — 2026-09-12

source001 원본40파일과manifest는 수정하지 않았다. 효과별 추가5개WAV와primary13개믹스시작값을 새버전에분리. `effect-audio-v02.json`, `verification.json`, 재현Python2개, offline청음reel2개 포함. 현재13종외신규7은source001후보유지/추가등록없음.

## 실제 신규파일

|Ability|index/type|의미|소재|
|---|---|---|---|
|op_kyle.active|1 taunt|방호행동 뒤 짧은도발신호,새피해·대사없음|metalLatch+clothBelt2|
|op_sera.active|1 interrupt|실제미래행동제거시전자차단|computerNoise002+generic light impact003|
|op_sera.active|2 mark|별도대상표식잠금|computerNoise001+filtered forceField001|
|op_sera.ultimate|1 mark|두개의짧은정밀표적신호|computerNoise003/000+small engine003|
|op_mira.ultimate|1 train_damage_buff|열차기계동력결합,수리성공음과구분|metalClick+small engine001+metal light000|

전부Kenney CC0 기존확보소재를새롭게편집. 각원본SHA/구간/EQ/배치/출력SHA manifest기록. 새구매/피치변형/공용임시음재사용없음. 새5개에총성소재없으므로CCBY원본섞지않았음. primary13 및합성reel은source001의SOURCE-LICENSES.md에따라VincentSevedge2009 CC BY3.0 총성귀속/링크/수정고지를계속보존.

## 연결 조건과 시점

Unity가전달한실제초안 CombatEffectJournal.cs/CombatEffectFeedback.cs를직접읽고 `typed-bindings-v02.json`18행에필드/positive조건/abilityId+effectIndex/파일/gain연결표를고정했다. ReadHudState.effectResults[]이며source초안SHA를저장한다. 초안은RED검증중보고시점이라liveproducer채택·테스트완료아님. 음원표자체가새wire필드정의는아니다.

- damage: 실제HpLoss>0 또는흡수>0. zero/protected/down배제. 레이나·지원포격·Sera의index0음원은이결과에만연결.
- heal/repair: 해당effect가돌려준실제복원량>0. 단순전체HP증가관찰로대체하지않음.
- shield: 실제초안SHIELD_ISSUE.actualResult.addedAmount>0에만획득음. 기존return값을쓰지않는다. outcomeREFRESH라도addedAmount0이면무음. refresh-only용큐는현재추가하지않는다.
- taunt/mark/train_damage_buff: 실제status삽입/유효refresh여부. taunt의value0을실패로해석하지않음. 더낮은값이라반환한경우재생금지.
- interrupt: 실제제거한pending action count>0. 현재코드는future basic attack뿐; 이미due/skill/channel범위를확장해서가정하지않음.

같은command/effect의여러대상결과는동일큐1회로묶고,다른effectindex의차단/표식은분리한다. 이벤트identity는Unity실제정의로확정,scene bind/restore/동일checkpoint재투영에서는중복재생금지. 음향이게임판정/타이밍을호출하지않음. source001의기존13cast는freshCASTING,효과음은실제성립tick에서재생.

초안row.id는그대로유지한다. 별도audio group=(runId,profileSha256,globalSequence,commandId,abilityId,effectIndex,pulseIndex). targetId/kind는음향group에서만제외해같은effect의복수대상ADD/REFRESH를1회로묶는다. event중복확인→positive조건→group예약순서;첫대상결과0이뒤대상positive음을소모해버리면안된다. 최초restore에서는기존event/group둘다seed. gain은`effectiveGain`한번또는`level × masterGain`한번,두방식중복곱금지. 효과5개추가파일은v02,primary13은원본source001절대경로참조.

## 믹스 검증과 한계

primary목표RMS-26dBFS/secondary-29,추가음level상한1/master0.65를시작제안으로기록했다. currentcast의Unity보고개별gain은source-cast-levels.json으로동결복사했다. 이것은모바일청감정답아님.

`audio/audition-current13-cast-effect-mix.wav`는각cast를0초,primary와그ability부가effect를windup끝같은시점에넣은오프라인혼합31.2초예시. 실제모든effect가성립했다고가정한청음용이므로gameplay/typedpositive/no-op검증이아님. 결과별peak/클리핑/시간을mix-preview-verification.json에기록. `audio/audition-secondary-five.wav`는추가5개분리청음용이다.

실제청음불가한현재환경한계를유지하며‘듣고검수PASS’라고하지않는다. 디코드/파형클리핑0은음색·역할·중첩마스킹합격이아님. Unity실제typed연결후13능력성립/불성립/동시다중대상/복원/무음·배속영상과사용자청음이남아있다. warning대역/음량을고정제한하거나성공음때문에경고를끊지않는다.

<!-- preserved:source-e26a925cab299273:end -->


<a id="source-67ea8a0a64f9b140"></a>
## Act 1 Final Unity Launch QA Report - 2026-08-30

원출처: `unity-project/Assets/_LASTRAIL/Docs/act1-final-unity-launch-qa-report-2026-08-30.md`

<!-- preserved:source-67ea8a0a64f9b140:start -->
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
| Android build smoke / internal testing path | GRADLE EXPORT GREEN, DIRECT APK OOM NON-BLOCKING | `af5c94c` | Gradle export succeeded, full EditMode `526/526` passed; direct APK IL2CPP OOM is reclassified as non-blocking when internal testing/AAB pipeline is used |

## Pass / Fail / Blocker Table

| Gate | Current Status | Evidence | Remaining Action |
| --- | --- | --- | --- |
| Code readiness | PASS | Full EditMode `526/526` passed after Android smoke changes | Continue regression checks after blocker work |
| Editor QA | PASS | Seven bound screens captured at portrait `1080x1920`, pass `7`, warning `0`, fail `0` | Re-run after final art/config changes |
| Gradle export artifact | PASS | `E:\Projects\LASTRAIL\unity-project\Builds\AndroidSmoke\GradleProjectExportClean`, 2412 files | Use as build-only smoke artifact |
| Direct APK artifact | NON-BLOCKING TOOLCHAIN ISSUE | Direct APK reached IL2CPP native compile/link and failed with `LLVM ERROR: out of memory` / `ld.lld` OOM | Use internal testing build path; retry direct APK only if a local APK artifact becomes necessary |
| Android device/emulator or internal test validation | BLOCKED | Embedded `adb devices` returned no attached devices | Connect Android device, start emulator, or run through approved internal testing track |
| Android vibration device validation | BLOCKED | Native bridge compiles, no Android device available | Validate light/medium/heavy cues on device |
| Android Keystore device validation | BLOCKED | First-party bridge compiles, no Android device available | Validate save/load/overwrite/clear/logout on device |
| Google OAuth and app registration | BLOCKED BY PRODUCT SETUP | Placeholder slots exist, no real client id/audience/secret inserted | Register app and provide real values in PRD/setup stage |
| Production/deploy decisions | NOT STARTED | No production secrets or paid/live setup added | Approve production settings, signing, deploy, and store-track decisions separately |

## Android Artifact Details

- Unity Editor: `D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe`
- Android Gradle export: `E:\Projects\LASTRAIL\unity-project\Builds\AndroidSmoke\GradleProjectExportClean`
- Export file count: `2412`
- Unity build log evidence before cleanup: `Build Finished, Result: Success`; reported export size `566156593`
- Direct APK result: `android_apk_il2cpp_toolchain_oom`, reclassified as non-blocking when internal testing/AAB pipeline is the chosen validation path
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
2. Confirm the internal testing artifact path: AAB or Play/internal-test compatible build pipeline, signing owner, and upload/install route.
3. Retry direct APK only on a higher-memory host if a local APK artifact is explicitly needed.
4. Validate Android vibration cues on device: light hit, heavy hit, skill, ultimate, boss phase, mission start/result, gacha reveal/result.
5. Validate Android Keystore token storage on device: save, load, overwrite after refresh, write failure handling, clear/logout.
6. Complete Google app registration and provide real OAuth client id/audience only in the PRD/setup stage.
7. Run final Android internal-test startup navigation pass: AuthGate, MissionShell, Gacha, Combat HUD, CodexArchive, RewardReview, OfflineReward, StoryProgressSummary.
8. Approve production signing, production secrets, live service, and deploy/store-track decisions in a separate release gate.

<!-- preserved:source-67ea8a0a64f9b140:end -->


<a id="source-145cd41ad44f4963"></a>
## Act 1 Launch Editor, Package, and Device Handoff Checklist

원출처: `unity-project/Assets/_LASTRAIL/Docs/act1-launch-editor-package-device-handoff-checklist.md`

<!-- preserved:source-145cd41ad44f4963:start -->
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

<!-- preserved:source-145cd41ad44f4963:end -->


<a id="source-b9ff709061f6c715"></a>
## Addressables Runtime Resources

원출처: `unity-project/Assets/_LASTRAIL/Docs/addressables-runtime.md`

<!-- preserved:source-b9ff709061f6c715:start -->
# Addressables Runtime Resources

Status: implemented and locally bundle-verified; NOT production-hosted or a
completed login/game-service integration. Existing dirty work is preserved.

## Package And Entry

Addressables 2.8.1 is pinned in Packages/manifest.json and packages-lock.json.
Resolved dependencies include Scriptable Build Pipeline 2.6.1. Unity 6000.3.22f1
resolved and compiled the package. Official compatibility references:
https://unity.com/releases/editor/whats-new/6000.3.5f1 (Addressables 2.8 release),
https://docs.unity3d.com/Packages/com.unity.addressables@2.8/manual/index.html.

ResourceBootstrap.unity is the sole enabled build scene (previously no scenes).
It references StartupScreen directly, not an Addressable startup dependency.
Loading, login, network/error/retry and update presentation therefore survive
unavailable remote content. Startup uses a dedicated 1024px dynamic Pretendard
font instead of retaining the 33.7MB serialized game font asset. The original
game font/other screens are unchanged. Existing startup builder preserves this
font binding. Additional characters can use the local original OTF at runtime.

## Classification

QA/Act1Launch/Addressables/resource-inventory.json is the complete path/key/group
inventory, including exclusions, source byte sizes and shared dependencies.
Addresses deliberately equal existing asset paths, preserving the existing
operator/catalog ID-to-path relationships. No resources were moved or renamed.
All 15 cards, 15 portraits and 15 combat AnimatorControllers are included.

| Classification | Entries |
| --- | ---: |
| remote_launch_required | 161 |
| remote_act1_operators | 69 |
| remote_act1_combat_operators | 522 |
| remote_act1_combat_enemies | 222 |
| remote_act1_raid_boss | 42 |
| remote_act1_missions | 18 |
| remote_gacha | 19 |
| remote_audio | 22 |
| Direct local bootstrap/platform dependencies | 38 |
| Excluded source/showcase/editor material | 70 |

Remote: 1,075 entries, 353,206,270 source-file bytes. Direct local _LASTRAIL
resources: 9,171,414 source-file bytes. These are NOT APK install-size measurements.
Local includes StartupScreen, its background/accent, small font and original
font file, buttons/slider/icons, and Android launcher/splash PlayerSettings art.
Unity/TMP Resources defaults outside _LASTRAIL remain local and appear in the
dependency inventory. Assets/Resources is empty and preloadedAssets is empty.

Eight category bundles use PackTogether, not one bundle per asset. The default
local Addressables group has no game entries; direct bootstrap assets do not
need to wait for a local bundle. launch_required labels the required group;
its referenced combat/audio/mission dependencies are also downloaded. This is
a conservative first partition, not an optimized per-screen download budget.

## Real Consumers And Lifetime

AddressableScreenHost.OpenAsync(key, cancellation) uses InstantiateAsync and
ReleaseInstance. Switching replaces/releases the previous instance, stale
requests cannot replace a newer screen, and disable/Close releases ownership.
Remote prefabs keep serialized art/audio/animation references INSIDE bundles,
not on the local scene. Root key loading carries their dependency graph.

Actual built-bundle instantiation verified all eight existing non-startup screens:
MissionShell, FullyAnimatedCombatHud, Gacha, CodexArchive, OfflineReward,
RewardReview, StoryProgressSummary and TutorialGuide. This does not invent their
missing server/gameplay producers. New screens should use this same host rather
than adding serialized references to the bootstrap scene.

ResourceBootstrapController composes client-config -> maintenance/minimum-version
gate -> Addressables init -> catalog check/update -> required bytes -> explicit
download consent -> dependency download -> required lobby prefab load -> auth gate.
100% bytes is not readiness. Config failures, maintenance, forced update, consent
decline, cancellation, failed download/load and unconfigured hosting fail closed.
Catalog requests time out after 20s; bundle requests after 30s (initial settings).
Cancelled shared Addressables operations release on completion rather than
aborting another consumer's operation. These are not production cellular policy.

ClientEnvironmentBootstrap's existing DEV/PRD client-config constants are reused;
the source server contract is minimumClientVersion and maintenance.enabled. No
server change or real service call was made. remoteHostingConfigured is false.
No runtime authentication owner existed: LoginRequested/GuestLoginRequested,
RenderAuthentication, AcceptAuthenticatedSessionAsync and ClearAuthenticatedSession
are explicit owner integration points. A real accepted session AND content-ready
are required for automatic MissionShell entry. Download never fabricates auth.
The login UI can be presented locally before content readiness; this does not
decide whether production login should occur before or after downloading.

## Profiles And Reproduction

LOCAL_TEST builds into ServerData/LOCAL_TEST/[BuildTarget], loads only through
http://127.0.0.1:18284/[BuildTarget]. DEV and PRD have separate UNCONFIGURED host
markers. No R2/CDN/domain/OAuth/secret/remote service was created or configured.
The selected PlayMode builder is packed-content mode, not Asset Database mode.

Run LAST RAIL/Resources/Configure Addressables, then Build LOCAL_TEST Content
with the desired active target. For Windows HTTP QA, serve ServerData/LOCAL_TEST
on loopback port18284 and put the committed client-config-fixture.json there as
client-config.json. Run BuiltBundleResourceTests with LASTRAIL_BUNDLE_QA=1. The
test enters actual PlayMode, clears only launch_required cache, downloads real
bundles, verifies cached size zero, opens screens and checks cancellation/errors.
Do not run Windows bundle QA against Android catalogs. The active target was
restored to Android after verification. No APK was built or signed.

Content outputs/hash-named obsolete bundles and content-state binaries remain
local build artifacts. Use the active catalog/build-layout, never upload the
entire output directory blindly. Retain a reviewed content-state artifact for
future approved content-update releases; local test artifacts are not releases.

## Evidence And Remaining Gates

- RED: TestResults-Addressables-Red.xml, 0/2 before settings/scene creation.
- Focused: TestResults-Addressables-Focused4.xml, 15/15 before two additional
  packaging assertions; final Windows full run passed all 17 Addressables checks.
- Final focused: TestResults-Addressables-FinalFocused.xml, 21/21 passed,
  including the individual ImportedLoop audio test. This does not resolve its
  failures in both full-suite runs; execution-order sensitivity is not diagnosed.
- Real HTTP: built-bundle-qa.json records 103,914,702 required Windows bytes on
  cold start and zero cached required bytes, all eight screens, missing-key,
  cancellation, HTTP config404 and unconfigured-host rejection. Actual HTTP
  request logs accompany it. Injected backend tests cover each download/catalog/
  load failure stage; real network interruption mid-bundle is not verified.
- Full Windows EditMode: 707 passed / 1 failed out of 708. Full Android:
  706 passed / 1 failed / 1 explicitly skipped Windows bundle test. Both fail
  existing LaunchAudioQualityTests.ImportedLoop_IsContinuousAndRepeatedCuesDoNotLatchAcrossFrames
  at line120 (No forced autoplay, bgm.isPlaying was true). Audio code unchanged;
  the failure is unresolved, not relabeled passing or assumed pre-existing.
- Android content build succeeded with catalog/hash and eight active remote
  bundles totaling 85,014,268 bytes, plus a 2,794-byte local MonoScript bundle.
  android-build-layout-summary.json contains paths/sizes. Bundled source/showcase/
  test/editor paths: zero. Bootstrap references to remote game entries: zero.
- Two remote-bundle duplicate assets remain: ui_button_primary_9slice.png and
  ui_button_secondary_9slice.png (required locally too). Seven direct-local/
  remote shared dependencies remain: those buttons, slider track/fill, info/
  warning icons, and original font OTF. This is disclosed duplication, not a
  zero-duplication claim. The large game font is no longer a bootstrap dependency.
- No APK extraction/device install, production catalog rollout/rollback, live
  authentication owner, cellular size policy, actual changed-catalog migration,
  or interrupted-network bundle retry evidence. Full test failure remains a gate.
- No video, MP4, remote upload, paid infrastructure or gameplay/reward changes.

<!-- preserved:source-b9ff709061f6c715:end -->


<a id="source-8e0feb1c851094e2"></a>
## LAST RAIL Unity Android Keystore Secure Token Storage Spec

원출처: `unity-project/Assets/_LASTRAIL/Docs/android-keystore-secure-token-storage-spec.md`

<!-- preserved:source-8e0feb1c851094e2:start -->
# LAST RAIL Unity Android Keystore Secure Token Storage Spec

Updated: 2026-08-29 KST

## Scope

This document defines the launch-facing secure token storage direction for the Unity client. It is a specification slice only.

No Unity package, Google Sign-In SDK, secure storage plugin, UnityWebRequest transport, OAuth client id, or production secret is added in this slice.

## Decision

Act 1 launch token storage must be backed by Android Keystore-protected secret storage. PlayerPrefs, encrypted PlayerPrefs, and preference-only storage are not acceptable for LAST RAIL auth tokens.

Recommended path:

1. Prefer a vetted Unity secure storage plugin only if it proves Android Keystore support on Android, optional Keychain support on iOS, IL2CPP Android compatibility, overwrite/delete support, active maintenance, and no raw token logging.
2. If no plugin passes review, implement a small first-party Android native bridge behind the existing Unity port. The bridge should use Android Keystore-protected encryption keys and store only encrypted token payloads in app-private storage.
3. Keep the Unity-facing API behind `IGameApiAuthTokenStore` so auth flows remain testable and token storage can be swapped without touching gameplay, gacha, reward, or story flows.

Unity's Android signing keystore is not runtime auth token storage. It is used for signing Android builds and must not be treated as a replacement for Android Keystore-backed secret storage.

## Current Unity Boundary

Existing pure C# auth flows already converge on `GameApiAuthSessionSnapshot`.

The real storage implementation must support:

- saving the current auth session snapshot after guest auth, Google auth, Google link, or refresh success
- loading the current auth session snapshot at app start
- replacing access token and refresh token values atomically after refresh rotation
- clearing stored auth state on logout, unrecoverable auth reuse, or account reset flows
- returning failure without logging token values

Current port:

- `IGameApiAuthTokenStore.SaveAsync(GameApiAuthSessionSnapshot value)`
- `IGameApiAuthTokenStore.LoadAsync()`

Spec gap for the implementation slice:

- Add or adapt an explicit clear/delete operation before wiring real logout or `refresh_token_reused` handling.
- Decide whether delete failure should block logout UX or only show a retryable storage warning.

## Candidate Evaluation

### Vetted Unity Secure Storage Plugin

Status: preferred if it passes review.

Required evidence:

- Android stores encryption key material through Android Keystore or an equivalent platform keystore wrapper.
- Token payloads are not stored as plaintext PlayerPrefs.
- iOS Keychain support is available or cleanly optional.
- Works in IL2CPP Android builds, ARM64, release signing, and min SDK target chosen for launch.
- Supports save, load, overwrite, delete, and clear.
- Does not print token values, raw auth bodies, account shortcuts, or secrets to logs.
- Provides a small API that can be wrapped by `IGameApiAuthTokenStore`.
- Has maintained source, release history, license compatibility, and reproducible package installation steps.

### First-Party Native Android Bridge

Status: fallback if no plugin passes review.

Recommended shape:

- Unity C# adapter implements `IGameApiAuthTokenStore`.
- Android native plugin owns secure persistence.
- Android Keystore holds non-exportable key material.
- Encrypted payload storage remains app-private.
- C# receives success/failure metadata only, never raw debug payloads.
- Logs include storage operation status and error code only.

This path costs more implementation time but gives stronger auditability and removes reliance on an unclear third-party package.

### Xamarin, Capacitor, Or Non-Unity Secure Storage Packages

Status: design references only.

These packages can inform API shape and platform behavior, but they should not be imported directly unless they are explicitly compatible with Unity IL2CPP Android builds.

### PlayerPrefs-Based Storage

Status: rejected for auth tokens.

Rejected examples include PlayerPrefs wrappers, encrypted preference helpers, and convenience packages that do not prove Android Keystore-backed key protection. They may be acceptable for non-sensitive preferences, but not for access tokens or refresh tokens.

## Storage Semantics

Save:

- Accept exactly one current `GameApiAuthSessionSnapshot`.
- Store access token, refresh token, expirations, account id, account status, and token type as one logical payload.
- Never split refresh token replacement into a partial write that can leave a mismatched token pair.

Load:

- Return the stored snapshot when present and readable.
- Return null or a typed missing-state result when absent.
- Return a typed failure when the encrypted payload cannot be decrypted.

Rotate:

- After successful `POST /v1/auth/refresh`, replace the stored snapshot with the new access token and refresh token bundle.
- If secure storage write fails after a server refresh succeeds, surface a storage failure state and avoid logging token values.
- Do not retry refresh with an already rotated refresh token without the existing auth flow policy deciding it is safe.

Clear:

- Remove the encrypted payload and any storage metadata owned by the token store.
- Required for logout and `refresh_token_reused` recovery.
- Must not clear unrelated player settings, graphics preferences, or local narrative presentation data.

## Debug And Logging Rules

Debug JSON, diagnostics, exceptions, and logs must not include:

- raw access token
- raw refresh token
- raw Google id token
- Authorization header value
- account shortcut header
- raw auth request body
- client secret
- signing key
- raw RNG material
- reward internals

Allowed diagnostic fields:

- storage operation kind
- outcome
- safe error code
- retryable flag
- requires reauth flag
- token presence boolean
- expiry presence boolean

Do not include token prefixes, token lengths, hashes, or partial token values unless a later security review explicitly approves a redaction standard.

## Future Wrapper Contract

Recommended implementation boundary:

```csharp
public interface ISecureAuthTokenStorageBackend
{
    Task<SecureAuthTokenStorageResult> SaveAsync(string encryptedPayloadLabel, string serializedSnapshot);
    Task<SecureAuthTokenStorageLoadResult> LoadAsync(string encryptedPayloadLabel);
    Task<SecureAuthTokenStorageResult> DeleteAsync(string encryptedPayloadLabel);
}
```

`IGameApiAuthTokenStore` should remain the game-facing port. Any native plugin, Java bridge, or platform-specific implementation should sit behind a small adapter and be tested with fake backends first.

## Verification Checklist

Before shipping a real storage implementation:

- Android IL2CPP release build can save, load, overwrite, and delete tokens.
- App restart preserves the token snapshot.
- Refresh rotation replaces both access and refresh tokens.
- Logout clears the snapshot.
- `refresh_token_reused` path clears or quarantines the session according to auth UX policy.
- Logcat contains no token, Google id token, Authorization value, account shortcut, raw auth body, or secret.
- Android backup/restore behavior is reviewed so encrypted payloads are not restored into an undecryptable or unsafe state.
- iOS Keychain behavior is reviewed if iOS enters launch scope.
- Unit tests continue to use fake storage and never require the real plugin.

## Decision Queue

- Select a specific Unity secure storage plugin, or approve first-party Android native bridge work.
- Confirm launch Unity version, Android min SDK, target SDK, ARM64, IL2CPP, and managed stripping level.
- Decide whether to add `ClearAsync` directly to `IGameApiAuthTokenStore`.
- Decide storage failure UX after successful refresh rotation.
- Decide Android backup exclusion or migration behavior for encrypted token payloads.
- Decide whether iOS Keychain support is launch required or backlog.

## References

- Android Keystore system: https://developer.android.com/privacy-and-security/keystore
- Android cryptography guidance: https://developer.android.com/privacy-and-security/cryptography
- AndroidX Security Crypto release notes: https://developer.android.com/jetpack/androidx/releases/security
- EncryptedSharedPreferences API status: https://developer.android.com/reference/androidx/security/crypto/EncryptedSharedPreferences
- Apple Keychain Services: https://developer.apple.com/documentation/security/keychain-services/
- Unity Android keystore signing documentation: https://docs.unity.cn/Manual/android-keystore.html

## Boundaries

- No plugin import or package installation in this slice.
- No Google SDK import.
- No UnityWebRequest or HttpClient implementation.
- No OAuth client id, audience, secret, or production credential.
- No PlayerPrefs token storage.
- No account id request shortcut.
- No reward, economy, story, unlock, offline reward, gacha probability, pity, ledger, RNG, or server truth calculation.
- Rail Choice Events remain outside Act 1 launch scope.

<!-- preserved:source-8e0feb1c851094e2:end -->


<a id="source-78c0c67d9491657d"></a>
## Android Keystore Token Storage Readiness

원출처: `unity-project/Assets/_LASTRAIL/Docs/android-keystore-token-storage-readiness.md`

<!-- preserved:source-78c0c67d9491657d:start -->
# Android Keystore Token Storage Readiness

Act 1 launch requires Game API auth tokens to be stored through Android Keystore backed secure storage.

Current Unity boundary:
- `IGameApiAuthTokenStore` remains the game-facing token store port.
- `AndroidKeystoreGameApiAuthTokenStore` adapts that port to `IAndroidKeystoreAuthTokenBridge`.
- The selected launch path is a first-party Android Keystore bridge: `AndroidUnityKeystoreAuthTokenBridge` + `AndroidKeystoreNativeStoragePort`.
- The native bridge is compile-safe and testable in Editor. Actual Android Keystore execution still requires an Android device/build validation pass.
- Save calls are overwrite-safe so refresh token rotation can replace the stored access/refresh token bundle.
- Clear supports logout/session reset.
- Availability/readiness is exposed without raw token values.

Required validation before real device storage:
- Run an Android player/device save/load/overwrite/clear pass through `AndroidKeystoreNativeStoragePort`.
- Confirm IL2CPP Android build compatibility.
- Confirm clear/delete behavior for logout.
- Confirm migration behavior if storage keys change.
- Confirm token values are never logged by the bridge or auth UI/debug adapters.
- Keep third-party secure-storage plugin import as a fallback only if the first-party bridge fails device validation.

Launch security boundaries:
- Do not store tokens in PlayerPrefs.
- Do not add account shortcut headers or body fields.
- Do not write raw access, refresh, or Google ID token values to debug JSON or logs.
- Do not add Google OAuth client id/audience values until app registration/PRD setup.

<!-- preserved:source-78c0c67d9491657d:end -->


<a id="source-eecc19cb170d2d90"></a>
## Animation Asset Pipeline Readiness

원출처: `unity-project/Assets/_LASTRAIL/Docs/animation-asset-pipeline-readiness.md`

<!-- preserved:source-eecc19cb170d2d90:start -->
# Animation Asset Pipeline Readiness

Act 1 launch combat uses Unity 2D Animation package plus Animator, Animation Clip, Sprite Atlas, and sprite-sheet VFX.

## Package Gate

- Required Unity package id: `com.unity.2d.animation`
- Current slice does not import or resolve the package in Editor.
- If the package is not declared in `Packages/manifest.json`, treat this as an Editor/package import approval item before prefab or rig authoring.

## Launch Coverage

- Visual operators: 15 total
  - Initial/free operators: 5
  - Gacha elite operators: 10
- Enemy minimum: `enemy_raider_grunt`
- Story raid boss minimum: `boss_raider_king_engine`

## Required Operator Animation Keys

- `idle_loop`
- `move_or_slot_shift`
- `attack_or_cast`
- `hit`
- `down_or_retreat`
- `skill_cue`
- `ultimate_cue`

## Required Enemy Animation Keys

- `idle_loop`
- `move_or_advance`
- `attack_or_cast`
- `hit`
- `down_or_destroyed`

## Required Story Raid Boss Animation Keys

- `idle_loop`
- `attack_or_cast`
- `hit`
- `phase_transition`
- `ultimate_cue`
- `down_or_destroyed`

## Atlas Policy

- Primary atlas max size: 2048
- Low-end fallback atlas size: 1024
- Keep UI, portrait/icon, combat, and VFX atlas groups separated.
- Candidate atlas keys:
  - `atlas_operator_combat`
  - `atlas_operator_portrait_icon`
  - `atlas_enemy_combat`
  - `atlas_raid_boss_combat`
  - `atlas_sprite_sheet_vfx`

## VFX Budget

- Large VFX concurrent limit: about 2
- Hit spark concurrent range: 8-12
- Full-screen flash max: 0.15 seconds
- Screen shake tiers: small, medium, heavy
- Boss phase VFX duration: 1-2 seconds
- HUD readability has priority over continuous glow or dense particles.

## Boundaries

- No paid or external art download in this slice.
- No Sprite, Animator Controller, Animation Clip, Prefab, or Scene asset is generated here.
- No reward, economy, story, unlock, offline reward, gacha probability, or ledger truth is calculated by Unity.
- No raw art payload or server submit payload is produced by this readiness document.

<!-- preserved:source-eecc19cb170d2d90:end -->


<a id="source-7c8ed103523fdc24"></a>
## Network-Aware Startup

원출처: `unity-project/Assets/_LASTRAIL/Docs/network-content-startup.md`

<!-- preserved:source-7c8ed103523fdc24:start -->
# Network-Aware Startup

Status: local implementation and Windows bundle transport evidence; Android
device/OAuth/config-release integration remain unverified. This supersedes the
earlier always-confirm, content-before-login startup behavior.

## Policy And Ownership

ResourceBootstrapController owns one DownloadCoordinator, DownloadAccessPolicy,
and ContentNetworkService per enabled bootstrap. Config minimum-version and
maintenance pass publishes local login and SessionRestoreRequested. Metadata
initialization/catalog/size work continues independently. Actual service-open,
release compatibility and registration-ready contracts are not yet implemented
in the server baseline; these are explicit integration gates, not assumed true.

Wi-Fi attempts downloads automatically. Initial other/unknown-online transport
requires explicit size/data-charge consent. Offline gives connection guidance.
During login, a Wi-Fi departure without consent aborts the owned request and
defers the transition consent popup until accepted authentication. Login failure
does not reveal it. Wi-Fi return permits automatic continuation. Zero required
bytes never prompts. Decline keeps login available but blocks content readiness;
Retry can request consent again. Approval is memory-only for the current bundle
name/hash/size fingerprint and is reused on same-plan retry, never a permanent
cellular preference or server release authorization.

Login acceptance updates the same coordinator; it does not call a second
DownloadDependenciesAsync or recreate an in-flight download. A single project-
owned prefetch lease also waits for an older cancelled owner to relinquish its
request across bootstrap disable/re-enable. Progress uses owned request bytes
plus completed bundle bytes, not GetDownloadSize while transfer is in flight.
Interrupted partial bundles are re-fetched in full; progress may honestly move
back. Complete versioned cache remains reusable. Byte100% is not load success.
Verification/load occurs after accepted auth; lobby also requires actual load.

## Request And Cache Boundary

AddressableContentBackend traverses ResourceLocation.Dependencies, deduplicates
bundle identities, and prefetches sequential UnityWebRequestAssetBundle requests
using the catalog's actual bundle name/hash/CRC. No ad-hoc catalog parsing.
Policy changes Abort only its current request; no global queue/provider monkey
patch or package-cache modification. Cancellation releases the lease only after
the request finishes. Disabling bootstrap cancels ownership; pause suspends it;
resume explicitly refreshes network state. No OS background service guarantee.

All eight remote schemas use project-owned CacheOnlyAssetBundleProvider. Local
bundles retain the base provider. Remote consumers resolve Unity's cache using
explicit CachedAssetBundle identity and a nonexistent local file fallback URL.
The remote CDN URL is never passed into the cache-consumption request. Missing
or invalid cache fails back to coordinator retry. This is necessary: merely
overriding provider retry returned an extra HTTP request under injected bad CRC,
because UnityWebRequest itself attempted a remote fallback. The final local-only
fallback passed normal Instantiate and injected-CRC rejection with zero HTTP.
No asset files are created at the local fallback path.

## Android Bridge

Existing vibration/Keystore code uses project-owned C# ports with Android JNI;
there was no existing Java/Kotlin network callback plug-in to reuse. Added
Assets/Plugins/Android/LASTRAILNetwork.androidlib, source and explicit Gradle src
mapping/namespace/consumer keep rules. It only observes network state. Normal
ACCESS_NETWORK_STATE permission; no location permission or runtime permission
dialog. Project minimum API26 is unchanged; default callback API24 is supported.

Native onAvailable publishes unknown until actual capabilities arrive; default
network capability/lost events are identity checked. Synchronous snapshots are
limited to initial registration and explicit refresh, not frame polling or
capability callbacks. C# proxy posts to Unity SynchronizationContext with an
owner generation fence; disposed/old callbacks cannot touch UI. Duplicate Start
is ignored, disable/dispose unregisters, and callbacks contain no login logic.
Editor injection is test-only and does not establish real Android detection.

Validated Wi-Fi means a permitted download attempt, not unmetered service or CDN
reachability. Native callback dispatch and abort have finite latency: bytes may
already be sent when transport changes. No claim of zero cellular packets.

References: [Android callback race guidance](https://developer.android.com/reference/android/net/ConnectivityManager.NetworkCallback),
[network state and transport capabilities](https://developer.android.com/develop/connectivity/network-ops/reading-network-state),
[Unity Android library import](https://docs.unity3d.com/6000.3/Documentation/Manual/android-library-project-import.html).
The installed Addressables 2.8.1 provider source was inspected read-only.

## Evidence

- RED NetworkContentContractTests: 0/2 before coordinator/provider implementation.
- Focused2 policy/contract: 20/20. Views run: 41/41 including existing startup
  tests and new actual prefab interaction/render fixtures. Additional cancellation
  test added afterward is included in the final full run, recorded separately.
- Transport2: 1/1. Transport3: 0/1 exposed CRC fallback extra HTTP request.
  Transport4: 1/1 after local-only cache consumer fix. Actual loopback, not
  AssetDatabase: required103,914,702 bytes; initial/uncached-Load bundle requests0;
  first-request abort leaves request count1 and no subsequent request until
  consent; same task continues after auth fixture/consent; cached required0.
  Six required unique bundles plus the aborted first attempt produced7 requests;
  all8 screen instantiations produced no further requests. Bad cached CRC was
  rejected with additional HTTP0. network-transfer-qa.json records the run.
- Four actual Camera.Render PNGs at1080x1920 and360x640 were individually viewed;
  local login progress and consent text/buttons are nonblank, readable and within
  bounds. These are state fixtures, not real Google/guest login captures.
- Final Java source compiled with installed JDK and Android35 android.jar. This
  does not verify Gradle merging, IL2CPP stripping, native callback delivery,
  actual Android cache URI behavior or device/OEM network switching.
- Pure async policy tests use a non-Unity synchronization context. The first
  focused runner and first transport runner were stopped after harness deadlocks;
  these are not passing results. Real transfer tests enter PlayMode and await
  asynchronous failures without synchronous NUnit blocking.

## Remaining Gates

Full regression result and final Android content build are recorded in the main
session report. Existing full-suite audio failure is not assumed fixed by focused
success. Actual Android native callback/Gradle/IL2CPP/cache behavior, on-device
Google activity resume, OS termination, cache eviction races and changed-release
deployment need device/release validation. Cached CRC injection is a validation
fault test, not a proof for every possible filesystem corruption.

Actual auth owner, 3-provider local UI and account conflict switching are not
implemented here. See player-auth-owner-design.md and server draft3f9b8f0.
No invented password endpoints, fake auth success, remote host, secrets, APK
sign/install, server mutation, account merge or video work.

<!-- preserved:source-7c8ed103523fdc24:end -->


<a id="source-4cac60290354068c"></a>
## Player Authentication Owner Design

원출처: `unity-project/Assets/_LASTRAIL/Docs/player-auth-owner-design.md`

<!-- preserved:source-4cac60290354068c:start -->
# Player Authentication Owner Design

Status: design and contract dependency only. No real three-provider authentication
owner or conflict switching has been implemented by this change. Server draft
3f9b8f0, server/docs/api/auth-account-linking.md, was received and read. Password
and account-switch endpoints there are candidates, not implemented routes.

## Local Presentation

The local bootstrap must offer Guest, login ID/password, and Google. ID is neither
displayName nor accountId. Local screens include ID login, ID registration,
current-account provider linking, errors, and authenticated conflict comparison.
Use TMP_InputField with password content type, obscured by default, no clipboard
autofill/save implementation for secrets. Disable repeated submit while pending;
keyboard/safe-area layouts need rendered portrait and device keyboard verification.
The current two-action Startup prefab is not yet the three-provider UI.

## Session Ownership

One auth owner subscribes to ResourceBootstrapController.SessionRestoreRequested
after server config gates. Load the existing Keystore-backed refresh credential;
refresh the same account. Only an explicit first Guest action without a usable
stored identity creates a guest; timeout/offline must not create another guest or
erase stored credentials. API rotation/retry idempotency must be supplied by the
server contract. No hardware-ID authentication, plaintext PlayerPrefs secrets,
long-term password/Google proof persistence, or hidden multi-account token vault.

Preserve the current session during pending login/link/preview/cancel/error. A
generation fence rejects late results after logout, cancel, or account change.
For accepted session rotation or switch, securely store the new credential before
publishing the new in-memory owner generation. Storage failure must keep the old
owner visible and enter recovery, but cannot pretend a server-revoked old token
still works. A server retry/recovery contract is required for that boundary.

## Link Versus Switch

Unused provider adds to the current guest account. Already linked to the same
account is idempotent success. Another account is always a conflict, including
equal or zero progress; no automatic merge, progress maximum, inventory transfer,
provider reassignment, or silent overwrite. Linking ID and Google to one account
is allowed by the requested model; both must be explicit authenticated actions.

Only successful target ownership proof unlocks safe server comparison summaries.
Do not show internal accountId, raw credentials or full inventory. Keep current
guest session and short-lived target proof separate. Preview/confirm uses server
transaction identity, expiry, both accounts, intent and snapshot revision.
Cancel/timeout retains guest. Confirm says both safe display names and that
progress is not merged. Result is ACCOUNT_SWITCHED, not LINKED. Disallow switching
during combat, reward processing or another link transaction.

For an unlinked guest, warn that the old account might be inaccessible after
switch. Prefer securing another unused ID/Google link first. Do not promise
recovery after app/data/Keystore loss or quietly add recovery codes. Verified
recovery email is now required for password registration/linking, as specified
below. Registration-consent policy and operational recovery remain release gates;
no email service provisioning or remote account writes are authorized.

## Verified Recovery Email - Confirmed 2026-09-06

ID/password registration and adding a password credential to an existing guest
or Google account require explicit recovery-email entry and actual server-side
ownership verification before credential completion. Guest-only and Google-only
login do not require this extra email. Never adopt Google claims email silently.
Email, loginId, displayName and accountId are separate; equal email does not link,
merge or switch accounts. A typed email is not a usable recovery method: conflict
warnings depend on the server's confirmed usable credentials.

Local bootstrap presentation must include recovery-email entry, verification
pending, server-controlled resend availability, expired/invalid/rate-limited/
network errors, reset-request acknowledgement, proof entry/verification, new
password and post-reset re-login guidance. Only mask email where needed for the
current player's flow; no email in public profile or target-account comparison.
Input validation is not ownership verification. Do not infer completion from a
timer, email-open event, local fake adapter or navigation back to the app.

Keep pending signup/link distinct from completed credentials. Pending, cancel,
timeout and resend must preserve the current guest/session/progress and the same
server transaction when supported. Never call AcceptAuthenticatedSessionAsync
because an email was sent or a local proof field was filled. Existing authenticated
guest use is not globally revoked by an abandoned optional ID-link attempt.
New ID signup cannot enter the game as that new identity until server completion.
DownloadCoordinator continues independently; deferred transition consent follows
actual accepted authentication/required registration completion, not email UI.

Password reset requests display the same acknowledgement regardless of account
existence. The server owns rate limits, cryptographic random proofs, safe verifier
storage, purpose/account/transaction binding, expiry and single-use consumption.
No credentials or proof in logs/analytics/PlayerPrefs; no password in email.
Reset link activation must not authenticate a game session. Successful new
password commit revokes existing sessions and requires re-login, retaining other
linked provider identities. A reset request alone never revokes sessions.

Changing recovery email requires recent current-account reauthentication and
verification of the new address before replacement. Proposed notification policy:
notify the old address after a successful change without including new-address
details or proof. Access to the old inbox is not assumed; the approved fallback
must still require an existing supported reauthentication method. If neither
method is available, show unsupported recovery rather than promising manual
support. Notification/fallback details require final server/product agreement.

## Email Contract And Approval Gates

The server draft3f9b8f0 predates the email decision. Await actual request/response
contracts for pending signup/link, resend and proof verification, recovery reset,
email change, cancellation/retry idempotency, server expiry/cooldown and session
revocation. Do not independently add endpoint names. Proposed UI state names
above describe presentation only, not a finalized wire contract.

Technical proposals, not approved policy: verification proof TTL15 minutes,
reset proof TTL15 minutes, resend cooldown60 seconds enforced by server, plus
per-transaction/account/address/IP abuse limits whose budgets require security
review. Proof format and retry/attempt limits remain server decisions. Show
server-returned deadlines, never trust the device clock for proof validity.
Whether one email may recover multiple accounts and whether/how account lookup
returns login IDs are still undecided. Do not list accounts from email input.
Earlier proposed loginId/password numeric rules remain proposals.

Mail delivery candidates only: [Amazon SES verified identities](https://docs.aws.amazon.com/ses/latest/dg/verify-addresses-and-domains.html)
or [Resend domain verification](https://resend.com/docs/dashboard/domains/introduction).
SES requires verified sender identity and has sandbox recipient restrictions;
Resend uses verified-domain sending. No provider is selected. Required approvals:
provider/account and region, sender domain/address, DNS ownership and changes,
credentials placement, expected mail volume/cost ceiling, retention/privacy and
bounce/complaint handling, allowed test recipients, and production sending access.
Only an explicitly labeled local fake mail adapter may be used before approval;
no real delivery, external account creation, DNS/secret or deployment changes.

Required future tests: unverified password credential cannot complete; guest/
Google-only remain unaffected; pending/cancel/timeout preserve current session;
proof wrong purpose/account, expired/reused, stale resend and concurrent verify
are rejected; reset request existence-indistinguishable and non-revoking;
successful reset revokes sessions but retains providers; email change requires
reauth/new verification; typed email cannot suppress guest-loss warning; email/
proof/password never appear in public summaries or logs. Local fakes are not
evidence of actual email ownership or production delivery.

## Password Reset Contract Update: 2026-09-06

Server commit `78c21ac` supersedes the earlier reset reference-paste/deep-link
proposal. Source and API documentation were inspected locally; no Unity auth
owner or password UI is implemented by this document update.

- `POST /v1/auth/password/reset/start` remains account-existence agnostic and
  returns no challenge ID. Keep the submitted login ID and recovery email in
  the current in-memory form for confirmation; do not infer account existence
  from the generic response.
- The preferred `POST /v1/auth/password/reset/confirm` body contains `loginId`,
  `recoveryEmail`, `verificationCode`, and `newPassword`. The verification code
  is an eight-digit string; preserve leading zeros. The user does not need to
  enter an internal email challenge reference or follow a deep link.
- Send only the preferred four-field variant. The server retains legacy
  `challengeId` compatibility; that is not a reason to add a reference field
  to the new UI or send both variants together.
- Server resolution uses the matching account/recovery email and latest
  pending reset challenge. Challenge/account/code failures use generic
  `password_reset_challenge_invalid`; malformed required input may return
  `contract_violation`. Neither response should become an account-existence
  hint in the UI.
- Register/link recovery-email verification still uses its existing
  challenge ID response and confirmation contract. Do not remove those IDs.
- Email receipt, entering a code, or starting reset is not reset completion.
  Successful reset confirmation returns no login tokens and requires login
  again. Clear password/code fields after the operation is completed or the
  form is abandoned; never place them in URLs, logs, diagnostics or disk drafts.

Server reported DEV deployment `132feb89-2bc7-4b88-8c0c-58efc15b3122` and local
test success. This is a Server report, not Unity verification of deployment,
real-account reset, signup, secure storage or the login-to-lobby flow.

## Download Integration

Auth operations do not recreate DownloadCoordinator. Google external activity
pause suspends owned requests; resume rechecks native network state and uses
completed bundle cache, without a byte-range resume promise. Login failure never
shows the deferred Wi-Fi-transition consent popup. After accepted auth and any
required registration completion, signal the content auth gate. Lobby requires
both authenticated/registration-ready session and actual content load success.
Account switch invalidates account-specific views/caches while retaining public
versioned content cache.

## Required Server Contracts And Tests

Need actual guest creation idempotency, ID registration/login/link/status,
Google conflict proof/preview/confirm, safe summaries, revision/expiry handling,
refresh rotation retry, registration-ready state, revoked/corrupt token errors,
and secure-store failure recovery. Need public config service-open/content
compatibility fields; current minimum version and maintenance are insufficient
to claim these gates exist. Content bundle fingerprints scope download consent
only, never release authorization or server compatibility.

Once contracts arrive: RED/GREEN for stored-guest restart, network timeout
preservation, duplicate submit, late responses, provider unused/current/other,
zero-progress conflict, proof-gated summary, stale/expired/replayed confirmation,
no-recovery guest warning, combat block, Keystore failure, and shared download
task continuity. Device Google OAuth/Keystore and keyboard evidence remain
separate from fixtures. No remote migrations, secrets or deployment.

<!-- preserved:source-4cac60290354068c:end -->


<a id="source-00b48c6b15bfd4a4"></a>
## LAST RAIL Unity Run API / Submit / Diagnostic TODO

원출처: `unity-project/Assets/_LASTRAIL/Docs/run-api-submit-diagnostic-todo.md`

<!-- preserved:source-00b48c6b15bfd4a4:start -->
# LAST RAIL Unity Run API / Submit / Diagnostic TODO

Updated: 2026-08-26 15:45 KST

## Current Unity Status

- Run start response JSON can create `RunRuntimeState` through `RunApiFlowFacade.StartActOne`.
- Act 1 runtime progression is modeled as 15 waves, final operation, then complete.
- Tactic selections are recorded in `RunTacticState` and exported without reward calculation.
- Completed runs can create a result submit request through `RunApiFlowFacade.CreateResultSubmitRequest`.
- Result submit is represented by the `IRunResultSubmitClient` port only. No HTTP client is implemented yet.
- Submit outcomes are mapped through `RunApiFlowSubmitPolicy`.
- Run end UX state is represented by `RunEndUxState`.
- Reward review entry state stores only `rewardClaimId` and `rewardPreviewId` references.
- Diagnostic snapshots and run end UX states have debug JSON adapters.
- In-memory diagnostic sink exists for local/test diagnostics only.

## Server Contract Items Waiting For Confirmation

- Final result submit endpoint/path.
- Final idempotency key location and naming.
- Final `resultStatus` enum values.
- Final `errorCode` classification for recoverable vs fatal rejection.
- Final `rewardClaimId` vs `rewardPreviewId` policy.

## Boundaries To Keep Until Server Contract Is Final

- Do not add real HTTP transport for result submit.
- Do not add remote diagnostic log transport.
- Do not add UI reward display logic.
- Do not calculate or confirm reward amounts in Unity.
- Keep reward values behind server-owned claim/preview references.
- Keep PRD as code/config axis only unless deployment is explicitly approved.

## Next Implementation Trigger

When the server result submit contract is confirmed, update the isolated Unity layers in this order:

1. `RunResultSubmitRequest`
2. `RunRuntimeStateResultSubmitJsonAdapter`
3. `RunResultSubmitResponseDto`
4. `RunResultSubmitResponseJsonAdapter`
5. `RunApiFlowSubmitPolicy`
6. `RunRewardReviewEntryState`
7. `RunEndUxState`
8. Debug/diagnostic adapters and integration tests

<!-- preserved:source-00b48c6b15bfd4a4:end -->


<a id="source-3c79b42e9b6284cc"></a>
## Safe Local Client Error Capture

원출처: `unity-project/Assets/_LASTRAIL/Docs/safe-client-errors.md`

<!-- preserved:source-3c79b42e9b6284cc:start -->
# Safe Local Client Error Capture

Readiness: local collection and bounded persistence implemented and tested on
Windows. A separate explicit current-session HTTP client is implemented against
the confirmed contract. The unscoped persistent queue is NOT uploadable. Actual
owner integration and server reception remain unverified. Collection is not server visibility.

## Metadata And Privacy

AppBuildSnapshot is immutable. In a player it captures Application.version and,
on Android, the currently installed package's getLongVersionCode (API28+) or
versionCode (API26-27). It never reads Editor build settings, R2 minimum/latest,
server deployment ID or a fixture value as installed metadata. Editor reports
both values unavailable; other non-Android players report versionCode unavailable.
The cached per-process build snapshot is attached when each event occurs, not
when a later application version loads the queue. Main-thread Unity/JNI access
finishes before registering the threaded callback.

Only fixed UnityError, UnityException and UnityAssert classifications, UTC event
timestamp, occurrence count, and build version are retained. Log callback text
and stack arguments are ignored completely: no raw text, hash, substring, token,
body, email, password, account ID or user input. Existing Run/Gacha diagnostic
JSON is not used or transmitted. Versions accept a bounded64-character build
label alphabet; unsupported/invalid values become null. Long versionCode is
preserved locally with explicit presence bit; this is a local storage schema,
NOT a finalized server wire type. The Android API documents the64-bit combined
value: https://developer.android.com/reference/android/content/pm/PackageInfo.

All events are currently unattributed. A future uploader MUST NOT associate a
queued prelogin/old-account event with the account authenticated at retry time.
Anonymous ingestion or explicit scope/drop behavior needs server agreement.
Versions are client-reported diagnostics, never authentication/reward authority.

## Lifetime And Queue

A player-only BeforeSceneLoad bootstrap owns one DontDestroyOnLoad collector;
there are no Addressables, login, UI, networking or third-party SDK dependencies.
Editor automatic capture is disabled; tests invoke an explicit fixture instead.
Threaded callbacks use a lock-protected queue and thread-local recursion guard,
without Unity API calls or disk I/O. Limit60 accepted events/minute, duplicate
aggregation by fixed kind/build within60 seconds. No hashing of private content.

At most100 rows and64KiB committed JSON, TTL7 days. Save occurs every5 seconds,
on pause and orderly shutdown; a crash/forced termination can lose unsaved events.
No guarantee of crash-safe receipt for the final5 seconds. Full application/data
removal can remove the queue. Existing app updates preserve the original build
snapshot locally; unscoped persisted events must never be relabeled for current-account upload.

Location: Application.persistentDataPath/diagnostics/safe-client-errors-v1.json.
Writes use a flushed same-directory temporary file then atomic Replace/Move;
failed writes retain the previous committed file and in-memory pending events.
Temporary output is at most another64KiB and is removed best-effort. No non-atomic
overwrite fallback. File corruption, unsupported schema or oversized input clears
the in-memory view and is replaced by a valid bounded file on the next save;
private corrupt contents are not copied to a backup/log. Concurrent callbacks
and saves are serialized. No account-specific data migration or hidden token vault.

No delivery retry is active while TransportAvailable=false. Local failed saves
are retried on later flushes without Debug.Log recursion. Platform atomic-file
support, actual Android installed metadata and device lifecycle remain unverified.

## Verification

CurrentSessionErrorClient is a separate main-thread explicit source. The owner
provides the current authenticated session and a local generation stable across
same-owner token refresh. Producers must pass their originating generation.
Stale, prelogin and prior-process events cannot be imported through this API.
POST /v1/client-errors sends Bearer and Idempotency-Key and an events-only body,
with one allowlisted event per frozen request. Scope is exactly
AUTHENTICATED_CURRENT_SESSION, versionCode a nullable decimal string. No raw
text/stack/account shortcut is serialized. Pending memory is limited to25 and
capture to60/min. Unknown sends retain body/key, replay is acknowledged only
with matching counts and a valid receipt,429 waits the server delay (600sec
fallback) across all sends for that account. Dispose discards this memory.
The old persisted queue retains TransportAvailable=false, without relabeling.

TestResults-AuthenticatedSubmission-Full.xml:790 total,787 passed,1 audio
autoplay failure,2 skips. All12 authenticated adapter tests passed. These
are local transport fixtures, not actual server reception or player integration.

- TestResults-ClientErrors-Red.xml:0/1 before snapshot/queue implementation.
- TestResults-ClientErrors-Focused.xml:9/9 passed. Includes restart/update original
  version preservation and null code, parallel flood cap/dedup, size/count/TTL,
  corrupt/oversized file recovery, locked-destination atomic failure and retry,
  and threaded callback private text/stack omission.
- No full-suite rerun for this narrow slice. The prior NetworkContent full run
  had732 pass/1 existing audio failure/2 explicit HTTP skips; not a new result.
- No Android device/APK validation, HTTP ingestion, Admin visibility, deployment,
  secrets, remote writes, real mail or video. Existing auth/resource work preserved.

<!-- preserved:source-3c79b42e9b6284cc:end -->


<a id="source-e305b417d6634f60"></a>
## Local Support Inquiry Slice

원출처: `unity-project/Assets/_LASTRAIL/Docs/support-inquiry.md`

<!-- preserved:source-e305b417d6634f60:start -->
# Local Support Inquiry Slice

## Readiness

- Local input panel and memory state controller: implemented, local QA only.
- HTTP adapter and wire DTO: implemented against the confirmed local Server contract.
- Real menu/authentication owner entry: not connected.
- Actual server ticket reception: not tested and not claimed.

The existing MissionShell settings panel is a visual resource, not an inquiry entry. This slice does not create a second login owner, bypass nickname/onboarding gates, or inject fixture credentials into runtime. An unauthenticated user with login trouble cannot submit through this authenticated-only scope.

## Owner Integration

The authenticated owner must use `SupportInquiryHttpClient.CreateController` with its actual `GameApiAuthSessionSnapshot`. The factory supplies confirmed categories, trimmed UTF-16 length10-2000 and `AppBuildSnapshot.CaptureInstalled` on the main thread. Category keys and localized labels are separate. No runtime sample client is installed.

Initialize `SupportInquiryPanel` with that controller, a Korean-capable TMP font, and a category-label resolver. The owner must provide the normal EventSystem/input module. `Show`, `Hide` and `Closed` provide the entry/return surface. Keep the controller alive across panel close/reopen; Hide is not cancellation and does not clear an uncertain request. Do not create a fresh controller to bypass an uncertain submission.

Call `SetSession` whenever authentication context changes. A different account or null detaches the controller, hides its body in the panel and forbids submission. Reattaching its original account requires server-authenticated context and preserves its original pending request. Generation fencing rejects stale receipts; a separate in-flight latch prevents overlapping requests during same-account token refresh. These are main-thread APIs.

## Submission Semantics

- First actual draft edit records client compose time. First logical submission captures installed build and client submit time; the immutable snapshot receives a fresh key once.
- The immutable client snapshot stores versionCode as nullable Int64; the adapter sends a nullable decimal string. Editor build fields are null.
- Sending locks editing and repeat clicks. Exceptions, missing receipts and incomplete success receipts mean delivery unknown, not rejection.
- Unknown delivery freezes category/body/key/build/times. Retry passes the exact same object and serialized body. Close/reopen cannot unlock it. HTTP429 retains the request and applies the server cooldown, including same-account refresh during the send.
- Only a receipt classified as received with nonempty ticket ID and server receive timestamp shows success. There is no email response or resolution promise.
- Only definitive server nonacceptance may map to `Rejected` and permit correction/new submission. A timeout, 5xx, ambiguous conflict or malformed response must not be mapped to rejection merely to unlock the form.
- A confirmed receipt allows an explicit new inquiry. An availability check is not a receipt. No imaginary status-lookup route is provided.

Client compose and submit clocks are untrusted diagnostic times, distinct from server receipt time. App build comes from the installed player, never R2/latest or server build metadata.

## Privacy and Lifetime

Drafts, pending requests and receipts exist only in memory. No PlayerPrefs, disk queue, automatic logs, token attachment, complete device logs or ops-body copy is added. Bearer handling belongs solely to the confirmed authenticated transport; the draft has no token field. The panel warns against entering personal information, passwords and authentication codes. It renders plain text, not rich text.

**Process exit or destruction of the owner loses both drafts and uncertain requests. This is not restart-safe idempotency.** An accepted request whose response was lost could be submitted again after restart if a new key is created. Durable recovery needs an explicit privacy/retention/encryption policy and Server reconciliation/idempotency retention contract first; the error queue is not an appropriate transaction outbox.

Attachments, anonymous reception, replies, status editing, email/push and account mutation are out of scope.

## Verification Scope

Tests exercise frozen retries, duplicate clicks, session refresh and account fencing, missing receipt fields, definitive rejection, exception handling and local panel close/reopen. The dropdown interaction test enters PlayMode to exercise TMP Start, expanded items, selection and closing. Fixture credentials remain in tests. Camera renders cover 360x640, 1080x1920 and a short 360x480 viewport (scrolled to the bottom); these do not establish device keyboard, touch, network or server delivery correctness.

2026-09-06 final local run: `TestResults-Support-Full2.xml` reports 756 total, 753 passed, one failed and two explicit skips. All 12 support tests passed. Remaining failure: `LaunchAudioQualityTests.ImportedLoop_IsContinuousAndRepeatedCuesDoNotLatchAcrossFrames`, "No forced autoplay". This failure was also observed in the earlier network full run; causality is not established and audio was not modified in this slice. The full suite is not green.

Review regressions were reproduced before fixes: session refresh allowed two outstanding sends, and short viewports lacked scrolling. An additional expanded-dropdown test caught an undersized template content rect. Current tests cover all three. Images in `QA/Act1Launch/Screenshots/SupportInquiry/draft_<width>_<height>.png` were visually inspected. No actual customer data or network was used.

Confirmed: POST /v1/support/tickets, Bearer and Idempotency-Key, five categories, body10-2000 UTF-16 units, versionCode decimal string, ISO client timestamps, 200 RECEIVED/DUPLICATE_REPLAY,409 conflict and429 account limit3 new tickets/10min. A receipt requires category match, ticket ID and server timestamp. Validation errors after earlier unknown delivery cannot unlock a new key.

Pending: restart reconciliation, idempotency retention and personal-data retention/deletion policy. Actual owner/menu integration and device keyboard QA remain unverified. TestResults-AuthenticatedSubmission-Full.xml:790 total,787 pass,1 audio autoplay failure,2 skips;12 authenticated adapter tests pass. TestResults-PlayerEntry-Red.xml also verifies all11 functional adapter tests, including the new same-owner429 review regression (the8 missing entry-client tests intentionally fail).

Latest shared regression: TestResults-PlayerEntry-FullFinal.xml reports813 total,
811 passed,0 failed,2 explicit local-HTTP skips. The audio failure was traced to
the test source already marked playing before configuration, corrected by an
explicit stopped fixture baseline. No production audio code changed. This
does not upgrade support readiness to real-menu or server-reception E2E.

<!-- preserved:source-e305b417d6634f60:end -->


<a id="source-deb3c8a26944fb44"></a>
## Unity Editor Environment Discovery - 2026-08-30

원출처: `unity-project/Assets/_LASTRAIL/Docs/unity-editor-environment-discovery-2026-08-30.md`

<!-- preserved:source-deb3c8a26944fb44:start -->
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

<!-- preserved:source-deb3c8a26944fb44:end -->


<a id="source-ad6c999fb5f40078"></a>
## Unity Test Runner CLI Readiness - 2026-09-01

원출처: `unity-project/Assets/_LASTRAIL/Docs/unity-test-runner-cli-readiness-2026-09-01.md`

<!-- preserved:source-ad6c999fb5f40078:start -->
# Unity Test Runner CLI Readiness - 2026-09-01

Purpose: keep launch verification evidence reproducible for Unity `6000.3.22f1` without changing gameplay, UI design, server contracts, rewards, economy, story truth, gacha probability, OAuth, signing, Rail Choice Events, or World Raid scope.

## Root Cause

The failing command combined `-runTests` with `-quit`. In this project on Unity `6000.3.22f1`, that combination can make Unity perform a normal batchmode quit after script compilation before `UnityEditor.TestTools.TestRunner.CommandLineTest.Executer.InitializeAndExecuteRun` starts. The process exits with code `0`, but the requested `-testResults` XML is not created.

The successful command omits `-quit`. Unity Test Framework starts the command-line test run, writes XML to the requested path, and then exits the Editor itself.

## Correct Command

Use:

```powershell
& "D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" `
  -batchmode `
  -nographics `
  -projectPath "E:\Projects\LASTRAIL\unity-project" `
  -runTests `
  -testPlatform EditMode `
  -testResults "E:\Projects\LASTRAIL\unity-project\TestResults-EditMode.xml" `
  -logFile "E:\Projects\LASTRAIL\unity-project\Unity-EditMode.log"
```

Do not add `-quit` to `-runTests` commands. Test Framework owns process exit after the run completes.

Focused example:

```powershell
.\Tools\run-editmode-tests.ps1 `
  -TestFilter "LaunchScreenScreenshotQaReportTests" `
  -TestResults "E:\Projects\LASTRAIL\unity-project\TestResults-ScreenshotFidelity.xml" `
  -LogFile "E:\Projects\LASTRAIL\unity-project\Unity-ScreenshotFidelity.log"
```

Full example:

```powershell
.\Tools\run-editmode-tests.ps1 `
  -TestResults "E:\Projects\LASTRAIL\unity-project\TestResults-EditMode.xml" `
  -LogFile "E:\Projects\LASTRAIL\unity-project\Unity-EditMode.log"
```

## Verification Evidence

- Unity version: `6000.3.22f1`.
- Test Framework package in manifest: `com.unity.test-framework` requested as `1.5.1`, resolved by Unity package cache as `1.6.0`.
- Focused no-quit run: `LaunchScreenScreenshotQaReportTests`, total `7`, passed `7`, failed `0`, XML generated at `TestResults-ScreenshotFidelity-NoQuit.xml`.
- Full no-quit run: total `588`, passed `588`, failed `0`, XML generated at `TestResults-ScreenshotFidelity-Full-NoQuit.xml`.
- Logs show `CommandLineTest.Executer.InitializeAndExecuteRun`, `Saving results to`, and `Test run completed. Exiting with code 0`.

## Stable Rule

- `-runTests` command: omit `-quit`.
- ExecuteMethod validation/screenshot commands: `-quit` is still acceptable because those methods are not Test Framework command-line runs.
- A launch verification run is valid only when the requested XML exists and contains a `/test-run` node with nonzero `total`.
- The wrapper waits briefly for the XML file and parseable `/test-run` node because Unity can log `Saving results to` before the file is visible to the caller.
- The wrapper reports nonzero failure counts from XML as command failure for launch verification.

<!-- preserved:source-ad6c999fb5f40078:end -->
