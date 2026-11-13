import {z} from "zod";
import {sha,pos} from "./ce03-local-schema";
const path=z.string().min(1).refine(p=>/^(Assets|Packages)\//u.test(p)&&!p.includes("\\")&&!p.includes(":")&&!p.split("/").some(s=>!s||s==="."||s===".."));
const guid=z.string().regex(/^[a-f0-9]{32}$/u),localFileId=z.string().regex(/^-?\d+$/u);
const type=z.enum(["LastRail.Battle.CombatPresentationAssets","LastRail.Battle.CombatOperatorRigSet","LastRail.Battle.CombatSupportFireAssets",
  "TMPro.TMP_FontAsset","UnityEditor.MonoScript","UnityEngine.Font","UnityEngine.Material","UnityEngine.Shader","UnityEngine.Sprite","UnityEngine.TextAsset","UnityEngine.Texture2D"]);
export const railRoleSchema=z.strictObject({role:z.string(),path,guid,localFileId,type});
export const railBindingSchema=z.strictObject({path,name:z.string(),guid,localFileId,type,assetRawSha256:sha,metaRawSha256:sha,loadVerified:z.literal(true),directDependencies:z.array(path)});
export const railSelectedSchema=z.strictObject({resourceKey:z.string(),path,guid,localFileId,assetRawSha256:sha,metaRawSha256:sha,width:pos.max(512),height:pos.max(512),
  runtimeBytes:pos,rootReferenced:z.literal(true),resourcesLoadSameObject:z.literal(true)});
export const railSourceSchema=z.strictObject({path,sha256:sha});
export const railSelectedFiles=["artillery_emplacement-v01.png","console_bezel-v01.png","console_plate-v01.png","control_handle-v01.png","eve-v02.png","kyle-v02.png","mira-v02.png","reina-v02.png","sera-v02.png",
  "support_barrier-v01.png","support_fire-v01.png","support_repair-v01.png","title_emblem-v01.png","train_carriage_armored-v03.png","train_carriage_cargo-v03.png","train_locomotive-v03.png",
  "ultimate_eve-v01.png","ultimate_kyle-v01.png","ultimate_mira-v01.png","ultimate_reina-v02.png","ultimate_sera-v01.png","artillery_ledge-v01.png"] as const;
const texture="UnityEngine.Texture2D";
export const railRoleTypes:Readonly<Record<string,string>>=Object.freeze({
  ...Object.fromEntries(["Console","IconAtlas","Buggy","Bike","TurretRig","ActiveIconAtlas","DistantFar","DistantTerrain","PursuitRoad","PursuitRidges","PursuitMarkers",
    "ExteriorMap","ExteriorTrain","Interior","DefaultTurret","Hit","Skill","Shield"].map(k=>[`approved.${k}`,texture])),
  "approved.m_Script":"UnityEditor.MonoScript","approved.Font":"TMPro.TMP_FontAsset","approved.Frame":"UnityEngine.Sprite","approved.Primary":"UnityEngine.Sprite","approved.Secondary":"UnityEngine.Sprite",
  "approved.DistantTerrainLayout":"UnityEngine.TextAsset","approved.PursuitLayout":"UnityEngine.TextAsset","approved.OperatorRigs":"LastRail.Battle.CombatOperatorRigSet","approved.SupportFire":"LastRail.Battle.CombatSupportFireAssets",
  ...Object.fromEntries([...[...Array(6).keys()].map(i=>`OperatorAtlases.Array.data[${i}]`),...[...Array(160).keys()].map(i=>`Frames.Array.data[${i}]`),...[...Array(22).keys()].map(i=>`SecondFinalResources.Array.data[${i}]`)].map(k=>[`approved.${k}`,texture]))
});
const manifestBase=z.strictObject({root:z.literal("Assets/_LASTRAIL/Resources/CombatPresentation/approved.asset"),
  auditSha256:sha,extensionSha256:sha,selectionManifestSha256:sha,roles:z.array(railRoleSchema).length(215),bindings:z.array(railBindingSchema).min(1).max(2048),
  selectedResources:z.array(railSelectedSchema).length(22),sourceFiles:z.array(railSourceSchema).min(4).max(5000)});
function validateClosure(m:z.infer<typeof manifestBase>,ctx:z.RefinementCtx,roleTypes:Readonly<Record<string,string>>){
  const fail=(message:string)=>ctx.addIssue({code:"custom",message});const key=(r:{guid:string;localFileId:string})=>`${r.guid}:${r.localFileId}`;
  const byKey=new Map(m.bindings.map(r=>[key(r),r])),byPath=new Map(m.bindings.map(r=>[r.path,r]));
  if(byKey.size!==m.bindings.length)fail("duplicate typed binding");
  for(const row of m.bindings){const original=byPath.get(row.path)!;
    if(row.guid!==original.guid||row.assetRawSha256!==original.assetRawSha256||row.metaRawSha256!==original.metaRawSha256||JSON.stringify(row.directDependencies)!==JSON.stringify(original.directDependencies))fail("inconsistent subasset");
    if(new Set(row.directDependencies).size!==row.directDependencies.length||row.directDependencies.some(d=>!byPath.has(d)||d===row.path))fail("dependency closure");}
  if(new Set([...byPath.values()].map(r=>r.guid)).size!==byPath.size)fail("guid path alias");
  if(new Set(m.roles.map(r=>r.role)).size!==Object.keys(roleTypes).length)fail("role set");
  for(const r of m.roles){const binding=byKey.get(key(r));if(roleTypes[r.role]!==r.type||!binding||binding.path!==r.path||binding.type!==r.type)fail("rail role identity");}
  if(m.bindings.filter(r=>r.path===m.root&&r.type==="LastRail.Battle.CombatPresentationAssets").length!==1)fail("root identity");
  const seen=new Set<string>(),queue=[m.root as string];while(queue.length){const p=queue.pop()!;if(seen.has(p))continue;seen.add(p);queue.push(...(byPath.get(p)?.directDependencies??[]));}
  // Unity's recursive AssetDatabase closure includes this TMP editor icon via
  // importer metadata, although GetDependencies(path,false) omits that edge.
  // Preserve and hash it; permit only this exact observed editor sidecar.
  const editorIcon="Packages/com.unity.ugui/Editor Resources/Gizmos/TMP - Font Asset Icon.psd";
  const detached=m.bindings.filter(row=>!seen.has(row.path));
  if(detached.length>1||detached.some(row=>row.path!==editorIcon||row.type!==texture||row.localFileId!=="2800000"))fail("orphan physical binding");
  const selectedPaths=m.selectedResources.map(r=>r.path);if(new Set(selectedPaths).size!==22)fail("selected duplicates");
  for(const [index,r] of m.selectedResources.entries()){const file=r.path.split("/").at(-1)!;const binding=byKey.get(key(r));
    if(!railSelectedFiles.includes(file as typeof railSelectedFiles[number])||r.path!==`Assets/_LASTRAIL/Resources/CombatSecondFinal/${file}`||r.resourceKey!==`CombatSecondFinal/${file.slice(0,-4)}`||
      !binding||binding.path!==r.path||binding.type!==texture||binding.assetRawSha256!==r.assetRawSha256||binding.metaRawSha256!==r.metaRawSha256||
      !m.roles.some(role=>role.role===`approved.SecondFinalResources.Array.data[${index}]`&&key(role)===key(r)))fail("selected identity");}
  if(new Set(m.sourceFiles.map(s=>s.path)).size!==m.sourceFiles.length||m.sourceFiles.some(s=>!s.path.startsWith("Assets/_LASTRAIL/")||!s.path.endsWith(".cs")))fail("source set");
}
export const railPresentationManifestSchema=manifestBase.extend({profile:z.literal("second-final-selected-v3")}).superRefine((m,ctx)=>validateClosure(m,ctx,railRoleTypes));
const zoneRoot="Assets/_LASTRAIL/UI/ApprovedCombat/ExteriorQuality/";
export const railZone02Files:Readonly<Record<string,readonly string[]>>=Object.freeze({
  "canyon-far-v02.png":["approved.DistantFar","approved.ExteriorMap"],
  "canyon-mid-ridge-v02.png":["approved.PursuitRidges"],"canyon-road-v02.png":["approved.PursuitRoad"],
  "exterior-bezel-v02.png":["approved.ExteriorBezel"],"pursuit-layout-v02.json":["approved.PursuitLayout"]
});
export const railZone02SelectionSchema=z.strictObject({schema:z.literal("combat-zone02-selection-v1"),profile:z.literal("second-final-zone02-v1"),scope:z.literal("APPLICATION_ASSET_IDENTIFICATION"),
  directionManifest:z.strictObject({path:z.string().regex(/^D:\/Project\/LASTRAIL\//u).refine(p=>!p.includes("\\")&&!p.slice(2).includes(":")&&!p.split("/").some(s=>!s||s==="."||s==="..")),sha256:sha}),
  resources:z.array(z.strictObject({path,assetRawSha256:sha,metaRawSha256:sha,guid,localFileId,type:z.enum(["UnityEngine.Texture2D","UnityEngine.TextAsset"]),roles:z.array(z.string()).min(1).max(2),sourceFile:z.string().nullable(),sourceRawSha256:sha.nullable()})).length(5)
}).superRefine((s,ctx)=>{
  const fail=()=>ctx.addIssue({code:"custom",message:"zone02 selection identity"});
  if(new Set(s.resources.map(r=>r.path)).size!==5)fail();
  for(const r of s.resources){const file=r.path.slice(zoneRoot.length),roles=railZone02Files[file],layout=file==="pursuit-layout-v02.json";
    if(!r.path.startsWith(zoneRoot)||!roles||JSON.stringify([...r.roles].sort())!==JSON.stringify([...(roles??[])].sort())||r.type!==(layout?"UnityEngine.TextAsset":texture)||r.localFileId!==(layout?"4900000":"2800000")||
      (layout?(r.sourceFile!==null||r.sourceRawSha256!==null):(r.sourceFile!==file||r.sourceRawSha256!==r.assetRawSha256)))fail();
  }
});
export const railZone02ManifestSchema=manifestBase.extend({profile:z.literal("second-final-zone02-v1"),roles:z.array(railRoleSchema).length(216),
  zone02SelectionManifestSha256:sha,zone02Selection:railZone02SelectionSchema}).superRefine((m,ctx)=>{
  validateClosure(m,ctx,{...railRoleTypes,"approved.ExteriorBezel":texture});
  for(const r of m.zone02Selection.resources){
    const binding=m.bindings.find(b=>b.guid===r.guid&&b.localFileId===r.localFileId);
    if(!binding||binding.path!==r.path||binding.type!==r.type||binding.assetRawSha256!==r.assetRawSha256||binding.metaRawSha256!==r.metaRawSha256||
      r.roles.some(name=>!m.roles.some(role=>role.role===name&&role.path===r.path&&role.type===r.type&&role.guid===r.guid&&role.localFileId===r.localFileId)))ctx.addIssue({code:"custom",message:"zone02 binding identity"});
  }
});
export const railPresentationSchema=z.union([
  z.strictObject({version:z.literal("ce03-rail-presentation-v1"),manifestSha256:sha,manifest:railPresentationManifestSchema}),
  z.strictObject({version:z.literal("ce03-rail-presentation-v2"),manifestSha256:sha,manifest:railZone02ManifestSchema})
]);
export type RailPresentation=z.infer<typeof railPresentationSchema>;
// Keep the original ce03-presentation-manifest-v1 and admission schemas closed.
export const railDesignSchema=z.object({version:z.literal(3),assets:z.array(z.object({file:z.string(),sha256:z.string().regex(/^[a-fA-F0-9]{64}$/u),width:pos,height:pos})).length(22)});
export const railExtensionSchema=z.object({schema:z.literal("second-final-current-closure-v1"),actualAdmissionReady:z.literal(false),sourceFiles:z.array(railSourceSchema).min(4).max(5000),selectedResources:z.array(railSelectedSchema).length(22)});
export const railAuditSchema=z.object({schema:z.literal("ce03-presentation-closure-audit-v1"),root:path,unityVersion:z.string().min(1),roles:z.array(railRoleSchema),rows:z.array(railBindingSchema),
  sourceFiles:z.array(railSourceSchema).length(4),operatorIds:z.array(z.string()).length(6),frameKeys:z.array(z.string()).length(160),unresolved:z.array(z.unknown()).length(0),actualAdmissionReady:z.literal(false)});
