using System;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 internal readonly struct BMmPoint {
  internal readonly long X,Y,Z;internal BMmPoint(long x,long y,long z){X=x;Y=y;Z=z;}
  internal static BMmPoint Read(JToken token){var a=BuffWire.Array(token,3,3);foreach(var n in a)BuffWire.Need(n.Type==JTokenType.Integer&&(long)n>=-1000000000&&(long)n<=1000000000,"b_geometry_mm_coordinate");return new BMmPoint((long)a[0],(long)a[1],(long)a[2]);}
  internal JArray Capture()=>new JArray(X,Y,Z);
  internal static BigInteger Squared(BMmPoint a,BMmPoint b)=>BigInteger.Pow(a.X-b.X,2)+BigInteger.Pow(a.Y-b.Y,2)+BigInteger.Pow(a.Z-b.Z,2);
  internal static long Distance(BMmPoint a,BMmPoint b){var square=Squared(a,b);BigInteger lo=0,hi=4000000000;while(lo<hi){var mid=(lo+hi)/2;if(mid*mid<square)lo=mid+1;else hi=mid;}return (long)lo;}
  internal static BMmPoint Move(BMmPoint a,BMmPoint b,long distance){long length=Distance(a,b);if(length==0||distance>=length)return b;return new BMmPoint(a.X+(long)((BigInteger)(b.X-a.X)*distance/length),a.Y+(long)((BigInteger)(b.Y-a.Y)*distance/length),a.Z+(long)((BigInteger)(b.Z-a.Z)*distance/length));}
  internal bool Inside(BMmPoint min,BMmPoint max)=>X>=min.X&&X<=max.X&&Y>=min.Y&&Y<=max.Y&&Z>=min.Z&&Z<=max.Z;
 }
 // Immutable millimetre authoring. It owns neither actors nor native time/HP.
 public sealed class BOrdinaryGeometrySnapshot {
  public const string Version="unity.combat-b.geometry-test-model.v1.20261004";
  readonly JObject original;public string Hash {get;}public string AssetId=>(string)original["assetId"];
  internal long OperatorProbeSpeed=>BuffWire.Number(original["operatorProbeSpeedMmPerSecond"],1,10000);
  internal BMmPoint Point(string field)=>BMmPoint.Read(original[field]);
  internal BMmPoint[] Path(int side,string field)=>((JArray)original[side==0?"left":"right"][field]).Select(BMmPoint.Read).ToArray();
  internal BMmPoint[] GunnerAnchors=>((JArray)original["gunnerAnchorsMm"]).Select(BMmPoint.Read).ToArray();
  internal JObject[] Obstacles=>((JArray)original["obstacles"]).Select(x=>(JObject)x.DeepClone()).ToArray();
  public JObject Capture()=>(JObject)original.DeepClone();
  public static BOrdinaryGeometrySnapshot Read(JObject p,string independentHash)=>new BOrdinaryGeometrySnapshot(p,independentHash);
  BOrdinaryGeometrySnapshot(JObject p,string hash){BuffWire.Need(p!=null&&BasicCombatSimulation.StableJson(p).Length<=65536&&BDirectAttackInput.Digest(p)==hash,"b_geometry_independent_hash");BuffWire.Fields(p,"version","assetId","provenance","dimensionSource","operatorProbeSpeedMmPerSecond","operatorMovementSource","bodyMinMm","bodyMaxMm","roofMinMm","roofMaxMm","actorMm","fireMm","hitMm","backMm","frontMm","left","right","gunnerAnchorsMm","scoutAnchorMm","obstacles");BuffWire.Need((string)p["version"]==Version&&(string)p["provenance"]=="TEST_JUDGEMENT_MODEL_NOT_ART_DIMENSIONS","b_geometry_version_or_source");BuffWire.Text(p["assetId"]);BuffWire.Text(p["dimensionSource"],512);BuffWire.Number(p["operatorProbeSpeedMmPerSecond"],1,10000);BuffWire.Need((string)p["operatorMovementSource"]=="EXPLICIT_TEST_PROBE_NOT_OPERATING_OPERATOR_PROFILE","b_geometry_movement_source");original=(JObject)p.DeepClone();Hash=hash;var min=Point("bodyMinMm");var max=Point("bodyMaxMm");var rmin=Point("roofMinMm");var rmax=Point("roofMaxMm");BuffWire.Need(min.X<max.X&&min.Y<max.Y&&min.Z<max.Z&&rmin.Inside(min,max)&&rmax.Inside(min,max)&&rmin.X<rmax.X&&rmin.Z<rmax.Z&&rmin.Y==rmax.Y,"b_geometry_bounds");foreach(string key in new[]{"actorMm","fireMm","hitMm","backMm","frontMm","scoutAnchorMm"})BuffWire.Need(Point(key).Inside(rmin,rmax),"b_geometry_anchor_outside_roof");
   foreach(int side in new[]{0,1}){var s=BuffWire.Object(p[side==0?"left":"right"]);BuffWire.Fields(s,"socketId","approachMm","climbMm");BuffWire.Need((string)s["socketId"]==(side==0?"left":"right"),"b_geometry_socket");foreach(string key in new[]{"approachMm","climbMm"}){BuffWire.Array(s[key],2,32);foreach(var x in (JArray)s[key])BMmPoint.Read(x);}BuffWire.Need(JToken.DeepEquals(s["approachMm"].Last,s["climbMm"].First)&&BMmPoint.Read(s["climbMm"].Last).Inside(rmin,rmax),"b_geometry_disconnected_path");}
   BuffWire.Array(p["gunnerAnchorsMm"],1,16);foreach(var a in GunnerAnchors)BuffWire.Need(a.Inside(rmin,rmax),"b_geometry_gunner_anchor");foreach(var row in BuffWire.Array(p["obstacles"],0,32)){var o=BuffWire.Object(row);BuffWire.Fields(o,"minMm","maxMm");var a=BMmPoint.Read(o["minMm"]);var b=BMmPoint.Read(o["maxMm"]);BuffWire.Need(a.X<b.X&&a.Y<b.Y&&a.Z<b.Z,"b_geometry_obstacle");}
  }
  internal static long Length(BMmPoint[] points){long n=0;for(int i=1;i<points.Length;i++)n=checked(n+BMmPoint.Distance(points[i-1],points[i]));return n;}
  internal static BMmPoint OnPath(BMmPoint[] points,long distance){for(int i=1;i<points.Length;i++){long length=BMmPoint.Distance(points[i-1],points[i]);if(distance<=length)return BMmPoint.Move(points[i-1],points[i],distance);distance-=length;}return points.Last();}
  internal static bool SegmentHits(BMmPoint a,BMmPoint b,BMmPoint min,BMmPoint max){BigInteger ln=0,ld=1,hn=1,hd=1;var start=new[]{a.X,a.Y,a.Z};var end=new[]{b.X,b.Y,b.Z};var lower=new[]{min.X,min.Y,min.Z};var upper=new[]{max.X,max.Y,max.Z};for(int i=0;i<3;i++){long d=end[i]-start[i];if(d==0){if(start[i]<lower[i]||start[i]>upper[i])return false;continue;}BigInteger x=lower[i]-start[i],y=upper[i]-start[i],den=d;if(den<0){den=-den;var z=x;x=-y;y=-z;}if(x*ld>ln*den){ln=x;ld=den;}if(y*hd<hn*den){hn=y;hd=den;}if(ln*hd>hn*ld)return false;}return true;}
 }
}
