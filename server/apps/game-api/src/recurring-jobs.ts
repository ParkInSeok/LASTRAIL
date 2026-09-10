import { claimRecurringRight, sendExpiredQuestMail } from "./recurring-grants";
import { passEarnedXp, syncRecurringAccount } from "./recurring-progress";
import type { RecurringDefinition } from "./recurring-policy";
export async function settleRecurringAccount(db: D1Database, accountId: string, maxGrants = 20) {
  await syncRecurringAccount(db, accountId);
  await sendExpiredQuestMail(db, accountId);
  const definitions = (await db.prepare("SELECT * FROM recurring_definitions WHERE system='PASS50' AND end_at<=? ORDER BY end_at,id").bind(Date.now()).all<RecurringDefinition>()).results;
  let granted = 0;
  for (const d of definitions) {
    const level = Math.min(50, Math.floor(await passEarnedXp(db, accountId, d.id) / 100));
    const entitled = await db.prepare(`SELECT 1 FROM pass50_entitlements e JOIN verified_store_orders o ON o.id=e.order_id
      WHERE e.account_id=? AND e.policy_id=? AND o.revoked_at IS NULL`).bind(accountId, d.id).first();
    const existing = new Set((await db.prepare("SELECT right_key FROM recurring_grants WHERE account_id=? AND policy_id=?").bind(accountId, d.id).all<{ right_key: string }>()).results.map(r => r.right_key));
    for (const track of ["FREE", "PREMIUM"] as const) {
      if (track === "PREMIUM" && !entitled) continue;
      for (let i = 1; i <= level; i++) {
        if (existing.has(`pass:${d.start_at}:${i}:${track}`)) continue;
        if (granted >= maxGrants) return { granted, more: true };
        await claimRecurringRight(db, accountId, { kind: "PASS_LEVEL", policyId: d.id, level: i, track }); granted++;
      }
    }
  }
  return { granted, more: false };
}
// Durable round-robin cursor: no login required, no unbounded single invocation.
// Failed accounts retain rights; wrap-around revisits them, without renewing mail.
export async function runRecurringJobs(db: D1Database) {
  const cursor = (await db.prepare("SELECT account_id FROM recurring_job_cursor WHERE singleton_id=1").first<{ account_id: string }>())?.account_id ?? "";
  const accounts = (await db.prepare(`SELECT id FROM accounts WHERE id>? AND (EXISTS(SELECT 1 FROM objective_events WHERE account_id=accounts.id)
    OR EXISTS(SELECT 1 FROM pass50_entitlements WHERE account_id=accounts.id) OR EXISTS(SELECT 1 FROM recurring_awards WHERE account_id=accounts.id)) ORDER BY id LIMIT 10`).bind(cursor).all<{ id: string }>()).results;
  let failures = 0;
  for (const account of accounts) { try { await settleRecurringAccount(db, account.id); } catch { failures++; } }
  const next = accounts.length < 10 ? "" : accounts[accounts.length - 1]!.id;
  await db.batch([db.prepare("INSERT INTO recurring_job_cursor(singleton_id,account_id) VALUES(1,?) ON CONFLICT(singleton_id) DO UPDATE SET account_id=excluded.account_id").bind(next)]);
  return { processedAccounts: accounts.length, failures };
}
