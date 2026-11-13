using System;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CombatSecondFinalTests
{
    [Test] public void RenderTextureCanvasUsesItsOwnViewportAndSupportLabelsStayInside(){var go=new GameObject("target-hud",typeof(RectTransform),typeof(Canvas));var cg=new GameObject("camera",typeof(Camera));var rt=new RenderTexture(720,1280,24);try{var d=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);d.Open();var view=go.AddComponent<ApprovedCombatView>();view.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var camera=cg.GetComponent<Camera>();camera.targetTexture=rt;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();var root=(RectTransform)go.transform.Find("ApprovedBattle");var corners=new Vector3[4];root.GetWorldCorners(corners);var a=camera.WorldToScreenPoint(corners[0]);var b=camera.WorldToScreenPoint(corners[2]);Assert.That(b.x-a.x,Is.EqualTo(720).Within(2));Assert.That(b.y-a.y,Is.EqualTo(1280).Within(2));for(int i=0;i<3;i++){var text=(RectTransform)root.Find("Command"+i+"/Text");Assert.GreaterOrEqual(text.anchoredPosition.y,-10);Assert.Greater(text.anchoredPosition.x,50);}}finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
    [Test] public void ConfiguredThrottleWritesActualSavedModelAndPreservesBattleSpeed(){var go=new GameObject("rail-ui",typeof(RectTransform),typeof(Canvas));try{BasicCombatSimulation Factory(){var s=Fresh();var c=CombatRailJourneyTests.Config();c["throttleMaximumBps"]=13000;c["throttleCruiseBps"]=10000;s.ConfigureRailJourney(c);return s;}var d=new CombatSceneDriver(Factory,()=>null,(s,r)=>r+1,()=>true);d.Open();d.SetSpeed(4);go.AddComponent<ApprovedCombatView>().Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var slider=go.transform.Find("ApprovedBattle/TrainThrottle").GetComponent<Slider>();Assert.IsTrue(slider.interactable);slider.value=0;Assert.AreEqual(7000,(int)d.HudState["railJourney"]["throttleBps"]);slider.value=1;Assert.AreEqual(13000,(int)d.HudState["railJourney"]["throttleBps"]);Assert.AreEqual(4,(int)d.HudState["speedHalfUnits"]);}finally{UnityEngine.Object.DestroyImmediate(go);}}
    static BasicCombatSimulation Fresh()
    {
        var p=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-general-active-auto-hM29XX/prepared.json"));
        var c=new Ce03CommandContract((JObject)p["commandContract"]);
        return c.CreateLocalTest(p,(string)p["ownedSelection"]["accountId"],c.Revision,1);
    }
    [Test]
    public void OperatorCardsShareApprovedFrameAndKeepInputClear()
    {
        var go=new GameObject("card-frames",typeof(RectTransform),typeof(Canvas));
        try
        {
            var driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);
            driver.Open();
            go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
            var texture=LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/operator-card-rim-d-v01");
            Assert.NotNull(texture);
            var rows=(JArray)driver.HudState["ultimates"];
            Assert.IsNotEmpty(rows);
            foreach(var row in rows)
            {
                var card=go.transform.Find("ApprovedBattle/OperatorCard_"+(string)row["actor"]);
                var frame=card.Find("CardFrame").GetComponent<RawImage>();
                Assert.AreSame(texture,frame.texture);
                Assert.IsFalse(frame.raycastTarget);
                Assert.NotNull(card.Find("OperatorNameplate"));
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test]
    public void ActualConsoleKeepsLiveControlsInSecondFinalRegions()
    {
        var go=new GameObject("second-final",typeof(RectTransform),typeof(Canvas));
        try
        {
            var d=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);d.Open();
            go.AddComponent<ApprovedCombatView>().Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
            var root=go.transform.Find("ApprovedBattle");
            Assert.IsTrue(root.Find("Title").gameObject.activeSelf,"Approved title must be visible");
            var title=(RectTransform)root.Find("Title");var wave=(RectTransform)root.Find("Wave");
            Assert.LessOrEqual(title.anchoredPosition.x+title.sizeDelta.x,wave.anchoredPosition.x);
            for(int i=0;i<3;i++)
            {
                var button=(RectTransform)root.Find("Command"+i);
                Assert.GreaterOrEqual(button.anchoredPosition.x,450,"Support stack must be right of throttle");
                Assert.GreaterOrEqual(button.sizeDelta.x,200);
                Assert.LessOrEqual(button.sizeDelta.y,70);
                if(i>0)Assert.Less(button.anchoredPosition.y,((RectTransform)root.Find("Command"+(i-1))).anchoredPosition.y);
                Assert.AreEqual((bool)d.HudState["manuals"][i]["enabled"],button.GetComponent<Button>().interactable);
            }
            Assert.NotNull(root.Find("BranchLever"));Assert.NotNull(root.Find("TrainThrottle"));
            Assert.IsNull(root.Find("ConfirmRoute"),"The branch lever itself confirms; no sixth control");
            Assert.GreaterOrEqual(((RectTransform)root.Find("BranchHousing")).sizeDelta.y,220);
            Assert.Greater(((RectTransform)root.Find("OperatorCard_op_kyle")).anchoredPosition.y,-800);
            var thumb=(RectTransform)root.Find("UltimateAuto/AutoThumb");
            Assert.AreEqual(new Vector2(14,14),thumb.sizeDelta);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [TestCase(1080,2400,0,100,1080,2180)]
    [TestCase(720,1280,0,0,720,1280)]
    [TestCase(1080,2340,30,90,1020,2160)]
    public void SafeAreaFitsWholeConsole(int w,int h,int x,int y,int sw,int sh)
    {
        var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatSafeArea");
        Assert.NotNull(type,"Uniform safe-area layout component required");
        var rect=(Rect)type.GetMethod("Fit").Invoke(null,new object[]{new Rect(x,y,sw,sh)});
        Assert.GreaterOrEqual(rect.xMin,x-.01f);Assert.GreaterOrEqual(rect.yMin,y-.01f);
        Assert.LessOrEqual(rect.xMax,x+sw+.01f);Assert.LessOrEqual(rect.yMax,y+sh+.01f);
        Assert.AreEqual(720f/1280,rect.width/rect.height,.0001f);
    }
}
