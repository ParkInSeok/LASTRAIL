using System;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    /// <summary>
    /// Verified frozen read candidate, not START, effective combat stats or a projectile producer.
    /// Trusted original + hash + account must be retained by the authenticated supplier/checkpoint
    /// owner independently of candidate bytes. This reader performs no DB/current-state refresh.
    /// Equipment/research/effective stats/geometry nulls are preserved; no passive is applied here.
    /// </summary>
    public sealed class BMiraBasicSource
    {
        public const string Version = "combat.mira-basic-source.v1.20261015";
        public const string DefinitionVersion = "mira-basic-ballistics-v1.20261015";
        readonly JObject original;
        public string Hash { get; }
        public string AccountId => (string)original["accountId"];
        public string AbilityId => (string)original["actor"]["abilityId"];
        public long FormationRevision => (long)original["formationRevision"];
        public long LoadoutRevision => (long)original["loadoutRevision"];
        public long OriginalBasicIntervalMs => (long)original["definition"]["ability"]["originalBasicIntervalMs"];
        public int PhysicalAdjacentCars => (int)original["definition"]["ability"]["range"]["physicalAdjacentCars"];
        public long SpeedMmPerSecond => (long)original["definition"]["ballistics"]["speedMmPerSecond"];
        public long RadiusMm => (long)original["definition"]["ballistics"]["radiusMm"];
        public long MaximumDistanceMm => (long)original["definition"]["ballistics"]["maximumFlightDistanceMm"];
        public long MaximumFlightMs => (long)original["definition"]["ballistics"]["maximumFlightMs"];
        public long LengthUnitMm => (long)original["definition"]["ballistics"]["lengthUnitMm"];
        public bool StartReady => false;
        public string[] MissingSources => ((JArray)original["missing"]).Select(t=>(string)t).ToArray();
        public JObject Capture() => (JObject)original.DeepClone();
        public JObject Actor() => (JObject)original["actor"].DeepClone();
        public void RequireRuntimeReady() =>
            throw new InvalidOperationException("mira_source_not_runtime_ready:"+string.Join(",",MissingSources));
        BMiraBasicSource(JObject packet,string hash) { original=(JObject)packet.DeepClone(); Hash=hash; }

        public static BMiraBasicSource Read(JObject input,JObject trustedOriginal,string independentlyPinnedHash,string accountId)
        {
            Need(input!=null && trustedOriginal!=null,"missing");
            Need(!string.IsNullOrWhiteSpace(accountId) && accountId.Length<=128,"account");
            Need(Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(input))<=512*1024,"capacity");
            Fields(input,"version","accountId","definition","definitionHash","ownedProfile","ownedProfileHash","ownedSourceHash",
                "formationRevision","loadoutRevision","refit","actor","modifierSources","geometry","sourceStatus","missing","runtimeSupported","canStart","packetHash");
            Need(BareHash(independentlyPinnedHash) && (string)input["packetHash"]==independentlyPinnedHash
                && (string)trustedOriginal["packetHash"]==independentlyPinnedHash
                && DigestWithout(input,"packetHash")==independentlyPinnedHash
                && JToken.DeepEquals(input,trustedOriginal),"original_conflict");
            Need(Text(input["accountId"])==accountId && Text(input["version"])==Version
                && Text(input["sourceStatus"])=="VERIFIED_OWNED_SELECTION_READ_ONLY_CANDIDATE_NOT_START","contract");
            False(input["runtimeSupported"]); False(input["canStart"]); Null(input["geometry"]);
            Positive(input["formationRevision"]); Positive(input["loadoutRevision"]);
            var definition=Object(input["definition"]);
            ReadDefinition(definition);
            Need((string)input["definitionHash"]==(string)definition["definitionHash"],"definition_hash");
            var profile=Object(input["ownedProfile"]);
            Need(Digest(profile)==(string)input["ownedProfileHash"],"owned_profile_hash");
            Need(Text(profile["schemaVersion"])=="combat.owned-profile-intake.v1"
                && Text(profile["binding"])=="READ_ONLY_CANDIDATE_NOT_ATOMIC_START"
                && Text(profile["accountId"])==accountId,"owned_profile");
            False(profile["canExecute"]); Null(profile["startSnapshot"]); Null(profile["checkpoint"]);
            Need("sha256:"+DigestWithout(profile,"contentHash")==(string)profile["contentHash"],"profile_content_hash");
            Need((string)profile["definitionHash"]=="sha256:"+(string)definition["originalBaseHash"]
                && (string)profile["policyHash"]=="sha256:"+(string)definition["originalPolicyHash"]
                && (string)profile["definitionVersion"]==(string)definition["originalBase"]["version"]
                && (string)profile["policyVersion"]==(string)definition["originalPolicy"]["version"]
                && JToken.DeepEquals(profile["formationRevision"],input["formationRevision"])
                && JToken.DeepEquals(profile["loadoutRevision"],input["loadoutRevision"])
                && Text(input["ownedSourceHash"])==Text(profile["sourceHash"])
                && ReferenceHash((string)input["ownedSourceHash"]),"owned_references");

            var actor=Object(input["actor"]); Fields(actor,"actorId","operatorId","abilityId","owned","placement","bcb","nativeSupported");
            Need(Text(actor["actorId"])=="op_mira" && Text(actor["operatorId"])=="op_mira"
                && Text(actor["abilityId"])=="op_mira.basic","actor");
            False(actor["nativeSupported"]);
            var selected=Array(profile["operators"]).Where(t=>(string)t["operatorId"]=="op_mira").ToArray();
            Need(selected.Length==1 && JToken.DeepEquals(selected[0],actor["owned"]),"owned_actor_copy");
            var owned=Object(actor["owned"]);
            Null(owned["effectiveStats"]); Null(owned["effectModifiers"]);
            if(owned["statInputs"]?.Type!=JTokenType.Null)
                Need((string)owned["statInputs"]?["sourceOwner"]==accountId
                    && (string)owned["statInputs"]?["targetDefinitionId"]=="op_mira","stat_owner");
            if(owned["permanentStats"]?.Type!=JTokenType.Null)
            {
                var stats=Object(owned["permanentStats"]); Fields(stats,"maxHp","attack","defense","basicIntervalMs");
                Positive(stats["maxHp"]); Nonnegative(stats["defense"]); Positive(stats["basicIntervalMs"]);
                CombatExactValue.From(stats["attack"] as JObject);
                Need((long)stats["basicIntervalMs"]==(long)definition["ability"]["originalBasicIntervalMs"],"basic_interval");
            }
            var modifiers=Object(input["modifierSources"]);
            Fields(modifiers,"equipment","research","commander","commanderSource","passive","application");
            Need(JToken.DeepEquals(modifiers["equipment"],profile["equipment"])
                && JToken.DeepEquals(modifiers["research"],profile["research"])
                && JToken.DeepEquals(modifiers["passive"],definition["passive"])
                && JToken.DeepEquals(modifiers["commander"],modifiers["commanderSource"]?["snapshot"])
                && Text(modifiers["application"])=="MODE_COMBAT_OWNER_REQUIRED_NOT_EFFECTIVE_STATS","modifier_sources");
            Null(modifiers["equipment"]); Null(modifiers["research"]);
            BindRefit(input,actor,accountId);
            var missing=Array(input["missing"]).Select(Text).ToArray();
            Need(missing.Distinct(StringComparer.Ordinal).Count()==missing.Length
                && Array(profile["missing"]).Select(Text).All(missing.Contains),"missing_sources");
            foreach(var required in new[]{"EFFECTIVE_STATS_AFTER_ORIGINAL_MODIFIERS","ACTUAL_MUZZLE_HIT_SHAPE_AND_GEOMETRY",
                "ORIGINAL_FIRE_OPPORTUNITY_D_CRITICAL_AND_BODY_TARGETS","MODE_COMBAT_LIFECYCLE_HP_AND_CHECKPOINT_BINDING"})
                Need(missing.Contains(required),"missing:"+required);
            if(actor["placement"].Type==JTokenType.Null)
                Need(missing.Contains("ORIGINAL_B_PLACEMENT_AND_BCB_REFIT"),"placement_missing");
            return new BMiraBasicSource(input,independentlyPinnedHash);
        }

        static void ReadDefinition(JObject d)
        {
            Fields(d,"version","scope","approval","originalBase","originalBaseHash","originalPolicy","originalPolicyHash",
                "registrySource","ability","ballistics","passive","passiveApplication","runtimeSupported","canStart","definitionHash");
            Need(Text(d["version"])==DefinitionVersion
                && Text(d["scope"])=="NEW_B_EXPERIMENTAL_MIRA_PROJECTILE_BASIC_ONLY"
                && Text(d["approval"])=="44#friendly-basic-ballistics-detail-v1-20261015"
                && BareHash((string)d["definitionHash"]) && DigestWithout(d,"definitionHash")==(string)d["definitionHash"]
                && Digest(d["originalBase"])==(string)d["originalBaseHash"]
                && Digest(d["originalPolicy"])==(string)d["originalPolicyHash"],"definition");
            False(d["runtimeSupported"]); False(d["canStart"]);
            Fields(Object(d["registrySource"]),"contentHash","collisionCheck");
            Need(Text(d["registrySource"]["collisionCheck"])=="ORIGINAL_PUBLISHED_ABILITY_INDEX","registry");
            Text(d["registrySource"]["contentHash"]);
            var a=Object(d["ability"]); Fields(a,"id","operatorId","kind","attackForm","attackCoefficient","originalBasicIntervalMs","opportunityPolicy","range");
            Need(Text(a["id"])=="op_mira.basic" && Text(a["operatorId"])=="op_mira" && Text(a["kind"])=="BASIC"
                && Text(a["attackForm"])=="PROJECTILE"
                && Text(a["opportunityPolicy"])=="ONE_ORIGINAL_BASIC_OPPORTUNITY_NO_NEW_BURST","ability");
            Need(JToken.DeepEquals(a["attackCoefficient"],new JObject { ["numerator"]="1",["denominator"]="1" })
                && Nonnegative(a["originalBasicIntervalMs"])==1800
                && JToken.DeepEquals(a["originalBasicIntervalMs"],d["originalBase"]?["operators"]?["op_mira"]?["basicIntervalMs"]),"original_basic");
            var range=Object(a["range"]); Fields(range,"physicalAdjacentCars","targetCategory","maximumDistanceMm","geometryRequired","rule");
            Need(Nonnegative(range["physicalAdjacentCars"])==2 && Text(range["targetCategory"])=="ENEMY"
                && Nonnegative(range["maximumDistanceMm"])==24000 && range["geometryRequired"]?.Type==JTokenType.Boolean
                && (bool)range["geometryRequired"] && Text(range["rule"])=="INTERSECTION_WITH_ORIGINAL_DOMAIN_LOS_AND_TARGET_ELIGIBILITY","range");
            var b=Object(d["ballistics"]);
            Fields(b,"speedMmPerSecond","radiusMm","maximumFlightDistanceMm","maximumFlightMs","lengthUnitMm","trajectory","attackEvaluation","targetDefenseEvaluation");
            Need(Nonnegative(b["speedMmPerSecond"])==48000 && Nonnegative(b["radiusMm"])==60
                && Nonnegative(b["maximumFlightDistanceMm"])==24000 && Nonnegative(b["maximumFlightMs"])==500
                && Nonnegative(b["lengthUnitMm"])==12000 && Text(b["trajectory"])=="FIXED_DIRECTION_NON_HOMING"
                && Text(b["attackEvaluation"])=="AT_FIRE" && Text(b["targetDefenseEvaluation"])=="AT_IMPACT","ballistics");
            var passive=Object(d["passive"]); Fields(passive,"operatorId","source","group","valueBps");
            Need(Text(passive["operatorId"])=="op_mira" && Text(passive["source"])=="OWN_BASIC"
                && Text(passive["group"])=="NON_EQUIPMENT_OUTGOING" && Nonnegative(passive["valueBps"])==1000
                && Text(d["passiveApplication"])=="ORIGINAL_BASIC_FIRE_OWNER_ONCE_NOT_APPLIED_TO_PERMANENT_STATS","passive");
            var original=Array(d["originalPolicy"]?["passives"]).Where(p=>(string)p["operatorId"]=="op_mira").ToArray();
            Need(original.Length==1 && JToken.DeepEquals(original[0],passive),"original_passive");
        }
        static void BindRefit(JObject input,JObject actor,string accountId)
        {
            if(actor["placement"].Type==JTokenType.Null)
            {
                Null(actor["bcb"]); Null(input["refit"]); return;
            }
            var placement=Object(actor["placement"]); Fields(placement,"physicalSlot","carriageInstanceId","carriageDefinitionId");
            Nonnegative(placement["physicalSlot"]); Text(placement["carriageInstanceId"]); Text(placement["carriageDefinitionId"]);
            var bcb=Object(actor["bcb"]); Fields(bcb,"type","rank","rankRevision","selectionRevision","sourceStatus");
            var refit=Object(input["refit"]); Fields(refit,"revision","sourceHash","definitionHash","departure");
            Positive(refit["revision"]); Need(BareHash((string)refit["sourceHash"]) && BareHash((string)refit["definitionHash"]),"refit_hash");
            var departure=Object(refit["departure"]);
            Need(Text(departure["accountId"])==accountId && Text(departure["version"])=="combat.b-carriage-buffs.v1.20261002","departure");
            var cars=Array(departure["cars"]).Where(c=>(string)c["instanceId"]==(string)placement["carriageInstanceId"]).ToArray();
            Need(cars.Length==1,"carriage");
            var car=cars[0];
            Need((string)car["occupant"]?["kind"]=="OPERATOR" && (string)car["occupant"]?["operatorId"]=="op_mira"
                && JToken.DeepEquals(car["slot"],placement["physicalSlot"]) && JToken.DeepEquals(car["carriageId"],placement["carriageDefinitionId"])
                && JToken.DeepEquals(car["type"],bcb["type"]) && JToken.DeepEquals(car["rank"],bcb["rank"])
                && JToken.DeepEquals(departure["rankRevision"],bcb["rankRevision"])
                && JToken.DeepEquals(departure["selectionRevision"],bcb["selectionRevision"])
                && JToken.DeepEquals(departure["sourceStatus"],bcb["sourceStatus"]),"refit_binding");
        }
        static string Digest(JToken value)
        {
            Need(value!=null,"missing_hash_body");
            return BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(value));
        }
        static string DigestWithout(JObject value,string field)
        {
            var body=(JObject)value.DeepClone(); body.Remove(field); return Digest(body);
        }
        static bool BareHash(string value) => value!=null && value.Length==64
            && value.All(c=>c>='0' && c<='9' || c>='a' && c<='f');
        static bool ReferenceHash(string value) => value!=null && value.Length==71
            && value.StartsWith("sha256:",StringComparison.Ordinal) && BareHash(value.Substring(7));
        static void Need(bool ok,string code) { if(!ok) throw new ArgumentException("mira_basic_source_"+code); }
        static JObject Object(JToken value) => value as JObject ?? throw new ArgumentException("mira_basic_source_object");
        static JArray Array(JToken value) => value as JArray ?? throw new ArgumentException("mira_basic_source_array");
        static void Fields(JObject value,params string[] fields) =>
            Need(value!=null && value.Count==fields.Length && fields.All(f=>value[f]!=null),"fields");
        static string Text(JToken value)
        {
            Need(value?.Type==JTokenType.String && !string.IsNullOrWhiteSpace((string)value),"text");
            return (string)value;
        }
        static long Nonnegative(JToken value)
        {
            Need(value?.Type==JTokenType.Integer && long.TryParse(value.ToString(),out _) ,"integer");
            long result=(long)value; Need(result>=0 && result<=9007199254740991L,"integer_range"); return result;
        }
        static void Positive(JToken value) => Need(Nonnegative(value)>0,"positive_revision_or_stat");
        static void False(JToken value) => Need(value?.Type==JTokenType.Boolean && !(bool)value,"unsupported_runtime");
        static void Null(JToken value) => Need(value?.Type==JTokenType.Null,"unsupported_source");
    }
}