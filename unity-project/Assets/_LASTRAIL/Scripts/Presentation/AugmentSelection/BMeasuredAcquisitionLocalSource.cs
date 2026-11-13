using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.AugmentSelection {
 // Explicit offline QA intake. Source pins detect substitution; this class
 // supplies neither authentication, accepted arrival nor operating START.
 public sealed class BMeasuredAcquisitionLocalSource {
  public const string Version="unity.b-measured-acquisition-local-source.v1.20261020";
  public BBoardingEventsNodeSnapshot Boarding{get;}
  public BDirectAttackBasicLoopBinding Binding{get;}
  public BRouteBinding NoncombatRoute{get;}
  public BRouteBinding CombatRoute{get;}
  public AugmentSelectionSnapshot InitialSelection{get;}
  public CombatO22ApplicationOriginal Original{get;}
  public BMeasuredPhysicalGeometry Geometry{get;}
  public BTravelRuntimeConfiguration Motion{get;}
  public string SourceHash{get;}
  public string Environment=>"TEST";
  public string SelectResponseJson{get;}
  static void Need(bool value,string reason){if(!value)throw new ArgumentException("b_measured_local_source_"+reason);}
  static JObject Obj(JToken token)=>token as JObject??throw new ArgumentException("b_measured_local_source_object");
  static string BytesHash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
  static string PinnedText(JObject source,string textKey,string hashKey){
   Need(source[textKey]?.Type==JTokenType.String&&source[hashKey]?.Type==JTokenType.String,"text_pin_fields");
   string text=(string)source[textKey];Need(text.Length<=2*1024*1024&&BytesHash(text)==(string)source[hashKey],"text_pin");
   return text;
  }
  public static BMeasuredAcquisitionLocalSource Read(JObject envelope,string trustedHash)=>new BMeasuredAcquisitionLocalSource(envelope,trustedHash);
  BMeasuredAcquisitionLocalSource(JObject envelope,string trustedHash){
   Need(envelope!=null&&BasicCombatSimulation.StableJson(envelope).Length<=4*1024*1024
    &&BDirectAttackInput.Digest(envelope)==trustedHash,"independent_pin");
   var p=(JObject)envelope.DeepClone();
   BDirectAttackInput.Fields(p,"version","classification","fixtureIssuer","independentArrivalVerified","environment",
    "boardingSource","boardingTrust","attack","ownedProfile","definitionIntake","buffDefinitionsJson","buffDefinitionsSha256",
    "composition","loop","nativeBindingHash","noncombatRoute","combatRoute","initialSelection","selectResponse",
    "railConfiguration","railHash","railSource","geometryDefinitionJson","geometryDefinitionSha256","geometryMeasurementsJson","geometryMeasurementsSha256");
   Need((string)p["version"]==Version&&(string)p["classification"]=="EXPLICIT_LOCAL_TEST_NOT_ADMITTED_RUN"
    &&p["fixtureIssuer"].Type==JTokenType.Boolean&&(bool)p["fixtureIssuer"]
    &&p["independentArrivalVerified"].Type==JTokenType.Boolean&&!(bool)p["independentArrivalVerified"]
    &&(string)p["environment"]==Environment,"classification");
   SourceHash=trustedHash;
   var boarding=Obj(p["boardingSource"]);
   Boarding=BBoardingEventsNodeSnapshot.Read(boarding,(string)boarding["sourceHash"],Obj(p["boardingTrust"]));
   var origin=Obj(Boarding.OriginalSource.Capture()["origin"]);
   string account=BDirectAttackInput.Text(origin,"accountId"),run=BDirectAttackInput.Text(origin,"runId");
   var noncombat=Obj(p["noncombatRoute"]);var combat=Obj(p["combatRoute"]);
   NoncombatRoute=BRouteBinding.Read(noncombat.ToString(),(string)noncombat["packetHash"],account,run);
   CombatRoute=BRouteBinding.Read(combat.ToString(),(string)combat["packetHash"],account,run);
   NoncombatRoute.RequireSameDeparture(CombatRoute);
   Need((string)noncombat["runStatus"]=="ACTIVE"&&(string)noncombat["phase"]=="ACTIVE"&&NoncombatRoute.NativeNode==null
    &&(string)noncombat["currentNode"]?["eventKind"]=="AUGMENT"
    &&(string)combat["runStatus"]=="ACTIVE"&&(string)combat["phase"]=="ACTIVE"
    &&CombatRoute.NativeNode!=null&&JToken.DeepEquals(CombatRoute.NativeNode.Capture(),Boarding.OriginalSource.Capture())
    &&CombatRoute.MapRevision>NoncombatRoute.MapRevision,"route_scope");
   var attack=Obj(p["attack"]);var f=Obj(attack["formation"]);
   var formation=new BCombatFormation((string)f["trainId"],(BCombatMode)Enum.Parse(typeof(BCombatMode),(string)f["mode"]),
    (BCombatBattlefield)Enum.Parse(typeof(BCombatBattlefield),(string)f["battlefield"]),
    ((JArray)f["slots"]).Select(s=>new BCombatSlot((string)s["carriageId"],(string)s["operatorId"])),(string)f["internalNodeId"]);
   Need(formation.Slots.Count==1&&formation.Slots[0].OperatorId=="op_kyle","bounded_fixture_actor");
   var profile=Obj(p["ownedProfile"]);
   var input=BDirectAttackInput.ReadGunnerInterceptLocalRuntime(attack,BDirectAttackInput.Digest(attack),formation,
    profile,BDirectAttackInput.Digest(profile),CombatDefinitionIntake.Read(Obj(p["definitionIntake"])),Boarding.OriginalSource);
   Need(!input.OperationallyExecutable&&input.OriginalAccountId==account,"local_input");
   string buffJson=PinnedText(p,"buffDefinitionsJson","buffDefinitionsSha256");
   var buffs=new BCarriageBuffDefinitions(buffJson,(string)p["buffDefinitionsSha256"]);
   var composition=Obj(p["composition"]);
   var bound=new BDirectAttackBuffBinding(input,buffs,composition,BDirectAttackInput.Digest(composition));
   var loop=Obj(p["loop"]);Binding=new BDirectAttackBasicLoopBinding(bound,loop,BDirectAttackInput.Digest(loop));
   Need(Binding.OriginalHash==(string)p["nativeBindingHash"]&&(string)composition["initial"]["contextId"]==Boarding.OriginalSource.NativeContextId,"native_binding");
   string geometryJson=PinnedText(p,"geometryDefinitionJson","geometryDefinitionSha256");
   string measurementsJson=PinnedText(p,"geometryMeasurementsJson","geometryMeasurementsSha256");
   var geometry=JObject.Parse(geometryJson);var measurements=JObject.Parse(measurementsJson);
   Geometry=BMeasuredPhysicalGeometry.ReadAuthored(geometry,BDirectAttackInput.Digest(geometry),measurements,BDirectAttackInput.Digest(measurements));
   var rail=Obj(p["railConfiguration"]);
   Need((string)p["railSource"]=="EXISTING_EXPLICIT_TEST_CONFIGURATION_NOT_OPERATING_RAIL"
    &&rail["offers"] is JArray offers&&offers.Count==0,"rail_classification");
   new CombatRailJourney(rail);
   Motion=BTravelRuntimeConfiguration.LocalValidation(rail,(string)p["railHash"]);
   InitialSelection=AugmentSelectionSnapshot.Parse(Obj(p["initialSelection"]).ToString());
   Need(!InitialSelection.Confirmed&&InitialSelection.RunId==run
    &&InitialSelection.StageId==(string)noncombat["currentNode"]["nodeId"],"selection_scope");
   var response=Obj(p["selectResponse"]);BDirectAttackInput.Fields(response,"version","committed","result");
   Need((string)response["version"]=="combat.augment-select.v1.20261019"
    &&response["committed"].Type==JTokenType.Boolean&&(bool)response["committed"],"selection_response");
   var result=Obj(response["result"]);BDirectAttackInput.Fields(result,"outcome","snapshot","approval","applicationOriginal");
   Need((string)result["outcome"]=="SELECTED"&&result["approval"].Type==JTokenType.Null,"selection_result");
   Original=CombatO22ApplicationOriginal.Read(Obj(result["applicationOriginal"]).ToString(),account,run,InitialSelection.EventId,Environment,(string)noncombat["startSnapshotHash"]);
   var confirmed=AugmentSelectionSnapshot.Parse(Original.SelectionJson);InitialSelection.RequireBinding(confirmed);
   Need(confirmed.Confirmed&&confirmed.SelectedCandidateId=="O22"&&confirmed.GrantRevision==InitialSelection.GrantRevision+1
    &&JToken.DeepEquals(JObject.Parse(Original.SelectionJson),result["snapshot"])
    &&InitialSelection.Slots.Count==confirmed.Slots.Count,"confirmed_selection");
   for(int i=0;i<InitialSelection.Slots.Count;i++){
    var before=InitialSelection.Slots[i];var after=confirmed.Slots[i];
    Need(before.CandidateId==after.CandidateId&&before.FreeRemaining==after.FreeRemaining&&before.ExtraRemaining==after.ExtraRemaining,"selected_slots");
   }
   SelectResponseJson=BasicCombatSimulation.StableJson(response);
  }
 }
}
