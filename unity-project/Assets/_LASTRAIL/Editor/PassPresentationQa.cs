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
public static class PassPresentationQa
{
    const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-PassBulkSamples-20260914/";
    sealed class Wire:IRunApiHttpTransport
    {
        public string Mode="free";public int? DisplayXp;public int Reads,Syncs,Claims;public TaskCompletionSource<bool> Gate=new TaskCompletionSource<bool>();
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if(request.EndpointPath=="/v1/recurring/sync")
            {Syncs++;await Gate.Task;return RunApiHttpResponse.Success(Mode=="missing"?503:200,Mode=="missing"?"{}":"{\"accepted\":true,\"addedObjectives\":0,\"lastEventId\":0}");}
            if(request.Method!="GET"){Claims++;throw new InvalidOperationException("No reward claims in pass capture");}Reads++;await Gate.Task;
            var body=JObject.Parse(File.ReadAllText(Source+"recurring-state-premium-"+(Mode=="premium"?"owned":"required")+".json"));
            // Explicit in-memory display fixtures only; source files and real progress are untouched.
            if(DisplayXp.HasValue)foreach(JObject policy in body["policies"]){if((string)policy["system"]!="PASS50")continue;policy["xp"]=DisplayXp.Value;policy["earnedXp"]=DisplayXp.Value;policy["level"]=DisplayXp.Value/100;foreach(JObject row in policy["rewardRows"]){row["freeState"]=(int)row["level"]<=DisplayXp.Value/100?"EARNED":"LOCKED";row["premiumState"]="PREMIUM_REQUIRED";}}
            return RunApiHttpResponse.Success(200,body.ToString());
        }
    }
    public static async void Run()
    {
        try
        {
            var output=Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));if(!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(output))throw new InvalidOperationException("Fresh D QA directory required");Directory.CreateDirectory(output);
            var importer=AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatPass/banner-v01.png") as TextureImporter;if(importer!=null){importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();}
            var cases=new JArray();foreach(var height in new[]{1280,1600})await Capture(output,height,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="26 full50-level basic and premium state through actual Deck/controller, synthetic in-memory sync acknowledgements followed by preserved producer GET fixtures. No real HTTP, reward claim, premium purchase or live progress sync. Native pointer wheel reaches50; existing bulk mixed failures tested separately.",["inputRoot"]=Source,["cases"]=cases}.ToString());EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static async Task Capture(string output,int height,JArray cases)
    {
        var root=new GameObject("PassQa",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
        var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
        var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var wire=new Wire();GameApiAuthSessionSnapshot session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
        try
        {
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
            int captures=0,clicks=0,wheels=0;var overflow=new JArray();var geometry=new JObject();
            void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
            void Record(string name)
            {
                Draw();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing&&text.GetComponentInParent<SeasonPassView>()!=null)overflow.Add(name+":"+text.name);}
                var passScroll=root.GetComponentInChildren<SeasonPassView>()?.GetComponentInChildren<ScrollRect>();if(passScroll!=null)geometry[name]=new JObject{["thumbSize"]=passScroll.verticalScrollbar.size,["thumbValue"]=passScroll.verticalScrollbar.value,["thumbHeight"]=passScroll.verticalScrollbar.handleRect.rect.height,["viewportHeight"]=passScroll.viewport.rect.height,["contentHeight"]=passScroll.content.rect.height,["normalizedPosition"]=passScroll.verticalNormalizedPosition};
                camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,"pass-"+height+"-"+name+".png"),pixels.EncodeToPNG());captures++;
            }
            void Click(string name)
            {
                Draw();var b=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);var r=(RectTransform)b.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Blocked pointer "+name+": "+string.Join(",",hits.Select(h=>h.gameObject.name)));ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);clicks++;
            }
            void Wheel(float amount)
            {
                Draw();var scroll=root.GetComponentInChildren<SeasonPassView>().GetComponentInChildren<ScrollRect>();var pointer=new PointerEventData(events.GetComponent<EventSystem>()){position=RectTransformUtility.WorldToScreenPoint(camera,scroll.viewport.TransformPoint(scroll.viewport.rect.center)),scrollDelta=new Vector2(0,amount)};
                var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);if(hits.Count==0||hits[0].gameObject.GetComponentInParent<ScrollRect>()!=scroll)throw new InvalidOperationException("Viewport scroll blocked");ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.scrollHandler);wheels++;
            }
            async Task Settle(){wire.Gate.TrySetResult(true);for(int i=0;account.Busy||account.RecurringLoading;i++){if(i>1000)throw new TimeoutException();await Task.Delay(1);}await Task.Delay(1);}
            async Task Refresh(string mode){wire.Mode=mode;wire.Gate=new TaskCompletionSource<bool>();Click("RefreshPass");await Settle();}
            Click("Shortcut1");Record("loading");await Settle();var view=root.GetComponentInChildren<SeasonPassView>();if(view?.CurrentState.Rewards.Count!=100||view.CurrentState.Rewards.Count(r=>r.CanClaim)!=1)throw new InvalidOperationException("Free track state failed: "+account.RecurringError);Record("free-required");
            var track=view.GetComponentsInChildren<Image>().Single(x=>x.name=="PassXpTrack");
            if(track.rectTransform.rect.height<10)throw new InvalidOperationException("Pass XP track remains thinner than the approved readable range");
            var fill=view.GetComponentsInChildren<Image>().Single(x=>x.name=="PassXpFill");
            void CheckProgress(float expected){Draw();if(Mathf.Abs(view.CurrentState.Progress-expected)>.0001f||Mathf.Abs(fill.rectTransform.rect.width-track.rectTransform.rect.width*expected)>.01f)throw new InvalidOperationException("Progress visual does not match actual fraction");}
            CheckProgress(.04f);
            foreach(var graphic in new[]{track,fill}){graphic.enabled=false;Record("hide-"+graphic.name);graphic.enabled=true;Record("restore-"+graphic.name);}
            var gaugeFrame=view.GetComponentsInChildren<RectTransform>().Single(x=>x.name=="PassXpFrame");gaugeFrame.gameObject.SetActive(false);Record("hide-PassXpFrame");gaugeFrame.gameObject.SetActive(true);Record("restore-PassXpFrame");
            foreach(var column in new[]{"FREE","PREMIUM"})
            {
                var surfaces=view.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("ClaimPass")&&b.name.EndsWith(column)).Select(b=>b.GetComponentInChildren<IndependentMetalSurface>()).ToArray();
                foreach(var surface in surfaces)surface.Backplate.enabled=false;Record("hide-"+column+"-materials");foreach(var surface in surfaces)surface.Backplate.enabled=true;Record("restore-"+column+"-materials");
            }
            var locked=view.GetComponentsInChildren<Button>().Single(b=>b.name=="ClaimPass3FREE");var rewardLabel=locked.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Reward");var statusLabel=locked.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Status");
            if(locked.interactable||rewardLabel.color.grayscale<=statusLabel.color.grayscale)throw new InvalidOperationException("Locked reward information must remain readable without enabling claim");
            foreach(var xp in new[]{0,5000}){wire.DisplayXp=xp;await Refresh("free");CheckProgress(xp/5000f);Record("derived-xp-"+xp);}
            wire.DisplayXp=null;await Refresh("free");CheckProgress(.04f);Record("restore-actual-xp");
            Wheel(-6000);Draw();var scrollView=view.GetComponentInChildren<ScrollRect>();var last=view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="PassLevel50");var lastPoint=RectTransformUtility.WorldToScreenPoint(camera,last.rectTransform.TransformPoint(last.rectTransform.rect.center));if(!RectTransformUtility.RectangleContainsScreenPoint(scrollView.viewport,lastPoint,camera))throw new InvalidOperationException("Level50 not visible after pointer scroll");Record("level50-bottom");if(Mathf.Abs(scrollView.verticalScrollbar.value-scrollView.verticalNormalizedPosition)>.001f)throw new InvalidOperationException("Bottom content and scrollbar positions disagree");Wheel(6000);Record("top-restored");
            var banner=view.GetComponentsInChildren<RawImage>().Single(x=>x.name=="PassBanner");bool bannerPresent=banner.texture!=null;if(bannerPresent){var original=banner.texture;banner.enabled=false;Record("hide-banner");banner.enabled=true;banner.texture=Texture2D.whiteTexture;Record("swap-banner");banner.texture=original;Record("restore-banner");}
            Click("PassProgressTab");Record("objectives");Click("PassRewardsTab");Record("rewards-return");await Refresh("premium");if(view.CurrentState.Rewards.Count(r=>r.CanClaim)!=2)throw new InvalidOperationException("Premium state failed");Record("premium-owned");
            await Refresh("missing");if(view.CurrentState.Rewards.Count!=0||view.CurrentState.CanBulk)throw new InvalidOperationException("Stale pass rows retained");Record("unavailable");
            await Refresh("free");Record("refreshed");session=null;account.SynchronizeSession();Record("owner-cleared");if(view.CurrentState.Rewards.Count!=0)throw new InvalidOperationException("Owner pass rows retained");Click("CloseSeasonPass");Record("lobby-return");
            if(deck.IsDialogOpen||wire.Claims!=0)throw new InvalidOperationException("Return or claim boundary failed");
            cases.Add(new JObject{["height"]=height,["captures"]=captures,["reads"]=wire.Reads,["mockSyncPosts"]=wire.Syncs,["rewardClaims"]=wire.Claims,["pointerClicks"]=clicks,["pointerWheels"]=wheels,["level50Visible"]=true,["bannerPresent"]=bannerPresent,["premiumAuthority"]=true,["staleHidden"]=true,["ownerCleared"]=true,["lobbyReturn"]=true,["overflow"]=overflow,["geometry"]=geometry});
        }
        finally{RenderTexture.active=prior;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(root);}
    }
}
#endif
