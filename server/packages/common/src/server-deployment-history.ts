import { canonicalJson } from "./canonical-json";
import {
  parseServerDeployPlan,
  validateServerDeployPlanForDeployment,
  type ServerDbCompatibility,
  type ServerDeployHistoryEntry,
  type ServerDeployPlan,
  type ServerDeployValidationErrorCode,
  type ServerReleaseEnvironment,
  type ServerReleaseService,
  type ServerVerificationStatus,
} from "./server-release-policy";

export type ServerDeploymentHistoryObject = {
  etag?: string;
  httpEtag?: string;
  json(): Promise<unknown>;
};

export type ServerDeploymentHistoryBucket = {
  get(key: string): Promise<ServerDeploymentHistoryObject | null>;
  put(
    key: string,
    value: string,
    options: {
      onlyIf?: Headers;
      httpMetadata: { contentType: string; cacheControl: string };
    },
  ): Promise<{ etag?: string; httpEtag?: string } | null>;
};

export type ServerDeployPreflightTarget = {
  environment: ServerReleaseEnvironment;
  service: ServerReleaseService;
};

export type ServerDeployPreflightErrorCode = ServerDeployValidationErrorCode | "deployment_target_mismatch";

export type ServerDeployExecutor<TDeployResult> = (input: {
  plan: ServerDeployPlan;
  service: ServerReleaseService;
}) => Promise<TDeployResult>;

export type ServerDeploymentReleaseDeployResult =
  | {
      accepted: true;
      cloudflareDeploymentId: string;
      previousDeploymentId?: string | null;
    }
  | {
      accepted: false;
      errorCode: "deployment_execution_failed";
    };

export type ServerDeploymentReleaseSmokeResult =
  | {
      accepted: true;
    }
  | {
      accepted: false;
      errorCode: "deployment_smoke_failed" | "deployment_smoke_blocked";
    };

export type ServerDeploymentReleaseDeployExecutor = (input: {
  plan: ServerDeployPlan;
  service: ServerReleaseService;
}) => Promise<ServerDeploymentReleaseDeployResult>;

export type ServerDeploymentReleaseSmokeExecutor = (input: {
  plan: ServerDeployPlan;
  service: ServerReleaseService;
  cloudflareDeploymentId: string;
}) => Promise<ServerDeploymentReleaseSmokeResult>;

export type ServerDeploymentReleaseFlowResult =
  | {
      accepted: true;
      service: ServerReleaseService;
      serverVersion: string;
      cloudflareDeploymentId: string;
      historyKey: string;
      currentKey: string;
      currentPointerStatus: "CURRENT";
      idempotentReplay: boolean;
    }
  | {
      accepted: false;
      errorCode:
        | ServerDeployPreflightErrorCode
        | "deployment_execution_failed"
        | "deployment_smoke_failed"
        | "deployment_smoke_blocked"
        | "deployment_history_invalid"
        | "deployment_history_immutable_conflict"
        | "deployment_current_pointer_conflict";
      historyKey?: string;
      currentKey?: string;
      currentPointerStatus?: "DEGRADED";
    };

export type ServerDeploymentEvidence = {
  releaseId: string;
  environment: ServerReleaseEnvironment;
  service: ServerReleaseService;
  serverVersion: string;
  commitSha: string;
  releaseBranch: string;
  deploymentTag: string;
  cloudflareDeploymentId: string;
  previousDeploymentId: string | null;
  database: {
    compatibility: ServerDbCompatibility;
    minimumVersionCode?: string;
    migrationRange?: string;
  };
  contentRelease: {
    releaseId: string;
    contentVersion: string;
  } | null;
  verification: {
    localRegression: Extract<ServerVerificationStatus, "PASSED">;
    dryRun: Extract<ServerVerificationStatus, "PASSED">;
    remoteSmoke: ServerVerificationStatus;
  };
  pointerStatus: "CURRENT" | "DEGRADED";
  createdAt: string;
};

export type ServerDeploymentCurrentPointer = {
  environment: ServerReleaseEnvironment;
  service: ServerReleaseService;
  serverVersion: string;
  historyKey: string;
  releaseId: string;
  cloudflareDeploymentId: string;
  updatedAt: string;
};

