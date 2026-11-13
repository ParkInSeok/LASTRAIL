#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class DepartureGuideQa
{
    public static void Run()
    {
        var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
        if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
        Directory.CreateDirectory(output);var cases=new JArray();
        foreach(int height in new[]{1280,1600})Capture(output,height,cases);
        File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Native manual guide with explicit synthetic read-only view data; no HTTP, START, or grants",["cases"]=cases}.ToString());
        EditorApplication.Exit(0);
    }
    static void Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("GuideQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));
        var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;Sprite skin=null;
        try{
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var ids=JObject.Parse(File.ReadAllText("Assets/_LASTRAIL/Content/RemoteResources/OperatorIdentity/catalog.json"))["operators"].Take(5).Select(x=>(string)x["operatorId"]);
            var data=new DepartureGuideData("LOCAL QA · 작전 03 / 위험 I",ids,new[]{"1 · 전방 객차","2 · 중앙 객차","3 · 후방 객차"},new[]{"지원 사격","방어막","긴급 수리"},"소모 연료 10 · 보유 100 (QA 예시)");
            skin=LobbyArtwork.CreateFrame();
            var view=DepartureGuideView.Open(root.transform,font,skin,data,()=>true);
            for(int step=0;step<4;step++){
                Canvas.ForceUpdateCanvases();foreach(var label in view.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing)throw new InvalidOperationException("Text overflow: "+label.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"guide-"+height+"-step"+(step+1)+".png"),pixels.EncodeToPNG());
                if(view.Step!=step)throw new InvalidOperationException("Native step mismatch");
                var button=view.GetComponentsInChildren<Button>().Single(b=>b.name=="GuideNext");
                ExecuteEvents.Execute(button.gameObject,new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            }
            if(view!=null&&view.gameObject.activeSelf)throw new InvalidOperationException("Final next did not close");
            cases.Add(new JObject{["height"]=height,["steps"]=4,["nativeNextClicks"]=4,["closed"]=true,["textOverflow"]=false});
        }finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);if(skin!=null)UnityEngine.Object.DestroyImmediate(skin);}
    }
}
#endif
