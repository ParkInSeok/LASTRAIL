import { describe, expect, it } from "vitest";
import { mapGachaPullBatchDetailResponse } from "./gachaContract";
describe("stored gacha cause references", () => {
  it("keeps safe evidence without treating it as a diagnosed cause", () => {
    const item = mapGachaPullBatchDetailResponse({ item: { causeEvidence: { source: "gacha_pull_batch_server_records", duplicateResultCount: 10, idempotencyReplayKnown: false, guaranteeTypes: ["NONE"], rawRng: "forbidden" } } }).item;
    expect(item).toHaveProperty("causeEvidence", { source: "gacha_pull_batch_server_records", duplicateResultCount: 10, idempotencyReplayKnown: false, guaranteeTypes: ["NONE"] });
    expect(JSON.stringify(item)).not.toContain("forbidden");
  });
});
