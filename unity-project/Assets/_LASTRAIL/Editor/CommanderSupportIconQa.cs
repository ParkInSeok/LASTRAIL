#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CommanderSupportIconQa
{
    const string Input="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation.json";
    public static void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
            foreach(var path in Directory.GetFiles("Assets/_LASTRAIL/Content/RemoteResources/CommanderSupportIcons","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
            }
            Directory.CreateDirectory(output);var cases=new JArray();
            foreach(int height in new[]{1280,1600})foreach(string state in new[]{"ready","cooldown","disabled"})Capture(output,height,state,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual ApprovedCombatView commander support only. Derived local fixtures set skillReady or paused through model; no HUD spoof, HTTP, START or economic action. Memory checkpoint saves include Open and accepted local command only.",["input"]=Input,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Capture(string output,int height,string state,JArray cases)
    {
        var preparation=new Ce03RailPreparation(Ce03NormalUiCapture.Read(Input),"a",1);
        BasicCombatSimulation Fresh()=>preparation.CreateModel(1);
        BasicCombatSimulation Fixture()
        {
            var raw=JObject.Parse(Fresh().CaptureLocalState());foreach(var row in (JArray)raw["skillReady"])row["ready"]=state=="cooldown"?120:0;
            var model=BasicCombatSimulation.RestoreLocalState(raw.ToString(),Fresh);if(state=="disabled")model.SetPaused(true);return model;
        }
        int saves=0;var driver=new CombatSceneDriver(Fixture,()=>null,(s,r)=>{saves++;return r+1;},()=>true);driver.Open();
        var root=new GameObject("SupportIconsQA",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var cameraObject=new GameObject("SupportIconsCamera",typeof(Camera));var events=new GameObject("SupportIconsEvents",typeof(EventSystem));
        var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var view=root.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));view.SetCapturePresentationClock(()=>0);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            int captures=0;var geometry=new JArray();string prefix="support-"+height+"-"+state;
            void Draw(){Canvas.ForceUpdateCanvases();view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();}
            void Record(string suffix){string before=driver.StateJson;Draw();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,prefix+"-"+suffix+".png"),pixels.EncodeToPNG());if(before!=driver.StateJson)throw new InvalidOperationException("Rendering mutated simulation");captures++;}
            Draw();var icons=new List<(RawImage image,Texture texture,Rect uv,Vector2 position,Vector2 size)>();
            for(int i=0;i<3;i++)
            {
                string id=(string)driver.HudState["manuals"][i]["ability"];var button=root.transform.Find("ApprovedBattle/Command"+i).GetComponent<Button>();var icon=button.transform.Find("SkillIcon").GetComponent<RawImage>();
                if(!CommanderSupportIconCatalog.TryGet(id,out var resource,out var uv)||icon.texture!=LastRail.Resources.PreparedContent.Load<Texture2D>(resource)||icon.uvRect!=uv)throw new InvalidOperationException("Shared support mapping mismatch");
                var texture=(Texture2D)icon.texture;var r=icon.rectTransform;icons.Add((icon,texture,uv,r.anchoredPosition,r.sizeDelta));
                geometry.Add(new JObject{["index"]=i,["id"]=id,["resource"]=resource,["textureWidth"]=texture.width,["textureHeight"]=texture.height,["mipmapCount"]=texture.mipmapCount,["format"]=texture.format.ToString(),["filter"]=texture.filterMode.ToString(),["uv"]=new JArray(uv.x,uv.y,uv.width,uv.height),["rect"]=new JArray(r.anchoredPosition.x,r.anchoredPosition.y,r.sizeDelta.x,r.sizeDelta.y),["enabled"]=button.interactable,["tint"]=icon.color.ToString(),["cooldownMs"]=driver.HudState["manuals"][i]["cooldownMs"]});
            }
            Record("current");
            for(int i=0;i<3;i++){icons[i].image.enabled=false;Record("hide"+i);icons[i].image.enabled=true;Record("restore"+i);}
            // Same scene/state with the original pre55 bindings, for a scoped comparison.
            for(int i=0;i<3;i++){var item=icons[i];item.image.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/"+item.texture.name);item.image.uvRect=new Rect(0,0,1,1);item.image.rectTransform.anchoredPosition=new Vector2(4,-3);item.image.rectTransform.sizeDelta=new Vector2(60,60);}
            Record("original-binding");
            foreach(var item in icons){item.image.texture=item.texture;item.image.uvRect=item.uv;item.image.rectTransform.anchoredPosition=item.position;item.image.rectTransform.sizeDelta=item.size;}
            Record("binding-restored");
            int beforeSaves=saves;string beforeState=driver.StateJson;var first=root.transform.Find("ApprovedBattle/Command0").GetComponent<Button>();var rect=(RectTransform)first.transform;
            var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=first)throw new InvalidOperationException("First-hit support blocked");
            bool expectedEnabled=first.interactable;ExecuteEvents.Execute(first.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            if(expectedEnabled){if(saves!=beforeSaves+1||beforeState==driver.StateJson)throw new InvalidOperationException("Accepted support did not commit once");}
            else if(saves!=beforeSaves||beforeState!=driver.StateJson)throw new InvalidOperationException("Disabled support mutated model");
            if((state=="ready")!=expectedEnabled)throw new InvalidOperationException("Fixture did not represent requested state "+state);
            Record("input-result");
            cases.Add(new JObject{["height"]=height,["state"]=state,["captures"]=captures,["geometry"]=geometry,["nativeFirstHitAttempts"]=1,["acceptedInputs"]=expectedEnabled?1:0,["memorySaves"]=saves,["httpCalls"]=0,["renderPreservesModel"]=true});
        }
        finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);}
    }
}
#endif
