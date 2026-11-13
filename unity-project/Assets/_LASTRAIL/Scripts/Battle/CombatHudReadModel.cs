using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    public sealed partial class BasicCombatSimulation
    {
        // Read-only projection: do not expire/mutate domain layers during rendering.
        static int KyleGuardAmount(CombatVitals vitals,long tick)=>((JArray)vitals.CaptureLocal()["layers"])
            .Where(l=>(string)l["abilityId"]=="op_kyle.active"&&(long)l["expiry"]>tick).Sum(l=>(int)l["remaining"]);
        public JObject ReadHudState()
        {
            if(ordinaryFactory!=null)return ReadOrdinaryHud();
            if(seraCommands!=null)throw new InvalidOperationException("Sera validation owner has no HUD contract.");
            var time=clock.Capture();
            JObject SkillView(Skill skill)
            {
                var caster=skill.Caster;var charged=ChargedSkill(skill);var remaining=charged?0:Math.Max(0,skill.Ready-Tick);
                var busy=commands.Values.FirstOrDefault(c=>c.Skill.Caster==caster&&(c.Status=="QUEUED"||c.Status=="CASTING"));
                var charge=charged?SkillCharge(skill):0;
                var status=busy?.Status??(caster.Hp==0?"DOWN":charged&&charge!=100?"CHARGING":remaining>0?"COOLDOWN":
                    Outcome!=BasicCombatOutcome.Running?"TERMINAL":time.IsPaused?"PAUSED":caster.RecoveryUntil>Tick?"RECOVERY":
                    attacks.Any(a=>a.Source==caster)?"ATTACKING":individualUltimates&&SkillTargets(caster,Text(skill.Data,"target")).Length==0?"NO_TARGET":"READY");
                return new JObject{["shield"]=((JArray)caster.Vitals.CaptureLocal()["layers"]).Where(l=>(long)l["expiry"]>Tick).Sum(l=>(int)l["remaining"]),["actor"]=caster.Config.Id,["ability"]=Text(skill.Data,"id"),["hp"]=caster.Hp,["maxHp"]=caster.Vitals.MaxHp,["chargeMode"]=charged,["charge"]=charge,["cooldownMs"]=remaining*skillStepMs,["cooldownTotalMs"]=Number(skill.Data,"cooldownMs"),["status"]=status,["enabled"]=status=="READY"};
            }
            var train=allies.Single(x=>x.Config.Train);
            var hud=new JObject{["frontSlots"]=JArray.FromObject(frontSlots),["motionSeconds"]=(time.CompletedSteps*(double)time.StepTicks+Math.Min(time.StepTicks,time.DebtHalfTicks/2d))/10000000d,["effectResults"]=effectResults.DeepClone(),["actors"]=new JArray(allies.Concat(enemies).Select(a=>new JObject{["id"]=a.Config.Id,["visual"]=actorVisualIds.TryGetValue(a.Config.Id,out var visual)?visual:a.Config.Id,["enemy"]=a.Config.Enemy,["train"]=a.Config.Train,["device"]=a.Device,["deviceType"]=a.Config.Timing?.Device,["exterior"]=a.Config.Exterior,["slot"]=a.Config.Slot,["shield"]=((JArray)a.Vitals.CaptureLocal()["layers"]).Where(l=>(long)l["expiry"]>Tick).Sum(l=>(int)l["remaining"]),["kyleGuardShield"]=KyleGuardAmount(a.Vitals,Tick),["hp"]=a.Hp,["maxHp"]=a.Vitals.MaxHp,["availableAt"]=a.AvailableAt,["recoverAt"]=a.RecoverAt,["protected"]=a.ProtectedUntil>Tick,["attackDue"]=attacks.Where(x=>x.Source==a).Select(x=>x.Due).DefaultIfEmpty(-1).First(),["attackWindup"]=a.Config.WindupTicks,["attackTarget"]=attacks.Where(x=>x.Source==a).Select(x=>x.Target.Config.Id).FirstOrDefault(),["attacking"]=attacks.Any(x=>x.Source==a),["casting"]=commands.Values.Any(x=>x.Skill.Caster==a&&x.Status=="CASTING")})),["hits"]=JArray.FromObject(events),["casts"]=new JArray(commands.Values.Where(c=>c.Seq>0).Select(c=>new JObject{["id"]=c.Id,["actor"]=c.Skill.Caster.Config.Id,["kind"]=Text(c.Skill.Data,"kind"),["ability"]=Text(c.Skill.Data,"id"),["windupTicks"]=ToTicks(Number(c.Skill.Data,"windupMs")),["status"]=c.Status,["due"]=c.Due,["targets"]=new JArray((c.Targets??System.Array.Empty<Actor>()).Select(a=>a.Config.Id))})),["tick"]=Tick,["wave"]=Math.Min(waves.Length,CompletedWaves+1),["totalWaves"]=waves.Length,["outcome"]=Outcome.ToString(),["paused"]=time.IsPaused,["speedHalfUnits"]=time.SpeedHalfUnits,["trainHp"]=train.Hp,["trainMaxHp"]=train.Vitals.MaxHp,["skills"]=new JArray(skills.Values.Where(s=>Text(s.Data,"kind")=="ACTIVE").OrderBy(s=>s.Caster.Config.Slot).Select(SkillView)),["ultimate"]=skills.Values.Where(s=>Text(s.Data,"kind")=="ULTIMATE"&&s.Caster.Config.Id==selectedUltimate).Select(SkillView).FirstOrDefault(),["manuals"]=new JArray(skills.Values.Where(s=>Text(s.Data,"kind")=="TRAIN_MANUAL").OrderBy(s=>Array.IndexOf(manualDisplayOrder,Text(s.Data,"id"))).Select(SkillView))};
            hud["stepTimeSpanTicks"]=time.StepTicks;
            if(individualUltimates){hud["ultimates"]=new JArray(skills.Values.Where(s=>Text(s.Data,"kind")=="ULTIMATE").OrderBy(s=>s.Caster.Config.Slot).Select(SkillView));hud["ultimateAuto"]=ultimateAuto;hud["generalAuto"]=generalSkillAuto;}
            if(railJourney!=null)hud["railJourney"]=railJourney.ReadHud();
            if(railArtillery!=null){hud["railArtillery"]=railArtillery.Capture();var artConfig=railArtillery.Configuration;for(int i=0;i<((JArray)hud["railArtillery"]["encounters"]).Count;i++)hud["railArtillery"]["encounters"][i]["exposureDistanceMm"]=artConfig["encounters"][i]["exposureDistanceMm"].DeepClone();foreach(var actor in (JArray)hud["actors"])if(railArtilleryActors.ContainsKey((string)actor["id"]))actor["artillery"]=true;}
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
            WriteDComparisonPlacement(hud);
#endif
            return hud;
        }
    }
}
