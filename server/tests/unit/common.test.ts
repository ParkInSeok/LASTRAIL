import { describe, expect, it } from "vitest";
import {
  assertSafeInteger,
  buildOpsErrorLogInsertEnvelope,
  canonicalJson,
  createRequestId,
  sha256Hex,
} from "@lastrail/common";

describe("common foundations", () => {
  it("canonicalJson sorts object keys recursively", () => {
    expect(canonicalJson({ z: 1, a: { y: 2, b: 3 } })).toBe('{"a":{"b":3,"y":2},"z":1}');
  });

  it("sha256Hex hashes canonical strings", async () => {
    await expect(sha256Hex("lastrail")).resolves.toMatch(/^[a-f0-9]{64}$/u);
  });

  it("assertSafeInteger rejects unsafe domain integers", () => {
    expect(() => assertSafeInteger(Number.MAX_SAFE_INTEGER + 1, "credits")).toThrow(/credits/u);
  });

  it("createRequestId returns a ULID-shaped request id", () => {
    expect(createRequestId()).toMatch(/^req_[0-9A-HJKMNP-TV-Z]{26}$/u);
  });

  it("buildOpsErrorLogInsertEnvelope sanitizes context and hashes idempotency keys", async () => {
    const envelope = await buildOpsErrorLogInsertEnvelope({
      id: "ops_error_unit_1",
      occurredAtMs: 1787650000000,
      environment: "DEV",
      service: "game_api",
      severity: "error",
      errorCode: "reward_plan_mismatch",
      messageSummary: "Reward plan mismatch.",
      status: "open",
      source: "game_api",
      requestId: "req_unit_1",
      accountId: "account-1",
      idempotencyOperation: "run_finish",
      idempotencyKey: "plain-idempotency-key",
      correlation: {
        accountId: "account-1",
        rawRng: "never",
      },
      context: {
        safe: true,
        Authorization: "Bearer secret",
        accessToken: "secret",
        nested: {
          refreshToken: "secret",
          value: "kept",
        },
      },
      retryable: true,
      createdAtMs: 1787650000001,
    });

    expect(envelope).toMatchObject({
      id: "ops_error_unit_1",
      occurredAt: 1787650000000,
      environment: "DEV",
      service: "game_api",
      severity: "error",
      errorCode: "reward_plan_mismatch",
      messageSummary: "Reward plan mismatch.",
      status: "open",
      source: "game_api",
      requestId: "req_unit_1",
      accountId: "account-1",
      idempotencyOperation: "run_finish",
      retryable: true,
      masked: true,
      createdAt: 1787650000001,
    });
    expect(envelope.idempotencyKeyHash).toMatch(/^sha256:[a-f0-9]{64}$/u);
    expect(JSON.parse(envelope.correlationJson)).toEqual({ accountId: "account-1" });
    expect(JSON.parse(envelope.sanitizedContextJson)).toEqual({ nested: { value: "kept" }, safe: true });
  });
});


