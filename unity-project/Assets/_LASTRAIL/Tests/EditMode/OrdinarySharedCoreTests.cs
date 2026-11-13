using System.IO;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class OrdinarySharedCoreTests {
 [Test] public void SameSemanticInputProjectionMatchesStandaloneManagedReplay(){
  const string qa="D:/Project/LASTRAIL/QA/CommanderS1-20260922/ReplayInput817/";
  var result=OrdinarySharedCoreProbe.RunInputs(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json"));
  File.WriteAllText(qa+"unity-inputs.json",result.ToString());
  Assert.IsTrue(JToken.DeepEquals(result,JObject.Parse(File.ReadAllText(qa+"standalone-inputs.json"))["result"]));
 }
 [Test] public void SameSourceUnderUnityMatchesStandaloneManagedReplay(){
  const string qa="D:/Project/LASTRAIL/QA/CommanderS1-20260922/SharedCore816/";
  var result=OrdinarySharedCoreProbe.Run(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json"),File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/OrdinaryDamage788/player-input.json"));
  File.WriteAllText(qa+"unity-result.json",result.ToString());
  var independent=JObject.Parse(File.ReadAllText(qa+"standalone-result.json"));
  Assert.IsTrue(JToken.DeepEquals(result,independent["result"]),"Shared-source output differs between Unity and standalone Mono.");
 }
}
