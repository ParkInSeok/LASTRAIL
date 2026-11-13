using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public class OrdinaryFiveSeraCheckpointTests {
 [Test] public void RetainedStartMustMatchWholeOriginalBeforeReusingDefinitions(){
  var original=Original();var response=JObject.Parse(original.StartResultJson);
  response["startResponse"]["serverStartedAt"]="2001-01-01T00:00:00.000Z";
  var changed=new PendingAccountAction(original.Account,original.Kind,original.Target,original.Count,original.Key,original.Payload,response.ToString());
  int writes=0;var store=new CombatCheckpointStore("D:/Project/LASTRAIL/LocalTemp/Unity/definition-reuse.json",()=>null,_=>writes++);
  Assert.Throws<ArgumentException>(()=>new OrdinaryFiveSeraCheckpoint(store,changed,Factory(),new AppBuildSnapshot("test",1)));
  Assert.AreEqual(0,writes);
 }
 static OrdinaryFiveSeraFactory Factory(){var source=OrdinaryFiveSeraFactoryTests.Sources();return new OrdinaryFiveSeraFactory(source.Input,source.Stats,500000);}
 static PendingAccountAction Original(string key="original-key"){
  var source=OrdinaryFiveSeraFactoryTests.Sources();var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]="future-run",["runStatus"]="ACTIVE",["startResponse"]=source.Start,["errorCode"]=null};
  return new PendingAccountAction("a","departure-start","preview",0,key,"{\"operationPreviewId\":\"preview\"}",response.ToString());
 }
 static object Binding(CombatCheckpointStore store,string key="original-key",string build="test-1"){
  var type=typeof(CombatCheckpointStore).Assembly.GetType("LastRail.Battle.OrdinaryFiveSeraCheckpoint");Assert.NotNull(type,"Product journal to native checkpoint binding missing");
  return Activator.CreateInstance(type,new object[]{store,Original(key),Factory(),new AppBuildSnapshot(build,1)});
 }
 static CombatSceneDriver Driver(object binding){var method=typeof(CombatSceneDriver).GetMethod("ForOrdinarySeraStart");Assert.NotNull(method);return (CombatSceneDriver)method.Invoke(null,new object[]{binding,(Func<bool>)(()=>true)});}
 static void Initial(object binding)=>binding.GetType().GetMethod("Save").Invoke(binding,new object[]{Factory().Create(),0L});
 static void Cast(CombatSceneDriver driver){var method=typeof(CombatSceneDriver).GetMethod("ScheduleResolvedSeraCommand");Assert.NotNull(method);try{method.Invoke(driver,new object[]{OrdinaryFiveSeraFactoryTests.Cast()});}catch(TargetInvocationException e){throw e.InnerException;}}
 static string PathForTest(){var root="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/OrdinarySeraStoreTests/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(root);return root+"/checkpoint.json";}
 [Test] public void ProductOriginalRequestPinsPendingNativeHealAndRejectsAnotherKeyOrBuild(){
  var path=PathForTest();var store=CombatCheckpointStore.CreateExplicitLocalQaFile(path);var binding=Binding(store);Initial(binding);var first=Driver(binding);first.Open();Cast(first);first.Advance(2000000,20);var pending=first.StateJson;
  var restored=Driver(Binding(CombatCheckpointStore.CreateExplicitLocalQaFile(path)));restored.Open();Assert.AreEqual(pending,restored.StateJson);restored.Advance(2000000,20);
  var state=JObject.Parse(restored.StateJson);Assert.AreEqual(2,((JArray)state["commands"]["receipts"]).Count);
  Assert.Throws<InvalidOperationException>(()=>Driver(Binding(store,"other-key")).Open());Assert.Throws<InvalidOperationException>(()=>Driver(Binding(store,build:"test-2")).Open());
 }
 [Test] public void FailedSaveDoesNotPublishAcceptedCommand(){string saved=null;bool fail=false;var store=new CombatCheckpointStore(PathForTest(),()=>saved,raw=>{if(fail)throw new IOException("test failure");saved=raw;});var binding=Binding(store);Initial(binding);var driver=Driver(binding);driver.Open();var before=driver.StateJson;fail=true;Assert.Throws<IOException>(()=>Cast(driver));Assert.AreEqual(before,driver.StateJson);Assert.IsTrue(driver.RecoveryRequired);fail=false;driver.Open();var expected=BasicCombatSimulation.RestoreLocalState(before,Factory().Create);expected.SetPaused(true);Assert.AreEqual(expected.CaptureLocalState(),driver.StateJson);}
 [Test] public void MissingCheckpointNeverCreatesNewRun(){string saved=null;var store=new CombatCheckpointStore(PathForTest(),()=>saved,raw=>saved=raw);var driver=Driver(Binding(store));Assert.Throws<InvalidOperationException>(()=>driver.Open());Assert.IsNull(saved);Assert.IsTrue(driver.RecoveryRequired);}
 [Test] public void StaleParallelOwnerCannotPublish(){var store=CombatCheckpointStore.CreateExplicitLocalQaFile(PathForTest());var binding=Binding(store);Initial(binding);var first=Driver(binding);var second=Driver(binding);first.Open();second.Open();Cast(first);Assert.Throws<InvalidOperationException>(()=>Cast(second));Assert.IsTrue(second.RecoveryRequired);second.Open();var expected=BasicCombatSimulation.RestoreLocalState(first.StateJson,Factory().Create);expected.SetPaused(true);Assert.AreEqual(expected.CaptureLocalState(),second.StateJson);}
 [Test] public void SeparateProcessPendingHealCheckpoint(){
  string root=Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_CHECKPOINT_QA"),mode=Environment.GetEnvironmentVariable("LASTRAIL_ORDINARY_CHECKPOINT_MODE");if(root==null||mode==null)Assert.Ignore("Explicit cross-process QA invocation only.");
  root=Path.GetFullPath(root);Assert.IsTrue(root.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase));Directory.CreateDirectory(root);var binding=Binding(CombatCheckpointStore.CreateExplicitLocalQaFile(root+"/checkpoint.json"));var driver=Driver(binding);
  if(mode=="WRITE"){
   var source=OrdinaryFiveSeraFactoryTests.Sources();var stats=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Stats589/fixture.json"))["first"];
   File.WriteAllText(root+"/player-input.json",new JObject{["scope"]="SYNTHETIC_START_NATIVE_SERA_COMPONENT_ONLY",["account"]="a",["run"]="future-run",["start"]=source.Start,["stats"]=stats.DeepClone()}.ToString());
   Initial(binding);driver.Open();Cast(driver);driver.Advance(2000000,20);var pending=driver.StateJson;var baseline=BasicCombatSimulation.RestoreLocalState(pending,Factory().Create);baseline.Advance(2000000,20);File.WriteAllText(root+"/expected.json",baseline.CaptureLocalState());File.WriteAllText(root+"/writer.json",new JObject{["pid"]=System.Diagnostics.Process.GetCurrentProcess().Id,["scope"]="SYNTHETIC_SOURCE_NATIVE_SERA_COMPONENT_EXPLICIT_QA_STORE",["tick"]=driver.Tick}.ToString());
  }else{
   Assert.AreEqual("READ",mode);var writer=JObject.Parse(File.ReadAllText(root+"/writer.json"));Assert.AreNotEqual((int)writer["pid"],System.Diagnostics.Process.GetCurrentProcess().Id);driver.Open();Assert.AreEqual(4,driver.Tick);driver.Advance(2000000,20);Assert.AreEqual(File.ReadAllText(root+"/expected.json"),driver.StateJson);File.WriteAllText(root+"/reader.json",new JObject{["pid"]=System.Diagnostics.Process.GetCurrentProcess().Id,["writerPid"]=writer["pid"].DeepClone(),["result"]="MATCHED",["tick"]=driver.Tick}.ToString());
  }
 }
}
