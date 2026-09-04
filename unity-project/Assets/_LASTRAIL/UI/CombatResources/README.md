# LAST RAIL Act 1 Combat Art Resources

These enemy, boss, accent, VFX, and combat-interior files are project-generated launch combat resources created with the OpenAI image_gen built-in tool for LAST RAIL Act 1.

Source sheets:
- `Assets/_LASTRAIL/UI/CombatResources/Sources/act1_enemy_boss_contact_sheet_generated.png`
- `Assets/_LASTRAIL/UI/CombatResources/Sources/act1_combat_vfx_contact_sheet_generated.png`
- `Assets/_LASTRAIL/UI/CombatResources/Sources/combat_train_interior_initial_squad_grounded_floor_source.png`

Scope:
- 6 normal enemy display resources.
- 2 elite enemy display resources.
- 1 `raid_raider_king_engine` boss visual resource.
- 8 combat VFX overlay resources plus 2 boss HUD accent resources.
- 4 canonical train-interior combat backgrounds under `Assets/_LASTRAIL/UI/CombatResources/Interiors/`.
- 1 reusable visual-only unit grounding shadow, `Accents/combat_unit_contact_shadow.png`.

Interior background note:
- `combat_train_interior_initial_squad.png`, `combat_train_interior_elite_squad_a.png`, `combat_train_interior_elite_squad_b.png`, and `combat_train_interior_story_raid_boss.png` are the only canonical Act 1 combat interior backgrounds.
- These interiors are composed for side-view / slight 3-quarter combat placement: a clear left/center-left operator deck, matching right-side enemy doorway/platform, shallow perspective, visible floor seams, and foot-contact baselines.
- Do not add `_2` variants or duplicate generated temp assets to the project; replace these canonical paths when polishing Act 1 combat interiors.
- The contact shadow is presentation-only and exists to ground combat sprites visually on these battle lanes.
- `combat_train_interior_initial_squad.png` was replaced with a grounded-floor interior so the left squad cluster reads as standing on the same train-car floor plane.

Usage:
- Use the manifest at `Assets/_LASTRAIL/UI/Style/lastrail-combat-art-resources.json` for QA and resource readiness.
- These are presentation resources only. They do not encode combat formulas, rewards, economy grants, story progress, unlocks, offline rewards, gacha odds, or ledger state.
- External paid asset dependency: none. No external paid asset dependency is required for these generated launch resources.
