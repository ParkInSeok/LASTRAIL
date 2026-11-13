using System;
using System.Collections.Generic;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // Immutable entry values. Relative ticks start at node battle entry, never at receipt/loading time.
 public sealed class OrdinaryNodeActions {
  public const string Version="ordinary.node-actions.v1.20261030";
  public long StepTimeSpanTicks {get;}
  public bool UltimateAuto {get;}
  public IReadOnlyList<Actor> Actors {get;}

  public sealed class Actor {
   public string OperatorId {get;}
   public int Slot {get;}
   public long BasicNextOpportunityInTicks {get;}
   public long ActiveCooldownRemainingTicks {get;}
   public int UltimateCharge {get;}
   public long ConditionalDefenseFlat {get;}
   public CombatExactValue SkillMagnitude {get;}
   internal Actor(OrdinaryFiveStartInput.Actor original,JObject action,JObject stats) {
    OperatorId=original.OperatorId;Slot=original.Slot;
    BasicNextOpportunityInTicks=Nat(action,"basicNextOpportunityInTicks");
    ActiveCooldownRemainingTicks=Nat(action,"activeCooldownRemainingTicks");
    UltimateCharge=(int)Nat(action,"ultimateCharge",0,int.MaxValue);
    ConditionalDefenseFlat=Nat(stats,"conditionalDefenseFlat");
    SkillMagnitude=CombatExactValue.From(stats["skillMagnitude"] as JObject);
   }
  }

  OrdinaryNodeActions(JObject actions,OrdinaryFiveStartInput original) {
   Need(original!=null);
   BDirectAttackInput.Fields(actions,"version","stepTimeSpanTicks","ultimateAuto","actors","stats");
   Need(BDirectAttackInput.Text(actions,"version",200)==Version);
   StepTimeSpanTicks=Nat(actions,"stepTimeSpanTicks",1);
   Need(StepTimeSpanTicks%TimeSpan.TicksPerMillisecond==0&&actions["ultimateAuto"]?.Type==JTokenType.Boolean);
   UltimateAuto=(bool)actions["ultimateAuto"];
   var rows=Rows(actions["actors"],"operatorId","basicNextOpportunityInTicks","activeCooldownRemainingTicks","ultimateCharge");
   var stats=Rows(actions["stats"],"operatorId","conditionalDefenseFlat","skillMagnitude");
   var actors=new Actor[5];
   foreach(var source in original.Actors) {
    Need(rows.ContainsKey(source.OperatorId)&&stats.ContainsKey(source.OperatorId));
    actors[source.Slot]=new Actor(source,rows[source.OperatorId],stats[source.OperatorId]);
   }
   Actors=Array.AsReadOnly(actors);
  }
  public static OrdinaryNodeActions Read(JObject actions,OrdinaryFiveStartInput original)=>new OrdinaryNodeActions(actions,original);
  static Dictionary<string,JObject> Rows(JToken token,params string[] fields) {
   Need(token is JArray array&&array.Count==5);
   var rows=new Dictionary<string,JObject>(StringComparer.Ordinal);
   foreach(var item in (JArray)token) {
    var row=item as JObject;BDirectAttackInput.Fields(row,fields);
    var id=BDirectAttackInput.Text(row,"operatorId",200);Need(!rows.ContainsKey(id));rows.Add(id,row);
   }
   return rows;
  }
  static long Nat(JToken value,string field,long min=0,long max=9007199254740991) {
   try{return BDirectAttackInput.Nat(value,field,min,max);}
   catch(OverflowException e){throw new ArgumentException("ordinary_node_actions_source_conflict",e);}
  }
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_node_actions_source_conflict");}
 }
}
