import { canonicalJson, isVersionCode, parseVersion, readVersionSnapshot, saveVersion, sha256Hex,
  type ClientReleaseEntry, type VersionBucket, type VersionDocument } from "@lastrail/common";

const historyPrefix = "versions/client-release-history/";
const fail = (code: string): never => { throw new Error(code); };
const fields: Record<string, string[]> = {
  register: ["version", "versionCode", "contentRelease", "legacyTransitionConfirmation"], status: ["status"],
  promote: ["confirmation", "expectedArtifactIdentity"], minimum: ["minimumVersionCode", "confirmMinimumChange"],
  rollback: ["historyId", "confirmation"],
};
export async function artifactIdentity(entry: ClientReleaseEntry): Promise<string> {
  return `sha256:${await sha256Hex(canonicalJson(entry.contentRelease))}`;
}
async function describe(version: VersionDocument | null, revision: string | null, target: string, canMutate: boolean) {
  return { target, registryStatus: version?.clientReleaseRegistry ? "CONFIGURED" : "LEGACY", revision,
    minimumVersionCode: version?.minimumVersionCode ?? null, auditHead: version?.clientReleaseRegistry?.auditHead ?? null,
    rows: await Promise.all((version?.clientReleaseRegistry?.entries ?? []).map(async (entry) => ({ ...entry,
      artifactIdentity: await artifactIdentity(entry), validation: { status: "STRUCTURAL_ONLY", artifactBytesVerified: false } }))),
    canMutate, capabilities: { register: canMutate, changeStatus: canMutate, promote: canMutate, changeMinimum: canMutate, rollback: canMutate, manageTesters: false },
  };
}
export async function describeClientReleaseRegistry(bucket: VersionBucket, target: string, canMutate: boolean) {
  const snapshot = await readVersionSnapshot(bucket);
  return describe(snapshot.version, snapshot.etag, target, canMutate);
}
type History = { schemaVersion: 1; id: string; parent: string | null; target: string; actorId: string; reason: string;
  operation: string; createdAt: string; expectedRevision: string; before: VersionDocument; after: VersionDocument };
