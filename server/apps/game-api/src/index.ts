import { Hono } from "hono";
import { createRequestId } from "@lastrail/common";

const app = new Hono<{ Bindings: Env }>();

const apiBaseUrls = {
  LOCAL: "http://localhost:8787",
  DEV: "https://dev-api.lastrail.app",
  PRD: "https://api.lastrail.app",
} as const;

type BalanceConfigRow = {
  version: number;
  schema_version: number;
  required_content_version: number;
  checksum: string;
  config_json: string;
  published_at: number;
};

type RewardPreviewRow = {
  id: string;
  account_id: string;
  run_id: string;
  status: "ACTIVE" | "CLAIMED" | "EXPIRED" | "INVALIDATED";
  balance_config_version: number;
  reward_plan_json: string;
  expires_at: number;
  created_at: number;
  claimed_at: number | null;
  reward_claim_id: string | null;
};

type CurrencyRewardLine = {
  kind: "CURRENCY";
  id: string;
  quantity: number;
  source?: string;
  adBonusEligible?: boolean;
};

type AccountMutationSequenceRow = {
  sequence: number | null;
};

type AccountCurrencyRow = {
  balance: number;
};

type IdempotencyRecordRow = {
  result_ref: string;
  created_at: number;
  reward_created_at: number | null;
};

type GachaBannerRow = {
  id: string;
  status: "DRAFT" | "ACTIVE" | "ENDED" | "DISABLED";
  starts_at: number;
  ends_at: number | null;
};

const currencyIdToD1Currency = {
  cur_credits: "CREDITS",
  cur_machine_parts: "MACHINE_PARTS",
  cur_tactical_data: "TACTICAL_DATA",
  cur_special_core: "SPECIAL_CORE",
  cur_operator_exp: "OPERATOR_EXP",
} as const;

function parsePositiveInteger(value: string): number | null {
  if (!/^[1-9]\d*$/u.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

function toIso(ms: number): string {
  return new Date(ms).toISOString();
}

function createDomainId(prefix: string): string {
  return `${prefix}_${createRequestId().slice(4)}`;
}

function getRewardLines(rewardPlanJson: string): unknown[] {
  const parsed = JSON.parse(rewardPlanJson) as { rewardLines?: unknown };
  return Array.isArray(parsed.rewardLines) ? parsed.rewardLines : [];
}

function getCurrencyRewardLines(rewardPlanJson: string): CurrencyRewardLine[] {
  return getRewardLines(rewardPlanJson).filter((line): line is CurrencyRewardLine => {
    if (typeof line !== "object" || line === null) return false;
    const candidate = line as Record<string, unknown>;
    return candidate.kind === "CURRENCY"
      && typeof candidate.id === "string"
      && Object.hasOwn(currencyIdToD1Currency, candidate.id)
      && typeof candidate.quantity === "number"
      && Number.isSafeInteger(candidate.quantity)
      && candidate.quantity > 0;
  });
}

function getClaimability(row: RewardPreviewRow): "CLAIMABLE" | "ALREADY_CLAIMED" | "EXPIRED" | "INVALIDATED" {
  if (row.status === "CLAIMED") return "ALREADY_CLAIMED";
  if (row.status === "EXPIRED") return "EXPIRED";
  if (row.status === "INVALIDATED") return "INVALIDATED";
  if (row.expires_at <= Date.now()) return "EXPIRED";
  return "CLAIMABLE";
}

function createGachaRejectedResponse(options: {
  bannerId: string;
  pullCount: number | null;
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    resultStatus: "REJECTED",
    batchId: null,
    bannerId: options.bannerId,
    pullCount: options.pullCount,
    cost: options.pullCount === 1 || options.pullCount === 10
      ? { itemId: "cur_command_ticket", quantity: options.pullCount }
      : null,
    results: [],
    postPityState: null,
    createdAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function isActiveGachaBanner(row: GachaBannerRow, now: number): boolean {
  return row.status === "ACTIVE" && row.starts_at <= now && (row.ends_at === null || row.ends_at > now);
}

app.get("/status", (c) => c.json({ ok: true, service: "game-api", requestId: createRequestId() }));
app.get("/v1/client-config", (c) =>
  c.json({
    environment: c.env.ENVIRONMENT,
    apiBaseUrl: apiBaseUrls[c.env.ENVIRONMENT],
    balanceConfigEndpointTemplate: "/v1/balance-config/:version",
    minimumClientVersion: "0.1.0",
    maintenance: { enabled: false },
  }),
);
app.get("/v1/balance-config/:version", async (c) => {
  const rawVersion = c.req.param("version");
  const version = parsePositiveInteger(rawVersion);
  if (version === null) {
    return c.json({ error: "INVALID_BALANCE_CONFIG_VERSION", version: rawVersion }, 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT version, schema_version, required_content_version, checksum, config_json, published_at
     FROM balance_config_versions
     WHERE version = ?`,
  )
    .bind(version)
    .first<BalanceConfigRow>();

  if (row === null) {
    return c.json({ error: "BALANCE_CONFIG_NOT_PUBLISHED", version }, 404);
  }

  let payload: unknown;
  try {
    payload = JSON.parse(row.config_json) as unknown;
  } catch {
    return c.json({ error: "BALANCE_CONFIG_PAYLOAD_INVALID", version }, 500);
  }

  return c.json({
    version: row.version,
    schemaVersion: row.schema_version,
    requiredContentVersion: row.required_content_version,
    checksum: row.checksum,
    publishedAt: row.published_at,
    payload,
  });
});

app.post("/v1/gacha/banners/:bannerId/pulls", async (c) => {
  const bannerId = c.req.param("bannerId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount: null,
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  let body: { bannerId?: unknown; pullCount?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  const pullCount = typeof body.pullCount === "number" && Number.isSafeInteger(body.pullCount)
    ? body.pullCount
    : null;

  if (body.bannerId !== undefined) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "contract_violation",
      message: "Request body must not include bannerId.",
    }), 400);
  }

  if (pullCount !== 1 && pullCount !== 10) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "invalid_pull_count",
      message: "pullCount must be 1 or 10.",
    }), 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT id, status, starts_at, ends_at
     FROM gacha_banners
     WHERE id = ?`,
  )
    .bind(bannerId)
    .first<GachaBannerRow>();

  if (row === null) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "banner_not_found",
      message: "Gacha banner was not found.",
    }), 404);
  }

  if (!isActiveGachaBanner(row, Date.now())) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "banner_inactive",
      message: "Gacha banner is not active.",
    }), 409);
  }

  return c.json(createGachaRejectedResponse({
    bannerId,
    pullCount,
    errorCode: "gacha_pull_not_implemented",
    message: "Gacha pull commit transaction is not implemented yet.",
  }), 501);
});

