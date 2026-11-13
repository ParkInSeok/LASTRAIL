using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.Battle {
 // Current committed display rows only. Camera projection and actual battle decisions keep their original owners.
 public static class ReinaBarrageFeedbackPresentation {
  const int PulseCount=4;
  public static JObject Project(JArray committedCastRows){
   if(committedCastRows==null)throw new ArgumentNullException(nameof(committedCastRows));
   var casts=new JArray();var targets=new JArray();var commands=new HashSet<string>(StringComparer.Ordinal);
   var markers=new Dictionary<(string,long),JObject>();
   for(int index=0;index<committedCastRows.Count;index++){
    var source=committedCastRows[index] as JObject;Need(source!=null);
    string command=Id(source["commandId"]);Need(commands.Add(command));
    int completed=(int)Integer(source["nextPulse"],0,PulseCount);int remaining=PulseCount-completed;
    Need(source["frame"] is JObject&&source["centerMm"] is JArray center&&center.Count==3);
    foreach(var coordinate in (JArray)source["centerMm"])Integer(coordinate,long.MinValue,long.MaxValue);
    Integer(source["radiusMm"],1,long.MaxValue);Need(source["targets"] is JArray);
    bool primary=index==committedCastRows.Count-1;
    var cast=(JObject)source.DeepClone();cast["isPrimary"]=primary;cast["simplified"]=!primary;cast["remainingPulses"]=remaining;casts.Add(cast);
    var selected=new HashSet<(string,long)>();
    foreach(var token in (JArray)source["targets"]){
     var target=token as JObject;Need(target!=null);string id=Id(target["id"]);long generation=Integer(target["generation"],1,long.MaxValue);
     var identity=(id,generation);Need(selected.Add(identity));Need(target["eligible"]?.Type==JTokenType.Boolean);
     Need(target["protected"]!=null&&(target["protected"].Type==JTokenType.Boolean||target["protected"].Type==JTokenType.Null));
     if(!markers.TryGetValue(identity,out var marker)){
      marker=new JObject{["id"]=id,["generation"]=generation,["casts"]=new JArray()};markers.Add(identity,marker);targets.Add(marker);
     }
     ((JArray)marker["casts"]).Add(new JObject{["commandId"]=command,["isPrimary"]=primary,["remainingPulses"]=remaining,
      ["eligible"]=target["eligible"].DeepClone(),["protected"]=target["protected"].DeepClone()});
    }
   }
   return new JObject{["casts"]=casts,["targets"]=targets};
  }
  static string Id(JToken token){Need(token?.Type==JTokenType.String&&!string.IsNullOrWhiteSpace((string)token));return (string)token;}
  static long Integer(JToken token,long minimum,long maximum){Need(token?.Type==JTokenType.Integer);long value=(long)token;Need(value>=minimum&&value<=maximum);return value;}
  static void Need(bool valid){if(!valid)throw new ArgumentException("Explicit committed barrage display source required.");}
 }
}
