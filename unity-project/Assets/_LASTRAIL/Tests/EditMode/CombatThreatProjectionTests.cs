using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatThreatProjectionTests {
 static BasicCombatSimulation RailFixture(){var model=CombatPresentationConnectionTests.Fresh();var config=CombatRailJourneyTests.Config();config["offers"]=new JArray();config["artillery"]=CombatRailArtilleryTests.Config();config["artillery"]["encounters"][0]["spawnWave"]=1;model.ConfigureRailJourney(config);return model;}
 [Test] public void ActualOwnerWarningDetailsReuseStateAndReleaseModal(){
  var driver=new CombatSceneDriver(RailFixture,()=>null,(s,r)=>r+1,()=>true);driver.Open();for(int i=0;i<12;i++)driver.Advance(500000,1);
  var go=new UnityEngine.GameObject("threat-owner-view",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.UseLandscapeLayout();
   var root=go.transform.Find("ApprovedBattle");Assert.IsTrue(root.Find("ThreatSummary").gameObject.activeSelf);string before=driver.StateJson;int groups=go.GetComponentsInChildren<UnityEngine.CanvasGroup>(true).Length;
   Capture(go.GetComponent<UnityEngine.Canvas>(),"summary.png");
   Assert.That(root.Find("Notice").GetComponent<TMPro.TMP_Text>().text,Does.Not.Contain("운행 설정 기록"),"Rendering must not issue a throttle command.");
   root.Find("ThreatDetailsOpen").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.IsTrue(root.Find("ThreatDetails").gameObject.activeSelf);Assert.IsFalse(root.Find("Speed").GetComponent<UnityEngine.UI.Button>().IsInteractable());Assert.AreEqual(before,driver.StateJson);
   Capture(go.GetComponent<UnityEngine.Canvas>(),"details.png");
   root.Find("ThreatDetails/ThreatDetailsClose").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.IsFalse(root.Find("ThreatDetails").gameObject.activeSelf);Assert.AreEqual(groups,go.GetComponentsInChildren<UnityEngine.CanvasGroup>(true).Length);Assert.AreEqual(before,driver.StateJson);
   root.Find("LeaderCutin").gameObject.SetActive(true);root.Find("ThreatDetailsOpen").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Assert.IsFalse(root.Find("ThreatDetails").gameObject.activeSelf,"Cutscene must not reopen a modal over skip.");
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 static void Capture(UnityEngine.Canvas canvas,string name){
  var output=Environment.GetEnvironmentVariable("LASTRAIL_THREAT_CAPTURE");if(string.IsNullOrEmpty(output))return;
  if(!System.IO.Path.GetFullPath(output).StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("D QA capture required");System.IO.Directory.CreateDirectory(output);
  var go=new UnityEngine.GameObject("capture",typeof(UnityEngine.Camera));var camera=go.GetComponent<UnityEngine.Camera>();var target=new UnityEngine.RenderTexture(1600,900,24);var prior=UnityEngine.RenderTexture.active;var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;UnityEngine.Texture2D texture=null;
  try{camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.targetTexture=target;canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;UnityEngine.Canvas.ForceUpdateCanvases();camera.Render();UnityEngine.RenderTexture.active=target;texture=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGB24,false);texture.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);texture.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,name),UnityEngine.ImageConversion.EncodeToPNG(texture));}
  finally{canvas.renderMode=mode;canvas.worldCamera=oldCamera;UnityEngine.RenderTexture.active=prior;camera.targetTexture=null;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void StableOriginalIdsSurviveRestoreAndInFlightShotSurvivesSourceExit(){
  var state=JObject.Parse("{\"outcome\":\"Running\",\"tick\":10,\"actors\":[],\"railArtillery\":{\"encounters\":[{\"id\":\"gun\",\"status\":\"Passed\"}],\"projectiles\":[{\"sequence\":2,\"source\":\"gun\",\"dueTick\":15},{\"sequence\":1,\"source\":\"gun\",\"dueTick\":20}]}}");
  var first=CombatThreatProjection.Read(state);var restored=CombatThreatProjection.Read(JObject.Parse(state.ToString()));
  CollectionAssert.AreEqual(first.Warnings.Select(w=>w.Id),restored.Warnings.Select(w=>w.Id));Assert.AreEqual("shell:gun:1",first.Warnings[0].Id);Assert.AreEqual(2,first.Warnings.Count);
  ((JArray)state["railArtillery"]["projectiles"]).RemoveAt(0);Assert.AreEqual(1,CombatThreatProjection.Read(state).Warnings.Count);
 }
 [Test] public void DisplayAndDetailsDoNotAdvanceOrMutateOriginalBattle(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new UnityEngine.GameObject("threat-view",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.UseLandscapeLayout();string before=driver.StateJson;view.Refresh();Assert.AreEqual(before,driver.StateJson);Assert.NotNull(go.transform.Find("ApprovedBattle/ThreatSummary"));}
  finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void DueTickIsNotAResponseDeadlineAndTerminalClears(){
  var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatThreatProjection");Assert.NotNull(type);
  var state=JObject.Parse("{\"outcome\":\"Running\",\"tick\":10,\"actors\":[],\"railArtillery\":{\"encounters\":[],\"projectiles\":[{\"sequence\":2,\"source\":\"gun\",\"dueTick\":15},{\"sequence\":1,\"source\":\"gun\",\"dueTick\":20}]}}");
  string before=state.ToString();var projection=type.GetMethod("Read").Invoke(null,new object[]{state});var rows=(System.Collections.IList)type.GetProperty("Warnings").GetValue(projection);Assert.AreEqual(2,rows.Count);Assert.AreEqual(before,state.ToString());Assert.IsNull(type.GetProperty("Primary").GetValue(projection));
  Assert.That(rows[0].ToString(),Does.Not.Contain("초"));
  state["outcome"]="Victory";projection=type.GetMethod("Read").Invoke(null,new object[]{state});Assert.AreEqual(0,((System.Collections.IList)type.GetProperty("Warnings").GetValue(projection)).Count);
 }
}
