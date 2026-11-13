#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Narrative;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class StoryArchiveQa
{
    sealed class Wire:IRunApiHttpTransport
    {
        public int Reads,Writes;
        public string Mode="ready";
        public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();
        public JObject Body()=>new JObject{
            ["accepted"]=true,["actId"]="act_01_wasteland_railway",
            ["storyProgress"]=new JObject{["highestStoryProgressId"]="none",["highestClearedOperationId"]=null,["order"]=0},
            ["clearedOperationIds"]=new JArray(),["unlockedOperations"]=Mode=="empty"?new JArray():new JArray("opn_01_last_departure"),
            ["narrativeVersion"]="local-read-qa-v1",
            ["narrative"]=new JObject{["narrativeVersion"]="local-read-qa-v1",["contentStatus"]="CANDIDATE",["localizationStatus"]="PENDING",["fixtureStatus"]="PARTIAL",["missingContentStatus"]="UNKNOWN"}};
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.Method!="GET"){Writes++;throw new InvalidOperationException("QA forbids writes");}
            if(request.EndpointPath!="/v1/story-progress")throw new InvalidOperationException("Unexpected read");
            Reads++;var mode=Mode;var body=Body().ToString();await Gate.Task;
            return RunApiHttpResponse.Success(mode=="error"?503:200,mode=="error"?"{}":body);
        }
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA output required");
            Directory.CreateDirectory(output);
            foreach(var path in Directory.GetFiles("Assets/_LASTRAIL/Content/RemoteResources/NoncombatArchive/V02","*.png"))
            {var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();}
            var cases=new JArray();foreach(int height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="Actual lobby menu and story-progress reader; delayed synthetic GET only. Text reading uses existing CodexArchivePresentationState. No live HTTP, START, fuel, rewards or full replay claim.",["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("StoryArchiveQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var state=new AccountScreenRuntime(wire,null,()=>session,()=>0);
        try
        {
            File.WriteAllText(Path.Combine(output,"synthetic-read-input-"+height+".json"),wire.Body().ToString());
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);
            var clicks=new JArray();
            void Click(string name){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();var button=root.GetComponentsInChildren<Button>().Single(b=>b.name==name);if(!button.interactable)throw new InvalidOperationException("Disabled "+name);var r=(RectTransform)button.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};var hits=new System.Collections.Generic.List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new InvalidOperationException("Story first-hit mismatch "+name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);clicks.Add(name);}
            int count=0;var overflow=new JArray();
            void Record(string name)
            {
                Canvas.ForceUpdateCanvases();deck.EvaluateLayout();
                foreach(var label in root.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing&&label.GetComponentInParent<StoryArchiveView>()!=null)overflow.Add(name+":"+label.name);}
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"story-"+height+"-"+name+".png"),pixels.EncodeToPNG());count++;
            }
            async Task Release(){wire.Gate.TrySetResult(true);for(int i=0;state.StoryArchive.Status==StoryArchiveReadStatus.Loading;i++){if(i>500)throw new TimeoutException();await Task.Delay(1);}await Task.Delay(1);}
            Click("LobbyMenu");Click("OpenStoryArchive");Record("loading");await Release();Record("available");
            var view=root.GetComponentInChildren<StoryArchiveView>();var highlights=view.GetComponentsInChildren<StorySelectionOutline>();if(highlights.Count(g=>g.enabled)!=1)throw new InvalidOperationException("Exactly one selection highlight required");var selected=highlights.Single(g=>g.enabled);selected.enabled=false;Record("hide-selection");selected.enabled=true;Record("restore-selection");
            var chapter=view.transform.Find("ArchiveList/ChapterNamePlate").GetComponent<Image>();chapter.enabled=false;Record("hide-chapter-plate");chapter.enabled=true;Record("restore-chapter-plate");var chapterFrame=chapter.transform.Find("Frame").GetComponent<Image>();chapterFrame.enabled=false;Record("hide-chapter-frame");chapterFrame.enabled=true;Record("restore-chapter-frame");
            var chapterLines=view.GetComponentsInChildren<Image>().Where(g=>g.name.StartsWith("ChapterLine")).ToArray();foreach(var line in chapterLines)line.enabled=false;Record("hide-chapter-lines");foreach(var line in chapterLines)line.enabled=true;Record("restore-chapter-lines");
            var arrows=view.GetComponentsInChildren<CodexBackChevron>();foreach(var arrow in arrows)arrow.enabled=false;Record("hide-arrows");foreach(var arrow in arrows)arrow.enabled=true;Record("restore-arrows");
            Click("StoryRow1");if(highlights.Count(g=>g.enabled)!=1||highlights.Single(g=>g.enabled).transform.parent.name!="StoryRow1")throw new InvalidOperationException("Selection did not move once");Record("locked");if(root.GetComponentsInChildren<Button>().Single(x=>x.name=="OpenStoryRecord").interactable)throw new InvalidOperationException("Locked row opened");
            Click("StoryRow0");Click("OpenStoryRecord");if(!root.GetComponentInChildren<StoryArchiveView>().CurrentState.IsReading)throw new InvalidOperationException("Text did not open");Record("reading");
            var readingView=root.GetComponentInChildren<StoryArchiveView>();var readState=readingView.CurrentState;
            readingView.Render(new StoryArchiveViewState(readState.Rows,readState.SelectedIndex,readState.Status,readState.NarrativeVersion,string.Join("\n",Enumerable.Repeat("긴 기록 표시 검수용 문장입니다. 실제 이야기 원문이 아닙니다.",60))+"\n긴 기록 검수 마지막 줄",true));Record("long-text-display-fixture-top");
            var scroll=readingView.GetComponentInChildren<ScrollRect>();ExecuteEvents.Execute(scroll.gameObject,new PointerEventData(events.GetComponent<EventSystem>()){scrollDelta=new Vector2(0,-100000)},ExecuteEvents.scrollHandler);
            if(scroll.verticalNormalizedPosition>.01f)throw new InvalidOperationException("Long text native scroll did not reach end");Record("long-text-display-fixture-end");
            Click("BackStoryArchive");Record("text-return");
            var env=root.GetComponentsInChildren<RawImage>().Single(x=>x.name=="ArchiveEnvironment");var original=env.texture;env.enabled=false;Record("hide-room");env.enabled=true;env.texture=Texture2D.whiteTexture;Record("swap-room");env.texture=original;Record("restored");
            wire.Mode="empty";wire.Gate=new TaskCompletionSource<bool>();Click("RefreshStoryArchive");await Release();Record("empty");
            wire.Mode="error";wire.Gate=new TaskCompletionSource<bool>();Click("RefreshStoryArchive");Record("error-loading");await Release();Record("error");
            session=null;state.SynchronizeSession();Record("owner-cleared");Click("StoryListReturn");if(deck.IsDialogOpen||wire.Writes!=0)throw new InvalidOperationException("Close/write boundary failed");Record("lobby-return");
            cases.Add(new JObject{["height"]=height,["nativeFirstHitClicks"]=clicks,["captures"]=count,["reads"]=wire.Reads,["writes"]=wire.Writes,["textReturn"]=true,["lobbyReturn"]=true,["overflow"]=overflow});
        }
        finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif

