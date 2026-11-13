#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class RoofBattleLocalStorageTests {
  static Type Type(string n){var t=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.RoofBattle."+n);Assert.That(t,Is.Not.Null,n);return t;}
  static object Call(object o,string m,params object[] args)=>FiveAgentVisualReferenceTests.Call(o,m,args);
  public static JObject SourceEnvelope(){var b=RoofBattleLocalOwnerTests.Binding();const string root="D:/Project/LASTRAIL/";return new JObject{["version"]="unity.roof-local-source.v1.20261017",["classification"]="EXISTING_EVE_BASIC_LOCAL_NOT_OPERATIONAL",["attack"]=b.Composition.Attack.Capture(),["formation"]=b.Composition.Attack.Formation.Capture(),["ownedProfile"]=JObject.Parse(File.ReadAllText(root+"QA/CommanderS1-20260922/c3-owned-profile-observed-sql-test-20261001.json"))["profile"].DeepClone(),["definitions"]=JObject.Parse(File.ReadAllText(root+"QA/CommanderS1-20260922/c3-definition-intake-20261001.json")),["buffDefinitions"]=JObject.Parse(File.ReadAllText(root+"QA/CombatBMigration-20261002/BuffConsumer/original-definitions.json")),["composition"]=b.Composition.Capture(),["loop"]=b.Capture()};}
  [Test]public void SerializedLocalSourceReconstructsExactOriginalBindingAndRejectsWrongPin(){var source=SourceEnvelope();var type=Type("RoofBattleLocalSource");var read=type.GetMethod("Read");object result=read.Invoke(null,new object[]{source,BDirectAttackInput.Digest(source)});var binding=(BDirectAttackBasicLoopBinding)type.GetProperty("Binding").GetValue(result);Assert.That(binding.OriginalHash,Is.EqualTo(RoofBattleLocalOwnerTests.Binding().OriginalHash));Assert.That(binding.Composition.Attack.OperationallyExecutable,Is.False);var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>read.Invoke(null,new object[]{source,"sha256:wrong"}));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
  [Test]public void AtomicLocalStoreRoundtripAndStaleWriterPreserveOriginalCheckpoint(){
   string directory="D:/Project/LASTRAIL/QA/FiveAgentVisual-20261017/RoofStore-"+Guid.NewGuid().ToString("N");var type=Type("RoofBattleLocalFileStore");var disk=Activator.CreateInstance(type,directory,"roof-local-eve-qa");
   Func<JObject> load=()=>(JObject)Call(disk,"Load");Action<JObject,long> save=(j,r)=>Call(disk,"Save",j,r);
   var ownerType=Type("RoofBattleLocalOwner");object Create()=>Activator.CreateInstance(ownerType,RoofBattleLocalOwnerTests.Binding(),1000,"roof-local-eve-qa",load,save);
   var a=Create();Call(a,"Open");var b=Create();Call(b,"Open");Call(a,"Advance","first",10000L,1);var saved=load();Assert.Throws<InvalidOperationException>(()=>Call(b,"Advance","stale",10000L,1));Assert.That(JToken.DeepEquals(saved,load()),Is.True);Call(b,"Open");Assert.That((long)((JObject)Call(b,"Snapshot"))["cursor"],Is.EqualTo(2));Assert.That(Directory.GetFiles(directory,"*.tmp").Length,Is.Zero);
  }
  [Test]public void StoreRejectsNonProjectOutputAndSourceContextMismatch(){var type=Type("RoofBattleLocalFileStore");var bad=Assert.Throws<System.Reflection.TargetInvocationException>(()=>Activator.CreateInstance(type,"C:/UnapprovedRoofOutput","roof-local-eve-qa"));Assert.That(bad.InnerException,Is.TypeOf<ArgumentException>());}
 }
}
#endif
