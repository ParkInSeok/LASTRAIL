import {
  gachaErrorCodeLabel,
  gachaGuaranteeTypeLabel,
  gachaResultStatusLabel
} from "../shared/labels/adminLabels";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { adminApi } from "../shared/api/client";
import { adminRequestFailureDisplay } from "../shared/api/errorDisplay";
import type {
  GachaBannerListResponse,
  GachaPityResponse,
  GachaPullBatchDetailResponse,
  GachaPullBatchListResponse
} from "../shared/gacha/gachaContract";

type GachaReadOnlySection = {
  title: string;
  status: string;
  description: string;
  fields: string[];
};

type GachaLaunchSupportCard = {
  code: string;
  label: string;
  value: string;
  note: string;
};

const labeledCode = (label: string, code: string) => `${label} (${code})`;

export const gachaSupportPullCountVisibility = ["1-pull", "10-pull"] as const;

export const actOneEliteOperatorDraftBoundary = {
  scopeStatus: "TESTING_TUNING_DRAFT",
  productionFinal: false,
  seedPromotionApproved: false
} as const;

export const actOneEliteOperatorIdsDraft = [
  "op_vex",
  "op_nyx",
  "op_sable",
  "op_marrow",
  "op_iris",
  "op_rail",
  "op_anchor",
  "op_lumen",
  "op_cinder",
  "op_warden"
] as const;

export const gachaDuplicateConversionSupportFields = [
  "displayItemId",
  "persistedTokenPattern",
  "rareConversionQuantityCandidate",
  "eliteConversionQuantityCandidate"
] as const;

export const gachaLaunchResultFields = [
  "pullSequence",
  "resultRarity",
  "resultType",
  "resultId",
  "reward",
  "isPickup",
  "wasDuplicate",
  "duplicateConversion"
] as const;

export const gachaLaunchTicketAcquisitionPolicy = {
  totalNonRepeatTickets: 12,
  costItemId: "cur_command_ticket",
  offlineRepeatIncludesCommandTicket: false,
  excludedOfflineRepeatRewards: ["cur_command_ticket", "cur_command_ticket_fragment", "cur_special_core"],
  boundary: "launch candidate/draft, not production seed/config promotion",
  firstClearTickets: { quantity: 7, state: "promoted_local_launch_config" },
  loginBonusTickets: { quantity: 5, state: "support_summary_read_model_available" },
  sources: [
    {
      code: "launch_login_bonus",
      label: "login bonus",
      quantity: 5,
      repeatable: false,
      state: "support_summary_read_model_available"
    },
    {
      code: "opn_03_water_stop_first_clear",
      label: "opn_03_water_stop first clear",
      quantity: 1,
      repeatable: false,
      state: "promoted_local_launch_config"
    },
    {
      code: "opn_05_night_boarders_first_clear",
      label: "opn_05_night_boarders first clear",
      quantity: 1,
      repeatable: false,
      state: "promoted_local_launch_config"
    },
    {
      code: "opn_06_broken_line_first_clear",
      label: "opn_06_broken_line first clear",
      quantity: 2,
      repeatable: false,
      state: "promoted_local_launch_config"
    },
    {
      code: "raid_raider_king_engine_story_raid_first_clear",
      label: "raid_raider_king_engine story raid first clear",
      quantity: 3,
      repeatable: false,
      state: "promoted_local_launch_config"
    }
  ]
} as const;

