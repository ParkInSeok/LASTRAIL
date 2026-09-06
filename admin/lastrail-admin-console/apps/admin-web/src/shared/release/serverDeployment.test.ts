import { describe, expect, it } from "vitest";
import { mapDeployment, deploymentFailure } from "./serverDeployment";
import { rolePermissions } from "@lastrail/admin-contracts";

const pointer = { environment: "dev", service: "game-api", serverVersion: "2026.09.06.1", historyKey: "server-deployments/dev/game-api/2026.09.06.1.json", releaseId: "local", cloudflareDeploymentId: "worker-1", updatedAt: "2026-09-06T00:00:00Z" };
describe("server deployment read evidence", () => {
  it("whitelists current and rejects cross-target stored evidence", () => {
    const raw = { accepted: true, environment: "DEV", service: "game-api", permission: "SERVER_DEPLOYMENT_READ", permissionMode: "ROLE_PERMISSION", current: { ...pointer, secret: "forbidden" } };
    expect(mapDeployment(raw, "game-api", "current").serverVersion).toBe(pointer.serverVersion);
    expect(JSON.stringify(mapDeployment(raw, "game-api", "current"))).not.toContain("forbidden");
    expect(() => mapDeployment({ ...raw, current: { ...pointer, environment: "prd" } }, "game-api", "current")).toThrow();
    expect(() => mapDeployment(raw, "async-worker", "current")).toThrow();
  });
  it("distinguishes missing, denied and damaged evidence", () => {
    expect(deploymentFailure({ status: 404, errorCode: "server_deployment_current_not_found" })).toBe("배포 기록 없음");
    expect(deploymentFailure({ status: 403 })).toContain("권한");
    expect(deploymentFailure({ status: 502, errorCode: "server_deployment_history_invalid" })).toContain("저장된 배포 기록");
    expect(deploymentFailure({ status: 502, errorCode: "server_deployment_current_invalid" })).toContain("저장된 배포 기록 형식");
    for (const errorCode of ["server_deployment_current_target_mismatch", "server_deployment_history_target_mismatch"]) {
      expect(deploymentFailure({status: 502, errorCode})).toContain("환경·서비스·버전 일치 여부");
    }
  });
  it("whitelists history and rejects a different version under the requested key", () => {
    const raw = {accepted:true,environment:"DEV",service:"game-api",permission:"SERVER_DEPLOYMENT_READ",permissionMode:"ROLE_PERMISSION",historyKey:pointer.historyKey,history:{...pointer,createdAt:pointer.updatedAt,pointerStatus:"DEGRADED",verification:{localRegression:"PASSED"},unexpected:"discard"}};
    const mapped = mapDeployment(raw,"game-api","history",pointer.serverVersion);
    expect(mapped.pointerStatus).toBe("DEGRADED");
    expect(mapped.contentVersion).toBeNull();
    expect(JSON.stringify(mapped)).not.toContain("discard");
    expect(() => mapDeployment(raw,"game-api","history","another-version")).toThrow();
  });
  it("exposes read permission for the three server-authorized roles", () => {
    for (const role of ["OWNER", "OPERATOR", "VIEWER"] as const) expect(rolePermissions[role]).toContain("SERVER_DEPLOYMENT_READ");
  });
});
