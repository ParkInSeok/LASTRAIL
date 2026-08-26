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

function timingSafeEqual(left: Uint8Array, right: Uint8Array): boolean {
  if (left.length !== right.length) return false;
  let diff = 0;
  for (let index = 0; index < left.length; index += 1) {
    diff |= (left[index] ?? 0) ^ (right[index] ?? 0);
  }
  return diff === 0;
}
