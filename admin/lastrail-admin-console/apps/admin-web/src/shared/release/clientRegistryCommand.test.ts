import { afterEach, describe, expect, it, vi } from "vitest";
import { onlineManager, QueryClient, QueryObserver } from "@tanstack/react-query";
import { adminApi } from "../api/client";
import { executeRegistryCommand, registryCommandKey } from "./clientRegistryCommand";
import { mapClientRegistry, type RegistryTarget } from "./clientReleaseRegistry";

const clients: QueryClient[] = [];
function client() { const value = new QueryClient(); clients.push(value); return value; }
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(a => { resolve = a; }); return { promise, resolve }; }
const state = (target: RegistryTarget) => mapClientRegistry({ target, registryStatus: "CONFIGURED", revision: '"r1"', minimumVersionCode: "1", auditHead: null, rows: [], canMutate: true, capabilities: {} });
const context = (target: RegistryTarget) => ({ target, editingRevision: '"r1"', readUnavailable: false, needsReview: false });
const input = { operation: "status" as const, versionCode: "2", status: "DISABLED" as const, reason: "local ownership test" };
afterEach(() => { onlineManager.setOnline(true); clients.splice(0).forEach(value => value.clear()); vi.restoreAllMocks(); });

describe("registry command ownership on the app QueryClient", () => {
  it("releases an offline failure even when its registry has a mounted observer", async () => {
    const owner = client(), read = vi.fn().mockResolvedValue(state("DEV"));
    const observer = new QueryObserver(owner, { queryKey: ["client-release-registry", "DEV"], queryFn: read, initialData: state("DEV"), staleTime: Infinity });
    const unsubscribe = observer.subscribe(() => {});
    onlineManager.setOnline(false);
    vi.spyOn(adminApi, "updateClientReleaseRegistry").mockRejectedValue(new Error("offline"));
    const result = executeRegistryCommand(owner, state("DEV"), context("DEV"), input);
    const rejection = expect(result).rejects.toThrow("offline");
    try {
      await vi.waitFor(() => expect(owner.isMutating()).toBe(0));
      await rejection; expect(read).not.toHaveBeenCalled();
    } finally { unsubscribe(); }
  });
  it("does not queue an offline command for automatic reconnect execution", async () => {
    onlineManager.setOnline(false);
    const owner = client();
    const write = vi.spyOn(adminApi, "updateClientReleaseRegistry").mockRejectedValue(new Error("offline"));
    const result = executeRegistryCommand(owner, state("DEV"), context("DEV"), input);
    const rejection = expect(result).rejects.toThrow("offline");
    await vi.waitFor(() => expect(write).toHaveBeenCalledTimes(1));
    await rejection; onlineManager.setOnline(true);
    expect(owner.isMutating()).toBe(0); expect(write).toHaveBeenCalledTimes(1);
  });
  it("rejects concurrent same-target commands without queuing stale revisions", async () => {
    const owner = client(), result = deferred<ReturnType<typeof state>>();
    const write = vi.spyOn(adminApi, "updateClientReleaseRegistry").mockReturnValue(result.promise);
    const first = executeRegistryCommand(owner, state("DEV"), context("DEV"), input);
    expect(owner.isMutating({ mutationKey: registryCommandKey("DEV") })).toBe(1);
    await expect(executeRegistryCommand(owner, state("DEV"), context("DEV"), input)).rejects.toThrow(/처리 중/);
    await expect(executeRegistryCommand(owner, { ...state("DEV"), revision: '"r2"' }, { ...context("DEV"), editingRevision: '"r2"' }, input)).rejects.toThrow(/처리 중/);
    expect(write).toHaveBeenCalledTimes(1);
    result.resolve(state("DEV")); await first;
    expect(owner.isMutating()).toBe(0); expect(write).toHaveBeenCalledTimes(1);
  });
  it("lets PRD finish independently while DEV remains pending", async () => {
    const owner = client(), dev = deferred<ReturnType<typeof state>>(), prd = deferred<ReturnType<typeof state>>();
    vi.spyOn(adminApi, "updateClientReleaseRegistry").mockImplementation(value => value.target === "DEV" ? dev.promise : prd.promise);
    const first = executeRegistryCommand(owner, state("DEV"), context("DEV"), input);
    const second = executeRegistryCommand(owner, state("PRD"), context("PRD"), input);
    prd.resolve(state("PRD")); await second;
    expect(owner.isMutating({ mutationKey: registryCommandKey("PRD") })).toBe(0);
    expect(owner.isMutating({ mutationKey: registryCommandKey("DEV") })).toBe(1);
    dev.resolve(state("DEV")); await first; expect(owner.isMutating()).toBe(0);
  });
  it("cleans up failures without retry and permits a fresh explicit request", async () => {
    const owner = client();
    const write = vi.spyOn(adminApi, "updateClientReleaseRegistry").mockRejectedValueOnce(new Error("synthetic failure")).mockResolvedValueOnce(state("DEV"));
    await expect(executeRegistryCommand(owner, state("DEV"), context("DEV"), input)).rejects.toThrow("synthetic failure");
    expect(write).toHaveBeenCalledTimes(1); expect(owner.isMutating()).toBe(0);
    await executeRegistryCommand(owner, state("DEV"), context("DEV"), input);
    expect(write).toHaveBeenCalledTimes(2); expect(owner.isMutating()).toBe(0);
  });
  it("invalidates detached cache data without issuing follow-up reads", async () => {
    const owner = client(), read = vi.fn().mockResolvedValue(state("DEV"));
    await owner.fetchQuery({ queryKey: ["client-release-registry", "DEV"], queryFn: read, staleTime: Infinity });
    owner.setQueryData(["current-version"], { revision: '"r1"' });
    vi.spyOn(adminApi, "updateClientReleaseRegistry").mockResolvedValue(state("DEV"));
    await executeRegistryCommand(owner, state("DEV"), context("DEV"), input);
    expect(read).toHaveBeenCalledTimes(1);
    expect(owner.getQueryState(["client-release-registry", "DEV"])?.isInvalidated).toBe(true);
    expect(owner.getQueryState(["current-version"])?.isInvalidated).toBe(true);
  });
  it("keeps independent QueryClients outside the same lock", async () => {
    const a = client(), b = client(), pending = deferred<ReturnType<typeof state>>();
    const write = vi.spyOn(adminApi, "updateClientReleaseRegistry").mockReturnValue(pending.promise);
    const first = executeRegistryCommand(a, state("DEV"), context("DEV"), input);
    const second = executeRegistryCommand(b, state("DEV"), context("DEV"), input);
    pending.resolve(state("DEV")); await Promise.all([first, second]);
    expect(write).toHaveBeenCalledTimes(2); expect(a.isMutating() + b.isMutating()).toBe(0);
  });
});
