import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { MemoryRouter } from "react-router-dom";
import { VersionManagementPage } from "./VersionManagementPage";
vi.mock("@tanstack/react-query", () => ({useQuery: () => ({ data: {server:"dev-admin.lastrail.app",version:"app-only-1",versionCode:"7",minimumVersionCode:"1",canExport:true},isLoading:false })}));
function render(path: string) { return renderToStaticMarkup(<MemoryRouter initialEntries={[path]}><VersionManagementPage/></MemoryRouter>); }
describe("version scope tabs", () => {
  it("keeps existing app version form in frontend tab", () => {
    expect(render("/versions")).toContain("app-only-1");
    expect(render("/versions")).toContain("최소 지원 버전코드");
  });
  it("does not reuse app metadata or export controls in server tab", () => {
    const html = render("/versions?tab=server");
    expect(html).toContain("서버 버전 API 미연동");
    expect(html).not.toContain("app-only-1");
    expect(html).not.toContain("<form");
    expect(html).not.toContain("최소 지원 버전코드");
  });
});
