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
