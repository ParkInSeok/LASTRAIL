using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Core.Simulation;

namespace LastRail.Battle
{
    public enum BasicCombatTarget { EnemyAny, EnemyInterior, EnemyExterior, Operators, Train }
    public enum BasicCombatOutcome { Running, Victory, Failure }
    public sealed class BasicCombatTiming
    {
        public int TargetableDelay { get; }
        public int FirstCastDelay { get; }
        public string Device { get; }
        public bool FrontReach { get; }
        public bool? PreferFront { get; }
        public BasicCombatTiming(int targetableDelay,int firstCastDelay,string device,bool frontReach,bool? preferFront)
        {
            if(targetableDelay<0||firstCastDelay<targetableDelay||(device!=null&&device!="Combat"&&device!="Gun"))throw new ArgumentException();
            TargetableDelay=targetableDelay;FirstCastDelay=firstCastDelay;Device=device;FrontReach=frontReach;PreferFront=preferFront;
        }
    }

    // First executable basic-attack slice. Resolved, explicit inputs only; not an
    // admission parser and not a replacement for the complete kit/effect consumer.
    public sealed class BasicCombatActor
    {
        public string Id { get; }
        public bool Enemy { get; }
        public bool Train { get; }
        public bool Exterior { get; }
        public int Slot { get; }
        public int MaxHp { get; }
        public int Attack { get; }
        public int ArmorBps { get; }
        public int PeriodTicks { get; }
        public int WindupTicks { get; }
        public int RecoveryTicks { get; }
        public BasicCombatTarget Target { get; }
        public int ShieldCapBps { get; }
        public BasicCombatTiming Timing { get; }
        public BasicCombatActor(string id,bool enemy,bool train,bool exterior,int slot,int hp,int attack,int armorBps,int periodTicks,int windupTicks,int recoveryTicks,BasicCombatTarget target,int shieldCapBps,BasicCombatTiming timing=null)
        {
            if(string.IsNullOrWhiteSpace(id)||slot<0||hp<=0||attack<0||armorBps<0||armorBps>10000||periodTicks<=0||windupTicks<0||recoveryTicks<0
                ||shieldCapBps<0||shieldCapBps>10000|| !Enum.IsDefined(typeof(BasicCombatTarget),target)|| (enemy&&train)
                || (enemy && target!=BasicCombatTarget.Train && target!=BasicCombatTarget.Operators)
                || (!enemy && (target==BasicCombatTarget.Train||target==BasicCombatTarget.Operators))) throw new ArgumentException("Invalid explicit basic actor configuration.");
            Id=id;Enemy=enemy;Train=train;Exterior=exterior;Slot=slot;MaxHp=hp;Attack=attack;ArmorBps=armorBps;
            PeriodTicks=periodTicks;WindupTicks=windupTicks;RecoveryTicks=recoveryTicks;Target=target;
            ShieldCapBps=shieldCapBps;
            Timing=timing;
        }
    }

    public sealed class BasicCombatHit
    {
        public long Tick { get; }
        public string Source { get; }
        public string Target { get; }
        public int HpLoss { get; }
        public int RemainingHp { get; }
        public int Absorbed { get; }
        internal BasicCombatHit(long tick,string source,string target,int loss,int remaining,int absorbed)
        {Tick=tick;Source=source;Target=target;HpLoss=loss;RemainingHp=remaining;Absorbed=absorbed;}
        public override string ToString()=>Tick+":"+Source+">"+Target+":"+HpLoss+":"+RemainingHp;
    }

