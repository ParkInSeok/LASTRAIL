using System;
using System.IO;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryLiveInitializationVersionTests {
 static JObject Source(){using var file=File.OpenText("D:/Project/LASTRAIL/QA/ObstacleTrial-20261106/actual-server-intake-v01.json");using var reader=new JsonTextReader(file){DateParseHandling=DateParseHandling.None};return JObject.Load(reader);}
 [Test] public void ActualServerV2InitializationUsesItsOriginalKitVersion(){
  var input=Source();var start=OrdinaryFiveStartInput.Read((JObject)input["originalStart"],(string)input["accountId"],(string)input["runId"]);
  Assert.AreEqual(OrdinaryFiveStartInput.KitV2Version,start.KitSourceVersion);
  Assert.NotNull(OrdinaryInitializationReceipt.Validate(input["initializationResponse"].ToString(),start));
 }
 [Test] public void AnotherKitVersionStillCannotReplaceOriginalBinding(){
  var input=Source();var start=OrdinaryFiveStartInput.Read((JObject)input["originalStart"],(string)input["accountId"],(string)input["runId"]);
  input["initializationResponse"]["initializationReceipt"]["ordinaryKitBinding"]["version"]="unknown";
  Assert.Throws<FormatException>(()=>OrdinaryInitializationReceipt.Validate(input["initializationResponse"].ToString(),start));
 }
}
