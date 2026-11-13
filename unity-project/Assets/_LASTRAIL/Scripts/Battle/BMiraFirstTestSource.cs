using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    /// <summary>
    /// Frozen, explicitly limited static input. The authenticated supplier independently retains
    /// both originals and their hashes, run identity and actual departure pin. No live lookup,
    /// fire judgement, critical inference, ability creation, HP lifecycle or START admission.
    /// </summary>
    public sealed class BMiraFirstTestSource
    {
        public const string Version="combat.mira-first-test-static-source.v1.20261015";
        public const string Profile="mira-first-no-effect-test.v1.20261015";
        readonly JObject original;
        public string Hash { get; }
        public string AccountId => (string)original["accountId"];
        public string RunId => (string)original["runId"];
        public bool StaticStatsReady => true;
        public bool StartReady => false;
        public string[] MissingSources => ((JArray)original["missing"]).Select(t=>(string)t).ToArray();
        public JObject Capture() => (JObject)original.DeepClone();
        public JObject StaticStats() => (JObject)original["effectiveStats"].DeepClone();
        BMiraFirstTestSource(JObject packet,string hash) { original=(JObject)packet.DeepClone();Hash=hash; }

        public static BMiraFirstTestSource Read(JObject input,JObject trustedOriginal,string independentlyPinnedHash,
            string accountId,string runId,JObject trustedOriginalMira,string independentlyPinnedMiraHash,string independentlyPinnedDepartureHash)
        {
            Need(input!=null && trustedOriginal!=null && trustedOriginalMira!=null,"missing");
            Identifier(accountId);Identifier(runId);
            Need(Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(input))<=1024*1024,"capacity");
            Fields(input,"version","profile","accountId","runId","originalComponent","originalComponentHash","research","configuration",
                "effectiveStats","staticStatsReady","unsupported","statStage","passiveApplication","missing","startBinding","runtimeSupported","canStart","packetHash");
            Need(HashValue(independentlyPinnedHash) && (string)input["packetHash"]==independentlyPinnedHash
                && (string)trustedOriginal["packetHash"]==independentlyPinnedHash && DigestBody(input)==independentlyPinnedHash
                && JToken.DeepEquals(input,trustedOriginal),"original_conflict");
            Need(Text(input["version"])==Version && Text(input["profile"])==Profile
                && Text(input["accountId"])==accountId && Text(input["runId"])==runId,"binding");
            False(input["runtimeSupported"]);False(input["canStart"]);True(input["staticStatsReady"]);
            Empty(input["unsupported"]);
            EqualText(input["statStage"],"FIRST_TEST_STATIC_BEFORE_DYNAMIC_COMBAT_EFFECTS");
            EqualText(input["passiveApplication"],"ORIGINAL_COMPONENT_OWN_BASIC_UNAPPLIED_FIRE_OWNER_ONCE");
            EqualText(input["startBinding"],"EXPLICIT_INTERNAL_PROFILE_NOT_AN_OPERATING_START_ADMISSION");
            Need(JToken.DeepEquals(input["missing"],new JArray("ACTUAL_GEOMETRY_AND_NATIVE_FIRE_HP_CHECKPOINT_BINDING",
                "ORIGINAL_FIRE_D_CRITICAL_OPPORTUNITY_AND_TARGET_SPECIFIC_EFFECTS")),"missing_judgements");
            var research=Object(input["research"]);
            Fields(research,"status","modifiers","ledgerEvidence","policyVersion","policySource","reason");
            EqualText(research["status"],"NOT_APPLICABLE");Null(research["modifiers"]);Null(research["ledgerEvidence"]);
            EqualText(research["policyVersion"],"mira-first-test-research-na-20261015");
            EqualText(research["policySource"],"44_COMBAT_DETAIL_CONTRACTS.md#mira-first-test-research-na-20261015");
            EqualText(research["reason"],"APPROVED_RESEARCH_INACTIVE_FOR_THIS_FIRST_NO_EFFECT_MIRA_TEST_ONLY");

            var component=Object(input["originalComponent"]);
            Fields(component,"version","accountId","runId","originalMira","originalMiraHash","departurePinHash","departureOriginHash","runSnapshotHash",
                "base","equipment","coreStats","research","commander","passive","effectiveStats","statStage","missing","runtimeSupported","canStart","packetHash");
            Need(HashValue((string)component["packetHash"]) && DigestBody(component)==(string)component["packetHash"]
                && JToken.DeepEquals(input["originalComponentHash"],component["packetHash"]),"component_hash");
            EqualText(component["version"],"combat.mira-effective-source.v1.20261015");
            Need(Text(component["accountId"])==accountId && Text(component["runId"])==runId,"component_binding");
            False(component["runtimeSupported"]);False(component["canStart"]);Null(component["effectiveStats"]);
            EqualText(component["statStage"],"VERIFIED_EQUIPMENT_CORE_COMPONENT_NOT_FINAL_EFFECTIVE_STATS");
            Need(HashValue(independentlyPinnedDepartureHash) && (string)component["departurePinHash"]==independentlyPinnedDepartureHash
                && HashValue((string)component["departureOriginHash"]) && HashValue((string)component["runSnapshotHash"]),"departure");
            // Reuse the strict original reader with separately supplied trust; nested self-pins confer no trust.
            var mira=BMiraBasicSource.Read(Object(component["originalMira"]),trustedOriginalMira,independentlyPinnedMiraHash,accountId).Capture();
            Need((string)component["originalMiraHash"]==independentlyPinnedMiraHash,"mira_hash");
            ValidateComponentResearch(component);
            var equipment=Object(component["equipment"]);
            Fields(equipment,"owner","inventoryRevision","inventoryHash","inventoryEmpty","equippedEmpty","slots","status","blocked");
            EqualText(equipment["owner"],"EquipmentInventoryStore/EquipmentEffectStateStore");
            Positive(equipment["inventoryRevision"]);Need(HashValue((string)equipment["inventoryHash"]),"inventory_hash");
            Need(equipment["inventoryEmpty"]?.Type==JTokenType.Boolean,"inventory_empty_evidence");
            True(equipment["equippedEmpty"]);EqualText(equipment["status"],"VERIFIED_CORE_COMPONENT");Empty(equipment["blocked"]);
            var slots=Array(equipment["slots"]);Need(slots.Count==3,"slots");foreach(var slot in slots)Null(slot);
            var commander=Object(component["commander"]);Fields(commander,"snapshot","sourceHash","stage");
            EqualText(commander["stage"],"ORIGINAL_DEPARTURE_PIN_NOT_CURRENT_ACCOUNT_RELOOKUP");
            Need(JToken.DeepEquals(commander["snapshot"],mira["modifierSources"]["commander"])
                && Digest(mira["modifierSources"]["commanderSource"])==(string)commander["sourceHash"],"commander_binding");
            if(commander["snapshot"].Type!=JTokenType.Null)
                Need(Object(Object(commander["snapshot"])["ranks"]).Count==0,"commander_effects");
            ValidatePassive(Object(component["passive"]),mira);
            var configuration=Object(input["configuration"]);
            Fields(configuration,"support","inventoryHash","inventoryRevision","departurePinHash","commanderSourceHash");
            EqualText(configuration["support"],"VERIFIED_EMPTY_MIRA_SLOTS_AND_NO_ACTIVE_COMMANDER_EFFECTS_ONLY");
            foreach(var field in new[]{"inventoryHash","inventoryRevision"})Need(JToken.DeepEquals(configuration[field],equipment[field]),"configuration");
            Need(JToken.DeepEquals(configuration["departurePinHash"],component["departurePinHash"])
                && JToken.DeepEquals(configuration["commanderSourceHash"],commander["sourceHash"]),"configuration_binding");
            ValidateStats(component,Object(input["effectiveStats"]),mira);
            return new BMiraFirstTestSource(input,independentlyPinnedHash);
        }
        static void ValidateComponentResearch(JObject c)
        {
            var r=Object(c["research"]);Fields(r,"status","modifiers","revision","sourceHash","reason");
            EqualText(r["status"],"SOURCE_OWNER_UNAVAILABLE");Null(r["modifiers"]);Null(r["revision"]);Null(r["sourceHash"]);
            EqualText(r["reason"],"NO_APPROVED_ACTIVE_NODE_CATALOG_AND_NO_ACCOUNT_RESEARCH_LEDGER_OWNER");
            Need(JToken.DeepEquals(c["missing"],new JArray("AUTHORITATIVE_RESEARCH_LEDGER_AND_APPLICABILITY",
                "FULL_EQUIPMENT_TIMING_AND_CONDITIONAL_EFFECT_OWNER","FINAL_STATIC_EFFECTIVE_STAT_COMPOSITION",
                "ORIGINAL_FIRE_D_CRITICAL_OPPORTUNITY_AND_TARGET_SPECIFIC_EFFECTS")),"component_missing");
        }
        static void ValidatePassive(JObject p,JObject mira)
        {
            Fields(p,"effect","appliedToStaticStats","applicationOwner","nonEquipmentOutgoingCapBps");
            False(p["appliedToStaticStats"]);EqualText(p["applicationOwner"],"ORIGINAL_MODE_COMBAT_FIRE_OWNER_ONCE");
            var effect=Object(p["effect"]);Fields(effect,"sourceId","group","valueBps");
            EqualText(effect["sourceId"],"passive:op_mira:OWN_BASIC");EqualText(effect["group"],"NON_EQUIPMENT_OUTGOING");
            Need(Integer(effect["valueBps"])==1000 && JToken.DeepEquals(effect["valueBps"],mira["definition"]["passive"]["valueBps"])
                && Integer(p["nonEquipmentOutgoingCapBps"])==3000
                && JToken.DeepEquals(p["nonEquipmentOutgoingCapBps"],mira["definition"]["originalPolicy"]["effectRules"]["nonEquipmentOutgoingCapBps"]),"passive");
        }
        static void ValidateStats(JObject c,JObject stats,JObject mira)
        {
            var permanent=Object(mira["actor"]["owned"]["permanentStats"]);
            var basis=Object(c["base"]);Fields(basis,"maxHp","attack","defense");
            Need(JToken.DeepEquals(basis["maxHp"],permanent["maxHp"]) && JToken.DeepEquals(basis["attack"],permanent["attack"])
                && Exact(basis["defense"]).Numerator==Integer(permanent["defense"]) && Exact(basis["defense"]).Denominator==1,"original_base");
            var core=Object(c["coreStats"]);Fields(core,"maxHp","exactMaxHp","attack","defense","additiveBps","rounding");
            EqualText(core["rounding"],"HP_INTEGER_FLOOR_AT_HP_BOUNDARY_ATK_DEF_EXACT_UNTIL_ORIGINAL_DAMAGE_OWNER");
            var additive=Object(core["additiveBps"]);Fields(additive,"HP","ATK","DEF");
            foreach(var field in new[]{"HP","ATK","DEF"})Need(Integer(additive[field])==0,"nonempty_additive");
            Need(Positive(core["maxHp"])==Positive(basis["maxHp"]) && Exact(core["exactMaxHp"]).Numerator==Integer(basis["maxHp"])
                && Exact(core["exactMaxHp"]).Denominator==1,"hp");
            foreach(var field in new[]{"attack","defense"})Need(JToken.DeepEquals(Exact(core[field]).Snapshot(),Exact(basis[field]).Snapshot()),"core_original_stats");
            Fields(stats,"maxHp","attack","defense","basicIntervalMs");
            foreach(var field in new[]{"maxHp","attack","defense"})Need(JToken.DeepEquals(stats[field],core[field]),"static_stats");
            Need(Positive(stats["basicIntervalMs"])==1800 && JToken.DeepEquals(stats["basicIntervalMs"],permanent["basicIntervalMs"]),"interval");
        }
        static CombatExactValue Exact(JToken value)=>CombatExactValue.From(Object(value));
        static string Digest(JToken value) { Need(value!=null,"hash_body");return BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(value)); }
        static string DigestBody(JObject value) { var body=(JObject)value.DeepClone();body.Remove("packetHash");return Digest(body); }
        static bool HashValue(string value)=>value!=null && value.Length==64 && value.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
        static void Identifier(string value)=>Need(value!=null && value.Length>=1 && value.Length<=128 && value.All(c=>c>='!'&&c<='~'),"identifier");
        static void Need(bool valid,string code) { if(!valid)throw new ArgumentException("mira_first_test_source_"+code); }
        static JObject Object(JToken value)=>value as JObject??throw new ArgumentException("mira_first_test_source_object");
        static JArray Array(JToken value)=>value as JArray??throw new ArgumentException("mira_first_test_source_array");
        static void Fields(JObject value,params string[] names)=>Need(value.Count==names.Length && names.All(n=>value[n]!=null),"fields");
        static string Text(JToken value) { Need(value?.Type==JTokenType.String && !string.IsNullOrWhiteSpace((string)value),"text");return (string)value; }
        static void EqualText(JToken value,string expected)=>Need(Text(value)==expected,"contract");
        static void Null(JToken value)=>Need(value?.Type==JTokenType.Null,"null");
        static void False(JToken value)=>Need(value?.Type==JTokenType.Boolean && !(bool)value,"runtime_or_applied");
        static void True(JToken value)=>Need(value?.Type==JTokenType.Boolean && (bool)value,"ready_or_empty");
        static void Empty(JToken value)=>Need(Array(value).Count==0,"unsupported");
        static long Integer(JToken value) { Need(value?.Type==JTokenType.Integer && long.TryParse(value.ToString(),out _),"integer");long result=(long)value;Need(result>=0&&result<=9007199254740991L,"integer_range");return result; }
        static long Positive(JToken value) { long result=Integer(value);Need(result>0,"positive");return result; }
    }
}
