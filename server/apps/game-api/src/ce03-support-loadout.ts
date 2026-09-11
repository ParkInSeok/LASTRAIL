import { z } from "zod";
import { canonicalJson } from "@lastrail/common";
import { ActivityError } from "./activity-policy";

// Installed only by an explicitly disposable local host, never a production migration.
export const ce03SupportSchemaSql = `
CREATE TABLE IF NOT EXISTS ce03_support_loadouts (
 account_id TEXT PRIMARY KEY REFERENCES accounts(id),
 rules_version TEXT NOT NULL, catalog_version TEXT NOT NULL,
 revision INTEGER NOT NULL CHECK(revision>0), slots_json TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS ce03_support_receipts (
 account_id TEXT NOT NULL REFERENCES accounts(id), idempotency_key TEXT NOT NULL,
 request_json TEXT NOT NULL, response_json TEXT NOT NULL,
 PRIMARY KEY(account_id,idempotency_key)
);
CREATE TRIGGER IF NOT EXISTS ce03_support_receipt_update BEFORE UPDATE ON ce03_support_receipts
 BEGIN SELECT RAISE(ABORT,'ce03 support receipt immutable'); END;
CREATE TRIGGER IF NOT EXISTS ce03_support_receipt_delete BEFORE DELETE ON ce03_support_receipts
 BEGIN SELECT RAISE(ABORT,'ce03 support receipt immutable'); END;
CREATE TABLE IF NOT EXISTS ce03_support_guard(valid INTEGER NOT NULL CHECK(valid=1));
`;

const version = z.string().min(1).max(200);
const slotsSchema = z.array(z.strictObject({ slot: z.number().int().min(0).max(2), skillId: z.string().min(1).max(200) })).length(3)
  .refine(slots => slots.every((item, index) => item.slot === index) && new Set(slots.map(item => item.skillId)).size === 3);
const inputSchema = z.strictObject({ rulesVersion: version, catalogVersion: version, expectedRevision: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER - 1), slots: slotsSchema });
export type Ce03SupportCatalog = { rulesVersion: string; catalogVersion: string; skillIds: readonly string[] };
export type Ce03SupportSnapshot = { rulesVersion: string; catalogVersion: string; revision: number; slots: { slot: number; skillId: string }[] };
type Row = { rules_version: string; catalog_version: string; revision: number; slots_json: string };
const openRun = "SELECT 1 FROM runs WHERE account_id=?1 AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED')";

function checkedCatalog(catalog: Ce03SupportCatalog) {
  const parsed = z.strictObject({ rulesVersion: version, catalogVersion: version, skillIds: z.array(z.string().min(1).max(200)).length(10) }).safeParse(catalog);
  if (!parsed.success || new Set(parsed.data.skillIds).size !== 10) throw new ActivityError(503, "ce03_support_catalog_unavailable");
  return parsed.data;
}
async function owner(db: D1Database, accountId: string) {
  if (!await db.prepare("SELECT 1 FROM accounts WHERE id=?").bind(accountId).first()) throw new ActivityError(403, "ce03_support_owner_missing");
}
function snapshot(row: Row): Ce03SupportSnapshot {
  let raw: unknown;
  try { raw = JSON.parse(row.slots_json); }
  catch { throw new ActivityError(409, "ce03_support_saved_invalid"); }
  const slots = slotsSchema.safeParse(raw);
  if (!slots.success || !Number.isSafeInteger(row.revision) || row.revision < 1) throw new ActivityError(409, "ce03_support_saved_invalid");
  return { rulesVersion: row.rules_version, catalogVersion: row.catalog_version, revision: row.revision, slots: slots.data };
}
export async function readCe03SupportLoadout(db: D1Database, accountId: string, catalogInput: Ce03SupportCatalog): Promise<Ce03SupportSnapshot> {
  const catalog = checkedCatalog(catalogInput);
  await owner(db, accountId);
  const row = await db.prepare("SELECT * FROM ce03_support_loadouts WHERE account_id=?").bind(accountId).first<Row>();
  return row ? snapshot(row) : { rulesVersion: catalog.rulesVersion, catalogVersion: catalog.catalogVersion, revision: 0, slots: [] };
}

