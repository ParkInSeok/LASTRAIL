using System;
using Newtonsoft.Json.Linq;
using LastRail.Run;
namespace LastRail.Battle
{
    public sealed partial class BDirectAttackBasicLoop
    {
        public const string TravelVersion = "unity.combat-b.travel-native-local.v1.20261005";
        public const string TravelEventVersion = "unity.combat-b.travel-native-local.v2.20261006";
        BNativeOriginalEventJournal originalEvents;
        public JObject CaptureOriginalEvents() => originalEvents?.Capture();
        public static BDirectAttackBasicLoop CreateOrdinaryTravelEventRuntime(BDirectAttackBasicLoopBinding binding,
            BBoardingEventsNodeSnapshot node, BOrdinaryGeometryBinding geometry, int capacity,
            JObject originalRailConfiguration, string independentRailHash, bool namedInterception = false)
        {
            var owner=CreateOrdinaryTravelRuntime(binding,node,geometry,capacity,originalRailConfiguration,independentRailHash,namedInterception);
            owner.originalEvents=new BNativeOriginalEventJournal();return owner;
        }
        BTravelMotionOwner travel;
        public static BDirectAttackBasicLoop CreateOrdinaryTravelRuntime(BDirectAttackBasicLoopBinding binding,
            BBoardingEventsNodeSnapshot node, BOrdinaryGeometryBinding geometry, int capacity,
            JObject originalRailConfiguration, string independentRailHash, bool namedInterception = false)
            => CreateSpatialTravelRuntime(binding,node,new BLegacySpatialGeometry(geometry),capacity,originalRailConfiguration,independentRailHash,namedInterception);
        internal static BDirectAttackBasicLoop CreateSpatialTravelRuntime(BDirectAttackBasicLoopBinding binding,
            BBoardingEventsNodeSnapshot node,BOrdinarySpatialGeometry geometry,int capacity,
            JObject originalRailConfiguration,string independentRailHash,bool namedInterception)
        {
            var motion = new BTravelMotionOwner(binding, node, originalRailConfiguration, independentRailHash);
            var owner = CreateSpatialGeometryRuntime(binding, node, geometry, capacity, namedInterception);
            owner.travel = motion; return owner;
        }
        public static BDirectAttackBasicLoop CreateMeasuredTravelEventRuntime(BDirectAttackBasicLoopBinding binding,
            BBoardingEventsNodeSnapshot node,BMeasuredPhysicalGeometry geometry,int capacity,
            JObject originalRailConfiguration,string independentRailHash,bool namedInterception=false)
            =>CreateSpatialTravelEventRuntime(binding,node,new BMeasuredSpatialGeometry(geometry,binding.Composition),capacity,originalRailConfiguration,independentRailHash,namedInterception);
        internal static BDirectAttackBasicLoop CreateSpatialTravelEventRuntime(BDirectAttackBasicLoopBinding binding,
            BBoardingEventsNodeSnapshot node,BOrdinarySpatialGeometry geometry,int capacity,
            JObject originalRailConfiguration,string independentRailHash,bool namedInterception)
        {
            var owner=CreateSpatialTravelRuntime(binding,node,geometry,capacity,originalRailConfiguration,independentRailHash,namedInterception);
            owner.originalEvents=new BNativeOriginalEventJournal();return owner;
        }
        public static BDirectAttackBasicLoop RestoreMeasuredTravelRuntime(JObject cache,string independentCacheHash,
            BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BMeasuredPhysicalGeometry geometry,int capacity,
            JObject originalRailConfiguration,string independentRailHash,bool namedInterception=false)
            =>RestoreSpatialTravelRuntime(cache,independentCacheHash,binding,node,new BMeasuredSpatialGeometry(geometry,binding.Composition),capacity,originalRailConfiguration,independentRailHash,namedInterception);
        public BDirectAttackBasicLoopResult SetTravelThrottle(string eventId, int bps) => Commit(new JObject
            { ["kind"] = "TRAVEL_THROTTLE", ["eventId"] = eventId, ["bps"] = bps });
        public static BDirectAttackBasicLoop RestoreOrdinaryTravelRuntime(JObject cache, string independentCacheHash,
            BDirectAttackBasicLoopBinding binding, BBoardingEventsNodeSnapshot node, BOrdinaryGeometryBinding geometry,
            int capacity, JObject originalRailConfiguration, string independentRailHash, bool namedInterception = false)
            =>RestoreSpatialTravelRuntime(cache,independentCacheHash,binding,node,new BLegacySpatialGeometry(geometry),capacity,originalRailConfiguration,independentRailHash,namedInterception);
        internal static BDirectAttackBasicLoop RestoreSpatialTravelRuntime(JObject cache,string independentCacheHash,
            BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BOrdinarySpatialGeometry geometry,int capacity,
            JObject originalRailConfiguration,string independentRailHash,bool namedInterception)
        {
            if (cache == null || BDirectAttackInput.Digest(cache) != independentCacheHash || ((string)cache["version"] != TravelVersion && (string)cache["version"] != TravelEventVersion))
                throw new ArgumentException("b_travel_original_cache_conflict");
            var owner = CreateSpatialTravelRuntime(binding, node, geometry, capacity, originalRailConfiguration, independentRailHash, namedInterception);
            if((string)cache["version"]==TravelEventVersion)owner.originalEvents=new BNativeOriginalEventJournal();
            foreach (var command in BuffWire.Array(cache["commands"], 0, capacity)) owner.Commit(BuffWire.Object(command));
            if (!JToken.DeepEquals(owner.Capture(), cache)) throw new ArgumentException("b_travel_original_replay_conflict");
            return owner;
        }
    }
}
