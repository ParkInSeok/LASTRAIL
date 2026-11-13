#if UNITY_EDITOR
using System;
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
public static class ResultPresentationQa
{
    const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
    sealed class Wire:IRunApiHttpTransport
    {
        public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();public string Mode="success";public int Writes,Reads;
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
        {
            if(r.Method!="GET"){Writes++;throw new InvalidOperationException("No result QA mutations");}Reads++;await Gate.Task;
            if(r.EndpointPath.EndsWith("/lifecycle"))return RunApiHttpResponse.Success(200,File.ReadAllText(Source+(Mode=="expired"?"result-expired-lifecycle.json":Mode=="pending"?"result-pending-lifecycle.json":"result-lost-finish-restored.json")));
            if(r.EndpointPath.Contains("/reward-preview/")){if(Mode=="missing")return RunApiHttpResponse.Success(404,File.ReadAllText(Source+"result-preview-missing.json"));var body=JObject.Parse(File.ReadAllText(Source+"result-preview.json"));if(Mode=="empty")body["rewardLines"]=new JArray();return RunApiHttpResponse.Success(200,body.ToString());}
            return RunApiHttpResponse.Success(404,"{}");
        }
    }
    sealed class Journal:IAccountActionJournal{public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction a)=>throw new InvalidOperationException("No journal write");public bool Remove(PendingAccountAction a)=>throw new InvalidOperationException("No journal removal");}
    public static async void Run()
    {
        try{
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");Directory.CreateDirectory(output);
            foreach(var name in new[]{"environment","train"}){var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatResult/"+name+".png");importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.alphaIsTransparency=name=="train";importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();}
            var pins=new JArray();foreach(var pin in (JArray)JObject.Parse(File.ReadAllText(Source+"verification.json"))["responseFiles"]){string file=(string)pin["file"];if(!file.StartsWith("result-"))continue;using var sha=System.Security.Cryptography.SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Source+file))).Replace("-","").ToLowerInvariant();if(hash!=(string)pin["sha256"])throw new InvalidOperationException("Pin "+file);pins.Add(pin.DeepClone());}
            var cases=new JArray();foreach(int height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual AccountScreenRuntime and native lobby result view with delayed in-memory GET transport. Empty rows are a derived local QA response. No real server action or historical wave/time/squad inference.",["sourcePins"]=pins,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("ResultQa",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        try{
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_03_water_stop","급수 정거장",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(state);deck.OpenRunLifecycle("run-smoke-1");int captures=0;
            void Record(string name){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();foreach(var label in root.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing&&label.transform.IsChildOf(root.GetComponentsInChildren<Button>().Single(b=>b.name=="CloseLobbyDialog").transform.parent))throw new InvalidOperationException("Overflow "+label.name);}camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"result-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;}
            async Task Complete(){wire.Gate.SetResult(true);for(int i=0;state.RunResultsLoading;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}}
            void Refresh(string mode){wire.Mode=mode;wire.Gate=new TaskCompletionSource<bool>();var button=root.GetComponentsInChildren<Button>().Single(x=>x.name=="RefreshRunLifecycle");if(!button.interactable)throw new InvalidOperationException("Refresh disabled");ExecuteEvents.Execute(button.gameObject,new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);if(!state.RunResultsLoading)throw new InvalidOperationException("Loading missing");}
            Record("loading");await Complete();if(state.Lifecycle?.RunStatus!="FAILED"||state.RunRewardPreview?.Lines.Count!=2)throw new InvalidOperationException(state.LifecycleNotice+state.RunResultNotice);Record("success");
            // Explicit presentation-only availability contrast. No callback or write is invoked.
            var compared=root.GetComponentsInChildren<Button>().Where(b=>b.name=="RecoverRunFinish"||b.name=="ClaimRunReward").ToArray();
            var originalAvailability=compared.Select(b=>b.interactable).ToArray();Record("controls-disabled");
            foreach(var button in compared)button.interactable=true;typeof(LobbyCommandDeck).GetMethod("ApplyResultControlVisuals",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(deck,null);Record("controls-enabled-visual-only");
            for(int i=0;i<compared.Length;i++)compared[i].interactable=originalAvailability[i];typeof(LobbyCommandDeck).GetMethod("ApplyResultControlVisuals",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(deck,null);
            foreach(var name in new[]{"ResultEnvironment","ResultTrain"}){var image=root.GetComponentsInChildren<RawImage>().Single(x=>x.name==name);if(image.raycastTarget)throw new InvalidOperationException("Decorative input");var saved=image.texture;image.gameObject.SetActive(false);Record("hide-"+name);image.gameObject.SetActive(true);image.texture=Texture2D.whiteTexture;Record("swap-"+name);image.texture=saved;}Record("restored");
            Refresh("missing");Record("missing-loading");await Complete();if(state.RunRewardPreview!=null||string.IsNullOrEmpty(state.RunResultNotice))throw new InvalidOperationException("Missing preview state");Record("missing");
            Refresh("success");Record("retry-loading");await Complete();if(state.RunRewardPreview?.Lines.Count!=2)throw new InvalidOperationException("Retry result");Record("retry-success");
            Refresh("empty");await Complete();if(state.RunRewardPreview?.Lines.Count!=0)throw new InvalidOperationException("Empty result");Record("empty");
            Refresh("expired");await Complete();if(state.Lifecycle?.RewardStatus!="EXPIRED")throw new InvalidOperationException("Expired result");Record("expired");
            Refresh("pending");await Complete();if(state.Lifecycle?.NextAction!="RESUME_FINISH")throw new InvalidOperationException("Pending result");Record("finish-pending");
            session=null;state.SynchronizeSession();Record("owner-cleared");if(state.Lifecycle!=null||wire.Writes!=0)throw new InvalidOperationException("Owner/mutation boundary");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["reads"]=wire.Reads,["writes"]=wire.Writes,["nativeRefreshes"]=5,["ownerCleared"]=true,["unknownHistoryNotInferred"]=true,["controlContrastUsesSyntheticAvailabilityWithoutCallbacks"]=true});
        }finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
