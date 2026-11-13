import { describe, expect, it } from "vitest";
import { overviewMetrics } from "./overviewMetrics";

describe("overview metrics", () => {
  it("does not treat sessions as daily users or fabricate monetization values", () => {
    const groups = overviewMetrics({ activeSessions: "12" });
    expect(groups.map(g => g.title)).toEqual(["접속자", "가입자", "광고", "패스 결제율", "가입자 대비 접속률"]);
    expect(groups[0].metrics.map(m => m.value)).toEqual(["집계 미연동", "12"]);
    expect(groups[2].metrics.find(m => m.label === "광고 수익")?.value).toBe("집계 미연동");
  });
  it("keeps zero distinct from unavailable and exposes query state only for connected data", () => {
    expect(overviewMetrics({ activeSessions: "0" })[0].metrics[1].value).toBe("0");
    expect(overviewMetrics({ loading: true })[0].metrics[1].value).toBe("조회 중");
    expect(overviewMetrics({ error: "인증 확인 필요" })[0].metrics[1].value).toBe("인증 확인 필요");
  });
});
