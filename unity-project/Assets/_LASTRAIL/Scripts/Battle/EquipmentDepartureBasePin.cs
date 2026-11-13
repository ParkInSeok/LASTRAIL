using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    // Immutable original permanent baseline only. No current ownership lookup,
    // equipment arithmetic, runtime HP/stat mutation or maintenance authority.
    public sealed class EquipmentDepartureBasePin
    {
        public const string Version = "equipment.departure-base-pin.v1.20261005";
        readonly JObject original;
        public string Hash => (string)original["pinHash"];
        public string AccountId => (string)original["origin"]["accountId"];
        public string RunId => (string)original["origin"]["runId"];
        public bool CanProveSafeMaintenance => false;
        public bool CanApplyRuntimeFinalEffects => false;
        EquipmentDepartureBasePin(JObject pin) { original = (JObject)pin.DeepClone(); }
        public JObject Capture() => (JObject)original.DeepClone();
        public JObject ActorBase(string operatorId)
        {
            var actor = ((JArray)original["origin"]["actors"]).SingleOrDefault(a => (string)a["wearerId"] == operatorId);
            if (actor == null) throw new ArgumentException("equipment_departure_target_unbound");
            return (JObject)actor["base"].DeepClone();
        }
        public void RequireSafeMaintenance() => throw new InvalidOperationException("equipment_departure_maintenance_source_unavailable");

        public static EquipmentDepartureBasePin Read(string snapshotJson, string trustedOriginalSnapshotJson,
            string independentPinHash, string expectedAccount, string expectedRun)
        {
            var root = Parse(snapshotJson); var response = Object(root["departureResponse"]);
            var trusted = Parse(trustedOriginalSnapshotJson); var trustedResponse = Object(trusted["departureResponse"]);
            var raw = root["equipmentDeparturePin"];
            if (Missing(raw))
            {
                if (!Missing(response["equipmentDeparturePin"]) || !Missing(trusted["equipmentDeparturePin"]) || !Missing(trustedResponse["equipmentDeparturePin"])) Fail("pin_conflict");
                if (Id(root, "runId") != expectedRun || Id(response, "runId") != expectedRun || Id(trusted, "runId") != expectedRun) Fail("pin_conflict");
                return null; // Legacy absence is unsupported, never current-owned backfill.
            }
            var pin = Object(raw);
            if (Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(pin)) > 512 * 1024) Fail("capacity");
            BDirectAttackInput.Fields(pin, "version", "origin", "originHash", "operationPreviewId", "operationId", "risk", "admissionHash", "source", "sourceHash", "definitions", "definitionHash", "pinHash");
            if (!HashText(independentPinHash) || (string)pin["pinHash"] != independentPinHash ||
                !JToken.DeepEquals(pin, trusted["equipmentDeparturePin"]) || !JToken.DeepEquals(pin, response["equipmentDeparturePin"]) || !JToken.DeepEquals(pin, trustedResponse["equipmentDeparturePin"])) Fail("pin_conflict");
            var body = (JObject)pin.DeepClone(); body.Remove("pinHash");
            if (Digest(body) != independentPinHash || (string)pin["version"] != Version) Fail("pin_conflict");
            foreach (var key in new[] { "origin", "source", "definitions" })
            {
                var hashKey = key == "definitions" ? "definitionHash" : key + "Hash";
                if (!HashText((string)pin[hashKey]) || Digest(pin[key]) != (string)pin[hashKey]) Fail("pin_conflict");
            }
            if (!new[] { "I", "II", "III", "IV", "V" }.Contains(Id(pin, "risk"))) Fail("pin_conflict");
            foreach (var key in new[] { "runId", "operationPreviewId", "operationId", "risk", "admission" })
            {
                if (!JToken.DeepEquals(root[key], response[key]) || !JToken.DeepEquals(root[key], trusted[key]) || !JToken.DeepEquals(root[key], trustedResponse[key])) Fail("pin_conflict");
                if (key != "runId" && key != "admission" && Id(pin, key) != Id(root, key)) Fail("pin_conflict");
            }
            if (Id(root, "runId") != expectedRun || !(root["admission"] is JObject) || Digest(root["admission"]) != (string)pin["admissionHash"]) Fail("pin_conflict");
            var origin = Object(pin["origin"]);
            BDirectAttackInput.Fields(origin, "version", "accountId", "runId", "stage", "actors");
            if ((string)origin["version"] != RunEquipmentStatSource.Version || Id(origin, "accountId") != expectedAccount || Id(origin, "runId") != expectedRun ||
                (string)origin["stage"] != "ORIGINAL_PRE_EQUIPMENT_RUN_BASE") Fail("pin_conflict");
            var actors = origin["actors"] as JArray;
            if (actors == null || actors.Count != 5) Fail("selection_conflict");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in actors)
            {
                var actor = Object(token); BDirectAttackInput.Fields(actor, "wearerKind", "wearerId", "targetId", "base");
                if ((string)actor["wearerKind"] != "operator" || Id(actor, "targetId") != Id(actor, "wearerId") || !ids.Add(Id(actor, "wearerId"))) Fail("selection_conflict");
                var basis = Object(actor["base"]); BDirectAttackInput.Fields(basis, "maxHp", "attack", "defense");
                BDirectAttackInput.Nat(basis, "maxHp", 1); CombatExactValue.From(Object(basis["attack"])); CombatExactValue.From(Object(basis["defense"]));
            }
            // These are original producer snapshots, not independently executable
            // profile/definition inputs. Preserve and check lineage without redoing growth.
            var source = Object(pin["source"]); var definitions = Object(pin["definitions"]);
            BDirectAttackInput.Fields(source, "ownedProfile", "namedSource", "commanderSnapshot", "stage", "targetIdentity", "runtimeSupported");
            BDirectAttackInput.Fields(definitions, "ownedBase", "policy", "named");
            var profile = Object(source["ownedProfile"]);
            if ((string)source["stage"] != "VERIFIED_PRE_EQUIPMENT_PERMANENT_BASE" || (string)source["targetIdentity"] != "ORIGINAL_SELECTED_OPERATOR_ACTOR_ID" ||
                source["runtimeSupported"]?.Type != JTokenType.Boolean || (bool)source["runtimeSupported"] || Id(profile, "accountId") != expectedAccount ||
                (string)profile["definitionHash"] != "sha256:" + Digest(Object(definitions["ownedBase"])) ||
                (string)profile["policyHash"] != "sha256:" + Digest(Object(definitions["policy"]))) Fail("pin_conflict");
            var selected = profile["operators"] as JArray;
            if (selected == null || selected.Count != actors.Count || !selected.Select(p => Id(p, "operatorId")).SequenceEqual(actors.Select(a => Id(a, "wearerId")))) Fail("selection_conflict");
            return new EquipmentDepartureBasePin(pin);
        }
        static bool Missing(JToken x) => x == null || x.Type == JTokenType.Null;
        static bool HashText(string x) => Regex.IsMatch(x ?? "", "^[a-f0-9]{64}$");
        static string Digest(JToken x) => BDirectAttackInput.Digest(x).Substring(7);
        static JObject Object(JToken x) => x as JObject ?? throw new ArgumentException("equipment_departure_context_invalid");
        static string Id(JToken x, string key) { var s = BDirectAttackInput.Text(x, key); if (!Regex.IsMatch(s, "^[\x21-\x7e]{1,128}$")) Fail("context_invalid"); return s; }
        static void Fail(string code) => throw new ArgumentException("equipment_departure_" + code);
        static JObject Parse(string json)
        {
            if (json == null || Encoding.UTF8.GetByteCount(json) > 24 * 1024 * 1024) { Fail("context_invalid"); }
            using (var reader = new StrictReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, MaxDepth = 64 })
            {
                var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
                if (reader.Read() || root.Descendants().Any(t => t.Type == JTokenType.Comment)) Fail("context_invalid");
                return root;
            }
        }
        // JObject.Load may discard comments between properties; reject while reading
        // tokens so that post-load inspection cannot silently miss them.
        sealed class StrictReader : JsonTextReader
        {
            internal StrictReader(TextReader input) : base(input) { }
            public override bool Read()
            {
                var more = base.Read();
                if (more && TokenType == JsonToken.Comment) Fail("context_invalid");
                return more;
            }
        }
    }
}
