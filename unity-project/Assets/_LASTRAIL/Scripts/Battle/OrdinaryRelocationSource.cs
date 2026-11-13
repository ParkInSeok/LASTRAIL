using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Original policy only; live reservations, crossing observations and effects belong to their owners.
 public sealed class OrdinaryRelocationSource {
  public const string V1="combat.ordinary-relocation.v1.20261026",V2="combat.ordinary-relocation.v2.20261026",V3="combat.ordinary-relocation.v3.20261026",V4="combat.ordinary-relocation.v4.20261026",V5="combat.ordinary-relocation.v5.20261026",V6="combat.ordinary-relocation.v6.20261026",V7="combat.ordinary-relocation.v7.20261102";
  public const string V9="combat.ordinary-relocation.v9.20261107";
  public const string V10="combat.ordinary-relocation.v10.20261107";
  static readonly IReadOnlyDictionary<string,string> Definitions=new Dictionary<string,string>{
   {V1,"be4f827df9907ded4eeb73e547f1c8c994382b86e8efea7f30dd585abceacb7a"},
   {V2,"dceda11ef92bca2d40c7e57b9aa7d4cfce3474f82a1fa7aa4dc34b1729ea9bde"},
   {V3,"933e23f65d0cd3bb9e671ac34123a7303d83320501fd41cd0550466ee446f6aa"},
   {V4,"ad6fcc4533d58738089303edf9c72865dfea26cff341a84a1db31354633ee5b7"},
   {V5,"3f787df7e44055960406a8c997ace138bbecab06ad9852fcb2ba3a6dcad3e3ec"},
   {V6,"368bdeb4d76f2f82cfa917ed85150f26ca4bceb4292bed43a04a7f7344dbac4d"},
   {V7,"fcfddbd1350e27aa0d2270415f4379bd6d9d4e63ba9eec23d9058172413e3e89"},
   {V9,"fb9cdc4691a9cbf591ab2991a8c2e229f1d6f948b5afbaaf9e21d86d20194318"},
   {V10,"a0aab36339ec85fae31c96a01ff8845a63e9c6318854a6a1369926c9f5f750eb"}};
  public sealed class SkillScope {
   readonly JObject definition;
   public string AbilityId{get;} public string Basis{get;} public string EffectRetention{get;}
   internal SkillScope(JToken row){definition=(JObject)row.DeepClone();AbilityId=(string)row["abilityId"];Basis=(string)row["basis"];EffectRetention=(string)row["effectRetention"];}
   public JObject CaptureDefinition()=>(JObject)definition.DeepClone();
  }
  public string SourceVersion{get;} public string RelocationHash{get;}
  public string ZoneSelectionHash{get;}
  public bool HasScopeMapping=>SourceVersion!=V1;
  public IReadOnlyList<SkillScope> SkillScopes{get;}
  public IReadOnlyList<string> UnclassifiedAbilityIds{get;}
  internal OrdinaryRelocationSource(JToken raw,OrdinaryFiveStartInput owner,IEnumerable<string> originalKitOrder){
   var source=raw as JObject;Need(source!=null);BDirectAttackInput.Fields(source,"packet","relocationHash");
   var packet=source["packet"] as JObject;Need(packet!=null&&Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(packet))<=131072);
   SourceVersion=Text(packet,"version");Need(Definitions.ContainsKey(SourceVersion));
   Need((SourceVersion==V7)==(owner.KitSourceVersion==OrdinaryFiveStartInput.KitV2Version));
   bool nyx=SourceVersion==V9||SourceVersion==V10;
   Need(nyx==(owner.KitSourceVersion==OrdinaryFiveStartInput.KitV3Version));
   RelocationHash=Text(source,"relocationHash");Need(RelocationHash==Hash(packet));
   Need(owner.Basic!=null&&owner.Basic.SourceVersion==(nyx?OrdinaryBasicSource.V5Version:OrdinaryBasicSource.V3Version)
    &&Text(packet,"accountId")==owner.AccountId&&Text(packet,"runId")==owner.RunId&&Text(packet,"originHash")==owner.OriginHash
    &&Text(packet,"basicVersion")==owner.Basic.SourceVersion&&Text(packet,"basicHash")==owner.Basic.BasicHash);
   if(HasScopeMapping)Need(Text(packet,"kitHash")==owner.KitHash);
   var definition=(JObject)packet.DeepClone();
   foreach(var key in new[]{"accountId","runId","originHash","basicHash"})definition.Remove(key);
   if(HasScopeMapping){
    definition.Remove("kitHash");
    Need(definition["basicScopes"] is JArray&&definition["passiveScopes"] is JArray);
    Need(((JArray)definition["passiveScopes"]).Select(p=>(string)p["operatorId"]).SequenceEqual(originalKitOrder));
    foreach(var row in (JArray)definition["basicScopes"]){Need(row["reference"] is JObject);var reference=(JObject)row["reference"];Need(Text(reference,"basicHash")==owner.Basic.BasicHash);reference.Remove("basicHash");}
    if(SourceVersion==V3||SourceVersion==V4||SourceVersion==V5||SourceVersion==V6||SourceVersion==V7||nyx){
     Need(definition["skillScopes"] is JArray);
     foreach(var row in (JArray)definition["skillScopes"])if(row["basicReference"] is JObject reference){Need(Text(reference,"basicHash")==owner.Basic.BasicHash);reference.Remove("basicHash");}
    }
    definition["passiveScopes"]=new JArray(((JArray)definition["passiveScopes"]).OrderBy(p=>(string)p["operatorId"],StringComparer.Ordinal));
   }
   Need(Hash(definition)==Definitions[SourceVersion]);
   if(SourceVersion==V10){var selection=packet["zoneSelectionSource"] as JObject;BDirectAttackInput.Fields(selection,"packet","sourceHash");ZoneSelectionHash=Text(selection,"sourceHash");Need(ZoneSelectionHash=="126e5387830a0e71c9dcc55dccf1f2fca9c5f33b02355e284204dad687b5999b"&&Hash(selection["packet"])==ZoneSelectionHash);}
   SkillScopes=Array.AsReadOnly(HasScopeMapping?packet["skillScopes"].Select(p=>new SkillScope(p)).ToArray():Array.Empty<SkillScope>());
   UnclassifiedAbilityIds=Array.AsReadOnly(HasScopeMapping?packet["unclassifiedAbilityIds"].Select(p=>(string)p).ToArray():Array.Empty<string>());
  }
  static string Text(JToken value,string key)=>BDirectAttackInput.Text(value,key);
  static string Hash(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_relocation_source_invalid");}
 }
}
