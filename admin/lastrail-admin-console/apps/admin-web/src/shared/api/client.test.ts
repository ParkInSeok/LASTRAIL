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
});
