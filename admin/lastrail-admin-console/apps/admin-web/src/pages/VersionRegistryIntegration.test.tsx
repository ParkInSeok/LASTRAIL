import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { VersionManagementPage } from "./VersionManagementPage";
const state = vi.hoisted(() => ({ registry: {} as Record<string, unknown> }));
vi.mock("@tanstack/react-query", async (importOriginal) => ({ ...await importOriginal<typeof import("@tanstack/react-query")>(), useQuery: ({ queryKey }: { queryKey: string[] }) => {
  if (queryKey[0] === "client-release-registry") return state.registry;
  if (queryKey[0] === "current-version") return { data: { server: "DEV", version: "old-version", versionCode: "1", minimumVersionCode: "1", revision: '"r1"', canExport: true } };
  return {};
} }));
const render = () => renderToStaticMarkup(<QueryClientProvider client={new QueryClient()}><MemoryRouter><VersionManagementPage /></MemoryRouter></QueryClientProvider>);
describe("registry and legacy export cannot coexist as writers", () => {
  it("removes legacy writer once registry is configured and retains server section", () => {
    state.registry = { data: { target: "DEV", registryStatus: "CONFIGURED", revision: '"r1"', rows: [], canMutate: false, capabilities: {} } };
    const html = render();
    expect(html).not.toContain("기존 단일 앱 버전 정책 저장");
    expect(html).toContain("버전코드별 콘텐츠");
    expect(html).toContain('aria-label="서버 버전 정책"');
  });
  it("does not enable either writer while registry query is unavailable", () => {
    state.registry = { isLoading: true, isFetching: true };
    const html = render();
    expect(html).toContain("조회 중");
    const submits = html.slice(0, html.indexOf('aria-label="서버 버전 정책"')).match(/<button type="submit"[^>]*>/g) ?? [];
    expect(submits.length).toBe(2);
    expect(submits.every(button => button.includes('disabled=""'))).toBe(true);
  });
});

vi.mock("../shared/auth/AdminContext", () => ({useAdmin:()=>({bootstrap:null,isLoading:false,error:null})}));
