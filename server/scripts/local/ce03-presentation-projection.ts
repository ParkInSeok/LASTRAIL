import { createHash } from "node:crypto";
import { readFile, realpath } from "node:fs/promises";
import { isAbsolute, relative, resolve, sep } from "node:path";
import { z } from "zod";
import { presentationBindingSchema, validatePresentationManifest } from "../../apps/game-api/src/ce03-presentation-manifest";
import { sha } from "../../apps/game-api/src/ce03-local-schema";

const assetPath = z.string().min(1).refine(p => /^(Assets|Packages)\//u.test(p) && !p.includes("\\") &&
  !p.includes(":") && !p.split("/").some(s => !s || s === "." || s === ".."));
const locator = presentationBindingSchema.shape.locator.shape;
const ref = z.object({ path: assetPath, guid: locator.guid, localFileId: locator.localFileId, type: z.string() });
const row = ref.extend({ name: z.string(), assetRawSha256: sha, metaRawSha256: sha, loadVerified: z.literal(true), directDependencies: z.array(assetPath) });
const auditSchema = z.object({ schema: z.literal("ce03-presentation-closure-audit-v1"), root: assetPath, unityVersion: z.string().min(1),
  rows: z.array(row).min(1).max(2048), roles: z.array(ref.extend({ role: z.string() })),
  operatorIds: z.array(z.string()).length(6), frameKeys: z.array(z.string()).length(160), unresolved: z.array(z.unknown()).length(0),
  sourceFiles: z.array(z.object({ path: assetPath, sha256: sha })).length(4) });
const sourceNames = { ApprovedCombatView: "approvedCombatView", CombatPresentationAssets: "combatPresentationAssets",
  CombatWindowView: "combatWindowView", CombatHudWidgets: "combatHudWidgets" } as const;
const fixedRoles = { font: "Font", console: "Console", icons: "IconAtlas", map: "ExteriorMap", trainExterior: "ExteriorTrain", interior: "Interior",
  defaultTurret: "DefaultTurret", turretRig: "TurretRig", buggy: "Buggy", bike: "Bike", hit: "Hit", skill: "Skill", shield: "Shield",
  frame: "Frame", primary: "Primary", secondary: "Secondary" } as const;
const hash = (bytes: Uint8Array | string) => createHash("sha256").update(bytes).digest("hex");
const fail = (reason: string): never => { throw new Error(`ce03_presentation_${reason}`); };
const key = (r: { guid: string; localFileId: string }) => `${r.guid}:${r.localFileId}`;
const bindingId = (r: { guid: string; localFileId: string }) => `binding.render.${r.guid}.${r.localFileId}`;

/** Project owner-generated AssetDatabase evidence; this does not replace the
 * actual Editor loader/type/localFileId tests required by report-v3. */
