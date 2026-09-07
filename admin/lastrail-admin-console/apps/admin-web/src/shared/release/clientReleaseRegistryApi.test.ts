import { afterEach, describe, expect, it, vi } from "vitest";
import { adminApi } from "../api/client";
import { AdminApiError } from "../api/client";
import { adminRequestFailureDisplay } from "../api/errorDisplay";
import { mapClientRegistry } from "./clientReleaseRegistry";

const response = { target: "DEV", registryStatus: "LEGACY", revision: '"r1"', minimumVersionCode: "1", auditHead: null, rows: [], canMutate: true, capabilities: { register: true } };
const context = { target: "DEV" as const, editingRevision: '"r1"', readUnavailable: false, needsReview: false };
const input = { operation: "register" as const, reason: "local test only", confirmed: true, version: "0.2.0", versionCode: "2", contentRelease: { status: "APPROVED", releaseId: "local", contentVersion: "local", catalogUrl: "https://example.invalid/catalog.bin", catalogHash: "sha256:" + "a".repeat(64), minimumVersionCode: "2" } };
describe("registry same-origin API boundary", () => {
  afterEach(() => vi.unstubAllGlobals());
  it("reads exact target and rejects a mismatching response", async () => {
    const fetcher = vi.fn().mockResolvedValue(Response.json(response));
    vi.stubGlobal("fetch", fetcher);
    expect((await adminApi.clientReleaseRegistry("DEV")).registryStatus).toBe("LEGACY");
    expect(fetcher.mock.calls[0][0]).toBe("/api/client-releases?target=DEV");
    await expect(adminApi.clientReleaseRegistry("PRD")).rejects.toThrow();
  });
  it("builds a whitelisted write and never retries stale conflicts", async () => {
    const fetcher = vi.fn().mockResolvedValue(Response.json({ error: { code: "version_current_pointer_conflict" } }, { status: 409 }));
    vi.stubGlobal("fetch", fetcher);
    await expect(adminApi.updateClientReleaseRegistry(mapClientRegistry(response), context, input)).rejects.toMatchObject({ status: 409, errorCode: "version_current_pointer_conflict" });
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(fetcher.mock.calls[0][0]).toBe("/api/client-releases/register");
    expect(JSON.parse(fetcher.mock.calls[0][1].body)).toEqual({ target: "DEV", expectedRevision: '"r1"', reason: input.reason, version: input.version, versionCode: "2", contentRelease: input.contentRelease, legacyTransitionConfirmation: "ENABLE_EXACT_LOOKUP" });
    await expect(adminApi.updateClientReleaseRegistry(mapClientRegistry(response), { ...context, needsReview: true }, input)).rejects.toThrow();
    expect(fetcher).toHaveBeenCalledTimes(1);
  });
  it("classifies missing registry endpoint separately from missing account fixtures", () => {
    const failure = adminRequestFailureDisplay(new AdminApiError({ status: 404, path: "/api/client-releases?target=DEV" }), { resourceLabel: "버전 등록", resourceId: "/api/client-releases" });
    expect(failure.reason).toBe("버전 등록 API 배포 확인 필요");
    expect(failure.reason).not.toContain("fixture");
  });
});
