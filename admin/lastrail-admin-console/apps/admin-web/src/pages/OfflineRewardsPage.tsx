import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { adminApi } from "../shared/api/client";
import { adminReadStatusLabel } from "../shared/api/errorDisplay";
import {
  accountStatusLabel,
  offlineRewardStateSourceLabel,
  opsErrorCodeLabel,
  permissionLabel,
  storyProgressLabel
} from "../shared/labels/adminLabels";
import {
  emptyOfflineRewardDetail,
  noDataOfflineRewardDetail,
  offlineRewardOperationsPolicy,
  offlineRewardOperationsSeverityMappings,
  offlineRewardReadFilterFields,
  type OfflineRewardReadModelItem
} from "../shared/offlineRewards/offlineRewardContract";

type OfflineRewardDetailPlaceholder = {
  label: string;
  field: string;
  value: "계정 선택 대기";
};

export const offlineRewardReadOnlyFilters = [...offlineRewardReadFilterFields];

export const offlineRewardFilterStatus = "필터 기준 준비";

export const offlineRewardListColumns = [
  "계정",
  "상태",
  "스토리 진행도",
  "state source",
  "마지막 수령 기준",
  "마지막 claim",
  "revision",
  "read-only"
];

export const offlineRewardDetailPlaceholders: OfflineRewardDetailPlaceholder[] = [
  { label: "상태 출처", field: "offlineRewardState.source", value: "계정 선택 대기" },
  { label: "마지막 수령 기준", field: "offlineRewardState.lastOfflineRewardClaimAt", value: "계정 선택 대기" },
  { label: "마지막 스토리 진행도", field: "offlineRewardState.lastStoryProgressId", value: "계정 선택 대기" },
  { label: "마지막 밸런스 설정 버전", field: "offlineRewardState.lastBalanceConfigVersion", value: "계정 선택 대기" },
  { label: "마지막 내러티브 버전", field: "offlineRewardState.lastNarrativeVersion", value: "계정 선택 대기" },
  { label: "마지막 미리보기 시각", field: "offlineRewardState.lastPreviewedAt", value: "계정 선택 대기" },
  { label: "리비전", field: "offlineRewardState.revision", value: "계정 선택 대기" },
  { label: "갱신 시각", field: "offlineRewardState.updatedAt", value: "계정 선택 대기" },
  { label: "오프라인 보상 수령 ID", field: "lastClaim.offlineRewardClaimId", value: "계정 선택 대기" },
  { label: "수령 시각", field: "lastClaim.claimedAt", value: "계정 선택 대기" }
];

export const offlineRewardSupportFlags = [
  "supportContext.readOnly=true",
  "supportContext.claimMutationEnabled=false",
  "supportContext.mutationEndpointsEnabled=false"
];

export const offlineRewardOperationsPolicyCards = offlineRewardOperationsPolicy.map((policy) => ({
  ...policy,
  value: "정책 기준" as const
}));

export const offlineRewardOperationsSeverityCards = offlineRewardOperationsSeverityMappings.map((mapping) => ({
  ...mapping,
  label: opsErrorCodeLabel(mapping.errorCode),
  value: "운영 분류 기준" as const
}));

function nullableDisplay(value: string | null, fallback = "없음"): string {
  return value ?? fallback;
}

function accountStatusDisplay(status: string): string {
  return ["ACTIVE", "SUSPENDED", "BANNED", "DELETED"].includes(status)
    ? accountStatusLabel(status as Parameters<typeof accountStatusLabel>[0])
    : status;
}

function displayItem(item: OfflineRewardReadModelItem): OfflineRewardReadModelItem {
  return item;
}

