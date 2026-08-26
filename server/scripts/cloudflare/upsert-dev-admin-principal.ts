import { spawnSync } from "node:child_process";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const allowedRoles = new Set(["OWNER", "OPERATOR", "VIEWER"]);

function requiredEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) throw new Error(`${name} is required`);
  return value;
}

function optionalEnv(name: string, fallback: string): string {
  const value = process.env[name]?.trim();
  return value && value.length > 0 ? value : fallback;
}

function validateId(id: string): string {
  if (!/^[a-zA-Z0-9_-]{3,80}$/u.test(id)) {
    throw new Error("DEV_ADMIN_PRINCIPAL_ID must be 3-80 chars of letters, numbers, underscore, or hyphen");
  }
  return id;
}

function validateRole(role: string): string {
  if (!allowedRoles.has(role)) throw new Error("DEV_ADMIN_ROLE must be OWNER, OPERATOR, or VIEWER");
  return role;
}

function sqlString(value: string): string {
  return `'${value.replaceAll("'", "''")}'`;
}

const id = validateId(optionalEnv("DEV_ADMIN_PRINCIPAL_ID", "dev-owner"));
const accessSubject = requiredEnv("DEV_ADMIN_ACCESS_SUB");
const role = validateRole(optionalEnv("DEV_ADMIN_ROLE", "OWNER"));
const displayName = optionalEnv("DEV_ADMIN_DISPLAY_NAME", "DEV Owner");
const now = Math.floor(Date.now() / 1000);

const sql = `
INSERT INTO admin_principals (
  id,
  access_subject,
  role,
  status,
  display_name,
  created_at,
  updated_at
) VALUES (
  ${sqlString(id)},
  ${sqlString(accessSubject)},
  ${sqlString(role)},
  'ACTIVE',
  ${sqlString(displayName)},
  ${now},
  ${now}
)
ON CONFLICT(id) DO UPDATE SET
  access_subject = excluded.access_subject,
  role = excluded.role,
  status = excluded.status,
  display_name = excluded.display_name,
  updated_at = excluded.updated_at;
`;

const pnpmCommand = process.platform === "win32" ? "pnpm.cmd" : "pnpm";
const sqlDir = mkdtempSync(join(tmpdir(), "lastrail-dev-admin-"));
const sqlFile = join(sqlDir, "upsert-admin-principal.sql");

try {
  writeFileSync(sqlFile, sql, "utf8");

  const result = spawnSync(
    pnpmCommand,
    [
      "exec",
      "wrangler",
      "d1",
      "execute",
      "lastrail-core-dev",
      "--remote",
      "--file",
      sqlFile,
      "--config",
      "apps/admin-api/wrangler.jsonc",
    ],
    { stdio: "inherit", shell: process.platform === "win32" },
  );

  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);

  console.log(`Upserted DEV admin principal ${id} for Access subject ${accessSubject}`);
} finally {
  rmSync(sqlDir, { recursive: true, force: true });
}
