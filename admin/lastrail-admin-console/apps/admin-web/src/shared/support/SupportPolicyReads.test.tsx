import { expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { SupportPolicyReads } from "./SupportPolicyReads";
it("separates local view states without inventing server availability", () => {
  const render = (state: Parameters<typeof SupportPolicyReads>[0]["state"]) => renderToStaticMarkup(<SupportPolicyReads state={state} />);
  expect(render({ kind: "loading" })).toContain("조회 중");
  expect(render({ kind: "not_connected" })).toContain("조회 계약 대기");
  expect(render({ kind: "select_account" })).toContain("계정 선택 필요");
  expect(render({ kind: "error", error: new Error("unavailable") })).toContain("조회 실패");
  const empty = render({ kind: "ready", sections: [] });
  expect(empty).toContain("데이터 없음");
  expect(empty).not.toContain("정상");
  const value = render({ kind: "ready", sections: [{ label: "로컬 테스트", fields: [{ label: "건수", value: 0 }, { label: "기록", value: null }, { label: "상태", value: "FUTURE" }] }] });
  expect(value).toContain("0"); expect(value).toContain("없음"); expect(value).toContain("FUTURE");
  expect(value).not.toMatch(/<button|<input|<textarea/);
});
