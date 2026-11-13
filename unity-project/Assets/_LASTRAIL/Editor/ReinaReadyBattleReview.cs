using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class ReinaReadyBattleReview
{
    public static void Run()
    {
        const string output = "D:/Project/LASTRAIL/LocalTemp/reina-ready-frames";
        Directory.CreateDirectory(output);
        var prepared = JObject.Parse(File.ReadAllText("QA/Unity1Artifacts/Fixtures/ce03-general-active-auto-hM29XX/prepared.json"));
        var contract = new Ce03CommandContract((JObject)prepared["commandContract"]);
        BasicCombatSimulation Fresh() => contract.CreateLocalTest(prepared,(string)prepared["ownedSelection"]["accountId"],contract.Revision,1);
        var initial = JObject.Parse(Fresh().CaptureLocalState());
        initial["operatorAutomation"]["charge"]["op_reina"] = 92;
        var driver = new CombatSceneDriver(()=>BasicCombatSimulation.RestoreLocalState(initial.ToString(),Fresh),()=>null,(s,r)=>r+1,()=>true);
        driver.Open();
        driver.SetUltimateAuto(false);
        var host=new GameObject("Battle review",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var camera=new GameObject("Review camera").AddComponent<Camera>();
        var target=new RenderTexture(960,540,24);
        var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.black;
            var canvas=host.GetComponent<Canvas>();
            var view=host.AddComponent<ApprovedCombatView>();
            view.Bind(driver,Resources.Load<CombatPresentationAssets>("CombatPresentation/approved"));
            view.UseLandscapeLayout();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;
            canvas.planeDistance=1;
            double clock=0;
            view.SetCapturePresentationClock(()=>clock);
            var card=host.transform.Find("ApprovedBattle/OperatorCard_op_reina");
            var button=card.Find("Ultimate_op_reina").GetComponent<Button>();
            var wash=card.Find("ReadyWash").GetComponent<Image>();
            int ready=-1, spent=-1;
            for(int f=0;f<400;f++)
            {
                clock=f*.05;
                if(ready>=0 && f==ready+32) button.onClick.Invoke();
                driver.Advance(500000,1);
                view.Refresh();
                var row=((JArray)driver.HudState["ultimates"]).Single(x=>(string)x["actor"]=="op_reina");
                if(ready<0 && wash.color.a>0 && (int)row["charge"]==100) ready=f;
                if(ready>=0 && f>=ready+32 && spent<0 && (int)row["charge"]<100 && wash.color.a==0) spent=f;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,960,540),0,0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(output,$"frame-{f:D3}.png"),pixels.EncodeToPNG());
                if(spent>=0 && f==spent+26)
                {
                    File.WriteAllText(Path.Combine(output,"range.txt"),$"{ready} {spent} {f}");
                    return;
                }
            }
            throw new InvalidOperationException($"Charge transition missing: ready={ready}, spent={spent}, state={driver.HudState["ultimates"]}");
        }
        finally
        {
            RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
