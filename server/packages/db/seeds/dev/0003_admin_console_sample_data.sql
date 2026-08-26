INSERT INTO accounts (id, status, created_at, updated_at)
VALUES ('sample-account-1', 'ACTIVE', 1800000000, 1800003600)
ON CONFLICT(id) DO UPDATE SET
  status = excluded.status,
  updated_at = excluded.updated_at;

INSERT INTO player_profiles (account_id, display_name, commander_xp, created_at, updated_at)
VALUES ('sample-account-1', 'Kyle Account', 12000, 1800000000, 1800003600)
ON CONFLICT(account_id) DO UPDATE SET
  display_name = excluded.display_name,
  commander_xp = excluded.commander_xp,
  updated_at = excluded.updated_at;

INSERT INTO account_currencies (account_id, currency, balance, updated_at)
VALUES ('sample-account-1', 'OPERATOR_EXP', 1840, 1800003600)
ON CONFLICT(account_id, currency) DO UPDATE SET
  balance = excluded.balance,
  updated_at = excluded.updated_at;

INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
VALUES
  ('sample-mutation-1', 'sample-account-1', 1, 'DEV_SAMPLE_BOOTSTRAP', 'dev-sample-bootstrap', 1800000000),
  ('sample-mutation-run-1', 'sample-account-1', 2, 'DEV_SAMPLE_RUN', 'dev-sample-run', 1800003600)
ON CONFLICT(account_id, sequence) DO NOTHING;

INSERT INTO account_operators (account_id, operator_id, skill_training, promotion, acquired_at, revision, last_mutation_id)
VALUES
  ('sample-account-1', 'kyle', 3, 1, 1800000000, 1, 'sample-mutation-1'),
  ('sample-account-1', 'reina', 3, 0, 1800000000, 1, 'sample-mutation-1'),
  ('sample-account-1', 'eve', 2, 0, 1800000000, 1, 'sample-mutation-1'),
  ('sample-account-1', 'sera', 2, 0, 1800000000, 1, 'sample-mutation-1'),
  ('sample-account-1', 'mira', 2, 0, 1800000000, 1, 'sample-mutation-1'),
  ('sample-account-1', 'vex', 1, 0, 1800000000, 1, 'sample-mutation-1')
ON CONFLICT(account_id, operator_id) DO UPDATE SET
  skill_training = excluded.skill_training,
  promotion = excluded.promotion,
  revision = excluded.revision,
  last_mutation_id = excluded.last_mutation_id;

INSERT INTO operator_training_core_slots (
  account_id,
  slot_index,
  operator_id,
  trained_level,
  invested_operator_exp,
  revision,
  last_mutation_id
) VALUES
  ('sample-account-1', 0, 'kyle', 32, 4500, 1, 'sample-mutation-1'),
  ('sample-account-1', 1, 'reina', 31, 4300, 1, 'sample-mutation-1'),
  ('sample-account-1', 2, 'eve', 30, 4000, 1, 'sample-mutation-1'),
  ('sample-account-1', 3, 'sera', 30, 4000, 1, 'sample-mutation-1'),
  ('sample-account-1', 4, 'mira', 33, 4800, 1, 'sample-mutation-1')
ON CONFLICT(account_id, slot_index) DO UPDATE SET
  operator_id = excluded.operator_id,
  trained_level = excluded.trained_level,
  invested_operator_exp = excluded.invested_operator_exp,
  revision = excluded.revision,
  last_mutation_id = excluded.last_mutation_id;

INSERT INTO client_builds (
  id,
  platform,
  version_name,
  build_number,
  content_version,
  protocol_version,
  status,
  created_at,
  updated_at
) VALUES (
  'sample-build-1',
  'ANDROID',
  '0.1.0-dev',
  1,
  1,
  1,
  'ALLOWED',
  1800000000,
  1800000000
)
ON CONFLICT(platform, build_number) DO UPDATE SET
  status = excluded.status,
  updated_at = excluded.updated_at;

INSERT INTO operation_previews (
  id,
  account_id,
  mutation_id,
  objective,
  risk,
  balance_config_version,
  preview_json,
  status,
  expires_at,
  created_at
) VALUES (
  'sample-preview-1',
  'sample-account-1',
  'sample-mutation-run-1',
  'sample-operation',
  'III',
  1,
  '{"operation":"sample-operation","risk":"III"}',
  'CONSUMED',
  1800007200,
  1800003600
)
ON CONFLICT(id) DO UPDATE SET
  status = excluded.status;

INSERT INTO runs (
  id,
  account_id,
  preview_id,
  mutation_id,
  balance_config_version,
  status,
  started_at,
  finished_at,
  build_id
) VALUES (
  'sample-run-1',
  'sample-account-1',
  'sample-preview-1',
  'sample-mutation-run-1',
  1,
  'COMPLETED',
  1800003600,
  1800004500,
  'sample-build-1'
)
ON CONFLICT(id) DO UPDATE SET
  status = excluded.status,
  finished_at = excluded.finished_at;

