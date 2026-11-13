using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using LastRail.Presentation.Audio;
using LastRail.Presentation.RoofBattle;
namespace LastRail.Tests.EditMode {
 public sealed class CombatSupportPositionAudioTests {
  static Type Need(string name){var t=typeof(CombatSfxPlayback).Assembly.GetType("LastRail.Presentation.Audio."+name);Assert.That(t,Is.Not.Null,name+" missing");return t;}
  [Test] public void NativeResultsSeparateAbsorptionAndActualLifestealWithoutHpDifference(){
   var method=Need("CombatAudioResults").GetMethod("ReadNative");
   var original=JObject.Parse("{hp:{hpLoss:0,absorbed:30,hpAfter:7},lifesteal:{actualHpGain:0,hpBefore:1,hpAfter:999}}");var before=original.ToString();
   var results=(string[])method.Invoke(null,new object[]{original});Assert.That(results,Is.EqualTo(new[]{"ShieldContact"}));Assert.That(original.ToString(),Is.EqualTo(before));
   original["hp"]["hpLoss"]=50;original["lifesteal"]["actualHpGain"]=35;
   Assert.That((string[])method.Invoke(null,new object[]{original}),Is.EqualTo(new[]{"MixedContact","Lifesteal"}));
  }
  [Test] public void SupportSuccessRequiresActualEffect(){var method=Need("CombatAudioResults").GetMethod("Support");Assert.That(method.Invoke(null,new object[]{"Heal",0L}),Is.Null);Assert.That(method.Invoke(null,new object[]{"Cleanse",0L}),Is.Null);Assert.That(method.Invoke(null,new object[]{"DownSupport",1L}),Is.EqualTo("DownSupport"));}
  [Test] public void SpatialPolicyDoesNotProjectUndisclosedOrMissingPoint(){
   int calls=0;var policy=Activator.CreateInstance(Need("CombatAudioSpatialPolicy"),.25f,(Func<Vector3,float>)(p=>{calls++;return p.x;}),(Func<float,float>)(distance=>.5f));
   var evaluate=policy.GetType().GetMethod("Evaluate");
   Assert.That(evaluate.Invoke(policy,new object[]{(Vector3?)new Vector3(1,0,0),false,true,false,(float?)10,3,false}),Is.Null);Assert.That(calls,Is.Zero);
   Assert.That(evaluate.Invoke(policy,new object[]{(Vector3?)null,true,true,false,(float?)null,3,false}),Is.Null);Assert.That(calls,Is.Zero);
   var neutral=(Vector2?)evaluate.Invoke(policy,new object[]{(Vector3?)null,true,false,true,(float?)null,3,false});Assert.That(neutral.Value,Is.EqualTo(new Vector2(0,1)));
  }
  [Test] public void MonoAndWarningKeepCentralIdentityAndNoDistanceLoss(){
   var policy=Activator.CreateInstance(Need("CombatAudioSpatialPolicy"),.25f,(Func<Vector3,float>)(p=>p.x),(Func<float,float>)(distance=>.2f));var method=policy.GetType().GetMethod("Evaluate");
   Assert.That((Vector2?)method.Invoke(policy,new object[]{(Vector3?)new Vector3(4,0,0),true,true,true,(float?)100,1,false}),Is.EqualTo(new Vector2(0,1)));
   Assert.That((Vector2?)method.Invoke(policy,new object[]{(Vector3?)Vector3.zero,true,true,true,(float?)100,3,true}),Is.EqualTo(new Vector2(0,.2f)));
   Assert.That((Vector2?)method.Invoke(policy,new object[]{(Vector3?)new Vector3(4,0,0),true,true,true,(float?)null,3,false}),Is.EqualTo(new Vector2(.25f,1)));
  }
  [TestCase(false)][TestCase(true)] public void OneShotFreezesPointWhileLoopFollowsSameGeneration(bool loop){
   float camera=1;int stops=0;Vector2 applied=default;var source=new CombatAudioPosition("device",4,new Vector3(1,0,0),true,true,null);
   var policy=new CombatAudioSpatialPolicy(.1f,p=>p.x*camera,null);
   // Use points inside signed viewport so the authored clamp is not what freezes the sound.
   source=new CombatAudioPosition("device",4,new Vector3(.25f,0,0),true,true,null);
   var t=Need("CombatAudioPositionPlayback");var playback=Activator.CreateInstance(t,policy,source,loop,true,3,(Func<CombatAudioPosition>)(()=>source),(Func<bool>)(()=>false),(Action<Vector2>)(v=>applied=v),(Action)(()=>stops++));
   t.GetMethod("Refresh").Invoke(playback,null);source=new CombatAudioPosition("device",4,new Vector3(.5f,0,0),true,true,null);camera=2;t.GetMethod("Refresh").Invoke(playback,null);
   Assert.That(applied.x,Is.EqualTo(loop?.1f:.05f).Within(.00001f));source=new CombatAudioPosition("device",5,Vector3.zero,true,true,null);t.GetMethod("Refresh").Invoke(playback,null);((IDisposable)playback).Dispose();Assert.That(stops,Is.EqualTo(1));
  }
  [Test] public void OneOriginalOrderCanCarryDistinctConfirmedEffectsButNotReplay(){
   int count=0;using var owner=new CombatSfxPlayback(4,(cue,end)=>{count++;return new TestLease();});var method=typeof(CombatSfxPlayback).GetMethod("OfferEffect");Assert.That(method,Is.Not.Null,"Per-original subeffect contract missing");
   object[] args={1L,"action","result","pulse",4L,"hit","clip",3,0,"contact",null};Assert.That(method.Invoke(owner,args),Is.EqualTo("played"));args[5]="heal";args[9]="lifesteal";Assert.That(method.Invoke(owner,args),Is.EqualTo("played"));Assert.That(method.Invoke(owner,args),Is.EqualTo("old_or_duplicate"));Assert.That(count,Is.EqualTo(2));
  }
  [Test] public void PlayerUsesOwnedPoolAndStopsOnDisclosureRevocation(){
   var go=new GameObject("support-audio-test");var clip=AudioClip.Create("fixture",1000,1,1000,false);
   try{
    var player=go.AddComponent<LastRailAudioCuePlayer>();var voice=go.AddComponent<AudioSource>();voice.volume=.8f;voice.panStereo=.3f;
    player.ConfigureForTests(null,null,new[]{new LastRailAudioClipBinding("fixture",clip,false,.4f)});
    player.ConfigureCombatSources(new[]{voice},null);player.ConfigureCombatSpatialPolicy(new CombatAudioSpatialPolicy(.2f,p=>p.x,d=>.5f),()=>false);
    player.BeginCombatScope(new object(),1);player.ApplyMixSettings(1,.5f);bool allowed=true;
    var position=new CombatAudioPosition("actor",2,new Vector3(.5f,0,0),true,true,4);
    var mapping=new CombatAudioResultBinding("Lifesteal","fixture","recovery",3,0,false);
    Assert.That(player.TryPlayCombatResult(1,"a","HIT","p",1,"Lifesteal",mapping,()=>allowed,()=>position,out var lease),Is.EqualTo("played"));
    Assert.That(voice.panStereo,Is.EqualTo(.1f).Within(.00001));Assert.That(voice.volume,Is.EqualTo(.2f).Within(.00001));
    player.ApplyMixSettings(1,.25f);Assert.That(voice.volume,Is.EqualTo(.1f).Within(.00001));
    allowed=false;typeof(LastRailAudioCuePlayer).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);Assert.That(voice.clip,Is.Null);Assert.That(voice.panStereo,Is.EqualTo(.3f).Within(.00001));
    lease.Dispose();player.EndCombatScope();Assert.That(voice.volume,Is.EqualTo(.8f).Within(.00001));
   }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(clip);}
  }
  [Test] public void PlayerRejectsMissingPointAndMuteAndKeepsOldLeaseFromStoppingReplacement(){
   var go=new GameObject("support-audio-test");var clip=AudioClip.Create("fixture",1000,1,1000,false);
   try {
    var player=go.AddComponent<LastRailAudioCuePlayer>();var voice=go.AddComponent<AudioSource>();
    player.ConfigureForTests(null,null,new[]{new LastRailAudioClipBinding("fixture",clip,true,.4f)});
    player.ConfigureCombatSources(new[]{voice},null);player.ConfigureCombatSpatialPolicy(new CombatAudioSpatialPolicy(.2f,p=>p.x,null),()=>true);
    player.BeginCombatScope(new object(),1);
    var mapping=new CombatAudioResultBinding("Heal","fixture","recovery",3,0,true);
    Assert.That(player.TryPlayCombatResult(1,"a","HIT","p",1,"Heal",mapping,()=>true,null,out _),Is.EqualTo("missing_original_position_owner"));
    var position=new CombatAudioPosition("device",1,Vector3.zero,true,true,null);
    player.ApplyMixSettings(1,0);Assert.That(player.TryPlayCombatResult(2,"a","HIT","p2",2,"Heal",mapping,()=>true,()=>position,out _),Is.EqualTo("muted"));Assert.That(voice.clip,Is.Null);
    player.ApplyMixSettings(1,1);
    Assert.That(player.TryPlayCombatResult(3,"a","HIT","p3",3,"Heal",mapping,()=>true,()=>position,out var old),Is.EqualTo("played"));Assert.That(voice.loop,Is.True);Assert.That(voice.panStereo,Is.Zero);
    position=new CombatAudioPosition("device",2,Vector3.zero,true,true,null);typeof(LastRailAudioCuePlayer).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);Assert.That(voice.clip,Is.Null);
    Assert.That(player.TryPlayCombatResult(4,"a","HIT","p4",4,"Heal",mapping,()=>true,()=>position,out var current),Is.EqualTo("played"));
    old.Dispose();Assert.That(voice.clip,Is.EqualTo(clip));current.Dispose();Assert.That(voice.clip,Is.Null);
   }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(clip);}
  }
  [Test] public void InvalidOriginalDataCannotBecomeAConfirmedSupportOrImmunity(){
   Assert.Throws<ArgumentException>(()=>CombatAudioResults.ReadNative(JObject.Parse("{hp:{hpLoss:0}}")));
   Assert.Throws<ArgumentException>(()=>CombatAudioResults.Support("Returned",-1));
   Assert.That(CombatAudioResults.Contact(0,0),Is.EqualTo("ContactOnly"));
   Assert.That(CombatAudioResults.Support("Returned",1),Is.EqualTo("Returned"));
   int projections=0;var policy=new CombatAudioSpatialPolicy(.1f,p=>{projections++;return p.x;},null);
   Assert.That(policy.Evaluate(Vector3.zero,true,false,false,null,3,false),Is.Null);Assert.That(projections,Is.Zero);
   Assert.Throws<ArgumentException>(()=>policy.Evaluate(new Vector3(float.NaN,0,0),true,true,true,null,3,false));
  }
  [Test] public void EndDuringPositionCallbackNeverAppliesToReturnedVoice(){
   int applied=0,stopped=0;CombatAudioPositionPlayback playback=null;
   var original=new CombatAudioPosition("a",1,Vector3.zero,true,true,null);
   playback=new CombatAudioPositionPlayback(new CombatAudioSpatialPolicy(.1f,p=>0,null),original,false,true,3,
    ()=>{playback.Dispose();return original;},()=>false,v=>applied++,()=>stopped++);
   playback.Refresh();Assert.That(stopped,Is.EqualTo(1));Assert.That(applied,Is.Zero);
  }
  [Test] public void PositionPolicyCannotBeBypassedByThreeDimensionalSource(){
   var go=new GameObject("three-dimensional-source");
   try{var player=go.AddComponent<LastRailAudioCuePlayer>();var voice=go.AddComponent<AudioSource>();voice.spatialBlend=1;
    player.ConfigureCombatSources(new[]{voice},null);
    Assert.Throws<ArgumentException>(()=>player.BeginCombatScope(new object(),1));
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test] public void OriginalCommittedNativeReceiptIsConsumedWithoutMutatingOwner(){
   JObject saved=null,frame=null;
   using var owner=new RoofBattleLocalOwner(RoofBattleLocalOwnerTests.Binding(),1000,"audio-result-fixture",
    ()=>saved==null?null:(JObject)saved.DeepClone(),(candidate,revision)=>saved=(JObject)candidate.DeepClone());
   owner.Open();using var subscription=owner.Subscribe(value=>frame=value);
   owner.Advance("first",10000,1);var before=owner.Snapshot().ToString();
   var hit=(JObject)((JArray)frame["events"])[1];
   Assert.That(CombatAudioResults.ReadNative((JObject)hit["nativeReceipt"]),Is.EqualTo(new[]{"HpContact"}));
   Assert.That(hit["worldPoint"].Type,Is.EqualTo(JTokenType.Null));
   Assert.That(owner.Snapshot().ToString(),Is.EqualTo(before));
  }
  sealed class TestLease:IDisposable{public void Dispose(){}} }
}

