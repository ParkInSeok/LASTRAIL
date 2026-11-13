using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using LastRail.Resources;

namespace LastRail.Presentation.Startup
{
    public sealed class StartupIntroOverlay : MonoBehaviour
    {
        public const string VideoRelativePath = "Startup/lastrail-opening-landscape-4s-v01.mp4";
        // The playback copy is retimed from the unchanged 8s source to 4s.
        const float SourceTimeScale = 2f;
        static readonly string[] VersionWaiting = { "버전 확인 중.", "버전 확인 중..", "버전 확인 중..." };
        static bool journeyConsumed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void NewLaunch() { journeyConsumed = false; }
#if UNITY_EDITOR
        public static void ResetEditorCaptureJourney() { journeyConsumed = false; }
#endif
        public StartupIntroTimeline Timeline { get; private set; }
        public VideoPlayer Player { get; private set; }
        public bool FirstFrameReady { get; private set; }
        public bool MediaEnded { get; private set; }
        public bool MediaFailed { get; private set; }
        public int PlaybackStarts { get; private set; }
        public long DecodedFrame { get; private set; } = -1;
        public double PreparationSeconds { get; private set; }
        public float WhiteOpacity => white == null ? 0 : white.color.a;
        CanvasGroup group;
        RawImage screen;
        Image white;
        AudioSource audioOwner;
        RenderTexture target;
        TMP_Text status;
        RawImage title;
        RectTransform titleContainer;
        float presentationSeconds;
        Func<bool> accepted;
        Func<ContentStage> stage;
        Action reveal;
        double startedAt, lastProgressAt;
        bool paused;
        float fallbackTime;

