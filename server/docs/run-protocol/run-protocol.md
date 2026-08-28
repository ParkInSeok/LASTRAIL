# Run Protocol

Server does not replay every combat frame. Runs freeze balance version, snapshot, wave plan, augment plan/history, tactical ledger, action ceiling, and validation envelopes.

Checkpoint waves are 3, 6, 9, 12, and 15. Rewards are calculated by the server from the run balance version, not the current active balance.

Act 1 launch note:

- Ordinary operations remain 15-wave encounters.
- Augment/tactic choices are granted/selected from server-authored
  start-of-run package refs instead of mid-wave checkpoint drafts.
- `run_checkpoints`, `run_augment_history`, and `run_tactical_ledger` remain
  schema/backlog surfaces for future mid-run drafting.
- Raid boss-only encounters use the same full start package refs before the
  boss fight.
- Unity may echo selected refs in run finish for validation/diagnostics, but
  reward/economy/story/offline/gacha truth remains server-owned.
