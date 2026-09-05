import {
  simpleVersionExportAction,
  simpleVersionInputFields,
  simpleVersionServerCards
} from "../shared/release/releaseManagementContract";

export const versionManagementServerCards = simpleVersionServerCards;
export const versionManagementInputFields = simpleVersionInputFields;
export const versionManagementExportAction = simpleVersionExportAction;

export function VersionManagementPage() {
  return (
    <main className="content">
      <header>
        <h1>버전 관리</h1>
        <p>현재 서버와 현재 버전을 확인하고, 변경할 버전을 R2 current version JSON으로 내보내는 화면입니다</p>
      </header>
      <section className="summary-grid" aria-label="버전 관리 요약">
        {versionManagementServerCards.map((card) => (
          <div key={card.code}>
            <span>{card.label}</span>
            <strong>{card.value}</strong>
            <small>{card.note}</small>
          </div>
        ))}
      </section>
      <section className="admin-panel">
        <header>
          <h2>버전 변경</h2>
          <p>입력값 검증과 R2 갱신은 서버 내보내기 API가 담당합니다</p>
        </header>
        <form className="metric-grid">
          {versionManagementInputFields.map((field) => (
            <label key={field.key} className="metric-card" htmlFor={field.key}>
              <span>{field.label}</span>
              <input id={field.key} defaultValue={field.value} placeholder={field.placeholder} aria-label={field.label} />
              <small>현재 서버에 내보낼 목표 버전을 입력합니다</small>
            </label>
          ))}
          <div className="metric-card">
            <span>R2 갱신</span>
            <strong>{versionManagementExportAction.status}</strong>
            <button type="button" disabled>
              {versionManagementExportAction.label}
            </button>
            <small>{versionManagementExportAction.note}</small>
          </div>
        </form>
      </section>
    </main>
  );
}
