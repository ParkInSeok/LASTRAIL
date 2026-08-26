import { Link } from "react-router-dom";

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

export const dashboardSections: DashboardSection[] = [
  {
    title: "오늘 운영 요약",
    description: "일일 운영 흐름을 빠르게 확인하는 핵심 지표",
    metrics: [
      { label: "DAU", value: "연동 대기", note: "일일 활성 계정" },
      { label: "신규 계정", value: "연동 대기", note: "오늘 생성된 계정" },
      { label: "출격 수", value: "연동 대기", note: "일반 출격/레이드 합산" },
      { label: "이상 로그", value: "연동 대기", note: "무결성 이벤트 기준" }
    ]
  },
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
        <p>광고, 보상, 위험 신호를 한눈에 보는 읽기 전용 대시보드</p>
      </header>
      <div className="dashboard-sections">
        {dashboardSections.map((section) => (
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
