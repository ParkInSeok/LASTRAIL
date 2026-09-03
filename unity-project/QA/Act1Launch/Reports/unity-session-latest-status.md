# LAST RAIL Unity Session Status

Status: BLOCKED

Commit: this commit; final hash is reported in the task response. Unity editor verification and regenerated QA evidence are blocked by local Unity startup failures.

Root cause found:
- Combat operator actual-flow validation had already been partially strengthened in `74f5030`, and current `frame_002`/`frame_003`/`frame_004` hashes now differ.
- The remaining false-positive gap is the train-interior background: `FullyAnimatedCombatHud.prefab` uses a single fixed `InteriorTrainCarBackgroundImage` sprite from `CombatResources/Lanes`, and the latest report has no `combatInteriorBackgroundBySurface` or `combatInteriorBackgroundsDiffer` fields.
- The MP4 export script also preserved operator evidence but did not validate or preserve combat-interior background evidence, so the latest report could still pass without proving deployment-specific interior art.

Implemented before the Unity blocker:
- Generated four illustrated train-interior background PNGs under `Assets/_LASTRAIL/UI/CombatResources/Interiors/`.
- Added Unity `.meta` files for the new interior folder/assets using sprite importer settings.
- Updated `LaunchEditorFlowVideoQaRunner` to bind a different `InteriorTrainCarBackgroundImage` sprite per combat segment and record actual background id/path evidence.
- Updated `LaunchEditorFlowVideoQaReport` and JSON serialization to include `combatInteriorBackgroundBySurface`, `combatInteriorBackgroundsDiffer`, and blockers for duplicate or incomplete combat-interior evidence.
- Updated `export-act1-launch-latest-qa-mp4.py` so latest MP4 export validates and preserves combat-interior evidence before writing `latest-video-qa-report.json`.
- Added a report regression test for missing/duplicated combat-interior evidence.

Changed files:
- `Assets/_LASTRAIL/Editor/LaunchEditorFlowVideoQaRunner.cs`
- `Assets/_LASTRAIL/Scripts/Presentation/LaunchEditorFlowVideoQaReport.cs`
- `Assets/_LASTRAIL/Tests/EditMode/LaunchEditorFlowVideoQaReportTests.cs`
- `Tools/export-act1-launch-latest-qa-mp4.py`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors.meta`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_initial_squad.png`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_initial_squad.png.meta`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_elite_squad_a.png`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_elite_squad_a.png.meta`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_elite_squad_b.png`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_elite_squad_b.png.meta`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_story_raid_boss.png`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_story_raid_boss.png.meta`
- `QA/Act1Launch/Reports/unity-session-latest-status.md`

Current stale combat frame evidence:
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Frames\EditorFlowVideoQa\frame_002_combat_ordinary_wave_demo_1080x1920.png`
  - SHA256: `E845CAFCA63BE9E620E1C8A43F796443897DFB6570FD591BD24A71760F4E07B8`
  - Visible IDs in current stale report: `op_kyle`, `op_reina`, `op_eve`, `op_sera`, `op_mira`
  - Background id/path: not present in current stale report.
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Frames\EditorFlowVideoQa\frame_003_combat_elite_squad_a_demo_1080x1920.png`
  - SHA256: `4A8E75C4081033383A64277C87F8EEF30D9783048A0243795B7BF9A7D24092CF`
  - Visible IDs in current stale report: `op_vex`, `op_nyx`, `op_sable`, `op_marrow`, `op_iris`
  - Background id/path: not present in current stale report.
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Frames\EditorFlowVideoQa\frame_004_combat_elite_squad_b_demo_1080x1920.png`
  - SHA256: `18BFD201A0E56F5D1AD662A8CDCC636C725AF24DF519A4FE0DBDD41B723702E0`
  - Visible IDs in current stale report: `op_rail`, `op_anchor`, `op_lumen`, `op_cinder`, `op_warden`
  - Background id/path: not present in current stale report.
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Frames\EditorFlowVideoQa\frame_005_combat_story_raid_boss_demo_1080x1920.png`
  - SHA256: `C5D27D1B133FE3C8A2863B8C5001F5A292E690A6BB99347F074DED0053A5D854`
  - Visible IDs in current stale report: not tracked for boss frame.
  - Background id/path: not present in current stale report.

Latest MP4:
- Path: `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Video\act1-launch-latest-qa.mp4`
- Retained MP4 count in current stale report: 1
- Not regenerated in this task because Unity could not start the capture method.

Verification results:
- RED: Added background-evidence regression test; first Unity run could not write XML because Unity exited before compilation. The production API was missing `LaunchEditorFlowCombatInteriorEvidence`/`combatInteriorBackgroundBySurface` at that point.
- PASS: `python -m py_compile E:\Projects\LASTRAIL\unity-project\Tools\export-act1-launch-latest-qa-mp4.py`
- PASS: `git diff --check` for the touched code/script/assets.
- BLOCKED: Focused EditMode test, capture runner, 7-screen screenshot QA, latest-video QA regeneration, visual spot check, and full EditMode wrapper. Unity exits before running tests/capture with Package Manager IPC failure:
  - `[Package Manager] Could not connect to IPC stream "Upm-7672" after 30.0 seconds.`
  - `[Package Manager] Could not establish a connection with the Unity Package Manager local server process.`
- BLOCKED: `-noUpm` avoided the Package Manager exit but hung in licensing/headless startup and had to be stopped:
  - `[Licensing::Module] Error: Licensing initialization failed after 74.82s`
  - `[Licensing::Module] The re-connection attempt was UN-successful.`
  - `[Licensing::Module] Error: 'com.unity.editor.headless' was not found.`

Next required command once Unity is healthy:
- Run focused report tests, then `LastRail.Editor.LaunchEditorFlowVideoQaRunner.CaptureEditorFlowVideoQa`, then `Tools/export-act1-launch-latest-qa-mp4.py`, then screenshot/full EditMode QA. The resulting reports should include `combatInteriorBackgroundBySurface` with four distinct background ids/paths and `combatInteriorBackgroundsDiffer: true`.

Blockers:
- Local Unity 6000.3.22f1 batch startup cannot currently reach Package Manager IPC, and the `-noUpm` workaround stalls on licensing/headless initialization.
