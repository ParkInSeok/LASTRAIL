export type RegistryTarget = "DEV" | "PRD";
export type RegistryContent = { status: "APPROVED"; releaseId: string; contentVersion: string; catalogUrl: string; catalogHash: string; minimumVersionCode: string; maximumVersionCode?: string };
export type RegistryRow = { version: string; versionCode: string; status: string; contentRelease: RegistryContent; artifactIdentity: string; validation: { status: string; artifactBytesVerified: boolean } };
export type ClientRegistry = { target: RegistryTarget; registryStatus: string; revision: string | null; minimumVersionCode: string | null; auditHead: string | null; rows: RegistryRow[]; canMutate: boolean; capabilities: { register: boolean; changeStatus: boolean; promote: boolean; changeMinimum: boolean; manageTesters: boolean } };
export type RegistryEditContext = { target: RegistryTarget; editingRevision: string | null | undefined; readUnavailable: boolean; needsReview: boolean };
export type RegistryInput = { reason: string } & (
  { operation: "register"; version: string; versionCode: string; contentRelease: unknown; confirmed: boolean } |
  { operation: "status"; versionCode: string; status: "INTERNAL" | "DISABLED" } |
  { operation: "promote"; versionCode: string; confirmed: boolean } |
  { operation: "minimum"; minimumVersionCode: string; confirmed: boolean }
);
export type RegistryCommand = { path: string; body: Record<string, unknown> };
function fail(message: string): never { throw new Error(message); }
function record(value: unknown): Record<string, unknown> { return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : fail("응답 형식 확인 필요"); }
function text(value: unknown): string { return typeof value === "string" && value.length > 0 ? value : fail("필수 값 확인 필요"); }
export function validRegistryCode(value: unknown): value is string { return typeof value === "string" && /^[1-9]\d{0,9}$/.test(value) && Number(value) <= 2147483647; }
function code(value: unknown): string { return validRegistryCode(value) ? value : fail("버전코드 확인 필요"); }
function hash(value: unknown): string { return typeof value === "string" && /^sha256:[a-f0-9]{64}$/.test(value) ? value : fail("SHA256 식별자 확인 필요"); }
export function parseRegistryContent(value: unknown): RegistryContent {
  const r = record(value);
  const url = text(r.catalogUrl);
  try { if (new URL(url).protocol !== "https:") fail("HTTPS 카탈로그 주소 필요"); } catch { fail("HTTPS 카탈로그 주소 필요"); }
  const min = code(r.minimumVersionCode);
  const max = r.maximumVersionCode === undefined ? undefined : code(r.maximumVersionCode);
  if (r.status !== "APPROVED" || (max !== undefined && Number(max) < Number(min))) fail("콘텐츠 호환 범위 확인 필요");
  const releaseId = text(r.releaseId), contentVersion = text(r.contentVersion);
  if (![releaseId, contentVersion].every(id => /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/.test(id))) fail("콘텐츠 ID 확인 필요");
  return { status: "APPROVED", releaseId, contentVersion, catalogUrl: url, catalogHash: hash(r.catalogHash), minimumVersionCode: min, ...(max === undefined ? {} : { maximumVersionCode: max }) };
}
export function mapClientRegistry(value: unknown): ClientRegistry {
  const r = record(value);
  if (r.target !== "DEV" && r.target !== "PRD") fail("조회 대상 응답 확인 필요");
  if (r.revision !== null && (typeof r.revision !== "string" || !/^"[\x21\x23-\x7e]{1,256}"$/.test(r.revision))) fail("저장 사전조건 응답 확인 필요");
  if (!Array.isArray(r.rows)) fail("등록 목록 응답 확인 필요");
  const caps = r.capabilities ? record(r.capabilities) : {};
  const rows = (r.rows as unknown[]).map(value => {
    const row = record(value), validation = record(row.validation);
    return { version: text(row.version), versionCode: code(row.versionCode), status: text(row.status), contentRelease: parseRegistryContent(row.contentRelease), artifactIdentity: hash(row.artifactIdentity), validation: { status: text(validation.status), artifactBytesVerified: validation.artifactBytesVerified === true } };
  });
  if (new Set(rows.map(row => row.versionCode)).size !== rows.length) fail("버전코드 중복 응답 확인 필요");
  return { target: r.target as RegistryTarget, registryStatus: text(r.registryStatus), revision: r.revision as string | null, minimumVersionCode: r.minimumVersionCode === null ? null : code(r.minimumVersionCode), auditHead: r.auditHead === null ? null : text(r.auditHead), rows, canMutate: r.canMutate === true, capabilities: { register: caps.register === true, changeStatus: caps.changeStatus === true, promote: caps.promote === true, changeMinimum: caps.changeMinimum === true, manageTesters: caps.manageTesters === true } };
}
export function buildRegistryCommand(state: ClientRegistry, context: RegistryEditContext, input: RegistryInput): RegistryCommand {
  if (context.target !== state.target) fail("선택 대상과 응답 서버 불일치");
  if (!state.canMutate) fail("읽기 전용 권한");
  if (context.readUnavailable || context.needsReview || !state.revision || state.revision !== context.editingRevision) fail("최신 정책 조회 후 다시 확인 필요");
  if (!["LEGACY", "CONFIGURED"].includes(state.registryStatus)) fail("등록 정책 상태 확인 필요");
  const reason = input.reason.trim();
  if (!reason || reason.length > 500) fail("변경 사유는 1~500자 필요");
  const cap = { register: "register", status: "changeStatus", promote: "promote", minimum: "changeMinimum" } as const;
  if (!state.capabilities[cap[input.operation]]) fail("작업 권한 또는 API 미지원");
  const body: Record<string, unknown> = { target: context.target, expectedRevision: state.revision, reason };
  if (input.operation === "register") {
    const versionCode = code(input.versionCode), version = input.version.trim();
    if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/.test(version)) fail("앱 버전 확인 필요");
    if (state.rows.some(row => row.versionCode === versionCode)) fail("이미 등록된 버전코드");
    if (!input.confirmed) fail("미등록 버전 차단 정책 확인 필요");
    const contentRelease = parseRegistryContent(input.contentRelease);
    if (Number(versionCode) < Number(contentRelease.minimumVersionCode) || (contentRelease.maximumVersionCode !== undefined && Number(versionCode) > Number(contentRelease.maximumVersionCode))) fail("앱 버전과 콘텐츠 호환 범위 불일치");
    return { path: "/api/client-releases/register", body: { ...body, version, versionCode, contentRelease, ...(state.registryStatus === "LEGACY" ? { legacyTransitionConfirmation: "ENABLE_EXACT_LOOKUP" } : {}) } };
  }
  if (input.operation === "minimum") {
    const minimumVersionCode = code(input.minimumVersionCode);
    if (!input.confirmed) fail("최소 지원 버전 변경 별도 확인 필요");
    if (!state.rows.some(row => row.status === "STABLE" && Number(row.versionCode) >= Number(minimumVersionCode))) fail("최소 버전 이상 정식 버전 필요");
    return { path: "/api/client-releases/minimum", body: { ...body, minimumVersionCode, confirmMinimumChange: true } };
  }
  const row = state.rows.find(row => row.versionCode === input.versionCode);
  if (!row) fail("미등록 버전코드");
  if (!["INTERNAL", "STABLE", "DISABLED"].includes(row.status)) fail("알 수 없는 배포 상태");
  const path = `/api/client-releases/${encodeURIComponent(row.versionCode)}`;
  if (input.operation === "status") {
    if (input.status !== "INTERNAL" && input.status !== "DISABLED") fail("상태 변경은 내부 테스트 또는 사용 중지만 허용");
    return { path: path + "/status", body: { ...body, status: input.status } };
  }
  if (row.status !== "INTERNAL" || !input.confirmed) fail("내부 테스트 버전의 동일 아티팩트 승급 확인 필요");
  return { path: path + "/promote", body: { ...body, expectedArtifactIdentity: row.artifactIdentity, confirmation: "PROMOTE_UNCHANGED_ARTIFACT" } };
}

export function registryStatusLabel(value: string) { return ({ INTERNAL: "내부 테스트", STABLE: "정식", DISABLED: "사용 중지", LEGACY: "기존 단일 정책", CONFIGURED: "버전코드별 등록", STRUCTURAL_ONLY: "구조 확인만 완료" } as Record<string, string>)[value] ?? value; }
