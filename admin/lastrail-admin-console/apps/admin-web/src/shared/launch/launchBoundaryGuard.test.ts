import { describe, expect, it } from "vitest";
import {
  assertLaunchReadOnlyBoundary,
  buildLaunchIntegrationRegressionSmoke,
  buildPreviewSmokeChecklist,
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
      "시스템 에러 로그",
      "무결성",
      "의심 플레이어",
      "사건",
      "제재",
      "지표",
      "게임 설정",
      "버전 관리",
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
      "reward_correction",
      "release_remote_execute"
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
      "시스템 에러 로그",
      "무결성",
      "의심 플레이어",
      "사건",
      "제재",
      "지표",
      "게임 설정",
      "버전 관리",
      "감사 로그"
    ]);
    expect(smoke.surfaces.map((surface) => surface.code)).toEqual([
      "dashboard",
      "support_account_summary",
      "player_detail",
      "run_detail",
      "gacha",
      "ops_error_logs",
      "version_management",
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
      "google_verifier_ops_label",
      "version_management_r2_export_contract"
    ]);
    expect(smoke.labels.googleVerifierUnavailable).toBe("Google 인증 검증기 사용 불가");
    expect(smoke.labels.raidBossOnly).toBe("보스 전용 레이드");
    expect(smoke.labels.ordinary15Wave).toBe("일반 15공세 작전");
    expect(smoke.labels.duplicateReplay).toBe("이전 결과 재조회");
    expect(smoke.supportEvidence).toEqual({
      authSessionMetrics: ["activeSessionCount", "lastLoginAt", "lastLoginAgeSeconds", "latestSessionAt"],
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
    expect(serialized).not.toContain("concurrentUsersPrecision");
  });

  it("locks the final preview smoke checklist for launch read-only pages", () => {
    const checklist = buildPreviewSmokeChecklist();

    expect(checklist.previewUrl).toBe("https://preview.lastrail-admin-console.pages.dev");
    expect(checklist.accessSmoke).toEqual({
      unauthenticatedExpectedStatus: 302,
      expectedProtection: "Cloudflare Access login redirect"
    });
    expect(checklist.loginSessionFallback).toBe(
      "If an authenticated browser session is unavailable, use Access 302 plus local test/typecheck/build verification."
    );
    expect(checklist.pages.map((page) => [page.code, page.path, page.readOnly])).toEqual([
      ["dashboard", "/", true],
      ["support_account_summary", "/support/accounts/:accountId", true],
      ["player_detail", "/players/:accountId", true],
      ["run_detail", "/runs/:runId", true],
      ["gacha", "/gacha", true],
      ["ops_error_logs", "/ops/error-logs", true],
      ["version_management", "/versions", true]
    ]);
    expect(checklist.assertions.map((assertion) => assertion.code)).toEqual([
      "read_only_boundary",
      "dashboard_trim",
      "gacha_ticket_12",
      "story_raid_policy",
      "world_raid_backlog",
      "google_verifier_ops_only",
      "rail_choice_backlog"
    ]);
    expect(checklist.assertions.find((assertion) => assertion.code === "gacha_ticket_12")?.expected).toContain(
      "login bonus 5 repeatable=false + opn_03_water_stop first clear 1 repeatable=false"
    );
    expect(checklist.assertions.find((assertion) => assertion.code === "gacha_ticket_12")?.expected).toContain(
      "first-clear 7 promoted_local_launch_config"
    );
    expect(checklist.assertions.find((assertion) => assertion.code === "gacha_ticket_12")?.expected).toContain(
      "login bonus 5 support_summary_read_model_available"
    );
    expect(checklist.assertions.find((assertion) => assertion.code === "gacha_ticket_12")?.expected).toContain(
      "opn_05_night_boarders first clear 1 repeatable=false + opn_06_broken_line first clear 2 repeatable=false"
    );
    expect(checklist.assertions.find((assertion) => assertion.code === "gacha_ticket_12")?.expected).toContain(
      "raid_raider_king_engine story raid first clear 3 repeatable=false"
    );
    expect(checklist.assertions.find((assertion) => assertion.code === "gacha_ticket_12")?.expected).toContain(
      "offline repeat excludes cur_command_ticket / cur_command_ticket_fragment / cur_special_core"
    );
    expect(checklist.assertions.find((assertion) => assertion.code === "gacha_ticket_12")?.expected).toContain(
      "not production seed/config promotion"
    );
    expect(checklist.assertions.find((assertion) => assertion.code === "story_raid_policy")?.expected).toContain(
      "boss-only / 5 operators / failure stamina refund / boss defeat success"
    );

    const serialized = JSON.stringify(checklist);
    expect(serialized).not.toContain("production deploy");
    expect(serialized).not.toContain("secret write");
    expect(serialized).not.toContain("link_account");
    expect(serialized).not.toContain("ops_resolve");
    expect(serialized).not.toContain("reward_correction");
    expect(serialized).not.toContain("raw_token");
    expect(serialized).not.toContain("raw_payload");
    expect(serialized).not.toContain("raw_rng");
    expect(serialized).not.toContain("animation_timeline");
  });
});
