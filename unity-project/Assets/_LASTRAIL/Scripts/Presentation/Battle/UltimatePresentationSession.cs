using System;
using LastRail.Battle;
using LastRail.Presentation.Settings;

namespace LastRail.Presentation.Battle
{
    /// <summary>
    /// A single, already accepted action's presentation lifetime. This does not accept commands,
    /// resolve effects, or own gameplay time. The caller owns accepted ordering and cross-session
    /// duplicate reservations. Dispose at account/scene replacement, unbind, or pool return.
    /// All ports and callbacks must run on the owning presentation thread.
    /// </summary>
    public sealed class UltimatePresentationSession : IDisposable
    {
        public const string Standard = "Standard", Summary = "Summary", Minimal = "Minimal";
        readonly CombatPresentationMode acceptedMode;
        readonly Func<bool> sourceCurrent, standardReady, summaryReady, dailyEligible;
        readonly Action firstVisible;
        readonly Func<IDisposable> acquireOwnedHoldAtSafeTick;
        readonly Func<string, Action, Action, Action, Action<Exception>, IDisposable> play;
        readonly Action<bool> restore;
        readonly Action<Exception> report;
        IDisposable ownedHold, playback;
        bool started, visible, starting, cleaned, completionSent;
        LocalSettingsModel settings;
        Func<string,bool,bool> profileReady;
        UltimatePresentationIntensity intensity;
        public bool ReducedPresentation { get; private set; }
        public void BindPresentationSettings(LocalSettingsModel owner,Func<string,bool,bool> validatedProfileReady,Func<bool> reduceOnCurrentTimeline)
        {
            if(State!="Waiting")throw new InvalidOperationException("Bind settings before selecting this presentation.");
            UnbindSettings();
            settings=owner??throw new ArgumentNullException(nameof(owner));
            profileReady=validatedProfileReady??throw new ArgumentNullException(nameof(validatedProfileReady));
            intensity=new UltimatePresentationIntensity(reduceOnCurrentTimeline,Cancel);
            settings.Changed+=OnSettingsChanged;
        }
        void OnSettingsChanged()
        {
            if(State=="Playing"&&settings.Value(LocalSetting.ReducedPresentation)==1&&!ReducedPresentation)
            {
                ReducedPresentation=true;
                try{intensity.Select(true);}catch(Exception error){Record("reduced profile switch",error);Finish();}
            }
        }
        bool ProfileReady(string kind)=>profileReady==null||Read(()=>profileReady(kind,
            State=="Waiting"?settings.Value(LocalSetting.ReducedPresentation)==1:ReducedPresentation),"validated presentation intensity profile");
        void UnbindSettings()
        {
            if(settings!=null)settings.Changed-=OnSettingsChanged;
            settings=null;intensity?.Dispose();intensity=null;
            profileReady=null;
        }

        // The owner unsubscribes its own handler; notification follows ALL playback cleanup,
        // including a lease returned after synchronous completion from Begin.
        public event Action Completed;
        public bool HasCurrentSource => State != "Disposed" && IsCurrent() && State != "Disposed";
        public bool CanPresentStandard => State == "Waiting" && IsCurrent() &&
            Read(standardReady, "standard readiness preview") && ProfileReady(Standard) &&
            (acceptedMode == CombatPresentationMode.Full ||
             acceptedMode == CombatPresentationMode.FirstUseFull && Read(dailyEligible, "daily eligibility preview"));

        public string ActionId { get; }
        public long ActionGeneration { get; }
        public string Kind { get; private set; }
        public string State { get; private set; } = "Waiting";
        public Exception LastFailure { get; private set; }

