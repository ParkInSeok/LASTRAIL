const keyUrl = "https://www.gstatic.com/admob/reward/verifier-keys.json";
const maxAgeMs = 24 * 60 * 60 * 1000;
type KeyFetch = (url: string, init: RequestInit) => Promise<Response>;

/** Server-owned public-key cache only; no account/intent state. One instance belongs
 * to its caller's service lifetime. Never accept a URL from the callback.
 * Unknown keys refresh immediately: provider retries can end within seconds.
 * The eventual route must check a known challenge and rate-limit before lookup.
 */
export function createAdmobKeyResolver(fetcher: KeyFetch = (url, init) => fetch(url, init), now: () => number = Date.now) {
  let cache: Map<string, string> | null = null;
  let fetchedAt = -Infinity;
  let pending: Promise<void> | null = null;

  async function refresh(): Promise<void> {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 5000);
    try {
      const response = await fetcher(keyUrl, { redirect: "error", signal: controller.signal });
      if (!response.ok || !response.body) throw new Error();
      const reader = response.body.getReader();
      const decoder = new TextDecoder("utf-8", { fatal: true, ignoreBOM: false });
      let length = 0;
      let text = "";
      try {
        while (true) {
          const part = await reader.read();
          if (part.done) break;
          length += part.value.byteLength;
          if (length > 65536) {
            await reader.cancel();
            throw new Error();
          }
          text += decoder.decode(part.value, { stream: true });
        }
        text += decoder.decode();
      } finally { reader.releaseLock(); }
      const document: unknown = JSON.parse(text);
      if (!document || typeof document !== "object" || !("keys" in document) || !Array.isArray(document.keys) ||
          document.keys.length < 1 || document.keys.length > 32) throw new Error();
      const keys = new Map<string, string>();
      for (const entry of document.keys) {
        if (!entry || typeof entry !== "object" || !Number.isSafeInteger(entry.keyId) || entry.keyId < 0 ||
            typeof entry.base64 !== "string" || entry.base64.length > 2048 || !/^[A-Za-z0-9+/]+={0,2}$/u.test(entry.base64)) throw new Error();
        const id = String(entry.keyId);
        if (keys.has(id)) throw new Error();
        keys.set(id, entry.base64);
      }
      cache = keys;
      fetchedAt = now();
    } catch {
      throw new Error("ad_ssv_keys_unavailable");
    } finally { clearTimeout(timeout); }
  }

  return async (keyId: string): Promise<string | null> => {
    if (!/^[0-9]{1,20}$/u.test(keyId)) return null;
    const age = now() - fetchedAt;
    if (!cache || age < 0 || age >= maxAgeMs || !cache.has(keyId)) {
      if (!pending) pending = refresh().finally(() => { pending = null; });
      await pending;
    }
    return cache?.get(keyId) ?? null;
  };
}
