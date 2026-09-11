import { readFile } from "node:fs/promises";
import { describe, expect, it } from "vitest";
import { projectPresentationAudit, verifyPresentationFiles } from "../../scripts/local/ce03-presentation-projection";

const auditPath = new URL("../fixtures/ce03-presentation-closure-audit.json", import.meta.url);
async function fixture() { return { audit: JSON.parse(await readFile(auditPath, "utf8")), asset: await readFile(new URL("../fixtures/ce03-approved.asset.txt", import.meta.url), "utf8") }; }
describe("actual exported presentation projection (no Unity execution claim)", () => {
  it("projects all typed rows with referenced roles and subasset/dependency closure", async () => {
    const f = await fixture(), manifest = await projectPresentationAudit(f.audit, f.asset);
    expect(manifest.bindings).toHaveLength(356);
    expect(manifest.roles.actorAtlases).toHaveLength(6); expect(manifest.roles.frames).toHaveLength(160);
    expect(manifest.bindings.some(b => b.locator.localFileId.startsWith("-"))).toBe(true);
    expect(manifest.sourceFiles.map(s => s.logicalName)).toContain("combatHudWidgets");
    expect(manifest.roles.frame).toBe(manifest.roles.secondary);
  });
  it.each(["duplicate", "missing", "type", "localId", "roleSwap", "operatorIndex", "source", "dep", "unresolved"])("rejects %s export corruption", async mode => {
    const f = await fixture();
    if (mode === "duplicate") f.audit.rows.push(f.audit.rows[0]);
    if (mode === "missing") f.audit.rows.splice(0, 1);
    if (mode === "type") f.audit.roles[1].type = "UnityEngine.Texture2D";
    if (mode === "localId") f.audit.rows[0].localFileId = "9223372036854775808";
    if (mode === "roleSwap") { const a = f.audit.roles[2], b = f.audit.roles[3]; f.audit.roles[2] = { ...b, role: a.role }; f.audit.roles[3] = { ...a, role: b.role }; }
    if (mode === "operatorIndex") f.audit.operatorIds.reverse();
    if (mode === "source") f.audit.sourceFiles.pop();
    if (mode === "dep") f.audit.rows[0].directDependencies.push("Assets/missing.png");
    if (mode === "unresolved") f.audit.unresolved.push("missing");
    await expect(projectPresentationAudit(f.audit, f.asset)).rejects.toThrow();
  });
  it("rejects disk hash mismatch and path escape using actual exported expectations", async () => {
    const f = await fixture();
    await expect(verifyPresentationFiles(f.audit, async () => new Uint8Array([1]))).rejects.toThrow();
    f.audit.rows[0].path = "../outside";
    await expect(projectPresentationAudit(f.audit, f.asset)).rejects.toThrow();
  });
});
