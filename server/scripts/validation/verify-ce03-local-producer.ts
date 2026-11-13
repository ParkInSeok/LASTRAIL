import { mkdir, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { Buffer } from "node:buffer";
import { ce03ProducerFixture } from "../../tests/helpers/ce03-producer-fixture";
import { finalizeLocalCombatArtifacts } from "../../apps/game-api/src/ce03-local-producer";

// No remote binding/credentials/files accepted. Uses only a new in-memory DB.
const fixture = await ce03ProducerFixture(), before = fixture.sql.export();
const response = await fixture.request(), result = await response.json();
if (response.status !== 200 || result.ready !== false || result.status !== "MISSING_EXECUTION_EVIDENCE") throw new Error("Producer contract mismatch");
if (!Buffer.from(before).equals(Buffer.from(fixture.sql.export()))) throw new Error("Producer mutated disposable DB");
let missingCode: string | null = null;
try { await finalizeLocalCombatArtifacts(result.draft, undefined); }
catch (error) { missingCode = (error as { code?: string }).code ?? null; }
if (missingCode !== "ce03_execution_evidence_missing") throw new Error("Missing execution evidence did not fail closed");
const output = resolve("audit-results/ce03-local-producer");
await mkdir(output, { recursive: true });
await writeFile(resolve(output, "draft.json"), JSON.stringify(result, null, 2) + "\n");
await writeFile(resolve(output, "verification.json"), JSON.stringify({ scope: "DISPOSABLE_IN_MEMORY_DB", authenticated: true,
  explicitUltimate: result.draft.template.selectedUltimateOperatorId, operators: result.draft.template.operators.length,
  waves: result.draft.template.waves.length, typedBindings: result.draft.bindings.length,
  dbUnchanged: true, missingCode, ready: false, frozen: false, seedIssued: false, runCreated: false }, null, 2) + "\n");
fixture.sql.close();
console.log(JSON.stringify({ output, missingCode, ready: false, frozen: false }));
