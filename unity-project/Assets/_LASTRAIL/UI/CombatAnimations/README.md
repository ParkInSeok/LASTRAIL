# LAST RAIL Act 1 Combat Animation Resources

These launch combat animation resources are visual-only combat unit sprites for LAST RAIL operators, enemies, and the story raid boss.

Coverage:
- 5 initial/free operators.
- 10 gacha elite operators.
- 6 normal Act 1 enemies.
- 2 elite Act 1 enemies.
- 1 `boss_raider_king_engine` story raid boss.
- 5 animation states per resource, 4 frames per state, 512x512 PNG frames.

Usage:
- Frame sequences live under `Assets/_LASTRAIL/UI/CombatAnimations/`.
- Source contact sheets and per-unit combat source cutouts live under `Assets/_LASTRAIL/UI/CombatAnimations/Sources/`.
- Operator frames must be `spriteType=combat_unit`, `portraitDerived=false`, `hasCardFrame=false`, and `transparentOrSceneCompatible=true`.
- These frames are not portrait/card/icon crops. Card and portrait resources stay under `Assets/_LASTRAIL/UI/Operators/`.
- Animation clips and Animator Controllers are generated beside the frames for Editor-verifiable playback.
- The showcase prefab is `Assets/_LASTRAIL/UI/CombatAnimations/Showcase/LRLaunchCombatAnimationShowcase.prefab`.
- These resources are presentation-only and do not encode combat formulas, rewards, economy grants, story progress, unlocks, offline rewards, gacha odds, or ledger state.
- External paid asset dependency: none.
