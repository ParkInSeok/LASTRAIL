#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Candidate texture substitution only. Production routing/layout remains authoritative.
public static class PreparationArtCandidateQa
{
    sealed class Wire:IRunApiHttpTransport
    {
        readonly JObject input=JObject.Parse(File.ReadAllText("QA/NoncombatFinal-20260915/Navigation13/synthetic-inputs-1280.json"));
        public int Reads,Writes;
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
        {
            if(r.Method!="GET"){Writes++;throw new InvalidOperationException("Candidate QA forbids writes");}
            Reads++;var prep=input["preparation"];
            var data=r.EndpointPath=="/v1/account/state"?prep["account"]:r.EndpointPath=="/v1/trains/loadout"?prep["train"]:r.EndpointPath=="/v1/formation/ultimate"?prep["ultimate"]:r.EndpointPath=="/__local/ce03/support-loadout"?input["support"]:null;
            return Task.FromResult(RunApiHttpResponse.Success(data==null?404:200,data?.ToString()??"{}"));
        }
    }
    sealed class Journal:IAccountActionJournal
    {
        public PendingAccountAction Read(string id)=>null;
        public bool Save(PendingAccountAction action)=>throw new InvalidOperationException("No journal write");
        public bool Remove(PendingAccountAction action)=>throw new InvalidOperationException("No journal removal");
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D output required");
            var manifest=JObject.Parse(File.ReadAllText("../QA/NoncombatFinal-20260915/Unity/ART52-CANDIDATE-MANIFEST.json"));
            foreach(JObject entry in manifest["assets"])
            {
                if(entry["fitBox"]!=null&&(!(entry["sourceUv"] is JArray requiredUv)||requiredUv.Count!=4))throw new InvalidOperationException("Explicit fitted box requires its source UV contract: "+entry["target"]);
                var path=LastRail.Resources.RemoteContentCatalog.AssetPath((string)entry["resource"]) ?? throw new InvalidOperationException("Unknown resource key");
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
            }
            Directory.CreateDirectory(output);var cases=new JArray();foreach(int height in new[]{1280,1600})await Capture(output,height,(JArray)manifest["assets"],cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="QA texture candidates on actual runtime51 View; production resource routing unchanged. Frozen Navigation13 memory GET fixture; no live network/START/economic writes.",["manifest"]=manifest,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray entries,JArray cases)
    {
        var root=new GameObject("PreparationArtQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var owner=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_01_last_departure","황무지 철로 · LOCAL QA",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(owner);
            deck.BindLocalCommandClient(new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"qa",wire,new GameApiAccessToken("lst1.fixture"),_=>throw new InvalidOperationException("No support save")));
            int count=0;var clicks=new JArray();var geometry=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate();camera.Render();}
            void Click(string name){Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);if(!b.interactable)throw new InvalidOperationException("Disabled "+name);var r=(RectTransform)b.transform;var p=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(p,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("First hit "+name);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);clicks.Add(name);}
            void Record(string name){Draw();foreach(var t in root.GetComponentsInChildren<TMP_Text>())if(t.GetComponentInParent<DeparturePreparationView>()!=null&&t.isTextOverflowing)throw new InvalidOperationException("Overflow "+t.name);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"art-"+height+"-"+name+".png"),pixels.EncodeToPNG());count++;}
            Click("Departure");Click("SelectDeparture0");await Task.Yield();Record("original");
            var view=root.GetComponentInChildren<DeparturePreparationView>();var originals=new List<(RawImage image,Texture texture,Rect uv,Color color,bool enabled,Vector2 position,Vector2 size)>();
            foreach(JObject entry in entries)
            {
                var image=view.GetComponentsInChildren<RawImage>().Single(x=>x.name==(string)entry["target"]);
                var texture=Resources.Load<Texture2D>((string)entry["resource"]);if(texture==null)throw new InvalidOperationException("Candidate resource missing "+entry["resource"]);
                var oldRect=image.rectTransform;originals.Add((image,image.texture,image.uvRect,image.color,image.enabled,oldRect.anchoredPosition,oldRect.sizeDelta));image.texture=texture;image.enabled=true;image.color=Color.white;image.uvRect=new Rect(0,0,1,1);
                if(entry["sourceUv"] is JArray uv)image.uvRect=new Rect((float)uv[0],(float)uv[1],(float)uv[2],(float)uv[3]);
                if(entry["fitBox"] is JArray dimensions){var nextBox=new Vector2((float)dimensions[0],(float)dimensions[1]);oldRect.anchoredPosition+=new Vector2((oldRect.sizeDelta.x-nextBox.x)/2,-((float?)entry["topOffset"]??0));oldRect.sizeDelta=nextBox;}
                if((bool?)entry["aspectFit"]==true){var box=oldRect.sizeDelta;float sourceWidth=texture.width*image.uvRect.width,sourceHeight=texture.height*image.uvRect.height;float scale=Mathf.Min(box.x/sourceWidth,box.y/sourceHeight);var fitted=new Vector2(sourceWidth*scale,sourceHeight*scale);oldRect.anchoredPosition+=new Vector2((box.x-fitted.x)/2,-(box.y-fitted.y)/2);oldRect.sizeDelta=fitted;}
                var r=image.rectTransform;geometry.Add(new JObject{["target"]=image.name,["resource"]=entry["resource"],["textureWidth"]=texture.width,["textureHeight"]=texture.height,["format"]=texture.format.ToString(),["filter"]=texture.filterMode.ToString(),["rect"]=new JArray(r.anchoredPosition.x,r.anchoredPosition.y,r.sizeDelta.x,r.sizeDelta.y),["uv"]=image.uvRect.ToString()});
            }
            Record("candidate");foreach(var item in originals){item.image.enabled=false;Record("hide-"+item.image.name);item.image.enabled=true;Record("restore-"+item.image.name);}
            Click("DepartureGuideEntry");Record("candidate-guide");Click("GuideClose");Record("candidate-guide-return");
            foreach(var item in originals){item.image.texture=item.texture;item.image.uvRect=item.uv;item.image.color=item.color;item.image.enabled=item.enabled;item.image.rectTransform.anchoredPosition=item.position;item.image.rectTransform.sizeDelta=item.size;}Record("original-restored");Click("BackDeparture");
            if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Close/write boundary");cases.Add(new JObject{["height"]=height,["captures"]=count,["reads"]=wire.Reads,["writes"]=wire.Writes,["nativeFirstHitClicks"]=clicks,["geometry"]=geometry});
        }
        finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif

