#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class OfflineSupplyPresentationQa
{
    const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914/BasicPreview01/preview.json";
    sealed class Wire:IRunApiHttpTransport
    {
        public string Mode="ready";public int Reads,Writes;public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method!="GET"){Writes++;throw new InvalidOperationException("No writes in offline presentation capture");}Reads++;await Gate.Task;
            if(Mode=="missing")return RunApiHttpResponse.Success(503,"{}");var json=JObject.Parse(File.ReadAllText(Source));
            if(Mode=="empty"){json["previewStatus"]="EMPTY";json["claimableSeconds"]=0;json["claimableHours"]=0;json["rewardLines"]=new JArray();json["calculation"]["cappedOfflineSeconds"]=0;}
            return RunApiHttpResponse.Success(200,json.ToString());
        }
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA directory required");Directory.CreateDirectory(output);
            foreach(var name in new[]{"room-v01","chest-v01"})
            {
                var importer=AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatOffline/"+name+".png") as TextureImporter;if(importer==null)continue;
                importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            }
            var cases=new JArray();foreach(var h in new[]{1280,1600})await Capture(output,h,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="24 native basic supply and existing27 roulette navigation. Preserved normal GET preview4rewards; derived EMPTY response explicitly synthetic. No POST/ad presentation/spin/claim or real HTTP.",["input"]=Source,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("OfflineSupplyQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
            int captures=0,clicks=0;var overflow=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
            void Record(string name)
            {
                Draw();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing&&text.GetComponentInParent<OfflineSupplyView>()!=null)overflow.Add(name+":"+text.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"offline-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;
            }
            void Click(string name)
            {
                Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);var r=(RectTransform)b.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked pointer "+name+": "+string.Join(",",hits.Select(h=>h.gameObject.name)));ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);clicks++;
            }
            async Task Settle(){wire.Gate.TrySetResult(true);for(int i=0;account.OfflineLoading;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}await Task.Delay(1);}
            async Task Refresh(string mode){wire.Mode=mode;wire.Gate=new TaskCompletionSource<bool>();Click("RefreshOffline");await Settle();}
            Click("Shortcut3");Record("loading");await Settle();var view=root.GetComponentInChildren<OfflineSupplyView>();if(view?.CurrentState.Rewards.Count!=4||view.CurrentState.TimeText!="04:00:00")throw new InvalidOperationException("Supply preview mapping failed");Record("available");
            var header=view.transform.Find("SupplyHeaderPlate");if(header==null)throw new InvalidOperationException("Independent supply header plate is missing");
            var headerSurface=header.GetComponentInChildren<IndependentMetalSurface>();
            foreach(var graphic in new Graphic[]{headerSurface.Backplate,headerSurface.Frame,view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="SupplyTitle")}){string name=graphic==headerSurface.Backplate?"header-plate":graphic==headerSurface.Frame?"header-frame":"header-title";graphic.enabled=false;Record("hide-"+name);graphic.enabled=true;Record("restore-"+name);}
            foreach(var name in new[]{"SupplyCloseGlyph","SupplyHeadingOrnaments"}){var layer=view.GetComponentsInChildren<RectTransform>().Single(r=>r.name==name);layer.gameObject.SetActive(false);Record("hide-"+name);layer.gameObject.SetActive(true);Record("restore-"+name);}
            var knownSymbols=new[]{"cur_tactical_data","cur_operator_exp"}.Select(id=>view.GetComponentsInChildren<RewardResourceSymbol>().Single(g=>g.Kind==id)).ToArray();
            foreach(var symbol in knownSymbols){if(symbol.canvasRenderer.GetMesh()==null||symbol.canvasRenderer.GetMesh().vertexCount==0)throw new InvalidOperationException("Known reward glyph is blank: "+symbol.Kind);symbol.enabled=false;Record("hide-"+symbol.Kind);symbol.enabled=true;Record("restore-"+symbol.Kind);}
            var materialPlates=knownSymbols.Select(g=>g.transform.parent.GetComponentInChildren<IndependentMetalSurface>().Backplate).ToArray();var textures=materialPlates.Select(p=>p.texture).ToArray();var colors=materialPlates.Select(p=>p.color).ToArray();foreach(var plate in materialPlates){plate.texture=Texture2D.whiteTexture;plate.color=Color.white;}Record("known-glyphs-white-plates");for(int i=0;i<materialPlates.Length;i++){materialPlates[i].texture=textures[i];materialPlates[i].color=colors[i];}Record("restore-known-glyph-plates");
            var probe=knownSymbols[0];string knownKind=probe.Kind;probe.Kind="unrecognized-item-fixture";probe.SetVerticesDirty();Record("unknown-symbol-blank");if(probe.canvasRenderer.GetMesh()?.vertexCount>0)throw new InvalidOperationException("Unknown reward invented a glyph");probe.Kind=knownKind;probe.SetVerticesDirty();Record("restore-known-after-unknown");
            var art=new JObject();foreach(var name in new[]{"SupplyRoom","SupplyChest"})
            {
                var image=view.GetComponentsInChildren<RawImage>().Single(x=>x.name==name);art[name]=image.texture!=null;if(image.texture==null)continue;
                var original=image.texture;image.enabled=false;Record("hide-"+name);image.enabled=true;image.texture=Texture2D.whiteTexture;Record("swap-"+name);image.texture=original;Record("restore-"+name);
            }
            await Refresh("missing");if(view.CurrentState.CanClaim||view.CurrentState.Rewards.Count!=0)throw new InvalidOperationException("Failed preview retained claim/rewards");Record("unavailable");
            await Refresh("empty");if(view.CurrentState.CanClaim||!view.CurrentState.HasPreview||view.CurrentState.Rewards.Count!=0)throw new InvalidOperationException("Empty preview mapping failed");Record("empty-derived");
            await Refresh("ready");Record("refreshed");Click("OpenOfflineRoulette");await Settle();if(root.GetComponentInChildren<OfflineSupplyView>()!=null)throw new InvalidOperationException("Basic view survived roulette transition");
            if(root.GetComponentsInChildren<Button>().Single(b=>b.name=="AdMainAction").interactable)throw new InvalidOperationException("Missing ad provider enabled");Record("roulette27-unavailable-provider");Click("CloseLobbyDialog");Record("roulette-return");
            Click("Shortcut3");await Settle();Record("reentry");session=null;account.SynchronizeSession();Record("owner-cleared");if(root.GetComponentInChildren<OfflineSupplyView>().CurrentState.Rewards.Count!=0)throw new InvalidOperationException("Owner rewards retained");
            Click("CloseOfflineSupply");Record("lobby-return");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Return or write boundary failed");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["reads"]=wire.Reads,["writes"]=wire.Writes,["pointerClicks"]=clicks,["artPresent"]=art,["emptyDistinct"]=true,["providerUnavailable"]=true,["roulette27EntryReturn"]=true,["ownerCleared"]=true,["lobbyReturn"]=true,["overflow"]=overflow});
        }
        finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif

