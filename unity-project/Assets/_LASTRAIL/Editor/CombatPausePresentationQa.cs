#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class CombatPausePresentationQa
{
    const string Input="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation.json";
    public static void Run()
    {
        try{
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!Application.isBatchMode||!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");Directory.CreateDirectory(output);
            var cases=new JArray();foreach(int height in new[]{1280,1600})Capture(output,height,cases);
            File.WriteAllText(output+"/verification.json",new JObject{["scope"]="28 central pause popup only on existing actual ApprovedCombatView. Preserved synthetic local rail preparation, memory checkpoint writes and injected local save failure; zero HTTP/START/reward/termination. Settings/lobby host unavailable and disabled, no actual navigation claimed.",["input"]=Input,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Capture(string output,int height,JArray cases)
    {
        var prep=new Ce03RailPreparation(Ce03NormalUiCapture.Read(Input),"a",1);bool fail=false;int writes=0;string stored=null;
        var driver=new CombatSceneDriver(()=>prep.CreateModel(1),()=>null,(s,r)=>{writes++;if(fail)throw new InvalidOperationException("Injected local QA failure");stored=s.CaptureLocalState();return r+1;},()=>true);driver.Open();
        var root=new GameObject("PauseQA",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var cameraObject=new GameObject("PauseCamera",typeof(Camera));var events=new GameObject("PauseEvents",typeof(EventSystem));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{
            var camera=cameraObject.GetComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var view=root.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.SetCapturePresentationClock(()=>0);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var controller=root.GetComponent<CombatPauseController>();int captures=0,clicks=0;var overflow=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();view.Refresh();controller.Refresh();Canvas.ForceUpdateCanvases();camera.Render();}
            Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
            List<RaycastResult> Hits(Button b,out PointerEventData pointer){var r=(RectTransform)b.transform;pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);return hits;}
            void Click(string name){Draw();var b=Find(name);var hits=Hits(b,out var pointer);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked pointer "+name);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);clicks++;}
            void Record(string name){string state=driver.StateJson;Draw();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.GetComponentInParent<CombatPauseView>()!=null&&text.isTextOverflowing)overflow.Add(name+":"+text.name);}camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(output+"/pause-"+height+"-"+name+".png",pixels.EncodeToPNG());if(state!=driver.StateJson)throw new InvalidOperationException("Rendering changed simulation");captures++;}
            Record("before");Click("Pause");if(!(bool)driver.HudState["paused"]||stored!=driver.StateJson)throw new InvalidOperationException("Pause not committed");Record("open");
            var medallion=root.GetComponentsInChildren<RectTransform>().SingleOrDefault(r=>r.name=="PauseMetalFace");if(medallion==null)throw new InvalidOperationException("Independent pause metal face is missing");
            foreach(var name in new[]{"PauseMetalFace","PauseBevel","PauseBars","PauseTitleOrnaments","PauseTitleDiamond"}){var layer=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name==name);layer.gameObject.SetActive(false);Record("hide-"+name);layer.gameObject.SetActive(true);Record("restore-"+name);}
            var metal=medallion.GetComponent<RawImage>();var borrowed=metal.texture;metal.texture=Texture2D.whiteTexture;Record("swap-PauseMetalFace");metal.texture=borrowed;Record("restore-metal-texture");
            string paused=driver.StateJson;driver.Advance(10000000,8);if(paused!=driver.StateJson)throw new InvalidOperationException("Paused simulation advanced");Draw();var behind=Find("Speed");var blocked=Hits(behind,out _);if(blocked.Count==0||blocked[0].gameObject.GetComponentInParent<Button>()==behind)throw new InvalidOperationException("Modal permits underlying input");
            var popup=root.GetComponentInChildren<CombatPauseView>();if(Find("PauseSettings").interactable||Find("PauseLobby").interactable)throw new InvalidOperationException("Unbound host appears usable");var surface=popup.GetComponentsInChildren<IndependentMetalSurface>().Single();surface.Frame.enabled=false;Record("hide-frame");surface.Frame.enabled=true;var texture=surface.Frame.texture;surface.Frame.texture=Texture2D.whiteTexture;Record("swap-frame");surface.Frame.texture=texture;Record("restore-frame");surface.Lights.gameObject.SetActive(false);Record("hide-lights");surface.Lights.gameObject.SetActive(true);Record("restore-lights");surface.Backplate.enabled=false;Record("hide-backplate");surface.Backplate.enabled=true;Record("restore-backplate");var emblem=popup.GetComponentInChildren<PauseEmblemGraphic>();emblem.enabled=false;Record("hide-rim");emblem.enabled=true;Record("restore-rim");
            Click("PauseClose");if(!(bool)driver.HudState["paused"]||controller.IsOpen)throw new InvalidOperationException("Close changed pause");Record("closed-still-paused");int before=writes;Click("Pause");if(writes!=before)throw new InvalidOperationException("Reopen added state transition");Record("already-paused-reopened");Click("PauseContinue");if((bool)driver.HudState["paused"]||controller.IsOpen)throw new InvalidOperationException("Continue failed");Record("resumed");
            Click("Pause");fail=true;Click("PauseContinue");if(!driver.RecoveryRequired||!(bool)driver.HudState["paused"]||!controller.IsOpen||Find("PauseContinue").interactable)throw new InvalidOperationException("Save failure not preserved");Record("resume-save-failed");Click("PauseClose");if(!driver.RecoveryRequired||!(bool)driver.HudState["paused"])throw new InvalidOperationException("Recovery close resumed");Record("closed-recovery");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["pointerClicks"]=clicks,["memorySaveAttempts"]=writes,["saveFailures"]=1,["httpCalls"]=0,["modalBlocksUnderlyingInput"]=true,["closePreservesPause"]=true,["alreadyPausedReopenNoSave"]=true,["continueCommitsResume"]=true,["failedResumeKeepsPause"]=true,["settingsHostConnected"]=false,["lobbyHostConnected"]=false,["overflow"]=overflow});
        }finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);}
    }
}
#endif
