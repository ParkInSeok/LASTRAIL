#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

// Isolated diagnostic capture. No account admission or gameplay animation binding implied.
[InitializeOnLoad]
public static class ConnectedCombatCapture
{
    const string Key="LastRail.ConnectedCombatCapture";
    public const string Output="D:/Project/LASTRAIL/unity-project/QA/ScreenIntegration/combat-hud-20260911-v04";
    static ConnectedCombatCapture(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run(){
        if(!Application.isBatchMode||Directory.Exists(Output))throw new InvalidOperationException("Dedicated batch and fresh output required.");
        ApprovedCombatBuilder.Build();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state){
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){
            try{new GameObject("DiagnosticCapture").AddComponent<ConnectedCombatRecorder>().Begin();}
            catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/failed.txt",e.ToString());EditorApplication.ExitPlaymode();}
        }
        if(state==PlayModeStateChange.EnteredEditMode){
            SessionState.SetBool(Key,false);Time.captureFramerate=0;
            EditorApplication.Exit(File.Exists(Output+"/complete.json")&&!File.Exists(Output+"/failed.txt")?0:1);
        }
    }
}
public sealed class ConnectedCombatRecorder:MonoBehaviour
{
    Camera cam;RenderTexture target;Texture2D pixels;CombatSceneDriver driver;ApprovedCombatView binding;int frame,fullAt=-1;StreamWriter trace;
    public void Begin(){
        Directory.CreateDirectory(ConnectedCombatCapture.Output);
        Time.captureFramerate=24;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab");
        var original=Instantiate(prefab);original.SetActive(false);
        var hud=new GameObject("ApprovedCombat",typeof(RectTransform),typeof(Canvas));
        BasicCombatSimulation Fresh(){
            var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));
            d["ownedSelection"]["accountId"]="a";
            var sim=BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);sim.ConfigureLocalLeaderCharge(4);return sim;
        }
        driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
        driver.SetSpeed(3);binding=hud.AddComponent<ApprovedCombatView>();binding.Bind(driver,UnityEngine.Resources.Load<CombatPresentationAssets>("CombatPresentation/approved"));
        cam=new GameObject("DiagnosticCamera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
        target=new RenderTexture(720,1280,24);cam.targetTexture=target;
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        var mark=new GameObject("DiagnosticLabel",typeof(RectTransform));mark.transform.SetParent(hud.transform,false);
        var rect=mark.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(0,28);rect.anchoredPosition=new Vector2(0,-66);
        var label=mark.AddComponent<TextMeshProUGUI>();var style=hud.GetComponentInChildren<TMP_Text>();if(style!=null)label.font=style.font;
        label.fontSize=18;label.color=Color.yellow;label.alignment=TextAlignmentOptions.Center;label.text="LOCAL QA - LIVE COMBAT MODEL / NO SERVER RUN";label.raycastTarget=false;
        pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);
        trace=new StreamWriter(ConnectedCombatCapture.Output+"/telemetry.jsonl");
    }
    void LateUpdate(){
        if(driver==null)return;
        try{
            // Exact 24fps media clock -> 50ms simulation steps; independent visual playback is diagnostic only.
            long current=(frame+1)*10000000L/24,previous=frame*10000000L/24;
            if(frame==144)driver.SetPaused(true);if(frame==168)driver.SetPaused(false);if(frame==240)driver.SetSpeed(4);if(frame==264)driver.SetSpeed(3);
            driver.Advance(current-previous,8);
            var state=driver.HudState;var ultimate=state["ultimate"];
            if((int)ultimate["charge"]<100)fullAt=-1;else if(fullAt<0)fullAt=frame;
            if((bool)ultimate["enabled"]&&fullAt>=0&&frame-fullAt>=24){driver.RequestSkill("qa-ultimate-"+frame,(string)ultimate["actor"],(string)ultimate["ability"]);fullAt=-1;}
            if(frame%96==0){var row=state["skills"][frame/96%5];if((bool)row["enabled"])driver.RequestSkill("qa-active-"+frame,(string)row["actor"],(string)row["ability"]);}
            binding.Refresh();
            Canvas.ForceUpdateCanvases();cam.Render();
            var old=RenderTexture.active;RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();RenderTexture.active=old;
            File.WriteAllBytes(ConnectedCombatCapture.Output+"/frame-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());
            if(frame%24==0)trace.WriteLine(driver.StateJson);
            if(++frame>=480){
                trace.Dispose();trace=null;
                File.WriteAllText(ConnectedCombatCapture.Output+"/counts.json",new JObject{["entrances"]=binding.EntrancePresentations,["hits"]=binding.HitPresentations,["downs"]=binding.DownPresentations,["allyDowns"]=binding.AllyDownPresentations,["recoveries"]=binding.RecoveryPresentations,["ultimates"]=binding.UltimatePresentations}.ToString());
                File.WriteAllText(ConnectedCombatCapture.Output+"/complete.json","{\"frames\":480,\"fps\":24,\"playMode\":true,\"serverRun\":false,\"actorAnimationBound\":true,\"audio\":false}");
                driver=null;EditorApplication.ExitPlaymode();
            }
        }catch(Exception e){File.WriteAllText(ConnectedCombatCapture.Output+"/failed.txt",e.ToString());driver=null;trace?.Dispose();trace=null;EditorApplication.ExitPlaymode();}
    }
    void OnDestroy(){trace?.Dispose();if(target!=null)target.Release();if(pixels!=null)Destroy(pixels);}
}
#endif

