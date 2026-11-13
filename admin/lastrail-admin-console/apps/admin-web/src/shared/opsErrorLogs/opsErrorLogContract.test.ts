import { describe, expect, it } from "vitest";
import {
  emptyOpsErrorLogDetail,
  emptyOpsErrorLogListItem,
  mapOpsErrorLogDetailResponse,
  mapOpsErrorLogListResponse,
  noDataOpsErrorLogListItem,
  opsErrorLogReadFilterFields
} from "./opsErrorLogContract";

describe("opsErrorLogContract", () => {
  it("maps Admin API ops error log list response into read-only rows", () => {
    const result = mapOpsErrorLogListResponse({
      permission: "OPS_ERROR_LOG_READ",
      permissionMode: "ROLE_PERMISSION",
      items: [
        {
          id: "ops-error-1",
          occurredAt: "2026-08-29T01:00:00.000Z",
          severity: "critical",
          status: "open",
          source: "game_api",
          service: "game_api",
          errorCode: "offline_reward_state_conflict",
          messageSummary: "sanitized summary",
          requestId: "req-1",
          accountId: "account-1",
          runId: "run-1",
          rewardPreviewId: null,
          rewardClaimId: null,
          offlineRewardClaimId: "offline-claim-1",
          gachaBatchId: null,
          gachaBannerId: "banner-launch-1",
          storyProgressId: "opn_01_last_departure",
          integrityEventId: null,
          supportTicketId: null,
          idempotencyKeyHash: "sha256:abc",
          payloadRef: null,
          traceRef: null,
          retryable: true,
          masked: true
        }
      ],
      nextCursor: "cursor-2"
    });

    expect(result.permission).toBe("OPS_ERROR_LOG_READ");
    expect(result.permissionMode).toBe("ROLE_PERMISSION");
    expect(result.items[0]).toMatchObject({
      id: "ops-error-1",
      severity: "critical",
      status: "open",
      source: "game_api",
      accountId: "account-1",
      gachaBannerId: "banner-launch-1",
      idempotencyKeyHash: "sha256:abc",
      retryable: true,
      masked: true
    });
    expect(JSON.stringify(result)).not.toContain("raw request body");
    expect(JSON.stringify(result)).not.toContain("rawIdempotencyKey");
  });

  it("maps Google verifier unavailable ops errors as masked retryable game api failures", () => {
    const result = mapOpsErrorLogListResponse({
      items: [
        {
          id: "ops-google-verifier-1",
          occurredAt: "2026-08-29T02:00:00.000Z",
          environment: "DEV",
          severity: "error",
          status: "open",
          source: "game_api",
          service: "game_api",
          errorCode: "google_auth_verifier_unavailable",
          messageSummary: "Google verifier unavailable during JWKS fetch",
          requestId: "req-google-1",
          correlationJson: { accountId: "account-1" },
          retryable: true,
          masked: true,
          jwksRawDump: "must-not-copy",
          fullClaims: "must-not-copy",
          stackDump: "must-not-copy",
          rawAuthHeader: "must-not-copy"
        }
      ]
    });

    expect(result.items[0]).toMatchObject({
      id: "ops-google-verifier-1",
      severity: "error",
      source: "game_api",
      service: "game_api",
      errorCode: "google_auth_verifier_unavailable",
      accountId: "account-1",
      retryable: true,
      masked: true
    });
    expect(JSON.stringify(result)).not.toContain("jwksRawDump");
    expect(JSON.stringify(result)).not.toContain("fullClaims");
    expect(JSON.stringify(result)).not.toContain("stackDump");
    expect(JSON.stringify(result)).not.toContain("rawAuthHeader");
  });

  it("maps run start state conflicts as masked retryable events with safe optional run correlation", () => {
    const result = mapOpsErrorLogListResponse({
      items: [
        {
          id: "ops-run-start-1",
          occurredAt: "2026-08-29T03:00:00.000Z",
          environment: "DEV",
          severity: "error",
          status: "open",
          source: "game_api",
          service: "game_api",
          errorCode: "run_start_state_conflict",
          messageSummary: "Run start state conflict recovered",
          requestId: "req-run-start-1",
          correlationJson: { accountId: "account-1", runId: "candidate-run-id-without-fk" },
          retryable: true,
          masked: true,
          rawBody: "must-not-copy",
          stackDump: "must-not-copy"
        }
      ]
    });

    expect(result.items[0]).toMatchObject({
      id: "ops-run-start-1",
      severity: "error",
      source: "game_api",
      service: "game_api",
      errorCode: "run_start_state_conflict",
      accountId: "account-1",
      runId: "candidate-run-id-without-fk",
      retryable: true,
      masked: true
    });
    expect(JSON.stringify(result)).not.toContain("rawBody");
    expect(JSON.stringify(result)).not.toContain("stackDump");
  });

  it("maps detail response with null detail placeholder and safe refs", () => {
    const result = mapOpsErrorLogDetailResponse({
      item: {
        id: "ops-error-2",
        occurredAt: "2026-08-29T01:05:00.000Z",
        severity: "warning",
        status: "acknowledged",
        source: "support_read_model",
        service: "support_read_model",
        errorCode: "ops_error_log_not_found",
        messageSummary: "sanitized summary",
        requestId: "req-2",
        correlationJson: { accountId: "account-2" },
        sanitizedContextJson: { route: "/v1/admin/ops/error-logs" },
        payloadRef: "r2://sanitized/payload",
        traceRef: null,
        idempotencyKeyHash: null,
        masked: true
      },
      detail: null,
      permission: "OPS_ERROR_LOG_READ",
      permissionMode: "ROLE_PERMISSION"
    });

    expect(result.item.id).toBe("ops-error-2");
    expect(result.detail).toBeNull();
    expect(result.item.payloadRef).toBe("r2://sanitized/payload");
    expect(result.item.traceRef).toBeNull();
  });

  it("keeps empty values from successful ops log responses distinct from integration pending placeholders", () => {
    const result = mapOpsErrorLogListResponse({
      items: [
        {
          id: "ops-empty-1",
          occurredAt: "2026-08-29T04:00:00.000Z",
          severity: "info",
          status: "open",
          source: "admin_api",
          service: "admin_api",
          errorCode: "contract_violation",
          messageSummary: "sanitized summary",
          requestId: null,
          retryable: null,
          masked: null
        }
      ]
    });

    expect(result.items[0]).toMatchObject({
      id: "ops-empty-1",
      requestId: "없음",
      retryable: "없음",
      masked: "없음"
    });
    expect(emptyOpsErrorLogListItem().requestId).toBe("연동 대기");
  });

  it("prepares a no-data row for successful empty ops log lists", () => {
    expect(noDataOpsErrorLogListItem()).toMatchObject({
      id: "데이터 없음",
      occurredAt: "없음",
      requestId: "없음",
      retryable: "없음",
      masked: "없음"
    });
  });

  it("keeps filters aligned with server contract and preserves empty fallback", () => {
    expect(opsErrorLogReadFilterFields).toEqual([
      "phase", "contractVersion", "category", "q", "serverVersion", "appVersion", "appVersionCode", "occurredFrom", "occurredTo",
      "environment",
      "severity",
      "status",
      "source",
      "service",
      "errorCode",
      "requestId",
      "accountId",
      "runId",
      "rewardPreviewId",
      "rewardClaimId",
      "offlineRewardClaimId",
      "gachaBatchId",
      "gachaBannerId",
      "storyProgressId",
      "integrityEventId",
      "supportTicketId",
      "limit",
      "cursor"
    ]);
    expect(mapOpsErrorLogListResponse(null).items).toEqual([]);
    expect(emptyOpsErrorLogListItem().id).toBe("연동 대기");
    expect(emptyOpsErrorLogDetail("ops-missing").item.id).toBe("ops-missing");
  });
});
