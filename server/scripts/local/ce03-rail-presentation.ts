import {readFile,readdir,realpath} from "node:fs/promises";
import {createHash} from "node:crypto";
import {resolve,relative,isAbsolute,sep,dirname} from "node:path";
import {canonicalJson,sha256Hex} from "@lastrail/common";
import {z} from "zod";
import {railAuditSchema,railExtensionSchema,railDesignSchema,railPresentationManifestSchema,railPresentationSchema,railZone02SelectionSchema,railZone02ManifestSchema} from "../../apps/game-api/src/ce03-rail-presentation";
import {verifyPresentationFiles} from "./ce03-presentation-projection";
const requireValid=(value:unknown,message:string)=>{if(!value)throw new Error(`ce03_rail_presentation_${message}`);};
type Inputs={audit:string;extension:string;selection:string;asset:string;profile?:"second-final-zone02-v1";zone02Selection?:string;directionManifest?:string};
const directionSchema=z.object({assets:z.array(z.object({file:z.string(),sha256:z.string().regex(/^[a-fA-F0-9]{64}$/u),width:z.number().int().positive(),height:z.number().int().positive()})).min(4)});
/** Separate local rail profile: no role filtering and no legacy schema widening. */
export async function projectRailPresentation(input:Inputs){
  const audit=railAuditSchema.parse(JSON.parse(input.audit)),extension=railExtensionSchema.parse(JSON.parse(input.extension)),design=railDesignSchema.parse(JSON.parse(input.selection));
  requireValid(input.profile==="second-final-zone02-v1"||(!input.zone02Selection&&!input.directionManifest),"explicit_profile");
  const zone=input.profile==="second-final-zone02-v1";
  let zoneFields={};
  if(zone){requireValid(input.zone02Selection&&input.directionManifest,"zone02_inputs");
    const selected=railZone02SelectionSchema.parse(JSON.parse(input.zone02Selection!)),direction=directionSchema.parse(JSON.parse(input.directionManifest!));
    requireValid(await sha256Hex(input.directionManifest!)===selected.directionManifest.sha256,"direction_manifest_hash");
    requireValid(new Set(direction.assets.map(a=>a.file)).size===direction.assets.length,"direction_duplicates");
    for(const r of selected.resources.filter(r=>r.sourceFile!==null))requireValid(direction.assets.some(a=>a.file===r.sourceFile&&a.sha256.toLowerCase()===r.sourceRawSha256),"direction_source_hash");
    zoneFields={zone02SelectionManifestSha256:await sha256Hex(input.zone02Selection!),zone02Selection:selected};
  }
  const manifest=(zone?railZone02ManifestSchema:railPresentationManifestSchema).parse({...zoneFields,profile:zone?"second-final-zone02-v1":"second-final-selected-v3",root:audit.root,auditSha256:await sha256Hex(input.audit),extensionSha256:await sha256Hex(input.extension),
    selectionManifestSha256:await sha256Hex(input.selection),roles:audit.roles,bindings:audit.rows,selectedResources:extension.selectedResources,sourceFiles:extension.sourceFiles});
  requireValid(await sha256Hex(input.asset)===audit.rows.find(r=>r.path===audit.root&&r.type==="LastRail.Battle.CombatPresentationAssets")!.assetRawSha256,"root_hash");
  const properties=new Map<string,string[]>();let current="";
  for(const line of input.asset.split(/\r?\n/u)){const property=/^ {2}([A-Za-z_][A-Za-z_0-9]*):\s*(.*)$/u.exec(line);
    if(property){current=property[1]!;requireValid(!properties.has(current),"duplicate_property");properties.set(current,property[2]?[property[2]]:[]);}
    else if(line.startsWith("  - ")&&current)properties.get(current)!.push(line.slice(4));}
  for(const role of audit.roles){const name=role.role.slice("approved.".length),indexed=/^(\w+)\.Array\.data\[(\d+)\]$/u.exec(name);
    const value=properties.get(indexed?indexed[1]!:name)?.[indexed?Number(indexed[2]):0],ref=value&&/^\{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: \d+\}$/u.exec(value);
    requireValid(ref&&ref[1]===role.localFileId&&ref[2]===role.guid,"serialized_role");}
  for(const [name,length] of [["OperatorAtlases",6],["Frames",160],["SecondFinalResources",22]] as const)requireValid(properties.get(name)?.length===length,"serialized_array_length");
  requireValid(JSON.stringify(properties.get("OperatorIds"))===JSON.stringify(audit.operatorIds)&&JSON.stringify(properties.get("Keys"))===JSON.stringify(audit.frameKeys),"serialized_indexes");
  const actors=["op_kyle","op_reina","op_eve","op_sera","op_mira","enemy_raider_scout"];
  requireValid(JSON.stringify([...audit.operatorIds].sort())===JSON.stringify([...actors].sort()),"operator_set");
  const keys=[...actors,"enemy_spike_cart_gunner","enemy_railbike_marauder"].flatMap(a=>["idle","attack","hit","down_or_defeated","skill_or_special"].flatMap(s=>[1,2,3,4].map(i=>`${a}/${s}/${i}`)));
  requireValid(JSON.stringify([...audit.frameKeys].sort())===JSON.stringify(keys.sort()),"frame_set");
  requireValid(new Set(design.assets.map(a=>a.file)).size===22,"design_duplicates");
  requireValid(JSON.stringify(audit.sourceFiles.map(s=>s.path).sort())===JSON.stringify(["ApprovedCombatView","CombatPresentationAssets","CombatWindowView","CombatHudWidgets"].map(n=>`Assets/_LASTRAIL/Scripts/Battle/${n}.cs`).sort()),"core_source_set");
  for(const selected of extension.selectedResources){const entry=design.assets.find(a=>selected.path.endsWith(`/${a.file}`));requireValid(entry&&entry.sha256.toLowerCase()===selected.assetRawSha256,"design_hash");}
  for(const source of audit.sourceFiles)requireValid(extension.sourceFiles.some(s=>s.path===source.path&&s.sha256===source.sha256),"core_source_pin");
  return railPresentationSchema.parse({version:zone?"ce03-rail-presentation-v2":"ce03-rail-presentation-v1",manifestSha256:await sha256Hex(canonicalJson(manifest)),manifest});
}

