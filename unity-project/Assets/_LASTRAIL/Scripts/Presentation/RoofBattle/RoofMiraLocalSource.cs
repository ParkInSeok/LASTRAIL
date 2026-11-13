using System;
using System.Linq;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.RoofBattle {
 // Reconstructs the explicit approved first no-effect LOCAL profile. No lookup/START or invented equipment/pose.
 public sealed class RoofMiraLocalSource {
  public const string Version="unity.roof-mira-local-source.v1.20261017";
  readonly JObject original,trust;
  readonly BBoardingEventsNodeSnapshot node;readonly BWaveBoardingLocalPaths paths;
  readonly BMiraFirstTrialContext context;readonly BTravelRuntimeConfiguration motion;
  public string SourceHash{get;}public BDirectAttackBasicLoopBinding Binding{get;}
  public JObject Capture()=>(JObject)original.DeepClone();
  RoofMiraLocalSource(JObject packet,string hash,BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BWaveBoardingLocalPaths paths,BMiraFirstTrialContext context,BTravelRuntimeConfiguration motion){
   original=(JObject)packet.DeepClone();SourceHash=hash;Binding=binding;this.node=node;this.paths=paths;this.context=context;this.motion=motion;
   var n=node.OriginalSource.Capture();trust=new JObject(new[]{"origin","nativeContextId","seedHash","sourceHash","nodeId","visitDepth","priorSourceHash"}.Select(k=>new JProperty(k,n[k].DeepClone())));
  }
  public static RoofMiraLocalSource Read(JObject p,string independentHash){
   if(p==null||BDirectAttackInput.Digest(p)!=independentHash)throw new ArgumentException("Roof Mira original source pin.");
   BDirectAttackInput.Fields(p,"version","profile","firstEntry","nodeEntry","attack","formation","definitions","buffDefinitionsJson","composition","loop","paths","critical","motion","geometry");
   if((string)p["version"]!=Version||(string)p["profile"]!=BMiraFirstTestSource.Profile)throw new ArgumentException("Only approved first no-effect Mira LOCAL profile.");
   var entry=(JObject)p["firstEntry"];var packet=(JObject)entry["packet"];var originalMira=(JObject)entry["originalMira"];
   var first=BMiraFirstTestSource.Read(packet,packet,(string)packet["packetHash"],(string)packet["accountId"],(string)packet["runId"],originalMira,(string)originalMira["packetHash"],(string)entry["departurePin"]["pinHash"]);
   var n=(JObject)p["nodeEntry"];var boarding=(JObject)n["boardingSource"];
   var node=BBoardingEventsNodeSnapshot.Read(boarding,(string)boarding["sourceHash"],(JObject)n["trust"]);
   var f=(JObject)p["formation"];BDirectAttackInput.Fields(f,"version","trainId","mode","battlefield","internalNodeId","slots");
   if((string)f["version"]!=BCombatFormation.LocalVersion)throw new ArgumentException("Original LOCAL formation version.");
   var formation=new BCombatFormation((string)f["trainId"],(BCombatMode)Enum.Parse(typeof(BCombatMode),(string)f["mode"]),(BCombatBattlefield)Enum.Parse(typeof(BCombatBattlefield),(string)f["battlefield"]),((JArray)f["slots"]).Select(s=>new BCombatSlot((string)s["carriageId"],(string)s["operatorId"])),(string)f["internalNodeId"]);
   if(formation.Mode!=BCombatMode.Solo||formation.Battlefield!=BCombatBattlefield.ExternalVertical||formation.Slots.Count!=5||formation.Slots.Count(s=>s.OperatorId!=null)!=1||formation.Slots[1].OperatorId!="op_mira")throw new ArgumentException("Explicit single Mira / original five-car Solo LOCAL placement required.");
   var owned=(JObject)originalMira["ownedProfile"];var attack=(JObject)p["attack"];
   var input=BDirectAttackInput.ReadWaveRuntime(attack,BDirectAttackInput.Digest(attack),formation,owned,BDirectAttackInput.Digest(owned),CombatDefinitionIntake.Read((JObject)p["definitions"]),node.OriginalSource);
   var buffDefinitions=new BCarriageBuffDefinitions((string)p["buffDefinitionsJson"],(string)p["composition"]["definitionHash"]);
   var composition=new BDirectAttackBuffBinding(input,buffDefinitions,(JObject)p["composition"],BDirectAttackInput.Digest(p["composition"]));
   var binding=new BDirectAttackBasicLoopBinding(composition,(JObject)p["loop"],BDirectAttackInput.Digest(p["loop"]));
   var context=BMiraFirstTrialContext.Read(first,(JObject)p["critical"]);
   if(first.AccountId!=(string)node.OriginalSource.Capture()["origin"]["accountId"]||first.RunId!=node.OriginalSource.RunId)throw new ArgumentException("Original node/Mira account/run conflict.");
   var paths=BWaveBoardingLocalPaths.Read((JObject)p["paths"],BDirectAttackInput.Digest(p["paths"]),input);
   var motion=BTravelRuntimeConfiguration.LocalValidation((JObject)p["motion"],BDirectAttackInput.Digest(p["motion"]));
   var geometry=(JObject)p["geometry"];BDirectAttackInput.Fields(geometry,"profile","profileHash","layout","layoutHash");
   if(BDirectAttackInput.Digest(geometry["profile"])!=(string)geometry["profileHash"]||BDirectAttackInput.Digest(geometry["layout"])!=(string)geometry["layoutHash"])throw new ArgumentException("Original geometry pin.");
   return new RoofMiraLocalSource(p,independentHash,binding,node,paths,context,motion);
  }
  public MiraJudgementSceneAdapter CreateGeometry(){
   var g=(JObject)original["geometry"];var result=MiraJudgementSceneAdapter.Create((JObject)g["profile"],(string)g["profileHash"],(JObject)g["layout"],(string)g["layoutHash"]);result.SetDebugOverlay(false);return result;
  }
  internal void RequireGeometry(IMiraFirstGeometrySource geometry){if(geometry==null||!JToken.DeepEquals(geometry.CaptureAuthoring(),original["geometry"]))throw new ArgumentException("Borrowed geometry must retain original profile/layout.");}
  internal State CreateState(IMiraFirstGeometrySource geometry,int capacity){
   RequireGeometry(geometry);var native=BDirectAttackBasicLoop.CreateMiraFirstTrialRuntime(Binding,node,paths,context,geometry,capacity,motion);
   var controller=new BNodeSessionController(context.Source.AccountId,context.Source.RunId);
   try{controller.BindVerifiedNode(node.OriginalSource.Capture(),trust,0);controller.AttachLocalTravel(node,motion,(n,m)=>native,controller.Revision);return new State(controller,native);}
   catch{controller.Dispose();throw;}
  }
  internal State RestoreState(JObject saved,IMiraFirstGeometrySource geometry,int capacity){
   RequireGeometry(geometry);BDirectAttackBasicLoop native=null;
   var c=BNodeSessionController.Restore(saved,BDirectAttackInput.Digest(saved),trust,p=>native=BDirectAttackBasicLoop.RestoreMiraFirstTrialRuntime(p,BDirectAttackInput.Digest(p),Binding,node,paths,context,geometry,capacity,motion),context.Source.AccountId,context.Source.RunId);
   return new State(c,native);
  }
  internal static void RestoreGeometry(JObject saved,IMiraFirstGeometrySource geometry){var state=(JObject)saved["runtime"]["miraFirstTrial"]["nativeState"];geometry.RestoreNativeState(state,BDirectAttackInput.Digest(state));}
  internal sealed class State:IDisposable {
   readonly BNodeSessionController controller;BDirectAttackBasicLoop native;
   internal State(BNodeSessionController controller,BDirectAttackBasicLoop native){this.controller=controller;this.native=native??throw new ArgumentNullException(nameof(native));}
   internal JObject Capture()=>controller.Capture();internal long Tick=>native.Tick;internal bool Paused=>native.Paused;internal string Outcome=>native.Outcome;internal int Hp(string id)=>native.Hp(id);
   internal void Advance(string id,long elapsed,IMiraFirstGeometrySource geometry)=>controller.AdvanceMiraFirstFrame(id,elapsed,geometry,controller.Revision);
   internal void SetPaused(string id,bool value)=>controller.SetPaused(id,value,controller.Revision);
   public void Dispose(){controller.Dispose();native=null;}
  }
 }
}