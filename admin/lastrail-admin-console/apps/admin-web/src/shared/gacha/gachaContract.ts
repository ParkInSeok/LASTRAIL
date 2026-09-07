import { adminReadValueLabel } from "../api/errorDisplay";
import { gachaGuaranteeTypeLabel } from "../labels/adminLabels";

export type GachaBannerSummary = {
  bannerId: string;
  bannerType: string;
  status: string;
  activeBannerId: string;
  pickupOperatorId: string;
  balanceVersion: string;
  pullCostItemId: string;
  pullCostQuantity: string;
  pullCountOptions: string[];
  pityThresholds: {
    rareOrHigher: string;
    elite: string;
    pickupElite: string;
  };
  guaranteeTypes: string[];
  startsAt: string;
  endsAt: string;
  updatedAt: string;
  detailAvailable: boolean;
};

export type GachaBannerListResponse = {
  permission: string;
  permissionMode: string;
  activeBannerId: string;
  items: GachaBannerSummary[];
  nextCursor: string | null;
};

export type GachaPityResponse = {
  permission: string;
  permissionMode: string;
  accountId: string;
  bannerId: string;
  pityState: {
    totalPullCount: string;
    elitePityCount: string;
    pickupElitePityCount: string;
    rareOrHigherPityCount: string;
    revision: string;
    lastBatchId: string;
    updatedAt: string;
  };
  detailAvailable: boolean;
};

export type GachaPullBatchSummary = {
  batchId: string;
  accountId: string;
  bannerId: string;
  resultStatus: string;
  idempotencyReplayKnown: string;
  pullCount: string;
  cost: {
    itemId: string;
    quantity: string;
    itemLedgerRef: string;
  };
  prePityState: unknown;
  postPityState: unknown;
  guaranteeTypes: string[];
  createdAt: string;
};

export type GachaPullBatchListResponse = {
  permission: string;
  permissionMode: string;
  items: GachaPullBatchSummary[];
  nextCursor: string | null;
};

export type GachaPullResultSummary = {
  resultId: string;
  pullSequence: string;
  resultRarity: string;
  resultType: string;
  rewardId: string;
  rewardQuantity: number | null;
  isPickup: string;
  wasDuplicate: string;
  guaranteeType: string;
  duplicateConversion: string;
  itemLedgerRef: string;
  operatorLedgerRef: string;
};

