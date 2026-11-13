import { describe, expect, it } from "vitest";
import { runServerDeployPreflightCli } from "../../scripts/cloudflare/server-deploy-preflight";

const plan = {
  releaseId: "act1-launch-2026-09-06",
  environment: "prd",
  services: ["game-api"],
  serverVersion: "2026.09.06.1",
  source: {
    commitSha: "30b2933abcdef1234567890abcdef123456789",
    releaseBranch: "release/2026-09-06-2026.09.06.1",
    workingTreeClean: true,
  },
  deploymentTags: [{ service: "game-api", tag: "game-api-2026.09.06.1" }],
  database: { compatibility: "ADDITIVE_COMPATIBLE", migrationRange: "0001-0020" },
  verification: { localRegression: "PASSED", dryRun: "PASSED", remoteSmoke: "PENDING" },
};

function createIo(files: Record<string, unknown>) {
  const stdout: string[] = [];
  const stderr: string[] = [];
  return {
    stdout,
    stderr,
    io: {
      readText: async (path: string) => JSON.stringify(files[path]),
      stdout: (line: string) => stdout.push(line),
      stderr: (line: string) => stderr.push(line),
    },
  };
}

describe("server deploy preflight CLI", () => {
  it("prints a sanitized accepted result for a valid plan", async () => {
    const { io, stdout, stderr } = createIo({ "plan.json": plan });
    const code = await runServerDeployPreflightCli(
      ["node", "server-deploy-preflight.ts", "--plan", "plan.json", "--service", "game-api", "--env", "prd"],
      io,
    );

    expect(code).toBe(0);
    expect(stderr).toEqual([]);
    expect(JSON.parse(stdout[0])).toEqual({
      accepted: true,
      environment: "prd",
      service: "game-api",
      serverVersion: "2026.09.06.1",
      releaseId: "act1-launch-2026-09-06",
      remoteSmoke: "PENDING",
    });
  });

  it("returns stable failures for invalid plans, target mismatch, and version reuse", async () => {
    const targetMismatch = createIo({ "plan.json": plan });
    await expect(runServerDeployPreflightCli(
      ["node", "server-deploy-preflight.ts", "--plan", "plan.json", "--service", "game-api", "--env", "dev"],
      targetMismatch.io,
    )).resolves.toBe(1);
    expect(JSON.parse(targetMismatch.stderr[0])).toEqual({ accepted: false, errorCode: "deployment_target_mismatch" });

    const invalid = createIo({ "plan.json": { ...plan, serverVersion: "bad" } });
    await expect(runServerDeployPreflightCli(
      ["node", "server-deploy-preflight.ts", "--plan", "plan.json", "--service", "game-api", "--env", "prd"],
      invalid.io,
    )).resolves.toBe(1);
    expect(JSON.parse(invalid.stderr[0])).toEqual({ accepted: false, errorCode: "server_version_invalid" });

    const reused = createIo({
      "plan.json": plan,
      "history.json": [{ environment: "prd", service: "game-api", serverVersion: "2026.09.06.1" }],
    });
    await expect(runServerDeployPreflightCli(
      ["node", "server-deploy-preflight.ts", "--plan", "plan.json", "--service", "game-api", "--env", "prd", "--history", "history.json"],
      reused.io,
    )).resolves.toBe(1);
    expect(JSON.parse(reused.stderr[0])).toEqual({ accepted: false, errorCode: "server_version_reused" });
  });
});
