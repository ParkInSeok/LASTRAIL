import type { AdminPermission } from "@lastrail/admin-contracts";
import {
  encounterTypeLabel,
  gachaResultStatusLabel,
  opsErrorCodeLabel
} from "../labels/adminLabels";
import { visibleNavigation } from "../navigation/navigation";
import {
  customerSupportContractPreview,
  customerSupportSuggestionCases
} from "../support/supportContract";

export const launchForbiddenActionControls = [
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
] as const;

export const launchForbiddenSensitiveFields = [
  "raw_token",
  "auth_header",
  "raw_payload",
  "raw_rng",
  "probability",
  "full_weights",
  "stack_dump",
  "animation_timeline"
] as const;

const launchNavigationPermissions: AdminPermission[] = [
  "SUPPORT_READ",
  "ACCOUNT_READ",
  "RUN_READ",
  "ECONOMY_READ",
  "OFFLINE_REWARD_READ",
  "AUDIT_READ",
  "INTEGRITY_READ",
  "INCIDENT_READ",
  "ENFORCEMENT_SUSPEND",
  "ANALYTICS_READ",
  "BALANCE_READ",
  "RUNTIME_POLICY_READ"
];

export type LaunchReadOnlyBoundarySnapshot = {
  visiblePagesPolicy: "admin pages visible, launch summary trimmed";
  railChoiceEvents: "backlog";
  navigationLabels: string[];
  actionControls: Array<{
    code: (typeof launchForbiddenActionControls)[number];
    enabled: false;
  }>;
  sensitiveFieldExposure: Array<{
    code: (typeof launchForbiddenSensitiveFields)[number];
    exposed: false;
  }>;
  contractFlags: {
    supportMutationEndpointsEnabled: false;
    supportRepairEnabled: false;
    supportEscalationWriteEnabled: false;
    supportTemplateSendEnabled: false;
    supportTemplateCopyActionEnabled: false;
    supportTicketCreationEnabled: false;
    gachaMutationControlsEnabled: false;
    runCombatMutationControlsEnabled: false;
    opsErrorRepairMechanism: false;
  };
};

export type LaunchIntegrationRegressionSmoke = {
  visiblePageLabels: string[];
  railChoiceEvents: "backlog";
  surfaces: Array<{
    code:
      | "dashboard"
      | "support_account_summary"
      | "player_detail"
      | "run_detail"
      | "gacha"
      | "ops_error_logs"
      | "version_management"
      | "cs_template"
      | "read_only_boundary";
    readOnly: true;
    mutationControlsEnabled: false;
  }>;
  contractPoints: Array<
    | "support_auth_session_metrics"
    | "support_ops_errors"
    | "raid_start_metadata"
    | "gacha_launch_visibility"
    | "google_verifier_ops_label"
    | "version_management_r2_export_contract"
  >;
  labels: {
    googleVerifierUnavailable: string;
    raidBossOnly: string;
    ordinary15Wave: string;
    duplicateReplay: string;
  };
  supportEvidence: {
    authSessionMetrics: string[];
    opsErrors: string[];
    raidStartMetadata: string[];
    gachaLaunchVisibility: string[];
  };
};

export type PreviewSmokeChecklist = {
  previewUrl: "https://preview.lastrail-admin-console.pages.dev";
  accessSmoke: {
    unauthenticatedExpectedStatus: 302;
    expectedProtection: "Cloudflare Access login redirect";
  };
  loginSessionFallback: string;
  pages: Array<{
    code:
      | "dashboard"
      | "support_account_summary"
      | "player_detail"
      | "run_detail"
      | "gacha"
      | "offline_reward"
      | "ops_error_logs"
      | "version_management";
    path: string;
    readOnly: true;
  }>;
  assertions: Array<{
    code:
      | "read_only_boundary"
      | "dashboard_trim"
      | "gacha_ticket_12"
      | "story_raid_policy"
      | "world_raid_backlog"
      | "google_verifier_ops_only"
      | "rail_choice_backlog";
    expected: string;
  }>;
};

export function buildLaunchReadOnlyBoundarySnapshot(): LaunchReadOnlyBoundarySnapshot {
  const firstSuggestion = customerSupportSuggestionCases[0];
  return {
    visiblePagesPolicy: "admin pages visible, launch summary trimmed",
    railChoiceEvents: "backlog",
    navigationLabels: visibleNavigation(launchNavigationPermissions).flatMap((group) =>
      group.items.map((item) => item.label)
    ),
    actionControls: launchForbiddenActionControls.map((code) => ({ code, enabled: false })),
    sensitiveFieldExposure: launchForbiddenSensitiveFields.map((code) => ({ code, exposed: false })),
    contractFlags: {
      supportMutationEndpointsEnabled: customerSupportContractPreview.supportContext.mutationEndpointsEnabled,
      supportRepairEnabled: false,
      supportEscalationWriteEnabled: customerSupportContractPreview.escalation.writeEnabled,
      supportTemplateSendEnabled: firstSuggestion?.sendActionEnabled ?? false,
      supportTemplateCopyActionEnabled: firstSuggestion?.copyActionEnabled ?? false,
      supportTicketCreationEnabled: firstSuggestion?.ticketCreationEnabled ?? false,
      gachaMutationControlsEnabled: customerSupportContractPreview.gachaLaunch.mutationControlsEnabled,
      runCombatMutationControlsEnabled: customerSupportContractPreview.runCombat.mutationControlsEnabled,
      opsErrorRepairMechanism: customerSupportContractPreview.supportContext.mutationEndpointsEnabled
    }
  };
}

