import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { VersionManagementPage } from "./VersionManagementPage";

const state = vi.hoisted(() => ({
  query: {} as Record<string, unknown>
}));
vi.mock("@tanstack/react-query", async (importOriginal) => ({ ...await importOriginal<typeof import("@tanstack/react-query")>(), useQuery: ({ queryKey }: { queryKey: string[] }) => queryKey[0].startsWith("server-deployment") || queryKey[0] === "client-release-registry" ? { isLoading: false } : state.query }));
const current = { server: "DEV", version: "known-version", versionCode: "7", minimumVersionCode: "1", revision: '"current"', canExport: false };
const render = () => renderToStaticMarkup(<QueryClientProvider client={new QueryClient()}><MemoryRouter><VersionManagementPage /></MemoryRouter></QueryClientProvider>);

describe("existing version read states before registry contract integration", () => {
  beforeEach(() => { state.query = { data: current, isLoading: false, isError: false, isFetching: false }; });
  it("keeps readonly data visible with initial mutation control disabled", () => {
    const html = render();
    expect(html).toContain("known-version");
    expect(html).toContain('type="submit" disabled=""');
    expect(html).not.toContain("저장 사전조건 조회 미지원");
  });
  it("distinguishes initial loading from successful version absence", () => {
    state.query = { isLoading: true, isFetching: true };
    expect(render()).toContain("조회 중");
    state.query = { data: { ...current, version: null, versionCode: null, revision: null }, isLoading: false };
    const html = render();
    expect(html).toContain("없음");
    expect(html).not.toContain("조회 중");
    expect(html).not.toContain("저장 사전조건 조회 미지원");
  });
  it("keeps the form disabled when a read fails or revision capability is absent", () => {
    state.query = { isLoading: false, isError: true, error: new Error("local-read-failure") };
    expect(render()).toContain('type="submit" disabled=""');
    state.query = { data: { ...current, canExport: true, revision: undefined }, isLoading: false };
    const html = render();
    expect(html).toContain("저장 사전조건 조회 미지원");
    expect(html).toContain('type="submit" disabled=""');
  });
});

vi.mock("../shared/auth/AdminContext", () => ({useAdmin:()=>({bootstrap:null,isLoading:false,error:null})}));
