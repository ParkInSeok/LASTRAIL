type Field = { label: string; value: string | number | null };
type Spec = readonly [key: string, label: string, type: "string" | "number" | "boolean"];
type Section = { label: string; fields: Field[] };
function fail(): never { throw new Error("ADMIN_ACTIVITY_RESPONSE_SHAPE_MISMATCH"); }
function object(value: unknown): Record<string, unknown> {
  return value !== null && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : fail();
}
const labels: Record<string, string> = {
  AVAILABLE: "조회 완료", UNCONFIGURED: "정책 미설정", UNAVAILABLE: "조회 불가",
  CLAIMED: "수령 완료", MAILED: "우편 이관", MAIL_PENDING: "우편 이관 대기", EARNED: "획득 기록",
  EXPIRED: "만료", CLAIMABLE: "수령 가능 기록", STORE_REQUIRED: "스토어 확인 필요",
  GAME_RUNTIME_NOT_OBSERVED: "게임 런타임 미관측",
  PERSISTED_AWARDS_NO_SYNC_ON_ADMIN_READ: "저장된 획득 기록 · 조회 시 동기화 없음",
  RECORDED_RIGHT_AND_EXPIRY_NOT_MUTATION_PREFLIGHT: "기록된 권리·만료 기준 · 지급 승인 아님"
};
function fields(data: Record<string, unknown>, specs: readonly Spec[]): Field[] {
  return specs.map(([key, label, type]) => {
    const value = data[key];
    if (value === undefined || (value !== null && (typeof value !== type || (type === "number" && !Number.isFinite(value))))) fail();
    return { label, value: value === null ? (["cashPrice", "purchaseVerificationAvailable"].includes(key) ? "미관측" : null)
      : typeof value === "boolean" ? value ? "예" : "아니요"
      : typeof value === "string" ? labels[value] ?? value : value as number };
  });
}
const specs: Record<string, readonly Spec[]> = {
  shop: [["policyVersion", "정책 버전", "string"], ["currencyId", "재화 ID", "string"], ["dayStart", "일일 기준 시각(ms)", "number"], ["nextResetAt", "다음 초기화(ms)", "number"], ["ticketOffersConfigured", "티켓 교환 설정", "boolean"], ["dailyRemaining", "일일 잔여 횟수", "number"], ["ticketUnavailableReason", "티켓 제한 사유", "string"], ["purchaseConfigured", "구매 정책 설정", "boolean"], ["configuredProductCount", "설정 상품 수", "number"], ["purchaseVerificationAvailable", "구매 검증 가용성", "boolean"], ["purchaseVerificationReason", "구매 검증 근거", "string"], ["cashPrice", "스토어 가격", "number"], ["cashPriceSource", "가격 출처", "string"], ["materialShopAvailability", "재료 상점", "string"]],
  monthlyAttendance: [["activityId", "출석 ID", "string"], ["policyVersion", "정책 버전", "string"], ["cycleId", "주기 ID", "string"], ["active", "활성", "boolean"], ["canAttend", "출석 가능", "boolean"], ["unavailableReason", "제한 사유", "string"], ["revision", "리비전", "number"], ["completedCells", "완료 칸", "number"], ["todayAttended", "오늘 출석", "boolean"], ["recoveryUsed", "복구 사용", "number"], ["recoverableCount", "복구 가능 칸", "number"]],
  pass: [["policyId", "정책 ID", "string"], ["startAt", "시작(ms)", "number"], ["endAt", "종료(ms)", "number"], ["earnedXp", "획득 경험치", "number"], ["xp", "적용 경험치", "number"], ["level", "레벨", "number"], ["premium", "프리미엄 권리", "boolean"], ["purchaseConfigured", "구매 정책 설정", "boolean"], ["cosmeticBindingsComplete", "외형 연결 완료", "boolean"], ["progressSyncPending", "진행도 동기화 대기", "boolean"], ["progressSource", "진행도 출처", "string"]],
  ordinaryQuests: [["policyId", "정책 ID", "string"], ["startAt", "시작(ms)", "number"], ["endAt", "종료(ms)", "number"], ["limit", "조회 상한", "number"], ["hasMore", "추가 기록 있음", "boolean"], ["progressSyncPending", "진행도 동기화 대기", "boolean"], ["progressSource", "진행도 출처", "string"]],
  mail: [["limit", "조회 상한", "number"], ["hasMore", "추가 기록 있음", "boolean"], ["claimabilitySource", "수령 상태 근거", "string"]]
};
const itemSpecs: Record<string, readonly Spec[]> = {
  monthlyAttendance: [["cell", "칸", "number"], ["claimed", "수령", "boolean"], ["source", "출처", "string"], ["dayIndex", "일차", "number"], ["unavailableReason", "제한 사유", "string"]],
  ordinaryQuests: [["awardId", "획득 ID", "string"], ["objectiveId", "목표 ID", "string"], ["periodId", "기간 ID", "string"], ["earnedAt", "획득(ms)", "number"], ["periodEnd", "기간 종료(ms)", "number"], ["status", "상태", "string"]],
  mail: [["awardId", "획득 ID", "string"], ["policyId", "정책 ID", "string"], ["objectiveId", "목표 ID", "string"], ["sentAt", "발송(ms)", "number"], ["expiresAt", "만료(ms)", "number"], ["status", "상태", "string"]]
};

// Explicit display projection: unknown wire fields never reach the component.
export function mapPlayerActivities(raw: unknown, accountId: string) {
  const root = object(raw);
  if (root.accountId !== accountId || root.version !== "admin-player-activities-v1" || root.readOnly !== true || root.permission !== "SUPPORT_READ" || root.permissionMode !== "ROLE_PERMISSION" || root.consistency !== "INDEPENDENT_LIVE_READS" || typeof root.observedAt !== "number" || !Number.isFinite(root.observedAt)) fail();
  const sections: Section[] = Object.entries({ shop: "상점", monthlyAttendance: "월간 출석", pass: "패스", ordinaryQuests: "일반 임무", mail: "우편" }).map(([key, label]) => {
    const section = object(root[key]);
    if (section.availability === "UNCONFIGURED" || section.availability === "UNAVAILABLE") {
      if (section.data !== null || typeof section.reason !== "string") fail();
      return { label, fields: [{ label: "조회 상태", value: labels[section.availability] }, { label: "사유 코드", value: section.reason }] };
    }
    if (section.availability !== "AVAILABLE" || section.reason !== null) fail();
    const data = object(section.data);
    const result = fields(data, specs[key]);
    if (key === "shop") result.push(...fields(object(data.wallet), [["freeBalance", "무료 잔액", "number"], ["paidBalance", "유료 잔액", "number"], ["totalBalance", "합계 잔액", "number"]]));
    if (itemSpecs[key]) {
      const rows = data[key === "monthlyAttendance" ? "cells" : "items"];
      if (!Array.isArray(rows) || rows.length > 100) fail();
      if (!rows.length) result.push({ label: "기록", value: "데이터 없음" });
      (rows as unknown[]).forEach((row, index) => result.push(...fields(object(row), itemSpecs[key]).map(field => ({ ...field, label: `${index + 1}. ${field.label}` }))));
    }
    return { label, fields: result };
  });
  return { accountId, observedAt: root.observedAt as number, sections };
}
