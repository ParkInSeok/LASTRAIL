using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    // Current installed LOCAL executors only; not a capability claim for unsupported
    // sentries, effects or server safe events. Unknown/missing executors fail closed.
    public sealed class BResidualHazardObservation
    {
        readonly JObject original;
        public bool Known => !((JArray)original["unknown"]).Any();
        public bool Settled => Known && !((JArray)original["owners"]).Any(o => (bool)o["pending"]);
        public JObject Capture() => (JObject)original.DeepClone();
        public void RequireSettled() { if (!Known) throw new InvalidOperationException("b_residual_hazard_source_unknown"); if (!Settled) throw new InvalidOperationException("b_residual_hazard_pending"); }
        BResidualHazardObservation(JObject p) { original = p; }
        internal static BResidualHazardObservation ReadOwned(JObject native)
        {
            var facts = new JArray(); var unknown = new JArray(); long tick = (long)native["state"]["direct"]["clock"]["completedSteps"];
            var allowed = new HashSet<string>(new[]{"version","bindingHash","capacity","sequence","commands","state","results","enemyTiming","sourcePacketHash","active","activeOwner","lifecycle","waveSourceHash","boardingPolicyVersion","ordinaryRuntimeGeometrySupported","spatialSource","boarding","waveEntry","boardingEventsSourceHash","receivedDefinitionHash","receivedBoardingPolicyVersion","receivedEventPolicyVersion","boardingEventsSource","gunnerIntercept","namedInterceptionAbilityBound","travel","originalEvents"}, StringComparer.Ordinal);
            foreach(var p in native.Properties()) if(!allowed.Contains(p.Name)) unknown.Add("UNRECOGNIZED_OWNER:"+p.Name);
            void Fact(string id, JToken state, bool pending) { facts.Add(new JObject { ["owner"] = id, ["pending"] = pending, ["snapshot"] = state.DeepClone() }); }
            var wave = native["waveEntry"] as JObject;
            if(wave == null) unknown.Add("WAVE_ENTRY_OWNER_MISSING");
            else { if((bool)wave["unknownProjectile"]) unknown.Add("UNBOUND_PROJECTILE_LAUNCH"); Fact("WAVE_ENTRY",wave,!(bool)wave["finished"]); }
            var enemy = native["enemyTiming"] as JObject;
            if(enemy == null) unknown.Add("ENEMY_ACTION_OWNER_MISSING");
            else {
                var actors = (JArray)enemy["actors"];
                foreach(var a in actors) if(!new[]{"IDLE","WINDUP","RECOVERY","OCCUPIED","TERMINAL"}.Contains((string)a["phase"])) unknown.Add("ENEMY_ACTION_PHASE_UNKNOWN");
                Fact("ENEMY_ACTION",enemy,actors.Any(a=>(string)a["phase"]=="WINDUP" || (string)a["phase"]=="RECOVERY" || (string)a["phase"]=="OCCUPIED" && (long)a["occupiedUntil"]>tick));
                var admission = enemy["admission"] as JObject;
                if(admission == null) unknown.Add("ENEMY_ADMISSION_OWNER_MISSING");
                else Fact("ENEMY_ADMISSION",admission,((JArray)admission["leases"]).Any(l=>(bool)l["actionPending"] || (long)l["hazardUntil"]>tick));
            }
            var gunner = native["gunnerIntercept"] as JObject;
            if(gunner == null) unknown.Add("GUNNER_PROJECTILE_AND_INTERCEPTION_OWNER_MISSING");
            else {
                var shots = (JArray)gunner["projectiles"];
                foreach(var shot in shots) if(!new[]{"FLYING","HIT","MISS","INTERCEPTED"}.Contains((string)shot["terminal"])) unknown.Add("GUNNER_TERMINAL_UNKNOWN");
                Fact("GUNNER_PROJECTILE",gunner,shots.Any(s=>(string)s["terminal"]=="FLYING"));
                var intercept = gunner["interception"] as JObject;
                if(intercept == null) unknown.Add("INTERCEPTION_OWNER_MISSING");
                else { var actions = (JArray)intercept["actions"];
                    foreach(var a in actions) if(!new[]{"PREPARATION","RECOVERY","DONE","DOWN_TERMINAL","TERMINAL"}.Contains((string)a["phase"])) unknown.Add("INTERCEPTION_PHASE_UNKNOWN");
                    Fact("INTERCEPTION",intercept,actions.Any(a=>(string)a["phase"]=="PREPARATION" || (string)a["phase"]=="RECOVERY" && (long)a["endTick"]>tick) || ((JObject)intercept["localManualOccupations"]).Properties().Any(p=>(long)p.Value>tick));
                }
            }
            var active = native["activeOwner"] as JObject;
            if(active != null) {
                if(!new[]{"IDLE","WINDUP","BURST","RECOVERY","TERMINAL"}.Contains((string)active["phase"])) unknown.Add("ACTIVE_PHASE_UNKNOWN");
                foreach(var s in (JArray)active["shots"]) if(!new[]{"IN_FLIGHT","RESOLVED_HIT","RESOLVED_MISS","CLEANED_TERMINAL"}.Contains((string)s["status"])) unknown.Add("ACTIVE_SHOT_STATUS_UNKNOWN");
                Fact("MIRA_ACTIVE",active,((JArray)active["reservations"]).Any() || ((JArray)active["shots"]).Any(s=>(string)s["status"]=="IN_FLIGHT") || (string)active["phase"]=="RECOVERY" && (long?)active["recoveryUntilTick"]>tick);
            }
            return new BResidualHazardObservation(new JObject { ["provenance"] = "INSTALLED_LOCAL_EXECUTORS_NOT_SERVER_TRUSTED", ["bindingHash"] = native["bindingHash"].DeepClone(), ["tick"] = tick, ["commandRevision"] = ((JArray)native["commands"]).Count, ["sequence"] = native["sequence"].DeepClone(), ["owners"] = facts, ["unknown"] = unknown });
        }
    }
    public sealed partial class BDirectAttackBasicLoop
    {
        public BResidualHazardObservation ObserveResidualHazards() => BResidualHazardObservation.ReadOwned(Capture());
    }
}
