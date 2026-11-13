using System;
using System.Linq;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // An initialization receipt is not a current-HP snapshot or permission to start combat.
 public static class OrdinaryInitializationReceipt {
  internal static OrdinaryRunAutomation ValidateEnvelope(JObject root,string account,string run){
   BDirectAttackInput.Fields(root,"accepted","initializationReceipt","runtimeExecutionVerified");
   Need((bool?)root["accepted"]==true&&(bool?)root["runtimeExecutionVerified"]==false);
   var receipt=root["initializationReceipt"] as JObject;Need(receipt!=null);
   Need((string)receipt["version"]=="combat.ordinary-phase-initialization.v1.20261023"&&(string)receipt["runId"]==run&&(string)receipt["phase"]=="RUNNING");
   var stats=receipt["stats"] as JObject;Need(stats!=null&&(string)stats["accountId"]==account&&(string)stats["runId"]==run&&(string)stats["version"]==OrdinaryFiveCoreStatSnapshot.Version);
   OrdinaryFiveCoreStatSnapshot.ValidateInitializationPacket(stats,(string)receipt["ordinaryKitBinding"]?["version"]==OrdinaryFiveStartInput.KitV3Version?OrdinaryFiveCoreStatSnapshot.NyxOriginVersion:OrdinaryFiveCoreStatSnapshot.Version);
   Need(receipt["health"] is JObject&&receipt["health"]["state"] is JObject&&receipt["health"]["state"]["targets"] is JArray&&stats["actors"] is JArray);
   var train=receipt.Property("trainInitialState")==null?null:new OrdinaryTrainInitialState(receipt,account,run);
   var targets=(JArray)receipt["health"]["state"]["targets"];var actors=(JArray)stats["actors"];Need(targets.Count==(train==null?5:6)&&actors.Count==5);
   Need(targets.Select(t=>(string)t["id"]).Distinct().Count()==targets.Count&&actors.Select(t=>(string)t["operatorId"]).Distinct().Count()==5&& !actors.Any(t=>(string)t["operatorId"]==train?.TrainId));
   foreach(var actor in actors){var target=targets.SingleOrDefault(t=>(string)t["id"]==(string)actor["operatorId"]);Need(target!=null&&actor["currentHp"]?.Type==JTokenType.Integer&&actor["effectiveStats"]?["maxHp"]?.Type==JTokenType.Integer);
    Need((long)actor["currentHp"]>=0&&(long)actor["effectiveStats"]["maxHp"]>0&&(long)actor["currentHp"]<=(long)actor["effectiveStats"]["maxHp"]&&JToken.DeepEquals(target["health"]?["hp"],actor["currentHp"])&&JToken.DeepEquals(target["health"]?["maxHp"],actor["effectiveStats"]["maxHp"]));}
   var automation=receipt.Property("ordinaryAutomation")==null?null:OrdinaryRunAutomation.Read(receipt["ordinaryAutomation"]);
   automation?.RequireActors(actors.Select(a=>(string)a["operatorId"]));return automation;
  }
  public static OrdinaryFiveCoreStatSnapshot Validate(string json,OrdinaryFiveStartInput original)=>Validate(json,original,out _);
  public static OrdinaryFiveCoreStatSnapshot Validate(string json,OrdinaryFiveStartInput original,out OrdinaryRunAutomation automation){
   if(original==null)throw new ArgumentNullException(nameof(original));var root=GameApiJson.ReadObject(json);automation=ValidateEnvelope(root,original.AccountId,original.RunId);automation?.RequireOriginal(original);var receipt=root["initializationReceipt"];
   Need((string)receipt["stats"]["originHash"]==original.OriginHash);
   foreach(var actor in original.Actors)Need(((JArray)receipt["stats"]["actors"]).Any(a=>(string)a["operatorId"]==actor.OperatorId&&(int?)a["slot"]==actor.Slot));
   Need(JToken.DeepEquals(receipt["ordinaryKitBinding"],new JObject{["version"]=original.KitSourceVersion,["originHash"]=original.OriginHash,["kitHash"]=original.KitHash}));
   Need(JToken.DeepEquals(receipt["ordinaryBasicBinding"],original.Basic==null?null:new JObject{["version"]=original.Basic.SourceVersion,["originHash"]=original.OriginHash,["kitHash"]=original.KitHash,["basicHash"]=original.Basic.BasicHash}));
   Need(JToken.DeepEquals(receipt["ordinaryRelocationBinding"],original.Relocation==null?null:new JObject{["version"]=original.Relocation.SourceVersion,["originHash"]=original.OriginHash,["basicHash"]=original.Basic.BasicHash,["relocationHash"]=original.Relocation.RelocationHash}));
   // Historical receipts remain unchanged; a supplied binding must match the retained START exactly.
   if(((JObject)receipt).Property("firstBattleEncounterBinding")!=null)
    Need(original.FirstBattle!=null&&JToken.DeepEquals(receipt["firstBattleEncounterBinding"],new JObject{["version"]=original.FirstBattle.SourceVersion,["originHash"]=original.OriginHash,["sourceHash"]=original.FirstBattle.SourceHash}));
   if(((JObject)receipt).Property("ordinaryTrainBinding")!=null)
    Need(original.Train!=null&&JToken.DeepEquals(receipt["ordinaryTrainBinding"],new JObject{["version"]=original.Train.SourceVersion,["ordinaryOriginHash"]=original.OriginHash,["sourceHash"]=original.Train.SourceHash}));
   if(original.Train?.SourceVersion==OrdinaryTrainSource.InitialHealthVersion||((JObject)receipt).Property("trainInitialState")!=null)
    _=new OrdinaryTrainInitialState((JObject)receipt,original.AccountId,original.RunId,original);
   if(original.Rail!=null||((JObject)receipt).Property("ordinaryRailBinding")!=null)
    Need(original.Rail!=null&&JToken.DeepEquals(receipt["ordinaryRailBinding"],new JObject{["version"]=original.Rail.SourceVersion,["ordinaryOriginHash"]=original.OriginHash,["sourceHash"]=original.Rail.SourceHash}));
   if(original.Roof!=null||((JObject)receipt).Property("ordinaryRoofBinding")!=null)
    Need(original.Roof!=null&&JToken.DeepEquals(receipt["ordinaryRoofBinding"],new JObject{["version"]=original.Roof.SourceVersion,["ordinaryOriginHash"]=original.OriginHash,["sourceHash"]=original.Roof.SourceHash}));
   if(((JObject)receipt).Property("sourceState")==null)return null;
   return OrdinaryFiveCoreStatSnapshot.Read((JObject)receipt["stats"],receipt["sourceState"] as JObject,(string)receipt["stats"]["packetHash"],original.AccountId,original.RunId,original.OriginHash,original.StatOriginVersion);
  }
  static void Need(bool valid){if(!valid)throw new FormatException("Ordinary initialization receipt differs from its original sources.");}
 }
}