export type GachaPullBatchDetailResponse = {
  item: GachaPullBatchSummary & {
    itemLedgerRefs: string[];
    operatorLedgerRefs: string[];
    results: GachaPullResultSummary[];
    causeEvidence: { source: string; duplicateResultCount: number | null; idempotencyReplayKnown: boolean | null; guaranteeTypes: string[] } | null;
  };
  detailAvailable: boolean;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function readValue(value: unknown, emptyLabel = "없음"): string {
  return adminReadValueLabel({ kind: "success", value, emptyLabel });
}

function numberValue(value: unknown): string {
  return typeof value === "number" && Number.isFinite(value) ? String(value) : readValue(value);
}

function stringList(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value
    .filter((item): item is string | number => typeof item === "string" || typeof item === "number")
    .map(String);
}

function guaranteeTypeValue(value: unknown): string {
  const code = readValue(value);
  return code === "없음" ? code : `${gachaGuaranteeTypeLabel(code)} (${code})`;
}

function guaranteeTypeList(value: unknown): string[] {
  return stringList(value).map(guaranteeTypeValue);
}

function booleanValue(value: unknown): string {
  if (value === true) return "예";
  if (value === false) return "아니오";
  return "없음";
}

function mapBanner(value: unknown, activeBannerId: unknown): GachaBannerSummary | null {
  const record = asRecord(value);
  if (!record) return null;
  const pityThresholds = asRecord(record.pityThresholds);
  const bannerId = readValue(record.bannerId);
  return {
    bannerId,
    bannerType: readValue(record.bannerType),
    status: readValue(record.status),
    activeBannerId: bannerId === activeBannerId ? bannerId : "없음",
    pickupOperatorId: readValue(record.pickupOperatorId),
    balanceVersion: readValue(record.balanceVersion),
    pullCostItemId: readValue(record.pullCostItemId),
    pullCostQuantity: numberValue(record.pullCostQuantity),
    pullCountOptions: stringList(record.pullCountOptions),
    pityThresholds: {
      rareOrHigher: numberValue(pityThresholds?.rareOrHigher),
      elite: numberValue(pityThresholds?.elite),
      pickupElite: numberValue(pityThresholds?.pickupElite)
    },
    guaranteeTypes: guaranteeTypeList(record.guaranteeTypes),
    startsAt: readValue(record.startsAt),
    endsAt: readValue(record.endsAt),
    updatedAt: readValue(record.updatedAt),
    detailAvailable: record.detailAvailable === true
  };
}

export function mapGachaBannerListResponse(response: unknown): GachaBannerListResponse {
  const record = asRecord(response) ?? {};
  const activeBannerId = record.activeBannerId;
  return {
    permission: readValue(record.permission, "SUPPORT_READ"),
    permissionMode: readValue(record.permissionMode, "ROLE_PERMISSION"),
    activeBannerId: readValue(activeBannerId),
    items: Array.isArray(record.items)
      ? record.items.map((item) => mapBanner(item, activeBannerId)).filter((item): item is GachaBannerSummary => Boolean(item))
      : [],
    nextCursor: typeof record.nextCursor === "string" ? record.nextCursor : null
  };
}

export function mapGachaPityResponse(response: unknown): GachaPityResponse {
  const record = asRecord(response) ?? {};
  const pityState = asRecord(record.pityState);
  return {
    permission: readValue(record.permission, "SUPPORT_READ"),
    permissionMode: readValue(record.permissionMode, "ROLE_PERMISSION"),
    accountId: readValue(record.accountId),
    bannerId: readValue(record.bannerId),
    pityState: {
      totalPullCount: numberValue(pityState?.totalPullCount),
      elitePityCount: numberValue(pityState?.elitePityCount),
      pickupElitePityCount: numberValue(pityState?.pickupElitePityCount),
      rareOrHigherPityCount: numberValue(pityState?.rareOrHigherPityCount),
      revision: numberValue(pityState?.revision),
      lastBatchId: readValue(pityState?.lastBatchId),
      updatedAt: readValue(pityState?.updatedAt)
    },
    detailAvailable: record.detailAvailable === true
  };
}

function mapCost(value: unknown): GachaPullBatchSummary["cost"] {
  const record = asRecord(value);
  return {
    itemId: readValue(record?.itemId),
    quantity: numberValue(record?.quantity),
    itemLedgerRef: readValue(record?.itemLedgerRef)
  };
}

function mapBatch(value: unknown): GachaPullBatchSummary | null {
  const record = asRecord(value);
  if (!record) return null;
  return {
    batchId: readValue(record.batchId),
    accountId: readValue(record.accountId),
    bannerId: readValue(record.bannerId),
    resultStatus: readValue(record.resultStatus),
    idempotencyReplayKnown: booleanValue(record.idempotencyReplayKnown),
    pullCount: numberValue(record.pullCount),
    cost: mapCost(record.cost),
    prePityState: record.prePityState ?? null,
    postPityState: record.postPityState ?? null,
    guaranteeTypes: guaranteeTypeList(record.guaranteeTypes),
    createdAt: readValue(record.createdAt)
  };
}

export function mapGachaPullBatchListResponse(response: unknown): GachaPullBatchListResponse {
  const record = asRecord(response) ?? {};
  return {
    permission: readValue(record.permission, "SUPPORT_READ"),
    permissionMode: readValue(record.permissionMode, "ROLE_PERMISSION"),
    items: Array.isArray(record.items)
      ? record.items.map(mapBatch).filter((item): item is GachaPullBatchSummary => Boolean(item))
      : [],
    nextCursor: typeof record.nextCursor === "string" ? record.nextCursor : null
  };
}

function duplicateConversionValue(value: unknown): string {
  const record = asRecord(value);
  if (!record) return "없음";
  const pattern = readValue(record.persistedTokenPattern);
  const quantity = numberValue(record.quantity);
  return pattern === "없음" ? "없음" : `${pattern} x${quantity}`;
}

function mapResult(value: unknown): GachaPullResultSummary | null {
  const record = asRecord(value);
  if (!record) return null;
  const reward = asRecord(record.reward);
  return {
    resultId: readValue(record.resultId),
    pullSequence: numberValue(record.pullSequence),
    resultRarity: readValue(record.resultRarity),
    resultType: readValue(record.resultType),
    rewardId: readValue(reward?.id),
    rewardQuantity: typeof reward?.quantity === "number" && Number.isSafeInteger(reward.quantity) && reward.quantity >= 0 ? reward.quantity : null,
    isPickup: booleanValue(record.isPickup),
    wasDuplicate: booleanValue(record.wasDuplicate),
    guaranteeType: guaranteeTypeValue(record.guaranteeType),
    duplicateConversion: duplicateConversionValue(record.duplicateConversion),
    itemLedgerRef: readValue(record.itemLedgerRef),
    operatorLedgerRef: readValue(record.operatorLedgerRef)
  };
}

export function mapGachaPullBatchDetailResponse(response: unknown): GachaPullBatchDetailResponse {
  const record = asRecord(response) ?? {};
  const item = asRecord(record.item);
  const evidence = asRecord(item?.causeEvidence);
  const batch = mapBatch(item) ?? {
    batchId: "없음",
    accountId: "없음",
    bannerId: "없음",
    resultStatus: "없음",
    idempotencyReplayKnown: "없음",
    pullCount: "없음",
    cost: { itemId: "없음", quantity: "없음", itemLedgerRef: "없음" },
    prePityState: null,
    postPityState: null,
    guaranteeTypes: [],
    createdAt: "없음"
  };
  return {
    item: {
      ...batch,
      causeEvidence: evidence?.source === "gacha_pull_batch_server_records" ? {
        source: evidence.source,
        duplicateResultCount: typeof evidence.duplicateResultCount === "number" && Number.isSafeInteger(evidence.duplicateResultCount) && evidence.duplicateResultCount >= 0 ? evidence.duplicateResultCount : null,
        idempotencyReplayKnown: typeof evidence.idempotencyReplayKnown === "boolean" ? evidence.idempotencyReplayKnown : null,
        guaranteeTypes: stringList(evidence.guaranteeTypes)
      } : null,
      itemLedgerRefs: stringList(item?.itemLedgerRefs),
      operatorLedgerRefs: stringList(item?.operatorLedgerRefs),
      results: Array.isArray(item?.results)
        ? item.results.map(mapResult).filter((result): result is GachaPullResultSummary => Boolean(result))
        : []
    },
    detailAvailable: record.detailAvailable === true
  };
}
