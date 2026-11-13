using System;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    // Composes immutable accepted-command observations; owns no clock or hazards.
    public sealed class BTravelMaintenanceObservationSource : IDisposable
    {
        public const string Version = "unity.combat-b.travel-maintenance-observation-local.v1.20261005";
        BSafeMaintenanceObservationSource source;
        public long Revision { get { Alive(); return source.Revision; } }
        public bool CanPrepareTrustedMaintenance => false;
        public BTravelMaintenanceObservationSource(BDirectAttackBasicLoop native)
        {
            if (native == null) throw new ArgumentNullException(nameof(native));
            if (!(native.Capture()["travel"] is JObject)) throw new InvalidOperationException("b_travel_source_unavailable");
            source = new BSafeMaintenanceObservationSource(native);
        }
        BTravelMaintenanceObservationSource(BSafeMaintenanceObservationSource restored) { source = restored; }
        public JObject Capture()
        {
            Alive();
            var observation = source.Capture();
            var native = (JObject)observation["native"];
            var hazards = BResidualHazardObservation.ReadOwned(native);
            return new JObject {
                ["version"] = Version, ["provenance"] = "LOCAL_INSTALLED_OWNERS_NOT_SERVER_TRUSTED",
                ["originalObservation"] = observation, ["travel"] = native["travel"].DeepClone(),
                ["hazards"] = hazards.Capture(), ["hazardsKnown"] = hazards.Known, ["hazardsSettled"] = hazards.Settled,
                ["combatStopped"] = (bool)native["state"]["direct"]["clock"]["paused"] || (string)native["travel"]["stopReason"] == "TERMINAL",
                ["travelStopped"] = native["travel"]["stopped"].DeepClone(),
                ["canPrepareTrustedMaintenance"] = false,
                ["unsupported"] = new JArray("OPERATING_B_SPEED_AND_ROUTE_SOURCE", "ACCEPTED_MAP_SAFE_EVENT", "SERVER_TRANSITION_VERIFIER_AND_SQL_FENCE", "UNINSTALLED_HAZARD_CAPABILITIES")
            };
        }
        public JObject ObserveAccepted(string eventId, long expectedRevision)
        { Alive(); source.ObserveAccepted(eventId, expectedRevision); return Capture(); }
        public void RequireUnchanged(long expectedRevision, string nativeHash)
        { Alive(); source.RequireUnchanged(expectedRevision, nativeHash); }
        public void RequireTrustedMaintenance()
        { Alive(); source.RequireTrustedMaintenance(); }
        public static BTravelMaintenanceObservationSource Restore(BDirectAttackBasicLoop restoredNative, JObject observation, string independentHash)
        {
            if (observation == null || BDirectAttackInput.Digest(observation) != independentHash)
                throw new ArgumentException("b_travel_observation_original_hash_conflict");
            if (!(restoredNative?.Capture()["travel"] is JObject)) throw new InvalidOperationException("b_travel_source_unavailable");
            var original = observation["originalObservation"] as JObject;
            var restored = BSafeMaintenanceObservationSource.Restore(restoredNative, original, BDirectAttackInput.Digest(original));
            var result = new BTravelMaintenanceObservationSource(restored);
            if (!JToken.DeepEquals(result.Capture(), observation)) { result.Dispose(); throw new ArgumentException("b_travel_observation_restore_conflict"); }
            return result;
        }
        void Alive() { if (source == null) throw new ObjectDisposedException(nameof(BTravelMaintenanceObservationSource)); }
        public void Dispose() { source?.Dispose(); source = null; }
    }
}
