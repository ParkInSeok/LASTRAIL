import { describe, expect, it, vi } from "vitest";
import app from "../../apps/admin-api/src/index";

type QueryResult = Record<string, unknown> | Record<string, unknown>[] | null;

const operatorPrincipal = {
  id: "principal-operator",
  access_subject: "access|operator",
  role: "OPERATOR",
  status: "ACTIVE",
  display_name: "Operator",
};

const viewerPrincipal = {
  ...operatorPrincipal,
  role: "VIEWER",
};

function createEnv(results: QueryResult[]): Env {
  const queue = [...results];

  return {
    ENVIRONMENT: "LOCAL",
    ALLOWED_ADMIN_ORIGINS: "http://localhost:5173,http://127.0.0.1:5173",
    ADMIN_AUTH_MODE: "LOCAL_HEADER",
    ADMIN_ACCESS_ISSUER: "",
    ADMIN_ACCESS_AUD: "",
    ADMIN_DELEGATED_ACCESS_ISSUER: "",
    ADMIN_DELEGATED_ACCESS_AUD: "",
    ADMIN_JOBS_QUEUE: {} as Queue,
    DB: {
      prepare() {
        return {
          bind() {
            return {
              first: async () => {
                const result = queue.shift();
                if (Array.isArray(result)) return result[0] ?? null;
                return result ?? null;
              },
              all: async () => {
                const result = queue.shift();
                return { results: Array.isArray(result) ? result : result === null || result === undefined ? [] : [result] };
              },
              run: async () => {
                throw new Error("Launch read models must be read-only.");
              },
            };
          },
        };
      },
    } as unknown as D1Database,
  };
}

const opsErrorRow = {
  id: "ops_launch_1",
  occurred_at: 1787658200000,
  environment: "DEV",
  service: "game_api",
  severity: "critical",
  error_code: "gacha_state_conflict",
  message_summary: "Gacha state conflict detected.",
  status: "open",
  source: "game_api",
  request_id: "req-gacha",
  account_id: "account-1",
  run_id: null,
  reward_preview_id: null,
  reward_claim_id: null,
  offline_reward_claim_id: null,
  gacha_batch_id: "batch-1",
  gacha_banner_id: "banner-1",
  story_progress_id: null,
  integrity_event_id: null,
  support_ticket_id: null,
  idempotency_operation: "gacha_pull",
  idempotency_key_hash: "sha256:gacha-key",
  correlation_json: "{}",
  sanitized_context_json: "{}",
  retryable: 0,
  masked: 1,
  payload_ref: "r2://safe-ref",
  trace_ref: "trace-safe",
  resolved_at: null,
  resolved_by: null,
  resolution_note: null,
  created_at: 1787658200000,
};

