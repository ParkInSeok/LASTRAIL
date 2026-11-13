using System;
using System.Collections.Generic;
using LastRail.Presentation.Audio;
using LastRail.Presentation.Settings;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode {
 public sealed class CombatAudioPresentationTests {
  static Type Need(string name){var type=typeof(LastRailAudioCuePlayer).Assembly.GetType(name);Assert.That(type,Is.Not.Null,name+" is not implemented");return type;}
  static object Make(string name,params object[] args)=>Activator.CreateInstance(Need(name),args);
  static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name).Invoke(target,args);
  sealed class Lease:IDisposable {Action release;public Lease(Action r){release=r;}public void Dispose(){var r=release;release=null;r?.Invoke();}}
  [Test] public void PriorityNeverLetsOrdinaryCutWarningAndReleaseIsOwned(){
   var started=new List<string>();var stopped=new List<string>();
   var owner=Make("LastRail.Presentation.Audio.CombatSfxPlayback",1,(Func<string,Action,IDisposable>)((cue,end)=>{started.Add(cue);return new Lease(()=>stopped.Add(cue));}));
   Assert.That(Call(owner,"Offer",1L,"action1","launch","pulse1",10L,"rifle","shot",3,0),Is.EqualTo("played"));
   Assert.That(Call(owner,"Offer",2L,"warning1","warning","pulse1",10L,"warning","danger",1,2),Is.EqualTo("played"));
   Assert.That(Call(owner,"Offer",3L,"action2","launch","pulse1",10L,"rifle","shot",3,0),Is.EqualTo("capacity_priority"));
   Assert.That(stopped,Is.EqualTo(new[]{"shot"}));((IDisposable)owner).Dispose();Assert.That(stopped,Is.EqualTo(new[]{"shot","danger"}));
  }
  [Test] public void SameActionExactPulseGroupsButNewPulseAndImpactRemain(){
   int count=0;var owner=Make("LastRail.Presentation.Audio.CombatSfxPlayback",8,(Func<string,Action,IDisposable>)((cue,end)=>{count++;return new Lease(()=>{});}));
   Assert.That(Call(owner,"Offer",1L,"a","launch","p1",20L,"rifle","shot",3,0),Is.EqualTo("played"));
   Assert.That(Call(owner,"Offer",2L,"a","launch","p1",20L,"rifle","shot",3,0),Is.EqualTo("same_original_group"));
   Assert.That(Call(owner,"Offer",2L,"a","launch","p1",20L,"rifle","shot",3,0),Is.EqualTo("old_or_duplicate"));
   Call(owner,"Offer",3L,"a","launch","p2",21L,"rifle","shot",3,0);
   Call(owner,"Offer",4L,"a","impact","p1",21L,"rifle","hit",3,0);
   Call(owner,"Offer",5L,"b","launch","p1",21L,"rifle","shot",3,0);
   Assert.That(count,Is.EqualTo(4));((IDisposable)owner).Dispose();
  }
  [Test] public void ReentrantCompletionDisposesReturnedLease(){int stopped=0;var owner=Make("LastRail.Presentation.Audio.CombatSfxPlayback",1,(Func<string,Action,IDisposable>)((cue,end)=>{end();return new Lease(()=>stopped++);}));Call(owner,"Offer",1L,"a","hit","p",1L,"hit","hit",3,0);Assert.That(stopped,Is.EqualTo(1));((IDisposable)owner).Dispose();Assert.That(stopped,Is.EqualTo(1));}
  [Test] public void ReducedIsIndependentDiscreteSetting(){Assert.That(Enum.TryParse<LocalSetting>("ReducedPresentation",out var key),Is.True);var backend=new UnitySettingsBackend(null);Assert.That(backend.Supports(key),Is.True);Assert.That(backend.Supports(LocalSetting.MusicVolume),Is.True);Assert.That(backend.Supports(LocalSetting.EffectsVolume),Is.True);}
  [Test] public void PlaybackProfileOnlyReducesCurrentFilmAndNeverRestarts(){int reduced=0,ended=0;var owner=Make("LastRail.Presentation.Battle.UltimatePresentationIntensity",(Func<bool>)(()=>{reduced++;return false;}),(Action)(()=>ended++));Call(owner,"Select",true);Call(owner,"Select",true);Call(owner,"Select",false);Assert.That(reduced,Is.EqualTo(1));Assert.That(ended,Is.EqualTo(1));((IDisposable)owner).Dispose();Call(owner,"Select",true);Assert.That(ended,Is.EqualTo(1));}
  [Test] public void PointerHeldAcrossHoldNeverBecomesBattleCommand(){var owner=Make("LastRail.Presentation.Battle.UltimatePointerGate");Assert.That(Call(owner,"Down",3),Is.EqualTo(true));var hold=(IDisposable)Call(owner,"Acquire");Assert.That(Call(owner,"Up",3),Is.EqualTo(false));Assert.That(Call(owner,"Down",4),Is.EqualTo(false));hold.Dispose();Assert.That(Call(owner,"Up",4),Is.EqualTo(false));Assert.That(Call(owner,"Down",5),Is.EqualTo(true));Assert.That(Call(owner,"Up",5),Is.EqualTo(true));}
  [Test] public void MusicRestoresLatestSelectionAndSummaryDoesNotTakeTheme(){
   var trace=new List<string>();var owner=Make("LastRail.Presentation.Audio.BattleMusicPlayback",(Func<string,IDisposable>)(cue=>{trace.Add("base:"+cue);return new Lease(()=>trace.Add("stop:"+cue));}),(Func<string,IDisposable>)(cue=>{trace.Add("theme:"+cue);return new Lease(()=>trace.Add("end:"+cue));}),(Action<bool>)(value=>trace.Add("suppress:"+value)));
   Call(owner,"Select",1L,"battle",false);Call(owner,"Select",2L,"battle",false);
   Assert.That(Call(owner,"BeginTheme",false,"short"),Is.Null);
   var film=(IDisposable)Call(owner,"BeginTheme",true,"hero");Call(owner,"Select",3L,"result",true);film.Dispose();
   Assert.That(trace.FindAll(x=>x=="base:battle").Count,Is.EqualTo(1));Assert.That(trace.FindAll(x=>x=="theme:short").Count,Is.Zero);Assert.That(trace[trace.Count-1],Is.EqualTo("suppress:False"));
   Assert.That(Call(owner,"Select",2L,"battle",false),Is.EqualTo(false));((IDisposable)owner).Dispose();
  }
  [Test] public void MissingThemeNeverSilencesBattleAndMissingResultStopsOldBattle(){
   int stops=0;bool suppressed=false;var owner=Make("LastRail.Presentation.Audio.BattleMusicPlayback",(Func<string,IDisposable>)(cue=>cue=="battle"?new Lease(()=>stops++):null),(Func<string,IDisposable>)(cue=>null),(Action<bool>)(value=>suppressed=value));
   Call(owner,"Select",1L,"battle",false);Assert.That(Call(owner,"BeginTheme",true,"missing"),Is.Null);Assert.That(suppressed,Is.False);
   Call(owner,"Select",2L,"missing-result",true);Assert.That(stops,Is.EqualTo(1));((IDisposable)owner).Dispose();
  }
  [Test] public void ProtectedFrameFitsSafeAreaWithoutStretch(){
   var type=Need("LastRail.Presentation.Battle.UltimateFrameLayout");var rect=(Rect)type.GetMethod("Fit").Invoke(null,new object[]{new Vector2(100,200),new Rect(0,0,1,1),new Rect(10,20,80,120)});
   Assert.That(rect.width/rect.height,Is.EqualTo(.5f).Within(.0001f));Assert.That(rect.xMin,Is.GreaterThanOrEqualTo(9.9999f));Assert.That(rect.yMin,Is.GreaterThanOrEqualTo(19.9999f));Assert.That(rect.xMax,Is.LessThanOrEqualTo(90.0001f));Assert.That(rect.yMax,Is.LessThanOrEqualTo(140.0001f));
  }  [Test] public async System.Threading.Tasks.Task AvSelectionPinsReferencesAndReturnsAfterPlayback(){
   int returned=0,stopped=0;var asset=new GameObject("AV fixture");
   try{
    using var preparation=new LastRail.Resources.UltimateAssetPreparation<GameObject>((key,revision,cancel)=>System.Threading.Tasks.Task.FromResult(LastRail.Resources.CombatAssetLease<GameObject>.CreateFixture(asset,()=>returned++)),(value,key,revision)=>true,error=>{});
    preparation.SynchronizeOwner(this,1);await preparation.PrepareAsync(new[]{"visual","stem"},new string[0],"content1",null,System.Threading.CancellationToken.None);
    var method=preparation.GetType().GetMethod("SelectCombination");Assert.That(method,Is.Not.Null,"Immutable AV selection is not implemented");
    var refs=new[]{"visual","stem"};var selected=method.Invoke(preparation,new object[]{"combo1","timeline1","cue1",false,refs,"content1",(Func<IReadOnlyList<GameObject>,bool>)(values=>values.Count==2)});
    refs[0]="mutated";
    Assert.That((IReadOnlyList<string>)selected.GetType().GetProperty("References").GetValue(selected),Is.EqualTo(new[]{"visual","stem"}));
    var playback=(IDisposable)Call(selected,"Play",(Func<IReadOnlyList<GameObject>,IDisposable>)(values=>new Lease(()=>{Assert.That(returned,Is.Zero);stopped++;})));
    await preparation.PrepareAsync(new string[0],new string[0],"content2",null,System.Threading.CancellationToken.None);Assert.That(returned,Is.Zero);
    playback.Dispose();Assert.That(stopped,Is.EqualTo(1));Assert.That(returned,Is.EqualTo(2));
   }finally{UnityEngine.Object.DestroyImmediate(asset);}
  }  [Test] public void OwnedAudioPoolAndThemeRespectLiveSettingsAndScopeTeardown(){
   var root=new GameObject("audio composition");var clip=AudioClip.Create("fixture",100,1,1000,false);
   try{
    var sfx=root.AddComponent<AudioSource>();var bgm=root.AddComponent<AudioSource>();var theme=root.AddComponent<AudioSource>();var voice=root.AddComponent<AudioSource>();
    sfx.volume=.8f;bgm.volume=.6f;theme.volume=.5f;voice.volume=.4f;bgm.mute=true;
    var player=root.AddComponent<LastRailAudioCuePlayer>();player.ConfigureForTests(sfx,bgm,new[]{new LastRailAudioClipBinding("combat_hit",clip,false,.7f),new LastRailAudioClipBinding("bgm_act1_rail_loop",clip,true,.8f),new LastRailAudioClipBinding("fixture-theme",clip,false,1)});
    player.ConfigureCombatSources(new[]{voice},theme);player.ConfigureThemeMix(.1f);player.BeginCombatScope(this,1);player.ApplyMixSettings(.5f,.25f);
    Assert.That(player.SelectBattleMusic(1,"bgm_act1_rail_loop",false),Is.True);
    var film=player.BeginUltimateTheme(true,"fixture-theme");Assert.That(film,Is.Not.Null);Assert.That(bgm.volume,Is.EqualTo(.03f).Within(.0001f));Assert.That(theme.volume,Is.EqualTo(.25f));
    Assert.That(player.TryPlayCombatCue(1,"a","hit","p",10,"hit","combat_hit",3,0),Is.EqualTo("played"));Assert.That(voice.volume,Is.EqualTo(.1f));
    player.ApplyMixSettings(.2f,0);Assert.That(voice.volume,Is.Zero);film.Dispose();Assert.That(bgm.volume,Is.EqualTo(.12f).Within(.0001f));Assert.That(bgm.mute,Is.True);
    Assert.That(player.TryPlayCombatCue(2,"b","hit","p",11,"hit","combat_hit",3,0),Is.Not.EqualTo("played"));
    player.EndCombatScope();Assert.That(voice.clip,Is.Null);Assert.That(voice.volume,Is.EqualTo(.4f));Assert.That(theme.clip,Is.Null);
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);}
  }
  [Test] public void SeparateWarningsNeverGroupAndEqualUrgencyKeepsEarlier(){int calls=0;using var owner=new CombatSfxPlayback(1,(cue,end)=>{calls++;return new Lease(()=>{});});Assert.That(owner.Offer(1,"a","warning","p",10,"warning","w",1,3),Is.EqualTo("played"));Assert.That(owner.Offer(2,"a","warning","p",10,"warning","w",1,3),Is.EqualTo("capacity_priority"));Assert.That(owner.Offer(3,"b","warning","p",10,"warning","w",1,4),Is.EqualTo("played"));Assert.That(calls,Is.EqualTo(2));}
  [Test] public void LateVoiceCompletionCannotStopReplacement(){Action old=null;int stops=0;using var owner=new CombatSfxPlayback(1,(cue,end)=>{if(old==null)old=end;return new Lease(()=>stops++);});owner.Offer(1,"a","hit","p",1,"f","hit",3,0);owner.Offer(2,"b","warn","p",2,"w","warn",1,0);old();Assert.That(owner.ActiveCount,Is.EqualTo(1));Assert.That(stops,Is.EqualTo(1));}
  [Test] public void OldThemeLeaseCannotReleaseNewTheme(){bool suppress=false;int stops=0;using var owner=new BattleMusicPlayback(cue=>new Lease(()=>{}),cue=>new Lease(()=>stops++),value=>suppress=value);var a=owner.BeginTheme(true,"a");var b=owner.BeginTheme(true,"b");a.Dispose();Assert.That(suppress,Is.True);Assert.That(stops,Is.EqualTo(1));b.Dispose();Assert.That(suppress,Is.False);Assert.That(stops,Is.EqualTo(2));}
  [Test] public void NestedInputHoldsPreserveOtherOwnerAndBlockedPointers(){using var gate=new LastRail.Presentation.Battle.UltimatePointerGate();var a=gate.Acquire();var b=gate.Acquire();a.Dispose();Assert.That(gate.Down(1),Is.False);b.Dispose();Assert.That(gate.Up(1),Is.False);Assert.That(gate.Down(2),Is.True);gate.Cancel(2);Assert.That(gate.Up(2),Is.False);}  [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator DisabledAudioObjectCanRestartSameLoop(){
   yield return new UnityEngine.TestTools.EnterPlayMode();
   var root=new GameObject("loop lifecycle");var clip=AudioClip.Create("loop fixture",44100,1,44100,false);
   try{
    var player=root.AddComponent<LastRailAudioCuePlayer>();var sfx=root.AddComponent<AudioSource>();var bgm=root.AddComponent<AudioSource>();
    player.ConfigureForTests(sfx,bgm,new[]{new LastRailAudioClipBinding("bgm_act1_rail_loop",clip,true,1)});
    player.TryPrepareBgmLoop("bgm_act1_loop");yield return null;Assert.That(bgm.isPlaying,Is.True);
    root.SetActive(false);root.SetActive(true);player.TryPrepareBgmLoop("bgm_act1_loop");yield return null;Assert.That(bgm.isPlaying,Is.True,"Lifecycle stop must invalidate the previous request");
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);}
   yield return new UnityEngine.TestTools.ExitPlayMode();
  }  sealed class ReducedBackend:ILocalSettingsBackend {
   public float Reduced;public bool Supports(LocalSetting key)=>key==LocalSetting.ReducedPresentation;
   public float Read(LocalSetting key)=>Reduced;public void ApplyAndSave(LocalSetting key,float value){Reduced=value;}
   public System.Threading.Tasks.Task<DisplaySettingsInfo> ReadDisplayAsync()=>throw new NotSupportedException();
  }
  [Test] public void SessionMissingReducedProfilesUsesMinimalWithoutGameplayHold(){
   using var settings=new LocalSettingsModel(new ReducedBackend{Reduced=1});int holds=0;string kind=null;
   using var session=new LastRail.Presentation.Battle.UltimatePresentationSession("a",1,LastRail.Battle.CombatPresentationMode.Full,()=>true,()=>true,()=>true,()=>true,()=>Assert.Fail("Minimal cannot mark daily"),()=>{holds++;return new Lease(()=>{});},(k,visible,end,skip,fail)=>{kind=k;return new Lease(()=>{});},valid=>{},error=>Assert.Fail(error.ToString()));
   session.BindPresentationSettings(settings,(k,reduced)=>!reduced,()=>false);session.Start();Assert.That(kind,Is.EqualTo("Minimal"));Assert.That(holds,Is.Zero);Assert.That(session.ReducedPresentation,Is.True);
  }
  [Test] public void LiveReducedSettingSafelyEndsUnsupportedPlayerOnceAndUnsubscribes(){
   using var settings=new LocalSettingsModel(new ReducedBackend());int released=0,stopped=0,switches=0;
   using var session=new LastRail.Presentation.Battle.UltimatePresentationSession("a",1,LastRail.Battle.CombatPresentationMode.Full,()=>true,()=>true,()=>true,()=>true,()=>{},()=>new Lease(()=>released++),(k,visible,end,skip,fail)=>new Lease(()=>stopped++),valid=>{},error=>Assert.Fail(error.ToString()));
   session.BindPresentationSettings(settings,(k,reduced)=>true,()=>{switches++;return false;});session.Start();Assert.That(settings.Change(LocalSetting.ReducedPresentation,.5f),Is.False);settings.Change(LocalSetting.ReducedPresentation,1);settings.Change(LocalSetting.ReducedPresentation,0);settings.Change(LocalSetting.ReducedPresentation,1);
   Assert.That(session.State,Is.EqualTo("Disposed"));Assert.That(released,Is.EqualTo(1));Assert.That(stopped,Is.EqualTo(1));Assert.That(switches,Is.EqualTo(1));
  }
  [Test] public async System.Threading.Tasks.Task RejectedOrSynchronousAvPlaybackReturnsEveryBorrow(){
   var asset=new GameObject("AV lifetime fixture");int returned=0,stopped=0;
   try{
    using var preparation=new LastRail.Resources.UltimateAssetPreparation<GameObject>((key,revision,cancel)=>System.Threading.Tasks.Task.FromResult(LastRail.Resources.CombatAssetLease<GameObject>.CreateFixture(asset,()=>returned++)),(value,key,revision)=>true,error=>{});
    preparation.SynchronizeOwner(this,1);await preparation.PrepareAsync(new[]{"v"},new string[0],"r1",null,System.Threading.CancellationToken.None);
    Assert.That(preparation.SelectCombination("combo","time","cue",false,new[]{"v","missing"},"r1",values=>true),Is.Null);Assert.That(preparation.ActiveBorrowCount,Is.Zero);
    Assert.That(preparation.SelectCombination("combo","time","cue",false,new[]{"v"},"r1",values=>false),Is.Null);Assert.That(preparation.ActiveBorrowCount,Is.Zero);
    var selected=preparation.SelectCombination("combo","time","cue",false,new[]{"v"},"r1",values=>true);
    selected.Play(values=>{selected.Dispose();return new Lease(()=>stopped++);});Assert.That(stopped,Is.EqualTo(1));Assert.That(preparation.ActiveBorrowCount,Is.Zero);preparation.Dispose();Assert.That(returned,Is.EqualTo(1));
   }finally{UnityEngine.Object.DestroyImmediate(asset);}
  }  [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator AudioHoldsDoNotReleaseOtherOwnersOrReplayPausedCues(){
   yield return new UnityEngine.TestTools.EnterPlayMode();
   var root=new GameObject("owned pause");var clip=AudioClip.Create("pause fixture",44100,1,44100,false);
   try{
    var player=root.AddComponent<LastRailAudioCuePlayer>();var sfx=root.AddComponent<AudioSource>();var bgm=root.AddComponent<AudioSource>();var voice=root.AddComponent<AudioSource>();
    player.ConfigureForTests(sfx,bgm,new[]{new LastRailAudioClipBinding("bgm_act1_rail_loop",clip,true,1),new LastRailAudioClipBinding("combat_hit",clip,false,1)});player.ConfigureCombatSources(new[]{voice},null);player.ApplyMixSettings(1,1);player.BeginCombatScope(this,1);player.SelectBattleMusic(1,"bgm_act1_loop",false);
    yield return null;Assert.That(bgm.isPlaying,Is.True);
    var method=player.GetType().GetMethod("AcquireAudioHold");Assert.That(method,Is.Not.Null,"Owned audio pause contract missing");var menu=(IDisposable)method.Invoke(player,null);var app=(IDisposable)method.Invoke(player,null);
    Assert.That(bgm.isPlaying,Is.False);app.Dispose();Assert.That(bgm.isPlaying,Is.False);
    player.TryPlayCombatCue(1,"a","hit","p",1,"hit","combat_hit",3,0);yield return null;Assert.That(voice.isPlaying,Is.False);menu.Dispose();yield return null;Assert.That(bgm.isPlaying,Is.True);Assert.That(voice.isPlaying,Is.True);
    player.EndCombatScope();Assert.That(voice.isPlaying,Is.False);
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);}
   yield return new UnityEngine.TestTools.ExitPlayMode();
  } }
}







