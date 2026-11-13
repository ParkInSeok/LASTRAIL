import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { applyEdits, modify } from "jsonc-parser";

const root = process.cwd();
const wranglerPath = join(root, "apps/admin-api/wrangler.jsonc");

function requiredEnv(name: string): string {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`${name} is required`);
  }
  return value;
}

function normalizeIssuer(value: string): string {
  const issuer = value.endsWith("/") ? value.slice(0, -1) : value;
  if (!issuer.startsWith("https://") || !issuer.endsWith(".cloudflareaccess.com")) {
    throw new Error("DEV_ADMIN_ACCESS_ISSUER must look like https://<team-name>.cloudflareaccess.com");
  }
  return issuer;
}

function validateAudience(value: string): string {
  if (value.length > 64 || !/^[a-zA-Z0-9_-]+$/u.test(value)) {
    throw new Error("DEV_ADMIN_ACCESS_AUD must be the Cloudflare Access application AUD tag");
  }
  return value;
}

const issuer = normalizeIssuer(requiredEnv("DEV_ADMIN_ACCESS_ISSUER"));
const audience = validateAudience(requiredEnv("DEV_ADMIN_ACCESS_AUD"));
const source = readFileSync(wranglerPath, "utf8");
const withIssuer = applyEdits(
  source,
  modify(source, ["env", "dev", "vars", "ADMIN_ACCESS_ISSUER"], issuer, { formattingOptions: { insertSpaces: true, tabSize: 2 } }),
);
const withAudience = applyEdits(
  withIssuer,
  modify(withIssuer, ["env", "dev", "vars", "ADMIN_ACCESS_AUD"], audience, { formattingOptions: { insertSpaces: true, tabSize: 2 } }),
);

writeFileSync(wranglerPath, withAudience);
console.log("Updated DEV ADMIN_ACCESS_ISSUER and ADMIN_ACCESS_AUD in apps/admin-api/wrangler.jsonc");
