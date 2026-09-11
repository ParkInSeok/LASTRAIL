import { describe, expect, it } from "vitest";
import { bindingSchema, contentSchema } from "../../apps/game-api/src/ce03-local-schema";
import { presentationManifestSchema, validatePresentationManifest, presentationContentSchema, compatibleContentSchema } from "../../apps/game-api/src/ce03-presentation-manifest";

import { presentationFixture as fixture } from "../helpers/ce03-presentation-fixture";
const zero = "0".repeat(64);
describe("versioned actual renderer manifest (synthetic locators only)", () => {
  it("accepts explicitly typed Texture2D roles only in new content and keeps r4 closed", () => {
    const manifest = fixture(); expect(presentationManifestSchema.safeParse(manifest).success).toBe(true); validatePresentationManifest(manifest);
    expect(bindingSchema.safeParse(manifest.bindings.find(b => b.assetType === "Texture2D")).success).toBe(false);
    const content = { schemaVersion: "ce03-isolated-presentation-content-r6", version: "test", profileSha256: zero,
      resolver: { mode: "UNITY_EDITOR_ASSET_DATABASE", playerBuildAllowed: false }, legacyBindings: [manifest.bindings.find(b => b.assetType === "Sprite")], presentationManifest: manifest };
    expect(presentationContentSchema.safeParse(content).success).toBe(true); expect(compatibleContentSchema.safeParse(content).success).toBe(true);
    expect(contentSchema.safeParse(content).success).toBe(false);
  });
  it("rejects missing/wrong roles, unknown types, dependencies, hashes, duplicates and removed referenced frames", () => {
    for (const mutate of [
      (m: ReturnType<typeof fixture>) => { m.bindings = m.bindings.filter(b => b.bindingId !== m.roles.console); },
      (m: ReturnType<typeof fixture>) => { m.bindings.find(b => b.bindingId === m.roles.console)!.assetType = "Sprite"; },
      (m: ReturnType<typeof fixture>) => { m.bindings[0]!.assetType = "Anything"; },
      (m: ReturnType<typeof fixture>) => { m.bindings[0]!.dependencies = ["binding.missing"]; },
      (m: ReturnType<typeof fixture>) => { m.bindings[0]!.assetRawSha256 = "fake"; },
      (m: ReturnType<typeof fixture>) => { m.bindings.push(m.bindings[0]!); },
      (m: ReturnType<typeof fixture>) => { m.roles.frames.pop(); },
      (m: ReturnType<typeof fixture>) => { m.roles.actorAtlases[0]!.actorId = "op_unowned"; },
    ]) { const m = fixture(); mutate(m); expect(() => validatePresentationManifest(m)).toThrow(); }
  });
});
