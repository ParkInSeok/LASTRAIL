using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    // Read-only projection of the authority's existing journal, not a new damage
    // ledger, grant proof or permission to replay recovery. Node/generation and
    // original lifesteal-definition binding still belong to the mode adapter.
    public sealed class ConfirmedSkillDamage
    {
        public string EventId {get;}
        public string RunId {get;}
        public string ProfileHash {get;}
        public string ParentCommandId {get;}
        public string SourceId {get;}
        public string OperatorId {get;}
        public string TargetId {get;}
        public string AbilityId {get;}
        public string NodeId {get;}
        public long? AttemptGeneration {get;}
        public int? PulseIndex {get;}
        public long? TargetGeneration {get;}
        public long GlobalSequence {get;}
        public CombatDamageResult Damage {get;}
        internal ConfirmedSkillDamage(JObject row,bool sharedTrain)
        {
            EventId=(string)row["id"];RunId=(string)row["runId"];ProfileHash=(string)row["profileSha256"];
            ParentCommandId=(string)row["commandId"];SourceId=(string)row["sourceId"];
            OperatorId=sharedTrain?null:SourceId;TargetId=(string)row["targetId"];GlobalSequence=(long)row["globalSequence"];
            AbilityId=(string)row["abilityId"];NodeId=(string)row["nodeId"];AttemptGeneration=(long?)row["attemptGeneration"];PulseIndex=(int?)row["pulseIndex"];TargetGeneration=(long?)row["targetGeneration"];
            Damage=new CombatDamageResult((int)row["actualResult"]["absorbed"],(int)row["actualResult"]["hpLoss"]);
        }
    }
}
