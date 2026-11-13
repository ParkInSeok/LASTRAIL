INSERT INTO admin_principals (
  id,
  access_subject,
  role,
  status,
  display_name,
  created_at,
  updated_at
) VALUES (
  'dev-local-owner',
  'access|local-owner',
  'OWNER',
  'ACTIVE',
  'Local Owner',
  1800000000,
  1800000000
)
ON CONFLICT(id) DO UPDATE SET
  access_subject = excluded.access_subject,
  role = excluded.role,
  status = excluded.status,
  display_name = excluded.display_name,
  updated_at = excluded.updated_at;

