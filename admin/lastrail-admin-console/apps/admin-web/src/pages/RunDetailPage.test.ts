import { describe, expect, it } from "vitest";
import {
  actOneNarrativeCoverage,
  encounterTypePlaceholders,
  forbiddenRunCombatEvidenceExamplesOutOfScope,
  narrativeCoverageRowsForReadState,
  raidBossResultPlaceholders,
  runRiskRewardCardsForLaunchEvidence,
  runStoryNarrativeCardsForStoryProgressDetail,
  storyRaidLaunchResultPolicyPlaceholders,
  storyRaidPolicyCardsForLaunchEvidence,
  storyProgressDetailToReadContractPlaceholders,
  raidNarrativeObjectivePlaceholders,
  runStartMetadataEvidenceFrom,
  runStartPackageEvidencePlaceholders,
  runRiskRewardPlaceholders,
  runStoryNarrativePlaceholders,
  storyProgressReadContractFilters,
  storyProgressReadContractPlaceholders
} from "./RunDetailPage";

describe("RunDetailPage risk reward skeleton", () => {
  it("prepares run risk reward read-only placeholders without economy amounts", () => {
    const labels = runRiskRewardPlaceholders.map((placeholder) => placeholder.label);

    expect(labels).toEqual([
      "위험도",
      "작전 ID",
      "결과",
      "클리어 공세",
      "rewardPreviewId / rewardClaimId",
      "적용 배율",
      "first clear",
      "무결성 판정",
      "계산 메타데이터 상태"
    ]);
    expect(runRiskRewardPlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
  });

  it("keeps risk and calculation placeholders aligned with display labels", () => {
    const notes = runRiskRewardPlaceholders.map((placeholder) => placeholder.note);

    expect(notes).toContain("위험도 I~V 표시 후보");
    expect(notes).toContain("첫 클리어 보너스 표시 후보");
    expect(notes).toContain("서버 보상 미리보기 / Unity 진단 참고 신호 분리");
  });

  it("prepares story progress and narrative status placeholders without narrative text", () => {
    const labels = runStoryNarrativePlaceholders.map((placeholder) => placeholder.label);

    expect(labels).toEqual([
      "작전 ID",
      "스토리 진행도",
      "첫 클리어 상태",
      "미션 내러티브 상태",
      "현지화 데이터 상태",
      "누락 데이터 상태",
      "플레이어 문의 지원 문맥"
    ]);
    expect(runStoryNarrativePlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
    expect(runStoryNarrativePlaceholders.map((placeholder) => placeholder.note).join(" ")).not.toContain("대사");
  });

  it("aligns story progress placeholders with read-only admin query contract fields", () => {
    expect(storyProgressReadContractPlaceholders.map((placeholder) => placeholder.field)).toEqual([
      "accountId",
      "actId",
      "highestStoryProgressId",
      "highestClearedOperationId",
      "order",
      "clearedOperationIds",
      "unlockedOperations",
      "contentStatus",
      "localizationStatus",
      "fixtureStatus",
      "narrativeVersion",
      "missingContentStatus",
      "lastProgressRunId",
      "updatedAt",
      "permission",
      "permissionMode",
      "readOnly"
    ]);
    expect(storyProgressReadContractPlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
    expect(storyProgressReadContractPlaceholders.map((placeholder) => placeholder.note).join(" ")).toContain(
      "STORY_PROGRESS_READ"
    );
  });

  it("maps story progress detail data into run detail read-only cards", () => {
    const placeholders = storyProgressDetailToReadContractPlaceholders({
      accountId: "account-1",
      actId: "act_01_wasteland_railway",
      highestStoryProgressId: "opn_05_night_boarders",
      highestClearedOperationId: "opn_05_night_boarders",
      order: 5,
      clearedOperationIds: ["opn_01_last_departure", "opn_05_night_boarders"],
      unlockedOperations: ["opn_06_broken_line"],
      narrativeVersion: "act1.2026-08-27.001",
      contentStatus: "ready",
      localizationStatus: "localization_pending",
      fixtureStatus: "future_status",
      missingContentStatus: "content_missing",
      supportContext: {
        lastProgressRunId: "run-5",
        updatedAt: "2026-08-28T06:00:00.000Z",
        permission: "STORY_PROGRESS_READ",
        permissionMode: "ROLE_PERMISSION",
        readOnly: true
      }
    });

    expect(placeholders.map((placeholder) => [placeholder.field, placeholder.value])).toEqual([
      ["accountId", "account-1"],
      ["actId", "act_01_wasteland_railway"],
      ["highestStoryProgressId", "야간 승선전 / opn_05_night_boarders"],
      ["highestClearedOperationId", "야간 승선전 / opn_05_night_boarders"],
      ["order", "5"],
      ["clearedOperationIds", "마지막 출발 / opn_01_last_departure, 야간 승선전 / opn_05_night_boarders"],
      ["unlockedOperations", "끊어진 선로 / opn_06_broken_line"],
      ["contentStatus", "준비 완료 / ready"],
      ["localizationStatus", "현지화 대기 / localization_pending"],
      ["fixtureStatus", "future_status"],
      ["narrativeVersion", "act1.2026-08-27.001"],
      ["missingContentStatus", "콘텐츠 누락 / content_missing"],
      ["lastProgressRunId", "run-5"],
      ["updatedAt", "2026-08-28T06:00:00.000Z"],
      ["permission", "스토리 진행 조회 / STORY_PROGRESS_READ"],
      ["permissionMode", "ROLE_PERMISSION"],
      ["readOnly", "읽기 전용"]
    ]);
  });

  it("maps story progress detail data into the top narrative summary cards", () => {
    const cards = runStoryNarrativeCardsForStoryProgressDetail(
      {
        accountId: "account-1",
        actId: "act_01_wasteland_railway",
        highestStoryProgressId: "opn_03_water_stop",
        highestClearedOperationId: "opn_03_water_stop",
        order: 3,
        clearedOperationIds: ["opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop"],
        unlockedOperations: ["opn_04_scrapstar_show"],
        narrativeVersion: "act1.2026-08-27.001",
        contentStatus: "ready",
        localizationStatus: "localization_pending",
        fixtureStatus: "ready",
        missingContentStatus: "content_missing",
        supportContext: {
          lastProgressRunId: "run-opn-03",
          updatedAt: "2026-08-28T06:00:00.000Z",
          permission: "STORY_PROGRESS_READ",
          permissionMode: "ROLE_PERMISSION",
          readOnly: true
        }
      },
      {
        operationId: "opn_03_water_stop",
        outcome: "CLEARED",
        clearedWaves: 15,
        rewardPreviewId: null,
        rewardClaimId: null,
        appliedMultiplierLabel: null,
        calculationMetadataStatus: "ready",
        firstClearConsumed: true,
        integrityDisposition: {
          status: "CLEAR",
          openSignals: 0,
          hardViolations: 0,
          primaryReason: null
        },
        encounterType: "ORDINARY_15_WAVE",
        raidId: null,
        bossId: null,
        bossDefeated: null,
        completedPhaseCount: null,
        encounterDurationSeconds: null,
        allowedOperatorSlots: 5,
        bossPhaseRefs: [],
        startAugmentPackageRef: "augment_pkg_opn_03_v1",
        startTacticPackageRef: "tactic_pkg_opn_03_v1",
        selectedAugmentIds: [],
        selectedTacticIds: [],
        staminaRefund: null,
        supportContext: {
          readOnly: true,
          mutationEndpointsEnabled: false,
          repairEndpointsEnabled: false,
          source: "run_detail"
        }
      }
    );

    expect(cards.map((card) => [card.label, card.value])).toEqual([
      ["작전 ID", "급수 정차 / opn_03_water_stop"],
      ["스토리 진행도", "급수 정차 / opn_03_water_stop"],
      ["첫 클리어 상태", "첫 클리어 보너스"],
      ["미션 내러티브 상태", "준비 완료 / ready"],
      ["현지화 데이터 상태", "현지화 대기 / localization_pending"],
      ["누락 데이터 상태", "콘텐츠 누락 / content_missing"],
      ["플레이어 문의 지원 문맥", "run-opn-03 / 2026-08-28T06:00:00.000Z"]
    ]);
    expect(cards.every((card) => card.value !== "연동 대기")).toBe(true);
  });

  it("prepares story progress read-only filter skeleton fields", () => {
    expect(storyProgressReadContractFilters).toEqual([
      "accountId",
      "actId",
      "highestStoryProgressId",
      "highestClearedOperationId",
      "narrativeVersion",
      "contentStatus",
      "localizationStatus",
      "fixtureStatus",
      "missingContentStatus",
      "updatedFrom",
      "updatedTo",
      "limit",
      "cursor"
    ]);
  });

  it("lists Act 1 narrative coverage by stable mission id with placeholder states", () => {
    expect(actOneNarrativeCoverage.map((item) => item.missionId)).toEqual([
      "opn_01_last_departure",
      "opn_02_first_signal",
      "opn_03_water_stop",
      "opn_04_scrapstar_show",
      "opn_05_night_boarders",
      "opn_06_broken_line",
      "raid_raider_king_engine"
    ]);
    expect(actOneNarrativeCoverage.map((item) => item.displayName)).toEqual([
      "마지막 출발",
      "첫 신호",
      "급수 정차",
      "스크랩스타 쇼",
      "야간 승선전",
      "끊어진 선로",
      "약탈왕 기관차"
    ]);
    expect(actOneNarrativeCoverage.every((item) => item.coverageState === "연동 대기")).toBe(true);
    expect(actOneNarrativeCoverage.every((item) => item.localizationState === "연동 대기")).toBe(true);
    expect(actOneNarrativeCoverage.every((item) => item.narrativeVersionState === "연동 대기")).toBe(true);
    expect(actOneNarrativeCoverage.map((item) => item.statusHint).join(" ")).not.toContain("보상량");
  });

  it("maps narrative coverage API rows into the Run detail coverage table", () => {
    const rows = narrativeCoverageRowsForReadState({
      data: {
        permission: "STORY_PROGRESS_READ",
        permissionMode: "ROLE_PERMISSION",
        items: [
          {
            missionId: "opn_01_last_departure",
            displayName: "마지막 출발",
            contentStatus: "draft_exists",
            localizationStatus: "localization_pending",
            fixtureStatus: "fixture_pending",
            missingContentStatus: "version_pending",
            narrativeVersion: "없음",
            updatedAt: "2026-08-31T09:00:00.000Z",
            source: "narrative_coverage_read_model"
          }
        ],
        summary: {
          contentMissingCount: "0",
          localizationPendingCount: "1",
          fixturePendingCount: "1",
          versionMismatchCount: "0",
          latestNarrativeVersion: "없음"
        }
      },
      isLoading: false,
      error: null
    });

    expect(rows[0]).toMatchObject({
      missionId: "opn_01_last_departure",
      coverageState: "초안 존재 / draft_exists",
      localizationState: "현지화 대기 / localization_pending",
      fixtureState: "fixture 대기 / fixture_pending",
      narrativeVersionState: "없음"
    });
  });

  it("prepares raid optional objectives as read-only scoring candidates", () => {
    expect(raidNarrativeObjectivePlaceholders.map((objective) => objective.objectiveId)).toEqual([
      "prisoner_car_preserved",
      "broadcast_relay_captured",
      "supply_car_stabilized",
      "fuel_core_stable",
      "engine_integrity"
    ]);
    expect(raidNarrativeObjectivePlaceholders.every((objective) => objective.state === "연동 대기")).toBe(true);
    expect(raidNarrativeObjectivePlaceholders.every((objective) => objective.note.includes("read-only"))).toBe(true);
  });

  it("separates ordinary 15-wave runs from boss-only raid encounter evidence", () => {
    expect(encounterTypePlaceholders.map((placeholder) => placeholder.code)).toEqual([
      "ORDINARY_15_WAVE",
      "RAID_BOSS"
    ]);
    expect(encounterTypePlaceholders.map((placeholder) => placeholder.label)).toEqual([
      "일반 15공세 작전",
      "보스 전용 레이드"
    ]);
    expect(encounterTypePlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
    expect(raidBossResultPlaceholders.map((placeholder) => placeholder.field)).toEqual([
      "raidId",
      "bossId",
      "bossDefeated",
      "completedPhaseCount",
      "encounterDurationSeconds"
    ]);
    expect(raidBossResultPlaceholders.every((placeholder) => placeholder.value === "연동 대기")).toBe(true);
    expect(storyRaidLaunchResultPolicyPlaceholders.map((placeholder) => [placeholder.field, placeholder.value])).toEqual([
      ["allowedOperatorSlots", "5"],
      ["failureStaminaRefund", "연동 대기"],
      ["failureStaminaRefundStatus", "COMMITTED"],
      ["failureStaminaRefundResource", "STAMINA"],
      ["failureStaminaRefundReason", "STORY_RAID_FAILURE"],
      ["failureStaminaRefundPolicyId", "act1_story_raid_failure_stamina_refund_v1"],
      ["serverOwnedMutation", "STORY_RAID_STAMINA_REFUND"],
      ["fuelLedgerReason", "STORY_RAID_FAILURE_REFUND"],
      ["successCondition", "boss_defeat"],
      ["worldRaidScope", "backlog"]
    ]);
    expect(storyRaidLaunchResultPolicyPlaceholders.map((placeholder) => placeholder.note).join(" ")).toContain(
      "read-only marker"
    );
    expect(storyRaidLaunchResultPolicyPlaceholders.map((placeholder) => placeholder.note).join(" ")).toContain(
      "World Raid와 분리"
    );
  });

  it("prepares augment-at-start evidence without exposing combat internals", () => {
    expect(runStartPackageEvidencePlaceholders.map((placeholder) => placeholder.field)).toEqual([
      "startAugmentPackageRef",
      "startTacticPackageRef",
      "selectedAugmentIds",
      "selectedTacticIds",
      "source"
    ]);
    expect(runStartPackageEvidencePlaceholders.find((placeholder) => placeholder.field === "source")?.value).toBe(
      "run_start"
    );
    expect(runStartPackageEvidencePlaceholders.find((placeholder) => placeholder.field === "startAugmentPackageRef")?.note).toContain(
      "server-authored run start package"
    );
    expect(runStartPackageEvidencePlaceholders.find((placeholder) => placeholder.field === "startTacticPackageRef")?.note).toContain(
      "server-authored run start package"
    );
    expect(runStartPackageEvidencePlaceholders.map((placeholder) => placeholder.note).join(" ")).toContain(
      "ORDINARY_15_WAVE / RAID_BOSS"
    );
    expect(runStartPackageEvidencePlaceholders.map((placeholder) => placeholder.note).join(" ")).toContain(
      "reward/economy/story/offline/gacha Source of Truth 아님"
    );

    const serialized = JSON.stringify({
      encounterTypePlaceholders,
      raidBossResultPlaceholders,
      runStartPackageEvidencePlaceholders
    });
    for (const forbidden of forbiddenRunCombatEvidenceExamplesOutOfScope) {
      expect(serialized).not.toContain(forbidden);
    }
    expect(serialized).not.toContain("mid-wave");
    expect(serialized).not.toContain("중간 공세 선택");
    expect(serialized).not.toContain("reprocess");
    expect(serialized).not.toContain("result edit");
    expect(serialized).not.toContain("reward correction");
  });

  it("maps server-authored raid start metadata into read-only run detail cards", () => {
    const cards = runStartMetadataEvidenceFrom({
      serverPreviewMetadata: {
        encounterType: "RAID_BOSS",
        raidId: "raid_raider_king_engine",
        bossId: "boss_raider_king_engine",
        bossPhaseRefs: ["phase_01", "phase_02"],
        startAugmentPackageRef: "augment_pkg_act1_raid_v1",
        startTacticPackageRef: "tactic_pkg_act1_raid_v1",
        contentVersion: "act1.content.001",
        balanceVersion: "balance.dev.001",
        allowedOperatorSlots: null,
        rawRng: "must-not-copy",
        animationTimeline: "must-not-copy",
        token: "must-not-copy"
      }
    });

    expect(cards.map((card) => [card.field, card.value])).toEqual([
      ["encounterType", "보스 전용 레이드 / RAID_BOSS"],
      ["raidId", "raid_raider_king_engine"],
      ["bossId", "boss_raider_king_engine"],
      ["bossPhaseRefs", "phase_01 / phase_02"],
      ["startAugmentPackageRef", "augment_pkg_act1_raid_v1"],
      ["startTacticPackageRef", "tactic_pkg_act1_raid_v1"],
      ["contentVersion", "act1.content.001"],
      ["balanceVersion", "balance.dev.001"],
      ["allowedOperatorSlots", "없음"],
      ["selectedAugmentIds", "없음"],
      ["selectedTacticIds", "없음"],
      ["source", "run_start"]
    ]);
    expect(cards.find((card) => card.field === "startAugmentPackageRef")?.note).toContain(
      "server-authored run start package"
    );
    expect(cards.find((card) => card.field === "encounterType")?.note).toContain("ORDINARY_15_WAVE / RAID_BOSS");

    const serialized = JSON.stringify(cards);
    expect(serialized).not.toContain("must-not-copy");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("animationTimeline");
    expect(serialized).not.toContain("token");
  });

  it("maps expanded run launch evidence into risk/reward read-only cards", () => {
    const cards = runRiskRewardCardsForLaunchEvidence({
      operationId: "raid_raider_king_engine",
      outcome: "FAILED",
      clearedWaves: null,
      rewardPreviewId: "preview-1",
      rewardClaimId: null,
      appliedMultiplierLabel: "server-calculated",
      calculationMetadataStatus: "ready",
      firstClearConsumed: false,
      integrityDisposition: {
        status: "CLEAR",
        openSignals: 0,
        hardViolations: 0,
        primaryReason: null
      },
      encounterType: "RAID_BOSS",
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossPhaseRefs: ["phase-1"],
      bossDefeated: false,
      completedPhaseCount: 2,
      encounterDurationSeconds: 180,
      allowedOperatorSlots: 5,
      startAugmentPackageRef: "augment_pkg_act1_raid_v1",
      startTacticPackageRef: "tactic_pkg_act1_raid_v1",
      selectedAugmentIds: ["aug-a"],
      selectedTacticIds: ["tac-a"],
      staminaRefund: {
        status: "COMMITTED",
        resource: "STAMINA",
        reason: "STORY_RAID_FAILURE",
        policyId: "act1_story_raid_failure_stamina_refund_v1",
        serverOwnedMutation: true,
        fuelLedgerReason: "STORY_RAID_FAILURE_REFUND",
        fuelLedgerRefs: ["fuel-ledger-1"]
      },
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        repairEndpointsEnabled: false,
        source: "admin_run_read_model"
      }
    });

    expect(cards.map((card) => [card.label, card.value])).toEqual([
      ["위험도", "없음"],
      ["작전 ID", "약탈왕 기관차 / raid_raider_king_engine"],
      ["결과", "실패 / FAILED"],
      ["클리어 공세", "없음"],
      ["rewardPreviewId / rewardClaimId", "preview-1 / 없음"],
      ["적용 배율", "server-calculated"],
      ["first clear", "첫 클리어 보너스 없음"],
      ["무결성 판정", "CLEAR / open=0 / hard=0"],
      ["계산 메타데이터 상태", "준비 완료 / ready"]
    ]);
  });

  it("maps committed story raid stamina refund launch evidence", () => {
    const cards = storyRaidPolicyCardsForLaunchEvidence({
      operationId: "raid_raider_king_engine",
      outcome: "FAILED",
      clearedWaves: null,
      rewardPreviewId: null,
      rewardClaimId: null,
      appliedMultiplierLabel: null,
      calculationMetadataStatus: null,
      firstClearConsumed: null,
      integrityDisposition: {
        status: null,
        openSignals: null,
        hardViolations: null,
        primaryReason: null
      },
      encounterType: "RAID_BOSS",
      raidId: "raid_raider_king_engine",
      bossId: "boss_raider_king_engine",
      bossPhaseRefs: [],
      bossDefeated: false,
      completedPhaseCount: 2,
      encounterDurationSeconds: 180,
      allowedOperatorSlots: 5,
      startAugmentPackageRef: null,
      startTacticPackageRef: null,
      selectedAugmentIds: [],
      selectedTacticIds: [],
      staminaRefund: {
        status: "COMMITTED",
        resource: "STAMINA",
        reason: "STORY_RAID_FAILURE",
        policyId: "act1_story_raid_failure_stamina_refund_v1",
        serverOwnedMutation: true,
        fuelLedgerReason: "STORY_RAID_FAILURE_REFUND",
        fuelLedgerRefs: ["fuel-ledger-1"]
      },
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        repairEndpointsEnabled: false,
        source: "admin_run_read_model"
      }
    });

    expect(cards.find((card) => card.field === "failureStaminaRefund")?.value).toBe("있음");
    expect(cards.find((card) => card.field === "failureStaminaRefundStatus")?.value).toBe("COMMITTED");
    expect(cards.find((card) => card.field === "serverOwnedMutation")?.value).toBe(
      "스토리 레이드 stamina 환급 / STORY_RAID_STAMINA_REFUND"
    );
    expect(cards.find((card) => card.field === "fuelLedgerReason")?.value).toBe(
      "스토리 레이드 실패 환급 / STORY_RAID_FAILURE_REFUND"
    );
  });

  it("maps launchEvidence start package refs without raw payload fields", () => {
    const cards = runStartMetadataEvidenceFrom({
      launchEvidence: {
        encounterType: "ORDINARY_15_WAVE",
        raidId: null,
        bossId: null,
        bossPhaseRefs: [],
        startAugmentPackageRef: "augment_pkg_act1_opn_v1",
        startTacticPackageRef: "tactic_pkg_act1_opn_v1",
        contentVersion: "act1.content.001",
        balanceVersion: "balance.dev.001",
        allowedOperatorSlots: 5,
        selectedAugmentIds: ["aug-1", "aug-2"],
        selectedTacticIds: ["tac-1"],
        rawRng: "must-not-copy",
        animationTimeline: "must-not-copy"
      }
    });

    expect(cards.find((card) => card.field === "encounterType")?.value).toBe("일반 15공세 작전 / ORDINARY_15_WAVE");
    expect(cards.find((card) => card.field === "startAugmentPackageRef")?.value).toBe("augment_pkg_act1_opn_v1");
    expect(cards.find((card) => card.field === "selectedAugmentIds")?.value).toBe("aug-1 / aug-2");
    expect(cards.find((card) => card.field === "selectedTacticIds")?.value).toBe("tac-1");

    const serialized = JSON.stringify(cards);
    expect(serialized).not.toContain("must-not-copy");
    expect(serialized).not.toContain("rawRng");
    expect(serialized).not.toContain("animationTimeline");
  });

  it("keeps run start metadata fail-soft for older run detail responses", () => {
    const cards = runStartMetadataEvidenceFrom({});

    expect(cards.find((card) => card.field === "encounterType")?.value).toBe("없음");
    expect(cards.find((card) => card.field === "bossPhaseRefs")?.value).toBe("없음");
    expect(cards.find((card) => card.field === "source")?.value).toBe("run_start");
  });
});
