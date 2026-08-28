import { Link } from "react-router-dom";
import { storyProgressLabel } from "../shared/labels/adminLabels";
import {
  offlineRewardClaimContractFields,
  offlineRewardPreviewContractFields,
  offlineRewardStateContractFields
} from "../shared/offlineRewards/offlineRewardContract";

type DashboardMetric = {
  label: string;
  value: string;
  note: string;
};

type DashboardSection = {
  title: string;
  description: string;
  metrics: DashboardMetric[];
};

type DashboardShortcut = {
  label: string;
  href: string;
  description: string;
};

type LaunchDetailSupportCandidate = {
  label: string;
  target: string;
  status: "연동 대기";
  note: string;
};

type OfflineRewardStoryProgressPolicy = {
  progressId: string;
  label: string;
  multiplier: string;
  capHours: string;
};

export const offlineRewardFormulaPlaceholders = [
  "baseHourlyReward × storyProgressMultiplier × cappedOfflineHours",
  "claimableHours = min(actualOfflineHours, offlineStorageCapHours, maxClaimableHours)",
  "10분 미만 launch 기본 0 reward",
  "Risk I~V는 오프라인 보상에 직접 곱하지 않음"
];

export const offlineRewardStoryProgressPolicy: OfflineRewardStoryProgressPolicy[] = [
  ["none", "1.00x", "4h"],
  ["opn_01_last_departure", "1.15x", "5h"],
  ["opn_02_first_signal", "1.32x", "6h"],
  ["opn_03_water_stop", "1.52x", "7h"],
  ["opn_04_scrapstar_show", "1.75x", "8h"],
  ["opn_05_night_boarders", "2.05x", "9h"],
  ["opn_06_broken_line", "2.40x", "10h"],
  ["raid_raider_king_engine", "3.00x", "12h"]
].map(([progressId, multiplier, capHours]) => ({
  progressId,
  label: storyProgressLabel(progressId),
  multiplier,
  capHours
}));

export const offlineRewardSourceCandidates = [
  "cur_credits",
  "cur_machine_parts",
  "cur_tactical_data",
  "cur_operator_exp"
];

export const offlineRewardBlockedDefaultRewards = [
  "cur_special_core",
  "cur_command_ticket",
  "cur_command_ticket_fragment"
];

export const offlineRewardDashboardContractFields = [...offlineRewardPreviewContractFields];

export const offlineRewardStateDashboardFields = [...offlineRewardStateContractFields];

export const offlineRewardClaimDashboardFields = [
  "claimStatus",
  "offlineRewardClaimId",
  "rewardTransactionId",
  "economyTransactionId",
  "accountMutationId",
  "serverClaimedAt",
  "revision",
  "last_offline_reward_claim_at"
] as const;

export const offlineRewardClaimDashboardContractFields = [...offlineRewardClaimContractFields];

export const launchDashboardSections: DashboardSection[] = [
  {
    title: "인증/세션",
    description: "Google 로그인과 활성 세션 상태를 보는 launch 핵심 요약",
    metrics: [
      { label: "현재 동접자", value: "연동 대기", note: "concurrentUsers 후보" },
      { label: "활성 세션", value: "연동 대기", note: "activeSessions 후보" },
      { label: "최근 5분 접속", value: "연동 대기", note: "recentActiveAccounts5m 후보" },
      { label: "마지막 로그인 경과", value: "연동 대기", note: "lastLoginAgeBuckets 후보" }
    ]
  },
  {
    title: "CS/계정 이슈",
    description: "지원 문의와 계정 복구 확인이 필요한 항목",
    metrics: [
      { label: "지원 확인 필요 계정", value: "연동 대기", note: "support account summary 기준" },
      { label: "계정 복구 후보", value: "연동 대기", note: "guest to Google recovery evidence 후보" },
      { label: "Google 연동 계정", value: "연동 대기", note: "provider/link read-only 상태 후보" }
    ]
  },
  {
    title: "무결성 리스크",
    description: "launch 운영자가 먼저 봐야 하는 계정/출격 위험 신호",
    metrics: [
      { label: "긴급/높음 의심 계정", value: "연동 대기", note: "계정 단위 검토 큐" },
      { label: "체크섬 불일치", value: "연동 대기", note: "필수 콘텐츠 버전 검증" },
      { label: "불가능한 피해량/점수", value: "연동 대기", note: "보스 레이드/출격 검증" }
    ]
  },
  {
    title: "보상/모집 이슈",
    description: "보상 수령, 오프라인 보상, 모집 문의 대응 요약",
    metrics: [
      { label: "보상 수령 실패", value: "연동 대기", note: "claim 실패 이벤트" },
      { label: "오프라인 보상 상태", value: "연동 대기", note: "claim 가능/충돌/무결성 제한 후보" },
      { label: "모집 문의 후보", value: "연동 대기", note: "pity/batch/result read-only 후보" },
      { label: "중복 재생 확인", value: "연동 대기", note: "duplicate replay는 이전 결과 재조회" }
    ]
  },
  {
    title: "치명 시스템 오류",
    description: "핵심 서버 실패와 운영 확인이 필요한 런타임 오류",
    metrics: [
      { label: "치명 오류", value: "연동 대기", note: "ops error logs critical" },
      { label: "미확인 핵심 실패", value: "연동 대기", note: "open/acknowledged latest critical 후보" }
    ]
  }
];

