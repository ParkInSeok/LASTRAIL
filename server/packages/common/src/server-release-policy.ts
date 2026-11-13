export type ServerReleaseService = "game-api" | "admin-api" | "async-worker";
export type ServerReleaseEnvironment = "dev" | "prd";
export type ServerDbCompatibility = "NO_DB_CHANGE" | "ADDITIVE_COMPATIBLE" | "MIN_VERSION_GATE_REQUIRED";
export type ServerApiCompatibility = "NO_API_CHANGE" | "ROLLBACK_COMPATIBLE" | "MIN_VERSION_GATE_REQUIRED";
export type ServerVerificationStatus = "PENDING" | "PASSED" | "FAILED" | "BLOCKED";

export type ServerDeploymentTag = {
  service: ServerReleaseService;
  tag: string;
};

export type ServerDeployPlan = {
  releaseId: string;
  environment: ServerReleaseEnvironment;
  services: ServerReleaseService[];
  serverVersion: string;
  source: {
    commitSha: string;
    releaseBranch: string;
    workingTreeClean: true;
  };
  deploymentTags: ServerDeploymentTag[];
  database: {
    compatibility: ServerDbCompatibility;
    minimumVersionCode?: string;
    migrationRange?: string;
  };
  api?: {
    compatibility: ServerApiCompatibility;
    rollbackVerified?: boolean;
    minimumVersionCode?: string;
  };
  contentRelease?: {
    releaseId: string;
    contentVersion: string;
  };
  verification: {
    localRegression: Extract<ServerVerificationStatus, "PASSED">;
    dryRun: Extract<ServerVerificationStatus, "PASSED">;
    remoteSmoke: ServerVerificationStatus;
  };
};

export type ServerDeployHistoryEntry = {
  environment: ServerReleaseEnvironment;
  service: ServerReleaseService;
  serverVersion: string;
};

export type ServerDeployValidationErrorCode =
  | "server_deploy_plan_invalid"
  | "server_version_invalid"
  | "server_version_reused"
  | "release_branch_missing_server_version"
  | "deployment_tag_missing"
  | "deployment_tag_invalid"
  | "minimum_version_required_for_db_change"
  | "api_rollback_verification_required"
  | "minimum_version_required_for_api_change";

const serviceValues = new Set<ServerReleaseService>(["game-api", "admin-api", "async-worker"]);
const environmentValues = new Set<ServerReleaseEnvironment>(["dev", "prd"]);
const dbCompatibilityValues = new Set<ServerDbCompatibility>(["NO_DB_CHANGE", "ADDITIVE_COMPATIBLE", "MIN_VERSION_GATE_REQUIRED"]);
const apiCompatibilityValues = new Set<ServerApiCompatibility>(["NO_API_CHANGE", "ROLLBACK_COMPATIBLE", "MIN_VERSION_GATE_REQUIRED"]);
const verificationValues = new Set<ServerVerificationStatus>(["PENDING", "PASSED", "FAILED", "BLOCKED"]);
const safeIdPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/u;
const safeBranchPattern = /^release\/[A-Za-z0-9][A-Za-z0-9._/-]{0,160}$/u;
const safeTagPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,160}$/u;
const commitShaPattern = /^[a-f0-9]{7,40}$/u;
const versionCodePattern = /^[1-9]\d{0,9}$/u;
const serverVersionPattern = /^(20\d{2})\.(0[1-9]|1[0-2])\.(0[1-9]|[12]\d|3[01])\.([1-9]\d{0,3})$/u;

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function isVersionCode(value: unknown): value is string {
  return typeof value === "string" && versionCodePattern.test(value) && Number(value) <= 2147483647;
}

function isSafeId(value: unknown): value is string {
  return typeof value === "string" && safeIdPattern.test(value);
}

function isServerReleaseService(value: unknown): value is ServerReleaseService {
  return typeof value === "string" && serviceValues.has(value as ServerReleaseService);
}

function isServerReleaseEnvironment(value: unknown): value is ServerReleaseEnvironment {
  return typeof value === "string" && environmentValues.has(value as ServerReleaseEnvironment);
}

function isCalendarServerVersion(value: unknown): value is string {
  if (typeof value !== "string") return false;
  const match = serverVersionPattern.exec(value);
  if (!match) return false;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
}

function parseServices(value: unknown): ServerReleaseService[] | null {
  if (!Array.isArray(value) || value.length === 0) return null;
  const services: ServerReleaseService[] = [];
  for (const service of value) {
    if (!isServerReleaseService(service) || services.includes(service)) return null;
    services.push(service);
  }
  return services;
}

function parseDeploymentTags(value: unknown): ServerDeploymentTag[] | null {
  if (!Array.isArray(value) || value.length === 0) return null;
  const tags: ServerDeploymentTag[] = [];
  for (const item of value) {
    if (!isRecord(item)) return null;
    if (!isServerReleaseService(item.service)) return null;
    if (typeof item.tag !== "string" || !safeTagPattern.test(item.tag)) return null;
    if (tags.some((existing) => existing.service === item.service)) return null;
    tags.push({ service: item.service, tag: item.tag });
  }
  return tags;
}

