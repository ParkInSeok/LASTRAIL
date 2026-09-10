import { prepareLaunchRewards, launchEntitlementStatements, type LaunchLine } from "./launch-entitlements";
import { readEconomyBinding } from "./economy-binding";
import { planCrystalChange } from "./crystal-wallet";
import { ActivityError } from "./activity-policy";
import { Hono } from "hono";
import type { Context } from "hono";
import { buildOpsErrorLogInsertEnvelope, canonicalJson, clientReleaseRegistrySupportsEnvironment, createRequestId, readVersionForAdmission, sha256Hex } from "@lastrail/common";
import type { ContentReleaseDocument, VersionBucket, VersionDocument } from "@lastrail/common";
import {
  calculateOfflineRewardPreview,
  calculateRunRewardPreview,
  launchOfflineRewardsConfig,
  parseRunRewardsConfig,
  type IntegrityDisposition,
  type RunOutcome,
  type RunRiskTier,
} from "../../../packages/balance/src/index";
import { createAccessToken, createGoogleIdTokenVerifier, GoogleIdTokenVerificationError, hashRefreshToken, verifyAccessToken } from "./auth";
import { isDevGachaScopeAllowed } from "./gacha-dev-scope";
import { registerActivityRoutes } from "./activity-routes";
import { registerCrystalRoutes } from "./crystal-routes";
import { registerGrowthRoutes } from "./growth-routes";
import { registerAccountStateRoutes } from "./account-state";
import { registerFormationRoutes } from "./formation";
import { registerTrainLoadoutRoutes } from "./train-loadout";
import { registerDepartureRoutes, startDeparture } from "./departure";
import { readOfflineEconomy, offlinePolicyGuard } from "./offline-economy";
import { registerGachaReadRoutes } from "./gacha-reads";
import type { GoogleIdentityClaims, GoogleIdTokenVerifier } from "./auth";
import {
  createVerificationCode,
  GmailRecoveryEmailSendError,
  createGmailRecoveryEmailSenderFromEnv,
  hashPassword,
  hashVerificationCode,
  normalizeLoginId,
  normalizeRecoveryEmail,
  validatePassword,
  verifyPasswordHash,
} from "./password-auth";
import type { RecoveryEmailPurpose, RecoveryEmailSender } from "./password-auth";
import { containsForbiddenRealtimeCoopField, createRealtimeCoopRejected, isSafeRealtimeId, RealtimeCoopRoom } from "./realtime-coop";
import type { RealtimeCoopRoomNamespace } from "./realtime-coop";

const app = new Hono<{ Bindings: Env }>();

type GameApiContext = Context<{ Bindings: Env }>;

// Retry atomic uniqueness conflicts from concurrent account/source claims. Hono
// caches parsed request bodies, so each attempt validates the original request.
const withEconomyRetry = (handler: (c: GameApiContext) => Promise<Response>) => async (c: GameApiContext) => {
  for (let attempt = 0; attempt < 4; attempt++) {
    try { return await handler(c); }
    catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      if (/UNIQUE constraint failed/iu.test(String(error))) {
        if (attempt < 3) continue;
        return c.json({ accepted: false, errorCode: "economy_write_conflict" }, 409);
      }
      throw error;
    }
  }
  return c.json({ accepted: false, errorCode: "economy_write_conflict" }, 409);
};

type GameApiEnv = Env & {
  AUTH_SIGNING_KEY_CURRENT?: string;
  GOOGLE_AUTH_AUDIENCES?: string;
  GOOGLE_ID_TOKEN_VERIFIER?: GoogleIdTokenVerifier;
  RECOVERY_EMAIL_SENDER?: RecoveryEmailSender;
  RECOVERY_EMAIL_PROVIDER?: string;
  GMAIL_RECOVERY_SENDER_EMAIL?: string;
  GMAIL_RECOVERY_OAUTH_CLIENT_ID?: string;
  GMAIL_RECOVERY_OAUTH_CLIENT_SECRET?: string;
  GMAIL_RECOVERY_OAUTH_REFRESH_TOKEN?: string;
  REALTIME_COOP_ROOMS?: RealtimeCoopRoomNamespace;
  VERSION_BUCKET?: VersionBucket;
  CLIENT_ADMISSION_MODE?: "DISABLED" | "ENFORCED";
};

type RunVersionSnapshotSource = "server_accepted_run_start" | "server_accepted_run_finish";

type RunVersionSnapshot = {
  source: RunVersionSnapshotSource;
  clientInstalled: {
    appVersion: string | null;
    versionCode: string | null;
    platform: string | null;
    channel: string | null;
  };
  clientAdmission: {
    status: string;
    minimumVersionCode: string | null;
    updateRequired: boolean;
  };
  serverAcceptedDeployment: {
    environment: "LOCAL" | "DEV" | "PRD";
    runtimeDeploymentId: string | null;
  };
  contentRelease: {
    status: string;
    releaseId: string | null;
    contentVersion: string | null;
    catalogHash: string | null;
  };
  capturedAt: string;
};

const apiBaseUrls = {
  LOCAL: "http://localhost:8787",
  DEV: "https://dev-api.lastrail.app",
  PRD: "https://api.lastrail.app",
} as const;

function normalizeClientConfigHeader(value: string | undefined): string | null {
  if (value === undefined) return null;
  const normalized = value.trim();
  if (normalized === "" || normalized.length > 64) return null;
  if (!/^[A-Za-z0-9._:-]+$/u.test(normalized)) return null;
  return normalized;
}

function normalizeInstalledVersionCode(value: string | undefined): string | null {
  const normalized = normalizeClientConfigHeader(value);
  if (normalized === null || !/^[1-9]\d{0,9}$/u.test(normalized)) return null;
  if (Number(normalized) > 2147483647) return null;
  return normalized;
}

function createEmptyContentRelease(status: "UNCONFIGURED" | "INCOMPATIBLE" | "NOT_APPROVED") {
  return {
    status,
    releaseId: null,
    contentVersion: null,
    catalogUrl: null,
    catalogHash: null,
    compatibility: null,
  };
}

function contentReleaseCompatible(contentRelease: ContentReleaseDocument, installedVersionCode: string): boolean {
  const installed = Number(installedVersionCode);
  if (installed < Number(contentRelease.minimumVersionCode)) return false;
  if (contentRelease.maximumVersionCode !== undefined && installed > Number(contentRelease.maximumVersionCode)) return false;
  return true;
}

function serializeCompatibleContentRelease(contentRelease: ContentReleaseDocument) {
  return {
    status: "APPROVED_COMPATIBLE",
    releaseId: contentRelease.releaseId,
    contentVersion: contentRelease.contentVersion,
    catalogUrl: contentRelease.catalogUrl,
    catalogHash: contentRelease.catalogHash,
    compatibility: {
      minimumVersionCode: contentRelease.minimumVersionCode,
      maximumVersionCode: contentRelease.maximumVersionCode ?? null,
    },
  };
}

function createClientConfigResponse(input: {
  env: GameApiEnv;
  version: VersionDocument | null;
  installedAppVersion: string | null;
  installedVersionCode: string | null;
  platform: string | null;
  channel: string | null;
}) {
  const base = {
    environment: input.env.ENVIRONMENT,
    apiBaseUrl: apiBaseUrls[input.env.ENVIRONMENT],
    balanceConfigEndpointTemplate: "/v1/balance-config/:version",
    minimumClientVersion: "0.1.0",
    maintenance: { enabled: false },
  };

  if (input.version === null || input.version.minimumVersionCode === undefined) {
    return {
      ...base,
      accepted: false,
      serverVersion: null,
      clientAdmission: {
        status: "VERSION_POLICY_UNCONFIGURED",
        installedAppVersion: input.installedAppVersion,
        installedVersionCode: input.installedVersionCode,
        minimumVersionCode: null,
        updateRequired: true,
        platform: input.platform,
        channel: input.channel,
      },
      contentRelease: createEmptyContentRelease("UNCONFIGURED"),
      errorCode: "version_policy_unconfigured",
      message: "Version policy is not configured.",
    };
  }

  const serverVersion = {
    version: input.version.version,
    versionCode: input.version.versionCode,
    minimumVersionCode: input.version.minimumVersionCode,
  };

  if (input.installedVersionCode === null) {
    return {
      ...base,
      accepted: false,
      serverVersion,
      clientAdmission: {
        status: "APP_VERSION_REQUIRED",
        installedAppVersion: input.installedAppVersion,
        installedVersionCode: null,
        minimumVersionCode: input.version.minimumVersionCode,
        updateRequired: true,
        platform: input.platform,
        channel: input.channel,
      },
      contentRelease: createEmptyContentRelease("UNCONFIGURED"),
      errorCode: "app_version_required",
      message: "Installed app version code is required.",
    };
  }

  const updateRequired = Number(input.installedVersionCode) < Number(input.version.minimumVersionCode);
  if (updateRequired) {
    return {
      ...base,
      accepted: false,
      serverVersion,
      clientAdmission: {
        status: "UPDATE_REQUIRED",
        installedAppVersion: input.installedAppVersion,
        installedVersionCode: input.installedVersionCode,
        minimumVersionCode: input.version.minimumVersionCode,
        updateRequired: true,
        platform: input.platform,
        channel: input.channel,
      },
      contentRelease: createEmptyContentRelease("INCOMPATIBLE"),
      errorCode: "client_update_required",
      message: "Client update is required.",
    };
  }

  const contentRelease = input.version.contentRelease;
  if (contentRelease === undefined) {
    return {
      ...base,
      accepted: false,
      serverVersion,
      clientAdmission: {
        status: "ACCEPTED",
        installedAppVersion: input.installedAppVersion,
        installedVersionCode: input.installedVersionCode,
        minimumVersionCode: input.version.minimumVersionCode,
        updateRequired: false,
        platform: input.platform,
        channel: input.channel,
      },
      contentRelease: createEmptyContentRelease("UNCONFIGURED"),
      errorCode: "content_release_unconfigured",
      message: "Approved content release is not configured.",
    };
  }

  if (!contentReleaseCompatible(contentRelease, input.installedVersionCode)) {
    return {
      ...base,
      accepted: false,
      serverVersion,
      clientAdmission: {
        status: "ACCEPTED",
        installedAppVersion: input.installedAppVersion,
        installedVersionCode: input.installedVersionCode,
        minimumVersionCode: input.version.minimumVersionCode,
        updateRequired: false,
        platform: input.platform,
        channel: input.channel,
      },
      contentRelease: createEmptyContentRelease("INCOMPATIBLE"),
      errorCode: "content_release_incompatible",
      message: "Approved content release is not compatible with this app version.",
    };
  }

  return {
    ...base,
    accepted: true,
    serverVersion,
    clientAdmission: {
      status: "ACCEPTED",
      installedAppVersion: input.installedAppVersion,
      installedVersionCode: input.installedVersionCode,
      minimumVersionCode: input.version.minimumVersionCode,
      updateRequired: false,
      platform: input.platform,
      channel: input.channel,
    },
    contentRelease: serializeCompatibleContentRelease(contentRelease),
    errorCode: null,
    message: null,
  };
}

function isClientAdmissionProtectedRoute(pathname: string): boolean {
  if (pathname === "/status" || pathname === "/v1/client-config" || pathname === "/v1/client-config/authorized") return false;
  if (pathname.startsWith("/v1/auth/password/recovery-email/verify/")) return false;
  if (pathname.startsWith("/v1/auth/password/reset/")) return false;
  if (pathname.startsWith("/v1/client-errors")) return false;
  return pathname.startsWith("/v1/");
}

function clientAdmissionHttpStatus(errorCode: string | null): 400 | 401 | 403 | 409 | 426 | 503 {
  if (errorCode === "authentication_required") return 401;
  if (["client_version_unregistered", "client_version_disabled", "tester_forbidden"].includes(errorCode ?? "")) return 403;
  if (["content_release_pin_required", "content_release_pin_mismatch"].includes(errorCode ?? "")) return 409;
  if (errorCode === "app_version_required") return 400;
  if (errorCode === "client_update_required") return 426;
  return 503;
}

async function createClientAdmissionForRequest(c: GameApiContext) {
  const env = c.env as GameApiEnv;
  let version: VersionDocument | null = null;
  let revision: string | null = null;
  if (env.VERSION_BUCKET !== undefined) {
    try {
      ({ version, revision } = await readVersionForAdmission(env.VERSION_BUCKET));
      if (!clientReleaseRegistrySupportsEnvironment(version?.clientReleaseRegistry, env.ENVIRONMENT)) version = null;
    } catch {
      version = null;
    }
  }
  const code = normalizeInstalledVersionCode(c.req.header("X-LastRail-App-Version-Code"));
  const registry = version?.clientReleaseRegistry;
  const entry = registry?.entries.find((row) => row.versionCode === code);
  let selectedVersion = version;
  if (registry && version) {
    selectedVersion = { ...version };
    delete selectedVersion.contentRelease;
    if (entry) selectedVersion.contentRelease = entry.contentRelease;
  }
  const response = createClientConfigResponse({
    env,
    version: selectedVersion,
    installedAppVersion: normalizeClientConfigHeader(c.req.header("X-LastRail-App-Version")),
    installedVersionCode: normalizeInstalledVersionCode(c.req.header("X-LastRail-App-Version-Code")),
    platform: normalizeClientConfigHeader(c.req.header("X-LastRail-App-Platform")),
    channel: normalizeClientConfigHeader(c.req.header("X-LastRail-App-Channel")),
  });
  const result = { ...response, registryRevision: registry ? revision : null,
    clientAdmission: { ...response.clientAdmission, distribution: entry?.status ?? null },
    bootstrap: { canAuthenticate: response.accepted, contentAuthorized: response.accepted, authenticationRequired: false } };
  if (!registry) return result;
  const deny = (status: string, errorCode: string, message: string, canAuthenticate = false) => ({ ...result,
    accepted: false, clientAdmission: { ...result.clientAdmission, status }, contentRelease: createEmptyContentRelease("NOT_APPROVED"),
    bootstrap: { canAuthenticate, contentAuthorized: false, authenticationRequired: env.ENVIRONMENT === "PRD" && entry?.status === "INTERNAL" }, errorCode, message });
  if (!code || response.errorCode === "client_update_required") return result;
  if (!entry) return deny("UNREGISTERED", "client_version_unregistered", "This exact version code is not registered.");
  if (entry.status === "DISABLED") return deny("DISABLED", "client_version_disabled", "This version code is disabled.");
  if (entry.status === "STABLE") return result;
  if (env.ENVIRONMENT !== "PRD") return deny("NOT_CONFIGURED", "version_policy_unconfigured", "This distribution is not supported in this environment.");
  const auth = await resolveAuthContext(c);
  if (!("accountId" in auth)) return deny("AUTH_REQUIRED", "authentication_required", "Authenticate before requesting internal content.", true);
  const tester = await env.DB.prepare(`SELECT account_id FROM account_tester_entitlements
    WHERE account_id = ? AND environment = ? AND version_code = ? AND status = 'ACTIVE'
    AND revoked_at IS NULL AND expires_at > unixepoch('subsec') * 1000`)
    .bind(auth.accountId, env.ENVIRONMENT, code).first();
  if (!tester) return deny("TESTER_FORBIDDEN", "tester_forbidden", "An active tester entitlement is required.", true);
  return result;
}

function isRegistryBootstrapRoute(method: string, path: string): boolean {
  return new Set([
    "POST /v1/auth/guest", "POST /v1/auth/password/register", "POST /v1/auth/password/login",
    "POST /v1/auth/google", "POST /v1/auth/refresh",
  ]).has(`${method} ${path}`);
}

function isRegistryHydrationRoute(method: string, path: string): boolean {
  return new Set(["GET /v1/profile", "GET /v1/profile/display-name/check", "POST /v1/profile/display-name", "GET /v1/story-progress"])
    .has(`${method} ${path}`);
}

function isObjectRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function createRunVersionSnapshotFromAdmission(
  admission: Awaited<ReturnType<typeof createClientAdmissionForRequest>>,
  env: GameApiEnv,
  capturedAtMs: number,
  source: RunVersionSnapshotSource,
): RunVersionSnapshot | null {
  if (admission.accepted !== true) return null;
  const clientAdmission = admission.clientAdmission;
  const contentRelease = admission.contentRelease;
  return {
    source,
    clientInstalled: {
      appVersion: clientAdmission.installedAppVersion,
      versionCode: clientAdmission.installedVersionCode,
      platform: clientAdmission.platform,
      channel: clientAdmission.channel,
    },
    clientAdmission: {
      status: clientAdmission.status,
      minimumVersionCode: clientAdmission.minimumVersionCode,
      updateRequired: clientAdmission.updateRequired,
    },
    serverAcceptedDeployment: {
      environment: env.ENVIRONMENT,
      runtimeDeploymentId: (env as GameApiEnv & { CF_VERSION_METADATA?: { id: string } }).CF_VERSION_METADATA?.id ?? null,
    },
    contentRelease: {
      status: contentRelease.status,
      releaseId: contentRelease.releaseId,
      contentVersion: contentRelease.contentVersion,
      catalogHash: contentRelease.catalogHash,
    },
    capturedAt: toIso(capturedAtMs),
  };
}

async function createRunVersionSnapshotForAcceptedRequest(c: GameApiContext, capturedAtMs: number): Promise<RunVersionSnapshot | null> {
  const env = c.env as GameApiEnv;
  if (env.CLIENT_ADMISSION_MODE !== "ENFORCED") return null;
  return createRunVersionSnapshotFromAdmission(
    await createClientAdmissionForRequest(c),
    env,
    capturedAtMs,
    "server_accepted_run_start",
  );
}

async function createRunFinishVersionSnapshotForAcceptedRequest(c: GameApiContext, capturedAtMs: number): Promise<RunVersionSnapshot | null> {
  const env = c.env as GameApiEnv;
  if (env.CLIENT_ADMISSION_MODE !== "ENFORCED") return null;
  return createRunVersionSnapshotFromAdmission(
    await createClientAdmissionForRequest(c),
    env,
    capturedAtMs,
    "server_accepted_run_finish",
  );
}

function parseRunVersionSnapshot(value: unknown, expectedSource?: RunVersionSnapshotSource): RunVersionSnapshot | null {
  if (!isObjectRecord(value)) return null;
  if (value.source !== "server_accepted_run_start" && value.source !== "server_accepted_run_finish") return null;
  if (expectedSource !== undefined && value.source !== expectedSource) return null;
  const clientInstalled = isObjectRecord(value.clientInstalled) ? value.clientInstalled : {};
  const clientAdmission = isObjectRecord(value.clientAdmission) ? value.clientAdmission : null;
  const serverAcceptedDeployment = isObjectRecord(value.serverAcceptedDeployment) ? value.serverAcceptedDeployment : {};
  const contentRelease = isObjectRecord(value.contentRelease) ? value.contentRelease : {};
  if (clientAdmission === null || typeof clientAdmission.status !== "string") return null;
  if (typeof clientAdmission.updateRequired !== "boolean") return null;
  if (typeof value.capturedAt !== "string") return null;
  return {
    source: value.source,
    clientInstalled: {
      appVersion: typeof clientInstalled.appVersion === "string" ? clientInstalled.appVersion : null,
      versionCode: typeof clientInstalled.versionCode === "string" ? clientInstalled.versionCode : null,
      platform: typeof clientInstalled.platform === "string" ? clientInstalled.platform : null,
      channel: typeof clientInstalled.channel === "string" ? clientInstalled.channel : null,
    },
    clientAdmission: {
      status: clientAdmission.status,
      minimumVersionCode: typeof clientAdmission.minimumVersionCode === "string" ? clientAdmission.minimumVersionCode : null,
      updateRequired: clientAdmission.updateRequired,
    },
    serverAcceptedDeployment: {
      environment: serverAcceptedDeployment.environment === "PRD" ? "PRD" : serverAcceptedDeployment.environment === "LOCAL" ? "LOCAL" : "DEV",
      runtimeDeploymentId: typeof serverAcceptedDeployment.runtimeDeploymentId === "string" ? serverAcceptedDeployment.runtimeDeploymentId : null,
    },
    contentRelease: {
      status: typeof contentRelease.status === "string" ? contentRelease.status : "UNCONFIGURED",
      releaseId: typeof contentRelease.releaseId === "string" ? contentRelease.releaseId : null,
      contentVersion: typeof contentRelease.contentVersion === "string" ? contentRelease.contentVersion : null,
      catalogHash: typeof contentRelease.catalogHash === "string" ? contentRelease.catalogHash : null,
    },
    capturedAt: value.capturedAt,
  };
}

function getStoredRunStartVersionSnapshot(snapshotJson: string | null | undefined): RunVersionSnapshot | null {
  if (typeof snapshotJson !== "string") return null;
  try {
    const snapshot = JSON.parse(snapshotJson) as unknown;
    if (!isObjectRecord(snapshot)) return null;
    return parseRunVersionSnapshot(snapshot.runStartVersionSnapshot, "server_accepted_run_start") ??
      parseRunVersionSnapshot(snapshot.runVersionSnapshot, "server_accepted_run_start");
  } catch {
    return null;
  }
}

function getStoredRunFinishVersionSnapshot(finalPayloadJson: string | null | undefined): RunVersionSnapshot | null {
  if (typeof finalPayloadJson !== "string") return null;
  try {
    const payload = JSON.parse(finalPayloadJson) as unknown;
    if (!isObjectRecord(payload)) return null;
    return parseRunVersionSnapshot(payload.runFinishVersionSnapshot, "server_accepted_run_finish");
  } catch {
    return null;
  }
}

function getStoredRunStartVersionSnapshotFromFinishPayload(finalPayloadJson: string | null | undefined): RunVersionSnapshot | null {
  if (typeof finalPayloadJson !== "string") return null;
  try {
    const payload = JSON.parse(finalPayloadJson) as unknown;
    if (!isObjectRecord(payload)) return null;
    return parseRunVersionSnapshot(payload.runStartVersionSnapshot, "server_accepted_run_start") ??
      parseRunVersionSnapshot(payload.runVersionSnapshot, "server_accepted_run_start");
  } catch {
    return null;
  }
}

type BalanceConfigRow = {
  version: number;
  schema_version: number;
  required_content_version: number;
  checksum: string;
  config_json: string;
  published_at: number;
};

type RewardPreviewRow = {
  id: string;
  account_id: string;
  run_id: string;
  status: "ACTIVE" | "CLAIMED" | "EXPIRED" | "INVALIDATED";
  balance_config_version: number;
  reward_plan_json: string;
  expires_at: number;
  created_at: number;
  claimed_at: number | null;
  reward_claim_id: string | null;
};

type RunFinishRow = {
  id: string;
  account_id: string;
  status: "ACTIVE" | "FINISH_PENDING" | "COMPLETED" | "FAILED" | "QUARANTINED" | "EXPIRED" | "ABANDONED";
  balance_config_version: number;
  objective: string;
  risk: string;
  preview_json: string;
  config_json: string;
  snapshot_json: string | null;
};

type RunStartPreviewRow = {
  admission_version?: string | null;
  id: string;
  account_id: string;
  mutation_id: string;
  objective: string;
  risk: string;
  balance_config_version: number;
  preview_json: string;
  status: "ACTIVE" | "CONSUMED" | "EXPIRED" | "CANCELLED";
  expires_at: number;
  created_at: number;
};

type CurrencyRewardLine = {
  kind: "CURRENCY";
  id: string;
  quantity: number;
  source?: string;
  adBonusEligible?: boolean;
};

type ItemRewardLine = {
  kind: "ITEM";
  id: string;
  quantity: number;
  source?: string;
};

type AccountMutationSequenceRow = {
  sequence: number | null;
};

type AccountCurrencyRow = {
  balance: number;
};

type FuelStateRow = {
  fuel: number;
  max_fuel: number;
};

type IdempotencyRecordRow = {
  result_ref: string;
  created_at: number;
  reward_created_at: number | null;
  balance_config_version?: number | null;
  preview_run_id?: string | null;
  final_payload_json?: string | null;
};

type AuthGuestReplayRow = {
  account_id: string;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  session_id: string;
  session_status: "ACTIVE" | "ROTATED" | "REUSED" | "REVOKED";
  session_expires_at: number;
  created_at: number;
};

type RunStartIdempotencyRecordRow = {
  result_ref: string;
  created_at: number;
  preview_id: string | null;
  snapshot_json: string | null;
};

type OfflineRewardReplayRow = {
  result_ref: string;
  created_at: number;
  reward_created_at: number | null;
  reward_json: string | null;
};

type LoginBonusClaimRow = {
  reward_transaction_id: string;
  claimed_at: number;
  reward_json: string;
};

type SupportTicketCategory = "ACCOUNT_LOGIN" | "RUN_REWARD" | "GACHA_RECRUITMENT" | "STORY_PROGRESS" | "BUG_OTHER";

type SupportTicketRow = {
  id: string;
  account_id: string;
  category: SupportTicketCategory;
  status: "OPEN" | "IN_REVIEW" | "CLOSED";
  body_text: string;
  body_preview: string;
  body_length: number;
  app_version: string | null;
  app_version_code: string | null;
  client_composed_at: number | null;
  client_submitted_at: number | null;
  server_received_at: number;
  idempotency_key: string;
  request_hash: string;
  created_at: number;
  updated_at: number;
};

type ClientErrorKind = "UNITY_ERROR" | "UNITY_EXCEPTION" | "UNITY_ASSERT";

type ClientErrorEvent = {
  kind: ClientErrorKind;
  occurredAt: number;
  occurrenceCount: number;
  appVersion: string | null;
  appVersionCode: string | null;
  scope: "AUTHENTICATED_CURRENT_SESSION";
};

type FirstClearRow = {
  cleared_at: number;
};

type AccountStoryProgressRow = {
  act_id?: string;
  highest_cleared_operation_id?: string;
  highest_story_progress_id: string;
  cleared_operations_json: string;
  story_flags_json: string;
  narrative_version?: string | null;
  last_progress_run_id?: string | null;
  updated_at?: number;
};

type AccountOfflineRewardStateRow = {
  last_offline_reward_claim_at: number;
  last_story_progress_id: string;
  last_balance_config_version: number | null;
  last_narrative_version: string | null;
  last_previewed_at: number | null;
  revision: number;
  updated_at: number;
};

type AccountUnlockRow = {
  unlock_id: string;
};

type RunFinishBody = {
  runId: string;
  currentStage: "Complete";
  currentWaveNumber: number;
  completedWaveCount: number;
  encounterType?: "ORDINARY_15_WAVE" | "RAID_BOSS";
  raidResult?: {
    raidId?: string;
    bossId?: string;
    bossDefeated: boolean;
    completedPhaseCount?: number;
    encounterDurationSeconds?: number;
  };
  outcome: RunOutcome;
  runtimeStateJson: string;
  tacticSelections: unknown[];
  clientCompletedAt: string;
};

type StoryRaidStaminaRefundCandidate = {
  status: "COMMITTED";
  refundType: "STAMINA";
  reason: "STORY_RAID_FAILURE";
  policyRef: "act1_story_raid_failure_stamina_refund_v1";
};

type GachaBannerRow = {
  id: string;
  banner_type: "STANDARD" | "PICKUP";
  status: "DRAFT" | "ACTIVE" | "ENDED" | "DISABLED";
  balance_config_version: number;
  pickup_operator_id: string | null;
  starts_at: number;
  ends_at: number | null;
  pity_config_json: string;
  pool_snapshot_json: string;
};

type AuthSessionAccountRow = {
  account_id: string;
  session_id: string;
  session_status: "ACTIVE" | "ROTATED" | "REUSED" | "REVOKED" | "EXPIRED";
  session_expires_at: number;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  account_created_at?: number;
};

type RefreshReplaySessionRow = {
  result_ref: string;
  created_at: number;
  account_id: string;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
  session_id: string;
  session_status: "ACTIVE" | "ROTATED" | "REUSED" | "REVOKED";
  session_expires_at: number;
};

type RefreshSessionAccountRow = AuthSessionAccountRow & {
  refresh_token_hash: string;
  family_id: string;
  generation: number;
};

type AuthIdentityAccountRow = {
  account_id: string;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
};

type PasswordCredentialAccountRow = {
  account_id: string;
  password_hash: string;
  password_hash_algorithm: "ARGON2ID" | "SCRYPT" | "BCRYPT" | "PBKDF2_SHA256";
  recovery_email_normalized: string;
  account_status: "ACTIVE" | "SUSPENDED" | "BANNED" | "DELETED";
};

type RecoveryEmailChallengeRow = {
  id: string;
  account_id: string | null;
  email_normalized: string;
  purpose: RecoveryEmailPurpose;
  verifier_hash: string;
  status: "PENDING" | "CONSUMED" | "EXPIRED" | "CANCELLED";
  expires_at: number;
  created_at: number;
};

type AuthContext = {
  accountId: string;
  sessionId: string;
  accountCreatedAt: number;
};

type PlayerProfileRow = {
  account_id: string;
  display_name: string | null;
  display_name_normalized?: string | null;
  display_name_updated_at?: number | null;
  commander_xp: number;
  created_at: number;
  updated_at: number;
};

type DisplayNameOwnerRow = {
  account_id: string;
};

type GachaPityState = {
  totalPullCount: number;
  elitePityCount: number;
  pickupElitePityCount: number;
  rareOrHigherPityCount: number;
  revision: number;
};

type GachaPityRow = {
  total_pull_count: number;
  elite_pity_count: number;
  pickup_elite_pity_count: number;
  rare_or_higher_pity_count: number;
  revision: number;
};

type GachaPityConfig = {
  elite: number;
  pickup: number;
  rare: number;
};

type GachaPoolResult = {
  resultRarity: "COMMON" | "RARE" | "ELITE";
  resultType: "ELITE_OPERATOR" | "RARE_OPERATOR" | "ELITE_OPERATOR_SHARD" | "RARE_OPERATOR_SHARD" | "OPERATOR_TOKEN" | "TRAINING_MATERIAL" | "WORK_SKILL_MATERIAL" | "RARE_OPERATOR_TOKEN" | "TACTICAL_MATERIAL";
  resultId: string;
  weight?: number;
  isPickup?: boolean;
  quantity?: number;
};

type GachaCommittedResult = {
  id: string;
  pullSequence: number;
  resultRarity: GachaPoolResult["resultRarity"];
  resultType: GachaPoolResult["resultType"];
  resultId: string;
  reward: Record<string, unknown>;
  isPickup: boolean;
  guaranteeType: "NONE" | "RARE_OR_HIGHER_10" | "ELITE_30" | "PICKUP_ELITE_60";
  wasDuplicate: boolean;
  duplicateConversion: Record<string, unknown> | null;
  operatorId: string | null;
  itemLedgerId: string | null;
  progressionLedgerId: string | null;
};

type AccountItemRow = {
  quantity: number;
};

type ExistingOperatorRow = {
  operator_id: string;
};

type GachaGuaranteeType = GachaCommittedResult["guaranteeType"];

type GachaReplayBatchRow = {
  id: string;
  banner_id: string;
  balance_config_version: number;
  pull_count: number;
  cost_item_id: string;
  cost_quantity: number;
  post_pity_json: string;
  result_summary_json: string;
  created_at: number;
};

type GachaReplayResultRow = {
  pull_sequence: number;
  result_rarity: GachaPoolResult["resultRarity"];
  result_type: GachaPoolResult["resultType"];
  result_id: string;
  is_pickup: number;
  guarantee_type: GachaCommittedResult["guaranteeType"];
  was_duplicate: number;
  duplicate_conversion_json: string | null;
  reward_quantity?: number | null;
};

type AuthFailureStatus = 400 | 401 | 403 | 409 | 500 | 503;

type AuthFailure = {
  status: AuthFailureStatus;
  errorCode: string;
  message: string;
};

const currencyIdToD1Currency = {
  cur_credits: "CREDITS",
  cur_machine_parts: "MACHINE_PARTS",
  cur_tactical_data: "TACTICAL_DATA",
  cur_special_core: "SPECIAL_CORE",
  cur_operator_exp: "OPERATOR_EXP",
} as const;

