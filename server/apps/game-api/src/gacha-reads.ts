import type { Context, Hono } from "hono";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { ActivityError } from "./activity-policy";
import { isDevGachaScopeAllowed } from "./gacha-dev-scope";
import type { GachaBannerRow, GachaPityState, GachaPityRow, GachaPityConfig, GachaPoolResult, GachaGuaranteeType, GachaReplayResultRow, GachaCommittedResult } from "./index";

type Result = Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">;
type Helpers = {
  parseGachaPoolResults(json: string): GachaPoolResult[];
  parseGachaPityConfig(json: string): GachaPityConfig;
  getInitialPityState(row: GachaPityRow | null): GachaPityState;
  getNextGachaGuaranteeType(options: { banner: GachaBannerRow; pityState: GachaPityState; pityConfig: GachaPityConfig }): GachaGuaranteeType;
  isGachaPoolResultEligibleForGuarantee(result: GachaPoolResult, guarantee: GachaGuaranteeType, banner: GachaBannerRow): boolean;
  getGachaResultWeight(result: GachaPoolResult, guarantee: GachaGuaranteeType): number;
  getGachaReplayResponseResults(options: { resultSummaryJson: string; resultRows: GachaReplayResultRow[]; expectedPullCount: number }): Result[];
};
type Ctx = Context<{ Bindings: Env }>;
type Banner = GachaBannerRow & { pity_json: string | null };
type Batch = { id: string; banner_id: string; pull_count: number; cost_item_id: string; cost_quantity: number; post_pity_json: string; result_summary_json: string; created_at: number };
const safeId = (s: string | undefined) => s !== undefined && /^[A-Za-z0-9_-]{1,128}$/u.test(s);
const bannerSelect = `SELECT b.id,b.banner_type,b.status,b.balance_config_version,b.pickup_operator_id,b.starts_at,b.ends_at,b.pity_config_json,b.pool_snapshot_json,
  (SELECT json_object('total_pull_count',total_pull_count,'elite_pity_count',elite_pity_count,
    'pickup_elite_pity_count',pickup_elite_pity_count,'rare_or_higher_pity_count',rare_or_higher_pity_count,'revision',revision)
    FROM player_gacha_pity WHERE account_id=? AND banner_id=b.id) AS pity_json FROM gacha_banners b`;
const batchSelect = "SELECT id,banner_id,pull_count,cost_item_id,cost_quantity,post_pity_json,result_summary_json,created_at FROM gacha_pull_batches";