        public static StartupIntroOverlay Create(Transform parent, TMP_FontAsset font,
            Func<bool> versionAccepted, Func<ContentStage> currentStage, Action onReveal)
        {
            var root = new GameObject("OneShotFlowOpening", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; GameLandscapeDisplay.Configure(scaler);
            var overlay = root.AddComponent<StartupIntroOverlay>();
            overlay.Timeline = new StartupIntroTimeline(journeyConsumed);
            overlay.MediaEnded = journeyConsumed;
            // Claim even during preparation: bootstrap recreation cannot replay media.
            journeyConsumed = true;
            overlay.accepted = versionAccepted; overlay.stage = currentStage; overlay.reveal = onReveal;
            overlay.group = root.GetComponent<CanvasGroup>();
            var backing = FullRect("OpeningInputCover", root.transform).gameObject.AddComponent<Image>();
            backing.color = new Color(.08f,.07f,.05f);
            overlay.screen = FullRect("FlowVideo", root.transform).gameObject.AddComponent<RawImage>();
            overlay.screen.raycastTarget = false;
            overlay.screen.texture = UnityEngine.Resources.Load<Texture2D>("StartupVideo/landscape-first-frame-v01");
            var fit = overlay.screen.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = overlay.screen.texture!=null?(float)overlay.screen.texture.width/overlay.screen.texture.height:16f/9;
            var titleRect = FullRect("GameTitle", root.transform);
            titleRect.anchorMin = new Vector2(0,1); titleRect.anchorMax = Vector2.one; titleRect.pivot = new Vector2(.5f,1);
            titleRect.anchoredPosition = new Vector2(0,-80); titleRect.sizeDelta = new Vector2(-64,110);
            titleRect.sizeDelta = new Vector2(-64, 220);
            overlay.titleContainer = titleRect;
            overlay.title = LastRail.Auth.BrandLogoView.Create(titleRect);
            overlay.title.color = new Color(1, 1, 1, 0);
            overlay.white = FullRect("SunlightWhiteTransition", root.transform).gameObject.AddComponent<Image>();
            overlay.white.color = new Color(1,1,1,overlay.MediaEnded ? 1 : 0); overlay.white.raycastTarget = false;
            var rect = FullRect("VersionStatus", root.transform);
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1,0); rect.pivot = new Vector2(.5f,0); rect.anchoredPosition = new Vector2(0,52); rect.sizeDelta = new Vector2(-72,42);
            overlay.status = rect.gameObject.AddComponent<TextMeshProUGUI>(); overlay.status.font = font; overlay.status.fontSize = 19; overlay.status.alignment = TextAlignmentOptions.Center; overlay.status.raycastTarget = false;
            if (!overlay.MediaEnded && Application.isPlaying) overlay.PrepareVideo();
            return overlay;
        }
        static RectTransform FullRect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        void PrepareVideo()
        {
            startedAt = lastProgressAt = Time.realtimeSinceStartupAsDouble;
            // Allocate from the decoded source dimensions in Prepared; never stretch a portrait clip into landscape.
            audioOwner = gameObject.AddComponent<AudioSource>(); audioOwner.playOnAwake = false; audioOwner.loop = false; audioOwner.spatialBlend = 0;
            Player = gameObject.AddComponent<VideoPlayer>(); Player.playOnAwake = false; Player.isLooping = false;
            Player.source = VideoSource.Url; Player.url = Path.Combine(Application.streamingAssetsPath, VideoRelativePath).Replace('\\','/');
            Player.renderMode = VideoRenderMode.RenderTexture; Player.targetTexture = target;
            Player.waitForFirstFrame = true; Player.skipOnDrop = false;
            // GameTime enables synchronous capture. Production ignores gameplay timeScale.
            Player.timeUpdateMode = Time.captureFramerate > 0 ? VideoTimeUpdateMode.GameTime : VideoTimeUpdateMode.UnscaledGameTime;
            Player.audioOutputMode = VideoAudioOutputMode.AudioSource; Player.controlledAudioTrackCount = 1;
            Player.EnableAudioTrack(0,true); Player.SetTargetAudioSource(0,audioOwner);
            Player.sendFrameReadyEvents = true; Player.prepareCompleted += Prepared; Player.frameReady += FrameReady;
            Player.loopPointReached += Ended; Player.errorReceived += VideoError;
            Player.Prepare();
        }
        void Prepared(VideoPlayer player)
        {
            PreparationSeconds = Time.realtimeSinceStartupAsDouble - startedAt;
            if (!isActiveAndEnabled || MediaEnded || MediaFailed || PlaybackStarts != 0) return;
            int width=checked((int)player.width),height=checked((int)player.height);
            if(width<=0||height<=0){FailMedia();return;}
            target=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32){name="OpeningVideoSource",useMipMap=false,autoGenerateMips=false};target.Create();player.targetTexture=target;
            screen.GetComponent<AspectRatioFitter>().aspectRatio=(float)width/height;
            PlaybackStarts++; if (!paused) player.Play();
        }
        void FrameReady(VideoPlayer player, long frame)
        {
            if (!FirstFrameReady) { FirstFrameReady = true; screen.texture = target; }
            if (frame != DecodedFrame) lastProgressAt = Time.realtimeSinceStartupAsDouble;
            DecodedFrame = frame;
        }
        void Ended(VideoPlayer player) { MediaEnded = true; }
        void VideoError(VideoPlayer player, string error) { FailMedia(); }
        void FailMedia()
        {
            if (MediaFailed || MediaEnded) return;
            MediaFailed = true; fallbackTime = Mathf.Max(Timeline.Travel, StartupIntroTimeline.BloomStartSeconds);
            if (Player != null) Player.Pause();
            Debug.LogWarning("Opening video unavailable; continuing through the version gate with a static fade.");
        }
        void Update()
        {
            if (Timeline == null || paused) return;
            var delta = Time.captureFramerate > 0 ? Time.captureDeltaTime : Time.unscaledDeltaTime;
            presentationSeconds += delta;
            if (!MediaEnded && !MediaFailed && Time.realtimeSinceStartupAsDouble - lastProgressAt > 15) FailMedia();
            var mediaTime = Player != null && FirstFrameReady ? (float)Player.time * SourceTimeScale : Timeline.Travel;
            if (MediaFailed)
            {
                fallbackTime = Mathf.Min(StartupIntroTimeline.TravelSeconds, fallbackTime + delta);
                mediaTime = fallbackTime; MediaEnded = fallbackTime >= StartupIntroTimeline.TravelSeconds;
            }
            var current = stage();
            var failed = current == ContentStage.Failed || current == ContentStage.Maintenance || current == ContentStage.UpdateRequired || current == ContentStage.Cancelled;
            var ok = accepted() && !failed;
            var open = Timeline.ObservePlayback(mediaTime, MediaEnded, delta, ok);
            var showError = failed && MediaEnded && !Timeline.Revealed;
            group.alpha = showError ? 0 : 1;
            group.blocksRaycasts = !showError && !Timeline.Finished;
            var opacity = Timeline.Revealed ? 1-Mathf.SmoothStep(0,1,(Timeline.Transition-StartupIntroTimeline.BloomSeconds)/StartupIntroTimeline.RevealSeconds)
                : Mathf.SmoothStep(0,1,Timeline.Transition/StartupIntroTimeline.BloomSeconds);
            white.color = new Color(1,1,1,opacity);
            if (audioOwner != null) audioOwner.volume = .7f * (1-Mathf.Clamp01(Timeline.Transition/StartupIntroTimeline.BloomSeconds));
            status.text = ok ? "버전 확인 완료" : failed ? "버전 확인 필요" : VersionWaiting[(int)(presentationSeconds * 2) % 3];
            status.color = Color.Lerp(new Color(.94f,.88f,.74f), new Color(.24f,.20f,.15f), opacity);
            status.alpha = Timeline.Revealed ? 0 : MediaEnded && !ok ? 1 : 1-opacity;
            var titleEntrance = Mathf.SmoothStep(0,1,(presentationSeconds-.12f)/.75f);
            var safeTop = Screen.height > 0 ? (Screen.height-Screen.safeArea.yMax)*1280f/Screen.height : 0;
            titleContainer.anchoredPosition = new Vector2(0,-96-safeTop+16*(1-titleEntrance));
            titleContainer.localScale = Vector3.one*Mathf.Lerp(.97f,1,titleEntrance);
            title.color = new Color(1, 1, 1, Timeline.Revealed ? 0 : titleEntrance*(1-opacity));
            if (open)
            {
                // Full white conceals the actual login UI being activated underneath.
                screen.enabled = false;
                transform.Find("OpeningInputCover").GetComponent<Image>().color = Color.clear;
                ReleaseVideo();
                reveal();
            }
            if (Timeline.Finished) gameObject.SetActive(false);
        }
        void OnApplicationPause(bool value)
        {
            paused = value; lastProgressAt = Time.realtimeSinceStartupAsDouble;
            if (Player == null || MediaEnded || MediaFailed) return;
            if (value) Player.Pause();
            else if (PlaybackStarts > 0) Player.Play();
        }
        void OnDisable()
        {
            // A resumed overlay must not wait on a decoder we intentionally released.
            MediaEnded = true;
            if (Timeline != null && !Timeline.Revealed)
            {
                Timeline.ObservePlayback(StartupIntroTimeline.TravelSeconds, true, 0, false);
                if (white != null) white.color = Color.white;
            }
            ReleaseVideo();
        }
        void OnDestroy()
        {
            ReleaseVideo();
        }
        void ReleaseVideo()
        {
            if (Player != null)
            {
                Player.prepareCompleted -= Prepared; Player.frameReady -= FrameReady;
                Player.loopPointReached -= Ended; Player.errorReceived -= VideoError;
                Player.Stop(); Player.targetTexture = null;
                if (Application.isPlaying) Destroy(Player); else DestroyImmediate(Player);
                Player = null;
            }
            if (audioOwner != null) { audioOwner.Stop(); audioOwner.volume = 0; }
            if (target != null)
            {
                if (screen != null && screen.texture == target) screen.texture = null;
                target.Release(); if (Application.isPlaying) Destroy(target); else DestroyImmediate(target); target = null;
            }
        }
    }
}
