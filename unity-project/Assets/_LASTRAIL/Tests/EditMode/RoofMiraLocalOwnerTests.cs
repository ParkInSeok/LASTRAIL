#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class RoofMiraLocalOwnerTests {
  const string Root="D:/Project/LASTRAIL/";
  static Type T(string n){var t=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.RoofBattle."+n);Assert.That(t,Is.Not.Null,n+" not implemented");return t;}
  static object Call(object o,string method,params object[] args)=>FiveAgentVisualReferenceTests.Call(o,method,args);
  static JObject Snap(object o)=>(JObject)Call(o,"Snapshot");
  public static JObject SourceEnvelope(){
   var binding=BMiraFirstNativeFixture.Binding();var geometry=BMiraFirstNativeFixture.Scene();
   try{
    var motion=global::CombatRailJourneyTests.Config();motion["offers"]=new JArray();motion["throttleMaximumBps"]=13000;motion["throttleCruiseBps"]=10000;
    return new JObject{
     ["version"]="unity.roof-mira-local-source.v1.20261017",["profile"]=BMiraFirstTestSource.Profile,
     ["firstEntry"]=JObject.Parse(File.ReadAllText(Root+"QA/MiraFirstStatic-20261015/NativeSource/UnifiedNative/server-packets.json"))["packets"]["grown"].DeepClone(),
     ["nodeEntry"]=JObject.Parse(File.ReadAllText(Root+"QA/MiraFirstStatic-20261015/NativeSource/UnifiedNative/node-sources.json"))["packets"]["grown"].DeepClone(),
     ["attack"]=binding.Composition.Attack.Capture(),["formation"]=binding.Composition.Attack.Formation.Capture(),
     ["definitions"]=JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/c3-definition-intake-20261001.json")),
     ["buffDefinitionsJson"]=File.ReadAllText(Root+"QA/CombatBMigration-20261002/BuffConsumer/original-definitions.json"),
     ["composition"]=binding.Composition.Capture(),["loop"]=binding.Capture(),["paths"]=BMiraFirstNativeFixture.Paths(binding.Composition.Attack).Capture(),
     ["critical"]=BMiraFirstTrialContextTests.Policy(),["motion"]=motion,["geometry"]=geometry.CaptureAuthoring()};
   }finally{UnityEngine.Object.DestroyImmediate(geometry.gameObject);}
  }
  static object Source(JObject envelope=null){
   var e=envelope??SourceEnvelope();try{return T("RoofMiraLocalSource").GetMethod("Read").Invoke(null,new object[]{e,BDirectAttackInput.Digest(e)});}catch(System.Reflection.TargetInvocationException error){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
  }
  static MiraJudgementSceneAdapter Geometry(object source)=>(MiraJudgementSceneAdapter)Call(source,"CreateGeometry");
  static object Owner(object source,MiraJudgementSceneAdapter geometry,Func<JObject> load,Action<JObject,long> save)=>Activator.CreateInstance(T("RoofMiraLocalOwner"),source,geometry,"roof-mira-qa",load,save,10000);
  static void Dispose(object owner,MiraJudgementSceneAdapter geometry){(owner as IDisposable)?.Dispose();if(geometry!=null)UnityEngine.Object.DestroyImmediate(geometry.gameObject);}
  [Test]public void PinnedSourceRebuildsExactSupportedOriginalMiraProfile(){
   var e=SourceEnvelope();var source=Source(e);
   var binding=(BDirectAttackBasicLoopBinding)source.GetType().GetProperty("Binding").GetValue(source);
   Assert.That(binding.OriginalHash,Is.EqualTo(BMiraFirstNativeFixture.Binding().OriginalHash));Assert.That(binding.Composition.Attack.OperationallyExecutable,Is.False);
   e["profile"]="invented-profile";Assert.Throws<ArgumentException>(()=>Source(e));
  }
  [Test]public void NativeFlightSaveFailureRestoreAndOriginalContactPublishOnce(){
   var source=Source();var geometry=Geometry(source);JObject cache=null;bool failContact=false;int saves=0,pulses=0;
   object owner=null;IDisposable binding=null;
   try{
    owner=Owner(source,geometry,()=>cache==null?null:(JObject)cache.DeepClone(),(c,r)=>{
     if(failContact&&((JArray)c["node"]["runtime"]["miraFirstTrial"]["contacts"]).Count>0)throw new IOException("contact save blocked");
     Assert.That((long?)cache?["revision"]??0,Is.EqualTo(r));cache=(JObject)c.DeepClone();saves++;
    });
    Call(owner,"Open");binding=(IDisposable)Call(owner,"Subscribe",(Action<JObject>)(f=>pulses+=((JArray)f["events"]).Count));
    string target=BMiraFirstNativeFixture.TargetId;int hp=(int)Snap(owner)["hp"][target],frame=0;
    for(;frame<200&&((JArray)Snap(owner)["shots"]).Count==0;frame++)Call(owner,"Advance","frame-"+frame,100000L);
    Assert.That(((JArray)Snap(owner)["flights"]).Count,Is.EqualTo(1));Assert.That((int)Snap(owner)["hp"][target],Is.EqualTo(hp));
    Assert.That((long)Snap(owner)["cursor"],Is.EqualTo(1));Assert.That(pulses,Is.EqualTo(1));
    var flying=Snap(owner);int beforeReplay=saves;Call(owner,"Advance","frame-0",100000L);Assert.That(saves,Is.EqualTo(beforeReplay));Assert.That(JToken.DeepEquals(flying,Snap(owner)),Is.True);
    failContact=true;bool rejected=false;
    for(int n=0;n<70&&!rejected;n++){try{Call(owner,"Advance","contact-"+n,100000L);}catch(IOException){rejected=true;}}
    Assert.That(rejected,Is.True);var held=Snap(owner);Assert.That((bool)held["recoveryRequired"],Is.True);Assert.That((int)held["hp"][target],Is.EqualTo(hp));Assert.That(pulses,Is.EqualTo(1));
    Assert.That(((JArray)held["flights"]).Count,Is.EqualTo(1));long priorTick=(long)held["tick"];
    failContact=false;Call(owner,"Open");Assert.That(pulses,Is.EqualTo(1));Assert.That((long)Snap(owner)["tick"],Is.EqualTo(priorTick));
    Call(owner,"Advance","retry-original-contact",100000L);
    Assert.That((int)Snap(owner)["hp"][target],Is.EqualTo(hp-43));Assert.That((long)Snap(owner)["cursor"],Is.EqualTo(2));Assert.That(pulses,Is.EqualTo(2));
    Assert.That(((JArray)Snap(owner)["flights"]).Count,Is.Zero);Assert.That((string)Snap(owner)["shots"][0]["status"],Is.EqualTo("HIT"));
    var accepted=Snap(owner);Call(owner,"Open");Assert.That(JToken.DeepEquals(accepted,Snap(owner)),Is.True);Assert.That(pulses,Is.EqualTo(2));
   }finally{binding?.Dispose();Dispose(owner,geometry);}
  }
  [Test]public void PauseAndSnapshotSubscriptionCannotAdvanceOrReplayOriginalFlight(){
   var source=Source();var geometry=Geometry(source);JObject cache=null;object owner=null;
   try{
    owner=Owner(source,geometry,()=>cache,(c,r)=>cache=(JObject)c.DeepClone());Call(owner,"Open");
    bool rejected=false;using var sub=(IDisposable)Call(owner,"Subscribe",(Action<JObject>)(f=>{try{Call(owner,"Advance","reentrant",100000L);}catch(InvalidOperationException){rejected=true;}}));
    Assert.That(rejected,Is.True);Call(owner,"SetPaused","pause",true);long tick=(long)Snap(owner)["tick"];Call(owner,"Advance","held",100000L);Assert.That((long)Snap(owner)["tick"],Is.EqualTo(tick));
    Call(owner,"SetPaused","resume",false);Call(owner,"Advance","resume-frame",100000L);Assert.That((long)Snap(owner)["tick"],Is.EqualTo(tick+1));
   }finally{Dispose(owner,geometry);}
  }
  [Test]public void RehashedWrongCursorFailsBeforePublishingNativePoses(){
   var source=Source();var geometry=Geometry(source);JObject cache=null;object owner=null;
   try{
    owner=Owner(source,geometry,()=>cache,(c,r)=>cache=(JObject)c.DeepClone());Call(owner,"Open");Call(owner,"Advance","first-frame",100000L);
    var native=geometry.CaptureNativeState(9,999);cache["cursor"]=99;cache.Remove("hash");cache["hash"]=BDirectAttackInput.Digest(cache);
    Assert.Throws<ArgumentException>(()=>Call(owner,"Open"));Assert.That(JToken.DeepEquals(native,geometry.CaptureNativeState(9,999)),Is.True);
   }finally{Dispose(owner,geometry);}
  }
  [Test]public void TypedStorageRequiresExplicitMiraContractWithoutChangingEveDefault(){
   T("RoofMiraLocalOwner");var type=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.RoofBattle.RoofBattleLocalFileStore");
   string dir=Root+"QA/FiveAgentVisual-20261017/MiraTypedStore-"+Guid.NewGuid().ToString("N");
   var source=Source();var geometry=Geometry(source);object owner=null;JObject candidate=null;
   try{
    owner=Owner(source,geometry,()=>null,(c,r)=>candidate=(JObject)c.DeepClone());Call(owner,"Open");
    var disk=Activator.CreateInstance(type,dir,"roof-mira-qa","unity.roof-mira-local-checkpoint.v1.20261017");Call(disk,"Save",candidate,0L);Assert.That(JToken.DeepEquals(candidate,Call(disk,"Load") as JObject),Is.True);
    var old=Activator.CreateInstance(type,dir+"-eve","roof-mira-qa");Assert.Throws<ArgumentException>(()=>Call(old,"Save",candidate,0L));
   }finally{Dispose(owner,geometry);}
  }
 }
}
#endif