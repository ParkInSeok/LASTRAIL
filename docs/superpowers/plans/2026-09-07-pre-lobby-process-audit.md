# Pre-Lobby Process Audit - 2026-09-07

Scope: read-only product audit and focused local policy execution. No product
fixes, builds, device work, remote checks, deployment, secrets, or real mail.

## Verdict

Not ready to certify shipping login -> loading -> lobby. The composition exists,
but content configuration and several authentication branches are incomplete.
The findings below distinguish source-derived reproductions from executed ones.

## Actual Graph

- EditorBuildSettings.asset:8 enables Scenes/ResourceBootstrap.unity.
- ResourceBootstrapController.OnEnable constructs the content backend, network
  policy, download overlay, PlayerEntryRuntime and AuthenticatedScreenEntry.
- Config acceptance triggers stored-session restore. Wi-Fi content preparation
  runs concurrently with authentication. Auth ownership saves credentials before
  profile/nickname preparation and story hydration.
- Accepted owner readiness enables the authenticated download gate; required
  assets are verified, the approved lobby is opened and bound, then the pending
  release is completed. Startup hides only after successful entry.
- Generation/session checks fence stale profile/nickname and screen completion;
  network disposal invalidates queued callbacks. These are source observations,
  not proof of Android execution or a complete account-switch transaction.

All source references below are relative to
E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/.

## Findings And Minimal Next Actions

1. **P1: Saved guest can become inaccessible until restart after nickname cancel.**
   Auth/PlayerAuthOwner.cs:87 saves before preparation; Auth/PlayerEntryRuntime.cs:409
   invalidates the in-memory session, and :445 retries only pending replay or an
   existing session. Fresh guest -> nickname -> return to login can therefore
   strand the saved guest. Add explicit serialized secure-session resume, without
   creating another guest or weakening replay/account guards. Source-derived;
   UI reproduction not executed in this audit.

2. **P1: Resume can temporarily authorize downloads using stale Wi-Fi state.**
   Resources/ResourceBootstrapController.cs:81 refreshes then immediately clears
   suspension; Resources/ContentNetworkService.cs:47 queues Android publication.
   Pause on Wi-Fi -> switch to cellular -> resume before queued callback can
   release download waiters prematurely. Keep suspension until the refreshed
   snapshot is applied, including unchanged snapshots, with lifecycle fencing.
   Executed policy ordering confirms Allowed=true before the cellular snapshot;
   an actual Android network request/race was NOT executed.

3. **P2: Wi-Fi departure before initial size resolution prompts before login.**
   Resources/DownloadAccessPolicy.cs:29 requires RemainingBytes > 0 to remember
   departure, while :38 resets deferral on a new plan. Start Wi-Fi -> cellular
   during catalog preparation -> resolve nonzero plan yields NeedsConsent=true
   with Authenticated=false. Preserve departure history across initial planning;
   keep revision approval separate. Reproduced using the actual policy source.

4. **P2: Failed password attempt followed by cancel removes fresh-account options.**
   Auth/PlayerAuthOwner.cs:125 sets missing=false, while :37 prevents repeating
   restore. Auth/PlayerEntryPanel.cs:59 gates Guest/Register on that state.
   Empty store -> invalid password attempt -> return to login disables both.
   Recheck secure storage serially after cancellation, restoring eligibility only
   after confirmed Missing and no pending replay. Source-derived, not UI-tested.

5. **P2: Logout/account switch is not a shipping workflow.**
   Local invalidation does not clear durable credentials. Auth/PlayerEntryRuntime.cs:201
   and Auth/KeystoreAuthReplayJournal.cs:102 reject replacing stored account A
   with B. No shipping logout/switch handler was found. Add explicit durable
   logout/switch transaction and guest-loss handling; preserve account guards.
   Source-derived, not an executed account-switch scenario.

6. **P2: Google login is disconnected.**
   Auth/PlayerEntryRuntime.cs:190 passes allowGoogle=false; PlayerEntryPanel.cs:45
   raises GoogleRequested with no shipping subscriber found. SDK bridge coverage
   is fixture-only. Native SDK/configuration AND owner-mediated runtime wiring
   are required. Do not label this branch implemented based on contract tests.

7. **P2: Lobby cache failure can repeat without content recovery.**
   Resources/ResourceBootstrapController.cs:104 skips preparation while coordinator
   remains Ready; :108 invalidates readiness only for pin confirmation. A required
   bundle evicted before lobby opening is rejected by AddressableContentBackend.cs:297.
   Retry may therefore repeat the same failure without recalculating/downloading.
   Route failed entry back through same-release preparation and byte calculation.
   Source-derived cache-eviction scenario, not executed.

## Configuration Gates

- ResourceBootstrap.unity:197 currently serializes remoteHostingConfigured=0;
  no serialized trustedCatalogRoot was found. AddressableContentBackend.cs:105
  rejects unconfigured content. This deliberate fail-closed configuration is a
  shipping integration blocker, not permission to invent or activate a host.
- ResourceBootstrapController.cs:140 defaults update action to null URL and an
  empty allowlist. No production caller of ConfigureRequiredUpdateAction was
  found. Update gating exists; usable shipping store navigation is unconnected.
- Existing local required resources and startup prefab references do not establish
  an installed Android package or successful full scene entry. No remote server
  status, current content release, real mail, or Android package was checked here.

## Fresh Verification And Limits

PowerShell Add-Type compiled the unchanged DownloadAccessPolicy.cs directly.
Executed sequence and observations:

```text
Wifi -> OtherOnline -> SetPlan("audit-release",100)
Authenticated=false; NeedsConsent=true; Allowed=false
Expected by Wi-Fi-departure policy: no consent prompt until login.

Wifi -> SetPlan(...,100) -> Suspended=true -> Suspended=false
Allowed=true before refreshed network snapshot.
SetNetwork(OtherOnline): Allowed=false; NeedsConsent=false.
```

This is actual policy-code execution, not a reimplementation, Unity test suite,
Android bridge test, or measured network transfer. Command completed with exit 0;
the observed values demonstrate the adverse states, not passing requirements.

Existing PlayerEntryRuntimeTests use an inactive bootstrap and injected storage,
HTTP and synthetic screen entry. RequiredUpdateBootstrapTests manually compose
the action and backend. Prior localhost bundle tests provide product-backend
coverage with controlled admission/auth; none proves shipping Android end-to-end.

No new Unity run was started: existing Unity 2021.3 process ownership could not
be established (observed PID 42456, later 40560; LASTRAIL requires 6000.3.22f1).
No process was stopped. No new NUnit XML, full regression, device Keystore,
disk-full, Android resume, or actual Google validation is claimed.

## Next Order

1. Authorize and fix saved-session/cancel recovery and resume network fencing;
   add focused regression cases for the exact sequences above.
2. Fix consent deferral and cache-entry retry; rerun focused product-path tests
   after confirming exclusive ownership of the Unity project runner.
3. Complete Google and logout/switch scope; supply approved update destination
   and content build inputs through their existing approval gates.
4. Validate the enabled ResourceBootstrap scene and actual shipping lobby on an
   approved Android candidate, including restart, expiry, cellular transition,
   hash/catalog rejection, offline/disk failures and repeated user actions.

Persistence: local report only, no commit. Existing dirty/untracked product/QA
files preserved. Canonical Unity handoff records this audit. Cross-task direct
report security hold remains; no report was sent through another task.
