import { z } from "zod";
import { bindingSchema, contentSchema, id, sourceFileSchema } from "./ce03-local-schema";

const actors = ["op_kyle", "op_reina", "op_eve", "op_sera", "op_mira", "enemy_raider_scout"] as const;
const animatedActors = [...actors, "enemy_spike_cart_gunner", "enemy_railbike_marauder"];
const states = ["idle", "attack", "hit", "down_or_defeated", "skill_or_special"];
export const presentationSourcePaths: Readonly<Record<string, string>> = Object.freeze({
  approvedCombatView: "Assets/_LASTRAIL/Scripts/Battle/ApprovedCombatView.cs",
  combatHudWidgets: "Assets/_LASTRAIL/Scripts/Battle/CombatHudWidgets.cs",
  combatPresentationAssets: "Assets/_LASTRAIL/Scripts/Battle/CombatPresentationAssets.cs",
  combatWindowView: "Assets/_LASTRAIL/Scripts/Battle/CombatWindowView.cs",
});
const expectedFrameKeys = new Set(animatedActors.flatMap(actor => states.flatMap(state => [1, 2, 3, 4].map(i => `${actor}/${state}/${i}`))));
// Historical bindingSchema stays closed to its original three types.
export const presentationBindingSchema = bindingSchema.extend({ assetType: z.enum(["Sprite", "Texture2D", "AnimationClip", "AnimatorController",
  "MonoScript", "Material", "Shader", "CombatPresentationAssets", "Font", "TMP_FontAsset"]),
  importValidation: z.strictObject({ unityVersion: z.string().min(1).max(128), importerVersion: z.string().max(128).nullable(), loadVerified: z.literal(true) }) });
const rolesSchema = z.strictObject({ configuration: id, font: id, console: id, icons: id, map: id, trainExterior: id, interior: id,
  defaultTurret: id, turretRig: id, buggy: id, bike: id, hit: id, skill: id, shield: id, frame: id, primary: id, secondary: id,
  actorAtlases: z.array(z.strictObject({ actorId: z.enum(actors), bindingId: id })).length(6),
  frames: z.array(z.strictObject({ key: z.string().min(1).max(128), bindingId: id })).length(160) });
export const presentationManifestSchema = z.strictObject({ schemaVersion: z.literal("ce03-presentation-manifest-v1"),
  rendererVersion: z.literal("approved-combat-view-v1"), roles: rolesSchema,
  bindings: z.array(presentationBindingSchema).min(1).max(2048), sourceFiles: z.array(sourceFileSchema).length(4),
}).superRefine((m, ctx) => {
  const fail = (message: string) => ctx.addIssue({ code: "custom", message });
  const byId = new Map(m.bindings.map(b => [b.bindingId, b]));
  if (byId.size !== m.bindings.length) fail("duplicate binding");
  const locators = new Set(m.bindings.map(b => `${b.locator.guid}:${b.locator.localFileId}`));
  if (locators.size !== m.bindings.length) fail("duplicate typed locator");
  for (let i = 0; i < m.bindings.length; i++) {
    const b = m.bindings[i]!;
    if (i > 0 && m.bindings[i - 1]!.bindingId >= b.bindingId) fail("binding order");
    if (b.dependencies.some((d, index) => !byId.has(d) || d === b.bindingId || (index > 0 && b.dependencies[index - 1]! >= d))) fail("dependency closure/order");
  }
  const required = (bindingId: string, type: string) => { if (byId.get(bindingId)?.assetType !== type) fail(`role requires ${type}`); };
  required(m.roles.configuration, "CombatPresentationAssets"); required(m.roles.font, "TMP_FontAsset");
  for (const role of ["console", "icons", "map", "trainExterior", "interior", "defaultTurret", "turretRig", "buggy", "bike", "hit", "skill", "shield"] as const) required(m.roles[role], "Texture2D");
  for (const role of ["frame", "primary", "secondary"] as const) required(m.roles[role], "Sprite");
  if (new Set(m.roles.actorAtlases.map(a => a.actorId)).size !== actors.length) fail("actor atlas closure");
  if (new Set(m.roles.frames.map(a => a.key)).size !== expectedFrameKeys.size || m.roles.frames.some(f => !expectedFrameKeys.has(f.key))) fail("frame closure");
  for (const role of m.roles.actorAtlases) required(role.bindingId, "Texture2D");
  for (const role of m.roles.frames) required(role.bindingId, "Texture2D");
  if (m.roles.actorAtlases.some((r, i) => i > 0 && m.roles.actorAtlases[i - 1]!.actorId >= r.actorId) ||
      m.roles.frames.some((r, i) => i > 0 && m.roles.frames[i - 1]!.key >= r.key)) fail("role order");
  const sourceNames = new Set(m.sourceFiles.map(s => s.logicalName));
  if (sourceNames.size !== m.sourceFiles.length || ["approvedCombatView", "combatPresentationAssets", "combatWindowView", "combatHudWidgets"].some(s => !sourceNames.has(s)) ||
      m.sourceFiles.some((s, i) => i > 0 && m.sourceFiles[i - 1]!.logicalName >= s.logicalName)) fail("renderer code identity");
});
export function validatePresentationManifest(value: unknown) { return presentationManifestSchema.parse(value); }
export const presentationContentSchema = z.strictObject({ schemaVersion: z.literal("ce03-isolated-presentation-content-r6"),
  version: id, profileSha256: contentSchema.shape.profileSha256, resolver: contentSchema.shape.resolver,
  legacyBindings: z.array(bindingSchema).min(1), presentationManifest: presentationManifestSchema });
export const compatibleContentSchema = z.union([contentSchema, presentationContentSchema]);
