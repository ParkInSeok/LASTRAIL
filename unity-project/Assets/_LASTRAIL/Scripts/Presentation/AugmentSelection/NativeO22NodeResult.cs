using System;
using System.Collections.Generic;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.AugmentSelection {
 // Read-only projection of a committed native owner capture; never an event ledger.
 // This executor covers instant-basic O22 only, not arbitrary skills or server settlement.
 public static class NativeO22NodeResult {
  public static AugmentNodeResult Read(BRouteRuntimeObservation observation,CombatO22ApplicationOriginal original,AugmentCardPresentation card){
   if(observation==null||original==null||card==null)throw new ArgumentNullException();
   var native=observation.OriginalCheckpoint;var travel=(JObject)native["travel"];
   string run=(string)travel["origin"]["runId"],node=(string)travel["nodeId"];
   var rows=(JArray)native["results"];var commands=(JArray)native["commands"];
   if(rows.Count==0||rows.Count!=commands.Count||((string)rows.Last["outcome"]!="VICTORY"&&(string)rows.Last["outcome"]!="FAILURE"))throw new InvalidOperationException("Original node has not ended");
   var application=native["state"]["direct"]["o22Application"] as JObject;
   if(run!=original.RunId||(string)travel["origin"]["accountId"]!=original.AccountId||application==null||
    (string)application["executorVersion"]!=BDirectAttackBasicLoop.O22ExecutorVersion||
    (string)application["applicationId"]!=original.ApplicationId||(string)application["definitionPacketHash"]!=original.DefinitionPacketHash||
    (string)application["runId"]!=run||(string)application["nodeId"]!=node)throw new InvalidOperationException("Original installed O22 owner mismatch");
   long total=0;var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(var row in rows)foreach(var hit in (JArray)row["hits"]){
    var receipt=hit["lifesteal"] as JObject;if(receipt==null)continue;
    foreach(string field in new[]{"applicationId","definitionPacketHash","runId","nodeId","nativeContextId","inputHash","logicalGeneration"})
     if(!JToken.DeepEquals(receipt[field],application[field]))throw new ArgumentException("Original recovery receipt scope mismatch: "+field);
    if((string)receipt["version"]!="combat.b-native-lifesteal.v1.20261019"||
     (string)receipt["actionId"]!=(string)hit["eventId"]||(string)receipt["logicalActorId"]!=(string)hit["sourceId"]||
     receipt["actualHpGain"]?.Type!=JTokenType.Integer||(long)receipt["actualHpGain"]<0)throw new ArgumentException("Unsupported original recovery receipt");
    string key=new JArray(receipt["actionId"],receipt["logicalActorId"],receipt["logicalGeneration"]).ToString(Newtonsoft.Json.Formatting.None);
    if(!seen.Add(key))throw new ArgumentException("Duplicate original recovery receipt");
    total=checked(total+(long)receipt["actualHpGain"]);
   }
   return new AugmentNodeResult(run,node,(long)native["sequence"],new[]{new AugmentNodeResultEntry("O22",card,AugmentRecordCoverage.Complete,null,new[]{new AugmentResultMetric("실제 회복 HP",total)})});
  }
 }
}
