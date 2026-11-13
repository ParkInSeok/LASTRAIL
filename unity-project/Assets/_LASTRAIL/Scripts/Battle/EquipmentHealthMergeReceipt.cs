using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    // Captures an original linked receipt. No equipment commit, HP event, clock or
    // ATK/DEF journal is executed here. Run/account binding is external to the wire
    // receipt and is verified through its original context and request-bound event IDs.
    public sealed class EquipmentHealthMergeReceipt
    {
        public const string Version = "equipment.health-merge.v1.20261005";
        readonly JObject original;
        public string Hash { get; }
        public string RequestHash { get; }
        public string AccountId { get; }
        public string RunId { get; }
        public bool IsActualHeal => false;
        public bool HasNativeAttackDefenseJournal => false;

        EquipmentHealthMergeReceipt(JObject receipt, string hash, string requestHash, RunEquipmentStatSource context)
        { original = (JObject)receipt.DeepClone(); Hash = hash; RequestHash = requestHash; AccountId = context.AccountId; RunId = context.RunId; }
        public JObject Capture() => (JObject)original.DeepClone();
        public JObject HealthCheckpoint(string targetId)
        {
            var health = ((JArray)original["health"]).SingleOrDefault(h => (string)h["targetId"] == targetId);
            if (health == null) throw new ArgumentException("equipment_health_merge_target_unbound");
            return (JObject)health["after"].DeepClone();
        }
        public void RequireNativeAttackDefenseJournal() => throw new InvalidOperationException("equipment_health_merge_native_stat_journal_unavailable");

        public static EquipmentHealthMergeReceipt Read(JObject input, JObject trustedOriginal, string responseHash,
            JObject originalCommand, string requestHash, RunEquipmentStatSource context, string expectedAccount, string expectedRun)
        {
            if (input == null || trustedOriginal == null || originalCommand == null || context == null) throw new ArgumentException("equipment_health_merge_source_missing");
            if (Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(input)) > 512 * 1024) throw new ArgumentException("equipment_health_merge_capacity");
            if (!HashText(responseHash) || BDirectAttackInput.Digest(input) != "sha256:" + responseHash || !JToken.DeepEquals(input, trustedOriginal) ||
                !HashText(requestHash) || BDirectAttackInput.Digest(originalCommand) != "sha256:" + requestHash)
                throw new ArgumentException("equipment_health_merge_original_conflict");
            if (context.AccountId != expectedAccount || context.RunId != expectedRun) throw new ArgumentException("equipment_health_merge_owner_conflict");
            BDirectAttackInput.Fields(originalCommand, "version", "merge", "expectedHealthRevision", "expectedStatVersion");
            var command = Object(originalCommand["merge"]);
            BDirectAttackInput.Fields(command, "commandId", "primaryInstanceId", "secondaryInstanceId", "expectedInventoryRevision", "expectedPrimaryRevision", "expectedSecondaryRevision", "expectedPrimaryEffectRevision", "expectedSecondaryEffectRevision", "expectedSourceVersion", "confirmedLosses");
            if ((string)originalCommand["version"] != Version || (string)originalCommand["expectedStatVersion"] != RunEquipmentStatSource.Version ||
                (string)command["expectedSourceVersion"] != RunEquipmentStatSource.Version || command["confirmedLosses"]?.Type != JTokenType.Boolean || !(bool)command["confirmedLosses"])
                throw new ArgumentException("equipment_health_merge_command_invalid");
            foreach (var revision in new[] { "expectedInventoryRevision", "expectedPrimaryRevision", "expectedSecondaryRevision", "expectedPrimaryEffectRevision", "expectedSecondaryEffectRevision" }) BDirectAttackInput.Nat(command, revision, 1, 9007199254740990);
            long expectedHealth = BDirectAttackInput.Nat(originalCommand, "expectedHealthRevision", 1, 9007199254740989);
            var commandId = Id(command, "commandId"); var primary = Id(command, "primaryInstanceId"); var secondary = Id(command, "secondaryInstanceId");
            BDirectAttackInput.Fields(input, "version", "commandId", "statVersion", "merge", "targets", "health");
            if ((string)input["version"] != Version || Id(input, "commandId") != commandId || (string)input["statVersion"] != RunEquipmentStatSource.Version || primary == secondary)
                throw new ArgumentException("equipment_health_merge_receipt_invalid");
            var merge = Object(input["merge"]);
            BDirectAttackInput.Fields(merge, "version", "commandId", "resultInstanceId", "consumedInstanceId", "sourceVersion", "star", "instanceRevision", "effectRevision", "inventoryRevision", "resultLocation", "vacatedLocation", "storedAfter", "suppliesCost");
            if ((string)merge["version"] != "equipment.merge-receipt.v1" || Id(merge, "commandId") != commandId || Id(merge, "resultInstanceId") != primary ||
                Id(merge, "consumedInstanceId") != secondary || (string)merge["sourceVersion"] != RunEquipmentStatSource.Version || BDirectAttackInput.Nat(merge, "star", 2, 5) != 2)
                throw new ArgumentException("equipment_health_merge_definition_or_handler_unavailable");
            foreach (var revision in new[] { "Inventory", "Primary", "PrimaryEffect" })
            {
                string output = revision == "Inventory" ? "inventoryRevision" : revision == "Primary" ? "instanceRevision" : "effectRevision";
                if (BDirectAttackInput.Nat(merge, output, 2) != (long)command["expected" + revision + "Revision"] + 1) throw new ArgumentException("equipment_health_merge_revision_conflict");
            }
            if (context.InventoryRevision != (long)merge["inventoryRevision"]) throw new ArgumentException("equipment_health_merge_revision_conflict");
            BDirectAttackInput.Nat(merge, "storedAfter"); BDirectAttackInput.Nat(merge, "suppliesCost", 0, 0);
            var resultLocation = Location(merge["resultLocation"]); var vacatedLocation = Location(merge["vacatedLocation"]);
            var affected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var location in new[] { resultLocation, vacatedLocation }) if (location != null) affected.Add((string)location["kind"] + ":" + (string)location["wearerId"]);
            var targets = Array(input["targets"], 1, 2); var healths = Array(input["health"], 1, 2);
            if (targets.Count != affected.Count || targets.Count != healths.Count) throw new ArgumentException("equipment_health_merge_target_unbound");
            var seen = new HashSet<string>(StringComparer.Ordinal); string previous = null;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = Object(targets[i]); BDirectAttackInput.Fields(target, "wearerKind", "wearerId", "targetId", "maxHp");
                string kind = Id(target, "wearerKind"), wearer = Id(target, "wearerId"), targetId = Id(target, "targetId"), key = kind + ":" + wearer;
                if (!affected.Remove(key) || !seen.Add(targetId) || previous != null && string.CompareOrdinal(previous, key) >= 0) throw new ArgumentException("equipment_health_merge_target_unbound");
                previous = key; long maximum = BDirectAttackInput.Nat(target, "maxHp", 1);
                var actor = context.ActorProposal(kind, wearer);
                if ((string)actor["targetId"] != targetId || (long)actor["stats"]["maxHp"] != maximum) throw new ArgumentException("equipment_health_merge_target_unbound");
                var health = Object(healths[i]); BDirectAttackInput.Fields(health, "eventId", "targetId", "change", "before", "after", "version", "revision");
                var eventBody = new JObject { ["runId"] = expectedRun, ["requestHash"] = requestHash, ["wearerKind"] = kind, ["wearerId"] = wearer, ["targetId"] = targetId };
                if ((string)health["version"] != "combat.health-receipt.v2" || Id(health, "eventId") != "eqmergehp:" + BDirectAttackInput.Digest(eventBody).Substring(7) ||
                    Id(health, "targetId") != targetId || BDirectAttackInput.Nat(health, "revision", 2) != expectedHealth + i + 1)
                    throw new ArgumentException("equipment_health_merge_health_identity_conflict");
                var change = Object(health["change"]); BDirectAttackInput.Fields(change, "kind", "maxHp");
                if ((string)change["kind"] != "MAX_HP_REBASE" || BDirectAttackInput.Nat(change, "maxHp", 1) != maximum) throw new ArgumentException("equipment_health_merge_not_rebase");
                var before = Checkpoint(health["before"]); var after = Checkpoint(health["after"]);
                if ((long)after["maxHp"] != maximum || !JToken.DeepEquals(before["anchorHp"], after["anchorHp"]) || !JToken.DeepEquals(before["anchorMaxHp"], after["anchorMaxHp"]) || (long)before["hp"] == 0 && (long)after["hp"] != 0)
                    throw new ArgumentException("equipment_health_merge_anchor_conflict");
                foreach (var slot in (JArray)actor["slots"]) if (slot.Type != JTokenType.Null && (string)slot["instanceId"] == secondary) throw new ArgumentException("equipment_health_merge_material_conflict");
            }
            if (resultLocation != null)
            {
                var actor = context.ActorProposal((string)resultLocation["kind"], (string)resultLocation["wearerId"]);
                var item = actor["slots"][(int)resultLocation["slotIndex"]];
                var source = ((JArray)actor["sources"]).SingleOrDefault(s => (string)s["instanceId"] == primary);
                if (item.Type == JTokenType.Null || (string)item["instanceId"] != primary || (int)item["star"] != 2 || source == null || (long)source["instanceRevision"] != (long)merge["instanceRevision"] || (long)source["effectRevision"] != (long)merge["effectRevision"])
                    throw new ArgumentException("equipment_health_merge_effect_conflict");
            }
            if (vacatedLocation != null && context.ActorProposal((string)vacatedLocation["kind"], (string)vacatedLocation["wearerId"])["slots"][(int)vacatedLocation["slotIndex"]].Type != JTokenType.Null)
                throw new ArgumentException("equipment_health_merge_material_conflict");
            return new EquipmentHealthMergeReceipt(input, responseHash, requestHash, context);
        }

        static bool HashText(string text) => Regex.IsMatch(text ?? "", "^[a-f0-9]{64}$");
        static JObject Object(JToken token) => token as JObject ?? throw new ArgumentException("equipment_health_merge_object_invalid");
        static JArray Array(JToken token, int min, int max) { if (!(token is JArray array) || array.Count < min || array.Count > max) throw new ArgumentException("equipment_health_merge_array_invalid"); return array; }
        static string Id(JToken token, string key) { var id = BDirectAttackInput.Text(token, key); if (!Regex.IsMatch(id, "^[\x21-\x7e]{1,128}$")) throw new ArgumentException("equipment_health_merge_id_invalid"); return id; }
        static JObject Location(JToken token) { if (token?.Type == JTokenType.Null) return null; var location = Object(token); BDirectAttackInput.Fields(location, "kind", "wearerId", "slotIndex"); if ((string)location["kind"] != "operator" && (string)location["kind"] != "turret") throw new ArgumentException("equipment_health_merge_location_invalid"); Id(location, "wearerId"); BDirectAttackInput.Nat(location, "slotIndex", 0, 2); return location; }
        static JObject Checkpoint(JToken token)
        {
            var checkpoint = Object(token); BDirectAttackInput.Fields(checkpoint, "version", "hp", "maxHp", "anchorHp", "anchorMaxHp");
            if ((string)checkpoint["version"] != "health.max-hp-rebase.v1") throw new ArgumentException("equipment_health_merge_health_version_conflict");
            long maximum = BDirectAttackInput.Nat(checkpoint, "maxHp", 1), hp = BDirectAttackInput.Nat(checkpoint, "hp", 0, maximum), anchorMaximum = BDirectAttackInput.Nat(checkpoint, "anchorMaxHp", 1), anchor = BDirectAttackInput.Nat(checkpoint, "anchorHp", 0, anchorMaximum);
            if ((anchor == 0) != (hp == 0)) throw new ArgumentException("equipment_health_merge_health_bounds_invalid");
            // Original receipt hash pins the ratio result; HP projection remains owned
            // by CombatVitals/CombatHealthStore, not reproduced by this wire boundary.
            return checkpoint;
        }
    }
}
