import { expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { BannerPolicy } from "./BannerPolicy";
import { mapGachaBannerListResponse } from "./gachaContract";

it("renders only queried banner policy, keeping unknown status and zero distinct from missing", () => {
  const data = mapGachaBannerListResponse({ items: [{ bannerId: "approved-reference", bannerType: "PICKUP", status: "FUTURE", startsAt: "2026-09-10T00:00:00Z", endsAt: null, pityThresholds: { elite: 0 }, pullCountOptions: [1, 10] }] });
  const html = renderToStaticMarkup(<BannerPolicy data={data} loading={false} error={null} />);
  expect(html).toContain("approved-reference");
  expect(html).toContain("FUTURE");
  expect(html).toContain("2026-09-10T00:00:00Z");
  expect(html).toContain("없음");
  expect(html).toContain("정예: 0");
  expect(html).not.toMatch(/<button|rawRng|probability|승인 완료/);
});
it("distinguishes loading, failure and successful empty policy", () => {
  expect(renderToStaticMarkup(<BannerPolicy loading error={null} />)).toContain("조회 중");
  expect(renderToStaticMarkup(<BannerPolicy loading={false} error={new Error("failed")} />)).toContain("배너 조회 실패");
  expect(renderToStaticMarkup(<BannerPolicy loading={false} error={null} data={mapGachaBannerListResponse({ items: [] })} />)).toContain("배너 없음");
});
