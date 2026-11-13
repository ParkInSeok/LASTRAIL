#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastRail.Presentation.Audio
{
    public sealed class LastRailAudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource themeSource;
        [SerializeField] private AudioSource[] combatVoices = Array.Empty<AudioSource>();
        // Negative means the authored/listened mix has not been supplied; keep battle music.
        [SerializeField] private float themeBattleGain = -1;
        [SerializeField] private LastRailAudioClipBinding[] clipBindings = Array.Empty<LastRailAudioClipBinding>();

        private readonly LaunchAudioCueCatalog catalog = LaunchAudioCueCatalog.CreateDefault();
        private readonly HashSet<string> frameCues = new HashSet<string>(StringComparer.Ordinal);
        private int cueFrame = -1;
        float musicGain=1, effectsGain=1, authoredBgmGain=1, authoredSfxGain=1;
        bool capturedGains;
        bool themeSuppressesBase;
        float authoredThemeGain=1,themeClipGain=1;
        BattleMusicPlayback music;
        CombatSfxPlayback combat;
        object combatScope;
        long combatGeneration=-1,basePlaybackGeneration,themePlaybackGeneration;
        readonly Dictionary<AudioSource,Action> voiceEnds=new Dictionary<AudioSource,Action>();
        readonly Dictionary<AudioSource,float> voiceGains=new Dictionary<AudioSource,float>();
        readonly Dictionary<AudioSource,float> voicePans=new Dictionary<AudioSource,float>();
        readonly Dictionary<AudioSource,float> voiceFactors=new Dictionary<AudioSource,float>();
        readonly Dictionary<AudioSource,CombatAudioPositionPlayback> positions=new Dictionary<AudioSource,CombatAudioPositionPlayback>();
        readonly Dictionary<AudioSource,Func<bool>> disclosures=new Dictionary<AudioSource,Func<bool>>();
        CombatAudioSpatialPolicy spatialPolicy;
        Func<bool> monoOutput;
        public string LastCombatAudioDiagnostic {get;private set;}
        public AudioSource LastResultSource {get;private set;}
        AudioClip requestedBgm;
        readonly HashSet<AudioSource> pausedByHold=new HashSet<AudioSource>();
        int audioHolds,combatLoopHolds;
        bool applicationPaused;
        bool destroyed;
        IDisposable applicationAudioHold;
        AudioSource reinaCinematicSource;
        ReinaCinematicAudioPlayback reinaCinematic;
        public void ConfigureReinaCinematicSource(AudioSource ownedSource)
        {
            if(reinaCinematic!=null&&!reinaCinematic.IsFinished)throw new InvalidOperationException("Original cinematic still owns its source.");
            if(ownedSource==null||ownedSource==sfxSource||ownedSource==bgmSource||ownedSource==themeSource||Array.IndexOf(combatVoices,ownedSource)>=0||!ownedSource.transform.IsChildOf(transform)||ownedSource.spatialBlend!=0||ownedSource.spatialize)
                throw new ArgumentException("A separate owned 2D cinematic source is required.");
            reinaCinematic?.Dispose();reinaCinematic=null;reinaCinematicSource=ownedSource;ownedSource.playOnAwake=false;
        }
        // Explicit approved-asset consumer only. No automatic load, cue substitution, BGM duck or impact candidate.
        public ReinaCinematicAudioPlayback BeginReinaCinematic(AudioClip originalFullVoiceFx,Action ended,Action skipped,Action<Exception> failed)
        {
            if(destroyed||combatScope==null||reinaCinematicSource==null)throw new InvalidOperationException("Original scope and owned cinematic source are required.");
            if(reinaCinematic!=null&&!reinaCinematic.IsFinished)throw new InvalidOperationException("Existing ultimate sequence still owns cinematic playback.");
            reinaCinematic?.Dispose();
            var created=new ReinaCinematicAudioPlayback(reinaCinematicSource,originalFullVoiceFx,effectsGain);reinaCinematic=created;
            try{created.Begin(ended,skipped,failed);created.SetPaused(audioHolds>0);return created;}
            catch{created.Dispose();if(ReferenceEquals(reinaCinematic,created))reinaCinematic=null;throw;}
        }
        sealed class OwnedPlayback:IDisposable
        {
            Action release;
            public OwnedPlayback(Action release){this.release=release;}
            public void Dispose(){var old=release;release=null;old?.Invoke();}
        }
        public void ApplyMixSettings(float music,float effects)
        {
            if(float.IsNaN(music)||float.IsNaN(effects)||music<0||music>1||effects<0||effects>1)
                throw new ArgumentOutOfRangeException("Normalized music/effects settings required.");
            CaptureGains();musicGain=music;effectsGain=effects;
            if(bgmSource!=null)bgmSource.volume=authoredBgmGain*musicGain*(themeSuppressesBase?themeBattleGain:1);
            if(sfxSource!=null)sfxSource.volume=authoredSfxGain*effectsGain;
            if(themeSource!=null)themeSource.volume=authoredThemeGain*themeClipGain*musicGain;
            foreach(var source in voiceEnds.Keys)if(source!=null)source.volume=voiceGains[source]*effectsGain*(voiceFactors.TryGetValue(source,out var factor)?factor:1);
            reinaCinematic?.SetEffectsGain(effectsGain);
        }
        void CaptureGains()
        {
            if(capturedGains)return;
            capturedGains=true;
            if(bgmSource!=null)authoredBgmGain=bgmSource.volume;
            if(sfxSource!=null)authoredSfxGain=sfxSource.volume;
            if(themeSource!=null)authoredThemeGain=themeSource.volume;
        }
        void OnEnable()=>ApplyMixSettings(Settings.UnitySettingsBackend.SavedMusic,Settings.UnitySettingsBackend.SavedEffects);
        // Menu/maintenance callers release only their own token. The application token is separate.
        public IDisposable AcquireAudioHold()
        {
            if(destroyed)throw new ObjectDisposedException(nameof(LastRailAudioCuePlayer));
            audioHolds++;
            if(audioHolds==1)
            {
                if(requestedBgm!=null)PauseForHold(bgmSource);
                if(themeSource!=null&&themeSource.clip!=null)PauseForHold(themeSource);
                foreach(var source in voiceEnds.Keys)PauseForHold(source);
                reinaCinematic?.SetPaused(true);
            }
            return new OwnedPlayback(()=>{
                if(destroyed)return;
                if(--audioHolds!=0)return;
                ResumeEligibleSources();
            });
        }
        public IDisposable AcquireCombatLoopHold()
        {
            if(destroyed)throw new ObjectDisposedException(nameof(LastRailAudioCuePlayer));
            combatLoopHolds++;
            foreach(var source in voiceEnds.Keys)if(source.loop)PauseForHold(source);
            return new OwnedPlayback(()=>{if(destroyed)return;if(--combatLoopHolds==0)ResumeEligibleSources();});
        }
        void ResumeEligibleSources()
        {
            if(audioHolds>0)return;
            reinaCinematic?.SetPaused(false);
            foreach(var source in new List<AudioSource>(pausedByHold))
                if(source==null){pausedByHold.Remove(source);}
                else if(!voiceEnds.ContainsKey(source)||!source.loop||combatLoopHolds==0){source.UnPause();pausedByHold.Remove(source);}
        }
        void PauseForHold(AudioSource source)
        {
            if(source!=null&&(audioHolds>0||combatLoopHolds>0&&voiceEnds.ContainsKey(source)&&source.loop)&&source.isPlaying){source.Pause();pausedByHold.Add(source);}
        }
        void StopOwnedSource(AudioSource source)
        {
            if(source==null)return;
            pausedByHold.Remove(source);source.Stop();source.clip=null;
        }
        void OnApplicationPause(bool paused)
        {
            applicationPaused=paused;
            if(paused){
                foreach(var pair in new List<KeyValuePair<AudioSource,Action>>(voiceEnds))if(pair.Key==null||!pair.Key.loop)pair.Value();
                if(applicationAudioHold==null&&!destroyed)applicationAudioHold=AcquireAudioHold();
            }
            else{var old=applicationAudioHold;applicationAudioHold=null;old?.Dispose();}
        }
        public void ConfigureThemeMix(float approvedBattleGain)
        {
            if(combatScope!=null)throw new InvalidOperationException("Configure the verified mix before binding a combat scope.");
            if(!float.IsFinite(approvedBattleGain)||approvedBattleGain<0||approvedBattleGain>1)throw new ArgumentOutOfRangeException(nameof(approvedBattleGain));
            themeBattleGain=approvedBattleGain;
        }
        public void ConfigureCombatSpatialPolicy(CombatAudioSpatialPolicy approvedPolicy,Func<bool> currentMonoOutput)
        {
            if(combatScope!=null)throw new InvalidOperationException("Configure spatial policy before the original scope.");
            spatialPolicy=approvedPolicy??throw new ArgumentNullException(nameof(approvedPolicy));
            monoOutput=currentMonoOutput??throw new ArgumentNullException(nameof(currentMonoOutput));
        }
        public void ConfigureCombatSources(AudioSource[] ownedVoices,AudioSource ownedTheme)
        {
            if(reinaCinematicSource!=null&&(ownedTheme==reinaCinematicSource||Array.IndexOf(ownedVoices??Array.Empty<AudioSource>(),reinaCinematicSource)>=0))throw new ArgumentException("Cinematic source cannot also be a combat or music voice.");
            EndCombatScope();
            if(ownedTheme!=null&&(ownedTheme==sfxSource||ownedTheme==bgmSource||!ownedTheme.transform.IsChildOf(transform)))
                throw new ArgumentException("Theme source must be independently owned by this player.");
            combatVoices=(AudioSource[])(ownedVoices??throw new ArgumentNullException(nameof(ownedVoices))).Clone();
            themeSource=ownedTheme;authoredThemeGain=ownedTheme==null?1:ownedTheme.volume;
        }
        public void BeginCombatScope(object owner,long generation)
        {
            if(owner==null||generation<0)throw new ArgumentException("Original account/run/node owner and generation required.");
            if(ReferenceEquals(owner,combatScope)&&generation==combatGeneration)return;
            EndCombatScope();
            var unique=new HashSet<AudioSource>();
            foreach(var source in combatVoices)
                if(source==null||source==sfxSource||source==bgmSource||source==themeSource||source.spatialBlend!=0||source.spatialize||!source.transform.IsChildOf(transform)||!unique.Add(source))
                    throw new ArgumentException("Combat voices must be distinct owned 2D sources without a spatializer, not shared UI/music sources.");
            foreach(var source in combatVoices){voiceGains[source]=source.volume;voicePans[source]=source.panStereo;}
            combatScope=owner;combatGeneration=generation;
            combat=new CombatSfxPlayback(unique.Count,BeginCombatVoice);
            music=new BattleMusicPlayback(BeginBaseTrack,BeginThemeTrack,SuppressBase);
        }
        public void EndCombatScope()
        {
            var oldCinematic=reinaCinematic;reinaCinematic=null;oldCinematic?.Dispose();
            LastResultSource=null;
            combatScope=null;combatGeneration=-1;
            var oldCombat=combat;var oldMusic=music;combat=null;music=null;
            try{oldCombat?.Dispose();}finally{
                try{oldMusic?.Dispose();}finally{
                    foreach(var pair in voiceGains)if(pair.Key!=null)pair.Key.volume=pair.Value;
                    voiceGains.Clear();
                    voicePans.Clear();
                }
            }
        }
        public string TryPlayCombatCue(long order,string actionId,string phase,string pulseId,long originalTime,string family,string cueId,int priority,int urgency)
        {
            if(combat==null)return "missing_original_scope";
            var binding=FindBinding(catalog.ResolveClipId(cueId)??cueId);
            if(binding?.Clip==null||binding.Loop)return "missing_sfx_binding";
            return combat.Offer(order,actionId,phase,pulseId,originalTime,family,binding.CueId,priority,urgency);
        }
        // The caller supplies the original subeffect and its disclosure owner, not HP differences.
        // Loop callers retain/dispose the returned lease; pooled owner generation is also checked.
        public string TryPlayCombatResult(long order,string actionId,string phase,string pulseId,long originalTime,string effectKey,
            CombatAudioResultBinding result,Func<bool> audible,Func<CombatAudioPosition> originalPosition,out IDisposable playback)
        {
            playback=null;
            LastResultSource=null;
            if(combat==null)return "missing_original_scope";
            var originalCombat=combat;
            if(result==null)return "missing_result_mapping";
            if(audible==null||!audible())return "not_disclosed";
            if(originalCombat!=combat)return "scope_changed";
            var clip=FindBinding(catalog.ResolveClipId(result.Cue)??result.Cue);
            if(clip?.Clip==null)return "missing_sfx_binding";
            if(originalPosition==null&&(!result.NonSpatialAllowed||clip.Loop))return "missing_original_position_owner";
            if(originalPosition!=null&&spatialPolicy==null)return "missing_spatial_policy";
            IDisposable created=null;
            string status=combat.OfferEffect(order,actionId,phase,pulseId,originalTime,result.Family,clip.CueId,result.Priority,result.Urgency,effectKey,
                (cue,end)=>{
                    var lease=BeginResultVoice(clip,result,audible,originalPosition,end);
                    if(lease==null)return null;
                    created=new OwnedPlayback(end);
                    return lease;
                });
            playback=created;
            return status=="missing_playback"?(effectsGain==0?"muted":applicationPaused?"background_suppressed":combatLoopHolds>0?"combat_paused":status):status;
        }
        IDisposable BeginResultVoice(LastRailAudioClipBinding binding,CombatAudioResultBinding result,Func<bool> audible,Func<CombatAudioPosition> position,Action ended)
        {
            var originalCombat=combat;
            if(effectsGain==0||applicationPaused||combatLoopHolds>0&&!binding.Loop||!audible())return null;
            if(originalCombat!=combat)return null;
            CombatAudioPosition initial=position?.Invoke();
            if(originalCombat!=combat)return null;
            if(position!=null&&initial==null)return null;
            Vector2? mix=position==null?new Vector2(0,1):spatialPolicy.Evaluate(initial.Point,initial.Audible,initial.DirectionDisclosed,result.NonSpatialAllowed,initial.Distance,result.Priority,monoOutput());
            if(!mix.HasValue)return null;
            if(originalCombat!=combat)return null;
            foreach(var source in combatVoices)
            {
                if(voiceEnds.ContainsKey(source))continue;
                CombatAudioPositionPlayback controller=null;
                // Validate the owner before allocating a pool slot or starting playback.
                if(position!=null)controller=new CombatAudioPositionPlayback(spatialPolicy,initial,binding.Loop,result.NonSpatialAllowed,result.Priority,position,monoOutput,
                    value=>{source.panStereo=value.x;voiceFactors[source]=value.y*(binding.Loop?binding.Volume:1);source.volume=voiceGains[source]*effectsGain*voiceFactors[source];},ended);
                voiceEnds.Add(source,ended);disclosures[source]=audible;
                if(controller!=null)positions[source]=controller;
                source.playOnAwake=false;source.loop=binding.Loop;source.clip=binding.Clip;source.panStereo=mix.Value.x;
                voiceFactors[source]=mix.Value.y*(binding.Loop?binding.Volume:1);
                source.volume=voiceGains[source]*effectsGain*voiceFactors[source];
                var lease=new OwnedPlayback(()=>ReleaseCombatVoice(source,ended));
                try {
                    if(Application.isPlaying){if(binding.Loop)source.Play();else source.PlayOneShot(binding.Clip,binding.Volume);}
                    PauseForHold(source);LastResultSource=source;return lease;
                } catch {lease.Dispose();throw;}
            }
            return null;
        }
        void ReleaseCombatVoice(AudioSource source,Action ended)
        {
            if(!voiceEnds.TryGetValue(source,out var active)||active!=ended)return;
            voiceEnds.Remove(source);disclosures.Remove(source);voiceFactors.Remove(source);
            if(positions.TryGetValue(source,out var controller)){positions.Remove(source);controller.Dispose();}
            StopOwnedSource(source);
            if(source!=null){source.volume=voiceGains[source];source.panStereo=voicePans[source];source.loop=false;}
        }
        public bool SelectBattleMusic(long originalRevision,string approvedCue,bool terminal)
            =>music!=null&&music.Select(originalRevision,approvedCue,terminal);
        public IDisposable BeginUltimateTheme(bool standard,string preparedCue)
            =>music?.BeginTheme(standard,preparedCue);
        void SuppressBase(bool value){themeSuppressesBase=value;ApplyMixSettings(musicGain,effectsGain);}
        IDisposable BeginBaseTrack(string cue)
        {
            var binding=FindBinding(catalog.ResolveClipId(cue)??cue);
            if(binding?.Clip==null||!binding.Loop||bgmSource==null)return null;
            long stamp=++basePlaybackGeneration;
            PrepareBgm(cue,binding.CueId,binding);
            return new OwnedPlayback(()=>{if(stamp==basePlaybackGeneration&&bgmSource!=null){StopOwnedSource(bgmSource);requestedBgm=null;}});
        }
        IDisposable BeginThemeTrack(string cue)
        {
            var binding=FindBinding(catalog.ResolveClipId(cue)??cue);
            if(binding?.Clip==null||themeSource==null||!float.IsFinite(themeBattleGain)||themeBattleGain<0||themeBattleGain>1)return null;
            long stamp=++themePlaybackGeneration;
            themeSource.playOnAwake=false;themeSource.loop=false;themeSource.clip=binding.Clip;
            themeClipGain=binding.Volume;themeSource.volume=authoredThemeGain*themeClipGain*musicGain;
            if(Application.isPlaying)themeSource.Play();
            PauseForHold(themeSource);
            return new OwnedPlayback(()=>{if(stamp==themePlaybackGeneration&&themeSource!=null)StopOwnedSource(themeSource);});
        }
        IDisposable BeginCombatVoice(string cue,Action ended)
        {
            if(effectsGain==0||applicationPaused||combatLoopHolds>0)return null;
            var binding=FindBinding(cue);
            if(binding?.Clip==null)return null;
            foreach(var source in combatVoices)
            {
                if(voiceEnds.ContainsKey(source))continue;
                voiceEnds.Add(source,ended);source.playOnAwake=false;source.loop=false;
                source.clip=binding.Clip;source.volume=voiceGains[source]*effectsGain;
                if(Application.isPlaying)source.PlayOneShot(binding.Clip,binding.Volume);
                PauseForHold(source);
                return new OwnedPlayback(()=>ReleaseCombatVoice(source,ended));
            }
            return null;
        }
        void Update()
        {
            if(Application.isPlaying)reinaCinematic?.Tick();
            foreach(var pair in new List<KeyValuePair<AudioSource,Action>>(voiceEnds))
            {
                try {
                    if(disclosures.TryGetValue(pair.Key,out var allowed)&&!allowed()){pair.Value();continue;}
                    if(positions.TryGetValue(pair.Key,out var position)){
                        position.Refresh();
                        if(position.LastFailure!=null)LastCombatAudioDiagnostic=position.LastFailure.GetType().Name+": "+position.LastFailure.Message;
                    }
                } catch(Exception error){LastCombatAudioDiagnostic=error.GetType().Name+": "+error.Message;pair.Value();}
            }
            if(!Application.isPlaying||audioHolds>0)return;
            foreach(var pair in new List<KeyValuePair<AudioSource,Action>>(voiceEnds))
                if(pair.Key==null||!pair.Key.isPlaying&&!pausedByHold.Contains(pair.Key))pair.Value();
        }
        void OnDisable(){EndCombatScope();requestedBgm=null;frameCues.Clear();cueFrame=-1;}
        void OnDestroy(){destroyed=true;EndCombatScope();applicationAudioHold?.Dispose();applicationAudioHold=null;pausedByHold.Clear();audioHolds=0;}

        public int BindingCount => clipBindings == null ? 0 : clipBindings.Length;
        public bool HasServerTransport => false;
        public bool HasRewardCalculation => false;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public bool HasMissingClipBinding
        {
            get
            {
                if (clipBindings == null)
                {
                    return true;
                }

                foreach (var binding in clipBindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.CueId) || binding.Clip == null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasUnintendedAutoplay
        {
            get
            {
                return (sfxSource != null && sfxSource.playOnAwake) ||
                       (bgmSource != null && bgmSource.playOnAwake);
            }
        }

        public void PlayCueFromUnityEvent(string cueId)
        {
            TryPlayCue(cueId);
        }

        public LastRailAudioCuePlayResult TryPlayCue(string cueId)
        {
            var resolved = catalog.ResolveClipId(cueId);
            if (resolved == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(cueId, null, "unknown_cue");
            }

            var binding = FindBinding(resolved);
            if (binding == null || binding.Clip == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(cueId, resolved, "missing_clip_binding");
            }

            if(binding.Loop&&music!=null)return LastRailAudioCuePlayResult.NotPlayed(cueId,resolved,"use_original_music_state");
            return binding.Loop ? PrepareBgm(cueId, resolved, binding) : PlaySfx(cueId, resolved, binding);
        }

        public LastRailAudioCuePlayResult TryPrepareBgmLoop(string cueId)
        {
            return TryPlayCue(cueId);
        }

        public void ConfigureForTests(
            AudioSource testSfxSource,
            AudioSource testBgmSource,
            LastRailAudioClipBinding[] testBindings)
            =>ConfigureBindings(testSfxSource,testBgmSource,testBindings);
        public void ConfigureBindings(AudioSource testSfxSource,AudioSource testBgmSource,LastRailAudioClipBinding[] testBindings)
        {
            if(combatScope!=null)throw new InvalidOperationException("Configure audio bindings before combat scope.");
            sfxSource = testSfxSource;
            bgmSource = testBgmSource;
            clipBindings = testBindings ?? Array.Empty<LastRailAudioClipBinding>();
            frameCues.Clear();
            cueFrame = -1;
            capturedGains=false;
            CaptureGains();
            ConfigureSourceDefaults();
        }

        private LastRailAudioCuePlayResult PlaySfx(string requested, string resolved, LastRailAudioClipBinding binding)
        {
            if (sfxSource == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(requested, resolved, "missing_sfx_source");
            }

            if (cueFrame != Time.frameCount)
            {
                frameCues.Clear();
                cueFrame = Time.frameCount;
            }
            if (!frameCues.Add(resolved))
                return LastRailAudioCuePlayResult.NotPlayed(requested, resolved, "same_frame_duplicate");
            ConfigureSourceDefaults();
            sfxSource.loop = false;
            if (Application.isPlaying)
            {
                sfxSource.PlayOneShot(binding.Clip, binding.Volume);
            }

            return LastRailAudioCuePlayResult.PlayedCue(requested, resolved, false);
        }

        internal AudioClip BoundLoopClip(string cue) => FindBinding(cue) is LastRailAudioClipBinding b && b.Loop ? b.Clip : null;
        internal void PlayBoundMusicLoop(LastRailAudioClipBinding binding)
        {
            if (!binding.Loop || binding.Clip == null || !ReferenceEquals(FindBinding(binding.CueId), binding))
                throw new InvalidOperationException("An original bound music loop is required.");
            PrepareBgm(binding.CueId, binding.CueId, binding);
        }

        private LastRailAudioCuePlayResult PrepareBgm(string requested, string resolved, LastRailAudioClipBinding binding)
        {
            if (bgmSource == null)
            {
                return LastRailAudioCuePlayResult.NotPlayed(requested, resolved, "missing_bgm_source");
            }

            ConfigureSourceDefaults();
            bool changed=bgmSource.clip!=binding.Clip;
            if (changed) bgmSource.clip = binding.Clip;
            bgmSource.loop = true;
            // The single loop uses its authored AudioSource gain; never reset owner volume/mute.
            if (Application.isPlaying && (changed||requestedBgm!=binding.Clip&&!bgmSource.isPlaying))
            {
                bgmSource.Play();
            }
            requestedBgm=binding.Clip;
            PauseForHold(bgmSource);

            return LastRailAudioCuePlayResult.PlayedCue(requested, resolved, true);
        }

        private LastRailAudioClipBinding FindBinding(string cueId)
        {
            if (clipBindings == null)
            {
                return null;
            }

            foreach (var binding in clipBindings)
            {
                if (binding != null && string.Equals(binding.CueId, cueId, StringComparison.Ordinal))
                {
                    return binding;
                }
            }

            return null;
        }

        private void ConfigureSourceDefaults()
        {
            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
            }

            if (bgmSource != null)
            {
                bgmSource.playOnAwake = false;
            }
        }
    }

    [Serializable]
    public sealed class LastRailAudioClipBinding
    {
        [SerializeField] private string cueId;
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool loop;
        [SerializeField] private float volume;

        public LastRailAudioClipBinding(string cueId, AudioClip clip, bool loop, float volume)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(cueId));
            }

            this.cueId = cueId.Trim();
            this.clip = clip;
            this.loop = loop;
            this.volume = Mathf.Clamp01(volume);
        }

        public string CueId => cueId;
        public AudioClip Clip => clip;
        public bool Loop => loop;
        public float Volume => volume;
    }

    public sealed class LastRailAudioCuePlayResult
    {
        private LastRailAudioCuePlayResult(
            bool played,
            string requestedCueId,
            string resolvedClipId,
            bool loop,
            string reason)
        {
            Played = played;
            RequestedCueId = requestedCueId;
            ResolvedClipId = resolvedClipId;
            Loop = loop;
            Reason = reason;
        }

        public bool Played { get; }
        public string RequestedCueId { get; }
        public string ResolvedClipId { get; }
        public bool Loop { get; }
        public string Reason { get; }

        public static LastRailAudioCuePlayResult PlayedCue(string requestedCueId, string resolvedClipId, bool loop)
        {
            return new LastRailAudioCuePlayResult(true, requestedCueId, resolvedClipId, loop, "ready");
        }

        public static LastRailAudioCuePlayResult NotPlayed(string requestedCueId, string resolvedClipId, string reason)
        {
            return new LastRailAudioCuePlayResult(false, requestedCueId, resolvedClipId, false, reason);
        }
    }
}
#endif