export const dashboardDetailSections: DashboardSection[] = [
  {
    title: "광고/보상형 광고",
    description: "광고 시청과 보상 지급 흐름을 보는 운영 지표",
    metrics: [
      { label: "광고 시청 수", value: "연동 대기", note: "광고 시작 이벤트 기준" },
      { label: "광고 완료율", value: "연동 대기", note: "완료/시작 비율" },
      { label: "광고 보상 지급", value: "연동 대기", note: "보상 지급 성공" },
      { label: "보상 실패", value: "연동 대기", note: "광고 완료 후 지급 실패" },
      { label: "계정당 평균 시청", value: "연동 대기", note: "반복 시청 패턴 확인" },
      { label: "비정상 반복 의심", value: "연동 대기", note: "경계값/과다 시청 후보" }
    ]
  },
  {
    title: "재화/보상",
    description: "지휘권과 주요 재화 지급/소모 이상을 보는 지표",
    metrics: [
      { label: "지휘권 지급", value: "연동 대기", note: "보상/패스/운영 지급" },
      { label: "지휘권 소모", value: "연동 대기", note: "요원 모집/콘텐츠 소모" },
      { label: "크레딧 지급량", value: "연동 대기", note: "일일 총 지급량" },
      { label: "보상 수령 실패", value: "연동 대기", note: "claim 실패 이벤트" },
      { label: "미리보기 불일치", value: "연동 대기", note: "preview 대비 claim 차이" }
    ]
  },
  {
    title: "위험도/보상 불균형",
    description: "위험도와 작전 진행도 대비 보상 미리보기/수령 흐름을 보는 지표",
    metrics: [
      { label: "위험도별 평균 보상", value: "연동 대기", note: "reward preview value 비교 후보" },
      { label: "위험도별 클리어율", value: "연동 대기", note: "riskTier별 완료/실패 비율" },
      { label: "위험도별 평균 실패 공세", value: "연동 대기", note: "partial failure 공세 위치" },
      { label: "예상 보상 vs 실제 미리보기 불일치", value: "연동 대기", note: "reward plan mismatch 후보" },
      { label: "Risk IV/V 보상 수령 실패율", value: "연동 대기", note: "고위험도 claim 실패 관측" },
      { label: "first clear 보상 지급 상태", value: "연동 대기", note: "별도 rewardLine 지급 후보" }
    ]
  },
  {
    title: "오프라인 보상 진행도",
    description: "최고 스토리 클리어 진행도에 따른 기본 생산력과 보관 한도 관측 후보",
    metrics: [
      { label: "최고 스토리 진행도", value: "연동 대기", note: "Act 1 stable story progress 기준" },
      { label: "공식 계산식", value: "연동 대기", note: offlineRewardFormulaPlaceholders[0] },
      { label: "수령 가능 시간", value: "연동 대기", note: offlineRewardFormulaPlaceholders[1] },
      { label: "최소 누적 시간", value: "연동 대기", note: offlineRewardFormulaPlaceholders[2] },
      { label: "Risk 직접 배율", value: "연동 대기", note: offlineRewardFormulaPlaceholders[3] },
      {
        label: "스토리 배율/보관 한도",
        value: "연동 대기",
        note: offlineRewardStoryProgressPolicy
          .map((policy) => `${policy.label}/${policy.progressId} ${policy.multiplier}/${policy.capHours}`)
          .join(" | ")
      },
      { label: "허용 반복 보상", value: "연동 대기", note: offlineRewardSourceCandidates.join(" / ") },
      { label: "기본 제외 보상", value: "연동 대기", note: offlineRewardBlockedDefaultRewards.join(" / ") },
      {
        label: "상태 row 관측 후보",
        value: "연동 대기",
        note: `${offlineRewardStateDashboardFields.join(
          " / "
        )} | accounts.created_at lazy fallback | last_previewed_at route 미갱신 | preview read-only, claimable duration 미소비`
      },
      {
        label: "수령 응답 관측 후보",
        value: "연동 대기",
        note: `${offlineRewardClaimDashboardFields.join(
          " / "
        )} | offlineRewardClaimId/rewardTransactionId/economyTransactionId/accountMutationId | last claim baseline`
      },
      { label: "오프라인 수령 상태", value: "연동 대기", note: "claim 가능/충돌/무결성 제한 후보" }
    ]
  },
  {
    title: "위험 신호",
    description: "조사 우선순위가 높은 계정과 검증 실패 신호",
    metrics: [
      { label: "긴급/높음 의심 계정", value: "연동 대기", note: "계정 단위 검토 큐" },
      { label: "체크섬 불일치", value: "연동 대기", note: "필수 콘텐츠 버전 검증" },
      { label: "불가능한 피해량/점수", value: "연동 대기", note: "레이드/출격 검증" },
      { label: "보상 버전 불일치", value: "연동 대기", note: "밸런스 버전 검증" }
    ]
  }
];

