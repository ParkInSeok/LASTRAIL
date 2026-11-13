using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 public sealed class BBoardingPhaseEligibility {
  public bool Targetable {get;}public bool AttackEligible {get;}public bool ClimbBonusEligible {get;}
  internal BBoardingPhaseEligibility(string phase){One(new JValue(phase),"GRACE","APPROACH","ATTACHED","CLIMBING","ROOFTOP");Targetable=phase!="GRACE";AttackEligible=phase=="ROOFTOP";ClimbBonusEligible=phase=="ATTACHED"||phase=="CLIMBING";}
 }
 // Immutable received policy. This does not advance phases or supply geometry/ability authority.
 public sealed class BBoardingPolicyDefinition {
  readonly JObject packet;
  public string Version=>(string)packet["version"];
  public int GraceMilliseconds {get;}public int ApproachMilliseconds {get;}public int AttachedMilliseconds {get;}public int ClimbingMilliseconds {get;}
  public int TargetableAfterMilliseconds=>(int)packet["targetableAfterMs"];public int NominalRooftopMilliseconds=>(int)packet["nominalRooftopAfterMs"];
  public int LeftSockets=>(int)packet["sockets"]["left"];public int RightSockets=>(int)packet["sockets"]["right"];
  internal BBoardingPolicyDefinition(JObject p){packet=(JObject)p.DeepClone();var rows=(JArray)packet["phases"];GraceMilliseconds=(int)rows[0]["durationMs"];ApproachMilliseconds=(int)rows[1]["durationMs"];AttachedMilliseconds=(int)rows[2]["durationMs"];ClimbingMilliseconds=(int)rows[3]["durationMs"];}
  public BBoardingPhaseEligibility Eligibility(string ownedPhase)=>new BBoardingPhaseEligibility(ownedPhase);
  public JObject Capture()=>(JObject)packet.DeepClone();
 }
 public sealed class BSimultaneousEventPolicyDefinition {
  readonly JObject packet;
  public string Version=>(string)packet["version"];
  public IReadOnlyList<string> PhaseOrder {get;}public IReadOnlyList<string> ActionPriority {get;}
  public bool NewReservationAtContactAllowed=>(bool)packet["newReservationAtContactAllowed"];
  public bool AutoOffPreservesInterception=>(bool)packet["autoOffDisablesOnlyAutomaticSkill"];
  internal BSimultaneousEventPolicyDefinition(JObject p){packet=(JObject)p.DeepClone();PhaseOrder=Array.AsReadOnly(((JArray)p["sameTickPhaseOrder"]).Select(x=>(string)x).ToArray());ActionPriority=Array.AsReadOnly(((JArray)p["actionPriority"]).Select(x=>(string)x).ToArray());}
  public JObject Capture()=>(JObject)packet.DeepClone();
 }
 public sealed class BBoardingEventsDefinitionSnapshot {
  public const string Version="combat.b-scout-gunner-source.v2.20261004",DefinitionVersion="combat.b-scout-gunner-definition.v2.20261004";
  public const string ApprovedDefinitionHash="6f543ed7ed36c086eee423c7dd6f196ec54799e5cc7db526e6a1e996826ac9f1";
  public const string ApprovedPacketHash="8eec6b80024a89f1f342032c34fcde9df21462c3915e5b174fa57f3527cf0c20";
  readonly JObject packet;
  public BBoardingPolicyDefinition Boarding {get;}public BSimultaneousEventPolicyDefinition Events {get;}
  public bool DefinitionAvailable=>true;public bool RuntimeSupported=>false;public bool CanStart=>false;
  public bool InterceptionTimingSupported=>(bool)packet["definition"]["interceptionTiming"]["supported"];
  public long? InterceptionPreparationMicroseconds=>null;public long? InterceptionRecoveryMicroseconds=>null;public string InterceptionTimingSource=>null;
  BBoardingEventsDefinitionSnapshot(JObject p){packet=(JObject)p.DeepClone();Boarding=new BBoardingPolicyDefinition((JObject)packet["definition"]["boarding"]);Events=new BSimultaneousEventPolicyDefinition((JObject)packet["definition"]["events"]);}
  internal static void Bounded(JObject p){Need(p!=null);RunWaveManifestReader.Bounded(p);}
  internal static void Flags(JObject p){Need(p["definitionAvailable"]?.Type==JTokenType.Boolean&&(bool)p["definitionAvailable"]&&p["runtimeSupported"]?.Type==JTokenType.Boolean&&!(bool)p["runtimeSupported"]&&p["canStart"]?.Type==JTokenType.Boolean&&!(bool)p["canStart"]);}
  internal static void ValidateDefinition(JObject d){Bounded(d);Fields(d,"version","approval","originalDefinition","originalDefinitionHash","boarding","events","interceptionTiming","definitionAvailable","runtimeSupported","canStart","missingRuntimeSources","definitionHash");Need((string)d["version"]==DefinitionVersion&&(string)d["definitionHash"]==ApprovedDefinitionHash&&(string)d["originalDefinitionHash"]==BScoutGunnerDefinitionSnapshot.ApprovedDefinitionHash);SelfHash(d,"definitionHash");BScoutGunnerDefinitionSnapshot.ValidateDefinition(Obj(d["originalDefinition"]));Flags(d);}
  public JObject Capture()=>(JObject)packet.DeepClone();
  public static BBoardingEventsDefinitionSnapshot Read(JObject input,string independentlyExpectedPacketHash){Bounded(input);var p=(JObject)input.DeepClone();Fields(p,"version","definition","definitionAvailable","runtimeSupported","canStart","packetHash");Need(Hash(new JValue(independentlyExpectedPacketHash))==ApprovedPacketHash&&(string)p["version"]==Version&&(string)p["packetHash"]==independentlyExpectedPacketHash);SelfHash(p,"packetHash");ValidateDefinition(Obj(p["definition"]));Flags(p);return new BBoardingEventsDefinitionSnapshot(p);}
  public static BBoardingEventsDefinitionSnapshot ReadJson(string json,string independentlyExpectedPacketHash)=>Read(Parse(json),independentlyExpectedPacketHash);
  internal static BBoardingEventsDefinitionSnapshot FromValidatedDefinition(JObject d)=>Read(new JObject{["version"]=Version,["definition"]=d.DeepClone(),["definitionAvailable"]=true,["runtimeSupported"]=false,["canStart"]=false,["packetHash"]=ApprovedPacketHash},ApprovedPacketHash);
 }
}
