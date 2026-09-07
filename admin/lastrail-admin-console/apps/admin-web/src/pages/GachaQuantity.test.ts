import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { GachaHistory } from "./GachaHistory";
import { mapGachaPullBatchDetailResponse, mapGachaPullBatchListResponse } from "../shared/gacha/gachaContract";
import { gachaResultTypeLabel } from "../shared/labels/adminLabels";

const batch = { batchId: "quantity-contract", accountId: "local-contract", bannerId: "local-banner", pullCount: 1 };
const detailFor = (quantity: unknown, wasDuplicate = false) => mapGachaPullBatchDetailResponse({ detailAvailable: true, item: { ...batch, results: [{
  resultId: "result-row", resultType: wasDuplicate ? "ELITE_OPERATOR" : "RARE_OPERATOR_TOKEN", resultRarity: wasDuplicate ? "ELITE" : "RARE", reward: { id: wasDuplicate ? "op_vex" : "cur_operator_token", quantity }, wasDuplicate,
  duplicateConversion: wasDuplicate ? { persistedTokenPattern: "cur_operator_token:op_vex", quantity: 50 } : null
}] } });

function renderDetail(quantity: unknown, duplicate = false) {
  const client = new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, retry: false } } });
  client.setQueryData(["gacha-history", "", "", ""], mapGachaPullBatchListResponse({ items: [batch] }));
  client.setQueryData(["gacha-banners"], { items: [] });
  client.setQueryData(["gacha-pull-batch-detail", batch.batchId], detailFor(quantity, duplicate));
  try { return renderToStaticMarkup(createElement(QueryClientProvider, { client }, createElement(MemoryRouter, null, createElement(GachaHistory)))); }
  finally { client.clear(); }
}

describe("server-owned gacha quantity display", () => {
  it("preserves numeric quantity without deriving it from candidate or rarity", () => {
    for (const quantity of [0, 1, 10, 50]) expect(detailFor(quantity).item.results[0].rewardQuantity).toBe(quantity);
    for (const value of [null, undefined, "10", -1, 1.5, Infinity, Number.MAX_SAFE_INTEGER + 1]) expect(detailFor(value).item.results[0].rewardQuantity).toBeNull();
  });
  it("labels both newly supported types while retaining unknown codes", () => {
    expect(gachaResultTypeLabel("RARE_OPERATOR_TOKEN")).toBe("희귀 요원 증표");
    expect(gachaResultTypeLabel("TACTICAL_MATERIAL")).toBe("전술 재료");
    expect(gachaResultTypeLabel("FUTURE_TYPE")).toBe("FUTURE_TYPE");
  });
  it("shows stored ordinary quantity and explicitly identifies missing legacy quantity", () => {
    expect(renderDetail(10)).toContain("획득 수량: 10");
    expect(renderDetail(null)).toContain("획득 수량: 미기록");
    expect(renderDetail(10)).toContain("희귀 요원 증표");
  });
  it("does not describe a duplicate conversion as fifty operators", () => {
    const html = renderDetail(50, true);
    expect(html).toContain("변환 수량: 50");
    expect(html).toContain("cur_operator_token:op_vex x50");
    expect(html).not.toContain("획득 수량: 50");
  });
});
