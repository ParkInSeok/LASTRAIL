#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class BEnemyBorrowedTimingTests {
  const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  static readonly Assembly Runtime=typeof(BDirectAttackBasicLoop).Assembly;
  static object Call(object owner,string method,params object[] args){try{return owner.GetType().GetMethods(Hidden).Single(m=>m.Name==method&&m.GetParameters().Length==args.Length).Invoke(owner,args);}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}}
  sealed class Harness {
   internal readonly BDirectAttackBuffSession Native;
   internal readonly object Timing;
   internal readonly List<long> Hits=new List<long>();
   long sequence;
   internal Harness(Func<string,long,CombatExactValue> effectivePeriod=null){
    var binding=BEnemyAttackAdmissionTests.Multi(1);Native=new BDirectAttackBuffSession(binding.Composition,10000);
    var rowType=Runtime.GetType("LastRail.Battle.BEnemyBasicTimingSource");var rows=Array.CreateInstance(rowType,1);
    rows.SetValue(Activator.CreateInstance(rowType,Hidden,null,new object[]{"enemy","car0",1600000L,0L,false,0},null),0);
    Timing=Activator.CreateInstance(Runtime.GetType("LastRail.Battle.BEnemyBasicTimingOwner"),Hidden,null,effectivePeriod==null?new object[]{"borrowed-test",1000L,rows,Native}:new object[]{"borrowed-test",1000L,rows,Native,effectivePeriod},null);
    Call(Timing,"ConfigureAdmission",BEnemyAttackAdmissionPolicy.ForBand("EARLY"));
   }
   internal Func<long> Next=>()=>++sequence;
   internal IList Collect(){
    var dueType=Runtime.GetType("LastRail.Battle.BasicCombatSimulation+DueAction");var due=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dueType));
    Action<string,string,string> hit=(eventId,source,target)=>{Hits.Add(Native.Tick);Call(Native,"ScheduleEnemyBasic",source,Native.Tick+1600);};
    var collect=Timing.GetType().GetMethods(Hidden).Single(m=>m.Name=="Collect"&&m.GetParameters()[3].ParameterType==typeof(Action<string,string,string>));
    collect.Invoke(Timing,new object[]{Native,Next,due,hit,null,null});return due;
   }
   internal static void Execute(IList due){foreach(var action in due)((Action)action.GetType().GetField("Execute",Hidden).GetValue(action))();}
   internal void Advance(int ticks,bool execute=true){Call(Native,"DriveBasicLoop",ticks*10000L,ticks,(Func<bool>)(()=>true),(Action)(()=>{var due=Collect();if(execute)Execute(due);}));}
   internal JObject Capture()=>(JObject)Call(Timing,"Capture");
  }
  [Test] public void BorrowedClockWaitsFullPeriodAfterRoofAndUsesSharedWindup(){
   var h=new Harness();Call(h.Timing,"ScheduleFirstRoofBasic","enemy",1000L,h.Native);h.Advance(2099);
   Assert.AreEqual("IDLE",(string)h.Capture()["actors"][0]["phase"]);h.Advance(1);Assert.AreEqual("WINDUP",(string)h.Capture()["actors"][0]["phase"]);
   h.Advance(499);Assert.IsEmpty(h.Hits);h.Advance(1);CollectionAssert.AreEqual(new[]{2600L},h.Hits);Assert.AreEqual(2600,h.Native.Tick);
  }
  [TestCase(false)][TestCase(true)] public void CollectedReservationCannotFireAfterGenerationCancellationOrTerminal(bool terminal){
   var h=new Harness();h.Advance(1599);h.Advance(1,false);var queued=h.Collect();Assert.AreEqual(1,queued.Count);
   if(terminal)Call(h.Timing,"Terminal",h.Native,h.Next);else Call(h.Timing,"ObserveWaveLifetime","enemy",1L,0L,false,h.Native,h.Next);
   Harness.Execute(queued);Assert.IsEmpty(h.Hits);Assert.AreEqual(0,(long)h.Capture()["actors"][0]["reservationSequence"]);
  }
  [Test] public void PeriodDelayDuringWindupKeepsCurrentCommitAndRecoveryButDelaysNextCycle(){
   bool delayed=false;int reads=0;var h=new Harness((id,tick)=>{reads++;return CombatExactValue.From(new JObject{["numerator"]=(delayed?2000000:1600000).ToString(),["denominator"]="1"});});h.Advance(1200);Assert.AreEqual("WINDUP",(string)h.Capture()["actors"][0]["phase"]);Assert.AreEqual(1600,(long)h.Capture()["actors"][0]["due"]);
   delayed=true;int before=reads;h.Advance(399);Assert.AreEqual(before,reads);Assert.AreEqual(1600,(long)h.Capture()["actors"][0]["due"]);h.Advance(1);CollectionAssert.AreEqual(new[]{1600L},h.Hits);Assert.AreEqual(1900,(long)h.Capture()["actors"][0]["due"]);
   before=reads;h.Advance(299);Assert.AreEqual(before,reads);h.Advance(1075);Assert.AreEqual("IDLE",(string)h.Capture()["actors"][0]["phase"]);h.Advance(1);Assert.AreEqual("WINDUP",(string)h.Capture()["actors"][0]["phase"]);Assert.AreEqual(3600,(long)h.Capture()["actors"][0]["due"]);h.Advance(625);CollectionAssert.AreEqual(new[]{1600L,3600L},h.Hits);
  }
  [Test] public void CancellationUsesReservedRecoveryBeforeSamplingChangedPeriod(){
   bool delayed=false;var h=new Harness((id,tick)=>CombatExactValue.From(new JObject{["numerator"]=(delayed?2000000:1600000).ToString(),["denominator"]="1"}));h.Advance(1200);delayed=true;
   Call(h.Timing,"ObserveWaveLifetime","enemy",1L,0L,false,h.Native,h.Next);Assert.AreEqual("RECOVERY",(string)h.Capture()["actors"][0]["phase"]);Assert.AreEqual(1500,(long)h.Capture()["actors"][0]["due"]);Assert.AreEqual(300,(long)h.Capture()["actors"][0]["reservedRecovery"]);
   h.Advance(299);Assert.AreEqual("RECOVERY",(string)h.Capture()["actors"][0]["phase"]);h.Advance(1);Assert.AreEqual(2000,(long)h.Capture()["actors"][0]["reservedPeriod"]);Assert.IsEmpty(h.Hits);
  }
  [Test] public void EffectivePeriodCloneRequiresExplicitRebindingAndKeepsReservedTiming(){
   var h=new Harness((id,tick)=>CombatExactValue.From(new JObject{["numerator"]="1600000",["denominator"]="1"}));h.Advance(1200);Assert.Throws<ArgumentException>(()=>Call(h.Timing,"Clone"));int reads=0;Func<string,long,CombatExactValue> rebound=(id,tick)=>{reads++;return CombatExactValue.From(new JObject{["numerator"]="2000000",["denominator"]="1"});};var clone=Call(h.Timing,"CloneWithEffectivePeriod",rebound);Assert.AreEqual(0,reads);Assert.IsTrue(JToken.DeepEquals(h.Capture(),(JObject)Call(clone,"Capture")));
  }
  [Test] public void CloneRetainsAdmissionButOwnsItsReservations(){
   var h=new Harness();h.Advance(1200);var clone=Call(h.Timing,"Clone");Assert.IsTrue(JToken.DeepEquals(h.Capture(),(JObject)Call(clone,"Capture")));
   Call(clone,"Terminal",h.Native,h.Next);Assert.AreEqual("WINDUP",(string)h.Capture()["actors"][0]["phase"]);Assert.AreEqual("TERMINAL",(string)((JObject)Call(clone,"Capture"))["actors"][0]["phase"]);
  }
 }
}
#endif
