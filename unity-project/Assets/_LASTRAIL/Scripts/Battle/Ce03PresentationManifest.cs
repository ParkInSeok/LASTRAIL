using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Shape and closure only. Actual Unity object/file validation belongs to the
// Editor loader; parsing this manifest never authorizes combat admission.
public sealed class Ce03PresentationManifest {
 public const string Version="ce03-presentation-manifest-v1";
 public static readonly string[] Actors={"op_kyle","op_reina","op_eve","op_sera","op_mira","enemy_raider_scout"};
 public static readonly string[] TextureRoles={"console","icons","map","trainExterior","interior","defaultTurret","turretRig","buggy","bike","hit","skill","shield"};
 public static readonly string[] SpriteRoles={"frame","primary","secondary"};
 public static readonly IReadOnlyDictionary<string,string> SourcePaths=new Dictionary<string,string>{
  {"approvedCombatView","Assets/_LASTRAIL/Scripts/Battle/ApprovedCombatView.cs"},
  {"combatHudWidgets","Assets/_LASTRAIL/Scripts/Battle/CombatHudWidgets.cs"},
  {"combatPresentationAssets","Assets/_LASTRAIL/Scripts/Battle/CombatPresentationAssets.cs"},
  {"combatWindowView","Assets/_LASTRAIL/Scripts/Battle/CombatWindowView.cs"}};
 readonly JObject value;
 public JObject Copy()=>(JObject)value.DeepClone();
 public string Hash=>Ce03CommandContract.Sha(Ce03CommandContract.Canonical(value));
 static void Fail(string reason){throw new FormatException("Presentation manifest: "+reason);}
 static string Text(JToken t,string pattern){if(t?.Type!=JTokenType.String||!Regex.IsMatch((string)t,pattern,CultureInvariant))Fail("invalid identifier/digest");return (string)t;}
 const RegexOptions CultureInvariant=RegexOptions.CultureInvariant;
 static string Id(JToken t)=>Text(t,@"\A[A-Za-z0-9_.:-]{1,128}\z");
 static void Ordered(IEnumerable<string> values){string prior=null;foreach(string v in values){if(prior!=null&&StringComparer.Ordinal.Compare(prior,v)>=0)Fail("duplicate or unsorted entry");prior=v;}}
 static JArray Array(JToken t,int min,int max){if(t is not JArray a||a.Count<min||a.Count>max){Fail("array size");return null;}return a;}
 public Ce03PresentationManifest(JObject input){
  value=input==null?null:(JObject)input.DeepClone();
  Ce03CommandContract.Fields(value,"schemaVersion","rendererVersion","roles","bindings","sourceFiles");
  Ce03CommandContract.Equal(value,"schemaVersion",Version);Ce03CommandContract.Equal(value,"rendererVersion","approved-combat-view-v1");
  var rows=Array(value["bindings"],1,2048);var byId=new Dictionary<string,JObject>(StringComparer.Ordinal);var locators=new HashSet<string>(StringComparer.Ordinal);
  var types=new[]{"Sprite","Texture2D","AnimationClip","AnimatorController","MonoScript","Material","Shader","CombatPresentationAssets","Font","TMP_FontAsset"};
  foreach(var item in rows){
   var b=item as JObject;Ce03CommandContract.Fields(b,"bindingId","actorOrRoleId","locator","assetType","assetRawSha256","metaRawSha256","dependencies","importValidation");
   string id=Id(b["bindingId"]);Id(b["actorOrRoleId"]);if(byId.ContainsKey(id))Fail("duplicate binding");byId.Add(id,b);
   if(b["assetType"]?.Type!=JTokenType.String||!types.Contains((string)b["assetType"]))Fail("asset type");
   Text(b["assetRawSha256"],@"\A[a-f0-9]{64}\z");Text(b["metaRawSha256"],@"\A[a-f0-9]{64}\z");
   var l=b["locator"] as JObject;Ce03CommandContract.Fields(l,"kind","guid","localFileId");Ce03CommandContract.Equal(l,"kind","UNITY_ASSET_GUID");
   string guid=Text(l["guid"],@"\A[a-f0-9]{32}\z"),fid=Text(l["localFileId"],@"\A(0|-[1-9][0-9]*|[1-9][0-9]*)\z");
   if(!long.TryParse(fid,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out _)||!locators.Add(guid+":"+fid))Fail("typed locator");
   var import=b["importValidation"] as JObject;Ce03CommandContract.Fields(import,"unityVersion","importerVersion","loadVerified");
   if(import["unityVersion"]?.Type!=JTokenType.String||((string)import["unityVersion"]).Length<1||((string)import["unityVersion"]).Length>128)Fail("Unity version");
   if(import["importerVersion"]?.Type!=JTokenType.Null&&(import["importerVersion"]?.Type!=JTokenType.String||((string)import["importerVersion"]).Length>128))Fail("importer version");
   if(import["loadVerified"]?.Type!=JTokenType.Boolean||!(bool)import["loadVerified"])Fail("unverified load");
   Ordered(Array(b["dependencies"],0,2048).Select(Id));
  }
  Ordered(rows.Select(b=>(string)b["bindingId"]));
  foreach(var b in byId.Values)foreach(string d in ((JArray)b["dependencies"]).Select(Id))if(d==(string)b["bindingId"]||!byId.ContainsKey(d))Fail("dependency closure");
  var roles=value["roles"] as JObject;Ce03CommandContract.Fields(roles,new[]{"configuration","font","actorAtlases","frames"}.Concat(TextureRoles).Concat(SpriteRoles).ToArray());
  void Role(JToken token,string type){string id=Id(token);if(!byId.TryGetValue(id,out var b)||(string)b["assetType"]!=type)Fail("role type or binding");}
  Role(roles["configuration"],"CombatPresentationAssets");Role(roles["font"],"TMP_FontAsset");foreach(string r in TextureRoles)Role(roles[r],"Texture2D");foreach(string r in SpriteRoles)Role(roles[r],"Sprite");
  var atlases=Array(roles["actorAtlases"],6,6);foreach(var a in atlases){Ce03CommandContract.Fields(a as JObject,"actorId","bindingId");if(!Actors.Contains(Id(a["actorId"])))Fail("actor identity");Role(a["bindingId"],"Texture2D");}Ordered(atlases.Select(a=>(string)a["actorId"]));
  var expected=new HashSet<string>(Actors.Concat(new[]{"enemy_spike_cart_gunner","enemy_railbike_marauder"}).SelectMany(a=>new[]{"idle","attack","hit","down_or_defeated","skill_or_special"}.SelectMany(s=>Enumerable.Range(1,4).Select(i=>a+"/"+s+"/"+i))),StringComparer.Ordinal);
  var frames=Array(roles["frames"],160,160);foreach(var f in frames){Ce03CommandContract.Fields(f as JObject,"key","bindingId");if(f["key"]?.Type!=JTokenType.String||!expected.Contains((string)f["key"]))Fail("frame identity");Role(f["bindingId"],"Texture2D");}Ordered(frames.Select(f=>(string)f["key"]));
  var sources=Array(value["sourceFiles"],4,4);foreach(var s in sources){Ce03CommandContract.Fields(s as JObject,"logicalName","rawSha256");if(!SourcePaths.ContainsKey(Id(s["logicalName"])))Fail("source identity");Text(s["rawSha256"],@"\A[a-f0-9]{64}\z");}Ordered(sources.Select(s=>(string)s["logicalName"]));
 }
}
}
