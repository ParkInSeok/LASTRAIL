import { createHash } from "node:crypto";
import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { publishCombatDefinitionContract, readCombatDefinitionContract } from "../../apps/game-api/src/combat-definition-contract";

const root = "D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server";
if (resolve(process.cwd()).toLowerCase() !== resolve(root).toLowerCase() || process.argv.length !== 2) {
  throw new Error("combat_definition_export_canonical_checkout_required");
}
const output = "D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-definition-intake-20261001.json";
const sourcePaths = ["apps/game-api/src/combat-definition-contract.ts", "apps/game-api/src/combat-policy-catalog.ts",
  "apps/game-api/src/combat-policy-binding.ts", "apps/game-api/src/combat-shield-budget.ts"];
const sources = await Promise.all(sourcePaths.map(async path => ({ path: `${root}/${path}`,
  sha256: createHash("sha256").update(await readFile(resolve(root, path))).digest("hex") })));
const contract = await publishCombatDefinitionContract();
await writeFile(output, JSON.stringify(contract, null, 2) + "\n", "utf8");
await readCombatDefinitionContract(JSON.parse(await readFile(output, "utf8")));
await writeFile(output.replace("intake", "provenance"), JSON.stringify({ output, contentHash: contract.contentHash,
  sources, environment: "LOCAL_DEFINITION_EXPORT", realStart: false, remoteMutation: false }, null, 2) + "\n", "utf8");
console.log(JSON.stringify({ output, schemaVersion: contract.schemaVersion, definitionVersion: contract.definitionVersion,
  contentHash: contract.contentHash, canExecute: contract.canExecute }));
