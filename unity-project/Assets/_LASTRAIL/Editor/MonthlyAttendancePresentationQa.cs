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

public static class MonthlyAttendancePresentationQa
{
    const string Source="D:/Project/LASTRAIL/QA/NoncombatFinal-20260915/Unity/MonthlyInputs01/synthetic-calendar.json";
    sealed class Registry:IMonthlyAttendanceRegistry{public string CurrentActivityId=>"monthly-view-fixture";}
    sealed class Wire:IRunApiHttpTransport
    {
        public string Mode="ready";public int Reads,Writes;public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method!="GET"){Writes++;throw new InvalidOperationException("No writes in attendance presentation capture");}
            Reads++;await Gate.Task;if(Mode=="missing")return RunApiHttpResponse.Success(503,"{}");var json=JObject.Parse(File.ReadAllText(Source));
            if(Mode=="paid") {json["recovery"]["recoverableCount"]=1;json["recovery"]["quote"]=new JObject{["walletPolicyVersion"]="qa_wallet",["currencyId"]="crystal",["sufficientFunds"]=true,["freeSpent"]=65,["paidSpent"]=35};}
            if(Mode=="binding") {json["unavailableReason"]="elite_ticket_binding_missing";json["cells"][27]["unavailableReason"]="elite_ticket_binding_missing";json["cells"][27]["rewards"]=new JArray();}
            if(Mode=="complete")
            {
                json["serverTime"]=new DateTimeOffset(2026,9,28,0,0,0,TimeSpan.Zero).ToUnixTimeMilliseconds();json["dayIndex"]=27;json["completedCells"]=28;json["complete"]=true;
                for(int i=0;i<28;i++)json["cells"][i]["receipt"]=new JObject{["cell"]=i+1,["source"]="NATURAL",["dayIndex"]=i,["claimId"]="synthetic-"+(i+1)};
            }
            return RunApiHttpResponse.Success(200,json.ToString());
        }
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA directory required");Directory.CreateDirectory(output);
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatAttendance/banner-v01.png");importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            var cases=new JArray();foreach(var h in new[]{1280,1600})await Capture(output,h,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="23 native popup, independent banner and actual deck callbacks. Explicit synthetic contract-valid memory GETs only; no receipt grant, POST, real server reward or production policy. Paid confirmation inspected but never confirmed. Full approved visual match OPEN.",["input"]=Source,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("AttendanceQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1,new Registry());
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
            int captures=0,clicks=0;var overflow=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
            void Record(string name)
            {
                Draw();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing&&text.GetComponentInParent<MonthlyAttendanceView>()!=null)overflow.Add(name+":"+text.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"attendance-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;
            }
            void Click(string name)
            {
                Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);var r=(RectTransform)b.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};
                var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked pointer "+name+": "+string.Join(",",hits.Select(h=>h.gameObject.name)));
                ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);clicks++;
            }
            async Task Settle(){wire.Gate.TrySetResult(true);for(int i=0;account.MonthlyLoading;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}await Task.Delay(1);}
            async Task Refresh(string mode){wire.Mode=mode;wire.Gate=new TaskCompletionSource<bool>();Click("RefreshMonthly");await Settle();}
            // Actual lobby attendance button opens the view and performs only the existing GET.
            Click("Shortcut0");Record("loading");await Settle();var view=root.GetComponentInChildren<MonthlyAttendanceView>();
            if(view?.CurrentState.Cells.Count!=28)throw new InvalidOperationException("Calendar failed: "+account.MonthlyError);Record("available");
            if(!view.CurrentState.TodayAttendanceConfirmed||!view.transform.Find("MonthlyStatusPanel/MonthlyStatusCheck").gameObject.activeSelf)throw new InvalidOperationException("Confirmed attendance status missing");
            foreach(var role in new[]{"title-lines","day-labels","reward-labels","day-separators","reward-separators","received-checks","status-check"})
            {
                var graphics=view.GetComponentsInChildren<Graphic>().Where(g=>role=="title-lines"?g.name.StartsWith("MonthlyTitleLine"):role=="day-labels"?g.name=="Day":role=="reward-labels"?g.name=="Reward":role=="day-separators"?g.name=="DaySeparator":role=="reward-separators"?g.name=="RewardSeparator":role=="received-checks"?g.name=="Received":g.name=="MonthlyStatusCheck").ToArray();if(graphics.Length==0)throw new InvalidOperationException("Missing attendance role "+role);foreach(var g in graphics)g.enabled=false;Record("hide-"+role);foreach(var g in graphics)g.enabled=true;Record("restore-"+role);
            }
            foreach(var role in new[]{"cell-materials","status-material"}){var plates=view.GetComponentsInChildren<IndependentMetalSurface>().Where(g=>role=="cell-materials"?g.transform.parent.name.StartsWith("MonthlyCell"):g.transform.parent.name=="MonthlyStatusPanel").Select(g=>g.Backplate).ToArray();foreach(var plate in plates)plate.enabled=false;Record("hide-"+role);foreach(var plate in plates)plate.enabled=true;Record("restore-"+role);}
            var banner=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="MonthlyBanner");var original=banner.texture;if(original==null)throw new InvalidOperationException("Banner missing");banner.enabled=false;Record("hide-banner");banner.enabled=true;banner.texture=Texture2D.whiteTexture;Record("swap-banner");banner.texture=original;Record("restored");
            await Refresh("missing");if(view.CurrentState.Cells.Count!=0)throw new InvalidOperationException("Stale cells visible");if(view.CurrentState.TodayAttendanceConfirmed||view.transform.Find("MonthlyStatusPanel/MonthlyStatusCheck").gameObject.activeSelf)throw new InvalidOperationException("Unavailable calendar displayed confirmation");Record("unavailable");
            await Refresh("binding");Record("reward-binding-unavailable");await Refresh("complete");Record("complete-synthetic");
            await Refresh("paid");if(!view.CurrentState.CanRecover)throw new InvalidOperationException("Paid quote unavailable");Record("paid-quote");Click("RecoverMonthly");
            if(!root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("유료 크리스탈 35")))throw new InvalidOperationException("Existing paid confirmation missing");Record("paid-confirmation-unsubmitted");Click("CloseLobbyDialog");
            wire.Mode="ready";Click("Shortcut0");await Settle();Record("reentry");session=null;account.SynchronizeSession();Record("owner-cleared");if(root.GetComponentInChildren<MonthlyAttendanceView>().CurrentState.Cells.Count!=0)throw new InvalidOperationException("Owner cells retained");
            Click("CloseMonthlyX");Record("lobby-return");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Return or write boundary failed");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["reads"]=wire.Reads,["writes"]=wire.Writes,["pointerClicks"]=clicks,["paidConfirmationUnsubmitted"]=true,["bannerIndependent"]=true,["staleHidden"]=true,["ownerCleared"]=true,["lobbyReturn"]=true,["overflow"]=overflow});
        }
        finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif


