#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class RoofBattleLocalOwnerTests {
  public static BDirectAttackBasicLoopBinding Binding(){var i=BEveBasicTests.Input();return (BDirectAttackBasicLoopBinding)typeof(BEveBasicTests).GetMethod("LoopBinding",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{i,"BCB02"});}
  static Type Type(string n){var t=typeof(BDirectAttackInput).Assembly.GetType("LastRail.Presentation.RoofBattle."+n);Assert.That(t,Is.Not.Null,n);return t;}
  static object Call(object o,string m,params object[] a)=>FiveAgentVisualReferenceTests.Call(o,m,a);
  static JObject Read(object o)=>(JObject)Call(o,"Snapshot");
  sealed class Store{public JObject Saved;public int Saves;public bool Fail;public JObject Load()=>Saved==null?null:(JObject)Saved.DeepClone();public void Save(JObject candidate,long expected){if(Fail)throw new IOException("local QA save failure");Assert.That((long?)Saved?["revision"]??0,Is.EqualTo(expected));Saved=(JObject)candidate.DeepClone();Saves++;}}
  static object Owner(Store s)=>Activator.CreateInstance(Type("RoofBattleLocalOwner"),Binding(),1000,"roof-local-eve-qa",(Func<JObject>)s.Load,(Action<JObject,long>)s.Save);
  [Test]public void NativeExecutionObservationComesFromOriginalActionAndKeepsCheckpointWire(){
   var b=Binding();var native=new BDirectAttackBasicLoop(b,1000);var r=native.Advance("original-fire",10000,1);
   var property=r.GetType().GetProperty("ExecutedBasics");Assert.That(property,Is.Not.Null,"Explicit native execution producer missing; do not infer launch from hit");
   var executed=((IEnumerable)property.GetValue(r)).Cast<object>().ToArray();Assert.That(executed.Length,Is.EqualTo(1));
   var fact=(JObject)Call(executed[0],"Capture");Assert.That((string)fact["sourceId"],Is.EqualTo("op_eve"));Assert.That((long)fact["tick"],Is.EqualTo(r.Hits[0].Attack.Tick));Assert.That((string)fact["actionId"],Is.EqualTo(r.Hits[0].Attack.EventId));Assert.That((long)fact["sequence"],Is.GreaterThan(0));
   var snapshot=native.Capture();Assert.That(snapshot.Properties().Select(p=>p.Name),Does.Not.Contain("presentation"));Assert.That(JToken.DeepEquals(snapshot,BDirectAttackBasicLoop.Restore(snapshot,b,1000).Capture()),Is.True);
  }
  [Test]public void SingleNativeOwnerPublishesOnlyAfterSaveAndDuplicateCommandDoesNotReplay(){
   var store=new Store();var o=Owner(store);Call(o,"Open");JObject frame=null;var subscription=(IDisposable)Call(o,"Subscribe",(Action<JObject>)(f=>frame=f));
   Assert.That(frame["events"].Count(),Is.Zero);Call(o,"Advance","first",10000L,1);
   Assert.That((string)frame["kind"],Is.EqualTo("COMMIT"));Assert.That(frame["events"].Select(e=>(string)e["phase"]),Is.EqualTo(new[]{"EXECUTED_INSTANT","HIT"}));Assert.That((long)frame["cursor"],Is.EqualTo(2));
   Assert.That((int)frame["hp"]["enemy"],Is.EqualTo(675));Assert.That(frame["events"].All(e=>e["worldPoint"].Type==JTokenType.Null),Is.True,"Distance-only input has no contact point; no invented target center");
   int saves=store.Saves;var old=frame.DeepClone();Call(o,"Advance","first",10000L,1);Assert.That(store.Saves,Is.EqualTo(saves));Assert.That(JToken.DeepEquals(old,frame),Is.True);
   subscription.Dispose();var next=Owner(store);Call(next,"Open");JObject restored=null;using var resumed=(IDisposable)Call(next,"Subscribe",(Action<JObject>)(f=>restored=f));Assert.That((long)restored["cursor"],Is.EqualTo(2));Assert.That(restored["events"].Count(),Is.Zero);Assert.That((int)restored["hp"]["enemy"],Is.EqualTo(675));
  }
  [Test]public void FailedSaveHoldsOriginalSnapshotAndRestoreDoesNotReplayUnsavedFire(){
   var store=new Store();var o=Owner(store);Call(o,"Open");JObject frame=null;using var sub=(IDisposable)Call(o,"Subscribe",(Action<JObject>)(f=>frame=f));var initial=store.Saved.DeepClone();store.Fail=true;
   Assert.Throws<IOException>(()=>Call(o,"Advance","failed-fire",10000L,1));Assert.That(JToken.DeepEquals(initial,store.Saved),Is.True);Assert.That((string)frame["kind"],Is.EqualTo("HOLD"));Assert.That((long)frame["cursor"],Is.Zero);Assert.That((int)frame["hp"]["enemy"],Is.EqualTo(700));
   Assert.Throws<InvalidOperationException>(()=>Call(o,"Advance","blocked",10000L,1));store.Fail=false;Call(o,"Open");Call(o,"Advance","failed-fire",10000L,1);Assert.That((int)frame["hp"]["enemy"],Is.EqualTo(675));Assert.That(frame["events"].Count(),Is.EqualTo(2));
  }
  [Test]public void HoldSpeedRebindAndSubscriptionDisposalKeepOriginalCursor(){
   var store=new Store();var o=Owner(store);Call(o,"Open");int calls=0;JObject frame=null;var a=(IDisposable)Call(o,"Subscribe",(Action<JObject>)(f=>{frame=f;calls++;}));
   Call(o,"SetPaused","pause",true);Call(o,"Advance","held",100000L,10);Assert.That((long)frame["tick"],Is.Zero);Assert.That((bool)frame["paused"],Is.True);
   a.Dispose();int before=calls;Call(o,"SetPaused","resume",false);Assert.That(calls,Is.EqualTo(before));using var b=(IDisposable)Call(o,"Subscribe",(Action<JObject>)(f=>frame=f));Assert.That(frame["events"].Count(),Is.Zero);
   Call(o,"SetSpeed","speed",4);Call(o,"Advance","double",10000L,2);Assert.That((long)frame["tick"],Is.EqualTo(2));Assert.That((long)frame["cursor"],Is.EqualTo(2));
  }
  [Test]public void InitialSnapshotConsumerCannotReenterOriginalOwnerCommand(){var store=new Store();var o=Owner(store);Call(o,"Open");int saves=store.Saves;Assert.Throws<InvalidOperationException>(()=>Call(o,"Subscribe",(Action<JObject>)(f=>{if((string)f["kind"]=="SNAPSHOT")Call(o,"Advance","reentrant",10000L,1);})));Assert.That(store.Saves,Is.EqualTo(saves));}
  [Test]public void FailingHoldSubscriberCannotHideOriginalPersistenceFailure(){var store=new Store();var o=Owner(store);Call(o,"Open");using var sub=(IDisposable)Call(o,"Subscribe",(Action<JObject>)(f=>{if((string)f["kind"]=="HOLD")throw new InvalidOperationException("QA view failure");}));store.Fail=true;var error=Assert.Throws<AggregateException>(()=>Call(o,"Advance","save-fail",10000L,1));Assert.That(error.Flatten().InnerExceptions.Any(e=>e is IOException),Is.True,"Original save failure must remain visible with notification failure");Assert.That((long)Read(o)["cursor"],Is.Zero);}
  [Test]public void FailedRestoreHoldsPriorCommittedSnapshot(){var store=new Store();bool failLoad=false;var o=Activator.CreateInstance(Type("RoofBattleLocalOwner"),Binding(),1000,"roof-local-eve-qa",(Func<JObject>)(()=>failLoad?throw new IOException("QA load failure"):store.Load()),(Action<JObject,long>)store.Save);Call(o,"Open");JObject frame=null;using var sub=(IDisposable)Call(o,"Subscribe",(Action<JObject>)(f=>frame=f));failLoad=true;Assert.Throws<IOException>(()=>Call(o,"Open"));Assert.That((string)frame["kind"],Is.EqualTo("HOLD"));Assert.That((long)frame["cursor"],Is.Zero);}
  [Test]public void FailingCommittedViewDoesNotTurnSuccessfulSaveIntoRecoveryFailure(){var store=new Store();var o=Owner(store);Call(o,"Open");using var sub=(IDisposable)Call(o,"Subscribe",(Action<JObject>)(f=>{if((string)f["kind"]=="COMMIT")throw new InvalidOperationException("QA committed view failure");}));Assert.Throws<AggregateException>(()=>Call(o,"Advance","saved",10000L,1));Assert.That((int)Read(o)["hp"]["enemy"],Is.EqualTo(675));Assert.That((bool)Read(o)["recoveryRequired"],Is.False);Assert.That((long)store.Saved["cursor"],Is.EqualTo(2));}
  [Test]public void MutatedCheckpointCursorOrEventCannotBeAcceptedAsOriginal(){
   var store=new Store();var o=Owner(store);Call(o,"Open");Call(o,"Advance","first",10000L,1);store.Saved["cursor"]=999;var altered=(JObject)store.Saved.DeepClone();altered.Remove("hash");store.Saved["hash"]=BDirectAttackInput.Digest(altered);Assert.Throws<ArgumentException>(()=>Call(Owner(store),"Open"));
  }
 }
}
#endif
