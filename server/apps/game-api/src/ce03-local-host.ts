import { Hono } from "hono";
import type { Context } from "hono";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
import { readBoundedBody } from "./activity-routes";
import { readLocalDepartureSelection, registerDepartureRoutes, startDeparture } from "./departure";
import { prepareLocalCombatDraft, type LocalCombatInputs } from "./ce03-local-producer";
import { attestLocalConsumer, type ConsumerAttestationVerifier } from "./ce03-consumer-attestation";
import { createCommandContract, leaderPolicySchema } from "./ce03-command-contract";
import { ce03SupportLoadoutGuard, readCe03SupportLoadout, saveCe03SupportLoadout, type Ce03SupportCatalog } from "./ce03-support-loadout";
import { issueLocalCombatSeed } from "./combat-seed-protocol";
import { resolveCarriageType, trainLoadoutRulesVersion } from "./train-loadout";
import { contentSchema, id, pos, profileSchema, sha } from "./ce03-local-schema";
import gameApi from "./index";
import { presentationContentSchema, validatePresentationManifest } from "./ce03-presentation-manifest";

// Explicit local setup only. Not in migrations or public app registration.
export const ce03HostSchemaSql = `
CREATE TABLE IF NOT EXISTS ce03_start_receipts(
 account_id TEXT NOT NULL REFERENCES accounts(id), idempotency_key TEXT NOT NULL,
 request_hash TEXT NOT NULL, run_id TEXT NOT NULL REFERENCES runs(id),
 response_json TEXT NOT NULL, response_sha256 TEXT NOT NULL,
 PRIMARY KEY(account_id,idempotency_key)
);
CREATE TRIGGER IF NOT EXISTS ce03_start_receipt_update BEFORE UPDATE ON ce03_start_receipts
 BEGIN SELECT RAISE(ABORT,'ce03 start receipt immutable'); END;
CREATE TRIGGER IF NOT EXISTS ce03_start_receipt_delete BEFORE DELETE ON ce03_start_receipts
 BEGIN SELECT RAISE(ABORT,'ce03 start receipt immutable'); END;
`;
type Auth = { accountId: string } | { status: 400 | 401 | 403 | 409 | 500 | 503; errorCode: string; message: string };
type Options = { disposableDatabase: D1Database; allowedAccountIds: readonly string[];
  authenticate(c: Context<{ Bindings: Env }>): Promise<Auth>; inputs: LocalCombatInputs;
  supportCatalog: Ce03SupportCatalog; leaderPolicy: unknown;
  presentation?: { mode: "LEGACY_R4" } | { mode: "APPROVED_VIEW_V1"; manifest?: unknown };
  artifacts?: () => Promise<unknown>; verifier?: ConsumerAttestationVerifier };
const prepareBody = z.strictObject({ selection: z.unknown(), expectedSupportRevision: pos });
const startBody = z.strictObject({ operationPreviewId: id, expectedSupportRevision: pos, prepareSha256: sha });
const fail = (code: string): never => { throw new ActivityError(409, code); };
type Preview = Parameters<typeof startDeparture>[2];

/** Dedicated app, not a switch on public START. Only trusted composition can
 * install artifacts/verifier and nominate the exact disposable DB/owner set. */
