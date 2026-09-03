# LAST RAIL Unity Session Status

Status: COMPLETE

Latest commit message pending: `fix(unity): ground combat QA units`.

Completed:
- Replaced the Act 1 train-interior combat backgrounds in-place at the four canonical paths. No `_2` or duplicate project interior variants were added.
- Shifted combat unit placement from showcase-style vertical spread to a lower side-view battle-lane baseline family.
- Added one reusable visual-only contact shadow asset so operators and enemies sit on the floor/pads instead of reading as floating.
- Kept `LaunchScreenPrefabBindingBuilder`, `LaunchEditorFlowVideoQaRunner`, and MP4 export validation on the canonical four interior ids/paths.
- Regenerated only `QA/Act1Launch/Video/act1-launch-latest-qa.mp4`.

Canonical interior paths:
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_initial_squad.png`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_elite_squad_a.png`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_elite_squad_b.png`
- `Assets/_LASTRAIL/UI/CombatResources/Interiors/combat_train_interior_story_raid_boss.png`

Latest MP4:
- `E:\Projects\LASTRAIL\unity-project\QA\Act1Launch\Video\act1-launch-latest-qa.mp4`
- Size: 129,035,466 bytes
- 1080x1920, 90 seconds, 30 fps
- Source frame count: 14
- Encoded frame count: 2688
- Retained MP4 count: 1

Actual-flow combat evidence:
- `combat_ordinary_wave_demo`
  - Operators: `op_kyle`, `op_reina`, `op_eve`, `op_sera`, `op_mira`
  - Frame SHA256: `75B57708151CF90C0FAF976908C0D7E40FCCF66A885505752E2E4ADAA61D6B91`
  - Grounding QA: PASS. `op_kyle` is now on the lower left floor lane rather than window/wall height; the full squad reads as grounded on pads/floor with contact shadows.
- `combat_elite_squad_a_demo`
  - Operators: `op_vex`, `op_nyx`, `op_sable`, `op_marrow`, `op_iris`
  - Frame SHA256: `26E03D8350950A3FF52E1543AADDD5DE581E8E66335CDE06A77CBF0BA4102A8B`
  - Grounding QA: PASS. Squad and enemies align to visible left/right pads in the side-view lane.
- `combat_elite_squad_b_demo`
  - Operators: `op_rail`, `op_anchor`, `op_lumen`, `op_cinder`, `op_warden`
  - Frame SHA256: `9E3F6AD0F9CEA7DFE6D42A8DE33C148B4BF0C62E0B0CE68F92C9EEFA8B6BB18F`
  - Grounding QA: PASS. Squad and enemies share the same floor-height system with no wall/window floating read.
- `combat_story_raid_boss_demo`
  - Grounding QA: PASS. Boss-fight squad sits on the damaged floor plane, and right-side enemies align to the same aisle/platform perspective.

Summary evidence:
- Initial/free actual-flow count: 5
- Elite actual-flow count: 10
- `combatSegmentFrameHashesDiffer`: true
- `combatInteriorBackgroundsDiffer`: true
- `usesFallbackComposite`: false
- `_2` interior asset count: 0

Verification:
- PASS: `python -m py_compile E:\Projects\LASTRAIL\unity-project\Tools\export-act1-launch-latest-qa-mp4.py`
- PASS: `git diff --check` for scoped files/assets
- PASS: focused `LaunchScreenPrefabBindingTests`, 16/16
- PASS: focused `LaunchEditorFlowVideoQaReportTests`, 5/5
- PASS: `LastRail.Editor.LaunchEditorFlowVideoQaRunner.CaptureEditorFlowVideoQa`, 14 frames
- PASS: latest MP4 export to `QA/Act1Launch/Video/act1-launch-latest-qa.mp4`
- PASS: full Unity EditMode suite, 619/619

Unity startup notes:
- Unity logged non-fatal licensing validation noise, but Package Manager connected and all requested validation completed.
