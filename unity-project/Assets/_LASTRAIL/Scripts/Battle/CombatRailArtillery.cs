using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Independent optional encounter clock. Exiting never cancels an in-flight shot.
public sealed class CombatRailArtillery {
 readonly JObject configuration;readonly int maxHp,damage,period,windup,travel;
 JArray encounters=new JArray(),projectiles=new JArray(),events=new JArray();long tick,distance,sequence;int wave;bool terminal;
 public bool HasProjectiles=>projectiles.Count>0;
 public JObject Configuration=>(JObject)configuration.DeepClone();
 public CombatRailArtillery(JObject input){
  if((string)input?["version"]!="rail-artillery-v1"||(int?)input["stepMs"]!=50)throw new ArgumentException("Artillery config");configuration=(JObject)input.DeepClone();var d=input["definition"];
  maxHp=Positive(d,"maxHp");damage=Positive(d,"trainDamage");period=Positive(d,"periodMs")/50;windup=Positive(d,"windupMs")/50;travel=Positive(d,"projectileTravelMs")/50;
  if(period<windup||windup<1||travel<1||new[]{"periodMs","windupMs","projectileTravelMs"}.Any(k=>(int)d[k]%50!=0))throw new ArgumentException("Whole artillery steps required");
  var rows=input["encounters"] as JArray;if(rows==null||rows.Count>15)throw new ArgumentException("Artillery encounters");
  int prior=0;foreach(var row in rows){if((int?)row["spawnWave"]<=prior||(int?)row["spawnWave"]>15||Positive(row,"exposureDistanceMm")<1||string.IsNullOrWhiteSpace((string)row["id"])||(string)row["definitionId"]!=(string)d["id"])throw new ArgumentException("Artillery identity");prior=(int)row["spawnWave"];encounters.Add(new JObject{["id"]=row["id"].DeepClone(),["status"]="Waiting",["hp"]=maxHp,["spawnTick"]=-1,["startDistanceMm"]=-1,["nextLaunch"]=-1});}
  if(rows.Select(x=>(string)x["id"]).Distinct().Count()!=rows.Count)throw new ArgumentException("Duplicate artillery");
 }
 static int Positive(JToken o,string key){if(o?[key]?.Type!=JTokenType.Integer||(long)o[key]<1||(long)o[key]>100000000)throw new ArgumentException(key);return (int)o[key];}
 JObject Find(string id)=>encounters.OfType<JObject>().SingleOrDefault(x=>(string)x["id"]==id)??throw new ArgumentException("Unknown artillery");
 public int Damage(string id,int amount){if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));var row=Find(id);if((string)row["status"]!="Active")return 0;int loss=Math.Min((int)row["hp"],amount);row["hp"]=(int)row["hp"]-loss;return loss;}
 public void SynchronizeHealth(string id,int hp){var row=Find(id);if(hp<0||hp>(int)row["hp"])throw new ArgumentException("Artillery health cannot increase");row["hp"]=hp;}
 void Event(string kind,string id)=>events.Add(new JObject{["kind"]=kind,["id"]=id,["tick"]=tick,["distanceMm"]=distance});
 public JArray Advance(long nextTick,int currentWave,long distanceMm,bool terminalRequested){
  if(nextTick<tick||distanceMm<distance||currentWave<wave)throw new ArgumentException("Artillery clock regression");tick=nextTick;distance=distanceMm;wave=currentWave;if(terminalRequested&&!terminal)Event("TERMINAL_REQUEST","battle");terminal|=terminalRequested;
  var definitions=(JArray)configuration["encounters"];
  for(int i=0;i<encounters.Count;i++){var state=encounters[i];string id=(string)state["id"];
   if((string)state["status"]=="Waiting"&&!terminal&&wave>=(int)definitions[i]["spawnWave"]){state["status"]="Active";state["spawnTick"]=tick;state["startDistanceMm"]=distance;state["nextLaunch"]=tick+windup;Event("SPAWN",id);}
   if((string)state["status"]!="Active")continue;
   if((int)state["hp"]==0){state["status"]="Destroyed";Event("DESTROYED",id);}
   else if(distance-(long)state["startDistanceMm"]>=(long)definitions[i]["exposureDistanceMm"]){state["status"]="Passed";Event("PASSED",id);}
  }
  foreach(var state in encounters){if((string)state["status"]!="Active"||terminal||(long)state["nextLaunch"]>tick)continue;projectiles.Add(new JObject{["sequence"]=++sequence,["source"]=(string)state["id"],["launchedTick"]=tick,["dueTick"]=tick+travel,["damage"]=damage});state["nextLaunch"]=tick+period;Event("LAUNCH",(string)state["id"]);}
  var impacts=new JArray();foreach(var shot in projectiles.Where(p=>(long)p["dueTick"]<=tick).OrderBy(p=>(long)p["dueTick"]).ThenBy(p=>(long)p["sequence"]).ToArray()){impacts.Add(shot.DeepClone());Event("IMPACT",(string)shot["source"]);shot.Remove();}return impacts;
 }
 public JObject Capture()=>new JObject{["version"]="rail-artillery-state-v1",["tick"]=tick,["wave"]=wave,["distanceMm"]=distance,["sequence"]=sequence,["terminalRequested"]=terminal,["destroyedCount"]=encounters.Count(x=>(string)x["status"]=="Destroyed"),["encounters"]=encounters.DeepClone(),["projectiles"]=projectiles.DeepClone(),["events"]=events.DeepClone()};
 public void Restore(JObject input){
  if((string)input?["version"]!="rail-artillery-state-v1")throw new ArgumentException("Artillery state version");long t=(long?)input["tick"]??-1,d=(long?)input["distanceMm"]??-1,seq=(long?)input["sequence"]??-1;int w=(int?)input["wave"]??-1;
  var rows=input["encounters"] as JArray;var shots=input["projectiles"] as JArray;var log=input["events"] as JArray;if(t<0||d<0||seq<0||w<0||w>15||rows==null||rows.Count!=encounters.Count||shots==null||log==null||log.Count>100000||input["terminalRequested"]?.Type!=JTokenType.Boolean)throw new ArgumentException("Artillery state bounds");
  for(int i=0;i<rows.Count;i++){var row=rows[i];if((string)row["id"]!=(string)encounters[i]["id"]||!new[]{"Waiting","Active","Destroyed","Passed"}.Contains((string)row["status"])||(int?)row["hp"]<0||(int?)row["hp"]>maxHp||(string)row["status"]=="Destroyed"&&(int?)row["hp"]!=0)throw new ArgumentException("Artillery actor mismatch");}
  foreach(var shot in shots){Find((string)shot["source"]);if((long?)shot["sequence"]<1||(long?)shot["sequence"]>seq||(long?)shot["launchedTick"]<0||(long?)shot["launchedTick"]>t||(long?)shot["dueTick"]!=(long?)shot["launchedTick"]+travel||(long?)shot["dueTick"]<=t||(int?)shot["damage"]!=damage)throw new ArgumentException("Artillery projectile mismatch");}
  if(shots.Select(x=>(long)x["sequence"]).Distinct().Count()!=shots.Count||(int?)input["destroyedCount"]!=rows.Count(x=>(string)x["status"]=="Destroyed"))throw new ArgumentException("Artillery sequence mismatch");
  ValidateHistory(input,rows,shots,log,t,d,w,seq);
  tick=t;distance=d;wave=w;sequence=seq;terminal=(bool)input["terminalRequested"];encounters=(JArray)rows.DeepClone();projectiles=(JArray)shots.DeepClone();events=(JArray)log.DeepClone();
 }
 static long Integer(JToken row,string key,long min,long max){if(row?[key]?.Type!=JTokenType.Integer)throw new ArgumentException("Missing artillery integer "+key);long n=(long)row[key];if(n<min||n>max)throw new ArgumentException("Artillery integer range "+key);return n;}
 void ValidateHistory(JObject input,JArray rows,JArray shots,JArray log,long t,long d,int w,long seq){
  foreach(string key in new[]{"tick","distanceMm","sequence","destroyedCount","wave"})Integer(input,key,0,long.MaxValue);
  var statuses=rows.ToDictionary(x=>(string)x["id"],x=>"Waiting");var starts=new System.Collections.Generic.Dictionary<string,JObject>();var lastLaunch=new System.Collections.Generic.Dictionary<string,long>();var pending=new JArray();long emitted=0,priorTick=0,priorDistance=0;bool stopped=false;
  foreach(var token in log){var e=token as JObject??throw new ArgumentException("Artillery event");if(e.Count!=4||e["kind"]?.Type!=JTokenType.String||e["id"]?.Type!=JTokenType.String)throw new ArgumentException("Artillery event fields");long et=Integer(e,"tick",priorTick,t),ed=Integer(e,"distanceMm",priorDistance,d);priorTick=et;priorDistance=ed;string id=(string)e["id"],kind=(string)e["kind"];
   if(kind=="TERMINAL_REQUEST"){if(stopped||id!="battle")throw new ArgumentException("Terminal event mismatch");stopped=true;continue;}
   if(!statuses.ContainsKey(id))throw new ArgumentException("Unknown artillery event actor");
   if(kind=="SPAWN"){if(stopped||statuses[id]!="Waiting")throw new ArgumentException("Duplicate artillery spawn");statuses[id]="Active";starts[id]=e;}
   else if(kind=="DESTROYED"||kind=="PASSED"){if(statuses[id]!="Active")throw new ArgumentException("Artillery exit order");statuses[id]=kind=="DESTROYED"?"Destroyed":"Passed";}
   else if(kind=="LAUNCH"){if(stopped||statuses[id]!="Active"||et<(lastLaunch.TryGetValue(id,out var last)?last+period:(long)starts[id]["tick"]+windup))throw new ArgumentException("Artillery launch timing");lastLaunch[id]=et;pending.Add(new JObject{["sequence"]=++emitted,["source"]=id,["launchedTick"]=et,["dueTick"]=et+travel,["damage"]=damage});}
   else if(kind=="IMPACT"){var shot=pending.FirstOrDefault(x=>(string)x["source"]==id&&(long)x["dueTick"]<=et);if(shot==null)throw new ArgumentException("Impact without launched projectile");shot.Remove();}
   else throw new ArgumentException("Unknown artillery event");
  }
  if(stopped!=(bool)input["terminalRequested"]||emitted!=seq||!JToken.DeepEquals(pending,shots))throw new ArgumentException("Artillery projectile history mismatch");
  for(int i=0;i<rows.Count;i++){var row=rows[i];string id=(string)row["id"],status=(string)row["status"];long hp=Integer(row,"hp",0,maxHp),spawn=Integer(row,"spawnTick",-1,t),start=Integer(row,"startDistanceMm",-1,d),next=Integer(row,"nextLaunch",-1,long.MaxValue);if(row is not JObject obj||obj.Count!=6||status!=statuses[id])throw new ArgumentException("Artillery status/history mismatch");
   if(status=="Waiting"){if(hp!=maxHp||spawn!=-1||start!=-1||next!=-1||!stopped&&w>=(int)configuration["encounters"][i]["spawnWave"])throw new ArgumentException("Invalid waiting artillery");}
   else{if(spawn!=(long)starts[id]["tick"]||start!=(long)starts[id]["distanceMm"]||next!=(lastLaunch.TryGetValue(id,out var at)?at+period:spawn+windup)||status=="Active"&&hp==0||status=="Passed"&&(hp==0||d-start<(long)configuration["encounters"][i]["exposureDistanceMm"])||status=="Destroyed"&&hp!=0)throw new ArgumentException("Invalid artillery timing/exit state");}
  }
 }
}
}
