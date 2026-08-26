import { afterEach, describe, expect, it, vi } from "vitest";
import { adminApi } from "./client";

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
});
