import { describe, expect, it } from "vitest";
import { Buffer } from "node:buffer";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ce03ProducerFixture, ce03Inputs } from "../helpers/ce03-producer-fixture";
import { body } from "../helpers/departure-fixture";
import { prepareLocalCombatDraft } from "../../apps/game-api/src/ce03-local-producer";
import { templateSchema, policySchema } from "../../apps/game-api/src/ce03-local-schema";
import { commandContractSchema, createCommandContract, createIndividualCommandContract, createAutomaticActiveCommandContract, localLeaderChargePolicy, localSupportCatalog } from "../../apps/game-api/src/ce03-command-contract";
import { attestLocalConsumer, requiredGeneralAutoConsumerCoverage } from "../../apps/game-api/src/ce03-consumer-attestation";
import { createCe03LocalHost, ce03HostSchemaSql } from "../../apps/game-api/src/ce03-local-host";
import { ce03SupportSchemaSql } from "../../apps/game-api/src/ce03-support-loadout";
import { resolveAuthContext } from "../../apps/game-api/src/index";

const policy = { targetingPolicyId: "ce03.frontline-first-v1", ultimatePolicyId: "ce03.per-operator-ultimate-v1" } as const;
const version = "ce03.execution.2026-09-13.002";
const policyHash = "68beb942a695ea761aa0f2aaf5677d7eee603aeeb3de078ffca148a27d824b50";

