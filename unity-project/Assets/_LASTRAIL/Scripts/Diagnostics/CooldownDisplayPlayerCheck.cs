#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
namespace LastRail.Diagnostics {
 public static class CooldownDisplayPlayerCheck {
  public static async Task Run(string output,TMP_FontAsset font){
   void Need(bool ok){if(!ok)throw new InvalidOperationException("Cooldown Player check failed");}
   var durations=new long?[]{null,0,1,50,999,1000,1001,4000};
   var expected=new[]{"미확인","","1초 미만","1초 미만","1초 미만","1","2","4"};
   var rows=new JArray();for(int i=0;i<durations.Length;i++){string text=CombatCooldownText.Format(durations[i]);Need(text==expected[i]);rows.Add(new JObject{["milliseconds"]=durations[i],["text"]=text});}
   var actors=new JArray(Enumerable.Range(0,5).Select(i=>new JObject{["operatorId"]="fixture-"+i,["activeCooldownRemainingTicks"]=40000000,["ultimateCharge"]=70}));
   var automation=OrdinaryRunAutomation.Read(new JObject{["version"]=OrdinaryRunAutomation.Version,["revision"]=1,["ultimateAuto"]=true,["actors"]=actors});
   var actions=new JObject{["stepTimeSpanTicks"]=500000,["ultimateAuto"]=true,["actors"]=actors.DeepClone()};
   foreach(JObject actor in (JArray)actions["actors"])actor["activeCooldownRemainingTicks"]=80;
   automation.RequireNodeSettings(actions);actions["actors"][0]["activeCooldownRemainingTicks"]=40000000;
   bool rejected=false;try{automation.RequireNodeSettings(actions);}catch(ArgumentException){rejected=true;}Need(rejected);
   var p=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-frontline-policy-UtU7yJ/prepared.json"));
   BasicCombatSimulation Fresh(){var model=BasicCombatSimulation.FromPreparedProfileForLocalTest(p,(JObject)p["ownedSelection"],(string)p["ownedSelection"]["accountId"],1);model.ConfigureLocalOperatorAutomation(false);return model;}
   var state=JObject.Parse(Fresh().CaptureLocalState());foreach(JObject actor in (JArray)state["actors"])actor["ready"]=100000;
   foreach(JObject skill in (JArray)state["skillReady"])skill["ready"]=20;
   var simulation=BasicCombatSimulation.RestoreLocalState(state.ToString(),Fresh);
   JObject Row()=> (JObject)((JArray)simulation.ReadHudState()["skills"]).Single(r=>(string)r["actor"]=="op_reina");
   Need((long)Row()["cooldownMs"]==1000);simulation.SetPaused(true);simulation.Advance(10000000,100);Need((long)Row()["cooldownMs"]==1000);
   simulation.SetPaused(false);simulation.SetSpeed(4);simulation.Advance(500000,10);Need(simulation.Tick==2&&(long)Row()["cooldownMs"]==900&&!(bool)Row()["enabled"]);
   var canvas=new GameObject("CooldownBoundaryCheck",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
   void Label(string name,string text,float y){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(canvas.transform,false);var label=go.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=28;label.color=Color.white;label.text=text;var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(55,-y);rect.sizeDelta=new Vector2(1160,54);}
   try{Label("Scope","쿨다운 경계 검수 · 합성 원응답 / 로컬 행동 owner · 실제 서버 전투 아님",25);
    for(int i=0;i<rows.Count;i++)Label("Boundary"+i,(durations[i].HasValue?durations[i].Value+" ms":"미공급")+" → "+(expected[i]==""?"시간 숫자 없음":expected[i]),90+i*55);
    Label("Clock","일시정지 유지 / 배속은 행동 시계에서만 적용: 1000 → 900 ms",550);
    Label("Units","현재 계약: 40000000 TimeSpan tick → 80 step / 원숫자 오용 거절",605);
    await LandscapeScreenSweepProbe.Capture(output,"cooldown-boundaries");
    File.WriteAllText(Path.Combine(output,"cooldown-check.json"),new JObject{["passed"]=true,["scope"]="SYNTHETIC_AUTOMATION_AND_LOCAL_OWNER_NOT_SERVER_COMBAT",["automationVersion"]=OrdinaryRunAutomation.Version,["nodeActionsVersion"]=OrdinaryNodeActions.Version,["rawDurationRejected"]=rejected,["pausePreservedMs"]=1000,["doubleSpeedRemainingMs"]=(long)Row()["cooldownMs"],["rows"]=rows}.ToString());
   }finally{canvas.SetActive(false);UnityEngine.Object.Destroy(canvas);}
   async Task Hud(string name,JObject saved,string expectedCooldown,bool noTarget){
    int writes=0;var driver=new CombatSceneDriver(()=>BasicCombatSimulation.RestoreLocalState(saved.ToString(),Fresh),()=>null,(body,revision)=>{writes++;return revision+1;},()=>true);driver.Open();
    var host=new GameObject("ActualProductHud",typeof(RectTransform));
    try{var view=host.AddComponent<ApprovedCombatView>();view.BindOrRefresh(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.UseLandscapeLayout();
     string before=driver.StateJson;int originalWrites=writes;
     await LandscapeScreenSweepProbe.Capture(output,name);
     var labels=host.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="CooldownSeconds").ToArray();Need(labels.Length==3&&labels.All(t=>t.text==expectedCooldown));
     if(noTarget){var row=((JArray)driver.HudState["ultimates"]).Single(r=>(string)r["actor"]=="op_reina");Need((string)row["status"]=="NO_TARGET"&&!(bool)row["enabled"]);Need(host.GetComponentsInChildren<CombatReadyFrame>(true).All(r=>r.Progress==0));}
     Need(before==driver.StateJson&&writes==originalWrites);
     File.WriteAllText(Path.Combine(output,name+".json"),new JObject{["passed"]=true,["scope"]="ACTUAL_APPROVED_HUD_WITH_LOCAL_FIXTURE_NOT_SERVER_RUN",["cooldownLabels"]=new JArray(labels.Select(t=>t.text)),["noTargetReadyGlowSuppressed"]=noTarget,["displayWrites"]=writes-originalWrites}.ToString());
    }finally{host.SetActive(false);UnityEngine.Object.Destroy(host);}
   }
   await Hud("product-hud-subsecond",JObject.Parse(simulation.CaptureLocalState()),"1초 미만",false);
   foreach(JObject skill in (JArray)state["skillReady"])skill["ready"]=0;
   foreach(JObject actor in (JArray)state["actors"])if(((string)actor["id"]).StartsWith("wave."))actor["availableAt"]=10000;
   state["operatorAutomation"]["charge"]["op_reina"]=100;
   await Hud("product-hud-no-target",state,"",true);
  }
 }
}
#endif