export function registerGachaReadRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Ctx) => Promise<ActivityAuthResult>, h: Helpers) {
  const visible = (c: Ctx, accountId: string, row: GachaBannerRow, now: number) => !row.id.startsWith("banner_dev_gacha_verify_")
    || isDevGachaScopeAllowed({ environment: c.env.ENVIRONMENT, accountId, nowMs: now, snapshot: row.pool_snapshot_json });
  function bannerDto(row: Banner, now: number) {
    const pool = h.parseGachaPoolResults(row.pool_snapshot_json);
    if (!pool.length) throw new ActivityError(503, "gacha_pool_unavailable");
    let pityConfig: GachaPityConfig, pityState: GachaPityState;
    try {
      pityConfig = h.parseGachaPityConfig(row.pity_config_json);
      pityState = h.getInitialPityState(row.pity_json === null ? null : JSON.parse(row.pity_json) as GachaPityRow);
    } catch { throw new ActivityError(503, "gacha_policy_invalid"); }
    const nextGuarantee = h.getNextGachaGuaranteeType({ banner: row, pityState, pityConfig });
    const base = pool.map(p => h.getGachaResultWeight(p, "NONE"));
    const next = pool.map(p => h.isGachaPoolResultEligibleForGuarantee(p, nextGuarantee, row) ? h.getGachaResultWeight(p, nextGuarantee) : 0);
    const total = base.reduce((a, b) => a + b, 0), nextTotal = next.reduce((a, b) => a + b, 0);
    if (total <= 0 || nextTotal <= 0) throw new ActivityError(503, "gacha_pool_unavailable");
    return { bannerId: row.id, bannerType: row.banner_type, balanceVersion: String(row.balance_config_version), pickupOperatorId: row.pickup_operator_id,
      startsAt: new Date(row.starts_at).toISOString(), endsAt: row.ends_at === null ? null : new Date(row.ends_at).toISOString(),
      availability: row.status === "ACTIVE" && row.starts_at <= now && (row.ends_at === null || now < row.ends_at) ? "ACTIVE" : row.status === "ACTIVE" && now < row.starts_at ? "UPCOMING" : "ENDED",
      prices: [1, 10].map(pullCount => ({ pullCount, itemId: "cur_command_ticket", quantity: pullCount })),
      pityConfig, pityState, nextGuarantee,
      probabilities: pool.map((p, i) => ({ resultId: p.resultId, resultType: p.resultType, resultRarity: p.resultRarity,
        quantity: p.quantity ?? 1, isPickup: !!p.isPickup || p.resultId === row.pickup_operator_id,
        baseProbability: base[i]! / total, nextPullProbability: next[i]! / nextTotal })) };
  }
  async function batchDto(c: Ctx, accountId: string, batch: Batch) {
    const rows = await c.env.DB.prepare(`SELECT r.pull_sequence,r.result_rarity,r.result_type,r.result_id,r.is_pickup,r.guarantee_type,
      r.was_duplicate,r.duplicate_conversion_json,l.delta AS reward_quantity FROM gacha_pull_results r
      LEFT JOIN item_ledger l ON l.id=r.item_ledger_id AND l.account_id=r.account_id
      WHERE r.batch_id=? AND r.account_id=? ORDER BY r.pull_sequence`).bind(batch.id, accountId).all<GachaReplayResultRow>();
    const results = h.getGachaReplayResponseResults({ resultSummaryJson: batch.result_summary_json, resultRows: rows.results, expectedPullCount: batch.pull_count })
      .filter(r => r.pullSequence >= 1 && r.pullSequence <= batch.pull_count);
    const missingSequences = Array.from({ length: batch.pull_count }, (_, i) => i + 1).filter(i => !results.some(r => r.pullSequence === i));
    const unknownQuantitySequences = results.filter(r => !Number.isSafeInteger(r.reward.quantity) || (r.reward.quantity as number) <= 0).map(r => r.pullSequence);
    let summary: { balanceVersion?: unknown } = {}, postPityState: GachaPityState | null = null;
    try { summary = JSON.parse(batch.result_summary_json) ?? {}; } catch { /* Legacy sparse receipt. */ }
    try {
      const parsed = JSON.parse(batch.post_pity_json) as GachaPityState;
      const { totalPullCount, elitePityCount, pickupElitePityCount, rareOrHigherPityCount, revision } = parsed;
      if ([totalPullCount, elitePityCount, pickupElitePityCount, rareOrHigherPityCount, revision].every(v => Number.isSafeInteger(v) && v >= 0)) {
        postPityState = { totalPullCount, elitePityCount, pickupElitePityCount, rareOrHigherPityCount, revision };
      }
    } catch { /* Unknown history stays unknown. */ }
    return { batchId: batch.id, bannerId: batch.banner_id, pullCount: batch.pull_count,
      balanceVersion: typeof summary.balanceVersion === "string" ? summary.balanceVersion : null,
      cost: { itemId: batch.cost_item_id, quantity: batch.cost_quantity }, createdAt: new Date(batch.created_at).toISOString(),
      postPityState, results, missingSequences, unknownQuantitySequences,
      recoveryStatus: missingSequences.length || unknownQuantitySequences.length ? "INCOMPLETE" : "COMPLETE" };
  }
  const handle = (kind: "LIST" | "BANNER" | "HISTORY" | "BATCH") => async (c: Ctx) => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if ((await readBoundedBody(c)).length) throw new ActivityError(400, "contract_violation");
      const query = new URL(c.req.url).searchParams;
      const allowed = kind === "LIST" ? ["limit", "afterBannerId"] : kind === "HISTORY" ? ["limit", "beforeBatchId"] : [];
      if ([...query.keys()].some(k => !allowed.includes(k) || query.getAll(k).length !== 1)) throw new ActivityError(400, "contract_violation");
      const rawLimit = query.get("limit") ?? "20";
      if (!/^\d{1,2}$/u.test(rawLimit) || +rawLimit < 1 || +rawLimit > 50) throw new ActivityError(400, "contract_violation");
      const limit = +rawLimit, now = Date.now();
      if (kind === "LIST") {
        const after = query.get("afterBannerId") ?? "";
        if (after && !safeId(after)) throw new ActivityError(400, "contract_violation");
        const rows: Banner[] = [];
        let scanAfter = after;
        while (rows.length <= limit) {
          const scanned = (await c.env.DB.prepare(`${bannerSelect} WHERE b.status='ACTIVE' AND b.starts_at<=? AND (b.ends_at IS NULL OR b.ends_at>?) AND b.id>? ORDER BY b.id LIMIT 100`)
            .bind(auth.accountId, now, now, scanAfter).all<Banner>()).results;
          rows.push(...scanned.filter(r => visible(c, auth.accountId, r, now)));
          if (scanned.length < 100) break;
          scanAfter = scanned.at(-1)!.id;
        }
        const page = rows.slice(0, limit);
        return c.json({ accepted: true, serverTime: new Date(now).toISOString(), banners: page.map(r => bannerDto(r, now)),
          nextAfterBannerId: rows.length > limit ? page.at(-1)!.id : null });
      }
      if (kind === "BANNER") {
        const id = c.req.param("bannerId"); if (!safeId(id)) throw new ActivityError(400, "contract_violation");
        const row = await c.env.DB.prepare(`${bannerSelect} WHERE b.id=? AND b.status IN ('ACTIVE','ENDED')`).bind(auth.accountId, id).first<Banner>();
        if (!row || !visible(c, auth.accountId, row, now)) return c.json({ accepted: false, errorCode: "banner_not_found" }, 404);
        return c.json({ accepted: true, serverTime: new Date(now).toISOString(), banner: bannerDto(row, now) });
      }
      if (kind === "BATCH") {
        const id = c.req.param("batchId"); if (!safeId(id)) throw new ActivityError(400, "contract_violation");
        const batch = await c.env.DB.prepare(`${batchSelect} WHERE account_id=? AND id=?`).bind(auth.accountId, id).first<Batch>();
        if (!batch) return c.json({ accepted: false, errorCode: "gacha_batch_not_found" }, 404);
        return c.json({ accepted: true, ...await batchDto(c, auth.accountId, batch) });
      }
      const before = query.get("beforeBatchId");
      if (before !== null && !safeId(before)) throw new ActivityError(400, "contract_violation");
      const cursor = before === null ? null : await c.env.DB.prepare("SELECT id,created_at FROM gacha_pull_batches WHERE account_id=? AND id=?")
        .bind(auth.accountId, before).first<{ id: string; created_at: number }>();
      if (before !== null && !cursor) throw new ActivityError(400, "invalid_history_cursor");
      const rows = cursor
        ? await c.env.DB.prepare(`${batchSelect} WHERE account_id=? AND (created_at<? OR (created_at=? AND id<?)) ORDER BY created_at DESC,id DESC LIMIT ?`)
          .bind(auth.accountId, cursor.created_at, cursor.created_at, cursor.id, limit + 1).all<Batch>()
        : await c.env.DB.prepare(`${batchSelect} WHERE account_id=? ORDER BY created_at DESC,id DESC LIMIT ?`).bind(auth.accountId, limit + 1).all<Batch>();
      const page = rows.results.slice(0, limit), batches = [];
      for (const batch of page) batches.push(await batchDto(c, auth.accountId, batch));
      return c.json({ accepted: true, batches, nextBeforeBatchId: rows.results.length > limit ? page.at(-1)!.id : null });
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  };
  app.get("/v1/gacha/banners", handle("LIST"));
  app.get("/v1/gacha/banners/:bannerId", handle("BANNER"));
  app.get("/v1/gacha/history", handle("HISTORY"));
  app.get("/v1/gacha/batches/:batchId", handle("BATCH"));
}
