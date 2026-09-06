import { describe, expect, it, vi } from "vitest";
import {
  createGmailRecoveryEmailSender,
  createGmailRecoveryEmailSenderFromEnv,
  GmailRecoveryEmailSendError,
  type RecoveryEmailMessage,
} from "../../apps/game-api/src/password-auth";

const message: RecoveryEmailMessage = {
  toEmail: "Commander@Example.com",
  emailMasked: "c********@example.com",
  purpose: "PASSWORD_RESET",
  challengeId: "email_challenge_test",
  verificationCode: "12345678",
  expiresAt: "2026-09-06T00:15:00.000Z",
};

function decodeBase64Url(value: string): string {
  const normalized = value.replaceAll("-", "+").replaceAll("_", "/");
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
  return Buffer.from(padded, "base64").toString("utf8");
}

describe("GmailRecoveryEmailSender", () => {
  it("exchanges a server-owned refresh token and sends recovery MIME through Gmail without leaking OAuth secrets", async () => {
    const fetcher = vi.fn(async (input: string | URL | Request) => {
      const url = input.toString();
      if (url === "https://oauth2.googleapis.com/token") {
        return new Response(JSON.stringify({ access_token: "access-token-from-google" }), { status: 200 });
      }
      if (url === "https://gmail.googleapis.com/gmail/v1/users/me/messages/send") {
        return new Response(JSON.stringify({ id: "gmail-message-id" }), { status: 200 });
      }
      throw new Error(`unexpected URL ${url}`);
    });

    const sender = createGmailRecoveryEmailSender({
      senderEmail: "noreply@lastrail.example",
      oauthClientId: "gmail-client-id",
      oauthClientSecret: "gmail-client-secret",
      oauthRefreshToken: "gmail-refresh-token",
      fetcher,
    });

    await sender.sendRecoveryEmail(message);

    expect(fetcher).toHaveBeenCalledTimes(2);
    const tokenCall = fetcher.mock.calls[0];
    expect(tokenCall?.[0]?.toString()).toBe("https://oauth2.googleapis.com/token");
    expect(tokenCall?.[1]?.method).toBe("POST");
    expect(tokenCall?.[1]?.body?.toString()).toContain("grant_type=refresh_token");
    expect(tokenCall?.[1]?.body?.toString()).toContain("refresh_token=gmail-refresh-token");

    const sendCall = fetcher.mock.calls[1];
    expect(sendCall?.[0]?.toString()).toBe("https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
    expect(sendCall?.[1]?.method).toBe("POST");
    expect(sendCall?.[1]?.headers).toEqual(expect.objectContaining({ Authorization: "Bearer access-token-from-google" }));
    const sendPayload = JSON.parse(sendCall?.[1]?.body?.toString() ?? "{}") as { raw: string };
    expect(JSON.stringify(sendPayload)).not.toContain("gmail-client-secret");
    expect(JSON.stringify(sendPayload)).not.toContain("gmail-refresh-token");

    const mime = decodeBase64Url(sendPayload.raw);
    expect(mime).toContain("To: Commander@Example.com");
    expect(mime).toContain("From: noreply@lastrail.example");
    expect(mime).toContain("Subject: LAST RAIL account verification");
    expect(mime).toContain("Reference: email_challenge_test");
    expect(mime).toContain("Verification code: 12345678");
    expect(mime).not.toContain("https://");
  });

  it("fails closed before network calls when Gmail sender config is incomplete", async () => {
    const fetcher = vi.fn();
    const sender = createGmailRecoveryEmailSender({
      senderEmail: "noreply@lastrail.example",
      oauthClientId: "",
      oauthClientSecret: "gmail-client-secret",
      oauthRefreshToken: "gmail-refresh-token",
      fetcher,
    });

    await expect(sender.sendRecoveryEmail(message)).rejects.toMatchObject({
      code: "gmail_config_missing",
    });
    expect(fetcher).not.toHaveBeenCalled();
  });

  it("keeps route wiring unconfigured until all Gmail env secrets are present", () => {
    expect(createGmailRecoveryEmailSenderFromEnv({
      RECOVERY_EMAIL_PROVIDER: "GMAIL",
      GMAIL_RECOVERY_SENDER_EMAIL: "noreply@lastrail.example",
      GMAIL_RECOVERY_OAUTH_CLIENT_ID: "gmail-client-id",
      GMAIL_RECOVERY_OAUTH_CLIENT_SECRET: "gmail-client-secret",
    })).toBeNull();
  });

  it("maps OAuth unauthorized responses to a stable Gmail sender error", async () => {
    const sender = createGmailRecoveryEmailSender({
      senderEmail: "noreply@lastrail.example",
      oauthClientId: "gmail-client-id",
      oauthClientSecret: "gmail-client-secret",
      oauthRefreshToken: "gmail-refresh-token",
      fetcher: async () => new Response(JSON.stringify({ error: "invalid_grant" }), { status: 401 }),
    });

    await expect(sender.sendRecoveryEmail(message)).rejects.toEqual(new GmailRecoveryEmailSendError("gmail_oauth_unauthorized"));
  });

  it("maps Gmail quota and transient send failures to stable Gmail sender errors without retrying duplicate sends", async () => {
    const calls: string[] = [];
    const quotaSender = createGmailRecoveryEmailSender({
      senderEmail: "noreply@lastrail.example",
      oauthClientId: "gmail-client-id",
      oauthClientSecret: "gmail-client-secret",
      oauthRefreshToken: "gmail-refresh-token",
      fetcher: async (input) => {
        calls.push(input.toString());
        if (calls.length === 1) return new Response(JSON.stringify({ access_token: "access-token-from-google" }), { status: 200 });
        return new Response(JSON.stringify({ error: { status: "RESOURCE_EXHAUSTED" } }), { status: 429 });
      },
    });

    await expect(quotaSender.sendRecoveryEmail(message)).rejects.toEqual(new GmailRecoveryEmailSendError("gmail_quota_exceeded"));
    expect(calls).toHaveLength(2);

    const transientSender = createGmailRecoveryEmailSender({
      senderEmail: "noreply@lastrail.example",
      oauthClientId: "gmail-client-id",
      oauthClientSecret: "gmail-client-secret",
      oauthRefreshToken: "gmail-refresh-token",
      fetcher: async (input) => {
        if (input.toString().includes("oauth2")) return new Response(JSON.stringify({ access_token: "access-token-from-google" }), { status: 200 });
        throw new Error("network timeout");
      },
    });

    await expect(transientSender.sendRecoveryEmail(message)).rejects.toEqual(new GmailRecoveryEmailSendError("gmail_send_unavailable"));
  });
});
