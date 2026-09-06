import { spawn } from "node:child_process";
import { readFile } from "node:fs/promises";
import { pathToFileURL } from "node:url";
import {
  buildServerDeployPreflight,
  type ServerDeployHistoryEntry,
  type ServerReleaseEnvironment,
  type ServerReleaseService,
} from "../../packages/common/src/index";

type ServerDeployCliIo = {
  readText(path: string): Promise<string>;
  stdout(line: string): void;
  stderr(line: string): void;
};

type ServerDeployCommandResult = {
  exitCode: number;
  deploymentId?: string;
};

type ServerDeployCommandExecutor = (command: string[]) => Promise<ServerDeployCommandResult>;

function parseArg(argv: string[], name: string): string | null {
  const index = argv.indexOf(`--${name}`);
  if (index < 0) return null;
  return argv[index + 1] ?? null;
}

function parseService(value: string | null): ServerReleaseService | null {
  return value === "game-api" || value === "admin-api" || value === "async-worker" ? value : null;
}

function parseEnvironment(value: string | null): ServerReleaseEnvironment | null {
  return value === "dev" || value === "prd" ? value : null;
}

function packageNameForService(service: ServerReleaseService): string {
  return `@lastrail/${service}`;
}

function buildWranglerDryRunCommand(service: ServerReleaseService, environment: ServerReleaseEnvironment): string[] {
  return ["pnpm", "--filter", packageNameForService(service), "wrangler", "deploy", "--env", environment, "--dry-run"];
}

async function defaultExecutor(command: string[]): Promise<ServerDeployCommandResult> {
  const executable = process.platform === "win32" && command[0] === "pnpm" ? "pnpm.cmd" : command[0];
  const args = command.slice(1);
  const exitCode = await new Promise<number>((resolve) => {
    const child = spawn(executable, args, { stdio: "inherit", shell: false });
    child.on("error", () => resolve(1));
    child.on("close", (code) => resolve(code ?? 1));
  });
  return { exitCode };
}

export async function runServerDeployCli(
  argv: string[],
  io: ServerDeployCliIo = {
    readText: (path) => readFile(path, "utf8"),
    stdout: (line) => console.log(line),
    stderr: (line) => console.error(line),
  },
  execute: ServerDeployCommandExecutor = defaultExecutor,
): Promise<number> {
  const mode = parseArg(argv, "mode") ?? "dry-run";
  if (mode !== "dry-run") {
    io.stderr("Only --mode dry-run is supported by this launch-safe wrapper. Actual deploy requires an explicitly approved release operation.");
    return 2;
  }

  const planPath = parseArg(argv, "plan");
  const historyPath = parseArg(argv, "history");
  const service = parseService(parseArg(argv, "service"));
  const environment = parseEnvironment(parseArg(argv, "env"));
  if (planPath === null || service === null || environment === null) {
    io.stderr("Usage: tsx scripts/cloudflare/server-deploy.ts --plan <plan.json> --service <game-api|admin-api|async-worker> --env <dev|prd> --mode dry-run [--history <history.json>]");
    return 2;
  }

  const plan = JSON.parse(await io.readText(planPath)) as unknown;
  const history = historyPath === null ? [] : JSON.parse(await io.readText(historyPath)) as unknown;
  const preflight = buildServerDeployPreflight(
    plan,
    Array.isArray(history) ? history as ServerDeployHistoryEntry[] : [],
    { service, environment },
  );
  if (!preflight.accepted) {
    io.stderr(JSON.stringify({ accepted: false, errorCode: preflight.errorCode }));
    return 1;
  }

  const command = buildWranglerDryRunCommand(service, environment);
  const result = await execute(command);
  io.stdout(JSON.stringify({
    accepted: result.exitCode === 0,
    environment,
    service,
    serverVersion: preflight.plan.serverVersion,
    mode,
    command,
    exitCode: result.exitCode,
    ...(result.deploymentId === undefined ? {} : { deploymentId: result.deploymentId }),
  }));
  return result.exitCode;
}

const invokedPath = process.argv[1] === undefined ? null : pathToFileURL(process.argv[1]).href;
if (import.meta.url === invokedPath) {
  process.exitCode = await runServerDeployCli(process.argv);
}
