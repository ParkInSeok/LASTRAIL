using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class LocalFiveCarPartyGeometryTests {
  sealed class Clock {internal long Tick;}
  static string RawHash(string raw){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false,true).GetBytes(raw))).Replace("-","").ToLowerInvariant();}
  static IOrdinaryPartyMotion Motion(Clock clock,out Dictionary<string,OrdinaryPartyAnchor> feet){
   string raw=File.ReadAllText("Assets/_LASTRAIL/Content/Battle/OrdinaryFiveCarV01/definition.json");
   feet=Enumerable.Range(0,5).ToDictionary(i=>"local-actor-"+i,i=>new OrdinaryPartyAnchor(3,-2000+i*1000,3213,0));
   return LocalFiveCarPartyGeometry.Create(raw,RawHash(raw),Enumerable.Range(0,6).Select(i=>"LOCAL_QA_HOST_"+i).ToArray(),feet,
    BInterceptSourceTests.Read(BInterceptSourceTests.Sample()).OriginalSource.OriginalSource,2000,()=>clock.Tick,_=>100,_=>1,_=>true);
  }
  [TestCase(1)][TestCase(5)] public void NativeOwnerTraversesRealCarBridgesAtApprovedLocalReviewSpeed(int destination){
   var clock=new Clock();var motion=Motion(clock,out var feet);var start=motion.Read("local-actor-2");var target=new OrdinaryPartyAnchor(destination,0,3213,0);
   motion.ValidateLeg("local-actor-2",start.Anchor,target);motion.Move("local-actor-2",target);
   clock.Tick=500;var after=motion.Read("local-actor-2"); // Existing source uses a 2ms native step: 500 ticks = 1 second.
   Assert.AreEqual(2000,Math.Abs(after.Anchor.X-start.Anchor.X));Assert.AreEqual(3,after.Anchor.Slot);
   clock.Tick=10000;Assert.AreEqual(destination,motion.Read("local-actor-2").Anchor.Slot);Assert.AreEqual(target,motion.Read("local-actor-2").Anchor);
  }
  [Test] public void NativeCancellationFreezesCurrentPositionAndStaleCancellationPreservesReplacement(){
   var clock=new Clock();var motion=Motion(clock,out var feet);long first=motion.Move("local-actor-2",new OrdinaryPartyAnchor(1,0,3213,0));
   clock.Tick=500;var stopped=motion.Read("local-actor-2").Anchor;Assert.IsTrue(motion.Cancel("local-actor-2",first));
   clock.Tick=1000;Assert.AreEqual(stopped,motion.Read("local-actor-2").Anchor);
   long second=motion.Move("local-actor-2",new OrdinaryPartyAnchor(5,0,3213,0));Assert.IsFalse(motion.Cancel("local-actor-2",first));
   Assert.AreEqual(second,motion.MovementRevision("local-actor-2"));clock.Tick=20000;Assert.AreEqual(5,motion.Read("local-actor-2").Anchor.Slot);
  }
  [Test] public void EngineDestinationAndForeignHashAreNotAccepted(){
   var clock=new Clock();var motion=Motion(clock,out var feet);
   Assert.Throws<ArgumentException>(()=>motion.ValidateLeg("local-actor-2",feet["local-actor-2"],new OrdinaryPartyAnchor(0,0,3213,0)));
   string raw=File.ReadAllText("Assets/_LASTRAIL/Content/Battle/OrdinaryFiveCarV01/definition.json");
   Assert.Throws<ArgumentException>(()=>LocalFiveCarPartyGeometry.Create(raw,"foreign",Enumerable.Range(0,6).Select(i=>"LOCAL_QA_HOST_"+i).ToArray(),feet,BInterceptSourceTests.Read(BInterceptSourceTests.Sample()).OriginalSource.OriginalSource,2000,()=>0,_=>100,_=>1,_=>true));
  }
  [TestCase(1,0)][TestCase(1,1)][TestCase(5,0)][TestCase(5,1)]
  public void VacatedCarReapproachUsesOriginalSameSideGroundPointsAndExcludesEngine(int destination,int side){
   var clock=new Clock();Motion(clock,out var feet);
   string raw=File.ReadAllText("Assets/_LASTRAIL/Content/Battle/OrdinaryFiveCarV01/definition.json");
   var type=typeof(LocalFiveCarPartyGeometry).GetNestedType("Geometry",BindingFlags.NonPublic);
   var geometry=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{raw,RawHash(raw),Enumerable.Range(0,6).Select(i=>"LOCAL_QA_HOST_"+i).ToArray(),feet},null);
   object Call(string name,params object[] args)=>type.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(geometry,args);
   var approach=(Array)Call("WorldPath",3,side,"approachMm");var target=(Array)Call("WorldPath",destination,side,"approachMm");
   var route=(Array)Call("GroundRoute",approach.GetValue(approach.Length-1),3,destination,side);
   Assert.NotNull(route);Assert.AreEqual(target.GetValue(0),route.GetValue(route.Length-1));Assert.Greater(route.Length,2);
   for(int i=1;i<route.Length;i++)Assert.IsTrue((bool)Call("Los",route.GetValue(i-1),route.GetValue(i)),"Ground reapproach cannot cross a carriage body");
   Assert.IsFalse((bool)Call("Connected",3,0));Assert.IsNull(Call("GroundRoute",approach.GetValue(0),3,0,side));
  }
  [Test] public void NativeFiveActorRoundTripPreservesHpAndClearsPassageWithoutCapsuleOverlap_GeometryOnlyNoDamage(){
   string raw=File.ReadAllText("Assets/_LASTRAIL/Content/Battle/OrdinaryFiveCarV01/definition.json");var definition=JObject.Parse(raw);
   var ids=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};var normalHp=new[]{1400,900,950,920,1000};
   var hp=ids.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>normalHp[p.i]);
   var feet=ids.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>new OrdinaryPartyAnchor(1,4000-p.i*2000,3213,p.i%2==0?-600:600));
   var hosts=Enumerable.Range(0,6).Select(i=>"LOCAL_QA_HOST_"+i).ToArray();var clock=new Clock();
   var motion=LocalFiveCarPartyGeometry.Create(raw,RawHash(raw),hosts,feet,BInterceptSourceTests.Read(BInterceptSourceTests.Sample()).OriginalSource.OriginalSource,50000,()=>clock.Tick,id=>hp[id],_=>1,_=>true);
   var radii=ids.ToDictionary(id=>id,id=>(long)((JArray)((JArray)definition["entities"]).Single(e=>(string)e["authoringId"]==id)["shapes"]).Single(s=>(string)s["kind"]=="CAPSULE_Y")["radiusMm"]);
   (long x,long z) World(OrdinaryPartyAnchor p){var origin=definition["source"]["liveLayout"][p.Slot==0?"Engine":"Car"+(p.Slot-1)]["originMm"];return (p.X+(long)origin[0],p.Z+(long)origin[2]);}
   foreach(bool left in new[]{true,false}){
    int destination=left?5:1;
    var plans=ids.Select((id,i)=>{var now=motion.Read(id).Anchor;long x=left?-4000+i*2000:4000-i*2000;return new OrdinaryPartyTransfer.Plan(id,new OrdinaryPartyAnchor(now.Slot,now.X,now.Y,0),new OrdinaryPartyAnchor(destination,x,3213,0),new OrdinaryPartyAnchor(destination,x,3213,i%2==0?-600:600),i%2==0);}).ToArray();
    using var transfer=new OrdinaryPartyTransfer(motion,hosts,left,plans);long deadline=clock.Tick+20000;
    while(transfer.State!=OrdinaryPartyTransfer.Phase.Complete&&clock.Tick<deadline){
     transfer.Step();var poses=ids.Select(id=>motion.Read(id).Anchor).ToArray();
     Assert.LessOrEqual(ids.Count(transfer.IsMoving),1,"Only one actor owns the passage");
     Assert.LessOrEqual(poses.Count(p=>p.Z==0),1,"Waiting and deployed actors must leave the central passage clear");
     for(int i=0;i<poses.Length;i++){
      Assert.That(poses[i].Slot,Is.InRange(1,5),"No actor may enter the locomotive");
      for(int j=i+1;j<poses.Length;j++){var a=World(poses[i]);var b=World(poses[j]);long dx=a.x-b.x,dz=a.z-b.z,r=radii[ids[i]]+radii[ids[j]];Assert.GreaterOrEqual(dx*dx+dz*dz,r*r,"Supplied vertical body capsules overlap");}
     }
     clock.Tick++;
    }
    Assert.AreEqual(OrdinaryPartyTransfer.Phase.Complete,transfer.State,"Native transfer must reach the far-side formation within the bounded review clock");
    for(int i=0;i<ids.Length;i++){Assert.AreEqual(plans[i].Deployment,motion.Read(ids[i]).Anchor);Assert.AreEqual(normalHp[i],hp[ids[i]]);}
   }
  }
 }
}