        /// <param name="sourceCurrent">The original owner checks acceptance, cancellation,
        /// action generation, account and scene lifetime. Close over its immutable accepted
        /// skill/actor/group and original event/result references; do not infer IDs from art.</param>
        /// <param name="standardReady">Must include usable manifest, end/error/cancel paths,
        /// first-frame notification, and a valid camera/HUD/input/audio return context.</param>
        /// <param name="dailyEligible">Rechecks authoritative current day/account/group viewing
        /// state at this action's presentation turn. Unknown state must return false.</param>
        /// <param name="firstVisible">Existing daily owner records actual standard visibility,
        /// including ALWAYS, using its captured start-day/group. This class stores no daily data.</param>
        /// <param name="acquireOwnedHoldAtSafeTick">Original time owner supplies only this
        /// presentation's lease, at an approved safe tick. Null means it cannot be acquired now;
        /// do not wait for assets or release other holds. No global pause/timeScale mutation.</param>
        /// <param name="play">View binds only these callbacks and returns its playback lease.
        /// Its lease cancels owned loads/subscriptions. It must not execute gameplay effects.
        /// Minimal uses original field-result decoration; it does not invent a new effect.</param>
        /// <param name="restore">Releases owned input/camera/HUD/audio context once. False means
        /// the original scene/action is obsolete: do not reopen its battlefield or resume time.</param>
        public UltimatePresentationSession(string actionId, long actionGeneration,
            CombatPresentationMode acceptedMode, Func<bool> sourceCurrent,
            Func<bool> standardReady, Func<bool> summaryReady, Func<bool> dailyEligible,
            Action firstVisible, Func<IDisposable> acquireOwnedHoldAtSafeTick,
            Func<string, Action, Action, Action, Action<Exception>, IDisposable> play,
            Action<bool> restore, Action<Exception> report)
        {
            if (string.IsNullOrWhiteSpace(actionId)) throw new ArgumentException("Accepted action identity is required.", nameof(actionId));
            if (actionGeneration < 0) throw new ArgumentOutOfRangeException(nameof(actionGeneration));
            if (!Enum.IsDefined(typeof(CombatPresentationMode), acceptedMode)) throw new ArgumentOutOfRangeException(nameof(acceptedMode));
            ActionId = actionId; ActionGeneration = actionGeneration; this.acceptedMode = acceptedMode;
            this.sourceCurrent = sourceCurrent ?? throw new ArgumentNullException(nameof(sourceCurrent));
            this.standardReady = standardReady ?? throw new ArgumentNullException(nameof(standardReady));
            this.summaryReady = summaryReady ?? throw new ArgumentNullException(nameof(summaryReady));
            this.dailyEligible = dailyEligible ?? throw new ArgumentNullException(nameof(dailyEligible));
            this.firstVisible = firstVisible ?? throw new ArgumentNullException(nameof(firstVisible));
            this.acquireOwnedHoldAtSafeTick = acquireOwnedHoldAtSafeTick ?? throw new ArgumentNullException(nameof(acquireOwnedHoldAtSafeTick));
            this.play = play ?? throw new ArgumentNullException(nameof(play));
            this.restore = restore ?? throw new ArgumentNullException(nameof(restore));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public bool Start()
        {
            if (State != "Waiting") return false;
            starting = true;
            try { return StartCore(); }
            finally { starting = false; Cleanup(); }
        }
        bool StartCore()
        {
            State = "Checking";
            ReducedPresentation=settings!=null&&settings.Value(LocalSetting.ReducedPresentation)==1;
            if (!IsCurrent()) { Finish(); return false; }
            // One readiness decision for this accepted action; a late load cannot upgrade it.
            bool fullReady = Read(standardReady, "standard readiness")&&ProfileReady(Standard);
            bool wantsFull = acceptedMode == CombatPresentationMode.Full ||
                acceptedMode == CombatPresentationMode.FirstUseFull && Read(dailyEligible, "daily eligibility");
            Kind = fullReady && wantsFull ? Standard : Fallback();
            if (State == "Disposed" || !IsCurrent()) { Finish(); return false; }
            if (Kind == Standard)
            {
                IDisposable acquired = null;
                try { acquired = acquireOwnedHoldAtSafeTick(); }
                catch (Exception error) { Record("safe-tick hold acquisition", error); }
                // Acquisition may synchronously cancel this session.
                ownedHold = acquired;
                if (State == "Disposed") return false;
                if (ownedHold == null) Kind = Fallback();
                if (!IsCurrent()) { Finish(); return false; }
            }
            if (State == "Disposed") return false;
            State = "Playing";
            started = true;
            try
            {
                var acquiredPlayback = play(Kind, OnFirstVisible, Finish, Skip, Fail);
                // Begin may synchronously finish, skip, fail or unbind before returning its lease.
                playback = acquiredPlayback;
                if (State != "Disposed" && acquiredPlayback == null)
                    Fail(new InvalidOperationException("Playback did not supply an owned lifetime."));
            }
            catch (Exception error) { Fail(error); }
            return true;
        }

        string Fallback() => Read(summaryReady, "summary readiness")&&ProfileReady(Summary) ? Summary : Minimal;
        bool Read(Func<bool> read, string boundary)
        {
            try { return read(); }
            catch (Exception error) { Record(boundary, error); return false; }
        }
        bool IsCurrent() => Read(sourceCurrent, "original action/scene validity");
        void OnFirstVisible()
        {
            if (State != "Playing" || Kind != Standard || visible) return;
            if (!IsCurrent()) { Finish(); return; }
            if (State != "Playing") return; // The validity port may synchronously dispose this session.
            visible = true; // Set before invoking an external port: it may synchronously reenter.
            try { firstVisible(); }
            catch (Exception error) { Record("standard first-visible daily record", error); }
        }
        void Fail(Exception error)
        {
            // Preserve original error context even when a player delivers it after teardown.
            Record("playback failure", error ?? new InvalidOperationException("Player reported an unspecified failure."));
            Finish();
        }
        public void Skip() => Finish();
        public void Cancel() => Finish();
        // The actual player's timeout policy invokes this; no arbitrary watchdog duration here.
        public void Timeout()
        {
            if (State == "Disposed") return;
            Record("playback timeout", new TimeoutException("Original presentation player timed out."));
            Finish();
        }
        public void Dispose() => Finish();
        void Finish()
        {
            if (State == "Disposed") return;
            State = "Disposed"; // Invalidate callbacks before any disposal can reenter.
            Cleanup();
        }
        void Cleanup()
        {
            // Start may still be acquiring a hold or returning the player's lifetime.
            // Fence callbacks immediately, but never restore/release before those leases arrive.
            if (State != "Disposed" || starting || cleaned) return;
            cleaned = true;
            UnbindSettings();
            var oldPlayback = playback; playback = null;
            var oldHold = ownedHold; ownedHold = null;
            Release(oldPlayback, "playback release");
            if (started)
            {
                bool valid = IsCurrent();
                try { restore(valid); }
                catch (Exception error) { Record("presentation context restoration", error); }
            }
            Release(oldHold, "owned safe-tick hold release");
            NotifyCompleted();
            // Actual field results remain owned by the original consumer after error/skip/end.
        }
        void NotifyCompleted()
        {
            if (!cleaned || completionSent) return;
            completionSent = true;
            var subscribers = Completed;
            if (subscribers == null) return;
            foreach (Action subscriber in subscribers.GetInvocationList())
                try { subscriber(); } catch (Exception error) { Record("completion subscriber", error); }
        }
        void Release(IDisposable resource, string boundary)
        {
            if (resource == null) return;
            try { resource.Dispose(); }
            catch (Exception error) { Record(boundary, error); }
        }
        void Record(string boundary, Exception original)
        {
            var context = new InvalidOperationException(
                "Ultimate presentation " + ActionId + " generation " + ActionGeneration + ": " + boundary, original);
            LastFailure = context;
            try { report(context); }
            catch (Exception sinkFailure)
            {
                // A diagnostic sink must not prevent release/restoration; preserve both causes.
                LastFailure = new AggregateException(context, sinkFailure);
            }
        }
    }
}
