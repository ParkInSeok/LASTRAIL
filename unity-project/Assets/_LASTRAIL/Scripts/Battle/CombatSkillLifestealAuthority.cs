using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    // Trusted local-mode port. Implementations must resolve the ORIGINAL action's
    // applied ownership/definition and healing conditions at the given authority
    // tick. A catalog, selection UI flag or client-reported damage is not proof.
    // Missing proof must throw; an explicit verified non-owner is a normal zero.
    public interface ICombatSkillLifestealAuthority
    {
        // Stable account/run/node + logical-generation scope of the original
        // authority. Historical resolution must use pinned action evidence,
        // never the currently selected card or a new GET during checkpoint load.
        string BindingId { get; }
        CombatSkillLifestealProof ResolveOriginal(ConfirmedSkillDamage damage, long tick);
    }
    public sealed class CombatSkillLifestealProof
    {
        public string EvidenceId { get; }
        public string DefinitionVersion { get; }
        public bool AppliedOwner { get; }
        public bool SameGeneration { get; }
        public bool HealingAllowed { get; }
        public int HealingMultiplierBps { get; }
        public CombatSkillLifestealProof(string evidenceId,string definitionVersion,bool appliedOwner,bool sameGeneration,bool healingAllowed,int healingMultiplierBps)
        {
            if(string.IsNullOrWhiteSpace(evidenceId)||string.IsNullOrWhiteSpace(definitionVersion)||healingMultiplierBps<0)throw new ArgumentException("Verified original proof required");
            EvidenceId=evidenceId;DefinitionVersion=definitionVersion;AppliedOwner=appliedOwner;SameGeneration=sameGeneration;HealingAllowed=healingAllowed;HealingMultiplierBps=healingMultiplierBps;
        }
        internal string Outcome(int hp,int maximum,int damage)
            =>!AppliedOwner?"NOT_OWNED":!SameGeneration?"GENERATION_CHANGED":hp==0?"DOWN":!HealingAllowed?"HEALING_BLOCKED":damage==0?"NO_HP_DAMAGE":hp==maximum?"FULL_HP":"HEALED";
        internal int Recovery(int hp,int maximum,int damage)
            =>Outcome(hp,maximum,damage)!="HEALED"?0:CombatAttributedLifesteal.BoundedRecovery(damage,HealingMultiplierBps,hp,maximum);
    }
    public sealed partial class BasicCombatSimulation
    {
        ICombatSkillLifestealAuthority skillLifestealAuthority;
        string skillLifestealBinding;
        // Detect partial checkpoint combinations, not authentication. The existing
        // secure checkpoint envelope and original authority still own trust.
        static string SkillRecoveryCommit(JObject root,JToken journal)=>Digest(StableJson(new JArray(root["configurationSha256"],root["commands"],root["actors"],journal)));
        // Configure from a trusted original-mode factory, before any actions.
        // Use CombatSceneDriver for transactional execution and publication.
        public void ConfigureSkillLifesteal(ICombatSkillLifestealAuthority authority)
        {
            if(authority==null||string.IsNullOrWhiteSpace(authority.BindingId))throw new ArgumentException("Original authority binding required");
            if(Tick!=0||commands.Count!=0||skillLifestealAuthority!=null||seraCommands!=null)throw new InvalidOperationException("Configure original skill owner before execution");
            skillLifestealAuthority=authority;skillLifestealBinding=authority.BindingId;
        }
        CombatSkillLifestealProof OriginalLifestealProof(JObject row)
        {
            if(skillLifestealAuthority.BindingId!=skillLifestealBinding)throw new InvalidOperationException("Original lifesteal authority changed");
            return skillLifestealAuthority.ResolveOriginal(new ConfirmedSkillDamage(row,false),(long)row["tick"])
                ??throw new InvalidOperationException("Original lifesteal proof missing");
        }
        static string RecoveryOutcome(CombatSkillLifestealProof p,int hp,int maximum,int damage)
            =>p.Outcome(hp,maximum,damage);
        static int ExpectedRecovery(CombatSkillLifestealProof p,int hp,int maximum,int damage)
            =>p.Recovery(hp,maximum,damage);
        void ResolveSkillLifesteal(Command command,JObject row)
        {
            if(skillLifestealAuthority==null||command.Skill.Caster.Config.Train)return;
            var source=command.Skill.Caster;if(source.Device)throw new InvalidOperationException("Device requires original logical owner mapping");
            var proof=OriginalLifestealProof(row);int before=source.Hp,maximum=source.Vitals.MaxHp,damage=(int)row["actualResult"]["hpLoss"];
            var outcome=RecoveryOutcome(proof,before,maximum,damage);
            int gain=new CombatAttributedLifesteal(source.Vitals).Apply(new[]{new CombatDamageResult(0,damage)},proof.HealingMultiplierBps,outcome=="HEALED").ActualHpGain;
            row["lifesteal"]=new JObject{["evidenceId"]=proof.EvidenceId,["definitionVersion"]=proof.DefinitionVersion,["hpBefore"]=before,["maximumHp"]=maximum,["hpAfter"]=source.Hp,["actualHpGain"]=gain,["outcome"]=outcome};
        }
        void ValidateSkillLifesteal(Command command,JObject row)
        {
            bool required=skillLifestealAuthority!=null&&(string)row["kind"]=="DAMAGE"&&!command.Skill.Caster.Config.Train;
            if(!required){if(row["lifesteal"]!=null)throw new ArgumentException("Unexpected recovery receipt");return;}
            if(command.Skill.Caster.Device)throw new ArgumentException("Unmapped recovery source");
            var r=row["lifesteal"] as JObject;Fields(r,"evidenceId","definitionVersion","hpBefore","maximumHp","hpAfter","actualHpGain","outcome");
            var proof=OriginalLifestealProof(row);int max=(int)Nat(r,"maximumHp",int.MaxValue),before=(int)Nat(r,"hpBefore",max),after=(int)Nat(r,"hpAfter",max),gain=(int)Nat(r,"actualHpGain",max),damage=(int)row["actualResult"]["hpLoss"];
            if(max==0||Text(r,"evidenceId")!=proof.EvidenceId||Text(r,"definitionVersion")!=proof.DefinitionVersion||Text(r,"outcome")!=RecoveryOutcome(proof,before,max,damage)||gain!=ExpectedRecovery(proof,before,max,damage)||after-before!=gain)throw new ArgumentException("Original recovery receipt mismatch");
            // State restoration already owns HP. Validation NEVER adds this gain.
        }
    }
}
