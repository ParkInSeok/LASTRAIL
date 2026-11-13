import { readFile } from "node:fs/promises";
import { pathToFileURL } from "node:url";
import {
  buildServerDeployPreflight,
  type ServerDeployHistoryEntry,
  type ServerReleaseEnvironment,
  type ServerReleaseService,
} from "../../packages/common/src/index";

type ServerDeployPreflightCliIo = {
  readText(path: string): Promise<string>;
  stdout(line: string): void;
  stderr(line: string): void;
};

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

export async function runServerDeployPreflightCli(
  argv: string[],
  io: ServerDeployPreflightCliIo = {
    readText: (path) => readFile(path, "utf8"),
    stdout: (line) => console.log(line),
    stderr: (line) => console.error(line),
  },
): Promise<number> {
  const planPath = parseArg(argv, "plan");
  const historyPath = parseArg(argv, "history");
  const service = parseService(parseArg(argv, "service"));
  const environment = parseEnvironment(parseArg(argv, "env"));

  if (planPath === null || service === null || environment === null) {
    io.stderr("Usage: tsx scripts/cloudflare/server-deploy-preflight.ts --plan <plan.json> --service <game-api|admin-api|async-worker> --env <dev|prd> [--history <history.json>]");
    return 2;
  }

  const plan = JSON.parse(await io.readText(planPath)) as unknown;
  const history = historyPath === null ? [] : JSON.parse(await io.readText(historyPath)) as unknown;
  const result = buildServerDeployPreflight(
    plan,
    Array.isArray(history) ? history as ServerDeployHistoryEntry[] : [],
    { service, environment },
  );

  if (!result.accepted) {
    io.stderr(JSON.stringify({ accepted: false, errorCode: result.errorCode }));
    return 1;
  }

  io.stdout(JSON.stringify({
    accepted: true,
    environment: result.plan.environment,
    service,
    serverVersion: result.plan.serverVersion,
    releaseId: result.plan.releaseId,
    remoteSmoke: result.plan.verification.remoteSmoke,
  }));
  return 0;
}

const invokedPath = process.argv[1] === undefined ? null : pathToFileURL(process.argv[1]).href;
if (import.meta.url === invokedPath) {
  process.exitCode = await runServerDeployPreflightCli(process.argv);
}
