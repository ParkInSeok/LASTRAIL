ALTER TABLE player_profiles
  ADD COLUMN display_name_normalized TEXT;

ALTER TABLE player_profiles
  ADD COLUMN display_name_updated_at INTEGER;

CREATE UNIQUE INDEX idx_player_profiles_display_name_normalized_unique
  ON player_profiles(display_name_normalized)
  WHERE display_name_normalized IS NOT NULL;
