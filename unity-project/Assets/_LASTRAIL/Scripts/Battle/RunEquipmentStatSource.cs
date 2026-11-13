using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    // An immutable precommit proposal. The server owns equipment arithmetic and SQL
    // commit authority; this boundary owns no placement, health or native stat state.
    public sealed class RunEquipmentStatSource
    {
        public const string Version = "equipment.run-hp-atk-def.v1.20261005";
        public const string ProposedPhase = "PROPOSED_ASSIGNMENT_NOT_COMMITTED";
        readonly JObject original;
        public string Hash { get; }
        public string AccountId => (string)original["accountId"];
        public string RunId => (string)original["runId"];
        public long InventoryRevision => (long)original["inventoryRevision"];
        public bool CanApplyToNative => false;

        RunEquipmentStatSource(JObject packet, string hash) { original = (JObject)packet.DeepClone(); Hash = hash; }
        public JObject Capture() => (JObject)original.DeepClone();
        public JObject ActorProposal(string kind, string wearerId)
        {
            var actor = ((JArray)original["actors"]).SingleOrDefault(a => (string)a["wearerKind"] == kind && (string)a["wearerId"] == wearerId);
            if (actor == null) throw new ArgumentException("equipment_stat_wearer_not_bound");
            return (JObject)actor.DeepClone();
        }
        public void RequireCommittedRuntimeSource() => throw new InvalidOperationException("equipment_stat_proposed_assignment_not_committed");

        public static RunEquipmentStatSource Read(JObject input, JObject trustedOriginal, string pinnedHash,
            string expectedAccount, string expectedRun, string expectedOriginalContextHash, long expectedInventoryRevision)
        {
            if (input == null || trustedOriginal == null) throw new ArgumentException("equipment_stat_source_missing");
            if (Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(input)) > 512 * 1024) throw new ArgumentException("equipment_stat_capacity");
            BDirectAttackInput.Fields(input, "version", "accountId", "runId", "originalContext", "originalContextHash", "inventoryRevision", "phase", "actors", "fullRuntimeSupported", "packetHash");
            var body = (JObject)input.DeepClone(); body.Remove("packetHash");
            if (!HashText(pinnedHash) || (string)input["packetHash"] != pinnedHash || (string)trustedOriginal["packetHash"] != pinnedHash ||
                BDirectAttackInput.Digest(body) != "sha256:" + pinnedHash || !JToken.DeepEquals(input, trustedOriginal))
                throw new ArgumentException("equipment_stat_original_snapshot_conflict");
            if ((string)input["version"] != Version || (string)input["phase"] != ProposedPhase ||
                input["fullRuntimeSupported"]?.Type != JTokenType.Boolean || (bool)input["fullRuntimeSupported"])
                throw new ArgumentException("equipment_stat_version_or_phase_conflict");
            if (Id(input, "accountId") != expectedAccount || Id(input, "runId") != expectedRun ||
                BDirectAttackInput.Nat(input, "inventoryRevision", 1) != expectedInventoryRevision)
                throw new ArgumentException("equipment_stat_owner_or_revision_conflict");
            var context = Object(input["originalContext"]);
            BDirectAttackInput.Fields(context, "version", "accountId", "runId", "stage", "actors");
            if ((string)context["version"] != Version || (string)context["accountId"] != expectedAccount || (string)context["runId"] != expectedRun ||
                (string)context["stage"] != "ORIGINAL_PRE_EQUIPMENT_RUN_BASE" || !HashText(expectedOriginalContextHash) ||
                (string)input["originalContextHash"] != expectedOriginalContextHash || BDirectAttackInput.Digest(context) != "sha256:" + expectedOriginalContextHash)
                throw new ArgumentException("equipment_stat_original_context_invalid");
            var origins = new Dictionary<string, JObject>(StringComparer.Ordinal); var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in Array(context["actors"], 1, 1000))
            {
                var actor = Object(token); BDirectAttackInput.Fields(actor, "wearerKind", "wearerId", "targetId", "base");
                var key = ActorKey(actor); if (origins.ContainsKey(key) || !targets.Add(Id(actor, "targetId"))) throw new ArgumentException("equipment_stat_duplicate_owner");
                Base(Object(actor["base"])); origins.Add(key, actor);
            }
            var seenActors = new HashSet<string>(StringComparer.Ordinal); var seenItems = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in Array(input["actors"], 1, 2))
            {
                var actor = Object(token); BDirectAttackInput.Fields(actor, "wearerKind", "wearerId", "targetId", "base", "slots", "sources", "stats");
                var key = ActorKey(actor);
                if (!seenActors.Add(key) || !origins.TryGetValue(key, out var origin) || Id(actor, "targetId") != (string)origin["targetId"] || !JToken.DeepEquals(actor["base"], origin["base"]))
                    throw new ArgumentException("equipment_stat_wearer_not_bound");
                var slots = Array(actor["slots"], 3, 3); var sources = Array(actor["sources"], 0, 3); int sourceIndex = 0;
                foreach (var slot in slots)
                {
                    if (slot.Type == JTokenType.Null) continue;
                    var item = Object(slot); BDirectAttackInput.Fields(item, "instanceId", "definitionId", "definitionVersion", "categoryId", "setTag", "star");
                    var instance = Id(item, "instanceId"); Id(item, "definitionId");
                    if (!seenItems.Add(instance)) throw new ArgumentException("equipment_stat_duplicate_instance");
                    if ((string)item["definitionVersion"] != Version) throw new ArgumentException("equipment_stat_definition_unsupported");
                    if (!new[] { "HP", "ATK", "DEF" }.Contains((string)item["categoryId"]) || !Regex.IsMatch((string)item["setTag"] ?? "", "^SET0[1-6]$"))
                        throw new ArgumentException("equipment_stat_category_or_tag_unsupported");
                    long star = BDirectAttackInput.Nat(item, "star", 1, 5);
                    if (sourceIndex >= sources.Count) throw new ArgumentException("equipment_stat_effect_source_missing");
                    var source = Object(sources[sourceIndex++]); BDirectAttackInput.Fields(source, "instanceId", "instanceRevision", "effectRevision", "snapshot");
                    if (Id(source, "instanceId") != instance) throw new ArgumentException("equipment_stat_effect_changed");
                    BDirectAttackInput.Nat(source, "instanceRevision", 1); BDirectAttackInput.Nat(source, "effectRevision", 1);
                    var snapshot = Object(source["snapshot"]); BDirectAttackInput.Fields(snapshot, "version", "equipment", "uniqueEffects", "enchantment");
                    var equipment = Object(snapshot["equipment"]); BDirectAttackInput.Fields(equipment, "definitionId", "definitionVersion", "star");
                    if ((string)snapshot["version"] != "equipment.effect-state.v1" || !JToken.DeepEquals(equipment["definitionId"], item["definitionId"]) || !JToken.DeepEquals(equipment["definitionVersion"], item["definitionVersion"]) || !JToken.DeepEquals(equipment["star"], item["star"]))
                        throw new ArgumentException("equipment_stat_effect_changed");
                    // A valid high-star calculation alone supplies no unique-effect handler.
                    if (star > 2 || !(snapshot["uniqueEffects"] is JArray unique) || unique.Count != 0 || snapshot["enchantment"]?.Type != JTokenType.Null)
                        throw new ArgumentException("equipment_stat_effect_handler_unavailable");
                }
                if (sourceIndex != sources.Count) throw new ArgumentException("equipment_stat_effect_changed");
                Stats(Object(actor["stats"]));
            }
            return new RunEquipmentStatSource(input, pinnedHash);
        }

        static bool HashText(string text) => Regex.IsMatch(text ?? "", "^[a-f0-9]{64}$");
        static JObject Object(JToken token) => token as JObject ?? throw new ArgumentException("equipment_stat_object_invalid");
        static JArray Array(JToken token, int min, int max) { if (!(token is JArray array) || array.Count < min || array.Count > max) throw new ArgumentException("equipment_stat_array_invalid"); return array; }
        static string Id(JToken token, string key) { var id = BDirectAttackInput.Text(token, key); if (!Regex.IsMatch(id, "^[\x21-\x7e]{1,128}$")) throw new ArgumentException("equipment_stat_id_invalid"); return id; }
        static string ActorKey(JObject actor) { var kind = Id(actor, "wearerKind"); if (kind != "operator" && kind != "turret") throw new ArgumentException("equipment_stat_wearer_kind_invalid"); return kind + ":" + Id(actor, "wearerId"); }
        static void Base(JObject basis) { BDirectAttackInput.Fields(basis, "maxHp", "attack", "defense"); BDirectAttackInput.Nat(basis, "maxHp", 1); CombatExactValue.From(Object(basis["attack"])); CombatExactValue.From(Object(basis["defense"])); }
        static void Stats(JObject stats)
        {
            BDirectAttackInput.Fields(stats, "maxHp", "exactMaxHp", "attack", "defense", "additiveBps", "rounding");
            long hp = BDirectAttackInput.Nat(stats, "maxHp", 1);
            if (CombatExactValue.From(Object(stats["exactMaxHp"])).Floor() != hp || (string)stats["rounding"] != "HP_INTEGER_FLOOR_AT_HP_BOUNDARY_ATK_DEF_EXACT_UNTIL_ORIGINAL_DAMAGE_OWNER")
                throw new ArgumentException("equipment_stat_exact_hp_invalid");
            CombatExactValue.From(Object(stats["attack"])); CombatExactValue.From(Object(stats["defense"]));
            var group = Object(stats["additiveBps"]); BDirectAttackInput.Fields(group, "HP", "ATK", "DEF");
            foreach (var category in new[] { "HP", "ATK", "DEF" }) BDirectAttackInput.Nat(group, category);
        }
    }
}
