using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Battle {
// References approved originals; selection stays downstream of the typed result binding.
public sealed class ApprovedResultAudio : ScriptableObject {
 public AudioClip Heal;
 public AudioClip Buff;
 public AudioClip Debuff;
 public AudioClip ShieldContact;
 public AudioClip BasicContact;
 public AudioClip Warning;
 [Range(0,1)] public float WarningGain=.85f;
 [Range(0,1)] public float NativeBasicContactGain=.85f;
 [Range(0,1)] public float NativeShieldContactGain=.85f;

 public AudioClip Select(JObject row) {
  switch(CombatResolvedVisual.Shape(row)) {
   case "Heal":return Heal;
   case "TrainBuff":return Buff;
   case "Mark":case "Taunt":return Debuff;
   case "ShieldAbsorb":return ShieldContact;
   default:return null;
  }
 }
}
}