export const gachaLaunchSupportVisibilityCards: GachaLaunchSupportCard[] = [
  { code: "activeBannerId", label: "활성 배너 ID", value: "연동 대기", note: "active banner id read-only 후보" },
  { code: "bannerType", label: "배너 유형", value: "연동 대기", note: "PICKUP/non-PICKUP banner type 후보" },
  { code: "pickupOperatorId", label: "픽업 요원 ID", value: "연동 대기", note: "pickup operator stable id 후보" },
  {
    code: "pullCountVisibility",
    label: "모집 횟수",
    value: "연동 대기",
    note: gachaSupportPullCountVisibility.join(" / ")
  },
  {
    code: "pityThresholds",
    label: "천장 기준",
    value: "연동 대기",
    note: "rare 10 / elite 30 / pickup 60 draft/contract candidate, live economy truth 아님"
  },
  {
    code: "accountPityState",
    label: "계정별 천장 상태",
    value: "계정 ID 필요",
    note: "accountId querystring이 있으면 Admin pity read model로 조회"
  },
  {
    code: "guaranteeType",
    label: "보장 발동",
    value: "연동 대기",
    note: ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"].join(" / ")
  },
  {
    code: "duplicateConversion",
    label: "중복 변환",
    value: "연동 대기",
    note: gachaDuplicateConversionSupportFields.join(" / ")
  },
  { code: "resultFields", label: "결과 필드", value: "연동 대기", note: gachaLaunchResultFields.join(" / ") },
  {
    code: "ticketAcquisition",
    label: "지휘권 획득처",
    value: "연동 대기",
    note: [
      `launch non-repeat total ${gachaLaunchTicketAcquisitionPolicy.totalNonRepeatTickets}`,
      `first-clear ${gachaLaunchTicketAcquisitionPolicy.firstClearTickets.quantity} ${gachaLaunchTicketAcquisitionPolicy.firstClearTickets.state}`,
      `login bonus ${gachaLaunchTicketAcquisitionPolicy.loginBonusTickets.quantity} ${gachaLaunchTicketAcquisitionPolicy.loginBonusTickets.state}`,
      `cost=${gachaLaunchTicketAcquisitionPolicy.costItemId}`,
      gachaLaunchTicketAcquisitionPolicy.boundary,
      ...gachaLaunchTicketAcquisitionPolicy.sources.map(
        (source) => `${source.label} ${source.quantity} repeatable=${source.repeatable}${"state" in source ? ` state=${source.state}` : ""}`
      ),
      `offline repeat reward excludes ${gachaLaunchTicketAcquisitionPolicy.excludedOfflineRepeatRewards.join(" / ")}`
    ].join(" / ")
  },
  {
    code: "actOneEliteVisualScope",
    label: "Act 1 정예 요원 10인",
    value: "TESTING_TUNING_DRAFT",
    note: [
      `productionFinal=${actOneEliteOperatorDraftBoundary.productionFinal}`,
      `seedPromotionApproved=${actOneEliteOperatorDraftBoundary.seedPromotionApproved}`,
      ...actOneEliteOperatorIdsDraft
    ].join(" / ")
  }
];

export function gachaLaunchSupportCardsForReadState(state: {
  banners?: GachaBannerListResponse;
  pity?: GachaPityResponse;
  batches?: GachaPullBatchListResponse;
  detail?: GachaPullBatchDetailResponse | null;
  isLoading: boolean;
  error: unknown;
}): GachaLaunchSupportCard[] {
  if (state.error) {
    const failure = adminRequestFailureDisplay(state.error, {
      resourceLabel: "요원 모집",
      resourceId: "/api/gacha/*"
    });
    return gachaLaunchSupportVisibilityCards.map((card) =>
      card.value === "TESTING_TUNING_DRAFT" ? card : { ...card, value: failure.reason }
    );
  }
  if (state.isLoading) {
    return gachaLaunchSupportVisibilityCards.map((card) =>
      card.value === "TESTING_TUNING_DRAFT" ? card : { ...card, value: "조회 중" }
    );
  }

  const activeBanner = state.banners?.items.find((item) => item.bannerId === state.banners?.activeBannerId);
  const latestBatch = state.batches?.items[0];
  const latestResult = state.detail?.item.results[0];

  return gachaLaunchSupportVisibilityCards.map((card) => {
    if (card.code === "activeBannerId") return { ...card, value: state.banners?.activeBannerId ?? "없음" };
    if (card.code === "bannerType") return { ...card, value: activeBanner?.bannerType ?? "없음" };
    if (card.code === "pickupOperatorId") return { ...card, value: activeBanner?.pickupOperatorId ?? "없음" };
    if (card.code === "pullCountVisibility") {
      return { ...card, value: activeBanner?.pullCountOptions.join(" / ") ?? "없음" };
    }
    if (card.code === "pityThresholds") {
      const thresholds = activeBanner?.pityThresholds;
      return {
        ...card,
        value: thresholds
          ? `rare ${thresholds.rareOrHigher} / elite ${thresholds.elite} / pickup ${thresholds.pickupElite}`
          : "없음"
      };
    }
    if (card.code === "accountPityState") {
      const pity = state.pity?.pityState;
      return {
        ...card,
        value: pity
          ? `${state.pity?.accountId} / ${state.pity?.bannerId} / total=${pity.totalPullCount} / elite=${pity.elitePityCount} / pickup=${pity.pickupElitePityCount} / rare=${pity.rareOrHigherPityCount}`
          : "계정 ID 필요"
      };
    }
    if (card.code === "guaranteeType") {
      return { ...card, value: latestBatch?.guaranteeTypes.join(" / ") || activeBanner?.guaranteeTypes.join(" / ") || "없음" };
    }
    if (card.code === "duplicateConversion") {
      return { ...card, value: latestResult?.duplicateConversion ?? "데이터 없음" };
    }
    if (card.code === "resultFields") {
      return {
        ...card,
        value: latestBatch
          ? `${latestBatch.batchId} / ${latestBatch.pullCount} pulls / ${latestBatch.resultStatus}`
          : "데이터 없음"
      };
    }
    if (card.code === "ticketAcquisition") return { ...card, value: "support summary/read config 연결됨" };
    return card;
  });
}

