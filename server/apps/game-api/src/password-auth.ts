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
