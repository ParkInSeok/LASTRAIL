#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class Ce03PresentationClosureExport {
 public static void Run()=>Export(Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Ce03CommandIntegration")));
 public static void RunFresh(){string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D audit output required");Export(output);EditorApplication.Exit(0);}
 static void Export(string dir){
  string root=LastRail.Resources.RemoteContentCatalog.AssetPath("CombatPresentation/approved");
  var rows=new JArray();var unresolved=new JArray();var roles=new JArray();
  var sourceAsset=AssetDatabase.LoadMainAssetAtPath(root);if(sourceAsset==null)throw new InvalidOperationException("Missing runtime presentation root.");
  var serialized=new SerializedObject(sourceAsset);var property=serialized.GetIterator();
  while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue!=null){var asset=property.objectReferenceValue;if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long localId))throw new InvalidOperationException();roles.Add(new JObject{["role"]="approved."+property.propertyPath,["path"]=Ce03RailPresentationBinding.LogicalAssetPath(AssetDatabase.GetAssetPath(asset)),["guid"]=guid,["localFileId"]=localId.ToString(System.Globalization.CultureInfo.InvariantCulture),["type"]=asset.GetType().FullName});}
  string Hash(string p){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
  foreach(var path in AssetDatabase.GetDependencies(root,true).OrderBy(x=>x,StringComparer.Ordinal)){
   if(!File.Exists(path)||!File.Exists(path+".meta")){unresolved.Add(new JObject{["path"]=Ce03RailPresentationBinding.LogicalAssetPath(path),["reason"]="BUILTIN_OR_NON_FILE_DEPENDENCY"});continue;}
   foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path).Where(x=>x!=null)){
    if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long fileId))throw new InvalidOperationException("No typed locator: "+path);
    rows.Add(new JObject{["path"]=Ce03RailPresentationBinding.LogicalAssetPath(path),["name"]=asset.name,["guid"]=guid,["localFileId"]=fileId.ToString(System.Globalization.CultureInfo.InvariantCulture),["type"]=asset.GetType().FullName,["assetRawSha256"]=Hash(path),["metaRawSha256"]=Hash(path+".meta"),["loadVerified"]=true,["directDependencies"]=new JArray(AssetDatabase.GetDependencies(path,false).Where(p=>p!=path).Select(Ce03RailPresentationBinding.LogicalAssetPath).OrderBy(p=>p,StringComparer.Ordinal))});
   }
  }
  Directory.CreateDirectory(dir);
  var config=(LastRail.Battle.CombatPresentationAssets)sourceAsset;
  var sources=new JArray();foreach(var name in new[]{"ApprovedCombatView","CombatPresentationAssets","CombatWindowView","CombatHudWidgets"}){string path="Assets/_LASTRAIL/Scripts/Battle/"+name+".cs";sources.Add(new JObject{["path"]=Ce03RailPresentationBinding.LogicalAssetPath(path),["sha256"]=Hash(path)});}
  File.WriteAllText(Path.Combine(dir,"presentation-closure-audit.json"),new JObject{["schema"]="ce03-presentation-closure-audit-v1",["root"]=Ce03RailPresentationBinding.LogicalAssetPath(root),["unityVersion"]=Application.unityVersion,["operatorIds"]=new JArray(config.OperatorIds),["frameKeys"]=new JArray(config.Keys),["sourceFiles"]=sources,["roles"]=roles,["rows"]=rows,["unresolved"]=unresolved,["actualAdmissionReady"]=false,["notes"]="Physical recursive approved.asset audit, not an accepted content manifest. Dynamic scene/default built-ins and code dependencies require explicit resolution."}.ToString());
  var sourceRows=new JArray();foreach(string path in Directory.GetFiles("Assets/_LASTRAIL","*.cs",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal)){string normalized=path.Replace(Path.DirectorySeparatorChar,'/');sourceRows.Add(new JObject{["path"]=normalized,["sha256"]=Hash(path)});}
  var selected=new JArray();foreach(var texture in config.SecondFinalResources??Array.Empty<Texture2D>()){if(texture==null)throw new InvalidOperationException("Missing selected resource");string path=AssetDatabase.GetAssetPath(texture);if(!AssetDatabase.GetDependencies(root,true).Contains(path)||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture,out string guid,out long id))throw new InvalidOperationException("Selected resource outside physical closure");selected.Add(new JObject{["resourceKey"]="CombatSecondFinal/"+texture.name,["path"]=Ce03RailPresentationBinding.LogicalAssetPath(path),["guid"]=guid,["localFileId"]=id.ToString(),["assetRawSha256"]=Hash(path),["metaRawSha256"]=Hash(path+".meta"),["width"]=texture.width,["height"]=texture.height,["runtimeBytes"]=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture),["rootReferenced"]=true,["resourcesLoadSameObject"]=LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/"+texture.name)==texture});}
  File.WriteAllText(Path.Combine(dir,"second-final-closure-extension.json"),new JObject{["schema"]="second-final-current-closure-v1",["sourceFiles"]=sourceRows,["selectedResources"]=selected,["actualAdmissionReady"]=false,["scope"]="Actual AssetDatabase dependencies and prepared asset object equality; paths retain logical provenance. Complete current LASTRAIL C# source hashes; no admission assertion."}.ToString());
  GalleryPopupClosureExport.Write(dir);
  GalleryNoncombatClosureExport.Write(dir);
  Debug.Log("CE03 closure audit rows="+rows.Count+" unresolved="+unresolved.Count);
 }
}
#endif