async function committedHistory(bucket: VersionBucket, current: VersionDocument, id: string, target: string): Promise<History> {
  if (!/^[A-Za-z0-9-]{1,128}$/u.test(id)) return fail("registry_history_invalid");
  let head = current.clientReleaseRegistry?.auditHead;
  const visited = new Set<string>();
  for (let count = 0; head && count < 1000; count++) {
    if (visited.has(head)) break;
    visited.add(head);
    const object = await bucket.get(`${historyPrefix}${head}.json`);
    const row = object ? await object.json() as History : null;
    if (!row || row.schemaVersion !== 1 || row.id !== head || row.target !== target || !parseVersion(row.before) || !parseVersion(row.after)) return fail("registry_history_invalid");
    if (head === id) return row;
    head = row.parent ?? undefined;
  }
  return fail("registry_history_not_committed");
}
export async function mutateClientReleaseRegistry(bucket: VersionBucket, target: string, actorId: string,
  operation: string, body: Record<string, unknown>, versionCode?: string) {
  if (target !== "DEV" && target !== "PRD" || body.target !== target) return fail("registry_target_mismatch");
  const allowed = fields[operation];
  if (!allowed || Object.keys(body).some((key) => !["target", "expectedRevision", "reason", ...allowed].includes(key))) return fail("registry_body_invalid");
  if (!("expectedRevision" in body)) return fail("version_precondition_required");
  if (typeof body.expectedRevision !== "string" || !/^"[\x21\x23-\x7e]{1,256}"$/u.test(body.expectedRevision)) return fail("invalid_version_revision");
  if (typeof body.reason !== "string" || !body.reason.trim() || body.reason.length > 500) return fail("registry_reason_required");
  const snapshot = await readVersionSnapshot(bucket);
  if (snapshot.etag !== body.expectedRevision) return fail("version_current_pointer_conflict");
  const before = snapshot.version;
  if (!before?.minimumVersionCode) return fail("version_policy_unconfigured");
  let after = structuredClone(before);
  const entries = after.clientReleaseRegistry?.entries ?? [];
  after.clientReleaseRegistry = { schemaVersion: 1, entries };
  const entry = entries.find((row) => row.versionCode === versionCode);
  if (operation === "register") {
    if (!before.clientReleaseRegistry && body.legacyTransitionConfirmation !== "ENABLE_EXACT_LOOKUP") return fail("registry_transition_confirmation_required");
    if (entries.some((row) => row.versionCode === body.versionCode)) return fail("version_code_already_registered");
    // First registration deliberately enables exact lookup. Legacy content is not inferred as a registry row.
    entries.push({ version: body.version, versionCode: body.versionCode, status: "INTERNAL", contentRelease: body.contentRelease } as ClientReleaseEntry);
  } else if (operation === "status" || operation === "promote") {
    if (!entry) return fail("client_version_unregistered");
    if (operation === "status") {
      if (body.status !== "INTERNAL" && body.status !== "DISABLED") return fail("registry_status_invalid");
      entry.status = body.status;
    } else {
      if (entry.status !== "INTERNAL") return fail("registry_promotion_state_invalid");
      if (body.confirmation !== "PROMOTE_UNCHANGED_ARTIFACT" || body.expectedArtifactIdentity !== await artifactIdentity(entry)) return fail("registry_artifact_identity_conflict");
      entry.status = "STABLE";
      if (Number(entry.versionCode) >= Number(after.versionCode)) { after.version = entry.version; after.versionCode = entry.versionCode; }
    }
  } else if (operation === "minimum") {
    if (body.confirmMinimumChange !== true || !isVersionCode(body.minimumVersionCode)) return fail("registry_minimum_confirmation_required");
    if (!entries.some((row) => row.status === "STABLE" && Number(row.versionCode) >= Number(body.minimumVersionCode))) return fail("registry_minimum_without_stable");
    after.minimumVersionCode = body.minimumVersionCode;
  } else {
    if (body.confirmation !== "RESTORE_REGISTRY" || typeof body.historyId !== "string") return fail("registry_rollback_confirmation_required");
    const history = await committedHistory(bucket, before, body.historyId, target);
    // Never silently return to permissive legacy lookup or change the current minimum during rollback.
    if (!history.before.clientReleaseRegistry) return fail("registry_legacy_rollback_forbidden");
    after = { ...structuredClone(history.before), minimumVersionCode: before.minimumVersionCode };
    const restoredEntries = after.clientReleaseRegistry!.entries;
    for (const registered of before.clientReleaseRegistry?.entries ?? []) {
      if (!restoredEntries.some((row) => row.versionCode === registered.versionCode)) {
        restoredEntries.push({ ...structuredClone(registered), status: "DISABLED" });
      }
    }
    if (!after.clientReleaseRegistry?.entries.some((row) => row.status === "STABLE" && Number(row.versionCode) >= Number(before.minimumVersionCode))) return fail("registry_minimum_without_stable");
  }
  const parsed = parseVersion(after);
  if (!parsed?.clientReleaseRegistry) return fail("registry_document_invalid");
  const id = crypto.randomUUID();
  parsed.clientReleaseRegistry.auditHead = id;
  const history: History = { schemaVersion: 1, id, parent: before.clientReleaseRegistry?.auditHead ?? null, target, actorId,
    reason: body.reason.trim(), operation, createdAt: new Date().toISOString(), expectedRevision: body.expectedRevision, before, after: parsed };
  const onlyIf = new Headers({ "If-None-Match": "*" });
  const written = await bucket.put(`${historyPrefix}${id}.json`, JSON.stringify(history), { onlyIf, httpMetadata: { contentType: "application/json", cacheControl: "no-store" } });
  if (!written) return fail("registry_history_write_failed");
  await saveVersion(bucket, parsed, { expectedEtag: snapshot.etag });
  // Readback may reflect a later successful mutation; clients always receive a fresh whole-document revision.
  return describeClientReleaseRegistry(bucket, target, true);
}