    public sealed partial class BasicCombatSimulation
    {
        sealed class Actor
        {
            internal readonly BasicCombatActor Config;
            readonly CombatVitals ownedVitals;readonly Func<CombatVitals> borrowedVitals;
            internal CombatVitals Vitals=>borrowedVitals?.Invoke()??ownedVitals;
            readonly string sourceIdentity;internal string Identity=>Config?.Id??sourceIdentity;
            internal Actor(string sourceId,int maxHp){sourceIdentity=sourceId;ownedVitals=new CombatVitals(maxHp,0,true);}
            internal Actor(string sourceId,Func<CombatVitals> currentVitals){sourceIdentity=sourceId;borrowedVitals=currentVitals??throw new ArgumentNullException(nameof(currentVitals));}
            internal int Hp=>Vitals.Hp;
            internal long Ready;
            internal long RecoveryUntil;
            internal int DownCount;
            internal long RecoverAt,ProtectedUntil;
            internal long AvailableAt;
            internal int TargetCursor;
            internal bool Device=>Config.Timing?.Device!=null;
            internal Actor(BasicCombatActor config,long baseTick=0,bool approvedVitals=false){Config=config;ownedVitals=new CombatVitals(config.MaxHp,config.ShieldCapBps,approvedVitals);AvailableAt=checked(baseTick+(config.Timing?.TargetableDelay??0));Ready=checked(baseTick+(config.Timing?.FirstCastDelay??0));}
        }
        sealed class Attack
        {
            internal Actor Source,Target;
            internal long Due,Sequence;
        }
        FixedStepDebt clock;
        RunRandom random;
        readonly uint configuredRandomState;
        readonly bool approvedVitals;
        readonly BasicCombatActor[][] waves;
        readonly List<Actor> allies;
        readonly List<Actor> enemies=new List<Actor>();
        readonly List<Attack> attacks=new List<Attack>();
        readonly List<BasicCombatHit> events=new List<BasicCombatHit>();
        long sequence;
        int waveGapTicks;
        int[] frontSlots=Array.Empty<int>();
        string interiorFocus,exteriorFocus;
        string localOwnerBinding;
        internal string LocalOwnerAccount { get; private set; }
        public string ConfigurationVersion { get; }
        public BasicCombatOutcome Outcome { get; private set; }
        public int CompletedWaves { get; private set; }
        public long Tick=>clock.CompletedSteps;
        public long PendingSteps=>clock.PendingSteps;
        public uint RandomState=>random.ExportState();
        public IReadOnlyList<BasicCombatHit> Events=>events.AsReadOnly();
        public BasicCombatSimulation(string configurationVersion,long stepTimeSpanTicks,uint seed,BasicCombatActor[] allyDefinitions,BasicCombatActor[][] waveDefinitions):this(configurationVersion,stepTimeSpanTicks,seed,allyDefinitions,waveDefinitions,false){}
        public BasicCombatSimulation(string configurationVersion,long stepTimeSpanTicks,uint seed,BasicCombatActor[] allyDefinitions,BasicCombatActor[][] waveDefinitions,bool useApprovedVitalsPolicy)
        {
            if(string.IsNullOrWhiteSpace(configurationVersion)||allyDefinitions==null||waveDefinitions==null||allyDefinitions.Any(x=>x==null||x.Enemy)
                ||allyDefinitions.Count(x=>x.Train)!=1||waveDefinitions.Length==0||waveDefinitions.Any(w=>w==null||w.Length==0||w.Any(x=>x==null||!x.Enemy)))
                throw new ArgumentException("Complete explicit allies and waves are required.");
            var all=allyDefinitions.Concat(waveDefinitions.SelectMany(x=>x)).ToArray();
            if(all.Select(x=>x.Id).Distinct(StringComparer.Ordinal).Count()!=all.Length)throw new ArgumentException("Actor spawn IDs must be unique across the run.");
            ConfigurationVersion=configurationVersion; clock=new FixedStepDebt(stepTimeSpanTicks);random=new RunRandom(seed);
            configuredRandomState=random.ExportState();approvedVitals=useApprovedVitalsPolicy;
            allies=allyDefinitions.Select(x=>new Actor(x,0,approvedVitals)).ToList();waves=waveDefinitions.Select(w=>w.ToArray()).ToArray();SpawnWave();
        }
        void ValidateEffectiveRecoveryHp(Actor target,int maxHp)
        {
            if(operatorRecoveryMs!=null&&!target.Config.Train&&!target.Config.Enemy&&!target.Device&&(long)maxHp*operatorRecoveryBps/10000<=0)
                throw new ArgumentException("Effective maximum cannot satisfy the original scheduled recovery policy.");
        }
        public int GetHp(string id)
        {
            var actor=allies.Concat(enemies).SingleOrDefault(x=>x.Identity==id);
            if(actor==null)throw new KeyNotFoundException(id);return actor.Hp;
        }
        public bool IsPaused=>clock.IsPaused;
        public void SetPaused(bool paused){if(ordinaryFactory!=null){if(clock.IsPaused!=paused)OrdinaryControl("PAUSE",new Newtonsoft.Json.Linq.JValue(paused));}else if(seraCommands!=null)ControlSera("PAUSE",new Newtonsoft.Json.Linq.JValue(paused));else clock.SetPaused(paused);}
        Actor EffectTarget(string id)
        {
            if(seraCommands!=null||ordinaryFactory!=null)throw new InvalidOperationException("Ordinary owner requires its resolved command boundary.");
            if(Outcome!=BasicCombatOutcome.Running)throw new InvalidOperationException("Combat is terminal.");
            return allies.Concat(enemies).SingleOrDefault(x=>x.Config.Id==id)??throw new KeyNotFoundException(id);
        }
        public void ApplyResolvedMaxHp(string targetId,int finalMaxHp)
        {
            ResolvedMaxHpTarget(targetId,finalMaxHp).Vitals.RebaseMaxHp(finalMaxHp);
        }
        Actor ResolvedMaxHpTarget(string targetId,int finalMaxHp)
        {
            var target=EffectTarget(targetId);
            if(!approvedVitals)throw new InvalidOperationException("Original HP policy does not support rebasing.");
            if(finalMaxHp<=0)throw new ArgumentOutOfRangeException(nameof(finalMaxHp));
            ValidateEffectiveRecoveryHp(target,finalMaxHp);
            return target;
        }
        // Final resolved values only. Validate the entire affected set before any
        // HP changes; equipment authority and effect application IDs belong upstream.
        public void ApplyResolvedMaxHpBatch(IReadOnlyDictionary<string,int> finalMaxima)
        {
            if(finalMaxima==null)throw new ArgumentNullException(nameof(finalMaxima));
            var staged=new List<(Actor Target,int Maximum)>();
            foreach(var pair in finalMaxima)staged.Add((ResolvedMaxHpTarget(pair.Key,pair.Value),pair.Value));
            foreach(var value in staged)value.Target.Vitals.RebaseMaxHp(value.Maximum);
        }
        // Low-level resolved effect dispatch; not player commands, cooldown acceptance or authority.
        public int ApplyResolvedHeal(string targetId,int amount)=>EffectTarget(targetId).Vitals.RestoreHp(amount,10000);
        public int ApplyResolvedRepair(string targetId,int amount,int repairCapBps)
        {
            var target=EffectTarget(targetId);if(!target.Config.Train)throw new ArgumentException("Repair requires the train.");
            return target.Vitals.RestoreHp(amount,repairCapBps);
        }
        public int ApplyResolvedShield(string targetId,CombatShieldSource source,int amount,long expiryTick)
            =>EffectTarget(targetId).Vitals.Shield(source,amount,expiryTick,Tick);
        public void SetSpeed(int halfUnits){if(ordinaryFactory!=null)OrdinaryControl("SPEED",new Newtonsoft.Json.Linq.JValue(halfUnits));else if(seraCommands!=null)ControlSera("SPEED",new Newtonsoft.Json.Linq.JValue(halfUnits));else clock.SetSpeed(halfUnits);}
        public void Advance(long elapsedTimeSpanTicks,int maximumSteps)
        {
            if(elapsedTimeSpanTicks<0||maximumSteps<0)throw new ArgumentOutOfRangeException();
            if(ordinaryFactory!=null){AdvanceOrdinary(elapsedTimeSpanTicks,maximumSteps);return;}
            if(seraCommands!=null){AdvanceSera(elapsedTimeSpanTicks,maximumSteps);return;}
            if(Outcome!=BasicCombatOutcome.Running)return;
            if(railJourney!=null){AdvanceRail(elapsedTimeSpanTicks,maximumSteps);return;}
            clock.Credit(elapsedTimeSpanTicks);
            for(var i=0;i<maximumSteps && Outcome==BasicCombatOutcome.Running && clock.TryConsumeStep();i++)Step();
        }
        void SpawnWave()
        {
            var start=checked(Tick+(CompletedWaves==0?0:waveGapTicks));
            var carried=enemies.Where(IsRailArtillery).ToArray();enemies.Clear();enemies.AddRange(waves[CompletedWaves].Select(x=>new Actor(x,start,approvedVitals)));enemies.AddRange(carried);
        }
        public bool SetEnemyFocus(string id)
        {
            if(ordinaryFactory!=null)return false;
            if(Outcome!=BasicCombatOutcome.Running)return false;
            var target=enemies.SingleOrDefault(x=>x.Config.Id==id&&x.Hp>0&&x.AvailableAt<=Tick);if(target==null)return false;
            if(target.Config.Exterior)exteriorFocus=id;else interiorFocus=id;return true;
        }
        Actor SelectTarget(Actor source)
        {
            if(source.Config.Enemy&&source.Config.Target==BasicCombatTarget.Operators)
            {
                var taunt=statuses.Where(x=>x.Target==source&&x.Type=="taunt"&&x.Expiry>Tick&&x.Source.Hp>0).OrderByDescending(x=>x.Sequence).ThenBy(x=>x.Source.Config.Id,StringComparer.Ordinal).FirstOrDefault();
                if(taunt!=null)return taunt.Source;
            }
            IEnumerable<Actor> candidates;
            switch(source.Config.Target)
            {
                case BasicCombatTarget.Train:candidates=allies.Where(x=>x.Config.Train);break;
                case BasicCombatTarget.Operators:candidates=allies.Where(x=>!x.Config.Train&&!x.Device);break;
                case BasicCombatTarget.EnemyInterior:candidates=enemies.Where(x=>!x.Config.Exterior);break;
                case BasicCombatTarget.EnemyExterior:candidates=enemies.Where(x=>x.Config.Exterior);break;
                default:candidates=enemies;break;
            }
            var eligible=candidates.Where(x=>x.Hp>0&&x.AvailableAt<=Tick&&CanDirectlyTarget(source,x)).OrderBy(x=>x.Config.Slot).ThenBy(x=>x.Config.Id,StringComparer.Ordinal).ToArray();
            if(source.Config.Timing?.PreferFront!=null)
            {
                var preferred=eligible.Where(x=>frontSlots.Contains(x.Config.Slot)==source.Config.Timing.PreferFront.Value).ToArray();if(preferred.Length>0)eligible=preferred;
                return eligible.FirstOrDefault(x=>x.Config.Slot>=source.TargetCursor)??eligible.FirstOrDefault();
            }
            if(!source.Config.Enemy)
            {
                if(source.Config.Timing?.FrontReach==true)eligible=eligible.Where(x=>x.Config.Slot<2).ToArray();
                var focus=source.Config.Target==BasicCombatTarget.EnemyExterior?exteriorFocus:interiorFocus;
                return eligible.FirstOrDefault(x=>x.Config.Id==focus)??eligible.FirstOrDefault();
            }
            return eligible.FirstOrDefault();
        }
        void Step()
        {
            if(railTerminalPending){AdvanceArtillery();return;}
            if(railWavePending&&!railJourney.Selecting){railWavePending=false;SpawnWave();}
            RecoverOperators();
            ProcessSkillCommands();
            // Explicit input array order assigns persistent action sequence, never a dictionary.
            foreach(var actor in allies.Concat(enemies))
            {
                if(actor.Hp<=0||actor.AvailableAt>Tick||actor.Ready>Tick||SkillBusy(actor)||attacks.Any(x=>x.Source==actor))continue;
                var target=SelectTarget(actor);if(target==null)continue;
                if(actor.Config.Timing?.PreferFront!=null)actor.TargetCursor=target.Config.Slot+1;
                attacks.Add(new Attack{Source=actor,Target=target,Due=checked(Tick+actor.Config.WindupTicks),Sequence=checked(++sequence)});
                actor.Ready=checked(Tick+actor.Config.PeriodTicks);
            }
            var due=OrderDueActions(attacks.Where(x=>x.Due<=Tick).Select(a=>new DueAction(a.Due,a.Sequence,()=>ResolveBasic(a))).Concat(DueSkills())).ToArray();
            foreach(var action in due) action.Execute();
            if(allies.Single(x=>x.Config.Train).Hp==0){if(railArtillery==null)Outcome=BasicCombatOutcome.Failure;else {railTerminalPending=true;AdvanceArtillery();}return;}
            if(!railWavePending&&enemies.Where(x=>!IsRailArtillery(x)).All(x=>x.Hp==0)&&attacks.Count==0&&!HasPendingSkills())
            {
                CompletedWaves++;
                if(CompletedWaves==waves.Length){if(railArtillery==null)Outcome=BasicCombatOutcome.Victory;else railTerminalPending=true;}
                else {railJourney?.CheckBoundary(CompletedWaves,Tick);if(railJourney?.Selecting==true)railWavePending=true;else SpawnWave();}
            }
            AdvanceArtillery();
        }
        void ResolveBasic(Attack attack)
        {
            if(!attacks.Contains(attack))return;
                // Committed impact survives its source's death. Invalid targets retarget
                // with a full windup without charging the accepted cooldown again.
                if(attack.Target.Hp<=0)
                {
                    if(attack.Source.Hp<=0){attacks.Remove(attack);return;}
                    var target=SelectTarget(attack.Source);
                    if(target==null){attacks.Remove(attack);return;}
                    attack.Target=target;attack.Due=checked(Tick+attack.Source.Config.WindupTicks);
                    if(attack.Due>Tick)return;
                }
                var damage=SkillAdjustedDamage(attack.Source,attack.Target,attack.Source.Config.Attack,1,1,false);
                var hit=attack.Target.Vitals.Damage(damage,Tick,attack.Target.ProtectedUntil>Tick);
                if(hit.HpLoss>0&&attack.Target.Hp==0)OperatorDown(attack.Target);
                events.Add(new BasicCombatHit(Tick,attack.Source.Config.Id,attack.Target.Config.Id,hit.HpLoss,attack.Target.Hp,hit.Absorbed));
                ChargeBasic(attack.Source,hit.HpLoss);
                attack.Source.Ready=Math.Max(attack.Source.Ready,checked(Tick+attack.Source.Config.RecoveryTicks));
                attack.Source.RecoveryUntil=checked(Tick+attack.Source.Config.RecoveryTicks);
                attacks.Remove(attack);
        }
    }
}
