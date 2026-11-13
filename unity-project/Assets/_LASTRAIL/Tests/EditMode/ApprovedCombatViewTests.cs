using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class ApprovedCombatViewTests {
 [Test] public void FiveCardsAndWorldShareSelectionWithoutCombatCommandsAfterRebind(){
  var prepared=JObject.Parse(System.IO.File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-frontline-policy-UtU7yJ/prepared.json"));
  BasicCombatSimulation Fresh(){var model=BasicCombatSimulation.FromPreparedProfileForLocalTest(prepared,(JObject)prepared["ownedSelection"],(string)prepared["ownedSelection"]["accountId"],1);model.ConfigureLocalOperatorAutomation(true);return model;}
  var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("FiveSelectionView",typeof(RectTransform),typeof(Canvas));
  try{
   var view=go.AddComponent<ApprovedCombatView>();var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");view.Bind(driver,assets);
   var actors=((JArray)driver.HudState["actors"]).Where(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]).ToArray();Assert.AreEqual(5,actors.Length);
   var before=driver.StateJson;
   for(int repeat=0;repeat<3;repeat++){
    go.SetActive(false);go.SetActive(true);view.BindOrRefresh(driver,assets);
    foreach(var actor in actors){
     string id=(string)actor["id"];var card=go.GetComponentsInChildren<Button>(true).Single(b=>b.name=="SelectOperator_"+id);var world=go.GetComponentsInChildren<Button>(true).Single(b=>b.name==id);
     card.onClick.Invoke();Assert.AreEqual(id,view.InformationFrame.Target.ActorId);Assert.AreEqual((string)actor["visual"],view.InformationFrame.Target.OperatorId);Assert.AreEqual((int)actor["hp"],view.InformationFrame.Snapshot.Health.CurrentHp);
     world.onClick.Invoke();Assert.IsNull(view.InformationFrame.Target,"Same actor toggles once through the shared handler.");
     world.onClick.Invoke();Assert.AreEqual(id,view.InformationFrame.Target.ActorId);card.onClick.Invoke();Assert.IsNull(view.InformationFrame.Target);
    }
   }
   var shuffled=(JObject)driver.HudState.DeepClone();shuffled["actors"]=new JArray(((JArray)shuffled["actors"]).Reverse());shuffled["ultimates"]=new JArray(((JArray)shuffled["ultimates"]).Reverse());
   int hp=10;foreach(var actor in (JArray)shuffled["actors"]){actor["hp"]=hp;actor["maxHp"]=100;actor["shield"]=hp/2;hp+=10;}
   typeof(ApprovedCombatView).GetMethod("RefreshOperatorCards",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,new object[]{shuffled});
   foreach(var actor in actors){string id=(string)actor["id"];var expected=((JArray)shuffled["actors"]).Single(a=>(string)a["id"]==id);var meter=go.GetComponentsInChildren<CombatMeter>(true).Single(m=>m.name=="OperatorHp"&&m.transform.parent.name=="OperatorCard_"+id);float denominator=Math.Max(100,(int)expected["hp"]+(int)expected["shield"]);Assert.AreEqual((int)expected["hp"]/denominator,meter.Value);Assert.AreEqual((int)expected["shield"]/denominator,meter.ShieldValue);}
   Assert.AreEqual(before,driver.StateJson,"Selection and display snapshots cannot mutate the combat owner.");
   go.SetActive(false);go.GetComponentsInChildren<Button>(true).First(b=>b.name.StartsWith("SelectOperator_")).onClick.Invoke();Assert.IsNull(view.InformationFrame.Target);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void MissingFigureKeepsSelectionAndStateAndRecoversWithoutReplayingHits(){
  var source=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");var assets=UnityEngine.Object.Instantiate(source);assets.OperatorRigs=null;
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("MissingFigureView",typeof(RectTransform),typeof(Canvas));
  try{
   var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,assets);view.UseLandscapeLayout();
   var actor=((JArray)driver.HudState["actors"]).First(a=>!(bool)a["enemy"]&&!(bool)a["train"]&&!(bool)a["device"]);var id=(string)actor["id"];var visual=(string)actor["visual"];
   var art=go.GetComponentsInChildren<RawImage>(true).Single(a=>a.name=="Unit"&&a.transform.parent.name==id);var body=art.transform.parent;
   body.GetComponent<Button>().onClick.Invoke();Assert.NotNull(view.InformationFrame.Target);var target=view.InformationFrame.Target;
   var before=driver.StateJson;var position=body.position;var hitCount=view.HitPresentations;var atlases=assets.OperatorAtlases.ToArray();var frames=assets.Frames.ToArray();
   var units=(System.Collections.IDictionary)typeof(ApprovedCombatView).GetField("units",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(view);int objects=go.GetComponentsInChildren<Transform>(true).Length;
   typeof(ApprovedCombatView).GetMethod("Effect",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,new object[]{units[id],null,Color.white,.28f});Assert.AreEqual(objects,go.GetComponentsInChildren<Transform>(true).Length,"Missing decorative FX allocates nothing.");
   assets.OperatorAtlases=assets.OperatorAtlases.Select((a,i)=>assets.OperatorIds[i]==visual?null:a).ToArray();
   assets.Frames=assets.Frames.Select((a,i)=>assets.Keys[i].StartsWith(visual+"/")?null:a).ToArray();typeof(CombatPresentationAssets).GetField("cache",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(assets,null);
   for(int i=0;i<3;i++)view.Refresh();Assert.IsFalse(art.enabled);Assert.IsTrue(body.Find("MissingFigure").gameObject.activeSelf);Assert.AreEqual(position,body.position);Assert.AreSame(target,view.InformationFrame.Target);Assert.AreEqual(before,driver.StateJson);
   var normal=CombatPresentationConnectionTests.Fresh();
   for(int i=0;i<100;i++){normal.Advance(500000,1);driver.Advance(500000,1);view.Refresh();Assert.AreEqual(normal.CaptureLocalState(),driver.StateJson,"Rendering failure cannot change combat results.");}
   before=driver.StateJson;hitCount=view.HitPresentations;
   assets.OperatorAtlases=atlases;assets.Frames=frames;typeof(CombatPresentationAssets).GetField("cache",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(assets,null);view.Refresh();
   Assert.IsTrue(art.enabled);Assert.IsFalse(body.Find("MissingFigure").gameObject.activeSelf);Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(hitCount,view.HitPresentations);Assert.AreSame(target,view.InformationFrame.Target);
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(assets);}
 }
 [TestCase(false)][TestCase(true)] public void BindingAfterHitDoesNotReplayPastFlashButNewHitsStillDisplay(bool restore){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();string target=null;
  for(int i=0;i<600&&target==null;i++){
   driver.Advance(500000,1);var state=driver.HudState;
   var hit=((JArray)state["hits"]).LastOrDefault(h=>(long)h["Tick"]==driver.Tick&&(int)h["HpLoss"]>0&&(int)h["RemainingHp"]>0&&((JArray)state["actors"]).Any(a=>(string)a["id"]==(string)h["Target"]&&!(bool)a["train"]&&!(bool)a["device"]));
   target=(string)hit?["Target"];
  }
  Assert.NotNull(target,"Fixture must produce a live actor's actual hit.");
  var saved=driver.StateJson;
  if(restore){
   var model=BasicCombatSimulation.RestoreLocalState(saved,CombatPresentationConnectionTests.Fresh);
   var checkpoint=(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{model,1L},null);
   driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>checkpoint,(s,r)=>r+1,()=>true);driver.Open();
  }
  var go=new GameObject("RestoredHitView",typeof(RectTransform),typeof(Canvas));
  try{
   var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var art=go.GetComponentsInChildren<RawImage>(true).Single(a=>a.name=="Unit"&&a.transform.parent.name==target);
   Assert.AreEqual(Color.white,art.color,"Acknowledged historical damage must not restart a hit flash.");
   Assert.AreEqual(0,view.HitPresentations);Assert.AreEqual(saved,driver.StateJson);
   for(int i=0;i<600&&view.HitPresentations==0;i++){driver.Advance(500000,1);var before=driver.StateJson;view.Refresh();Assert.AreEqual(before,driver.StateJson);}
   Assert.Greater(view.HitPresentations,0,"New original hit events still reach the view.");
   var count=view.HitPresentations;view.Refresh();Assert.AreEqual(count,view.HitPresentations,"Refresh must not duplicate hit presentation.");
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void AppearanceSwapDoesNotMutateCombatAndAllOwnedActorsHaveFrames(){
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");Assert.NotNull(assets);Assert.NotNull(assets.Console);Assert.NotNull(assets.IconAtlas);foreach(var id in new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira","enemy_raider_scout"}){Assert.NotNull(assets.ActorAtlas(id),id);for(int cell=0;cell<6;cell++){var uv=assets.PoseUv(id,cell);Assert.Greater(uv.width,0);Assert.GreaterOrEqual(uv.xMin,0);Assert.LessOrEqual(uv.xMax,1);Assert.GreaterOrEqual(uv.yMin,0);Assert.LessOrEqual(uv.yMax,1);}}
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  foreach(var row in (JArray)driver.HudState["actors"])if(!(bool)row["train"]&&!(bool)row["device"])foreach(var state in new[]{"idle","attack","hit","down_or_defeated","skill_or_special"})for(int i=1;i<=4;i++)Assert.NotNull(assets.Find((string)row["visual"]+"/"+state+"/"+i),(string)row["visual"]);
  var go=new GameObject("ViewTest",typeof(RectTransform),typeof(Canvas));var texture=new Texture2D(2,2);
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,assets);var before=driver.StateJson;
   view.SetAppearance("test-map",texture,"test-skin",texture,texture);view.SetTurret("car_gun","test-turret",texture,new Vector2(20,20));
   Assert.AreEqual(before,driver.StateJson);Assert.AreEqual("test-map",view.MapId);Assert.AreEqual("test-skin",view.SkinId);
   Assert.AreEqual(2,go.GetComponentsInChildren<RawImage>(true).Count(x=>x.name.StartsWith("Turret_")));
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(texture);}
 }
 [Test] public void DeadActorAndRecoveryComeFromModelNotAnimation(){
  var s=CombatPresentationConnectionTests.Fresh();var data=JObject.Parse(s.CaptureLocalState());var id=(string)s.ReadHudState()["skills"][0]["actor"];
  // A valid low-HP restored checkpoint stress fixture, not a damage event generated by UI.
  var row=((JArray)data["actors"]).Single(r=>(string)r["id"]==id);row["vitals"]["hp"]=1;
  var restored=BasicCombatSimulation.RestoreLocalState(data.ToString(),CombatPresentationConnectionTests.Fresh);
  bool down=false,recovered=false;
  for(int i=0;i<1000;i++){restored.Advance(500000,1);var a=((JArray)restored.ReadHudState()["actors"]).Single(x=>(string)x["id"]==id);if((int)a["hp"]==0)down=true;else if(down){recovered=true;break;}}
  Assert.IsTrue(down,"Stress fixture must actually be hit");Assert.IsTrue(recovered,"Recovery must be simulation scheduled");
 }
}

