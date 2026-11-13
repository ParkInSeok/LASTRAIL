using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class BArrivalRouteSessionTests {
  static void Attach(BRouteSessionController c,BOperationDestinationReceipt destination,Func<JObject> load,Action<JObject,long> save){
   var method=typeof(BRouteSessionController).GetMethod("AttachPersistentBoundaryTravel");
   Assert.NotNull(method,"Route must own the original persistent boundary session");
   method.Invoke(c,new object[]{destination,"route-boundary-test",load,save});
  }
  static JObject Export(BRouteSessionController c)=>(JObject)typeof(BRouteSessionController).GetMethod("CaptureOriginalBoundaryArrival").Invoke(c,new object[]{"report"});
  [Test] public async Task NativeCandidateCallbackCannotInstallCompetingBoundaryClock(){
   var source=BArrivalBoundarySessionTests.Fixture();var native=source.NativeNode.Route;var p=native.Capture();
   p["nativeNodeSource"]=null;p["nodeSourceStatus"]="ORIGINAL_NODE_REFS_ONLY";p.Remove("packetHash");p["packetHash"]=BDirectAttackInput.Digest(p).Substring(7);
   var before=BRouteBinding.Read(p.ToString(),(string)p["packetHash"],native.AccountId,native.RunId);
   p=native.Capture();p["mapRevision"]=native.MapRevision+1;p.Remove("packetHash");p["packetHash"]=BDirectAttackInput.Digest(p).Substring(7);
   var next=BRouteBinding.Read(p.ToString(),(string)p["packetHash"],native.AccountId,native.RunId);
   var config=global::CombatRailJourneyTests.Config();config["offers"]=new JArray();config["throttleMaximumBps"]=13000;config["throttleCruiseBps"]=10000;
   var motion=BTravelRuntimeConfiguration.LocalValidation(config,BDirectAttackInput.Digest(config));
   using(var c=new BRouteSessionController(native.AccountId,native.RunId)){
    await c.ReadDepartureSourceBundleAsync(_=>Task.FromResult(source));await c.RefreshAsync(()=>Task.FromResult(before));bool called=false;int writes=0;
    Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.RefreshAsync(()=>Task.FromResult(next),source.NativeNode.Boarding,motion,(b,m)=>{
     called=true;Assert.Throws<InvalidOperationException>(()=>c.AttachPersistentBoundaryTravel(BArrivalBoundarySessionTests.Destination(before),"competing",()=>null,(s,e)=>writes++));
     throw new InvalidOperationException("Deliberate candidate rejection");
    }));
    Assert.IsTrue(called);Assert.AreEqual(0,writes);Assert.AreSame(before,c.Route);
    Assert.Throws<InvalidOperationException>(()=>c.CaptureBoundaryTravel());
   }
  }
  [Test] public async Task RejectedRouteCandidatePreservesPublishedBoundaryClock(){
   var source=BArrivalBoundarySessionTests.Fixture();var route=source.NativeNode.Route;JObject disk=null;
   using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.ReadDepartureSourceBundleAsync(_=>Task.FromResult(source));await c.RefreshAsync(()=>Task.FromResult(route));
    Attach(c,BArrivalBoundarySessionTests.Destination(route),()=>disk,(p,e)=>disk=(JObject)p.DeepClone());
    c.Advance("first",1000000,10000);
    var p=route.Capture();p["mapRevision"]=route.MapRevision+1;p["nativeNodeSource"]=null;p["nodeSourceStatus"]="ORIGINAL_NODE_REFS_ONLY";
    p.Remove("packetHash");p["packetHash"]=BDirectAttackInput.Digest(p).Substring(7);
    var next=BRouteBinding.Read(p.ToString(),(string)p["packetHash"],route.AccountId,route.RunId);
    Assert.ThrowsAsync<InvalidOperationException>(async()=>await c.RefreshAsync(()=>Task.FromResult(next),create:(b,m)=>throw new Exception("Must not invent native source")));
    Assert.AreSame(route,c.Route);c.Advance("finish",2000000,10000);
    Assert.AreEqual(3,(long)Export(c)["report"]["travel"]["arrival"]["tick"]);
   }
  }
  [Test] public async Task ExistingRouteInputsUseRetainedSourceAndRecoverOriginalSavedArrival(){
   var source=BArrivalBoundarySessionTests.Fixture();var route=source.NativeNode.Route;JObject disk=null;bool loseAck=false;int reads=0;
   using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.ReadDepartureSourceBundleAsync(_=>{reads++;return Task.FromResult(source);});
    await c.RefreshAsync(()=>Task.FromResult(route));
    Attach(c,BArrivalBoundarySessionTests.Destination(route),()=>disk,(p,e)=>{disk=(JObject)p.DeepClone();if(loseAck)throw new IOException("lost ACK");});
    Assert.Throws<InvalidOperationException>(()=>c.AttachLocalTravel(null,null,null));
    c.SetPaused("pause",true);c.Advance("paused",9000000,10000);c.SetPaused("resume",false);
    loseAck=true;Assert.Throws<IOException>(()=>c.Advance("crossing",10000000,10000));
    Assert.IsTrue(c.CheckpointRecoveryRequired);Assert.Throws<TargetInvocationException>(()=>Export(c));
    loseAck=false;c.RecoverLocalCheckpoint();Assert.IsFalse(c.CheckpointRecoveryRequired);
    c.Advance("crossing",10000000,10000);
    Assert.AreEqual(3,(long)Export(c)["report"]["travel"]["arrival"]["tick"]);Assert.AreEqual(1,reads);
    c.Unbind();Assert.AreEqual(3,(long)Export(c)["report"]["travel"]["arrival"]["tick"]);
   }
  }
  [Test] public async Task MissingRetainedBundleCannotCreateClockOrFetchImplicitly(){
   var source=BArrivalBoundarySessionTests.Fixture();var route=source.NativeNode.Route;
   using(var c=new BRouteSessionController(route.AccountId,route.RunId)){
    await c.RefreshAsync(()=>Task.FromResult(route));int writes=0;
    Assert.Throws<TargetInvocationException>(()=>Attach(c,BArrivalBoundarySessionTests.Destination(route),()=>null,(p,e)=>writes++));
    Assert.AreEqual(0,writes);
   }
  }
 }
}
