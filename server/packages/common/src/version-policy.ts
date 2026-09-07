export type VersionBucket = {
  get(key: string): Promise<{ etag?: string; httpEtag?: string; json(): Promise<unknown> } | null>;
  put(
    key: string,
    value: string,
    options: { onlyIf: Headers; httpMetadata: { contentType: string; cacheControl: string } },
  ): Promise<{ etag?: string; httpEtag?: string } | null>;
};

export type ContentReleaseDocument = {
  status: "APPROVED";
  releaseId: string;
  contentVersion: string;
  catalogUrl: string;
  catalogHash: string;
  minimumVersionCode: string;
  maximumVersionCode?: string;
};

export type VersionDocument = {
  version: string;
  versionCode: string;
  minimumVersionCode?: string;
  contentRelease?: ContentReleaseDocument;
};

const currentVersionKey = "versions/current.json";
const safeIdPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/u;
const safeVersionPattern = /^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/u;
const sha256Pattern = /^sha256:[a-f0-9]{64}$/u;

function isVersionCode(value: unknown): value is string {
  return typeof value === "string" && /^[1-9]\d{0,9}$/u.test(value) && Number(value) <= 2147483647;
}

function parseContentRelease(value: unknown): ContentReleaseDocument | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const raw = value as Record<string, unknown>;
  if (raw.status !== "APPROVED") return null;
  if (typeof raw.releaseId !== "string" || !safeIdPattern.test(raw.releaseId)) return null;
  if (typeof raw.contentVersion !== "string" || !safeIdPattern.test(raw.contentVersion)) return null;
  if (typeof raw.catalogUrl !== "string") return null;
  let catalogUrl: URL;
  try {
    catalogUrl = new URL(raw.catalogUrl);
  } catch {
    return null;
  }
  if (catalogUrl.protocol !== "https:") return null;
  if (typeof raw.catalogHash !== "string" || !sha256Pattern.test(raw.catalogHash)) return null;
  if (!isVersionCode(raw.minimumVersionCode)) return null;
  if (raw.maximumVersionCode !== undefined && !isVersionCode(raw.maximumVersionCode)) return null;
  if (raw.maximumVersionCode !== undefined && Number(raw.maximumVersionCode) < Number(raw.minimumVersionCode)) return null;

  return {
    status: "APPROVED",
    releaseId: raw.releaseId,
    contentVersion: raw.contentVersion,
    catalogUrl: raw.catalogUrl,
    catalogHash: raw.catalogHash,
    minimumVersionCode: raw.minimumVersionCode,
    ...(raw.maximumVersionCode === undefined ? {} : { maximumVersionCode: raw.maximumVersionCode }),
  };
}

export function parseVersion(value: unknown): VersionDocument | null {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const raw = value as Record<string, unknown>;
  if (typeof raw.version !== "string" || !safeVersionPattern.test(raw.version)) return null;
  if (!isVersionCode(raw.versionCode)) return null;
  if (raw.minimumVersionCode !== undefined) {
    if (!isVersionCode(raw.minimumVersionCode) || Number(raw.minimumVersionCode) > Number(raw.versionCode)) return null;
  }
  if (raw.contentRelease !== undefined) {
    const contentRelease = parseContentRelease(raw.contentRelease);
    if (contentRelease === null) return null;
    return {
      version: raw.version,
      versionCode: raw.versionCode,
      ...(raw.minimumVersionCode === undefined ? {} : { minimumVersionCode: raw.minimumVersionCode }),
      contentRelease,
    };
  }

  return {
    version: raw.version,
    versionCode: raw.versionCode,
    ...(raw.minimumVersionCode === undefined ? {} : { minimumVersionCode: raw.minimumVersionCode }),
  };
}

export async function readVersion(bucket: VersionBucket) {
  const object = await bucket.get(currentVersionKey);
  if (!object) return null;
  const version = parseVersion(await object.json());
  if (!version) throw new Error("version_document_invalid");
  return version;
}

export async function readVersionSnapshot(bucket: VersionBucket): Promise<{ version: VersionDocument | null; etag: string | null }> {
  const object = await bucket.get(currentVersionKey);
  if (object === null) return { version: null, etag: null };
  const version = parseVersion(await object.json());
  if (version === null) throw new Error("version_document_invalid");
  const etag = object.httpEtag ?? (object.etag ? `"${object.etag}"` : null);
  if (!etag) throw new Error("version_etag_unavailable");
  return { version, etag };
}

export async function saveVersion(bucket: VersionBucket, version: VersionDocument, options: { expectedEtag?: string | null } = {}) {
  const expectedEtag = options.expectedEtag === undefined ? (await readVersionSnapshot(bucket)).etag : options.expectedEtag;
  if (expectedEtag === "") throw new Error("version_etag_unavailable");
  const onlyIf = new Headers();
  if (expectedEtag === null) onlyIf.set("If-None-Match", "*");
  else onlyIf.set("If-Match", expectedEtag);
  const result = await bucket.put(currentVersionKey, JSON.stringify(version), {
    onlyIf,
    httpMetadata: { contentType: "application/json", cacheControl: "no-store" },
  });
  if (result === null) throw new Error("version_current_pointer_conflict");
  if (result === undefined) throw new Error("version_storage_write_failed");
  return version;
}
