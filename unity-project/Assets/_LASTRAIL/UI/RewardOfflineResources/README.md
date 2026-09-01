# LAST RAIL Act 1 Reward / Offline Reward Visual Resources

These reward and offline settlement files are high-quality 2D UI illustration replacements for the Act 1 launch UI.

Source:
- Generated with the built-in image generation tool, then resized/padded into Unity sprite assets at the stable paths listed below.
- The source mapping lives in `Assets/_LASTRAIL/UI/RewardOfflineResources/high-quality-replacement-sources.json`.
- `LaunchRewardOfflineResourceBuilder` now preserves these replacement PNGs and only regenerates manifest/showcase/import settings.
- No external paid asset dependency.

Quality audit:
- Before: `PROCEDURAL_PLACEHOLDER`; the previous builder drew line/rectangle-based Unity textures.
- After: `HIGH_QUALITY_2D_ILLUSTRATION`; each player-facing reward/offline visual is a generated 2D rail UI asset.

Scope:
- `reward_result_plate`
- `reward_line_item_frame`
- `claim_success_badge`
- `claim_pending_claimable_accent`
- `offline_settlement_meter`
- `offline_duration_accumulation_strip`
- `offline_empty_state_visual`
- `reward_chest_supply_crate_accent`
- `reward_material_glow`

Usage:
- These are presentation resources only for RewardReview and OfflineReward screen polish.
- They are not authoritative gameplay, account, claim, balance, grant, or offline accumulation data.
- The manifest at `Assets/_LASTRAIL/UI/Style/lastrail-reward-offline-resources.json` is the stable QA/readiness reference.
