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
});