INSERT INTO run_snapshots (run_id, snapshot_json, created_at)
VALUES (
  'sample-run-1',
  '{"operatorIds":["kyle","reina","eve","sera","mira"],"ultimateOperatorId":"kyle","operators":[{"operatorId":"kyle","effectiveLevel":32,"levelSource":"TRAINED","syncLevelAtStart":30,"trainedLevelAtStart":32,"skillTraining":3,"promotion":"1"},{"operatorId":"reina","effectiveLevel":31,"levelSource":"TRAINED","syncLevelAtStart":30,"trainedLevelAtStart":31,"skillTraining":3,"promotion":"0"},{"operatorId":"eve","effectiveLevel":30,"levelSource":"TRAINED","syncLevelAtStart":30,"trainedLevelAtStart":30,"skillTraining":2,"promotion":"0"},{"operatorId":"sera","effectiveLevel":30,"levelSource":"TRAINED","syncLevelAtStart":30,"trainedLevelAtStart":30,"skillTraining":2,"promotion":"0"},{"operatorId":"mira","effectiveLevel":33,"levelSource":"TRAINED","syncLevelAtStart":30,"trainedLevelAtStart":33,"skillTraining":2,"promotion":"0"}]}',
  1800003600
)
ON CONFLICT(run_id) DO UPDATE SET
  snapshot_json = excluded.snapshot_json,
  created_at = excluded.created_at;

INSERT INTO validator_versions (id, version, config_json, created_at)
VALUES ('sample-validator-version-1', 'validator-dev-1', '{"mode":"LOG_ONLY"}', 1800000000)
ON CONFLICT(id) DO UPDATE SET
  config_json = excluded.config_json;

INSERT INTO validator_runtime_configs (id, validator_version_id, runtime_revision, config_json, created_at)
VALUES ('sample-validator-runtime-1', 'sample-validator-version-1', 1, '{"mode":"LOG_ONLY"}', 1800000000)
ON CONFLICT(validator_version_id, runtime_revision) DO UPDATE SET
  config_json = excluded.config_json;

INSERT INTO validator_runtime_active (singleton_id, runtime_config_id, updated_at)
VALUES (1, 'sample-validator-runtime-1', 1800000000)
ON CONFLICT(singleton_id) DO UPDATE SET
  runtime_config_id = excluded.runtime_config_id,
  updated_at = excluded.updated_at;

INSERT INTO integrity_events (
  id,
  account_id,
  run_id,
  balance_config_version,
  run_start_build_id,
  submission_build_id,
  original_validator_version_id,
  runtime_revision,
  evaluated_validator_version_id,
  category,
  evidence_code,
  evidence_json,
  fingerprint,
  classification,
  created_at
) VALUES (
  'sample-integrity-event-1',
  'sample-account-1',
  'sample-run-1',
  1,
  'sample-build-1',
  'sample-build-1',
  'sample-validator-version-1',
  1,
  'sample-validator-version-1',
  'DAMAGE_ANALYSIS',
  'DAMAGE_SPIKE',
  '{"maxDamageRatio":2.4,"expectedBand":"RECOMMENDED_SPEC"}',
  'sample-integrity-event-1',
  'SOFT_ANOMALY',
  1787716800000
)
ON CONFLICT(fingerprint) DO UPDATE SET
  evidence_json = excluded.evidence_json,
  classification = excluded.classification,
  created_at = excluded.created_at;

INSERT INTO integrity_event_review_states (event_id, state, assigned_to, updated_at)
VALUES ('sample-integrity-event-1', 'OPEN', NULL, 1787716800000)
ON CONFLICT(event_id) DO UPDATE SET
  state = excluded.state,
  assigned_to = excluded.assigned_to,
  updated_at = excluded.updated_at;

INSERT INTO suspicious_player_reviews (
  account_id,
  status,
  assigned_to,
  reason,
  updated_by,
  updated_at,
  created_at
) VALUES (
  'sample-account-1',
  'REVIEWING',
  'dev-local-owner',
  'Sample account-level mark-only review.',
  'dev-local-owner',
  1787717100000,
  1787717000000
)
ON CONFLICT(account_id) DO UPDATE SET
  status = excluded.status,
  assigned_to = excluded.assigned_to,
  reason = excluded.reason,
  updated_by = excluded.updated_by,
  updated_at = excluded.updated_at;

INSERT INTO suspicious_player_review_history (
  id,
  account_id,
  from_status,
  to_status,
  reason,
  created_by,
  created_at,
  request_id,
  audit_log_id
) VALUES (
  'sample-suspicious-review-history-1',
  'sample-account-1',
  'NEW',
  'REVIEWING',
  'Sample account-level mark-only review.',
  'dev-local-owner',
  1787717100000,
  'sample-review-request-1',
  NULL
)
ON CONFLICT(id) DO UPDATE SET
  from_status = excluded.from_status,
  to_status = excluded.to_status,
  reason = excluded.reason,
  created_by = excluded.created_by,
  created_at = excluded.created_at,
  request_id = excluded.request_id,
  audit_log_id = excluded.audit_log_id;