export const gachaReadOnlySections: GachaReadOnlySection[] = [
  {
    title: "배너 상태",
    status: "조회 가능",
    description: "운영 중인 요원 모집 배너와 픽업/비픽업 설정을 확인합니다.",
    fields: ["배너 ID", "배너 유형", "시작/종료 시각", "확률표 버전", "활성 여부"]
  },
  {
    title: "계정별 천장 상태",
    status: "계정 ID 필요",
    description: "계정과 배너별 pity/guarantee state를 문의 대응용으로 조회합니다.",
    fields: ["총 모집 횟수", "정예 천장 카운트", "픽업 정예 천장 카운트", "희귀 이상 천장 카운트"]
  },
  {
    title: "뽑기 이력",
    status: "조회 가능",
    description: "pull batch/request와 개별 pull result를 순서대로 추적합니다.",
    fields: [
      "batch ID",
      "Idempotency-Key",
      "소모 지휘권",
      "개별 결과",
      labeledCode(gachaResultStatusLabel("ACCEPTED"), "ACCEPTED"),
      labeledCode(gachaResultStatusLabel("DUPLICATE_REPLAY"), "DUPLICATE_REPLAY"),
      labeledCode(gachaResultStatusLabel("REJECTED"), "REJECTED")
    ]
  },
  {
    title: "보장 발동",
    status: "조회 가능",
    description: "서버가 확정한 guaranteeType을 표시하고 클라이언트 계산값은 사용하지 않습니다.",
    fields: [
      labeledCode(gachaGuaranteeTypeLabel("RARE_OR_HIGHER_10"), "RARE_OR_HIGHER_10"),
      labeledCode(gachaGuaranteeTypeLabel("ELITE_30"), "ELITE_30"),
      labeledCode(gachaGuaranteeTypeLabel("PICKUP_ELITE_60"), "PICKUP_ELITE_60"),
      labeledCode(gachaGuaranteeTypeLabel("NONE"), "NONE")
    ]
  },
  {
    title: "거절/오류 코드",
    status: "조회 가능",
    description: "문의 대응과 진단에 필요한 gacha errorCode를 한국어로 표시합니다.",
    fields: [
      labeledCode(gachaErrorCodeLabel("insufficient_command_ticket"), "insufficient_command_ticket"),
      labeledCode(gachaErrorCodeLabel("auth_required"), "auth_required"),
      labeledCode(gachaErrorCodeLabel("banner_inactive"), "banner_inactive"),
      labeledCode(gachaErrorCodeLabel("invalid_pull_count"), "invalid_pull_count"),
      labeledCode(gachaErrorCodeLabel("contract_violation"), "contract_violation"),
      labeledCode(gachaErrorCodeLabel("gacha_pool_unavailable"), "gacha_pool_unavailable"),
      labeledCode(gachaErrorCodeLabel("gacha_state_conflict"), "gacha_state_conflict"),
      labeledCode(gachaErrorCodeLabel("gacha_temporarily_unavailable"), "gacha_temporarily_unavailable")
    ]
  },
  {
    title: "중복/조각 변환",
    status: "조회 가능",
    description: "중복 요원 여부와 조각 변환 결과를 확인합니다.",
    fields: ["요원 ID", "중복 여부", "변환 조각", "duplicate_conversion_json"]
  },
  {
    title: "아이템 원장 연결",
    status: "조회 가능",
    description: "지휘권 소모와 조각/증표/재료 지급을 item ledger 기준으로 추적합니다.",
    fields: ["item_ledger ID", "소모/지급 수량", "연결 batch", "복구/문의 참조"]
  }
];

