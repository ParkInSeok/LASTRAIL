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
public static class CombatDiagnosticCapture
{
    const string Key="LastRail.CombatDiagnosticCapture";
    public const string Output="D:/Project/LASTRAIL/unity-project/QA/ScreenIntegration/combat-diagnostic-20260911-v01";
    static CombatDiagnosticCapture(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run(){
        if(!Application.isBatchMode||Directory.Exists(Output))throw new InvalidOperationException("Dedicated batch and fresh output required.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    static void Changed(PlayModeStateChange state){
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode){
            try{new GameObject("DiagnosticCapture").AddComponent<CombatDiagnosticRecorder>().Begin();}
            catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/failed.txt",e.ToString());EditorApplication.ExitPlaymode();}
        }
        if(state==PlayModeStateChange.EnteredEditMode){
            SessionState.SetBool(Key,false);Time.captureFramerate=0;
            EditorApplication.Exit(File.Exists(Output+"/complete.json")&&!File.Exists(Output+"/failed.txt")?0:1);
        }
    }
}
public sealed class CombatDiagnosticRecorder:MonoBehaviour
{
    Camera cam;RenderTexture target;Texture2D pixels;CombatSceneDriver driver;CombatLiveHudBinding binding;int frame;StreamWriter trace;
    public void Begin(){
        Directory.CreateDirectory(CombatDiagnosticCapture.Output);
        Time.captureFramerate=24;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab");
        var hud=Instantiate(prefab);
        BasicCombatSimulation Fresh(){
            var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));
            d["ownedSelection"]["accountId"]="a";
            return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",1);
        }
        driver=new CombatSceneDriver(Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
        binding=hud.AddComponent<CombatLiveHudBinding>();binding.Bind(driver);
        cam=new GameObject("DiagnosticCamera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
        target=new RenderTexture(720,1280,24);cam.targetTexture=target;
        var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        var mark=new GameObject("DiagnosticLabel",typeof(RectTransform));mark.transform.SetParent(hud.transform,false);
        var rect=mark.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(0,75);rect.anchoredPosition=new Vector2(0,-8);
        var label=mark.AddComponent<TextMeshProUGUI>();var style=hud.GetComponentInChildren<TMP_Text>();if(style!=null)label.font=style.font;
        label.fontSize=18;label.color=Color.yellow;label.alignment=TextAlignmentOptions.Center;label.text="LOCAL QA - CURRENT PREFAB / TEST MODEL\nACTOR ANIMATION NOT BOUND - NO SERVER RUN";label.raycastTarget=false;
        pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);
        trace=new StreamWriter(CombatDiagnosticCapture.Output+"/telemetry.jsonl");
    }
    void LateUpdate(){
        if(driver==null)return;
        try{
            // Exact 24fps media clock -> 50ms simulation steps; independent visual playback is diagnostic only.
            long current=(frame+1)*10000000L/24,previous=frame*10000000L/24;
            driver.Advance(current-previous,8);
            Canvas.ForceUpdateCanvases();cam.Render();
            var old=RenderTexture.active;RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();RenderTexture.active=old;
            File.WriteAllBytes(CombatDiagnosticCapture.Output+"/frame-"+frame.ToString("D4")+".png",pixels.EncodeToPNG());
            if(frame%24==0)trace.WriteLine(driver.StateJson);
            if(++frame>=288){
                trace.Dispose();trace=null;
                File.WriteAllText(CombatDiagnosticCapture.Output+"/complete.json","{\"frames\":288,\"fps\":24,\"playMode\":true,\"serverRun\":false,\"actorAnimationBound\":false,\"audio\":false}");
                driver=null;EditorApplication.ExitPlaymode();
            }
        }catch(Exception e){File.WriteAllText(CombatDiagnosticCapture.Output+"/failed.txt",e.ToString());driver=null;trace?.Dispose();trace=null;EditorApplication.ExitPlaymode();}
    }
    void OnDestroy(){trace?.Dispose();if(target!=null)target.Release();if(pixels!=null)Destroy(pixels);}
}
#endif