describe("admin-api launch read-only aggregate models", () => {
  it("returns dashboard launch summary with nullable session semantics and issue counts", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(1787659000000);
    try {
      const response = await app.request(
        "/v1/admin/dashboard/launch-summary",
        { headers: { "Cf-Access-Subject": "access|operator" } },
        createEnv([
          operatorPrincipal,
          { active_session_count: 2, latest_session_created_at: 1787658700000 },
          {
            dau_7d: 12,
            dau_30d: 31,
            signups_7d: 4,
            signups_30d: 9,
            ad_rewards_verified_7d: 5,
            ad_rewards_verified_30d: 15,
            ad_rewards_granted_7d: 3,
            ad_rewards_granted_30d: 11,
          },
          { support_review_needed_count: 3 },
          { integrity_high_risk_count: 4 },
          { reward_claim_failure_count: 1, offline_reward_issue_count: 2, gacha_support_issue_count: 1, critical_open_ops_error_count: 5, unacknowledged_core_failure_count: 6 },
          { duplicate_replay_count: 7 },
          { account_recovery_candidate_count: 1 },
          { accounts_with_story_progress_count: 8, version_mismatch_count: 2 },
          [
            { highest_story_progress_id: "opn_03_water_stop", account_count: 5 },
            { highest_story_progress_id: "opn_06_broken_line", account_count: 3 },
          ],
        ]),
      );

      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toMatchObject({
        permission: "SUPPORT_READ",
        permissionMode: "ROLE_PERMISSION",
        activeSessions: 2,
        concurrentUsers: null,
        concurrentUsersPrecision: "UNAVAILABLE",
        lastLoginAtLatest: "2026-08-25T11:51:40.000Z",
        lastLoginAgeBuckets: {
          latestAgeSeconds: 300,
          exactConcurrentUsersAvailable: false,
        },
        dailySourceMetrics: {
          timezone: "Asia/Seoul",
          windowSemantics: "KST_CALENDAR_DAY_INCLUSIVE",
          dauSource: "auth_sessions.created_at_distinct_account",
          signupSource: "accounts.created_at",
          adRewardSource: "ad_reward_transactions.status",
          passSource: "UNAVAILABLE_NO_SOURCE_TABLE",
          sevenDay: {
            dau: 12,
            signups: 4,
            adRewardsVerified: 5,
            adRewardsGranted: 3,
            passPurchases: null,
          },
          thirtyDay: {
            dau: 31,
            signups: 9,
            adRewardsVerified: 15,
            adRewardsGranted: 11,
            passPurchases: null,
          },
        },
        supportReviewNeededCount: 3,
        accountRecoveryCandidateCount: 1,
        integrityHighRiskCount: 4,
        rewardClaimFailureCount: 1,
        offlineRewardIssueCount: 2,
        gachaSupportIssueCount: 1,
        duplicateReplayCount: 7,
        criticalOpenOpsErrorCount: 5,
        unacknowledgedCoreFailureCount: 6,
        accountsWithStoryProgressCount: 8,
        highestProgressDistribution: [
          { storyProgressId: "opn_03_water_stop", order: 3, accountCount: 5 },
          { storyProgressId: "opn_06_broken_line", order: 6, accountCount: 3 },
        ],
        contentMissingCount: 0,
        localizationPendingCount: 7,
        fixturePendingCount: 7,
        versionMismatchCount: 2,
        latestNarrativeVersion: "act1.2026-08-30.001",
        serverCalculatedAt: "2026-08-25T11:56:40.000Z",
      });
    } finally {
      vi.useRealTimers();
    }
  });

  it("returns zero/null dashboard semantics when there is no data", async () => {
    const response = await app.request(
      "/v1/admin/dashboard/launch-summary",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([operatorPrincipal, null, null, null, null, null, null, null, null, []]),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      activeSessions: 0,
      lastLoginAtLatest: null,
      dailySourceMetrics: {
        sevenDay: {
          dau: 0,
          signups: 0,
          adRewardsVerified: 0,
          adRewardsGranted: 0,
          passPurchases: null,
        },
        thirtyDay: {
          dau: 0,
          signups: 0,
          adRewardsVerified: 0,
          adRewardsGranted: 0,
          passPurchases: null,
        },
        passSource: "UNAVAILABLE_NO_SOURCE_TABLE",
      },
      supportReviewNeededCount: 0,
      accountRecoveryCandidateCount: 0,
      integrityHighRiskCount: 0,
      rewardClaimFailureCount: 0,
      offlineRewardIssueCount: 0,
      gachaSupportIssueCount: 0,
      duplicateReplayCount: 0,
      criticalOpenOpsErrorCount: 0,
      unacknowledgedCoreFailureCount: 0,
      accountsWithStoryProgressCount: 0,
      highestProgressDistribution: [],
      contentMissingCount: 0,
      localizationPendingCount: 7,
      fixturePendingCount: 7,
      versionMismatchCount: 0,
      latestNarrativeVersion: "act1.2026-08-30.001",
    });
  });

  it("returns ops error aggregate summary without raw detail", async () => {
    const response = await app.request(
      "/v1/admin/ops/error-logs/summary",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([
        operatorPrincipal,
        {
          recent_error_count: 9,
          warning_count: 2,
          critical_count: 3,
          retryable_count: 4,
          masked_count: 8,
          open_count: 5,
          acknowledged_count: 1,
          latest_critical_at: 1787658200000,
        },
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "OPS_ERROR_LOG_READ",
      permissionMode: "ROLE_PERMISSION",
      recentErrorCount: 9,
      warningCount: 2,
      criticalCount: 3,
      retryableCount: 4,
      maskedCount: 8,
      openCount: 5,
      acknowledgedCount: 1,
      latestCriticalAt: "2026-08-25T11:43:20.000Z",
    });
  });

  it("denies viewer access to ops error aggregate summary", async () => {
    const response = await app.request(
      "/v1/admin/ops/error-logs/summary",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([viewerPrincipal]),
    );

    expect(response.status).toBe(403);
  });
});

