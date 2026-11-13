#if UNITY_EDITOR
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Presentation.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.TestTools;
namespace LastRail.Tests.EditMode {
 public class OperatorInformationViewTests {
  [UnityTest] public IEnumerator ConfirmedImpactKeepsWorldAndInformationHealthTogether(){
   yield return new EnterPlayMode();foreach(int shield in new[]{1500,2000,2500})CheckImpact(shield);
  }
  static void CheckImpact(int initialShield){
   // Explicit synthetic shield/stat inputs; exercise the existing real attack, save and view paths.
   System.Func<BasicCombatSimulation> fresh=()=>{var s=HealthBase();s.ApplyResolvedShield("op_kyle",new CombatShieldSource("train","TEST_ONLY_impact",0),initialShield,5000);return s;};
   string saved=null;long revision=0;bool fail=false;
   System.Func<RestoredCombatCheckpoint> load=()=>saved==null?null:(RestoredCombatCheckpoint)System.Activator.CreateInstance(typeof(RestoredCombatCheckpoint),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{BasicCombatSimulation.RestoreLocalState(saved,fresh),revision},null);
   var driver=new CombatSceneDriver(fresh,load,(s,r)=>{if(fail)throw new System.IO.IOException("TEST_ONLY save failure");saved=s.CaptureLocalState();return ++revision;},()=>true);driver.Open();
   var go=new GameObject("ImpactInformation",typeof(RectTransform),typeof(Canvas));GameObject restored=null;
   try{
    var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,UnityEngine.Resources.Load<CombatPresentationAssets>("CombatPresentation/approved"));
    var button=go.GetComponentsInChildren<Button>(true).First(b=>b.name=="op_kyle");button.onClick.Invoke();
    var meter=button.transform.Find("Health").GetComponent<CombatMeter>();float originalHealth=meter.Value,originalShield=meter.ShieldValue;string before=driver.StateJson;
    fail=true;Assert.Throws<System.IO.IOException>(()=>driver.Advance(1000000,2));view.Refresh();
    Assert.That(driver.StateJson,Is.EqualTo(before));Assert.That(meter.Value,Is.EqualTo(originalHealth));Assert.That(meter.ShieldValue,Is.EqualTo(originalShield));Assert.That(view.HitPresentations,Is.Zero);Assert.That(view.InformationFrame.Visible,Is.False);
    fail=false;driver.Open();button.onClick.Invoke();driver.Advance(1000000,2);view.Refresh();
    var hit=((Newtonsoft.Json.Linq.JArray)driver.HudState["hits"]).Single(h=>(string)h["Target"]=="op_kyle");
    int absorbed=System.Math.Min(initialShield,2000),hp=1000-(2000-absorbed),remaining=initialShield-absorbed;
    Assert.That((int)hit["Absorbed"],Is.EqualTo(absorbed));Assert.That((int)hit["HpLoss"],Is.EqualTo(2000-absorbed));Assert.That((int)hit["RemainingHp"],Is.EqualTo(hp));
    var health=view.InformationFrame.Snapshot.Health;Assert.That(health.CurrentHp,Is.EqualTo(hp));Assert.That(health.MaxHp,Is.EqualTo(1000));Assert.That(health.Shield,Is.EqualTo(remaining));Assert.That(view.InformationFrame.Snapshot.Revision,Is.EqualTo(revision));
    float denominator=System.Math.Max(1000,hp+remaining);Assert.That(meter.Value,Is.EqualTo(hp/denominator));Assert.That(meter.ShieldValue,Is.EqualTo(remaining/denominator));Assert.That(view.DownPresentations,Is.Zero);Assert.That(driver.StateJson,Is.EqualTo(saved));
    Assert.That(view.InformationFrame.Visible,Is.True);
    var hpText=go.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Health");var shieldText=go.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Shield");
    Assert.That(hpText.gameObject.activeInHierarchy,Is.True);Assert.That(hpText.text,Does.Contain(hp+" / 1000"));Assert.That(shieldText.gameObject.activeInHierarchy,Is.True);Assert.That(shieldText.text,Does.Contain(remaining.ToString()));
    int shown=view.HitPresentations;Assert.That(shown,Is.GreaterThan(0));view.Refresh();Assert.That(view.HitPresentations,Is.EqualTo(shown));Assert.That(driver.StateJson,Is.EqualTo(saved));
    driver.Open();restored=new GameObject("RestoredImpactInformation",typeof(RectTransform),typeof(Canvas));var next=restored.AddComponent<ApprovedCombatView>();next.Bind(driver,UnityEngine.Resources.Load<CombatPresentationAssets>("CombatPresentation/approved"));
    var restoredButton=restored.GetComponentsInChildren<Button>(true).First(b=>b.name=="op_kyle");restoredButton.onClick.Invoke();var restoredMeter=restoredButton.transform.Find("Health").GetComponent<CombatMeter>();
    Assert.That(restoredMeter.Value,Is.EqualTo(meter.Value));Assert.That(restoredMeter.ShieldValue,Is.EqualTo(meter.ShieldValue));Assert.That(next.InformationFrame.Snapshot.Health.CurrentHp,Is.EqualTo(hp));Assert.That(next.InformationFrame.Snapshot.Health.Shield,Is.EqualTo(remaining));Assert.That(next.HitPresentations,Is.Zero);Assert.That(next.DownPresentations,Is.Zero);Assert.That(driver.StateJson,Is.EqualTo(saved));
   }finally{Object.DestroyImmediate(go);if(restored!=null)Object.DestroyImmediate(restored);}
  }
  static BasicCombatSimulation HealthBase(){var model=(BasicCombatSimulation)typeof(CombatVitalsModelConnectionTests).GetMethod("SkillFresh",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);typeof(BasicCombatSimulation).GetField("frontSlots",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(model,new[]{0,1});return model;}
  static BasicCombatSimulation HealthFixture(){
   var fresh=HealthBase();
   var state=Newtonsoft.Json.Linq.JObject.Parse(fresh.CaptureLocalState());
   var vitals=((Newtonsoft.Json.Linq.JArray)state["actors"]).Single(a=>(string)a["id"]=="op_kyle")["vitals"];vitals["hp"]=333;vitals["anchorHp"]=333;
   fresh=BasicCombatSimulation.RestoreLocalState(state.ToString(),HealthBase);
   fresh.ApplyResolvedShield("op_kyle",new CombatShieldSource("train","fixture",0),2000,100);return fresh;
  }
  [UnityTest] public IEnumerator OpenInformationFollowsCommittedHealthAndPreservesShield(){
   yield return new EnterPlayMode();CheckCommittedHealth();
  }
  static void CheckCommittedHealth(){int saves=0;bool fail=false;bool owner=true;
   var driver=new CombatSceneDriver(HealthFixture,()=>null,(s,r)=>{if(fail)throw new System.IO.IOException("test save failure");saves++;return r+1;},()=>owner);driver.Open();
   var go=new GameObject("HealthInformation",typeof(RectTransform),typeof(Canvas));
   try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,UnityEngine.Resources.Load<CombatPresentationAssets>("CombatPresentation/approved"));go.GetComponentsInChildren<Button>(true).First(b=>b.name=="op_kyle").onClick.Invoke();
    var health=typeof(OperatorInformationSnapshot).GetProperty("Health");Assert.That(health,Is.Not.Null,"Missing original committed HP information");
    System.Action<int,int> check=(hp,max)=>{var h=health.GetValue(view.InformationFrame.Snapshot);Assert.That(h,Is.Not.Null);Assert.That(h.GetType().GetProperty("CurrentHp").GetValue(h),Is.EqualTo(hp));Assert.That(h.GetType().GetProperty("MaxHp").GetValue(h),Is.EqualTo(max));Assert.That(h.GetType().GetProperty("Shield").GetValue(h),Is.EqualTo(2000));};
    check(333,1000);var first=view.InformationFrame.Snapshot;view.Refresh();Assert.That(view.InformationFrame.Snapshot,Is.SameAs(first));
    var effects=driver.HudState["effectResults"].ToString();driver.ApplyResolvedMaxHp("op_kyle",1500);view.Refresh();Assert.That(driver.HudState["effectResults"].ToString(),Is.EqualTo(effects));check(499,1500);Assert.That(view.InformationFrame.Snapshot.Revision,Is.EqualTo(driver.CaptureCommittedState().Revision));
    Assert.That(go.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Health").text,Does.Contain("499 / 1500"));
    Assert.That(go.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Shield").text,Does.Contain("2000"));
    CaptureHealth(go);
    var saved=driver.StateJson;var restored=BasicCombatSimulation.RestoreLocalState(saved,HealthFixture);Assert.That(restored.GetHp("op_kyle"),Is.EqualTo(499));
    driver.ApplyResolvedMaxHp("op_kyle",1500);view.Refresh();Assert.That(saves,Is.EqualTo(2));
    driver.ApplyResolvedMaxHp("op_kyle",1000);view.Refresh();check(333,1000);
    owner=false;view.Refresh();Assert.That(view.InformationFrame.Visible,Is.False);owner=true;go.GetComponentsInChildren<Button>(true).First(b=>b.name=="op_kyle").onClick.Invoke();check(333,1000);
    fail=true;Assert.Throws<System.IO.IOException>(()=>driver.ApplyResolvedMaxHp("op_kyle",2000));view.Refresh();Assert.That(view.InformationFrame.Visible,Is.False);
   }finally{Object.DestroyImmediate(go);}
  }
  static void CaptureHealth(GameObject go){
   var path=System.Environment.GetEnvironmentVariable("LASTRAIL_HEALTH_CAPTURE");if(string.IsNullOrEmpty(path))return;
   var cam=new GameObject("HealthCapture",typeof(Camera));var target=new RenderTexture(720,1280,24);var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);var previous=RenderTexture.active;var canvas=go.GetComponent<Canvas>();
   try{go.GetComponent<CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;((RectTransform)go.transform).sizeDelta=new Vector2(720,1280);go.transform.position=Vector3.zero;go.transform.localScale=Vector3.one;
    var camera=cam.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=640;camera.aspect=720f/1280;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.cullingMask=1<<31;canvas.worldCamera=camera;
    foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();System.IO.File.WriteAllBytes(path,pixels.EncodeToPNG());
   }finally{RenderTexture.active=previous;cam.GetComponent<Camera>().targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);Object.DestroyImmediate(cam);}
  }
  [UnityTearDown] public IEnumerator ExitRuntime(){if(Application.isPlaying)yield return new ExitPlayMode();}
  [UnityTest] public IEnumerator ActualLegacyActorCallbackKeepsCombatUnchanged(){
   yield return new EnterPlayMode();
   var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("InformationIntegration",typeof(RectTransform),typeof(Canvas));
   try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,UnityEngine.Resources.Load<CombatPresentationAssets>("CombatPresentation/approved"));var before=driver.StateJson;
    var actor=go.GetComponentsInChildren<Button>(true).First(b=>b.name=="op_mira");Assert.That(actor.gameObject.activeInHierarchy,Is.True);actor.onClick.Invoke();Assert.That(view.InformationFrame.Target.OperatorId,Is.EqualTo("op_mira"));Assert.That(view.InformationFrame.Snapshot.EquipmentKnown,Is.False);Assert.That(driver.StateJson,Is.EqualTo(before));
    actor.onClick.Invoke();Assert.That(view.InformationFrame.Visible,Is.False);Assert.That(driver.StateJson,Is.EqualTo(before));
    actor.onClick.Invoke();go.SetActive(false);Assert.That(view.InformationFrame.Visible,Is.False);go.SetActive(true);actor.onClick.Invoke();Assert.That(view.InformationFrame.Visible,Is.True);
   }finally{Object.DestroyImmediate(go);}
  }
  [Test] public async Task ViewReusesOneDetailAndSuppressionRestoresWithoutCommands(){
   var go=new GameObject("InformationViewTest",typeof(RectTransform));var target=new OperatorInformationTarget("r","m","op_mira",1);
   using(var controller=new OperatorInformationController((t,ct)=>Task.FromResult(new OperatorInformationSnapshot(t,1,new[]{new OperatorEquipmentInformation("i",1,"장비",1,null,"확정 설명"),null,null},null,"")))){
    try{var view=go.AddComponent<OperatorInformationView>();view.Configure(go.transform,TMP_Settings.defaultFontAsset);controller.Bind(view.Present);view.Bind(controller.OpenDetail,controller.CloseDetail,controller.Back);await controller.SelectAsync(target);
     Assert.That(go.GetComponentsInChildren<Button>(true).Any(b=>b.name=="RetryInformation"),Is.False);
     var slot=go.GetComponentsInChildren<Button>(true).Single(b=>b.name=="EquippedSlot0");slot.onClick.Invoke();Assert.That(view.DetailVisible,Is.True);slot.onClick.Invoke();Assert.That(go.GetComponentsInChildren<RectTransform>(true).Count(r=>r.name=="OperatorEquipmentDetail"),Is.EqualTo(1));
     Assert.That(go.GetComponentsInChildren<Image>(true).Single(i=>i.name=="OperatorEquipmentDetail").raycastTarget,Is.True);
     view.SetSuppressed(true);Assert.That(view.DetailVisible,Is.False);view.SetSuppressed(false);Assert.That(view.DetailVisible,Is.True);
     go.GetComponentsInChildren<Button>(true).Single(b=>b.name=="CloseEquipmentDetail").onClick.Invoke();Assert.That(view.DetailVisible,Is.False);Assert.That(controller.Frame.Target,Is.SameAs(target));
     view.Unbind();slot.onClick.Invoke();Assert.That(controller.Frame.DetailSlot,Is.EqualTo(-1));
    }finally{controller.Unbind();Object.DestroyImmediate(go);}
   }
  }
 }
}
#endif
