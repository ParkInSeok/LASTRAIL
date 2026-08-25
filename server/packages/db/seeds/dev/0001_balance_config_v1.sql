INSERT OR IGNORE INTO balance_config_versions (
  version,
  schema_version,
  required_content_version,
  checksum,
  config_json,
  published_at,
  published_by,
  draft_id
) VALUES (
  1,
  1,
  1,
  'dev-seed-balance-config-v1',
  '{"fuel":{"max":100,"runStartCost":10,"rechargeSeconds":300},"train":{"baseSpeed":10}}',
  1800000000,
  'dev-seed',
  NULL
);

INSERT OR IGNORE INTO balance_config_activations (
  id,
  version,
  activation_type,
  activated_by,
  activated_at,
  reason
) VALUES (
  'dev-seed-balance-config-v1-activation',
  1,
  'PUBLISH',
  'dev-seed',
  1800000000,
  'Initial DEV balance config seed'
);

INSERT INTO balance_config_active (
  singleton_id,
  version,
  activation_id,
  updated_at
) VALUES (
  1,
  1,
  'dev-seed-balance-config-v1-activation',
  1800000000
)
ON CONFLICT(singleton_id) DO UPDATE SET
  version = excluded.version,
  activation_id = excluded.activation_id,
  updated_at = excluded.updated_at;
