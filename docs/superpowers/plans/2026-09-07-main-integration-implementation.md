# Main Integration Implementation Plan

Approved: user via Plan, 2026-09-07. Execute local fixes and scene integration;
no actual content/APK build, remote activation, SDK installation or device work.

1. Auth slice: regression tests for saved guest nickname cancellation and failed
   password cancellation; RED, serialized secure-session recovery, GREEN. Auth
   worker owns owner/runtime/panel/tests; preserve replay/account guards.
2. Network slice: regression tests for initial-plan consent deferral and fresh
   resume snapshot; RED, policy/service fixes, GREEN. Network worker owns policy,
   network service/bridge/tests. Parent owns bootstrap integration.
3. Parent slice: retry failed lobby through same-release preparation, with focused
   regression; add approved-location scene loader and Main mount, retain boot
   owner and original shipping lobby binding. No duplicated services or local art.
4. Register scene source/meta and required Addressable manifest membership, extend
   candidate validation for scene closure without building actual content.
5. Parent runs Unity 6000.3.22f1 exclusively, all C# writers frozen during each run.
   Start focused RED then GREEN; validate actual boot/Main scene composition and
   lifecycle with clearly labeled local fixtures. No forced process termination.
6. Review changes, run justified regression, record blocked external integrations,
   owner file list, verification and source-packet invalidation in canonical.
   Selectively commit only attributable work when dependency-safe; never include
   unrelated dirty changes or claim fresh-checkout reproducibility otherwise.

Current workspace is the authorized dirty runtime E:/Projects/LASTRAIL; preserve
untracked dependencies rather than create a HEAD-only worktree that loses them.
Audit and proposed Main design remain in the canonical Unity handoff. Google,
durable logout/account switch and update destination must remain explicitly
unavailable where approved SDK/configuration/transaction contracts are absent.
