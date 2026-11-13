#if UNITY_EDITOR
using System;
using NUnit.Framework;
using LastRail.Presentation.Audio;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public class MeasuredCombatAudioTests {
  [Test] public void ExplicitLocalAudioConsumerExistsWithSnapshotAndCommittedPaths(){
   var t=typeof(LastRailAudioCuePlayer).Assembly.GetType("LastRail.Presentation.Audio.BMeasuredCombatAudio");
   Assert.That(t,Is.Not.Null,"Actual measured Player audio consumer missing");
   Assert.That(t.GetMethod("Observe"),Is.Not.Null);Assert.That(t.GetMethod("Finish"),Is.Not.Null);
  }
  [Test] public void BackgroundDropsOneShotInsteadOfResumingPastEvent(){
   var root=new GameObject("background-audio");var clip=AudioClip.Create("fixture",1000,1,1000,false);
   try{
    var owner=root.AddComponent<LastRailAudioCuePlayer>();var source=root.AddComponent<AudioSource>();
    owner.ConfigureBindings(null,null,new[]{new LastRailAudioClipBinding("fixture",clip,false,1)});owner.ConfigureCombatSources(new[]{source},null);owner.BeginCombatScope(root,1);
    Assert.That(owner.TryPlayCombatCue(1,"a","HIT","1",1,"hit","fixture",3,0),Is.EqualTo("played"));
    typeof(LastRailAudioCuePlayer).GetMethod("OnApplicationPause",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(owner,new object[]{true});
    Assert.That(source.clip,Is.Null,"Past one-shot must not survive app background for replay");
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);}
  }
  [Test] public void CombatPauseHasSeparateOwnedLoopHold(){
   Assert.That(typeof(LastRailAudioCuePlayer).GetMethod("AcquireCombatLoopHold"),Is.Not.Null,"Combat loop pause must not pause music/UI/short tails");
  }
  [Test] public void SnapshotAndDuplicateObservationNeverReplayOriginalHit(){
   var root=new GameObject("measured-audio");var clip=AudioClip.Create("combat_hit",1000,1,1000,false);
   var manifest=new TextAsset("{}");string folder="D:/Project/LASTRAIL/LocalTemp/AudioTrialTests/"+Guid.NewGuid().ToString("N");System.IO.Directory.CreateDirectory(folder);
   try{
    var owner=root.AddComponent<LastRailAudioCuePlayer>();
    AudioSource Voice(string name){var g=new GameObject(name);g.transform.SetParent(root.transform);g.AddComponent<LocalAudioOutputCapture>();return g.AddComponent<AudioSource>();}
    var music=Voice("bgm");var hit=Voice("hit");
    owner.ConfigureBindings(null,music,new[]{new LastRailAudioClipBinding(BMeasuredCombatAudio.HitCue,clip,false,1)});
    owner.ConfigureCombatSources(new[]{hit},null);
    var consumer=root.AddComponent<BMeasuredCombatAudio>();consumer.Initialize(owner,new[]{hit},music,root.AddComponent<LocalAudioOutputCapture>(),manifest);consumer.Begin(false);
    var native=JObject.Parse("{results:[{hits:[{eventId:'old',tick:1,hp:{hpLoss:5,absorbed:0}}]}]}");consumer.Observe(native,true);
    native["results"].Last["hits"][0]["eventId"]="new";
    ((JArray)native["results"]).Add(JObject.Parse("{hits:[{eventId:'committed',tick:2,hp:{hpLoss:5,absorbed:0}}]}"));
    consumer.Observe(native,false);consumer.Observe(native,false);consumer.Observe(native,true);consumer.Observe(native,false);
    var result=consumer.Finish(folder);Assert.That(((JArray)result["events"]).Count,Is.EqualTo(1));Assert.That((string)result["events"][0]["eventId"],Is.EqualTo("committed"));
    Assert.That((bool)result["allSourcesStopped"],Is.True);
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);UnityEngine.Object.DestroyImmediate(manifest);}
  }
  [TestCase(2,false)][TestCase(3,true)] public void RequiredRecoveryTrialCannotPassWhenContactOccupiesItsVoice(int capacity,bool expectedPass){
   var root=new GameObject("three-original-effects");var hitClip=AudioClip.Create("contact",1000,1,1000,false);var healClip=AudioClip.Create("heal",1000,1,1000,false);var manifest=new TextAsset("{}");
   string folder="D:/Project/LASTRAIL/LocalTemp/AudioTrialTests/"+Guid.NewGuid().ToString("N");System.IO.Directory.CreateDirectory(folder);
   try{
    var owner=root.AddComponent<LastRailAudioCuePlayer>();
    AudioSource Source(string name){var g=new GameObject(name);g.transform.SetParent(root.transform);g.AddComponent<LocalAudioOutputCapture>();return g.AddComponent<AudioSource>();}
    var voices=new AudioSource[capacity];for(int i=0;i<capacity;i++)voices[i]=Source("voice"+i);var music=Source("music");
    owner.ConfigureBindings(null,music,new[]{new LastRailAudioClipBinding(BMeasuredCombatAudio.HitCue,hitClip,false,1),new LastRailAudioClipBinding(BMeasuredCombatAudio.HealCue,healClip,false,1)});owner.ConfigureCombatSources(voices,null);
    var consumer=root.AddComponent<BMeasuredCombatAudio>();consumer.Initialize(owner,voices,music,root.AddComponent<LocalAudioOutputCapture>(),manifest);consumer.Begin(false);
    consumer.Observe(JObject.Parse("{results:[]}"),true);
    consumer.Observe(JObject.Parse("{results:[{hits:[{eventId:'enemy',tick:1,hp:{hpLoss:19,absorbed:0}}]},{hits:[{eventId:'own',tick:2,hp:{hpLoss:61,absorbed:0},lifesteal:{actualHpGain:7}}]}]}"),false);
    if(expectedPass)Assert.DoesNotThrow(()=>consumer.Finish(folder,true));else Assert.Throws<InvalidOperationException>(()=>consumer.Finish(folder,true));
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(hitClip);UnityEngine.Object.DestroyImmediate(healClip);UnityEngine.Object.DestroyImmediate(manifest);}
  }
 }
}
#endif
