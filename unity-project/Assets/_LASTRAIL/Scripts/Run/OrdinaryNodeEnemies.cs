using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Borrows only the node owner's private immutable snapshot; definitions are copied only for consumers.
 public sealed class OrdinaryNodeEnemies {
  public const string Version="ordinary.node-enemies.v1.20261031";
  public const string SpatialVersion="ordinary.node-enemies.v2.20261031";
  readonly JObject source;
  readonly string sourceHash,profileHash;
  public string ScalingBindingHash{get;}
  public IReadOnlyList<Gunner> Gunners{get;}
  public ArtilleryInput Artillery{get;}
  public SpatialInput Spatial{get;}
  public sealed class SpatialInput {
   readonly JObject definition;
   public string PhysicalInstanceId{get;} public string DefinitionHash{get;} public string GeometryDefinitionHash{get;}
   public JObject CaptureDefinition()=>(JObject)definition.DeepClone();
   internal SpatialInput(JObject value,FirstPlayableScaling scale,JObject profile,JObject binding){
    BDirectAttackInput.Fields(value,"physicalInstanceId","definition");definition=value["definition"] as JObject;
    FirstPlayableDefinitions.RequireEnemyDefinition(definition);
    PhysicalInstanceId=BDirectAttackInput.Text(value,"physicalInstanceId",200);Need(!string.IsNullOrEmpty(scale.IssuedEnemyTrainId)&&PhysicalInstanceId==scale.IssuedEnemyTrainId);
    DefinitionHash=(string)definition["definitionHash"];GeometryDefinitionHash=(string)definition["geometry"]["definitionHash"];
    var reference=profile["enemies"]?["spatial"] as JObject;BDirectAttackInput.Fields(reference,"version","definitionHash");
    Need(JToken.DeepEquals(reference["version"],definition["version"])&&(string)reference["definitionHash"]==DefinitionHash);
    var spatial=binding["enemySpatial"] as JObject;BDirectAttackInput.Fields(spatial,"version","definitionHash","geometryDefinitionHash","physicalInstanceId");
    Need(JToken.DeepEquals(spatial["version"],definition["version"])&&(string)spatial["definitionHash"]==DefinitionHash&&(string)spatial["geometryDefinitionHash"]==GeometryDefinitionHash&&(string)spatial["physicalInstanceId"]==PhysicalInstanceId);
   }
  }
  public sealed class Gunner {
   public string InstanceId{get;} public CombatExactValue Hp{get;} public CombatExactValue MaxHp{get;}
   public long Defense{get;} public CombatExactValue RawDamage{get;}
   internal Gunner(JObject p,string id,FirstPlayableScaling scale,JToken defense){
    BDirectAttackInput.Fields(p,"instanceId","hp","maxHp","defense","rawDamage");Need(BDirectAttackInput.Text(p,"instanceId",200)==id);InstanceId=id;
    Hp=Exact(p,"hp",scale.GunnerHp);MaxHp=Exact(p,"maxHp",scale.GunnerHp);RawDamage=Exact(p,"rawDamage",scale.GunnerRawDamage);
    Defense=BDirectAttackInput.Nat(p,"defense");Need(JToken.DeepEquals(p["defense"],defense));
   }
  }
  public sealed class ArtilleryInput {
   readonly JObject source;
   public string HazardId{get;} public CombatExactValue Hp{get;} public CombatExactValue MaxHp{get;}
   public CombatExactValue RawDamage{get;} public long MinimumThreatClearToPreviewMs{get;}
   public IReadOnlyList<string> PreviewRequires{get;}
   public JObject CaptureDefinition()=>(JObject)source["definition"].DeepClone();
   internal ArtilleryInput(JObject p,FirstPlayableScaling scale,JObject profile){
    BDirectAttackInput.Fields(p,"hazardId","hp","maxHp","rawDamage","definition","minimumThreatClearToPreviewMs","previewRequires");
    Need(BDirectAttackInput.Text(p,"hazardId",200)==scale.IssuedHazardId);HazardId=scale.IssuedHazardId;
    Hp=Exact(p,"hp",scale.LauncherHp);MaxHp=Exact(p,"maxHp",scale.LauncherHp);RawDamage=Exact(p,"rawDamage",scale.ArtilleryRawDamage);
    Need(p["definition"] is JObject&&JToken.DeepEquals(p["definition"],profile["artillery"]));
    MinimumThreatClearToPreviewMs=BDirectAttackInput.Nat(p,"minimumThreatClearToPreviewMs");Need(JToken.DeepEquals(p["minimumThreatClearToPreviewMs"],profile["minimumThreatClearToPreviewMs"]));
    Need(p["previewRequires"] is JArray requirements&&requirements.Count<=32&&requirements.All(r=>r.Type==JTokenType.String)&&JToken.DeepEquals(requirements,profile["previewRequires"]));
    PreviewRequires=Array.AsReadOnly(((JArray)p["previewRequires"]).Select(r=>(string)r).ToArray());source=p;
   }
  }
  OrdinaryNodeEnemies(JObject owned,FirstPlayableScaling scale,JObject profile){
   bool spatial=(string)owned?["version"]==SpatialVersion;
   BDirectAttackInput.Fields(owned,new[]{"version","scalingBindingHash","actionDefinition","gunners","artillery"}.Concat(spatial?new[]{"spatial"}:Array.Empty<string>()).ToArray());
   Need((spatial||(string)owned["version"]==Version)&&scale!=null&&scale.IssuedGunnerIds?.Count==2&&scale.IssuedHazardId!=null);
   Need((string)owned["scalingBindingHash"]==scale.BindingHash&&profile!=null);
   Need(owned["actionDefinition"] is JObject&&JToken.DeepEquals(owned["actionDefinition"],profile["enemies"]?["definition"]));
   Need(owned["gunners"] is JArray rows&&rows.Count==2);
   var gunners=(JArray)owned["gunners"];Gunners=Array.AsReadOnly(new[]{new Gunner(gunners[0] as JObject,scale.IssuedGunnerIds[0],scale,profile["enemies"]["defense"]),new Gunner(gunners[1] as JObject,scale.IssuedGunnerIds[1],scale,profile["enemies"]["defense"])});
   Artillery=new ArtilleryInput(owned["artillery"] as JObject,scale,profile);ScalingBindingHash=scale.BindingHash;source=owned;
   var binding=scale.Capture();sourceHash=(string)binding["sourceHash"];profileHash=(string)binding["profileHash"];
   if(spatial)Spatial=new SpatialInput(owned["spatial"] as JObject,scale,profile,binding);
  }
  internal static OrdinaryNodeEnemies ReadOwned(JObject owned,FirstPlayableScaling scaling,JObject verifiedProfile)=>new OrdinaryNodeEnemies(owned,scaling,verifiedProfile);
  public JObject CaptureActionDefinition()=>(JObject)source["actionDefinition"].DeepClone();
  internal void RequireOriginal(OrdinaryFiveStartInput start){Need(start?.FirstBattle!=null&&start.FirstBattle.SourceHash==sourceHash&&start.FirstBattle.ProfileHash==profileHash);}
  static CombatExactValue Exact(JObject p,string key,CombatExactValue expected){var result=CombatExactValue.From(p[key] as JObject);Need(JToken.DeepEquals(p[key],expected.Snapshot()));return result;}
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_node_enemy_source_conflict");}
 }
}
