using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle
{
    public sealed partial class BasicCombatSimulation
    {
        static void ValidateOperatorSource(JObject draft,string logical,string hash){var rows=((JArray)draft["sourceFiles"]).Where(r=>(string)r["logicalName"]==logical).ToArray();if(rows.Length!=1||(string)rows[0]["rawSha256"]!=hash)throw new ArgumentException("Operator policy source pin mismatch.");}
        public static BasicCombatSimulation FromPreparedProfileForLocalTest(JObject prepared,JObject currentOwnedSelection,string expectedAccount,uint explicitTestSeed)
        {
            var projection=prepared?["carriageOwnership"] as JObject;Fields(projection,"rulesVersion","loadoutRevision","carriages");
            if(Text(projection,"rulesVersion")!="train-loadout-v1"||Number(projection,"loadoutRevision",1)!=Number(currentOwnedSelection,"loadoutRevision",1)||!(projection["carriages"] is JArray rows)||rows.Count!=2)throw new ArgumentException("Car ownership projection mismatch.");
            var owned=currentOwnedSelection?["ownedState"]?["carriages"] as JArray;var selected=currentOwnedSelection?["loadout"]?["carriages"] as JArray;
            if(owned==null||selected==null||selected.Count!=2)throw new ArgumentException();var map=new Dictionary<string,string>(StringComparer.Ordinal);var slots=new HashSet<int>();
            foreach(var row in rows)
            {
                Fields(row as JObject,"carriageId","slot","level","revision","type");var id=Text(row,"carriageId");var type=Text(row,"type");var slot=Number(row,"slot",0,1);
                if((type!="Combat"&&type!="Gun")||map.ContainsKey(id)||!slots.Add(slot))throw new ArgumentException("Invalid car type projection.");
                var facts=owned.Where(x=>(string)x["carriageId"]==id).ToArray();var selection=selected.Where(x=>(string)x["carriageId"]==id).ToArray();
                if(facts.Length!=1||selection.Length!=1||Number(selection[0],"slot")!=slot||Number(facts[0],"level",1)!=Number(row,"level",1)||Number(facts[0],"revision",1)!=Number(row,"revision",1))throw new ArgumentException("Car ownership facts changed.");map.Add(id,type);
            }
            return FromProducerDraftForLocalTest(prepared,currentOwnedSelection,expectedAccount,explicitTestSeed,map);
        }
        // Headless LOCAL TEST factory only. The caller supplies a fresh trusted owned
        // selection; this method does not authenticate, debit, issue a run or attest P/C/S.
        public static BasicCombatSimulation FromProducerDraftForLocalTest(JObject artifact,JObject currentOwnedSelection,string expectedAccount,uint explicitTestSeed,IReadOnlyDictionary<string,string> trustedCarTypes)
        {
            if(artifact==null||currentOwnedSelection==null||trustedCarTypes==null||string.IsNullOrWhiteSpace(expectedAccount)
                ||(string)currentOwnedSelection["accountId"]!=expectedAccount||!JToken.DeepEquals(artifact["ownedSelection"],currentOwnedSelection))throw new ArgumentException("Owner/source revision changed.");
            var draft=artifact["draft"] as JObject;var t=draft?["template"] as JObject;var owned=currentOwnedSelection["ownedState"];
            Fields(draft,"profileId","engineProfileVersion","effectiveStatsVersion","roundingVersion","simulationVersion","template","executionPolicy","bindings","sourceFiles");
            // Opt-in is authenticated by the complete prepared digest in the caller.
            // Never reinterpret an existing accepted/default policy as frontline-first.
            bool generalAuto=Text(draft,"simulationVersion")=="ce03.execution.2026-09-13.003";bool individual=generalAuto||Text(draft,"simulationVersion")=="ce03.execution.2026-09-13.002";bool frontline=individual||Text(draft,"simulationVersion")=="ce03.execution.2026-09-13.001";
            string implementedPolicy=generalAuto?"ce03.execution.2026-09-13.003":individual?"ce03.execution.2026-09-13.002":frontline?"ce03.execution.2026-09-13.001":"ce03.execution.2026-09-11.002";
            string implementedPolicyHash=generalAuto?"266d1e98ef39c5596f0f8cf9f1bd78ce77207c943d1f3192fe7e5b3fd2c9e8d4":individual?"68beb942a695ea761aa0f2aaf5677d7eee603aeeb3de078ffca148a27d824b50":frontline?"08cd3350fde47c5d819836bf6c6be981467e01ea5f814ee9cd56a42e17d0f052":"8914fc4c3a1cfd4f3733ae745e5f42d1457af9beb0a855a2f0f7b7fb75b5f448";
            if(Text(draft,"engineProfileVersion")!="train-combat-test-profile-v1"||Text(draft,"effectiveStatsVersion")!="combat.design.2026-09-09.001"||Text(draft,"roundingVersion")!="ce03-rational-final-floor-v1"
                ||Text(draft,"simulationVersion")!=implementedPolicy||Text(t?["execution"],"policyVersion")!=implementedPolicy||Text(t["execution"],"policySha256")!=implementedPolicyHash
                ||draft["executionPolicy"] is not JObject||Digest(StableJson(draft["executionPolicy"]))!=implementedPolicyHash)throw new ArgumentException("Unsupported execution policy identity.");
            Fields(t,"operation","selectedUltimateOperatorId","operators","train","cars","enemyDefinitions","waves","package","rules","execution","presentation","variants");
            if(Text(draft,"profileId")!="ce03.local.qa.20260911.001"||Text(t?["operation"],"id")!="opn_01_last_departure"||Text(t["operation"],"risk")!="I"||Text(t["operation"],"mode")!="SOLO")throw new ArgumentException("Unsupported local profile.");
            if(Text(currentOwnedSelection,"operationId")!=(string)t["operation"]["id"]||Text(currentOwnedSelection,"risk")!="I")throw new ArgumentException();
            Number(currentOwnedSelection,"formationRevision",1);Number(currentOwnedSelection,"loadoutRevision",1);Number(currentOwnedSelection["ultimateSelection"],"revision");
            var ultimate=individual?NullableText(currentOwnedSelection["ultimateSelection"],"operatorId"):Text(currentOwnedSelection["ultimateSelection"],"operatorId");if(individual?t["selectedUltimateOperatorId"]?.Type!=JTokenType.Null:ultimate!=Text(t,"selectedUltimateOperatorId"))throw new ArgumentException("Ultimate selection changed.");
            var execution=t["execution"];var step=Number(execution,"stepMs",1);var entrance=Number(execution,"entranceMs");
            var executionFields=new[]{"stepMs","clock","crit","dodge","policyVersion","policySha256","entranceMs","frontSlots","rearSlots","queue","noWallClockCatchup","enemyBasicInterruptibleWindup","trainActionChannel","basicLogicalTravel","carriageDamageEvents"};
            Fields(execution as JObject,generalAuto?executionFields.Concat(new[]{"targetingPolicyId","ultimatePolicyId","activePolicyId"}).ToArray():individual?executionFields.Concat(new[]{"targetingPolicyId","ultimatePolicyId"}).ToArray():frontline?executionFields.Concat(new[]{"targetingPolicyId"}).ToArray():executionFields);
            if(individual){if(Text(execution,"ultimatePolicyId")!="ce03.per-operator-ultimate-v1")throw new ArgumentException("Ultimate policy mismatch.");var contract=new Ce03CommandContract(artifact["commandContract"] as JObject);contract.ValidatePrepared(artifact,expectedAccount,contract.Revision);ValidateOperatorSource(draft,"individualUltimatePolicyV1","8372369130f3dfcb5d0445cd9197ced0636468ac5823dca521e49d340aa65b2e");}
            if(generalAuto){if(Text(execution,"activePolicyId")!="ce03.general-active-auto-v1")throw new ArgumentException("Active policy mismatch.");ValidateOperatorSource(draft,"generalActiveAutoPolicyV1","5e10e525742ef06da1c1d8d03ea8949150c8186f24a4994448fe941294b57fa6");}
            if(frontline&&Text(execution,"targetingPolicyId")!=FrontlinePolicyId)throw new ArgumentException("Unsupported targeting policy.");
            if(step!=50||execution["noWallClockCatchup"]?.Type!=JTokenType.Boolean||!(bool)execution["noWallClockCatchup"])throw new ArgumentException("Unsupported fixed-step/catchup policy.");
            if(Text(execution,"clock")!="INTEGER_FIXED_STEP"||Text(execution,"carriageDamageEvents")!="UNSUPPORTED"||Text(execution,"trainActionChannel")!="SHARED_BASIC_AND_MANUAL")throw new ArgumentException("Unsupported execution semantics.");
            foreach(var flag in new[]{"crit","dodge"})if(execution[flag]?.Type!=JTokenType.Boolean||(bool)execution[flag])throw new ArgumentException("Unsupported "+flag);
            if(Text(execution,"basicLogicalTravel")!="DIRECT_IMPACT"||execution["enemyBasicInterruptibleWindup"]?.Type!=JTokenType.Boolean||!(bool)execution["enemyBasicInterruptibleWindup"])throw new ArgumentException("Unsupported impact/interrupt policy.");
            foreach(var feature in new[]{"equipment","research","extraPressure"})if(Text(t["variants"],feature)!="OFF")throw new ArgumentException("Unsupported variant.");
            if(Text(t["variants"],"factionBonuses")!="UNSUPPORTED"||t["operation"]["factionRef"]?.Type!=JTokenType.Null||Text(t["operation"],"factionDisposition")!="UNSUPPORTED_IN_ISOLATED_PROFILE")throw new ArgumentException("Unsupported faction policy.");
            int Ticks(int ms)=>checked((int)(((long)ms+step-1)/step));
            var rules=t["rules"];var shieldCap=Number(rules,"normalShieldCapMaxHpBps",0,10000);var windupBps=Number(rules,"basicWindupRatioBps",0,10000);var recovery=Ticks(Number(rules,"basicRecoveryMs"));
            JArray Array(JToken parent,string key,int count){if(!(parent?[key] is JArray a)||a.Count!=count)throw new ArgumentException("Invalid "+key+" count.");return a;}
            JToken Unique(JArray array,string key,string id){var values=array.Where(x=>(string)x[key]==id).ToArray();if(values.Length!=1)throw new ArgumentException("Missing/duplicate owned "+id);return values[0];}
            var ops=Array(t,"operators",5);var formations=Array(currentOwnedSelection["formation"],"operators",5);var ownedOps=owned?["operators"] as JArray;var cores=owned?["trainingCoreSlots"] as JArray;
            if(ownedOps==null||cores==null)throw new ArgumentException();
            var actors=new List<BasicCombatActor>();
            foreach(var op in ops)
            {
                var id=Text(op,"operatorId");if(!new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}.Contains(id))throw new ArgumentException("Unsupported operator.");
                var slot=Number(op,"slot",0,4);var own=Unique(ownedOps,"operatorId",id);var core=Unique(cores,"operatorId",id);var form=Unique(formations,"operatorId",id);
                if(Number(form,"slot",0,4)!=slot||Number(op,"level",1)!=Number(core,"trainedLevel",1)||Number(op,"promotion")!=Number(own,"promotion")||Number(op,"skillRank",1,5)!=Number(own,"skillTraining",1,5))throw new ArgumentException("Owned operator mismatch.");
                Number(own,"revision",1);Number(core,"revision",1);Number(core,"slot");
                var stats=op["stats"] as JObject;Fields(stats,"maxHp","atk","armorBps","attackMs","reach");var attackMs=Number(stats,"attackMs",1);var period=Ticks(attackMs);var windup=checked((int)(((long)attackMs*windupBps+10000L*step-1)/(10000L*step)));
                var reach=Text(stats,"reach");if(reach!="front"&&reach!="any")throw new ArgumentException();
                actors.Add(new BasicCombatActor(id,false,false,false,slot,Number(stats,"maxHp",1),Number(stats,"atk"),Number(stats,"armorBps",0,10000),period,windup,recovery,BasicCombatTarget.EnemyInterior,shieldCap,new BasicCombatTiming(0,Math.Max(0,period-windup),null,reach=="front",null)));
            }
            if(actors.Select(x=>x.Slot).Distinct().Count()!=5)throw new ArgumentException("Duplicate formation slot.");
            var train=t["train"];var trainId=Text(currentOwnedSelection["loadout"],"trainId");if(trainId!=Text(owned["train"],"trainId")||Number(train,"level",1)!=Number(owned["train"],"level",1))throw new ArgumentException("Owned train mismatch.");Number(owned["train"],"revision",1);
            var ts=train["stats"];var tp=Ticks(Number(ts,"attackMs",1));var tw=checked((int)(((long)Number(ts,"attackMs",1)*windupBps+10000L*step-1)/(10000L*step)));
            actors.Add(new BasicCombatActor(trainId,false,true,false,5,Number(ts,"maxHp",1),Number(ts,"atk"),Number(ts,"armorBps",0,10000),tp,tw,recovery,BasicCombatTarget.EnemyExterior,shieldCap,new BasicCombatTiming(0,Math.Max(0,tp-tw),null,false,null)));
            var cars=Array(t,"cars",2);var loadout=Array(currentOwnedSelection["loadout"],"carriages",2);var ownedCars=owned["carriages"] as JArray;if(ownedCars==null)throw new ArgumentException();
            for(var i=0;i<2;i++)
            {
                var car=cars[i];var kind=i==0?"Combat":"Gun";if(Text(car,"type")!=kind||Number(car,"slot")!=i||Number(loadout[i],"slot")!=i)throw new ArgumentException("Car order mismatch.");
                var id=Text(loadout[i],"carriageId");var own=Unique(ownedCars,"carriageId",id);Number(own,"revision",1);if(Number(own,"level",1)!=Number(car,"level",1))throw new ArgumentException();
                if(!trustedCarTypes.TryGetValue(id,out var ownedType)||ownedType!=kind)throw new ArgumentException("Owned carriage type mismatch.");
                var device=car["device"];if(Text(device,"type")!="DAMAGE"||Text(device,"target")!=(i==0?"enemy_focus_any":"EXTERIOR_ENEMY_FOCUS"))throw new ArgumentException("Unsupported device.");var period=Ticks(Number(device,"periodMs",1));
                actors.Add(new BasicCombatActor(id,false,false,false,6+i,Number(car["stats"],"maxHp",1),Number(device,"amount"),Number(car["stats"],"armorBps",0,10000),period,0,0,i==0?BasicCombatTarget.EnemyInterior:BasicCombatTarget.EnemyExterior,shieldCap,new BasicCombatTiming(0,period,kind,false,null)));
            }
            var definitions=Array(t,"enemyDefinitions",3);var waveRows=Array(t,"waves",15);var waveActors=new List<BasicCombatActor[]>();
            foreach(var wave in waveRows)
            {
                if(Number(wave,"number",1)!=waveActors.Count+1||Text(wave,"completion")!="ALL_REQUIRED_ACTORS_AND_TERMINAL_IMPACTS_RESOLVED"||!(wave["spawns"] is JArray spawns)||spawns.Count==0)throw new ArgumentException("Invalid authored wave.");var list=new List<BasicCombatActor>();
                foreach(var spawn in spawns)
                {
                    var id=Text(spawn,"definitionId");if(!new[]{"enemy_raider_scout","enemy_spike_cart_gunner","enemy_railbike_marauder"}.Contains(id))throw new ArgumentException("Unsupported enemy.");var def=Unique(definitions,"definitionId",id);
                    if(spawn["requiredForCompletion"]?.Type!=JTokenType.Boolean||!(bool)spawn["requiredForCompletion"])throw new ArgumentException();
                    var lane=Text(spawn,"lane");if(lane!="INTERIOR"&&lane!="EXTERIOR")throw new ArgumentException();var exterior=lane=="EXTERIOR";var target=Text(def,"target");
                    if(exterior?target!="train":target!="front_ally"&&target!="rear_ally")throw new ArgumentException("Lane/target mismatch.");
                    var period=Ticks(Number(def,"attackMs",1));var windup=Ticks(Number(def,"windupMs"));if(windup>period)throw new ArgumentException("Unsupported windup interval.");
                    var available=checked(Number(spawn,"atTick")+Ticks(entrance));
                    list.Add(new BasicCombatActor(Text(spawn,"spawnId"),true,false,exterior,Number(spawn,"logicalSlot"),Number(spawn,"resolvedMaxHp",1),Number(def,"atk"),Number(def,"armorBps",0,10000),period,windup,0,exterior?BasicCombatTarget.Train:BasicCombatTarget.Operators,shieldCap,new BasicCombatTiming(available,checked(available+period-windup),null,false,exterior?(bool?)null:frontline||target=="front_ally")));
                }
                waveActors.Add(list.ToArray());
            }
            var front=Array(execution,"frontSlots",2).Select(x=>x.Type==JTokenType.Integer?(int)x:throw new ArgumentException()).ToArray();var rear=Array(execution,"rearSlots",3).Select(x=>x.Type==JTokenType.Integer?(int)x:throw new ArgumentException()).ToArray();
            if(!front.Concat(rear).OrderBy(x=>x).SequenceEqual(new[]{0,1,2,3,4}))throw new ArgumentException("Invalid front/rear mapping.");
            // Caller-supplied TEST seed, never represented as the original server-issued run seed.
            var simulation=new BasicCombatSimulation("LOCAL_TEST_ADAPTER_"+Text(draft,"profileId"),(long)step*10000,explicitTestSeed,actors.ToArray(),waveActors.ToArray());
            foreach(var w in waveRows)foreach(var spawn in (JArray)w["spawns"])simulation.actorVisualIds[Text(spawn,"spawnId")]=Text(spawn,"definitionId");
            simulation.targetingPolicyId=frontline?FrontlinePolicyId:null;
            simulation.frontSlots=front;simulation.waveGapTicks=Ticks(Number(rules,"waveGapMs"));simulation.ConfigureSkills(t,individual?null:ultimate,step,individual);if(individual)simulation.ConfigureLocalOperatorAutomation(generalAuto);simulation.localOwnerBinding=Digest(StableJson(currentOwnedSelection));simulation.LocalOwnerAccount=expectedAccount;return simulation;
        }
    }
}