export async function projectPresentationAudit(input: unknown, approvedAsset: string) {
  const a = auditSchema.parse(input), byKey = new Map(a.rows.map(r => [key(r), r]));
  if (byKey.size !== a.rows.length) return fail("duplicate_locator");
  const byPath = new Map<string, typeof a.rows>();
  for (const r of a.rows) {
    const siblings = byPath.get(r.path) ?? [];
    if (siblings.some(s => s.guid !== r.guid || s.assetRawSha256 !== r.assetRawSha256 || s.metaRawSha256 !== r.metaRawSha256 ||
      JSON.stringify([...s.directDependencies].sort()) !== JSON.stringify([...r.directDependencies].sort()))) return fail("subasset_identity_mismatch");
    if (new Set(r.directDependencies).size !== r.directDependencies.length) return fail("duplicate_dependency");
    siblings.push(r); byPath.set(r.path, siblings);
  }
  if (new Set([...byPath.values()].map(rows => rows[0]!.guid)).size !== byPath.size) return fail("guid_path_mismatch");
  const root = a.rows.filter(r => r.path === a.root && r.type === "LastRail.Battle.CombatPresentationAssets");
  if (root.length !== 1 || hash(approvedAsset) !== root[0]!.assetRawSha256) return fail("configuration_mismatch");
  // Match serialized properties against the raw hash-pinned Unity YAML. The
  // parser intentionally supports only this exported Unity scalar/ref format.
  const properties = new Map<string, string[]>(); let current = "";
  for (const line of approvedAsset.split(/\r?\n/u)) {
    const property = /^ {2}([A-Za-z_][A-Za-z_0-9]*):\s*(.*)$/u.exec(line);
    if (property) { current = property[1]!; if (properties.has(current)) return fail("duplicate_property"); properties.set(current, property[2] ? [property[2]] : []); }
    else if (line.startsWith("  - ") && current) properties.get(current)!.push(line.slice(4));
  }
  for (const [name, expected] of [["OperatorIds", a.operatorIds], ["Keys", a.frameKeys]] as const)
    if (JSON.stringify(properties.get(name)) !== JSON.stringify(expected)) return fail("serialized_index_mismatch");
  const roleMap = new Map(a.roles.map(r => [r.role, r]));
  if (roleMap.size !== a.roles.length) return fail("duplicate_role");
  const usedRoles = new Set<string>();
  const useRole = (name: string) => {
    const r = roleMap.get(`approved.${name}`); if (!r) return fail("role_missing");
    usedRoles.add(r.role);
    const target = byKey.get(key(r));
    if (!target || target.path !== r.path || target.type !== r.type) return fail("role_identity_mismatch");
    const indexed = /^(\w+)\.Array\.data\[(\d+)\]$/u.exec(name);
    const value = properties.get(indexed ? indexed[1]! : name)?.[indexed ? Number(indexed[2]) : 0];
    const serialized = value && /^\{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: \d+\}$/u.exec(value);
    if (!serialized || serialized[1] !== r.localFileId || serialized[2] !== r.guid) return fail("serialized_role_mismatch");
    return bindingId(r);
  };
  useRole("m_Script");
  const fixed = Object.fromEntries(Object.entries(fixedRoles).map(([role, name]) => [role, useRole(name)]));
  const actorAtlases = a.operatorIds.map((actorId, i) => ({ actorId, bindingId: useRole(`OperatorAtlases.Array.data[${i}]`) })).sort((x, y) => x.actorId < y.actorId ? -1 : 1);
  const frames = a.frameKeys.map((key, i) => ({ key, bindingId: useRole(`Frames.Array.data[${i}]`) })).sort((x, y) => x.key < y.key ? -1 : 1);
  if (usedRoles.size !== a.roles.length || properties.get("OperatorAtlases")?.length !== 6 || properties.get("Frames")?.length !== 160) return fail("unexpected_role");
  const bindings = a.rows.map(r => {
    const type = r.type === "LastRail.Battle.CombatPresentationAssets" ? "CombatPresentationAssets" :
      r.type === "TMPro.TMP_FontAsset" ? "TMP_FontAsset" : r.type === "UnityEditor.MonoScript" ? "MonoScript" : r.type.startsWith("UnityEngine.") ? r.type.slice(12) : "UNKNOWN";
    const dependencies = new Set((byPath.get(r.path) ?? []).filter(s => key(s) !== key(r)).map(bindingId));
    for (const path of r.directDependencies) {
      const rows = byPath.get(path); if (!rows) return fail("dependency_missing");
      for (const dep of rows) if (key(dep) !== key(r)) dependencies.add(bindingId(dep));
    }
    return { bindingId: bindingId(r), actorOrRoleId: "presentation.asset", assetType: type,
      locator: { kind: "UNITY_ASSET_GUID", guid: r.guid, localFileId: r.localFileId }, assetRawSha256: r.assetRawSha256, metaRawSha256: r.metaRawSha256,
      dependencies: [...dependencies].sort(), importValidation: { unityVersion: a.unityVersion, importerVersion: null, loadVerified: true } };
  }).sort((x, y) => x.bindingId < y.bindingId ? -1 : 1);
  const sourceFiles = Object.entries(sourceNames).map(([file, logicalName]) => {
    const rows = a.sourceFiles.filter(s => s.path === `Assets/_LASTRAIL/Scripts/Battle/${file}.cs`);
    if (rows.length !== 1) return fail("source_identity_missing");
    return { logicalName, rawSha256: rows[0]!.sha256 };
  }).sort((x, y) => x.logicalName < y.logicalName ? -1 : 1);
  return validatePresentationManifest({ schemaVersion: "ce03-presentation-manifest-v1", rendererVersion: "approved-combat-view-v1",
    roles: { configuration: bindingId(root[0]!), ...fixed, actorAtlases, frames }, bindings, sourceFiles });
}

/** Recheck physical raw/meta/GUID and renderer source identity. readAsset must
 * resolve trusted Assets/Packages roots; no audit-supplied absolute path is used. */
export async function verifyPresentationFiles(input: unknown, readAsset: (path: string) => Promise<Uint8Array>) {
  const a = auditSchema.parse(input), paths = new Map(a.rows.map(r => [r.path, r]));
  for (const [path, r] of paths) {
    const asset = await readAsset(path), meta = await readAsset(`${path}.meta`);
    const guid = /^guid: ([a-f0-9]{32})\r?$/mu.exec(new TextDecoder("utf8", { fatal: true, ignoreBOM: false }).decode(meta));
    if (hash(asset) !== r.assetRawSha256 || hash(meta) !== r.metaRawSha256 || guid?.[1] !== r.guid) return fail("physical_identity_mismatch");
  }
  for (const s of a.sourceFiles) if (hash(await readAsset(s.path)) !== s.sha256) return fail("source_changed");
  return { physicalFilesVerified: paths.size, sourceFilesVerified: a.sourceFiles.length };
}

export async function loadPresentationFromAudit(auditPath: string, projectRoot: string, packageRoots: Readonly<Record<string, string>>) {
  const auditBytes = await readFile(auditPath), a = auditSchema.parse(JSON.parse(new TextDecoder("utf8", { fatal: true, ignoreBOM: false }).decode(auditBytes)));
  const readAsset = async (path: string) => {
    assetPath.parse(path);
    const parts = path.split("/"), isPackage = parts[0] === "Packages";
    const configuredRoot = isPackage ? packageRoots[parts[1]!] : resolve(projectRoot, "Assets");
    if (!configuredRoot) return fail("package_root_missing");
    const root = await realpath(configuredRoot), file = await realpath(resolve(root, parts.slice(isPackage ? 2 : 1).join("/")));
    const rel = relative(root, file);
    if (!rel || isAbsolute(rel) || rel === ".." || rel.startsWith(`..${sep}`)) return fail("path_escape");
    return readFile(file);
  };
  const manifest = await projectPresentationAudit(a, new TextDecoder("utf8", { fatal: true, ignoreBOM: true }).decode(await readAsset(a.root)));
  const verification = await verifyPresentationFiles(a, readAsset);
  return { manifest, verification: { ...verification, auditRawSha256: hash(auditBytes) } };
}