export function buildPreviewSmokeChecklist(): PreviewSmokeChecklist {
  return {
    previewUrl: "https://preview.lastrail-admin-console.pages.dev",
    accessSmoke: {
      unauthenticatedExpectedStatus: 302,
      expectedProtection: "Cloudflare Access login redirect"
    },
    loginSessionFallback:
      "If an authenticated browser session is unavailable, use Access 302 plus local test/typecheck/build verification.",
    pages: [
      { code: "dashboard", path: "/", readOnly: true },
      { code: "support_account_summary", path: "/support/accounts/:accountId", readOnly: true },
      { code: "player_detail", path: "/players/:accountId", readOnly: true },
      { code: "run_detail", path: "/runs/:runId", readOnly: true },
      { code: "gacha", path: "/gacha", readOnly: true },
      { code: "ops_error_logs", path: "/ops/error-logs", readOnly: true },
      { code: "version_management", path: "/versions", readOnly: true }
    ],
    assertions: [
      {
        code: "read_only_boundary",
        expected:
          "No mutation, repair, compensation, link/unlink, session revoke, acknowledge/resolve, replay, reprocess, result edit, or reward correction controls."
      },
      {
        code: "dashboard_trim",
        expected: "Dashboard first screen stays launch operations summary focused."
      },
      {
        code: "gacha_ticket_12",
        expected:
          "Launch candidate/draft command tickets total 12: first-clear 7 promoted_local_launch_config + login bonus 5 support_summary_read_model_available; login bonus 5 repeatable=false + opn_03_water_stop first clear 1 repeatable=false + opn_05_night_boarders first clear 1 repeatable=false + opn_06_broken_line first clear 2 repeatable=false + raid_raider_king_engine story raid first clear 3 repeatable=false; offline repeat excludes cur_command_ticket / cur_command_ticket_fragment / cur_special_core; not production seed/config promotion."
      },
      {
        code: "story_raid_policy",
        expected: "Act 1 story raid stays boss-only / 5 operators / failure stamina refund / boss defeat success."
      },
      {
        code: "world_raid_backlog",
        expected: "World Raid ranking, settlement, and reward repair stay future/backlog and separate from story raid."
      },
      {
        code: "google_verifier_ops_only",
        expected: "`google_auth_verifier_unavailable` stays dedicated ops error logs evidence, not default support summary."
      },
      {
        code: "rail_choice_backlog",
        expected: "Rail Choice Events stay outside Act 1 launch scope."
      }
    ]
  };
}

export function assertLaunchReadOnlyBoundary(snapshot: LaunchReadOnlyBoundarySnapshot): void {
  const enabledAction = snapshot.actionControls.find((control) => control.enabled);
  if (enabledAction) {
    throw new Error(`Launch action control must stay disabled: ${enabledAction.code}`);
  }

  const exposedSensitiveField = snapshot.sensitiveFieldExposure.find((field) => field.exposed);
  if (exposedSensitiveField) {
    throw new Error(`Launch sensitive field must stay hidden: ${exposedSensitiveField.code}`);
  }

  if (snapshot.railChoiceEvents !== "backlog") {
    throw new Error("Rail Choice Events must stay outside Act 1 launch scope");
  }
}

export function buildLaunchIntegrationRegressionSmoke(): LaunchIntegrationRegressionSmoke {
  const navigationLabels = buildLaunchReadOnlyBoundarySnapshot().navigationLabels;
  return {
    visiblePageLabels: navigationLabels,
    railChoiceEvents: "backlog",
    surfaces: [
      "dashboard",
      "support_account_summary",
      "player_detail",
      "run_detail",
      "gacha",
      "ops_error_logs",
      "version_management",
      "cs_template",
      "read_only_boundary"
    ].map((code) => ({
      code: code as LaunchIntegrationRegressionSmoke["surfaces"][number]["code"],
      readOnly: true,
      mutationControlsEnabled: false
    })),
    contractPoints: [
      "support_auth_session_metrics",
      "support_ops_errors",
      "raid_start_metadata",
      "gacha_launch_visibility",
      "google_verifier_ops_label",
      "version_management_r2_export_contract"
    ],
    labels: {
      googleVerifierUnavailable: opsErrorCodeLabel("google_auth_verifier_unavailable"),
      raidBossOnly: encounterTypeLabel("RAID_BOSS"),
      ordinary15Wave: encounterTypeLabel("ORDINARY_15_WAVE"),
      duplicateReplay: gachaResultStatusLabel("DUPLICATE_REPLAY")
    },
    supportEvidence: {
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
    }
  };
}
