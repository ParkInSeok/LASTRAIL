import { readFileSync } from "node:fs";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { GachaHistory } from "./GachaHistory";
import { mapGachaPullBatchDetailResponse, mapGachaPullBatchListResponse } from "../shared/gacha/gachaContract";

// Actual isolated Hono Admin projections; supported subset only, not shipping approval.
const fixture = JSON.parse(readFileSync(new URL("../../scripts/gacha-supported-subset-diagnostic.json", import.meta.url), "utf8"));
const details: ReturnType<typeof mapGachaPullBatchDetailResponse>[] = fixture.details.map(mapGachaPullBatchDetailResponse);
const candidates = ["op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris", "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden", "op_signal_runner", "mat_rare_manual", "mat_scrap", "mat_work_skill_basic"];

describe("supported-subset Admin projection diagnostic, not shipping pool", () => {
  it("preserves all fourteen supplied candidate IDs and sixteen batch references", () => {
    expect(fixture.status).toBe("DIAGNOSTIC_SUPPORTED_SUBSET_NOT_SHIPPING");
    expect(fixture.syntheticLocalAccountOnly).toBe(true);
    const list = mapGachaPullBatchListResponse(fixture.list);
    expect(list.items).toHaveLength(16);
    expect(details).toHaveLength(16);
    expect(new Set(details.flatMap(d => d.item.results.map(r => r.rewardId)))).toEqual(new Set(candidates));
    for (const detail of details) {
      expect(list.items.some(b => b.batchId === detail.item.batchId)).toBe(true);
      expect(detail.detailAvailable).toBe(true);
      expect(detail.item.results).toHaveLength(1);
      expect(detail.item.results[0].resultId).toMatch(/^gacha_result_/);
      expect(detail.item.results[0].pullSequence).toBe("1");
    }
  });

  it("keeps duplicate candidate identity separate from conversion and quantity", () => {
    const duplicates = details.flatMap(d => d.item.results).filter(r => r.wasDuplicate === "예");
    expect(duplicates).toHaveLength(2);
    expect(duplicates.map(r => [r.rewardId, r.duplicateConversion]).sort()).toEqual([
      ["op_signal_runner", "cur_operator_token:op_signal_runner x30"],
      ["op_vex", "cur_operator_token:op_vex x50"]
    ]);
  });

  it("renders the real history component with all detail query projections", () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, retry: false } } });
    queryClient.setQueryData(["gacha-history", "", "", ""], mapGachaPullBatchListResponse(fixture.list));
    queryClient.setQueryData(["gacha-banners"], { items: [] });
    for (const detail of details) queryClient.setQueryData(["gacha-pull-batch-detail", detail.item.batchId], detail);
    try {
      const html = renderToStaticMarkup(createElement(QueryClientProvider, { client: queryClient }, createElement(MemoryRouter, null, createElement(GachaHistory))));
      for (const id of candidates) expect(html).toContain(id);
      expect(html).toContain("cur_operator_token:op_signal_runner x30");
      expect(html).toContain("cur_operator_token:op_vex x50");
      expect(html).not.toContain("연동 대기");
      expect(html).not.toContain("조회 중");
      // No catalog was supplied; do not invent labels or images.
      expect(html).not.toContain("<img");
    } finally { queryClient.clear(); }
  });

  it("retains unknown safe IDs/codes without claiming unsupported types are eligible", () => {
    const response = structuredClone(fixture.details[0]);
    response.item.results = [{ resultId: "unknown-row", reward: { id: "future_candidate" }, resultType: "UNKNOWN_TYPE", resultRarity: "UNKNOWN_RARITY", guaranteeType: "FUTURE_GUARANTEE" }];
    const result = mapGachaPullBatchDetailResponse(response).item.results[0];
    expect(result.rewardId).toBe("future_candidate");
    expect(result.resultType).toBe("UNKNOWN_TYPE");
    expect(result.resultRarity).toBe("UNKNOWN_RARITY");
    expect(result.guaranteeType).toContain("FUTURE_GUARANTEE");
    expect(result.duplicateConversion).toBe("없음");
    expect(details.flatMap(d => d.item.results).some(r => ["RARE_OPERATOR_TOKEN", "TACTICAL_MATERIAL"].includes(r.resultType))).toBe(false);
  });
});
