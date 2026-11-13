using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    // LOCAL accepted-command observation, not the server OriginalSafeRuntimeSource.
    // Original native owners retain clock/HP/hazards. This owner retains only the
    // last immutable observation and its local generation; no eligibility is issued.
    public sealed class BSafeMaintenanceObservationSource : IDisposable
    {
        public const string Version = "unity.combat-b.maintenance-observation-local.v1.20261005";
        BDirectAttackBasicLoop native;
        JObject original;
        long revision;
        string acceptedEvent;
        public long Revision => revision;
        public bool CanPrepareTrustedMaintenance => false;

        public BSafeMaintenanceObservationSource(BDirectAttackBasicLoop originalOwner)
        {
            native = originalOwner ?? throw new ArgumentNullException(nameof(originalOwner));
            original = native.Capture();
        }
        public JObject Capture()
        {
            Alive();
            return new JObject
            {
                ["version"] = Version, ["provenance"] = "LOCAL_NATIVE_OBSERVATION_NOT_SERVER_TRUSTED_EVIDENCE",
                ["sourceId"] = "native-b:" + (string)original["bindingHash"], ["sourceRevision"] = revision,
                ["acceptedEventId"] = acceptedEvent == null ? JValue.CreateNull() : new JValue(acceptedEvent),
                ["nativeHash"] = BDirectAttackInput.Digest(original), ["native"] = original.DeepClone(),
                ["combatStopped"] = original["state"]["direct"]["clock"]["paused"].DeepClone(),
                ["travelStopped"] = JValue.CreateNull(), ["residualHazards"] = JValue.CreateNull(),
                ["safeEventId"] = JValue.CreateNull(), ["canPrepareTrustedMaintenance"] = false,
                ["unsupported"] = new JArray("ORIGINAL_RUN_NODE_MAP_BINDING", "ORIGINAL_TRAVEL_OWNER",
                    "ACCEPTED_SAFE_EVENT_OWNER", "ALL_HAZARD_AUTHORITY", "SERVER_ACCEPTED_TRANSITION_VERIFIER_AND_SQL_FENCE")
            };
        }
        // Called after the original owner accepts a command, never from a UI pause
        // assertion. A rejected command cannot appear in this journal. Do not skip
        // accepted commands: each advances this source generation exactly once.
        public JObject ObserveAccepted(string eventId, long expectedRevision)
        {
            Alive();
            if (expectedRevision != revision) throw new InvalidOperationException("b_maintenance_observation_revision_conflict");
            var current = native.Capture(); var commands = (JArray)current["commands"];
            if (JToken.DeepEquals(current, original))
            {
                if (eventId != acceptedEvent || eventId == null) throw new InvalidOperationException("b_maintenance_observation_not_accepted");
                return Capture();
            }
            var previous = (JArray)original["commands"];
            if ((string)current["bindingHash"] != (string)original["bindingHash"] || commands.Count != previous.Count + 1 ||
                (string)commands.Last["eventId"] != eventId || string.IsNullOrEmpty(eventId) ||
                !JToken.DeepEquals(new JArray(commands.Take(previous.Count)), previous))
                throw new InvalidOperationException("b_maintenance_observation_accepted_prefix_conflict");
            if (revision >= 9007199254740991) throw new InvalidOperationException("b_maintenance_observation_revision_exhausted");
            original = (JObject)current.DeepClone(); acceptedEvent = eventId; revision++;
            return Capture();
        }
        // Checks the actual original owner again. A clock resume, extra accepted
        // command, terminal transition or hazard change invalidates the old capture.
        // This is a LOCAL synchronous fence, never a SQL assertion or server proof.
        public void RequireUnchanged(long expectedRevision, string originalNativeHash)
        {
            Alive();
            if (expectedRevision != revision || originalNativeHash != BDirectAttackInput.Digest(original) ||
                !JToken.DeepEquals(native.Capture(), original))
                throw new InvalidOperationException("b_maintenance_observation_source_changed");
        }
        public void RequireTrustedMaintenance() => throw new InvalidOperationException("equipment_safe_source_unavailable");
        public static BSafeMaintenanceObservationSource Restore(BDirectAttackBasicLoop restoredOriginalOwner,
            JObject originalObservation, string independentObservationHash)
        {
            if (originalObservation == null || BDirectAttackInput.Digest(originalObservation) != independentObservationHash)
                throw new ArgumentException("b_maintenance_observation_original_hash_conflict");
            var source = new BSafeMaintenanceObservationSource(restoredOriginalOwner);
            source.revision = BDirectAttackInput.Nat(originalObservation, "sourceRevision");
            var id = originalObservation["acceptedEventId"];
            source.acceptedEvent = id?.Type == JTokenType.Null ? null : BDirectAttackInput.Text(originalObservation, "acceptedEventId");
            if (source.revision > 0 && (string)((JArray)source.original["commands"]).Last?["eventId"] != source.acceptedEvent ||
                source.revision == 0 && source.acceptedEvent != null || !JToken.DeepEquals(source.Capture(), originalObservation))
                throw new ArgumentException("b_maintenance_observation_original_restore_conflict");
            return source;
        }
        void Alive() { if (native == null) throw new ObjectDisposedException(nameof(BSafeMaintenanceObservationSource)); }
        public void Dispose() { native = null; original = null; acceptedEvent = null; }
    }
}
