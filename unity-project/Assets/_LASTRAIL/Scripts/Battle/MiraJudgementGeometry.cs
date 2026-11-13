using System;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    /// <summary>Pure observations to fixed trajectory/contact facts. No HP, schedule, RNG or wall clock.</summary>
    public static class MiraJudgementGeometry
    {
        public const string ObservationVersion="mira.judgement-observation.v1.20261015";
        public const string RayVersion="mira.judgement-ray.v1.20261015";
        public static JObject CreateRay(JObject observation,string independentlyPinnedObservationHash,string targetId)
        {
            CheckObservation(observation);
            if(BDirectAttackInput.Digest(observation)!=independentlyPinnedObservationHash)throw new ArgumentException("Observation original pin.");
            var source=observation["source"];
            var target=((JArray)observation["targets"]).SingleOrDefault(t=>(string)t["id"]==targetId)
                ?? throw new ArgumentException("Unreserved target.");
            var profile=observation["profile"];var b=profile["ballistics"];
            if(target["eligible"]?.Type!=JTokenType.Boolean || !(bool)target["eligible"]
                || (string)target["domain"]!=(string)source["domain"]
                || BigInteger.Abs((BigInteger)(long)source["slot"]-(long)target["slot"])>(long)b["adjacentCars"])
                throw new InvalidOperationException("Original target domain/adjacency/eligibility.");
            var origin=Point(source["muzzleMm"]);var anchor=Point(target["anchorMm"]);var delta=Sub(anchor,origin);
            BigInteger square=delta.Aggregate(BigInteger.Zero,(a,x)=>a+(BigInteger)x*x),max=(long)b["maxDistanceMm"];
            if(square==0 || square>max*max)throw new InvalidOperationException("Original range or zero direction.");
            foreach(var box in (JArray)observation["blockers"])
            {
                var center=Point(box["centerMm"]);var size=Point(box["sizeMm"]);
                if(MiraSweepKernel.Box(origin,origin,center,center,size,0).HasValue)
                    throw new InvalidOperationException("Muzzle is inside a solid; no teleport/exemption.");
                if(MiraSweepKernel.Box(origin,anchor,center,center,size,(long)b["radiusMm"]).HasValue)
                    throw new InvalidOperationException("Original line of sight blocked.");
            }
            double length=Math.Sqrt((double)square);
            return new JObject {
                ["version"]=RayVersion,["launchObservation"]=observation.DeepClone(),["launchObservationHash"]=independentlyPinnedObservationHash,
                ["profileHash"]=observation["profileHash"].DeepClone(),["targetId"]=targetId,["targetGeneration"]=target["generation"].DeepClone(),
                ["originMm"]=new JArray(origin),["anchorMm"]=new JArray(anchor),["maximumDistanceMm"]=(long)max,
                ["anchorTravelUs"]=length*1000000/(long)b["speedMmPerSecond"]
            };
        }
        public static JObject RestoreRay(JObject original,string independentlyPinnedRayHash)
        {
            if(original==null || original.Count!=10 || (string)original["version"]!=RayVersion
                || BDirectAttackInput.Digest(original)!=independentlyPinnedRayHash)throw new ArgumentException("Ray version/hash.");
            var rebuilt=CreateRay((JObject)original["launchObservation"],(string)original["launchObservationHash"],(string)original["targetId"]);
            if(!JToken.DeepEquals(rebuilt,original))throw new ArgumentException("Ray source replay.");
            return rebuilt;
        }
        public static JObject Trace(JObject ray,string independentlyPinnedRayHash,JObject begin,JObject end,long elapsedStartUs,long elapsedEndUs)
            =>TraceCore(ray,independentlyPinnedRayHash,begin,end,elapsedStartUs,elapsedEndUs,null,0);
        // Same frozen launch ray, not a new ray aimed at the reserved enemy. A lost
        // enemy does not hide solid collision or extend the original distance/time.
        public static JObject TraceReserved(JObject ray,string independentlyPinnedRayHash,JObject begin,JObject end,long elapsedStartUs,long elapsedEndUs,string targetId,long generation)
        {
            var original=RestoreRay(ray,independentlyPinnedRayHash);
            if(!((JArray)original["launchObservation"]["targets"]).Any(t=>(string)t["id"]==targetId&&(long)t["generation"]==generation))
                throw new ArgumentException("Reservation outside original target set.");
            return TraceCore(original,independentlyPinnedRayHash,begin,end,elapsedStartUs,elapsedEndUs,targetId,generation);
        }
        static JObject TraceCore(JObject ray,string independentlyPinnedRayHash,JObject begin,JObject end,long elapsedStartUs,long elapsedEndUs,string reservedId,long reservedGeneration)
        {
            ray=RestoreRay(ray,independentlyPinnedRayHash);CheckObservation(begin);CheckObservation(end);
            string targetId=reservedId??(string)ray["targetId"];
            long generation=reservedId==null?(long)ray["targetGeneration"]:reservedGeneration;
            var launch=ray["launchObservation"];var b=launch["profile"]["ballistics"];
            if(elapsedStartUs<0 || elapsedEndUs<elapsedStartUs || (long)end["tick"]<(long)begin["tick"]
                || (long)end["revision"]<(long)begin["revision"]
                || (string)begin["profileHash"]!=(string)ray["profileHash"] || (string)end["profileHash"]!=(string)ray["profileHash"]
                || (string)begin["layoutHash"]!=(string)launch["layoutHash"] || (string)end["layoutHash"]!=(string)launch["layoutHash"]
                || (string)begin["basisHash"]!=(string)launch["basisHash"] || (string)end["basisHash"]!=(string)launch["basisHash"])
                throw new ArgumentException("Observation context/time mismatch.");
            var result=new JObject { ["kind"]="NONE",["profileHash"]=ray["profileHash"].DeepClone() };
            if(elapsedStartUs==elapsedEndUs)return result;
            var a=((JArray)begin["targets"]).SingleOrDefault(t=>(string)t["id"]==targetId);
            var z=((JArray)end["targets"]).SingleOrDefault(t=>(string)t["id"]==targetId);
            bool lost=a==null || z==null || (long)a["generation"]!=generation || (long)z["generation"]!=generation || !(bool)z["eligible"];
            if(lost && reservedId==null){result["kind"]="TARGET_LOST";return result;}
            long maxUs=(long)b["maxFlightUs"];
            if(elapsedStartUs>=maxUs){result["kind"]="EXPIRED";return result;}
            long boundedEnd=Math.Min(elapsedEndUs,maxUs);
            long duration=elapsedEndUs-elapsedStartUs,clipped=boundedEnd-elapsedStartUs;
            var start=PositionNumerators(ray,elapsedStartUs,out var pathScale).Select(x=>x*duration).ToArray();
            var finish=PositionNumerators(ray,boundedEnd,out _).Select(x=>x*duration).ToArray();
            var scale=pathScale*duration;
            var candidates=new JArray();
            if(!lost){
                var af=Point(a["feetMm"]).Select(x=>(BigInteger)x*scale).ToArray();
                var zf=TranslatedNumerators(Point(a["feetMm"]),Point(z["feetMm"]),duration,clipped,pathScale);
                double? time=MiraSweepKernel.CapsuleScaled(start,finish,af,zf,(long)a["heightMm"],(long)a["radiusMm"],(long)b["radiusMm"],scale);
                if(time.HasValue)candidates.Add(new JObject{["time"]=time.Value,["kind"]="HIT",["instanceId"]=targetId});
            }
            foreach(var before in (JArray)begin["blockers"])
            {
                var after=((JArray)end["blockers"]).SingleOrDefault(v=>(string)v["id"]==(string)before["id"]);
                if(after==null || !JToken.DeepEquals(before["sizeMm"],after["sizeMm"]))throw new InvalidOperationException("Solid lifecycle/rotation needs authority source event before tracing.");
                var c0=Point(before["centerMm"]).Select(x=>(BigInteger)x*scale).ToArray();
                var c1=TranslatedNumerators(Point(before["centerMm"]),Point(after["centerMm"]),duration,clipped,pathScale);
                var collision=MiraSweepKernel.BoxScaled(start,finish,c0,c1,Point(before["sizeMm"]),(long)b["radiusMm"],scale);
                if(collision.HasValue)candidates.Add(new JObject{["time"]=collision.Value,["kind"]="BLOCKED",["instanceId"]=before["id"].DeepClone()});
            }
            if(((JArray)end["blockers"]).Count!=((JArray)begin["blockers"]).Count)throw new InvalidOperationException("Unobserved solid lifecycle change.");
            if(candidates.Count>0)
            {
                result=SelectContact(candidates);
                double t=(double)result["time"];
                result["elapsedUs"]=elapsedStartUs+t*(boundedEnd-elapsedStartUs);
                result["pointMm"]=new JArray(start.Select((x,i)=>checked((long)Math.Round((double)x/(double)scale+t*(double)(finish[i]-x)/(double)scale,MidpointRounding.AwayFromZero))));
                result["profileHash"]=ray["profileHash"].DeepClone();result["targetGeneration"]=generation;
            }
            else if(elapsedEndUs>=maxUs)result["kind"]="EXPIRED";
            else if(lost)result["kind"]="TARGET_LOST";
            return result;
        }
        public static JObject SelectContact(JArray contacts)
        {
            if(contacts==null || contacts.Count==0)throw new ArgumentException("Contact candidates required.");
            foreach(var c in contacts)
            {
                double t=(double)c["time"];
                if(double.IsNaN(t)||double.IsInfinity(t)||t<0||t>1
                    || ((string)c["kind"]!="HIT" && (string)c["kind"]!="BLOCKED") || c["instanceId"]?.Type!=JTokenType.String)
                    throw new ArgumentException("Invalid contact.");
            }
            // No epsilon merges near-but-distinct contacts into a policy tie.
            return (JObject)contacts.OrderBy(c=>(double)c["time"]).ThenBy(c=>(string)c["kind"]=="BLOCKED"?0:1)
                .ThenBy(c=>(string)c["instanceId"],StringComparer.Ordinal).First().DeepClone();
        }
        public static long[] Position(JObject ray,long elapsedUs)
        {
            var numerators=PositionNumerators(ray,elapsedUs,out var scale);
            return numerators.Select(x=>{
                var value=BigInteger.DivRem(x,scale,out var remainder);
                if(BigInteger.Abs(remainder)*2>=scale)value+=x.Sign;
                return checked((long)value);
            }).ToArray();
        }
        internal static JArray PresentationPoint(JObject ray,long elapsedUs){var p=PositionNumerators(ray,elapsedUs,out var scale);return new JArray(p.Select(v=>(double)v/(double)scale));}
        static BigInteger[] PositionNumerators(JObject ray,long elapsedUs,out BigInteger scale)
        {
            if(elapsedUs<0)throw new ArgumentOutOfRangeException(nameof(elapsedUs));
            var origin=Point(ray["originMm"]);var delta=Sub(Point(ray["anchorMm"]),origin);
            BigInteger precision=BigInteger.Pow(10,18);
            var squared=delta.Aggregate(BigInteger.Zero,(v,x)=>v+(BigInteger)x*x)*precision*precision;
            if(squared<=0)throw new ArgumentException("Frozen ray direction.");
            BigInteger length=squared,next=(length+1)/2;
            while(next<length){length=next;next=(length+squared/length)/2;}
            if(length*length<squared)length++; // ceil avoids numerical overtravel at the maximum.
            scale=length*1000000;
            var b=ray["launchObservation"]["profile"]["ballistics"];
            var distanceTime=BigInteger.Min((BigInteger)(long)b["speedMmPerSecond"]*elapsedUs,
                (BigInteger)(long)b["maxDistanceMm"]*1000000);
            var denominator=scale;
            // All times share this rational ray; only public/reporting positions round to mm.
            return origin.Select((x,i)=>(BigInteger)x*denominator+(BigInteger)delta[i]*precision*distanceTime).ToArray();
        }
        static BigInteger[] TranslatedNumerators(long[] from,long[] to,long duration,long clipped,BigInteger pathScale)
            =>from.Select((x,i)=>((BigInteger)x*duration+((BigInteger)to[i]-x)*clipped)*pathScale).ToArray();
        static void CheckObservation(JObject p)
        {
            if(p==null || (string)p["version"]!=ObservationVersion
                || p["targets"] is not JArray || p["blockers"] is not JArray
                || BDirectAttackInput.Digest(p["profile"])!=(string)p["profileHash"]
                || BDirectAttackInput.Digest(p["basis"])!=(string)p["basisHash"])
                throw new ArgumentException("Geometry observation source.");
            Point(p["source"]?["muzzleMm"]);
        }
        static long[] Point(JToken p)
        { if(p is not JArray a || a.Count!=3 || a.Any(v=>v.Type!=JTokenType.Integer))throw new ArgumentException("Explicit mm point required.");return a.Select(v=>(long)v).ToArray(); }
        static long[] Sub(long[] a,long[] b)=>a.Select((v,i)=>checked(v-b[i])).ToArray();
        static long[] Interpolate(long[] to,long[] from,double t)=>to.Select((v,i)=>checked(from[i]+(long)Math.Round((v-from[i])*t,MidpointRounding.AwayFromZero))).ToArray();
    }
}
