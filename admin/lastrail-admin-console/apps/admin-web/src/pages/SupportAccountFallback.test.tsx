import { afterEach, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AdminApiError } from "../shared/api/client";
import { emptySupportAccountSummary } from "../shared/support/supportContract";
import { SupportAccountPage } from "./SupportAccountPage";

const clients: QueryClient[] = [];
afterEach(() => clients.splice(0).forEach(client => client.clear()));
function client() {
  const value = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } });
  clients.push(value);
  return value;
}
function render(value: QueryClient, accountId = "account_b") {
  return renderToStaticMarkup(<QueryClientProvider client={value}><MemoryRouter initialEntries={[`/support/accounts/${accountId}`]}><Routes><Route path="/support/accounts/:accountId" element={<SupportAccountPage />} /></Routes></MemoryRouter></QueryClientProvider>);
}
function panel(markup: string, title: string) {
  return markup.split(`<h2>${title}</h2>`)[1].split("</section>")[0];
}
const panels = ["지원 요약 섹션", "Google 연동 근거", "최근 시스템 에러 근거", "출격/레이드 전투 근거", "로그인 보너스 근거"];

it.each([
  [403, "Access/admin credential 확인 필요"],
  [503, "Admin API 응답 실패"]
])("shows classified %s failures in every legacy fallback instead of pending or empty evidence", async (status, label) => {
  const value = client();
  const stale = emptySupportAccountSummary("account_b");
  stale.account.displayName = "STALE_NAME";
  stale.sections[0].value = "연결됨";
  value.setQueryData(["support-account", "account_b"], stale);
  await value.fetchQuery({ queryKey: ["support-account", "account_b"], queryFn: () => Promise.reject(new AdminApiError({ status, path: "/api/support/accounts/account_b" })) }).catch(() => {});
  const markup = render(value);
  for (const title of panels) {
    expect(panel(markup, title)).toContain(label);
    expect(panel(markup, title)).not.toContain("연동 대기");
  }
  expect(markup).not.toContain("STALE_");
  expect(markup).not.toContain("재생 기록 없음");
});

it.each([["pending", "계정 ID 선택 필요"], ["account_b", "조회 중"]])("distinguishes %s fallback from missing integration", (accountId, label) => {
  const markup = render(client(), accountId);
  for (const title of panels) {
    expect(panel(markup, title)).toContain(label);
    expect(panel(markup, title)).not.toContain("연동 대기");
  }
});

it("keeps successful activity zero/null/unavailable values when legacy summary fails", async () => {
  const value = client();
  value.setQueryData(["support-account", "account_b"], emptySupportAccountSummary("account_b"));
  await value.fetchQuery({ queryKey: ["support-account", "account_b"], queryFn: () => Promise.reject(new AdminApiError({ status: 403, path: "/api/support/accounts/account_b" })) }).catch(() => {});
  value.setQueryData(["player-activities", "account_b"], { accountId: "account_b", sections: [{ label: "독립 결과", fields: [{ label: "패스 XP", value: 0 }, { label: "미관측 기록", value: null }, { label: "상점", value: "조회 불가" }] }] });
  const markup = render(value);
  expect(markup).toContain("패스 XP</dt><dd>0</dd>");
  expect(markup).toContain("미관측 기록</dt><dd>없음</dd>");
  expect(markup).toContain("상점</dt><dd>조회 불가</dd>");
  expect(panel(markup, "지원 요약 섹션")).toContain("Access/admin credential 확인 필요");
});

it("preserves successful legacy values and empty evidence", () => {
  const value = client();
  const summary = emptySupportAccountSummary("account_b");
  summary.account.displayName = "CURRENT_NAME";
  summary.sections[0].value = "연결됨";
  summary.sections[0].referenceCount = 0;
  value.setQueryData(["support-account", "account_b"], summary);
  const markup = render(value);
  expect(markup).toContain("CURRENT_NAME");
  expect(panel(markup, "지원 요약 섹션")).toContain("<strong>연결됨</strong>");
  expect(panel(markup, "지원 요약 섹션")).toContain("account / 0");
  expect(panel(markup, "최근 시스템 에러 근거")).toContain("최근 시스템 에러 근거 없음");
});
