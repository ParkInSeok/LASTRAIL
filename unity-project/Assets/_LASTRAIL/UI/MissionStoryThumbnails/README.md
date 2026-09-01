# LAST RAIL Act 1 Mission Story Thumbnails

These mission/story thumbnail and overlay files are procedurally generated local launch UI assets for LAST RAIL Act 1.

## Resources

- 6 ordinary operation thumbnails: `opn_01_last_departure` through `opn_06_broken_line`
- 1 story raid thumbnail: `raid_raider_king_engine`
- Visual state overlays: locked, available, cleared, first-clear badge, risk accent, and boss-only badge

## Source And Usage

Source: Unity procedural texture builder in `LaunchMissionStoryThumbnailResourceBuilder`.

License note: no external paid or third-party art asset was introduced by this slice.

Use: composable 1024x576 thumbnails for 1080x1920 portrait MissionShell, StoryProgressSummary, and CodexArchive presentation surfaces. Mission access and progression are supplied by existing game state/server responses; these images are presentation-only.

Boundary: this resource set does not change server contracts, gameplay progression, operation access, first-clear policy, rewards, economy, offline rewards, gacha odds, or ledger behavior.
