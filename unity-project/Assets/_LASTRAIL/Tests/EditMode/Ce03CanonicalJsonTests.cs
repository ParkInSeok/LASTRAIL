using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class Ce03CanonicalJsonTests {
 static JToken Legacy(JToken t)=>t is JObject o?new JObject(o.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Legacy(p.Value)))):t is JArray a?new JArray(a.Select(Legacy)):t.DeepClone();
 static string Write(JToken t){var type=typeof(Ce03CommandContract).Assembly.GetType("LastRail.Battle.Ce03CanonicalJson");Assert.IsNotNull(type,"Direct writer required");return (string)type.GetMethod("Write",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{t});}
 [TestCase("en-US")][TestCase("tr-TR")][TestCase("ko-KR")]
 public void DirectWriterPreservesLegacyBytesHashesAndInput(string culture){
  var before=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=new CultureInfo(culture);
   var tokens=new JToken[]{new JObject{["한글"]=new JArray(new JObject{["z"]=1,["a"]="\"\\\n\t\u0001😀"},new JObject(),new JArray()),["I"]=true,["ı"]=JValue.CreateNull()},
    new JArray(long.MinValue,ulong.MaxValue,1.2300m,-0.0d,double.NaN,double.PositiveInfinity,double.NegativeInfinity),
    new JValue(new DateTime(2026,10,22,0,0,0,DateTimeKind.Utc)),new JValue(new DateTimeOffset(2026,10,22,0,0,0,TimeSpan.FromHours(9))),
    new JValue(Guid.Parse("11111111-2222-3333-4444-555555555555")),new JValue(new byte[]{0,1,255}),new JValue(TimeSpan.FromSeconds(5)),
    new JProperty("root",new JObject{["z"]=1,["a"]=2}),new JConstructor("Date",new JObject{["z"]=1,["a"]=2}),new JRaw("{\"z\":1,\"a\":2}"),JValue.CreateComment("comment"),JValue.CreateUndefined()};
   foreach(var token in tokens){string original=token.ToString(Formatting.None),expected=Legacy(token).ToString(Formatting.None),actual=Write(token);Assert.AreEqual(expected,actual);Assert.AreEqual(Ce03CommandContract.Sha(expected),Ce03CommandContract.Sha(actual));Assert.AreEqual(original,token.ToString(Formatting.None));}
  }finally{CultureInfo.CurrentCulture=before;}
 }
 [Test] public void RetainedPlayerDocumentsKeepExactCanonicalBytes(){
  const string root="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BattlePlayer465/";
  foreach(string path in new[]{"prepare.json","normal/run-0/original-start.json","normal/run-0/model-final.json","recovery/run-0/model-final.json"}){
   var token=Ce03R6RecoveryChecks.ReadJson(File.ReadAllText(root+path));Assert.AreEqual(Legacy(token).ToString(Formatting.None),Write(token),path);
  }
 }
 [Test] public void PreviousPlayerCheckpointRestoresWithoutChangingState(){
  const string folder="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/BattlePlayer465/normal/run-0/";
  var raw=File.ReadAllText(folder+"original-start.json");var value=JObject.Parse(raw);var original=new Ce03OriginalStart(raw,(string)value["owner"],new Uri((string)value["origin"]+"/"));
  var method=typeof(Ce03OriginalStart).GetMethod("CreateModel",BindingFlags.NonPublic|BindingFlags.Instance);
  var state=BasicCombatSimulation.RestoreLocalState(File.ReadAllText(folder+"model-final.json"),()=> (BasicCombatSimulation)method.Invoke(original,null));
  Assert.AreEqual(File.ReadAllText(folder+"model-final.json"),state.CaptureLocalState());
 }
}
