# LAST RAIL Unity Session Status

Status: COMPLETE

Latest commit message: `fix(unity): polish combat QA flow`.

Completed:
- Polished `FullyAnimatedCombatHud.prefab` so Act 1 combat QA frames read as actual gameplay flow: exterior train/progression/threat layer on top, illustrated train interior battle layer below.
- Removed combat-frame showcase furniture from the battle presentation: no operator card frames, no enemy slot row frames, no CardPolish/Frames art in combat evidence.
- Preserved the four combat interior backgrounds by surface:
  - `combat_ordinary_wave_demo` -> `combat_train_interior_initial_squad`
  - `combat_elite_squad_a_demo` -> `combat_train_interior_elite_squad_a`
  - `combat_elite_squad_b_demo` -> `combat_train_interior_elite_squad_b`
  - `combat_story_raid_boss_demo` -> `combat_train_interior_story_raid_boss`
- Added a capture-time validator that rejects combat surfaces containing `Operators/CardPolish/Frames` sprites.
- Rehashed final combat PNG files immediately before writing the QA report so MP4 export validates the actual bytes on disk.
- Updated tests to assert the new actual-flow combat layout contract.
- Regenerated only `QA/Act1Launch/Video/act1-launch-latest-qa.mp4`.

Latest MP4:
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Video\act1-launch-latest-qa.mp4`
- Size: 132,958,710 bytes
- 1080x1920, 90 seconds, 30 fps
- Source frame count: 14
- Encoded frame count: 2688
- Retained MP4 count: 1

Actual-flow combat evidence:
- Initial/free operators: 5
  - `op_kyle`, `op_reina`, `op_eve`, `op_sera`, `op_mira`
  - Frame SHA256: `C08CE0B67719A195D0115DF11EF2BA87593C1D492732A3B7842F24E58453ABDB`
- Elite operators: 10
  - Elite A: `op_vex`, `op_nyx`, `op_sable`, `op_marrow`, `op_iris`
  - Frame SHA256: `082CAB8284A781167BDBFB634729B911A30A82D640BE4BB61573ECE0754F43E8`
  - Elite B: `op_rail`, `op_anchor`, `op_lumen`, `op_cinder`, `op_warden`
  - Frame SHA256: `DF2861C2B9A42DCB2B8534264254DD09AE42803E9E5725C668B0576411F9AB34`
- `combatSegmentFrameHashesDiffer`: true
- `combatInteriorBackgroundsDiffer`: true
- `usesFallbackComposite`: false

Verification:
- PASS: `python -m py_compile E:\Projects\LASTRAIL\unity-project\Tools\export-act1-launch-latest-qa-mp4.py`
- PASS: focused `LaunchEditorFlowVideoQaReportTests`, 5/5
- PASS: focused `LaunchScreenPrefabBindingTests`, 16/16
- PASS: full Unity EditMode suite, 619/619
- PASS: `python Tools\export-act1-launch-latest-qa-mp4.py --project-root E:\Projects\LASTRAIL\unity-project`
- Visual spot check: combat ordinary, elite B, and story raid boss frames show exterior train/progression above and interior battle composition below, with operators left/center-left and enemies right.

Unity startup notes:
- Earlier blocker was transient in this run. Unity still logged non-fatal licensing validation noise, but Package Manager connected and tests/capture completed.
- Two immediate test starts hit a temporary project-lock race after prefab rebuilds; retrying after Unity fully exited succeeded.
