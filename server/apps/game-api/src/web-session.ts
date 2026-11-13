import { sha256Hex } from "@lastrail/common";

/** web-session-v1: a browser credential never grants game or Admin authority. */
export const WEB_SESSION_CONTRACT_VERSION = "web-session-v1";
const COOKIE_NAME = "__Host-lr_web_session";
// web-session-v1 implementation policy: fixed one-hour lifetime, with no refresh/extension.
const TTL_MS = 60 * 60 * 1000;
const MAX_COOKIE_BYTES = 4096;

export type WebSessionRecord = {
  tokenHash: string;
  csrfHash: string;
  accountId: string;
  createdAt: number;
  expiresAt: number;
  revokedAt: number | null;
};

export type WebSessionStore = {
  create(record: WebSessionRecord): Promise<void>;
  findActive(tokenHash: string, now: number): Promise<WebSessionRecord | null>;
  revoke(tokenHash: string, now: number): Promise<boolean>;
  rotateCsrf(tokenHash: string, csrfHash: string, now: number): Promise<boolean>;
  accountIsActive(accountId: string): Promise<boolean>;
  hasExactActiveTesterEntitlement(accountId: string, versionCode: string, now: number): Promise<boolean>;
};

export type VerifiedWebSession = { accountId: string; expiresAt: number };

/** Only an owner-verified server login handler may call issue. Never pass a browser assertion directly. */
export function createWebSessionService(options: {
  store: WebSessionStore;
  origin: string;
  /** Implement with an existing owner-verified Google linkage or password credential flow; never accept a nickname or guest identity. */
  resolveOwnedAccount?: (proof: unknown) => Promise<{ accountId: string; identity: "PASSWORD" | "GOOGLE" } | null>;
  now?: () => number;
}) {
  const origin = parseAllowedOrigin(options.origin);
  const now = options.now ?? Date.now;
  const clearCookie = `${COOKIE_NAME}=; Path=/; Max-Age=0; Secure; HttpOnly; SameSite=Lax`;

  async function read(cookieHeader: string | null | undefined): Promise<{ record: WebSessionRecord; tokenHash: string } | null> {
    const token = readCookie(cookieHeader);
    if (token === null) return null;
    const tokenHash = await hashSecret(token);
    const record = await options.store.findActive(tokenHash, now());
    if (!record || record.expiresAt <= now() || record.revokedAt !== null) return null;
    if (!await options.store.accountIsActive(record.accountId)) return null;
    return { record, tokenHash };
  }

  async function authorizeMutation(cookieHeader: string | null | undefined, requestOrigin: string | null | undefined, csrfToken: string | null | undefined): Promise<VerifiedWebSession | null> {
    if (requestOrigin !== origin || !isSecret(csrfToken)) return null;
    const session = await read(cookieHeader);
    if (!session) return null;
    const hash = await hashSecret(csrfToken);
    if (!constantTimeEqual(hash, session.record.csrfHash)) return null;
    return { accountId: session.record.accountId, expiresAt: session.record.expiresAt };
  }

  return {
    clearCookie,
    async issue(requestOrigin: string, proof: unknown) {
      if (requestOrigin !== origin) throw new Error("web_origin_invalid");
      const identity = options.resolveOwnedAccount ? await options.resolveOwnedAccount(proof) : null;
      if (!identity) throw new Error("web_identity_unverified");
      if (identity.identity !== "PASSWORD" && identity.identity !== "GOOGLE") throw new Error("web_identity_not_eligible");
      if (!/^[A-Za-z0-9_-]{1,128}$/u.test(identity.accountId) || !await options.store.accountIsActive(identity.accountId)) {
        throw new Error("web_account_not_active");
      }
      const createdAt = now();
      if (!Number.isSafeInteger(createdAt) || createdAt < 0) throw new Error("web_clock_invalid");
      const token = randomSecret();
      const csrfToken = randomSecret();
      const expiresAt = createdAt + TTL_MS;
      await options.store.create({
        tokenHash: await hashSecret(token), csrfHash: await hashSecret(csrfToken),
        accountId: identity.accountId, createdAt, expiresAt, revokedAt: null,
      });
      return {
        setCookie: `${COOKIE_NAME}=${token}; Path=/; Max-Age=3600; Secure; HttpOnly; SameSite=Lax`,
        csrfToken, expiresAt,
      };
    },
    async verify(cookieHeader: string | null | undefined): Promise<VerifiedWebSession | null> {
      const session = await read(cookieHeader);
      return session ? { accountId: session.record.accountId, expiresAt: session.record.expiresAt } : null;
    },
    async verifyDevPublicRead(cookieHeader: string | null | undefined, versionCode: string | null | undefined): Promise<VerifiedWebSession | null> {
      if (!isCanonicalVersion(versionCode)) return null;
      const session = await read(cookieHeader);
      if (!session || !await options.store.hasExactActiveTesterEntitlement(session.record.accountId, versionCode, now())) return null;
      return { accountId: session.record.accountId, expiresAt: session.record.expiresAt };
    },
    authorizeMutation,
    async renewCsrf(cookieHeader: string | null | undefined, requestOrigin: string | null | undefined): Promise<string | null> {
      if (requestOrigin !== origin) return null;
      const session = await read(cookieHeader);
      if (!session) return null;
      const csrfToken = randomSecret();
      return await options.store.rotateCsrf(session.tokenHash, await hashSecret(csrfToken), now()) ? csrfToken : null;
    },
    async logout(cookieHeader: string | null | undefined, requestOrigin: string | null | undefined, csrfToken: string | null | undefined): Promise<boolean> {
      if (!await authorizeMutation(cookieHeader, requestOrigin, csrfToken)) return false;
      const token = readCookie(cookieHeader);
      return token !== null && options.store.revoke(await hashSecret(token), now());
    },
  };
}

function parseAllowedOrigin(value: string): string {
  const url = new URL(value);
  if (url.protocol !== "https:" || url.origin !== value || url.username || url.password || url.pathname !== "/" || url.search || url.hash) {
    throw new Error("web_origin_invalid");
  }
  return url.origin;
}

export function hasWebSessionCookie(header:string|null|undefined):boolean {return !!header?.split(';').some(part=>part.split('=')[0]?.trim()===COOKIE_NAME);}

function readCookie(header: string | null | undefined): string | null {
  if (!header || header.length > MAX_COOKIE_BYTES) return null;
  const matches = header.split(";").map((part) => part.trim()).filter((part) => part.startsWith(`${COOKIE_NAME}=`));
  if (matches.length !== 1) return null;
  const token = matches[0]!.slice(COOKIE_NAME.length + 1);
  return isSecret(token) ? token : null;
}

function isSecret(value: string | null | undefined): value is string {
  return typeof value === "string" && /^[A-Za-z0-9_-]{43}$/u.test(value);
}

function isCanonicalVersion(value: string | null | undefined): value is string {
  return typeof value === "string" && /^[1-9][0-9]*$/u.test(value) && Number.isSafeInteger(Number(value));
}

function randomSecret(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return btoa(String.fromCharCode(...bytes)).replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/u, "");
}

async function hashSecret(value: string): Promise<string> { return `sha256:${await sha256Hex(value)}`; }

function constantTimeEqual(left: string, right: string): boolean {
  if (left.length !== right.length) return false;
  let difference = 0;
  for (let i = 0; i < left.length; i += 1) difference |= left.charCodeAt(i) ^ right.charCodeAt(i);
  return difference === 0;
}
