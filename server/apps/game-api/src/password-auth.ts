import { sha256Hex } from "@lastrail/common";

export type RecoveryEmailPurpose = "PASSWORD_REGISTER" | "PASSWORD_LINK" | "PASSWORD_RESET" | "RECOVERY_EMAIL_CHANGE";

export type RecoveryEmailMessage = {
  toEmail: string;
  emailMasked: string;
  purpose: RecoveryEmailPurpose;
  challengeId: string;
  verificationCode: string;
  expiresAt: string;
};

export type RecoveryEmailSender = {
  sendRecoveryEmail: (message: RecoveryEmailMessage) => Promise<void>;
};

export type GmailRecoveryEmailSendErrorCode =
  | "gmail_config_missing"
  | "gmail_oauth_rejected"
  | "gmail_oauth_unavailable"
  | "gmail_oauth_unauthorized"
  | "gmail_send_unavailable"
  | "gmail_send_unauthorized"
  | "gmail_quota_exceeded";

export type GmailRecoveryEmailFailureStage =
  | "config"
  | "oauth_fetch"
  | "oauth_response"
  | "oauth_payload"
  | "send_fetch"
  | "send_response";

export class GmailRecoveryEmailSendError extends Error {
  readonly code: GmailRecoveryEmailSendErrorCode;
  readonly failureStage: GmailRecoveryEmailFailureStage | null;
  readonly upstreamStatus: number | null;

  constructor(code: GmailRecoveryEmailSendErrorCode, details: {
    failureStage?: GmailRecoveryEmailFailureStage;
    upstreamStatus?: number;
  } = {}) {
    super(code);
    this.name = "GmailRecoveryEmailSendError";
    this.code = code;
    this.failureStage = details.failureStage ?? null;
    this.upstreamStatus = details.upstreamStatus ?? null;
  }
}

type Fetcher = typeof fetch;

export type GmailRecoveryEmailSenderConfig = {
  senderEmail?: string | undefined;
  oauthClientId?: string | undefined;
  oauthClientSecret?: string | undefined;
  oauthRefreshToken?: string | undefined;
  tokenEndpoint?: string | undefined;
  gmailSendEndpoint?: string | undefined;
  fetcher?: Fetcher | undefined;
};

export type GmailRecoveryEmailSenderEnv = {
  RECOVERY_EMAIL_PROVIDER?: string;
  GMAIL_RECOVERY_SENDER_EMAIL?: string;
  GMAIL_RECOVERY_OAUTH_CLIENT_ID?: string;
  GMAIL_RECOVERY_OAUTH_CLIENT_SECRET?: string;
  GMAIL_RECOVERY_OAUTH_REFRESH_TOKEN?: string;
};

export type NormalizedLoginId = {
  loginId: string;
  normalized: string;
};

export type NormalizedRecoveryEmail = {
  email: string;
  normalized: string;
  masked: string;
};

export type PasswordHashRecord = {
  passwordHash: string;
  algorithm: "PBKDF2_SHA256";
  paramsJson: string;
};

const passwordHashAlgorithm = "PBKDF2_SHA256" as const;
const passwordHashIterations = 210_000;
const passwordHashSaltBytes = 16;
const passwordHashBytes = 32;

const reservedLoginIds = new Set(["admin", "administrator", "gm", "system", "operator", "support"]);
const reservedLoginPrefixes = ["account_", "admin_", "gm_", "system_"];
const gmailTokenEndpoint = "https://oauth2.googleapis.com/token";
const gmailMessagesSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

export function createGmailRecoveryEmailSender(config: GmailRecoveryEmailSenderConfig): RecoveryEmailSender {
  return {
    sendRecoveryEmail: async (message) => {
      const prepared = prepareGmailConfig(config);
      const accessToken = await fetchGmailAccessToken(prepared);
      await sendGmailMimeMessage(prepared, accessToken, message);
    },
  };
}

export function createGmailRecoveryEmailSenderFromEnv(env: GmailRecoveryEmailSenderEnv): RecoveryEmailSender | null {
  if (env.RECOVERY_EMAIL_PROVIDER?.trim().toUpperCase() !== "GMAIL") return null;
  if (
    !isPresent(env.GMAIL_RECOVERY_SENDER_EMAIL)
    || !isPresent(env.GMAIL_RECOVERY_OAUTH_CLIENT_ID)
    || !isPresent(env.GMAIL_RECOVERY_OAUTH_CLIENT_SECRET)
    || !isPresent(env.GMAIL_RECOVERY_OAUTH_REFRESH_TOKEN)
  ) {
    return null;
  }
  return createGmailRecoveryEmailSender({
    senderEmail: env.GMAIL_RECOVERY_SENDER_EMAIL,
    oauthClientId: env.GMAIL_RECOVERY_OAUTH_CLIENT_ID,
    oauthClientSecret: env.GMAIL_RECOVERY_OAUTH_CLIENT_SECRET,
    oauthRefreshToken: env.GMAIL_RECOVERY_OAUTH_REFRESH_TOKEN,
  });
}

