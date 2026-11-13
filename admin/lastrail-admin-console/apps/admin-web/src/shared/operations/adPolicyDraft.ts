// Unsent editor state, never an authoritative policy or a write permission.
export type AdPolicyDraft = { adsEnabled: boolean | null; reason: string };
export function validateAdPolicyDraft(draft: AdPolicyDraft, canEnable:boolean): string[] {
  const errors: string[] = [];
  if (draft.adsEnabled === null) errors.push("변경할 광고 ON/OFF를 선택하세요.");
  if (!draft.reason.trim() || draft.reason.trim().length>500) errors.push("변경 사유를 1~500자로 입력하세요.");
  if (draft.adsEnabled === true && !canEnable) errors.push("광고 ON 준비 검증이 연결되지 않아 활성화할 수 없습니다.");
  return errors;
}
