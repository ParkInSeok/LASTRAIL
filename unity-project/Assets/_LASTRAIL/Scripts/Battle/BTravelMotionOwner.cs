using System;
using Newtonsoft.Json.Linq;
using LastRail.Run;
namespace LastRail.Battle
{
    // Motion only. The original B session consumes time; this owner integrates its
    // consumed steps using the existing rail model, without A offers or stat effects.
    internal sealed class BTravelMotionOwner
    {
        internal const string Version = "unity.combat-b.travel-motion-local.v1.20261005";
        readonly JObject node, config;
        readonly string configHash, bindingHash;
        readonly long stepTicks;
        CombatRailJourney motion;
        internal BTravelMotionOwner(BDirectAttackBasicLoopBinding binding, BBoardingEventsNodeSnapshot originalNode, JObject configuration, string independentHash)
        {
            if (binding == null || configuration == null || BDirectAttackInput.Digest(configuration) != independentHash || originalNode == null)
                throw new ArgumentException("b_travel_original_source_missing");
            BDirectAttackInput.Fields(configuration,"version","selectionScaleBps","recoveryMs","baseSpeedMillimetersPerSecond","throttleMinimumBps","throttleMaximumBps","throttleCruiseBps","offers");
            if (!(configuration["offers"] is JArray offers) || offers.Count != 0)
                throw new ArgumentException("b_travel_route_or_augment_source_unavailable");
            BDirectAttackInput.Nat(configuration, "throttleCruiseBps", 1, 13000);
            config = (JObject)configuration.DeepClone(); configHash = independentHash; bindingHash = binding.OriginalHash;
            node = originalNode.OriginalSource.Capture(); stepTicks = binding.Composition.Attack.StepTimeSpanTicks;
            if ((string)binding.Composition.Attack.Capture()["waveSourceHash"] != (string)node["sourceHash"])
                throw new ArgumentException("b_travel_original_node_conflict");
            motion = new CombatRailJourney(config);
        }
        BTravelMotionOwner(BTravelMotionOwner other)
        {
            node = other.node; config = other.config; configHash = other.configHash; bindingHash = other.bindingHash; stepTicks = other.stepTicks;
            motion = new CombatRailJourney(config); motion.Restore(other.motion.Capture());
        }
        internal BTravelMotionOwner Clone() => new BTravelMotionOwner(this);
        internal void Step(long tick, int completedWaves)
        {
            if (tick != (long)motion.Capture()["currentTick"] + 1) throw new InvalidOperationException("b_travel_original_step_conflict");
            motion.AdvanceDistance(stepTicks); motion.CheckBoundary(completedWaves, tick);
        }
        internal void SetThrottle(int bps) => motion.SetThrottle(bps);
        internal void ObserveBoundary(long tick, int completedWaves) => motion.CheckBoundary(completedWaves, tick);
        internal JObject Capture(bool paused, string outcome, int commandRevision, long sequence) => new JObject
        {
            ["version"] = Version, ["provenance"] = "EXPLICIT_ORIGINAL_RAIL_MODEL_LOCAL_NOT_OPERATING_B_SPEED",
            ["configurationHash"] = configHash, ["bindingHash"] = bindingHash, ["stepTicks"] = stepTicks,
            ["origin"] = node["origin"].DeepClone(), ["nativeContextId"] = node["nativeContextId"].DeepClone(),
            ["nodeId"] = node["nodeId"].DeepClone(), ["nodeSourceHash"] = node["sourceHash"].DeepClone(),
            ["commandRevision"] = commandRevision, ["sequence"] = sequence, ["stopped"] = paused || outcome != "RUNNING",
            ["stopReason"] = paused ? new JValue("PAUSED") : outcome != "RUNNING" ? new JValue("TERMINAL") : JValue.CreateNull(),
            ["motion"] = motion.Capture(), ["routeSelectionSupported"] = false, ["operatingSpeedSourceSupported"] = false
        };
    }
}
