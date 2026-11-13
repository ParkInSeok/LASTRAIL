using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    /// <summary>Immutable accepted definition index. It owns no account, formation, or combat state.</summary>
    public sealed class CombatDefinitionIndex
    {
        readonly JObject contract;
        public string SchemaVersion => CombatDefinitionIntake.SchemaVersion;
        public string DefinitionVersion => CombatDefinitionIntake.DefinitionVersion;
        public string ContentHash => CombatDefinitionIntake.ApprovedContentHash;
        public bool CanExecute => false;
        public string DefinitionSupport => "AVAILABLE";
        public string RuntimeBindingSupport => "MISSING";
        public IReadOnlyList<string> OperatorDefinitionIds { get; }
        public IReadOnlyList<string> RequiredEffectKinds { get; }
        public IReadOnlyList<string> MissingRuntimeInputs { get; }
        internal CombatDefinitionIndex(JObject accepted)
        {
            contract = (JObject)accepted.DeepClone();
            OperatorDefinitionIds = Array.AsReadOnly(((JArray)contract["definition"]["operators"]).Select(row => (string)row["operatorId"]).ToArray());
            RequiredEffectKinds = Array.AsReadOnly(contract["definition"]["requiredEffectKinds"].Values<string>().ToArray());
            MissingRuntimeInputs = Array.AsReadOnly(contract["definition"]["missingRuntimeInputs"].Values<string>().ToArray());
        }
        public JObject Capture() => (JObject)contract.DeepClone();
        public void RequireExecutable() => throw new InvalidOperationException("combat_definition_execution_unavailable");
    }

    /// <summary>
    /// Reads the current approved publication only; never reinterpret historical START/checkpoints.
    /// A digest supplied by the sender is not an approval. New publications require a reviewed pin update.
    /// </summary>
    public static class CombatDefinitionIntake
    {
        public const string SchemaVersion = "combat.definition-intake.v1";
        public const string DefinitionVersion = "combat.policy-catalog.2026-09-29.1";
        public const string ApprovedContentHash = "sha256:49a0f88a5f2c2a28f626d32dff383a68de1c54f6acf2284557a6f7368713ad34";
        const string ServerRoot = "D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/";
        static readonly string[] SourceNames = { "combat-definition-contract.ts", "combat-policy-catalog.ts", "combat-policy-binding.ts", "combat-shield-budget.ts" };
        static readonly string[] SourceHashes = {
            "03d37358f13e5834c67362ff559186a7b45451000e262d0ffbfd16dbd0366d26",
            "c06e1620e021a5cf254aa6995902effc554482cb850c1958ffe131dee0297df0",
            "560d0a459a0ff5e64e010bec67b698abda4c151f2fdc06b404fb119f3af8ec50",
            "d612096a9b763584382345b3a31d1dd46e53c4a2e993725b6bc2310b1731eee6" };
        static void Need(bool valid) { if (!valid) throw new ArgumentException("combat_definition_contract_mismatch"); }
        static bool Text(JToken value, string expected) => value?.Type == JTokenType.String && (string)value == expected;
        static void Fields(JObject value, params string[] names) => Need(value != null && value.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(names.OrderBy(n => n, StringComparer.Ordinal)));
        // The approved publication uses only integer JSON numbers. Reject unsupported scalar types at this boundary.
        static JToken Canonical(JToken value)
        {
            if (value is JObject obj) return new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, Canonical(p.Value))));
            if (value is JArray array) return new JArray(array.Select(Canonical));
            Need(value != null && (value.Type == JTokenType.String || value.Type == JTokenType.Integer || value.Type == JTokenType.Boolean || value.Type == JTokenType.Null));
            return value.DeepClone();
        }
        static string Hash(JToken value)
        {
            using var sha = SHA256.Create();
            return "sha256:" + BitConverter.ToString(sha.ComputeHash(new UTF8Encoding(false, true).GetBytes(Canonical(value).ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant();
        }
        public static CombatDefinitionIndex Read(JObject input)
        {
            Fields(input, "schemaVersion", "definitionVersion", "contentHash", "purpose", "canExecute", "support", "blockingReasons", "definition");
            Need(Text(input["schemaVersion"], SchemaVersion) && Text(input["definitionVersion"], DefinitionVersion)
                && Text(input["contentHash"], ApprovedContentHash) && Text(input["purpose"], "DEFINITION_INTAKE_ONLY")
                && input["canExecute"]?.Type == JTokenType.Boolean && !(bool)input["canExecute"]);
            var definition = input["definition"] as JObject;
            Need(definition != null && Hash(definition) == ApprovedContentHash);
            Need(Text(definition["policy"]?["version"], DefinitionVersion));
            Fields(input["support"] as JObject, "definitions", "runtimeBinding", "executionEvidence");
            Need(Text(input["support"]["definitions"], "AVAILABLE") && Text(input["support"]["runtimeBinding"], "MISSING") && Text(input["support"]["executionEvidence"], "NOT_RUN"));
            var blockers = new JArray("effective_stat_profiles_missing", "effect_runtime_binding_missing", "operation_definitions_missing", "start_checkpoint_replay_binding_missing");
            foreach (var kind in definition["requiredEffectKinds"].Values<string>()) blockers.Add("combat_effect_handler_missing:" + kind);
            Need(JToken.DeepEquals(input["blockingReasons"], blockers));
            foreach (var key in new[] { "effectiveStatProfiles", "startSnapshot", "effectRuntime" }) Need(definition[key]?.Type == JTokenType.Null);
            return new CombatDefinitionIndex(input);
        }
        /// <summary>Offline export provenance metadata; caller supplies bytes, no runtime filesystem dependency.</summary>
        public static CombatDefinitionIndex ReadWithProvenance(JObject input, JObject provenance)
        {
            var accepted = Read(input);
            Fields(provenance, "output", "contentHash", "sources", "environment", "realStart", "remoteMutation");
            Need(Text(provenance["output"], "D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-definition-intake-20261001.json")
                && Text(provenance["contentHash"], ApprovedContentHash) && Text(provenance["environment"], "LOCAL_DEFINITION_EXPORT")
                && provenance["realStart"]?.Type == JTokenType.Boolean && !(bool)provenance["realStart"]
                && provenance["remoteMutation"]?.Type == JTokenType.Boolean && !(bool)provenance["remoteMutation"]);
            var sources = provenance["sources"] as JArray;
            Need(sources != null && sources.Count == SourceNames.Length);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                Fields(source as JObject, "path", "sha256");
                var index = Array.FindIndex(SourceNames, name => Text(source["path"], ServerRoot + name));
                Need(index >= 0 && seen.Add(SourceNames[index]) && Text(source["sha256"], SourceHashes[index]));
            }
            return accepted;
        }
    }
}