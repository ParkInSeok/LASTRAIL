using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
public sealed class CombatAbilityAudioSet {
 public static readonly string[] InitialAbilityIds={"op_kyle.active","op_kyle.ultimate","op_reina.active","op_reina.ultimate","op_eve.active","op_eve.ultimate","op_sera.active","op_sera.ultimate","op_mira.active","op_mira.ultimate","support_fire","barrier","emergency_repair"};
 readonly Dictionary<string,AudioClip> cast;
 readonly Dictionary<string,float> levels=new Dictionary<string,float>(StringComparer.Ordinal);
 public CombatAbilityAudioSet(IDictionary<string,AudioClip> clips){
  if(clips==null||clips.Count!=InitialAbilityIds.Length||InitialAbilityIds.Any(id=>!clips.ContainsKey(id)||clips[id]==null))throw new ArgumentException("Thirteen explicit ability cast clips required");
  if(clips.Values.Select(c=>c.GetInstanceID()).Distinct().Count()!=clips.Count)throw new ArgumentException("Shared generic clip is not per-ability audio");
  cast=new Dictionary<string,AudioClip>(clips,StringComparer.Ordinal);
  foreach(var id in InitialAbilityIds)levels[id]=1;
 }
 public float CastGain(string ability)=>ability!=null&&levels.TryGetValue(ability,out var gain)?gain:0;
 public AudioClip FindCastClip(string ability)=>ability!=null&&cast.TryGetValue(ability,out var clip)?clip:null;
 public static CombatAbilityAudioSet LoadResources(){
  var map=new Dictionary<string,AudioClip>();foreach(var id in InitialAbilityIds)map.Add(id,LastRail.Resources.PreparedContent.Load<AudioClip>("CombatPresentation/Audio/"+id));var result=new CombatAbilityAudioSet(map);
  var source=LastRail.Resources.PreparedContent.Load<TextAsset>("CombatPresentation/Audio/levels");if(source==null)throw new InvalidOperationException("Per-ability mix levels missing");
  var values=JObject.Parse(source.text);if(values.Count!=InitialAbilityIds.Length)throw new ArgumentException("Exact per-ability levels required");
  foreach(var id in InitialAbilityIds){var token=values[id];if(token==null||(token.Type!=JTokenType.Float&&token.Type!=JTokenType.Integer))throw new ArgumentException("Missing mix level");float gain=(float)token;if(float.IsNaN(gain)||float.IsInfinity(gain)||gain<0||gain>2)throw new ArgumentException("Mix level out of bounds");result.levels[id]=gain;}return result;
 }
}
}
