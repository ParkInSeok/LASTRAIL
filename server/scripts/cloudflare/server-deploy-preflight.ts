import { readFile } from "node:fs/promises";
import {
  buildServerDeployPreflight,
  type ServerDeployHistoryEntry,
  type ServerReleaseEnvironment,
  type ServerReleaseService,
} from "../../packages/common/src/index";

function parseArg(name: string): string | null {
  const index = process.argv.indexOf(`--${name}`);
  if (index < 0) return null;
  return process.argv[index + 1] ?? null;
}

function parseService(value: string | null): ServerReleaseService | null {
  return value === "game-api" || value === "admin-api" || value === "async-worker" ? value : null;
}

function parseEnvironment(value: string | null): ServerReleaseEnvironment | null {
  return value === "dev" || value === "prd" ? value : null;
}

async function readJson(path: string): Promise<unknown> {
  return JSON.parse(await readFile(path, "utf8")) as unknown;
}

async function main() {
  const planPath = parseArg("plan");
  const historyPath = parseArg("history");
  const service = parseService(parseArg("service"));
  const environment = parseEnvironment(parseArg("env"));

  if (planPath === null || service === null || environment === null) {
    console.error("Usage: tsx scripts/cloudflare/server-deploy-preflight.ts --plan <plan.json> --service <game-api|admin-api|async-worker> --env <dev|prd> [--history <history.json>]");
    process.exitCode = 2;
    return;
  }

  const plan = await readJson(planPath);
  const history = historyPath === null ? [] : await readJson(historyPath);
  const result = buildServerDeployPreflight(
    plan,
    Array.isArray(history) ? history as ServerDeployHistoryEntry[] : [],
    { service, environment },
  );

  if (!result.accepted) {
    console.error(JSON.stringify({ accepted: false, errorCode: result.errorCode }));
    process.exitCode = 1;
    return;
  }

  console.log(JSON.stringify({
    accepted: true,
    environment: result.plan.environment,
    service,
    serverVersion: result.plan.serverVersion,
    releaseId: result.plan.releaseId,
    remoteSmoke: result.plan.verification.remoteSmoke,
  }));
}

await main();