const runRewardPreviewTtlMs = 24 * 60 * 60 * 1000;
const launchLoginBonusId = "launch_login_bonus_act1_v1";
const launchLoginBonusRewardLine = {
  kind: "ITEM",
  id: "cur_command_ticket",
  quantity: 5,
  source: "LAUNCH_LOGIN_BONUS",
} as const;
const supportTicketRateWindowMs = 10 * 60 * 1000;
const supportTicketRateLimitPerWindow = 3;
const clientErrorIngestRateWindowMs = 10 * 60 * 1000;
const clientErrorIngestRateLimitPerWindow = 12;
const supportTicketCategories = new Set<SupportTicketCategory>([
  "ACCOUNT_LOGIN",
  "RUN_REWARD",
  "GACHA_RECRUITMENT",
  "STORY_PROGRESS",
  "BUG_OTHER",
]);
const clientErrorKinds = new Set<ClientErrorKind>(["UNITY_ERROR", "UNITY_EXCEPTION", "UNITY_ASSERT"]);
const clientErrorEventKeys = new Set(["kind", "occurredAt", "occurrenceCount", "appVersion", "appVersionCode", "scope"]);
const runFinishForbiddenRewardFields = new Set([
  "rewardAmount",
  "rewardAmounts",
  "rewardLines",
  "rewardClaimId",
  "rewardPreviewId",
  "currencyGrants",
  "itemGrants",
  "ledgerIds",
  "firstClearEligible",
  "firstClearApplied",
  "storyProgress",
  "highestStoryProgressId",
  "unlockedOperations",
  "narrativeFlags",
  "storyFlags",
  "railChoiceResults",
  "railChoices",
  "choiceResults",
  "optionalFlags",
  "startAugmentPackageRef",
  "startTacticPackageRef",
  "rawAnimationTimeline",
  "animationTimeline",
  "frameData",
  "vfxEvents",
  "sfxEvents",
  "cameraEvents",
  "hitStopEvents",
  "rawRng",
  "rawRngMaterial",
  "probability",
  "validatorDetails",
]);
const runFinishNestedForbiddenFields = new Set([
  ...runFinishForbiddenRewardFields,
  "rewardAmount",
  "rewardAmounts",
  "currencyGrants",
  "itemGrants",
  "economyGrants",
  "ledgerIds",
]);

const actOneStoryProgressOrder = [
  "none",
  "opn_01_last_departure",
  "opn_02_first_signal",
  "opn_03_water_stop",
  "opn_04_scrapstar_show",
  "opn_05_night_boarders",
  "opn_06_broken_line",
  "raid_raider_king_engine",
] as const;

const actOneOperationIds = actOneStoryProgressOrder.filter((id) => id !== "none");
type ActOneStoryProgressId = typeof actOneStoryProgressOrder[number];
const actOneNarrativeVersion = "act1.2026-08-27.001";
const offlineRewardForbiddenQueryKeys = [
  "accountId",
  "account_id",
  "storyProgress",
  "story_progress",
  "storyProgressId",
  "story_progress_id",
  "highestStoryProgressId",
  "risk",
  "offlineSeconds",
  "offline_seconds",
  "actualOfflineSeconds",
  "rewardAmount",
  "rewardAmounts",
  "rewardLines",
  "currencyGrants",
  "itemGrants",
  "economyGrants",
];

