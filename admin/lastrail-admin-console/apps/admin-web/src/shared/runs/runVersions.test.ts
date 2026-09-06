import { describe, expect, it } from "vitest";
import { mapRunVersions, versionSummary } from "./runVersions";

describe("accepted run version evidence", () => {
  const snapshot = (source: string, version: string) => ({ source, clientInstalled: { appVersion: version, versionCode: "9007199254740993" }, serverAcceptedDeployment: { runtimeDeploymentId: version + "-worker" }, rawPayload: "forbidden" });
  it("keeps independently captured start and finish, dropping unknown fields", () => {
    const result = mapRunVersions({ runStartVersionSnapshot: snapshot("server_accepted_run_start", "start"), runFinishVersionSnapshot: snapshot("server_accepted_run_finish", "finish"), runStartVersionSnapshotStatus: "AVAILABLE", runFinishVersionSnapshotStatus: "AVAILABLE" });
    expect(versionSummary(result.start)).toContain("start");
    expect(versionSummary(result.finish)).toContain("finish");
    expect(result.finish.snapshot?.versionCode).toBe("9007199254740993");
    expect(JSON.stringify(result)).not.toContain("forbidden");
  });
  it("never copies legacy start to finish and respects unfinished state", () => {
    const result = mapRunVersions({ runVersionSnapshot: snapshot("server_accepted_run_start", "old"), runFinishVersionSnapshotStatus: "NOT_FINISHED" });
    expect(result.start.snapshot?.appVersion).toBe("old");
    expect(versionSummary(result.finish)).toBe("미종료");
    expect(mapRunVersions({}).finish.snapshot).toBeNull();
  });
  it("rejects start-source evidence supplied as finish", () => {
    expect(mapRunVersions({ runFinishVersionSnapshot: snapshot("server_accepted_run_start", "old"), runFinishVersionSnapshotStatus: "AVAILABLE" }).finish.snapshot).toBeNull();
  });
});
