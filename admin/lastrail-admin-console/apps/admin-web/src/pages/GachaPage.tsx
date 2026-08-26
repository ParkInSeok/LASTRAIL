type GachaReadOnlySection = {
  title: string;
  status: "조회 API 대기";
  description: string;
  fields: string[];
};

export const gachaReadOnlySections: GachaReadOnlySection[] = [
  {
    title: "배너 상태",
    status: "조회 API 대기",
    description: "운영 중인 요원 모집 배너와 픽업/비픽업 설정을 확인합니다.",
    fields: ["배너 ID", "배너 유형", "시작/종료 시각", "확률표 버전", "활성 여부"]
  },
  {
    title: "계정별 천장 상태",
    status: "조회 API 대기",
    description: "계정과 배너별 pity/guarantee state를 문의 대응용으로 조회합니다.",
    fields: ["총 모집 횟수", "정예 천장 카운트", "픽업 정예 천장 카운트", "희귀 이상 천장 카운트"]
  },
  {
    title: "뽑기 이력",
    status: "조회 API 대기",
    description: "pull batch/request와 개별 pull result를 순서대로 추적합니다.",
    fields: ["batch ID", "Idempotency-Key", "소모 지휘권", "개별 결과", "픽업/보장 발동"]
  },
  {
    title: "중복/조각 변환",
    status: "조회 API 대기",
    description: "중복 요원 여부와 조각 변환 결과를 확인합니다.",
    fields: ["요원 ID", "중복 여부", "변환 조각", "duplicate_conversion_json"]
  },
  {
    title: "아이템 원장 연결",
    status: "조회 API 대기",
    description: "지휘권 소모와 조각/증표/재료 지급을 item ledger 기준으로 추적합니다.",
    fields: ["item_ledger ID", "소모/지급 수량", "연결 batch", "복구/문의 참조"]
  }
];

export function GachaPage() {
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
          <strong>연동 대기</strong>
          <small>서버 조회 API 계약 대기</small>
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
