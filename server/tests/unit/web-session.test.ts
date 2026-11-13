import { describe, expect, it } from "vitest";
import { createWebSessionService, type WebSessionStore, type WebSessionRecord } from "../../apps/game-api/src/web-session";

function fixture() {
  const records = new Map<string, WebSessionRecord>();
  let accountActive = true;
  let testerActive = true;
  const store: WebSessionStore = {
    create: async (record) => { records.set(record.tokenHash, record); },
    findActive: async (hash, now) => {
      const record = records.get(hash);
      return record && !record.revokedAt && record.expiresAt > now ? record : null;
    },
    revoke: async (hash, now) => {
      const record = records.get(hash);
      if (!record || record.revokedAt || record.expiresAt <= now) return false;
      records.set(hash, { ...record, revokedAt: now });
      return true;
    },
    rotateCsrf: async (hash, csrfHash, now) => {
      const record = records.get(hash);
      if (!record || record.revokedAt || record.expiresAt <= now) return false;
      records.set(hash, { ...record, csrfHash });
      return true;
    },
    accountIsActive: async (id) => accountActive && id === "owned-account",
    hasExactActiveTesterEntitlement: async (id, code) => testerActive && id === "owned-account" && code === "47",
  };
  return { service: createWebSessionService({ store, origin: "https://dev.lastrail.app", now: () => 1000,
    resolveOwnedAccount: async (proof) => proof === "password-proof" ? { accountId: "owned-account", identity: "PASSWORD" }
      : proof === "google-proof" ? { accountId: "owned-account", identity: "GOOGLE" }
      : proof === "guest-proof" ? { accountId: "owned-account", identity: "GUEST" as "PASSWORD" } : null,
  }), records, suspendAccount: () => { accountActive = false; }, revokeTester: () => { testerActive = false; } };
}

describe("browser web session", () => {
  it("issues a host-only HttpOnly cookie, then verifies an active account without touching game sessions", async () => {
    const { service } = fixture();
    const issued = await service.issue("https://dev.lastrail.app", "password-proof");
    expect(issued.setCookie).toMatch(/^__Host-lr_web_session=[A-Za-z0-9_-]+; Path=\/; Max-Age=3600; Secure; HttpOnly; SameSite=Lax$/u);
    expect(issued.csrfToken).toMatch(/^[A-Za-z0-9_-]+$/u);
    expect(await service.verify(issued.setCookie)).toMatchObject({ accountId: "owned-account" });
    expect(await service.verify("Authorization: Bearer game-token")).toBeNull();
  });

  it("rejects guest issuance and requires exact active DEV entitlement on each read", async () => {
    const { service, revokeTester } = fixture();
    await expect(service.issue("https://dev.lastrail.app", "guest-proof")).rejects.toThrow();
    await expect(service.issue("https://dev.lastrail.app", { accountId: "owned-account" })).rejects.toThrow();
    await expect(service.issue("https://evil.example", "password-proof")).rejects.toThrow();
    const issued = await service.issue("https://dev.lastrail.app", "password-proof");
    expect(await service.verifyDevPublicRead(issued.setCookie, "47")).toMatchObject({ accountId: "owned-account" });
    expect(await service.verifyDevPublicRead(issued.setCookie, "48")).toBeNull();
    expect(await service.verifyDevPublicRead(issued.setCookie, "047")).toBeNull();
    revokeTester();
    expect(await service.verifyDevPublicRead(issued.setCookie, "47")).toBeNull();
  });

  it("requires exact Origin and CSRF token for mutations and revokes only web session on logout", async () => {
    const { service } = fixture();
    const issued = await service.issue("https://dev.lastrail.app", "google-proof");
    expect(await service.authorizeMutation(issued.setCookie, "https://evil.example", issued.csrfToken)).toBeNull();
    expect(await service.authorizeMutation(issued.setCookie, "https://dev.lastrail.app", "wrong")).toBeNull();
    expect(await service.authorizeMutation(issued.setCookie, "https://dev.lastrail.app", issued.csrfToken)).toMatchObject({ accountId: "owned-account" });
    expect(await service.logout(issued.setCookie, "https://dev.lastrail.app", issued.csrfToken)).toBe(true);
    expect(await service.verify(issued.setCookie)).toBeNull();
    expect(service.clearCookie).toContain("Max-Age=0");
  });

  it("rejects an expired session", async () => {
    const { service, records } = fixture();
    const issued = await service.issue("https://dev.lastrail.app", "password-proof");
    const [hash, record] = [...records.entries()][0]!;
    records.set(hash, { ...record, expiresAt: 1000 });
    expect(await service.verify(issued.setCookie)).toBeNull();
  });

  it("rejects duplicate session cookies and suspended accounts", async () => {
    const { service, suspendAccount } = fixture();
    const issued = await service.issue("https://dev.lastrail.app", "password-proof");
    expect(await service.verify(`${issued.setCookie}; ${issued.setCookie}`)).toBeNull();
    suspendAccount();
    expect(await service.verify(issued.setCookie)).toBeNull();
  });
});
