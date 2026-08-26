import { describe, expect, it } from "vitest";
import {
  BootstrapResponseSchema,
  IntegrityEventsResponseSchema,
  PlayerDetailSchema,
  hasPermission,
  rolePermissions
} from "./index";

describe("admin contracts", () => {
  it("derives read permissions from the permission registry", () => {
    const bootstrap = BootstrapResponseSchema.parse({
      environment: "staging",
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
});
