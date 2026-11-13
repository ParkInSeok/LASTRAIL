import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { VersionManagementPage } from "./VersionManagementPage";
vi.mock("@tanstack/react-query", async (importOriginal) => ({...await importOriginal<typeof import("@tanstack/react-query")>(), useQuery: ({queryKey}: {queryKey: string[]}) => queryKey[0].startsWith("server-deployment") || queryKey[0] === "client-release-registry" ? {isLoading:false} : ({ data: {server:"dev-admin.lastrail.app",version:"app-only-1",versionCode:"7",minimumVersionCode:"1",canExport:true},isLoading:false })}));
function render(path: string) { return renderToStaticMarkup(<QueryClientProvider client={new QueryClient()}><MemoryRouter initialEntries={[path]}><VersionManagementPage/></MemoryRouter></QueryClientProvider>); }
describe("version scope tabs", () => {
  it("keeps a legacy revision-less API readable but export disabled", () => {
    const html = render("/versions");
    expect(html).toContain("저장 사전조건 조회 미지원");
    expect(html).toContain('type="submit" disabled=""');
    expect(html).toContain("최신 정책 조회");
  });
  it("keeps existing app version form in frontend tab", () => {
    expect(render("/versions")).toContain("app-only-1");
    expect(render("/versions")).toContain("최소 지원 버전코드");
  });
  it("does not reuse app metadata or export controls in server tab", () => {
    const html = render("/versions?tab=server");
    expect(html).toContain("현재 포인터 버전");
    expect(html).toContain("app-only-1");
    expect(html.indexOf('aria-label="프론트 앱 버전 정책"')).toBeLessThan(html.indexOf('aria-label="서버 버전 정책"'));
    const server = html.slice(html.indexOf('aria-label="서버 버전 정책"'));
    expect(server).toContain("이력 조회");
    expect(server).not.toContain("내보내기");
    expect(server).not.toContain("app-only-1");
  });
  it("separates deployment evidence from DB recovery and conditional app admission", () => {
    const html = render("/versions");
    const server = html.slice(html.indexOf('aria-label="서버 버전 정책"'));
    expect(server).toContain("Unity 앱 버전과 독립된 배포·진단 버전");
    expect(server).toContain("호환되는 추가 변경만으로 최소 버전 상향 불필요");
    expect(server).toContain("minimumVersionCode 상향과 서버 접속 제한 검증 후 적용");
    expect(server).toContain("배포 증거이며 DB 백업 지점이 아님");
    expect(server).toContain("별도 승인된 감사 기록 기반 복구");
    expect(server).not.toContain('type="submit">배포');
  });
});

vi.mock("../shared/auth/AdminContext", () => ({useAdmin:()=>({bootstrap:null,isLoading:false,error:null})}));