describe("versioned individual ultimates", () => {
  it("separately opts into always automatic actives without changing the earlier ultimate-only draft", async () => {
    const f = await ce03ProducerFixture(), { ownedSelection } = await (await f.request()).json();
    const earlier = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, policy);
    const options = { ...policy, activePolicyId: "ce03.general-active-auto-v1" } as const;
    const next = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, options);
    expect(next.simulationVersion).toBe("ce03.execution.2026-09-13.003");
    expect(next.executionPolicy.orderedStages[1]).toBe("A1.manual-ultimate-auto-active-auto-commands");
    expect(next.template.execution).toMatchObject(options);
    expect(next.template.operators).toEqual(earlier.template.operators);
    expect(next.template.train).toEqual(earlier.template.train);
    expect(next.sourceFiles.some(s => s.logicalName === "generalActiveAutoPolicyV1")).toBe(true);
    expect(await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, policy)).toStrictEqual(earlier);
    await expect(prepareLocalCombatDraft(ce03Inputs(), ownedSelection, { activePolicyId: options.activePolicyId } as never)).rejects.toThrow();
    const missing = structuredClone(next.template); Reflect.deleteProperty(missing.execution, "activePolicyId");
    expect(templateSchema.safeParse(missing).success).toBe(false);
    const mixed = structuredClone(earlier.template); Object.assign(mixed.execution, { activePolicyId: options.activePolicyId });
    expect(templateSchema.safeParse(mixed).success).toBe(false);
    f.sql.close();
  });
  it("produces all operators with null selected ultimate only under explicit new identity", async () => {
    const f = await ce03ProducerFixture(), { ownedSelection, draft: old } = await (await f.request()).json();
    expect(await sha256Hex(canonicalJson(old))).toBe("c5cbc882a853a1a1e157d52e4497204fb8ce142a5f057f81f68b5900ee71a544");
    const next = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, policy);
    expect(next.simulationVersion).toBe(version);
    expect(next.template.selectedUltimateOperatorId).toBeNull();
    expect(next.template.execution).toMatchObject({ ...policy, policyVersion: version, policySha256: policyHash });
    expect(next.template.operators).toEqual(old.template.operators);
    expect(next.template.train).toEqual(old.template.train);
    expect(next.executionPolicy.orderedStages[1]).toBe("U1.manual-auto-commands");
    expect(await sha256Hex(canonicalJson(next.executionPolicy))).toBe(policyHash);
    const noSelection = structuredClone(ownedSelection); noSelection.ultimateSelection.operatorId = null;
    expect((await prepareLocalCombatDraft(ce03Inputs(), noSelection, policy)).template.selectedUltimateOperatorId).toBeNull();
    await expect(prepareLocalCombatDraft(ce03Inputs(), noSelection)).rejects.toThrow();
    await expect(prepareLocalCombatDraft(ce03Inputs(), ownedSelection, { ultimatePolicyId: policy.ultimatePolicyId } as never)).rejects.toThrow();
    expect(templateSchema.safeParse({ ...old.template, selectedUltimateOperatorId: null }).success).toBe(false);
    expect(templateSchema.safeParse({ ...next.template, selectedUltimateOperatorId: "op_reina" }).success).toBe(false);
    const missing = structuredClone(next.template); Reflect.deleteProperty(missing.execution, "ultimatePolicyId");
    expect(templateSchema.safeParse(missing).success).toBe(false);
    expect(policySchema.safeParse({ ...next.executionPolicy, orderedStages: old.executionPolicy.orderedStages }).success).toBe(false);
    f.sql.close();
  });

  it.each([false, true])("normal new host preparation requires no leader choice (general auto=%s)", async general => {
    const f = await ce03ProducerFixture(); f.sql.run(ce03SupportSchemaSql); f.sql.run(ce03HostSchemaSql);
    f.sql.run("UPDATE account_formation SET ultimate_operator_id=NULL WHERE account_id='a'");
    const options = { ...policy, ...(general ? { activePolicyId: "ce03.general-active-auto-v1" as const } : {}), disposableDatabase: f.env.DB, allowedAccountIds: ["a"], authenticate: resolveAuthContext,
      inputs: ce03Inputs(), supportCatalog: localSupportCatalog };
    const host = createCe03LocalHost(options);
    const post = (path: string, data: unknown) => host.request("http://127.0.0.1" + path, { method: "POST",
      headers: { ...f.headers, "Idempotency-Key": "individual-support" }, body: JSON.stringify(data) }, f.env);
    expect((await post("/__local/ce03/support-loadout", { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion,
      expectedRevision: 0, slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) })).status).toBe(200);
    const selection = { ...body }; Reflect.deleteProperty(selection, "expectedUltimateRevision");
    const before = f.sql.export();
    const response = await post("/__local/ce03/prepare", { selection, expectedSupportRevision: 1 });
    expect(response.status).toBe(200);
    const { ready, status, prepareSha256, ...prepared } = await response.json();
    expect({ ready, status }).toEqual({ ready: false, status: "MISSING_EXECUTION_EVIDENCE" });
    expect(prepareSha256).toBe(await sha256Hex(canonicalJson(prepared)));
    expect(prepared.ownedSelection.ultimateSelection.operatorId).toBeNull();
    expect(prepared.commandContract).not.toHaveProperty("leader");
    expect(prepared.commandContract).toMatchObject({ version: general ? "ce03-command-contract-v3" : "ce03-command-contract-v2", ultimates: {
      version: "per-operator-basic-hp-hit-v1", perHit: 4, maximum: 100, consumeAt: "CAST_ACCEPTANCE",
      overrides: "ULTIMATE_FIRST_READY_AND_COOLDOWN", auto: { defaultEnabled: false, scope: "ULTIMATES_ONLY", order: "FORMATION_SLOT_THEN_ACTOR_ID" } } });
    if (general) {
      expect(prepared.commandContract.actives).toMatchObject({ enabled: "ALWAYS", readiness: "AUTHORED_FIRST_READY_AND_COOLDOWN",
        decisionStage: "AFTER_MANUAL_AND_ULTIMATE_AUTO_BEFORE_BASIC_SCHEDULE", trainSkills: "MANUAL_ONLY", failedCandidate: "SKIP_WITHOUT_JOURNAL" });
      const disabled = structuredClone(prepared.commandContract); disabled.actives.enabled = false;
      expect(commandContractSchema.safeParse(disabled).success).toBe(false);
    } else expect(prepared.commandContract).not.toHaveProperty("actives");
    expect(commandContractSchema.safeParse(prepared.commandContract).success).toBe(true);
    const invalid = structuredClone(prepared.commandContract); invalid.ultimates.perHit = 5;
    expect(commandContractSchema.safeParse(invalid).success).toBe(false);
    invalid.ultimates.perHit = 4; invalid.leader = localLeaderChargePolicy;
    expect(commandContractSchema.safeParse(invalid).success).toBe(false);
    expect((await post("/__local/ce03/prepare", { selection, expectedSupportRevision: 1, ultimateAutoEnabled: true })).status).toBe(400);
    expect((await f.request()).status).toBe(409);
    expect(Buffer.from(before).equals(Buffer.from(f.sql.export()))).toBe(true);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    f.sql.close();
  });

  it("rejects command/draft generation mixing before running any consumer verification", async () => {
    const f = await ce03ProducerFixture(), { draft: old, ownedSelection } = await (await f.request()).json();
    const next = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, policy);
    const support = { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion, revision: 1,
      slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) };
    const v1 = createCommandContract(support, localLeaderChargePolicy), v2 = createIndividualCommandContract(support);
    const artifacts = { algorithmSource: "test-only bytes", checkpointSchema: "test-only schema", vectorsJson: "{}", reportJson: "{}" };
    let verified = 0; const verifier = { verify: async () => { verified++; return true; } };
    for (const [draft, command] of [[next, v1], [old, v2], [next, undefined]] as const)
      await expect(attestLocalConsumer(draft, artifacts, verifier, command)).rejects.toMatchObject({ code: "ce03_consumer_command_policy_mismatch" });
    await expect(attestLocalConsumer(next, artifacts, verifier, v2)).rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    expect(verified).toBe(0); f.sql.close();
  });

  it("requires general auto coverage and exact command generation before delegating proof to the trusted verifier", async () => {
    const f = await ce03ProducerFixture(), { ownedSelection } = await (await f.request()).json();
    const earlier = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, policy);
    const next = await prepareLocalCombatDraft(ce03Inputs(), ownedSelection, { ...policy, activePolicyId: "ce03.general-active-auto-v1" });
    const support = { rulesVersion: localSupportCatalog.rulesVersion, catalogVersion: localSupportCatalog.catalogVersion, revision: 1,
      slots: ["barrier", "support_fire", "emergency_repair"].map((skillId, slot) => ({ slot, skillId })) };
    const v2 = createIndividualCommandContract(support), v3 = createAutomaticActiveCommandContract(support);
    let verified = 0; const verifier = { verify: async () => { verified++; return true; } };
    // Deliberately synthetic evidence tests only the contract boundary, not Unity execution.
    const algorithmSource = "synthetic algorithm", checkpointSchema = "synthetic checkpoint";
    const artifacts = async (coverages: readonly string[], schemaVersion = "ce03-consumer-runner-report-v4") => {
      const cases = coverages.map((coverage, i) => ({ id: `case_${i}`, coverage }));
      const vectorsJson = canonicalJson({ cases });
      const reportJson = canonicalJson({ schemaVersion, scope: "FULL_LOCAL_CONSUMER", runId: "synthetic",
        simulationVersion: next.simulationVersion, draftSha256: await sha256Hex(canonicalJson(next)),
        algorithmSourceSha256: await sha256Hex(algorithmSource), checkpointSchemaSha256: await sha256Hex(checkpointSchema),
        vectorsSha256: await sha256Hex(vectorsJson), commandContractSha256: await sha256Hex(canonicalJson(v3)),
        results: cases.map(c => ({ caseId: c.id, coverage: c.coverage, status: "PASS" })), passed: cases.length, failed: 0, skipped: 0 });
      return { algorithmSource, checkpointSchema, vectorsJson, reportJson };
    };
    const full = await artifacts(requiredGeneralAutoConsumerCoverage);
    for (const [draft, command] of [[next, v2], [earlier, v3], [next, undefined]] as const)
      await expect(attestLocalConsumer(draft, full, verifier, command)).rejects.toMatchObject({ code: "ce03_consumer_command_policy_mismatch" });
    await expect(attestLocalConsumer(next, await artifacts(requiredGeneralAutoConsumerCoverage.filter(c => c !== "GENERAL_ACTIVE_AUTO")), verifier, v3))
      .rejects.toMatchObject({ code: "ce03_consumer_coverage_incomplete" });
    await expect(attestLocalConsumer(next, await artifacts(requiredGeneralAutoConsumerCoverage, "ce03-consumer-runner-report-v2"), verifier, v3))
      .rejects.toMatchObject({ code: "ce03_consumer_report_invalid" });
    expect(verified).toBe(0);
    await expect(attestLocalConsumer(next, full, verifier, v3)).resolves.toMatchObject({ status: "ATTESTATION_VERIFIED", ready: false });
    expect(verified).toBe(1);
    await expect(attestLocalConsumer(next, full, { verify: async () => false }, v3)).rejects.toMatchObject({ code: "ce03_consumer_attestation_rejected" });
    f.sql.close();
  });
});
