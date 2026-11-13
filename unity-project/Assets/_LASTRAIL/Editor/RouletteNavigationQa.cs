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

// Local presentation/navigation evidence only. Never connects an HTTP transport or ad SDK.
public static class RouletteNavigationQa
{
    const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914/";
    static string Sample(string name)=>File.ReadAllText(Source+"DisplayMultipliers01/"+name+".json");
    sealed class Wire:IRunApiHttpTransport
    {
        public string Mode="pending";public int Reads,Spins;public readonly List<string> Keys=new List<string>();
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method=="POST" && request.EndpointPath.EndsWith("/spin"))
            {
                if(request.BodyJson!=null)throw new InvalidOperationException("Client reward body");
                Keys.Add(request.Headers.Single(h=>h.Name=="Idempotency-Key").Value);
                if(++Spins==1)return Task.FromResult(RunApiHttpResponse.NetworkError("Local lost spin response"));
                if(Spins!=2)throw new InvalidOperationException("Unexpected extra spin");
                Mode="claimed-restored";return Task.FromResult(RunApiHttpResponse.Success(200,Sample("spin-claimed")));
            }
            if(request.Method!="GET")throw new InvalidOperationException("Unexpected local mutation "+request.EndpointPath);
            Reads++;
            return Task.FromResult(request.EndpointPath=="/v1/offline-rewards/preview"
                ?RunApiHttpResponse.Success(200,File.ReadAllText(Source+"BasicPreview01/preview.json"))
                :request.EndpointPath.Contains("/ad-intents/")?RunApiHttpResponse.Success(200,Sample(Mode))
                :RunApiHttpResponse.Success(404,"{}"));
        }
    }
    sealed class Journal:IAccountActionJournal,IAccountAdActionJournal
    {
        public PendingAccountAction Row;
        public PendingAccountAction Read(string account)=>Row?.Account==account?Row:null;
        public bool Save(PendingAccountAction row){if(Row!=null)return false;Row=row;return true;}
        public bool Remove(PendingAccountAction row){if(Row!=row)return false;Row=null;return true;}
        public bool ReplaceAd(PendingAccountAction old,PendingAccountAction next){if(Row!=old)return false;Row=next;return true;}
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
            Directory.CreateDirectory(output);var cases=new JArray();
            foreach(int height in new[]{1280,1600})await Capture(output,height,cases);
            var pins=new JArray();foreach(var relative in new[]{"BasicPreview01/preview.json","DisplayMultipliers01/pending.json","DisplayMultipliers01/verified.json","DisplayMultipliers01/spin-claimed.json","DisplayMultipliers01/claimed-restored.json"})
            {using var sha=System.Security.Cryptography.SHA256.Create();pins.Add(new JObject{["path"]=Source+relative,["sha256"]=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Source+relative))).Replace("-","").ToLowerInvariant()});}
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="27 native first-hit navigation with frozen TEST_ONLY Sep14 fixtures, in-memory journal/transport only. Two mock spin POSTs replay one key; no HTTP, ad SDK, real grant or currently valid entitlement. Editor still frames do not verify PlayMode spin animation.",["sourcePins"]=pins,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("RouletteNavigationQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();var journal=new Journal();var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));long generation=1;using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>generation);
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var deck=LobbyCommandDeck.Attach(root,AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);
            var clicks=new JArray();var overflows=new JArray();int captures=0;
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
            Button Button(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
            void Click(string name)
            {
                Draw();var button=Button(name);if(!button.interactable)throw new InvalidOperationException("Disabled "+name);var rect=(RectTransform)button.transform;
                var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("First hit mismatch "+name);
                clicks.Add(name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            }
            async Task Settle(){for(int i=0;state.Busy;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}}
            void Record(string name)
            {
                Draw();var dialog=ButtonOrNull("CloseLobbyDialog")?.transform.parent;
                foreach(var label in root.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing&&dialog!=null&&label.transform.IsChildOf(dialog))overflows.Add(name+":"+label.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"roulette-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;
            }
            Button ButtonOrNull(string name)=>root.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name);
            async Task Open(){Click("Shortcut3");await Settle();Click("OpenOfflineRoulette");await Settle();}
            await Open();if(Button("AdMainAction").interactable||state.AdProviderAvailable)throw new InvalidOperationException("Missing provider enabled");Record("provider-unavailable");Click("CloseLobbyDialog");Record("provider-return");
            var pending=OfflineAdIntent.Parse(Sample("pending"));journal.Row=new PendingAccountAction("a","ad-presented",pending.Id,0,"local_original_key",pending.Json);
            // Advance the local session generation after seeding so the owner reloads the saved fixture.
            generation++;state.SynchronizeSession();await Open();
            if(state.AdIntent?.Status!="AD_PENDING"||state.CanSpinAd)throw new InvalidOperationException("Pending gate missing");Record("pending");
            wire.Mode="verified";Click("AdMainAction");await Settle();if(!state.CanSpinAd)throw new InvalidOperationException("Verified gate missing");Record("verified");
            Click("AdMainAction");await Settle();if(wire.Spins!=1||journal.Row?.Kind!="ad-spin"||state.AdIntent.Result!=null)throw new InvalidOperationException("Lost reply not retained");Record("spin-response-lost");
            Click("CloseLobbyDialog");await Open();if(wire.Spins!=2||wire.Keys.Distinct().Count()!=1||state.AdIntent?.Status!="CLAIMED"||(int)state.AdIntent.Result["multiplier"]!=2||state.AdIntent.Result["rewardLines"].Count()!=4)throw new InvalidOperationException("Replay restoration failed");Record("claimed-restored");
            foreach(var name in new[]{"RouletteRoom","RouletteRing","RoulettePointer"})
            {
                var graphic=root.GetComponentsInChildren<RawImage>().Single(g=>g.name==name);if(graphic.raycastTarget||graphic.texture==null)throw new InvalidOperationException("Resource contract "+name);var texture=graphic.texture;
                graphic.gameObject.SetActive(false);Record("hide-"+name);graphic.gameObject.SetActive(true);graphic.texture=Texture2D.whiteTexture;Record("swap-"+name);graphic.texture=texture;Record("restore-"+name);
            }
            foreach(var name in new[]{"RouletteDisc","RouletteInnerRim","RouletteMedallionPlate","RouletteMedallionRim"})
            {
                var graphic=root.GetComponentsInChildren<RouletteSurfaceGraphic>().Single(g=>g.name==name);if(graphic.raycastTarget)throw new InvalidOperationException("Roulette surface intercepted input");graphic.enabled=false;Record("hide-"+name);graphic.enabled=true;Record("restore-"+name);
                if(name=="RouletteDisc"||name=="RouletteMedallionPlate"){var texture=graphic.texture;if(texture==null)throw new InvalidOperationException("Roulette material missing");graphic.texture=Texture2D.whiteTexture;Record("swap-"+name);graphic.texture=texture;Record("restore-material-"+name);}
            }
            foreach(var name in new[]{"AdResult","PointerStateLight"}){var graphic=root.GetComponentsInChildren<Graphic>().Single(g=>g.name==name);graphic.enabled=false;Record("hide-"+name);graphic.enabled=true;Record("restore-"+name);}
            Click("CloseLobbyDialog");await Open();if(wire.Spins!=2||state.AdIntent?.Status!="CLAIMED")throw new InvalidOperationException("Reentry duplicated spin");Record("reopened");
            Click("AdMainAction");await Settle();if(journal.Row!=null||Button("AdMainAction").interactable)throw new InvalidOperationException("Acknowledgement failed");Record("acknowledged");Click("CloseLobbyDialog");if(deck.IsDialogOpen)throw new InvalidOperationException("Return failed");Record("lobby-return");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["nativeFirstHitClicks"]=clicks,["mockReads"]=wire.Reads,["mockSpinPosts"]=wire.Spins,["sameReplayKey"]=wire.Keys.Distinct().Count()==1,["sdkConfigured"]=false,["journalCleared"]=journal.Row==null,["lobbyReturn"]=!deck.IsDialogOpen,["textOverflows"]=overflows});
        }
        finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif

