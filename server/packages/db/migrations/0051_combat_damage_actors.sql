ALTER TABLE combat_damage_states ADD COLUMN actor_state_json TEXT CHECK(actor_state_json IS NULL OR json_valid(actor_state_json));