app.get("/v1/runs/:runId/reward-preview/:rewardPreviewId", async (c) => {
  const runId = c.req.param("runId");
  const rewardPreviewId = c.req.param("rewardPreviewId");
  const row = await c.env.DB.prepare(
    `SELECT id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     FROM run_reward_previews
     WHERE run_id = ? AND id = ?`,
  )
    .bind(runId, rewardPreviewId)
    .first<RewardPreviewRow>();

  if (row === null) {
    return c.json({ errorCode: "reward_preview_not_found", message: "Reward preview was not found." }, 404);
  }

  const claimability = getClaimability(row);
  return c.json({
    runId: row.run_id,
    rewardPreviewId: row.id,
    status: row.status,
    claimability,
    balanceVersion: String(row.balance_config_version),
    expiresAt: toIso(row.expires_at),
    serverCalculatedAt: toIso(row.created_at),
    rewardLines: getRewardLines(row.reward_plan_json),
    claim: {
      claimable: claimability === "CLAIMABLE",
      rewardClaimId: row.reward_claim_id,
      claimedAt: row.claimed_at === null ? null : toIso(row.claimed_at),
      errorCode: claimability === "CLAIMABLE" || claimability === "ALREADY_CLAIMED" ? null : `reward_preview_${claimability.toLowerCase()}`,
    },
  });
});

