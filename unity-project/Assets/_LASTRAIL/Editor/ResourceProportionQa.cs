#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using LastRail.Presentation.Startup;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ResourceProportionQa
{
    const long Total=80L*1048576;
    sealed class Backend:IContentStartupBackend
    {
        public Action<long,long> Progress;public TaskCompletionSource<bool> Hold=new TaskCompletionSource<bool>();
        public Task<ClientContentGate> FetchConfigAsync(CancellationToken c)=>Task.FromResult(new ClientContentGate{MinimumVersion="0.1"});
        public Task InitializeAsync(CancellationToken c)=>Task.CompletedTask;
        public Task UpdateCatalogAsync(CancellationToken c)=>Task.CompletedTask;
        public Task<long> RequiredBytesAsync(CancellationToken c)=>Task.FromResult(Total);
        public async Task DownloadAsync(Action<long,long> progress,CancellationToken c){Progress=progress;progress(0,Total);using(c.Register(()=>Hold.TrySetCanceled()))await Hold.Task;}
        public Task VerifyRequiredAssetsAsync(CancellationToken c)=>Task.CompletedTask;
    }
    public static async void Run()
    {
        try{var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");Directory.CreateDirectory(output);var cases=new JArray();foreach(int h in new[]{1280,1600})await Capture(output,h,cases);File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual StartupDownloadOverlay/ResourcePreparationView with DownloadCoordinator and in-memory80MiB backend. No files downloaded, HTTP, authentication, Main entry, START or economy. 100percent is local synthetic byte progress held before finishing the real coordinator, not a real content download. Canvas1080 width and camera ratio emulate width-matched scaler; motion frozen for restore comparison.",["cases"]=cases}.ToString());EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("ResourceQa",typeof(RectTransform));var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var access=new DownloadAccessPolicy();access.SetNetwork(ContentNetwork.OtherOnline);var backend=new Backend();var coordinator=new DownloadCoordinator(backend,access);coordinator.StartAsync("1.0",CancellationToken.None);
        try{
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var overlay=root.AddComponent<StartupDownloadOverlay>();int pauses=0,approvals=0,retries=0;
            overlay.Initialize(font,()=>{approvals++;access.Approve();},()=>access.Decline(),()=>{retries++;backend.Hold=new TaskCompletionSource<bool>();coordinator.StartAsync("1.0",CancellationToken.None);});overlay.PauseRequested=()=>{pauses++;access.SetPaused(!access.Paused);};
            root.GetComponent<CanvasScaler>().enabled=false;var canvas=root.GetComponent<Canvas>();var camera=cameraObject.GetComponent<Camera>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;((RectTransform)root.transform).sizeDelta=new Vector2(1080,height*1.5f);camera.orthographic=true;camera.orthographicSize=height*.75f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            root.transform.position=Vector3.zero;root.transform.localScale=Vector3.one;
            var view=root.GetComponentInChildren<ResourcePreparationView>(true);var motion=view.GetComponentInChildren<StartupMotionBackdrop>(true);motion.ReducedMotion=true;int captures=0;var clicks=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();view.SendMessage("LateUpdate");motion.Evaluate(0);Canvas.ForceUpdateCanvases();camera.Render();}
            void Refresh(){overlay.Show(access.NeedsConsent,Math.Max(0,Total-coordinator.ReceivedBytes),coordinator.Stage==ContentStage.Failed);overlay.RenderPreparation(true,coordinator,access);}
            void Record(string name){Draw();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"resource-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;}
            void Click(string name){Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);var r=(RectTransform)b.transform;var p=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(p,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Resource first hit "+name);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);clicks.Add(name);Refresh();}
            Refresh();Record("consent");if(view.transform.Find("ResourceProgressContent").gameObject.activeInHierarchy)throw new InvalidOperationException("Consent leaked progress");Click("ApproveDownload");await Task.Yield();Refresh();Record("zero");
            var track=(RectTransform)view.transform.Find("ResourceProgressContent/ProgressTrack");var pause=(RectTransform)view.transform.Find("ResourceProgressContent/PauseDownload");
            if(Mathf.Abs(track.rect.height-28)>.01f||Mathf.Abs(pause.rect.height-134)>.01f)throw new InvalidOperationException("Resource geometry stretched with viewport");
            backend.Progress(27L*1048576,Total);Refresh();Record("progress");
            var titleRect=view.transform.Find("ResourceTitle").GetComponent<RectTransform>();var titleCorners=new Vector3[4];titleRect.GetWorldCorners(titleCorners);var titleTop=RectTransformUtility.WorldToScreenPoint(camera,titleCorners[1]);if(Mathf.Abs(titleTop.x-56.16f)>.1f||Mathf.Abs(height-titleTop.y-70)>.1f)throw new InvalidOperationException("Resource capture origin is not centered");
            var geometry=new JArray();foreach(var n in new[]{"ResourceTitle","ResourceProgressContent/ProgressTrack","ResourceProgressContent/PauseDownload","ResourceProgressContent/Heading"}){var r=(RectTransform)view.transform.Find(n);var corners=new Vector3[4];r.GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(camera,corners[1]);var b=RectTransformUtility.WorldToScreenPoint(camera,corners[3]);geometry.Add(new JObject{["name"]=n,["screenX"]=a.x,["screenY"]=height-a.y,["screenWidth"]=b.x-a.x,["screenHeight"]=a.y-b.y});}
            foreach(var n in new[]{"ProgressTrack","MeasuredProgress","Percent","PauseDownload"}){var item=view.GetComponentsInChildren<RectTransform>().Single(t=>t.name==n);if(n=="ProgressTrack"){var g=item.GetComponent<Graphic>();g.enabled=false;Record("hide-"+n);g.enabled=true;}else{item.gameObject.SetActive(false);Record("hide-"+n);item.gameObject.SetActive(true);}Record("restore-"+n);}
            ((RectTransform)root.transform).sizeDelta=new Vector2(1080,(height==1280?1600:1280)*1.5f);Draw();if(Mathf.Abs(track.rect.height-28)>.01f)throw new InvalidOperationException("Resource resize stretched track");((RectTransform)root.transform).sizeDelta=new Vector2(1080,height*1.5f);Record("resize-restored");
            Click("PauseDownload");if(!access.Paused)throw new InvalidOperationException("Pause lost");Record("paused");Click("PauseDownload");if(access.Paused)throw new InvalidOperationException("Resume lost");Record("resumed");
            backend.Hold.SetException(new IOException("Synthetic download failure"));await coordinator.Operation;Refresh();Record("failed");Click("RetryContent");await Task.Yield();backend.Progress(Total,Total);Refresh();Record("complete100");if(view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Percent").text!="100%")throw new InvalidOperationException("Measured complete missing");
            access.SetAuthenticated(true);backend.Hold.SetResult(true);await coordinator.Operation;Refresh();Record("ready");if(coordinator.Stage!=ContentStage.Ready||pauses!=2||approvals!=1||retries!=1)throw new InvalidOperationException("Resource lifecycle callbacks");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["geometry"]=geometry,["nativeFirstHitClicks"]=clicks,["pauseCallbacks"]=pauses,["approveCallbacks"]=approvals,["retryCallbacks"]=retries,["consentBlocks"]=true,["resizeRoundTrip"]=true,["localCoordinatorReady"]=true,["liveDownloads"]=0});
        }finally{await coordinator.ResetAdmissionAsync();RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
