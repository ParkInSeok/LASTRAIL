#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    // Explicit isolated first-test composition. Server packets remain unchanged.
    // Train/carriage HP and DEF are borrowed LOCAL_REFERENCE test values, never game bases.
    // Paths and poses below are declared judgement fixtures, never operating scene evidence.
    internal static class BMiraFirstNativeFixture
    {
        const string Root="D:/Project/LASTRAIL/";
        const string Unified="QA/MiraFirstStatic-20261015/NativeSource/UnifiedNative/";
        static JObject Read(string path)=>JObject.Parse(File.ReadAllText(Root+path));
        static JObject Entry()=>(JObject)Read(Unified+"server-packets.json")["packets"]["grown"].DeepClone();
        static JObject NodeEntry()=>(JObject)Read(Unified+"node-sources.json")["packets"]["grown"].DeepClone();
        static void Need(bool condition,string reason){if(!condition)throw new InvalidOperationException("mira_first_native_fixture_"+reason);}
        static JObject Ratio(long numerator)=>new JObject{["numerator"]=numerator.ToString(System.Globalization.CultureInfo.InvariantCulture),["denominator"]="1"};
        static JArray Cars(JObject entry)=>(JArray)entry["originalMira"]["refit"]["departure"]["cars"];
        static JObject MiraPlacement(JObject entry)=>(JObject)entry["originalMira"]["actor"]["placement"];
        static void RequireIdentity(JObject first,JObject node)
        {
            Need((string)first["packet"]["accountId"]==(string)node["trust"]["origin"]["accountId"]
                &&(string)first["packet"]["runId"]==(string)node["trust"]["origin"]["runId"],"account_run");
            Need(JToken.DeepEquals(first["originalSource"],node["originalSource"])
                &&JToken.DeepEquals(first["boardingSource"],node["boardingSource"]),"companion");
        }
        public static BMiraFirstTestSource Source()
        {
            var e=Entry();RequireIdentity(e,NodeEntry());var packet=(JObject)e["packet"];var mira=(JObject)e["originalMira"];
            return BMiraFirstTestSource.Read((JObject)packet.DeepClone(),packet,(string)packet["packetHash"],
                (string)packet["accountId"],(string)packet["runId"],mira,(string)mira["packetHash"],(string)e["departurePin"]["pinHash"]);
        }
        public static BBoardingEventsNodeSnapshot Node()
        {
            var e=NodeEntry();RequireIdentity(Entry(),e);var packet=(JObject)e["boardingSource"];
            return BBoardingEventsNodeSnapshot.Read(packet,(string)packet["sourceHash"],(JObject)e["trust"]);
        }
        public static string TargetId=>Node().OriginalSource.Waves[0].Instances[0].InstanceId;

        static void EmptyModifiers(JObject entity)
        {
            foreach(var name in new[]{"attackSources","defenseSources","effects","weakening","penetration"})entity[name]=new JArray();
            entity["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()};
        }
        static BCarriageBuffDefinitions Definitions()
        {
            string json=File.ReadAllText(Root+"QA/CombatBMigration-20261002/BuffConsumer/original-definitions.json");
            using var sha=SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-","").ToLowerInvariant();
            return new BCarriageBuffDefinitions(json,hash);
        }
        public static BDirectAttackBasicLoopBinding Binding()
        {
            var entry=Entry();var source=Node().OriginalSource;Source();
            var reference=Read("QA/CombatBMigration-20261002/AttackConsumer/local-original-input.json");
            var referenceEntities=(JArray)reference["entities"];
            var cars=Cars(entry).OrderBy(c=>(int)c["slot"]).ToArray();
            Need(cars.Length==5&&cars.Where(c=>(string)c["occupant"]["kind"]=="OPERATOR").Count()==1
                &&(string)cars[1]["occupant"]["operatorId"]=="op_mira","mira_only_placement");
            string trainId=(string)reference["formation"]["trainId"];
            var formation=new BCombatFormation(trainId,BCombatMode.Solo,BCombatBattlefield.ExternalVertical,
                cars.Select(c=>new BCombatSlot((string)c["instanceId"],(string)c["occupant"]["kind"]=="OPERATOR"?(string)c["occupant"]["operatorId"]:null)));
            var entities=new JArray(referenceEntities.Single(e=>(string)e["kind"]=="TRAIN").DeepClone());
            var carReference=referenceEntities.Single(e=>(string)e["kind"]=="CARRIAGE");
            foreach(var car in cars)
            {
                var entity=(JObject)carReference.DeepClone();entity["id"]=car["instanceId"].DeepClone();
                entity["definitionId"]="LOCAL_REFERENCE_"+(string)car["instanceId"];entity["slot"]=car["slot"].DeepClone();entities.Add(entity);
            }
            var placement=MiraPlacement(entry);var stats=entry["originalMira"]["actor"]["owned"]["permanentStats"];
            var mira=(JObject)referenceEntities.Single(e=>(string)e["id"]=="op_mira").DeepClone();
            mira["slot"]=placement["physicalSlot"].DeepClone();mira["maxHp"]=stats["maxHp"].DeepClone();
            mira["attack"]=stats["attack"].DeepClone();mira["defense"]=Ratio((long)stats["defense"]);
            mira["basicIntervalMicroseconds"]=checked((long)stats["basicIntervalMs"]*1000);EmptyModifiers(mira);entities.Add(mira);
            var nodePacket=source.Capture();var types=(JArray)nodePacket["definition"]["types"];
            var enemyReference=referenceEntities.First(e=>(string)e["kind"]=="LOCAL_ENEMY");
            foreach(var instance in source.Waves.SelectMany(w=>w.Instances))
            {
                var definition=types.Single(t=>(string)t["definitionId"]==instance.DefinitionId);
                var enemy=(JObject)enemyReference.DeepClone();enemy["id"]=instance.InstanceId;enemy["definitionId"]=instance.DefinitionId;
                enemy["slot"]=placement["physicalSlot"].DeepClone();enemy["maxHp"]=checked((int)instance.Hp);
                enemy["attack"]=definition["rawAction"]["value"].DeepClone();enemy["defense"]=Ratio((long)definition["defense"]);
                enemy["basicIntervalMicroseconds"]=checked((long)definition["basicPeriodMs"]*1000);
                enemy["allowedTargets"]=new JArray("OPERATOR");EmptyModifiers(enemy);entities.Add(enemy);
            }
            var owned=(JObject)entry["originalMira"]["ownedProfile"].DeepClone();
            var definitionIndex=CombatDefinitionIntake.Read(Read("QA/CommanderS1-20260922/c3-definition-intake-20261001.json"));
            var inputPacket=new JObject{
                ["version"]=BDirectAttackInput.WaveRuntimeVersion,["provenance"]="LOCAL_VERIFIED_WAVE_SOURCE_NOT_OPERATING_START",
                ["formation"]=formation.Capture(),["ownedProfileHash"]=BDirectAttackInput.Digest(owned),["definitionHash"]=definitionIndex.ContentHash,
                ["stepTimeSpanTicks"]=100000,["entities"]=entities,["geometry"]=new JArray(),["initialShields"]=new JArray(),
                ["sourceEvidence"]=new JArray(
                    new JObject{["sourceId"]="LOCAL_REFERENCE_TRAIN_CARRIAGE_HP_DEF_ONLY_NOT_GAME_BASE",["sha256"]=BDirectAttackInput.Digest(reference).Substring(7)},
                    new JObject{["sourceId"]="ACTUAL_UNIFIED_MIRA_ORIGINAL",["sha256"]=entry["originalMira"]["packetHash"].DeepClone()},
                    new JObject{["sourceId"]="ACTUAL_UNIFIED_SCOUT_GUNNER_NODE",["sha256"]=nodePacket["sourceHash"].DeepClone()}),
                ["waveSourceHash"]=nodePacket["sourceHash"].DeepClone()};
            var input=BDirectAttackInput.ReadWaveRuntime(inputPacket,BDirectAttackInput.Digest(inputPacket),formation,owned,BDirectAttackInput.Digest(owned),definitionIndex,source);
            var definitions=Definitions();var departure=(JObject)entry["originalMira"]["refit"]["departure"].DeepClone();
            var buff=new JObject{
                ["version"]=BDirectAttackBuffBinding.WaveVersion,["provenance"]="BOUNDED_LOCAL_NOT_FULL_RUN",["attackInputHash"]=input.OriginalHash,
                ["definitionHash"]=definitions.Hash,["departure"]=departure,
                ["initial"]=new JObject{["contextId"]=source.NativeContextId,["mode"]="EXTERIOR",
                    ["cars"]=new JArray(cars.Select(c=>new JObject{["slot"]=c["slot"].DeepClone(),["body"]="NORMAL",["actor"]=(string)c["occupant"]["kind"]=="CARGO"?"ABSENT":"UP"}))},
                ["targetCategories"]=new JArray(source.Waves.SelectMany(w=>w.Instances).Select(i=>new JObject{["targetId"]=i.InstanceId,["category"]="ENEMY"}))};
            var composition=new BDirectAttackBuffBinding(input,definitions,buff,BDirectAttackInput.Digest(buff));
            var routes=new JObject{["version"]=BDirectAttackBasicLoopBinding.WaveVersion,["provenance"]="BOUNDED_LOCAL_NOT_FULL_RUN",["compositionHash"]=composition.OriginalHash,
                ["targets"]=new JArray(source.Waves.SelectMany(w=>w.Instances).Select(i=>new JObject{["sourceId"]=i.InstanceId,["targetId"]="op_mira"})
                    .Concat(new[]{new JObject{["sourceId"]="op_mira",["targetId"]=source.Waves[0].Instances[0].InstanceId}}))};
            return new BDirectAttackBasicLoopBinding(composition,routes,BDirectAttackInput.Digest(routes));
        }
        public static BWaveBoardingLocalPaths Paths(BDirectAttackInput input)
        {
            if(input==null)throw new ArgumentNullException(nameof(input));var placement=MiraPlacement(Entry());int slot=(int)placement["physicalSlot"];
            Need(input.Formation.Slots[slot].CarriageId==(string)placement["carriageInstanceId"],"path_original_car");
            // Same connected graph fixture as BWaveBoardingRuntimeTests.Side; no runtime path claim.
            JObject Side(string id)=>new JObject{["socketId"]=id,["approachPathMm"]=new JArray(new JArray(0,0,0),new JArray(100,0,0)),["climbPathMm"]=new JArray(new JArray(100,0,0),new JArray(100,100,0))};
            var p=new JObject{["version"]=BWaveBoardingLocalPaths.Version,["provenance"]="EXPLICIT_LOCAL_GRAPH_FIXTURE_NOT_ORDINARY_RUNTIME",["inputHash"]=input.OriginalHash,
                ["carriages"]=new JArray(new JObject{["carriageId"]=placement["carriageInstanceId"].DeepClone(),["slot"]=slot,["left"]=Side("MIRA_FIRST_LEFT_1"),["right"]=Side("MIRA_FIRST_RIGHT_1")})};
            return BWaveBoardingLocalPaths.Read(p,BDirectAttackInput.Digest(p),input);
        }
        public static MiraJudgementSceneAdapter Scene()
        {
            var entry=Entry();var source=Node().OriginalSource;var placement=MiraPlacement(entry);int slot=(int)placement["physicalSlot"];
            var profile=JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"_LASTRAIL/Data/Combat/MiraJudgementProfileV1.json")));
            // Author explicit approved judgement poses; never fill a Server geometry:null field.
            var layout=new JObject{["version"]="mira.judgement-layout.v1.20261015",["provenance"]="EXPLICIT_ISOLATED_FIRST_TEST_LAYOUT_NOT_OWNED_PLACEMENT",
                ["source"]=new JObject{["id"]="op_mira",["generation"]=1,["slot"]=slot,["feetMm"]=new JArray(slot*12800,2400,0),["yawDegrees"]=90,["domain"]="FIRST_TEST_EXTERNAL"},
                ["cars"]=new JArray(Cars(entry).OrderBy(c=>(int)c["slot"]).Select(c=>new JObject{["id"]=c["instanceId"].DeepClone(),["slot"]=c["slot"].DeepClone(),["centerMm"]=new JArray((int)c["slot"]*12800,0,0)})),
                ["targets"]=new JArray(source.Waves.SelectMany(w=>w.Instances).Select((instance,ordinal)=>new JObject{
                    ["id"]=instance.InstanceId,["definitionId"]=instance.DefinitionId,["generation"]=0,["slot"]=slot,
                    ["feetMm"]=new JArray(slot*12800+(ordinal%2)*2000,2400,6000+(ordinal/2)*2000),["eligible"]=false,["domain"]="FIRST_TEST_EXTERNAL"}))};
            // Caller owns the created scene lifetime; native owner projects actual boarding generations/eligibility.
            return MiraJudgementSceneAdapter.Create(profile,BDirectAttackInput.Digest(profile),layout,BDirectAttackInput.Digest(layout));
        }
    }
}
#endif