export type ServerDeploymentHistoryWriteResult =
  | {
      accepted: true;
      historyKey: string;
      currentKey: string;
      currentPointerStatus: "CURRENT";
      idempotentReplay: boolean;
    }
  | {
      accepted: false;
      errorCode: "deployment_history_invalid" | "deployment_history_immutable_conflict" | "deployment_current_pointer_conflict";
      historyKey: string;
      currentKey?: string;
      currentPointerStatus: "DEGRADED";
    };

const safeDeploymentIdPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,160}$/u;

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function stableEqual(a: unknown, b: unknown): boolean {
  return canonicalJson(a as never) === canonicalJson(b as never);
}

function ifNoneMatchStar(): Headers {
  return new Headers({ "If-None-Match": "*" });
}

function ifMatch(etag: string): Headers {
  return new Headers({ "If-Match": etag });
}

function getComparableEtag(object: ServerDeploymentHistoryObject): string | null {
  if (typeof object.etag === "string" && object.etag.length > 0) return object.etag;
  if (typeof object.httpEtag === "string" && object.httpEtag.length > 0) return object.httpEtag.replaceAll("\"", "");
  return null;
}

export function buildServerDeployPreflight(
  value: unknown,
  history: ServerDeployHistoryEntry[],
  target: ServerDeployPreflightTarget,
): { accepted: true; plan: ServerDeployPlan } | { accepted: false; errorCode: ServerDeployPreflightErrorCode } {
  const validation = validateServerDeployPlanForDeployment(value, history);
  if (!validation.accepted) return validation;
  if (validation.plan.environment !== target.environment || !validation.plan.services.includes(target.service)) {
    return { accepted: false, errorCode: "deployment_target_mismatch" };
  }
  return validation;
}

export async function executeServerDeployWithPreflight<TDeployResult>(
  value: unknown,
  history: ServerDeployHistoryEntry[],
  target: ServerDeployPreflightTarget,
  deploy: ServerDeployExecutor<TDeployResult>,
): Promise<
  | { accepted: true; service: ServerReleaseService; serverVersion: string; deployResult: TDeployResult }
  | { accepted: false; errorCode: ServerDeployPreflightErrorCode }
> {
  const preflight = buildServerDeployPreflight(value, history, target);
  if (!preflight.accepted) return preflight;
  return {
    accepted: true,
    service: target.service,
    serverVersion: preflight.plan.serverVersion,
    deployResult: await deploy({ plan: preflight.plan, service: target.service }),
  };
}

export async function runServerDeploymentReleaseFlow(
  value: unknown,
  history: ServerDeployHistoryEntry[],
  target: ServerDeployPreflightTarget,
  bucket: ServerDeploymentHistoryBucket,
  deploy: ServerDeploymentReleaseDeployExecutor,
  smoke: ServerDeploymentReleaseSmokeExecutor,
  options: {
    createdAt: string;
    expectedCurrentEtag?: string | null;
  },
): Promise<ServerDeploymentReleaseFlowResult> {
  const preflight = buildServerDeployPreflight(value, history, target);
  if (!preflight.accepted) return preflight;

  const deployResult = await deploy({ plan: preflight.plan, service: target.service });
  if (!deployResult.accepted) return deployResult;

  const smokeResult = await smoke({
    plan: preflight.plan,
    service: target.service,
    cloudflareDeploymentId: deployResult.cloudflareDeploymentId,
  });
  if (!smokeResult.accepted) return smokeResult;

  const smokePassedPlan: ServerDeployPlan = {
    ...preflight.plan,
    verification: {
      ...preflight.plan.verification,
      remoteSmoke: "PASSED",
    },
  };
  const evidence = buildServerDeploymentEvidence(smokePassedPlan, target.service, {
    cloudflareDeploymentId: deployResult.cloudflareDeploymentId,
    createdAt: options.createdAt,
    ...(deployResult.previousDeploymentId === undefined ? {} : { previousDeploymentId: deployResult.previousDeploymentId }),
  });
  const writeResult = await saveServerDeploymentEvidence(
    bucket,
    evidence,
    options.expectedCurrentEtag === undefined ? {} : { expectedCurrentEtag: options.expectedCurrentEtag },
  );
  if (!writeResult.accepted) return writeResult;

  return {
    accepted: true,
    service: target.service,
    serverVersion: preflight.plan.serverVersion,
    cloudflareDeploymentId: deployResult.cloudflareDeploymentId,
    historyKey: writeResult.historyKey,
    currentKey: writeResult.currentKey,
    currentPointerStatus: writeResult.currentPointerStatus,
    idempotentReplay: writeResult.idempotentReplay,
  };
}

