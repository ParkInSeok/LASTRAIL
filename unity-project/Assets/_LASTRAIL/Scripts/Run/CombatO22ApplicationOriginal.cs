using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Run
{
    // Validated acquisition original, NOT runtime APPLIED or permission to start.
    // The authenticated selection receiver/checkpoint owner supplies trust scope.
    public sealed class CombatO22ApplicationOriginal
    {
        public const string Version="combat.o22-application-original.v1.20261019";
        public const string CatalogVersion=CombatUtilityCatalogVersions.Legacy;
        public const string CurrentCatalogVersion=CombatUtilityCatalogVersions.Current;
        readonly string original;
        public string ApplicationId { get; }
        public string AccountId { get; }
        public string RunId { get; }
        public string GrantEventId { get; }
        public string Environment { get; }
        public string StartSnapshotHash { get; }
        public string DefinitionPacketHash { get; }
        public string SelectionJson { get; }
        public bool Applied=>false;
        CombatO22ApplicationOriginal(JObject p,JObject d,JObject s)
        {
            original=BasicCombatSimulation.StableJson(p);ApplicationId=(string)p["applicationId"];
            AccountId=(string)p["accountId"];RunId=(string)p["runId"];GrantEventId=(string)p["grantEventId"];Environment=(string)p["environment"];
            StartSnapshotHash=(string)d["startSnapshotHash"];DefinitionPacketHash=(string)d["packetHash"];SelectionJson=BasicCombatSimulation.StableJson(s["selection"]);
        }
        public string Capture()=>original;
        static void Need(bool valid){if(!valid)throw new FormatException("O22 original binding invalid");}
        static JObject Obj(JToken t)=>t as JObject??throw new FormatException("O22 object required");
        static string Id(JToken t){Need(t?.Type==JTokenType.String);string s=(string)t;Need(!string.IsNullOrWhiteSpace(s)&&s.Length<=200);return s;}
        static string Hash(JToken t){string s=Id(t);Need(Regex.IsMatch(s,"^[a-f0-9]{64}$"));return s;}
        static void Keys(JObject o,params string[] keys)=>Need(o.Count==keys.Length&&o.Properties().All(p=>keys.Contains(p.Name)));
        static void False(JObject o,params string[] keys){foreach(string k in keys)Need(o[k]?.Type==JTokenType.Boolean&&!(bool)o[k]);}
        static void Pin(JObject o){string hash=Hash(o["packetHash"]);var body=(JObject)o.DeepClone();body.Remove("packetHash");Need(BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(body))==hash);}
        public static CombatO22ApplicationOriginal Read(string json,string account,string run,string grant,string environment,string expectedStartHash)
        {
            if(json==null||json.Length>2*1024*1024)throw new FormatException("O22 original capacity");
            foreach(string id in new[]{account,run,grant,environment})Id(new JValue(id));Hash(new JValue(expectedStartHash));
            JObject p;using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,MaxDepth=48}){
                p=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load});
                Need(!reader.Read()&&!p.Descendants().Any(t=>t.Type==JTokenType.Comment));
            }
            Keys(p,"version","applicationId","accountId","runId","grantEventId","environment","effectId","status","combatOwner","applied","definition");
            Need((string)p["version"]==Version&&(string)p["accountId"]==account&&(string)p["runId"]==run&&(string)p["grantEventId"]==grant&&(string)p["environment"]==environment&&(string)p["effectId"]=="O22"&&(string)p["status"]=="SERVER_CONFIRMED_RUNTIME_APPLY_REQUIRED"&&(string)p["combatOwner"]=="UNITY_LOCAL");False(p,"applied");
            var d=Obj(p["definition"]);Pin(d);
            Need((string)d["version"]=="combat.utility-selection-definition.v1.20261019"&&(string)d["bindingStatus"]=="CONFIRMED_SELECTION_DEFINITION_ONLY");False(d,"effectOwnershipEstablished","applied","runtimeSupported","canStart");
            var s=Obj(d["selectionSource"]);Pin(s);
            Need((string)s["version"]=="combat.augment-selection-source.v1.20261016"&&(string)s["sourceStatus"]=="ORIGINAL_GRANT_SELECTION_NOT_EFFECT_OWNERSHIP"&&s["effectBinding"]?.Type==JTokenType.Null);False(s,"effectOwnershipEstablished","runtimeSupported","canStart");
            foreach(var node in new[]{d,s})Need((string)node["accountId"]==account&&(string)node["runId"]==run&&(string)node["eventId"]==grant&&(string)node["environment"]==environment&&Hash(node["startSnapshotHash"])==expectedStartHash);
            Hash(s["issueHash"]);Hash(s["checkpointHash"]);Need(s["storeRevision"]?.Type==JTokenType.Integer&&(long)s["storeRevision"]>0&&(long)s["storeRevision"]<=9007199254740991L);
            var catalog=Obj(d["definitionCatalog"]);string catalogVersion=(string)catalog["version"];Need(CombatUtilityCatalogVersions.Supports(catalogVersion)&&(string)s["catalogVersion"]==catalogVersion);Hash(catalog["definitionHash"]);
            if(catalogVersion==CurrentCatalogVersion){Need((string)catalog["definitionHash"]==CombatUtilityCatalogVersions.CurrentCatalogHash);CombatUtilityCatalogVersions.RequireCurrentDefinition(Obj(d["selectedDefinition"]));}
            var selection=Obj(s["selection"]);Need((string)selection["state"]=="CONFIRMED"&&(string)selection["selectedCandidateId"]=="O22"&&(string)selection["runId"]==run&&(string)selection["eventId"]==grant&&(string)selection["stageId"]==Id(s["stageId"])&&(string)selection["catalogVersion"]==catalogVersion);
            Need(selection["grantRevision"]?.Type==JTokenType.Integer&&(long)selection["grantRevision"]>0&&selection["slots"] is JArray slots&&slots.Count>=1&&slots.Count<=3&&slots.Count(x=>(string)x["candidateId"]=="O22")==1);
            var selected=Obj(d["selectedDefinition"]);var rules=Obj(selected["rules"]);Need((string)selected["id"]=="O22"&&rules["damageToHealBps"]?.Type==JTokenType.Integer&&(int)rules["damageToHealBps"]==1200&&(string)rules["eligible"]=="ALL_CONFIRMED_ENEMY_HP_DAMAGE_ATTRIBUTED_TO_THIS_OPERATOR"&&rules["addedRecoveryCap"]?.Type==JTokenType.Null&&rules["internalCooldownMs"]?.Type==JTokenType.Integer&&(int)rules["internalCooldownMs"]==0);
            var request=new JObject{["accountId"]=account,["runId"]=run,["eventId"]=grant,["environment"]=environment,["expectedStoreRevision"]=s["storeRevision"],["expectedCatalogVersion"]=catalogVersion,["expectedStartSnapshotHash"]=expectedStartHash};
            Need((string)p["applicationId"]=="o22:"+BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(request)));
            return new CombatO22ApplicationOriginal(p,d,s);
        }
    }
}
