using System;
using System.Linq;
using System.Numerics;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 // Validates the accepted journal, never reads current rank or haste. Native acceptance is separate.
 public sealed class BInterceptTimingSnapshot {
  readonly JObject packet;
  public BBoardingEventsNodeSnapshot OriginalSource {get;}
  public string Hash=>(string)packet["sourceHash"];public string EventId=>(string)packet["timing"]["eventId"];public string OperatorId=>(string)packet["timing"]["operatorId"];
  public long StepMicroseconds=>(long)packet["timing"]["stepUs"];public long PreparationTicks=>long.Parse((string)packet["timing"]["phases"]["preparation"]["ticks"]);public long RecoveryTicks=>long.Parse((string)packet["timing"]["phases"]["recovery"]["ticks"]);
  BInterceptTimingSnapshot(JObject p,BBoardingEventsNodeSnapshot old){packet=p;OriginalSource=old;}
  public JObject Capture()=>(JObject)packet.DeepClone();
  internal JObject AcceptedAction=>((JArray)packet["timing"]["buffSnapshot"]["actions"]).Single(x=>(string)x["input"]["eventId"]==EventId) as JObject;
  internal JObject BuffSnapshot=>(JObject)packet["timing"]["buffSnapshot"].DeepClone();
  static JObject Phase(CombatExactValue exact,long step){var denominator=exact.Denominator*step;var ticks=BigInteger.Max(1,(exact.Numerator+denominator-1)/denominator);Need(ticks<=long.MaxValue);return new JObject{["exactUs"]=exact.Snapshot(),["ticks"]=ticks.ToString(),["durationUs"]=(ticks*step).ToString()};}
  public static BInterceptTimingSnapshot Read(JObject input,string independentHash,JObject originalTrust,BCarriageBuffDefinitions buffs){
   BBoardingEventsDefinitionSnapshot.Bounded(input);var p=(JObject)input.DeepClone();Fields(p,"version","originalSource","originalSourceHash","definition","definitionHash","timing","definitionAvailable","timingDefinitionAvailable","runtimeSupported","canStart","sourceHash");Need((string)p["version"]==BInterceptDefinitionSnapshot.Version&&Hash(new JValue(independentHash))==(string)p["sourceHash"]&&(string)p["definitionHash"]==BInterceptDefinitionSnapshot.ApprovedDefinitionHash);SelfHash(p,"sourceHash");BInterceptDefinitionSnapshot.Validate(Obj(p["definition"]));BBoardingEventsDefinitionSnapshot.Flags(p);Need(p["timingDefinitionAvailable"]?.Type==JTokenType.Boolean&&(bool)p["timingDefinitionAvailable"]);
   var old=BBoardingEventsNodeSnapshot.Read(Obj(p["originalSource"]),Hash(p["originalSourceHash"]),originalTrust);var origin=old.OriginalSource.Capture();var t=Obj(p["timing"]);Fields(t,"accountId","runId","nativeContextId","operatorId","abilityId","abilityHash","eventId","stepUs","buffSnapshot","originalHasteSources","totalHasteBps","phases","timingHash");SelfHash(t,"timingHash");Need(Id(t["accountId"])==(string)origin["origin"]["accountId"]&&Id(t["runId"])==old.OriginalSource.RunId&&Id(t["nativeContextId"])==old.OriginalSource.NativeContextId);
   string actor=Id(t["operatorId"]),eventId=Id(t["eventId"]);var ability=((JArray)p["definition"]["abilities"]).SingleOrDefault(a=>(string)a["operatorId"]==actor&&(string)a["abilityId"]==Id(t["abilityId"]));Need(ability!=null&&(string)ability["abilityHash"]==Hash(t["abilityHash"]));long step=Nat(t["stepUs"],1);
   var model=BCarriageBuffModel.Restore(Obj(t["buffSnapshot"]),buffs);var snapshot=model.Snapshot();Need((string)snapshot["departure"]["accountId"]==(string)t["accountId"]&&(string)snapshot["initial"]["contextId"]==old.OriginalSource.NativeContextId);var action=((JArray)snapshot["actions"]).SingleOrDefault(a=>(string)a["input"]["eventId"]==eventId);Need(action!=null);var command=action["input"];var car=((JArray)snapshot["departure"]["cars"]).SingleOrDefault(c=>JToken.DeepEquals(c["slot"],command["slot"]));Need(car!=null&&(string)car["occupant"]["kind"]=="OPERATOR"&&(string)car["occupant"]["operatorId"]==actor&&(string)command["kind"]=="INTERCEPT"&&(string)command["ability"]=="INTERCEPT"&&(string)command["timing"]=="INTERCEPTION_PHASE"&&command["originalEligible"]?.Type==JTokenType.Boolean&&(bool)command["originalEligible"]);
   var ids=new System.Collections.Generic.HashSet<string>{(string)action["source"]["sourceId"]};BigInteger original=0;foreach(var token in Rows(t["originalHasteSources"],0,32)){var term=Obj(token);Fields(term,"sourceId","group","valueBps","appliedVersion");Need(ids.Add(Id(term["sourceId"]))&&(string)term["group"]=="INTERCEPTION_HASTE"&&term["appliedVersion"].Type==JTokenType.Null);original+=Nat(term["valueBps"]);}
   var total=original+(long)action["source"]["interceptionHasteBps"];Need(original<=9007199254740991&&total<=9007199254740991&&Nat(t["totalHasteBps"])==total);var calc=new BCarriageBuffCalculator(model);var prep=Phase(calc.Time(eventId,CombatExactValue.Create((long)ability["preparationMs"]*1000,1),(long)original),step);var recovery=Phase(calc.Time(eventId,CombatExactValue.Create((long)ability["recoveryMs"]*1000,1),(long)original),step);
   var expected=new JObject{["preparation"]=prep,["recovery"]=recovery,["occupancyUs"]=(BigInteger.Parse((string)prep["durationUs"])+BigInteger.Parse((string)recovery["durationUs"])).ToString(),["occupancyTicks"]=(BigInteger.Parse((string)prep["ticks"])+BigInteger.Parse((string)recovery["ticks"])).ToString()};Need(JToken.DeepEquals(expected,t["phases"]));return new BInterceptTimingSnapshot(p,old);
  }
 }
}
