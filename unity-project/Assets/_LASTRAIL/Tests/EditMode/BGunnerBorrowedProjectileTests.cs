#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class BGunnerBorrowedProjectileTests {
  const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
  static readonly Assembly Runtime=typeof(BDirectAttackBasicLoop).Assembly;
  static readonly Type OwnerType=Runtime.GetType("LastRail.Battle.BGunnerProjectileOwner"),Actors=Runtime.GetType("LastRail.Battle.IBBoardingActors");
  sealed class Harness {
   internal readonly object Owner,Context;
   internal readonly BDirectAttackBuffSession Native;
   internal int Hits;
   long sequence;
   internal Func<long> Next=>()=>++sequence;
   internal Harness(bool weakTarget=false){
    var binding=BEnemyAttackAdmissionTests.Multi(1,adjust:p=>{
     var enemy=((JArray)p["entities"]).Single(e=>(string)e["id"]=="enemy");enemy["maxHp"]=1;if(weakTarget)enemy["attack"]["numerator"]="100000";
     ((JArray)p["geometry"]).Single(g=>(string)g["sourceId"]=="enemy"&&(string)g["targetId"]=="op_mira")["distanceMm"]=500;
    });
    var route=binding.Capture();route["targets"][0]["targetId"]="op_mira";binding=new BDirectAttackBasicLoopBinding(binding.Composition,route,BDirectAttackInput.Digest(route));
    Native=new BDirectAttackBuffSession(binding.Composition,10000);typeof(BDirectAttackBuffSession).GetMethod("BindProjectileTargetLifetimes",Hidden).Invoke(Native,null);Native.Advance(18000000,1800);
    Owner=Activator.CreateInstance(OwnerType,Hidden,null,new object[]{1000L,100},null);
    Context=Activator.CreateInstance(OwnerType.GetNestedType("NativeContext",BindingFlags.NonPublic),Hidden,null,new object[]{binding,Native},null);
   }
   internal bool Launch()=> (bool)OwnerType.GetMethods(Hidden).Single(m=>m.Name=="Launch"&&m.GetParameters()[2].ParameterType==typeof(long)).Invoke(Owner,new object[]{"enemy",Native.Tick,7L,Context,Next});
   internal void Resolve(){Action<string,string,string,JObject> hit=(id,source,target,attack)=>++Hits;OwnerType.GetMethods(Hidden).Single(m=>m.Name=="Resolve"&&m.GetParameters()[0].ParameterType==Actors).Invoke(Owner,new object[]{Native,Next,hit});}
   internal void Terminal()=>OwnerType.GetMethods(Hidden).Single(m=>m.Name=="Terminal"&&m.GetParameters()[0].ParameterType==Actors).Invoke(Owner,new object[]{Native,Next});
   internal JObject Capture()=>(JObject)OwnerType.GetMethod("Capture",Hidden).Invoke(Owner,null);
   internal void Advance(int ticks)=>Native.Advance(ticks*10000L,ticks);
  }
  [Test] public void SharedFlightArrivesAtCeilingBoundaryAndSurvivesSourceDeath(){
   var h=new Harness();Assert.IsTrue(h.Launch());var shot=h.Capture()["projectiles"][0];Assert.AreEqual(1821,(long)shot["contactTick"]);Assert.AreEqual(7,(long)shot["sourceGeneration"]);
   h.Native.ApplyBasicImpact("source-death","op_mira","enemy",h.Native.Tick);Assert.AreEqual(0,h.Native.Hp("enemy"));
   h.Advance(20);h.Resolve();Assert.AreEqual(0,h.Hits);h.Advance(1);h.Resolve();h.Resolve();Assert.AreEqual(1,h.Hits);Assert.AreEqual("HIT",(string)h.Capture()["projectiles"][0]["terminal"]);
  }
  [Test] public void DeadTargetMissesWithoutImpactCallback(){
   var h=new Harness(true);Assert.IsTrue(h.Launch());h.Native.ApplyBasicImpact("target-death","enemy","op_mira",h.Native.Tick);Assert.AreEqual(0,h.Native.Hp("op_mira"));
   h.Advance(21);h.Resolve();Assert.AreEqual(0,h.Hits);Assert.AreEqual("MISS",(string)h.Capture()["projectiles"][0]["terminal"]);
  }
  [Test] public void TargetGenerationChangeMissesEvenWhenHpRemains(){
   var h=new Harness();Assert.IsTrue(h.Launch());var shots=(IEnumerable)OwnerType.GetProperty("All",Hidden).GetValue(h.Owner);var shot=shots.Cast<object>().Single();shot.GetType().GetField("TargetGeneration",Hidden).SetValue(shot,2L);
   h.Advance(21);h.Resolve();Assert.Greater(h.Native.Hp("op_mira"),0);Assert.AreEqual(0,h.Hits);Assert.AreEqual("MISS",(string)h.Capture()["projectiles"][0]["terminal"]);
  }
  [Test] public void CloneKeepsPayloadIndependentAndTerminalCancelsPending(){
   var h=new Harness();Assert.IsTrue(h.Launch());var clone=OwnerType.GetMethod("Clone",Hidden).Invoke(h.Owner,null);var before=h.Capture();Assert.IsTrue(JToken.DeepEquals(before,(JObject)OwnerType.GetMethod("Capture",Hidden).Invoke(clone,null)));
   h.Terminal();h.Advance(21);h.Resolve();Assert.AreEqual(0,h.Hits);Assert.AreEqual("FLYING",(string)((JObject)OwnerType.GetMethod("Capture",Hidden).Invoke(clone,null))["projectiles"][0]["terminal"]);
  }
 }
}
#endif
