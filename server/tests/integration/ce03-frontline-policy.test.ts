import { describe, expect, it } from "vitest";
import { Buffer } from "node:buffer";
import { Hono } from "hono";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ce03ProducerFixture, ce03Inputs } from "../helpers/ce03-producer-fixture";
import { body } from "../helpers/departure-fixture";
import { prepareLocalCombatDraft, registerLocalCombatProducer } from "../../apps/game-api/src/ce03-local-producer";
import { templateSchema, policySchema, profileSchema } from "../../apps/game-api/src/ce03-local-schema";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { localSupportCatalog, localLeaderChargePolicy } from "../../apps/game-api/src/ce03-command-contract";
import { resolveAuthContext } from "../../apps/game-api/src/index";

const targetingPolicyId = "ce03.frontline-first-v1" as const;
const version = "ce03.execution.2026-09-13.001";
const hash = "08cd3350fde47c5d819836bf6c6be981467e01ea5f814ee9cd56a42e17d0f052";

describe("explicit frontline-first producer selection", () => {
  it("preserves default legacy bytes and changes only the explicit targeting contract", async () => {
    const f = await ce03ProducerFixture();
    const { draft: legacy, ownedSelection } = await (await f.request()).json();
    expect(await sha256Hex(canonicalJson(legacy))).toBe("c5cbc882a853a1a1e157d52e4497204fb8ce142a5f057f81f68b5900ee71a544");
    expect(legacy.template.execution).not.toHaveProperty("targetingPolicyId");
    expect(await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, { targetingPolicyId: undefined })).toStrictEqual(legacy);
    const next = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, { targetingPolicyId });
    expect(next.simulationVersion).toBe(version);
    expect(next.template.execution).toMatchObject({ targetingPolicyId, policyVersion: version, policySha256: hash, frontSlots: [3, 4], rearSlots: [0, 1, 2] });
    expect(next.executionPolicy).toEqual({ ...legacy.executionPolicy, version, targetTiePolicy: targetingPolicyId });
    expect(await sha256Hex(canonicalJson(next.executionPolicy))).toBe(hash);
    const restoredShape = structuredClone(next);
    Reflect.deleteProperty(restoredShape.template.execution, "targetingPolicyId");
    restoredShape.template.execution.policyVersion = legacy.template.execution.policyVersion;
    restoredShape.template.execution.policySha256 = legacy.template.execution.policySha256;
    restoredShape.executionPolicy = legacy.executionPolicy;
    restoredShape.simulationVersion = legacy.simulationVersion;
    restoredShape.sourceFiles = legacy.sourceFiles;
    expect(restoredShape).toEqual(legacy);
    expect(next.sourceFiles.filter(s => !legacy.sourceFiles.some((o: { logicalName: string }) => o.logicalName === s.logicalName)))
      .toEqual([{ logicalName: "frontlineFirstPolicyV1", rawSha256: expect.stringMatching(/^[a-f0-9]{64}$/u) }]);
    const options = { targetingPolicyId: "unknown" };
    await expect(prepareLocalCombatDraft(ce03Inputs(), ownedSelection, options as never)).rejects.toThrow();
    f.sql.close();
  });

  it("rejects mixed new identity, missing opt-in, unsupported IDs and modified policy stages", async () => {
    const f = await ce03ProducerFixture(), { draft, ownedSelection } = await (await f.request()).json();
    const next = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, { targetingPolicyId });
    expect(templateSchema.safeParse(next.template).success).toBe(true);
    for (const execution of [
      { ...draft.template.execution, targetingPolicyId },
      { ...next.template.execution, targetingPolicyId: "unknown" },
      { ...next.template.execution, policySha256: "0".repeat(64) },
      { ...next.template.execution, policyVersion: draft.simulationVersion },
    ]) expect(templateSchema.safeParse({ ...next.template, execution }).success).toBe(false);
    const missing = structuredClone(next.template);
    Reflect.deleteProperty(missing.execution, "targetingPolicyId");
    expect(templateSchema.safeParse(missing).success).toBe(false);
    expect(policySchema.safeParse({ ...next.executionPolicy, interruptPolicy: "NEW" }).success).toBe(false);
    expect(policySchema.safeParse({ ...next.executionPolicy, version: draft.simulationVersion }).success).toBe(false);
    f.sql.close();
  });

  it("rejects cross-object version mixing in both legacy and new profiles", async () => {
    const f = await ce03ProducerFixture(), { draft: legacy, ownedSelection } = await (await f.request()).json();
    const next = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, { targetingPolicyId });
    const profile = (draft: typeof next) => {
      const data = structuredClone(draft); Reflect.deleteProperty(data, "bindings");
      return { schemaVersion: "ce03-isolated-profile-r4", ...data,
        executionEvidence: { algorithmSourceSha256: "1".repeat(64), conformanceVectorsSha256: "2".repeat(64) } };
    };
    const oldProfile = profile(legacy), newProfile = profile(next);
    expect(profileSchema.safeParse(oldProfile).success).toBe(true);
    expect(profileSchema.safeParse(newProfile).success).toBe(true);
    for (const mixed of [
      { ...newProfile, simulationVersion: legacy.simulationVersion },
      { ...oldProfile, simulationVersion: next.simulationVersion },
      { ...newProfile, executionPolicy: legacy.executionPolicy },
      { ...oldProfile, executionPolicy: next.executionPolicy },
    ]) expect(profileSchema.safeParse(mixed).success).toBe(false);
    f.sql.close();
  });

  it("pins the host option for normal prepared commands without writing DB or accepting client policy switches", async () => {
    const f = await ce03ProducerFixture(); f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
    const options = { disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
      inputs: ce03Inputs(), supportCatalog: localSupportCatalog, leaderPolicy: localLeaderChargePolicy, targetingPolicyId };
    const host = createCe03LocalHost(options);
    const post = (path: string, data: unknown) => host.request("http://127.0.0.1" + path, { method: "POST", headers: { ...f.headers, "Idempotency-Key": "frontline-support-save" }, body: JSON.stringify(data) }, f.env);
    expect((await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
      expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) })).status).toBe(200);
    Reflect.set(options, "targetingPolicyId", "unknown");
    const before = f.sql.export();
    const response = await post("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1 });
    expect(response.status).toBe(200);
    const { ready, status, prepareSha256, ...prepared } = await response.json();
    expect(ready).toBe(false); expect(status).toBe("MISSING_EXECUTION_EVIDENCE");
    expect(prepared.draft.template.execution.targetingPolicyId).toBe(targetingPolicyId);
    expect(prepared.commandContract.version).toBe("ce03-command-contract-v1");
    expect(prepareSha256).toBe(await sha256Hex(canonicalJson(prepared)));
    expect((await post("/__local/ce03/prepare", { selection: body, expectedSupportRevision: 1, targetingPolicyId })).status).toBe(400);
    expect(Buffer.from(before).equals(Buffer.from(f.sql.export()))).toBe(true);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    expect(() => createCe03LocalHost(options)).toThrow();
    const standalone = new Hono<{ Bindings: Env }>();
    registerLocalCombatProducer(standalone, resolveAuthContext, { inputs: ce03Inputs(), disposableDatabase: f.env.DB, allowedAccountIds: ["a"], targetingPolicyId });
    const standaloneResponse = await standalone.request("http://127.0.0.1/__local/ce03/prepare", { method: "POST", headers: f.headers, body: JSON.stringify(body) }, f.env);
    expect(standaloneResponse.status).toBe(200);
    expect((await standaloneResponse.json()).draft.template.execution.targetingPolicyId).toBe(targetingPolicyId);
    f.sql.close();
  });
});