export function buildServerDeploymentHistoryKey(environment: ServerReleaseEnvironment, service: ServerReleaseService, serverVersion: string): string {
  return `server-deployments/${environment}/${service}/${serverVersion}.json`;
}

export function buildServerDeploymentCurrentKey(environment: ServerReleaseEnvironment, service: ServerReleaseService): string {
  return `server-deployments/${environment}/${service}/current.json`;
}

export function parseServerDeploymentEvidence(value: unknown): ServerDeploymentEvidence | null {
  if (!isRecord(value)) return null;
  const plan = parseServerDeployPlan({
    releaseId: value.releaseId,
    environment: value.environment,
    services: [value.service],
    serverVersion: value.serverVersion,
    source: {
      commitSha: value.commitSha,
      releaseBranch: value.releaseBranch,
      workingTreeClean: true,
    },
    deploymentTags: [{ service: value.service, tag: value.deploymentTag }],
    database: value.database,
    ...(value.contentRelease === null ? {} : { contentRelease: value.contentRelease }),
    verification: value.verification,
  });
  if (plan === null) return null;
  if (typeof value.cloudflareDeploymentId !== "string" || !safeDeploymentIdPattern.test(value.cloudflareDeploymentId)) return null;
  if (value.previousDeploymentId !== null && (typeof value.previousDeploymentId !== "string" || !safeDeploymentIdPattern.test(value.previousDeploymentId))) return null;
  if (value.pointerStatus !== "CURRENT" && value.pointerStatus !== "DEGRADED") return null;
  if (typeof value.createdAt !== "string" || Number.isNaN(Date.parse(value.createdAt))) return null;
  const service = plan.services[0];
  const deploymentTag = plan.deploymentTags[0];
  if (service === undefined || deploymentTag === undefined) return null;
  return {
    releaseId: plan.releaseId,
    environment: plan.environment,
    service,
    serverVersion: plan.serverVersion,
    commitSha: plan.source.commitSha,
    releaseBranch: plan.source.releaseBranch,
    deploymentTag: deploymentTag.tag,
    cloudflareDeploymentId: value.cloudflareDeploymentId,
    previousDeploymentId: value.previousDeploymentId,
    database: plan.database,
    contentRelease: plan.contentRelease ?? null,
    verification: plan.verification,
    pointerStatus: value.pointerStatus,
    createdAt: value.createdAt,
  };
}

export function buildServerDeploymentEvidence(
  plan: ServerDeployPlan,
  service: ServerReleaseService,
  input: {
    cloudflareDeploymentId: string;
    previousDeploymentId?: string | null;
    createdAt: string;
  },
): ServerDeploymentEvidence {
  const tag = plan.deploymentTags.find((deploymentTag) => deploymentTag.service === service);
  if (!plan.services.includes(service) || tag === undefined) {
    throw new Error("deployment_target_mismatch");
  }
  if (!safeDeploymentIdPattern.test(input.cloudflareDeploymentId)) {
    throw new Error("cloudflare_deployment_id_invalid");
  }
  if (input.previousDeploymentId !== undefined && input.previousDeploymentId !== null && !safeDeploymentIdPattern.test(input.previousDeploymentId)) {
    throw new Error("previous_deployment_id_invalid");
  }

  return {
    releaseId: plan.releaseId,
    environment: plan.environment,
    service,
    serverVersion: plan.serverVersion,
    commitSha: plan.source.commitSha,
    releaseBranch: plan.source.releaseBranch,
    deploymentTag: tag.tag,
    cloudflareDeploymentId: input.cloudflareDeploymentId,
    previousDeploymentId: input.previousDeploymentId ?? null,
    database: plan.database,
    contentRelease: plan.contentRelease ?? null,
    verification: plan.verification,
    pointerStatus: "CURRENT",
    createdAt: input.createdAt,
  };
}

export function parseServerDeploymentCurrentPointer(value: unknown): ServerDeploymentCurrentPointer | null {
  if (!isRecord(value)) return null;
  if (
    (value.environment !== "dev" && value.environment !== "prd") ||
    (value.service !== "game-api" && value.service !== "admin-api" && value.service !== "async-worker") ||
    typeof value.serverVersion !== "string" ||
    typeof value.historyKey !== "string" ||
    typeof value.releaseId !== "string" ||
    typeof value.cloudflareDeploymentId !== "string" ||
    typeof value.updatedAt !== "string"
  ) {
    return null;
  }
  return {
    environment: value.environment,
    service: value.service,
    serverVersion: value.serverVersion,
    historyKey: value.historyKey,
    releaseId: value.releaseId,
    cloudflareDeploymentId: value.cloudflareDeploymentId,
    updatedAt: value.updatedAt,
  };
}

