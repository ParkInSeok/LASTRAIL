import {describe,it,expect,vi} from "vitest";
import {renderToStaticMarkup} from "react-dom/server";
import {MemoryRouter} from "react-router-dom";
import {RunsPage} from "./RunsPage";
import {RunVersionEvidence} from "../shared/runs/RunVersionEvidence";
import {mapRunVersions} from "../shared/runs/runVersions";
const rows = vi.hoisted(() => ({items: [] as unknown[]}));
vi.mock("@tanstack/react-query", () => ({useQuery: () => ({data: rows})}));
describe("run list and detail version display", () => {
  it("shows independent start and finish plus explicit Worker reference", () => {
    const snapshot = (source: string, appVersion: string) => ({source, clientInstalled:{appVersion,versionCode:appVersion === "start-app" ? "100" : "101"},serverAcceptedDeployment:{runtimeDeploymentId:"worker-"+appVersion}});
    const evidence=mapRunVersions({runStartVersionSnapshot:snapshot("server_accepted_run_start","start-app"),runFinishVersionSnapshot:snapshot("server_accepted_run_finish","finish-app")});
    rows.items=[{runId:"local-run",accountId:"local-account",versionEvidence:evidence}];
    const html=renderToStaticMarkup(<MemoryRouter><RunsPage/></MemoryRouter>);
    expect(html).toContain("완료 앱 / 서버 버전");
    expect(html).toContain("start-app");
    expect(html).toContain("finish-app");
    expect(html).toContain("Worker 버전 참조");
    expect(renderToStaticMarkup(<RunVersionEvidence value={evidence}/>)).toContain("Worker 버전 참조");
  });
  it("preserves unfinished and legacy absence in the list", () => {
    rows.items=[{runId:"local-empty",accountId:"local-account",versionEvidence:mapRunVersions({runFinishVersionSnapshotStatus:"NOT_FINISHED"})}];
    const html=renderToStaticMarkup(<MemoryRouter><RunsPage/></MemoryRouter>);
    expect(html).toContain("미종료");
    expect(html).toContain("미기록 (이전 기록)");
    expect(html).not.toContain("2026.09.07.1");
  });
});