export const dashboardSections: DashboardSection[] = [...launchDashboardSections, ...dashboardDetailSections];

export const launchDetailSupportCandidates: LaunchDetailSupportCandidate[] = [
  {
    label: "Google provider/link visibility",
    target: "Support/Player detail",
    status: "연동 대기",
    note: "guest/google provider, linked state, recovery evidence read-only 후보"
  },
  {
    label: "Gacha support visibility",
    target: "Support/Gacha detail",
    status: "연동 대기",
    note: "pity, batch, resultStatus, guaranteeType, duplicate replay refs 후보"
  },
  {
    label: "Boss-only raid label/status",
    target: "Run/Raid detail",
    status: "연동 대기",
    note: "raid는 15-wave가 아닌 boss-only encounter로 분리 표시"
  },
  {
    label: "Augment-at-start visibility",
    target: "Run detail",
    status: "연동 대기",
    note: "일반 Act 1 작전 증강은 시작 시점 획득 상태로 표시"
  },
  {
    label: "Player/support login session detail",
    target: "Support/Player detail",
    status: "연동 대기",
    note: "lastLoginAt, lastLoginAge, activeSessionCount, latestSessionAt 후보"
  }
];

export const dashboardShortcuts: DashboardShortcut[] = [
  {
    label: "의심 플레이어",
    href: "/integrity/suspicious-players",
    description: "계정 단위 리스크 검토 큐"
  },
  {
    label: "무결성 이벤트",
    href: "/integrity",
    description: "검증 신호와 증거 조회"
  },
  {
    label: "감사 로그",
    href: "/audit",
    description: "운영자 작업 이력"
  },
  {
    label: "출격 조회",
    href: "/runs",
    description: "출격 이력과 밸런스 버전 확인"
  }
];

export function DashboardPage() {
  return (
    <main className="content">
      <header>
        <h1>운영 현황</h1>
        <p>출시 운영 판단에 필요한 핵심 신호를 먼저 보는 읽기 전용 대시보드</p>
      </header>
      <div className="dashboard-sections">
        {launchDashboardSections.map((section) => (
          <section key={section.title} className="admin-panel">
            <header>
              <h2>{section.title}</h2>
              <p>{section.description}</p>
            </header>
            <div className="metric-grid">
              {section.metrics.map((metric) => (
                <div key={metric.label} className="metric-card">
                  <span>{metric.label}</span>
                  <strong>{metric.value}</strong>
                  <small>{metric.note}</small>
                </div>
              ))}
            </div>
          </section>
        ))}
      </div>
      <section className="admin-panel">
        <header>
          <h2>상세 지표 후보</h2>
          <p>긴 정책 설명과 서버 API 대기 항목은 첫 화면 아래에서만 확인합니다</p>
        </header>
        <div className="dashboard-sections">
          {dashboardDetailSections.map((section) => (
            <section key={section.title} className="metric-card">
              <span>{section.title}</span>
              <strong>연동 대기</strong>
              <small>{section.description}</small>
            </section>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>상세/지원 화면 연결 후보</h2>
          <p>Dashboard가 아니라 Run, Player, Support 상세에서 볼 launch read-only 후보입니다</p>
        </header>
        <div className="metric-grid">
          {launchDetailSupportCandidates.map((candidate) => (
            <div key={candidate.label} className="metric-card">
              <span>{candidate.target}</span>
              <strong>{candidate.status}</strong>
              <small>
                {candidate.label}: {candidate.note}
              </small>
            </div>
          ))}
        </div>
      </section>
      <section className="admin-panel">
        <header>
          <h2>운영 큐 바로가기</h2>
          <p>현재 연결된 읽기 전용 화면으로 이동합니다</p>
        </header>
        <div className="shortcut-grid">
          {dashboardShortcuts.map((shortcut) => (
            <Link key={shortcut.href} to={shortcut.href} className="shortcut-card">
              <strong>{shortcut.label}</strong>
              <small>{shortcut.description}</small>
            </Link>
          ))}
        </div>
      </section>
    </main>
  );
}
