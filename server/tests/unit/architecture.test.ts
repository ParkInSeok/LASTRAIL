import { describe, expect, it } from "vitest";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";
import { parse } from "jsonc-parser";

const root = process.cwd();

function filesUnder(dir: string): string[] {
  if (!existsSync(dir)) return [];
  return readdirSync(dir).flatMap((entry) => {
    const full = join(dir, entry);
    return statSync(full).isDirectory() ? filesUnder(full) : [full];
  });
}

describe("repository architecture", () => {
  it("packages do not import apps", () => {
    const packageFiles = filesUnder(join(root, "packages")).filter((file) => file.endsWith(".ts"));
    const offenders = packageFiles.filter((file) => readFileSync(file, "utf8").includes("../apps") || readFileSync(file, "utf8").includes("apps/"));
    expect(offenders).toEqual([]);
  });

  it("game-api and admin-api declare dev and prd custom domains only", () => {
    const game = parse(readFileSync(join(root, "apps/game-api/wrangler.jsonc"), "utf8"));
    const admin = parse(readFileSync(join(root, "apps/admin-api/wrangler.jsonc"), "utf8"));
    expect(Object.keys(game.env).sort()).toEqual(["dev", "prd"]);
    expect(Object.keys(admin.env).sort()).toEqual(["dev", "prd"]);
    expect(game.env.dev.routes).toEqual([{ pattern: "dev-api.lastrail.app", custom_domain: true }]);
    expect(game.env.prd.routes).toEqual([{ pattern: "api.lastrail.app", custom_domain: true }]);
    expect(game.env.prd.vars.ENVIRONMENT).toBe("PRD");
    expect(admin.env.dev.routes).toEqual([{ pattern: "dev-admin.lastrail.app", custom_domain: true }]);
    expect(admin.env.prd.routes).toEqual([{ pattern: "admin.lastrail.app", custom_domain: true }]);
    expect(admin.env.prd.vars.ENVIRONMENT).toBe("PRD");
  });

  it("async-worker has no public routes", () => {
    const asyncWorker = parse(readFileSync(join(root, "apps/async-worker/wrangler.jsonc"), "utf8"));
    expect(Object.keys(asyncWorker.env).sort()).toEqual(["dev", "prd"]);
    expect(asyncWorker.routes).toBeUndefined();
    expect(asyncWorker.env.dev.routes).toBeUndefined();
    expect(asyncWorker.env.prd.routes).toBeUndefined();
    expect(asyncWorker.env.prd.vars.ENVIRONMENT).toBe("PRD");
  });

  it("documents Access protection for admin domains without protecting Game API", () => {
    const adminAccess = readFileSync(join(root, "docs/cloudflare/admin-access.md"), "utf8");
    const setup = readFileSync(join(root, "docs/cloudflare/setup.md"), "utf8");
    const packageJson = JSON.parse(readFileSync(join(root, "package.json"), "utf8")) as { scripts: Record<string, string> };

    expect(adminAccess).toContain("dev-admin.lastrail.app");
    expect(adminAccess).toContain("LAST RAIL Admin DEV");
    expect(adminAccess).toContain("pnpm access:dev:apply-vars");
    expect(adminAccess).toContain("GET /v1/admin/bootstrap/access-subject");
    expect(adminAccess).toContain("Confirm `https://dev-api.lastrail.app/status` remains public");
    expect(adminAccess).toContain("`api.lastrail.app` is public Game API traffic and must not be protected by Cloudflare Access.");
    expect(setup).toContain("Keep `dev-api.lastrail.app` public.");
    expect(packageJson.scripts["access:dev:apply-vars"]).toBe("tsx scripts/cloudflare/apply-dev-admin-access-vars.ts");
    expect(packageJson.scripts["admin:dev:upsert-principal"]).toBe("tsx scripts/cloudflare/upsert-dev-admin-principal.ts");
    expect(existsSync(join(root, "scripts/cloudflare/apply-dev-admin-access-vars.ts"))).toBe(true);
    expect(existsSync(join(root, "scripts/cloudflare/upsert-dev-admin-principal.ts"))).toBe(true);
  });

  it("targets the DEV D1 binding when seeding DEV balance config", () => {
    const packageJson = JSON.parse(readFileSync(join(root, "package.json"), "utf8")) as { scripts: Record<string, string> };

    expect(packageJson.scripts["d1:dev:seed:balance"]).toContain("wrangler d1 execute DB");
    expect(packageJson.scripts["d1:dev:seed:balance"]).toContain("--env dev");
    expect(packageJson.scripts["d1:dev:seed:balance"]).toContain("--remote");
    expect(packageJson.scripts["d1:dev:seed:balance"]).toContain("apps/game-api/wrangler.jsonc");
  });
});
