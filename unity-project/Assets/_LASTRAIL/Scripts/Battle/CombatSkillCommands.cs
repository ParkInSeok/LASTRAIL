using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    public sealed partial class BasicCombatSimulation
    {
        internal sealed class DueAction {internal long Due,Sequence;internal Action Execute;internal DueAction(long due,long seq,Action action){Due=due;Sequence=seq;Execute=action;}}
        sealed class Skill {internal Actor Caster;internal JObject Data;internal long Ready;}
        sealed class Command {internal string Id,Status;internal Skill Skill;internal long InputOrder,Seq,Expiry,Due;internal Actor[] Targets;}
        sealed class Status {internal Actor Source,Target;internal string Ability,Type;internal int Index,Value;internal long Expiry,Sequence;}
        readonly Dictionary<string,Skill> skills=new Dictionary<string,Skill>(StringComparer.Ordinal);
        readonly Dictionary<string,Command> commands=new Dictionary<string,Command>(StringComparer.Ordinal);
        readonly Dictionary<string,JObject> passives=new Dictionary<string,JObject>(StringComparer.Ordinal);
        readonly List<Status> statuses=new List<Status>();
        string selectedUltimate;
        string[] manualDisplayOrder;
        readonly Dictionary<string,string> actorVisualIds=new Dictionary<string,string>();
        long inputSequence;
        int leaderChargePerHit,leaderCharge;
        public void ConfigureLocalLeaderCharge(int perResolvedBasicHit){if(Tick!=0||skills.Count==0||leaderChargePerHit!=0||individualUltimates||perResolvedBasicHit<1||perResolvedBasicHit>100)throw new ArgumentException();leaderChargePerHit=perResolvedBasicHit;}
        int queueExpiryMs;
        int[] operatorRecoveryMs;
        int operatorRecoveryBps,operatorProtectionMs;
        int skillStepMs,repairCap,outgoingCap,magnitudeCap,markCap,armorCap,squadBonus,trainBonus,shieldBonus,precisionBonus;
        static readonly string[] Targets={"ally_lowest_hp","all_allies","enemy_focus_any","enemies_front_2","train","self","EXTERIOR_ENEMY_FOCUS"};
        static int Number(JToken obj,string name,int minimum=0,int maximum=int.MaxValue)
        {
            var token=obj?[name];if(token?.Type!=JTokenType.Integer)throw new ArgumentException("Missing integer "+name);
            var n=(long)token;if(n<minimum||n>maximum)throw new ArgumentException("Out of range "+name);return (int)n;
        }
        static string Text(JToken obj,string name)
        {if(obj?[name]?.Type!=JTokenType.String||string.IsNullOrEmpty((string)obj[name]))throw new ArgumentException("Missing string "+name);return (string)obj[name];}
        static void Fields(JObject obj,params string[] expected)
        {if(obj==null||!obj.Properties().Select(x=>x.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(expected.OrderBy(x=>x,StringComparer.Ordinal)))throw new ArgumentException("Unexpected skill fields.");}
        long ToTicks(int ms)=>((long)ms+skillStepMs-1)/skillStepMs;
        public void ConfigureSkills(JObject template,string ultimateOperatorId,int stepMs)=>ConfigureSkills(template,ultimateOperatorId,stepMs,false);
        void ConfigureSkills(JObject template,string ultimateOperatorId,int stepMs,bool allUltimates)
        {
            if(Tick!=0||skills.Count!=0||stepMs<=0||clock.Capture().StepTicks!=(long)stepMs*10000)throw new ArgumentException("Configure once before time advances with matching step.");
            if(!(template?["operators"] is JArray ops)||ops.Count!=5||(!allUltimates&&!ops.Any(x=>(string)x["operatorId"]==ultimateOperatorId)))throw new ArgumentException("Explicit selected ultimate required.");
            var staged=new Dictionary<string,Skill>(StringComparer.Ordinal);var stagedPassives=new Dictionary<string,JObject>();
            void Add(Actor caster,JObject raw,string expectedKind)
            {
                Fields(raw,"id","kind","firstReadyMs","cooldownMs","windupMs","recoveryMs","target","effects");
                var data=(JObject)raw.DeepClone();var kind=Text(data,"kind");
                if(kind!=expectedKind||caster==null||caster.Config.Enemy||(caster.Config.Train?kind!="TRAIN_MANUAL":kind!="ACTIVE"&&kind!="ULTIMATE")||!Targets.Contains(Text(data,"target")))throw new ArgumentException("Invalid caster/target/ability kind.");
                Number(data,"firstReadyMs");Number(data,"cooldownMs",1);Number(data,"windupMs");Number(data,"recoveryMs");
                if(!(data["effects"] is JArray effects)||effects.Count==0)throw new ArgumentException("Effects required.");
                foreach(var token in effects)
                {
                    if(!(token is JObject effect))throw new ArgumentException();var type=Text(effect,"type");
                    var names=new List<string>{"type","targetOverride"};
                    if(new[]{"damage","heal","repair","shield","train_shield"}.Contains(type))
                    {
                        names.AddRange(new[]{"basis","coefficient"});if(!new[]{"CASTER_ATK","CASTER_MAX_HP","TRAIN_ATK","TRAIN_MAX_HP"}.Contains(Text(effect,"basis")))throw new ArgumentException();
                        var ratio=effect["coefficient"] as JObject;Fields(ratio,"numerator","denominator");var a=Number(ratio,"numerator",1);var b=Number(ratio,"denominator",1);if(BigInteger.GreatestCommonDivisor(a,b)!=1)throw new ArgumentException("Coefficient must be reduced.");
                    }
                    else if(type!="taunt"&&type!="interrupt"&&type!="mark"&&type!="train_damage_buff")throw new ArgumentException("Unsupported effect "+type);
                    if(new[]{"shield","train_shield","taunt","mark","train_damage_buff"}.Contains(type)){names.Add("durationMs");Number(effect,"durationMs",1);}
                    if(type=="mark"||type=="train_damage_buff"){names.Add("valueBps");Number(effect,"valueBps",0,10000);}
                    Fields(effect,names.ToArray());if(effect["targetOverride"].Type!=JTokenType.Null&&!Targets.Contains((string)effect["targetOverride"]))throw new ArgumentException();
                }
                var key=caster.Config.Id+"/"+Text(data,"id");if(staged.ContainsKey(key))throw new ArgumentException("Duplicate ability.");
                staged.Add(key,new Skill{Caster=caster,Data=data,Ready=((long)Number(data,"firstReadyMs")+stepMs-1)/stepMs});
            }
            foreach(var op in ops)
            {
                var id=Text(op,"operatorId");var caster=allies.SingleOrDefault(x=>x.Config.Id==id&&!x.Config.Train);Add(caster,op["active"] as JObject,"ACTIVE");Add(caster,op["ultimate"] as JObject,"ULTIMATE");
                var passive=op["passive"] as JObject;var type=Text(passive,"type");if(!new[]{"armor_when_low_hp","damage_to_marked","healing_done","repair_done","damage_to_saboteur"}.Contains(type))throw new ArgumentException("Unsupported passive.");
                Number(passive,"valueBps",0,10000);if(type=="armor_when_low_hp")Number(passive,"belowHpBps",0,10000);
                stagedPassives.Add(id,(JObject)passive.DeepClone());
            }
            if(!(template["train"]?["manual"] is JArray manual)||manual.Count!=3)throw new ArgumentException("Three train manual abilities required.");
            foreach(var a in manual)Add(allies.Single(x=>x.Config.Train),a as JObject,"TRAIN_MANUAL");
            var execution=template["execution"];var queue=execution?["queue"];
            if(Number(execution,"stepMs",1)!=stepMs||Number(queue,"capacity",1)!=1||Text(queue,"secondCommand")!="REJECT"||Text(queue,"cooldownAt")!="CAST_ACCEPTANCE")throw new ArgumentException("Unsupported command queue policy.");
            var expiry=Number(queue,"expiryMs",1);
            var rules=template["rules"];var caps=new[]{"trainRepairCapPerEffectMaxHpBps","outgoingBuffCapBps","magnitudeBonusCapBps","markCapBps","armorCapBps"}.Select(x=>Number(rules,x,0,10000)).ToArray();
            if(!(rules["operatorDownMs"] is JArray delays)||delays.Count!=3||delays.Any(x=>x.Type!=JTokenType.Integer||(long)x<=0||(long)x>int.MaxValue))throw new ArgumentException("Three explicit operator recovery delays required.");
            var recoveryBps=Number(rules,"operatorRecoveryHpBps",1,10000);var protectionMs=Number(rules,"operatorRecoveryProtectionMs",0);
            if(allies.Where(x=>!x.Config.Train&&!x.Device).Any(x=>(long)x.Vitals.MaxHp*recoveryBps/10000<=0))throw new ArgumentException("Recovery HP must be positive.");
            var package=template["package"];if(!(package?["augments"] is JArray aug)||aug.Count!=3)throw new ArgumentException("Package required.");
            int Bonus(string filter){var matches=aug.Concat(new[]{package["tactic"]}).Where(x=>(string)x?["eventFilter"]==filter).ToArray();if(matches.Length!=1)throw new ArgumentException("Exact package filter required.");return Number(matches[0],"valueBps",0,10000);}
            var bonuses=new[]{Bonus("OPERATOR_DAMAGE"),Bonus("LOCOMOTIVE_GUN_DAMAGE_INCLUDING_SUPPORT_EXCLUDING_COMBAT"),Bonus("SHIELD_ISSUED"),Bonus("SINGLE_TARGET_OPERATOR_ACTIVE_DIRECT_DAMAGE")};
            manualDisplayOrder=manual.Select(x=>Text(x,"id")).ToArray();
            skillStepMs=stepMs;queueExpiryMs=expiry;selectedUltimate=ultimateOperatorId;repairCap=caps[0];outgoingCap=caps[1];magnitudeCap=caps[2];markCap=caps[3];armorCap=caps[4];squadBonus=bonuses[0];trainBonus=bonuses[1];shieldBonus=bonuses[2];precisionBonus=bonuses[3];
            operatorRecoveryMs=delays.Select(x=>(int)x).ToArray();operatorRecoveryBps=recoveryBps;operatorProtectionMs=protectionMs;
            foreach(var pair in staged)skills.Add(pair.Key,pair.Value);foreach(var pair in stagedPassives)passives.Add(pair.Key,pair.Value);
        }
        public string CommandStatus(string id)=>commands.TryGetValue(id,out var cmd)?cmd.Status:"UNKNOWN";
        public string RequestSkill(string commandId,string actorId,string abilityId)
        {
            if(ordinaryFactory!=null)return RequestOrdinarySkill(commandId,actorId,abilityId);
            if(string.IsNullOrWhiteSpace(commandId)||string.IsNullOrWhiteSpace(actorId)||string.IsNullOrWhiteSpace(abilityId))throw new ArgumentException();
            if(commands.TryGetValue(commandId,out var old))
            {if(old.Skill.Caster.Config.Id!=actorId||Text(old.Skill.Data,"id")!=abilityId)throw new InvalidOperationException("Command identity conflict.");return old.Status;}
            if(!skills.TryGetValue(actorId+"/"+abilityId,out var skill))return "REJECTED_ABILITY";
            var status=Outcome!=BasicCombatOutcome.Running?"REJECTED_TERMINAL":skill.Caster.Hp==0?"REJECTED_DOWN":Text(skill.Data,"kind")=="ULTIMATE"&&!individualUltimates&&actorId!=selectedUltimate?"REJECTED_ULTIMATE":commands.Values.Any(x=>x.Skill.Caster==skill.Caster&&(x.Status=="QUEUED"||x.Status=="CASTING"))?"REJECTED_BUSY":"QUEUED";
            var cmd=new Command{Id=commandId,Skill=skill,Status=status,InputOrder=checked(++inputSequence),Expiry=checked(Tick+ToTicks(queueExpiryMs))};commands.Add(commandId,cmd);return status;
        }
        bool SkillBusy(Actor actor)=>commands.Values.Any(x=>x.Skill.Caster==actor&&(x.Status=="CASTING"||x.Status=="QUEUED"))||actor.RecoveryUntil>Tick;
        bool HasPendingSkills()=>commands.Values.Any(x=>x.Status=="CASTING"||x.Status=="QUEUED");
        Actor[] SkillTargets(Actor caster,string target)
        {
            var living=allies.Where(x=>x.Hp>0&&!x.Config.Train&&!x.Device);
            switch(target)
            {
                case "train":return allies.Where(x=>x.Config.Train&&x.Hp>0).ToArray();
                case "self":return caster.Hp>0?new[]{caster}:Array.Empty<Actor>();
                case "all_allies":return living.OrderBy(x=>x.Config.Slot).ToArray();
                case "ally_lowest_hp":return living.OrderBy(x=>x,new HpComparer()).Take(1).ToArray();
                default:
                    var candidates=enemies.Where(x=>x.Hp>0&&x.AvailableAt<=Tick&&CanDirectlyTarget(caster,x)&&(target=="EXTERIOR_ENEMY_FOCUS"?x.Config.Exterior:!x.Config.Exterior)).OrderBy(x=>x.Config.Slot).ThenBy(x=>x.Config.Id,StringComparer.Ordinal).ToArray();
                    if(target=="enemies_front_2")return candidates.Take(2).ToArray();
                    var focus=target=="EXTERIOR_ENEMY_FOCUS"?exteriorFocus:interiorFocus;
                    var chosen=candidates.FirstOrDefault(x=>x.Config.Id==focus)??candidates.FirstOrDefault();return chosen==null?Array.Empty<Actor>():new[]{chosen};
            }
        }
        sealed class HpComparer:IComparer<Actor>{public int Compare(Actor a,Actor b){var n=((long)a.Hp*b.Vitals.MaxHp).CompareTo((long)b.Hp*a.Vitals.MaxHp);return n!=0?n:a.Config.Slot!=b.Config.Slot?a.Config.Slot.CompareTo(b.Config.Slot):StringComparer.Ordinal.Compare(a.Config.Id,b.Config.Id);}}
        void ProcessSkillCommands()
        {
            statuses.RemoveAll(x=>x.Expiry<=Tick||(x.Type=="taunt"&&x.Source.Hp==0));
            foreach(var cmd in commands.Values.OrderBy(x=>x.InputOrder))
            {
                if(cmd.Status!="QUEUED"&&cmd.Status!="CASTING")continue;
                if(cmd.Skill.Caster.Hp==0){cmd.Status="CANCELLED_DOWN";continue;}
                if(cmd.Status=="CASTING")continue;
                if(Tick>=cmd.Expiry){cmd.Status="EXPIRED";continue;}
                bool charged=ChargedSkill(cmd.Skill);
                if(charged?SkillCharge(cmd.Skill)<100:cmd.Skill.Ready>Tick){cmd.Status="REJECTED_COOLDOWN";continue;}
                if(attacks.Any(x=>x.Source==cmd.Skill.Caster)||cmd.Skill.Caster.RecoveryUntil>Tick)continue;
                var targets=SkillTargets(cmd.Skill.Caster,Text(cmd.Skill.Data,"target"));if(targets.Length==0){cmd.Status="REJECTED_TARGET";continue;}
                AcceptSkill(cmd,targets);
            }
            if(individualUltimates){if(ultimateAuto)AutomaticSkills("ULTIMATE");if(generalSkillAuto)AutomaticSkills("ACTIVE");}
        }
        IEnumerable<DueAction> DueSkills()=>commands.Values.Where(x=>x.Status=="CASTING"&&x.Due<=Tick).Select(x=>new DueAction(x.Due,x.Seq,()=>ExecuteSkill(x)));
        int Passive(Actor actor,string type)=>passives.TryGetValue(actor.Config.Id,out var p)&&(string)p["type"]==type?Number(p,"valueBps"):0;
        int Strongest(Actor actor,string type)=>statuses.Where(x=>x.Target==actor&&x.Type==type&&x.Expiry>Tick).Select(x=>x.Value).DefaultIfEmpty(0).Max();
        int SkillAdjustedDamage(Actor source,Actor target,int basis,int numerator,int denominator,bool precision)
        {
            var marked=Strongest(target,"mark");var bonus=skills.Count==0?0:source.Config.Train||source.Config.Timing?.Device=="Gun"?trainBonus:source.Config.Enemy||source.Device?0:squadBonus;
            if(!source.Config.Enemy)bonus+=source.Config.Train||source.Config.Timing?.Device=="Gun"?RailBonus("TRAIN_DAMAGE"):source.Device?0:RailBonus("SQUAD_DAMAGE");
            if(precision)bonus+=precisionBonus;if(marked>0)bonus+=Passive(source,"damage_to_marked");
            if(source.Config.Train)bonus+=statuses.Where(x=>x.Target==source&&x.Type=="train_damage_buff"&&x.Expiry>Tick).Sum(x=>x.Value);
            var armor=target.Config.ArmorBps;
            if(passives.TryGetValue(target.Config.Id,out var p)&&(string)p["type"]=="armor_when_low_hp"&&(long)target.Hp*10000<(long)target.Vitals.MaxHp*Number(p,"belowHpBps"))armor+=Number(p,"valueBps");
            if(skills.Count>0){bonus=Math.Min(outgoingCap,bonus);marked=Math.Min(markCap,marked);armor=Math.Min(armorCap,armor);}
            var n=(BigInteger)basis*numerator*(10000+bonus)*(10000+marked)*(10000-armor);return checked((int)(n/((BigInteger)denominator*10000*10000*10000)));
        }
        void ExecuteSkill(Command cmd)
        {
            if(cmd.Status!="CASTING")return;var caster=cmd.Skill.Caster;
            if(caster.Hp==0){cmd.Status="CANCELLED_DOWN";return;}
            if(cmd.Targets.All(x=>x.Hp==0))
            {var targets=SkillTargets(caster,Text(cmd.Skill.Data,"target"));if(targets.Length==0){cmd.Status="CANCELLED_TARGET";return;}long due=checked(Tick+ToTicks(Number(cmd.Skill.Data,"windupMs")));var timing=localActiveTiming;if(timing!=null&&timing.Binds(caster.Config.Id,Text(cmd.Skill.Data,"kind")))timing=timing.Reschedule(cmd.Id,Tick,due);cmd.Targets=targets;cmd.Due=due;localActiveTiming=timing;if(cmd.Due>Tick)return;}
            var effects=(JArray)cmd.Skill.Data["effects"];
            // Freeze recipient lists at commit, before applying any authored effect.
            var recipients=effects.Select(e=>e["targetOverride"].Type==JTokenType.Null?cmd.Targets:SkillTargets(caster,(string)e["targetOverride"])).ToArray();
            for(var i=0;i<effects.Count;i++)foreach(var target in recipients[i].Where(x=>x.Hp>0))ApplySkillEffect(cmd,(JObject)effects[i],i,target);
            caster.RecoveryUntil=checked(Tick+ToTicks(Number(cmd.Skill.Data,"recoveryMs")));caster.Ready=Math.Max(caster.Ready,caster.RecoveryUntil);cmd.Status="APPLIED";
        }
        void ApplySkillEffect(Command cmd,JObject effect,int index,Actor target)
        {
            var source=cmd.Skill.Caster;var type=Text(effect,"type");var id=Text(cmd.Skill.Data,"id");
            if(type=="interrupt")
            {var cancelled=new JArray();foreach(var a in attacks.Where(x=>x.Source==target&&x.Due>Tick).ToArray()){target.Ready=Math.Max(target.Ready,checked(a.Due+target.Config.PeriodTicks));attacks.Remove(a);cancelled.Add(a.Sequence);}RecordEffect(cmd,index,target,"INTERRUPT",new JObject{["cancelledActionIds"]=cancelled});return;}
            if(type=="mark"||type=="taunt"||type=="train_damage_buff")
            {
                var value=type=="taunt"?0:Number(effect,"valueBps");var prior=statuses.SingleOrDefault(x=>x.Source==source&&x.Target==target&&x.Ability==id&&x.Index==index&&x.Type==type);
                long oldExpiry=prior?.Expiry??0;long expiry=checked(Tick+ToTicks(Number(effect,"durationMs")));bool accepted=prior==null||value>=prior.Value&&(value!=prior.Value||expiry!=prior.Expiry);
                RecordEffect(cmd,index,target,!accepted?"STATUS_IGNORED":prior==null?"STATUS_ADD":"STATUS_REFRESH",new JObject{["category"]=type,["magnitude"]=value,["previousExpiry"]=oldExpiry,["expiry"]=accepted?expiry:oldExpiry,["accepted"]=accepted});
                if(prior!=null&&value<prior.Value)return;if(prior!=null)statuses.Remove(prior);
                statuses.Add(new Status{Source=source,Target=target,Ability=id,Index=index,Type=type,Value=value,Sequence=cmd.Seq,Expiry=checked(Tick+ToTicks(Number(effect,"durationMs")))});return;
            }
            var train=allies.Single(x=>x.Config.Train);var basis=Text(effect,"basis");var amount=basis=="CASTER_ATK"?source.Config.Attack:basis=="CASTER_MAX_HP"?source.Vitals.MaxHp:basis=="TRAIN_ATK"?train.Config.Attack:train.Vitals.MaxHp;
            var numerator=Number(effect["coefficient"],"numerator",1);var denominator=Number(effect["coefficient"],"denominator",1);
            if(type=="damage")
            {
                var damage=SkillAdjustedDamage(source,target,amount,numerator,denominator,!source.Config.Train&&Text(cmd.Skill.Data,"kind")=="ACTIVE"&&Text(cmd.Skill.Data,"target")!="all_allies");
                var hit=target.Vitals.Damage(damage,Tick,target.ProtectedUntil>Tick);if(hit.HpLoss>0&&target.Hp==0)OperatorDown(target);events.Add(new BasicCombatHit(Tick,source.Config.Id,target.Config.Id,hit.HpLoss,target.Hp,hit.Absorbed));RecordEffect(cmd,index,target,"DAMAGE",new JObject{["hpLoss"]=hit.HpLoss,["absorbed"]=hit.Absorbed});return;
            }
            var bonus=type=="heal"?Passive(source,"healing_done"):type=="repair"?Passive(source,"repair_done"):shieldBonus+RailBonus("SHIELD_MAGNITUDE");
            var magnitude=checked((int)((BigInteger)amount*numerator*(10000+Math.Min(magnitudeCap,bonus))/((BigInteger)denominator*10000)));
            if(type=="heal")RecordEffect(cmd,index,target,"HEAL",new JObject{["actualHpGain"]=target.Vitals.RestoreHp(magnitude,10000)});
            else if(type=="repair"){if(!target.Config.Train)throw new InvalidOperationException("Invalid repair target.");RecordEffect(cmd,index,target,"REPAIR",new JObject{["actualHpGain"]=target.Vitals.RestoreHp(magnitude,repairCap)});}
            else {
                var old=((JArray)target.Vitals.CaptureLocal()["layers"]).SingleOrDefault(l=>l["transferred"]==null&&(string)l["entityId"]==source.Config.Id&&(string)l["abilityId"]==id&&(int)l["effectIndex"]==index&&(long)l["expiry"]>Tick&&(int)l["remaining"]>0);
                int previous=old==null?0:(int)old["remaining"];long oldExpiry=old==null?0:(long)old["expiry"];long expiry=checked(Tick+ToTicks(Number(effect,"durationMs")));
                int issued=target.Vitals.Shield(new CombatShieldSource(source.Config.Id,id,index),magnitude,expiry,Tick);int current=issued>0?issued:previous;
                RecordEffect(cmd,index,target,"SHIELD_ISSUE",new JObject{["previousRemaining"]=previous,["resultingRemaining"]=current,["addedAmount"]=Math.Max(0,current-previous),["previousExpiry"]=oldExpiry,["expiry"]=issued>0?expiry:oldExpiry,["outcome"]=current>previous?"ADD":issued>0&&expiry!=oldExpiry?"REFRESH":"UNCHANGED"});
            }
        }
        public int OperatorDownCount(string actorId)=>allies.Single(x=>x.Config.Id==actorId&&!x.Config.Train).DownCount;
        public bool IsRecoveryProtected(string actorId)=>allies.Any(x=>x.Config.Id==actorId&&x.Hp>0&&x.ProtectedUntil>Tick);
        void OperatorDown(Actor actor)
        {
            if(actor.Config.Enemy||actor.Config.Train||actor.Device||operatorRecoveryMs==null)return;
            actor.DownCount=checked(actor.DownCount+1);
            actor.RecoverAt=checked(Tick+ToTicks(operatorRecoveryMs[Math.Min(actor.DownCount,3)-1]));
            foreach(var cmd in commands.Values.Where(x=>x.Skill.Caster==actor&&(x.Status=="QUEUED"||x.Status=="CASTING")))cmd.Status="CANCELLED_DOWN";
            // Future windups are uncommitted; impacts already due this tick retain order.
            attacks.RemoveAll(x=>x.Source==actor&&x.Due>Tick);
        }
        void RecoverOperators()
        {
            if(operatorRecoveryMs==null)return;
            foreach(var actor in allies.Where(x=>!x.Config.Train&&!x.Device&&x.Hp==0&&x.DownCount>0&&x.RecoverAt<=Tick))
            {
                actor.Vitals.RecoverFromDown((int)((long)actor.Vitals.MaxHp*operatorRecoveryBps/10000));
                actor.ProtectedUntil=checked(Tick+ToTicks(operatorProtectionMs));
            }
        }
    }
}
