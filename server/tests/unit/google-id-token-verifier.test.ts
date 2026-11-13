import { describe, expect, it, vi } from "vitest";
import { createGoogleIdTokenVerifier } from "../../apps/game-api/src/auth";

const encoder = new TextEncoder();

async function createSignedGoogleIdToken(payloadOverrides: Record<string, unknown> = {}, keyId = "test-key-1") {
  const keyPair = await crypto.subtle.generateKey(
    {
      name: "RSASSA-PKCS1-v1_5",
      modulusLength: 2048,
      publicExponent: new Uint8Array([1, 0, 1]),
      hash: "SHA-256",
    },
    true,
    ["sign", "verify"],
  );
  const publicJwk = await crypto.subtle.exportKey("jwk", keyPair.publicKey);
  const header = {
    alg: "RS256",
    kid: keyId,
    typ: "JWT",
  };
  const payload = {
    iss: "https://accounts.google.com",
    aud: "unit-google-client.apps.googleusercontent.com",
    sub: "google-subject-1",
    email: "commander@example.com",
    email_verified: true,
    iat: 1_787_650_000,
    exp: 1_787_653_600,
    ...payloadOverrides,
  };
  const signingInput = `${base64UrlJson(header)}.${base64UrlJson(payload)}`;
  const signature = await crypto.subtle.sign(
    "RSASSA-PKCS1-v1_5",
    keyPair.privateKey,
    encoder.encode(signingInput),
  );
  return {
    token: `${signingInput}.${base64UrlEncode(new Uint8Array(signature))}`,
    jwks: {
      keys: [
        {
          ...publicJwk,
          kid: keyId,
          alg: "RS256",
          use: "sig",
        },
      ],
    },
  };
}

function base64UrlJson(value: unknown): string {
  return base64UrlEncode(encoder.encode(JSON.stringify(value)));
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/u, "");
}

function jsonResponse(body: unknown, cacheControl = "public, max-age=3600"): Response {
  return new Response(JSON.stringify(body), {
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": cacheControl,
    },
  });
}

describe("Google ID token verifier adapter", () => {
  it("normalizes malformed inputs and empty audience lists to stable verifier errors", async () => {
    const verifier = createGoogleIdTokenVerifier({ fetcher: async () => jsonResponse({ keys: [] }) });

    await expect(verifier.verifyIdToken("not-a-jwt", ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_id_token_malformed");
    await expect(verifier.verifyIdToken("a.b.c", ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_id_token_malformed");
    await expect(verifier.verifyIdToken("a.b.c", ["", "   "])).rejects.toThrow("google_id_token_audience_missing");
  });

  it("verifies Google-signed RS256 ID tokens against audience, issuer, expiry, and subject", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-25T09:46:40.000Z"));
    try {
      const fixture = await createSignedGoogleIdToken();
      const fetcher = vi.fn(async () => jsonResponse(fixture.jwks));
      const verifier = createGoogleIdTokenVerifier({ fetcher });

      await expect(verifier.verifyIdToken(fixture.token, ["unit-google-client.apps.googleusercontent.com"])).resolves.toEqual({
        subject: "google-subject-1",
        email: "commander@example.com",
        emailVerified: true,
      });
      await expect(verifier.verifyIdToken(fixture.token, ["unit-google-client.apps.googleusercontent.com"])).resolves.toMatchObject({
        subject: "google-subject-1",
      });
      expect(fetcher).toHaveBeenCalledTimes(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it("rejects invalid audience, issuer, expiry, subject, and non-RS256 tokens", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-25T09:46:40.000Z"));
    try {
      const valid = await createSignedGoogleIdToken();
      const verifier = createGoogleIdTokenVerifier({ fetcher: async () => jsonResponse(valid.jwks) });

      await expect(verifier.verifyIdToken(valid.token, ["different-client.apps.googleusercontent.com"])).rejects.toThrow("google_id_token_audience_invalid");

      const invalidIssuer = await createSignedGoogleIdToken({ iss: "https://evil.example" });
      await expect(verifier.verifyIdToken(invalidIssuer.token, ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_id_token_issuer_invalid");

      const expired = await createSignedGoogleIdToken({ exp: 1_787_649_999 });
      await expect(verifier.verifyIdToken(expired.token, ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_id_token_expired");

      const missingSubject = await createSignedGoogleIdToken({ sub: "" });
      await expect(verifier.verifyIdToken(missingSubject.token, ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_id_token_subject_invalid");

      const nonRs256 = `${base64UrlJson({ alg: "HS256", kid: "test-key-1" })}.${valid.token.split(".")[1]}.${valid.token.split(".")[2]}`;
      await expect(verifier.verifyIdToken(nonRs256, ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_id_token_algorithm_invalid");
    } finally {
      vi.useRealTimers();
    }
  });

  it("refreshes Google public keys once when a token key id is not in the cached key set", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-25T09:46:40.000Z"));
    try {
      const first = await createSignedGoogleIdToken({}, "test-key-1");
      const rotated = await createSignedGoogleIdToken({ sub: "google-subject-2" }, "test-key-2");
      const fetcher = vi.fn()
        .mockResolvedValueOnce(jsonResponse(first.jwks, "public, max-age=3600"))
        .mockResolvedValueOnce(jsonResponse(rotated.jwks, "public, max-age=3600"));
      const verifier = createGoogleIdTokenVerifier({ fetcher });

      await expect(verifier.verifyIdToken(first.token, ["unit-google-client.apps.googleusercontent.com"])).resolves.toMatchObject({
        subject: "google-subject-1",
      });
      await expect(verifier.verifyIdToken(rotated.token, ["unit-google-client.apps.googleusercontent.com"])).resolves.toMatchObject({
        subject: "google-subject-2",
      });
      expect(fetcher).toHaveBeenCalledTimes(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it("reports stable verifier errors for unavailable or invalid JWKS responses without raw dumps", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-08-25T09:46:40.000Z"));
    try {
      const fixture = await createSignedGoogleIdToken();
      const unavailableVerifier = createGoogleIdTokenVerifier({
        fetcher: async () => new Response("raw-jwks-upstream-body", { status: 503 }),
      });
      const invalidVerifier = createGoogleIdTokenVerifier({
        fetcher: async () => jsonResponse({ nope: [] }),
      });

      await expect(unavailableVerifier.verifyIdToken(fixture.token, ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_jwks_unavailable");
      await expect(invalidVerifier.verifyIdToken(fixture.token, ["unit-google-client.apps.googleusercontent.com"])).rejects.toThrow("google_jwks_invalid");
    } finally {
      vi.useRealTimers();
    }
  });
});
