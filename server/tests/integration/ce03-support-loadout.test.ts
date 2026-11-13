import { describe, expect, it } from "vitest";
import { ce03ProducerFixture } from "../helpers/ce03-producer-fixture";
import { ce03SupportSchemaSql, readCe03SupportLoadout, saveCe03SupportLoadout, ce03SupportLoadoutGuard } from "../../apps/game-api/src/ce03-support-loadout";

// Synthetic ten-ID catalog tests persistence only, not adopted effects or unlocks.
const catalog = { rulesVersion: "test-r1", catalogVersion: "test-c1", skillIds: Array.from({ length: 10 }, (_, n) => `test_support_${n}`) };
const body = (expectedRevision = 0, ids = catalog.skillIds.slice(0, 3)) => ({ rulesVersion: catalog.rulesVersion, catalogVersion: catalog.catalogVersion, expectedRevision, slots: ids.map((skillId, slot) => ({ slot, skillId })) });
async function fixture() { const f = await ce03ProducerFixture(); f.sql.run(ce03SupportSchemaSql); return f; }

describe("CE03 disposable support selection", () => {
  it("has no default; persists explicit selection isolated by owner and guards exact snapshot", async () => {
    const f = await fixture(), db = f.env.DB;
    expect(await readCe03SupportLoadout(db, "a", catalog)).toMatchObject({ revision: 0, slots: [] });
    const saved = await saveCe03SupportLoadout(db, "a", catalog, body(), " key ");
    expect(saved).toEqual({ rulesVersion: catalog.rulesVersion, catalogVersion: catalog.catalogVersion, revision: 1, slots: body().slots });
    expect(await readCe03SupportLoadout(db, "b", catalog)).toMatchObject({ revision: 0, slots: [] });
    await db.batch([ce03SupportLoadoutGuard(db, "a", saved)]);
    await expect(db.batch([ce03SupportLoadoutGuard(db, "b", saved)])).rejects.toThrow();
    await expect(saveCe03SupportLoadout(db, "missing", catalog, body(), "x")).rejects.toMatchObject({ code: "ce03_support_owner_missing" });
    await expect(readCe03SupportLoadout(db, "missing", catalog)).rejects.toMatchObject({ code: "ce03_support_owner_missing" });
  });
  it("rejects malformed, duplicate, unordered, unknown and wrong-version choices", async () => {
    const f = await fixture(), db = f.env.DB;
    for (const input of [body(0, [catalog.skillIds[0]!, catalog.skillIds[0]!, catalog.skillIds[2]!]), body(0, ["unknown", "x", "y"]), { ...body(), slots: [...body().slots].reverse() }, { ...body(), effects: [] }, { ...body(), catalogVersion: "other" }, { ...body(), rulesVersion: "other" }])
      await expect(saveCe03SupportLoadout(db, "a", catalog, input, "x")).rejects.toThrow();
    await expect(saveCe03SupportLoadout(db, "a", catalog, body(), " \t")).rejects.toThrow();
    expect(await readCe03SupportLoadout(db, "a", catalog)).toMatchObject({ revision: 0 });
  });
  it("replays original opaque key after newer saves and rejects changed body", async () => {
    const f = await fixture(), db = f.env.DB;
    const original = await saveCe03SupportLoadout(db, "a", catalog, body(), " key ");
    await saveCe03SupportLoadout(db, "a", catalog, body(1, catalog.skillIds.slice(3, 6)), "key");
    expect(await saveCe03SupportLoadout(db, "a", catalog, body(), " key ")).toEqual(original);
    await expect(saveCe03SupportLoadout(db, "a", catalog, body(0, [...catalog.skillIds.slice(0, 3)].reverse()), " key ")).rejects.toMatchObject({ code: "idempotency_conflict" });
    await expect(saveCe03SupportLoadout(db, "a", catalog, body(), "new")).rejects.toMatchObject({ code: "ce03_support_revision_changed" });
    expect(() => f.sql.run("UPDATE ce03_support_receipts SET response_json='{}'")).toThrow();
    await expect(db.batch([ce03SupportLoadoutGuard(db, "a", original)])).rejects.toThrow();
  });
  it("one concurrent CAS wins; same-key concurrent retries return one receipt", async () => {
    const f = await fixture(), db = f.env.DB;
    const results = await Promise.allSettled([saveCe03SupportLoadout(db, "a", catalog, body(), "a"), saveCe03SupportLoadout(db, "a", catalog, body(), "b")]);
    expect(results.filter(x => x.status === "fulfilled")).toHaveLength(1);
    const retries = await Promise.all([saveCe03SupportLoadout(db, "b", catalog, body(), "same"), saveCe03SupportLoadout(db, "b", catalog, body(), "same")]);
    expect(retries[0]).toEqual(retries[1]);
    expect(f.scalar("SELECT COUNT(*) FROM ce03_support_receipts")).toBe(2);
    expect(f.scalar("SELECT COUNT(*) FROM ce03_support_guard")).toBe(0);
  });
  it("rolls back selection on receipt failure, leaving the key retryable", async () => {
    const f = await fixture(), db = f.env.DB;
    f.sql.run("CREATE TRIGGER fail_support BEFORE INSERT ON ce03_support_receipts BEGIN SELECT RAISE(ABORT,'injected failure'); END");
    await expect(saveCe03SupportLoadout(db, "a", catalog, body(), "retry")).rejects.toThrow();
    expect(await readCe03SupportLoadout(db, "a", catalog)).toMatchObject({ revision: 0 });
    f.sql.run("DROP TRIGGER fail_support");
    expect(await saveCe03SupportLoadout(db, "a", catalog, body(), "retry")).toMatchObject({ revision: 1 });
  });
  it("rejects open runs both before save and if one appears immediately before the atomic batch", async () => {
    const f = await fixture(), db = f.env.DB;
    const old = await saveCe03SupportLoadout(db, "a", catalog, body(), "old");
    f.adapter.beforeBatch = () => {
      f.adapter.beforeBatch = undefined;
      f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) SELECT 'test-run','a',COALESCE(MAX(sequence),0)+1,'TEST',0 FROM account_mutations WHERE account_id='a'");
      f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('test-run','a','test-run',1,'ACTIVE',0)");
    };
    await expect(saveCe03SupportLoadout(db, "a", catalog, body(1), "race")).rejects.toMatchObject({ code: "departure_open_run" });
    expect(await readCe03SupportLoadout(db, "a", catalog)).toEqual(old);
    await expect(saveCe03SupportLoadout(db, "a", catalog, body(1), "blocked")).rejects.toMatchObject({ code: "departure_open_run" });
    expect(await saveCe03SupportLoadout(db, "a", catalog, body(), "old")).toEqual(old);
    expect(f.scalar("SELECT COUNT(*) FROM ce03_support_receipts")).toBe(1);
  });
  it("recovers the persisted original receipt when the batch response is lost after commit", async () => {
    const f = await fixture(), db = f.env.DB;
    const batch = f.adapter.batch.bind(f.adapter);
    f.adapter.batch = async statements => {
      await batch(statements);
      throw new Error("simulated response loss after commit");
    };
    const saved = await saveCe03SupportLoadout(db, "a", catalog, body(), "lost");
    expect(saved.revision).toBe(1);
    expect(await saveCe03SupportLoadout(db, "a", catalog, body(), "lost")).toEqual(saved);
    expect(f.scalar("SELECT COUNT(*) FROM ce03_support_receipts")).toBe(1);
  });
  it("rejects damaged persisted JSON as a domain error", async () => {
    const f = await fixture(), db = f.env.DB;
    await saveCe03SupportLoadout(db, "a", catalog, body(), "valid");
    f.sql.run("UPDATE ce03_support_loadouts SET slots_json='broken' WHERE account_id='a'");
    await expect(readCe03SupportLoadout(db, "a", catalog)).rejects.toMatchObject({ code: "ce03_support_saved_invalid" });
  });
});
