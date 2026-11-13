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

public static class VictoryPresentationQa
{
    const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
    sealed class Wire : IRunApiHttpTransport
    {
        public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();
        public string Mode="ready"; public int Reads,Writes;
        public JObject Lifecycle()
        {
            var json=JObject.Parse(File.ReadAllText(Source+"result-lost-finish-restored.json"));
            if(Mode!="failed"){json["runStatus"]="COMPLETED";json["finish"]["outcome"]="VICTORY";}
            if(Mode=="claimed")
            {
                json["reward"]["status"]="CLAIMED";json["reward"]["claimId"]="qa_claim_1";
                json["reward"]["receipt"]=new JObject{["rewardTransactionId"]="qa_claim_1",["runId"]="run-smoke-1",["serverClaimedAt"]=(long)json["serverTime"],
                    ["rewardPlan"]=new JObject{["rewardLines"]=JObject.Parse(File.ReadAllText(Source+"result-preview.json"))["rewardLines"]}};
                json["nextAction"]="RETURN_TO_LOBBY";json["disposition"]="REWARDED";
            }
            return json;
        }
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method!="GET"){Writes++;throw new InvalidOperationException("No writes in victory capture");}
            Reads++;await Gate.Task;
            if(request.EndpointPath.EndsWith("/lifecycle"))return RunApiHttpResponse.Success(200,Lifecycle().ToString());
            if(request.EndpointPath.Contains("/reward-preview/"))
            {
                if(Mode=="missing")return RunApiHttpResponse.Success(404,"{}");
                var json=JObject.Parse(File.ReadAllText(Source+"result-preview.json"));if(Mode=="empty")json["rewardLines"]=new JArray();return RunApiHttpResponse.Success(200,json.ToString());
            }
            return RunApiHttpResponse.Success(404,"{}");
        }
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA directory required");
            Directory.CreateDirectory(output);
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatVictory/environment.png");
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            var cases=new JArray();foreach(var height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Dedicated22 UI and original21 fallback through actual AccountScreenRuntime/Deck, in-memory GET only. COMPLETED and CLAIMED replies explicitly derived from original FAILED server fixture; not new actual server outcomes. No START/finish/claim mutation. Historical wave/time/participants unknown. True-alpha victory train MISSING; rejected opaque checker candidate never imported. Environment aspect/right checkpoint retained; final composition OPEN.",["cases"]=cases}.ToString());
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("VictoryQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
        using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
        try
        {
            File.WriteAllText(Path.Combine(output,"derived-victory-lifecycle-"+height+".json"),wire.Lifecycle().ToString());
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new[]{new MissionShellMissionRowState("opn_03_water_stop","급수 정거장",RunEncounterType.Ordinary15Wave,true,"start","loading",false)},null,null);deck.BindAccount(account);
            int count=0,pointerClicks=0;var overflow=new JArray();
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
            void Record(string name)
            {
                Draw();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing&&text.GetComponentInParent<RunVictoryView>()!=null)overflow.Add(name+":"+text.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"victory-"+height+"-"+name+".png"),pixels.EncodeToPNG());count++;
            }
            void Click(string name)
            {
                Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);var r=(RectTransform)b.transform;
                var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};
                var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked pointer "+name+": "+string.Join(",",hits.Select(h=>h.gameObject.name)));
                // The first hit already resolves this Button. EditMode return destroys its hierarchy immediately.
                ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);pointerClicks++;
            }
            async Task Settle(){wire.Gate.TrySetResult(true);for(int i=0;account.RunResultsLoading;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}await Task.Delay(1);}
            void Refresh(string mode)
            {
                wire.Mode=mode;wire.Gate=new TaskCompletionSource<bool>();
                if(root.GetComponentInChildren<RunVictoryView>()!=null){if(!root.GetComponentsInChildren<Button>().Any(b=>b.name=="VictoryRefresh"))Click("VictoryDetails");Click("VictoryRefresh");}
                else Click("RefreshRunLifecycle");
            }
            deck.OpenRunLifecycle("run-smoke-1");Record("initial-loading");await Settle();
            var view=root.GetComponentInChildren<RunVictoryView>();if(view?.CurrentState.Title!="작전 완료"||view.CurrentState.Rewards.Count!=2)throw new InvalidOperationException("Victory mapping failed");Record("available");
            var geometry=new JArray();foreach(var item in new[]{"VictoryMetrics","VictoryParticipants","VictoryRewards"}){var r=(RectTransform)view.transform.Find("VictoryLower/"+item);var corners=new Vector3[4];r.GetWorldCorners(corners);var top=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);var bottom=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);geometry.Add(new JObject{["name"]=item,["logicalHeight"]=r.rect.height,["screenTop"]=height-top.y,["screenHeight"]=top.y-bottom.y});if(r.rect.height!=(item=="VictoryMetrics"?106:item=="VictoryParticipants"?335:305))throw new InvalidOperationException("Victory fixed content height changed");}
            Click("VictoryDetails");Record("details");Click("VictoryDetails");
            var environment=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="VictoryEnvironment");if(environment.texture==null)throw new InvalidOperationException("Environment missing");
            var original=environment.texture;environment.enabled=false;Record("hide-environment");environment.enabled=true;environment.texture=Texture2D.whiteTexture;Record("swap-environment");environment.texture=original;Record("restored");
            Refresh("missing");Record("refresh-loading");await Settle();Click("VictoryDetails");Record("preview-unavailable");
            Refresh("empty");await Settle();Click("VictoryDetails");Record("known-empty");
            Refresh("claimed");await Settle();Click("VictoryDetails");if(view.CurrentState.RewardStatus!="지급 완료")throw new InvalidOperationException("Verified receipt display failed");Record("claimed-derived-fixture");
            Refresh("failed");await Settle();if(root.GetComponentInChildren<RunVictoryView>()!=null)throw new InvalidOperationException("Failed result did not restore21");Record("failed21-fallback");
            Refresh("ready");await Settle();Record("victory-reentry");
            session=null;account.SynchronizeSession();Record("owner-cleared");if(root.GetComponentInChildren<RunVictoryView>().CurrentState.Rewards.Count!=0)throw new InvalidOperationException("Foreign owner result retained");
            Click("VictoryReturn");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Return/write boundary failed");Record("lobby-return");
            cases.Add(new JObject{["height"]=height,["geometry"]=geometry,["captures"]=count,["reads"]=wire.Reads,["writes"]=wire.Writes,["pointerClicks"]=pointerClicks,["knownEmptyDistinct"]=true,["failed21Fallback"]=true,["ownerCleared"]=true,["lobbyReturn"]=true,["environmentIndependent"]=true,["victoryTrainPresent"]=false,["overflow"]=overflow});
        }
        finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif

