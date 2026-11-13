#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Auth;
using LastRail.Resources;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class ConsentMaterialQa
{
    static bool comparisonOnly;
    public static void Compare(){comparisonOnly=true;Run();}
    public static void Run()
    {
        try{
            string output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA directory required");Directory.CreateDirectory(output);
            var cases=new JArray();foreach(int height in new[]{1280,1600})Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="08 actual StartupDownloadOverlay with local80MiB display and native first-hit callback counters only. No coordinator download, policy change, HTTP or storage.",["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("ConsentMaterialQa",typeof(RectTransform));var cameraObject=new GameObject("Camera",typeof(Camera));var events=new GameObject("Events",typeof(EventSystem));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{
            int approve=0,defer=0;var overlay=root.AddComponent<StartupDownloadOverlay>();overlay.Initialize(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),()=>approve++,()=>defer++,()=>throw new InvalidOperationException("Unexpected retry"));overlay.Show(true,80L*1024*1024,false);
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1080,1080f*height/720);
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1080f*height/720/2;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.018f,.02f);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            rect.position=Vector3.zero;rect.localScale=Vector3.one;
            var surfaces=root.GetComponentsInChildren<IndependentMetalSurface>();var lights=surfaces.Select(s=>s.Lights.gameObject.activeSelf).ToArray();var overflow=new JArray();int captures=0;
            void Draw(){Canvas.ForceUpdateCanvases();camera.Render();var panel=root.GetComponentsInChildren<RectTransform>().Single(x=>x.name=="ConsentPanel");var corners=new Vector3[4];panel.GetWorldCorners(corners);foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(camera,c);if(p.x<0||p.x>720||p.y<0||p.y>height)throw new InvalidOperationException("Consent outside capture viewport "+p);}}
            void Save(string name){Draw();foreach(var t in root.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(name+":"+t.name);}RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(output+"/consent-"+height+"-"+name+".png",pixels.EncodeToPNG());captures++;}
            Save("composed");
            var amountGeometry=new JArray();
            void VerifyAmount(long bytes)
            {
                var number=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="ConsentMessage");var unit=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="ConsentUnit");number.ForceMeshUpdate(true);unit.ForceMeshUpdate(true);
                if(number.text!=(bytes/1048576d).ToString("0.0")||unit.text!="MiB")throw new InvalidOperationException("Consent amount digits/unit lost");
                float baselineGap=Mathf.Abs(number.rectTransform.anchoredPosition.y+number.textInfo.lineInfo[0].baseline-unit.rectTransform.anchoredPosition.y-unit.textInfo.lineInfo[0].baseline);
                float gap=unit.rectTransform.anchoredPosition.x-number.rectTransform.anchoredPosition.x-number.textBounds.max.x;
                if(baselineGap>.01f||gap<11.99f||unit.rectTransform.anchoredPosition.x+unit.textBounds.max.x>735.1f)throw new InvalidOperationException("Consent number/unit fit or baseline");
                amountGeometry.Add(new JObject{["bytes"]=bytes,["number"]=number.text,["numberFont"]=number.fontSize,["unitFont"]=unit.fontSize,["baselineDistance"]=baselineGap,["inkGap"]=gap});
            }
            VerifyAmount(80L*1024*1024);
            var accent=root.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="ConsentHeadingAccent");
            var eyebrow=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="ConsentEyebrow");var heading=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="ConsentTitle");
            eyebrow.ForceMeshUpdate(true);heading.ForceMeshUpdate(true);
            float eyebrowGap=eyebrow.rectTransform.anchoredPosition.y+eyebrow.textBounds.min.y-(accent.anchoredPosition.y+1.5f);
            float titleGap=accent.anchoredPosition.y-1.5f-heading.rectTransform.anchoredPosition.y-heading.textBounds.max.y;
            if(eyebrowGap<12||titleGap<12)throw new InvalidOperationException("Accent glyph clearance "+eyebrowGap+" / "+titleGap);
            amountGeometry.Add(new JObject{["eyebrowAccentGap"]=eyebrowGap,["accentTitleGap"]=titleGap});
            foreach(var name in new[]{"ConsentMessage","ConsentUnit","ConsentHeadingAccent"}){var item=root.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name==name);item.gameObject.SetActive(false);Save("hide-"+name);item.gameObject.SetActive(true);Save("restore-"+name);}
            overlay.Show(true,long.MaxValue,false);Save("largest-amount");VerifyAmount(long.MaxValue);overlay.Show(true,0,false);Save("zero-amount");VerifyAmount(0);overlay.Show(true,80L*1024*1024,false);Save("amount-restored");VerifyAmount(80L*1024*1024);
            if(comparisonOnly){var primary=surfaces.Single(s=>s.transform.parent.name=="ApproveDownload");primary.EmphasizePrimary(2.7f);Save("legacy-intensity");primary.EmphasizePrimary(1.25f);Save("intensity-restored");cases.Add(new JObject{["height"]=height,["comparisonOnly"]=true,["currentIntensity"]=1.25f,["legacyIntensity"]=2.7f,["inputActions"]=0});return;}
            foreach(var role in new[]{"frame","plate","lights"}){foreach(var s in surfaces){if(role=="frame")s.Frame.enabled=false;else if(role=="plate")s.Backplate.enabled=false;else s.Lights.gameObject.SetActive(false);}Save("hide-"+role);for(int i=0;i<surfaces.Length;i++){surfaces[i].Frame.enabled=true;surfaces[i].Backplate.enabled=true;surfaces[i].Lights.gameObject.SetActive(lights[i]);}Save("restore-"+role);}
            foreach(var role in new[]{"frame","plate"}){var original=surfaces.Select(s=>role=="frame"?s.Frame.texture:s.Backplate.texture).ToArray();foreach(var s in surfaces){if(role=="frame")s.Frame.texture=Texture2D.whiteTexture;else s.Backplate.texture=Texture2D.whiteTexture;}Save("swap-"+role);for(int i=0;i<surfaces.Length;i++){if(role=="frame")surfaces[i].Frame.texture=original[i];else surfaces[i].Backplate.texture=original[i];}Save("restored-"+role);}
            foreach(var name in new[]{"ApproveDownload","DeferDownload"}){Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);var r=(RectTransform)b.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("First hit mismatch "+name);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
            if(approve!=1||defer!=1||overflow.Count!=0)throw new InvalidOperationException("Callback/overflow contract "+overflow);
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["amountGeometry"]=amountGeometry,["surfaceCount"]=surfaces.Length,["approveCallbacks"]=approve,["deferCallbacks"]=defer,["nativeFirstHitClicks"]=2,["displayBytes"]=80L*1024*1024,["overflow"]=overflow});
        }finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);}
    }
}
#endif
