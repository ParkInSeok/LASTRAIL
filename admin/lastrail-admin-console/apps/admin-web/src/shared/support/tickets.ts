export const ticketCategories: Record<string, string> = { ACCOUNT_LOGIN: "계정·로그인", RUN_REWARD: "출격·보상", GACHA_RECRUITMENT: "모집", STORY_PROGRESS: "스토리 진행", BUG_OTHER: "오류·기타" };
function record(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== "object" || Array.isArray(value)) throw new Error("문의 응답 형식 확인 필요");
  return value as Record<string, unknown>;
}
function mapTicket(value: unknown) {
  const row = record(value);
  if (typeof row.ticketId !== "string" || !row.ticketId) throw new Error("문의 ID 응답 확인 필요");
  const text = (key: string) => typeof row[key] === "string" ? row[key] as string : "";
  return { ticketId: text("ticketId"), accountId: text("accountId"), category: text("category"), status: text("status"), bodyPreview: text("bodyPreview"), body: text("body"), appVersion: text("appVersion"), appVersionCode: text("appVersionCode"), clientComposedAt: text("clientComposedAt"), clientSubmittedAt: text("clientSubmittedAt"), serverReceivedAt: text("serverReceivedAt"), idempotencyReplayKnown: row.idempotencyReplayKnown === true };
}
export function ticketList(value: unknown) {
  const root = record(value);
  if (!Array.isArray(root.items)) throw new Error("문의 목록 응답 형식 확인 필요");
  return { items: root.items.map(mapTicket), nextCursor: typeof root.nextCursor === "string" ? root.nextCursor : null };
}
export function ticketDetail(value: unknown) { return mapTicket(record(value).ticket); }
