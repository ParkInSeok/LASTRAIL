using System;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatResolvedVisualIntegrationTests {
 [Test] public void DelayedRefreshAtActualTerminalKeepsResultsButDoesNotReplaySkillDecorations(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("terminal-delayed-refresh",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));int audio=0;view.EffectPresentationCue+=(kind,id)=>audio++;
   driver.RequestSkill("terminal-backlog","op_kyle","op_kyle.active");driver.Advance(5000000000,10000);
   Assert.That(driver.Outcome,Is.Not.EqualTo(BasicCombatOutcome.Running));Assert.That(((Newtonsoft.Json.Linq.JArray)driver.HudState["effectResults"]).Count,Is.GreaterThan(0));
   Assert.That(((Newtonsoft.Json.Linq.JArray)driver.HudState["actors"]).Any(a=>(string)a["id"]=="op_kyle"&&(int)a["hp"]>0),Is.True,"Keep the result recipient alive so the terminal guard is exercised.");
   string committed=driver.StateJson;view.Refresh();Assert.That(view.ResolvedVisualCount,Is.Zero);Assert.That(audio,Is.Zero);Assert.That(driver.StateJson,Is.EqualTo(committed));view.Refresh();Assert.That(view.ResolvedVisualCount,Is.Zero);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [TestCase("Victory")][TestCase("Failure")]
 public void TerminalPausedFrameReleasesDecorationsWithoutChangingResults(string outcome){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("terminal-decoration",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.SetCapturePresentationClock(()=>1);view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   // Explicit view-boundary terminal fixture, not a production battle result producer.
   var state=driver.HudState;var actors=(Newtonsoft.Json.Linq.JArray)state["actors"];foreach(var actor in actors){actor["attacking"]=false;actor["attackDue"]=-1;}
   var row=new Newtonsoft.Json.Linq.JObject{["id"]="TEST_ONLY_terminal_shield",["sourceId"]="op_kyle",["targetId"]="op_kyle",["commandId"]="TEST_ONLY_cast",["abilityId"]="op_kyle.active",["kind"]="SHIELD_ISSUE",["actualResult"]=new Newtonsoft.Json.Linq.JObject{["addedAmount"]=1}};
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   typeof(ApprovedCombatView).GetMethod("ResultVisual",flags).Invoke(view,new object[]{state,row});
   typeof(ApprovedCombatView).GetMethod("AddSourceLine",flags).Invoke(view,new object[]{Vector2.zero,Vector2.one,"op_kyle","op_kyle",true,.1f});
   var linked=(System.Collections.Generic.HashSet<string>)typeof(ApprovedCombatView).GetField("linkedCommands",flags).GetValue(view);linked.Add("TEST_ONLY_link");
   System.Func<string,int> count=name=>((System.Collections.ICollection)typeof(ApprovedCombatView).GetField(name,flags).GetValue(view)).Count;
   foreach(var field in new[]{"resultMarks","sourceLines","kyleGrants"})Assert.That(count(field),Is.GreaterThan(0),field+" fixture not exercised");
   state["outcome"]=outcome;state["paused"]=true;string before=state.ToString(),committed=driver.StateJson;
   typeof(ApprovedCombatView).GetMethod("UpdateResultVisuals",flags).Invoke(view,new object[]{state});
   foreach(var field in new[]{"resultMarks","sourceLines","kyleGrants"})Assert.That(count(field),Is.Zero,field);
   Assert.That(linked,Is.Empty);Assert.That(go.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Actual_ShieldIssue_op_kyle"||t.name=="ActualCannonTrace"||t.name=="KyleGrant_op_kyle.active_op_kyle"),Is.False);
   Assert.That(state.ToString(),Is.EqualTo(before));Assert.That(driver.StateJson,Is.EqualTo(committed));
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void NativeGlyphActuallyRendersVisiblePixels(){
  var cameraGo=new GameObject("glyph-camera");var canvasGo=new GameObject("glyph-canvas",typeof(RectTransform),typeof(Canvas));var target=new RenderTexture(128,128,24);var pixels=new Texture2D(128,128,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try{var camera=cameraGo.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=target;var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var glyphGo=new GameObject("glyph",typeof(RectTransform));glyphGo.transform.SetParent(canvasGo.transform,false);glyphGo.GetComponent<RectTransform>().sizeDelta=new Vector2(100,100);var glyph=glyphGo.AddComponent<CombatResultGlyph>();glyph.Shape=CombatResultShape.Heal;glyph.SetProgress(.25f);Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,128,128),0,0);pixels.Apply();bool visible=false;foreach(var p in pixels.GetPixels32())if(p.g>80&&p.r>20){visible=true;break;}Assert.IsTrue(visible,"Geometry must reach the actual camera, not merely VertexHelper");
  }finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(canvasGo);UnityEngine.Object.DestroyImmediate(cameraGo);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);}
 }
 [Test] public void NativeGlyphsHaveVisibleFiniteGeometryAndCloseAtBothEnds(){var go=new GameObject("glyph",typeof(RectTransform));try{go.GetComponent<RectTransform>().sizeDelta=new Vector2(100,100);var glyph=go.AddComponent<CombatResultGlyph>();Assert.NotNull(go.GetComponent<CanvasRenderer>());var method=typeof(CombatResultGlyph).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly);foreach(CombatResultShape shape in Enum.GetValues(typeof(CombatResultShape))){glyph.Shape=shape;using(var vh=new UnityEngine.UI.VertexHelper()){glyph.SetProgress(.25f);method.Invoke(glyph,new object[]{vh});Assert.Greater(vh.currentVertCount,0);for(int i=0;i<vh.currentVertCount;i++){var v=new UIVertex();vh.PopulateUIVertex(ref v,i);Assert.IsFalse(float.IsNaN(v.position.x));Assert.Less(Mathf.Abs(v.position.x),100);Assert.Less(Mathf.Abs(v.position.y),100);}glyph.SetProgress(1);method.Invoke(glyph,new object[]{vh});Assert.AreEqual(0,vh.currentVertCount);glyph.SetProgress(0);method.Invoke(glyph,new object[]{vh});Assert.AreEqual(0,vh.currentVertCount);}}}finally{UnityEngine.Object.DestroyImmediate(go);}}
 [TestCase(CombatPresentationMode.Full)][TestCase(CombatPresentationMode.Simplified)]
 public void ResultDecorationsLeaveGameplayExactAndHistoricalResultsSilent(CombatPresentationMode mode){
  var expected=CombatPresentationConnectionTests.Fresh();var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("v03-test",typeof(RectTransform),typeof(Canvas));GameObject restored=null;
  try{var view=go.AddComponent<ApprovedCombatView>();double clock=0;view.SetCapturePresentationClock(()=>clock);view.ConfigurePresentation(new CombatPresentationPolicy(()=>false,()=>true){Mode=mode});view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   expected.RequestSkill("same-command","op_kyle","op_kyle.active");driver.RequestSkill("same-command","op_kyle","op_kyle.active");
   bool checkedAnchor=false;for(int i=0;i<50;i++){clock=i*.05;expected.Advance(500000,1);driver.Advance(500000,1);view.Refresh();if(i==3)view.SkipCurrentPresentation();Assert.AreEqual(expected.CaptureLocalState(),driver.StateJson);var mark=go.transform.Find("ApprovedBattle/Actual_ShieldIssue_op_kyle") as RectTransform;if(mark!=null){var actor=go.transform.Find("ApprovedBattle/CabinContent/BattleActors/op_kyle/Health") as RectTransform;Assert.NotNull(actor);Assert.Less(Vector3.Distance(mark.TransformPoint(mark.rect.center),actor.TransformPoint(new Vector2(actor.rect.xMax+16,actor.rect.center.y))),.01f,"Effect must remain on its actual recipient despite center-pivot root");checkedAnchor=true;}}Assert.IsTrue(checkedAnchor);
   Assert.Greater(view.ResolvedVisualCount,0);restored=new GameObject("restored-view",typeof(RectTransform),typeof(Canvas));var next=restored.AddComponent<ApprovedCombatView>();next.SetCapturePresentationClock(()=>clock);next.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));next.Refresh();Assert.AreEqual(0,next.ResolvedVisualCount);Assert.IsFalse(next.FullCutinVisible);Assert.AreEqual(expected.CaptureLocalState(),driver.StateJson);
  }finally{UnityEngine.Object.DestroyImmediate(go);if(restored!=null)UnityEngine.Object.DestroyImmediate(restored);}
 }
}
