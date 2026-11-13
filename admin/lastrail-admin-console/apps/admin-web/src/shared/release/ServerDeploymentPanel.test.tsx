import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { ServerDeploymentPanel } from "./ServerDeploymentPanel";
const state = vi.hoisted(() => ({ current: {} as Record<string, unknown>, history: {} as Record<string, unknown> }));
vi.mock("@tanstack/react-query", () => ({ useQuery: ({queryKey}: {queryKey: string[]}) => queryKey[0] === "server-deployment-current" ? state.current : state.history }));
const render = () => renderToStaticMarkup(<ServerDeploymentPanel />);
describe("deployment read panel", () => {
  beforeEach(() => { state.current = {}; state.history = {}; });
  it("renders loading, missing and permission errors without mutation controls", () => {
    state.current = {isLoading: true};
    expect(render()).toContain("조회 중");
    state.current = {error: {status: 404, errorCode: "server_deployment_current_not_found"}};
    expect(render()).toContain("배포 기록 없음");
    state.current = {error: {status: 403}};
    expect(render()).toContain("권한 확인 필요");
    expect(render()).not.toContain("내보내기");
  });
  it("renders degraded evidence and nullable fields", () => {
    state.history = {data: {environment: "dev", service: "game-api", pointerStatus: "DEGRADED", serverVersion: "local-1"}};
    expect(render()).toContain("대조 필요 (DEGRADED)");
    expect(render()).toContain("없음");
  });
  it("flags disagreement between current pointer and referenced history", () => {
    state.current = {data: {environment: "dev", serverVersion: "local-1", releaseId: "a", cloudflareDeploymentId: "worker-a"}};
    state.history = {data: {environment: "dev", service: "game-api", pointerStatus: "CURRENT", releaseId: "b", cloudflareDeploymentId: "worker-b"}};
    expect(render()).toContain("대조 필요 (DEGRADED)");
  });
  it("requires explicit compatible and verified evidence for rollback wording", () => {
    const data = {environment:"dev",service:"game-api",pointerStatus:"CURRENT",apiCompatibility:"ROLLBACK_COMPATIBLE",apiRollbackVerified:false};
    state.history = {data};
    expect(render()).toContain("가능 근거 미확인");
    expect(render()).not.toContain("API 롤백 호환 검증 완료");
    state.history = {data:{...data,apiRollbackVerified:true}};
    expect(render()).toContain("API 롤백 호환 검증 완료");
    state.history = {data:{...data,apiCompatibility:"MIN_VERSION_GATE_REQUIRED",apiRollbackVerified:true}};
    expect(render()).not.toContain("API 롤백 호환 검증 완료");
  });
});