export function OfflineRewardsPage() {
  const query = useQuery({
    queryKey: ["offline-rewards"],
    queryFn: () => adminApi.offlineRewardList({}),
    retry: false
  });
  const hasReadItems = Boolean(query.data?.items.length);
  const items = hasReadItems
    ? query.data?.items.map(displayItem) ?? []
    : [query.data ? noDataOfflineRewardDetail() : emptyOfflineRewardDetail()];

  return (
    <main className="content">
      <header>
        <h1>오프라인 보상</h1>
        <p>계정별 오프라인 보상 상태와 마지막 수령 참조를 조회하는 읽기 전용 화면</p>
      </header>
      <section className="summary-grid" aria-label="오프라인 보상 조회 요약">
        <div>
          <span>권한</span>
          <strong>{permissionLabel(query.data?.permission ?? "OFFLINE_REWARD_READ")}</strong>
          <small>OFFLINE_REWARD_READ</small>
        </div>
        <div>
          <span>권한 모드</span>
          <strong>{query.data?.permissionMode ?? "ROLE_PERMISSION"}</strong>
          <small>ROLE_PERMISSION</small>
        </div>
        <div>
          <span>연동 상태</span>
          <strong>
            {adminReadStatusLabel({
              hasData: Boolean(query.data),
              isError: query.isError,
              isLoading: query.isLoading
            })}
          </strong>
          <small>API 실패 시 placeholder 유지</small>
        </div>
        <div>
          <span>mutation</span>
          <strong>비활성</strong>
          <small>{offlineRewardSupportFlags.join(" / ")}</small>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>필터</h2>
          <p>Admin API filter contract 후보입니다</p>
        </header>
        <div className="metric-grid">
          {offlineRewardReadOnlyFilters.map((filter) => (
            <div key={filter} className="metric-card">
              <span>{filter}</span>
              <strong>{offlineRewardFilterStatus}</strong>
              <small>read-only query parameter</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>목록</h2>
          <p>Admin API `/v1/admin/offline-rewards` 응답을 읽기 전용으로 표시합니다</p>
        </header>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                {offlineRewardListColumns.map((column) => (
                  <th key={column}>{column}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.accountId}>
                  <td>
                    <Link to={`/support/accounts/${item.accountId}`}>{item.displayName}</Link>
                    <small>{item.accountId}</small>
                  </td>
                  <td>{accountStatusDisplay(item.accountStatus)}</td>
                  <td>{storyProgressLabel(item.storyProgressId)}</td>
                  <td>{offlineRewardStateSourceLabel(item.offlineRewardState.source)}</td>
                  <td>{item.offlineRewardState.lastOfflineRewardClaimAt}</td>
                  <td>{item.lastClaim.offlineRewardClaimId}</td>
                  <td>{item.offlineRewardState.revision}</td>
                  <td>{item.supportContext.readOnly ? "읽기 전용" : "확인 필요"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>상세 placeholder</h2>
          <p>목록에서 계정을 선택하기 전까지 상세 필드 구조만 표시합니다</p>
        </header>
        <div className="metric-grid">
          {offlineRewardDetailPlaceholders.map((placeholder) => (
            <div key={placeholder.field} className="metric-card">
              <span>{placeholder.label}</span>
              <strong>{placeholder.value}</strong>
              <small>{placeholder.field}</small>
            </div>
          ))}
          <div className="metric-card">
            <span>lastPreviewedAt</span>
            <strong>{nullableDisplay(items[0]?.offlineRewardState.lastPreviewedAt ?? null, hasReadItems ? "없음" : "연동 대기")}</strong>
            <small>현재 route 갱신 없음</small>
          </div>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>운영 정책</h2>
          <p>서버 기록 기준의 오프라인 보상 운영 정책을 read-only로 표시합니다</p>
        </header>
        <div className="metric-grid">
          {offlineRewardOperationsPolicyCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.note}</small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>운영 분류 후보</h2>
          <p>expected/info, needs review, high risk로 분리되는 support/ops 판단 placeholder입니다</p>
        </header>
        <div className="metric-grid">
          {offlineRewardOperationsSeverityCards.map((card) => (
            <div key={card.errorCode} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>{card.errorCode} / {card.severity} / {card.category}</small>
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}
