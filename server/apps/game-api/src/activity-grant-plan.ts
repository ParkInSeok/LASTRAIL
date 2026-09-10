import { ActivityError, type Reward } from "./activity-policy";
import { planCrystalChange, walletId } from "./crystal-wallet";
export type ActivityGrantReward = Reward | { kind: "CURRENCY"; id: string; quantity: number };
const currencyIds: Record<string, string> = { cur_credits: "CREDITS", cur_machine_parts: "MACHINE_PARTS", cur_operator_exp: "OPERATOR_EXP", cur_tactical_data: "TACTICAL_DATA" };

// Shared ledger/wallet planning for monthly attendance and upcoming mission grants.
// The caller must put this plan behind an account sequence fence in one DB batch.
export async function planActivityGrant(input: {
  db: D1Database; accountId: string; mutationId: string; claimId: string; now: number;
  rewards: ActivityGrantReward[]; operation: string; economyId: string;
  recovery?: { walletPolicyVersion: string; confirmedPaid: number };
}) {
  const { db, accountId, mutationId, claimId, now, rewards, operation } = input;
  const statements: D1PreparedStatement[] = [], lines: Array<Record<string, unknown>> = [];
  const crystals = rewards.filter(r => r.kind === "CRYSTAL");
  let wallet: Record<string, unknown> | null = null;
  if (crystals.length || input.recovery) {
    const plan = await planCrystalChange({ db, accountId, mutationId, operationId: claimId, operation, now,
      grants: crystals.map(r => ({ quantity: r.quantity, sourceType: "ATTENDANCE", sourceRef: claimId, orderId: null })),
      ...(input.recovery ? { expectedPolicyVersion: input.recovery.walletPolicyVersion,
        spend: { quantity: 100, reason: "ATTENDANCE_RECOVERY" as const, downstreamRef: claimId } } : {}),
    });
    if (input.recovery && plan.response.paidSpent !== input.recovery.confirmedPaid) throw new ActivityError(409, "attendance_paid_confirmation_changed");
    statements.push(...plan.statements); lines.push(...plan.grantLines); wallet = plan.response;
  }
  for (const reward of rewards) {
    if (reward.kind === "CRYSTAL") continue;
    if (reward.kind === "CURRENCY") {
      const currency = currencyIds[reward.id];
      if (!currency) throw new ActivityError(503, "activity_currency_binding_missing");
      const before = (await db.prepare("SELECT balance FROM account_currencies WHERE account_id=? AND currency=?").bind(accountId, currency).first<{ balance: number }>())?.balance ?? 0;
      const after = before + reward.quantity;
      if (!Number.isSafeInteger(after)) throw new ActivityError(409, "reward_balance_overflow");
      statements.push(db.prepare(`INSERT INTO account_currencies(account_id,currency,balance,updated_at) VALUES(?,?,?,?)
        ON CONFLICT(account_id,currency) DO UPDATE SET balance=account_currencies.balance+excluded.balance,updated_at=excluded.updated_at`).bind(accountId, currency, reward.quantity, now),
      db.prepare("INSERT INTO currency_ledger(id,account_id,transaction_id,currency,delta,balance_after,created_at) VALUES(?,?,?,?,?,?,?)").bind(walletId("currency_ledger"), accountId, input.economyId, currency, reward.quantity, after, now));
      lines.push({ ...reward, balanceAfter: after });
      continue;
    }
    const before = (await db.prepare("SELECT quantity FROM account_items WHERE account_id=? AND item_id=?").bind(accountId, reward.id).first<{ quantity: number }>())?.quantity ?? 0;
    const after = before + reward.quantity;
    if (!Number.isSafeInteger(after)) throw new ActivityError(409, "reward_balance_overflow");
    statements.push(db.prepare(`INSERT INTO account_items(account_id,item_id,quantity,updated_at) VALUES(?,?,?,?)
      ON CONFLICT(account_id,item_id) DO UPDATE SET quantity=account_items.quantity+excluded.quantity,updated_at=excluded.updated_at`).bind(accountId, reward.id, reward.quantity, now),
    db.prepare(`INSERT INTO item_ledger(id,account_id,mutation_id,item_id,delta,quantity_after,reason,source_type,source_id,created_at)
      VALUES(?,?,?,?,?,?,?,'ACTIVITY',?,?)`).bind(walletId("item_ledger"), accountId, mutationId, reward.id, reward.quantity, after, operation, claimId, now));
    lines.push({ kind: "ITEM", ...reward, balanceAfter: after });
  }
  return { statements, lines, wallet };
}
