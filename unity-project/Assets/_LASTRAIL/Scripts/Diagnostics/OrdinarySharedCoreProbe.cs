#if UNITY_EDITOR || LASTRAIL_SHARED_CORE_QA
using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

namespace LastRail.Diagnostics {
 // Explicit QA fixture execution, never a server evidence issuer or an input acceptance API.
 public static class OrdinarySharedCoreProbe {
  static void Check(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
  static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new InvalidOperationException("Changed source or checkpoint was accepted.");}
  static JObject Observed(string state){var value=JObject.Parse(state);((JObject)value["commands"]).Remove("operations");return value;}
  static OrdinaryFiveStartInput Read(JObject fixture)=>OrdinaryFiveStartInput.Read((JObject)fixture["originalStart"],(string)fixture["accountId"],(string)fixture["runId"]);
  public static JObject RunInputs(string sourceJson){
   var source=JObject.Parse(sourceJson);var original=Read(source);
   var factory=OrdinaryFiveCombatFactory.FromInitialization(original,source["initializationResponse"].ToString(),500000);
   var model=factory.Create();model.SetSpeed(3);model.SetSpeed(3);model.SetPaused(true);model.SetPaused(false);model.SetUltimateAuto(true);
   string saved=model.CaptureLocalState();var inputs=model.ReadOrdinaryReplayInputs();var restored=BasicCombatSimulation.RestoreLocalState(saved,factory.Create);
   Check(((JArray)inputs["inputs"]).Count==4&&JToken.DeepEquals(inputs,restored.ReadOrdinaryReplayInputs()),"Semantic input replay differs.");
   return new JObject{["scope"]="LOCAL_SEMANTIC_INPUTS_NOT_SERVER_ACCEPTANCE",["state"]=JObject.Parse(saved),["inputs"]=inputs};
  }
  public static JObject Run(string sourceJson,string effectsJson){
   var source=JObject.Parse(sourceJson);var original=Read(source);
   var factory=OrdinaryFiveCombatFactory.FromInitialization(original,source["initializationResponse"].ToString(),500000);
   var prepared=factory.Create();string first=prepared.CaptureLocalState();prepared.Advance(10000000,20);
   Check(first==prepared.CaptureLocalState(),"Missing ARRIVE must not execute.");
   var effects=JObject.Parse(effectsJson);
   Check((string)effects["nativeEffectClassification"]=="SYNTHETIC_TRUSTED_EFFECTS_NOT_ACTUAL_ENEMY_OR_RAIL_IMPACT","Explicit resolved-effect fixture required.");
   var effectOriginal=Read(effects);var legacy=OrdinaryFiveSeraFactory.FromInitialization(effectOriginal,effects["initializationResponse"].ToString(),500000);
   var model=legacy.Create();var commands=(JArray)effects["nativeResolvedEffects"];
   Check(commands.Count==4,"Expected fixed existing four-effect scenario.");
   foreach(JObject command in commands)model.ScheduleResolvedOrdinaryEffect(command);
   model.Advance(2000000,20);string interrupted=model.CaptureLocalState();
   var resumed=BasicCombatSimulation.RestoreLocalState(interrupted,legacy.Create);
   model.Advance(2000000,20);resumed.Advance(2000000,20);
   string final=model.CaptureLocalState();Check(final==resumed.CaptureLocalState(),"Interrupted replay changed state.");
   var partitioned=legacy.Create();foreach(JObject command in commands)partitioned.ScheduleResolvedOrdinaryEffect(command);
   for(int i=0;i<8;i++)partitioned.Advance(500000,1);
   // Different frame calls legitimately retain different ADVANCE journals; compare all resulting state.
   Check(JToken.DeepEquals(Observed(final),Observed(partitioned.CaptureLocalState())),"Frame partition changed observed state.");
   foreach(JObject command in commands)Check((bool)resumed.ScheduleResolvedOrdinaryEffect(command)["replayed"],"Duplicate applied twice.");
   Check(final==resumed.CaptureLocalState(),"Duplicate replay mutated state.");
   Check(model.Tick==8&&model.GetHp(effectOriginal.Train.TrainId)==4988&&model.GetHp("op_kyle")==1350,"Existing damage/shield scenario changed.");
   var altered=JObject.Parse(final);altered["actors"][0]["vitals"]["hp"]=1;
   Reject(()=>BasicCombatSimulation.RestoreLocalState(altered.ToString(),legacy.Create));
   var wrong=(JObject)commands[1].DeepClone();wrong["command"]["accountId"]="foreign";Reject(()=>resumed.ScheduleResolvedOrdinaryEffect(wrong));
   Check(final==resumed.CaptureLocalState(),"Rejected input mutated state.");
   return new JObject{["scope"]="SHARED_SOURCE_QA_NOT_FULL_FIVE_VERIFIER",["fullCombatVerified"]=false,["serverEvidenceIssued"]=false,
    ["preparedState"]=JObject.Parse(first),["preparedHud"]=prepared.ReadHudState(),["resolvedFixtureState"]=JObject.Parse(final),
    ["resolvedFixtureEmissions"]=model.ReadSeraEmissions(),["restoredEqual"]=true,["framePartitionObservedStateEqual"]=true,["duplicateUnchanged"]=true,["tamperRejected"]=true};
  }
 }
}
#endif
