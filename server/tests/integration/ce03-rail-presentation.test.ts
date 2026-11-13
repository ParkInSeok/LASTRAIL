import {readFile} from "node:fs/promises";
import {describe,it,expect} from "vitest";
import * as rail from "../../scripts/local/ce03-rail-presentation";
import {projectPresentationAudit} from "../../scripts/local/ce03-presentation-projection";
import {sha256Hex} from "@lastrail/common";
export async function zoneFixture(){const base=new URL("../fixtures/rail-presentation-zone02-v1/",import.meta.url);
  const [audit,extension,selection,asset,zone02Selection,directionManifest]=await Promise.all(["presentation-closure-audit.json","second-final-closure-extension.json","design-manifest-v3.json","approved.asset.txt","zone02-selection-manifest.json","direction-manifest.json"].map(name=>readFile(new URL(name,base),"utf8")));
  return {audit,extension,selection,asset,zone02Selection,directionManifest,profile:"second-final-zone02-v1" as const};}
async function fixture(){const base=new URL("../fixtures/rail-presentation-v1/",import.meta.url);
  const [audit,extension,selection,asset]=await Promise.all(["presentation-closure-audit.json","second-final-closure-extension.json","design-manifest-v3.json","approved.asset.txt"].map(name=>readFile(new URL(name,base),"utf8")));
  return {audit,extension,selection,asset};}
describe("explicit rail presentation profile",()=>{
  it("accepts the exact approved roles only in the new profile while legacy still rejects",async()=>{
    const f=await fixture();await expect(projectPresentationAudit(JSON.parse(f.audit),f.asset)).rejects.toThrow("unexpected_role");
    const p=await rail.projectRailPresentation(f);
    expect(p.version).toBe("ce03-rail-presentation-v1");expect(p.manifest.selectedResources).toHaveLength(22);
    expect(p.manifest.roles).toHaveLength(215);expect(p.manifest.bindings).toHaveLength(406);
  });
  it.each(["unknown-role","missing-role","wrong-ref","wrong-type","missing-selected","wrong-selected-hash","wrong-resource-path","broken-graph","arbitrary-orphan","orphan-sibling","selected-order","core-source-set"])("rejects %s",async mode=>{
    const f=await fixture(),audit=JSON.parse(f.audit),extension=JSON.parse(f.extension);
    if(mode==="unknown-role")audit.roles.push({...audit.roles[0],role:"approved.Unknown"});
    if(mode==="missing-role")audit.roles=audit.roles.filter((r:{role:string})=>r.role!=="approved.ActiveIconAtlas");
    if(mode==="wrong-ref")audit.roles.find((r:{role:string})=>r.role==="approved.SecondFinalResources.Array.data[0]").guid="0".repeat(32);
    if(mode==="wrong-type")audit.rows.find((r:{path:string})=>r.path===extension.selectedResources[0].path).type="UnityEngine.TextAsset";
    if(mode==="missing-selected")extension.selectedResources.pop();
    if(mode==="wrong-selected-hash")extension.selectedResources[0].assetRawSha256="0".repeat(64);
    if(mode==="wrong-resource-path")extension.selectedResources[0].path="Assets/../outside.png";
    if(mode==="broken-graph")audit.rows.find((r:{path:string})=>r.path===audit.root).directDependencies.push("Assets/missing.png");
    if(mode==="arbitrary-orphan")for(const r of audit.rows)if(r.path.endsWith("TMP - Font Asset Icon.psd"))r.path="Packages/com.unity.ugui/Other.psd";
    if(mode==="orphan-sibling"){const i=audit.rows.findIndex((r:{path:string})=>r.path.endsWith("TMP - Font Asset Icon.psd"));audit.rows.splice(i,0,{...audit.rows[i],localFileId:"999",type:"UnityEngine.Material"});}
    if(mode==="selected-order")extension.selectedResources.reverse();
    if(mode==="core-source-set")audit.sourceFiles[0]=audit.sourceFiles[1];
    await expect(rail.projectRailPresentation({...f,audit:JSON.stringify(audit),extension:JSON.stringify(extension)})).rejects.toThrow();
  });
});

describe("explicit Zone02 profile",()=>{
  it("projects actual Zone02 only when explicitly selected",async()=>{
    const f=await zoneFixture(); const p=await rail.projectRailPresentation(f);
    expect(p.version).toBe("ce03-rail-presentation-v2");expect(p.manifest.roles).toHaveLength(216);
    expect(p.manifest.bindings).toHaveLength(407);expect(p.manifest.selectedResources).toHaveLength(22);
    expect(p.manifest).toMatchObject({profile:"second-final-zone02-v1",zone02SelectionManifestSha256:"53bd20b8f4630f8d55e667f81ddd3f24053b803b1aa93b88e377c4c3adce6dfc"});
    await expect(rail.projectRailPresentation({...f,profile:undefined})).rejects.toThrow();
  });
  it.each(["missing","duplicate-role","extra-role","wrong-path","wrong-type","wrong-guid","wrong-meta","source-hash","layout-source","wrong-direction-hash","wrong-direction-entry"])("rejects Zone02 %s",async mode=>{
    const f=await zoneFixture(),s=JSON.parse(f.zone02Selection);
    if(mode==="missing")s.resources.pop();
    if(mode==="duplicate-role")s.resources[1].roles.push("approved.DistantFar");
    if(mode==="extra-role")s.resources[0].roles.push("approved.Interior");
    if(mode==="wrong-path")s.resources[0].path=s.resources[1].path;
    if(mode==="wrong-type")s.resources[0].type="UnityEngine.TextAsset";
    if(mode==="wrong-guid")s.resources[0].guid="0".repeat(32);
    if(mode==="wrong-meta")s.resources[0].metaRawSha256="0".repeat(64);
    if(mode==="source-hash")s.resources[0].sourceRawSha256="0".repeat(64);
    if(mode==="layout-source")s.resources[4].sourceFile="layout.json";
    if(mode==="wrong-direction-hash")s.directionManifest.sha256="0".repeat(64);
    if(mode==="wrong-direction-entry"){const d=JSON.parse(f.directionManifest);d.assets[0].sha256="0".repeat(64);f.directionManifest=JSON.stringify(d);s.directionManifest.sha256=await sha256Hex(f.directionManifest);}
    await expect(rail.projectRailPresentation({...f,zone02Selection:JSON.stringify(s)})).rejects.toThrow();
  });
});
