using System;
using System.Collections;
using System.Reflection;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class ReinaBarrageVideoTests {
 sealed class ImpactRecorder {
  readonly ReinaBarrageVideoView view;
  internal readonly System.Collections.Generic.List<double> Times=new System.Collections.Generic.List<double>();
  internal ImpactRecorder(ReinaBarrageVideoView view){this.view=view;}
  internal void Record(Newtonsoft.Json.Linq.JObject row){Assert.True(view.ImpactAudioPlaying,$"Pulse {row["pulseIndex"]}: DSP display={view.LastImpactDisplayDsp}, audio={view.LastImpactAudioDsp}");Assert.That(view.LastImpactDisplayDsp-view.LastImpactAudioDsp,Is.InRange(-.001,.2),"Visual uses the first available frame after the audio deadline");Times.Add(view.LastImpactAudioDsp);}
 }
 [UnityTest] public IEnumerator NativeVideoEndsIntoNativeImpactAndSharedSoundCallback(){
  yield return new EnterPlayMode();
  var d=(CombatSceneDriver)typeof(OrdinaryReinaBarrageHostTests).GetMethod("Driver",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{false});
   var root=new GameObject("Reina native playback fixture",typeof(RectTransform),typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");
   Assert.NotNull(assets,"Canonical combat presentation resources must be loaded");
   var view=root.AddComponent<ReinaBarrageVideoView>();var parent=(RectTransform)root.transform;
  try{
   Assert.NotNull(assets.SupportFire);view.Bind(d,parent,assets.Font,assets.SupportFire,_=>new Vector2(400,300),()=>true);
   // EnterPlayMode reloads the domain; do not capture locals in an iterator closure created before it.
   var recorder=new ImpactRecorder(view);view.ImpactPresented+=recorder.Record;
   var video=root.GetComponent<VideoPlayer>();double deadline=Time.realtimeSinceStartupAsDouble+15;
   while(!video.isPrepared&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
   Assert.True(video.isPrepared,"Native final video preparation");Assert.That(video.length,Is.InRange(5.0,5.1));
   d.RequestSkill("native-film","op_reina","op_reina.ultimate");d.Advance(170000L*48,48);
   Assert.True(view.FilmVisible);Assert.True(d.HasPresentationHold);
   deadline=Time.realtimeSinceStartupAsDouble+10;bool decoded=false,sawFade=false,testedPause=false;double firstDecodedAt=-1;
   while(view.FilmVisible&&Time.realtimeSinceStartupAsDouble<deadline){
    if(video.texture!=null&&video.frame>=0&&firstDecodedAt<0){Assert.That(video.time,Is.InRange(0,.25),"Opening frame must be presented");firstDecodedAt=Time.realtimeSinceStartupAsDouble;}
    decoded|=video.texture!=null&&video.time>.2;
    sawFade|=root.transform.Find("ReinaCinematic").GetComponent<CanvasGroup>().alpha<.9f;
    if(!testedPause&&video.time>.4){testedPause=true;d.SetPaused(true);yield return null;double position=video.time,until=Time.realtimeSinceStartupAsDouble+.25;while(Time.realtimeSinceStartupAsDouble<until)yield return null;Assert.That(video.time,Is.EqualTo(position).Within(.08));d.SetPaused(false);}
    yield return null;
   }
   Assert.True(decoded,"Real decoder must advance");Assert.That(Time.realtimeSinceStartupAsDouble-firstDecodedAt,Is.InRange(4.5,6.5),"Five-second movie must retain its playback speed");Assert.True(sawFade,"Final frames reveal the battlefield");Assert.True(testedPause);Assert.False(view.FilmVisible);Assert.False(d.HasPresentationHold);Assert.AreEqual(0,view.ImpactPresentations);
   Assert.NotNull(root.transform.Find("ReinaDecorative62"),"Committed schedule owns the independent decorative layer");
   d.Advance(170000L*83,83);deadline=Time.realtimeSinceStartupAsDouble+2;while(view.ImpactPresentations==0&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;Assert.AreEqual(1,view.ImpactPresentations);Assert.NotNull(root.transform.Find("ReinaImpact_0"));
   Assert.True(view.ImpactAudioPlaying,"Same native impact uses scheduled unchanged audio");Assert.AreEqual(4,view.ImpactPoolSize);
   d.Advance(170000L*141,141);Assert.AreEqual(1,view.ImpactPresentations,"Batched native results must wait for display spacing");
   d.SetPaused(true);double pausedUntil=Time.realtimeSinceStartupAsDouble+.25;while(Time.realtimeSinceStartupAsDouble<pausedUntil)yield return null;Assert.AreEqual(1,view.ImpactPresentations);d.SetPaused(false);
   deadline=Time.realtimeSinceStartupAsDouble+6;while(view.ImpactPresentations<4&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
   Assert.AreEqual(4,view.ImpactPresentations);for(int i=1;i<recorder.Times.Count;i++)Assert.That(recorder.Times[i]-recorder.Times[i-1],Is.GreaterThanOrEqualTo(.7989),"This 17ms-step fixture supplies 47-tick (.799s) native intervals");
   Assert.AreEqual(4,view.ImpactPoolSize,"Four impacts reuse the prewarmed pool");
   var status=root.transform.Find("ReinaBarrageStatus");Assert.False(status.gameObject.activeSelf);
  }finally{d.Retire();UnityEngine.Object.Destroy(root);}
  yield return null;
  yield return new ExitPlayMode();
 }
}
