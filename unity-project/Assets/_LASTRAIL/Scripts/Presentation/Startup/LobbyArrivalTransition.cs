using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Startup
{
    // Presentation begins only inside the accepted, bound Main entry operation.
    public sealed class LobbyArrivalTransition : MonoBehaviour
    {
        public const float LightingSeconds=2f, RevealSeconds=.5f;
        CanvasGroup group;
        Image bloom;
        Material bloomMaterial;
        SharedRailMap lobbyMap;
        LastRail.Presentation.MissionShell.LobbyCommandDeck commandDeck;
        CanvasGroup lobbyUi;
        StartupMotionBackdrop motion;
        TaskCompletionSource<bool> pending;
        CancellationToken cancellation;
        float elapsed, duration;
        bool fading;
        public bool Illuminating => pending!=null && !fading;
        public static LobbyArrivalTransition Create(Transform parent, StartupMotionBackdrop previous)
        {
            var root=new GameObject("LobbyArrival",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup),typeof(GraphicRaycaster));
            root.transform.SetParent(parent,false);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=90;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; GameLandscapeDisplay.Configure(scaler);
            var view=root.AddComponent<LobbyArrivalTransition>(); view.group=root.GetComponent<CanvasGroup>();
            view.group.alpha=0; // Do not expose the newly created canvas before its pose is synchronized.
            view.motion=StartupMotionBackdrop.Attach(root.transform,true);
            Canvas.ForceUpdateCanvases();
            view.motion.CopyPhaseFrom(previous);
            var block=new GameObject("InputShield",typeof(RectTransform),typeof(Image)); block.transform.SetParent(root.transform,false);
            var rect=(RectTransform)block.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.sizeDelta=Vector2.zero;
            block.GetComponent<Image>().color=Color.clear;
            var glow=new GameObject("JunctionLightFill",typeof(RectTransform),typeof(Image));
            glow.transform.SetParent(root.transform,false);
            var gr=(RectTransform)glow.transform;gr.anchorMin=Vector2.zero;gr.anchorMax=Vector2.one;gr.sizeDelta=Vector2.zero;
            view.bloom=glow.GetComponent<Image>();view.bloom.raycastTarget=false;
            view.bloomMaterial=new Material(LastRail.Resources.PreparedContent.Load<Shader>("StartupMotion/JunctionLightFill"));
            view.bloom.material=view.bloomMaterial;
            return view;
        }
        public void PrepareLobby(GameObject screen)
        {
            // Only the approved bound screen is passed here by the entry owner.
            foreach(var image in screen.GetComponentsInChildren<Image>(true))
                if(image.name=="MissionSelectBackgroundImage" || image.name=="MissionSelectBackgroundShade") image.enabled=false;
            commandDeck=screen.GetComponentInChildren<LastRail.Presentation.MissionShell.LobbyCommandDeck>(true);
            if(commandDeck==null) {lobbyMap=SharedRailMap.Create(screen.transform); lobbyMap.transform.SetAsFirstSibling();lobbyMap.ConfigureLobby();}
            else commandDeck.SetReducedMotion(motion.ReducedMotion);
            var safe=screen.transform.Find("SafeArea");
            if(safe!=null) {lobbyUi=safe.GetComponent<CanvasGroup>();if(lobbyUi==null) lobbyUi=safe.gameObject.AddComponent<CanvasGroup>();lobbyUi.alpha=0;lobbyUi.blocksRaycasts=false;}
        }
        public Task IlluminateAsync(CancellationToken token)
        {
            motion.Show(true,true);
            Canvas.ForceUpdateCanvases(); motion.Evaluate(0);
            return Begin(token,false,LightingSeconds);
        }
        public Task RevealAsync(CancellationToken token)
        {
            // Keep the full junction light over the scene swap, then reveal the bound lobby.
            Canvas.ForceUpdateCanvases(); lobbyMap?.EvaluateLayout(); Canvas.ForceUpdateCanvases();
            if(!motion.ReducedMotion) motion.gameObject.SetActive(false); // Full light is opaque now; do not dissolve the old V-shaped route back in.
            return Begin(token,true,RevealSeconds);
        }
        Task Begin(CancellationToken token,bool fade,float seconds)
        {
            if(pending!=null) throw new InvalidOperationException("Arrival is already running.");
            fading=fade; elapsed=0; duration=seconds; cancellation=token;
            group.alpha=1; group.blocksRaycasts=true;
            pending=new TaskCompletionSource<bool>(); var task=pending.Task;
            if(token.IsCancellationRequested) Abort();
            return task;
        }
        void Update() => Evaluate(Time.captureFramerate>0 ? Time.captureDeltaTime : Time.unscaledDeltaTime);
        public void Evaluate(float delta)
        {
            if(pending==null) return;
            if(cancellation.IsCancellationRequested) { Abort(); return; }
            elapsed+=Mathf.Max(0,delta);
            if(fading) {
                var reveal=Mathf.SmoothStep(0,1,elapsed/duration);
                if(motion.ReducedMotion || commandDeck!=null) group.alpha=1-reveal;
                lobbyMap?.SetTint(reveal);
                if(lobbyUi!=null) {lobbyUi.alpha=reveal;lobbyUi.blocksRaycasts=reveal>=1;}
            }
            else {
                var approach=Mathf.Clamp01((elapsed-.8f)/1.2f);
                // A single ease-in in log-scale: zoom velocity increases through the final frame.
                float accelerated=approach*approach;
                motion.SetMapFocus(accelerated);
                motion.SetMapDive(accelerated);
                bloomMaterial.SetFloat("_Progress",motion.ReducedMotion ? 0 : Mathf.Clamp01((accelerated-.35f)/.65f));
                var area=((RectTransform)transform).rect;
                bloomMaterial.SetFloat("_Aspect",area.width/Mathf.Max(1,area.height));
            }
            if(elapsed+0.0001f<duration) return;
            var completed=pending; pending=null;
            if(fading) {group.alpha=0; group.blocksRaycasts=false;}
            completed.TrySetResult(true);
        }
        public void Abort()
        {
            var cancelled=pending; pending=null;
            if(group!=null) {group.alpha=0; group.blocksRaycasts=false;}
            if(lobbyUi!=null) {lobbyUi.alpha=1;lobbyUi.blocksRaycasts=true;}
            cancelled?.TrySetCanceled();
        }
        void OnDisable() => Abort();
        void OnDestroy() {
            Abort();
            if(bloomMaterial!=null) {if(Application.isPlaying) Destroy(bloomMaterial); else DestroyImmediate(bloomMaterial);}
        }
    }
}
