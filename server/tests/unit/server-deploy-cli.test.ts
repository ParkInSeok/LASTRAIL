import { describe, expect, it } from "vitest";
import { runServerDeployCli } from "../../scripts/cloudflare/server-deploy";

const validPlan = {
  releaseId: "act1-launch-2026-09-06",
  environment: "dev",
  services: ["game-api"],
  serverVersion: "2026.09.06.1",
  source: {
    commitSha: "adbf255abcdef1234567890abcdef123456789",
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

describe("server deploy CLI", () => {
  it("runs the supported Wrangler dry-run command only after mandatory preflight accepts", async () => {
    const { io, stdout, stderr } = createIo({ "plan.json": validPlan });
    const executed: string[][] = [];

    const code = await runServerDeployCli(
      ["node", "server-deploy.ts", "--plan", "plan.json", "--service", "game-api", "--env", "dev", "--mode", "dry-run"],
      io,
      async (command) => {
        executed.push(command);
        return { exitCode: 0, deploymentId: "dry-run-no-remote-deployment" };
      },
    );

    expect(code).toBe(0);
    expect(stderr).toEqual([]);
    expect(executed).toEqual([["pnpm", "--filter", "@lastrail/game-api", "wrangler", "deploy", "--env", "dev", "--dry-run"]]);
    expect(JSON.parse(stdout[0])).toEqual({
      accepted: true,
      environment: "dev",
      service: "game-api",
      serverVersion: "2026.09.06.1",
      mode: "dry-run",
      command: ["pnpm", "--filter", "@lastrail/game-api", "wrangler", "deploy", "--env", "dev", "--dry-run"],
      exitCode: 0,
      deploymentId: "dry-run-no-remote-deployment",
    });
  });

  it("does not call Wrangler when the preflight plan is invalid, reused, or targets another environment", async () => {
    const executed: string[][] = [];
    const exec = async (command: string[]) => {
      executed.push(command);
      return { exitCode: 0 };
    };

    const invalid = createIo({ "plan.json": { ...validPlan, serverVersion: "bad" } });
    await expect(runServerDeployCli(
      ["node", "server-deploy.ts", "--plan", "plan.json", "--service", "game-api", "--env", "dev", "--mode", "dry-run"],
      invalid.io,
      exec,
    )).resolves.toBe(1);
    expect(JSON.parse(invalid.stderr[0])).toEqual({ accepted: false, errorCode: "server_version_invalid" });

    const targetMismatch = createIo({ "plan.json": validPlan });
    await expect(runServerDeployCli(
      ["node", "server-deploy.ts", "--plan", "plan.json", "--service", "game-api", "--env", "prd", "--mode", "dry-run"],
      targetMismatch.io,
      exec,
    )).resolves.toBe(1);
    expect(JSON.parse(targetMismatch.stderr[0])).toEqual({ accepted: false, errorCode: "deployment_target_mismatch" });

    const reused = createIo({
      "plan.json": validPlan,
      "history.json": [{ environment: "dev", service: "game-api", serverVersion: "2026.09.06.1" }],
    });
    await expect(runServerDeployCli(
      ["node", "server-deploy.ts", "--plan", "plan.json", "--service", "game-api", "--env", "dev", "--mode", "dry-run", "--history", "history.json"],
      reused.io,
      exec,
    )).resolves.toBe(1);
    expect(JSON.parse(reused.stderr[0])).toEqual({ accepted: false, errorCode: "server_version_reused" });
    expect(executed).toEqual([]);
  });

  it("rejects actual deploy mode until an approved release operation wires it explicitly", async () => {
    const { io, stderr } = createIo({ "plan.json": validPlan });
    const code = await runServerDeployCli(
      ["node", "server-deploy.ts", "--plan", "plan.json", "--service", "game-api", "--env", "dev", "--mode", "deploy"],
      io,
      async () => ({ exitCode: 0 }),
    );

    expect(code).toBe(2);
    expect(stderr[0]).toContain("Only --mode dry-run is supported");
  });
});