export async function readServerDeploymentCurrentPointer(
  bucket: ServerDeploymentHistoryBucket,
  environment: ServerReleaseEnvironment,
  service: ServerReleaseService,
): Promise<{ pointer: ServerDeploymentCurrentPointer; etag: string | null } | null> {
  const object = await bucket.get(buildServerDeploymentCurrentKey(environment, service));
  if (object === null) return null;
  const pointer = parseServerDeploymentCurrentPointer(await object.json());
  if (pointer === null || pointer.environment !== environment || pointer.service !== service) return null;
  return {
    pointer,
    etag: getComparableEtag(object),
  };
}

export async function saveServerDeploymentEvidence(
  bucket: ServerDeploymentHistoryBucket,
  evidence: ServerDeploymentEvidence,
  options: { expectedCurrentEtag?: string | null } = {},
): Promise<ServerDeploymentHistoryWriteResult> {
  const parsedEvidence = parseServerDeploymentEvidence(evidence);
  const historyKey = buildServerDeploymentHistoryKey(evidence.environment, evidence.service, evidence.serverVersion);
  const currentKey = buildServerDeploymentCurrentKey(evidence.environment, evidence.service);
  if (parsedEvidence === null) {
    return { accepted: false, errorCode: "deployment_history_invalid", historyKey, currentKey, currentPointerStatus: "DEGRADED" };
  }

  let idempotentReplay = false;
  const existingHistory = await bucket.get(historyKey);
  if (existingHistory !== null) {
    const existingEvidence = parseServerDeploymentEvidence(await existingHistory.json());
    if (existingEvidence === null || !stableEqual(existingEvidence, parsedEvidence)) {
      return { accepted: false, errorCode: "deployment_history_immutable_conflict", historyKey, currentPointerStatus: "DEGRADED" };
    }
    idempotentReplay = true;
  } else {
    const historyWrite = await bucket.put(historyKey, canonicalJson(parsedEvidence as never), {
      onlyIf: ifNoneMatchStar(),
      httpMetadata: { contentType: "application/json", cacheControl: "no-store" },
    });
    if (historyWrite === null) {
      const concurrentHistory = await bucket.get(historyKey);
      const concurrentEvidence = concurrentHistory === null ? null : parseServerDeploymentEvidence(await concurrentHistory.json());
      if (concurrentEvidence === null || !stableEqual(concurrentEvidence, parsedEvidence)) {
        return { accepted: false, errorCode: "deployment_history_immutable_conflict", historyKey, currentPointerStatus: "DEGRADED" };
      }
      idempotentReplay = true;
    }
  }

  const current = await readServerDeploymentCurrentPointer(bucket, evidence.environment, evidence.service);
  const expectedCurrentEtag = options.expectedCurrentEtag === undefined ? current?.etag ?? null : options.expectedCurrentEtag;
  if (options.expectedCurrentEtag !== undefined && current?.etag !== null && current?.etag !== options.expectedCurrentEtag) {
    return { accepted: false, errorCode: "deployment_current_pointer_conflict", historyKey, currentKey, currentPointerStatus: "DEGRADED" };
  }
  const pointer: ServerDeploymentCurrentPointer = {
    environment: evidence.environment,
    service: evidence.service,
    serverVersion: evidence.serverVersion,
    historyKey,
    releaseId: evidence.releaseId,
    cloudflareDeploymentId: evidence.cloudflareDeploymentId,
    updatedAt: evidence.createdAt,
  };
  const pointerWrite = await bucket.put(currentKey, canonicalJson(pointer as never), {
    onlyIf: expectedCurrentEtag === null ? ifNoneMatchStar() : ifMatch(expectedCurrentEtag),
    httpMetadata: { contentType: "application/json", cacheControl: "no-store" },
  });
  if (pointerWrite === null) {
    return { accepted: false, errorCode: "deployment_current_pointer_conflict", historyKey, currentKey, currentPointerStatus: "DEGRADED" };
  }

  return { accepted: true, historyKey, currentKey, currentPointerStatus: "CURRENT", idempotentReplay };
}
