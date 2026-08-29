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
  "reward_correction"
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
  "ACCOUNT_READ",
  "RUN_READ",
  "ECONOMY_READ",
  "OFFLINE_REWARD_READ",
  "AUDIT_READ",
  "INTEGRITY_READ",
  "INCIDENT_READ",
  "ENFORCEMENT_SUSPEND",
  "ANALYTICS_READ",
  "BALANCE_READ"
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
      "google_verifier_ops_label"
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