function parsePositiveInteger(value: string): number | null {
  if (!/^[1-9]\d*$/u.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

function toIso(ms: number): string {
  return new Date(ms).toISOString();
}

function toNullableIso(ms: number | null | undefined): string | null {
  return ms === null || ms === undefined ? null : toIso(ms);
}

function normalizeOptionalText(value: unknown, maxLength: number): string | null | "INVALID" {
  if (value === undefined || value === null) return null;
  if (typeof value !== "string") return "INVALID";
  const normalized = value.trim();
  if (normalized.length === 0) return null;
  if (normalized.length > maxLength) return "INVALID";
  return normalized;
}

function parseOptionalIsoMs(value: unknown): number | null | "INVALID" {
  if (value === undefined || value === null) return null;
  if (typeof value !== "string" || value.trim().length === 0) return "INVALID";
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : "INVALID";
}

function createDomainId(prefix: string): string {
  return `${prefix}_${createRequestId().slice(4)}`;
}

function d1Changes(result: unknown): number | null {
  if (result === null || typeof result !== "object") return null;
  const meta = "meta" in result ? result.meta : null;
  if (meta === null || typeof meta !== "object") return null;
  const changes = "changes" in meta ? meta.changes : null;
  return typeof changes === "number" ? changes : null;
}

function createRefreshToken(): string {
  return `refresh_${createRequestId().slice(4)}${createRequestId().slice(4)}`;
}

async function createReplaySafeRefreshToken(secret: string, scope: string, material: string): Promise<string> {
  return `refresh_replay_${await sha256Hex(`${scope}:${secret}:${material}`)}`;
}

function getOpsLogEnvironment(env: Env): "DEV" | "PRD" {
  return env.ENVIRONMENT === "PRD" ? "PRD" : "DEV";
}

async function writeGameApiOpsErrorLog(c: GameApiContext, input: {
  severity: "error" | "critical";
  errorCode: string;
  messageSummary: string;
  requestId?: string | null;
  accountId?: string | null;
  runId?: string | null;
  gachaBatchId?: string | null;
  gachaBannerId?: string | null;
  storyProgressId?: string | null;
  idempotencyOperation?: string | null;
  idempotencyKey?: string | null;
  correlation?: Record<string, string | number | boolean | null>;
  context?: Record<string, string | number | boolean | null>;
  retryable?: boolean;
}): Promise<void> {
  try {
    const now = Date.now();
    const envelope = await buildOpsErrorLogInsertEnvelope({
      id: createDomainId("ops_error"),
      occurredAtMs: now,
      environment: getOpsLogEnvironment(c.env),
      service: "game_api",
      serverVersion: (c.env as Env & { CF_VERSION_METADATA?: { id: string } }).CF_VERSION_METADATA?.id ?? null,
      severity: input.severity,
      errorCode: input.errorCode,
      messageSummary: input.messageSummary,
      source: "game_api",
      requestId: input.requestId ?? c.req.header("X-Request-Id") ?? null,
      accountId: input.accountId ?? null,
      runId: input.runId ?? null,
      gachaBatchId: input.gachaBatchId ?? null,
      gachaBannerId: input.gachaBannerId ?? null,
      storyProgressId: input.storyProgressId ?? null,
      idempotencyOperation: input.idempotencyOperation ?? null,
      idempotencyKey: input.idempotencyKey ?? null,
      correlation: input.correlation ?? {},
      context: input.context ?? {},
      retryable: input.retryable ?? false,
      createdAtMs: now,
    });
    await c.env.DB.prepare(
      `INSERT INTO ops_error_logs (
         id, occurred_at, environment, service, severity, error_code, message_summary,
         status, source, request_id, account_id, run_id, reward_preview_id, reward_claim_id,
         offline_reward_claim_id, gacha_batch_id, gacha_banner_id, story_progress_id,
         integrity_event_id, support_ticket_id, idempotency_operation, idempotency_key_hash,
         correlation_json, sanitized_context_json, retryable, masked, payload_ref, trace_ref,
         resolved_at, resolved_by, resolution_note, created_at
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, NULL, NULL, ?, ?, ?, NULL, NULL, ?, ?, ?, ?, ?, ?, NULL, NULL, NULL, NULL, NULL, ?)`,
    ).bind(
      envelope.id,
      envelope.occurredAt,
      envelope.environment,
      envelope.service,
      envelope.severity,
      envelope.errorCode,
      envelope.messageSummary,
      envelope.status,
      envelope.source,
      envelope.requestId,
      envelope.accountId,
      envelope.runId,
      envelope.gachaBatchId,
      envelope.gachaBannerId,
      envelope.storyProgressId,
      envelope.idempotencyOperation,
      envelope.idempotencyKeyHash,
      envelope.correlationJson,
      envelope.sanitizedContextJson,
      envelope.retryable ? 1 : 0,
      envelope.masked ? 1 : 0,
      envelope.createdAt,
    ).run();
  } catch {
    // Ops logging is observational only; never worsen the player-facing API path.
  }
}

function getRewardLines(rewardPlanJson: string): unknown[] {
  const parsed = JSON.parse(rewardPlanJson) as { rewardLines?: unknown };
  return Array.isArray(parsed.rewardLines) ? parsed.rewardLines : [];
}

function getCurrencyRewardLines(rewardPlanJson: string): CurrencyRewardLine[] {
  return getRewardLines(rewardPlanJson).filter((line): line is CurrencyRewardLine => {
    if (typeof line !== "object" || line === null) return false;
    const candidate = line as Record<string, unknown>;
    return candidate.kind === "CURRENCY"
      && typeof candidate.id === "string"
      && Object.hasOwn(currencyIdToD1Currency, candidate.id)
      && typeof candidate.quantity === "number"
      && Number.isSafeInteger(candidate.quantity)
      && candidate.quantity > 0;
  });
}

function getItemRewardLines(rewardPlanJson: string): ItemRewardLine[] {
  return getRewardLines(rewardPlanJson).filter((line): line is ItemRewardLine => {
    if (typeof line !== "object" || line === null) return false;
    const candidate = line as Record<string, unknown>;
    return candidate.kind === "ITEM"
      && typeof candidate.id === "string"
      && candidate.id.trim().length > 0
      && typeof candidate.quantity === "number"
      && Number.isSafeInteger(candidate.quantity)
      && candidate.quantity > 0;
  });
}

function getClaimability(row: RewardPreviewRow): "CLAIMABLE" | "ALREADY_CLAIMED" | "EXPIRED" | "INVALIDATED" {
  if (row.status === "CLAIMED") return "ALREADY_CLAIMED";
  if (row.status === "EXPIRED") return "EXPIRED";
  if (row.status === "INVALIDATED") return "INVALIDATED";
  if (row.expires_at <= Date.now()) return "EXPIRED";
  return "CLAIMABLE";
}

function createRunFinishRejectedResponse(options: {
  runId: string;
  balanceVersion?: string | null;
  errorCode: string;
  message: string;
}) {
  return {
    runId: options.runId,
    accepted: false,
    resultStatus: "REJECTED",
    rewardClaimId: null,
    rewardPreviewId: null,
    serverReceivedAt: null,
    balanceVersion: options.balanceVersion ?? null,
    runStartVersionSnapshot: null,
    runFinishVersionSnapshot: null,
    message: options.message,
    errorCode: options.errorCode,
  };
}

function createRunFinishAcceptedResponse(options: {
  runId: string;
  rewardPreviewId: string;
  balanceVersion: string;
  serverReceivedAt: number;
  staminaRefund?: StoryRaidStaminaRefundCandidate | null;
  runStartVersionSnapshot?: RunVersionSnapshot | null;
  runFinishVersionSnapshot?: RunVersionSnapshot | null;
}) {
  return {
    runId: options.runId,
    accepted: true,
    resultStatus: "ACCEPTED",
    rewardClaimId: null,
    rewardPreviewId: options.rewardPreviewId,
    serverReceivedAt: toIso(options.serverReceivedAt),
    balanceVersion: options.balanceVersion,
    staminaRefund: options.staminaRefund ?? null,
    runStartVersionSnapshot: options.runStartVersionSnapshot ?? null,
    runFinishVersionSnapshot: options.runFinishVersionSnapshot ?? null,
    message: null,
    errorCode: null,
  };
}

function createRunFinishDuplicateReplayResponse(options: {
  runId: string;
  rewardPreviewId: string;
  balanceVersion?: string | null;
  serverReceivedAt: number;
  staminaRefund?: StoryRaidStaminaRefundCandidate | null;
  runStartVersionSnapshot?: RunVersionSnapshot | null;
  runFinishVersionSnapshot?: RunVersionSnapshot | null;
}) {
  return {
    runId: options.runId,
    accepted: false,
    resultStatus: "DUPLICATE_REPLAY",
    rewardClaimId: null,
    rewardPreviewId: options.rewardPreviewId,
    serverReceivedAt: toIso(options.serverReceivedAt),
    balanceVersion: options.balanceVersion ?? null,
    staminaRefund: options.staminaRefund ?? null,
    runStartVersionSnapshot: options.runStartVersionSnapshot ?? null,
    runFinishVersionSnapshot: options.runFinishVersionSnapshot ?? null,
    message: "Run finish was already accepted.",
    errorCode: "duplicate_replay",
  };
}

function createRunStartRejectedResponse(options: {
  operationPreviewId: string | null;
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    startStatus: "REJECTED",
    runId: null,
    operationPreviewId: options.operationPreviewId,
    operationId: null,
    encounterType: null,
    riskLevel: null,
    risk: null,
    balanceVersion: null,
    contentVersion: null,
    ordinary: null,
    raid: null,
    raidId: null,
    bossId: null,
    bossPhaseRefs: [],
    allowedOperatorSlots: null,
    startAugmentPackageRef: null,
    startTacticPackageRef: null,
    runVersionSnapshot: null,
    serverStartedAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createRunStartAcceptedResponse(options: {
  runId: string;
  operationPreviewId: string;
  previewMetadata: ReturnType<typeof parseRunPreviewMetadata>;
  risk: string;
  balanceVersion: number;
  serverStartedAt: number;
  runVersionSnapshot: RunVersionSnapshot | null;
}) {
  const raid = options.previewMetadata.encounterType === "RAID_BOSS"
    ? {
        raidId: options.previewMetadata.raidId,
        bossId: options.previewMetadata.bossId,
        bossPhaseRefs: options.previewMetadata.bossPhaseRefs,
      }
    : null;
  return {
    accepted: true,
    startStatus: "STARTED",
    runId: options.runId,
    operationPreviewId: options.operationPreviewId,
    operationId: options.previewMetadata.operationId,
    actId: options.previewMetadata.actId,
    regionId: options.previewMetadata.regionId,
    encounterType: options.previewMetadata.encounterType,
    riskLevel: options.risk,
    risk: options.risk,
    balanceVersion: String(options.balanceVersion),
    contentVersion: options.previewMetadata.contentVersion,
    ordinary: options.previewMetadata.encounterType === "ORDINARY_15_WAVE"
      ? {
          totalWaves: 15,
          startWaveNumber: 1,
        }
      : null,
    raid,
    raidId: raid?.raidId ?? null,
    bossId: raid?.bossId ?? null,
    bossPhaseRefs: raid?.bossPhaseRefs ?? [],
    allowedOperatorSlots: options.previewMetadata.allowedOperatorSlots,
    startAugmentPackageRef: options.previewMetadata.startAugmentPackageRef,
    startTacticPackageRef: options.previewMetadata.startTacticPackageRef,
    runVersionSnapshot: options.runVersionSnapshot,
    serverStartedAt: toIso(options.serverStartedAt),
    errorCode: null,
    message: null,
  };
}

function createRunStartReplayResponse(row: RunStartIdempotencyRecordRow) {
  if (row.snapshot_json === null) return null;
  try {
    const snapshot = JSON.parse(row.snapshot_json) as Record<string, unknown>;
    if (snapshot.departureResponse && typeof snapshot.departureResponse === "object" && !Array.isArray(snapshot.departureResponse)) return snapshot.departureResponse as Record<string, unknown>;
    if (snapshot.encounterType !== "RAID_BOSS" && snapshot.encounterType !== "ORDINARY_15_WAVE") return null;
    const encounterType = snapshot.encounterType;
    const raid = encounterType === "RAID_BOSS" && typeof snapshot.raid === "object" && snapshot.raid !== null
      ? snapshot.raid
      : null;
    const ordinary = encounterType === "ORDINARY_15_WAVE" && typeof snapshot.ordinary === "object" && snapshot.ordinary !== null
      ? snapshot.ordinary
      : null;
    return {
      accepted: true,
      startStatus: "STARTED",
      runId: row.result_ref,
      operationPreviewId: typeof snapshot.operationPreviewId === "string" ? snapshot.operationPreviewId : row.preview_id,
      operationId: typeof snapshot.operationId === "string" ? snapshot.operationId : null,
      actId: typeof snapshot.actId === "string" ? snapshot.actId : null,
      regionId: typeof snapshot.regionId === "string" ? snapshot.regionId : null,
      encounterType,
      riskLevel: typeof snapshot.riskLevel === "string" ? snapshot.riskLevel : null,
      risk: typeof snapshot.risk === "string" ? snapshot.risk : null,
      balanceVersion: typeof snapshot.balanceVersion === "string" ? snapshot.balanceVersion : null,
      contentVersion: typeof snapshot.contentVersion === "string" ? snapshot.contentVersion : null,
      ordinary,
      raid,
      raidId: encounterType === "RAID_BOSS" && typeof snapshot.raidId === "string" ? snapshot.raidId : null,
      bossId: encounterType === "RAID_BOSS" && typeof snapshot.bossId === "string" ? snapshot.bossId : null,
      bossPhaseRefs: encounterType === "RAID_BOSS" && Array.isArray(snapshot.bossPhaseRefs)
        ? snapshot.bossPhaseRefs.filter((value): value is string => typeof value === "string")
        : [],
      allowedOperatorSlots: typeof snapshot.allowedOperatorSlots === "number" ? snapshot.allowedOperatorSlots : null,
      startAugmentPackageRef: typeof snapshot.startAugmentPackageRef === "string" ? snapshot.startAugmentPackageRef : null,
      startTacticPackageRef: typeof snapshot.startTacticPackageRef === "string" ? snapshot.startTacticPackageRef : null,
      runVersionSnapshot: parseRunVersionSnapshot(snapshot.runVersionSnapshot),
      serverStartedAt: typeof snapshot.serverStartedAt === "string" ? snapshot.serverStartedAt : toIso(row.created_at),
      errorCode: null,
      message: null,
    };
  } catch {
    return null;
  }
}

function parseRunStartBody(value: unknown): { operationPreviewId: string } | null {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return null;
  const body = value as Record<string, unknown>;
  if (typeof body.operationPreviewId !== "string" || body.operationPreviewId.trim() === "") return null;
  const allowedFields = new Set(["operationPreviewId"]);
  if (Object.keys(body).some((key) => !allowedFields.has(key))) return null;
  return {
    operationPreviewId: body.operationPreviewId,
  };
}

function parseRunFinishBody(value: unknown, pathRunId: string): RunFinishBody | null {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return null;
  const body = value as Record<string, unknown>;
  for (const forbiddenField of runFinishForbiddenRewardFields) {
    if (Object.hasOwn(body, forbiddenField)) return null;
  }
  if (body.runId !== pathRunId) return null;
  const completedWaveCount = body.completedWaveCount;
  if (body.currentStage !== "Complete") return null;
  if (body.currentWaveNumber !== 0) return null;
  if (
    typeof completedWaveCount !== "number" ||
    !Number.isInteger(completedWaveCount) ||
    completedWaveCount < 0 ||
    completedWaveCount > 15
  ) {
    return null;
  }
  if (body.outcome !== "VICTORY" && body.outcome !== "FAILURE" && body.outcome !== "ABANDONED") {
    return null;
  }
  if (typeof body.runtimeStateJson !== "string" || typeof body.clientCompletedAt !== "string") {
    return null;
  }
  if (!Array.isArray(body.tacticSelections)) return null;
  if (containsForbiddenRunFinishField(body.tacticSelections, runFinishNestedForbiddenFields)) return null;
  let encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS" | undefined;
  if (body.encounterType !== undefined) {
    if (body.encounterType !== "ORDINARY_15_WAVE" && body.encounterType !== "RAID_BOSS") return null;
    encounterType = body.encounterType;
  }
  let raidResult: RunFinishBody["raidResult"];
  if (body.raidResult !== undefined) {
    if (typeof body.raidResult !== "object" || body.raidResult === null || Array.isArray(body.raidResult)) return null;
    const candidate = body.raidResult as Record<string, unknown>;
    if (typeof candidate.bossDefeated !== "boolean") return null;
    if (candidate.raidId !== undefined && typeof candidate.raidId !== "string") return null;
    if (candidate.bossId !== undefined && typeof candidate.bossId !== "string") return null;
    if (
      candidate.completedPhaseCount !== undefined &&
      (
        typeof candidate.completedPhaseCount !== "number" ||
        !Number.isInteger(candidate.completedPhaseCount) ||
        candidate.completedPhaseCount < 0
      )
    ) {
      return null;
    }
    if (
      candidate.encounterDurationSeconds !== undefined &&
      (
        typeof candidate.encounterDurationSeconds !== "number" ||
        !Number.isFinite(candidate.encounterDurationSeconds) ||
        candidate.encounterDurationSeconds < 0
      )
    ) {
      return null;
    }
    raidResult = {
      bossDefeated: candidate.bossDefeated,
      ...(typeof candidate.raidId === "string" ? { raidId: candidate.raidId } : {}),
      ...(typeof candidate.bossId === "string" ? { bossId: candidate.bossId } : {}),
      ...(typeof candidate.completedPhaseCount === "number" ? { completedPhaseCount: candidate.completedPhaseCount } : {}),
      ...(typeof candidate.encounterDurationSeconds === "number" ? { encounterDurationSeconds: candidate.encounterDurationSeconds } : {}),
    };
  }
  if (encounterType === "RAID_BOSS" && raidResult === undefined) return null;
  if (encounterType !== "RAID_BOSS" && raidResult !== undefined) return null;
  return {
    runId: body.runId,
    currentStage: body.currentStage,
    currentWaveNumber: body.currentWaveNumber,
    completedWaveCount,
    ...(encounterType === undefined ? {} : { encounterType }),
    ...(raidResult === undefined ? {} : { raidResult }),
    outcome: body.outcome,
    runtimeStateJson: body.runtimeStateJson,
    tacticSelections: body.tacticSelections,
    clientCompletedAt: body.clientCompletedAt,
  };
}

function containsForbiddenRunFinishField(value: unknown, forbiddenFields: Set<string>): boolean {
  if (Array.isArray(value)) {
    return value.some((item) => containsForbiddenRunFinishField(item, forbiddenFields));
  }
  if (typeof value !== "object" || value === null) return false;
  for (const [key, nestedValue] of Object.entries(value as Record<string, unknown>)) {
    if (forbiddenFields.has(key)) return true;
    if (containsForbiddenRunFinishField(nestedValue, forbiddenFields)) return true;
  }
  return false;
}

function getTacticSelectionPackageRefs(tacticSelections: unknown[]): string[] {
  const packageRefs: string[] = [];
  for (const selection of tacticSelections) {
    if (typeof selection !== "object" || selection === null || Array.isArray(selection)) continue;
    const candidate = selection as Record<string, unknown>;
    if (typeof candidate.packageRef === "string" && candidate.packageRef.trim() !== "") {
      packageRefs.push(candidate.packageRef);
    }
  }
  return packageRefs;
}

function parseRunPreviewMetadata(previewJson: string, objective: string): {
  operationId: string;
  actId: string;
  regionId: string;
  encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS";
  raidId: string | null;
  bossId: string | null;
  bossPhaseRefs: string[];
  allowedOperatorSlots: number | null;
  startAugmentPackageRef: string | null;
  startTacticPackageRef: string | null;
  contentVersion: string | null;
} {
  try {
    const parsed = JSON.parse(previewJson) as Record<string, unknown>;
    const encounterType = parsed.encounterType === "RAID_BOSS" ? "RAID_BOSS" : "ORDINARY_15_WAVE";
    const bossPhaseRefs = Array.isArray(parsed.bossPhaseRefs)
      ? parsed.bossPhaseRefs.filter((value): value is string => typeof value === "string" && value.trim() !== "")
      : [];
    const allowedOperatorSlots = typeof parsed.allowedOperatorSlots === "number" &&
      Number.isInteger(parsed.allowedOperatorSlots) &&
      parsed.allowedOperatorSlots > 0
      ? parsed.allowedOperatorSlots
      : null;
    return {
      operationId: typeof parsed.operationId === "string" ? parsed.operationId : objective,
      actId: typeof parsed.actId === "string" ? parsed.actId : "act_01_wasteland_railway",
      regionId: typeof parsed.regionId === "string" ? parsed.regionId : "region_wasteland_railway",
      encounterType,
      raidId: encounterType === "RAID_BOSS" && typeof parsed.raidId === "string" ? parsed.raidId : null,
      bossId: encounterType === "RAID_BOSS" && typeof parsed.bossId === "string" ? parsed.bossId : null,
      bossPhaseRefs: encounterType === "RAID_BOSS" ? bossPhaseRefs : [],
      allowedOperatorSlots,
      startAugmentPackageRef: typeof parsed.startAugmentPackageRef === "string" ? parsed.startAugmentPackageRef : null,
      startTacticPackageRef: typeof parsed.startTacticPackageRef === "string" ? parsed.startTacticPackageRef : null,
      contentVersion: typeof parsed.contentVersion === "string" ? parsed.contentVersion : null,
    };
  } catch {
    return {
      operationId: objective,
      actId: "act_01_wasteland_railway",
      regionId: "region_wasteland_railway",
      encounterType: "ORDINARY_15_WAVE",
      raidId: null,
      bossId: null,
      bossPhaseRefs: [],
      allowedOperatorSlots: null,
      startAugmentPackageRef: null,
      startTacticPackageRef: null,
      contentVersion: null,
    };
  }
}

function createStoryRaidStaminaRefundCandidate(
  body: RunFinishBody,
  previewMetadata: ReturnType<typeof parseRunPreviewMetadata>,
): StoryRaidStaminaRefundCandidate | null {
  if (previewMetadata.operationId !== "raid_raider_king_engine") return null;
  if (previewMetadata.encounterType !== "RAID_BOSS") return null;
  if (body.encounterType !== "RAID_BOSS") return null;
  if (body.outcome !== "FAILURE") return null;
  if (body.raidResult?.bossDefeated !== false) return null;
  return {
    status: "COMMITTED",
    refundType: "STAMINA",
    reason: "STORY_RAID_FAILURE",
    policyRef: "act1_story_raid_failure_stamina_refund_v1",
  };
}

function parseStoryRaidStaminaRefundCandidate(value: unknown): StoryRaidStaminaRefundCandidate | null {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return null;
  const candidate = value as Record<string, unknown>;
  if (candidate.status !== "COMMITTED") return null;
  if (candidate.refundType !== "STAMINA") return null;
  if (candidate.reason !== "STORY_RAID_FAILURE") return null;
  if (candidate.policyRef !== "act1_story_raid_failure_stamina_refund_v1") return null;
  return {
    status: "COMMITTED",
    refundType: "STAMINA",
    reason: "STORY_RAID_FAILURE",
    policyRef: "act1_story_raid_failure_stamina_refund_v1",
  };
}

function getStoredStoryRaidStaminaRefundCandidate(payloadJson: string | null | undefined): StoryRaidStaminaRefundCandidate | null {
  if (payloadJson === null || payloadJson === undefined) return null;
  try {
    const parsed = JSON.parse(payloadJson) as unknown;
    if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) return null;
    return parseStoryRaidStaminaRefundCandidate((parsed as Record<string, unknown>).staminaRefund);
  } catch {
    return null;
  }
}

function getStoryRaidStaminaRefundAmount(balanceConfig: unknown): number | null {
  if (typeof balanceConfig !== "object" || balanceConfig === null || Array.isArray(balanceConfig)) return null;
  const fuelConfig = (balanceConfig as Record<string, unknown>).fuel;
  if (typeof fuelConfig !== "object" || fuelConfig === null || Array.isArray(fuelConfig)) return null;
  const runStartCost = (fuelConfig as Record<string, unknown>).runStartCost;
  return typeof runStartCost === "number" && Number.isSafeInteger(runStartCost) && runStartCost > 0
    ? runStartCost
    : null;
}

function createRunFinishPayload(
  body: RunFinishBody,
  previewMetadata: ReturnType<typeof parseRunPreviewMetadata>,
  balanceVersion: number,
  runStartVersionSnapshot: RunVersionSnapshot | null,
  runFinishVersionSnapshot: RunVersionSnapshot | null,
): RunFinishBody & {
  serverPreviewMetadata: {
    operationId: string;
    actId: string;
    regionId: string;
    encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS";
    raidId: string | null;
    bossId: string | null;
    bossPhaseRefs: string[];
    allowedOperatorSlots: number | null;
    startAugmentPackageRef: string | null;
    startTacticPackageRef: string | null;
    balanceVersion: string;
    contentVersion: string | null;
  };
  runVersionSnapshot: RunVersionSnapshot | null;
  runStartVersionSnapshot: RunVersionSnapshot | null;
  runFinishVersionSnapshot: RunVersionSnapshot | null;
  staminaRefund?: StoryRaidStaminaRefundCandidate;
} {
  const staminaRefund = createStoryRaidStaminaRefundCandidate(body, previewMetadata);
  return {
    ...body,
    serverPreviewMetadata: {
      operationId: previewMetadata.operationId,
      actId: previewMetadata.actId,
      regionId: previewMetadata.regionId,
      encounterType: previewMetadata.encounterType,
      raidId: previewMetadata.raidId,
      bossId: previewMetadata.bossId,
      bossPhaseRefs: previewMetadata.bossPhaseRefs,
      allowedOperatorSlots: previewMetadata.allowedOperatorSlots,
      startAugmentPackageRef: previewMetadata.startAugmentPackageRef,
      startTacticPackageRef: previewMetadata.startTacticPackageRef,
      balanceVersion: String(balanceVersion),
      contentVersion: previewMetadata.contentVersion,
    },
    runVersionSnapshot: runStartVersionSnapshot,
    runStartVersionSnapshot,
    runFinishVersionSnapshot,
    ...(staminaRefund === null ? {} : { staminaRefund }),
  };
}

function isRunFullCompletion(body: RunFinishBody, previewMetadata: { encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS" }): boolean {
  if (body.outcome !== "VICTORY") return false;
  if (previewMetadata.encounterType === "RAID_BOSS") {
    return body.encounterType === "RAID_BOSS" && body.raidResult?.bossDefeated === true;
  }
  return body.completedWaveCount === 15;
}

function parseRunRisk(value: string): RunRiskTier | null {
  if (value === "I" || value === "II" || value === "III" || value === "IV" || value === "V") {
    return value;
  }
  return null;
}

function getActOneStoryProgressOrder(operationId: string): number | null {
  const index = actOneStoryProgressOrder.indexOf(operationId as ActOneStoryProgressId);
  return index < 0 ? null : index;
}

function parseJsonStringArray(value: string): string[] {
  try {
    const parsed = JSON.parse(value) as unknown;
    if (!Array.isArray(parsed)) return [];
    return parsed.filter((item): item is string => typeof item === "string");
  } catch {
    return [];
  }
}

function uniqueStrings(values: string[]): string[] {
  return [...new Set(values)];
}

function sortActOneOperationIds(values: string[]): string[] {
  return uniqueStrings(values)
    .filter((value) => actOneOperationIds.includes(value as typeof actOneOperationIds[number]))
    .sort((left, right) => (getActOneStoryProgressOrder(left) ?? 999) - (getActOneStoryProgressOrder(right) ?? 999));
}

function createStoryProgressRejectedResponse(options: {
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    actId: "act_01_wasteland_railway",
    operationId: null,
    storyProgress: null,
    firstClear: null,
    clearedOperationIds: [],
    narrativeFlags: [],
    storyFlags: [],
    unlockedOperations: [],
    narrativeVersion: actOneNarrativeVersion,
    narrative: null,
    lastProgressRunId: null,
    updatedAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createStoryProgressAcceptedResponse(options: {
  row: (AccountStoryProgressRow & {
    act_id: string;
    highest_cleared_operation_id: string;
    narrative_version: string | null;
    last_progress_run_id: string | null;
    updated_at: number;
  }) | null;
  unlockedOperationIds: string[];
}) {
  const narrativeVersion = options.row?.narrative_version ?? actOneNarrativeVersion;
  if (options.row === null) {
    return {
      accepted: true,
      actId: "act_01_wasteland_railway",
      operationId: null,
      storyProgress: {
        highestStoryProgressId: "none",
        highestClearedOperationId: null,
        order: 0,
      },
      firstClear: null,
      clearedOperationIds: [],
      narrativeFlags: [],
      storyFlags: [],
      unlockedOperations: ["opn_01_last_departure"],
      narrativeVersion,
      narrative: {
        narrativeVersion,
        contentStatus: "CANDIDATE",
        localizationStatus: "PENDING",
        fixtureStatus: "PARTIAL",
        missingContentStatus: "UNKNOWN",
      },
      lastProgressRunId: null,
      updatedAt: null,
      errorCode: null,
      message: null,
    };
  }

  const storyFlags = parseJsonStringArray(options.row.story_flags_json);
  const unlockedOperations = sortActOneOperationIds([
    "opn_01_last_departure",
    ...options.unlockedOperationIds,
  ]);

  return {
    accepted: true,
    actId: options.row.act_id,
    operationId: options.row.highest_story_progress_id,
    storyProgress: {
      highestStoryProgressId: options.row.highest_story_progress_id,
      highestClearedOperationId: options.row.highest_cleared_operation_id,
      order: getActOneStoryProgressOrder(options.row.highest_story_progress_id) ?? 0,
    },
    firstClear: null,
    clearedOperationIds: sortActOneOperationIds(parseJsonStringArray(options.row.cleared_operations_json)),
    narrativeFlags: storyFlags,
    storyFlags,
    unlockedOperations,
    narrativeVersion,
    narrative: {
      narrativeVersion,
      contentStatus: "CANDIDATE",
      localizationStatus: "PENDING",
      fixtureStatus: "PARTIAL",
      missingContentStatus: "UNKNOWN",
    },
    lastProgressRunId: options.row.last_progress_run_id,
    updatedAt: toIso(options.row.updated_at),
    errorCode: null,
    message: null,
  };
}

function createOfflineRewardRejectedResponse(options: {
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    previewStatus: "REJECTED",
    storyProgressId: null,
    claimableSeconds: 0,
    claimableHours: 0,
    offlineStorageCapHours: null,
    minimumClaimSeconds: launchOfflineRewardsConfig.minimumOfflineMinutes * 60,
    maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
    rewardLines: [],
    calculation: null,
    narrativeVersion: actOneNarrativeVersion,
    serverCalculatedAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createOfflineRewardAcceptedResponse(options: {
  storyProgressId: string;
  rewardLines: ReturnType<typeof calculateOfflineRewardPreview>["rewardLines"];
  claimableSeconds: number;
  offlineStorageCapHours: number;
  minimumOfflineMinutes: number;
  maxClaimableHours: number;
  storyProgressMultiplierBps: number;
  narrativeVersion: string;
  serverCalculatedAt: number;
}) {
  return {
    accepted: true,
    previewStatus: options.rewardLines.length === 0 ? "EMPTY" : "AVAILABLE",
    storyProgressId: options.storyProgressId,
    claimableSeconds: options.claimableSeconds,
    claimableHours: options.claimableSeconds / 3600,
    offlineStorageCapHours: options.offlineStorageCapHours,
    minimumClaimSeconds: options.minimumOfflineMinutes * 60,
    maxClaimableHours: options.maxClaimableHours,
    rewardLines: options.rewardLines,
    calculation: {
      storyProgressId: options.storyProgressId,
      storyProgressMultiplierLabel: `${(options.storyProgressMultiplierBps / 10000).toFixed(2)}x`,
      cappedOfflineSeconds: options.claimableSeconds,
      capSource: options.claimableSeconds >= options.offlineStorageCapHours * 3600
        ? "STORY_PROGRESS_STORAGE_CAP"
        : "ACTUAL_OFFLINE_DURATION",
    },
    narrativeVersion: options.narrativeVersion,
    serverCalculatedAt: toIso(options.serverCalculatedAt),
    errorCode: null,
    message: null,
  };
}

function createOfflineRewardClaimRejectedResponse(options: {
  storyProgressId?: string | null;
  claimableSeconds?: number;
  offlineStorageCapHours?: number | null;
  serverClaimedAt?: number | null;
  errorCode: string;
  message: string;
}) {
  const claimableSeconds = options.claimableSeconds ?? 0;
  return {
    accepted: false,
    claimStatus: "REJECTED",
    offlineRewardClaimId: null,
    rewardTransactionId: null,
    economyTransactionId: null,
    accountMutationId: null,
    storyProgressId: options.storyProgressId ?? null,
    claimableSeconds,
    claimableHours: claimableSeconds / 3600,
    offlineStorageCapHours: options.offlineStorageCapHours ?? null,
    minimumClaimSeconds: launchOfflineRewardsConfig.minimumOfflineMinutes * 60,
    maxClaimableHours: launchOfflineRewardsConfig.maxClaimableHours,
    rewardLines: [],
    calculation: null,
    serverClaimedAt: options.serverClaimedAt === null || options.serverClaimedAt === undefined
      ? null
      : toIso(options.serverClaimedAt),
    narrativeVersion: actOneNarrativeVersion,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createOfflineRewardClaimDuplicateReplayResponse(options: {
  offlineRewardClaimId: string;
  rewardJson: string | null;
  serverClaimedAt: number;
}) {
  let parsed: Record<string, unknown> = {};
  try {
    parsed = options.rewardJson === null ? {} : JSON.parse(options.rewardJson) as Record<string, unknown>;
  } catch {
    parsed = {};
  }

  return {
    accepted: false,
    claimStatus: "DUPLICATE_REPLAY",
    offlineRewardClaimId: options.offlineRewardClaimId,
    rewardTransactionId: parsed.rewardTransactionId ?? options.offlineRewardClaimId,
    economyTransactionId: parsed.economyTransactionId ?? null,
    accountMutationId: parsed.accountMutationId ?? null,
    storyProgressId: parsed.storyProgressId ?? null,
    balanceVersion: parsed.balanceVersion ?? null,
    economyVersion: parsed.economyVersion ?? null,
    economyHash: parsed.economyHash ?? null,
    claimableSeconds: null,
    claimableHours: null,
    offlineStorageCapHours: parsed.offlineStorageCapHours ?? null,
    minimumClaimSeconds: parsed.minimumClaimSeconds ?? launchOfflineRewardsConfig.minimumOfflineMinutes * 60,
    maxClaimableHours: parsed.maxClaimableHours ?? launchOfflineRewardsConfig.maxClaimableHours,
    rewardLines: [],
    calculation: null,
    serverClaimedAt: toIso(options.serverClaimedAt),
    narrativeVersion: parsed.narrativeVersion ?? actOneNarrativeVersion,
    errorCode: "duplicate_replay",
    message: "Offline reward claim was already committed.",
  };
}

function createLoginBonusClaimRejectedResponse(options: {
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    claimStatus: "REJECTED",
    bonusId: launchLoginBonusId,
    loginBonusClaimId: null,
    rewardTransactionId: null,
    economyTransactionId: null,
    accountMutationId: null,
    rewardLines: [],
    serverClaimedAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createLoginBonusClaimReplayResponse(options: {
  claimStatus: "DUPLICATE_REPLAY" | "ALREADY_CLAIMED";
  loginBonusClaimId: string;
  rewardJson: string | null;
  serverClaimedAt: number;
}) {
  let parsed: Record<string, unknown> = {};
  try {
    parsed = options.rewardJson === null ? {} : JSON.parse(options.rewardJson) as Record<string, unknown>;
  } catch {
    parsed = {};
  }
  const errorCode = options.claimStatus === "DUPLICATE_REPLAY" ? "duplicate_replay" : "already_claimed";

  return {
    accepted: false,
    claimStatus: options.claimStatus,
    bonusId: launchLoginBonusId,
    loginBonusClaimId: options.loginBonusClaimId,
    rewardTransactionId: parsed.rewardTransactionId ?? options.loginBonusClaimId,
    economyTransactionId: parsed.economyTransactionId ?? null,
    accountMutationId: parsed.accountMutationId ?? null,
    rewardLines: [],
    serverClaimedAt: toIso(options.serverClaimedAt),
    errorCode,
    message: options.claimStatus === "DUPLICATE_REPLAY"
      ? "Launch login bonus claim was already committed."
      : "Launch login bonus was already claimed for this account.",
  };
}

function createStoryProgressPlan(options: {
  accountId: string;
  runId: string;
  operationId: string;
  actId: string;
  existing: AccountStoryProgressRow | null;
  now: number;
}): null | {
  highestStoryProgressId: string;
  highestClearedOperationId: string;
  clearedOperations: string[];
  storyFlags: string[];
  nextOperationUnlockId: string | null;
} {
  const newOrder = getActOneStoryProgressOrder(options.operationId);
  if (newOrder === null || newOrder === 0) return null;
  const existingOrder = options.existing === null
    ? 0
    : getActOneStoryProgressOrder(options.existing.highest_story_progress_id) ?? 0;
  if (newOrder <= existingOrder) return null;

  const previousCleared = options.existing === null
    ? []
    : parseJsonStringArray(options.existing.cleared_operations_json);
  const previousFlags = options.existing === null
    ? []
    : parseJsonStringArray(options.existing.story_flags_json);
  const clearedOperations = uniqueStrings([
    ...previousCleared,
    ...actOneOperationIds.slice(0, newOrder),
  ]);
  const storyFlags = uniqueStrings([
    ...previousFlags,
    `operation_cleared:${options.operationId}`,
  ]);
  const nextOperationId = actOneOperationIds[newOrder] ?? null;

  return {
    highestStoryProgressId: options.operationId,
    highestClearedOperationId: options.operationId === "raid_raider_king_engine"
      ? "opn_06_broken_line"
      : options.operationId,
    clearedOperations,
    storyFlags,
    nextOperationUnlockId: nextOperationId === null ? null : `operation:${nextOperationId}`,
  };
}

function getFinishedRunStatus(outcome: RunOutcome): "COMPLETED" | "FAILED" | "ABANDONED" {
  if (outcome === "VICTORY") return "COMPLETED";
  if (outcome === "ABANDONED") return "ABANDONED";
  return "FAILED";
}

function createGachaRejectedResponse(options: {
  bannerId: string;
  pullCount: number | null;
  errorCode: string;
  message: string;
}) {
  return {
    accepted: false,
    resultStatus: "REJECTED",
    batchId: null,
    bannerId: options.bannerId,
    pullCount: options.pullCount,
    cost: options.pullCount === 1 || options.pullCount === 10
      ? { itemId: "cur_command_ticket", quantity: options.pullCount }
      : null,
    results: [],
    postPityState: null,
    createdAt: null,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function createGachaAcceptedResponse(options: {
  batchId: string;
  bannerId: string;
  balanceVersion: string;
  pullCount: 1 | 10;
  results: Array<Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">>;
  postPityState: GachaPityState;
  createdAt: number;
}) {
  return {
    accepted: true,
    resultStatus: "ACCEPTED",
    batchId: options.batchId,
    bannerId: options.bannerId,
    balanceVersion: options.balanceVersion,
    pullCount: options.pullCount,
    cost: {
      itemId: "cur_command_ticket",
      quantity: options.pullCount,
    },
    results: options.results,
    postPityState: options.postPityState,
    createdAt: toIso(options.createdAt),
    errorCode: null,
    message: null,
  };
}

function createGachaDuplicateReplayResponse(options: {
  batchId: string;
  bannerId: string;
  balanceVersion: string;
  pullCount: 1 | 10;
  results: Array<Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">>;
  postPityState: GachaPityState;
  createdAt: number;
}) {
  return {
    accepted: false,
    resultStatus: "DUPLICATE_REPLAY",
    batchId: options.batchId,
    bannerId: options.bannerId,
    balanceVersion: options.balanceVersion,
    pullCount: options.pullCount,
    cost: {
      itemId: "cur_command_ticket",
      quantity: options.pullCount,
    },
    results: options.results,
    postPityState: options.postPityState,
    createdAt: toIso(options.createdAt),
    errorCode: "duplicate_replay",
    message: "Gacha pull was already committed.",
  };
}

function isActiveGachaBanner(row: GachaBannerRow, now: number): boolean {
  return row.status === "ACTIVE" && row.starts_at <= now && (row.ends_at === null || row.ends_at > now);
}

function parseGachaPoolResults(poolSnapshotJson: string): GachaPoolResult[] {
  let parsed: { results?: unknown };
  try {
    const value: unknown = JSON.parse(poolSnapshotJson);
    if (typeof value !== "object" || value === null) return [];
    parsed = value as { results?: unknown };
  } catch {
    return [];
  }
  if (!Array.isArray(parsed.results)) return [];

  const results = parsed.results.filter((result): result is GachaPoolResult => {
    if (typeof result !== "object" || result === null) return false;
    const candidate = result as Record<string, unknown>;
    return (candidate.resultRarity === "COMMON" || candidate.resultRarity === "RARE" || candidate.resultRarity === "ELITE")
      && (candidate.resultType === "ELITE_OPERATOR"
        || candidate.resultType === "RARE_OPERATOR"
        || candidate.resultType === "ELITE_OPERATOR_SHARD"
        || candidate.resultType === "RARE_OPERATOR_SHARD"
        || candidate.resultType === "OPERATOR_TOKEN"
        || candidate.resultType === "TRAINING_MATERIAL"
        || candidate.resultType === "RARE_OPERATOR_TOKEN"
        || candidate.resultType === "TACTICAL_MATERIAL"
        || candidate.resultType === "WORK_SKILL_MATERIAL")
      && typeof candidate.resultId === "string" && candidate.resultId.trim().length > 0
      && (candidate.weight === undefined || (typeof candidate.weight === "number"
        && Number.isSafeInteger(candidate.weight) && candidate.weight >= 0 && candidate.weight <= 2 ** 32))
      && (candidate.quantity === undefined || (typeof candidate.quantity === "number"
        && Number.isSafeInteger(candidate.quantity) && candidate.quantity > 0))
      && (candidate.isPickup === undefined || typeof candidate.isPickup === "boolean");
  });
  // Never silently remove candidates and renormalize a published pool.
  if (results.length !== parsed.results.length) return [];
  if (new Set(results.map((result) => result.resultId)).size !== results.length) return [];
  const maximumTotal = results.reduce((sum, result) => sum + Math.max(result.weight ?? 0, 1), 0);
  if (!Number.isSafeInteger(maximumTotal) || maximumTotal > 2 ** 32) return [];
  return results;
}

function parseGachaPityConfig(pityConfigJson: string): GachaPityConfig {
  const parsed = JSON.parse(pityConfigJson) as Record<string, unknown>;
  return {
    elite: typeof parsed.elite === "number" && Number.isSafeInteger(parsed.elite) && parsed.elite > 0 ? parsed.elite : 30,
    pickup: typeof parsed.pickup === "number" && Number.isSafeInteger(parsed.pickup) && parsed.pickup > 0 ? parsed.pickup : 60,
    rare: typeof parsed.rare === "number" && Number.isSafeInteger(parsed.rare) && parsed.rare > 0 ? parsed.rare : 10,
  };
}

function getInitialPityState(row: GachaPityRow | null): GachaPityState {
  if (row === null) {
    return {
      totalPullCount: 0,
      elitePityCount: 0,
      pickupElitePityCount: 0,
      rareOrHigherPityCount: 0,
      revision: 0,
    };
  }

  return {
    totalPullCount: row.total_pull_count,
    elitePityCount: row.elite_pity_count,
    pickupElitePityCount: row.pickup_elite_pity_count,
    rareOrHigherPityCount: row.rare_or_higher_pity_count,
    revision: row.revision,
  };
}

function applyGachaPityResult(state: GachaPityState, result: Pick<GachaCommittedResult, "resultRarity" | "isPickup">): GachaPityState {
  const isElite = result.resultRarity === "ELITE";
  const isRareOrHigher = result.resultRarity === "RARE" || isElite;
  return {
    totalPullCount: state.totalPullCount + 1,
    elitePityCount: isElite ? 0 : state.elitePityCount + 1,
    pickupElitePityCount: result.isPickup && isElite ? 0 : state.pickupElitePityCount + 1,
    rareOrHigherPityCount: isRareOrHigher ? 0 : state.rareOrHigherPityCount + 1,
    revision: state.revision,
  };
}

function finalizeGachaPityState(state: GachaPityState): GachaPityState {
  return {
    ...state,
    revision: state.revision + 1,
  };
}

function getNextGachaGuaranteeType(options: {
  banner: GachaBannerRow;
  pityState: GachaPityState;
  pityConfig: GachaPityConfig;
}): GachaGuaranteeType {
  if (
    options.banner.banner_type === "PICKUP"
    && options.banner.pickup_operator_id !== null
    && options.pityState.pickupElitePityCount + 1 >= options.pityConfig.pickup
  ) {
    return "PICKUP_ELITE_60";
  }
  if (options.pityState.elitePityCount + 1 >= options.pityConfig.elite) {
    return "ELITE_30";
  }
  if (options.pityState.rareOrHigherPityCount + 1 >= options.pityConfig.rare) {
    return "RARE_OR_HIGHER_10";
  }
  return "NONE";
}

function isGachaPoolResultEligibleForGuarantee(
  result: GachaPoolResult,
  guaranteeType: GachaGuaranteeType,
  banner: GachaBannerRow,
): boolean {
  if (guaranteeType === "PICKUP_ELITE_60") {
    return result.resultRarity === "ELITE"
      && (result.isPickup === true || result.resultId === banner.pickup_operator_id);
  }
  if (guaranteeType === "ELITE_30") {
    return result.resultRarity === "ELITE";
  }
  if (guaranteeType === "RARE_OR_HIGHER_10") {
    return result.resultRarity === "RARE" || result.resultRarity === "ELITE";
  }
  return true;
}

function getGachaResultWeight(result: GachaPoolResult, guaranteeType: GachaGuaranteeType): number {
  if (guaranteeType !== "NONE") {
    return typeof result.weight === "number" && Number.isFinite(result.weight) && result.weight > 0
      ? result.weight
      : 1;
  }
  return typeof result.weight === "number" && Number.isFinite(result.weight) && result.weight > 0
    ? result.weight
    : 0;
}

function selectGachaPoolResult(options: {
  poolResults: GachaPoolResult[];
  guaranteeType: GachaGuaranteeType;
  banner: GachaBannerRow;
}): GachaPoolResult | null {
  const candidates = options.poolResults
    .filter((result) => isGachaPoolResultEligibleForGuarantee(result, options.guaranteeType, options.banner))
    .map((result) => ({ result, weight: getGachaResultWeight(result, options.guaranteeType) }))
    .filter((candidate) => candidate.weight > 0);
  const totalWeight = candidates.reduce((sum, candidate) => sum + candidate.weight, 0);
  if (!Number.isSafeInteger(totalWeight) || totalWeight <= 0 || totalWeight > 2 ** 32) return null;

  // Rejection sampling gives each integer interval equal mass. Fresh entropy
  // is server-owned; replay reads committed results instead of sampling again.
  const limit = Math.floor(2 ** 32 / totalWeight) * totalWeight;
  const random = new Uint32Array(1);
  let value: number | null = null;
  try {
    for (let attempt = 0; attempt < 128; attempt += 1) {
      crypto.getRandomValues(random);
      const candidate = random[0]!;
      if (candidate < limit) { value = candidate; break; }
    }
  } catch {
    return null;
  }
  if (value === null) return null;
  let roll = value % totalWeight;
  for (const candidate of candidates) {
    if (roll < candidate.weight) return candidate.result;
    roll -= candidate.weight;
  }
  return candidates[candidates.length - 1]?.result ?? null;
}

function getOperatorShardItemId(operatorId: string): string {
  return `cur_operator_token:${operatorId}`;
}

function getDuplicateConversionQuantity(resultType: GachaPoolResult["resultType"]): number {
  return resultType === "ELITE_OPERATOR" ? 50 : 30;
}

function toGachaResponseResult(result: GachaCommittedResult): Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId"> {
  return {
    pullSequence: result.pullSequence,
    resultRarity: result.resultRarity,
    resultType: result.resultType,
    resultId: result.resultId,
    reward: result.reward,
    isPickup: result.isPickup,
    guaranteeType: result.guaranteeType,
    wasDuplicate: result.wasDuplicate,
    duplicateConversion: result.duplicateConversion,
  };
}

function toGachaReplayResult(row: GachaReplayResultRow): Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId"> {
  let duplicateConversion: Record<string, unknown> | null = null;
  try {
    const parsed: unknown = row.duplicate_conversion_json === null ? null : JSON.parse(row.duplicate_conversion_json);
    if (isObjectRecord(parsed) && typeof parsed.itemId === "string" && Number.isSafeInteger(parsed.quantity) && (parsed.quantity as number) > 0) {
      duplicateConversion = { sourceResultType: parsed.sourceResultType, sourceResultId: parsed.sourceResultId,
        itemId: parsed.itemId, operatorId: parsed.operatorId, quantity: parsed.quantity };
    }
  } catch { /* Retain unknown quantity rather than invent a grant. */ }
  const reward = row.was_duplicate === 1 && duplicateConversion === null
    ? { kind: "UNKNOWN", id: null, quantity: null }
    : duplicateConversion === null
    ? {
        kind: row.result_type === "ELITE_OPERATOR" || row.result_type === "RARE_OPERATOR" ? "OPERATOR" : "ITEM",
        id: row.result_id,
        quantity: row.was_duplicate === 0 && (row.result_type === "ELITE_OPERATOR" || row.result_type === "RARE_OPERATOR") ? 1 : row.reward_quantity ?? null,
    }
    : {
      kind: "ITEM",
      id: duplicateConversion.itemId,
      operatorId: duplicateConversion.operatorId,
      quantity: duplicateConversion.quantity,
    };

  return {
    pullSequence: row.pull_sequence,
    resultRarity: row.result_rarity,
    resultType: row.result_type,
    resultId: row.result_id,
    reward,
    isPickup: row.is_pickup === 1,
    guaranteeType: row.guarantee_type,
    wasDuplicate: row.was_duplicate === 1,
    duplicateConversion,
  };
}

function getGachaReplayResponseResults(options: {
  resultSummaryJson: string;
  resultRows: GachaReplayResultRow[];
  expectedPullCount: number;
}): Array<Omit<GachaCommittedResult, "id" | "operatorId" | "itemLedgerId" | "progressionLedgerId">> {
  type Result = ReturnType<typeof toGachaReplayResult>;
  const results = new Map<number, Result>();
  for (const row of options.resultRows) results.set(row.pull_sequence, toGachaReplayResult(row));
  try {
    const parsed = JSON.parse(options.resultSummaryJson) as { responseResults?: unknown };
    if (Array.isArray(parsed?.responseResults)) for (const value of parsed.responseResults) {
      if (!isObjectRecord(value) || !Number.isSafeInteger(value.pullSequence) || (value.pullSequence as number) < 1
        || !isObjectRecord(value.reward) || !Number.isSafeInteger(value.reward.quantity) || (value.reward.quantity as number) <= 0
        || (value.reward.kind !== "ITEM" && value.reward.kind !== "OPERATOR") || typeof value.reward.id !== "string" || value.reward.id.length === 0
        || typeof value.resultId !== "string" || typeof value.resultType !== "string" || typeof value.resultRarity !== "string"
        || typeof value.isPickup !== "boolean" || typeof value.wasDuplicate !== "boolean" || typeof value.guaranteeType !== "string") continue;
      const result = {
        pullSequence: value.pullSequence, resultRarity: value.resultRarity, resultType: value.resultType, resultId: value.resultId,
        isPickup: value.isPickup, guaranteeType: value.guaranteeType, wasDuplicate: value.wasDuplicate,
        reward: { kind: value.reward.kind, id: value.reward.id, quantity: value.reward.quantity,
          ...(typeof value.reward.operatorId === "string" ? { operatorId: value.reward.operatorId } : {}) },
        duplicateConversion: isObjectRecord(value.duplicateConversion) ? {
          sourceResultType: value.duplicateConversion.sourceResultType, sourceResultId: value.duplicateConversion.sourceResultId,
          itemId: value.duplicateConversion.itemId, operatorId: value.duplicateConversion.operatorId, quantity: value.duplicateConversion.quantity,
        } : null,
      } as Result;
      const row = results.get(result.pullSequence);
      if (!row || (row.resultId === result.resultId && row.resultType === result.resultType && row.resultRarity === result.resultRarity
        && row.wasDuplicate === result.wasDuplicate && (row.reward.quantity === null || row.reward.quantity === result.reward.quantity))) results.set(result.pullSequence, result);
    }
  } catch { /* Individual committed rows and ledgers remain usable. */ }
  return [...results.values()].filter(r => r.pullSequence >= 1 && r.pullSequence <= options.expectedPullCount).sort((a, b) => a.pullSequence - b.pullSequence);
}

function getAuthSecret(env: Env): string | null {
  return (env as GameApiEnv).AUTH_SIGNING_KEY_CURRENT ?? null;
}

function getGoogleAudiences(env: Env): string[] {
  return ((env as GameApiEnv).GOOGLE_AUTH_AUDIENCES ?? "")
    .split(",")
    .map((audience) => audience.trim())
    .filter((audience) => audience.length > 0);
}

function getGoogleVerifier(env: Env): GoogleIdTokenVerifier | null {
  return (env as GameApiEnv).GOOGLE_ID_TOKEN_VERIFIER ?? createGoogleIdTokenVerifier();
}

function getRecoveryEmailSender(env: Env): RecoveryEmailSender | null {
  return (env as GameApiEnv).RECOVERY_EMAIL_SENDER ?? createGmailRecoveryEmailSenderFromEnv(env as GameApiEnv);
}

function getRecoveryEmailSenderFailureCode(error: unknown): string {
  return error instanceof GmailRecoveryEmailSendError ? error.code : "recovery_email_service_unavailable";
}

function getRecoveryEmailSenderFailureStage(error: unknown): string | null {
  return error instanceof GmailRecoveryEmailSendError ? error.failureStage : null;
}

function getRecoveryEmailSenderUpstreamStatus(error: unknown): number | null {
  return error instanceof GmailRecoveryEmailSendError ? error.upstreamStatus : null;
}

function isRetryableRecoveryEmailSenderFailure(errorCode: string): boolean {
  return errorCode === "gmail_oauth_unavailable"
    || errorCode === "gmail_send_unavailable"
    || errorCode === "gmail_quota_exceeded";
}

async function writeRecoveryEmailSenderFailureOpsLog(c: GameApiContext, input: {
  error: unknown;
  accountId: string | null;
  purpose: RecoveryEmailPurpose;
  challengeId: string;
  emailMasked: string;
}): Promise<void> {
  const errorCode = getRecoveryEmailSenderFailureCode(input.error);
  await writeGameApiOpsErrorLog(c, {
    severity: "error",
    errorCode,
    messageSummary: "Recovery email sender failed.",
    accountId: input.accountId,
    correlation: {
      challengeId: input.challengeId,
      purpose: input.purpose,
    },
    context: {
      provider: errorCode.startsWith("gmail_") ? "GMAIL" : "unknown",
      failureCode: errorCode,
      failureStage: getRecoveryEmailSenderFailureStage(input.error),
      upstreamStatus: getRecoveryEmailSenderUpstreamStatus(input.error),
      emailMasked: input.emailMasked,
    },
    retryable: isRetryableRecoveryEmailSenderFailure(errorCode),
  });
}

function createAuthError(errorCode: string, message: string) {
  return {
    errorCode,
    message,
  };
}

function createAuthFailure(status: AuthFailureStatus, errorCode: string, message: string): AuthFailure {
  return {
    status,
    errorCode,
    message,
  };
}

function isAuthFailure(value: unknown): value is AuthFailure {
  return typeof value === "object" && value !== null && "errorCode" in value;
}

function createRecoveryVerificationResponse(options: {
  accepted: boolean;
  verificationStatus: "PENDING" | "VERIFIED" | "REJECTED";
  challengeId: string | null;
  expiresAt: number | null;
  retryAfterSeconds: number | null;
  errorCode: string | null;
  message: string | null;
}) {
  return {
    accepted: options.accepted,
    verificationStatus: options.verificationStatus,
    challengeId: options.challengeId,
    expiresAt: options.expiresAt === null ? null : toIso(options.expiresAt),
    retryAfterSeconds: options.retryAfterSeconds,
    errorCode: options.errorCode,
    message: options.message,
  };
}

function parseOptionalIdempotencyKey(c: GameApiContext): string | null | AuthFailure {
  const idempotencyKey = c.req.header("Idempotency-Key")?.trim();
  if (idempotencyKey === undefined || idempotencyKey.length === 0) return null;
  if (idempotencyKey.length < 16) {
    return createAuthFailure(400, "contract_violation", "Idempotency-Key header is too weak.");
  }
  if (idempotencyKey.length > 128) {
    return createAuthFailure(400, "contract_violation", "Idempotency-Key header is too long.");
  }
  return idempotencyKey;
}

function isUniqueConstraintError(error: unknown): boolean {
  return error instanceof Error && error.message.includes("UNIQUE constraint failed");
}

async function readGuestReplayRow(db: D1Database, providerSubject: string): Promise<AuthGuestReplayRow | null> {
  return db.prepare(
    `SELECT identities.account_id, accounts.status AS account_status,
            sessions.id AS session_id, sessions.status AS session_status,
            sessions.expires_at AS session_expires_at,
            accounts.created_at
     FROM auth_identities identities
     JOIN accounts ON accounts.id = identities.account_id
     JOIN auth_sessions sessions ON sessions.account_id = accounts.id
     WHERE identities.provider = 'GUEST'
       AND identities.provider_subject = ?
     ORDER BY sessions.created_at ASC
     LIMIT 1`,
  )
    .bind(providerSubject)
    .first<AuthGuestReplayRow>();
}

async function readRefreshReplaySessionRow(db: D1Database, input: {
  accountId: string;
  familyId: string;
  generation: number;
  idempotencyKey: string;
}): Promise<RefreshReplaySessionRow | null> {
  return db.prepare(
    `SELECT idem.result_ref, idem.created_at,
            sessions.account_id, sessions.id AS session_id,
            sessions.status AS session_status, sessions.expires_at AS session_expires_at,
            accounts.status AS account_status
     FROM idempotency_records idem
     JOIN auth_sessions sessions ON sessions.id = idem.result_ref
     JOIN accounts ON accounts.id = sessions.account_id
     WHERE idem.account_id = ?
       AND idem.operation = 'auth_refresh'
       AND idem.idempotency_key = ?
       AND sessions.family_id = ?
       AND sessions.generation = ?`,
  )
    .bind(input.accountId, input.idempotencyKey, input.familyId, input.generation)
    .first<RefreshReplaySessionRow>();
}

async function createAuthResponseForSession(options: {
  secret: string;
  accountId: string;
  sessionId: string;
  refreshToken: string;
  refreshTokenExpiresAt: number;
  now: number;
}) {
  const accessTokenTtlMs = 15 * 60 * 1000;
  const accessTokenExpiresAt = options.now + accessTokenTtlMs;
  const accessToken = await createAccessToken({
    secret: options.secret,
    accountId: options.accountId,
    sessionId: options.sessionId,
    nowMs: options.now,
    ttlMs: accessTokenTtlMs,
  });

  return {
    tokenType: "Bearer",
    account: {
      id: options.accountId,
      status: "ACTIVE",
    },
    accessToken,
    accessTokenExpiresAt: toIso(accessTokenExpiresAt),
    refreshToken: options.refreshToken,
    refreshTokenExpiresAt: toIso(options.refreshTokenExpiresAt),
  };
}

function createPasswordRegisterRejected(errorCode: string, message: string) {
  return {
    accepted: false,
    registerStatus: "REJECTED",
    account: null,
    accessToken: null,
    refreshToken: null,
    errorCode,
    message,
  };
}

function createPasswordLinkRejected(errorCode: string, message: string) {
  return {
    accepted: false,
    linkStatus: "REJECTED",
    account: null,
    accessToken: null,
    refreshToken: null,
    errorCode,
    message,
  };
}

function createSupportTicketRejectedResponse(errorCode: string, message: string, retryAfterSeconds: number | null = null) {
  return {
    accepted: false,
    ticketStatus: "REJECTED",
    ticketId: null,
    category: null,
    bodyPreview: null,
    appVersion: null,
    appVersionCode: null,
    clientComposedAt: null,
    clientSubmittedAt: null,
    serverReceivedAt: null,
    retryAfterSeconds,
    errorCode,
    message,
  };
}

function supportTicketResponse(row: SupportTicketRow, ticketStatus: "RECEIVED" | "DUPLICATE_REPLAY") {
  return {
    accepted: true,
    ticketStatus,
    ticketId: row.id,
    category: row.category,
    bodyPreview: row.body_preview,
    appVersion: row.app_version,
    appVersionCode: row.app_version_code,
    clientComposedAt: toNullableIso(row.client_composed_at),
    clientSubmittedAt: toNullableIso(row.client_submitted_at),
    serverReceivedAt: toIso(row.server_received_at),
    retryAfterSeconds: null,
    errorCode: ticketStatus === "DUPLICATE_REPLAY" ? "duplicate_replay" : null,
    message: ticketStatus === "DUPLICATE_REPLAY" ? "Support ticket was already received." : null,
  };
}

function supportTicketReceiptFoundResponse(row: SupportTicketRow) {
  return {
    accepted: true,
    receiptStatus: "FOUND",
    ticketId: row.id,
    accountId: row.account_id,
    category: row.category,
    bodyPreview: row.body_preview,
    appVersion: row.app_version,
    appVersionCode: row.app_version_code,
    clientComposedAt: toNullableIso(row.client_composed_at),
    clientSubmittedAt: toNullableIso(row.client_submitted_at),
    serverReceivedAt: toIso(row.server_received_at),
    errorCode: null,
    message: null,
  };
}

function supportTicketReceiptNotFoundResponse() {
  return {
    accepted: false,
    receiptStatus: "NOT_FOUND",
    ticketId: null,
    accountId: null,
    category: null,
    bodyPreview: null,
    appVersion: null,
    appVersionCode: null,
    clientComposedAt: null,
    clientSubmittedAt: null,
    serverReceivedAt: null,
    errorCode: "support_ticket_receipt_not_found",
    message: "Support ticket receipt was not found for this account.",
  };
}

function parseSupportTicketCategory(value: unknown): SupportTicketCategory | null {
  return typeof value === "string" && supportTicketCategories.has(value as SupportTicketCategory)
    ? value as SupportTicketCategory
    : null;
}

function buildSupportTicketPreview(bodyText: string): string {
  return bodyText.length <= 160 ? bodyText : `${bodyText.slice(0, 157)}...`;
}

function createClientErrorIngestRejectedResponse(
  errorCode: string,
  message: string,
  options: { droppedEventCount?: number; retryAfterSeconds?: number | null } = {},
) {
  return {
    accepted: false,
    ingestStatus: "REJECTED",
    ingestId: null,
    acceptedEventCount: 0,
    droppedEventCount: options.droppedEventCount ?? 0,
    serverReceivedAt: null,
    retryAfterSeconds: options.retryAfterSeconds ?? null,
    errorCode,
    message,
  };
}

function createClientErrorIngestReceiptNotFoundResponse() {
  return {
    accepted: false,
    receiptStatus: "NOT_FOUND",
    ingestId: null,
    acceptedEventCount: 0,
    serverReceivedAt: null,
    errorCode: "client_error_ingest_receipt_not_found",
    message: "Client error ingest receipt was not found for this account.",
  };
}

function clientErrorCode(kind: ClientErrorKind): string {
  if (kind === "UNITY_EXCEPTION") return "frontend_unity_exception";
  if (kind === "UNITY_ASSERT") return "frontend_unity_assert";
  return "frontend_unity_error";
}

function clientErrorSummary(kind: ClientErrorKind): string {
  if (kind === "UNITY_EXCEPTION") return "Frontend Unity exception diagnostic captured.";
  if (kind === "UNITY_ASSERT") return "Frontend Unity assert diagnostic captured.";
  return "Frontend Unity error diagnostic captured.";
}

function parseClientErrorEvent(value: unknown): ClientErrorEvent | null {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return null;
  const candidate = value as Record<string, unknown>;
  if (Object.keys(candidate).some((key) => !clientErrorEventKeys.has(key))) return null;
  const kind = typeof candidate.kind === "string" && clientErrorKinds.has(candidate.kind as ClientErrorKind)
    ? candidate.kind as ClientErrorKind
    : null;
  const occurredAt = parseOptionalIsoMs(candidate.occurredAt);
  const occurrenceCount = typeof candidate.occurrenceCount === "number" && Number.isSafeInteger(candidate.occurrenceCount)
    ? candidate.occurrenceCount
    : null;
  const appVersion = normalizeOptionalText(candidate.appVersion, 64);
  const appVersionCode = normalizeOptionalText(candidate.appVersionCode, 19);
  if (
    kind === null ||
    occurredAt === null ||
    occurredAt === "INVALID" ||
    occurrenceCount === null ||
    occurrenceCount < 1 ||
    occurrenceCount > 60 ||
    appVersion === "INVALID" ||
    appVersionCode === "INVALID" ||
    (typeof appVersionCode === "string" && !/^\d{1,19}$/u.test(appVersionCode)) ||
    candidate.scope !== "AUTHENTICATED_CURRENT_SESSION"
  ) {
    return null;
  }
  return {
    kind,
    occurredAt,
    occurrenceCount,
    appVersion,
    appVersionCode,
    scope: "AUTHENTICATED_CURRENT_SESSION",
  };
}

async function buildClientErrorIngestHash(events: ClientErrorEvent[]): Promise<string> {
  return `sha256:${await sha256Hex(canonicalJson({ events }))}`;
}

async function buildSupportTicketRequestHash(input: {
  category: SupportTicketCategory;
  bodyText: string;
  appVersion: string | null;
  appVersionCode: string | null;
  clientComposedAt: number | null;
  clientSubmittedAt: number | null;
}): Promise<string> {
  const canonical = canonicalJson({
    category: input.category,
    bodyText: input.bodyText,
    appVersion: input.appVersion,
    appVersionCode: input.appVersionCode,
    clientComposedAt: input.clientComposedAt,
    clientSubmittedAt: input.clientSubmittedAt,
  });
  return `sha256:${await sha256Hex(canonical)}`;
}

async function parseJsonObjectBody(c: GameApiContext): Promise<Record<string, unknown> | AuthFailure> {
  if (Object.keys(c.req.query()).length > 0) {
    return createAuthFailure(400, "contract_violation", "Query parameters are not accepted.");
  }

  let parsed: unknown;
  try {
    parsed = await c.req.json();
  } catch {
    parsed = {};
  }

  if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) {
    return createAuthFailure(400, "contract_violation", "Request body must be a JSON object.");
  }

  const body = parsed as Record<string, unknown>;
  if (Object.hasOwn(body, "accountId") || Object.hasOwn(body, "account_id")) {
    return createAuthFailure(400, "contract_violation", "accountId must not be supplied by the client.");
  }
  return body;
}

function parseRecoveryEmailPurpose(value: unknown): RecoveryEmailPurpose | null {
  if (
    value === "PASSWORD_REGISTER"
    || value === "PASSWORD_LINK"
    || value === "PASSWORD_RESET"
    || value === "RECOVERY_EMAIL_CHANGE"
  ) {
    return value;
  }
  return null;
}

function getChallengeId(value: unknown): string | null {
  if (typeof value !== "string" || !/^email_challenge_[A-Za-z0-9_-]+$/u.test(value)) return null;
  return value;
}

async function requireConsumedRecoveryChallenge(c: GameApiContext, input: {
  challengeId: string;
  purpose: RecoveryEmailPurpose;
  emailNormalized: string;
  accountId: string | null;
}): Promise<RecoveryEmailChallengeRow | AuthFailure> {
  const row = await c.env.DB.prepare(
    `SELECT id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, created_at
     FROM auth_email_verification_challenges
     WHERE id = ? AND purpose = ? AND email_normalized = ?`,
  )
    .bind(input.challengeId, input.purpose, input.emailNormalized)
    .first<RecoveryEmailChallengeRow>();

  if (
    row === null
    || row.status !== "CONSUMED"
    || row.expires_at <= Date.now()
    || (input.accountId === null ? row.account_id !== null : row.account_id !== input.accountId)
  ) {
    return createAuthFailure(409, "recovery_email_verification_required", "Recovery email ownership verification is required.");
  }

  return row;
}

async function findPasswordResetChallenge(c: GameApiContext, body: Record<string, unknown>): Promise<RecoveryEmailChallengeRow | AuthFailure> {
  const explicitChallengeId = getChallengeId(body.challengeId);
  if (explicitChallengeId !== null) {
    const row = await c.env.DB.prepare(
      `SELECT id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, created_at
       FROM auth_email_verification_challenges
       WHERE id = ? AND purpose = 'PASSWORD_RESET'`,
    )
      .bind(explicitChallengeId)
      .first<RecoveryEmailChallengeRow>();
    if (row === null || row.account_id === null || row.status !== "PENDING" || row.expires_at <= Date.now()) {
      return createAuthFailure(409, "password_reset_challenge_invalid", "Password reset challenge is invalid or expired.");
    }
    return row;
  }

  const loginId = normalizeLoginId(body.loginId);
  const recoveryEmail = normalizeRecoveryEmail(body.recoveryEmail);
  if (loginId === null || recoveryEmail === null) {
    return createAuthFailure(400, "contract_violation", "loginId, recoveryEmail, verificationCode, and newPassword are required.");
  }

  const credential = await c.env.DB.prepare(
    `SELECT credentials.account_id, credentials.password_hash, credentials.password_hash_algorithm,
            credentials.recovery_email_normalized, accounts.status AS account_status
     FROM auth_password_credentials credentials
     JOIN accounts ON accounts.id = credentials.account_id
     WHERE credentials.login_id_normalized = ? AND credentials.recovery_email_normalized = ?`,
  )
    .bind(loginId.normalized, recoveryEmail.normalized)
    .first<PasswordCredentialAccountRow>();
  if (credential === null || credential.account_status !== "ACTIVE") {
    return createAuthFailure(409, "password_reset_challenge_invalid", "Password reset challenge is invalid or expired.");
  }

  const row = await c.env.DB.prepare(
    `SELECT id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, created_at
     FROM auth_email_verification_challenges
     WHERE account_id = ? AND email_normalized = ? AND purpose = 'PASSWORD_RESET' AND status = 'PENDING'
     ORDER BY created_at DESC
     LIMIT 1`,
  )
    .bind(credential.account_id, recoveryEmail.normalized)
    .first<RecoveryEmailChallengeRow>();
  if (row === null || row.account_id === null || row.expires_at <= Date.now()) {
    return createAuthFailure(409, "password_reset_challenge_invalid", "Password reset challenge is invalid or expired.");
  }
  return row;
}

function isSendableRecoveryPurpose(purpose: RecoveryEmailPurpose): boolean {
  return purpose === "PASSWORD_REGISTER" || purpose === "PASSWORD_LINK" || purpose === "PASSWORD_RESET";
}

const displayNameCooldownMs = 7 * 24 * 60 * 60 * 1000;
const reservedDisplayNamePrefixes = ["account_", "admin_", "gm_", "system_"];
const reservedDisplayNames = new Set(["admin", "administrator", "gm", "system", "operator", "support"]);

function profileError(errorCode: string, message: string) {
  return {
    accepted: false,
    errorCode,
    message,
  };
}

function hasNonEmptyRequestBody(c: GameApiContext): boolean {
  const contentLength = c.req.header("content-length");
  if (contentLength !== undefined && contentLength !== "0") return true;
  return c.req.header("content-type") !== undefined;
}

function getRealtimeCoopRooms(env: Env): RealtimeCoopRoomNamespace | null {
  return (env as GameApiEnv).REALTIME_COOP_ROOMS ?? null;
}

function realtimeCoopUnavailable() {
  return createRealtimeCoopRejected("realtime_coop_unavailable", "Realtime coop room service is not configured.");
}

async function parseRealtimeCoopRoomBody(c: GameApiContext): Promise<{ runId: string } | AuthFailure> {
  if (new URL(c.req.url).searchParams.toString() !== "") {
    return createAuthFailure(400, "contract_violation", "Query parameters are not accepted.");
  }

  let body: unknown;
  try {
    body = await c.req.json();
  } catch {
    return createAuthFailure(400, "contract_violation", "runId is required.");
  }

  if (typeof body !== "object" || body === null || Array.isArray(body)) {
    return createAuthFailure(400, "contract_violation", "Realtime coop room request must be an object.");
  }
  if (containsForbiddenRealtimeCoopField(body)) {
    return createAuthFailure(400, "contract_violation", "Client-supplied realtime source-of-truth fields are not accepted.");
  }

  const record = body as Record<string, unknown>;
  if (typeof record.runId !== "string" || !isSafeRealtimeId(record.runId)) {
    return createAuthFailure(400, "contract_violation", "runId is required.");
  }
  if (Object.keys(record).some((key) => key !== "runId")) {
    return createAuthFailure(400, "contract_violation", "Only runId is accepted.");
  }

  return { runId: record.runId };
}

function createRealtimeConnectionFailure(errorCode: string, message: string) {
  return {
    accepted: false,
    connectionStatus: "REJECTED",
    errorCode,
    message,
  };
}

function normalizeDisplayNameInput(input: unknown): { displayName: string; normalized: string } | AuthFailure {
  if (typeof input !== "string") {
    return createAuthFailure(400, "invalid_display_name", "Display name must be a string.");
  }

  const displayName = input.normalize("NFKC").trim().replace(/\s+/gu, " ");
  const normalized = displayName.toLocaleLowerCase("en-US");
  const length = [...displayName].length;
  if (length < 2 || length > 16) {
    return createAuthFailure(400, "invalid_display_name", "Display name must be between 2 and 16 characters.");
  }
  if (!/^[\p{L}\p{N} _-]+$/u.test(displayName)) {
    return createAuthFailure(400, "invalid_display_name", "Display name contains unsupported characters.");
  }
  if (reservedDisplayNames.has(normalized) || reservedDisplayNamePrefixes.some((prefix) => normalized.startsWith(prefix))) {
    return createAuthFailure(400, "invalid_display_name", "Display name is reserved.");
  }

  return { displayName, normalized };
}

function nextDisplayNameChangeAt(updatedAt: number | null | undefined): string | null {
  if (typeof updatedAt !== "number") return null;
  return toIso(updatedAt + displayNameCooldownMs);
}

function displayNameCooldownActive(updatedAt: number | null | undefined, now: number): boolean {
  return typeof updatedAt === "number" && updatedAt + displayNameCooldownMs > now;
}

async function resolveAuthContext(c: GameApiContext): Promise<AuthContext | AuthFailure> {
  const authorization = c.req.header("Authorization");
  if (authorization === undefined || !authorization.startsWith("Bearer ")) {
    return createAuthFailure(401, "auth_required", "Authorization bearer token is required.");
  }

  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return createAuthFailure(500, "auth_not_configured", "Auth signing key is not configured.");
  }

  const token = authorization.slice("Bearer ".length).trim();
  const verified = await verifyAccessToken({ secret, token, nowMs: Date.now() });
  if (verified === null) {
    return createAuthFailure(401, "access_token_invalid", "Access token is invalid or expired.");
  }

  const row = await c.env.DB.prepare(
    `SELECT sessions.account_id, sessions.id AS session_id, sessions.status AS session_status,
            sessions.expires_at AS session_expires_at, accounts.status AS account_status,
            accounts.created_at AS account_created_at
     FROM auth_sessions sessions
     JOIN accounts ON accounts.id = sessions.account_id
     WHERE sessions.id = ? AND sessions.account_id = ?`,
  )
    .bind(verified.sessionId, verified.accountId)
    .first<AuthSessionAccountRow>();

  if (row === null || row.session_status !== "ACTIVE" || row.session_expires_at <= Date.now()) {
    return createAuthFailure(401, "auth_session_invalid", "Auth session is invalid or expired.");
  }

  if (row.account_status !== "ACTIVE") {
    return createAuthFailure(403, "account_not_active", "Account is not active.");
  }

  return {
    accountId: row.account_id,
    sessionId: row.session_id,
    accountCreatedAt: typeof row.account_created_at === "number" ? row.account_created_at : Date.now(),
  };
}

async function createAuthTokenBundle(options: {
  env: Env;
  secret: string;
  accountId: string;
  now: number;
}) {
  const sessionId = createDomainId("session");
  const familyId = createDomainId("family");
  const refreshToken = createRefreshToken();
  const refreshTokenHash = await hashRefreshToken(refreshToken);
  const accessTokenTtlMs = 15 * 60 * 1000;
  const refreshTokenTtlMs = 30 * 24 * 60 * 60 * 1000;
  const accessTokenExpiresAt = options.now + accessTokenTtlMs;
  const refreshTokenExpiresAt = options.now + refreshTokenTtlMs;
  const accessToken = await createAccessToken({
    secret: options.secret,
    accountId: options.accountId,
    sessionId,
    nowMs: options.now,
    ttlMs: accessTokenTtlMs,
  });

  return {
    sessionId,
    familyId,
    refreshToken,
    refreshTokenHash,
    refreshTokenExpiresAt,
    response: {
      tokenType: "Bearer",
      account: {
        id: options.accountId,
        status: "ACTIVE",
      },
      accessToken,
      accessTokenExpiresAt: toIso(accessTokenExpiresAt),
      refreshToken,
      refreshTokenExpiresAt: toIso(refreshTokenExpiresAt),
    },
  };
}

async function verifyGoogleIdentity(c: GameApiContext, idToken: string): Promise<{ subject: string } | AuthFailure> {
  const audiences = getGoogleAudiences(c.env);
  const verifier = getGoogleVerifier(c.env);
  if (audiences.length === 0 || verifier === null) {
    await writeGameApiOpsErrorLog(c, {
      severity: "critical",
      errorCode: "google_auth_config_missing",
      messageSummary: "Google auth verifier or audience configuration is missing.",
      context: {
        hasAudiences: audiences.length > 0,
        hasVerifier: verifier !== null,
      },
      retryable: false,
    });
    return createAuthFailure(500, "google_auth_not_configured", "Google auth verifier is not configured.");
  }

  let claims: GoogleIdentityClaims;
  try {
    claims = await verifier.verifyIdToken(idToken, audiences);
  } catch (error) {
    if (error instanceof GoogleIdTokenVerificationError && error.code.startsWith("google_jwks_")) {
      await writeGameApiOpsErrorLog(c, {
        severity: "error",
        errorCode: "google_auth_verifier_unavailable",
        messageSummary: "Google auth verifier public keys are unavailable.",
        context: {
          verifierErrorCode: error.code,
        },
        retryable: true,
      });
      return createAuthFailure(503, "google_auth_temporarily_unavailable", "Google auth verification is temporarily unavailable.");
    }
    return createAuthFailure(401, "google_id_token_invalid", "Google ID token is invalid.");
  }

  if (typeof claims.subject !== "string" || claims.subject.trim() === "") {
    return createAuthFailure(401, "google_id_token_invalid", "Google ID token subject is invalid.");
  }

  return { subject: claims.subject };
}

async function parseGoogleAuthBody(c: GameApiContext): Promise<{ idToken: string } | AuthFailure> {
  if (Object.keys(c.req.query()).length > 0) {
    return createAuthFailure(400, "contract_violation", "Query parameters are not accepted.");
  }

  let body: Record<string, unknown>;
  try {
    const parsed = await c.req.json();
    body = typeof parsed === "object" && parsed !== null ? parsed as Record<string, unknown> : {};
  } catch {
    body = {};
  }

  if (Object.hasOwn(body, "accountId") || Object.hasOwn(body, "account_id")) {
    return createAuthFailure(400, "contract_violation", "accountId must not be supplied by the client.");
  }

  if (typeof body.idToken !== "string" || body.idToken.trim() === "") {
    return createAuthFailure(400, "contract_violation", "idToken is required.");
  }

  return { idToken: body.idToken };
}

app.use("*", async (c, next) => {
  const env = c.env as GameApiEnv;
  if (env.CLIENT_ADMISSION_MODE !== "ENFORCED" || !isClientAdmissionProtectedRoute(new URL(c.req.url).pathname)) {
    return next();
  }

  const admission = await createClientAdmissionForRequest(c);
  if (admission.registryRevision !== null && admission.bootstrap.canAuthenticate) {
    const path = new URL(c.req.url).pathname;
    if (isRegistryBootstrapRoute(c.req.method, path)) return next();
    if (isRegistryHydrationRoute(c.req.method, path)) {
      const auth = await resolveAuthContext(c);
      if (!("accountId" in auth)) return c.json(auth, auth.status);
      return next();
    }
  }
  if (admission.accepted === false) {
    return c.json(admission, clientAdmissionHttpStatus(admission.errorCode));
  }
  if (admission.registryRevision !== null) {
    const pin = c.req.header("X-LastRail-Content-Release-Id");
    if (pin !== admission.contentRelease.releaseId) return c.json({ accepted: false,
      errorCode: pin ? "content_release_pin_mismatch" : "content_release_pin_required",
      message: "Authorize and pin the selected content release before gameplay." }, 409);
  }

  return next();
});

app.get("/status", (c) => c.json({ ok: true, service: "game-api", requestId: createRequestId() }));
app.post("/v1/auth/password/recovery-email/verify/start", async (c) => {
  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: null,
      retryAfterSeconds: null,
      errorCode: body.errorCode,
      message: body.message,
    }), body.status);
  }

  const sender = getRecoveryEmailSender(c.env);
  if (sender === null) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: null,
      retryAfterSeconds: null,
      errorCode: "recovery_email_service_unavailable",
      message: "Recovery email verification is not configured.",
    }), 503);
  }

  const purpose = parseRecoveryEmailPurpose(body.purpose);
  const recoveryEmail = normalizeRecoveryEmail(body.recoveryEmail);
  if (purpose === null || !isSendableRecoveryPurpose(purpose) || recoveryEmail === null) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: null,
      retryAfterSeconds: null,
      errorCode: "contract_violation",
      message: "A valid recovery email verification purpose and email are required.",
    }), 400);
  }

  let accountId: string | null = null;
  if (purpose === "PASSWORD_LINK") {
    const auth = await resolveAuthContext(c);
    if (isAuthFailure(auth)) {
      return c.json(createRecoveryVerificationResponse({
        accepted: false,
        verificationStatus: "REJECTED",
        challengeId: null,
        expiresAt: null,
        retryAfterSeconds: null,
        errorCode: auth.errorCode,
        message: auth.message,
      }), auth.status);
    }
    accountId = auth.accountId;
  }

  const now = Date.now();
  const latestChallenge = await c.env.DB.prepare(
    `SELECT id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, created_at
     FROM auth_email_verification_challenges
     WHERE email_normalized = ? AND purpose = ? AND status = 'PENDING'
     ORDER BY created_at DESC
     LIMIT 1`,
  )
    .bind(recoveryEmail.normalized, purpose)
    .first<RecoveryEmailChallengeRow>();

  if (latestChallenge !== null && latestChallenge.created_at + 60_000 > now) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: latestChallenge.expires_at,
      retryAfterSeconds: 60,
      errorCode: "recovery_email_resend_limited",
      message: "Please wait before requesting another recovery email verification.",
    }), 429);
  }

  const challengeId = createDomainId("email_challenge");
  const verificationCode = createVerificationCode();
  const verifierHash = await hashVerificationCode(challengeId, purpose, verificationCode);
  const expiresAt = now + 15 * 60 * 1000;

  await c.env.DB.prepare(
    `INSERT INTO auth_email_verification_challenges (
       id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, consumed_at, created_at, updated_at
     ) VALUES (?, ?, ?, ?, ?, 'PENDING', ?, NULL, ?, ?)`,
  ).bind(challengeId, accountId, recoveryEmail.normalized, purpose, verifierHash, expiresAt, now, now).run();

  try {
    await sender.sendRecoveryEmail({
      toEmail: recoveryEmail.email,
      emailMasked: recoveryEmail.masked,
      purpose,
      challengeId,
      verificationCode,
      expiresAt: toIso(expiresAt),
    });
  } catch (error) {
    await c.env.DB.prepare(
      `UPDATE auth_email_verification_challenges
       SET status = 'CANCELLED', updated_at = ?
       WHERE id = ? AND status = 'PENDING'`,
    ).bind(Date.now(), challengeId).run();
    await writeRecoveryEmailSenderFailureOpsLog(c, {
      error,
      accountId,
      purpose,
      challengeId,
      emailMasked: recoveryEmail.masked,
    });
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: null,
      retryAfterSeconds: null,
      errorCode: "recovery_email_service_unavailable",
      message: "Recovery email verification is not configured.",
    }), 503);
  }

  return c.json(createRecoveryVerificationResponse({
    accepted: true,
    verificationStatus: "PENDING",
    challengeId,
    expiresAt,
    retryAfterSeconds: 60,
    errorCode: null,
    message: null,
  }));
});

