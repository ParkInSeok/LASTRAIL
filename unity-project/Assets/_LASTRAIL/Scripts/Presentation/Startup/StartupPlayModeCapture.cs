#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Auth;
using LastRail.Resources;
using LastRail.Presentation.AuthGate;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Collections;
using System.Globalization;

namespace LastRail.Presentation.Startup
{
    // Editor-only presentation fixture. Never installed in a player or used as DEV auth.
    public sealed class StartupPlayModeCapture : MonoBehaviour
    {
        public const int Fps=24, FrameTotal=432;
        Camera cameraOwner;
        RenderTexture target;
        Texture2D pixels;
        StartupScreenRootBehaviour startup;
        PlayerEntryPanel panel;
        TMP_Text label;
        string directory;
        int frame, previousCaptureRate;
        int captureHeight=1280;
        bool initialized;
        ResourceBootstrapController boot;
        VersionFixture version;
        bool arrivalCapture;
        bool resourceCapture;
        // Explicit local consent fixture; remove when equivalent PlayMode coverage owns this scenario.
        bool MeteredConsent => directory != null && directory.Contains("metered-consent");
        bool MeteredWifiWait => MeteredConsent && directory.Contains("wifi-wait");
        bool TransitionOnly => directory!=null && directory.Contains("transition-only");
        bool LobbyOnly => directory!=null && directory.Contains("lobby-motion");
        bool PostLogin => directory!=null && directory.Contains("post-login");
        int postLoginFirstFrame=-1;
        bool deckFixture;
        bool RecordFrame => PostLogin ? postLoginFirstFrame>=0 : LobbyOnly ? lobbyFirstFrame>=0 : (!TransitionOnly || arrivalFirstFrame>=0);
        int CaptureFrameTotal => PostLogin && lobbyFirstFrame>=0 ? lobbyFirstFrame+(directory.Contains("random-sites") ? 432 : 96) : LobbyOnly && lobbyFirstFrame>=0 ? lobbyFirstFrame+(directory.Contains("fullpages") ? 504 : 144) : (TransitionOnly && arrivalFirstFrame>=0 ? arrivalFirstFrame+72 : (resourceCapture ? 576 : FrameTotal));
        long pausedBytes;
        LobbyArrivalCaptureFixture.Wire arrivalWire;
        int arrivalFirstFrame=-1, lobbyFirstFrame=-1;
        int restoreEvents, revealFrame=-1;
        string scenario;
        bool saveFrames, audioCapturing;
        int priorStarts;
        StreamWriter telemetry;
        BinaryWriter audioOutput;

        bool InternalAdmission => directory != null && directory.Contains("internal-admission");
        bool InternalDenied => InternalAdmission && directory.Contains("denied");
        bool InternalDeniedReturn => InternalDenied && directory.Contains("denied-return");
        int updateOpens;
        sealed class SyntheticUpdateDestination:ITrustedUpdateDestination
        {public string PlayUrl=>"https://play.google.com/store/apps/details?id=fixture.update.test";}
        bool NetworkCapture=>directory!=null&&directory.Contains("network-error");
        bool NetworkLoginReturn=>NetworkCapture&&directory.Contains("login-return");