export function createCe03LocalHost(options: Options) {
  const app = new Hono<{ Bindings: Env }>();
  const database = options.disposableDatabase, allowed = new Set(options.allowedAccountIds);
  const catalog = structuredClone(options.supportCatalog), inputs = { ...options.inputs };
  const leader = structuredClone(options.leaderPolicy);
  const presentation = structuredClone(options.presentation ?? { mode: "LEGACY_R4" as const });
  const auth = options.authenticate;
  app.use("*", async (c, next) => {
    c.header("Cache-Control", "no-store");
    if (c.env.DB !== database || !["127.0.0.1", "localhost", "[::1]"].includes(new URL(c.req.url).hostname))
      return c.json({ accepted: false, errorCode: "ce03_isolated_context_required" }, 403);
    const identity = await auth(c);
    if (!("accountId" in identity)) return c.json({ accepted: false, errorCode: identity.errorCode }, identity.status);
    if (!allowed.has(identity.accountId)) return c.json({ accepted: false, errorCode: "ce03_account_not_allowed" }, 403);
    if (new URL(c.req.url).search) return c.json({ accepted: false, errorCode: "contract_violation" }, 400);
    await next();
    // Forwarded public handlers return their own Response, replacing headers
    // prepared above. Reapply the local account response cache boundary.
    c.header("Cache-Control", "no-store");
  });
  app.onError((error, c) => {
    if (error instanceof ActivityError) return c.json({ accepted: false, ready: false, errorCode: error.code }, error.status);
    if (error instanceof z.ZodError || error instanceof SyntaxError) return c.json({ accepted: false, errorCode: "contract_violation" }, 400);
    return c.json({ accepted: false, errorCode: "ce03_local_unavailable" }, 503);
  });
  const account = async (c: Context<{ Bindings: Env }>) => {
    const result = await auth(c); if (!("accountId" in result)) throw new ActivityError(403, result.errorCode);
    return result.accountId;
  };
  const input = async (c: Context<{ Bindings: Env }>) => JSON.parse(await readBoundedBody(c)) as unknown;
  const key = (c: Context<{ Bindings: Env }>) => {
    const value = c.req.header("Idempotency-Key");
    if (value === undefined || !value.trim()) throw new ActivityError(400, "idempotency_key_required");
    return value; // opaque original value, never normalized
  };
  const readPreparation = async (accountId: string, selection: unknown, expectedSupportRevision: number) => {
    if (presentation.mode === "APPROVED_VIEW_V1" && presentation.manifest === undefined) return fail("ce03_presentation_manifest_missing");
    const presentationManifest = presentation.mode === "APPROVED_VIEW_V1" ? validatePresentationManifest(presentation.manifest) : undefined;
    const ownedSelection = await readLocalDepartureSelection(database, accountId, selection);
    const support = await readCe03SupportLoadout(database, accountId, catalog);
    if (support.revision !== expectedSupportRevision || support.rulesVersion !== catalog.rulesVersion || support.catalogVersion !== catalog.catalogVersion ||
      support.slots.some(s => !catalog.skillIds.includes(s.skillId))) return fail("ce03_support_revision_changed");
    const draft = await prepareLocalCombatDraft(inputs, ownedSelection);
    const commandContract = createCommandContract(support, leaderPolicySchema.parse(leader));
    const manuals = commandContract.support.slots.map(s => draft.template.train.manual.find(m => m.id === s.skillId));
    if (manuals.some(m => m === undefined)) return fail("ce03_support_effects_unavailable");
    draft.template.train.manual = manuals.map(m => m!);
    const current = await readLocalDepartureSelection(database, accountId, selection);
    const currentSupport = await readCe03SupportLoadout(database, accountId, catalog);
    if (canonicalJson(current) !== canonicalJson(ownedSelection) || canonicalJson(currentSupport) !== canonicalJson(support)) return fail("ce03_selection_changed_during_prepare");
    const ownedCars = (ownedSelection.ownedState as { carriages: { carriageId: string; level: number; revision: number }[] }).carriages;
    const carriageOwnership = { rulesVersion: trainLoadoutRulesVersion, loadoutRevision: ownedSelection.loadoutRevision,
      carriages: ownedSelection.loadout.carriages.map(row => {
        const owned = ownedCars.find(car => car.carriageId === row.carriageId), catalogType = resolveCarriageType(row.carriageId);
        const type = catalogType === "COMBAT" ? "Combat" : catalogType === "GUN" ? "Gun" : null;
        if (!owned || !type || draft.template.cars.find(car => car.slot === row.slot)?.type !== type) return fail("ce03_carriage_ownership_mismatch");
        return { ...owned, slot: row.slot, type };
      }).sort((a, b) => a.slot - b.slot) };
    const prepared = { ownedSelection, carriageOwnership, draft, commandContract, ...(presentationManifest === undefined ? {} : { presentationManifest }) };
    return { ...prepared, prepareSha256: await sha256Hex(canonicalJson(prepared)) };
  };
  const receipt = async (accountId: string, originalKey: string, hash: string) => {
    const row = await database.prepare(`SELECT c.*,r.account_id AS run_owner,s.snapshot_json FROM ce03_start_receipts c
      LEFT JOIN runs r ON r.id=c.run_id LEFT JOIN run_snapshots s ON s.run_id=r.id
      WHERE c.account_id=? AND c.idempotency_key=?`).bind(accountId, originalKey).first<{
        request_hash: string; response_json: string; response_sha256: string; run_owner: string | null; snapshot_json: string | null; run_id: string }>();
    if (!row) return null;
    if (row.request_hash !== hash) return fail("idempotency_conflict");
    if (row.run_owner !== accountId || !row.snapshot_json || await sha256Hex(row.response_json) !== row.response_sha256) return fail("ce03_snapshot_unavailable");
    const stored = JSON.parse(row.response_json), snapshot = JSON.parse(row.snapshot_json);
    if (stored.runId !== row.run_id || canonicalJson(snapshot.departureResponse) !== row.response_json) return fail("ce03_snapshot_unavailable");
    return row.response_json;
  };
  const jsonRaw = (c: Context<{ Bindings: Env }>, raw: string) => c.body(raw, 200, { "Content-Type": "application/json; charset=UTF-8" });

  // Same existing quote API, but inside this independently isolated app only.
  registerDepartureRoutes(app, auth);
  app.get("/__local/ce03/support-loadout", async c => c.json({
    selection: await readCe03SupportLoadout(database, await account(c), catalog),
    catalog: { ...catalog, scope: "LOCAL_SELECTION_ONLY", entries: catalog.skillIds.map(skillId => ({ skillId,
      executionAvailable: ["support_fire", "barrier", "emergency_repair"].includes(skillId) })) },
  }));
  app.post("/__local/ce03/support-loadout", async c => c.json(await saveCe03SupportLoadout(database, await account(c), catalog, await input(c), key(c))));
  app.post("/__local/ce03/prepare", async c => {
    const b = prepareBody.parse(await input(c));
    return c.json({ ready: false, status: "MISSING_EXECUTION_EVIDENCE", ...await readPreparation(await account(c), b.selection, b.expectedSupportRevision) });
  });
  app.post("/__local/ce03/start", async c => {
    const b = startBody.parse(await input(c)), originalKey = key(c), accountId = await account(c);
    const requestHash = await sha256Hex(canonicalJson(b)), previous = await receipt(accountId, originalKey, requestHash);
    if (previous !== null) return jsonRaw(c, previous);
    try {
    if (await database.prepare("SELECT 1 FROM idempotency_records WHERE account_id=? AND operation='run_start' AND idempotency_key=?").bind(accountId, originalKey).first()) return fail("idempotency_conflict");
    const row = await database.prepare("SELECT * FROM operation_previews WHERE id=? AND account_id=?").bind(b.operationPreviewId, accountId).first<Preview>();
    if (!row) throw new ActivityError(403, "operation_preview_not_found");
    const metadata = JSON.parse(row.preview_json), pinned = metadata.admission;
    if (!pinned || row.admission_version !== "departure-v2") return fail("ce03_departure_quote_required");
    const selection = { operationId: row.objective, risk: row.risk, expectedFormationRevision: pinned.formationRevision,
      expectedLoadoutRevision: pinned.loadoutRevision, expectedUltimateRevision: pinned.ultimateSelection?.revision };
      const prepared = await readPreparation(accountId, selection, b.expectedSupportRevision);
      if (prepared.prepareSha256 !== b.prepareSha256) return fail("ce03_preparation_changed");
      const verified = await attestLocalConsumer(prepared.draft, await options.artifacts?.(), options.verifier, prepared.commandContract,
        prepared.presentationManifest === undefined ? undefined : { manifest: prepared.presentationManifest, prepareSha256: prepared.prepareSha256 });
      const { bindings, ...profileData } = prepared.draft;
      const baseProfile = profileSchema.parse({ schemaVersion: "ce03-isolated-profile-r4", ...profileData, executionEvidence: verified.executionEvidence });
      const profile = { schemaVersion: "ce03-isolated-command-profile-r5", baseProfile, commandContract: prepared.commandContract };
      const profileSha256 = await sha256Hex(canonicalJson(profile));
      const contentManifest = prepared.presentationManifest === undefined
        ? contentSchema.parse({ schemaVersion: "ce03-isolated-content-r4", version: prepared.draft.profileId, profileSha256,
          resolver: { mode: "UNITY_EDITOR_ASSET_DATABASE", playerBuildAllowed: false }, bindings })
        : presentationContentSchema.parse({ schemaVersion: "ce03-isolated-presentation-content-r6", version: "ce03.approved-presentation.20260912.001", profileSha256,
          resolver: { mode: "UNITY_EDITOR_ASSET_DATABASE", playerBuildAllowed: false }, legacyBindings: bindings, presentationManifest: prepared.presentationManifest });
      const contentSha256 = await sha256Hex(canonicalJson(contentManifest));
      // Freeze JSON before any transaction retry. The callback adds a fresh candidate
      // seed only after verification. A committed original-key replay never calls it.
      await startDeparture(database, accountId, row, originalKey, async (_preview, runId, now) => {
        const seed = issueLocalCombatSeed(now);
        const snapshot = { version: "ce03-local-snapshot-v1", accountId, runId, operationPreviewId: row.id, prepareSha256: b.prepareSha256,
          profileSha256, contentSha256, seed, ownedSelection: prepared.ownedSelection, carriageOwnership: prepared.carriageOwnership,
          commandContract: prepared.commandContract, attestation: verified };
        const snapshotSha256 = await sha256Hex(canonicalJson(snapshot));
        return { accepted: true, startStatus: "STARTED", scope: "DISPOSABLE_LOCAL", runId, operationPreviewId: row.id,
          operationId: row.objective, risk: row.risk, riskLevel: row.risk, balanceVersion: String(row.balance_config_version),
          actId: metadata.actId, regionId: metadata.regionId, encounterType: metadata.encounterType, contentVersion: metadata.contentVersion,
          allowedOperatorSlots: metadata.allowedOperatorSlots, startAugmentPackageRef: metadata.startAugmentPackageRef, startTacticPackageRef: metadata.startTacticPackageRef,
          ordinary: { totalWaves: 15, startWaveNumber: 1 }, raid: null, raidId: null, bossId: null, bossPhaseRefs: [], runVersionSnapshot: null,
          serverStartedAt: new Date(now).toISOString(),
          combat: { schemaVersion: "ce03-accepted-combat-v1", profile, profileSha256, contentManifest, contentSha256, snapshot, snapshotSha256, seed } };
      }, async response => {
        const raw = canonicalJson(JSON.parse(JSON.stringify(response)));
        return [ce03SupportLoadoutGuard(database, accountId, prepared.commandContract.support),
        database.prepare("INSERT INTO ce03_start_receipts(account_id,idempotency_key,request_hash,run_id,response_json,response_sha256) VALUES(?,?,?,?,?,?)")
          .bind(accountId, originalKey, requestHash, response.runId as string, raw, await sha256Hex(raw)),
        database.prepare("DELETE FROM ce03_support_guard")];
      });
      const stored = await receipt(accountId, originalKey, requestHash); if (stored === null) return fail("ce03_snapshot_unavailable");
      return jsonRaw(c, stored);
    } catch (error) {
      const completed = await receipt(accountId, originalKey, requestHash);
      if (completed !== null) return jsonRaw(c, completed);
      throw error;
    }
  });
  app.post("/__local/ce03/start-result", async c => {
    const b = startBody.extend({ startIdempotencyKey: z.string().min(1) }).parse(await input(c));
    if (!b.startIdempotencyKey.trim()) throw new ActivityError(400, "idempotency_key_required");
    const { startIdempotencyKey, ...original } = b;
    const raw = await receipt(await account(c), startIdempotencyKey, await sha256Hex(canonicalJson(original)));
    return c.json({ lookupStatus: raw === null ? "NOT_FOUND" : "COMMITTED", startResponse: raw === null ? null : JSON.parse(raw), newAttemptAuthorized: false });
  });
  // Existing owner-scoped lobby reads, behind the same exact DB/loopback/auth
  // boundary. Explicit GET allowlist does not mount public mutations or START.
  for (const path of ["/v1/account/state", "/v1/wallet/crystals", "/v1/growth/config", "/v1/trains/loadout", "/v1/formation/ultimate"])
    app.get(path, c => gameApi.fetch(c.req.raw, c.env));
  // Reuse existing settlement/recovery only for runs admitted by THIS host.
  // Do not forward ordinary START or arbitrary public endpoints.
  const lifecycle = async (c: Context<{ Bindings: Env }>) => {
    const accountId = await account(c), runId = c.req.param("runId");
    const localRun = await database.prepare(`SELECT 1 FROM ce03_start_receipts c JOIN runs r ON r.id=c.run_id AND r.account_id=c.account_id
      WHERE c.account_id=? AND c.run_id=?`).bind(accountId, runId).first();
    if (!localRun) throw new ActivityError(403, "ce03_run_not_owned");
    return gameApi.fetch(c.req.raw, c.env);
  };
  app.get("/v1/runs/:runId/lifecycle", lifecycle);
  app.post("/v1/runs/:runId/finish", lifecycle);
  app.get("/v1/runs/:runId/reward-preview/:rewardPreviewId", lifecycle);
  app.post("/v1/runs/:runId/reward-preview/:rewardPreviewId/claim", lifecycle);
  return app;
}