app.post("/v1/auth/password/recovery-email/verify/confirm", async (c) => {
  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: null,
      retryAfterSeconds: null,
      errorCode: body.errorCode,
      message: body.message,
    }), body.status);
  }

  const challengeId = getChallengeId(body.challengeId);
  const verificationCode = typeof body.verificationCode === "string" ? body.verificationCode.trim() : "";
  if (challengeId === null || verificationCode === "") {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId: null,
      expiresAt: null,
      retryAfterSeconds: null,
      errorCode: "contract_violation",
      message: "challengeId and verificationCode are required.",
    }), 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, created_at
     FROM auth_email_verification_challenges
     WHERE id = ?`,
  )
    .bind(challengeId)
    .first<RecoveryEmailChallengeRow>();

  if (row === null || row.status !== "PENDING" || row.expires_at <= Date.now()) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId,
      expiresAt: row?.expires_at ?? null,
      retryAfterSeconds: null,
      errorCode: "recovery_email_challenge_invalid",
      message: "Recovery email verification challenge is invalid or expired.",
    }), 409);
  }

  if (row.purpose === "PASSWORD_LINK") {
    const auth = await resolveAuthContext(c);
    if (isAuthFailure(auth) || auth.accountId !== row.account_id) {
      const status = isAuthFailure(auth) ? auth.status : 409;
      return c.json(createRecoveryVerificationResponse({
        accepted: false,
        verificationStatus: "REJECTED",
        challengeId,
        expiresAt: row.expires_at,
        retryAfterSeconds: null,
        errorCode: isAuthFailure(auth) ? auth.errorCode : "recovery_email_challenge_invalid",
        message: isAuthFailure(auth) ? auth.message : "Recovery email verification challenge is invalid or expired.",
      }), status);
    }
  }

  const expectedHash = await hashVerificationCode(challengeId, row.purpose, verificationCode);
  if (expectedHash !== row.verifier_hash) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId,
      expiresAt: row.expires_at,
      retryAfterSeconds: null,
      errorCode: "recovery_email_challenge_invalid",
      message: "Recovery email verification challenge is invalid or expired.",
    }), 409);
  }

  const consumed = await c.env.DB.prepare(
    `UPDATE auth_email_verification_challenges
     SET status = 'CONSUMED', consumed_at = ?, updated_at = ?
     WHERE id = ? AND status = 'PENDING'`,
  ).bind(Date.now(), Date.now(), challengeId).run();
  if (d1Changes(consumed) === 0) {
    return c.json(createRecoveryVerificationResponse({
      accepted: false,
      verificationStatus: "REJECTED",
      challengeId,
      expiresAt: row.expires_at,
      retryAfterSeconds: null,
      errorCode: "recovery_email_challenge_invalid",
      message: "Recovery email verification challenge is invalid or expired.",
    }), 409);
  }

  return c.json(createRecoveryVerificationResponse({
    accepted: true,
    verificationStatus: "VERIFIED",
    challengeId,
    expiresAt: row.expires_at,
    retryAfterSeconds: null,
    errorCode: null,
    message: null,
  }));
});

app.post("/v1/auth/password/register", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createPasswordRegisterRejected("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) return c.json(createPasswordRegisterRejected(body.errorCode, body.message), body.status);

  const loginId = normalizeLoginId(body.loginId);
  const recoveryEmail = normalizeRecoveryEmail(body.recoveryEmail);
  const password = validatePassword(body.password);
  const verificationChallengeId = getChallengeId(body.verificationChallengeId);
  if (loginId === null || recoveryEmail === null || password === null) {
    return c.json(createPasswordRegisterRejected("contract_violation", "loginId, password, and recoveryEmail are required."), 400);
  }
  if (verificationChallengeId === null) {
    return c.json(createPasswordRegisterRejected("recovery_email_verification_required", "Recovery email ownership verification is required."), 409);
  }

  const existingLogin = await c.env.DB.prepare(
    `SELECT account_id, password_hash, password_hash_algorithm, recovery_email_normalized, accounts.status AS account_status
     FROM auth_password_credentials credentials
     JOIN accounts ON accounts.id = credentials.account_id
     WHERE credentials.login_id_normalized = ?`,
  )
    .bind(loginId.normalized)
    .first<PasswordCredentialAccountRow>();
  if (existingLogin !== null) {
    return c.json(createPasswordRegisterRejected("login_id_unavailable", "Login ID is unavailable."), 409);
  }

  const challenge = await requireConsumedRecoveryChallenge(c, {
    challengeId: verificationChallengeId,
    purpose: "PASSWORD_REGISTER",
    emailNormalized: recoveryEmail.normalized,
    accountId: null,
  });
  if (isAuthFailure(challenge)) {
    return c.json(createPasswordRegisterRejected(challenge.errorCode, challenge.message), challenge.status);
  }

  const now = Date.now();
  const accountId = createDomainId("account");
  const passwordRecord = await hashPassword(password);
  const bundle = await createAuthTokenBundle({ env: c.env, secret, accountId, now });

  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO accounts (id, status, created_at, updated_at)
       VALUES (?, 'ACTIVE', ?, ?)`,
    ).bind(accountId, now, now),
    c.env.DB.prepare(
      `INSERT INTO auth_password_credentials (
         account_id, login_id_normalized, password_hash, password_hash_algorithm,
         password_hash_params_json, recovery_email_normalized, created_at, updated_at
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
    ).bind(
      accountId,
      loginId.normalized,
      passwordRecord.passwordHash,
      passwordRecord.algorithm,
      passwordRecord.paramsJson,
      recoveryEmail.normalized,
      now,
      now,
    ),
    c.env.DB.prepare(
      `INSERT INTO auth_recovery_emails (
         account_id, email_normalized, email_masked, status, verified_at, created_at, updated_at
       ) VALUES (?, ?, ?, 'VERIFIED', ?, ?, ?)`,
    ).bind(accountId, recoveryEmail.normalized, recoveryEmail.masked, now, now, now),
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(bundle.sessionId, accountId, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
    c.env.DB.prepare(
      `INSERT INTO player_profiles (account_id, display_name, commander_xp, created_at, updated_at)
       VALUES (?, NULL, 0, ?, ?)`,
    ).bind(accountId, now, now),
    c.env.DB.prepare(
      `INSERT INTO account_runtime_state (account_id, protection_mode, maintenance_until, updated_at)
       VALUES (?, 'NORMAL', NULL, ?)`,
    ).bind(accountId, now),
    c.env.DB.prepare(
      `INSERT INTO account_tutorial_state (account_id, status, completed_at, updated_at)
       VALUES (?, 'NOT_STARTED', NULL, ?)`,
    ).bind(accountId, now),
    c.env.DB.prepare(
      `INSERT INTO account_offline_reward_state (
         account_id, last_offline_reward_claim_at, last_story_progress_id,
         last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at
       ) VALUES (?, ?, ?, NULL, ?, NULL, 0, ?)`,
    ).bind(accountId, now, "none", actOneNarrativeVersion, now),
    c.env.DB.prepare(
      `UPDATE auth_email_verification_challenges
       SET status = 'CANCELLED', updated_at = ?
       WHERE id = ? AND status = 'CONSUMED'`,
    ).bind(now, verificationChallengeId),
  ]);

  return c.json({
    ...bundle.response,
    accepted: true,
    registerStatus: "REGISTERED",
    errorCode: null,
    message: null,
  });
});

app.post("/v1/auth/password/link", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createPasswordLinkRejected("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const auth = await resolveAuthContext(c);
  if (isAuthFailure(auth)) return c.json(createPasswordLinkRejected(auth.errorCode, auth.message), auth.status);

  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) return c.json(createPasswordLinkRejected(body.errorCode, body.message), body.status);

  const loginId = normalizeLoginId(body.loginId);
  const recoveryEmail = normalizeRecoveryEmail(body.recoveryEmail);
  const password = validatePassword(body.password);
  const verificationChallengeId = getChallengeId(body.verificationChallengeId);
  if (loginId === null || recoveryEmail === null || password === null) {
    return c.json(createPasswordLinkRejected("contract_violation", "loginId, password, and recoveryEmail are required."), 400);
  }
  if (verificationChallengeId === null) {
    return c.json(createPasswordLinkRejected("recovery_email_verification_required", "Recovery email ownership verification is required."), 409);
  }

  const accountCredential = await c.env.DB.prepare(
    `SELECT account_id, password_hash, password_hash_algorithm, recovery_email_normalized, accounts.status AS account_status
     FROM auth_password_credentials credentials
     JOIN accounts ON accounts.id = credentials.account_id
     WHERE credentials.account_id = ?`,
  )
    .bind(auth.accountId)
    .first<PasswordCredentialAccountRow>();
  if (accountCredential !== null) {
    return c.json(createPasswordLinkRejected("password_credential_already_linked", "Password credential is already linked to this account."), 409);
  }

  const existingLogin = await c.env.DB.prepare(
    `SELECT account_id, password_hash, password_hash_algorithm, recovery_email_normalized, accounts.status AS account_status
     FROM auth_password_credentials credentials
     JOIN accounts ON accounts.id = credentials.account_id
     WHERE credentials.login_id_normalized = ?`,
  )
    .bind(loginId.normalized)
    .first<PasswordCredentialAccountRow>();
  if (existingLogin !== null) {
    return c.json(createPasswordLinkRejected("login_id_unavailable", "Login ID is unavailable."), 409);
  }

  const challenge = await requireConsumedRecoveryChallenge(c, {
    challengeId: verificationChallengeId,
    purpose: "PASSWORD_LINK",
    emailNormalized: recoveryEmail.normalized,
    accountId: auth.accountId,
  });
  if (isAuthFailure(challenge)) return c.json(createPasswordLinkRejected(challenge.errorCode, challenge.message), challenge.status);

  const now = Date.now();
  const passwordRecord = await hashPassword(password);
  const bundle = await createAuthTokenBundle({ env: c.env, secret, accountId: auth.accountId, now });

  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO auth_password_credentials (
         account_id, login_id_normalized, password_hash, password_hash_algorithm,
         password_hash_params_json, recovery_email_normalized, created_at, updated_at
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
    ).bind(
      auth.accountId,
      loginId.normalized,
      passwordRecord.passwordHash,
      passwordRecord.algorithm,
      passwordRecord.paramsJson,
      recoveryEmail.normalized,
      now,
      now,
    ),
    c.env.DB.prepare(
      `INSERT INTO auth_recovery_emails (
         account_id, email_normalized, email_masked, status, verified_at, created_at, updated_at
       ) VALUES (?, ?, ?, 'VERIFIED', ?, ?, ?)
       ON CONFLICT(account_id) DO UPDATE SET
         email_normalized = excluded.email_normalized,
         email_masked = excluded.email_masked,
         status = 'VERIFIED',
         verified_at = excluded.verified_at,
         updated_at = excluded.updated_at`,
    ).bind(auth.accountId, recoveryEmail.normalized, recoveryEmail.masked, now, now, now),
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(bundle.sessionId, auth.accountId, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
    c.env.DB.prepare(
      `UPDATE auth_email_verification_challenges
       SET status = 'CANCELLED', updated_at = ?
       WHERE id = ? AND status = 'CONSUMED'`,
    ).bind(now, verificationChallengeId),
  ]);

  return c.json({
    ...bundle.response,
    accepted: true,
    linkStatus: "LINKED",
    errorCode: null,
    message: null,
  });
});

app.post("/v1/auth/password/login", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) return c.json(createAuthError(body.errorCode, body.message), body.status);

  const loginId = normalizeLoginId(body.loginId);
  const password = validatePassword(body.password);
  if (loginId === null || password === null) {
    return c.json(createAuthError("contract_violation", "loginId and password are required."), 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT credentials.account_id, credentials.password_hash, credentials.password_hash_algorithm,
            credentials.recovery_email_normalized, accounts.status AS account_status
     FROM auth_password_credentials credentials
     JOIN accounts ON accounts.id = credentials.account_id
     WHERE credentials.login_id_normalized = ?`,
  )
    .bind(loginId.normalized)
    .first<PasswordCredentialAccountRow>();
  if (row === null || row.account_status !== "ACTIVE" || row.password_hash_algorithm !== "PBKDF2_SHA256") {
    return c.json(createAuthError("password_login_invalid", "Login ID or password is invalid."), 401);
  }

  const verified = await verifyPasswordHash(password, row.password_hash);
  if (!verified) {
    return c.json(createAuthError("password_login_invalid", "Login ID or password is invalid."), 401);
  }

  const now = Date.now();
  const bundle = await createAuthTokenBundle({ env: c.env, secret, accountId: row.account_id, now });
  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(bundle.sessionId, row.account_id, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
  ]);

  return c.json({
    ...bundle.response,
    signInStatus: "SIGNED_IN",
  });
});

