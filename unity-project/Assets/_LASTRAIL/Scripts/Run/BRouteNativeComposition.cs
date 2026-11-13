using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Borrows immutable original inputs; created runtime is transferred to the controller.
 // This assembly is explicitly LOCAL until operating sources are supplied.
 public sealed class BRouteNativeComposition {
  readonly BRouteBinding route;readonly BBoardingEventsNodeSnapshot boarding;
  readonly BDirectAttackBasicLoopBinding binding;readonly BOrdinarySpatialGeometry geometry;
  readonly BTravelRuntimeConfiguration motion;readonly int capacity;readonly bool namedInterception;
  public BRouteNativeComposition(BRouteBinding route,BBoardingEventsNodeSnapshot boarding,BDirectAttackBasicLoopBinding binding,BOrdinaryGeometryBinding geometry,BTravelRuntimeConfiguration motion,int capacity,bool namedInterception=false)
   :this(route,boarding,binding,new BLegacySpatialGeometry(geometry),motion,capacity,namedInterception){}
  public BRouteNativeComposition(BRouteBinding route,BBoardingEventsNodeSnapshot boarding,BDirectAttackBasicLoopBinding binding,BMeasuredPhysicalGeometry geometry,BTravelRuntimeConfiguration motion,int capacity,bool namedInterception=false)
   :this(route,boarding,binding,new BMeasuredSpatialGeometry(geometry,binding.Composition),motion,capacity,namedInterception){}
  BRouteNativeComposition(BRouteBinding route,BBoardingEventsNodeSnapshot boarding,BDirectAttackBasicLoopBinding binding,BOrdinarySpatialGeometry geometry,BTravelRuntimeConfiguration motion,int capacity,bool namedInterception){
   if(route?.NativeNode==null||boarding==null||binding==null||geometry==null||geometry.Input==null||motion==null||capacity<1)throw new ArgumentException("b_route_native_original_inputs_missing");
   if(!JToken.DeepEquals(route.NativeNode.Capture(),boarding.OriginalSource.Capture())||(string)binding.Composition.Attack.Capture()["waveSourceHash"]!=boarding.OriginalSourceHash||binding.Composition.Attack.OriginalAccountId!=route.AccountId||(string)binding.Composition.Capture()["initial"]["contextId"]!=route.NativeNode.NativeContextId||geometry.Input.OriginalHash!=binding.Composition.Attack.OriginalHash)throw new ArgumentException("b_route_native_original_inputs_conflict");
   this.route=route;this.boarding=boarding;this.binding=binding;this.geometry=geometry;this.motion=motion;this.capacity=capacity;this.namedInterception=namedInterception;
  }
  public BDirectAttackBasicLoop CreateNative()=>BDirectAttackBasicLoop.CreateSpatialTravelEventRuntime(binding,boarding,geometry,capacity,motion.Capture(),motion.Hash,namedInterception);
  public BDirectAttackBasicLoop RestoreNative(JObject cache)=>BDirectAttackBasicLoop.RestoreSpatialTravelRuntime(cache,BDirectAttackInput.Digest(cache),binding,boarding,geometry,capacity,motion.Capture(),motion.Hash,namedInterception);
  public void AttachPersistentTo(BRouteSessionController controller,string context,Func<JObject> load,Action<JObject,long> save){
   if(load==null||save==null||string.IsNullOrWhiteSpace(context))throw new ArgumentException("Original node checkpoint ports required");
   AttachTo(controller);controller.ConfigureLocalCheckpoint(context,load,save,RestoreNative);
  }
  public void AttachPersistentWithAcquisitionTo(BRouteSessionController controller,string context,Func<JObject> load,Action<JObject,long> save,CombatO22ApplicationStore originals,string environment){
   if(controller==null||originals==null||load==null||save==null||string.IsNullOrWhiteSpace(context)||string.IsNullOrWhiteSpace(environment))throw new ArgumentException("Original combat entry dependencies required");
   var current=controller.Route??throw new InvalidOperationException("Original route unavailable");
   route.RequireOriginalDeparture(current);
   if(!JToken.DeepEquals(current.NativeNode?.Capture(),boarding.OriginalSource.Capture()))throw new ArgumentException("b_route_native_current_node_conflict");
   var packet=current.Capture();
   if((string)packet["phase"]!="ACTIVE"||(string)packet["runStatus"]!="ACTIVE")throw new InvalidOperationException("Original active combat required");
   // The existing secure store is the acquisition owner across node lifetimes.
   // Its original grant stage can differ from this combat node; account/run,
   // environment and retained START must still match exactly.
   var acquired=originals.LoadForRun(current.AccountId,current.RunId,environment,(string)packet["startSnapshotHash"]);
   if(!ReferenceEquals(current,controller.Route))throw new InvalidOperationException("Original route changed while reading acquisition");
   controller.AttachPersistentLocalTravel(boarding,motion,(n,c)=>CreateNative(),context,load,save,RestoreNative,acquired);
  }
  public void AttachTo(BRouteSessionController controller){if(controller==null)throw new ArgumentNullException(nameof(controller));route.RequireOriginalDeparture(controller.Route);if(!JToken.DeepEquals(controller.Route.NativeNode?.Capture(),boarding.OriginalSource.Capture()))throw new ArgumentException("b_route_native_current_node_conflict");controller.AttachLocalTravel(boarding,motion,(n,c)=>CreateNative());}
 }
}
