using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
public sealed partial class Ce03RailPresentation {
 public const string Zone02SelectionFile="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Exterior-20260914/zone02-selection-manifest.json";
 public const string Zone02DirectionFile="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Design/Zone02-03-Quality-20260914/manifest.json";
 const string Zone02Assets="Assets/_LASTRAIL/UI/ApprovedCombat/ExteriorQuality/";
 static void ValidateRoleSet(JArray roles,bool zone02){
  var names=new HashSet<string>("m_Script Font Console IconAtlas Buggy Bike TurretRig ActiveIconAtlas DistantFar DistantTerrain DistantTerrainLayout PursuitRoad PursuitRidges PursuitMarkers PursuitLayout OperatorRigs SupportFire ExteriorMap ExteriorTrain Interior DefaultTurret Frame Primary Secondary Hit Skill Shield".Split(' ').Select(s=>"approved."+s),StringComparer.Ordinal);
  foreach(var p in new[]{("Frames",160),("OperatorAtlases",6),("SecondFinalResources",22)})for(int i=0;i<p.Item2;i++)names.Add("approved."+p.Item1+".Array.data["+i+"]");if(zone02)names.Add("approved.ExteriorBezel");
  Need(names.SetEquals(roles.Select(r=>(string)r["role"]))&&roles.Count==names.Count,"exact versioned roles");
  foreach(var r in roles){string expected=(string)r["role"] switch {"approved.m_Script"=>"UnityEditor.MonoScript","approved.Font"=>"TMPro.TMP_FontAsset","approved.OperatorRigs"=>"LastRail.Battle.CombatOperatorRigSet","approved.SupportFire"=>"LastRail.Battle.CombatSupportFireAssets","approved.Frame" or "approved.Primary" or "approved.Secondary"=>"UnityEngine.Sprite","approved.PursuitLayout" or "approved.DistantTerrainLayout"=>"UnityEngine.TextAsset",_=>"UnityEngine.Texture2D"};Need((string)r["type"]==expected,"role type");}
 }
 static void ValidateZone02(JObject m,Dictionary<string,JObject> bindings,JArray roles){
  Digest(m["zone02SelectionManifestSha256"]);var selection=m["zone02Selection"] as JObject;Ce03CommandContract.Fields(selection,"schema","profile","scope","directionManifest","resources");Ce03CommandContract.Equal(selection,"schema","combat-zone02-selection-v1");Ce03CommandContract.Equal(selection,"profile","second-final-zone02-v1");Ce03CommandContract.Equal(selection,"scope","APPLICATION_ASSET_IDENTIFICATION");var direction=selection["directionManifest"] as JObject;Ce03CommandContract.Fields(direction,"path","sha256");Ce03CommandContract.Equal(direction,"path",Zone02DirectionFile);Digest(direction["sha256"]);
  var expected=new Dictionary<string,string[]>{{"canyon-far-v02.png",new[]{"approved.DistantFar","approved.ExteriorMap"}},{"canyon-mid-ridge-v02.png",new[]{"approved.PursuitRidges"}},{"canyon-road-v02.png",new[]{"approved.PursuitRoad"}},{"exterior-bezel-v02.png",new[]{"approved.ExteriorBezel"}},{"pursuit-layout-v02.json",new[]{"approved.PursuitLayout"}}};var seen=new HashSet<string>(StringComparer.Ordinal);
  foreach(var token in Rows(selection["resources"],5,5)){
   var r=token as JObject;Ce03CommandContract.Fields(r,"path","assetRawSha256","metaRawSha256","guid","localFileId","type","roles","sourceFile","sourceRawSha256");string path=Path(r["path"]);Need(path.StartsWith(Zone02Assets,StringComparison.Ordinal),"zone02 path");string name=path.Substring(Zone02Assets.Length);Need(expected.ContainsKey(name)&&seen.Add(name),"zone02 selected identity");bool layout=name.EndsWith(".json",StringComparison.Ordinal);Need(Text(r["type"])==(layout?"UnityEngine.TextAsset":"UnityEngine.Texture2D"),"zone02 type");string locator=Locator(r);Need(bindings.TryGetValue(locator,out var b)&&Text(b["path"])==path&&Text(b["type"])==Text(r["type"])&&Digest(b["assetRawSha256"])==Digest(r["assetRawSha256"])&&Digest(b["metaRawSha256"])==Digest(r["metaRawSha256"]),"zone02 physical binding");
   var mapped=Rows(r["roles"],1,2).Select(Text).ToArray();Need(mapped.Distinct(StringComparer.Ordinal).Count()==mapped.Length&&new HashSet<string>(mapped,StringComparer.Ordinal).SetEquals(expected[name]),"zone02 role mapping");foreach(string role in mapped){var actual=(JObject)roles.Single(p=>(string)p["role"]==role);Need(Locator(actual)==locator&&(string)actual["path"]==path&&(string)actual["type"]==(string)r["type"],"zone02 actual role");}
   if(layout)Need(r["sourceFile"].Type==JTokenType.Null&&r["sourceRawSha256"].Type==JTokenType.Null,"layout provenance");else Need(Text(r["sourceFile"])==name&&Digest(r["sourceRawSha256"])==Digest(r["assetRawSha256"]),"source provenance");
  }
 }
}
}