describe("admin-api gacha read-only support model", () => {
  it("lists launch banner support metadata without raw pool weights", async () => {
    const response = await app.request(
      "/v1/admin/gacha/banners",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([
        operatorPrincipal,
        [
          {
            id: "banner-1",
            banner_type: "PICKUP",
            status: "ACTIVE",
            balance_config_version: 1,
            pickup_operator_id: "op_vex",
            starts_at: 1787650000000,
            ends_at: null,
            pity_config_json: JSON.stringify({ rareOrHigherGuarantee: 10, eliteGuarantee: 30, pickupEliteGuarantee: 60 }),
            pool_snapshot_json: JSON.stringify({ cost: { itemId: "cur_command_ticket", quantity: 1 }, pullCountOptions: [1, 10], results: [{ id: "hidden-weight", weight: 999 }] }),
            created_at: 1787650000000,
            updated_at: 1787650000000,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      permission: "SUPPORT_READ",
      permissionMode: "ROLE_PERMISSION",
      activeBannerId: "banner-1",
      items: [
        {
          bannerId: "banner-1",
          bannerType: "PICKUP",
          status: "ACTIVE",
          pickupOperatorId: "op_vex",
          pullCostItemId: "cur_command_ticket",
          pullCountOptions: [1, 10],
          pityThresholds: {
            rareOrHigher: 10,
            elite: 30,
            pickupElite: 60,
          },
          guaranteeTypes: ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"],
        },
      ],
    });
    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain("weight");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("rawRng");
  });

  it("returns account pity state for a banner with stable empty defaults", async () => {
    const response = await app.request(
      "/v1/admin/gacha/accounts/account-1/pity?bannerId=banner-1",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([
        operatorPrincipal,
        {
          account_id: "account-1",
          banner_id: "banner-1",
          total_pull_count: 12,
          elite_pity_count: 12,
          pickup_elite_pity_count: 12,
          rare_or_higher_pity_count: 2,
          revision: 3,
          last_batch_id: "batch-1",
          updated_at: 1787658200000,
        },
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      permission: "SUPPORT_READ",
      accountId: "account-1",
      bannerId: "banner-1",
      pityState: {
        totalPullCount: 12,
        elitePityCount: 12,
        pickupElitePityCount: 12,
        rareOrHigherPityCount: 2,
        revision: 3,
        lastBatchId: "batch-1",
      },
      detailAvailable: true,
    });
  });

  it("lists gacha pull batches with replay/cost/pity/result shape", async () => {
    const response = await app.request(
      "/v1/admin/gacha/pull-batches?accountId=account-1&bannerId=banner-1&limit=10",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([
        operatorPrincipal,
        [
          {
            batch_id: "batch-1",
            account_id: "account-1",
            banner_id: "banner-1",
            pull_count: 10,
            cost_item_id: "cur_command_ticket",
            cost_quantity: 10,
            pre_pity_json: JSON.stringify({ totalPullCount: 0 }),
            post_pity_json: JSON.stringify({ totalPullCount: 10 }),
            result_summary_json: JSON.stringify({ responseResults: [{ resultId: "op_vex", pullIndex: 1, guaranteeType: "PICKUP_ELITE_60" }] }),
            created_at: 1787658200000,
            idempotency_replay_known: 1,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      items: [
        {
          batchId: "batch-1",
          accountId: "account-1",
          bannerId: "banner-1",
          resultStatus: "ACCEPTED",
          idempotencyReplayKnown: true,
          cost: { itemId: "cur_command_ticket", quantity: 10 },
          prePityState: { totalPullCount: 0 },
          postPityState: { totalPullCount: 10 },
          guaranteeTypes: ["PICKUP_ELITE_60"],
        },
      ],
    });
  });

  it("returns gacha batch detail with result refs and duplicate conversion metadata", async () => {
    const response = await app.request(
      "/v1/admin/gacha/pull-batches/batch-1",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([
        operatorPrincipal,
        {
          batch_id: "batch-1",
          account_id: "account-1",
          banner_id: "banner-1",
          pull_count: 1,
          cost_item_id: "cur_command_ticket",
          cost_quantity: 1,
          cost_ledger_id: "ledger-cost-1",
          pre_pity_json: JSON.stringify({ totalPullCount: 29 }),
          post_pity_json: JSON.stringify({ totalPullCount: 30 }),
          result_summary_json: JSON.stringify({ responseResults: [{ resultId: "op_vex", pullIndex: 1, guaranteeType: "ELITE_30" }] }),
          created_at: 1787658200000,
          idempotency_replay_known: 1,
        },
        [
          {
            result_id: "result-1",
            pull_sequence: 1,
            result_rarity: "ELITE",
            result_type: "ELITE_OPERATOR_SHARD",
            reward_id: "op_vex",
            is_pickup: 1,
            was_duplicate: 1,
            guarantee_type: "ELITE_30",
            duplicate_conversion_json: JSON.stringify({ itemId: "cur_operator_token", operatorId: "op_vex", quantity: 30 }),
            item_ledger_id: "ledger-token-1",
            progression_ledger_id: null,
          },
        ],
      ]),
    );

    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({
      item: {
        batchId: "batch-1",
        resultStatus: "ACCEPTED",
        idempotencyReplayKnown: true,
        cost: { itemId: "cur_command_ticket", quantity: 1, itemLedgerRef: "ledger-cost-1" },
        itemLedgerRefs: ["ledger-cost-1", "ledger-token-1"],
        operatorLedgerRefs: [],
        results: [
          {
            resultId: "result-1",
            pullSequence: 1,
            resultRarity: "ELITE",
            resultType: "ELITE_OPERATOR_SHARD",
            reward: { id: "op_vex" },
            isPickup: true,
            wasDuplicate: true,
            guaranteeType: "ELITE_30",
            duplicateConversion: {
              displayItemId: "cur_operator_token",
              persistedTokenPattern: "cur_operator_token:op_vex",
              quantity: 30,
            },
            itemLedgerRef: "ledger-token-1",
          },
        ],
        causeEvidence: {
          source: "gacha_pull_batch_server_records",
          accountId: "account-1",
          batchId: "batch-1",
          bannerId: "banner-1",
          resultStatus: "ACCEPTED",
          idempotencyReplayKnown: true,
          guaranteeTypes: ["ELITE_30"],
          prePityState: { totalPullCount: 29 },
          postPityState: { totalPullCount: 30 },
          duplicateResultCount: 1,
          duplicateConversionItems: [
            {
              displayItemId: "cur_operator_token",
              persistedTokenPattern: "cur_operator_token:op_vex",
              quantity: 30,
            },
          ],
          cost: { itemId: "cur_command_ticket", quantity: 1, itemLedgerRef: "ledger-cost-1" },
          resultLedgerRefs: {
            itemLedgerRefs: ["ledger-cost-1", "ledger-token-1"],
            operatorLedgerRefs: [],
          },
          rawProbabilityAvailable: false,
          fullWeightsAvailable: false,
          rngMaterialAvailable: false,
        },
      },
      detailAvailable: true,
    });
  });
});

describe("admin-api player and run launch support read models", () => {
  it("extends player detail with auth/session and recent ops evidence", async () => {
    vi.useFakeTimers();
    vi.setSystemTime(1787659000000);
    try {
      const response = await app.request(
        "/v1/admin/players/account-1",
        { headers: { "Cf-Access-Subject": "access|operator" } },
        createEnv([
          operatorPrincipal,
          {
            account_id: "account-1",
            display_name: "Kyle Account",
            status: "ACTIVE",
            created_at: 1787650000000,
            operator_exp: 1200,
          },
          [],
          [],
          [{ provider: "GOOGLE" }, { provider: "GUEST" }],
          {
            active_session_count: 2,
            latest_session_created_at: 1787658700000,
            latest_session_expires_at: 1787662600000,
            last_login_at: 1787658700000,
          },
          [opsErrorRow],
        ]),
      );

      expect(response.status).toBe(200);
      const body = await response.json();
      expect(body).toMatchObject({
        accountId: "account-1",
        auth: {
          identityProviders: ["GOOGLE", "GUEST"],
          providerNames: ["GOOGLE", "GUEST"],
          linkedProviders: ["GOOGLE", "GUEST"],
          primaryProvider: "GOOGLE",
          hasGoogleLink: true,
          activeSessionCount: 2,
          latestSessionAt: "2026-08-25T11:51:40.000Z",
          lastLoginAt: "2026-08-25T11:51:40.000Z",
          lastLoginAgeSeconds: 300,
          concurrentUsers: null,
          concurrentUsersPrecision: "UNAVAILABLE",
        },
        opsErrors: [
          {
            errorLogId: "ops_launch_1",
            severity: "critical",
            errorCode: "gacha_state_conflict",
            accountId: "account-1",
            gachaBatchId: "batch-1",
            idempotencyKeyHash: "sha256:gacha-key",
          },
        ],
        supportContext: {
          readOnly: true,
          mutationEndpointsEnabled: false,
          source: "admin_player_detail",
        },
      });
      const serialized = JSON.stringify(body);
      expect(serialized).not.toContain("accessToken");
      expect(serialized).not.toContain("refreshToken");
      expect(serialized).not.toContain("Authorization");
      expect(serialized).not.toContain("rawRng");
    } finally {
      vi.useRealTimers();
    }
  });

  it("returns run detail reward, raid, start package, and stamina refund evidence", async () => {
    const previewJson = {
      operationId: "raid_raider_king_engine",
      encounterType: "RAID_BOSS",
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossPhaseRefs: ["phase_chase", "phase_break", "phase_engine"],
      allowedOperatorSlots: 5,
      startAugmentPackageRef: "act1_full_start_augment_package_v1",
      startTacticPackageRef: "act1_full_start_tactic_package_v1",
      contentVersion: "act1.2026-08-27.001",
    };
    const finalPayload = {
      encounterType: "RAID_BOSS",
      selectedAugmentIds: ["aug_rail_guard"],
      tacticSelections: [{ selectedTacticId: "tactic_cover_fire" }],
      raidResult: {
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossDefeated: false,
        completedPhaseCount: 2,
        encounterDurationSeconds: 360,
      },
      staminaRefund: {
        status: "COMMITTED",
        refundType: "STAMINA",
        reason: "STORY_RAID_FAILURE",
        policyRef: "act1_story_raid_failure_stamina_refund_v1",
      },
    };
    const rewardPlan = {
      rewardLines: [],
      calculation: {
        appliedMultiplierLabel: "story raid failure refund only",
        staminaRefund: finalPayload.staminaRefund,
      },
    };

    const response = await app.request(
      "/v1/admin/runs/run-raid-1",
      { headers: { "Cf-Access-Subject": "access|operator" } },
      createEnv([
        operatorPrincipal,
        {
          run_id: "run-raid-1",
          account_id: "account-1",
          status: "COMPLETED",
          started_at: 1787650000000,
          finished_at: 1787650360000,
          risk: "V",
          balance_config_version: 7,
          current_active_balance_version: 8,
          build_id: "build-9",
          content_version: 4,
          validator_version: "validator-1",
          validator_runtime_revision: 12,
          snapshot_json: JSON.stringify({
            operatorIds: ["op_a", "op_b", "op_c", "op_d", "op_e"],
            runVersionSnapshot: {
              source: "server_accepted_run_start",
              clientInstalled: {
                versionCode: "100",
                platform: "android",
                channel: "internal",
              },
              clientAdmission: {
                status: "ACCEPTED",
                minimumVersionCode: "90",
                updateRequired: false,
              },
              serverAcceptedDeployment: {
                environment: "DEV",
                runtimeDeploymentId: "cf-runtime-deploy-admin",
              },
              contentRelease: {
                status: "APPROVED_COMPATIBLE",
                releaseId: "content_act1_001",
                contentVersion: "act1.2026-09-06.001",
                catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
              },
              capturedAt: "2026-08-25T11:40:00.000Z",
            },
          }),
          preview_json: JSON.stringify(previewJson),
        },
        {
          outcome: "FAILURE",
          cleared_waves: 0,
          payload_json: JSON.stringify(finalPayload),
          submitted_at: 1787650360000,
        },
        {
          reward_preview_id: "reward-preview-raid-1",
          preview_status: "ACTIVE",
          reward_claim_id: null,
          reward_plan_json: JSON.stringify(rewardPlan),
        },
        null,
        [{ id: "fuel-ledger-1", delta: 10, reason: "STORY_RAID_FAILURE_REFUND" }],
        { open_signals: 0, hard_violations: 0, primary_reason: null },
      ]),
    );

    expect(response.status).toBe(200);
    const body = await response.json();
    expect(body).toMatchObject({
      runId: "run-raid-1",
      operationId: "raid_raider_king_engine",
      riskTier: "V",
      outcome: "FAILURE",
      clearedWaves: 0,
      rewardPreviewId: "reward-preview-raid-1",
      rewardClaimId: null,
      appliedMultiplierLabel: "story raid failure refund only",
      calculationMetadataStatus: "AVAILABLE",
      firstClearConsumed: false,
      encounterType: "RAID_BOSS",
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossDefeated: false,
      completedPhaseCount: 2,
      encounterDurationSeconds: 360,
      allowedOperatorSlots: 5,
      startAugmentPackageRef: "act1_full_start_augment_package_v1",
      startTacticPackageRef: "act1_full_start_tactic_package_v1",
      selectedAugmentIds: ["aug_rail_guard"],
      selectedTacticIds: ["tactic_cover_fire"],
      staminaRefund: {
        status: "COMMITTED",
        resource: "STAMINA",
        reason: "STORY_RAID_FAILURE",
        policyId: "act1_story_raid_failure_stamina_refund_v1",
        serverOwnedMutation: true,
        fuelLedgerReason: "STORY_RAID_FAILURE_REFUND",
        fuelLedgerRefs: ["fuel-ledger-1"],
      },
      runVersionSnapshot: {
        source: "server_accepted_run_start",
        clientInstalled: {
          versionCode: "100",
          platform: "android",
          channel: "internal",
        },
        clientAdmission: {
          status: "ACCEPTED",
          minimumVersionCode: "90",
          updateRequired: false,
        },
        serverAcceptedDeployment: {
          environment: "DEV",
          runtimeDeploymentId: "cf-runtime-deploy-admin",
        },
        contentRelease: {
          status: "APPROVED_COMPATIBLE",
          releaseId: "content_act1_001",
          contentVersion: "act1.2026-09-06.001",
          catalogHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        },
      },
      integrityDisposition: {
        status: "CLEAR",
        openSignals: 0,
        hardViolations: 0,
        primaryReason: null,
      },
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        source: "admin_run_detail",
      },
    });
    const serialized = JSON.stringify(body);
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("probability");
    expect(serialized).not.toContain("fullWeights");
    expect(serialized).not.toContain("animationTimeline");
    expect(serialized).not.toContain("catalogUrl");
  });
});
