using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public static class CombatFormationLayout {
 public static Dictionary<string,Vector2> Positions(JObject hud){
  var front=((JArray)hud["frontSlots"]).Select(x=>(int)x).ToArray();
  var rows=((JArray)hud["actors"]).Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]).OrderBy(a=>(int)a["slot"]).ToArray();
  var result=new Dictionary<string,Vector2>(StringComparer.Ordinal);
  foreach(bool isFront in new[]{false,true}){
   bool ordinary=(string)hud["schema"]==OrdinaryFiveCombatFactory.Version;
   var points=ordinary
    ?isFront?new[]{new Vector2(228,480),new Vector2(228,554),new Vector2(228,628)}:new[]{new Vector2(8,510),new Vector2(8,620)}
    :isFront?new[]{new Vector2(242,498),new Vector2(228,622)}:new[]{new Vector2(8,485),new Vector2(94,554),new Vector2(8,629)};
   var group=rows.Where(a=>front.Contains((int)a["slot"])==isFront).ToArray();
   if(group.Length>points.Length)throw new ArgumentException("Unsupported formation layout");
   for(int i=0;i<group.Length;i++)result.Add((string)group[i]["id"],points[i]);
  }
  return result;
 }
}
}
