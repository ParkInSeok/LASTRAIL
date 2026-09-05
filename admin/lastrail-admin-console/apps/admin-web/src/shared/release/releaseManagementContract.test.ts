import { describe, expect, it } from "vitest";
import {
  buildReleaseManagementSnapshot,
  releaseManagementActionCandidates,
  releaseManagementCandidateFields,
  releaseManagementReadStates,
  releaseManagementTargetAxes,
  releaseManagementTargetRegistryPolicy,
  releaseManagementValidationChecks
} from "./releaseManagementContract";

describe("releaseManagementContract", () => {
  it("models target-scoped release management without relying on spreadsheet sync", () => {
    const snapshot = buildReleaseManagementSnapshot();

    expect(releaseManagementTargetAxes).toEqual(["targetId", "environment", "platform", "channel"]);
    expect(snapshot.spreadsheetIntegrationEnabled).toBe(false);
    expect(snapshot.serverContractStatus).toBe("서버 계약 대기");
    expect(snapshot.targetRegistry.targetIsolation).toContain("targetId");
    expect(snapshot.targetRegistry.browserSubmittedInfrastructureAllowed).toBe(false);
    expect(snapshot.targetRegistry.globalCurrentJsonShared).toBe(false);
  });

  it("keeps release candidate fields as form fields and separates app/content/protocol/release IDs", () => {
    expect(releaseManagementCandidateFields.map((field) => field.key)).toEqual([
      "releaseId",
      "appMinimumBuild",
      "appRecommendedBuild",
      "contentVersion",
      "contentCatalogId",
      "contentCatalogHash",
      "protocolVersion",
      "updateNotice",
      "storeUpdateUrl",
      "platform",
      "channel"
    ]);
    expect(releaseManagementCandidateFields.find((field) => field.key === "updateNotice")?.input).toBe("textarea");
    expect(JSON.stringify(releaseManagementCandidateFields)).not.toContain("raw JSON editor");
  });

  it("prepares explicit target-scoped release actions as disabled until the server contract exists", () => {
    expect(releaseManagementActionCandidates.map((action) => [action.code, action.enabled])).toEqual([
      ["save_draft", false],
      ["apply_internal_testing", false],
      ["approve_candidate", false],
      ["prestage_production", false],
      ["promote_pointer", false],
      ["open_release", false],
      ["rollback_release", false]
    ]);
    expect(releaseManagementActionCandidates.every((action) => action.requiresTargetConfirmation)).toBe(true);
    expect(releaseManagementActionCandidates.every((action) => action.requiresReason)).toBe(true);
    expect(releaseManagementActionCandidates.every((action) => action.permissionStatus === "서버 계약 대기")).toBe(true);
  });

  it("lists fail-closed read states and validation checks for release publishing safety", () => {
    expect(releaseManagementReadStates).toEqual([
      "unset",
      "no_permission",
      "loading",
      "empty",
      "conflict",
      "publish_failed",
      "verification_in_progress",
      "target_unavailable",
      "r2_unconfigured"
    ]);
    expect(releaseManagementValidationChecks.map((check) => check.code)).toEqual([
      "integer_version_compare",
      "compatibility_range",
      "catalog_hash_exists",
      "allowed_target_paths",
      "store_update_availability",
      "approved_content_hash",
      "public_json_redaction",
      "etag_revision_match",
      "target_permission_match",
      "dev_to_prd_artifact_revalidation"
    ]);
    expect(JSON.stringify(releaseManagementValidationChecks)).not.toContain("token");
    expect(JSON.stringify(releaseManagementValidationChecks)).not.toContain("secret");
  });

  it("captures target-switch and production safety requirements without executing remote actions", () => {
    expect(releaseManagementTargetRegistryPolicy).toMatchObject({
      serverOwned: true,
      conditionalPointerUpdateRequired: true,
      remoteR2WritesApproved: false,
      productionExecutionApproved: false,
      publicTestFlagGrantsInternalAccess: false
    });
  });
});
