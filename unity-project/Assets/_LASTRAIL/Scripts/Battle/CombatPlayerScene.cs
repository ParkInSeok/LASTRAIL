using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastRail.Battle
{
    // Presentation lifetime only. The caller retains admission, checkpoint and result recovery.
    public sealed class CombatSceneResult
    {
        public BasicCombatOutcome Outcome { get; }
        public long Tick { get; }
        internal CombatSceneResult(BasicCombatOutcome outcome,long tick){Outcome=outcome;Tick=tick;}
    }
    public sealed class CombatPlayerScene : MonoBehaviour
    {
        enum Phase { Closed, AwaitingRender, Running }
        Phase phase;
        CombatSceneDriver driver;
        CombatPlayerAssetBinding.Lease assets;
        GameObject sceneRoot;
        ApprovedCombatView view;
        CanvasGroup input;
        Func<bool> ownerCurrent;
        Func<bool> foreground;
        Action<CombatSceneResult> completed;
        Action<Exception> failed;
        bool discardFrame,suspended,applicationPaused;
        bool Foreground=>!applicationPaused&&(foreground?.Invoke()??true);
        long renderedRevision;
        Exception renderFailure;
        long generation;

        public Camera SceneCamera { get; private set; }
        public ApprovedCombatView View=>view;
        public bool IsRunning=>phase==Phase.Running&&driver?.IsExecutionReady==true;
        public bool IsOpen=>phase!=Phase.Closed;
        public event Action Closed;

        // Transfers this lease only after rejecting duplicate Bind. Never opens/resets the driver.
        public void Bind(CombatSceneDriver current,CombatPlayerAssetBinding.Lease lease,EventSystem eventSystem,
            Func<bool> currentOwner,Action<CombatSceneResult> onCompleted,Action<Exception> onFailed,Func<bool> foregroundReady=null)
        {
            if(phase!=Phase.Closed||sceneRoot!=null)throw new InvalidOperationException("Close the previous presentation before binding.");
            generation++;assets=lease??throw new ArgumentNullException(nameof(lease));
            try
            {
                if(!isActiveAndEnabled||eventSystem==null||!eventSystem.isActiveAndEnabled)throw new InvalidOperationException("An active host and existing input system are required.");
                driver=current??throw new ArgumentNullException(nameof(current));ownerCurrent=currentOwner??throw new ArgumentNullException(nameof(currentOwner));
                if(!ownerCurrent())throw new InvalidOperationException("Combat owner changed.");
                renderedRevision=ReadRevision();
                completed=onCompleted;failed=onFailed;foreground=foregroundReady;suspended=!Foreground;
                sceneRoot=new GameObject("OwnedCombatPresentation");sceneRoot.transform.SetParent(transform,false);sceneRoot.SetActive(false);
                var cameraObject=new GameObject("CombatCamera",typeof(Camera));cameraObject.transform.SetParent(sceneRoot.transform,false);
                SceneCamera=cameraObject.GetComponent<Camera>();SceneCamera.clearFlags=CameraClearFlags.SolidColor;SceneCamera.backgroundColor=Color.black;
                SceneCamera.orthographic=true;SceneCamera.nearClipPlane=.1f;SceneCamera.farClipPlane=10;
                var surface=new GameObject("CombatCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));surface.transform.SetParent(sceneRoot.transform,false);
                input=surface.GetComponent<CanvasGroup>();input.interactable=false;input.blocksRaycasts=false;
                LastRail.Presentation.GameLandscapeDisplay.ApplyOrientation();
                view=surface.AddComponent<ApprovedCombatView>();long boundGeneration=generation;view.SetCommandAvailability(()=>generation==boundGeneration&&phase==Phase.Running&&!suspended&&Foreground&&ownerCurrent()!=false);view.Bind(driver,assets.ReadAssets());view.UseLandscapeLayout();
                var canvas=surface.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=SceneCamera;canvas.planeDistance=1;
                if(!ownerCurrent()||ReadRevision()!=renderedRevision)throw new InvalidOperationException("Combat changed during presentation binding.");
                phase=Phase.AwaitingRender;Camera.onPreCull+=BeforeRender;Camera.onPostRender+=Rendered;
                driver.StateChanged+=OwnerStateChanged;
                sceneRoot.SetActive(true);Canvas.ForceUpdateCanvases();
            }
            catch{Close();throw;}
        }
        void BeforeRender(Camera camera)
        {
            if(phase!=Phase.AwaitingRender||camera!=SceneCamera||renderFailure!=null)return;
            try{
                if(!ownerCurrent())throw new InvalidOperationException("Combat owner changed before render.");
                if(ReadRevision()!=renderedRevision)throw new InvalidOperationException("Unpresented combat state changed.");
            }catch(Exception error){
                // Suppress drawing before culling. Defer camera/lease teardown to the normal frame owner.
                renderFailure=error;input.gameObject.SetActive(false);camera.cullingMask=0;camera.clearFlags=CameraClearFlags.Nothing;
            }
        }
        void Rendered(Camera camera)
        {
            if(phase!=Phase.AwaitingRender||camera!=SceneCamera||suspended||!Foreground||renderFailure!=null)return;
            try
            {
                if(!ownerCurrent()) {renderFailure=new InvalidOperationException("Combat owner changed during render.");return;}
                if(ReadRevision()!=renderedRevision)throw new InvalidOperationException("Unpresented combat state changed.");
                phase=Phase.Running;discardFrame=true;input.interactable=true;input.blocksRaycasts=true;
            }
            catch(Exception error){renderFailure=error;}
        }
        long ReadRevision(){if(driver==null||!driver.TryGetCommittedRevision(out var value))throw new InvalidOperationException("Combat committed source unavailable.");return value;}
        // Fixed-step simulation consumes elapsed time; discrete presentation now follows commit callbacks.
        void Update()=>AdvanceFrame(Time.unscaledDeltaTime);
        void AdvanceFrame(double seconds)
        {
            if(phase==Phase.Closed)return;
            long frameGeneration=generation;
            try
            {
                if(renderFailure!=null){Fail(renderFailure);return;}
                if(!ownerCurrent()){Close();return;}
                if(!Foreground){suspended=true;input.interactable=false;input.blocksRaycasts=false;return;}
                if(suspended){suspended=false;renderedRevision=ReadRevision();view.Refresh();phase=Phase.AwaitingRender;return;}
                if(!IsRunning||suspended)return;
                // A state already terminal at Bind must not destroy its camera inside onPostRender.
                if(discardFrame){discardFrame=false;OwnerStateChanged();return;}
                if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
                // The simulation owns fixed-step timing and terminal damage rules.
                driver.Advance(checked((long)(seconds*TimeSpan.TicksPerSecond)),10);
            }
            catch(Exception error){if(frameGeneration==generation)Fail(error);else Debug.LogException(error);}
        }
        void OwnerStateChanged()
        {
            if(phase==Phase.Closed||driver==null)return;
            if(driver.IsRetired){Close();return;}
            if(driver.RecoveryRequired){Fail(new InvalidOperationException("Combat requires recovery after a failed command."));return;}
            if(phase!=Phase.Running||driver.Outcome==BasicCombatOutcome.Running)return;
            var result=new CombatSceneResult(driver.Outcome,driver.Tick);var notify=completed;
            // Invalidate before notifying so reentrant/late commits cannot execute on the old scene.
            var release=Detach();try{notify?.Invoke(result);}finally{release();}
        }
        void OnApplicationPause(bool paused)
        {
            applicationPaused=paused;if(phase==Phase.Closed)return;suspended=paused||!Foreground;
            if(input!=null){input.interactable=false;input.blocksRaycasts=false;}
            if(!paused&&Foreground)
            {
                try{renderedRevision=ReadRevision();view.Refresh();phase=Phase.AwaitingRender;}
                catch(Exception error){Fail(error);}
            }
        }
        void Fail(Exception error){var notify=failed;var release=Detach();try{if(notify!=null)notify(error);else Debug.LogException(error);}finally{release();}}
        public void Close(){var notify=Closed;var release=Detach();try{notify?.Invoke();}finally{release();}}
        LastRail.Resources.PreparedContent.Scope contentLifetime;
        public void OwnPreparedContent(LastRail.Resources.PreparedContent.Scope scope){contentLifetime?.Dispose();contentLifetime=scope;}
        Action Detach()
        {
            if(driver!=null)driver.StateChanged-=OwnerStateChanged;
            generation++;phase=Phase.Closed;Camera.onPreCull-=BeforeRender;Camera.onPostRender-=Rendered;

            var ownedContent=contentLifetime;contentLifetime=null;
            var ownedRoot=sceneRoot;var ownedLease=assets;var ownedView=view;sceneRoot=null;assets=null;
            if(ownedRoot!=null){ownedRoot.SetActive(false);ownedRoot.transform.SetParent(null,false);}
            SceneCamera=null;view=null;input=null;driver=null;ownerCurrent=null;foreground=null;completed=null;failed=null;Closed=null;
            suspended=false;discardFrame=false;renderFailure=null;
            return ()=>{try{if(ownedView!=null)ownedView.ReleaseOwnedPresentationResources();}finally{try{if(ownedRoot!=null){if(Application.isPlaying)Destroy(ownedRoot);else DestroyImmediate(ownedRoot);}}finally{try{ownedLease?.Dispose();}finally{ownedContent?.Dispose();}}}};
        }
        void OnDisable()=>Close();
        void OnDestroy()=>Close();
    }
}


