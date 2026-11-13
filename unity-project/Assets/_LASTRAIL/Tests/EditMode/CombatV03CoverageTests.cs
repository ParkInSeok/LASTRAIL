using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
public class CombatV03CoverageTests {
 const string Output="D:/Project/LASTRAIL/Worktrees/5f93/LASTRAIL/QA/Design/CombatV03Coverage-20260912";
 static BasicCombatSimulation Make(string leader,bool fixture)=>(BasicCombatSimulation)typeof(Ce03EffectValueTests).GetMethod(fixture?"Fixture":"Make",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{leader});
 [TestCase(false)][TestCase(true)] public void WarningArrivingDuringDecorationHidesCutinAndLeaderTrace(bool prepared){
  var driver=new CombatSceneDriver(()=>prepared?CombatPresentationConnectionTests.Fresh():Make("op_kyle",false),()=>null,(s,r)=>r+1,()=>true);driver.Open();for(int i=0;i<1000&&!((JArray)driver.HudState["actors"]).Any(a=>(bool)a["enemy"]&&(bool)a["attacking"]&&(long)a["attackDue"]<=driver.Tick+4);i++)driver.Advance(500000,1);
  Assert.IsTrue(((JArray)driver.HudState["actors"]).Any(a=>(bool)a["enemy"]&&(bool)a["attacking"]&&(long)a["attackDue"]<=driver.Tick+4));
  var assets=UnityEngine.Object.Instantiate(LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));if(!prepared)assets.OperatorRigs=null;
  var go=new GameObject("warning",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.SetCapturePresentationClock(()=>1.0);var policy=new CombatPresentationPolicy(()=>false,()=>true){Mode=CombatPresentationMode.Full};view.ConfigurePresentation(policy);view.Bind(driver,assets);
   // Start decoration before the warning observation; this is not a skill receipt.
   policy.Begin("decorative-fixture",1,.4,false);typeof(ApprovedCombatView).GetMethod("AddSourceLine",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{Vector2.zero,Vector2.one,"op_kyle","op_reina",false,.22f});
   var before=driver.StateJson;view.Refresh();Assert.IsFalse(view.FullCutinVisible,"Newly imminent enemy attack must win over an already active cutin");Assert.IsNull(go.transform.Find("ApprovedBattle/LeaderResolvedRecipient"),"Existing source decoration must not cover a new warning");Assert.AreEqual(before,driver.StateJson);if(prepared)CaptureWarning(go,driver.HudState);
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(assets);}
 }
 static void CaptureWarning(GameObject go,JObject state){
  var cameraGo=new GameObject("warning-camera");var target=new RenderTexture(720,1280,24);var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try{var camera=cameraGo.AddComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();Directory.CreateDirectory(Output);File.WriteAllBytes(Output+"/warning-prepared.png",pixels.EncodeToPNG());File.WriteAllText(Output+"/warning-prepared-state.json",state.ToString());}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(cameraGo);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
 }
 [TestCase("Taunt","op_kyle")][TestCase("Interrupt","op_sera")][TestCase("ShieldAbsorb","op_reina")]
 public void ActualCommittedFixtureResultRendersAndDoesNotReplay(string shape,string leader){
  // Isolated numeric fixture, NOT a live encounter or admission. Only initial
  // shield state is seeded for absorption; all receipts come from the producer.
  Func<BasicCombatSimulation> fresh=()=>Make(leader,false);var raw=JObject.Parse(Make(leader,true).CaptureLocalState());
  if(shape=="ShieldAbsorb")((JArray)((JArray)raw["actors"]).Single(a=>(string)a["id"]=="inside")["vitals"]["layers"]).Add(new JObject{["entityId"]="op_kyle",["abilityId"]="op_kyle.active",["effectIndex"]=0,["remaining"]=1000,["expiry"]=100});
  Directory.CreateDirectory(Output);
  foreach(int speed in new[]{2,3,4})foreach(bool mute in new[]{false,true}){
   var expected=BasicCombatSimulation.RestoreLocalState(raw.ToString(),fresh);
   var initial=BasicCombatSimulation.RestoreLocalState(raw.ToString(),fresh);
   var checkpoint=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{initial,1L},null);
   var driver=new CombatSceneDriver(fresh,()=>checkpoint,(s,r)=>r+1,()=>true);driver.Open();driver.SetSpeed(speed);expected.SetSpeed(speed);
   var assets=UnityEngine.Object.Instantiate(LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));assets.OperatorRigs=null;
   var go=new GameObject("coverage",typeof(RectTransform),typeof(Canvas));var cameraGo=new GameObject("coverage-camera");var target=new RenderTexture(720,1280,24);var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);var previous=RenderTexture.active;GameObject restored=null;
   try{
    var camera=cameraGo.AddComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
    double clock=1;var view=go.AddComponent<ApprovedCombatView>();view.SetCapturePresentationClock(()=>clock);view.Bind(driver,assets);view.ConfigurePresentationAudio(CombatAbilityAudioSet.LoadResources(),mute?0:.65f);
    var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
    bool seen=false;view.ResolvedVisualCue+=(kind,id,recipient)=>{if(kind==shape)seen=true;};
    expected.RequestSkill("coverage",leader,leader+".active");driver.RequestSkill("coverage",leader,leader+".active");
    for(int i=0;i<80&&!seen;i++){clock+=.05;expected.Advance(500000,1);driver.Advance(500000,1);view.Refresh();Assert.AreEqual(expected.CaptureLocalState(),driver.StateJson);}
    Assert.IsTrue(seen,"Actual positive "+shape+" must be produced, not an empty receipt");
    var rows=(JArray)driver.HudState["effectResults"];var row=(JObject)rows.Single(r=>CombatResolvedVisual.Shape((JObject)r)==shape);string recipient=(string)row["targetId"];
    if(shape=="Interrupt")Assert.Greater(((JArray)row["actualResult"]["cancelledActionIds"]).Count,0);
    if(shape=="ShieldAbsorb")Assert.Greater((int)row["actualResult"]["absorbed"],0);
    clock+=.1;view.Refresh();var mark=go.transform.Find("ApprovedBattle/Actual_"+shape+"_"+recipient) as RectTransform;Assert.NotNull(mark);
    var actor=go.transform.Find("ApprovedBattle/BattleActors/"+recipient+"/Unit") as RectTransform;Assert.NotNull(actor);Assert.Less(Vector3.Distance(mark.TransformPoint(mark.rect.center),actor.TransformPoint(actor.rect.center)),.01f);
    if(speed==2&&!mute){Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();File.WriteAllBytes(Output+"/"+shape+"-actual-peak.png",pixels.EncodeToPNG());File.WriteAllText(Output+"/"+shape+"-actual-result.json",row.ToString());}
    if(mute)Assert.IsFalse(go.GetComponent<AudioSource>().isPlaying,"Muted presentation must not start audio playback");
    var before=driver.StateJson;restored=new GameObject("restored",typeof(RectTransform),typeof(Canvas));var next=restored.AddComponent<ApprovedCombatView>();next.Bind(driver,assets);next.Refresh();Assert.AreEqual(0,next.ResolvedVisualCount);Assert.AreEqual(before,driver.StateJson);
   }finally{RenderTexture.active=previous;if(restored!=null)UnityEngine.Object.DestroyImmediate(restored);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cameraGo);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(assets);}
  }
 }
}