app.post("/v1/auth/password/reset/start", async (c) => {
  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) return c.json(createAuthError(body.errorCode, body.message), body.status);

  const sender = getRecoveryEmailSender(c.env);
  if (sender === null) {
    return c.json(createAuthError("recovery_email_service_unavailable", "Recovery email verification is not configured."), 503);
  }

  const loginId = normalizeLoginId(body.loginId);
  const recoveryEmail = normalizeRecoveryEmail(body.recoveryEmail);
  if (loginId === null || recoveryEmail === null) {
    return c.json(createAuthError("contract_violation", "loginId and recoveryEmail are required."), 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT credentials.account_id, credentials.password_hash, credentials.password_hash_algorithm,
            credentials.recovery_email_normalized, accounts.status AS account_status
     FROM auth_password_credentials credentials
     JOIN accounts ON accounts.id = credentials.account_id
     WHERE credentials.login_id_normalized = ? AND credentials.recovery_email_normalized = ?`,
  )
    .bind(loginId.normalized, recoveryEmail.normalized)
    .first<PasswordCredentialAccountRow>();

  const response = {
    accepted: true,
    resetStatus: "EMAIL_SENT_IF_AVAILABLE",
    errorCode: null,
    message: null,
  };
  if (row === null || row.account_status !== "ACTIVE") return c.json(response);

  const now = Date.now();
  const latestChallenge = await c.env.DB.prepare(
    `SELECT id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, created_at
     FROM auth_email_verification_challenges
     WHERE email_normalized = ? AND purpose = 'PASSWORD_RESET' AND status = 'PENDING'
     ORDER BY created_at DESC
     LIMIT 1`,
  )
    .bind(recoveryEmail.normalized)
    .first<RecoveryEmailChallengeRow>();
  if (latestChallenge !== null && latestChallenge.created_at + 60_000 > now) return c.json(response);

  const challengeId = createDomainId("email_challenge");
  const verificationCode = createVerificationCode();
  const expiresAt = now + 15 * 60 * 1000;
  const verifierHash = await hashVerificationCode(challengeId, "PASSWORD_RESET", verificationCode);
  await c.env.DB.prepare(
    `INSERT INTO auth_email_verification_challenges (
       id, account_id, email_normalized, purpose, verifier_hash, status, expires_at, consumed_at, created_at, updated_at
     ) VALUES (?, ?, ?, 'PASSWORD_RESET', ?, 'PENDING', ?, NULL, ?, ?)`,
  ).bind(challengeId, row.account_id, recoveryEmail.normalized, verifierHash, expiresAt, now, now).run();
  try {
    await sender.sendRecoveryEmail({
      toEmail: recoveryEmail.email,
      emailMasked: recoveryEmail.masked,
      purpose: "PASSWORD_RESET",
      challengeId,
      verificationCode,
      expiresAt: toIso(expiresAt),
    });
  } catch (error) {
    await c.env.DB.prepare(
      `UPDATE auth_email_verification_challenges
       SET status = 'CANCELLED', updated_at = ?
       WHERE id = ? AND status = 'PENDING'`,
    ).bind(Date.now(), challengeId).run();
    await writeRecoveryEmailSenderFailureOpsLog(c, {
      error,
      accountId: row.account_id,
      purpose: "PASSWORD_RESET",
      challengeId,
      emailMasked: recoveryEmail.masked,
    });
  }

  return c.json(response);
});

app.post("/v1/auth/password/reset/confirm", async (c) => {
  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) return c.json(createAuthError(body.errorCode, body.message), body.status);

  const verificationCode = typeof body.verificationCode === "string" ? body.verificationCode.trim() : "";
  const newPassword = validatePassword(body.newPassword);
  if (verificationCode === "" || newPassword === null) {
    return c.json(createAuthError("contract_violation", "loginId, recoveryEmail, verificationCode, and newPassword are required."), 400);
  }

  const row = await findPasswordResetChallenge(c, body);
  if (isAuthFailure(row)) return c.json(createAuthError(row.errorCode, row.message), row.status);

  const expectedHash = await hashVerificationCode(row.id, "PASSWORD_RESET", verificationCode);
  if (expectedHash !== row.verifier_hash) {
    return c.json(createAuthError("password_reset_challenge_invalid", "Password reset challenge is invalid or expired."), 409);
  }

  const passwordRecord = await hashPassword(newPassword);
  const now = Date.now();
  await c.env.DB.batch([
    c.env.DB.prepare(
      `UPDATE auth_email_verification_challenges
       SET status = 'CONSUMED', consumed_at = ?, updated_at = ?
       WHERE id = ? AND status = 'PENDING'`,
    ).bind(now, now, row.id),
    c.env.DB.prepare(
      `UPDATE auth_password_credentials
       SET password_hash = ?, password_hash_algorithm = ?, password_hash_params_json = ?, updated_at = ?
       WHERE account_id = ?`,
    ).bind(passwordRecord.passwordHash, passwordRecord.algorithm, passwordRecord.paramsJson, now, row.account_id),
    c.env.DB.prepare(
      `UPDATE auth_sessions
       SET status = 'REVOKED', rotated_at = ?
       WHERE account_id = ? AND status = 'ACTIVE'`,
    ).bind(now, row.account_id),
  ]);

  return c.json({
    accepted: true,
    resetStatus: "RESET_CONFIRMED",
    errorCode: null,
    message: "Password was reset. Please sign in again.",
  });
});

app.post("/v1/auth/guest", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const idempotencyKey = parseOptionalIdempotencyKey(c);
  if (isAuthFailure(idempotencyKey)) {
    return c.json(createAuthError(idempotencyKey.errorCode, idempotencyKey.message), idempotencyKey.status);
  }

  const now = Date.now();
  const guestReplayProviderSubject = idempotencyKey === null
    ? null
    : `auth_guest_create:${await sha256Hex(idempotencyKey)}`;
  if (guestReplayProviderSubject !== null) {
    const replayRow = await readGuestReplayRow(c.env.DB, guestReplayProviderSubject);
    if (replayRow !== null) {
      if (replayRow.account_status !== "ACTIVE") {
        return c.json(createAuthError("account_not_active", "Account is not active."), 403);
      }
      if (replayRow.session_status !== "ACTIVE" || replayRow.session_expires_at <= now) {
        return c.json(createAuthError("auth_replay_expired", "Auth replay window is expired."), 401);
      }
      const refreshToken = await createReplaySafeRefreshToken(
        secret,
        "auth_guest_create",
        `${guestReplayProviderSubject}:${replayRow.account_id}:${replayRow.session_id}`,
      );
      return c.json(await createAuthResponseForSession({
        secret,
        accountId: replayRow.account_id,
        sessionId: replayRow.session_id,
        refreshToken,
        refreshTokenExpiresAt: replayRow.session_expires_at,
        now,
      }));
    }
  }

  const accountId = createDomainId("account");
  const identityId = createDomainId("identity");
  const sessionId = createDomainId("session");
  const familyId = createDomainId("family");
  const providerSubject = guestReplayProviderSubject ?? createDomainId("guest");
  const refreshToken = guestReplayProviderSubject === null
    ? createRefreshToken()
    : await createReplaySafeRefreshToken(secret, "auth_guest_create", `${guestReplayProviderSubject}:${accountId}:${sessionId}`);
  const refreshTokenHash = await hashRefreshToken(refreshToken);
  const accessTokenTtlMs = 15 * 60 * 1000;
  const refreshTokenTtlMs = 30 * 24 * 60 * 60 * 1000;
  const accessTokenExpiresAt = now + accessTokenTtlMs;
  const refreshTokenExpiresAt = now + refreshTokenTtlMs;
  const accessToken = await createAccessToken({
    secret,
    accountId,
    sessionId,
    nowMs: now,
    ttlMs: accessTokenTtlMs,
  });

  try {
    await c.env.DB.batch([
      c.env.DB.prepare(
        `INSERT INTO accounts (id, status, created_at, updated_at)
         VALUES (?, 'ACTIVE', ?, ?)`,
      ).bind(accountId, now, now),
      c.env.DB.prepare(
        `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
         VALUES (?, ?, 'GUEST', ?, ?)`,
      ).bind(identityId, accountId, providerSubject, now),
      c.env.DB.prepare(
        `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
         VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
      ).bind(sessionId, accountId, refreshTokenHash, familyId, now, refreshTokenExpiresAt),
      c.env.DB.prepare(
        `INSERT INTO player_profiles (account_id, display_name, commander_xp, created_at, updated_at)
         VALUES (?, NULL, 0, ?, ?)`,
      ).bind(accountId, now, now),
      c.env.DB.prepare(
        `INSERT INTO account_runtime_state (account_id, protection_mode, maintenance_until, updated_at)
         VALUES (?, 'NORMAL', NULL, ?)`,
      ).bind(accountId, now),
      c.env.DB.prepare(
        `INSERT INTO account_tutorial_state (account_id, status, completed_at, updated_at)
         VALUES (?, 'NOT_STARTED', NULL, ?)`,
      ).bind(accountId, now),
      c.env.DB.prepare(
        `INSERT INTO account_offline_reward_state (
           account_id, last_offline_reward_claim_at, last_story_progress_id,
           last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at
         ) VALUES (?, ?, ?, NULL, ?, NULL, 0, ?)`,
      ).bind(accountId, now, "none", actOneNarrativeVersion, now),
      ...(idempotencyKey === null ? [] : [
        c.env.DB.prepare(
          `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
           VALUES (?, ?, ?, 'auth_guest_create', ?, ?)`,
        ).bind(createDomainId("idem"), accountId, idempotencyKey, sessionId, now),
      ]),
    ]);
  } catch (error) {
    if (!isUniqueConstraintError(error) || guestReplayProviderSubject === null) {
      throw error;
    }
    const replayRow = await readGuestReplayRow(c.env.DB, guestReplayProviderSubject);
    if (replayRow === null) {
      throw error;
    }
    if (replayRow.account_status !== "ACTIVE") {
      return c.json(createAuthError("account_not_active", "Account is not active."), 403);
    }
    if (replayRow.session_status !== "ACTIVE" || replayRow.session_expires_at <= now) {
      return c.json(createAuthError("auth_replay_expired", "Auth replay window is expired."), 401);
    }
    const replayRefreshToken = await createReplaySafeRefreshToken(
      secret,
      "auth_guest_create",
      `${guestReplayProviderSubject}:${replayRow.account_id}:${replayRow.session_id}`,
    );
    return c.json(await createAuthResponseForSession({
      secret,
      accountId: replayRow.account_id,
      sessionId: replayRow.session_id,
      refreshToken: replayRefreshToken,
      refreshTokenExpiresAt: replayRow.session_expires_at,
      now,
    }));
  }

  return c.json({
    tokenType: "Bearer",
    account: {
      id: accountId,
      status: "ACTIVE",
    },
    accessToken,
    accessTokenExpiresAt: toIso(accessTokenExpiresAt),
    refreshToken,
    refreshTokenExpiresAt: toIso(refreshTokenExpiresAt),
  });
});
app.post("/v1/auth/google", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const body = await parseGoogleAuthBody(c);
  if (isAuthFailure(body)) {
    return c.json(createAuthError(body.errorCode, body.message), body.status);
  }

  const identity = await verifyGoogleIdentity(c, body.idToken);
  if (isAuthFailure(identity)) {
    return c.json(createAuthError(identity.errorCode, identity.message), identity.status);
  }

  const identityRow = await c.env.DB.prepare(
    `SELECT identities.account_id, accounts.status AS account_status
     FROM auth_identities identities
     JOIN accounts ON accounts.id = identities.account_id
     WHERE identities.provider = 'GOOGLE' AND identities.provider_subject = ?`,
  )
    .bind(identity.subject)
    .first<AuthIdentityAccountRow>();

  const now = Date.now();
  if (identityRow !== null) {
    if (identityRow.account_status !== "ACTIVE") {
      return c.json(createAuthError("account_not_active", "Account is not active."), 403);
    }

    const bundle = await createAuthTokenBundle({
      env: c.env,
      secret,
      accountId: identityRow.account_id,
      now,
    });

    await c.env.DB.batch([
      c.env.DB.prepare(
        `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
         VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
      ).bind(bundle.sessionId, identityRow.account_id, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
    ]);

    return c.json(bundle.response);
  }

  const accountId = createDomainId("account");
  const identityId = createDomainId("identity");
  const bundle = await createAuthTokenBundle({
    env: c.env,
    secret,
    accountId,
    now,
  });

  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO accounts (id, status, created_at, updated_at)
       VALUES (?, 'ACTIVE', ?, ?)`,
    ).bind(accountId, now, now),
    c.env.DB.prepare(
      `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
       VALUES (?, ?, 'GOOGLE', ?, ?)`,
    ).bind(identityId, accountId, identity.subject, now),
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(bundle.sessionId, accountId, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
    c.env.DB.prepare(
      `INSERT INTO player_profiles (account_id, display_name, commander_xp, created_at, updated_at)
       VALUES (?, NULL, 0, ?, ?)`,
    ).bind(accountId, now, now),
    c.env.DB.prepare(
      `INSERT INTO account_runtime_state (account_id, protection_mode, maintenance_until, updated_at)
       VALUES (?, 'NORMAL', NULL, ?)`,
    ).bind(accountId, now),
    c.env.DB.prepare(
      `INSERT INTO account_tutorial_state (account_id, status, completed_at, updated_at)
       VALUES (?, 'NOT_STARTED', NULL, ?)`,
    ).bind(accountId, now),
    c.env.DB.prepare(
      `INSERT INTO account_offline_reward_state (
         account_id, last_offline_reward_claim_at, last_story_progress_id,
         last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at
       ) VALUES (?, ?, ?, NULL, ?, NULL, 0, ?)`,
    ).bind(accountId, now, "none", actOneNarrativeVersion, now),
  ]);

  return c.json(bundle.response);
});
app.post("/v1/auth/google/link", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const body = await parseGoogleAuthBody(c);
  if (isAuthFailure(body)) {
    return c.json(createAuthError(body.errorCode, body.message), body.status);
  }

  const auth = await resolveAuthContext(c);
  if (isAuthFailure(auth)) {
    return c.json(createAuthError(auth.errorCode, auth.message), auth.status);
  }

  const identity = await verifyGoogleIdentity(c, body.idToken);
  if (isAuthFailure(identity)) {
    return c.json(createAuthError(identity.errorCode, identity.message), identity.status);
  }

  const identityRow = await c.env.DB.prepare(
    `SELECT identities.account_id, accounts.status AS account_status
     FROM auth_identities identities
     JOIN accounts ON accounts.id = identities.account_id
     WHERE identities.provider = 'GOOGLE' AND identities.provider_subject = ?`,
  )
    .bind(identity.subject)
    .first<AuthIdentityAccountRow>();

  if (identityRow !== null && identityRow.account_id !== auth.accountId) {
    return c.json(createAuthError("google_identity_already_linked", "Google identity is already linked to another account."), 409);
  }

  const now = Date.now();
  const bundle = await createAuthTokenBundle({
    env: c.env,
    secret,
    accountId: auth.accountId,
    now,
  });
  const linkStatus = identityRow === null ? "LINKED" : "ALREADY_LINKED";
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
       VALUES (?, ?, ?, ?, 0, 'ACTIVE', ?, ?, NULL)`,
    ).bind(bundle.sessionId, auth.accountId, bundle.refreshTokenHash, bundle.familyId, now, bundle.refreshTokenExpiresAt),
  ];

  if (identityRow === null) {
    statements.unshift(
      c.env.DB.prepare(
        `INSERT INTO auth_identities (id, account_id, provider, provider_subject, linked_at)
         VALUES (?, ?, 'GOOGLE', ?, ?)`,
      ).bind(createDomainId("identity"), auth.accountId, identity.subject, now),
    );
  }

  await c.env.DB.batch(statements);

  return c.json({
    ...bundle.response,
    linkStatus,
  });
});
app.post("/v1/auth/refresh", async (c) => {
  const secret = getAuthSecret(c.env);
  if (secret === null || secret.trim() === "") {
    return c.json(createAuthError("auth_not_configured", "Auth signing key is not configured."), 500);
  }

  const idempotencyKey = parseOptionalIdempotencyKey(c);
  if (isAuthFailure(idempotencyKey)) {
    return c.json(createAuthError(idempotencyKey.errorCode, idempotencyKey.message), idempotencyKey.status);
  }

  let body: { refreshToken?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  if (typeof body.refreshToken !== "string" || body.refreshToken.trim() === "") {
    return c.json(createAuthError("contract_violation", "refreshToken is required."), 400);
  }

  const refreshTokenHash = await hashRefreshToken(body.refreshToken);
  const row = await c.env.DB.prepare(
    `SELECT sessions.id AS session_id, sessions.account_id, sessions.refresh_token_hash,
            sessions.family_id, sessions.generation, sessions.status AS session_status,
            sessions.expires_at AS session_expires_at, accounts.status AS account_status
     FROM auth_sessions sessions
     JOIN accounts ON accounts.id = sessions.account_id
     WHERE sessions.refresh_token_hash = ?`,
  )
    .bind(refreshTokenHash)
    .first<RefreshSessionAccountRow>();

  if (row === null) {
    return c.json(createAuthError("refresh_token_invalid", "Refresh token is invalid."), 401);
  }

  const now = Date.now();
  if (row.session_status === "ROTATED") {
    if (idempotencyKey !== null) {
      const replayRow = await readRefreshReplaySessionRow(c.env.DB, {
        accountId: row.account_id,
        familyId: row.family_id,
        generation: row.generation + 1,
        idempotencyKey,
      });
      if (replayRow !== null) {
        if (replayRow.account_status !== "ACTIVE") {
          return c.json(createAuthError("account_not_active", "Account is not active."), 403);
        }
        if (replayRow.session_status !== "ACTIVE" || replayRow.session_expires_at <= now) {
          return c.json(createAuthError("auth_replay_expired", "Auth replay window is expired."), 401);
        }
        const replayRefreshToken = await createReplaySafeRefreshToken(
          secret,
          "auth_refresh",
          `${row.refresh_token_hash}:${idempotencyKey}:${replayRow.session_id}`,
        );
        return c.json(await createAuthResponseForSession({
          secret,
          accountId: replayRow.account_id,
          sessionId: replayRow.session_id,
          refreshToken: replayRefreshToken,
          refreshTokenExpiresAt: replayRow.session_expires_at,
          now,
        }));
      }
    }
    await c.env.DB.batch([
      c.env.DB.prepare(
        `UPDATE auth_sessions
         SET status = 'REUSED', rotated_at = ?
         WHERE family_id = ? AND status IN ('ACTIVE','ROTATED')`,
      ).bind(now, row.family_id),
    ]);
    return c.json(createAuthError("refresh_token_reused", "Refresh token was already rotated."), 401);
  }

  if (row.session_status !== "ACTIVE" || row.session_expires_at <= now) {
    return c.json(createAuthError("refresh_session_invalid", "Refresh session is invalid or expired."), 401);
  }

  if (row.account_status !== "ACTIVE") {
    return c.json(createAuthError("account_not_active", "Account is not active."), 403);
  }

  const newSessionId = createDomainId("session");
  if (idempotencyKey !== null) {
    const existingIdempotency = await c.env.DB.prepare(
      `SELECT result_ref, created_at
       FROM idempotency_records
       WHERE account_id = ? AND operation = 'auth_refresh' AND idempotency_key = ?`,
    )
      .bind(row.account_id, idempotencyKey)
      .first<IdempotencyRecordRow>();
    if (existingIdempotency !== null) {
      return c.json(createAuthError("idempotency_conflict", "Idempotency-Key was already used for another refresh request."), 409);
    }
  }
  const newRefreshToken = idempotencyKey === null
    ? createRefreshToken()
    : await createReplaySafeRefreshToken(secret, "auth_refresh", `${row.refresh_token_hash}:${idempotencyKey}:${newSessionId}`);
  const newRefreshTokenHash = await hashRefreshToken(newRefreshToken);
  const accessTokenTtlMs = 15 * 60 * 1000;
  const refreshTokenTtlMs = 30 * 24 * 60 * 60 * 1000;
  const accessTokenExpiresAt = now + accessTokenTtlMs;
  const refreshTokenExpiresAt = now + refreshTokenTtlMs;
  const accessToken = await createAccessToken({
    secret,
    accountId: row.account_id,
    sessionId: newSessionId,
    nowMs: now,
    ttlMs: accessTokenTtlMs,
  });

  try {
    await c.env.DB.batch([
      c.env.DB.prepare(
        `UPDATE auth_sessions
         SET status = 'ROTATED', rotated_at = ?
         WHERE id = ? AND status = 'ACTIVE'`,
      ).bind(now, row.session_id),
      c.env.DB.prepare(
        `INSERT INTO auth_sessions (id, account_id, refresh_token_hash, family_id, generation, status, created_at, expires_at, rotated_at)
         VALUES (?, ?, ?, ?, ?, 'ACTIVE', ?, ?, NULL)`,
      ).bind(newSessionId, row.account_id, newRefreshTokenHash, row.family_id, row.generation + 1, now, refreshTokenExpiresAt),
      ...(idempotencyKey === null ? [] : [
        c.env.DB.prepare(
          `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
           VALUES (?, ?, ?, 'auth_refresh', ?, ?)`,
        ).bind(createDomainId("idem"), row.account_id, idempotencyKey, newSessionId, now),
      ]),
    ]);
  } catch (error) {
    if (!isUniqueConstraintError(error) || idempotencyKey === null) {
      throw error;
    }
    const replayRow = await readRefreshReplaySessionRow(c.env.DB, {
      accountId: row.account_id,
      familyId: row.family_id,
      generation: row.generation + 1,
      idempotencyKey,
    });
    if (replayRow === null) {
      throw error;
    }
    if (replayRow.account_status !== "ACTIVE") {
      return c.json(createAuthError("account_not_active", "Account is not active."), 403);
    }
    if (replayRow.session_status !== "ACTIVE" || replayRow.session_expires_at <= now) {
      return c.json(createAuthError("auth_replay_expired", "Auth replay window is expired."), 401);
    }
    const replayRefreshToken = await createReplaySafeRefreshToken(
      secret,
      "auth_refresh",
      `${row.refresh_token_hash}:${idempotencyKey}:${replayRow.session_id}`,
    );
    return c.json(await createAuthResponseForSession({
      secret,
      accountId: replayRow.account_id,
      sessionId: replayRow.session_id,
      refreshToken: replayRefreshToken,
      refreshTokenExpiresAt: replayRow.session_expires_at,
      now,
    }));
  }

  return c.json({
    tokenType: "Bearer",
    account: {
      id: row.account_id,
      status: "ACTIVE",
    },
    accessToken,
    accessTokenExpiresAt: toIso(accessTokenExpiresAt),
    refreshToken: newRefreshToken,
    refreshTokenExpiresAt: toIso(refreshTokenExpiresAt),
  });
});
app.get("/v1/profile", async (c) => {
  const hasForbiddenAccountQuery = c.req.query("accountId") !== undefined || c.req.query("account_id") !== undefined;
  if (hasForbiddenAccountQuery || hasNonEmptyRequestBody(c)) {
    return c.json(profileError("contract_violation", "GET /v1/profile must not include account id query parameters or a request body."), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(profileError(authContext.errorCode, authContext.message), authContext.status);
  }

  const profile = await c.env.DB.prepare(
    `SELECT account_id, display_name, display_name_normalized, display_name_updated_at,
            commander_xp, created_at, updated_at
     FROM player_profiles
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<PlayerProfileRow>();

  if (profile === null) {
    return c.json(profileError("profile_not_found", "Player profile was not found."), 404);
  }

  return c.json({
    accepted: true,
    account: {
      id: authContext.accountId,
    },
    profile: {
      displayName: profile.display_name,
      commanderXp: profile.commander_xp,
      displayNameUpdatedAt: typeof profile.display_name_updated_at === "number" ? toIso(profile.display_name_updated_at) : null,
      nextDisplayNameChangeAvailableAt: nextDisplayNameChangeAt(profile.display_name_updated_at),
    },
    errorCode: null,
    message: null,
  });
});
app.get("/v1/profile/display-name/check", async (c) => {
  const hasForbiddenAccountQuery = c.req.query("accountId") !== undefined || c.req.query("account_id") !== undefined;
  if (hasForbiddenAccountQuery || hasNonEmptyRequestBody(c)) {
    return c.json(profileError("contract_violation", "Display name check must not include account id query parameters or a request body."), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(profileError(authContext.errorCode, authContext.message), authContext.status);
  }

  const parsed = normalizeDisplayNameInput(c.req.query("displayName"));
  if (isAuthFailure(parsed)) {
    return c.json(profileError(parsed.errorCode, parsed.message), parsed.status);
  }

  const owner = await c.env.DB.prepare(
    `SELECT account_id
     FROM player_profiles
     WHERE display_name_normalized = ?`,
  )
    .bind(parsed.normalized)
    .first<DisplayNameOwnerRow>();
  const available = owner === null || owner.account_id === authContext.accountId;

  return c.json({
    accepted: true,
    displayName: parsed.displayName,
    available,
    errorCode: available ? null : "display_name_taken",
    message: available ? null : "Display name is already taken.",
  });
});
app.post("/v1/profile/display-name", async (c) => {
  const hasForbiddenAccountQuery = c.req.query("accountId") !== undefined || c.req.query("account_id") !== undefined;
  if (hasForbiddenAccountQuery) {
    return c.json(profileError("contract_violation", "Display name update must not include account id query parameters."), 400);
  }

  let body: Record<string, unknown>;
  try {
    const parsedBody = await c.req.json();
    body = typeof parsedBody === "object" && parsedBody !== null ? parsedBody as Record<string, unknown> : {};
  } catch {
    body = {};
  }

  if (Object.hasOwn(body, "accountId") || Object.hasOwn(body, "account_id")) {
    return c.json(profileError("contract_violation", "accountId must not be supplied by the client."), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(profileError(authContext.errorCode, authContext.message), authContext.status);
  }

  const parsed = normalizeDisplayNameInput(body.displayName);
  if (isAuthFailure(parsed)) {
    return c.json(profileError(parsed.errorCode, parsed.message), parsed.status);
  }

  const profile = await c.env.DB.prepare(
    `SELECT account_id, display_name, display_name_normalized, display_name_updated_at,
            commander_xp, created_at, updated_at
     FROM player_profiles
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<PlayerProfileRow>();

  if (profile === null) {
    return c.json(profileError("profile_not_found", "Player profile was not found."), 404);
  }

  if (profile.display_name_normalized === parsed.normalized) {
    return c.json({
      accepted: true,
      displayName: profile.display_name ?? parsed.displayName,
      changeStatus: "UNCHANGED",
      displayNameUpdatedAt: typeof profile.display_name_updated_at === "number" ? toIso(profile.display_name_updated_at) : null,
      nextDisplayNameChangeAvailableAt: nextDisplayNameChangeAt(profile.display_name_updated_at),
      errorCode: null,
      message: null,
    });
  }

  const now = Date.now();
  if (displayNameCooldownActive(profile.display_name_updated_at, now)) {
    return c.json({
      accepted: false,
      errorCode: "display_name_change_cooldown",
      message: "Display name can be changed after the cooldown.",
      nextDisplayNameChangeAvailableAt: nextDisplayNameChangeAt(profile.display_name_updated_at),
    }, 409);
  }

  const owner = await c.env.DB.prepare(
    `SELECT account_id
     FROM player_profiles
     WHERE display_name_normalized = ?`,
  )
    .bind(parsed.normalized)
    .first<DisplayNameOwnerRow>();

  if (owner !== null && owner.account_id !== authContext.accountId) {
    return c.json(profileError("display_name_taken", "Display name is already taken."), 409);
  }

  try {
    await c.env.DB.prepare(
      `UPDATE player_profiles
       SET display_name = ?, display_name_normalized = ?, display_name_updated_at = ?, updated_at = ?
       WHERE account_id = ?`,
    )
      .bind(parsed.displayName, parsed.normalized, now, now, authContext.accountId)
      .run();
  } catch {
    return c.json(profileError("display_name_taken", "Display name is already taken."), 409);
  }

  return c.json({
    accepted: true,
    displayName: parsed.displayName,
    changeStatus: "UPDATED",
    displayNameUpdatedAt: toIso(now),
    nextDisplayNameChangeAvailableAt: toIso(now + displayNameCooldownMs),
    errorCode: null,
    message: null,
  });
});
app.get("/v1/client-config", async (c) => {
  c.header("Cache-Control", "no-store");
  return c.json(await createClientAdmissionForRequest(c));
});

app.get("/v1/client-config/authorized", async (c) => {
  c.header("Cache-Control", "no-store");
  const auth = await resolveAuthContext(c);
  if (!("accountId" in auth)) return c.json(auth, auth.status);
  const admission = await createClientAdmissionForRequest(c);
  return c.json(admission, admission.accepted ? 200 : clientAdmissionHttpStatus(admission.errorCode));
});
app.post("/v1/realtime/coop/rooms", async (c) => {
  const idempotencyKey = c.req.header("Idempotency-Key")?.trim();
  if (idempotencyKey === undefined || idempotencyKey === "") {
    return c.json(createRealtimeCoopRejected("idempotency_key_required", "Idempotency-Key is required."), 400);
  }

  const body = await parseRealtimeCoopRoomBody(c);
  if (isAuthFailure(body)) {
    return c.json(createRealtimeCoopRejected(body.errorCode, body.message), body.status);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createRealtimeCoopRejected(authContext.errorCode, authContext.message), authContext.status);
  }

  const rooms = getRealtimeCoopRooms(c.env);
  if (rooms === null) {
    return c.json(realtimeCoopUnavailable(), 503);
  }

  const response = await rooms.getByName(`coop:${body.runId}`).createRoom({
    runId: body.runId,
    hostAccountId: authContext.accountId,
    idempotencyKey,
    requestId: c.req.header("X-Request-Id") ?? createRequestId(),
  });

  return c.json(response, response.accepted ? 200 : 409);
});
app.get("/v1/realtime/coop/rooms/:roomId/connect", async (c) => {
  if (c.req.header("Upgrade")?.toLowerCase() !== "websocket") {
    return c.json(createRealtimeConnectionFailure("realtime_websocket_required", "WebSocket upgrade is required."), 400);
  }

  const url = new URL(c.req.url);
  const runId = url.searchParams.get("runId");
  const roomId = c.req.param("roomId");
  const allowedQuery = url.searchParams.size === 1 && runId !== null;
  if (!allowedQuery || !isSafeRealtimeId(roomId) || runId === null || !isSafeRealtimeId(runId)) {
    return c.json(createRealtimeConnectionFailure("contract_violation", "Only runId query is accepted for realtime coop connections."), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createRealtimeConnectionFailure(authContext.errorCode, authContext.message), authContext.status);
  }

  const rooms = getRealtimeCoopRooms(c.env);
  if (rooms === null) {
    return c.json(createRealtimeConnectionFailure("realtime_coop_unavailable", "Realtime coop room service is not configured."), 503);
  }

  const headers = new Headers(c.req.raw.headers);
  headers.delete("Authorization");
  headers.set("X-Lastrail-Realtime-Account-Id", authContext.accountId);
  headers.set("X-Lastrail-Realtime-Session-Id", authContext.sessionId);
  headers.set("X-Lastrail-Realtime-Room-Id", roomId);
  headers.set("X-Lastrail-Realtime-Run-Id", runId);

  return rooms.getByName(`coop:${runId}`).fetch(new Request(c.req.raw.url, {
    method: "GET",
    headers,
  }));
});
app.get("/v1/story-progress", async (c) => {
  const hasForbiddenAccountQuery = c.req.query("accountId") !== undefined || c.req.query("account_id") !== undefined;
  const contentLength = c.req.header("content-length");
  if (hasForbiddenAccountQuery || (contentLength !== undefined && contentLength !== "0")) {
    return c.json(createStoryProgressRejectedResponse({
      errorCode: "contract_violation",
      message: "GET /v1/story-progress must not include account id query parameters or a request body.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createStoryProgressRejectedResponse({
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const storyProgress = await c.env.DB.prepare(
    `SELECT act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountStoryProgressRow & {
      act_id: string;
      highest_cleared_operation_id: string;
      narrative_version: string | null;
      last_progress_run_id: string | null;
      updated_at: number;
    }>();

  if (storyProgress === null) {
    return c.json(createStoryProgressAcceptedResponse({
      row: null,
      unlockedOperationIds: [],
    }));
  }

  const unlockRows = await c.env.DB.prepare(
    `SELECT unlock_id
     FROM account_unlocks
     WHERE account_id = ? AND unlock_id LIKE 'operation:%'
     ORDER BY unlocked_at, unlock_id`,
  )
    .bind(authContext.accountId)
    .all<AccountUnlockRow>();
  const unlockedOperationIds = unlockRows.results
    .map((row) => row.unlock_id.startsWith("operation:") ? row.unlock_id.slice("operation:".length) : null)
    .filter((value): value is string => value !== null);

  return c.json(createStoryProgressAcceptedResponse({
    row: storyProgress,
    unlockedOperationIds,
  }));
});
app.get("/v1/offline-rewards/preview", async (c) => {
  const requestUrl = new URL(c.req.url);
  const hasForbiddenQuery = offlineRewardForbiddenQueryKeys.some((key) => requestUrl.searchParams.has(key));
  const hasAnyQuery = [...requestUrl.searchParams.keys()].length > 0;
  const contentLength = c.req.header("content-length");
  if (hasForbiddenQuery || hasAnyQuery || (contentLength !== undefined && contentLength !== "0")) {
    return c.json(createOfflineRewardRejectedResponse({
      errorCode: "contract_violation",
      message: "GET /v1/offline-rewards/preview must not include query parameters or a request body.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createOfflineRewardRejectedResponse({
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const storyProgress = await c.env.DB.prepare(
    `SELECT highest_story_progress_id, narrative_version
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<Pick<AccountStoryProgressRow, "highest_story_progress_id" | "narrative_version">>();
  const offlineState = await c.env.DB.prepare(
    `SELECT last_offline_reward_claim_at, last_story_progress_id, last_balance_config_version,
            last_narrative_version, last_previewed_at, revision, updated_at
     FROM account_offline_reward_state
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountOfflineRewardStateRow>();

  const now = Date.now();
  const storyProgressId = storyProgress?.highest_story_progress_id ?? "none";
  const baselineMs = offlineState?.last_offline_reward_claim_at ?? authContext.accountCreatedAt;
  const actualOfflineSeconds = Math.floor((now - baselineMs) / 1000);

  try {
    const offlinePolicy = await readOfflineEconomy(c.env.DB);
    const preview = calculateOfflineRewardPreview({
      config: offlinePolicy.config,
      storyProgressKey: storyProgressId,
      actualOfflineSeconds,
    });
    return c.json({ ...createOfflineRewardAcceptedResponse({
      storyProgressId: preview.calculation.storyProgressKey,
      rewardLines: preview.rewardLines,
      claimableSeconds: preview.calculation.claimableSeconds,
      offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
      minimumOfflineMinutes: preview.calculation.minimumOfflineMinutes,
      maxClaimableHours: offlinePolicy.config.maxClaimableHours,
      storyProgressMultiplierBps: preview.calculation.storyProgressMultiplierBps,
      narrativeVersion: storyProgress?.narrative_version ?? actOneNarrativeVersion,
      serverCalculatedAt: now,
    }), ...offlinePolicy.metadata });
  } catch (error) {
    const errorCode = error instanceof Error && error.message !== "" ? error.message : "offline_reward_policy_missing";
    const status = error instanceof ActivityError ? error.status : errorCode === "contract_violation" ? 400 : 409;
    return c.json(createOfflineRewardRejectedResponse({
      errorCode,
      message: "Offline reward preview is unavailable.",
    }), status);
  }
});

app.post("/v1/offline-rewards/claim", withEconomyRetry(async (c) => {
  const requestUrl = new URL(c.req.url);
  const hasAnyQuery = [...requestUrl.searchParams.keys()].length > 0;
  const contentLength = c.req.header("content-length");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createOfflineRewardClaimRejectedResponse({
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  if (hasAnyQuery || (contentLength !== undefined && contentLength !== "0")) {
    return c.json(createOfflineRewardClaimRejectedResponse({
      errorCode: "contract_violation",
      message: "POST /v1/offline-rewards/claim must not include query parameters or a request body.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createOfflineRewardClaimRejectedResponse({
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const replay = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, reward.created_at AS reward_created_at,
            reward.reward_json AS reward_json
     FROM idempotency_records idem
     LEFT JOIN reward_transactions reward ON reward.id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'offline_reward_claim' AND idem.idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<OfflineRewardReplayRow>();

  if (replay !== null) {
    return c.json(createOfflineRewardClaimDuplicateReplayResponse({
      offlineRewardClaimId: replay.result_ref,
      rewardJson: replay.reward_json,
      serverClaimedAt: replay.reward_created_at ?? replay.created_at,
    }));
  }

  // Fence all entitlement/balance reads, including the elapsed-time baseline.
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence FROM account_mutations WHERE account_id = ?`,
  ).bind(authContext.accountId).first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;

  const storyProgress = await c.env.DB.prepare(
    `SELECT highest_story_progress_id, narrative_version
     FROM account_story_progress
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<Pick<AccountStoryProgressRow, "highest_story_progress_id" | "narrative_version">>();
  const offlineState = await c.env.DB.prepare(
    `SELECT last_offline_reward_claim_at, last_story_progress_id, last_balance_config_version,
            last_narrative_version, last_previewed_at, revision, updated_at
     FROM account_offline_reward_state
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountOfflineRewardStateRow>();

  const now = Date.now();
  const baselineMs = offlineState?.last_offline_reward_claim_at ?? authContext.accountCreatedAt;
  if (baselineMs > now) {
    await writeGameApiOpsErrorLog(c, {
      severity: "error",
      errorCode: "offline_reward_state_conflict",
      messageSummary: "Offline reward state is ahead of server time.",
      accountId: authContext.accountId,
      storyProgressId: storyProgress?.highest_story_progress_id ?? "none",
      idempotencyOperation: "offline_reward_claim",
      idempotencyKey,
      correlation: {
        accountId: authContext.accountId,
        storyProgressId: storyProgress?.highest_story_progress_id ?? "none",
      },
      context: {
        component: "offline_rewards",
        stateAheadOfServerTime: true,
        hasOfflineRewardState: offlineState !== null,
      },
      retryable: false,
    });
    return c.json(createOfflineRewardClaimRejectedResponse({
      storyProgressId: storyProgress?.highest_story_progress_id ?? "none",
      errorCode: "offline_reward_state_conflict",
      message: "Offline reward state is ahead of server time.",
    }), 409);
  }

  const storyProgressId = storyProgress?.highest_story_progress_id ?? "none";
  const narrativeVersion = storyProgress?.narrative_version ?? actOneNarrativeVersion;
  const actualOfflineSeconds = Math.floor((now - baselineMs) / 1000);
  let preview;
  let offlinePolicy: Awaited<ReturnType<typeof readOfflineEconomy>>;
  try {
    offlinePolicy = await readOfflineEconomy(c.env.DB);
    preview = calculateOfflineRewardPreview({
      config: offlinePolicy.config,
      storyProgressKey: storyProgressId,
      actualOfflineSeconds,
    });
  } catch (error) {
    const errorCode = error instanceof Error && error.message !== "" ? error.message : "offline_reward_policy_missing";
    const status = error instanceof ActivityError ? error.status : errorCode === "contract_violation" ? 400 : 409;
    return c.json(createOfflineRewardClaimRejectedResponse({
      storyProgressId,
      errorCode,
      message: "Offline reward claim is unavailable.",
    }), status);
  }

  if (preview.calculation.claimableSeconds === 0 || preview.rewardLines.length === 0) {
    return c.json(createOfflineRewardClaimRejectedResponse({
      storyProgressId: preview.calculation.storyProgressKey,
      claimableSeconds: preview.calculation.claimableSeconds,
      offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
      errorCode: "offline_reward_not_claimable",
      message: "Offline reward is not claimable yet.",
    }), 409);
  }

  const offlineRewardClaimId = createDomainId("offline_claim");
  const mutationId = createDomainId("mutation");
  const economyTransactionId = createDomainId("economy");
  const claimRewardLines = [];
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'OFFLINE_REWARD_CLAIM', ?, ?)`,
    ).bind(mutationId, authContext.accountId, nextSequence, idempotencyKey, now),
    offlinePolicyGuard(c.env.DB, offlinePolicy.metadata),
    c.env.DB.prepare("DELETE FROM growth_write_guard"),
    c.env.DB.prepare(
      `INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'OFFLINE_REWARD_CLAIM', 'OFFLINE_REWARD_CLAIM', ?, ?)`,
    ).bind(economyTransactionId, authContext.accountId, mutationId, offlineRewardClaimId, now),
  ];

  for (const rewardLine of preview.rewardLines) {
    const currency = currencyIdToD1Currency[rewardLine.id as keyof typeof currencyIdToD1Currency];
    const current = await c.env.DB.prepare(
      `SELECT balance
       FROM account_currencies
       WHERE account_id = ? AND currency = ?`,
    )
      .bind(authContext.accountId, currency)
      .first<AccountCurrencyRow>();
    const balanceAfter = (current?.balance ?? 0) + rewardLine.quantity;
    const ledgerId = createDomainId("ledger");
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO account_currencies (account_id, currency, balance, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, currency) DO UPDATE SET balance = excluded.balance, updated_at = excluded.updated_at`,
      ).bind(authContext.accountId, currency, balanceAfter, now),
      c.env.DB.prepare(
        `INSERT INTO currency_ledger (id, account_id, transaction_id, currency, delta, balance_after, created_at)
         VALUES (?, ?, ?, ?, ?, ?, ?)`,
      ).bind(ledgerId, authContext.accountId, economyTransactionId, currency, rewardLine.quantity, balanceAfter, now),
    );
    claimRewardLines.push({
      kind: rewardLine.kind,
      id: rewardLine.id,
      quantity: rewardLine.quantity,
      source: rewardLine.source,
      balanceAfter,
    });
  }

  const calculation = {
    storyProgressId: preview.calculation.storyProgressKey,
    storyProgressMultiplierLabel: `${(preview.calculation.storyProgressMultiplierBps / 10000).toFixed(2)}x`,
    cappedOfflineSeconds: preview.calculation.claimableSeconds,
    capSource: preview.calculation.claimableSeconds >= preview.calculation.offlineStorageCapHours * 3600
      ? "STORY_PROGRESS_STORAGE_CAP"
      : "ACTUAL_OFFLINE_DURATION",
  };
  const responsePayload = {
    accepted: true,
    claimStatus: "CLAIMED",
    offlineRewardClaimId,
    ...offlinePolicy.metadata,
    rewardTransactionId: offlineRewardClaimId,
    economyTransactionId,
    accountMutationId: mutationId,
    storyProgressId: preview.calculation.storyProgressKey,
    claimableSeconds: preview.calculation.claimableSeconds,
    claimableHours: preview.calculation.claimableSeconds / 3600,
    offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
    minimumClaimSeconds: preview.calculation.minimumOfflineMinutes * 60,
    maxClaimableHours: offlinePolicy.config.maxClaimableHours,
    rewardLines: claimRewardLines,
    calculation,
    serverClaimedAt: toIso(now),
    narrativeVersion,
    errorCode: null,
    message: null,
  };
  const rewardJson = JSON.stringify({
    ...offlinePolicy.metadata,
    rewardLines: claimRewardLines,
    calculation,
    storyProgressId: preview.calculation.storyProgressKey,
    claimableSeconds: preview.calculation.claimableSeconds,
    claimableHours: preview.calculation.claimableSeconds / 3600,
    offlineStorageCapHours: preview.calculation.offlineStorageCapHours,
    minimumClaimSeconds: preview.calculation.minimumOfflineMinutes * 60,
    maxClaimableHours: offlinePolicy.config.maxClaimableHours,
    narrativeVersion,
    rewardTransactionId: offlineRewardClaimId,
    economyTransactionId,
    accountMutationId: mutationId,
  });

  statements.push(
    c.env.DB.prepare(
      `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
       VALUES (?, ?, NULL, ?, ?, ?, ?)`,
    ).bind(offlineRewardClaimId, authContext.accountId, mutationId, offlinePolicy.metadata.balanceVersion, rewardJson, now),
    c.env.DB.prepare(
      `INSERT INTO account_offline_reward_state (
         account_id, last_offline_reward_claim_at, last_story_progress_id,
         last_balance_config_version, last_narrative_version, last_previewed_at, revision, updated_at, last_reward_transaction_id
       ) VALUES (?, ?, ?, ?, ?, NULL, 1, ?, ?)
       ON CONFLICT(account_id) DO UPDATE SET
         last_offline_reward_claim_at = excluded.last_offline_reward_claim_at,
         last_story_progress_id = excluded.last_story_progress_id,
         last_balance_config_version = excluded.last_balance_config_version,
         last_narrative_version = excluded.last_narrative_version,
         last_reward_transaction_id = excluded.last_reward_transaction_id,
         revision = account_offline_reward_state.revision + 1,
         updated_at = excluded.updated_at`,
    ).bind(authContext.accountId, now, preview.calculation.storyProgressKey, offlinePolicy.metadata.balanceVersion, narrativeVersion, now, offlineRewardClaimId),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'offline_reward_claim', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, offlineRewardClaimId, now),
  );

  try { await c.env.DB.batch(statements); }
  catch (error) {
    if (/CHECK constraint failed: valid=1/iu.test(String(error))) throw new ActivityError(409, "offline_reward_policy_changed");
    throw error;
  }

  return c.json(responsePayload);
}));

app.post("/v1/login-bonus/claim", withEconomyRetry(async (c) => {
  const requestUrl = new URL(c.req.url);
  const hasAnyQuery = [...requestUrl.searchParams.keys()].length > 0;
  const contentLength = c.req.header("content-length");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createLoginBonusClaimRejectedResponse({
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  if (hasAnyQuery || (contentLength !== undefined && contentLength !== "0") || (await c.req.text()) !== "") {
    return c.json(createLoginBonusClaimRejectedResponse({
      errorCode: "contract_violation",
      message: "POST /v1/login-bonus/claim must not include query parameters or a request body.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createLoginBonusClaimRejectedResponse({
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const replay = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, reward.created_at AS reward_created_at,
            reward.reward_json AS reward_json
     FROM idempotency_records idem
     LEFT JOIN reward_transactions reward ON reward.id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'login_bonus_claim' AND idem.idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<OfflineRewardReplayRow>();

  if (replay !== null) {
    return c.json(createLoginBonusClaimReplayResponse({
      claimStatus: "DUPLICATE_REPLAY",
      loginBonusClaimId: replay.result_ref,
      rewardJson: replay.reward_json,
      serverClaimedAt: replay.reward_created_at ?? replay.created_at,
    }));
  }

  const existingClaim = await c.env.DB.prepare(
    `SELECT reward_transaction_id, claimed_at, reward_json
     FROM account_login_bonus_claims
     WHERE account_id = ? AND bonus_id = ?`,
  )
    .bind(authContext.accountId, launchLoginBonusId)
    .first<LoginBonusClaimRow>();

  if (existingClaim !== null) {
    return c.json(createLoginBonusClaimReplayResponse({
      claimStatus: "ALREADY_CLAIMED",
      loginBonusClaimId: existingClaim.reward_transaction_id,
      rewardJson: existingClaim.reward_json,
      serverClaimedAt: existingClaim.claimed_at,
    }));
  }

  const now = Date.now();
  const loginBonusClaimId = createDomainId("login_bonus_claim");
  const mutationId = createDomainId("mutation");
  const economyTransactionId = createDomainId("economy");
  const itemLedgerId = createDomainId("item_ledger");
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const currentItem = await c.env.DB.prepare(
    `SELECT quantity
     FROM account_items
     WHERE account_id = ? AND item_id = ?`,
  )
    .bind(authContext.accountId, launchLoginBonusRewardLine.id)
    .first<AccountItemRow>();
  const launchBinding = await readEconomyBinding(c.env.DB);
  const launchPlan = await prepareLaunchRewards(c.env.DB, authContext.accountId, launchLoginBonusId, [launchLoginBonusRewardLine], launchBinding !== null);
  if (launchPlan.sources.length === 0) {
    const committed = await c.env.DB.prepare("SELECT reward_transaction_id,claimed_at,reward_json FROM account_login_bonus_claims WHERE account_id=? AND bonus_id=?")
      .bind(authContext.accountId, launchLoginBonusId).first<LoginBonusClaimRow>();
    if (committed) return c.json(createLoginBonusClaimReplayResponse({ claimStatus: "ALREADY_CLAIMED",
      loginBonusClaimId: committed.reward_transaction_id, rewardJson: committed.reward_json, serverClaimedAt: committed.claimed_at }));
    return c.json(createLoginBonusClaimRejectedResponse({errorCode:"launch_source_consumed",message:"Launch source was already consumed."}),409);
  }
  const crystalPlan = launchBinding === null ? null : await planCrystalChange({db:c.env.DB,accountId:authContext.accountId,operationId:createDomainId("crystal_operation"),mutationId,operation:"LAUNCH_LOGIN_BONUS",now,
    ...(launchPlan.conversionPolicyVersion ? { expectedPolicyVersion: launchPlan.conversionPolicyVersion } : {}),
    grants:launchPlan.lines.filter(line => line.kind === "CRYSTAL").map(line => ({quantity:line.quantity,sourceType:"LAUNCH_ENTITLEMENT" as const,sourceRef:line.launchSourceKey!,orderId:null}))});
  const balanceAfter = (currentItem?.quantity ?? 0) + launchLoginBonusRewardLine.quantity;
  const rewardLines = crystalPlan === null ? [{ ...launchLoginBonusRewardLine, balanceAfter }] : launchPlan.lines;
  const rewardJson = JSON.stringify({
    bonusId: launchLoginBonusId,
    rewardLines,
    rewardTransactionId: loginBonusClaimId,
    economyTransactionId,
    accountMutationId: mutationId,
  });

  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'LOGIN_BONUS_CLAIM', ?, ?)`,
    ).bind(mutationId, authContext.accountId, nextSequence, idempotencyKey, now),
    c.env.DB.prepare(
      `INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'LOGIN_BONUS_CLAIM', 'LOGIN_BONUS_CLAIM', ?, ?)`,
    ).bind(economyTransactionId, authContext.accountId, mutationId, loginBonusClaimId, now),
    ...(crystalPlan === null ? [c.env.DB.prepare(
      `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
       VALUES (?, ?, ?, ?)
       ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at`,
    ).bind(authContext.accountId, launchLoginBonusRewardLine.id, balanceAfter, now),
    c.env.DB.prepare(
      `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, ?, ?, ?, 'LAUNCH_LOGIN_BONUS', 'LOGIN_BONUS_CLAIM', ?, ?)`,
    ).bind(
      itemLedgerId,
      authContext.accountId,
      mutationId,
      launchLoginBonusRewardLine.id,
      launchLoginBonusRewardLine.quantity,
      balanceAfter,
      loginBonusClaimId,
      now,
    )] : crystalPlan.statements),
    ...launchEntitlementStatements(c.env.DB, authContext.accountId, launchPlan.sources, loginBonusClaimId, now),
    c.env.DB.prepare(
      `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
       VALUES (?, ?, NULL, ?, NULL, ?, ?)`,
    ).bind(loginBonusClaimId, authContext.accountId, mutationId, rewardJson, now),
    c.env.DB.prepare(
      `INSERT INTO account_login_bonus_claims (
         account_id, bonus_id, reward_transaction_id, claimed_at, idempotency_key, reward_json
       ) VALUES (?, ?, ?, ?, ?, ?)`,
    ).bind(authContext.accountId, launchLoginBonusId, loginBonusClaimId, now, idempotencyKey, rewardJson),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'login_bonus_claim', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, loginBonusClaimId, now),
  ]);

  return c.json({
    accepted: true,
    claimStatus: "CLAIMED",
    bonusId: launchLoginBonusId,
    loginBonusClaimId,
    rewardTransactionId: loginBonusClaimId,
    economyTransactionId,
    accountMutationId: mutationId,
    rewardLines,
    serverClaimedAt: toIso(now),
    errorCode: null,
    message: null,
  });
}));

app.get("/v1/support/tickets/receipt", async (c) => {
  if (Object.keys(c.req.query()).length > 0 || hasNonEmptyRequestBody(c)) {
    return c.json(createSupportTicketRejectedResponse("contract_violation", "Support ticket receipt lookup accepts only headers."), 400);
  }
  const idempotencyKey = parseOptionalIdempotencyKey(c);
  if (isAuthFailure(idempotencyKey)) {
    return c.json(createSupportTicketRejectedResponse(idempotencyKey.errorCode, idempotencyKey.message), idempotencyKey.status);
  }
  if (idempotencyKey === null) {
    return c.json(createSupportTicketRejectedResponse("contract_violation", "Idempotency-Key header is required."), 400);
  }
  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createSupportTicketRejectedResponse(authContext.errorCode, authContext.message), authContext.status);
  }

  const row = await c.env.DB.prepare(
    `SELECT id, account_id, category, status, body_text, body_preview, body_length,
            app_version, app_version_code, client_composed_at, client_submitted_at,
            server_received_at, idempotency_key, request_hash, created_at, updated_at
     FROM support_tickets
     WHERE account_id = ? AND idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<SupportTicketRow>();
  if (row === null) return c.json(supportTicketReceiptNotFoundResponse(), 404);

  return c.json(supportTicketReceiptFoundResponse(row));
});

app.post("/v1/support/tickets", async (c) => {
  const idempotencyKey = c.req.header("Idempotency-Key")?.trim();
  if (idempotencyKey === undefined || idempotencyKey.length === 0) {
    return c.json(createSupportTicketRejectedResponse("contract_violation", "Idempotency-Key header is required."), 400);
  }
  if (idempotencyKey.length > 128) {
    return c.json(createSupportTicketRejectedResponse("contract_violation", "Idempotency-Key header is too long."), 400);
  }

  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) {
    return c.json(createSupportTicketRejectedResponse(body.errorCode, body.message), body.status);
  }

  const category = parseSupportTicketCategory(body.category);
  const bodyText = normalizeOptionalText(body.body, 2000);
  const appVersion = normalizeOptionalText(body.appVersion, 64);
  const appVersionCode = normalizeOptionalText(body.appVersionCode, 19);
  const clientComposedAt = parseOptionalIsoMs(body.clientComposedAt);
  const clientSubmittedAt = parseOptionalIsoMs(body.clientSubmittedAt);
  if (
    category === null ||
    bodyText === null ||
    bodyText === "INVALID" ||
    bodyText.length < 10 ||
    appVersion === "INVALID" ||
    appVersionCode === "INVALID" ||
    (typeof appVersionCode === "string" && !/^\d{1,19}$/u.test(appVersionCode)) ||
    clientComposedAt === "INVALID" ||
    clientSubmittedAt === "INVALID"
  ) {
    return c.json(createSupportTicketRejectedResponse(
      "support_ticket_invalid",
      "Support ticket category, body, or client metadata is invalid.",
    ), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createSupportTicketRejectedResponse(authContext.errorCode, authContext.message), authContext.status);
  }

  const requestHash = await buildSupportTicketRequestHash({
    category,
    bodyText,
    appVersion,
    appVersionCode,
    clientComposedAt,
    clientSubmittedAt,
  });
  const existing = await c.env.DB.prepare(
    `SELECT id, account_id, category, status, body_text, body_preview, body_length,
            app_version, app_version_code, client_composed_at, client_submitted_at,
            server_received_at, idempotency_key, request_hash, created_at, updated_at
     FROM support_tickets
     WHERE account_id = ? AND idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<SupportTicketRow>();
  if (existing !== null) {
    if (existing.request_hash !== requestHash) {
      return c.json(createSupportTicketRejectedResponse(
        "idempotency_conflict",
        "Idempotency-Key was already used for a different support ticket request.",
      ), 409);
    }
    return c.json(supportTicketResponse(existing, "DUPLICATE_REPLAY"));
  }

  const now = Date.now();
  const acceptedTicketCount = await c.env.DB.prepare(
    `SELECT COUNT(*) AS accepted_count
     FROM support_tickets
     WHERE account_id = ? AND created_at >= ?`,
  )
    .bind(authContext.accountId, now - supportTicketRateWindowMs)
    .first<{ accepted_count: number }>();
  if ((acceptedTicketCount?.accepted_count ?? 0) >= supportTicketRateLimitPerWindow) {
    const retryAfterSeconds = Math.ceil(supportTicketRateWindowMs / 1000);
    c.header("Retry-After", String(retryAfterSeconds));
    return c.json(createSupportTicketRejectedResponse(
      "support_ticket_rate_limited",
      "Too many support tickets were received for this account. Please retry later.",
      retryAfterSeconds,
    ), 429);
  }

  const ticketId = createDomainId("support_ticket");
  const row: SupportTicketRow = {
    id: ticketId,
    account_id: authContext.accountId,
    category,
    status: "OPEN",
    body_text: bodyText,
    body_preview: buildSupportTicketPreview(bodyText),
    body_length: bodyText.length,
    app_version: appVersion,
    app_version_code: appVersionCode,
    client_composed_at: clientComposedAt,
    client_submitted_at: clientSubmittedAt,
    server_received_at: now,
    idempotency_key: idempotencyKey,
    request_hash: requestHash,
    created_at: now,
    updated_at: now,
  };
  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO support_tickets (
         id, account_id, category, status, body_text, body_preview, body_length,
         app_version, app_version_code, client_composed_at, client_submitted_at,
         server_received_at, idempotency_key, request_hash, created_at, updated_at
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    ).bind(
      row.id,
      row.account_id,
      row.category,
      row.status,
      row.body_text,
      row.body_preview,
      row.body_length,
      row.app_version,
      row.app_version_code,
      row.client_composed_at,
      row.client_submitted_at,
      row.server_received_at,
      row.idempotency_key,
      row.request_hash,
      row.created_at,
      row.updated_at,
    ),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'support_ticket_create', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, ticketId, now),
  ]);

  return c.json(supportTicketResponse(row, "RECEIVED"));
});

app.get("/v1/client-errors/receipt", async (c) => {
  if (Object.keys(c.req.query()).length > 0 || hasNonEmptyRequestBody(c)) {
    return c.json(createClientErrorIngestRejectedResponse("contract_violation", "Client error ingest receipt lookup accepts only headers."), 400);
  }
  const idempotencyKey = parseOptionalIdempotencyKey(c);
  if (isAuthFailure(idempotencyKey)) {
    return c.json(createClientErrorIngestRejectedResponse(idempotencyKey.errorCode, idempotencyKey.message), idempotencyKey.status);
  }
  if (idempotencyKey === null) {
    return c.json(createClientErrorIngestRejectedResponse("contract_violation", "Idempotency-Key header is required."), 400);
  }
  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createClientErrorIngestRejectedResponse(authContext.errorCode, authContext.message), authContext.status);
  }

  const replay = await c.env.DB.prepare(
    `SELECT result_ref, created_at
     FROM idempotency_records
     WHERE account_id = ? AND operation = 'client_error_ingest' AND idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<IdempotencyRecordRow>();
  if (replay === null) return c.json(createClientErrorIngestReceiptNotFoundResponse(), 404);

  const idempotencyKeyHash = `sha256:${await sha256Hex(idempotencyKey)}`;
  const count = await c.env.DB.prepare(
    `SELECT COUNT(*) AS accepted_count
     FROM ops_error_logs
     WHERE account_id = ? AND idempotency_operation = 'client_error_ingest' AND idempotency_key_hash = ?`,
  )
    .bind(authContext.accountId, idempotencyKeyHash)
    .first<{ accepted_count: number }>();

  return c.json({
    accepted: true,
    receiptStatus: "FOUND",
    ingestId: replay.result_ref,
    acceptedEventCount: count?.accepted_count ?? 0,
    serverReceivedAt: toIso(replay.created_at),
    errorCode: null,
    message: null,
  });
});

app.post("/v1/client-errors", async (c) => {
  const idempotencyKey = c.req.header("Idempotency-Key")?.trim();
  if (idempotencyKey === undefined || idempotencyKey.length === 0) {
    return c.json(createClientErrorIngestRejectedResponse("contract_violation", "Idempotency-Key header is required."), 400);
  }
  if (idempotencyKey.length > 128) {
    return c.json(createClientErrorIngestRejectedResponse("contract_violation", "Idempotency-Key header is too long."), 400);
  }

  const body = await parseJsonObjectBody(c);
  if (isAuthFailure(body)) {
    return c.json(createClientErrorIngestRejectedResponse(body.errorCode, body.message), body.status);
  }
  if (Object.keys(body).some((key) => key !== "events") || !Array.isArray(body.events) || body.events.length < 1 || body.events.length > 25) {
    return c.json(createClientErrorIngestRejectedResponse(
      "contract_violation",
      "Client error ingest accepts only a bounded events array.",
    ), 400);
  }

  const events = body.events.map(parseClientErrorEvent);
  if (events.some((event) => event === null)) {
    return c.json(createClientErrorIngestRejectedResponse(
      "contract_violation",
      "Client error events must use fixed safe diagnostic fields only.",
    ), 400);
  }
  const safeEvents = events as ClientErrorEvent[];

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createClientErrorIngestRejectedResponse(authContext.errorCode, authContext.message), authContext.status);
  }

  const requestHash = await buildClientErrorIngestHash(safeEvents);
  const ingestId = `client_error_ingest_${requestHash.slice("sha256:".length, "sha256:".length + 32)}`;
  const replay = await c.env.DB.prepare(
    `SELECT result_ref, created_at
     FROM idempotency_records
     WHERE account_id = ? AND operation = 'client_error_ingest' AND idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<IdempotencyRecordRow>();
  if (replay !== null) {
    if (replay.result_ref !== ingestId) {
      return c.json(createClientErrorIngestRejectedResponse(
        "idempotency_conflict",
        "Idempotency-Key was already used for a different client error ingest request.",
      ), 409);
    }
    return c.json({
      accepted: true,
      ingestStatus: "DUPLICATE_REPLAY",
      ingestId,
      acceptedEventCount: 0,
      droppedEventCount: safeEvents.length,
      serverReceivedAt: toIso(replay.created_at),
      errorCode: "duplicate_replay",
      message: "Client error ingest was already received.",
    });
  }

  const now = Date.now();
  const acceptedIngestCount = await c.env.DB.prepare(
    `SELECT COUNT(*) AS accepted_count
     FROM idempotency_records
     WHERE account_id = ? AND operation = 'client_error_ingest' AND created_at >= ?`,
  )
    .bind(authContext.accountId, now - clientErrorIngestRateWindowMs)
    .first<{ accepted_count: number }>();
  if ((acceptedIngestCount?.accepted_count ?? 0) >= clientErrorIngestRateLimitPerWindow) {
    const retryAfterSeconds = Math.ceil(clientErrorIngestRateWindowMs / 1000);
    c.header("Retry-After", String(retryAfterSeconds));
    return c.json(createClientErrorIngestRejectedResponse(
      "client_error_ingest_rate_limited",
      "Too many client error diagnostic batches were received for this account. Please retry later.",
      { droppedEventCount: safeEvents.length, retryAfterSeconds },
    ), 429);
  }

  const serverVersion = (c.env as Env & { CF_VERSION_METADATA?: { id: string } }).CF_VERSION_METADATA?.id ?? null;
  const statements = [];
  for (const event of safeEvents) {
    const envelope = await buildOpsErrorLogInsertEnvelope({
      id: createDomainId("ops_error"),
      occurredAtMs: event.occurredAt,
      environment: getOpsLogEnvironment(c.env),
      service: "game_api",
      serverVersion,
      severity: "warning",
      errorCode: clientErrorCode(event.kind),
      messageSummary: clientErrorSummary(event.kind),
      source: "game_api",
      requestId: c.req.header("X-Request-Id") ?? null,
      accountId: authContext.accountId,
      idempotencyOperation: "client_error_ingest",
      idempotencyKey,
      correlation: {
        accountId: authContext.accountId,
        ingestId,
      },
      context: {
        clientDiagnosticCategory: "frontend",
        clientDiagnosticKind: event.kind,
        occurrenceCount: event.occurrenceCount,
        appVersion: event.appVersion,
        appVersionCode: event.appVersionCode,
        scope: event.scope,
      },
      retryable: false,
      createdAtMs: now,
    });
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO ops_error_logs (
           id, occurred_at, environment, service, severity, error_code, message_summary,
           status, source, request_id, account_id, run_id, reward_preview_id, reward_claim_id,
           offline_reward_claim_id, gacha_batch_id, gacha_banner_id, story_progress_id,
           integrity_event_id, support_ticket_id, idempotency_operation, idempotency_key_hash,
           correlation_json, sanitized_context_json, retryable, masked, payload_ref, trace_ref,
           resolved_at, resolved_by, resolution_note, created_at
         ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, ?, ?, ?, ?, ?, ?, NULL, NULL, NULL, NULL, NULL, ?)`,
      ).bind(
        envelope.id,
        envelope.occurredAt,
        envelope.environment,
        envelope.service,
        envelope.severity,
        envelope.errorCode,
        envelope.messageSummary,
        envelope.status,
        envelope.source,
        envelope.requestId,
        envelope.accountId,
        envelope.idempotencyOperation,
        envelope.idempotencyKeyHash,
        envelope.correlationJson,
        envelope.sanitizedContextJson,
        envelope.retryable ? 1 : 0,
        envelope.masked ? 1 : 0,
        envelope.createdAt,
      ),
    );
  }
  statements.push(
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'client_error_ingest', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, ingestId, now),
  );

  try {
    await c.env.DB.batch(statements);
  } catch {
    return c.json(createClientErrorIngestRejectedResponse(
      "client_error_ingest_conflict",
      "Client error diagnostics could not be recorded safely.",
    ), 409);
  }

  return c.json({
    accepted: true,
    ingestStatus: "RECEIVED",
    ingestId,
    acceptedEventCount: safeEvents.length,
    droppedEventCount: 0,
    serverReceivedAt: toIso(now),
    errorCode: null,
    message: null,
  });
});

app.get("/v1/balance-config/:version", async (c) => {
  const rawVersion = c.req.param("version");
  const version = parsePositiveInteger(rawVersion);
  if (version === null) {
    return c.json({ error: "INVALID_BALANCE_CONFIG_VERSION", version: rawVersion }, 400);
  }

  const row = await c.env.DB.prepare(
    `SELECT version, schema_version, required_content_version, checksum, config_json, published_at
     FROM balance_config_versions
     WHERE version = ?`,
  )
    .bind(version)
    .first<BalanceConfigRow>();

  if (row === null) {
    return c.json({ error: "BALANCE_CONFIG_NOT_PUBLISHED", version }, 404);
  }

  let payload: unknown;
  try {
    payload = JSON.parse(row.config_json) as unknown;
  } catch {
    return c.json({ error: "BALANCE_CONFIG_PAYLOAD_INVALID", version }, 500);
  }

  return c.json({
    version: row.version,
    schemaVersion: row.schema_version,
    requiredContentVersion: row.required_content_version,
    checksum: row.checksum,
    publishedAt: row.published_at,
    payload,
  });
});

app.post("/v1/gacha/banners/:bannerId/pulls", async (c) => {
  const bannerId = c.req.param("bannerId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount: null,
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  let body: { bannerId?: unknown; pullCount?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  const pullCount = typeof body.pullCount === "number" && Number.isSafeInteger(body.pullCount)
    ? body.pullCount
    : null;

  if (body.bannerId !== undefined) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "contract_violation",
      message: "Request body must not include bannerId.",
    }), 400);
  }

  if (pullCount !== 1 && pullCount !== 10) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "invalid_pull_count",
      message: "pullCount must be 1 or 10.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  if (bannerId.startsWith("banner_dev_gacha_verify_")) {
    const scopedBanner = await c.env.DB.prepare("SELECT pool_snapshot_json FROM gacha_banners WHERE id = ?")
      .bind(bannerId).first<{ pool_snapshot_json: string }>();
    if (!scopedBanner || !isDevGachaScopeAllowed({ environment: c.env.ENVIRONMENT, accountId: authContext.accountId, nowMs: Date.now(), snapshot: scopedBanner.pool_snapshot_json })) {
      return c.json(createGachaRejectedResponse({ bannerId, pullCount, errorCode: "gacha_banner_unavailable", message: "Gacha banner is unavailable." }), 403);
    }
  }

  const replayBatch = await c.env.DB.prepare(
    `SELECT batch.id, batch.banner_id, banner.balance_config_version, batch.pull_count, batch.cost_item_id, batch.cost_quantity,
            batch.post_pity_json, batch.result_summary_json, batch.created_at
     FROM idempotency_records idem
     JOIN gacha_pull_batches batch ON batch.id = idem.result_ref
     JOIN gacha_banners banner ON banner.id = batch.banner_id
     WHERE idem.account_id = ? AND idem.operation = 'gacha_pull' AND idem.idempotency_key = ? AND batch.banner_id = ?`,
  )
    .bind(authContext.accountId, idempotencyKey, bannerId)
    .first<GachaReplayBatchRow>();

  if (replayBatch !== null) {
    if (replayBatch.pull_count !== pullCount) return c.json(createGachaRejectedResponse({ bannerId, pullCount,
      errorCode: "idempotency_conflict", message: "The original pull count must be retained when retrying." }), 409);
    let replayBalanceVersion = String(replayBatch.balance_config_version);
    try {
      const saved = JSON.parse(replayBatch.result_summary_json) as { balanceVersion?: unknown };
      if (typeof saved?.balanceVersion === "string") replayBalanceVersion = saved.balanceVersion;
    } catch { /* Legacy receipts did not pin a version. */ }
    const replayResults = await c.env.DB.prepare(
      `SELECT r.pull_sequence, r.result_rarity, r.result_type, r.result_id, r.is_pickup,
              r.guarantee_type, r.was_duplicate, r.duplicate_conversion_json, l.delta AS reward_quantity
       FROM gacha_pull_results r LEFT JOIN item_ledger l ON l.id=r.item_ledger_id AND l.account_id=r.account_id
       WHERE r.batch_id = ? AND r.account_id = ? ORDER BY r.pull_sequence`,
    )
      .bind(replayBatch.id, authContext.accountId)
      .all<GachaReplayResultRow>();

    return c.json(createGachaDuplicateReplayResponse({
      batchId: replayBatch.id,
      bannerId: replayBatch.banner_id,
      balanceVersion: replayBalanceVersion,
      pullCount: replayBatch.pull_count as 1 | 10,
      results: getGachaReplayResponseResults({
        resultSummaryJson: replayBatch.result_summary_json,
        resultRows: replayResults.results,
        expectedPullCount: replayBatch.pull_count,
      }),
      postPityState: JSON.parse(replayBatch.post_pity_json) as GachaPityState,
      createdAt: replayBatch.created_at,
    }));
  }

  const row = await c.env.DB.prepare(
    `SELECT id, banner_type, status, balance_config_version, pickup_operator_id, starts_at, ends_at, pity_config_json, pool_snapshot_json
     FROM gacha_banners
     WHERE id = ?`,
  )
    .bind(bannerId)
    .first<GachaBannerRow>();

  if (row === null) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "banner_not_found",
      message: "Gacha banner was not found.",
    }), 404);
  }

  if (!isActiveGachaBanner(row, Date.now())) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "banner_inactive",
      message: "Gacha banner is not active.",
    }), 409);
  }

  const ticketRow = await c.env.DB.prepare(
    `SELECT quantity
     FROM account_items
     WHERE account_id = ? AND item_id = 'cur_command_ticket'`,
  )
    .bind(authContext.accountId)
    .first<AccountItemRow>();
  const currentTickets = ticketRow?.quantity ?? 0;
  if (currentTickets < pullCount) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "insufficient_command_ticket",
      message: "Not enough command tickets.",
    }), 409);
  }

  const poolResults = parseGachaPoolResults(row.pool_snapshot_json);
  if (poolResults.length === 0) {
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "gacha_pool_unavailable",
      message: "Gacha pool is unavailable.",
    }), 503);
  }

  const pityRow = await c.env.DB.prepare(
    `SELECT total_pull_count, elite_pity_count, pickup_elite_pity_count, rare_or_higher_pity_count, revision
     FROM player_gacha_pity
     WHERE account_id = ? AND banner_id = ?`,
  )
    .bind(authContext.accountId, bannerId)
    .first<GachaPityRow>();
  const prePityState = getInitialPityState(pityRow);
  const pityConfig = parseGachaPityConfig(row.pity_config_json);
  let nextPityState = prePityState;
  const now = Date.now();
  const batchId = createDomainId("gacha_batch");
  const mutationId = createDomainId("mutation");
  const costLedgerId = createDomainId("item_ledger");
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'GACHA_PULL', ?, ?)`,
    ).bind(mutationId, authContext.accountId, nextSequence, idempotencyKey, now),
    c.env.DB.prepare(
      `UPDATE account_items
       SET quantity = ?, updated_at = ?
       WHERE account_id = ? AND item_id = 'cur_command_ticket'`,
    ).bind(currentTickets - pullCount, now, authContext.accountId),
    c.env.DB.prepare(
      `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'cur_command_ticket', ?, ?, 'GACHA_PULL_COST', 'GACHA_PULL', ?, ?)`,
    ).bind(costLedgerId, authContext.accountId, mutationId, -pullCount, currentTickets - pullCount, batchId, now),
  ];
  const committedResults: GachaCommittedResult[] = [];
  const ownedOperatorIds = new Set<string>();
  const existingOperators = await c.env.DB.prepare(
    `SELECT operator_id
     FROM account_operators
     WHERE account_id = ?`,
  )
    .bind(authContext.accountId)
    .all<ExistingOperatorRow>();
  for (const operator of existingOperators.results) {
    ownedOperatorIds.add(operator.operator_id);
  }
  const itemBalanceCache = new Map<string, number>();

  const getCachedItemBalance = async (itemId: string): Promise<number> => {
    const cachedBalance = itemBalanceCache.get(itemId);
    if (cachedBalance !== undefined) return cachedBalance;
    const row = await c.env.DB.prepare(
      `SELECT quantity
       FROM account_items
       WHERE account_id = ? AND item_id = ?`,
    )
      .bind(authContext.accountId, itemId)
      .first<AccountItemRow>();
    const balance = row?.quantity ?? 0;
    itemBalanceCache.set(itemId, balance);
    return balance;
  };

  for (let index = 0; index < pullCount; index += 1) {
    const guaranteeType = getNextGachaGuaranteeType({
      banner: row,
      pityState: nextPityState,
      pityConfig,
    });
    const poolResult = selectGachaPoolResult({
      poolResults,
      guaranteeType,
      banner: row,
    });
    if (poolResult === null) {
      return c.json(createGachaRejectedResponse({
        bannerId,
        pullCount,
        errorCode: "gacha_pool_unavailable",
        message: "Gacha pool is unavailable.",
      }), 503);
    }
    const isOperator = poolResult.resultType === "ELITE_OPERATOR" || poolResult.resultType === "RARE_OPERATOR";
    const isDuplicate = isOperator && ownedOperatorIds.has(poolResult.resultId);
    const isPickup = Boolean(poolResult.isPickup) || poolResult.resultId === row.pickup_operator_id;
    const resultId = createDomainId("gacha_result");
    let itemLedgerId: string | null = null;
    let progressionLedgerId: string | null = null;
    let duplicateConversion: Record<string, unknown> | null = null;
    let reward: Record<string, unknown>;

    if (isOperator && isDuplicate) {
      const conversionQuantity = getDuplicateConversionQuantity(poolResult.resultType);
      const shardItemId = getOperatorShardItemId(poolResult.resultId);
      const shardBalanceAfter = await getCachedItemBalance(shardItemId) + conversionQuantity;
      itemBalanceCache.set(shardItemId, shardBalanceAfter);
      itemLedgerId = createDomainId("item_ledger");
      duplicateConversion = {
        sourceResultType: poolResult.resultType,
        sourceResultId: poolResult.resultId,
        itemId: "cur_operator_token",
        operatorId: poolResult.resultId,
        quantity: conversionQuantity,
      };
      reward = {
        kind: "ITEM",
        id: "cur_operator_token",
        operatorId: poolResult.resultId,
        quantity: conversionQuantity,
      };
      statements.push(
        c.env.DB.prepare(
          `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
           VALUES (?, ?, ?, ?)
           ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at`,
        ).bind(authContext.accountId, shardItemId, shardBalanceAfter, now),
        c.env.DB.prepare(
          `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
           VALUES (?, ?, ?, ?, ?, ?, 'GACHA_DUPLICATE_CONVERSION', 'GACHA_PULL', ?, ?)`,
        ).bind(itemLedgerId, authContext.accountId, mutationId, shardItemId, conversionQuantity, shardBalanceAfter, batchId, now),
      );
    } else if (isOperator) {
      progressionLedgerId = createDomainId("progression");
      reward = {
        kind: "OPERATOR",
        id: poolResult.resultId,
        quantity: 1,
      };
      statements.push(
        c.env.DB.prepare(
          `INSERT INTO account_operators (account_id, operator_id, skill_training, promotion, acquired_at, revision, last_mutation_id)
           VALUES (?, ?, 1, 0, ?, 1, ?)`,
        ).bind(authContext.accountId, poolResult.resultId, now, mutationId),
        c.env.DB.prepare(
          `INSERT INTO progression_ledger (id, account_id, mutation_id, progression_type, target_id, delta_json, created_at)
           VALUES (?, ?, ?, 'OPERATOR_ACQUIRED', ?, ?, ?)`,
        ).bind(progressionLedgerId, authContext.accountId, mutationId, poolResult.resultId, JSON.stringify({ acquired: true }), now),
      );
      ownedOperatorIds.add(poolResult.resultId);
    } else {
      const quantity = poolResult.quantity ?? 1;
      const itemBalanceAfter = await getCachedItemBalance(poolResult.resultId) + quantity;
      itemBalanceCache.set(poolResult.resultId, itemBalanceAfter);
      itemLedgerId = createDomainId("item_ledger");
      reward = {
        kind: "ITEM",
        id: poolResult.resultId,
        quantity,
      };
      statements.push(
        c.env.DB.prepare(
          `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
           VALUES (?, ?, ?, ?)
           ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at`,
        ).bind(authContext.accountId, poolResult.resultId, itemBalanceAfter, now),
        c.env.DB.prepare(
          `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
           VALUES (?, ?, ?, ?, ?, ?, 'GACHA_ITEM_GRANT', 'GACHA_PULL', ?, ?)`,
        ).bind(itemLedgerId, authContext.accountId, mutationId, poolResult.resultId, quantity, itemBalanceAfter, batchId, now),
      );
    }

    const committedResult: GachaCommittedResult = {
      id: resultId,
      pullSequence: index + 1,
      resultRarity: poolResult.resultRarity,
      resultType: poolResult.resultType,
      resultId: poolResult.resultId,
      reward,
      isPickup,
      guaranteeType,
      wasDuplicate: isDuplicate,
      duplicateConversion,
      operatorId: isOperator ? poolResult.resultId : null,
      itemLedgerId,
      progressionLedgerId,
    };
    nextPityState = applyGachaPityResult(nextPityState, committedResult);
    committedResults.push(committedResult);
  }

  const postPityState = finalizeGachaPityState(nextPityState);
  const responseResults = committedResults.map(toGachaResponseResult);
  statements.push(
    c.env.DB.prepare(
      `INSERT INTO gacha_pull_batches (
         id, account_id, banner_id, mutation_id, idempotency_key, pull_count, cost_item_id, cost_quantity,
         cost_ledger_id, pre_pity_json, post_pity_json, rng_seed_hash, result_summary_json, created_at
       ) VALUES (?, ?, ?, ?, ?, ?, 'cur_command_ticket', ?, ?, ?, ?, ?, ?, ?)`,
    ).bind(
      batchId,
      authContext.accountId,
      bannerId,
      mutationId,
      idempotencyKey,
      pullCount,
      pullCount,
      costLedgerId,
      JSON.stringify(prePityState),
      JSON.stringify(postPityState),
      `rng_hash_${createRequestId().slice(4)}`,
      JSON.stringify({
        responseResults,
        balanceVersion: String(row.balance_config_version),
        results: committedResults.map((result) => ({
          resultRarity: result.resultRarity,
          resultType: result.resultType,
          resultId: result.resultId,
          wasDuplicate: result.wasDuplicate,
        })),
      }),
      now,
    ),
  );
  for (const result of committedResults) {
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO gacha_pull_results (
           id, batch_id, account_id, banner_id, pull_sequence, result_rarity, result_type, result_id, is_pickup,
           guarantee_type, was_duplicate, duplicate_conversion_json, operator_id, item_ledger_id, progression_ledger_id, created_at
         ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      ).bind(
        result.id,
        batchId,
        authContext.accountId,
        bannerId,
        result.pullSequence,
        result.resultRarity,
        result.resultType,
        result.resultId,
        result.isPickup ? 1 : 0,
        result.guaranteeType,
        result.wasDuplicate ? 1 : 0,
        result.duplicateConversion === null ? null : JSON.stringify(result.duplicateConversion),
        result.operatorId,
        result.itemLedgerId,
        result.progressionLedgerId,
        now,
      ),
    );
  }
  statements.push(
    c.env.DB.prepare(
      `INSERT INTO player_gacha_pity (
         account_id, banner_id, total_pull_count, elite_pity_count, pickup_elite_pity_count,
         rare_or_higher_pity_count, revision, last_batch_id, updated_at
       ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
       ON CONFLICT(account_id, banner_id) DO UPDATE SET
         total_pull_count = excluded.total_pull_count,
         elite_pity_count = excluded.elite_pity_count,
         pickup_elite_pity_count = excluded.pickup_elite_pity_count,
         rare_or_higher_pity_count = excluded.rare_or_higher_pity_count,
         revision = excluded.revision,
         last_batch_id = excluded.last_batch_id,
         updated_at = excluded.updated_at`,
    ).bind(
      authContext.accountId,
      bannerId,
      postPityState.totalPullCount,
      postPityState.elitePityCount,
      postPityState.pickupElitePityCount,
      postPityState.rareOrHigherPityCount,
      postPityState.revision,
      batchId,
      now,
    ),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'gacha_pull', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, batchId, now),
  );

  try {
    await c.env.DB.batch(statements);
  } catch {
    await writeGameApiOpsErrorLog(c, {
      severity: "error",
      errorCode: "gacha_state_conflict",
      messageSummary: "Gacha pull commit transaction failed.",
      accountId: authContext.accountId,
      gachaBannerId: bannerId,
      idempotencyOperation: "gacha_pull",
      idempotencyKey,
      correlation: {
        bannerId,
        batchId,
        pullCount,
      },
      context: {
        phase: "gacha_pull_commit",
        responseShape: "gacha_rejected",
      },
      retryable: true,
    });
    return c.json(createGachaRejectedResponse({
      bannerId,
      pullCount,
      errorCode: "gacha_state_conflict",
      message: "Gacha pull could not be committed safely.",
    }), 409);
  }

  return c.json(createGachaAcceptedResponse({
    batchId,
    bannerId,
    balanceVersion: String(row.balance_config_version),
    pullCount,
    results: responseResults,
    postPityState,
    createdAt: now,
  }));
});

app.post("/v1/runs/start", async (c) => {
  c.header("Cache-Control", "no-store");
  if (new URL(c.req.url).search !== "") {
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: null,
      errorCode: "contract_violation",
      message: "Run start request must not include query parameters.",
    }), 400);
  }

  let requestJson: unknown;
  try {
    requestJson = await c.req.json();
  } catch {
    requestJson = null;
  }

  const body = parseRunStartBody(requestJson);
  if (body === null) {
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: null,
      errorCode: "contract_violation",
      message: "Run start request body is invalid.",
    }), 400);
  }

  const idempotencyKey = c.req.header("Idempotency-Key");
  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: body.operationPreviewId,
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: body.operationPreviewId,
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const now = Date.now();
  const idempotencyRecord = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, runs.preview_id, snapshots.snapshot_json
     FROM idempotency_records idem
     LEFT JOIN runs ON runs.id = idem.result_ref
     LEFT JOIN run_snapshots snapshots ON snapshots.run_id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'run_start' AND idem.idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<RunStartIdempotencyRecordRow>();

  if (idempotencyRecord !== null) {
    if (idempotencyRecord.preview_id !== body.operationPreviewId) {
      return c.json(createRunStartRejectedResponse({
        operationPreviewId: body.operationPreviewId,
        errorCode: "contract_violation",
        message: "Idempotency-Key was already used for another run start.",
      }), 400);
    }
    const replayResponse = createRunStartReplayResponse(idempotencyRecord);
    if (replayResponse === null) {
      return c.json(createRunStartRejectedResponse({
        operationPreviewId: body.operationPreviewId,
        errorCode: "operation_preview_already_consumed",
        message: "Operation preview was already consumed.",
      }), 409);
    }
    return c.json(replayResponse);
  }

  const row = await c.env.DB.prepare(
    `SELECT id, account_id, mutation_id, objective, risk, balance_config_version,
            preview_json, status, expires_at, created_at, admission_version
     FROM operation_previews
     WHERE id = ? AND account_id = ?`,
  )
    .bind(body.operationPreviewId, authContext.accountId)
    .first<RunStartPreviewRow>();

  if (row === null) {
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: body.operationPreviewId,
      errorCode: "operation_preview_not_found",
      message: "Operation preview was not found.",
    }), 404);
  }

  if (row.admission_version) {
    try {
      return c.json(await startDeparture(c.env.DB, authContext.accountId, row, idempotencyKey, async (preview, runId, startedAt) =>
        createRunStartAcceptedResponse({ runId, operationPreviewId: preview.id,
          previewMetadata: parseRunPreviewMetadata(preview.preview_json, preview.objective), risk: preview.risk,
          balanceVersion: preview.balance_config_version, serverStartedAt: startedAt,
          runVersionSnapshot: await createRunVersionSnapshotForAcceptedRequest(c, startedAt) })));
    } catch (error) {
      if (error instanceof ActivityError) return c.json(createRunStartRejectedResponse({ operationPreviewId: row.id, errorCode: error.code, message: "Departure admission rejected." }), error.status);
      throw error;
    }
  }
  if (row.status === "CONSUMED") {
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: body.operationPreviewId,
      errorCode: "operation_preview_already_consumed",
      message: "Operation preview was already consumed.",
    }), 409);
  }

  if (row.status !== "ACTIVE" || row.expires_at <= now) {
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: body.operationPreviewId,
      errorCode: "operation_preview_not_startable",
      message: "Operation preview is not startable.",
    }), 409);
  }

  const previewMetadata = parseRunPreviewMetadata(row.preview_json, row.objective);
  const runId = createDomainId("run");
  const runVersionSnapshot = await createRunVersionSnapshotForAcceptedRequest(c, now);
  const snapshotJson = JSON.stringify({
    runId,
    operationPreviewId: row.id,
    operationId: previewMetadata.operationId,
    actId: previewMetadata.actId,
    regionId: previewMetadata.regionId,
    encounterType: previewMetadata.encounterType,
    riskLevel: row.risk,
    risk: row.risk,
    balanceVersion: String(row.balance_config_version),
    contentVersion: previewMetadata.contentVersion,
    ordinary: previewMetadata.encounterType === "ORDINARY_15_WAVE"
      ? { totalWaves: 15, startWaveNumber: 1 }
      : null,
    raid: previewMetadata.encounterType === "RAID_BOSS"
      ? {
          raidId: previewMetadata.raidId,
          bossId: previewMetadata.bossId,
          bossPhaseRefs: previewMetadata.bossPhaseRefs,
        }
      : null,
    raidId: previewMetadata.encounterType === "RAID_BOSS" ? previewMetadata.raidId : null,
    bossId: previewMetadata.encounterType === "RAID_BOSS" ? previewMetadata.bossId : null,
    bossPhaseRefs: previewMetadata.encounterType === "RAID_BOSS" ? previewMetadata.bossPhaseRefs : [],
    allowedOperatorSlots: previewMetadata.allowedOperatorSlots,
    startAugmentPackageRef: previewMetadata.startAugmentPackageRef,
    startTacticPackageRef: previewMetadata.startTacticPackageRef,
    runVersionSnapshot,
    serverStartedAt: toIso(now),
  });

  try {
    await c.env.DB.batch([
      c.env.DB.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN
        EXISTS(SELECT 1 FROM operation_previews WHERE id=? AND account_id=? AND status='ACTIVE'
          AND expires_at>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) AND admission_version IS NULL AND preview_json=?)
        AND NOT EXISTS(SELECT 1 FROM runs WHERE account_id=? AND status IN ('ACTIVE','FINISH_PENDING','QUARANTINED'))
        THEN 1 ELSE 0 END`).bind(row.id, authContext.accountId, row.preview_json, authContext.accountId),
      c.env.DB.prepare("UPDATE operation_previews SET status='CONSUMED' WHERE id=? AND account_id=?").bind(row.id, authContext.accountId),
      c.env.DB.prepare(
        `INSERT INTO runs (id, account_id, preview_id, mutation_id, balance_config_version, status, started_at, finished_at, build_id)
         VALUES (?, ?, ?, ?, ?, 'ACTIVE', ?, NULL, NULL)`,
      ).bind(runId, authContext.accountId, row.id, row.mutation_id, row.balance_config_version, now),
      c.env.DB.prepare(
        `INSERT INTO run_snapshots (run_id, snapshot_json, created_at)
         VALUES (?, ?, ?)`,
      ).bind(runId, snapshotJson, now),
      c.env.DB.prepare(
        `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
         VALUES (?, ?, ?, 'run_start', ?, ?)`,
      ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, runId, now),
      c.env.DB.prepare("DELETE FROM departure_write_guard"),
    ]);
  } catch {
    const completed = await c.env.DB.prepare(`SELECT idem.result_ref,idem.created_at,runs.preview_id,snapshots.snapshot_json
      FROM idempotency_records idem JOIN runs ON runs.id=idem.result_ref JOIN run_snapshots snapshots ON snapshots.run_id=runs.id
      WHERE idem.account_id=? AND idem.operation='run_start' AND idem.idempotency_key=?`)
      .bind(authContext.accountId, idempotencyKey).first<RunStartIdempotencyRecordRow>();
    if (completed?.preview_id === row.id) {
      const replay = createRunStartReplayResponse(completed);
      if (replay) return c.json(replay);
    }
    await writeGameApiOpsErrorLog(c, {
      severity: "error",
      errorCode: "run_start_state_conflict",
      messageSummary: "Atomic run start persistence failed.",
      accountId: authContext.accountId,
      idempotencyOperation: "run_start",
      idempotencyKey,
      correlation: {
        runId,
        operationPreviewId: row.id,
        operationId: previewMetadata.operationId,
        encounterType: previewMetadata.encounterType,
      },
      context: {
        phase: "run_start_atomic_commit",
        rollbackAttempted: false,
        responseShape: "run_start_rejected",
      },
      retryable: true,
    });
    return c.json(createRunStartRejectedResponse({
      operationPreviewId: body.operationPreviewId,
      errorCode: "run_start_state_conflict",
      message: "Run start could not be committed safely.",
    }), 409);
  }

  return c.json(createRunStartAcceptedResponse({
    runId,
    operationPreviewId: row.id,
    previewMetadata,
    risk: row.risk,
    balanceVersion: row.balance_config_version,
    serverStartedAt: now,
    runVersionSnapshot,
  }));
});

app.post("/v1/runs/:runId/finish", async (c) => {
  const runId = c.req.param("runId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }), 400);
  }

  let requestJson: unknown;
  try {
    requestJson = await c.req.json();
  } catch {
    requestJson = null;
  }

  const body = parseRunFinishBody(requestJson, runId);
  if (body === null) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: "contract_violation",
      message: "Run finish request body is invalid.",
    }), 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: authContext.errorCode,
      message: authContext.message,
    }), authContext.status);
  }

  const idempotencyRecord = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, NULL AS reward_created_at,
            preview.balance_config_version AS balance_config_version,
            preview.run_id AS preview_run_id,
            final.payload_json AS final_payload_json
     FROM idempotency_records idem
     LEFT JOIN run_reward_previews preview ON preview.id = idem.result_ref
     LEFT JOIN run_final_results final ON final.run_id = preview.run_id
     WHERE idem.account_id = ? AND idem.operation = 'run_finish' AND idem.idempotency_key = ?`,
  )
    .bind(authContext.accountId, idempotencyKey)
    .first<IdempotencyRecordRow>();

  if (idempotencyRecord !== null) {
    if (idempotencyRecord.preview_run_id !== runId) {
      return c.json(createRunFinishRejectedResponse({
        runId,
        errorCode: "contract_violation",
        message: "Idempotency-Key was already used for another run finish.",
      }), 400);
    }
    return c.json(createRunFinishDuplicateReplayResponse({
      runId,
      rewardPreviewId: idempotencyRecord.result_ref,
      balanceVersion: idempotencyRecord.balance_config_version === null || idempotencyRecord.balance_config_version === undefined
        ? null
        : String(idempotencyRecord.balance_config_version),
      serverReceivedAt: idempotencyRecord.created_at,
      staminaRefund: getStoredStoryRaidStaminaRefundCandidate(idempotencyRecord.final_payload_json),
      runStartVersionSnapshot: getStoredRunStartVersionSnapshotFromFinishPayload(idempotencyRecord.final_payload_json),
      runFinishVersionSnapshot: getStoredRunFinishVersionSnapshot(idempotencyRecord.final_payload_json),
    }));
  }

  const row = await c.env.DB.prepare(
    `SELECT runs.id, runs.account_id, runs.status, runs.balance_config_version,
            preview.objective, preview.risk, preview.preview_json,
            balance.config_json,
            snapshots.snapshot_json
     FROM runs
     JOIN operation_previews preview ON preview.id = runs.preview_id
     JOIN balance_config_versions balance ON balance.version = runs.balance_config_version
     LEFT JOIN run_snapshots snapshots ON snapshots.run_id = runs.id
     WHERE runs.id = ?`,
  )
    .bind(runId)
    .first<RunFinishRow>();

  if (row === null) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      errorCode: "run_not_found",
      message: "Run was not found.",
    }));
  }

  if (row.account_id !== authContext.accountId) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "run_account_mismatch",
      message: "Run does not belong to the authenticated account.",
    }));
  }

  if (row.status !== "ACTIVE" && row.status !== "FINISH_PENDING") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "run_state_conflict",
      message: "Run is not finishable.",
    }));
  }

  const risk = parseRunRisk(row.risk);
  if (risk === null) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "risk_reward_policy_missing",
      message: "Run risk policy is invalid.",
    }));
  }

  let balanceConfig: { runRewards?: unknown };
  let runRewardsConfig;
  try {
    balanceConfig = JSON.parse(row.config_json) as { runRewards?: unknown };
    runRewardsConfig = parseRunRewardsConfig(balanceConfig.runRewards);
  } catch (error) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: error instanceof Error ? error.message : "reward_plan_unavailable",
      message: "Run reward policy is unavailable.",
    }));
  }

  const previewMetadata = parseRunPreviewMetadata(row.preview_json, row.objective);
  const allowedStartPackageRefs = [
    previewMetadata.startAugmentPackageRef,
    previewMetadata.startTacticPackageRef,
  ].filter((value): value is string => value !== null && value.trim() !== "");
  const submittedPackageRefs = getTacticSelectionPackageRefs(body.tacticSelections);
  if (
    submittedPackageRefs.length > 0 &&
    submittedPackageRefs.some((packageRef) => !allowedStartPackageRefs.includes(packageRef))
  ) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "contract_violation",
      message: "Tactic selections must reference the server-authored start package.",
    }), 400);
  }
  if (previewMetadata.encounterType === "RAID_BOSS" && body.encounterType !== "RAID_BOSS") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "contract_violation",
      message: "Raid boss run finish requires raid encounter metadata.",
    }), 400);
  }
  if (previewMetadata.encounterType === "ORDINARY_15_WAVE" && body.encounterType === "RAID_BOSS") {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "contract_violation",
      message: "Ordinary run finish must not submit raid encounter metadata.",
    }), 400);
  }
  const fullCompletion = isRunFullCompletion(body, previewMetadata);
  const rewardClearedWaves = previewMetadata.encounterType === "RAID_BOSS" && fullCompletion
    ? 15
    : body.completedWaveCount;
  const firstClearRecord = await c.env.DB.prepare(
    `SELECT cleared_at
     FROM account_first_clears
     WHERE account_id = ? AND operation_key = ? AND risk = ?`,
  )
    .bind(authContext.accountId, previewMetadata.operationId, risk)
    .first<FirstClearRow>();
  const firstClearEligible =
    firstClearRecord === null &&
    fullCompletion;
  const existingStoryProgress = fullCompletion
    ? await c.env.DB.prepare(
        `SELECT highest_story_progress_id, cleared_operations_json, story_flags_json
         FROM account_story_progress
         WHERE account_id = ?`,
      )
        .bind(authContext.accountId)
        .first<AccountStoryProgressRow>()
    : null;
  const rewardPreview = calculateRunRewardPreview({
    config: runRewardsConfig,
    operationId: previewMetadata.operationId,
    actId: previewMetadata.actId,
    regionId: previewMetadata.regionId,
    risk,
    outcome: body.outcome,
    clearedWaves: rewardClearedWaves,
    firstClearEligible,
    integrityDisposition: "ALLOW" satisfies IntegrityDisposition,
  });
  const launchRewards = await prepareLaunchRewards(c.env.DB, authContext.accountId, previewMetadata.operationId, rewardPreview.rewardLines, (await readEconomyBinding(c.env.DB, row.balance_config_version)) !== null);
  const staminaRefund = createStoryRaidStaminaRefundCandidate(body, previewMetadata);
  const staminaRefundAmount = staminaRefund === null ? null : getStoryRaidStaminaRefundAmount(balanceConfig);
  if (staminaRefund !== null && staminaRefundAmount === null) {
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "story_raid_stamina_refund_policy_missing",
      message: "Story raid stamina refund policy is unavailable.",
    }), 409);
  }
  const fuelState = staminaRefund === null
    ? null
    : await c.env.DB.prepare(
        `SELECT fuel, max_fuel
         FROM fuel_state
         WHERE account_id = ?`,
      )
        .bind(authContext.accountId)
        .first<FuelStateRow>();
  if (staminaRefund !== null && fuelState === null) {
    await writeGameApiOpsErrorLog(c, {
      severity: "error",
      errorCode: "story_raid_stamina_refund_state_conflict",
      messageSummary: "Story raid stamina refund could not find fuel state.",
      accountId: authContext.accountId,
      runId,
      idempotencyOperation: "run_finish",
      idempotencyKey,
      correlation: {
        runId,
        operationId: previewMetadata.operationId,
        encounterType: previewMetadata.encounterType,
      },
      context: {
        phase: "run_finish_story_raid_stamina_refund",
        responseShape: "run_finish_rejected",
      },
      retryable: true,
    });
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "story_raid_stamina_refund_state_conflict",
      message: "Story raid stamina refund could not be committed safely.",
    }), 409);
  }
  const staminaRefundFuelDelta = staminaRefund !== null && staminaRefundAmount !== null && fuelState !== null
    ? Math.max(0, Math.min(staminaRefundAmount, fuelState.max_fuel - fuelState.fuel))
    : null;
  const staminaRefundFuelAfter = staminaRefund !== null && staminaRefundAmount !== null && fuelState !== null
    ? fuelState.fuel + (staminaRefundFuelDelta ?? 0)
    : null;
  const now = Date.now();
  const runStartVersionSnapshot = getStoredRunStartVersionSnapshot(row.snapshot_json);
  const runFinishVersionSnapshot = await createRunFinishVersionSnapshotForAcceptedRequest(c, now);
  const rewardPreviewId = createDomainId("reward_preview");
  const storyProgressPlan = fullCompletion
    ? createStoryProgressPlan({
        accountId: authContext.accountId,
        runId,
        operationId: previewMetadata.operationId,
        actId: previewMetadata.actId,
        existing: existingStoryProgress,
        now,
      })
    : null;
  const storyProgressMutationId = storyProgressPlan === null ? null : createDomainId("mutation");
  const storyProgressLedgerId = storyProgressPlan === null ? null : createDomainId("progression");
  const staminaRefundMutationId = staminaRefund === null ? null : createDomainId("mutation");
  const staminaRefundFuelLedgerId = staminaRefund === null ? null : createDomainId("fuel_ledger");
  const latestMutation = storyProgressPlan === null && staminaRefund === null
    ? null
    : await c.env.DB.prepare(
        `SELECT MAX(sequence) AS sequence
         FROM account_mutations
         WHERE account_id = ?`,
      )
        .bind(authContext.accountId)
        .first<AccountMutationSequenceRow>();
  const nextMutationSequence = (latestMutation?.sequence ?? 0) + 1;
  const staminaRefundMutationSequence = storyProgressPlan === null ? nextMutationSequence : nextMutationSequence + 1;
  const rewardPlanJson = JSON.stringify({
    rewardLines: launchRewards.lines,
    calculation: {
      ...rewardPreview.calculation,
      balanceVersion: String(row.balance_config_version),
      ...(staminaRefund === null ? {} : { staminaRefund }),
    },
  });
  const finalPayloadJson = JSON.stringify(createRunFinishPayload(
    body,
    previewMetadata,
    row.balance_config_version,
    runStartVersionSnapshot,
    runFinishVersionSnapshot,
  ));

  const finishStatements = [
    c.env.DB.prepare(
      `INSERT INTO run_final_results (run_id, outcome, cleared_waves, payload_json, submitted_at)
       VALUES (?, ?, ?, ?, ?)`,
    ).bind(runId, body.outcome, body.completedWaveCount, finalPayloadJson, now),
    c.env.DB.prepare(
      `UPDATE runs
       SET status = ?, finished_at = ?
       WHERE id = ? AND account_id = ?`,
    ).bind(getFinishedRunStatus(body.outcome), now, runId, authContext.accountId),
    c.env.DB.prepare(
      `INSERT INTO run_reward_previews (
         id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
       ) VALUES (?, ?, ?, 'ACTIVE', ?, ?, ?, ?, NULL, NULL)`,
    ).bind(
      rewardPreviewId,
      authContext.accountId,
      runId,
      row.balance_config_version,
      rewardPlanJson,
      now + runRewardPreviewTtlMs,
      now,
    ),
    ...(rewardPreview.calculation.firstClearApplied
      ? [
          c.env.DB.prepare(
            `INSERT INTO account_first_clears (account_id, operation_key, risk, cleared_at, run_id)
             VALUES (?, ?, ?, ?, ?)`,
          ).bind(authContext.accountId, previewMetadata.operationId, risk, now, runId),
        ]
      : []),
    ...(storyProgressPlan === null || storyProgressMutationId === null || storyProgressLedgerId === null
      ? []
      : [
          c.env.DB.prepare(
            `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
             VALUES (?, ?, ?, 'STORY_PROGRESS', ?, ?)`,
          ).bind(storyProgressMutationId, authContext.accountId, nextMutationSequence, `run_finish:${idempotencyKey}`, now),
          c.env.DB.prepare(
            `INSERT INTO account_story_progress (
               account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
               cleared_operations_json, story_flags_json, narrative_version, last_progress_run_id, updated_at
             ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
             ON CONFLICT(account_id) DO UPDATE SET
               act_id = excluded.act_id,
               highest_cleared_operation_id = excluded.highest_cleared_operation_id,
               highest_story_progress_id = excluded.highest_story_progress_id,
               cleared_operations_json = excluded.cleared_operations_json,
               story_flags_json = excluded.story_flags_json,
               narrative_version = excluded.narrative_version,
               last_progress_run_id = excluded.last_progress_run_id,
               updated_at = excluded.updated_at`,
          ).bind(
            authContext.accountId,
            previewMetadata.actId,
            storyProgressPlan.highestClearedOperationId,
            storyProgressPlan.highestStoryProgressId,
            JSON.stringify(storyProgressPlan.clearedOperations),
            JSON.stringify(storyProgressPlan.storyFlags),
            "act1-narrative-v1",
            runId,
            now,
          ),
          ...(storyProgressPlan.nextOperationUnlockId === null
            ? []
            : [
                c.env.DB.prepare(
                  `INSERT OR IGNORE INTO account_unlocks (account_id, unlock_id, unlocked_at)
                   VALUES (?, ?, ?)`,
                ).bind(authContext.accountId, storyProgressPlan.nextOperationUnlockId, now),
              ]),
          c.env.DB.prepare(
            `INSERT INTO progression_ledger (id, account_id, mutation_id, progression_type, target_id, delta_json, created_at)
             VALUES (?, ?, ?, 'STORY_PROGRESS', ?, ?, ?)`,
          ).bind(
            storyProgressLedgerId,
            authContext.accountId,
            storyProgressMutationId,
            storyProgressPlan.highestStoryProgressId,
            JSON.stringify({
              runId,
              actId: previewMetadata.actId,
              highestStoryProgressId: storyProgressPlan.highestStoryProgressId,
              highestClearedOperationId: storyProgressPlan.highestClearedOperationId,
              unlockedOperation: storyProgressPlan.nextOperationUnlockId,
            }),
            now,
          ),
        ]),
    ...(staminaRefund === null ||
      staminaRefundAmount === null ||
      staminaRefundFuelDelta === null ||
      staminaRefundFuelAfter === null ||
      staminaRefundMutationId === null ||
      staminaRefundFuelLedgerId === null
      ? []
      : [
          c.env.DB.prepare(
            `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
             VALUES (?, ?, ?, 'STORY_RAID_STAMINA_REFUND', ?, ?)`,
          ).bind(staminaRefundMutationId, authContext.accountId, staminaRefundMutationSequence, `run_finish:${idempotencyKey}:stamina_refund`, now),
          c.env.DB.prepare(
            `INSERT INTO fuel_ledger (id, account_id, mutation_id, delta, fuel_after, reason, created_at)
             VALUES (?, ?, ?, ?, ?, 'STORY_RAID_FAILURE_REFUND', ?)`,
          ).bind(
            staminaRefundFuelLedgerId,
            authContext.accountId,
            staminaRefundMutationId,
            staminaRefundFuelDelta,
            staminaRefundFuelAfter,
            now,
          ),
          c.env.DB.prepare(
            `UPDATE fuel_state
             SET fuel = ?, updated_at = ?
             WHERE account_id = ?`,
          ).bind(staminaRefundFuelAfter, now, authContext.accountId),
        ]),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'run_finish', ?, ?)`,
    ).bind(createDomainId("idem"), authContext.accountId, idempotencyKey, rewardPreviewId, now),
  ];

  try {
    await c.env.DB.batch(finishStatements);
  } catch {
    if (storyProgressPlan !== null) {
      await writeGameApiOpsErrorLog(c, {
        severity: "error",
        errorCode: "story_progress_state_conflict",
        messageSummary: "Run finish story progress commit transaction failed.",
        accountId: authContext.accountId,
        runId,
        storyProgressId: storyProgressPlan.highestStoryProgressId,
        idempotencyOperation: "run_finish",
        idempotencyKey,
        correlation: {
          runId,
          operationId: previewMetadata.operationId,
          storyProgressId: storyProgressPlan.highestStoryProgressId,
        },
        context: {
          phase: "run_finish_story_progress_commit",
          responseShape: "run_finish_rejected",
        },
        retryable: true,
      });
      return c.json(createRunFinishRejectedResponse({
        runId,
        balanceVersion: String(row.balance_config_version),
        errorCode: "story_progress_state_conflict",
        message: "Story progress could not be committed safely.",
      }), 409);
    }
    return c.json(createRunFinishRejectedResponse({
      runId,
      balanceVersion: String(row.balance_config_version),
      errorCode: "run_state_conflict",
      message: "Run finish could not be committed safely.",
    }), 409);
  }

  return c.json(createRunFinishAcceptedResponse({
    runId,
    rewardPreviewId,
    balanceVersion: String(row.balance_config_version),
    serverReceivedAt: now,
    staminaRefund,
    runStartVersionSnapshot,
    runFinishVersionSnapshot,
  }));
});

app.get("/v1/runs/:runId/reward-preview/:rewardPreviewId", async (c) => {
  const runId = c.req.param("runId");
  const rewardPreviewId = c.req.param("rewardPreviewId");
  const row = await c.env.DB.prepare(
    `SELECT id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     FROM run_reward_previews
     WHERE run_id = ? AND id = ?`,
  )
    .bind(runId, rewardPreviewId)
    .first<RewardPreviewRow>();

  if (row === null) {
    return c.json({ errorCode: "reward_preview_not_found", message: "Reward preview was not found." }, 404);
  }

  const claimability = getClaimability(row);
  const storedPlan = JSON.parse(row.reward_plan_json) as {calculation?:{operationId?:string}};
  const visibleLaunchPlan = row.status === "CLAIMED" ? null : await prepareLaunchRewards(c.env.DB,row.account_id,storedPlan.calculation?.operationId ?? "",getRewardLines(row.reward_plan_json) as LaunchLine[],(await readEconomyBinding(c.env.DB,row.balance_config_version)) !== null);
  return c.json({
    runId: row.run_id,
    rewardPreviewId: row.id,
    status: row.status,
    claimability,
    balanceVersion: String(row.balance_config_version),
    expiresAt: toIso(row.expires_at),
    serverCalculatedAt: toIso(row.created_at),
    rewardLines: visibleLaunchPlan?.lines ?? getRewardLines(row.reward_plan_json),
    claim: {
      claimable: claimability === "CLAIMABLE",
      rewardClaimId: row.reward_claim_id,
      claimedAt: row.claimed_at === null ? null : toIso(row.claimed_at),
      errorCode: claimability === "CLAIMABLE" || claimability === "ALREADY_CLAIMED" ? null : `reward_preview_${claimability.toLowerCase()}`,
    },
  });
});

app.post("/v1/runs/:runId/reward-preview/:rewardPreviewId/claim", withEconomyRetry(async (c) => {
  const runId = c.req.param("runId");
  const rewardPreviewId = c.req.param("rewardPreviewId");
  const idempotencyKey = c.req.header("Idempotency-Key");

  if (idempotencyKey === undefined || idempotencyKey.trim() === "") {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Idempotency-Key header is required.",
    }, 400);
  }

  let body: { runId?: unknown; rewardPreviewId?: unknown };
  try {
    body = await c.req.json();
  } catch {
    body = {};
  }

  if (body.runId !== runId || body.rewardPreviewId !== rewardPreviewId) {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "contract_violation",
      message: "Path and body runId/rewardPreviewId must match.",
    }, 400);
  }

  const authContext = await resolveAuthContext(c);
  if (isAuthFailure(authContext)) return c.json({status:"REJECTED",errorCode:authContext.errorCode},authContext.status);
  if (Object.keys(body).some(key => key !== "runId" && key !== "rewardPreviewId")) return c.json({status:"REJECTED",errorCode:"contract_violation"},400);
  const prior = await c.env.DB.prepare(`SELECT r.run_id, r.reward_json, r.id, r.created_at FROM idempotency_records i JOIN reward_transactions r ON r.id = i.result_ref WHERE i.account_id = ? AND i.operation = 'run_reward_claim' AND i.idempotency_key = ?`)
    .bind(authContext.accountId,idempotencyKey).first<{run_id:string;reward_json:string;id:string;created_at:number}>();
  if (prior && prior.run_id !== runId) return c.json({status:"REJECTED",errorCode:"idempotency_conflict"},409);
  const row = await c.env.DB.prepare(
    `SELECT id, account_id, run_id, status, balance_config_version, reward_plan_json, expires_at, created_at, claimed_at, reward_claim_id
     FROM run_reward_previews
     WHERE run_id = ? AND id = ?`,
  )
    .bind(runId, rewardPreviewId)
    .first<RewardPreviewRow>();

  if (row !== null && row.account_id !== authContext.accountId) return c.json({status:"REJECTED",errorCode:"reward_preview_not_found"},404);
  if (row !== null && row.status === "CLAIMED" && row.reward_claim_id !== null && row.claimed_at !== null) {
    return c.json({
      runId: row.run_id,
      rewardPreviewId: row.id,
      rewardClaimId: row.reward_claim_id,
      status: "DUPLICATE_REPLAY",
      serverClaimedAt: toIso(row.claimed_at),
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: "duplicate_replay",
      message: "Reward claim was already committed.",
    });
  }

  if (row === null) {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: null,
      rewardLines: [],
      errorCode: "reward_preview_not_found",
      message: "Reward preview was not found.",
    }, 404);
  }

  if (getClaimability(row) !== "CLAIMABLE") {
    return c.json({
      runId,
      rewardPreviewId,
      rewardClaimId: null,
      status: "REJECTED",
      serverClaimedAt: null,
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: `reward_preview_${getClaimability(row).toLowerCase()}`,
      message: "Reward preview is not claimable.",
    });
  }

  const idempotencyRecord = await c.env.DB.prepare(
    `SELECT idem.result_ref, idem.created_at, reward.created_at AS reward_created_at, reward.run_id
     FROM idempotency_records idem
     LEFT JOIN reward_transactions reward ON reward.id = idem.result_ref
     WHERE idem.account_id = ? AND idem.operation = 'run_reward_claim' AND idem.idempotency_key = ?`,
  )
    .bind(row.account_id, idempotencyKey)
    .first<IdempotencyRecordRow & { run_id: string }>();

  if (idempotencyRecord !== null) {
    if (idempotencyRecord.run_id !== runId) return c.json({ status: "REJECTED", errorCode: "idempotency_conflict" }, 409);
    return c.json({
      runId: row.run_id,
      rewardPreviewId: row.id,
      rewardClaimId: idempotencyRecord.result_ref,
      status: "DUPLICATE_REPLAY",
      serverClaimedAt: toIso(idempotencyRecord.reward_created_at ?? idempotencyRecord.created_at),
      balanceVersion: String(row.balance_config_version),
      rewardLines: [],
      errorCode: "duplicate_replay",
      message: "Reward claim was already committed.",
    });
  }

  const now = Date.now();
  const accountId = row.account_id;
  const rewardClaimId = createDomainId("reward_claim");
  const mutationId = createDomainId("mutation");
  const economyTransactionId = createDomainId("economy");
  const latestMutation = await c.env.DB.prepare(
    `SELECT MAX(sequence) AS sequence
     FROM account_mutations
     WHERE account_id = ?`,
  )
    .bind(accountId)
    .first<AccountMutationSequenceRow>();
  const nextSequence = (latestMutation?.sequence ?? 0) + 1;
  const originalPlan = JSON.parse(row.reward_plan_json) as {calculation?:{operationId?:string}};
  const launchPlan = await prepareLaunchRewards(c.env.DB, accountId, originalPlan.calculation?.operationId ?? "", getRewardLines(row.reward_plan_json) as LaunchLine[], (await readEconomyBinding(c.env.DB,row.balance_config_version)) !== null);
  const effectiveRewardJson = JSON.stringify({...originalPlan,rewardLines:launchPlan.lines});
  const rewardLines = getCurrencyRewardLines(effectiveRewardJson);
  const itemRewardLines = getItemRewardLines(effectiveRewardJson);
  const crystalLines = launchPlan.lines.filter(line => line.kind === "CRYSTAL");
  const crystalPlan = crystalLines.length === 0 ? null : await planCrystalChange({db:c.env.DB,accountId,operationId:createDomainId("crystal_operation"),mutationId,operation:"LAUNCH_FIRST_CLEAR",now,
    ...(launchPlan.conversionPolicyVersion ? { expectedPolicyVersion: launchPlan.conversionPolicyVersion } : {}),
    grants:crystalLines.map(line => ({quantity:line.quantity,sourceType:"LAUNCH_ENTITLEMENT" as const,sourceRef:line.launchSourceKey!,orderId:null}))});
  const claimRewardLines = [];
  const statements = [
    c.env.DB.prepare(
      `INSERT INTO account_mutations (id, account_id, sequence, mutation_type, idempotency_key, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', ?, ?)`,
    ).bind(mutationId, accountId, nextSequence, idempotencyKey, now),
    c.env.DB.prepare(
      `INSERT INTO reward_transactions (id, account_id, run_id, mutation_id, balance_config_version, reward_json, created_at)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
    ).bind(rewardClaimId, accountId, runId, mutationId, row.balance_config_version, effectiveRewardJson, now),
    c.env.DB.prepare(
      `INSERT INTO economy_transactions (id, account_id, mutation_id, reason, source_type, source_id, created_at)
       VALUES (?, ?, ?, 'RUN_REWARD_CLAIM', 'RUN_REWARD_PREVIEW', ?, ?)`,
    ).bind(economyTransactionId, accountId, mutationId, rewardPreviewId, now),
  ];

  const currencyBalances = new Map<string, number>();
  for (const rewardLine of rewardLines) {
    const currency = currencyIdToD1Currency[rewardLine.id as keyof typeof currencyIdToD1Currency];
    const current = await c.env.DB.prepare(
      `SELECT balance
       FROM account_currencies
       WHERE account_id = ? AND currency = ?`,
    )
      .bind(accountId, currency)
      .first<AccountCurrencyRow>();
    const balanceAfter = (currencyBalances.get(currency) ?? current?.balance ?? 0) + rewardLine.quantity;
    currencyBalances.set(currency,balanceAfter);
    const ledgerId = createDomainId("ledger");
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO account_currencies (account_id, currency, balance, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, currency) DO UPDATE SET balance = excluded.balance, updated_at = excluded.updated_at`,
      ).bind(accountId, currency, balanceAfter, now),
      c.env.DB.prepare(
        `INSERT INTO currency_ledger (id, account_id, transaction_id, currency, delta, balance_after, created_at)
         VALUES (?, ?, ?, ?, ?, ?, ?)`,
      ).bind(ledgerId, accountId, economyTransactionId, currency, rewardLine.quantity, balanceAfter, now),
    );
    claimRewardLines.push({
      kind: rewardLine.kind,
      id: rewardLine.id,
      quantity: rewardLine.quantity,
      source: rewardLine.source,
      balanceAfter,
    });
  }

  statements.push(...launchEntitlementStatements(c.env.DB, accountId, launchPlan.sources, rewardClaimId, now), ...(crystalPlan?.statements ?? []));
  claimRewardLines.push(...crystalLines);

  const itemBalances = new Map<string, number>();
  for (const rewardLine of itemRewardLines) {
    const current = await c.env.DB.prepare(
      `SELECT quantity
       FROM account_items
       WHERE account_id = ? AND item_id = ?`,
    )
      .bind(accountId, rewardLine.id)
      .first<AccountItemRow>();
    const balanceAfter = (itemBalances.get(rewardLine.id) ?? current?.quantity ?? 0) + rewardLine.quantity;
    itemBalances.set(rewardLine.id,balanceAfter);
    const itemLedgerId = createDomainId("item_ledger");
    statements.push(
      c.env.DB.prepare(
        `INSERT INTO account_items (account_id, item_id, quantity, updated_at)
         VALUES (?, ?, ?, ?)
         ON CONFLICT(account_id, item_id) DO UPDATE SET quantity = excluded.quantity, updated_at = excluded.updated_at`,
      ).bind(accountId, rewardLine.id, balanceAfter, now),
      c.env.DB.prepare(
        `INSERT INTO item_ledger (id, account_id, mutation_id, item_id, delta, quantity_after, reason, source_type, source_id, created_at)
         VALUES (?, ?, ?, ?, ?, ?, 'RUN_REWARD_ITEM_GRANT', 'RUN_REWARD_CLAIM', ?, ?)`,
      ).bind(itemLedgerId, accountId, mutationId, rewardLine.id, rewardLine.quantity, balanceAfter, rewardPreviewId, now),
    );
    claimRewardLines.push({
      kind: rewardLine.kind,
      id: rewardLine.id,
      quantity: rewardLine.quantity,
      source: rewardLine.source,
      balanceAfter,
    });
  }

  statements.push(
    c.env.DB.prepare(
      `UPDATE run_reward_previews
       SET status = 'CLAIMED', claimed_at = ?, reward_claim_id = ?, reward_plan_json = ?
       WHERE id = ? AND run_id = ? AND status = 'ACTIVE'`,
    ).bind(now, rewardClaimId, effectiveRewardJson, rewardPreviewId, runId),
    c.env.DB.prepare(
      `INSERT INTO idempotency_records (id, account_id, idempotency_key, operation, result_ref, created_at)
       VALUES (?, ?, ?, 'run_reward_claim', ?, ?)`,
    ).bind(createDomainId("idem"), accountId, idempotencyKey, rewardClaimId, now),
  );

  await c.env.DB.batch(statements);

  return c.json({
    runId,
    rewardPreviewId,
    rewardClaimId,
    status: "CLAIMED",
    serverClaimedAt: toIso(now),
    balanceVersion: String(row.balance_config_version),
    rewardLines: claimRewardLines,
    errorCode: null,
    message: null,
  });
}));

export { RealtimeCoopRoom };

registerActivityRoutes(app, resolveAuthContext);
registerCrystalRoutes(app, resolveAuthContext);
registerGrowthRoutes(app, resolveAuthContext);
registerAccountStateRoutes(app, resolveAuthContext);
registerFormationRoutes(app, resolveAuthContext);
registerTrainLoadoutRoutes(app, resolveAuthContext);
registerDepartureRoutes(app, resolveAuthContext);
registerGachaReadRoutes(app, resolveAuthContext, {
  parseGachaPoolResults, parseGachaPityConfig, getInitialPityState, getNextGachaGuaranteeType,
  isGachaPoolResultEligibleForGuarantee, getGachaResultWeight, getGachaReplayResponseResults,
});

export type { GachaBannerRow, GachaPityState, GachaPityRow, GachaPityConfig, GachaPoolResult, GachaGuaranteeType, GachaReplayResultRow, GachaCommittedResult };

export default app;