export function parseServerDeployPlan(value: unknown): ServerDeployPlan | null {
  if (!isRecord(value)) return null;
  if (!isSafeId(value.releaseId)) return null;
  if (!isServerReleaseEnvironment(value.environment)) return null;
  const services = parseServices(value.services);
  if (services === null) return null;
  if (!isCalendarServerVersion(value.serverVersion)) return null;
  if (!isRecord(value.source)) return null;
  if (typeof value.source.commitSha !== "string" || !commitShaPattern.test(value.source.commitSha)) return null;
  if (typeof value.source.releaseBranch !== "string" || !safeBranchPattern.test(value.source.releaseBranch)) return null;
  if (value.source.workingTreeClean !== true) return null;
  const deploymentTags = parseDeploymentTags(value.deploymentTags);
  if (deploymentTags === null) return null;
  if (!isRecord(value.database) || typeof value.database.compatibility !== "string") return null;
  const compatibility = value.database.compatibility as ServerDbCompatibility;
  if (!dbCompatibilityValues.has(compatibility)) return null;
  if (value.database.minimumVersionCode !== undefined && !isVersionCode(value.database.minimumVersionCode)) return null;
  if (value.database.migrationRange !== undefined && !isSafeId(value.database.migrationRange)) return null;
  let api: ServerDeployPlan["api"];
  if (value.api !== undefined) {
    if (!isRecord(value.api) || typeof value.api.compatibility !== "string") return null;
    const apiCompatibility = value.api.compatibility as ServerApiCompatibility;
    if (!apiCompatibilityValues.has(apiCompatibility)) return null;
    if (value.api.rollbackVerified !== undefined && typeof value.api.rollbackVerified !== "boolean") return null;
    if (value.api.minimumVersionCode !== undefined && !isVersionCode(value.api.minimumVersionCode)) return null;
    api = {
      compatibility: apiCompatibility,
      ...(value.api.rollbackVerified === undefined ? {} : { rollbackVerified: value.api.rollbackVerified }),
      ...(value.api.minimumVersionCode === undefined ? {} : { minimumVersionCode: value.api.minimumVersionCode }),
    };
  }
  if (!isRecord(value.verification)) return null;
  if (value.verification.localRegression !== "PASSED" || value.verification.dryRun !== "PASSED") return null;
  if (typeof value.verification.remoteSmoke !== "string" || !verificationValues.has(value.verification.remoteSmoke as ServerVerificationStatus)) return null;

  let contentRelease: ServerDeployPlan["contentRelease"];
  if (value.contentRelease !== undefined) {
    if (!isRecord(value.contentRelease)) return null;
    if (!isSafeId(value.contentRelease.releaseId) || !isSafeId(value.contentRelease.contentVersion)) return null;
    contentRelease = {
      releaseId: value.contentRelease.releaseId,
      contentVersion: value.contentRelease.contentVersion,
    };
  }

  return {
    releaseId: value.releaseId,
    environment: value.environment,
    services,
    serverVersion: value.serverVersion,
    source: {
      commitSha: value.source.commitSha,
      releaseBranch: value.source.releaseBranch,
      workingTreeClean: true,
    },
    deploymentTags,
    database: {
      compatibility,
      ...(value.database.minimumVersionCode === undefined ? {} : { minimumVersionCode: value.database.minimumVersionCode }),
      ...(value.database.migrationRange === undefined ? {} : { migrationRange: value.database.migrationRange }),
    },
    ...(api === undefined ? {} : { api }),
    ...(contentRelease === undefined ? {} : { contentRelease }),
    verification: {
      localRegression: "PASSED",
      dryRun: "PASSED",
      remoteSmoke: value.verification.remoteSmoke as ServerVerificationStatus,
    },
  };
}

export function validateServerDeployPlanForDeployment(
  value: unknown,
  history: ServerDeployHistoryEntry[],
): { accepted: true; plan: ServerDeployPlan } | { accepted: false; errorCode: ServerDeployValidationErrorCode } {
  const rawVersion = isRecord(value) ? value.serverVersion : undefined;
  if (!isCalendarServerVersion(rawVersion)) return { accepted: false, errorCode: "server_version_invalid" };

  const plan = parseServerDeployPlan(value);
  if (plan === null) return { accepted: false, errorCode: "server_deploy_plan_invalid" };

  if (!plan.source.releaseBranch.includes(plan.serverVersion)) {
    return { accepted: false, errorCode: "release_branch_missing_server_version" };
  }

  const tagsByService = new Map(plan.deploymentTags.map((tag) => [tag.service, tag.tag]));
  for (const service of plan.services) {
    const tag = tagsByService.get(service);
    if (tag === undefined) return { accepted: false, errorCode: "deployment_tag_missing" };
    if (!tag.startsWith(`${service}-`) || !tag.includes(plan.serverVersion)) {
      return { accepted: false, errorCode: "deployment_tag_invalid" };
    }
  }

  if (plan.database.compatibility === "MIN_VERSION_GATE_REQUIRED" && plan.database.minimumVersionCode === undefined) {
    return { accepted: false, errorCode: "minimum_version_required_for_db_change" };
  }
  if (plan.api?.compatibility === "ROLLBACK_COMPATIBLE" && plan.api.rollbackVerified !== true) {
    return { accepted: false, errorCode: "api_rollback_verification_required" };
  }
  if (plan.api?.compatibility === "MIN_VERSION_GATE_REQUIRED" && plan.api.minimumVersionCode === undefined) {
    return { accepted: false, errorCode: "minimum_version_required_for_api_change" };
  }

  const reused = history.some((entry) =>
    entry.environment === plan.environment &&
    entry.serverVersion === plan.serverVersion &&
    plan.services.includes(entry.service)
  );
  if (reused) return { accepted: false, errorCode: "server_version_reused" };

  return { accepted: true, plan };
}
