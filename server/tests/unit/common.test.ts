import { describe, expect, it } from "vitest";
import { canonicalJson, sha256Hex, assertSafeInteger, createRequestId } from "@lastrail/common";

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
});


