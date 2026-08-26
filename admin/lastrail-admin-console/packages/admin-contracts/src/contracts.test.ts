import { describe, expect, it } from "vitest";
import {
  BootstrapResponseSchema,
  IntegrityEventsResponseSchema,
  SuspiciousPlayerDetailSchema,
  SuspiciousPlayersResponseSchema,
  PlayerDetailSchema,
  hasPermission,
  rolePermissions
} from "./index";

describe("admin contracts", () => {
  it("derives read permissions from the permission registry", () => {
    const bootstrap = BootstrapResponseSchema.parse({
      environment: "dev",
      admin: {
        id: "admin-1",
        displayName: "Operator One",
        role: "OPERATOR",
        permissions: ["ACCOUNT_READ", "RUN_READ"]
      }
    });

    expect(hasPermission(bootstrap.admin, "ACCOUNT_READ")).toBe(true);
    expect(hasPermission(bootstrap.admin, "BALANCE_PUBLISH")).toBe(false);
  });

  it("allows operators to create player notes while keeping viewers read-only", () => {
    expect(hasPermission({ permissions: rolePermissions.OPERATOR }, "ACCOUNT_NOTE_WRITE")).toBe(true);
    expect(hasPermission({ permissions: rolePermissions.VIEWER }, "ACCOUNT_NOTE_WRITE")).toBe(false);
  });

  it("requires Training Core and Sync fields for player progression", () => {
    const player = PlayerDetailSchema.parse({
      accountId: "account-1",
      displayName: "LASTRAIL Tester",
      status: "ACTIVE",
      createdAt: "2026-08-25T00:00:00.000Z",
      progression: {
        operatorExp: 1840,
        syncLevel: 30,
        coreLevelSpread: 3,
        trainingCore: [
          { slot: 1, operatorId: "kyle", name: "Kyle", trainedLevel: 32 },
          { slot: 2, operatorId: "reina", name: "Reina", trainedLevel: 31 },
          { slot: 3, operatorId: "eve", name: "Eve", trainedLevel: 30 },
          { slot: 4, operatorId: "sera", name: "Sera", trainedLevel: 30 },
          { slot: 5, operatorId: "mira", name: "Mira", trainedLevel: 33 }
        ],
        operators: [
          {
            operatorId: "kyle",
            name: "Kyle",
            effectiveLevel: 32,
            levelSource: "TRAINED",
            trainedLevel: 32,
            skillTraining: 3,
            promotion: "I",
            coreSlot: 1
          },
          {
            operatorId: "vex",
            name: "Vex",
            effectiveLevel: 30,
            levelSource: "SYNC",
            trainedLevel: 1,
            skillTraining: 1,
            promotion: "0"
          }
        ]
      }
    });

    expect(player.progression.trainingCore).toHaveLength(5);
    expect(player.progression.syncLevel).toBe(30);
    expect(player.progression.operators[1].levelSource).toBe("SYNC");
  });

  it("describes integrity events as investigation signals, not ban decisions", () => {
    const response = IntegrityEventsResponseSchema.parse({
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
    });

    expect(response.items[0].classification).toBe("SOFT_ANOMALY");
    expect(response.items[0].evidence).toEqual({ maxDamageRatio: 2.4 });
    expect(response.items[0]).not.toHaveProperty("banAction");
  });

  it("describes suspicious players as review queues without automatic enforcement", () => {
    const response = SuspiciousPlayersResponseSchema.parse({
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
    });

    expect(response.items[0].priority).toBe("HIGH");
    expect(response.items[0]).not.toHaveProperty("banAction");
  });

  it("describes suspicious player detail as a read-only investigation dossier", () => {
    const detail = SuspiciousPlayerDetailSchema.parse({
      account: {
        accountId: "account-1",
        displayName: "Kyle Account",
        status: "ACTIVE",
        createdAt: "2026-08-25T00:00:00.000Z"
      },
      summary: {
        priority: "CRITICAL",
        openSignals: 4,
        hardViolations: 2,
        suspiciousRuns: 2,
        lastDetectionAt: "2026-08-26T04:00:00.000Z",
        primaryReason: "IMPOSSIBLE_DAMAGE",
        reviewStatus: "ESCALATED",
        reviewReason: "mark-only escalation for owner review",
        reviewUpdatedAt: "2026-08-26T04:02:00.000Z",
        currentEnforcement: "REWARD_HELD",
        reviewOwner: "operator-1"
      },
      riskSignals: [
        {
          id: "event-1",
          runId: "run-1",
          classification: "HARD_VIOLATION",
          category: "DAMAGE_ANALYSIS",
          evidenceCode: "IMPOSSIBLE_DAMAGE",
          evidence: { maxDamageRatio: 12.8 },
          reviewStatus: "ESCALATED",
          reviewOwner: "operator-1",
          createdAt: "2026-08-26T04:00:00.000Z"
        }
      ],
      suspiciousRuns: [
        {
          runId: "run-1",
          runType: "RUN",
          result: "CLEARED",
          riskTier: "V",
          balanceVersion: "7",
          startedAt: "2026-08-26T03:30:00.000Z",
          completedAt: "2026-08-26T03:45:00.000Z"
        }
      ],
      notes: [],
      decisions: []
    });

    expect(detail.summary.reviewStatus).toBe("ESCALATED");
    expect(detail.summary.reviewReason).toBe("mark-only escalation for owner review");
    expect(detail.riskSignals[0]).not.toHaveProperty("banAction");
  });
});