        sealed class VersionFixture : IContentStartupBackend, IAuthenticatedContentBackend
        {
            public TaskCompletionSource<ClientContentGate> Reply=new TaskCompletionSource<ClientContentGate>();
            public int Fetches;
            public bool AllowLobby;
            public bool Nonzero;
            public bool Internal, Denied;
            public int ContentCalls, AuthorizedCalls;
            public int DownloadCalls;
            public void FailDownload(){if(download==null||download.Task.IsCompleted)throw new InvalidOperationException("No active fixture download to interrupt");download.TrySetException(new IOException("LOCAL QA interrupted download"));}
            public static ClientContentGate Admission(string receipt) => ClientAdmissionContract.Parse(File.ReadAllText("QA/Unity1Artifacts/Reports/Server2-AdmissionSamples-20260914/" + receipt + ".json"), "2", "https://api.lastrail.app/v1/client-config", "https://example.test/");
            public Task<ClientContentGate> FetchAuthorizedConfigAsync(CancellationToken c)
            {
                c.ThrowIfCancellationRequested(); AuthorizedCalls++;
                return Task.FromResult(SessionContentAuthorization.ParseResponse(Denied ? 403 : 200,
                    File.ReadAllText("QA/Unity1Artifacts/Reports/Server2-AdmissionSamples-20260914/" + (Denied ? "tester-forbidden" : "authorized") + ".json"),
                    "2", "https://api.lastrail.app/v1/client-config", "https://example.test/"));
            }
            public DownloadAccessPolicy Access;
            public long Bytes;
            Action<long,long> progress;
            TaskCompletionSource<bool> download;
            public void Tick()
            {
                if(download==null || download.Task.IsCompleted || !Access.Allowed) return;
                Bytes=Math.Min(80L*1048576,Bytes+349526);
                Access.SetRemaining(80L*1048576-Bytes); progress(Bytes,80L*1048576);
                if(Bytes==80L*1048576) download.TrySetResult(true);
            }
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken cancel) { Fetches++; cancel.Register(()=>Reply.TrySetCanceled()); return Reply.Task; }
            public Task InitializeAsync(CancellationToken c) { ContentCalls++; return Task.CompletedTask; }
            public Task UpdateCatalogAsync(CancellationToken c) { ContentCalls++; return Task.CompletedTask; }
            public Task<long> RequiredBytesAsync(CancellationToken c) { ContentCalls++; return Task.FromResult(Nonzero ? 80L*1048576 : 0L); }
            public Task DownloadAsync(Action<long,long> p,CancellationToken c)
            {
                DownloadCalls++;
                if(!Nonzero) return Task.CompletedTask;
                progress=p; download=new TaskCompletionSource<bool>(); c.Register(()=>download.TrySetCanceled()); return download.Task;
            }
            public Task VerifyRequiredAssetsAsync(CancellationToken c)=>AllowLobby ? Task.CompletedTask : throw new InvalidOperationException("Recording must stop at login.");
        }
        sealed class EmptyStore : IGameApiAuthTokenReader, IGameApiAuthTokenStore
        {
            public Task<GameApiAuthTokenReadResult> ReadAsync()=>Task.FromResult(new GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome.Missing));
            public Task<GameApiAuthSessionSnapshot> LoadAsync()=>Task.FromResult<GameApiAuthSessionSnapshot>(null);
            public Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot s)=>throw new InvalidOperationException("Recording must not authenticate.");
        }
        sealed class NoAuthWire : IRunApiHttpTransport
        {
            readonly string output;
            public NoAuthWire(string directory) {output=directory;}
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
            {
                if(output.Contains("credential-design"))
                {
                    string json=null;
                    if(r.EndpointPath=="/v1/auth/password/recovery-email/verify/start" || r.EndpointPath=="/v1/auth/password/recovery-email/verify/confirm")
                        json="{\"accepted\":true,\"verificationStatus\":\""+(r.EndpointPath.EndsWith("/start")?"PENDING":"VERIFIED")+"\",\"challengeId\":\"local_qa_challenge\",\"expiresAt\":\"2030-01-01T00:00:00.000Z\",\"retryAfterSeconds\":60}";
                    if(r.EndpointPath=="/v1/auth/password/reset/start") json="{\"accepted\":true,\"resetStatus\":\"EMAIL_SENT_IF_AVAILABLE\"}";
                    if(json!=null)
                    {
                        File.AppendAllText(Path.Combine(output,"credential-fixture-requests.txt"),"LOCAL RESPONSE ONLY "+r.EndpointPath+"\n");
                        return Task.FromResult(RunApiHttpResponse.Success(200,json));
                    }
                }
                throw new InvalidOperationException("Recording must not send auth writes/network requests.");
            }
        }

        public void Begin(string output, string scenario = "fast", bool saveFrames = true)
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Actual PlayMode required.");
            var args=Environment.GetCommandLineArgs();int heightArg=Array.IndexOf(args,"-flowHeight");
            if(heightArg>=0&&(heightArg+1>=args.Length||!int.TryParse(args[heightArg+1],out captureHeight)||(captureHeight!=1280&&captureHeight!=1600)))throw new InvalidOperationException("Supported QA heights are1280/1600.");
            directory=Path.GetFullPath(output);
            if(Directory.Exists(directory)) throw new InvalidOperationException("Fresh candidate directory required.");
            Directory.CreateDirectory(directory);
            this.scenario = scenario; this.saveFrames = saveFrames;
            previousCaptureRate=Time.captureFramerate; Time.captureFramerate=Fps;
            telemetry = new StreamWriter(Path.Combine(directory,"playback.csv"));
            telemetry.WriteLine("frame,mediaTime,decodedFrame,starts,white,ended,configAccepted,stage,loginEvents,statusText,titleText,titleAlpha,titleX");
            startup=FindFirstObjectByType<StartupScreenRootBehaviour>();
            if(startup==null) throw new InvalidOperationException("Real ResourceBootstrap startup not loaded.");
            // Exercise the real coordinator -> opening -> PlayerEntryRuntime chain with explicit local version/storage inputs.
            boot=FindFirstObjectByType<ResourceBootstrapController>();
            var observed=startup.CurrentState==null?"none":startup.CurrentState.PhaseKey;
            if(boot==null) throw new InvalidOperationException("Real bootstrap controller required.");
            boot.enabled=false;
            StartupIntroOverlay.ResetEditorCaptureJourney();
            arrivalCapture=directory.Contains("lobby-arrival");
            resourceCapture=directory.Contains("resources");
            version=new VersionFixture {AllowLobby=arrivalCapture,Nonzero=resourceCapture,Internal=InternalAdmission,Denied=InternalDenied};
            ResourceBootstrapController.EditorCaptureBackend=()=>version;
            ResourceBootstrapController.EditorCaptureAuthentication=(owner,font,url)=> {
                if(arrivalCapture) {var localStore=new LobbyArrivalCaptureFixture.Store(); arrivalWire=new LobbyArrivalCaptureFixture.Wire {Output=directory}; return new PlayerEntryRuntime(owner,font,arrivalWire,localStore,localStore);}
                var store=new EmptyStore(); return new PlayerEntryRuntime(owner,font,new NoAuthWire(directory),store,store);
            };
            boot.SessionRestoreRequested+=()=>restoreEvents++;
            boot.enabled=true;
            if(arrivalCapture) LobbyArrivalCaptureFixture.Configure(boot,directory);
            if(resourceCapture)
            {
                version.Access=boot.Coordinator.Access;
                version.Access.SetNetwork(ContentNetwork.Wifi);
                // Exercise the existing Wi-Fi departure policy, which defers consent until login.
                if(MeteredConsent) version.Access.SetNetwork(ContentNetwork.OtherOnline);
            }
            var panels=boot.GetComponentsInChildren<PlayerEntryPanel>(true);
            panel=panels[panels.Length-1];
            var cam=new GameObject("LocalQaCaptureCamera"); cameraOwner=cam.AddComponent<Camera>();
            if(FindFirstObjectByType<AudioListener>()==null) cam.AddComponent<AudioListener>();
            if(saveFrames)
            {
                if(AudioSettings.speakerMode!=AudioSpeakerMode.Stereo) throw new InvalidOperationException("Capture expects stereo output.");
                audioCapturing=AudioRenderer.Start();
                if(!audioCapturing) throw new InvalidOperationException("Audio capture did not start.");
                audioOutput=new BinaryWriter(File.Create(Path.Combine(directory,"audio.f32le")));
                File.WriteAllText(Path.Combine(directory,"audio-rate.txt"),AudioSettings.outputSampleRate.ToString());
            }
            cameraOwner.clearFlags=CameraClearFlags.SolidColor; cameraOwner.backgroundColor=new Color(.02f,.025f,.03f);
            cameraOwner.orthographic=true; cameraOwner.orthographicSize=6.4f; cameraOwner.nearClipPlane=.1f; cameraOwner.farClipPlane=100;
            cam.transform.position=new Vector3(0,0,-10);
            target=new RenderTexture(720,captureHeight,24,RenderTextureFormat.ARGB32); target.Create(); cameraOwner.targetTexture=target;
            pixels=new Texture2D(720,captureHeight,TextureFormat.RGB24,false);
            var qa=new GameObject("LocalQaDisclosure",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            var canvas=qa.GetComponent<Canvas>(); canvas.sortingOrder=1000;
            var scaler=qa.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(720,1280);
            var text=new GameObject("Disclosure",typeof(RectTransform)); text.transform.SetParent(qa.transform,false);
            var r=(RectTransform)text.transform; r.anchorMin=new Vector2(0,1); r.anchorMax=Vector2.one; r.pivot=new Vector2(.5f,1); r.anchoredPosition=new Vector2(0,-12); r.sizeDelta=new Vector2(-32,100);
            label=text.AddComponent<TextMeshProUGUI>(); label.font=startup.GetComponentInChildren<TMP_Text>(true).font; label.fontSize=19; label.color=new Color(1,.85f,.45f); label.alignment=TextAlignmentOptions.TopLeft; label.raycastTarget=false;
            foreach(var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            { c.renderMode=RenderMode.ScreenSpaceCamera; c.worldCamera=cameraOwner; c.planeDistance=5; c.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1; }
            File.WriteAllText(Path.Combine(directory,"capture-scope.txt"),"Actual Unity PlayMode camera frames + AudioRenderer output, 720x"+captureHeight+", 24fps, 18s. Real local Flow MP4 VideoPlayer/ResourceBootstrap/DownloadCoordinator/PlayerEntryRuntime. Scenario="+scenario+". Local version and missing-token fixture; no authentication or lobby entry. NOT DEV/device E2E. Veo watermark retained. No generated frame-sequence substitute. Original boot="+observed+".\n");
            if(arrivalCapture) File.WriteAllText(Path.Combine(directory,"capture-scope.txt"),"Actual PlayMode opening -> Guest click -> nickname submit -> delayed local accepted name/profile/story -> existing Main scene and MissionShellScreen binding -> .8s route + 1.2s shared detailed map approach + .5s lobby UI reveal (no screen flash). Local in-memory auth/store and source Addressables providers; no external API/download or credential persistence. NOT DEV/device E2E. Existing lobby shell is not the next approved lobby mockup. Original video/media preserved.");
            if (InternalAdmission) File.WriteAllText(Path.Combine(directory,"capture-scope.txt"),
                "Actual local Bootstrap/Guest/nickname/auth flow with frozen synthetic Server admission receipts. " +
                (InternalDenied ? "Tester denial must block all content operations and Main." : "Authorized local fixture may proceed through resource preparation and Main.") +
                " Transport is an explicit fixture; not deployed PRD or real account entitlement validation. No external API, live admission, START or economy.");
            initialized=true;
        }

        void LateUpdate()
        {
            if(!initialized) return;
            try
            {
                float seconds=frame/(float)Fps;
                if(frame==2*Fps && scenario!="slow")
                {
                    if(scenario=="failed") version.Reply.TrySetException(new IOException("Local QA version failure"));
                    else version.Reply.TrySetResult(InternalAdmission ? VersionFixture.Admission("auth-required") : new ClientContentGate {MinimumVersion=scenario=="update" ? "999.0.0" : "0.0.0"});
                }
                if(frame==3*Fps && scenario=="reentry")
                {
                    priorStarts=boot.Opening.PlaybackStarts;
                    boot.enabled=false;
                    ResourceBootstrapController.EditorCaptureBackend=()=>version;
                    ResourceBootstrapController.EditorCaptureAuthentication=(owner,font,url)=> {
                if(arrivalCapture) {var localStore=new LobbyArrivalCaptureFixture.Store(); arrivalWire=new LobbyArrivalCaptureFixture.Wire {Output=directory}; return new PlayerEntryRuntime(owner,font,arrivalWire,localStore,localStore);}
                var store=new EmptyStore(); return new PlayerEntryRuntime(owner,font,new NoAuthWire(directory),store,store);
            };
                    boot.enabled=true;
                    var panels=boot.GetComponentsInChildren<PlayerEntryPanel>(true); panel=panels[panels.Length-1];
                }
                if(frame==10*Fps && (scenario=="slow" || scenario=="failed" || scenario=="update"))
                {
                    if(boot.Opening.Timeline.Revealed || restoreEvents!=0 || panel.gameObject.activeSelf) throw new InvalidOperationException("Closed version gate admitted login.");
                    if((scenario=="failed" || scenario=="update") && boot.Opening.GetComponent<CanvasGroup>().blocksRaycasts) throw new InvalidOperationException("Version error/retry UI is obstructed.");
                }
                if(frame==12*Fps && scenario=="slow") version.Reply.TrySetResult(new ClientContentGate {MinimumVersion="0.0.0"});
                if(frame==12*Fps && (scenario=="failed" || scenario=="update"))
                {
                    version.Reply=new TaskCompletionSource<ClientContentGate>();
                    _ = boot.Coordinator.StartAsync("1.0.0",CancellationToken.None);
                    version.Reply.TrySetResult(new ClientContentGate {MinimumVersion="0.0.0"});
                }
                var timeline=boot.Opening.Timeline;
                var opening=boot.Opening;
                if(opening.MediaFailed) throw new InvalidOperationException("Video decoder fallback occurred; this is not valid video QA.");
                if(opening.PlaybackStarts+priorStarts>1) throw new InvalidOperationException("Video restarted during retry/reentry.");
                if(!timeline.Revealed && (restoreEvents!=0 || panel.gameObject.activeSelf)) throw new InvalidOperationException("Login appeared before opening/version gate.");
                if(timeline.Revealed && revealFrame<0) revealFrame=frame;
                if(restoreEvents>1) throw new InvalidOperationException("Opening dispatched login more than once.");
                var activePlayers=FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsSortMode.None);
                if(activePlayers.Length>1) throw new InvalidOperationException("Duplicate video/audio player.");
                if(opening.Player!=null && (opening.Player.audioOutputMode!=UnityEngine.Video.VideoAudioOutputMode.AudioSource || opening.GetComponents<AudioSource>().Length!=1)) throw new InvalidOperationException("Audio must have one output owner.");
                var versionLabel=opening.transform.Find("VersionStatus").GetComponent<TMP_Text>();
                var titleLabel=opening.transform.Find("GameTitle").GetComponentInChildren<RawImage>();
                telemetry.WriteLine(string.Join(",",frame,timeline.Travel.ToString("F4",CultureInfo.InvariantCulture),opening.DecodedFrame,opening.PlaybackStarts+priorStarts,opening.WhiteOpacity.ToString("F4",CultureInfo.InvariantCulture),opening.MediaEnded,boot.Coordinator.ConfigAccepted,boot.Coordinator.Stage,restoreEvents,versionLabel.text,titleLabel.texture.name,titleLabel.color.a.ToString("F4",CultureInfo.InvariantCulture),titleLabel.rectTransform.anchoredPosition.x.ToString("F2",CultureInfo.InvariantCulture)));
                var phase=timeline.Revealed?"CABIN LOGIN":timeline.Transition>0?"SUNLIGHT TRANSITION":"ONE-SHOT JOURNEY";
                label.fontSize=16;
                label.text=(arrivalCapture ? "LOCAL QA · 로컬 인증/콘텐츠 · " : "LOCAL QA · 로컬 버전 응답 · ")+seconds.ToString("00.0")+"s";
                Canvas.ForceUpdateCanvases();
                if(directory.Contains("login-design"))
                {
                    if(frame==216 || frame==384) ClickLoginControl("ChooseIdLogin");
                    if(frame==336)
                    {
                        panel.GetComponentsInChildren<TMP_InputField>().Single(x=>x.name=="Password").SetTextWithoutNotify("synthetic_secret");
                        ClickLoginControl("BackToMethods");
                        if(panel.GetComponentsInChildren<TMP_InputField>(true).Single(x=>x.name=="Password").text!="") throw new InvalidOperationException("Back retained password.");
                    }
                    if(frame==192 || frame==288 || frame==360 || frame==408)
                    {
                        foreach(var text in panel.GetComponentsInChildren<TMP_Text>()) {text.ForceMeshUpdate(); if(text.isTextOverflowing) throw new InvalidOperationException("Login text overflow: "+text.name);}
                    }
                }
                if(directory.Contains("credential-design")) CaptureCredentialPages();
                if(scenario=="update"&&directory.Contains("native-popup"))CaptureUpdateActions();
                if(arrivalCapture) CaptureArrival();
                if(resourceCapture) CaptureResources();
                if(PostLogin && postLoginFirstFrame<0 && frame>=192 && FindFirstObjectByType<ResourcePreparationView>()!=null && !panel.gameObject.activeSelf) postLoginFirstFrame=frame;
                foreach(var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                { c.renderMode=RenderMode.ScreenSpaceCamera; c.worldCamera=cameraOwner; c.planeDistance=5; c.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1; }
                foreach(var motion in FindObjectsByType<StartupMotionBackdrop>(FindObjectsSortMode.None)) motion.Evaluate(0);
                Canvas.ForceUpdateCanvases();
                if(directory.Contains("copy-align") && (frame==138 || frame==147 || frame==162 || frame==204 || frame==270 || frame==348)) CheckButtonAlignment();
                cameraOwner.Render();
                var previous=RenderTexture.active; RenderTexture.active=target;
                try { pixels.ReadPixels(new Rect(0,0,720,captureHeight),0,0); pixels.Apply(false); }
                finally { RenderTexture.active=previous; }
                if(saveFrames && RecordFrame) File.WriteAllBytes(Path.Combine(directory,frame.ToString("D6")+".png"),pixels.EncodeToPNG());
                if(audioCapturing)
                {
                    using(var samples=new NativeArray<float>(AudioRenderer.GetSampleCountForCaptureFrame()*2,Allocator.Temp))
                    {
                        if(!AudioRenderer.Render(samples)) throw new InvalidOperationException("Audio render failed.");
                        if(RecordFrame) for(int i=0;i<samples.Length;i++) audioOutput.Write(samples[i]);
                    }
                }
                frame++;
                if(frame%Fps==0) File.WriteAllText(Path.Combine(directory,"progress.txt"),frame+"/"+CaptureFrameTotal);
                if(frame>=CaptureFrameTotal)
                {
                    int expectedFetches=scenario=="failed" || scenario=="update" || scenario=="reentry" || NetworkLoginReturn || InternalDeniedReturn ? 2 : 1;
                    if(restoreEvents!=1 || !timeline.Finished || (!arrivalCapture && !panel.gameObject.activeSelf) || version.Fetches!=expectedFetches || opening.PlaybackStarts+priorStarts!=1) throw new InvalidOperationException("Connected one-shot/login assertions failed.");
                    if(scenario!="reentry" && opening.DecodedFrame<94) throw new InvalidOperationException("Video never reached its final decoded frames.");
                    FinishOutputs();
                    if(TransitionOnly) File.WriteAllText(Path.Combine(directory,"clip.json"),"{\"startFrame\":"+arrivalFirstFrame+",\"frames\":72,\"fps\":24,\"seconds\":3}");
                    if(LobbyOnly) File.WriteAllText(Path.Combine(directory,"clip.json"),"{\"startFrame\":"+lobbyFirstFrame+",\"frames\":"+(frame-lobbyFirstFrame)+",\"fps\":24,\"seconds\":"+((frame-lobbyFirstFrame)/24f).ToString("F6",CultureInfo.InvariantCulture)+"}");
                    if(PostLogin) File.WriteAllText(Path.Combine(directory,"clip.json"),"{\"startFrame\":"+postLoginFirstFrame+",\"frames\":"+(frame-postLoginFirstFrame)+",\"fps\":24,\"seconds\":"+((frame-postLoginFirstFrame)/24f).ToString("F6",CultureInfo.InvariantCulture)+"}");
                    File.WriteAllText(Path.Combine(directory,"complete.json"),"{\"frames\":"+frame+",\"fps\":24,\"width\":720,\"height\":"+captureHeight+",\"seconds\":"+(CaptureFrameTotal/Fps)+",\"actualPlayMode\":true,\"flowVideoPlayer\":true,\"scenario\":\""+scenario+"\",\"connectedBootstrap\":true,\"versionFetches\":"+version.Fetches+",\"videoStarts\":"+(opening.PlaybackStarts+priorStarts)+",\"lastDecodedFrame\":"+opening.DecodedFrame+",\"prepareSeconds\":"+opening.PreparationSeconds.ToString("F4",CultureInfo.InvariantCulture)+",\"loginRevealEvents\":"+restoreEvents+",\"loginRevealFrame\":"+revealFrame+",\"localVersionFixture\":true,\"devE2E\":false,\"lobbyEntered\":"+(lobbyFirstFrame >= 0 ? "true" : "false")+"}");
                    initialized=false; Time.captureFramerate=previousCaptureRate; EditorApplication.isPlaying=false;
                }
            }
            catch(Exception e) { File.WriteAllText(Path.Combine(directory,"failed.txt"),e.ToString()); FinishOutputs(); initialized=false; Time.captureFramerate=previousCaptureRate; EditorApplication.isPlaying=false; }
        }
        void CaptureResources()
        {
            if(NetworkCapture){CaptureNetworkError();return;}
            if(MeteredConsent && frame==230)
            {
                if(!version.Access.NeedsConsent || version.Bytes!=0 || FindFirstObjectByType<MainSceneRoot>()!=null) throw new InvalidOperationException("Metered consent did not gate local download and Main.");
                File.WriteAllText(Path.Combine(directory,"resource-checks.txt"),"Explicit metered local fixture: consent blocks bytes and Main before approval PASS\n");
            }
            if(MeteredConsent && frame==240) ClickControl(boot,MeteredWifiWait?"DeferDownload":"ApproveDownload");
            if(MeteredWifiWait && frame==251)
            {
                if(version.Access.Allowed||version.Access.NeedsConsent||version.Bytes!=0||FindFirstObjectByType<MainSceneRoot>()!=null)throw new InvalidOperationException("Wi-Fi wait bypassed download or Main gate");
                File.AppendAllText(Path.Combine(directory,"resource-checks.txt"),"Native Wi-Fi wait: consent closed, zero bytes and Main absent before simulated Wi-Fi PASS\n");
            }
            if(MeteredWifiWait && frame==252)version.Access.SetNetwork(ContentNetwork.Wifi);
            if(!PostLogin && !MeteredConsent && frame==240) { ClickControl(boot,"PauseDownload"); pausedBytes=version.Bytes; }
            if(!PostLogin && !MeteredConsent && frame==276)
            {
                if(!version.Access.Paused || version.Bytes!=pausedBytes || FindFirstObjectByType<MainSceneRoot>()!=null) throw new InvalidOperationException("Download pause/gate failed.");
                File.WriteAllText(Path.Combine(directory,"resource-checks.txt"),"Nonzero80MiB local coordinator callbacks; explicit pause held bytes and blocked Main PASS\n");
            }
            if(!PostLogin && !MeteredConsent && frame==288) ClickControl(boot,"PauseDownload");
            version.Tick();
            if((frame==230 || frame==270 || frame==320) && (!PostLogin || FindFirstObjectByType<ResourcePreparationView>()!=null))
            {
                var view=FindFirstObjectByType<ResourcePreparationView>();
                if(view==null) throw new InvalidOperationException("Resource presentation missing after login.");
                foreach(var text in view.GetComponentsInChildren<TMP_Text>()) {text.ForceMeshUpdate(); if(text.isTextOverflowing) throw new InvalidOperationException("Resource text overflow: "+text.name);}
                File.AppendAllText(Path.Combine(directory,"resource-view-checks.txt"),frame+" no overflow PASS\n");
            }
            if(frame==CaptureFrameTotal-1)
            {
                if(version.Bytes!=80L*1048576 || version.Access.Paused) throw new InvalidOperationException("Nonzero fixture did not complete.");
                File.AppendAllText(Path.Combine(directory,"resource-checks.txt"),(MeteredWifiWait ? "After native wait, simulated Wi-Fi completed local80MiB operation; Main only after completion PASS\n" : MeteredConsent ? "Actual consent approval completed local80MiB operation; Main only after completion PASS\n" : PostLogin ? "Continuous80MiB local QA download; Main only after completion PASS\n" : "Actual resume completed same80MiB operation; actual Main reached after resource completion PASS\n"));
            }
        }

        void CaptureNetworkError()
        {
            if(frame==240){pausedBytes=version.Bytes;version.FailDownload();}
            if(frame==252){
                if(startup.CurrentState.PhaseKey!="network_interrupted"||FindFirstObjectByType<MainSceneRoot>()!=null||version.Bytes!=pausedBytes||FindFirstObjectByType<ResourcePreparationView>()!=null)throw new InvalidOperationException("Network failure did not hold bytes/Main or expose popup");
                var backdrop=startup.GetComponentInChildren<StartupMotionBackdrop>();if(backdrop==null||!backdrop.IsLogin)throw new InvalidOperationException("Actual cabin backdrop missing");
                File.WriteAllText(Path.Combine(directory,"network-checks.txt"),"Actual coordinator failure -> network popup, cabin backdrop, no Main, fixture bytes held PASS\n");
            }
            if(frame==264){ClickControl(startup,"StartupSupport");if(FindFirstObjectByType<LastRail.Support.SupportInquiryPanel>()==null)throw new InvalidOperationException("Authenticated startup support did not open");File.AppendAllText(Path.Combine(directory,"network-checks.txt"),"Native support click opened existing authenticated panel; no submit PASS\n");}
            if(frame==276)ClickControl(FindFirstObjectByType<LastRail.Support.SupportInquiryPanel>(),"Close");
            if(frame==288)ClickControl(startup,NetworkLoginReturn?"StartupSecondary":"StartupPrimary");
            if(frame==312){
                if(NetworkLoginReturn){if(boot.Authentication.Owner.Ready||boot.Authentication.Owner.Session!=null||version.Access.Authenticated||!panel.gameObject.activeInHierarchy||FindFirstObjectByType<MainSceneRoot>()!=null)throw new InvalidOperationException("Native login return failed to clear authenticated entry");}
                else if(version.DownloadCalls!=2||boot.Coordinator.Stage!=ContentStage.Downloading)throw new InvalidOperationException("Native retry did not restart coordinator download");
                File.AppendAllText(Path.Combine(directory,"network-checks.txt"),NetworkLoginReturn?"Native login return cleared auth/Main and showed login PASS\n":"Native retry started exactly one replacement fixture download PASS\n");
            }
            version.Tick();
            if(frame==CaptureFrameTotal-1){if(!NetworkLoginReturn&&version.Bytes!=80L*1048576)throw new InvalidOperationException("Network fixture final byte boundary");File.AppendAllText(Path.Combine(directory,"network-checks.txt"),NetworkLoginReturn?"Existing Wi-Fi background content policy retained; no authenticated Main entry. Download calls="+version.DownloadCalls+", bytes="+version.Bytes+"\n":"Final byte boundary PASS. Retention is fixture evidence, not production partial-file resume proof.\n");}
        }

        void CaptureUpdateActions()
        {
            if(frame==252){ClickControl(startup,"StartupPrimary");File.WriteAllText(Path.Combine(directory,"update-checks.txt"),"Native missing-destination click: "+startup.CurrentState.Details+"\n");}
            if(frame==264||frame==276){bool opened=frame==276;var destination=new SyntheticUpdateDestination();boot.ConfigureRequiredUpdateAction(new RequiredUpdateAction(destination,new[]{destination.PlayUrl},_=>{updateOpens++;return opened;}));ClickControl(startup,"StartupPrimary");File.AppendAllText(Path.Combine(directory,"update-checks.txt"),"Native in-memory opener "+(opened?"accepted":"failed")+": "+startup.CurrentState.Details+"\n");}
            if(frame==282){if(updateOpens!=2||boot.Coordinator.ConfigAccepted||boot.Coordinator.CanEnterGame||boot.Coordinator.Stage!=ContentStage.UpdateRequired||version.ContentCalls!=0)throw new InvalidOperationException("Update action bypassed gate or invoked duplicate opener");File.AppendAllText(Path.Combine(directory,"update-checks.txt"),"Two memory opener calls; content0 and admission blocked PASS. No browser/store launched. Later version-gate release is synthetic fixture input, not actual app update.\n");}
        }
        void CaptureArrival()
        {
            if(frame==144) ClickLoginControl("Guest");
            if(frame==174) panel.GetComponentsInChildren<TMP_InputField>().Single(x=>x.name=="Nickname").text="철도지휘관";
            if(frame==192) ClickLoginControl("SubmitNickname");
            if(InternalDeniedReturn&&frame==300){if(startup.CurrentState.Kind!=StartupScreenKind.InternalAccessDenied||version.ContentCalls!=0||version.Bytes!=0)throw new InvalidOperationException("Expected actual denied state before return");File.AppendAllText(Path.Combine(directory,"arrival-gates.txt"),"Frozen tester_forbidden blocks content before native login return PASS\n");}
            if(InternalDeniedReturn&&frame==312)ClickControl(startup,"StartupPrimary");
            if(InternalDeniedReturn&&frame==336){if(boot.Authentication.Owner.Ready||boot.Authentication.Owner.Session!=null||boot.Coordinator.Access.Authenticated||!panel.gameObject.activeInHierarchy)throw new InvalidOperationException("Denied native return did not clear session");File.AppendAllText(Path.Combine(directory,"arrival-gates.txt"),"Native denied login return clears owner/session, visible login, content0 PASS\n");}
            var main=FindFirstObjectByType<MainSceneRoot>();
            var transition=FindFirstObjectByType<LobbyArrivalTransition>();
            if(frame==210)
            {
                if(arrivalWire.NameWrites!=1 || boot.Authentication.Owner.Ready || transition!=null || main!=null) throw new InvalidOperationException("Nickname acknowledgement gate bypassed.");
                if(resourceCapture && !InternalAdmission && (panel.gameObject.activeSelf || FindFirstObjectByType<ResourcePreparationView>()==null)) throw new InvalidOperationException("Nickname wait must stay on the resource screen, not the old connecting panel.");
                if(InternalAdmission && (version.ContentCalls != 0 || version.AuthorizedCalls != 0)) throw new InvalidOperationException("Internal content plan was accessed before auth preparation completed.");
                File.WriteAllText(Path.Combine(directory,"arrival-gates.txt"),"Nickname acknowledgement pending: no ready auth, arrival, or Main PASS\n");
            }
            if(frame==216) arrivalWire.NameAccepted.TrySetResult(true);
            if(transition!=null && arrivalFirstFrame<0) arrivalFirstFrame=frame;
            if(main!=null && main.ContentRoot.gameObject.activeInHierarchy && lobbyFirstFrame<0) lobbyFirstFrame=frame;
            if((LobbyOnly || PostLogin) && main!=null && !deckFixture) {
                var deck=main.GetComponentInChildren<LastRail.Presentation.MissionShell.LobbyCommandDeck>(true);
                if(deck!=null){deck.SetEditorVisualFixture();deckFixture=true;}
            }
            if(directory.Contains("fullpages") && main!=null && lobbyFirstFrame>=0)
            {
                var deck=main.GetComponentInChildren<LastRail.Presentation.MissionShell.LobbyCommandDeck>(true);
                int offset=frame-lobbyFirstFrame;
                if(offset>=24 && offset<=384 && (offset-24)%72==0)
                {
                    int page=(offset-24)/72;
                    string buttonName=page<4 ? "Navigation"+(page+1) : page==4 ? "Formation" : "Shortcut2";
                    // Return through native Back, then click the actual lobby destination button.
                    if(deck.Pages!=null && deck.Pages.IsOpen)deck.Pages.GetComponentsInChildren<Button>().Single(x=>x.name=="Back").onClick.Invoke();
                    deck.GetComponentsInChildren<Button>().Single(x=>x.name==buttonName).onClick.Invoke();
                    if(deck.Pages==null || !deck.Pages.IsOpen || (int)deck.Pages.CurrentPage!=page)throw new InvalidOperationException("Destination route failed: "+buttonName);
                    File.AppendAllText(Path.Combine(directory,"destination-routes.txt"),frame+" "+deck.Pages.CurrentPage+" native button PASS\n");
                }
                if(offset==456 && deck.Pages!=null)deck.Pages.GetComponentsInChildren<Button>().Single(x=>x.name=="Back").onClick.Invoke();
            }
            if(transition!=null && transition.Illuminating && main!=null && main.ContentRoot.gameObject.activeInHierarchy) throw new InvalidOperationException("Main revealed before lighting finished.");
            if(directory.Contains("interference") && main!=null && RecordFrame) {
                var cabin=main.GetComponentInChildren<LastRail.Presentation.MissionShell.LobbyCabinMotion>();
                if(cabin!=null && cabin.InterferenceLevel>0)
                    File.AppendAllText(Path.Combine(directory,"interference-events.txt"),frame+" site="+cabin.InterferenceSite+" level="+cabin.InterferenceLevel.ToString("F4",CultureInfo.InvariantCulture)+"\n");
            }
            if(frame==CaptureFrameTotal-1)
            {
                if(NetworkLoginReturn){if(main!=null||boot.Authentication.Owner.Ready||!panel.gameObject.activeInHierarchy)throw new InvalidOperationException("Login-return final gate");return;}
                if (InternalDenied)
                {
                    if(InternalDeniedReturn){if(main!=null||transition!=null||version.ContentCalls!=0||version.Bytes!=0||boot.Authentication.Owner.Ready||!panel.gameObject.activeInHierarchy)throw new InvalidOperationException("Denied return final gate");return;}
                    if (main != null || transition != null || version.ContentCalls != 0 || version.Bytes != 0 || version.AuthorizedCalls != 1 || startup.CurrentState.Kind != StartupScreenKind.InternalAccessDenied)
                        throw new InvalidOperationException("Internal denial did not block actual Bootstrap content and Main.");
                    File.AppendAllText(Path.Combine(directory,"arrival-gates.txt"), "Actual Bootstrap + local frozen tester_forbidden: native denial view, contentCalls0/bytes0/Main absent PASS\n");
                    return;
                }
                if(main==null || !main.ContentRoot.gameObject.activeInHierarchy || transition!=null || !boot.Authentication.Owner.Ready || startup.gameObject.activeSelf) throw new InvalidOperationException("Arrival did not finish into actual Main.");
                if(directory.Contains("shared-map")) {
                    var map=main.GetComponentInChildren<SharedRailMap>();
                    if(map==null || map.Map.texture.width<1400) throw new InvalidOperationException("Detailed shared lobby map missing.");
                    var rt=(RectTransform)map.transform; var rootCanvas=map.GetComponentInParent<Canvas>();
                    var pos=rt.TransformPoint(new Vector3((SharedRailMap.Landmark.x-.5f)*rt.rect.width,(.5f-SharedRailMap.Landmark.y)*rt.rect.height));
                    var local=((RectTransform)rootCanvas.transform).InverseTransformPoint(pos);
                    if(Mathf.Abs(local.x)>2 || Mathf.Abs(local.y)>2) throw new InvalidOperationException("Lobby landmark no longer centered.");
                    File.WriteAllText(Path.Combine(directory,"shared-map-checks.txt"),"Detailed map in actual Main, same centered landmark PASS. Texture="+map.Map.texture.width+"x"+map.Map.texture.height+" GPU bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(map.Map.texture));
                }
                var profile=main.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="PlayerProfileName");
                if(profile.text!="철도지휘관" || arrivalWire.NameWrites!=1 || lobbyFirstFrame-arrivalFirstFrame<46) throw new InvalidOperationException("Profile/one submit/lighting duration mismatch.");
                File.AppendAllText(Path.Combine(directory,"arrival-gates.txt"),"Actual Main + profile bound + one nickname write + arrival complete PASS\narrivalFrame="+arrivalFirstFrame+" lobbyRevealFrame="+lobbyFirstFrame+"\n");
            }
        }

        void CheckButtonAlignment()
        {
            foreach(var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var graphic=button.targetGraphic as Image;
                if(graphic==null || graphic.sprite==null || (graphic.sprite.name!="primary" && graphic.sprite.name!="secondary")) continue;
                var label=button.GetComponentInChildren<TMP_Text>(); if(label==null) continue;
                label.ForceMeshUpdate();
                var min=new Vector2(float.MaxValue,float.MaxValue); var max=new Vector2(float.MinValue,float.MinValue);
                foreach(var ch in label.textInfo.characterInfo)
                {
                    if(!ch.isVisible) continue;
                    min=Vector2.Min(min,ch.bottomLeft); max=Vector2.Max(max,ch.topRight);
                }
                var rect=(RectTransform)button.transform;
                var actual=(Vector2)rect.InverseTransformPoint(label.transform.TransformPoint((min+max)*.5f));
                float shift=graphic.sprite.name=="primary" ? 5.5f/310 : 5f/306;
                var expected=rect.rect.center+new Vector2(rect.rect.width*shift,-rect.rect.height*.03f);
                var delta=actual-expected;
                if(Mathf.Abs(delta.x)>4 || Mathf.Abs(delta.y)>1) throw new InvalidOperationException("Visible glyph center mismatch: "+button.name+" "+delta);
                File.AppendAllText(Path.Combine(directory,"button-alignment.txt"),frame+" "+button.name+" "+label.text+" delta="+delta+" PASS\n");
            }
        }

        PasswordCredentialPanel ActiveCredential() => boot.GetComponentsInChildren<PasswordCredentialPanel>().Single();
        TMP_InputField Field(Component root,string name) => root.GetComponentsInChildren<TMP_InputField>(true).Single(x=>x.name==name);
        void CaptureCredentialPages()
        {
            if(frame==144) ClickControl(panel,"ChooseIdLogin");
            if(frame==150) ClickControl(panel,"Register");
            if(frame==174)
            {
                var modal=ActiveCredential(); Field(modal,"LoginId").SetTextWithoutNotify("qa_commander"); Field(modal,"RecoveryEmail").SetTextWithoutNotify("synthetic@example.invalid");
                Field(modal,"Password").SetTextWithoutNotify("fixture_password");Field(modal,"ConfirmPassword").SetTextWithoutNotify("fixture_password");foreach(var consent in modal.GetComponentsInChildren<Toggle>())consent.isOn=true;
                ClickControl(modal,"SendCode");
            }
            if(frame==192) {var modal=ActiveCredential(); Field(modal,"VerificationCode").SetTextWithoutNotify("00123456"); ClickControl(modal,"ConfirmCode");}
            if(frame==228) ProbeKeyboard(ActiveCredential(),"ConfirmPassword",.38f);
            if(frame==234) ActiveCredential().GetComponent<AuthKeyboardAvoidance>().EvaluateEditorKeyboard(0,null);
            if(frame==240) ClickControl(ActiveCredential(),"BackCredential");
            if(frame==246) ClickControl(panel,"ChooseIdLogin");
            if(frame==252) ClickControl(panel,"ResetPassword");
            if(frame==282)
            {
                var modal=ActiveCredential(); Field(modal,"LoginId").SetTextWithoutNotify("qa_commander"); Field(modal,"RecoveryEmail").SetTextWithoutNotify("synthetic@example.invalid"); ClickControl(modal,"SendCode");
            }
            if(frame==300) ProbeKeyboard(ActiveCredential(),"ConfirmPassword",.46f);
            if(frame==306) ActiveCredential().GetComponent<AuthKeyboardAvoidance>().EvaluateEditorKeyboard(0,null);
            if(frame==312) ClickControl(ActiveCredential(),"BackCredential");
            if(frame==324)
            {
                panel.ShowNickname();
                File.WriteAllText(Path.Combine(directory,"nickname-preview-scope.txt"),"Nickname page explicitly previewed through public ShowNickname; no authentication/profile success or lobby entry is simulated. Real registration-to-nickname ownership separately covered by EditMode runtime tests.");
            }
            if(frame==384) ProbeKeyboard(panel,"Nickname",.42f);
            if(frame==390) {panel.GetComponent<AuthKeyboardAvoidance>().EvaluateEditorKeyboard(0,null); panel.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=1;}
            if(frame==162 || frame==204 || frame==270 || frame==294 || frame==348)
            {
                var view=frame>=324?(Component)panel:ActiveCredential();
                Canvas.ForceUpdateCanvases();
                foreach(var text in view.GetComponentsInChildren<TMP_Text>()) { text.ForceMeshUpdate(); if(text.isTextOverflowing) throw new InvalidOperationException("Credential text overflow: "+text.name); }
                File.AppendAllText(Path.Combine(directory,"credential-ui-checks.txt"),frame+" text bounds PASS\n");
            }
        }
        void ProbeKeyboard(Component root,string field,float fraction)
        {
            var input=Field(root,field); var avoid=root.GetComponent<AuthKeyboardAvoidance>();
            var scroll=root.GetComponentInChildren<ScrollRect>();
            Canvas.ForceUpdateCanvases(); avoid.EvaluateEditorKeyboard(Screen.height*fraction,input); Canvas.ForceUpdateCanvases();
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,(RectTransform)input.transform);
            if(bounds.min.y<scroll.viewport.rect.yMin+23 || bounds.max.y>scroll.viewport.rect.yMax-23) throw new InvalidOperationException("Keyboard overlap: "+field);
            File.AppendAllText(Path.Combine(directory,"keyboard-layout-checks.txt"),frame+" "+root.GetType().Name+"/"+field+" simulated fraction "+fraction+" focused field visible PASS; native keyboard NOT tested\n");
        }
        void ClickControl(Component root,string name)
        {
            var button=root.GetComponentsInChildren<Button>().Single(x=>x.name==name);
            var scroll=root.GetComponentInChildren<ScrollRect>();
            foreach(var c in root.GetComponentsInChildren<Canvas>()) {c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cameraOwner;c.planeDistance=5;}
            Canvas.ForceUpdateCanvases();
            if(scroll!=null && button.transform.IsChildOf(scroll.content))
            {
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,(RectTransform)button.transform);
                var position=scroll.content.anchoredPosition;
                if(bounds.min.y<scroll.viewport.rect.yMin+16) position.y+=scroll.viewport.rect.yMin+16-bounds.min.y;
                if(bounds.max.y>scroll.viewport.rect.yMax-16) position.y+=scroll.viewport.rect.yMax-16-bounds.max.y;
                position.y=Mathf.Clamp(position.y,0,Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height));
                scroll.StopMovement();scroll.content.anchoredPosition=position; Canvas.ForceUpdateCanvases();
            }
            if(!button.interactable) throw new InvalidOperationException("QA target disabled: "+name);
            var events=EventSystem.current; if(events==null) events=new GameObject("CredentialQaEventSystem").AddComponent<EventSystem>();
            var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(events) {position=RectTransformUtility.WorldToScreenPoint(cameraOwner,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>(); events.RaycastAll(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=button) throw new InvalidOperationException("Credential QA target occluded: "+name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            File.AppendAllText(Path.Combine(directory,"credential-interactions.txt"),frame+" "+name+" raycast/click PASS\n");
        }

        void ClickLoginControl(string name)
        {
            var button=panel.GetComponentsInChildren<Button>().Single(x=>x.name==name);
            if(!button.interactable) throw new InvalidOperationException("QA target disabled: "+name);
            var events=EventSystem.current;
            if(events==null) events=new GameObject("LoginQaEventSystem").AddComponent<EventSystem>();
            var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(events) { position=RectTransformUtility.WorldToScreenPoint(cameraOwner,rect.TransformPoint(rect.rect.center)), button=PointerEventData.InputButton.Left };
            var hits=new List<RaycastResult>(); events.RaycastAll(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=button) throw new InvalidOperationException("QA target occluded: "+name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            File.AppendAllText(Path.Combine(directory,"login-interactions.txt"),frame+" "+name+" raycast/click PASS\n");
        }

        void FinishOutputs()
        {
            telemetry?.Dispose(); telemetry=null; audioOutput?.Dispose(); audioOutput=null;
            if(audioCapturing) { AudioRenderer.Stop(); audioCapturing=false; }
        }
        void OnDestroy()
        {
            FinishOutputs();
            if(initialized) Time.captureFramerate=previousCaptureRate;
            if(target!=null){target.Release(); Destroy(target);} if(pixels!=null) Destroy(pixels);
        }
    }
}
#endif
