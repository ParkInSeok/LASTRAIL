#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class SupportSelectionQa
{
    sealed class Wire:IRunApiHttpTransport
    {
        public bool Lose;public int Posts;public string FirstBody,FirstKey;public bool SameRetry=true;
        readonly string[] ids={"support_fire","barrier","emergency_repair","ce03.role.suppressive_barrage","ce03.role.medical_support","ce03.role.smoke","ce03.role.weakness_scan","ce03.role.firepower_boost","ce03.role.electronic_disruption","ce03.role.emergency_protection"};
        JObject selection=new JObject{["rulesVersion"]="ce03-support-selection-v1",["catalogVersion"]="ce03-local-support-roles-v1",["revision"]=0,["slots"]=new JArray()};
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
        {
            if(r.Method=="GET")return Task.FromResult(RunApiHttpResponse.Success(200,new JObject{["selection"]=selection.DeepClone(),["catalog"]=new JObject{["scope"]="LOCAL_SELECTION_ONLY",["entries"]=new JArray(ids.Select((id,i)=>new JObject{["skillId"]=id,["executionAvailable"]=i<3}))}}.ToString()));
            Posts++;string key=r.Headers.Single(h=>h.Name=="Idempotency-Key").Value;
            if(FirstBody==null){FirstBody=r.BodyJson;FirstKey=key;}else SameRetry&=FirstBody==r.BodyJson&&FirstKey==key;
            if(Lose)return Task.FromResult(RunApiHttpResponse.NetworkError("LOCAL QA lost save response"));
            selection["revision"]=1;selection["slots"]=JObject.Parse(r.BodyJson)["slots"].DeepClone();return Task.FromResult(RunApiHttpResponse.Success(200,selection.ToString()));
        }
    }
    public static void Run()
    {
        var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
        if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");Directory.CreateDirectory(output);
        foreach(string name in new[]{"support-room","support-host"}){var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatSupport/"+name+".png");importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.alphaIsTransparency=name.EndsWith("host");importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();}
        var cases=new JArray();foreach(int height in new[]{1280,1600})Capture(output,height,cases);
        File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Native support panel / in-memory HTTP transport / saved order and same-key lost response retry; no real START or grant",["cases"]=cases}.ToString());EditorApplication.Exit(0);
    }
    static void Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("SupportQa",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        try{
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var wire=new Wire();string journal=null;
            var client=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"qa",wire,new GameApiAccessToken("lst1.fixture"),s=>journal=s);var panel=Ce03SupportSelectionPanel.Open(root.transform,client,()=>{});
            Button B(string name)=>panel.GetComponentsInChildren<Button>().Single(x=>x.name==name);
            void Click(string name){var b=B(name);if(!b.interactable)throw new InvalidOperationException("Disabled "+name);ExecuteEvents.Execute(b.gameObject,new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
            void Record(string name){Canvas.ForceUpdateCanvases();foreach(var label in panel.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing)throw new InvalidOperationException("Overflow "+label.name);}camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"support-"+height+"-"+name+".png"),pixels.EncodeToPNG());}
            Record("empty");Click("Choice0");Record("incomplete");Click("Choice1");Click("Choice2");Record("selected");Click("MoveLeft2");Record("reordered");wire.Lose=true;Click("Save");Record("pending");
            if(!client.HasPending||journal==null||B("RemoveSlot0").interactable)throw new InvalidOperationException("Pending selection guard");wire.Lose=false;Click("Save");Record("recovered");
            if(client.HasPending||wire.Posts!=2||!wire.SameRetry)throw new InvalidOperationException("Save replay mismatch");
            foreach(var name in new[]{"SupportRoom","SupportHost"}){var image=panel.GetComponentsInChildren<RawImage>().Single(x=>x.name==name);if(image.raycastTarget)throw new InvalidOperationException("Decorative input");var saved=image.texture;image.texture=Texture2D.whiteTexture;Record("swap-"+name);image.texture=saved;}
            Record("restored");cases.Add(new JObject{["height"]=height,["captures"]=9,["sameKeyBodyRetry"]=true,["posts"]=2,["pendingCleared"]=true,["decorativeInput"]=false,["textOverflow"]=false});
        }finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
