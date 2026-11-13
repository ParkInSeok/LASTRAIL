/** Cryptographic verification only. The caller must atomically enforce intent lifetime,
 * environment/ad-unit registration, transaction uniqueness and payout state.
 * https://developers.google.com/admob/unity/ssv
 */
export type AdmobIntentBinding = {
  adUnit: string;
  customData: string;
  userId: string;
  rewardAmount: string;
  rewardItem: string;
};

export type VerifiedAdmobCallback = {
  provider: "ADMOB";
  transactionId: string;
  timestamp: number;
  adUnit: string;
  userId: string;
  customData: string;
};

function malformed(): never { throw new Error("ad_ssv_malformed"); }

function decodeBase64(value: string): Uint8Array<ArrayBuffer> {
  try {
    return Uint8Array.from(atob(value.replace(/-/gu, "+").replace(/_/gu, "/")), (c) => c.charCodeAt(0));
  } catch { return malformed(); }
}

// AdMob uses DER; Web Crypto uses fixed-width IEEE P1363 r||s for P-256.
// Reject noncanonical DER (negative/oversized/redundantly padded integers).
function derToRaw(der: Uint8Array): Uint8Array<ArrayBuffer> {
  if (der.length < 8 || der.length > 72 || der[0] !== 0x30 || der[1] !== der.length - 2) malformed();
  let cursor = 2;
  const result = new Uint8Array(64);
  for (let component = 0; component < 2; component++) {
    if (der[cursor++] !== 2) malformed();
    const length = der[cursor++];
    if (length === undefined || length < 1 || length > 33 || cursor + length > der.length) malformed();
    let bytes = der.subarray(cursor, cursor + length);
    if ((bytes[0]! & 0x80) !== 0) malformed();
    if (bytes.length > 1 && bytes[0] === 0) {
      if ((bytes[1]! & 0x80) === 0) malformed();
      bytes = bytes.subarray(1);
    }
    if (bytes.length > 32) malformed();
    result.set(bytes, component * 32 + 32 - bytes.length);
    cursor += length;
  }
  if (cursor !== der.length) malformed();
  return result;
}

/** rawQuery is the original encoded query without '?', never URLSearchParams.toString().
 * trustedKey must resolve only from a server-owned Google key source, never request URLs.
 * Keys are base64 SPKI. A successful signature is not a grant or replay authorization.
 */
export async function verifyAdmobCallback(
  rawQuery: string,
  trustedKey: (keyId: string) => Promise<string | null>,
  binding: AdmobIntentBinding,
): Promise<VerifiedAdmobCallback> {
  if (rawQuery.length === 0 || rawQuery.length > 8192 || /[\s#?]/u.test(rawQuery)) malformed();
  const parts = rawQuery.split("&");
  const keyPart = parts.pop();
  const signaturePart = parts.pop();
  if (!keyPart?.match(/^key_id=[0-9]+$/u) || !signaturePart?.match(/^signature=[A-Za-z0-9_-]+={0,2}$/u)) malformed();
  const content = parts.join("&");
  const values = new Map<string, string>();
  for (const part of parts) {
    const split = part.indexOf("=");
    if (split < 1) malformed();
    let name: string;
    let value: string;
    try {
      name = decodeURIComponent(part.slice(0, split).replace(/\+/gu, " "));
      value = decodeURIComponent(part.slice(split + 1).replace(/\+/gu, " "));
    } catch { return malformed(); }
    if (values.has(name) || name === "signature" || name === "key_id" || value === "") malformed();
    values.set(name, value);
  }
  for (const name of ["ad_network", "ad_unit", "custom_data", "reward_amount", "reward_item", "timestamp", "transaction_id", "user_id"]) {
    if (!values.has(name)) malformed();
  }
  const timestampText = values.get("timestamp")!;
  const timestamp = Number(timestampText);
  if (!/^[0-9]+$/u.test(timestampText) || !Number.isSafeInteger(timestamp) || timestamp <= 0) malformed();
  const encodedKey = await trustedKey(keyPart.slice(7));
  if (encodedKey === null) throw new Error("ad_ssv_unknown_key");
  const signature = derToRaw(decodeBase64(signaturePart.slice(10)));
  let valid: boolean;
  try {
    const key = await crypto.subtle.importKey("spki", decodeBase64(encodedKey), { name: "ECDSA", namedCurve: "P-256" }, false, ["verify"]);
    valid = await crypto.subtle.verify({ name: "ECDSA", hash: "SHA-256" }, key, signature, new TextEncoder().encode(content));
  } catch { throw new Error("ad_ssv_verification_failed"); }
  if (!valid) throw new Error("ad_ssv_verification_failed");
  if (values.get("ad_unit") !== binding.adUnit || values.get("custom_data") !== binding.customData ||
      values.get("user_id") !== binding.userId || values.get("reward_amount") !== binding.rewardAmount || values.get("reward_item") !== binding.rewardItem) {
    throw new Error("ad_ssv_binding_mismatch");
  }
  return { provider: "ADMOB", transactionId: values.get("transaction_id")!, timestamp,
    adUnit: binding.adUnit, userId: binding.userId, customData: binding.customData };
}
