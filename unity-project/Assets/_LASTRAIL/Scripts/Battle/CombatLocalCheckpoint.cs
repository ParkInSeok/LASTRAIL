using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LastRail.Core.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    public sealed partial class BasicCombatSimulation
    {
        const string FrontlinePolicyId="ce03.frontline-first-v1";
        // Set only while constructing a model from an exact versioned prepared profile.
        string targetingPolicyId;
        string LocalStateSchema=>LegacyLocalStateSchema+(approvedVitals?"-approved-vitals-v1":"")+(localActiveTiming!=null?"-local-b-active-timing-v1":"");
        string LegacyLocalStateSchema=>railJourney!=null?"ce03-rail-headless-state-v1":individualUltimates?OperatorAutomationSchema:targetingPolicyId==null?"ce03-headless-state-v1":"ce03-headless-frontline-state-v1";
        // Complete current headless model state, NOT the authenticated/durable r2
        // run envelope. Caller must supply trusted original config and storage.
        internal static string StableJson(JToken value)=>Ce03CanonicalJson.Write(value);
        internal static string Digest(string value){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false,true).GetBytes(value))).Replace("-","").ToLowerInvariant();}
        string ConfigurationDigest()
        {
            var config=new JObject{["version"]=ConfigurationVersion,["step"]=clock.Capture().StepTicks,["seed"]=configuredRandomState,["allies"]=JArray.FromObject(allies.Select(x=>x.Config)),["waves"]=JArray.FromObject(waves),["skills"]=new JObject(skills.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new JProperty(x.Key,x.Value.Data.DeepClone()))),["passives"]=JObject.FromObject(passives),["selectedUltimate"]=selectedUltimate,["stepMs"]=skillStepMs,["queueExpiryMs"]=queueExpiryMs,["frontSlots"]=JArray.FromObject(frontSlots),["waveGapTicks"]=waveGapTicks,["recoveryMs"]=JArray.FromObject(operatorRecoveryMs??Array.Empty<int>()),["limits"]=new JArray(repairCap,outgoingCap,magnitudeCap,markCap,armorCap,squadBonus,trainBonus,shieldBonus,precisionBonus,operatorRecoveryBps,operatorProtectionMs)};
            config["ownerBinding"]=localOwnerBinding;
            if(skillLifestealAuthority!=null)config["skillLifestealBinding"]=skillLifestealBinding;
            if(localActiveTiming!=null)config["localActiveTiming"]=localActiveTiming.Configuration;
            if(approvedVitals)config["vitalsPolicy"]="combat-vitals.rebase-v1";
            if(railJourney!=null)config["railJourney"]=railJourney.Configuration;
            if(targetingPolicyId!=null)config["targetingPolicyId"]=targetingPolicyId;
            if(leaderChargePerHit>0)config["localLeaderChargePolicy"]=new JObject{["version"]="basic-hp-hit-v1",["perHit"]=leaderChargePerHit};
            if(individualUltimates)config["operatorAutomationPolicy"]=new JObject{["schema"]=OperatorAutomationSchema,["perOwnBasicHpHit"]=4,["generalAuto"]=generalSkillAuto};
            return Digest(StableJson(config));
        }
        static long Nat(JToken parent,string key,long maximum=long.MaxValue)
        {if(parent?[key]?.Type!=JTokenType.Integer)throw new ArgumentException("Missing integer "+key);var value=(long)parent[key];if(value<0||value>maximum)throw new ArgumentException("Invalid integer "+key);return value;}
        static bool Boolean(JToken parent,string key){if(parent?[key]?.Type!=JTokenType.Boolean)throw new ArgumentException();return (bool)parent[key];}
        static JArray Rows(JToken parent,string key,int maximum=100000){if(!(parent?[key] is JArray array)||array.Count>maximum)throw new ArgumentException("Invalid array "+key);return array;}
        static string NullableText(JToken parent,string key){if(parent?[key]?.Type==JTokenType.Null)return null;return Text(parent,key);}
        public string CaptureLocalState()
        {
            if(ordinaryFactory!=null)return CaptureOrdinary();
            if(seraCommands!=null)return CaptureSeraOwner();
            var referenced=allies.Concat(enemies).Concat(attacks.SelectMany(x=>new[]{x.Source,x.Target})).Concat(statuses.SelectMany(x=>new[]{x.Source,x.Target})).Concat(commands.Values.SelectMany(x=>x.Targets??Array.Empty<Actor>())).Distinct().OrderBy(x=>x.Config.Id,StringComparer.Ordinal).ToArray();
            var c=clock.Capture();
            var root=new JObject{
                ["schema"]=LocalStateSchema,["configurationSha256"]=ConfigurationDigest(),
                ["clock"]=new JObject{["step"]=c.StepTicks,["debt"]=c.DebtHalfTicks,["completed"]=c.CompletedSteps,["speed"]=c.SpeedHalfUnits,["paused"]=c.IsPaused},
                ["rng"]=random.ExportState(),["sequence"]=sequence,["inputSequence"]=inputSequence,["completedWaves"]=CompletedWaves,["outcome"]=Outcome.ToString(),["interiorFocus"]=interiorFocus,["exteriorFocus"]=exteriorFocus,
                ["allies"]=new JArray(allies.Select(x=>x.Config.Id)),["enemies"]=new JArray(enemies.Select(x=>x.Config.Id)),
                ["actors"]=new JArray(referenced.Select(x=>new JObject{["id"]=x.Config.Id,["vitals"]=x.Vitals.CaptureLocal(),["ready"]=x.Ready,["recoveryUntil"]=x.RecoveryUntil,["downCount"]=x.DownCount,["recoverAt"]=x.RecoverAt,["protectedUntil"]=x.ProtectedUntil,["availableAt"]=x.AvailableAt,["cursor"]=x.TargetCursor})),
                ["attacks"]=new JArray(attacks.Select(x=>new JObject{["source"]=x.Source.Config.Id,["target"]=x.Target.Config.Id,["due"]=x.Due,["sequence"]=x.Sequence})),
                ["skillReady"]=new JArray(skills.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new JObject{["key"]=x.Key,["ready"]=x.Value.Ready})),
                ["commands"]=new JArray(commands.Values.OrderBy(x=>x.InputOrder).Select(x=>new JObject{["id"]=x.Id,["status"]=x.Status,["skill"]=x.Skill.Caster.Config.Id+"/"+Text(x.Skill.Data,"id"),["order"]=x.InputOrder,["sequence"]=x.Seq,["expiry"]=x.Expiry,["due"]=x.Due,["targets"]=x.Targets==null?JValue.CreateNull():new JArray(x.Targets.Select(a=>a.Config.Id))})),
                ["statuses"]=new JArray(statuses.Select(x=>new JObject{["source"]=x.Source.Config.Id,["target"]=x.Target.Config.Id,["ability"]=x.Ability,["type"]=x.Type,["index"]=x.Index,["value"]=x.Value,["expiry"]=x.Expiry,["sequence"]=x.Sequence})),
                ["events"]=JArray.FromObject(events)};
            root["effectJournal"]=CaptureEffectJournal();
            if(skillLifestealAuthority!=null)root["skillRecoveryCommit"]=SkillRecoveryCommit(root,root["effectJournal"]);
            if(localActiveTiming!=null)root["localActiveTiming"]=localActiveTiming.Capture();
            if(railJourney!=null)root["railJourney"]=CaptureRail();
            if(targetingPolicyId!=null)root["targetingPolicyId"]=targetingPolicyId;
            if(leaderChargePerHit>0)root["leaderCharge"]=leaderCharge;
            if(individualUltimates)root["operatorAutomation"]=CaptureOperatorAutomation();
            return StableJson(root);
        }
        public static BasicCombatSimulation RestoreLocalState(string json,Func<BasicCombatSimulation> freshFactory)
        {
            if(json==null||json.Length>8*1024*1024||freshFactory==null)throw new ArgumentException("Invalid checkpoint input.");
            try
            {
                JObject root;
                using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=48})
                {
                    root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});
                    if(reader.Read()||root.Descendants().Any(x=>x.Type==JTokenType.Comment))throw new ArgumentException("Invalid JSON.");
                }
                if((string)root["schema"]==OrdinaryFiveCombatFactory.CheckpointVersion)return RestoreOrdinary(root,freshFactory);
                if((string)root["schema"]=="combat.sera-unity-owner.v1"||(string)root["schema"]==OrdinaryFiveSeraFactory.CheckpointVersion)return RestoreSeraOwner(root,freshFactory);
                var rail=root["railJourney"];if(rail!=null&&!(rail is JObject))throw new ArgumentException("Invalid rail state");root.Remove("railJourney");
                var timing=root["localActiveTiming"];if(timing!=null&&!(timing is JObject))throw new ArgumentException("Invalid timing state.");root.Remove("localActiveTiming");
                var automation=root["operatorAutomation"];if(automation!=null&&!(automation is JObject))throw new ArgumentException("Invalid automation state.");root.Remove("operatorAutomation");
                var localCharge=root.Property("leaderCharge");
                var chargeValue=localCharge==null?(long?)null:Nat(root,"leaderCharge",100);localCharge?.Remove();
                var effectJournal=root["effectJournal"];if(effectJournal!=null&&!(effectJournal is JObject))throw new ArgumentException("Invalid effect journal");root.Remove("effectJournal");
                var recoveryCommit=root["skillRecoveryCommit"];root.Remove("skillRecoveryCommit");
                var policyProperty=root.Property("targetingPolicyId");
                string checkpointPolicy=policyProperty==null?null:Text(root,"targetingPolicyId");policyProperty?.Remove();
                Fields(root,"schema","configurationSha256","clock","rng","sequence","inputSequence","completedWaves","outcome","interiorFocus","exteriorFocus","allies","enemies","actors","attacks","skillReady","commands","statuses","events");
                var s=freshFactory();if(s==null||s.Tick!=0||s.commands.Count!=0||Text(root,"schema")!=s.LocalStateSchema||checkpointPolicy!=s.targetingPolicyId||Text(root,"configurationSha256")!=s.ConfigurationDigest())throw new ArgumentException("Original configuration mismatch.");
                if(s.skillLifestealAuthority!=null)
                {if(effectJournal==null||recoveryCommit?.Type!=JTokenType.String||(string)recoveryCommit!=SkillRecoveryCommit(root,effectJournal))throw new ArgumentException("Incomplete skill recovery checkpoint");}
                else if(recoveryCommit!=null)throw new ArgumentException("Unexpected skill recovery checkpoint");
                if((s.leaderChargePerHit>0)!=chargeValue.HasValue)throw new ArgumentException("Charge policy mismatch.");
                if(rail!=null){if(rail["waitingForWave"]?.Type!=JTokenType.Boolean)throw new ArgumentException("Missing rail hold");s.railWavePending=(bool)rail["waitingForWave"];}
                if(rail!=null){if(rail["terminalPending"]?.Type!=JTokenType.Boolean)throw new ArgumentException("Missing terminal hold");s.railTerminalPending=(bool)rail["terminalPending"];if(s.railArtillery!=null)s.railArtillery.Restore(rail["artillery"] as JObject);else if(rail["artillery"]!=null)throw new ArgumentException("Unexpected artillery state");}
                s.leaderCharge=(int)(chargeValue??0);s.RestoreState(root);s.RestoreOperatorAutomation(automation as JObject);s.RestoreEffectJournal(effectJournal as JObject);s.RestoreRail(rail as JObject);s.RestoreLocalActiveTiming(timing as JObject);return s;
            }
            catch(Exception e)when(e is JsonException||e is OverflowException||e is InvalidCastException||e is KeyNotFoundException||e is InvalidOperationException){throw new ArgumentException("Invalid local checkpoint.",e);}
        }
        void RestoreState(JObject root)
        {
            var c=root["clock"] as JObject;Fields(c,"step","debt","completed","speed","paused");
            if(Nat(c,"step")!=clock.Capture().StepTicks)throw new ArgumentException();
            clock=FixedStepDebt.Restore(new FixedStepDebt.Snapshot(Nat(c,"step"),Nat(c,"debt"),Nat(c,"completed"),(int)Nat(c,"speed",4),Boolean(c,"paused")));
            random=RunRandom.FromState((uint)Nat(root,"rng",uint.MaxValue));sequence=Nat(root,"sequence");inputSequence=Nat(root,"inputSequence");CompletedWaves=(int)Nat(root,"completedWaves",waves.Length);
            if(!Enum.TryParse(Text(root,"outcome"),out BasicCombatOutcome outcome)||!Enum.IsDefined(typeof(BasicCombatOutcome),outcome)||outcome.ToString()!=Text(root,"outcome"))throw new ArgumentException();Outcome=outcome;
            if((Outcome==BasicCombatOutcome.Victory||railTerminalPending&&CompletedWaves==waves.Length)!=(CompletedWaves==waves.Length))throw new ArgumentException("Invalid terminal wave.");
            interiorFocus=NullableText(root,"interiorFocus");exteriorFocus=NullableText(root,"exteriorFocus");
            var config=allies.Select(x=>x.Config).Concat(waves.SelectMany(x=>x)).Concat(railArtilleryActors.Values.Select(a=>a.Config)).ToDictionary(x=>x.Id,StringComparer.Ordinal);var map=new Dictionary<string,Actor>(StringComparer.Ordinal);
            foreach(var row in Rows(root,"actors",config.Count))
            {
                Fields(row as JObject,"id","vitals","ready","recoveryUntil","downCount","recoverAt","protectedUntil","availableAt","cursor");var id=Text(row,"id");
                if(!config.TryGetValue(id,out var definition)||map.ContainsKey(id))throw new ArgumentException("Unknown/duplicate actor.");var actor=new Actor(definition,0,approvedVitals);actor.Vitals.RestoreLocal(row["vitals"] as JObject,Tick);if(approvedVitals)ValidateEffectiveRecoveryHp(actor,actor.Vitals.MaxHp);
                actor.Ready=Nat(row,"ready");actor.RecoveryUntil=Nat(row,"recoveryUntil");actor.DownCount=(int)Nat(row,"downCount",int.MaxValue);actor.RecoverAt=Nat(row,"recoverAt");actor.ProtectedUntil=Nat(row,"protectedUntil");actor.AvailableAt=Nat(row,"availableAt");actor.TargetCursor=(int)Nat(row,"cursor",int.MaxValue);map.Add(id,actor);
            }
            Actor[] Resolve(JArray array){var ids=array.Select(x=>x.Type==JTokenType.String?(string)x:throw new ArgumentException()).ToArray();if(ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)throw new ArgumentException("Duplicate actor references.");return ids.Select(x=>map[x]).ToArray();}
            var restoredAllies=Resolve(Rows(root,"allies",allies.Count));if(!restoredAllies.Select(x=>x.Config.Id).SequenceEqual(allies.Select(x=>x.Config.Id)))throw new ArgumentException("Ally order mismatch.");
            var restoredEnemies=Resolve(Rows(root,"enemies",config.Count));int restoreWave=railWavePending?CompletedWaves-1:Math.Min(CompletedWaves,waves.Length-1);var artilleryIds=railArtillery==null?Array.Empty<string>():((JArray)railArtillery.Capture()["encounters"]).Where(r=>(string)r["status"]=="Active").Select(r=>(string)r["id"]).ToArray();if(restoreWave<0||!restoredEnemies.Select(x=>x.Config.Id).SequenceEqual(waves[restoreWave].Select(x=>x.Id).Concat(artilleryIds)))throw new ArgumentException("Wave actor mismatch.");
            foreach(string id in railArtilleryActors.Keys.ToArray())if(map.TryGetValue(id,out var artilleryActor))railArtilleryActors[id]=artilleryActor;
            allies.Clear();allies.AddRange(restoredAllies);enemies.Clear();enemies.AddRange(restoredEnemies);
            bool deadTrain=allies.Single(x=>x.Config.Train).Hp==0;if(Outcome==BasicCombatOutcome.Failure&&!deadTrain||deadTrain&&Outcome!=BasicCombatOutcome.Failure&&!(railTerminalPending&&Outcome==BasicCombatOutcome.Running))throw new ArgumentException("Train terminal mismatch.");
            attacks.Clear();var actionSequences=new HashSet<long>();
            foreach(var row in Rows(root,"attacks",config.Count))
            {
                Fields(row as JObject,"source","target","due","sequence");var seq=Nat(row,"sequence",sequence);var due=Nat(row,"due");
                if(seq==0||!actionSequences.Add(seq)||due<=Tick)throw new ArgumentException("Invalid pending attack.");
                attacks.Add(new Attack{Source=map[Text(row,"source")],Target=map[Text(row,"target")],Due=due,Sequence=seq});
            }
            foreach(var skill in skills.Values)skill.Caster=map[skill.Caster.Config.Id];
            var readyRows=Rows(root,"skillReady",skills.Count);var readyKeys=new HashSet<string>(StringComparer.Ordinal);if(readyRows.Count!=skills.Count)throw new ArgumentException();
            foreach(var row in readyRows){Fields(row as JObject,"key","ready");var key=Text(row,"key");if(!readyKeys.Add(key)||!skills.ContainsKey(key))throw new ArgumentException();skills[key].Ready=Nat(row,"ready");}
            commands.Clear();var orders=new HashSet<long>();
            var allowed=new[]{"QUEUED","CASTING","APPLIED","EXPIRED","CANCELLED_DOWN","CANCELLED_TARGET","REJECTED_TERMINAL","REJECTED_DOWN","REJECTED_ULTIMATE","REJECTED_BUSY","REJECTED_COOLDOWN","REJECTED_TARGET"};
            foreach(var row in Rows(root,"commands"))
            {
                Fields(row as JObject,"id","status","skill","order","sequence","expiry","due","targets");var id=Text(row,"id");var order=Nat(row,"order",inputSequence);var status=Text(row,"status");if(!allowed.Contains(status)||commands.ContainsKey(id)||order==0||!orders.Add(order))throw new ArgumentException();
                var cmd=new Command{Id=id,Status=status,Skill=skills[Text(row,"skill")],InputOrder=order,Seq=Nat(row,"sequence",sequence),Expiry=Nat(row,"expiry"),Due=Nat(row,"due"),Targets=row["targets"].Type==JTokenType.Null?null:Resolve(Rows(row,"targets",config.Count))};
                if(status=="CASTING"&&(cmd.Seq==0||!actionSequences.Add(cmd.Seq)||cmd.Due<=Tick||cmd.Targets==null||cmd.Targets.Length==0||cmd.Skill.Caster.Hp==0))throw new ArgumentException("Invalid pending cast.");commands.Add(id,cmd);
            }
            if(commands.Values.Where(x=>x.Status=="QUEUED"||x.Status=="CASTING").GroupBy(x=>x.Skill.Caster).Any(x=>x.Count()>1))throw new ArgumentException("Multiple actor commands.");
            statuses.Clear();var statusKeys=new HashSet<string>();
            foreach(var row in Rows(root,"statuses"))
            {
                Fields(row as JObject,"source","target","ability","type","index","value","expiry","sequence");var status=new Status{Source=map[Text(row,"source")],Target=map[Text(row,"target")],Ability=Text(row,"ability"),Type=Text(row,"type"),Index=(int)Nat(row,"index",int.MaxValue),Value=(int)Nat(row,"value",10000),Expiry=Nat(row,"expiry"),Sequence=Nat(row,"sequence",sequence)};
                if(!new[]{"mark","taunt","train_damage_buff"}.Contains(status.Type)||!statusKeys.Add(StableJson(new JArray(status.Source.Config.Id,status.Target.Config.Id,status.Ability,status.Index,status.Type))))throw new ArgumentException();statuses.Add(status);
            }
            events.Clear();foreach(var row in Rows(root,"events"))
            {
                Fields(row as JObject,"Tick","Source","Target","HpLoss","RemainingHp","Absorbed");events.Add(new BasicCombatHit(Nat(row,"Tick",Tick),Text(row,"Source"),Text(row,"Target"),(int)Nat(row,"HpLoss",int.MaxValue),(int)Nat(row,"RemainingHp",int.MaxValue),(int)Nat(row,"Absorbed",int.MaxValue)));
            }
        }
    }
}
