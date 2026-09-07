import { readFileSync } from "node:fs";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { GachaHistory } from "./GachaHistory";
import { mapGachaPullBatchDetailResponse, mapGachaPullBatchListResponse } from "../shared/gacha/gachaContract";

const fixture = JSON.parse(readFileSync(new URL("../../scripts/gacha-approved-21-local.json", import.meta.url), "utf8"));
const details: ReturnType<typeof mapGachaPullBatchDetailResponse>[] = fixture.details.map(mapGachaPullBatchDetailResponse);
const initial = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira"];
const candidates = [...initial, "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris", "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden", "op_signal_runner", "cur_operator_token", "mat_rare_manual", "mat_scrap", "mat_work_skill_basic", "mat_tactical_data_pack"];

describe("approved21 actual local Admin projection, not remote evidence", () => {
  it("maps all21 candidates and28 batch references including the initial five", () => {
    expect(fixture.status).toBe("APPROVED_21_POLICY_LOCAL_API_DB_VERIFIED_NOT_REMOTE");
    const list = mapGachaPullBatchListResponse(fixture.list);
    expect(list.items).toHaveLength(28);
    expect(details).toHaveLength(28);
    const results = details.flatMap(d => d.item.results);
    expect(new Set(results.map(r => r.rewardId))).toEqual(new Set(candidates));
    for (const d of details) {
      expect(list.items.some(b => b.batchId === d.item.batchId)).toBe(true);
      expect(d.detailAvailable).toBe(true);
      expect(d.item.results).toHaveLength(1);
      expect(d.item.results[0].resultId).toMatch(/^gacha_result_/);
    }
    expect(results.filter(r => r.wasDuplicate === "예")).toHaveLength(7);
    for (const id of [...initial, "op_vex", "op_signal_runner"]) {
      const r = results.find(r => r.rewardId === id && r.wasDuplicate === "예")!;
      const quantity = id === "op_signal_runner" ? 30 : 50;
      expect(r.rewardQuantity).toBe(quantity);
      expect(r.duplicateConversion).toBe(`cur_operator_token:${id} x${quantity}`);
    }
    expect(results.find(r => r.resultType === "RARE_OPERATOR_TOKEN")?.rewardQuantity).toBe(10);
    expect(results.find(r => r.resultType === "TACTICAL_MATERIAL")?.rewardQuantity).toBe(1);
  });
  it("renders stored quantities and both newly supported result labels", () => {
    const client = new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, retry: false } } });
    client.setQueryData(["gacha-history", "", "", ""], mapGachaPullBatchListResponse(fixture.list));
    client.setQueryData(["gacha-banners"], { items: [] });
    for (const d of details) client.setQueryData(["gacha-pull-batch-detail", d.item.batchId], d);
    try {
      const html = renderToStaticMarkup(createElement(QueryClientProvider, { client }, createElement(MemoryRouter, null, createElement(GachaHistory))));
      for (const id of candidates) expect(html).toContain(id);
      for (const label of ["희귀 요원 증표", "전술 재료", "획득 수량: 10", "획득 수량: 1", "변환 수량: 50", "변환 수량: 30"]) expect(html).toContain(label);
      expect(html).not.toContain("수량: 미기록");
      expect(html).not.toContain("조회 중");
      expect(html).not.toContain("<img");
    } finally { client.clear(); }
  });
});
