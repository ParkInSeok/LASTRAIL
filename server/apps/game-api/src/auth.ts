import { sha256Hex } from "@lastrail/common";

type AccessTokenPayload = {
  typ: "access";
  accountId: string;
  sessionId: string;
  exp: number;
};

export type VerifiedAccessToken = {
  accountId: string;
  sessionId: string;
  expiresAt: number;
};

export type GoogleIdentityClaims = {
  subject: string;
  email?: string;
  emailVerified?: boolean;
};

export type GoogleIdTokenVerifier = {
  verifyIdToken: (idToken: string, audiences: string[]) => Promise<GoogleIdentityClaims>;
};

export class GoogleIdTokenVerificationError extends Error {
  readonly code: string;

  constructor(code: string) {
    super(code);
    this.name = "GoogleIdTokenVerificationError";
    this.code = code;
  }
}

type GoogleVerifierOptions = {
  fetcher?: typeof fetch;
  jwksUrl?: string;
  nowMs?: () => number;
};

type JwtHeader = {
  alg: string;
  kid: string;
};

type JwtPayload = {
  iss: string;
  aud: string | string[];
  sub: string;
  exp: number;
  email?: string;
  email_verified?: boolean;
};

type GoogleJwks = {
  keys: GoogleJwk[];
};

type GoogleJwk = JsonWebKey & {
  kid?: string;
};

const GOOGLE_OIDC_JWKS_URL = "https://www.googleapis.com/oauth2/v3/certs";
const GOOGLE_ID_TOKEN_ISSUERS = new Set(["accounts.google.com", "https://accounts.google.com"]);

export function createGoogleIdTokenVerifier(options: GoogleVerifierOptions = {}): GoogleIdTokenVerifier {
  const fetcher = options.fetcher ?? fetch;
  const jwksUrl = options.jwksUrl ?? GOOGLE_OIDC_JWKS_URL;
  const nowMs = options.nowMs ?? (() => Date.now());
  let cachedJwks: { keys: JsonWebKey[]; expiresAt: number } | null = null;

  return {
    async verifyIdToken(idToken, audiences) {
      const audienceSet = new Set(audiences.map((audience) => audience.trim()).filter((audience) => audience.length > 0));
      if (audienceSet.size === 0) throw new GoogleIdTokenVerificationError("google_id_token_audience_missing");

      const parsed = parseJwt(idToken);
      if (parsed === null) throw new GoogleIdTokenVerificationError("google_id_token_malformed");
      if (parsed.header.alg !== "RS256") throw new GoogleIdTokenVerificationError("google_id_token_algorithm_invalid");

      const payload = parsed.payload;
      if (!hasAcceptedAudience(payload.aud, audienceSet)) throw new GoogleIdTokenVerificationError("google_id_token_audience_invalid");
      if (!GOOGLE_ID_TOKEN_ISSUERS.has(payload.iss)) throw new GoogleIdTokenVerificationError("google_id_token_issuer_invalid");
      if (!Number.isSafeInteger(payload.exp) || payload.exp * 1000 <= nowMs()) throw new GoogleIdTokenVerificationError("google_id_token_expired");
      if (typeof payload.sub !== "string" || payload.sub.trim() === "" || payload.sub.length > 255) {
        throw new GoogleIdTokenVerificationError("google_id_token_subject_invalid");
      }

      let jwks = await getCachedJwks();
      let jwk = jwks.keys.find((key) => key.kid === parsed.header.kid);
      if (jwk === undefined) {
        jwks = await fetchAndCacheJwks();
        jwk = jwks.keys.find((key) => key.kid === parsed.header.kid);
      }
      if (jwk === undefined) throw new GoogleIdTokenVerificationError("google_id_token_key_not_found");

      const key = await crypto.subtle.importKey(
        "jwk",
        jwk,
        { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
        false,
        ["verify"],
      );
      const verified = await crypto.subtle.verify(
        "RSASSA-PKCS1-v1_5",
        key,
        parsed.signature,
        new TextEncoder().encode(parsed.signingInput),
      );
      if (!verified) throw new GoogleIdTokenVerificationError("google_id_token_signature_invalid");

      const verifiedClaims: GoogleIdentityClaims = {
        subject: payload.sub,
      };
      if (payload.email !== undefined) verifiedClaims.email = payload.email;
      if (payload.email_verified !== undefined) verifiedClaims.emailVerified = payload.email_verified;
      return verifiedClaims;
    },
  };

  async function getCachedJwks(): Promise<GoogleJwks> {
    const now = nowMs();
    if (cachedJwks !== null && cachedJwks.expiresAt > now) return { keys: cachedJwks.keys };

    return fetchAndCacheJwks();
  }

  async function fetchAndCacheJwks(): Promise<GoogleJwks> {
    const now = nowMs();
    const response = await fetcher(jwksUrl);
    if (!response.ok) throw new GoogleIdTokenVerificationError("google_jwks_unavailable");
    const parsed = await response.json() as unknown;
    if (!isGoogleJwks(parsed)) throw new GoogleIdTokenVerificationError("google_jwks_invalid");

    cachedJwks = {
      keys: parsed.keys,
      expiresAt: now + parseCacheMaxAgeMs(response.headers.get("Cache-Control")),
    };
    return parsed;
  }
}

export async function hashRefreshToken(refreshToken: string): Promise<string> {
  return sha256Hex(refreshToken);
}

export async function createAccessToken(options: {
  secret: string;
  accountId: string;
  sessionId: string;
  nowMs: number;
  ttlMs: number;
}): Promise<string> {
  const payload: AccessTokenPayload = {
    typ: "access",
    accountId: options.accountId,
    sessionId: options.sessionId,
    exp: options.nowMs + options.ttlMs,
  };
  const encodedPayload = base64UrlEncode(new TextEncoder().encode(JSON.stringify(payload)));
  const signingInput = `lst1.${encodedPayload}`;
  const signature = await signHmacSha256(options.secret, signingInput);
  return `${signingInput}.${base64UrlEncode(signature)}`;
}

export async function verifyAccessToken(options: {
  secret: string;
  token: string;
  nowMs: number;
}): Promise<VerifiedAccessToken | null> {
  const parts = options.token.split(".");
  if (parts.length !== 3 || parts[0] !== "lst1" || parts[1] === undefined || parts[2] === undefined) {
    return null;
  }

  const signingInput = `${parts[0]}.${parts[1]}`;
  const expectedSignature = await signHmacSha256(options.secret, signingInput);
  const actualSignature = base64UrlDecode(parts[2]);
  if (actualSignature === null || !timingSafeEqual(expectedSignature, actualSignature)) {
    return null;
  }

  const payloadBytes = base64UrlDecode(parts[1]);
  if (payloadBytes === null) return null;

  let payload: unknown;
  try {
    payload = JSON.parse(new TextDecoder().decode(payloadBytes)) as unknown;
  } catch {
    return null;
  }

  if (!isAccessTokenPayload(payload)) return null;
  if (payload.exp <= options.nowMs) return null;

  return {
    accountId: payload.accountId,
    sessionId: payload.sessionId,
    expiresAt: payload.exp,
  };
}

function isAccessTokenPayload(payload: unknown): payload is AccessTokenPayload {
  if (typeof payload !== "object" || payload === null) return false;
  const candidate = payload as Record<string, unknown>;
  return candidate.typ === "access"
    && typeof candidate.accountId === "string"
    && candidate.accountId.length > 0
    && typeof candidate.sessionId === "string"
    && candidate.sessionId.length > 0
    && typeof candidate.exp === "number"
    && Number.isSafeInteger(candidate.exp);
}

async function signHmacSha256(secret: string, input: string): Promise<Uint8Array> {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(input));
  return new Uint8Array(signature);
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }
  return btoa(binary).replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/u, "");
}

