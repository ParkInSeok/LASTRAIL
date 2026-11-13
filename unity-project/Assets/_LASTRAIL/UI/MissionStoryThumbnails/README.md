# LAST RAIL Act 1 Mission Story Thumbnails

These mission/story thumbnail and overlay files are local launch UI assets for LAST RAIL Act 1.

## Resources

- 6 ordinary operation thumbnails: `opn_01_last_departure` through `opn_06_broken_line`
- 1 story raid thumbnail: `raid_raider_king_engine`
- Visual state overlays: locked, available, cleared, first-clear badge, risk accent, and boss-only badge

## Source And Usage

Source:

- Thumbnails: generated 2D illustration contact sheet copied into `Sources/act1_mission_story_thumbnail_contact_sheet_generated_v2.png`, then cropped into the seven launch mission/story raid thumbnail PNGs.
- State overlays: Unity procedural texture builder in `LaunchMissionStoryThumbnailResourceBuilder`.

License note: no external paid or third-party art asset was introduced by this slice.

Use: composable 1024x576 thumbnails for 1080x1920 portrait MissionShell, StoryProgressSummary, and CodexArchive presentation surfaces. Mission access and progression are supplied by existing game state/server responses; these images are presentation-only.

Boundary: this resource set does not change server contracts, gameplay progression, operation access, first-clear policy, rewards, economy, offline rewards, gacha odds, or ledger behavior.
