-- Original confirmed selection/definition binding. Never HP or runtime APPLIED.
CREATE TABLE run_augment_effect_originals (
 run_id TEXT NOT NULL REFERENCES runs(id),
 effect_id TEXT NOT NULL CHECK(effect_id='O22'),
 grant_event_id TEXT NOT NULL,
 account_id TEXT NOT NULL REFERENCES accounts(id),
 request_hash TEXT NOT NULL,
 original_hash TEXT NOT NULL,
 original_json TEXT NOT NULL,
 PRIMARY KEY(run_id,effect_id),
 UNIQUE(run_id,grant_event_id)
);
CREATE TRIGGER augment_effect_original_immutable_update BEFORE UPDATE ON run_augment_effect_originals
BEGIN SELECT RAISE(ABORT,'augment_effect_original_immutable'); END;
CREATE TRIGGER augment_effect_original_immutable_delete BEFORE DELETE ON run_augment_effect_originals
BEGIN SELECT RAISE(ABORT,'augment_effect_original_immutable'); END;
