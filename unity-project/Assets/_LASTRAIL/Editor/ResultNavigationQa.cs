#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
public static class ResultNavigationQa
{
    const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
    sealed class Wire:IRunApiHttpTransport
    {
        public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();public string Mode="success";public int Writes,Reads;
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
        {
            if(r.Method!="GET"){Writes++;throw new InvalidOperationException("No result QA mutations");}Reads++;await Gate.Task;
            if(r.EndpointPath.EndsWith("/lifecycle")){var data=JObject.Parse(File.ReadAllText(Source+(Mode=="expired"?"result-expired-lifecycle.json":Mode=="pending"?"result-pending-lifecycle.json":"result-lost-finish-restored.json")));if(Mode=="victory"){data["runStatus"]="COMPLETED";data["finish"]["outcome"]="VICTORY";}return RunApiHttpResponse.Success(200,data.ToString());}
            if(r.EndpointPath.Contains("/reward-preview/")){if(Mode=="missing")return RunApiHttpResponse.Success(404,File.ReadAllText(Source+"result-preview-missing.json"));var body=JObject.Parse(File.ReadAllText(Source+"result-preview.json"));if(Mode=="empty")body["rewardLines"]=new JArray();return RunApiHttpResponse.Success(200,body.ToString());}
            return RunApiHttpResponse.Success(404,"{}");
        }
    }
    sealed class Journal:IAccountActionJournal{public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction a)=>throw new InvalidOperationException("No journal write");public bool Remove(PendingAccountAction a)=>throw new InvalidOperationException("No journal removal");}
    public static async void Run()
    {
        try{
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");Directory.CreateDirectory(output);
            var pins=new JArray();foreach(var pin in (JArray)JObject.Parse(File.ReadAllText(Source+"verification.json"))["responseFiles"]){string file=(string)pin["file"];if(!file.StartsWith("result-"))continue;using var sha=System.Security.Cryptography.SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Source+file))).Replace("-","").ToLowerInvariant();if(hash!=(string)pin["sha256"])throw new InvalidOperationException("Pin "+file);pins.Add(pin.DeepClone());}
            var cases=new JArray();foreach(int height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual AccountScreenRuntime and native lobby result view with delayed in-memory GET transport. Empty rows are a derived local QA response. No real server action or historical wave/time/squad inference.",["sourcePins"]=pins,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("ResultQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        try{
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_03_water_stop","급수 정거장",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(state);int captures=0;var clicks=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
            void Click(string name){Draw();var button=root.GetComponentsInChildren<Button>().Single(b=>b.name==name);if(!button.interactable)throw new InvalidOperationException("Disabled "+name);var rect=(RectTransform)button.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("First hit mismatch "+name);clicks.Add(name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
            void Record(string name){Draw();var resultParent=root.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="CloseLobbyDialog")?.transform.parent;foreach(var label in root.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing&&resultParent!=null&&label.transform.IsChildOf(resultParent))throw new InvalidOperationException("Overflow "+label.name);}camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"result-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;}
            deck.OpenDepartureRecovery("run-smoke-1");Click("RunLifecycle");
            async Task Complete(){wire.Gate.SetResult(true);for(int i=0;state.RunResultsLoading;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}}
            void Refresh(string mode){wire.Mode=mode;wire.Gate=new TaskCompletionSource<bool>();Click("RefreshRunLifecycle");if(!state.RunResultsLoading)throw new InvalidOperationException("Loading missing");}
            Record("loading");await Complete();if(state.Lifecycle?.RunStatus!="FAILED"||state.RunRewardPreview?.Lines.Count!=2)throw new InvalidOperationException(state.LifecycleNotice+state.RunResultNotice);Record("success");
            var ornament=root.GetComponentsInChildren<RectTransform>().SingleOrDefault(x=>x.name=="ResultTitleOrnaments");
            if(ornament==null)throw new InvalidOperationException("Result independent title ornament missing");
            var result=ornament.parent;var title=result.Find("DialogTitle").GetComponent<TMP_Text>();title.ForceMeshUpdate();
            float glyphTop=title.rectTransform.anchoredPosition.y+title.textBounds.max.y;
            if(-45-glyphTop<12)throw new InvalidOperationException("Result title ornament glyph clearance");
            void HideRestore(Transform item,string name){item.gameObject.SetActive(false);Record("hide-"+name);item.gameObject.SetActive(true);Record("restore-"+name);}
            HideRestore(ornament,"ornament");
            foreach(var name in new[]{"ResultStats","ResultParticipants","ResultRewardPanel"})
            {
                var panel=result.Find(name);var interior=panel.Find("Interior");
                if(interior==null||panel.GetComponent<Image>().color.a!=0||interior.GetComponent<RawImage>().texture==null||interior.GetComponent<RawImage>().raycastTarget)throw new InvalidOperationException("Result independent panel interior missing "+name);
                HideRestore(interior,name+"-interior");HideRestore(panel.Find("MetalFrame"),name+"-frame");
            }
            foreach(var name in new[]{"ResultEnvironment","ResultTrain"}){var image=root.GetComponentsInChildren<RawImage>().Single(x=>x.name==name);if(image.raycastTarget)throw new InvalidOperationException("Decorative input");var saved=image.texture;image.gameObject.SetActive(false);Record("hide-"+name);image.gameObject.SetActive(true);image.texture=Texture2D.whiteTexture;Record("swap-"+name);image.texture=saved;}Record("restored");
            Refresh("victory");await Complete();Draw();if(ornament.gameObject.activeInHierarchy||root.GetComponentInChildren<RunVictoryView>()==null)throw new InvalidOperationException("Result decoration leaked into victory");Record("victory-switch");
            if(!root.GetComponentsInChildren<Button>().Any(b=>b.name=="VictoryRefresh"))Click("VictoryDetails");
            wire.Mode="success";wire.Gate=new TaskCompletionSource<bool>();Click("VictoryRefresh");await Complete();Draw();
            if(!ornament.gameObject.activeInHierarchy||root.GetComponentInChildren<RunVictoryView>()!=null)throw new InvalidOperationException("Result visibility did not restore");Record("victory-return");
            Refresh("missing");Record("missing-loading");await Complete();if(state.RunRewardPreview!=null||string.IsNullOrEmpty(state.RunResultNotice))throw new InvalidOperationException("Missing preview state");Record("missing");
            Refresh("success");Record("retry-loading");await Complete();if(state.RunRewardPreview?.Lines.Count!=2)throw new InvalidOperationException("Retry result");Record("retry-success");
            Refresh("empty");await Complete();if(state.RunRewardPreview?.Lines.Count!=0)throw new InvalidOperationException("Empty result");Record("empty");
            Refresh("expired");await Complete();if(state.Lifecycle?.RewardStatus!="EXPIRED")throw new InvalidOperationException("Expired result");Record("expired");
            Refresh("pending");await Complete();if(state.Lifecycle?.NextAction!="RESUME_FINISH")throw new InvalidOperationException("Pending result");Record("finish-pending");
            session=null;state.SynchronizeSession();Record("owner-cleared");if(state.Lifecycle!=null||wire.Writes!=0)throw new InvalidOperationException("Owner/mutation boundary");
            Click("CloseLobbyDialog");if(deck.IsDialogOpen)throw new InvalidOperationException("Native lobby return failed");Record("lobby-return");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["reads"]=wire.Reads,["writes"]=wire.Writes,["nativeFirstHitClicks"]=clicks,["ornamentGlyphClearance"]= -45-glyphTop,["victoryRoundTrip"]=true,["victoryFixture"]="QA-only COMPLETED/VICTORY derived from FAILED input; no server outcome",["ownerCleared"]=true,["unknownHistoryNotInferred"]=true,["lobbyReturn"]=true,["entryScope"]="Existing recovery controller opened with frozen run id, then native RunLifecycle button; not a played defeat or live run lookup."});
        }finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif

