import { describe, expect, it } from "vitest";
import {
  assertLaunchReadOnlyBoundary,
  buildLaunchIntegrationRegressionSmoke,
  buildLaunchReadOnlyBoundarySnapshot,
  launchForbiddenActionControls,
  launchForbiddenSensitiveFields
} from "./launchBoundaryGuard";

describe("launch read-only boundary guard", () => {
  it("keeps visible admin pages while marking launch-only rail choice events as backlog", () => {
    const snapshot = buildLaunchReadOnlyBoundarySnapshot();

    expect(snapshot.visiblePagesPolicy).toBe("admin pages visible, launch summary trimmed");
    expect(snapshot.railChoiceEvents).toBe("backlog");
    expect(snapshot.navigationLabels).toEqual([
      "개요",
      "플레이어",
      "출격",
      "CS 지원",
      "요원 모집",
      "오프라인 보상",
      "시스템 에러 로그",
      "무결성",
      "의심 플레이어",
      "사건",
      "제재",
      "지표",
      "게임 설정",
      "감사 로그"
    ]);
  });

  it("locks forbidden launch action controls as disabled across support/run/gacha/ops/auth/session surfaces", () => {
    const snapshot = buildLaunchReadOnlyBoundarySnapshot();

    expect(launchForbiddenActionControls).toEqual([
      "repair",
      "manual_compensation",
      "economy_correction",
      "link_account",
      "unlink_account",
      "merge_account",
      "revoke_provider",
      "session_revoke",
      "logout_all",
      "ops_acknowledge",
      "ops_resolve",
      "ops_ignore",
      "ticket_creation",
      "send_template",
      "copy_as_action",
      "run_replay",
      "run_reprocess",
      "result_edit",
      "reward_correction"
    ]);
    expect(snapshot.actionControls.every((control) => control.enabled === false)).toBe(true);
    expect(snapshot.actionControls.map((control) => control.code)).toEqual(launchForbiddenActionControls);
    expect(snapshot.contractFlags).toEqual({
      supportMutationEndpointsEnabled: false,
      supportRepairEnabled: false,
      supportEscalationWriteEnabled: false,
      supportTemplateSendEnabled: false,
      supportTemplateCopyActionEnabled: false,
      supportTicketCreationEnabled: false,
      gachaMutationControlsEnabled: false,
      runCombatMutationControlsEnabled: false,
      opsErrorRepairMechanism: false
    });
    expect(() => assertLaunchReadOnlyBoundary(snapshot)).not.toThrow();
  });

  it("keeps raw tokens, payloads, RNG, stack dumps, and animation internals out of the launch evidence snapshot", () => {
    const snapshot = buildLaunchReadOnlyBoundarySnapshot();

    expect(launchForbiddenSensitiveFields).toEqual([
      "raw_token",
      "auth_header",
      "raw_payload",
      "raw_rng",
      "probability",
      "full_weights",
      "stack_dump",
      "animation_timeline"
    ]);
    expect(snapshot.sensitiveFieldExposure.every((field) => field.exposed === false)).toBe(true);
    expect(snapshot.sensitiveFieldExposure.map((field) => field.code)).toEqual(launchForbiddenSensitiveFields);

    const serialized = JSON.stringify(snapshot);
    expect(serialized).not.toContain("raw body");
    expect(serialized).not.toContain("raw auth header");
    expect(serialized).not.toContain("raw RNG");
    expect(serialized).not.toContain("full weight list");
    expect(serialized).not.toContain("stack dump");
    expect(serialized).not.toContain("animation timeline");
  });

  it("covers launch read-only integration smoke points across visible admin surfaces", () => {
    const smoke = buildLaunchIntegrationRegressionSmoke();

    expect(smoke.visiblePageLabels).toEqual([
      "개요",
      "플레이어",
      "출격",
      "CS 지원",
      "요원 모집",
      "오프라인 보상",
      "시스템 에러 로그",
      "무결성",
      "의심 플레이어",
      "사건",
      "제재",
      "지표",
      "게임 설정",
      "감사 로그"
    ]);
    expect(smoke.surfaces.map((surface) => surface.code)).toEqual([
      "dashboard",
      "support_account_summary",
      "player_detail",
      "run_detail",
      "gacha",
      "ops_error_logs",
      "cs_template",
      "read_only_boundary"
    ]);
    expect(smoke.surfaces.every((surface) => surface.readOnly === true)).toBe(true);
    expect(smoke.surfaces.every((surface) => surface.mutationControlsEnabled === false)).toBe(true);
    expect(smoke.contractPoints).toEqual([
      "support_auth_session_metrics",
      "support_ops_errors",
      "raid_start_metadata",
      "gacha_launch_visibility",
      "google_verifier_ops_label"
    ]);
    expect(smoke.labels.googleVerifierUnavailable).toBe("Google 인증 검증기 사용 불가");
    expect(smoke.labels.raidBossOnly).toBe("보스 전용 레이드");
    expect(smoke.labels.ordinary15Wave).toBe("일반 15공세 작전");
    expect(smoke.labels.duplicateReplay).toBe("이전 결과 재조회");
    expect(smoke.supportEvidence).toEqual({
      authSessionMetrics: ["lastLoginAt", "lastLoginAgeSeconds", "concurrentUsers", "concurrentUsersPrecision"],
      opsErrors: ["latest 5", "critical first", "safe correlation ids"],
      raidStartMetadata: [
        "encounterType",
        "raidId",
        "bossId",
        "bossPhaseRefs",
        "startAugmentPackageRef",
        "startTacticPackageRef",
        "contentVersion",
        "balanceVersion",
        "allowedOperatorSlots"
      ],
      gachaLaunchVisibility: ["activeBannerId", "guaranteeType", "actOneEliteVisualScope"]
    });
    expect(smoke.railChoiceEvents).toBe("backlog");

    const serialized = JSON.stringify(smoke);
    expect(serialized).not.toContain("link_account");
    expect(serialized).not.toContain("ops_resolve");
    expect(serialized).not.toContain("reward_correction");
    expect(serialized).not.toContain("raw_token");
    expect(serialized).not.toContain("raw_payload");
    expect(serialized).not.toContain("raw_rng");
    expect(serialized).not.toContain("full_weights");
    expect(serialized).not.toContain("stack_dump");
    expect(serialized).not.toContain("animation_timeline");
  });
});
