import { z } from "zod";
import { createReadStream } from "node:fs";
import { createHash } from "node:crypto";
import { realpath, stat } from "node:fs/promises";
import { isAbsolute, relative, resolve, sep } from "node:path";
import { canonicalJson } from "@lastrail/common";

export const dynamicResourceManifestPath = "QA/Ce03CommandIntegration/dynamic-resource-closure.json";
const abilities = ["op_kyle.active", "op_kyle.ultimate", "op_reina.active", "op_reina.ultimate", "op_eve.active", "op_eve.ultimate", "op_sera.active", "op_sera.ultimate", "op_mira.active", "op_mira.ultimate", "support_fire", "barrier", "emergency_repair"];
const expected = new Map<string, "AudioClip" | "TextAsset">([
  ...abilities.map(id => [`CombatPresentation/Audio/${id}`, "AudioClip"] as const),
  ...abilities.map(id => [`CombatPresentation/EffectAudio/${id}-0`, "AudioClip"] as const),
  ...["op_kyle.active-1", "op_sera.active-1", "op_sera.active-2", "op_sera.ultimate-1", "op_mira.ultimate-1"].map(id => [`CombatPresentation/EffectAudio/${id}`, "AudioClip"] as const),
  ["CombatPresentation/Audio/levels", "TextAsset"], ["CombatPresentation/EffectAudio/bindings", "TextAsset"],
]);
const hash = z.string().regex(/^[a-f0-9]{64}$/u);
const int64 = z.string().regex(/^(0|-[1-9][0-9]*|[1-9][0-9]*)$/u).refine(v => { const n = BigInt(v); return n >= -(2n ** 63n) && n <= 2n ** 63n - 1n; });
const manifestSchema = z.strictObject({ schema: z.literal("ce03-dynamic-resource-closure-v1"), unityVersion: z.literal("6000.3.22f1"),
  rows: z.array(z.strictObject({ resourceKey: z.string(), path: z.string(), type: z.enum(["AudioClip", "TextAsset"]),
    guid: z.string().regex(/^[a-f0-9]{32}$/u), localFileId: int64, rawSha256: hash, metaSha256: hash })).length(33),
});
async function fileHash(path: string) {
  const hash = createHash("sha256"); for await (const bytes of createReadStream(path)) hash.update(bytes); return hash.digest("hex");
}
async function confined(root: string, name: string) {
  const path = await realpath(resolve(root, name)), rel = relative(root, path);
  if (!rel || isAbsolute(rel) || rel === ".." || rel.startsWith(`..${sep}`) || !(await stat(path)).isFile()) throw new Error("ce03_runner_dynamic_resource_path_invalid");
  return path;
}
// Manifest content must already be part of the reviewed canonical source bundle.
// Unity remains responsible for actual GUID/localFileId/type/Resources.Load checks.
export async function pinDynamicResources(root: string, content: string) {
  const manifest = manifestSchema.parse(JSON.parse(content)), keys = [...expected.keys()].sort();
  if (canonicalJson(manifest) !== content || new Set(manifest.rows.map(r => r.guid)).size !== 33 ||
      manifest.rows.some((r, i) => r.resourceKey !== keys[i] || r.type !== expected.get(r.resourceKey) ||
        r.path !== `Assets/_LASTRAIL/Resources/${r.resourceKey}${r.type === "AudioClip" ? ".wav" : ".json"}`)) throw new Error("ce03_runner_dynamic_resource_manifest_invalid");
  const pinned: { name: string; path: string; sha256: string }[] = [];
  for (const row of manifest.rows) for (const [name, sha256] of [[row.path, row.rawSha256], [row.path + ".meta", row.metaSha256]] as const) {
    const path = await confined(root, name);
    if (pinned.some(p => p.path.toLowerCase() === path.toLowerCase())) throw new Error("ce03_runner_dynamic_resource_path_duplicate");
    if (await fileHash(path) !== sha256) throw new Error("ce03_runner_dynamic_resource_mismatch");
    pinned.push({ name, path, sha256 });
  }
  return async () => {
    try { for (const pin of pinned) if (await confined(root, pin.name) !== pin.path || await fileHash(pin.path) !== pin.sha256) return false; return true; }
    catch { return false; }
  };
}
