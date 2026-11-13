using System;
using System.Collections.Generic;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatAbilityAudioTests {
 [Test] public void SharedClipCannotMasqueradeAsThirteenSkillCues(){
  var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatAbilityAudioSet");Assert.NotNull(type);
  var ids=new[]{"op_kyle.active","op_kyle.ultimate","op_reina.active","op_reina.ultimate","op_eve.active","op_eve.ultimate","op_sera.active","op_sera.ultimate","op_mira.active","op_mira.ultimate","support_fire","barrier","emergency_repair"};
  var shared=AudioClip.Create("shared",100,1,48000,false);var clips=new List<AudioClip>{shared};
  try{var map=new Dictionary<string,AudioClip>();foreach(var id in ids)map.Add(id,shared);
   Assert.Throws<System.Reflection.TargetInvocationException>(()=>Activator.CreateInstance(type,new object[]{map}));
   foreach(var id in ids){var clip=AudioClip.Create(id,100,1,48000,false);clips.Add(clip);map[id]=clip;}
   var valid=Activator.CreateInstance(type,new object[]{map});var method=type.GetMethod("FindCastClip");
   foreach(var id in ids)Assert.AreSame(map[id],method.Invoke(valid,new object[]{id}));
   Assert.IsNull(method.Invoke(valid,new object[]{"unknown.ability"}),"No common-sound fallback");
  }finally{foreach(var clip in clips)UnityEngine.Object.DestroyImmediate(clip);}
 }
}
