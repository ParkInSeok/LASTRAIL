using System;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    // Separate LOCAL saved-model source. Its revision is the original checkpoint
    // revision, never the B command generation or server map/journal revision.
    public sealed class CombatSavedMaintenanceObservationSource : IDisposable
    {
        public const string Version = "unity.combat.maintenance-saved-observation-local.v1.20261005";
        CombatSceneDriver driver;
        JObject original;
        public long Revision { get; private set; }
        public bool CanPrepareTrustedMaintenance => false;
        public CombatSavedMaintenanceObservationSource(CombatSceneDriver originalDriver)
        {
            driver = originalDriver ?? throw new ArgumentNullException(nameof(originalDriver));
            var saved = driver.CaptureCommittedState(); Revision = saved.Revision; original = JObject.Parse(saved.StateJson);
        }
        public JObject Capture()
        {
            Alive();
            bool paused = (bool)original["clock"]["paused"];
            return new JObject
            {
                ["version"] = Version, ["provenance"] = "LOCAL_SAVED_MODEL_NOT_SERVER_TRUSTED_EVIDENCE",
                ["checkpointRevision"] = Revision, ["stateHash"] = BDirectAttackInput.Digest(original), ["state"] = original.DeepClone(),
                ["combatStopped"] = paused, ["travelStopped"] = original["railJourney"] is JObject ? new JValue(paused) : JValue.CreateNull(),
                ["residualHazards"] = JValue.CreateNull(), ["safeEventId"] = JValue.CreateNull(), ["canPrepareTrustedMaintenance"] = false,
                ["unsupported"] = new JArray("ACCEPTED_SERVER_NODE_MAP_AND_SAFE_EVENT_BINDING", "ALL_HAZARD_AUTHORITY", "SERVER_VERIFIER_AND_SQL_FENCE")
            };
        }
        public JObject ObserveCommitted(long expectedRevision)
        {
            Alive();
            if (Revision != expectedRevision) throw new InvalidOperationException("combat_maintenance_observation_revision_conflict");
            var saved = driver.CaptureCommittedState(); var state = JObject.Parse(saved.StateJson);
            if (saved.Revision == Revision && JToken.DeepEquals(original, state)) return Capture();
            if (Revision >= 9007199254740991 || saved.Revision != Revision + 1 ||
                !JToken.DeepEquals(original["configurationSha256"], state["configurationSha256"]))
                throw new InvalidOperationException("combat_maintenance_observation_committed_prefix_conflict");
            Revision = saved.Revision; original = state; return Capture();
        }
        public void RequireUnchanged(long expectedRevision, string originalStateHash)
        {
            Alive(); var saved = driver.CaptureCommittedState();
            if (expectedRevision != Revision || saved.Revision != Revision || originalStateHash != BDirectAttackInput.Digest(original) ||
                !JToken.DeepEquals(original, JObject.Parse(saved.StateJson)))
                throw new InvalidOperationException("combat_maintenance_observation_source_changed");
        }
        public void RequireTrustedMaintenance() => throw new InvalidOperationException("equipment_safe_source_unavailable");
        public static CombatSavedMaintenanceObservationSource Restore(CombatSceneDriver restoredOriginalDriver, JObject observation, string independentHash)
        {
            if (observation == null || BDirectAttackInput.Digest(observation) != independentHash)
                throw new ArgumentException("combat_maintenance_observation_original_hash_conflict");
            var source = new CombatSavedMaintenanceObservationSource(restoredOriginalDriver);
            if (!JToken.DeepEquals(source.Capture(), observation)) throw new ArgumentException("combat_maintenance_observation_original_restore_conflict");
            return source;
        }
        void Alive() { if (driver == null) throw new ObjectDisposedException(nameof(CombatSavedMaintenanceObservationSource)); }
        public void Dispose() { driver = null; original = null; }
    }
}
