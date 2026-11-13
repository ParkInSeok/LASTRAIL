using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public sealed class CombatEffectAudio {
 sealed class Cue {public AudioClip Clip;public float Gain;public string[] Kinds;}
 readonly Dictionary<string,Cue> cues=new Dictionary<string,Cue>(StringComparer.Ordinal);
 ApprovedResultAudio approvedResults;
 public AudioClip WarningClip=>approvedResults.Warning;
 public float WarningGain=>approvedResults.WarningGain;
 static string Key(string ability,int index)=>new JArray(ability,index).ToString(Newtonsoft.Json.Formatting.None);
 public bool Find(JObject row,out AudioClip clip,out float gain){
  clip=null;gain=0;
  if((bool?)row["reinaBarrage"]==true||!CombatEffectFeedback.Positive(row))return false;
  string sourceKind=(string)row["sourceKind"];
  if((sourceKind=="BASIC"||sourceKind=="ENEMY_BASIC")&&(string)row["kind"]=="DAMAGE"&&(int?)row["effectIndex"]==0&&
     row["nodeId"]?.Type==JTokenType.String&&(long?)row["attemptGeneration"]>0&&row["healthReceipt"] is JObject receipt&&
     (string)receipt["targetId"]==(string)row["targetId"]&&(int?)receipt["absorbed"]>=0&&(int?)receipt["absorbed"]==(int?)row["actualResult"]?["absorbed"]){
   if((int)receipt["absorbed"]>0){clip=approvedResults.ShieldContact;gain=approvedResults.NativeShieldContactGain;return true;}
   string source=(string)row["sourceId"];
   if(sourceKind=="BASIC"&&(source=="op_kyle"||source=="op_reina"||source=="op_eve"||source=="op_sera"||source=="op_mira")&&
      (string)row["abilityId"]==source+".basic"&&(int?)receipt["hpLoss"]>0&&(int?)receipt["hpLoss"]==(int?)row["actualResult"]?["hpLoss"]){
    clip=approvedResults.BasicContact;gain=approvedResults.NativeBasicContactGain;return true;
   }
  }
  if(!cues.TryGetValue(Key((string)row["abilityId"],(int)row["effectIndex"]),out var cue))return false;
  // Current node Sera support emits HEAL; the preserved legacy mapping used DAMAGE.
  bool nativeHeal=(string)row["abilityId"]=="op_sera.active"&&(int)row["effectIndex"]==0&&(string)row["kind"]=="HEAL"&&(string)row["sourceKind"]=="SKILL"&&row["nodeId"]?.Type==JTokenType.String&&row["attemptGeneration"]?.Type==JTokenType.Integer;
  if(!nativeHeal&&!cue.Kinds.Contains((string)row["kind"]))return false;
  clip=approvedResults.Select(row)??cue.Clip;gain=cue.Gain;return true;
 }
 public static CombatEffectAudio LoadResources(){
  var source=LastRail.Resources.PreparedContent.Load<TextAsset>("CombatPresentation/EffectAudio/bindings");if(source==null)throw new InvalidOperationException("Typed effect sound bindings missing");
  var rows=JArray.Parse(source.text);if(rows.Count!=18)throw new ArgumentException("Expected thirteen primary and five secondary mappings");var result=new CombatEffectAudio();
  foreach(JObject row in rows){string ability=(string)row["abilityId"];int index=(int)row["effectIndex"];if(!CombatAbilityAudioSet.InitialAbilityIds.Contains(ability)||index<0||index>2)throw new ArgumentException("Unknown effect audio binding");
   var clip=LastRail.Resources.PreparedContent.Load<AudioClip>("CombatPresentation/EffectAudio/"+ability+"-"+index);float gain=(float)row["effectiveGain"];if(clip==null||float.IsNaN(gain)||float.IsInfinity(gain)||gain<0||gain>1)throw new ArgumentException("Invalid typed effect audio resource");result.cues.Add(Key(ability,index),new Cue{Clip=clip,Gain=gain,Kinds=((JArray)row["eventKinds"]).Select(v=>(string)v).ToArray()});
  }result.approvedResults=LastRail.Resources.PreparedContent.Load<ApprovedResultAudio>("CombatPresentation/EffectAudio/ApprovedResults");
  if(result.approvedResults==null||result.approvedResults.Heal==null||result.approvedResults.Buff==null||result.approvedResults.Debuff==null||result.approvedResults.ShieldContact==null||result.approvedResults.BasicContact==null||result.approvedResults.Warning==null)throw new InvalidOperationException("Approved result sound references missing");
  foreach(float contactGain in new[]{result.approvedResults.NativeShieldContactGain,result.approvedResults.NativeBasicContactGain,result.approvedResults.WarningGain})if(float.IsNaN(contactGain)||float.IsInfinity(contactGain)||contactGain<0||contactGain>1)throw new InvalidOperationException("Invalid native contact gain");
  return result;
 }
}
}