export function normalizeLoginId(value: unknown): NormalizedLoginId | null {
  if (typeof value !== "string") return null;
  const loginId = value.normalize("NFKC").trim();
  const normalized = loginId.toLocaleLowerCase("en-US");
  if (Array.from(loginId).length < 4 || Array.from(loginId).length > 32) return null;
  if (!/^[\p{L}\p{N}_.-]+$/u.test(loginId)) return null;
  if (reservedLoginIds.has(normalized) || reservedLoginPrefixes.some((prefix) => normalized.startsWith(prefix))) return null;
  return { loginId, normalized };
}

export function normalizeRecoveryEmail(value: unknown): NormalizedRecoveryEmail | null {
  if (typeof value !== "string") return null;
  const email = value.normalize("NFKC").trim();
  const normalized = email.toLocaleLowerCase("en-US");
  if (normalized.length < 6 || normalized.length > 254) return null;
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/u.test(normalized)) return null;
  return { email, normalized, masked: maskRecoveryEmail(normalized) };
}

export function validatePassword(value: unknown): string | null {
  if (typeof value !== "string") return null;
  if (value.length < 8 || value.length > 128) return null;
  return value;
}

export function createVerificationCode(): string {
  const bytes = new Uint8Array(9);
  crypto.getRandomValues(bytes);
  let code = "";
  for (const byte of bytes) {
    code += (byte % 10).toString();
    if (code.length === 8) break;
  }
  return code.padEnd(8, "0");
}

export async function hashVerificationCode(challengeId: string, purpose: RecoveryEmailPurpose, code: string): Promise<string> {
  return `sha256:${await sha256Hex(`${purpose}:${challengeId}:${code}`)}`;
}

export async function hashPassword(password: string): Promise<PasswordHashRecord> {
  const salt = randomBytes(passwordHashSaltBytes);
  const hash = await derivePbkdf2Sha256(password, salt, passwordHashIterations, passwordHashBytes);
  return {
    passwordHash: `pbkdf2_sha256$${passwordHashIterations}$${base64UrlEncode(salt)}$${base64UrlEncode(hash)}`,
    algorithm: passwordHashAlgorithm,
    paramsJson: JSON.stringify({
      algorithm: passwordHashAlgorithm,
      iterations: passwordHashIterations,
      saltBytes: passwordHashSaltBytes,
      hashBytes: passwordHashBytes,
    }),
  };
}

export async function verifyPasswordHash(password: string, storedHash: string): Promise<boolean> {
  const parts = storedHash.split("$");
  if (parts.length !== 4 || parts[0] !== "pbkdf2_sha256") return false;
  const iterations = Number(parts[1]);
  const salt = parts[2] === undefined ? null : base64UrlDecode(parts[2]);
  const expectedHash = parts[3] === undefined ? null : base64UrlDecode(parts[3]);
  if (!Number.isSafeInteger(iterations) || iterations < 1 || salt === null || expectedHash === null) return false;
  const actualHash = await derivePbkdf2Sha256(password, salt, iterations, expectedHash.length);
  return timingSafeEqual(actualHash, expectedHash);
}

function maskRecoveryEmail(email: string): string {
  const [localPart, domain] = email.split("@");
  if (localPart === undefined || domain === undefined) return "***";
  const first = localPart[0] ?? "*";
  return `${first}${"*".repeat(Math.max(3, Math.min(localPart.length - 1, 8)))}@${domain}`;
}

function isPresent(value: string | undefined): value is string {
  return value !== undefined && value.trim() !== "";
}

type PreparedGmailConfig = {
  senderEmail: string;
  oauthClientId: string;
  oauthClientSecret: string;
  oauthRefreshToken: string;
  tokenEndpoint: string;
  gmailSendEndpoint: string;
  fetcher: Fetcher;
};

function prepareGmailConfig(config: GmailRecoveryEmailSenderConfig): PreparedGmailConfig {
  const senderEmail = config.senderEmail?.trim() ?? "";
  const oauthClientId = config.oauthClientId?.trim() ?? "";
  const oauthClientSecret = config.oauthClientSecret?.trim() ?? "";
  const oauthRefreshToken = config.oauthRefreshToken?.trim() ?? "";
  const tokenEndpoint = config.tokenEndpoint?.trim() || gmailTokenEndpoint;
  const gmailSendEndpoint = config.gmailSendEndpoint?.trim() || gmailMessagesSendEndpoint;
  const fetcher = config.fetcher ?? globalThis.fetch.bind(globalThis);

  if (
    senderEmail === ""
    || !senderEmail.includes("@")
    || oauthClientId === ""
    || oauthClientSecret === ""
    || oauthRefreshToken === ""
    || tokenEndpoint === ""
    || gmailSendEndpoint === ""
  ) {
    throw new GmailRecoveryEmailSendError("gmail_config_missing", { failureStage: "config" });
  }

  return {
    senderEmail,
    oauthClientId,
    oauthClientSecret,
    oauthRefreshToken,
    tokenEndpoint,
    gmailSendEndpoint,
    fetcher,
  };
}

