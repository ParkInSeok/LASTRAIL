import { adminRequestFailureDisplay } from "../api/errorDisplay";
import type { GachaBannerListResponse } from "./gachaContract";

type Props = { data?: GachaBannerListResponse; loading: boolean; error: unknown };
const statusLabel = (value: string) => ({ ACTIVE: "활성", INACTIVE: "비활성", DISABLED: "사용 중지", ENDED: "종료" } as Record<string, string>)[value] ?? value;
export function BannerPolicy({ data, loading, error }: Props) {
  return <details>
    <summary>배너 운영 정보</summary>
    {loading ? <p role="status">조회 중</p> : error ? <p role="alert">배너 조회 실패 · {adminRequestFailureDisplay(error, { resourceLabel: "배너", resourceId: "/api/gacha/banners" }).reason}</p> : !data ? <p>조회 데이터 없음</p> : !data.items.length ? <p>배너 없음</p> :
      <div className="gacha-table-scroll"><table><thead><tr><th>배너 / 유형</th><th>상태</th><th>기간 (서버 시각)</th><th>픽업 요원</th><th>비용 / 횟수</th><th>보장 기준</th><th>밸런스 / 갱신 시각</th></tr></thead><tbody>
        {data.items.map(row => <tr key={row.bannerId}>
          <td>{row.bannerId}<br />{row.bannerType}</td>
          <td>{statusLabel(row.status)}<br /><small>{row.status}</small></td>
          <td>{row.startsAt}<br />{row.endsAt}</td>
          <td>{row.pickupOperatorId}</td>
          <td>{row.pullCostItemId} / {row.pullCostQuantity}<br />{row.pullCountOptions.join(" / ") || "없음"}</td>
          <td>희귀 이상: {row.pityThresholds.rareOrHigher}<br />정예: {row.pityThresholds.elite}<br />픽업 정예: {row.pityThresholds.pickupElite}</td>
          <td>{row.balanceVersion}<br />{row.updatedAt}</td>
        </tr>)}
      </tbody></table></div>}
  </details>;
}
