using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Read-only classification of already committed results. Never infers an effect from HP changes.
public static class CombatResolvedVisual {
 public static string Shape(JObject row){
  if(!CombatEffectFeedback.Positive(row))return null;
  var actual=(JObject)row["actualResult"];
  switch((string)row["kind"]){
   case "SHIELD_ISSUE":return "ShieldIssue";
   case "HEAL":return "Heal";
   case "REPAIR":return "Repair";
   case "INTERRUPT":return "Interrupt";
   case "STATUS_ADD":case "STATUS_REFRESH":
    switch((string)actual["category"]){case "mark":return "Mark";case "taunt":return "Taunt";case "train_damage_buff":return "TrainBuff";default:return null;}
   case "DAMAGE":return (int?)actual["absorbed"]>0?"ShieldAbsorb":null;
   default:return null;
  }
 }
 public static float Duration(string shape){switch(shape){case "ShieldAbsorb":return .24f;case "Interrupt":return .32f;case "Repair":return .48f;case "Mark":return .45f;case "Taunt":return .42f;default:return .58f;}}
}
}
