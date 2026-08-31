import { afterEach, describe, expect, it, vi } from "vitest";
import { AdminApiError, adminApi } from "./client";

describe("admin web API client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("posts player notes with a JSON body", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json(
        {
          note: {
            accountId: "account-1",
            action: "PLAYER_NOTE_CREATE",
            result: "SUCCESS",
            requestId: "req-note-1"
          }
        },
        { status: 201 }
      )
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.createPlayerNote(
      "account-1",
      "Reviewed abnormal Training Core spread."
    );

    expect(result.note.accountId).toBe("account-1");
    expect(fetchSpy).toHaveBeenCalledWith("/api/players/account-1/notes", {
      method: "POST",
      headers: {
        accept: "application/json",
        "content-type": "application/json"
      },
      body: JSON.stringify({ note: "Reviewed abnormal Training Core spread." })
    });
  });

  it("reads player notes from the account note endpoint", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        items: [
          {
            id: "audit-1",
            accountId: "account-1",
            note: "Reviewed abnormal Training Core spread.",
            authorId: "principal-1",
            authorDisplayName: "Owner",
            requestId: "req-note-list-1",
            createdAt: "2026-08-26T03:30:00.000Z"
          }
        ]
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.playerNotes("account-1");

    expect(result.items[0]?.note).toBe("Reviewed abnormal Training Core spread.");
    expect(fetchSpy).toHaveBeenCalledWith("/api/players/account-1/notes", {
      headers: {
        accept: "application/json"
      }
    });
  });

  it("preserves normalized Admin API error status and code for actionable UI failures", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({ error: "ADMIN_PLAYER_NOT_FOUND", message: "Player was not found." }, { status: 404 })
    );
    vi.stubGlobal("fetch", fetchSpy);

    await expect(adminApi.playerDetail("missing-account")).rejects.toMatchObject({
      name: "AdminApiError",
      status: 404,
      errorCode: "ADMIN_PLAYER_NOT_FOUND",
      upstreamMessage: "Player was not found.",
      path: "/api/players/missing-account"
    });
  });

  it("preserves proxy error codes for environment and credential diagnosis", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json(
        { error: { code: "ADMIN_API_BASE_URL_REQUIRED", message: "Admin API base URL is missing." } },
        { status: 500 }
      )
    );
    vi.stubGlobal("fetch", fetchSpy);

    const error = await adminApi.runDetail("run-1").catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(AdminApiError);
    expect(error).toMatchObject({
      status: 500,
      errorCode: "ADMIN_API_BASE_URL_REQUIRED"
    });
  });

  it("classifies non-JSON successful responses as proxy or Access response mismatch", async () => {
    const fetchSpy = vi.fn(async () =>
      new Response("<html>Access login</html>", {
        status: 200,
        headers: { "content-type": "text/html" }
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const error = await adminApi.runDetail("run-html").catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(AdminApiError);
    expect(error).toMatchObject({
      status: 200,
      errorCode: "ADMIN_API_NON_JSON_RESPONSE",
      path: "/api/runs/run-html"
    });
  });

  it("reads expanded player detail without failing legacy training core length assumptions", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        accountId: "account-dev",
        displayName: "DEV Account",
        status: "ACTIVE",
        createdAt: "2026-08-31T08:00:00.000Z",
        progression: {
          operatorExp: 0,
          syncLevel: 1,
          coreLevelSpread: 0,
          trainingCore: [],
          operators: []
        },
        auth: {
          providerNames: ["GOOGLE", "GUEST"],
          hasGoogleLink: true,
          activeSessionCount: 1,
          lastLoginAt: "2026-08-31T08:05:00.000Z",
          concurrentUsers: null,
          concurrentUsersPrecision: "UNAVAILABLE"
        },
        opsErrors: [],
        supportContext: {
          readOnly: true,
          mutationEndpointsEnabled: false,
          source: "admin_player_detail"
        }
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.playerDetail("account-dev");

    expect(result.accountId).toBe("account-dev");
    expect(result.progression.trainingCore).toEqual([]);
    expect(result.launchEvidence.authSession.items.find((item) => item.field === "hasGoogleLink")?.value).toBe("연동됨");
  });

  it("reads expanded run detail without failing legacy snapshot shape assumptions", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        runId: "run-dev",
        accountId: "account-dev",
        startedAt: "2026-08-31T08:00:00.000Z",
        completedAt: "2026-08-31T08:03:00.000Z",
        result: "FAILED",
        riskTier: "V",
        balanceVersion: "7",
        currentActiveBalanceVersion: "8",
        clientBuild: "build-dev",
        contentVersion: "4",
        validatorRuntimeRevision: "12",
        snapshot: { operatorIds: ["op_vex"] },
        operationId: "raid_raider_king_engine",
        outcome: "FAILURE",
        rewardPreviewId: "reward-preview-1",
        encounterType: "RAID_BOSS",
        allowedOperatorSlots: 5,
        startAugmentPackageRef: "augment-pkg",
        startTacticPackageRef: "tactic-pkg",
        staminaRefund: {
          status: "COMMITTED",
          resource: "STAMINA",
          reason: "STORY_RAID_FAILURE",
          policyId: "act1_story_raid_failure_stamina_refund_v1",
          serverOwnedMutation: true,
          fuelLedgerReason: "STORY_RAID_FAILURE_REFUND",
          fuelLedgerRefs: []
        },
        supportContext: {
          readOnly: true,
          mutationEndpointsEnabled: false,
          source: "admin_run_detail"
        }
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.runDetail("run-dev");

    expect(result.runId).toBe("run-dev");
    expect(result.snapshot.operatorIds).toEqual(["op_vex"]);
    expect(result.snapshot.operators).toEqual([]);
    expect(result.launchEvidence.encounterType).toBe("RAID_BOSS");
    expect(result.launchEvidence.staminaRefund?.status).toBe("COMMITTED");
  });

  it("reads audit logs with optional filters", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        items: [
          {
            id: "audit-1",
            principalId: "principal-1",
            principalDisplayName: "Owner",
            role: "OWNER",
            permission: "ACCOUNT_NOTE_WRITE",
            action: "PLAYER_NOTE_CREATE",
            resourceType: "account",
            resourceId: "account-1",
            requestId: "req-note-1",
            confirmationId: null,
            payloadHash: "sha256:abc",
            changeSummary: { note: "Reviewed abnormal Training Core spread." },
            result: "SUCCESS",
            errorCode: null,
            createdAt: "2026-08-26T03:30:00.000Z"
          }
        ]
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.auditLogs({
      action: "PLAYER_NOTE_CREATE",
      resourceId: "account-1"
    });

    expect(result.items[0]?.changeSummary).toEqual({
      note: "Reviewed abnormal Training Core spread."
    });
    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/audit?action=PLAYER_NOTE_CREATE&resourceId=account-1",
      {
        headers: {
          accept: "application/json"
        }
      }
    );
  });

  it("reads integrity events with optional filters", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        items: [
          {
            id: "event-1",
            accountId: "account-1",
            runId: "run-1",
            classification: "SOFT_ANOMALY",
            category: "DAMAGE_ANALYSIS",
            evidenceCode: "DAMAGE_SPIKE",
            evidence: { maxDamageRatio: 2.4 },
            reviewState: "OPEN",
            assignedTo: null,
            createdAt: "2026-08-26T04:00:00.000Z"
          }
        ]
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.integrityEvents({
      accountId: "account-1",
      classification: "SOFT_ANOMALY",
      reviewState: "OPEN"
    });

    expect(result.items[0]?.evidenceCode).toBe("DAMAGE_SPIKE");
    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/integrity/events?accountId=account-1&classification=SOFT_ANOMALY&reviewState=OPEN",
      {
        headers: {
          accept: "application/json"
        }
      }
    );
  });

  it("reads suspicious players with optional filters", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        items: [
          {
            accountId: "account-1",
            displayName: "Kyle Account",
            priority: "HIGH",
            openSignals: 3,
            hardViolations: 1,
            suspiciousRuns: 2,
            lastDetectionAt: "2026-08-26T04:00:00.000Z",
            primaryReason: "DAMAGE_SPIKE",
            reviewStatus: "REVIEWING",
            currentEnforcement: "NONE",
            reviewOwner: "operator-1"
          }
        ]
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.suspiciousPlayers({ reviewStatus: "REVIEWING" });

    expect(result.items[0]?.priority).toBe("HIGH");
    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/integrity/suspicious-players?reviewStatus=REVIEWING",
      {
        headers: {
          accept: "application/json"
        }
      }
    );
  });

  it("reads suspicious player detail by account id", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        account: {
          accountId: "account-1",
          displayName: "Kyle Account",
          status: "ACTIVE",
          createdAt: "2026-08-25T12:00:00.000Z"
        },
        summary: {
          priority: "CRITICAL",
          openSignals: 2,
          hardViolations: 1,
          suspiciousRuns: 2,
          lastDetectionAt: "2026-08-26T04:00:00.000Z",
          primaryReason: "IMPOSSIBLE_DAMAGE",
          reviewStatus: "REVIEWING",
          currentEnforcement: "REWARD_HELD",
          reviewOwner: "operator-1"
        },
        riskSignals: [],
        suspiciousRuns: [],
        notes: [],
        decisions: []
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.suspiciousPlayerDetail("account-1");

    expect(result.summary.currentEnforcement).toBe("REWARD_HELD");
    expect(fetchSpy).toHaveBeenCalledWith("/api/integrity/suspicious-players/account-1", {
      headers: {
        accept: "application/json"
      }
    });
  });

  it("reads story progress list with read-only filters", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        items: [
          {
            accountId: "account-1",
            actId: "act_01_wasteland_railway",
            operationId: "opn_02_first_signal",
            storyProgress: {
              highestStoryProgressId: "opn_02_first_signal",
              highestClearedOperationId: "opn_02_first_signal",
              order: 2
            },
            clearedOperationIds: ["opn_01_last_departure", "opn_02_first_signal"],
            unlockedOperations: ["opn_03_water_stop"],
            narrative: {
              narrativeVersion: "act1.2026-08-27.001",
              contentStatus: "CANDIDATE",
              localizationStatus: "PENDING",
              fixtureStatus: "PARTIAL",
              missingContentStatus: "UNKNOWN"
            },
            supportContext: {
              source: "account_story_progress",
              readOnly: true
            },
            updatedAt: "2026-08-28T05:00:00.000Z"
          }
        ],
        permission: "STORY_PROGRESS_READ",
        permissionMode: "ROLE_PERMISSION",
        nextCursor: "cursor-2"
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.storyProgressList({
      accountId: "account-1",
      actId: "act_01_wasteland_railway",
      highestStoryProgressId: "opn_02_first_signal",
      limit: "25"
    });

    expect(result.items[0]?.highestStoryProgressId).toBe("opn_02_first_signal");
    expect(result.items[0]?.missingContentStatus).toBe("UNKNOWN");
    expect(result.permissionMode).toBe("ROLE_PERMISSION");
    expect(result.items[0]?.supportContext.permissionMode).toBe("ROLE_PERMISSION");
    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/story-progress?accountId=account-1&actId=act_01_wasteland_railway&highestStoryProgressId=opn_02_first_signal&limit=25",
      {
        headers: {
          accept: "application/json"
        }
      }
    );
  });

  it("reads story progress detail by account id and preserves no-row none defaults", async () => {
    const fetchSpy = vi.fn(async () => Response.json(null));
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.storyProgressDetail("account-no-row");

    expect(result.accountId).toBe("account-no-row");
    expect(result.highestStoryProgressId).toBe("none");
    expect(result.supportContext.readOnly).toBe(true);
    expect(fetchSpy).toHaveBeenCalledWith("/api/story-progress/account-no-row", {
      headers: {
        accept: "application/json"
      }
    });
  });

  it("reads offline reward list with optional read-only filters", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        permission: "OFFLINE_REWARD_READ",
        permissionMode: "ROLE_PERMISSION",
        items: [],
        nextCursor: "cursor-2"
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.offlineRewardList({
      accountId: "account-1",
      storyProgressId: "none",
      limit: "25"
    });

    expect(result.permission).toBe("OFFLINE_REWARD_READ");
    expect(result.permissionMode).toBe("ROLE_PERMISSION");
    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/offline-rewards?accountId=account-1&storyProgressId=none&limit=25",
      {
        headers: {
          accept: "application/json"
        }
      }
    );
  });

  it("reads offline reward detail by account id and preserves read-only support flags", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        accountId: "account-1",
        displayName: "Kyle Account",
        accountStatus: "ACTIVE",
        storyProgressId: "none",
        narrativeVersion: "act1.2026-08-27.001",
        offlineRewardState: {
          source: "account_offline_reward_state",
          lastOfflineRewardClaimAt: "2026-08-28T00:00:00.000Z",
          lastStoryProgressId: "none",
          lastBalanceConfigVersion: null,
          lastNarrativeVersion: "act1.2026-08-27.001",
          lastPreviewedAt: null,
          revision: 0,
          updatedAt: "2026-08-28T00:00:00.000Z"
        },
        lastClaim: null,
        supportContext: {
          readOnly: true,
          claimMutationEnabled: false,
          mutationEndpointsEnabled: false
        }
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.offlineRewardDetail("account-1");

    expect(result.accountId).toBe("account-1");
    expect(result.supportContext.claimMutationEnabled).toBe(false);
    expect(fetchSpy).toHaveBeenCalledWith("/api/offline-rewards/account-1", {
      headers: {
        accept: "application/json"
      }
    });
  });

  it("reads support account summary by account id as read-only Admin API data", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        permission: "SUPPORT_READ",
        permissionMode: "ROLE_PERMISSION",
        account: {
          accountId: "account-1",
          displayName: "Kyle Account",
          accountStatus: "ACTIVE"
        },
        auth: { providerNames: ["guest"], activeSessionCount: 1 },
        recentRuns: [],
        runRewards: [],
        offlineReward: null,
        storyProgress: null,
        gacha: null,
        idempotency: null,
        integrity: null,
        opsErrors: [],
        supportNotes: { safeTemplateHints: ["server_record_checked"] },
        supportContext: {
          readOnly: true,
          repairEnabled: false,
          mutationEndpointsEnabled: false
        }
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.supportAccountSummary("account-1");

    expect(result.permission).toBe("SUPPORT_READ");
    expect(result.account.accountId).toBe("account-1");
    expect(result.supportContext.mutationEndpointsEnabled).toBe(false);
    expect(fetchSpy).toHaveBeenCalledWith("/api/support/accounts/account-1", {
      headers: {
        accept: "application/json"
      }
    });
  });

  it("reads ops error logs with read-only filters", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        permission: "OPS_ERROR_LOG_READ",
        permissionMode: "ROLE_PERMISSION",
        items: [],
        nextCursor: null
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.opsErrorLogs({
      accountId: "account-1",
      severity: "critical",
      status: "open",
      limit: "25"
    });

    expect(result.permission).toBe("OPS_ERROR_LOG_READ");
    expect(result.permissionMode).toBe("ROLE_PERMISSION");
    expect(fetchSpy).toHaveBeenCalledWith(
      "/api/ops/error-logs?accountId=account-1&severity=critical&status=open&limit=25",
      {
        headers: {
          accept: "application/json"
        }
      }
    );
  });

  it("reads ops error log detail by id with null detail support", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        permission: "OPS_ERROR_LOG_READ",
        permissionMode: "ROLE_PERMISSION",
        item: {
          id: "ops-error-1",
          occurredAt: "2026-08-29T01:00:00.000Z",
          severity: "error",
          status: "open",
          source: "admin_api",
          service: "admin_api",
          errorCode: "contract_violation",
          messageSummary: "sanitized summary",
          masked: true
        },
        detail: null
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.opsErrorLogDetail("ops-error-1");

    expect(result.permission).toBe("OPS_ERROR_LOG_READ");
    expect(result.item.id).toBe("ops-error-1");
    expect(result.detail).toBeNull();
    expect(fetchSpy).toHaveBeenCalledWith("/api/ops/error-logs/ops-error-1", {
      headers: {
        accept: "application/json"
      }
    });
  });

  it("reads launch dashboard summary from the Admin read-only endpoint", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        permission: "SUPPORT_READ",
        permissionMode: "ROLE_PERMISSION",
        activeSessions: 2,
        concurrentUsers: null,
        concurrentUsersPrecision: "UNAVAILABLE",
        lastLoginAtLatest: null,
        supportReviewNeededCount: 0,
        accountRecoveryCandidateCount: 0,
        integrityHighRiskCount: 0,
        rewardClaimFailureCount: 0,
        offlineRewardIssueCount: 0,
        gachaSupportIssueCount: 0,
        duplicateReplayCount: 0,
        criticalOpenOpsErrorCount: 0,
        unacknowledgedCoreFailureCount: 0
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.dashboardLaunchSummary();

    expect(result.activeSessions).toBe("2");
    expect(result.concurrentUsers).toBe("정밀 동접자 백로그 (UNAVAILABLE)");
    expect(fetchSpy).toHaveBeenCalledWith("/api/dashboard/launch-summary", {
      headers: {
        accept: "application/json"
      }
    });
  });

  it("reads gacha banner, pity, pull batch list, and detail read-only endpoints", async () => {
    const fetchSpy = vi
      .fn()
      .mockResolvedValueOnce(
        Response.json({
          permission: "SUPPORT_READ",
          permissionMode: "ROLE_PERMISSION",
          activeBannerId: "banner-1",
          items: [],
          nextCursor: null
        })
      )
      .mockResolvedValueOnce(
        Response.json({
          permission: "SUPPORT_READ",
          permissionMode: "ROLE_PERMISSION",
          accountId: "account-1",
          bannerId: "banner-1",
          pityState: { totalPullCount: 0, elitePityCount: 0, pickupElitePityCount: 0, rareOrHigherPityCount: 0 },
          detailAvailable: false
        })
      )
      .mockResolvedValueOnce(
        Response.json({
          permission: "SUPPORT_READ",
          permissionMode: "ROLE_PERMISSION",
          items: [],
          nextCursor: null
        })
      )
      .mockResolvedValueOnce(
        Response.json({
          item: { batchId: "batch-1" },
          detailAvailable: true
        })
      );
    vi.stubGlobal("fetch", fetchSpy);

    await adminApi.gachaBanners();
    await adminApi.gachaAccountPity("account-1", "banner-1");
    await adminApi.gachaPullBatches({ accountId: "account-1", bannerId: "banner-1", limit: "10" });
    const detail = await adminApi.gachaPullBatchDetail("batch-1");

    expect(detail.detailAvailable).toBe(true);
    expect(fetchSpy).toHaveBeenNthCalledWith(1, "/api/gacha/banners", {
      headers: { accept: "application/json" }
    });
    expect(fetchSpy).toHaveBeenNthCalledWith(2, "/api/gacha/accounts/account-1/pity?bannerId=banner-1", {
      headers: { accept: "application/json" }
    });
    expect(fetchSpy).toHaveBeenNthCalledWith(3, "/api/gacha/pull-batches?accountId=account-1&bannerId=banner-1&limit=10", {
      headers: { accept: "application/json" }
    });
    expect(fetchSpy).toHaveBeenNthCalledWith(4, "/api/gacha/pull-batches/batch-1", {
      headers: { accept: "application/json" }
    });
  });

  it("reads ops error log aggregate summary", async () => {
    const fetchSpy = vi.fn(async () =>
      Response.json({
        permission: "OPS_ERROR_LOG_READ",
        permissionMode: "ROLE_PERMISSION",
        recentErrorCount: 9,
        warningCount: 2,
        criticalCount: 3,
        retryableCount: 4,
        maskedCount: 8,
        openCount: 5,
        acknowledgedCount: 1,
        latestCriticalAt: null
      })
    );
    vi.stubGlobal("fetch", fetchSpy);

    const result = await adminApi.opsErrorLogSummary();

    expect(result.criticalCount).toBe("3");
    expect(result.latestCriticalAt).toBe("없음");
    expect(fetchSpy).toHaveBeenCalledWith("/api/ops/error-logs/summary", {
      headers: {
        accept: "application/json"
      }
    });
  });
});