// Include this assertion with START mutations in the same atomic batch.
export function ce03SupportLoadoutGuard(db: D1Database, accountId: string, saved: Ce03SupportSnapshot): D1PreparedStatement {
  if (!Number.isSafeInteger(saved.revision) || saved.revision < 1 || !slotsSchema.safeParse(saved.slots).success)
    throw new ActivityError(409, "ce03_support_selection_required");
  return db.prepare(`INSERT INTO ce03_support_guard(valid) SELECT CASE WHEN EXISTS(
    SELECT 1 FROM ce03_support_loadouts WHERE account_id=? AND revision=? AND rules_version=? AND catalog_version=? AND slots_json=?
  ) THEN 1 ELSE 0 END`).bind(accountId, saved.revision, saved.rulesVersion, saved.catalogVersion, canonicalJson(saved.slots));
}

export async function saveCe03SupportLoadout(db: D1Database, accountId: string, catalogInput: Ce03SupportCatalog, input: unknown, key: string | undefined): Promise<Ce03SupportSnapshot> {
  const catalog = checkedCatalog(catalogInput), parsed = inputSchema.safeParse(input);
  if (!parsed.success) throw new ActivityError(400, "ce03_support_request_invalid");
  if (typeof key !== "string" || !key.trim()) throw new ActivityError(400, "idempotency_key_required");
  const body = parsed.data, requestJson = canonicalJson(body);
  await owner(db, accountId);
  const replay = async () => {
    const row = await db.prepare("SELECT request_json,response_json FROM ce03_support_receipts WHERE account_id=? AND idempotency_key=?").bind(accountId, key).first<{ request_json: string; response_json: string }>();
    if (!row) return null;
    if (row.request_json !== requestJson) throw new ActivityError(409, "idempotency_conflict");
    return JSON.parse(row.response_json) as Ce03SupportSnapshot;
  };
  const prior = await replay();
  if (prior) return prior;
  if (body.rulesVersion !== catalog.rulesVersion || body.catalogVersion !== catalog.catalogVersion) throw new ActivityError(409, "ce03_support_catalog_changed");
  if (body.slots.some(item => !catalog.skillIds.includes(item.skillId))) throw new ActivityError(400, "ce03_support_skill_unavailable");
  if (await db.prepare(openRun).bind(accountId).first()) {
    const concurrent = await replay();
    if (concurrent) return concurrent;
    throw new ActivityError(409, "departure_open_run");
  }
  const current = await readCe03SupportLoadout(db, accountId, catalog);
  if (current.revision !== body.expectedRevision) {
    const concurrent = await replay();
    if (concurrent) return concurrent;
    throw new ActivityError(409, "ce03_support_revision_changed");
  }
  const result: Ce03SupportSnapshot = { rulesVersion: body.rulesVersion, catalogVersion: body.catalogVersion, revision: body.expectedRevision + 1, slots: body.slots };
  try {
    await db.batch([
      db.prepare(`INSERT INTO ce03_support_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM accounts WHERE id=?1)
        AND NOT EXISTS(${openRun}) AND COALESCE((SELECT revision FROM ce03_support_loadouts WHERE account_id=?1),0)=?2
        THEN 1 ELSE 0 END`).bind(accountId, body.expectedRevision),
      db.prepare(`INSERT INTO ce03_support_loadouts(account_id,rules_version,catalog_version,revision,slots_json) VALUES(?,?,?,?,?)
        ON CONFLICT(account_id) DO UPDATE SET rules_version=excluded.rules_version,catalog_version=excluded.catalog_version,revision=excluded.revision,slots_json=excluded.slots_json`)
        .bind(accountId, result.rulesVersion, result.catalogVersion, result.revision, canonicalJson(result.slots)),
      db.prepare("INSERT INTO ce03_support_receipts(account_id,idempotency_key,request_json,response_json) VALUES(?,?,?,?)").bind(accountId, key, requestJson, canonicalJson(result)),
      db.prepare("DELETE FROM ce03_support_guard"),
    ]);
  } catch (error) {
    const recovered = await replay();
    if (recovered) return recovered;
    if (await db.prepare(openRun).bind(accountId).first()) throw new ActivityError(409, "departure_open_run");
    if ((await readCe03SupportLoadout(db, accountId, catalog)).revision !== body.expectedRevision) throw new ActivityError(409, "ce03_support_revision_changed");
    throw error;
  }
  return result;
}
