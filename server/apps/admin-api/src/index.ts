import { Hono, type Context } from "hono";
import { canonicalJson, createRequestId, sha256Hex } from "@lastrail/common";

const app = new Hono<{ Bindings: Env }>();

type JsonValue = null | boolean | number | string | JsonValue[] | { readonly [key: string]: JsonValue };

type AdminPrincipalRow = {
  id: string;
  access_subject: string;
  role: "OWNER" | "OPERATOR" | "VIEWER";
  status: "ACTIVE" | "DISABLED";
  display_name: string | null;
};

type AppContext = Context<{ Bindings: Env }>;

type AccessJwtPayload = {
  iss?: unknown;
  aud?: unknown;
  sub?: unknown;
  exp?: unknown;
  nbf?: unknown;
};

type AccessJwk = JsonWebKey & {
  kid?: string;
  alg?: string;
  use?: string;
};

type AccessJwks = {
  keys?: AccessJwk[];
};

function decodeBase64Url(value: string): Uint8Array {
  const padded = value.replaceAll("-", "+").replaceAll("_", "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  const binary = atob(padded);
  return Uint8Array.from(binary, (char) => char.charCodeAt(0));
}

function decodeJson<T>(value: string): T {
  return JSON.parse(new TextDecoder().decode(decodeBase64Url(value))) as T;
}

function hasAudience(payloadAudience: unknown, expectedAudience: string): boolean {
  if (typeof payloadAudience === "string") return payloadAudience === expectedAudience;
  if (Array.isArray(payloadAudience)) return payloadAudience.includes(expectedAudience);
  return false;
}

async function verifyAccessJwt(token: string, issuer: string, audience: string, nowSeconds = Math.floor(Date.now() / 1000)): Promise<string | null> {
  const [encodedHeader, encodedPayload, encodedSignature] = token.split(".");
  if (encodedHeader === undefined || encodedPayload === undefined || encodedSignature === undefined) return null;

  const header = decodeJson<{ alg?: unknown; kid?: unknown }>(encodedHeader);
  if (header.alg !== "RS256" || typeof header.kid !== "string") return null;

  const jwksResponse = await fetch(`${issuer}/cdn-cgi/access/certs`);
  if (!jwksResponse.ok) return null;

  const jwks = (await jwksResponse.json()) as AccessJwks;
  const jwk = jwks.keys?.find((key) => key.kid === header.kid && key.kty === "RSA");
  if (jwk === undefined) return null;

  const publicKey = await crypto.subtle.importKey(
    "jwk",
    jwk,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["verify"],
  );
  const signingInput = `${encodedHeader}.${encodedPayload}`;
  const verified = await crypto.subtle.verify(
    "RSASSA-PKCS1-v1_5",
    publicKey,
    decodeBase64Url(encodedSignature),
    new TextEncoder().encode(signingInput),
  );
  if (!verified) return null;

  const payload = decodeJson<AccessJwtPayload>(encodedPayload);
  if (payload.iss !== issuer) return null;
  if (!hasAudience(payload.aud, audience)) return null;
  if (typeof payload.sub !== "string" || payload.sub.length === 0) return null;
  if (typeof payload.exp !== "number" || payload.exp <= nowSeconds) return null;
  if (typeof payload.nbf === "number" && payload.nbf > nowSeconds) return null;

  return payload.sub;
}

async function authenticateAdmin(c: AppContext): Promise<AdminPrincipalRow | Response> {
  let accessSubject: string | null = null;

  if (c.env.ADMIN_AUTH_MODE === "LOCAL_HEADER") {
    accessSubject = c.req.header("Cf-Access-Subject") ?? null;
  } else if (c.env.ADMIN_ACCESS_ISSUER.length > 0 && c.env.ADMIN_ACCESS_AUD.length > 0) {
    const token = c.req.header("Cf-Access-Jwt-Assertion");
    if (!token) {
      return c.json({ error: "ADMIN_ACCESS_JWT_REQUIRED" }, 401);
    }

    accessSubject = await verifyAccessJwt(token, c.env.ADMIN_ACCESS_ISSUER, c.env.ADMIN_ACCESS_AUD);
    if (accessSubject === null) {
      return c.json({ error: "ADMIN_ACCESS_JWT_INVALID" }, 403);
    }
  } else {
    return c.json({ error: "ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED" }, 501);
  }

  if (!accessSubject) {
    return c.json({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" }, 401);
  }

  const principal = await c.env.DB.prepare(
    `SELECT id, access_subject, role, status, display_name
     FROM admin_principals
     WHERE access_subject = ?
       AND status = 'ACTIVE'`,
  )
    .bind(accessSubject)
    .first<AdminPrincipalRow>();

  if (principal === null) {
    return c.json({ error: "ADMIN_PRINCIPAL_NOT_AUTHORIZED" }, 403);
  }

  return principal;
}

function isJsonValue(value: unknown): value is JsonValue {
  if (value === null) return true;
  if (typeof value === "boolean" || typeof value === "number" || typeof value === "string") return Number.isFinite(value);
  if (Array.isArray(value)) return value.every((item) => isJsonValue(item));
  if (typeof value === "object") return Object.values(value).every((item) => isJsonValue(item));
  return false;
}

function isJsonObject(value: unknown): value is { readonly [key: string]: JsonValue } {
  return value !== null && typeof value === "object" && !Array.isArray(value) && isJsonValue(value);
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value > 0;
}

function isNonNegativeInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value >= 0;
}

app.get("/status", (c) => c.json({ ok: true, service: "admin-api", requestId: createRequestId() }));
app.get("/v1/admin/me", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;

  return c.json({
    principal: {
      id: principal.id,
      accessSubject: principal.access_subject,
      role: principal.role,
      displayName: principal.display_name,
    },
  });
});
app.post("/v1/admin/balance-config/drafts", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role === "VIEWER") {
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }

  const body = (await c.req.json()) as {
    baseVersion?: unknown;
    schemaVersion?: unknown;
    requiredContentVersion?: unknown;
    config?: unknown;
  };
  if (
    !(body.baseVersion === undefined || body.baseVersion === null || isPositiveInteger(body.baseVersion)) ||
    !isPositiveInteger(body.schemaVersion) ||
    !isNonNegativeInteger(body.requiredContentVersion) ||
    !isJsonObject(body.config)
  ) {
    return c.json({ error: "BALANCE_DRAFT_INVALID" }, 400);
  }

  const now = Date.now();
  const draftId = `draft_${createRequestId().slice(4)}`;
  const configJson = canonicalJson(body.config);
  const checksum = `sha256:${await sha256Hex(configJson)}`;
  const baseVersion = body.baseVersion ?? null;

  await c.env.DB.prepare(
    `INSERT INTO balance_config_drafts (
       id,
       base_version,
       schema_version,
       required_content_version,
       config_json,
       checksum,
       revision,
       status,
       created_by,
       created_at,
       updated_at
     ) VALUES (?, ?, ?, ?, ?, ?, 1, 'DRAFT', ?, ?, ?)`,
  )
    .bind(draftId, baseVersion, body.schemaVersion, body.requiredContentVersion, configJson, checksum, principal.id, now, now)
    .run();

  return c.json(
    {
      draft: {
        id: draftId,
        baseVersion,
        schemaVersion: body.schemaVersion,
        requiredContentVersion: body.requiredContentVersion,
        checksum,
        revision: 1,
        status: "DRAFT",
      },
    },
    201,
  );
});

export default app;