export async function loadRailPresentationFromAudit(options:{auditPath:string;extensionPath:string;selectionPath:string;projectRoot:string;packageRoots:Readonly<Record<string,string>>;profile?:"second-final-zone02-v1";zone02SelectionPath?:string}){
  const [audit,extension,selection]=await Promise.all([options.auditPath,options.extensionPath,options.selectionPath].map(p=>readFile(p,"utf8")));
  const parsed=railAuditSchema.parse(JSON.parse(audit));
  const readAsset=async(path:string)=>{
    const parts=path.split("/"),pkg=parts[0]==="Packages",configured=pkg?options.packageRoots[parts[1]!]:resolve(options.projectRoot,"Assets");
    requireValid(configured,"package_root");const root=await realpath(configured!),file=await realpath(resolve(root,parts.slice(pkg?2:1).join("/"))),rel=relative(root,file);
    requireValid(rel&&!isAbsolute(rel)&&rel!==".."&&!rel.startsWith(`..${sep}`),"path_escape");return readFile(file);
  };
  requireValid(options.profile==="second-final-zone02-v1"?options.zone02SelectionPath:!options.zone02SelectionPath,"zone02_inputs");
  let zoneInputs:Pick<Inputs,"zone02Selection"|"directionManifest">={};
  if(options.profile){const zone02Selection=await readFile(options.zone02SelectionPath!,"utf8"),s=railZone02SelectionSchema.parse(JSON.parse(zone02Selection));
    const boundary=await realpath("D:/Project/LASTRAIL"),actual=await realpath(s.directionManifest.path),rel=relative(boundary,actual);
    requireValid(rel&&!isAbsolute(rel)&&rel!==".."&&!rel.startsWith(`..${sep}`),"direction_path_escape");
    const directionManifest=await readFile(actual,"utf8");
    for(const resource of s.resources.filter(r=>r.sourceFile!==null)){
      const source=await realpath(resolve(dirname(actual),resource.sourceFile!)),sourceRel=relative(dirname(actual),source);
      requireValid(sourceRel&&!isAbsolute(sourceRel)&&sourceRel!==".."&&!sourceRel.startsWith(`..${sep}`),"direction_source_escape");
      requireValid(createHash("sha256").update(await readFile(source)).digest("hex")===resource.sourceRawSha256,"direction_source_changed");
    }
    zoneInputs={zone02Selection,directionManifest};
  }
  const value=await projectRailPresentation({audit,extension,selection,asset:(await readAsset(parsed.root)).toString("utf8"),...(options.profile?{profile:options.profile}:{}),...zoneInputs});
  const verification=await verifyPresentationFiles(parsed,readAsset);
  const currentSources:string[]=[];
  const walk=async(dir:string):Promise<void>=>{for(const entry of await readdir(dir,{withFileTypes:true})){const full=resolve(dir,entry.name);if(entry.isDirectory())await walk(full);else if(entry.name.endsWith(".cs"))currentSources.push(relative(options.projectRoot,full).split(sep).join("/"));}};
  await walk(resolve(options.projectRoot,"Assets/_LASTRAIL"));
  requireValid(JSON.stringify(currentSources.sort())===JSON.stringify(value.manifest.sourceFiles.map(s=>s.path).sort()),"source_inventory");
  for(const s of value.manifest.sourceFiles)requireValid(createHash("sha256").update(await readAsset(s.path)).digest("hex")===s.sha256,"source_changed");
  const design=railDesignSchema.parse(JSON.parse(selection));
  for(const selected of value.manifest.selectedResources){const png=await readAsset(selected.path),entry=design.assets.find(a=>selected.path.endsWith(`/${a.file}`))!;
    requireValid(png.length>=24&&png.subarray(0,8).toString("hex")==="89504e470d0a1a0a"&&png.readUInt32BE(16)===entry.width&&png.readUInt32BE(20)===entry.height,"png_dimensions");}
  if(value.manifest.profile==="second-final-zone02-v1"&&zoneInputs.directionManifest){
    const direction=directionSchema.parse(JSON.parse(zoneInputs.directionManifest));
    for(const r of value.manifest.zone02Selection.resources.filter(r=>r.sourceFile!==null)){
      const png=await readAsset(r.path),entry=direction.assets.find(a=>a.file===r.sourceFile)!;
      requireValid(png.length>=24&&png.subarray(0,8).toString("hex")==="89504e470d0a1a0a"&&png.readUInt32BE(16)===entry.width&&png.readUInt32BE(20)===entry.height,"zone02_png_dimensions");
    }
  }
  return {value,verification:{...verification,currentSourceFilesVerified:currentSources.length,selectedResourcesVerified:22,rolesVerified:value.manifest.roles.length}};
}
