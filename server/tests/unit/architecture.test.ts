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

  it("game-api and admin-api declare separate custom domains for each environment", () => {
    const game = parse(readFileSync(join(root, "apps/game-api/wrangler.jsonc"), "utf8"));
    const admin = parse(readFileSync(join(root, "apps/admin-api/wrangler.jsonc"), "utf8"));
    expect(game.env.dev.routes).toEqual([{ pattern: "dev-api.lastrail.app", custom_domain: true }]);
    expect(game.env.staging.routes).toEqual([{ pattern: "staging-api.lastrail.app", custom_domain: true }]);
    expect(game.env.production.routes).toEqual([{ pattern: "api.lastrail.app", custom_domain: true }]);
    expect(admin.env.dev.routes).toEqual([{ pattern: "dev-admin.lastrail.app", custom_domain: true }]);
    expect(admin.env.staging.routes).toEqual([{ pattern: "staging-admin.lastrail.app", custom_domain: true }]);
    expect(admin.env.production.routes).toEqual([{ pattern: "admin.lastrail.app", custom_domain: true }]);
  });

  it("async-worker has no public routes", () => {
    const asyncWorker = parse(readFileSync(join(root, "apps/async-worker/wrangler.jsonc"), "utf8"));
    expect(asyncWorker.routes).toBeUndefined();
    expect(asyncWorker.env.dev.routes).toBeUndefined();
    expect(asyncWorker.env.staging.routes).toBeUndefined();
    expect(asyncWorker.env.production.routes).toBeUndefined();
  });
});
