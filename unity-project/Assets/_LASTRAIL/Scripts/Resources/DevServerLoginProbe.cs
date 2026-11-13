#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEngine;

namespace LastRail.Resources
{
    // Explicit Editor QA only: real DEV HTTP, no saved credentials or synthetic account responses.
    public sealed class DevServerLoginProbe : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string source="Real DEV API and R2; user-authorized QA guest; memory-only tokens";
            public string stage, authStage, failureType, accountStatus, accountError;
            public int portraitCount,guestButtonInvocations;
            public bool initialLoginCacheChecked,initialLoginArtworkCached,initialMinimalUi,cachedLoginArtworkDisplayed,cachedLoginClosedAndReleased;
            public long downloadBytes, plannedBytes, cachedRequiredBytes=-1;
            public bool guestLoginSucceeded, passwordLoginSucceeded, profileSucceeded, storySucceeded, lobbyVisible, profileLabelMatches;
            public bool walletHttpSucceeded,growthHttpSucceeded,growthReady,growthCostDisplayed,formationVerified;
            public string walletStatus,crystalDisplay,growthNotice,growthCost,actionNotice;
        }
        sealed class MemoryTokens : IGameApiAuthTokenStore, IGameApiAuthTokenReader
        {
            GameApiAuthSessionSnapshot session;
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value){session=value;return Task.FromResult(GameApiAuthTokenStoreResult.Saved);}
            public Task<GameApiAuthSessionSnapshot> LoadAsync()=>Task.FromResult(session);
            public Task<GameApiAuthTokenReadResult> ReadAsync()=>Task.FromResult(new GameApiAuthTokenReadResult(session==null?GameApiAuthTokenReadOutcome.Missing:GameApiAuthTokenReadOutcome.Loaded,session));
            public void Clear(){session=null;}
        }
        // shortcut: Editor QA retains only login receipts in-session; restart recovery needs an encrypted desktop adapter.
        sealed class LoginJournal : IAccountActionJournal
        {
            PendingAccountAction pending;
            public PendingAccountAction Read(string account)=>pending?.Account==account?pending:null;
            public bool Save(PendingAccountAction action){if(action==null||(action.Kind!="recurring-login"&&action.Kind!="formation-save"&&action.Kind!="formation-activate")||pending!=null)return false;pending=action;return true;}
            public bool Remove(PendingAccountAction action){if(!ReferenceEquals(pending,action))return false;pending=null;return true;}
        }
        sealed class RecordedWire : IRunApiHttpTransport
        {
            readonly IRunApiHttpTransport inner; readonly Report report; readonly string output;
            public RecordedWire(IRunApiHttpTransport inner,Report report,string output){this.inner=inner;this.report=report;this.output=output;}
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                var path=request.EndpointPath.Split('?')[0];
                bool formation=System.Text.RegularExpressions.Regex.IsMatch(path,@"^/v1/formation/presets/preset_[a-f0-9]{32}/(save|activate)$");
                if(request.Method=="POST"&&!formation&&path!="/v1/auth/guest"&&path!="/v1/auth/password/login"&&path!="/v1/auth/refresh"&&path!="/v1/profile/display-name"&&path!="/v1/recurring/login"&&path!="/v1/recurring/sync")
                    throw new InvalidOperationException("DEV login QA does not permit this mutation.");
                if(path=="/v1/auth/guest")
                {
                    var key=File.ReadAllText("D:/Project/LASTRAIL/LocalTemp/DevLogin/landscape-guest-replay.key").Trim();
                    GameApiRefreshAuthRequest.ValidateReplayKey(key);
                    request=new RunApiHttpRequest(request.Method,request.EndpointPath,request.BodyJson,new[]{new RunApiHttpHeader("Idempotency-Key",key)});
                }
                File.AppendAllText(Path.Combine(output,"http-status.log"),"START "+request.Method+" "+path+Environment.NewLine);
                var result=await inner.SendAsync(request);
                if(formation){File.WriteAllText(Path.Combine(output,"formation-request.json"),request.BodyJson);File.WriteAllText(Path.Combine(output,"formation-response.json"),result.BodyJson);}
                File.AppendAllText(Path.Combine(output,"http-status.log"),request.Method+" "+path+" "+result.StatusCode+Environment.NewLine);
                if(result.StatusCode==200){if(path=="/v1/auth/guest")report.guestLoginSucceeded=true;if(path=="/v1/auth/password/login")report.passwordLoginSucceeded=true;if(path=="/v1/profile")report.profileSucceeded=true;if(path=="/v1/story-progress")report.storySucceeded=true;}
                if(path=="/v1/wallet/crystals")report.walletHttpSucceeded=result.StatusCode==200;
                if(path=="/v1/growth/config")report.growthHttpSucceeded=result.StatusCode==200;
                return result;
            }
        }
        readonly Report report=new Report();
        readonly MemoryTokens tokens=new MemoryTokens();
        string output; ResourceBootstrapController bootstrap; AddressableContentBackend backend;
        Cache priorCache,cache,reusedCache,completedCache,formationCache,correctedCache; bool priorBackground; Func<string,Type,UnityEngine.Object> priorEditorAsset;
        public void Begin(ResourceBootstrapController boot,string directory)
        {
            if(Environment.GetEnvironmentVariable("LASTRAIL_DEV_LOGIN_QA")!="1"||boot==null||boot.enabled)throw new InvalidOperationException("Explicit idle DEV login QA required.");
            output=directory;bootstrap=boot;Directory.CreateDirectory(output);
            priorBackground=Application.runInBackground;Application.runInBackground=true;
            var camera=new GameObject("DEV QA presentation camera",typeof(Camera),typeof(AudioListener));camera.transform.SetParent(transform);camera.GetComponent<Camera>().cullingMask=0;
            priorCache=Caching.currentCacheForWriting;
            reusedCache=Caching.AddCache("D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/dev-bundle-20261111/v02/cache/3941df7096234299beae9705f580d52f");
            completedCache=Caching.AddCache("D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/dev-bundle-20261111/real-login-05/cache");
            formationCache=Caching.AddCache("D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/formation-plan-v01/dev/real-login-01/cache");
            var correctedPath="D:/Project/LASTRAIL/QA/ResourceIntake-20261110/connection/formation-plan-v02/dev/real-login-02/cache";
            if(Directory.Exists(correctedPath)&&Path.GetFullPath(Path.Combine(output,"cache"))!=Path.GetFullPath(correctedPath))correctedCache=Caching.AddCache(correctedPath);
            var cachePath=Path.Combine(output,"cache");Directory.CreateDirectory(cachePath);cache=Caching.AddCache(cachePath);
            if(!cache.valid)throw new InvalidOperationException("Fresh QA cache required.");Caching.currentCacheForWriting=cache;
            priorEditorAsset=PreparedContent.EditorAsset;PreparedContent.EditorAsset=null;
            ResourceBootstrapController.EditorVerificationBackend=access=>backend=AddressableContentBackend.CreateForEditorLoopbackFixture(
                EditorLoopbackContentFixture.CreateDevServerProbe("D:/Project/LASTRAIL/unity-project/QA/PCFormation03/runtime-output/settings.json",Path.Combine(output,"state")),access);
            ResourceBootstrapController.EditorCaptureAuthentication=(owner,font,unused)=>{
                var build=new AppBuildSnapshot("0.1.0",45);
                var wire=new RecordedWire(new InstalledAdmissionTransport(new UnityWebRequestRunApiHttpTransport(
                    new UnityWebRequestRunApiHttpAdapter("https://dev-api.lastrail.app",30)),build,"windows","qa",()=>owner.ApprovedContentReleaseId),report,output);
                return new PlayerEntryRuntime(owner,font,wire,tokens,tokens,()=>build,null,null,new LoginJournal());
            };
            bootstrap.enabled=true;StartCoroutine(Observe());
        }
        IEnumerator Observe()
        {
            var deadline=DateTime.UtcNow.AddMinutes(30);string previous=null;bool openingCaptured=false,guestPressed=false,downloadApproved=false;
            while(DateTime.UtcNow<deadline&&bootstrap!=null)
            {
                var coordinator=bootstrap.Coordinator;var auth=bootstrap.Authentication;
                if(!openingCaptured&&bootstrap.Opening?.FirstFrameReady==true&&!bootstrap.Opening.MediaEnded){ScreenCapture.CaptureScreenshot(Path.Combine(output,"opening.png"));openingCaptured=true;}
                report.stage=coordinator?.Stage.ToString();report.authStage=auth?.Owner.Stage.ToString();
                report.downloadBytes=coordinator?.ReceivedBytes??0;report.plannedBytes=coordinator?.TotalBytes??0;
                report.failureType=coordinator?.Failure?.GetType().Name;
                report.accountStatus=auth?.AccountScreens.AccountStatus.ToString();report.accountError=auth?.AccountScreens.AccountErrorCode;
                var state=report.stage+"/"+report.authStage+"/"+report.accountStatus;
                if(state!=previous){Save();previous=state;}
                var loginPanel=FindFirstObjectByType<PlayerEntryPanel>();
                if(!guestPressed&&auth?.Owner.Stage==PlayerAuthStage.Login&&loginPanel!=null&&(bootstrap.Opening==null||bootstrap.Opening.Timeline.Finished))
                {
                    var button=loginPanel.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="Guest"&&b.interactable);
                    if(button!=null)
                    {
                        var cached=PublicEntryTexture.IsCachedAsync(CancellationToken.None);while(!cached.IsCompleted)yield return null;
                        if(cached.Status!=TaskStatus.RanToCompletion){report.failureType="InitialCacheInspectionFailed";Save();UnityEditor.EditorApplication.ExitPlaymode();yield break;}
                        report.initialLoginCacheChecked=true;report.initialLoginArtworkCached=cached.Result;
                        var background=loginPanel.GetComponentsInChildren<RawImage>().FirstOrDefault(i=>i.name=="LoginBackdrop");
                        report.initialMinimalUi=background!=null&&background.texture==null;
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"first-login.png"));Save();yield return new WaitForSecondsRealtime(.5f);
                        guestPressed=true;report.guestButtonInvocations++;button.onClick.Invoke();
                    }
                }
                var consent=FindFirstObjectByType<StartupDownloadOverlay>();
                if(!downloadApproved&&consent!=null&&consent.ConsentVisible)
                {
                    var approve=consent.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="ApproveDownload"&&b.interactable);
                    if(approve!=null){downloadApproved=true;approve.onClick.Invoke();}
                }
                var main=FindFirstObjectByType<MainSceneRoot>();
                var host=FindFirstObjectByType<AddressableScreenHost>();
                if(auth?.Owner.Ready==true&&main!=null&&main.ContentRoot.gameObject.activeInHierarchy&&host?.Current!=null)
                {
                    report.lobbyVisible=true;
                    report.portraitCount=System.Linq.Enumerable.Count(main.GetComponentsInChildren<UnityEngine.UI.RawImage>(true),image=>image.name.StartsWith("SquadPortrait")&&image.texture!=null);
                    if(report.accountStatus=="Loading"||report.accountStatus=="NotLoaded"||auth.AccountScreens.WalletLoading){yield return new WaitForSecondsRealtime(.5f);continue;}
                    foreach(var text in host.Current.GetComponentsInChildren<TMP_Text>(true))
                        if(text.name=="PlayerProfileName"||text.name=="LegacyProfileName")report.profileLabelMatches=text.text==auth.Profile?.DisplayName;
                    var required=backend.RequiredBytesAsync(CancellationToken.None);
                    while(!required.IsCompleted)yield return null;
                    if(required.Status==TaskStatus.RanToCompletion)report.cachedRequiredBytes=required.Result;
                    else report.failureType="CacheVerificationFailed";
                    yield return new WaitForSecondsRealtime(1);
                    report.walletStatus=auth.AccountScreens.Wallet?.Status.ToString();
                    report.crystalDisplay=auth.AccountScreens.BalanceText(2);
                    report.growthReady=auth.AccountScreens.GrowthReady;
                    report.growthNotice=auth.AccountScreens.GrowthNotice;
                    report.actionNotice=auth.AccountScreens.ActionNotice;
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"lobby.png"));Save();
                    yield return new WaitForSecondsRealtime(3);
                    var operatorsButton=host.Current.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="Navigation1");
                    operatorsButton?.onClick.Invoke();
                    var pageDeadline=DateTime.UtcNow.AddSeconds(30);
                    LastRail.Presentation.MissionShell.LobbyDestinationPages growthPage=null;
                    while(growthPage==null&&DateTime.UtcNow<pageDeadline){growthPage=FindFirstObjectByType<LastRail.Presentation.MissionShell.LobbyDestinationPages>();yield return null;}
                    if(growthPage!=null)
                    {
                        var growthDeadline=DateTime.UtcNow.AddSeconds(30);
                        TMP_Text cost=null;
                        while(DateTime.UtcNow<growthDeadline)
                        {
                            cost=growthPage.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t.name=="GrowthCost1");
                            if(cost!=null&&cost.text.Contains("160")&&cost.text.Contains("80"))break;yield return null;
                        }
                        report.growthCost=cost?.text;
                        report.growthCostDisplayed=cost!=null&&cost.text.Contains("160")&&cost.text.Contains("80");
                        report.actionNotice=auth.AccountScreens.ActionNotice;
                        yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"growth.png"));
                        yield return new WaitForSecondsRealtime(.5f);growthPage.Close();
                    }
                    if(!report.walletHttpSucceeded||!report.growthHttpSucceeded||report.walletStatus!="Ready"||!report.growthReady||report.crystalDisplay!="0"||!report.growthCostDisplayed||(report.actionNotice??"").Contains("못했습니다"))report.failureType="EconomyDisplayVerificationFailed";
                    if(growthPage!=null){yield return DevFormationProbe.Run(growthPage,auth.AccountScreens,output);report.formationVerified=DevFormationProbe.Passed;if(!report.formationVerified)report.failureType="FormationVerificationFailed";Save();}
                    var cachedLogin=new GameObject("DEV cached login screen",typeof(RectTransform));
                    var cachedPanel=cachedLogin.AddComponent<PlayerEntryPanel>();
                    cachedPanel.Initialize(UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));cachedPanel.ShowLogin(true,false);
                    var cachedBackground=cachedPanel.GetComponentsInChildren<RawImage>().First(i=>i.name=="LoginBackdrop");
                    var viewDeadline=DateTime.UtcNow.AddSeconds(30);
                    while(cachedBackground.texture==null&&DateTime.UtcNow<viewDeadline)yield return null;
                    var ownedTexture=cachedBackground.texture;report.cachedLoginArtworkDisplayed=ownedTexture!=null;
                    yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"cached-login.png"));
                    yield return new WaitForSecondsRealtime(.5f);cachedLogin.SetActive(false);yield return new WaitForSecondsRealtime(.3f);
                    report.cachedLoginClosedAndReleased=cachedBackground.texture==null&&ownedTexture==null;
                    Destroy(cachedLogin);Save();
                    UnityEditor.EditorApplication.ExitPlaymode();yield break;
                }
                if(coordinator?.Failure!=null){Save();UnityEditor.EditorApplication.ExitPlaymode();yield break;}
                yield return new WaitForSecondsRealtime(.5f);
            }
            report.failureType="VerificationTimeout";Save();UnityEditor.EditorApplication.ExitPlaymode();
        }
        void Save()=>File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));
        void OnDestroy()
        {
            StopAllCoroutines();tokens.Clear();ResourceBootstrapController.EditorVerificationBackend=null;ResourceBootstrapController.EditorCaptureAuthentication=null;
            PreparedContent.EditorAsset=priorEditorAsset;
            Application.runInBackground=priorBackground;
            if(priorCache.valid)Caching.currentCacheForWriting=priorCache;if(cache.valid)Caching.RemoveCache(cache);if(reusedCache.valid)Caching.RemoveCache(reusedCache);if(completedCache.valid)Caching.RemoveCache(completedCache);if(formationCache.valid)Caching.RemoveCache(formationCache);if(correctedCache.valid)Caching.RemoveCache(correctedCache);
        }
    }
}
#endif