async function fetchGmailAccessToken(config: PreparedGmailConfig): Promise<string> {
  const body = new URLSearchParams({
    client_id: config.oauthClientId,
    client_secret: config.oauthClientSecret,
    refresh_token: config.oauthRefreshToken,
    grant_type: "refresh_token",
  });

  let response: Response;
  try {
    response = await config.fetcher(config.tokenEndpoint, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body,
    });
  } catch {
    throw new GmailRecoveryEmailSendError("gmail_oauth_unavailable", { failureStage: "oauth_fetch" });
  }

  if (response.status === 401 || response.status === 403) {
    throw new GmailRecoveryEmailSendError("gmail_oauth_unauthorized", { failureStage: "oauth_response", upstreamStatus: response.status });
  }
  if (response.status === 429 || response.status >= 500) {
    throw new GmailRecoveryEmailSendError("gmail_oauth_unavailable", { failureStage: "oauth_response", upstreamStatus: response.status });
  }
  if (!response.ok) {
    throw new GmailRecoveryEmailSendError("gmail_oauth_rejected", { failureStage: "oauth_response", upstreamStatus: response.status });
  }

  const payload = await readJsonObject(response);
  const accessToken = typeof payload.access_token === "string" ? payload.access_token.trim() : "";
  if (accessToken === "") {
    throw new GmailRecoveryEmailSendError("gmail_oauth_rejected", { failureStage: "oauth_payload", upstreamStatus: response.status });
  }
  return accessToken;
}

async function sendGmailMimeMessage(config: PreparedGmailConfig, accessToken: string, message: RecoveryEmailMessage): Promise<void> {
  let response: Response;
  try {
    response = await config.fetcher(config.gmailSendEndpoint, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ raw: base64UrlEncodeString(createRecoveryEmailMime(config.senderEmail, message)) }),
    });
  } catch {
    throw new GmailRecoveryEmailSendError("gmail_send_unavailable", { failureStage: "send_fetch" });
  }

  if (response.status === 401 || response.status === 403) {
    throw new GmailRecoveryEmailSendError("gmail_send_unauthorized", { failureStage: "send_response", upstreamStatus: response.status });
  }
  if (response.status === 429) {
    throw new GmailRecoveryEmailSendError("gmail_quota_exceeded", { failureStage: "send_response", upstreamStatus: response.status });
  }
  if (response.status >= 500 || !response.ok) {
    throw new GmailRecoveryEmailSendError("gmail_send_unavailable", { failureStage: "send_response", upstreamStatus: response.status });
  }
}

function createRecoveryEmailMime(senderEmail: string, message: RecoveryEmailMessage): string {
  const body = [
    "LAST RAIL account verification",
    "",
    `Purpose: ${message.purpose}`,
    `Verification code: ${message.verificationCode}`,
    `Expires at: ${message.expiresAt}`,
    "",
    "Enter the verification code in LAST RAIL. Do not share this code.",
  ].join("\r\n");

  return [
    `To: ${message.toEmail}`,
    `From: ${senderEmail}`,
    "Subject: LAST RAIL account verification",
    "MIME-Version: 1.0",
    "Content-Type: text/plain; charset=UTF-8",
    "Content-Transfer-Encoding: 8bit",
    "",
    body,
  ].join("\r\n");
}

function base64UrlEncodeString(value: string): string {
  return base64UrlEncode(new TextEncoder().encode(value));
}

async function readJsonObject(response: Response): Promise<Record<string, unknown>> {
  try {
    const payload: unknown = await response.json();
    return typeof payload === "object" && payload !== null ? payload as Record<string, unknown> : {};
  } catch {
    return {};
  }
}

function randomBytes(length: number): Uint8Array {
  const bytes = new Uint8Array(length);
  crypto.getRandomValues(bytes);
  return bytes;
}

async function derivePbkdf2Sha256(password: string, salt: Uint8Array, iterations: number, length: number): Promise<Uint8Array> {
  const key = await crypto.subtle.importKey("raw", new TextEncoder().encode(password), "PBKDF2", false, ["deriveBits"]);
  const bits = await crypto.subtle.deriveBits(
    {
      name: "PBKDF2",
      hash: "SHA-256",
      salt,
      iterations,
    },
    key,
    length * 8,
  );
  return new Uint8Array(bits);
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";
  for (const byte of bytes) binary += String.fromCharCode(byte);
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
