using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Authority replica and boundary validation only. Does not run artillery, health or success rules.
 public sealed class WorldBossRailGeometrySnapshot {
  readonly JObject state;readonly MainlineDetourPath path;
  public JObject Capture()=>(JObject)state.DeepClone();
  public string SelectedRoute=>(string)state["selectedRoute"];
  public bool NextBranchAllowed=>(bool)state["nextBranchAllowed"];
  public long AtMs=>(long)state["sample"]["atMs"];
  public WorldBossRailGeometrySnapshot(JObject input,JObject trustedProfile){
   Check(input!=null&&WorldBossRailSnapshot.Canonical(input).Length<=8388608);
   state=(JObject)input.DeepClone();path=new MainlineDetourPath(trustedProfile);
   WorldBossRailSnapshot.Fields(state,"version","profile","initial","selectedRoute","selectedAtSequence","physicalRoute","sample","samples","vehicles","events","attackResult","nextBranchAllowed");
   Check((string)state["version"]==MainlineDetourPath.Version&&Same(state["profile"],trustedProfile));
   Check(new[]{"ENTRY","LEFT","RIGHT"}.Contains(SelectedRoute));string side=SelectedRoute=="RIGHT"?"RIGHT":"LEFT";Check((string)state["physicalRoute"]==side);
   var samples=state["samples"] as JArray;Check(samples!=null&&samples.Count>0&&samples.Count<=36001&&Same(samples[0],state["initial"])&&Same(samples.Last,state["sample"]));
   var initial=state["initial"] as JObject;new RailConsistMotion(path,initial,Math.Min(2000,path.MaxAcceleration),Math.Min(4000,path.MaxAcceleration));
   long? selection=null;if(state["selectedAtSequence"].Type!=JTokenType.Null)selection=N(state,"selectedAtSequence",36000);
   Check((SelectedRoute=="ENTRY")==!selection.HasValue);Check(!selection.HasValue||selection.Value<samples.Count&&(long)samples[(int)selection.Value]["headDistanceMm"]<path.SplitSmm);
   var expectedEvents=new JArray();JObject previous=null;
   foreach(JObject sample in samples){Sample(sample,path);Check((long)sample["sequence"]==(previous==null?0:(long)previous["sequence"]+1));long head=(long)sample["headDistanceMm"];Check(head>=path.ConsistLengthMm&&head<=path.LengthMm(side));
    if(previous!=null){long dt=(long)sample["atMs"]-(long)previous["atMs"],v=(long)sample["actualMmPerSecond"],oldV=(long)previous["actualMmPerSecond"];
     Check(dt>0&&dt<=path.MaxSampleMs&&Math.Abs(v-oldV)*1000<=path.MaxAcceleration*dt);long num=(oldV+v)*dt+(long)previous["distanceRemainder2000"];
     Check(head==(long)previous["headDistanceMm"]+num/2000&&(long)sample["distanceRemainder2000"]==num%2000);
     Observe(expectedEvents,path,"HEAD_SPLIT",(long)previous["headDistanceMm"],head,path.SplitSmm,previous,sample);
     Observe(expectedEvents,path,"TAIL_SPLIT",(long)previous["headDistanceMm"]-path.ConsistLengthMm,head-path.ConsistLengthMm,path.SplitSmm,previous,sample);
     Observe(expectedEvents,path,"TAIL_MERGE",(long)previous["headDistanceMm"]-path.ConsistLengthMm,head-path.ConsistLengthMm,path.MergeSmm(side),previous,sample);
    }previous=sample;
   }
   Check(Same(expectedEvents,state["events"]));Check(state["nextBranchAllowed"].Type==JTokenType.Boolean&&NextBranchAllowed==((long)state["sample"]["headDistanceMm"]-path.ConsistLengthMm>=path.MergeSmm(side)));
   var vehicles=state["vehicles"] as JArray;Check(vehicles!=null&&vehicles.Count==path.Vehicles.Count);long offset=0;
   for(int i=0;i<vehicles.Count;i++){var v=vehicles[i] as JObject;var shape=path.Vehicles[i];WorldBossRailSnapshot.Fields(v,"id","lengthMm","gapAfterMm","halfWidthMm","front","rear");Check((string)v["id"]==shape.Id&&N(v,"lengthMm",1000000000)==shape.LengthMm&&N(v,"gapAfterMm",1000000000)==shape.GapAfterMm&&N(v,"halfWidthMm",1000000000)==shape.HalfWidthMm);long front=(long)state["sample"]["headDistanceMm"]-offset;Pose(v["front"] as JObject,path.At(side,front));Pose(v["rear"] as JObject,path.At(side,front-shape.LengthMm));offset+=shape.LengthMm+shape.GapAfterMm;}
   if(state["attackResult"].Type!=JTokenType.Null){var a=state["attackResult"] as JObject;WorldBossRailSnapshot.Fields(a,"eventId","observedAtMs","hit","hitVehicles");Check((string)a["eventId"]==EventId(path,"ATTACK")&&N(a,"observedAtMs",9007199254740991)==(long)trustedProfile["attack"]["impactAtMs"]&&samples.Any(s=>(long)s["atMs"]==(long)a["observedAtMs"]));var hits=a["hitVehicles"] as JArray;Check(hits!=null&&hits.All(h=>h.Type==JTokenType.String&&path.Vehicles.Any(v=>v.Id==(string)h))&&hits.Select(h=>(string)h).Distinct().Count()==hits.Count&&a["hit"].Type==JTokenType.Boolean&&(bool)a["hit"]==(hits.Count>0));}
  }
  internal static bool Same(JToken a,JToken b)=>a==null||b==null?ReferenceEquals(a,b):WorldBossRailSnapshot.Canonical(a)==WorldBossRailSnapshot.Canonical(b);
  internal static void Check(bool valid){if(!valid)throw new ArgumentException("world_boss_geometry_invalid");}
  internal static long N(JObject o,string k,long max)=>MainlineDetourPath.Natural(o,k,max);
  static string EventId(MainlineDetourPath p,string kind)=>new JArray(p.GeometryId,p.RoundId,kind).ToString(Newtonsoft.Json.Formatting.None);
  static void Observe(JArray events,MainlineDetourPath p,string kind,long old,long current,long threshold,JObject before,JObject after){if(old<threshold&&current>=threshold&&!events.Any(e=>(string)e["kind"]==kind))events.Add(new JObject{["eventId"]=EventId(p,kind),["kind"]=kind,["previousSampleAtMs"]=(long)before["atMs"],["observedAtMs"]=(long)after["atMs"]});}
  internal static void Sample(JObject s,MainlineDetourPath p){WorldBossRailSnapshot.Fields(s,"atMs","sequence","headDistanceMm","distanceRemainder2000","actualMmPerSecond","targetMmPerSecond","forcedSlowdownBps");N(s,"atMs",9007199254740991);N(s,"sequence",36000);N(s,"headDistanceMm",1000000000);N(s,"distanceRemainder2000",1999);N(s,"actualMmPerSecond",p.MaxSpeed);N(s,"targetMmPerSecond",p.MaxSpeed);N(s,"forcedSlowdownBps",10000);}
  static void Pose(JObject actual,RailPathPoint expected){WorldBossRailSnapshot.Fields(actual,"sMm","xMm","yMm");Check(N(actual,"sMm",1000000000)==expected.ArcMm);foreach(string key in new[]{"xMm","yMm"}){Check(actual[key].Type==JTokenType.Integer||actual[key].Type==JTokenType.Float);double n=(double)actual[key],e=(double)(key=="xMm"?expected.ForwardMm:expected.DriverRightMm);Check(!double.IsNaN(n)&&!double.IsInfinity(n)&&Math.Abs(n-e)<=0.000001);}}
  // Association checks an authority response; its hit decision is consumed, never recomputed here.
  internal void ValidateAssociation(WorldBossRailSnapshot rail,JObject bound){
   var r=rail.Capture();var d=rail.Definition;string round=path.RoundId;
   Check(path.GeometryId==new JArray(rail.AttemptId,round).ToString(Newtonsoft.Json.Formatting.None)&&(string)state["profile"]["contentVersion"]==(string)d["contentVersion"]);
   var cfg=((JArray)d["rounds"]).SingleOrDefault(x=>(string)x["roundId"]==round);Check(cfg!=null);
   var current=((JArray)r["rounds"]).Single(x=>(string)x["roundId"]==round);Check((string)current["route"]==SelectedRoute);
   var receipt=((JArray)r["receipts"]).Select(x=>x["receipt"]).SingleOrDefault(x=>(string)x["roundId"]==round&&x["internalAction"].Type==JTokenType.Null);
   if(SelectedRoute=="ENTRY")Check(receipt==null);else{Check(receipt!=null);var selected=state["samples"][(int)state["selectedAtSequence"]];Check((long)selected["atMs"]==(long)receipt["acceptedAt"]&&(string)receipt["route"]==SelectedRoute);}
   if(bound==null)return;
   WorldBossRailSnapshot.Fields(bound,"version","status","result","geometryEvidence");Check((string)bound["version"]==MainlineDetourPath.Version&&(string)bound["status"]=="LOCAL_PATH_VERIFIED");
   var result=((JArray)r["results"]).SingleOrDefault(x=>(string)x["roundId"]==round);var attack=state["attackResult"] as JObject;Check(result!=null&&attack!=null&&Same(bound["result"],result));
   Check((bool)result["railSuccess"]!=(bool)attack["hit"]&&(long)attack["observedAtMs"]>=(long)d["startedAt"]+(long)cfg["lockAt"]&&(long)attack["observedAtMs"]<=(long)result["resolvedAt"]);
   var expected=new JObject{["geometryRef"]=path.GeometryId,["roundId"]=round,["physicalRoute"]=state["physicalRoute"].DeepClone(),["attack"]=attack.DeepClone(),["events"]=state["events"].DeepClone(),["nextBranchAllowed"]=NextBranchAllowed};Check(Same(bound["geometryEvidence"],expected));
  }
 }
 // Keeps separate partial impact and complete traversal flags exactly as supplied by authority.
 public sealed class WorldBossRailTraversalEvidence {
  readonly JObject state;
  public bool ImpactSupported=>(bool)state["impactSupported"];public bool Supported=>(bool)state["supported"];
  public JObject Capture()=>(JObject)state.DeepClone();
  public WorldBossRailTraversalEvidence(JObject report){WorldBossRailSnapshot.Fields(report,"supported","impactSupported","reasons","forbiddenHeadWindows","safeHeadWindows","headAtLockMax","minimumHeadAtImpact","maximumHeadAtImpact","minimumRearAtImpact","minimumRearAtNext");WorldBossRailGeometrySnapshot.Check(report["supported"].Type==JTokenType.Boolean&&report["impactSupported"].Type==JTokenType.Boolean&&report["reasons"] is JArray reasons&&reasons.All(x=>x.Type==JTokenType.String)&&((bool)report["supported"]==(reasons.Count==0))&&(!(bool)report["supported"]||(bool)report["impactSupported"]));foreach(string key in new[]{"headAtLockMax","minimumHeadAtImpact","maximumHeadAtImpact","minimumRearAtImpact","minimumRearAtNext"}){var t=report[key];WorldBossRailGeometrySnapshot.Check(t.Type==JTokenType.Integer||t.Type==JTokenType.Float);double n=(double)t;WorldBossRailGeometrySnapshot.Check(!double.IsNaN(n)&&!double.IsInfinity(n));}WorldBossRailGeometrySnapshot.Check(report["forbiddenHeadWindows"] is JArray&&report["safeHeadWindows"] is JArray);state=(JObject)report.DeepClone();}
 }
}

