using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Separate local rail metadata. Never widens the legacy presentation/admission contract.
public sealed partial class Ce03RailPresentation {
 public const string Root="Assets/_LASTRAIL/Resources/CombatPresentation/approved.asset";
 public static readonly string[] SelectedNames={"artillery_emplacement-v01","artillery_ledge-v01","console_bezel-v01","console_plate-v01","control_handle-v01","eve-v02","kyle-v02","mira-v02","reina-v02","sera-v02","support_barrier-v01","support_fire-v01","support_repair-v01","title_emblem-v01","train_carriage_armored-v03","train_carriage_cargo-v03","train_locomotive-v03","ultimate_eve-v01","ultimate_kyle-v01","ultimate_mira-v01","ultimate_reina-v02","ultimate_sera-v01"};
 readonly JObject value;
 public JObject Copy()=>(JObject)value.DeepClone();
 public string Hash=>(string)value["manifestSha256"];
 static void Need(bool ok,string reason){if(!ok)throw new FormatException("Rail presentation: "+reason);}
 static string Text(JToken v){Need(v?.Type==JTokenType.String,"text");return (string)v;}
 static string Digest(JToken v,int length=64){string s=Text(v);Need(Regex.IsMatch(s,"\\A[a-f0-9]{"+length+"}\\z"),"digest");return s;}
 static string Path(JToken v){string p=Text(v);Need((p.StartsWith("Assets/",StringComparison.Ordinal)||p.StartsWith("Packages/",StringComparison.Ordinal))&&!p.Contains('\\')&&!p.Contains(':')&&!p.Split('/').Any(s=>s==".."||s=="."||s.Length==0),"path");return p;}
 static string Locator(JObject v){string id=Text(v["localFileId"]);Need(Regex.IsMatch(id,@"\A(0|-[1-9][0-9]*|[1-9][0-9]*)\z")&&long.TryParse(id,out _),"file ID");return Digest(v["guid"],32)+":"+id;}
 static JArray Rows(JToken v,int min,int max){Need(v is JArray a&&a.Count>=min&&a.Count<=max,"row count");return (JArray)v;}
 public Ce03RailPresentation(JObject input){
  value=input==null?null:(JObject)input.DeepClone();Ce03CommandContract.Fields(value,"version","manifestSha256","manifest");bool zone02=(string)value["version"]=="ce03-rail-presentation-v2";Need(zone02||(string)value["version"]=="ce03-rail-presentation-v1","version");
  var m=value["manifest"] as JObject;var fields=new[]{"profile","root","auditSha256","extensionSha256","selectionManifestSha256","roles","bindings","selectedResources","sourceFiles"};if(zone02)fields=fields.Concat(new[]{"zone02SelectionManifestSha256","zone02Selection"}).ToArray();Ce03CommandContract.Fields(m,fields);Ce03CommandContract.Equal(m,"profile",zone02?"second-final-zone02-v1":"second-final-selected-v3");Ce03CommandContract.Equal(m,"root",Root);
  Need(Digest(value["manifestSha256"])==Ce03CommandContract.Sha(Ce03CommandContract.Canonical(m)),"manifest hash");foreach(string k in new[]{"auditSha256","extensionSha256","selectionManifestSha256"})Digest(m[k]);
  var bindings=Rows(m["bindings"],1,2048);var byLocator=new Dictionary<string,JObject>(StringComparer.Ordinal);var paths=new HashSet<string>(StringComparer.Ordinal);
  var types=new[]{"LastRail.Battle.CombatOperatorRigSet","LastRail.Battle.CombatPresentationAssets","LastRail.Battle.CombatSupportFireAssets","TMPro.TMP_FontAsset","UnityEditor.MonoScript","UnityEngine.Font","UnityEngine.Material","UnityEngine.Shader","UnityEngine.Sprite","UnityEngine.TextAsset","UnityEngine.Texture2D"};
  foreach(var token in bindings){var b=token as JObject;Ce03CommandContract.Fields(b,"path","name","guid","localFileId","type","assetRawSha256","metaRawSha256","loadVerified","directDependencies");string path=Path(b["path"]);paths.Add(path);Text(b["name"]);Need(types.Contains(Text(b["type"]))&&(bool?)b["loadVerified"]==true,"type/load");Digest(b["assetRawSha256"]);Digest(b["metaRawSha256"]);Need(byLocator.TryAdd(Locator(b),b),"duplicate locator");var deps=Rows(b["directDependencies"],0,2048).Select(Path).ToArray();Need(deps.Distinct().Count()==deps.Length&&!deps.Contains(path),"dependencies");}
  foreach(var group in bindings.GroupBy(b=>(string)b["path"])){var first=group.First();Need(group.All(b=>JToken.DeepEquals(b["assetRawSha256"],first["assetRawSha256"])&&JToken.DeepEquals(b["metaRawSha256"],first["metaRawSha256"])&&JToken.DeepEquals(b["directDependencies"],first["directDependencies"])),"inconsistent path");}
  foreach(var b in bindings)foreach(string d in (JArray)b["directDependencies"])Need(paths.Contains(d),"open dependency");
  var root=bindings.SingleOrDefault(b=>(string)b["path"]==Root&&(string)b["type"]=="LastRail.Battle.CombatPresentationAssets");Need(root!=null,"root binding");
  var reachable=new HashSet<string>(StringComparer.Ordinal);var queue=new Queue<string>();queue.Enqueue(Root);while(queue.Count>0){string p=queue.Dequeue();if(!reachable.Add(p))continue;foreach(string dep in (JArray)bindings.First(b=>(string)b["path"]==p)["directDependencies"])queue.Enqueue(dep);}
  // Unity includes this package editor icon in recursive dependencies without a direct edge.
  Need(paths.Except(reachable).All(p=>p=="Packages/com.unity.ugui/Editor Resources/Gizmos/TMP - Font Asset Icon.psd"&&bindings.Count(b=>(string)b["path"]==p)==1&&bindings.Where(b=>(string)b["path"]==p).All(b=>(string)b["type"]=="UnityEngine.Texture2D"&&(string)b["localFileId"]=="2800000")),"detached binding");
  var roleNames=new HashSet<string>(StringComparer.Ordinal);var roles=Rows(m["roles"],1,512);foreach(var token in roles){var r=token as JObject;Ce03CommandContract.Fields(r,"role","path","guid","localFileId","type");string role=Text(r["role"]);Need(role.StartsWith("approved.",StringComparison.Ordinal)&&roleNames.Add(role),"role identity");Need(byLocator.TryGetValue(Locator(r),out var b)&&Path(r["path"])==(string)b["path"]&&Text(r["type"])==(string)b["type"],"role binding");}
  ValidateRoleSet(roles,zone02);if(zone02)ValidateZone02(m,byLocator,roles);
  var selected=Rows(m["selectedResources"],22,22);var keys=new HashSet<string>(StringComparer.Ordinal);foreach(var token in selected){var r=token as JObject;Ce03CommandContract.Fields(r,"resourceKey","path","guid","localFileId","assetRawSha256","metaRawSha256","width","height","runtimeBytes","rootReferenced","resourcesLoadSameObject");string key=Text(r["resourceKey"]);Need(keys.Add(key)&&SelectedNames.Any(n=>key=="CombatSecondFinal/"+n),"selected key");string path=Path(r["path"]);Need(path=="Assets/_LASTRAIL/Resources/"+key+".png","resource path");Need(byLocator.TryGetValue(Locator(r),out var b)&&(string)b["path"]==path&&(string)b["type"]=="UnityEngine.Texture2D"&&Digest(r["assetRawSha256"])==(string)b["assetRawSha256"]&&Digest(r["metaRawSha256"])==(string)b["metaRawSha256"],"selected binding");Need((bool?)r["rootReferenced"]==true&&(bool?)r["resourcesLoadSameObject"]==true&&((JArray)root["directDependencies"]).Any(d=>(string)d==path),"selected root");foreach(string k in new[]{"width","height"})Need(r[k]?.Type==JTokenType.Integer&&(long)r[k]>0&&(long)r[k]<=512,"dimensions");Need(r["runtimeBytes"]?.Type==JTokenType.Integer&&(long)r["runtimeBytes"]>0,"memory");}
  var sources=Rows(m["sourceFiles"],4,4096);var sourcePaths=new HashSet<string>(StringComparer.Ordinal);foreach(var token in sources){var s=token as JObject;Ce03CommandContract.Fields(s,"path","sha256");string p=Path(s["path"]);Need(p.StartsWith("Assets/_LASTRAIL/",StringComparison.Ordinal)&&p.EndsWith(".cs",StringComparison.Ordinal)&&sourcePaths.Add(p),"source");Digest(s["sha256"]);}
 }
}
}
