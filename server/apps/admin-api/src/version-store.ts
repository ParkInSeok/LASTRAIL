export type VersionBucket = Pick<R2Bucket, "get" | "put">;
const key = "versions/current.json";

export function parseVersion(value: unknown): { version: string; versionCode: string; minimumVersionCode?: string } | null {
  if (!value || typeof value !== "object") return null;
  const raw = value as Record<string, unknown>;
  if (typeof raw.version !== "string" || !/^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/.test(raw.version)) return null;
  if (typeof raw.versionCode !== "string" || !/^[1-9]\d{0,9}$/.test(raw.versionCode)) return null;
  if (Number(raw.versionCode) > 2147483647) return null;
  if (raw.minimumVersionCode !== undefined) {
    if (typeof raw.minimumVersionCode !== "string" || !/^[1-9]\d{0,9}$/.test(raw.minimumVersionCode) || Number(raw.minimumVersionCode) > Number(raw.versionCode)) return null;
    return { version: raw.version, versionCode: raw.versionCode, minimumVersionCode: raw.minimumVersionCode };
  }
  return { version: raw.version, versionCode: raw.versionCode };
}

export async function readVersion(bucket: VersionBucket) {
  const object = await bucket.get(key);
  if (!object) return null;
  const version = parseVersion(await object.json());
  if (!version) throw new Error("version_document_invalid");
  return version;
}

export async function saveVersion(bucket: VersionBucket, version: NonNullable<ReturnType<typeof parseVersion>>) {
  await bucket.put(key, JSON.stringify(version), {
    httpMetadata: { contentType: "application/json", cacheControl: "no-store" }
  });
  return version;
}