export function GachaPage() {
  const [searchParams] = useSearchParams();
  const accountId = searchParams.get("accountId") ?? "";
  const bannerId = searchParams.get("bannerId") ?? "";
  const bannersQuery = useQuery({
    queryKey: ["gacha-banners"],
    queryFn: () => adminApi.gachaBanners(),
    retry: false
  });
  const batchesQuery = useQuery({
    queryKey: ["gacha-pull-batches"],
    queryFn: () => adminApi.gachaPullBatches({ limit: "10" }),
    retry: false
  });
  const latestBatchId = batchesQuery.data?.items[0]?.batchId ?? "";
  const batchDetailQuery = useQuery({
    queryKey: ["gacha-pull-batch-detail", latestBatchId],
    queryFn: () => adminApi.gachaPullBatchDetail(latestBatchId),
    enabled: latestBatchId.length > 0,
    retry: false
  });
  const pityQuery = useQuery({
    queryKey: ["gacha-account-pity", accountId, bannerId],
    queryFn: () => adminApi.gachaAccountPity(accountId, bannerId.length > 0 ? bannerId : undefined),
    enabled: accountId.length > 0,
    retry: false
  });
  const launchSupportCards = gachaLaunchSupportCardsForReadState({
    banners: bannersQuery.data,
    pity: pityQuery.data,
    batches: batchesQuery.data,
    detail: batchDetailQuery.data ?? null,
    isLoading: bannersQuery.isLoading || batchesQuery.isLoading || batchDetailQuery.isLoading || pityQuery.isLoading,
    error: bannersQuery.error ?? batchesQuery.error ?? batchDetailQuery.error ?? pityQuery.error
  });

  return (
    <main className="content">
      <header>
        <h1>요원 모집</h1>
        <p>배너, 천장, 뽑기 이력, 중복 변환, 아이템 원장을 조회하는 읽기 전용 준비 화면</p>
      </header>
      <section className="summary-grid" aria-label="요원 모집 운영 요약">
        <div>
          <span>30뽑 보장</span>
          <strong>정예 확정</strong>
          <small>기획 확정 기준</small>
        </div>
        <div>
          <span>60뽑 보장</span>
          <strong>픽업 확정</strong>
          <small>기획 확정 기준</small>
        </div>
        <div>
          <span>기록 저장</span>
          <strong>필수</strong>
          <small>batch/result 단위 추적</small>
        </div>
        <div>
          <span>상태</span>
          <strong>{bannersQuery.data || batchesQuery.data ? "조회 가능" : bannersQuery.isLoading ? "조회 중" : "연동 대기"}</strong>
          <small>Admin gacha read-only API</small>
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>Launch CS 가시성</h2>
          <p>Gacha launch 문의 대응에 필요한 read-only 후보만 표시하고 수동 복구/재추첨은 제공하지 않습니다</p>
        </header>
        <div className="metric-grid">
          {launchSupportCards.map((card) => (
            <div key={card.code} className="metric-card">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
              <small>
                {card.code} / {card.note}
              </small>
            </div>
          ))}
        </div>
      </section>
      <div className="dashboard-sections">
        {gachaReadOnlySections.map((section) => (
          <section key={section.title} className="admin-panel">
            <header>
              <h2>{section.title}</h2>
              <p>{section.description}</p>
            </header>
            <div className="metric-grid">
              {section.fields.map((field) => (
                <div key={field} className="metric-card">
                  <span>{field}</span>
                  <strong>{section.status}</strong>
                  <small>서버 migration/API 확정 후 연결</small>
                </div>
              ))}
            </div>
          </section>
        ))}
      </div>
    </main>
  );
}
