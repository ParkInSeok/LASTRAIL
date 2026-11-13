using System;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 /// <summary>Immutable geometric reservations only; no augment ownership, RNG or HP authority.</summary>
 public sealed class MiraPierceReservation {
  public const string Version="mira.pierce-reservation.v3.20261016";
  readonly JObject snapshot;
  MiraPierceReservation(JObject value){snapshot=value;}
  public JObject Capture()=>(JObject)snapshot.DeepClone();
  public static MiraPierceReservation Create(JObject observation,string pinnedHash,JArray primaryIds){
   // O08 multi-body geometry requires its own existing ability adapter. Do not
   // treat a list of arbitrary enemies as a supported original body attack.
   if(primaryIds==null||primaryIds.Count!=1||primaryIds[0].Type!=JTokenType.String)throw new ArgumentException("First Mira path requires one original primary.");
   string primary=(string)primaryIds[0];
   var ray=MiraJudgementGeometry.CreateRay(observation,pinnedHash,primary);string rayHash=BDirectAttackInput.Digest(ray);
   var targets=(JArray)observation["targets"];
   if(targets.Select(t=>(string)t["id"]).Distinct(StringComparer.Ordinal).Count()!=targets.Count)throw new ArgumentException("Shared HP enemy identity must be unique.");
   long limit=(long)observation["profile"]["ballistics"]["maxFlightUs"];
   var main=targets.Single(t=>(string)t["id"]==primary);
   var mainHit=MiraJudgementGeometry.TraceReserved(ray,rayHash,observation,observation,0,limit,primary,(long)main["generation"]);
   if((string)mainHit["kind"]!="HIT")throw new InvalidOperationException("Original primary is not on frozen path.");
   var origin=(JArray)ray["originMm"];var anchor=(JArray)ray["anchorMm"];
   BigInteger Dot(JToken p){BigInteger sum=0;for(int i=0;i<3;i++)sum+=((BigInteger)(long)p[i]-(long)origin[i])*((BigInteger)(long)anchor[i]-(long)origin[i]);return sum;}
   BigInteger behind=Dot(main["anchorMm"]);
   var selected=targets.Where(t=>(string)t["id"]!=primary&&(bool)t["eligible"]&&(string)t["domain"]==(string)observation["source"]["domain"]
     &&BigInteger.Abs((BigInteger)(long)t["slot"]-(long)observation["source"]["slot"])<=(long)observation["profile"]["ballistics"]["adjacentCars"]&&Dot(t["anchorMm"])>behind)
    .Select(t=>new {Target=t,Trace=MiraJudgementGeometry.TraceReserved(ray,rayHash,observation,observation,0,limit,(string)t["id"],(long)t["generation"])})
    .Where(x=>(string)x.Trace["kind"]=="HIT"&&(double)x.Trace["elapsedUs"]>=(double)mainHit["elapsedUs"])
    .OrderBy(x=>(double)x.Trace["elapsedUs"]).ThenBy(x=>(string)x.Target["id"],StringComparer.Ordinal).Take(2).ToArray();
   var contacts=new JArray();
   void Add(JToken t,int bps){contacts.Add(new JObject{["contactId"]="main-"+contacts.Count,["targetId"]=t["id"].DeepClone(),["targetGeneration"]=t["generation"].DeepClone(),["ordinal"]=contacts.Count,["coefficientBps"]=bps});}
   Add(main,10000);foreach(var candidate in selected)Add(candidate.Target,contacts.Count==1?6000:3000);
   return new MiraPierceReservation(new JObject{["version"]=Version,["ray"]=ray,["rayHash"]=rayHash,["contacts"]=contacts,["excludedTargetIds"]=new JArray(contacts.Select(c=>c["targetId"].DeepClone()))});
  }
 }
}