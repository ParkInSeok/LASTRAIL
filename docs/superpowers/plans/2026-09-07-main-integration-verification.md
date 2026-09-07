# Pre-Lobby Fixes And Main Integration - 2026-09-07

Scope: user-approved local pre-lobby fixes and ResourceBootstrap -> Main source
integration. No actual content/APK build, remote activation, new SDK, signing,
device installation, real email/OAuth, video, or credential deletion.

## Result

Local implementation, focused actual-scene verification and full
regression completed: 1377 total, 1369 passed, 0 failed, 8 skipped. This is NOT a shipping Android or DEV
release PASS: source remains remote-content-unconfigured and the new scene is not
present in previously built catalogs. Existing preparation manifests are stale.

## Implemented

| Audit issue | Local outcome |
| --- | --- |
| Saved guest nickname cancellation | Secure storage recheck after cancellation settles; explicit resume performs serialized replay-protected refresh, preserving the guest and nickname gate |
| Failed password cancellation | Guest/register eligibility returns only after a fresh confirmed Missing read, with pending replay and previous guest-attempt guards retained |
| Stale Wi-Fi resume | Suspension clears only after the fresh snapshot is applied on the Unity context, fenced by lifetime/pause generation |
| Wi-Fi departure before size resolution | Deferral survives initial plan resolution; login precedes cellular consent in this transition |
| Cache/entry retry | Failed entry reruns preparation and byte calculation while retaining accepted same-release admission |
| Google | Remains explicitly unavailable; no approved native SDK/configuration/runtime completion claimed |
| Durable logout/account switch | Remains unimplemented/blocked; local invalidation still preserves credentials, with no guest deletion, merge or silent account replacement |

Update gate/action code remains available but the production approved store URL
is absent. No synthetic successful update navigation was installed. Remote content
flag remains false; no trusted host/release was invented or activated.

## Scene And Ownership

- Start scene remains Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity, the sole
  enabled local build scene. Original bootstrap asset was not deleted/replaced.
- New Main: Assets/_LASTRAIL/Scenes/Main.unity, GUID
  d7f68a30c1254de4b98205fdc163a7e9, stable address identical to asset path.
- Main belongs to remote_launch_required and carries launch_required. The
  configuration builder permits this exact production scene while excluding
  other scene/showcase paths. Main is not a local boot-art container.
- Main contains one MainSceneRoot and initially inactive ContentMount, no auth,
  network owner, EventSystem, camera/audio listener, or direct prefab/art refs.
- AddressableContentBackend resolves the scene by SceneInstance from the approved
  locator. ApprovedCatalogSafety accepts only the shipping Main SceneProvider
  path and verifies bundle dependencies. DEV candidate preflight requires Main;
  catalog verification checks Main/lobby coverage by required bundle closure.
- Resident bootstrap owns auth, network, coordinator and EventSystem. The existing
  MissionShellScreen prefab is instantiated into Main's mount; existing presenter,
  profile/story snapshot, account-link and support binding are reused.
- Admission, accepted auth/profile/story and required content precede Main load.
  Binding must succeed before pending-release completion, Main reveal and hiding
  startup. Missing Main remains on bootstrap with retry.
- A shared entry lease serializes entry and cleanup. Each attempt captures its
  original owners. Pending prefab operations drain before Main unload. Duplicate
  opens do not invalidate the first request. Unload status is checked; failed
  cleanup retains its lease for explicit retry, while lost ownership fails closed.

## Verification Evidence

Unity 6000.3.22f1, Tools/run-editmode-tests.ps1, graphics enabled, no -quit.
All paths below are under E:/Projects/LASTRAIL/unity-project/.

| Evidence | Total / Passed / Failed | Meaning |
| --- | --- | --- |
| TestResults-MainIntegration-Red.xml | 20 / 9 / 11 | New auth, network and Main regressions failed before implementation |
| TestResults-MainIntegration-ContractRed.xml | 130 / 123 / 7 | Scene-provider/required-scene RED; fixture and isolation failures preserved |
| TestResults-MainIntegration-Green1.xml | 130 / 126 / 4 | Auth/network passed; scene fixture initialization and DEV serialization checks still failed |
| TestResults-MainIntegration-Green2.xml | 55 / 53 / 2 | DEV checks passed; missing fixture tracking/provider metadata caused scene log failures |
| TestResults-MainIntegration-Scenes.xml | 7 / 7 / 0 | Final focused Main source and actual-scene fixture checks passed |
| TestResults-MainIntegration-Full.xml | 1377 / 1369 / 0, 8 skipped | Full regression of shared auth/resource changes, process exit 0 |

Matching Unity-MainIntegration-*.log files are retained. The isolated test
Addressables runtime required frame callbacks, completion tracking delegates and
valid local provider metadata; those harness omissions were corrected, not
suppressed with log-ignore rules. Earlier DEV source-invariance failures were
retained; the guard was not weakened and now reports differing field names.
The final full run passed those DEV isolation cases. Eight skipped cases require
opt-in localhost HTTP servers, built bundle/delta fixtures, or receipt/run harnesses;
they were not rerun against new built content. No test failure was hidden as a skip.
Repository-wide diff whitespace checking reported pre-existing/generated metadata
whitespace; unrelated assets were left unchanged. Scoped owner-code checking is
performed separately before commit.

The scene test loads the enabled ResourceBootstrap, first verifies unconfigured
failure, then substitutes explicit in-memory HTTP/storage/catalog and content
verification dependencies. It runs the ORIGINAL bootstrap entry callback,
Addressable scene/instance hosts, actual Main.unity, actual MissionShellScreen
prefab, profile/name/story preparation and BindLobby. It verifies nickname/content
gates, missing-Main retry, single entry, one owner/EventSystem, displayed profile,
matching release completion and cleanup on local invalidation.

A delayed source provider verifies pending instance drain before releasing the
inactive mount. A failed-cleanup state is injected while retaining a real scene
handle to verify retry ownership. This is NOT an actual platform unload-failure
reproduction. Source providers do not prove remote bundle delivery or disk/cache
behavior. Initializers/storage/HTTP are fixtures, not deployed auth or Keystore.

Android NetworkBridge.java separately compiled with the installed Unity OpenJDK
javac and android-36/android.jar, exit 0. Output: Temp/MainNetworkJavaCheck.
This is a Java compilation check, not APK generation or device callback evidence.

Independent read-only review found four scene lifetime issues during development;
pending-instance drain, captured ownership, duplicate generation and checked
unload were corrected. Follow-up review found cleanup-retry ownership missing;
that was corrected and covered by an injected failure-state case. Final scoped
review reported no further concrete blocker; this is not a repository-wide review.

## Process And Remaining Gates

Unity2021 was identified as another project (SpiritCreatureWork), not LASTRAIL.
No process was forcibly stopped. Delayed fixture runs ended with their own test
timeouts; the earlier request to stop the owned test became unnecessary.

1. Preserve the verified local result and selective owner commit; keep failed evidence.
2. Refresh source inclusion/hash/membership observations before a separately
   approved source freeze. Preserve the original preparation packet as history.
3. Obtain approved SDK/Google configuration, durable logout/switch contract and
   update destination before enabling those unavailable branches.
4. Build/activate an approved content release containing Main and create an
   approved Android candidate only under separate authorization. Then validate
   actual auth/Keystore/restart, cellular transitions, disk/cache failure and UI.

Persistence at checkpoint: local/uncommitted report and owner code; unrelated
server/admin/QA/art changes preserved. No full dirty-workspace commit or new source
snapshot claimed. Direct-report security HOLD remains; canonical Unity handoff is
the integration record and no report was rerouted through another task.