function base64UrlDecode(value: string): Uint8Array | null {
  const normalized = value.replaceAll("-", "+").replaceAll("_", "/");
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
  try {
    const binary = atob(padded);
    const bytes = new Uint8Array(binary.length);
    for (let index = 0; index < binary.length; index += 1) {
      bytes[index] = binary.charCodeAt(index);
    }
    return bytes;
  } catch {
    return null;
  }
}

function parseJwt(token: string): {
  header: JwtHeader;
  payload: JwtPayload;
  signingInput: string;
  signature: Uint8Array;
} | null {
  const parts = token.split(".");
  if (parts.length !== 3 || parts[0] === undefined || parts[1] === undefined || parts[2] === undefined) return null;
  const header = decodeJson(parts[0]);
  const payload = decodeJson(parts[1]);
  const signature = base64UrlDecode(parts[2]);
  if (!isJwtHeader(header) || !isJwtPayload(payload) || signature === null) return null;
  return {
    header,
    payload,
    signingInput: `${parts[0]}.${parts[1]}`,
    signature,
  };
}

function decodeJson(value: string): unknown {
  const bytes = base64UrlDecode(value);
  if (bytes === null) return null;
  try {
    return JSON.parse(new TextDecoder().decode(bytes)) as unknown;
  } catch {
    return null;
  }
}

function isJwtHeader(value: unknown): value is JwtHeader {
  if (typeof value !== "object" || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return typeof candidate.alg === "string" && typeof candidate.kid === "string" && candidate.kid.length > 0;
}

function isJwtPayload(value: unknown): value is JwtPayload {
  if (typeof value !== "object" || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return typeof candidate.iss === "string"
    && (typeof candidate.aud === "string" || (Array.isArray(candidate.aud) && candidate.aud.every((audience) => typeof audience === "string")))
    && typeof candidate.sub === "string"
    && typeof candidate.exp === "number";
}

function isGoogleJwks(value: unknown): value is GoogleJwks {
  if (typeof value !== "object" || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return Array.isArray(candidate.keys);
}

function hasAcceptedAudience(audience: string | string[], allowedAudiences: Set<string>): boolean {
  if (typeof audience === "string") return allowedAudiences.has(audience);
  return audience.some((entry) => allowedAudiences.has(entry));
}

function parseCacheMaxAgeMs(cacheControl: string | null): number {
  const match = cacheControl?.match(/(?:^|,)\s*max-age=(\d+)/iu);
  if (match?.[1] === undefined) return 5 * 60 * 1000;
  const seconds = Number(match[1]);
  return Number.isSafeInteger(seconds) && seconds > 0 ? seconds * 1000 : 5 * 60 * 1000;
}

function timingSafeEqual(left: Uint8Array, right: Uint8Array): boolean {
  if (left.length !== right.length) return false;
  let diff = 0;
  for (let index = 0; index < left.length; index += 1) {
    diff |= (left[index] ?? 0) ^ (right[index] ?? 0);
  }
  return diff === 0;
}
