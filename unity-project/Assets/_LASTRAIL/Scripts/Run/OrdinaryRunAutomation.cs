using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Immutable receipt projection. The original initialization/node/completion response owns persistence.
 public sealed class OrdinaryRunAutomation {
  public const string Version="ordinary.run-actions.v1.20261031";
  public long Revision{get;} public bool UltimateAuto{get;}
  public IReadOnlyList<Actor> Actors{get;}
  public sealed class Actor {
   public string OperatorId{get;} public long ActiveCooldownRemainingTimeSpanTicks{get;} public int UltimateCharge{get;}
   internal Actor(JObject value){BDirectAttackInput.Fields(value,"operatorId","activeCooldownRemainingTicks","ultimateCharge");OperatorId=BDirectAttackInput.Text(value,"operatorId",200);Need(OperatorId.Length>0);ActiveCooldownRemainingTimeSpanTicks=Number(value,"activeCooldownRemainingTicks");UltimateCharge=(int)Number(value,"ultimateCharge",0,200);}
  }
  OrdinaryRunAutomation(JObject value){
   BDirectAttackInput.Fields(value,"version","revision","ultimateAuto","actors");Need((string)value["version"]==Version&&value["ultimateAuto"]?.Type==JTokenType.Boolean);
   Revision=Number(value,"revision",1);UltimateAuto=(bool)value["ultimateAuto"];Need(value["actors"] is JArray rows&&rows.Count==5);
   var actors=((JArray)value["actors"]).Select(a=>new Actor(a as JObject)).ToArray();Need(actors.Select(a=>a.OperatorId).Distinct(StringComparer.Ordinal).Count()==5);Actors=Array.AsReadOnly(actors);
  }
  internal static OrdinaryRunAutomation Read(JToken value)=>new OrdinaryRunAutomation(value as JObject);
  internal void RequireActors(IEnumerable<string> ids){var expected=ids?.ToArray();Need(expected!=null&&expected.Length==5&&expected.Distinct(StringComparer.Ordinal).Count()==5&&Actors.All(a=>expected.Contains(a.OperatorId,StringComparer.Ordinal)));}
  public void RequireOriginal(OrdinaryFiveStartInput start){Need(start!=null);RequireActors(start.Actors.Select(a=>a.OperatorId));}
  internal void RequireNodeSettings(JObject actions){
   Need(actions!=null&&actions["ultimateAuto"]?.Type==JTokenType.Boolean&&(bool)actions["ultimateAuto"]==UltimateAuto&&actions["actors"] is JArray rows&&rows.Count==5);
   long step=Number(actions,"stepTimeSpanTicks",1);Need(step%TimeSpan.TicksPerMillisecond==0);
   var actors=(JArray)actions["actors"];RequireActors(actors.Select(a=>BDirectAttackInput.Text(a,"operatorId",200)));
   foreach(var actor in Actors){
    var action=actors.Single(a=>(string)a["operatorId"]==actor.OperatorId);Need(Number(action,"ultimateCharge",0,200)==actor.UltimateCharge);
    long duration=actor.ActiveCooldownRemainingTimeSpanTicks,steps=duration/step+(duration%step==0?0:1);
    Need(Number(action,"activeCooldownRemainingTicks")==steps);
   }
  }
  static long Number(JToken value,string field,long min=0,long max=9007199254740991){try{return BDirectAttackInput.Nat(value,field,min,max);}catch(OverflowException error){throw new ArgumentException("ordinary_run_automation_invalid",error);}}
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_run_automation_invalid");}
 }
}