app.post("/v1/runs/:runId/reward-preview/:rewardPreviewId/claim", async (c) => {
  const runId = c.req.param("runId");
  const rewardPreviewId = c.req.param("rewardPreviewId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }, 400);
  }

  let body: { runId?: unknown; rewardPreviewId?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  if (body.runId !== runId || body.rewardPreviewId !== rewardPreviewId) {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Path and body runId/rewardPreviewId must match.",
    }, 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     FROM run_reward_previews
     WHERE run_id = ? AND id = ?`,
  )
    .bind(runId, rewardPreviewId)
    .first<RewardPreviewRow>();

  if (row !== null && row.status === "CLAIMED" && row.reward_claim_id !== null && row.claimed_at !== null) {
    return c.json({
      runId: row.run_id,
      rewardPreviewId: row.id,
      rewardClaimId: row.reward_claim_id,
      status: "DUPLICATE_REPLAY",
      serverClaimedAt: toIso(row.claimed_at),
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: "duplicate_replay",
      message: "Reward claim was already committed.",
    });
  }

  if (row === null) {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "reward_preview_not_found",
      message: "Reward preview was not found.",
    }, 404);
  }

  if (getClaimability(row) !== "CLAIMABLE") {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: `reward_preview_${getClaimability(row).toLowerCase()}`,
      message: "Reward preview is not claimable.",
    });
  }

  const idempotencyRecord = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, reward.created_at AS reward_created_at
     FROM idempotency_records idem
     LEFT JOIN reward_transactions reward ON reward.id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'run_reward_claim' AND idem.idempotency_key = ?`,
  )
    .bind(row.account_id, idempotencyKey)
    .first<IdempotencyRecordRow>();

  if (idempotencyRecord !== null) {
    return c.json({
      runId: row.run_id,
      rewardPreviewId: row.id,
      rewardClaimId: idempotencyRecord.result_ref,
      status: "DUPLICATE_REPLAY",
      serverClaimedAt: toIso(idempotencyRecord.reward_created_at ?? idempotencyRecord.created_at),
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: "duplicate_replay",
      message: "Reward claim was already committed.",
    });
  }

  const now = Date.now();
  const accountId = row.account_id;
  const rewardClaimId = createDomainId("reward_claim");
  const mutationId = createDomainId("mutation");
  const economyTransactionId = createDomainId("economy");
  const rewardLines = getCurrencyRewardLines(row.reward_plan_json);
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const claimRewardLines = [];
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', ?, ?)`,
    ).bind(mutationId, accountId, nextSequence, idempotencyKey, now),
    c.env.DB.prepare(
      `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
    ).bind(rewardClaimId, accountId, runId, mutationId, row.balance_config_version, row.reward_plan_json, now),
    c.env.DB.prepare(
      `INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', 'RUN_REWARD_PREVIEW', ?, ?)`,
    ).bind(economyTransactionId, accountId, mutationId, rewardPreviewId, now),
  ];

  for (const rewardLine of rewardLines) {
    const currency = currencyIdToD1Currency[rewardLine.id as keyof typeof currencyIdToD1Currency];
    const current = await c.env.DB.prepare(
      `SELECT balance
       FROM account_currencies
       WHERE account_id = ? AND currency = ?`,
    )
      .bind(accountId, currency)
      .first<AccountCurrencyRow>();
    const balanceAfter = (current?.balance ?? 0) + rewardLine.quantity;
    const ledgerId = createDomainId("ledger");
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO account_currencies (account_id, currency, balance, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, currency) DO UPDATE SET balance = excluded.balance, updated_at = excluded.updated_at`,
      ).bind(accountId, currency, balanceAfter, now),
      c.env.DB.prepare(
        `INSERT INTO currency_ledger (id, account_id, transaction_id, currency, delta, balance_after, created_at)
         VALUES (?, ?, ?, ?, ?, ?, ?)`,
      ).bind(ledgerId, accountId, economyTransactionId, currency, rewardLine.quantity, balanceAfter, now),
    );
    claimRewardLines.push({
      kind: rewardLine.kind,
      id: rewardLine.id,
      quantity: rewardLine.quantity,
      source: rewardLine.source,
      balanceAfter,
    });
  }

  statements.push(
    c.env.DB.prepare(
      `UPDATE run_reward_previews
       SET status = 'CLAIMED', claimed_at = ?, reward_claim_id = ?
       WHERE id = ? AND run_id = ? AND status = 'ACTIVE'`,
    ).bind(now, rewardClaimId, rewardPreviewId, runId),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'run_reward_claim', ?, ?)`,
    ).bind(createDomainId("idem"), accountId, idempotencyKey, rewardClaimId, now),
  );

  await c.env.DB.batch(statements);

  return c.json({
    runId,
    rewardPreviewId,
    rewardClaimId,
    status: "CLAIMED",
    serverClaimedAt: toIso(now),
    balanceVersion: String(row.balance_config_version),
    rewardLines: claimRewardLines,
    errorCode: null,
    message: null,
  });
});

export default app;


