# LAST RAIL Unity Session Status

Status: COMPLETE

Commit: reported in final task report.

Root cause:
- The previous latest-video QA report could pass from screen filenames and resource readiness without proving actual combat-flow operator IDs or checking duplicate combat frame hashes.
- The saved combat frames were stale false-positive evidence: `frame_002`, `frame_003`, and `frame_004` previously shared SHA256 `E845CAFCA63BE9E620E1C8A43F796443897DFB6570FD591BD24A71760F4E07B8`.
- Headless `-nographics` capture forces fallback composite frames; the final capture was run with graphics enabled and now records `usesFallbackComposite: false`.

Implementation:
- `LaunchEditorFlowVideoQaRunner` now records actual visible operator IDs from instantiated combat HUD slots, validates them against expected segment IDs, stores per-combat-frame SHA256, and fails if the three combat segment hashes are not unique.
- `FullyAnimatedCombatHudRootBehaviour` now renders distinct squad labels for initial, elite A, and elite B combat states.
- `LaunchEditorFlowVideoQaReport` now serializes actual-flow evidence and coverage fields, and fails report status on ID mismatch, incomplete actual-flow union, or identical combat frame hashes.
- `export-act1-launch-latest-qa-mp4.py` now validates editor actual-flow evidence before MP4 cleanup/encoding and preserves the evidence in `latest-video-qa-report.json`.

Changed files:
- `Assets/_LASTRAIL/Editor/LaunchEditorFlowVideoQaRunner.cs`
- `Assets/_LASTRAIL/Scripts/Presentation/Battle/FullyAnimatedCombatHudRootBehaviour.cs`
- `Assets/_LASTRAIL/Scripts/Presentation/LaunchEditorFlowVideoQaReport.cs`
- `Assets/_LASTRAIL/Tests/EditMode/LaunchEditorFlowVideoQaReportTests.cs`
- `Tools/export-act1-launch-latest-qa-mp4.py`
- `QA/Act1Launch/Reports/editor-video-qa-report.json`
- `QA/Act1Launch/Reports/latest-video-qa-report.json`

Combat frames:
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Frames\EditorFlowVideoQa\frame_002_combat_ordinary_wave_demo_1080x1920.png`
  - SHA256: `E845CAFCA63BE9E620E1C8A43F796443897DFB6570FD591BD24A71760F4E07B8`
  - Visible IDs: `op_kyle`, `op_reina`, `op_eve`, `op_sera`, `op_mira`
  - Label: `초기 분대 5 / 침입 적 4`
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Frames\EditorFlowVideoQa\frame_003_combat_elite_squad_a_demo_1080x1920.png`
  - SHA256: `4A8E75C4081033383A64277C87F8EEF30D9783048A0243795B7BF9A7D24092CF`
  - Visible IDs: `op_vex`, `op_nyx`, `op_sable`, `op_marrow`, `op_iris`
  - Label: `정예 분대 A 5 / 침입 적 4`
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Frames\EditorFlowVideoQa\frame_004_combat_elite_squad_b_demo_1080x1920.png`
  - SHA256: `18BFD201A0E56F5D1AD662A8CDCC636C725AF24DF519A4FE0DBDD41B723702E0`
  - Visible IDs: `op_rail`, `op_anchor`, `op_lumen`, `op_cinder`, `op_warden`
  - Label: `정예 분대 B 5 / 침입 적 4`

Latest MP4:
- Path: `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Video\act1-launch-latest-qa.mp4`
- SHA256: `A79AC8EAAB8BBF1E7684340BDEB26F34F583DAACA510BCD63981B1E07717353E`
- Retained MP4 count: 1

Verification:
- RED: stale latest-video validation failed with `Missing actual-flow combat evidence for combat_ordinary_wave_demo`.
- GREEN: `LaunchEditorFlowVideoQaReportTests`: 4 passed, 0 failed.
- GREEN: `LaunchResourcePlacementFlowTests`: 4 passed, 0 failed.
- GREEN: editor flow capture PASS, `usesFallbackComposite: false`, actual-flow coverage 15.
- GREEN: seven-screen screenshot QA PASS, 7 screenshots, 0 failures.
- GREEN: latest-video QA PASS, retained MP4 count 1, actual-flow coverage 15.
- GREEN: full EditMode wrapper PASS, 618 passed, 0 failed.

Visual spot check:
- Ordinary frame shows the original initial squad in the lower-left combat lane.
- Elite A frame now visibly swaps to Vex/Nyx/Sable/Marrow/Iris and shows the elite A label.
- Elite B frame now visibly swaps to Rail/Anchor/Lumen/Cinder/Warden and shows the elite B label.

Blockers: none.
