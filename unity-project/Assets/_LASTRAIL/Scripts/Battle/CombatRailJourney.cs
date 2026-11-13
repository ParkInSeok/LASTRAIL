using System;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
// Explicit versioned rail inputs only. No random offers or reinterpretation of departure packages.
public sealed class CombatRailJourney {
 const long TicksPerSecond=10000000;
 readonly JObject config;
 readonly JArray offers;
 readonly int scaleBps,recoveryMs,minimum,maximum,speed;
 int offerIndex,previewIndex,throttle;
 long recoveryTicks,distanceMm,currentTick;
 int completedWaves;
 decimal fraction,distanceRemainder;
 string phase="Cruising";
 JArray confirmed=new JArray(),journal=new JArray(),routeDistances=new JArray();
 public JObject Configuration=>(JObject)config.DeepClone();
 public bool Selecting=>phase=="Selecting";
 public int Bonus(string stat)=>confirmed.Sum(x=>(string)x["stat"]==stat?(int)x["valueBps"]:0);
 static int Int(JObject value,string key,int min,int max){if(value?[key]?.Type!=JTokenType.Integer)throw new ArgumentException(key);long n=(long)value[key];if(n<min||n>max)throw new ArgumentException(key);return (int)n;}
 static string Text(JToken value,string key){var s=(string)value?[key];if(value?[key]?.Type!=JTokenType.String||string.IsNullOrWhiteSpace(s)||s.Length>512)throw new ArgumentException(key);return s;}
 public CombatRailJourney(JObject input){
  if(input==null||(string)input["version"]!="rail-journey-v1")throw new ArgumentException("Rail configuration version");
  config=(JObject)input.DeepClone();scaleBps=Int(config,"selectionScaleBps",1,10000);recoveryMs=Int(config,"recoveryMs",1,10000);speed=Int(config,"baseSpeedMillimetersPerSecond",1,1000000);minimum=Int(config,"throttleMinimumBps",1,10000);maximum=Int(config,"throttleMaximumBps",10000,13000);throttle=config["throttleCruiseBps"]==null?10000:Int(config,"throttleCruiseBps",minimum,maximum);
  offers=config["offers"] as JArray??throw new ArgumentException("Offers required");if(offers.Count>64)throw new ArgumentException("Offer limit");int previous=0;
  foreach(var token in offers){var offer=token as JObject??throw new ArgumentException();Text(offer,"id");int wave=Int(offer,"afterCompletedWaves",1,10000);if(wave<=previous)throw new ArgumentException("Sequential offers required");previous=wave;var options=offer["options"] as JArray;if(options==null||options.Count<2||options.Count>4)throw new ArgumentException("Option count");
   foreach(var option in options){Text(option,"id");Text(option,"routeId");Text(option,"label");Text(option,"description");Text(option,"augmentId");var effect=option["effect"] as JObject;if((string)effect?["kind"]!="ADD_BPS"||!new[]{"SQUAD_DAMAGE","TRAIN_DAMAGE","SHIELD_MAGNITUDE"}.Contains((string)effect["stat"]))throw new ArgumentException("Unsupported rail effect");Int(effect,"valueBps",1,3000);}
   if(options.Select(x=>(string)x["id"]).Distinct().Count()!=options.Count)throw new ArgumentException("Duplicate options");
  }
  if(offers.Select(x=>(string)x["id"]).Distinct().Count()!=offers.Count)throw new ArgumentException("Duplicate offers");
 }
 JObject Offer=>offerIndex<offers.Count?(JObject)offers[offerIndex]:null;
 void Event(string kind,string offer=null,string option=null,int? throttleBps=null){var row=new JObject{["sequence"]=journal.Count+1,["kind"]=kind,["tick"]=currentTick,["completedWaves"]=completedWaves};if(offer!=null)row["offerId"]=offer;if(option!=null)row["optionId"]=option;if(throttleBps.HasValue)row["throttleBps"]=throttleBps.Value;journal.Add(row);}
 public void ObserveCompletedWaves(int count)=>CheckBoundary(count,currentTick);
 public void CheckBoundary(int count,long tick){if(count<completedWaves||tick<currentTick)throw new ArgumentException("Nonmonotonic rail boundary");if(Offer!=null&&count>(int)Offer["afterCompletedWaves"])throw new ArgumentException("Skipped pending junction");completedWaves=count;currentTick=tick;if(phase=="Cruising"&&Offer!=null&&count==(int)Offer["afterCompletedWaves"]){phase="Selecting";previewIndex=0;Event("OPEN",(string)Offer["id"]);}}
 public void Preview(int index){if(!Selecting||index<0||index>=((JArray)Offer["options"]).Count)throw new ArgumentException("No offered route");if(index==previewIndex)return;previewIndex=index;Event("PREVIEW",(string)Offer["id"],(string)Offer["options"][index]["id"]);}
 public bool Confirm(string offerId,string optionId,long tick){
  var duplicate=confirmed.FirstOrDefault(x=>(string)x["offerId"]==offerId);if(duplicate!=null){if((string)duplicate["optionId"]!=optionId)throw new ArgumentException("Conflicting route confirmation");return false;}
  if(!Selecting||(string)Offer["id"]!=offerId||tick<currentTick)throw new ArgumentException("No pending offer");var options=(JArray)Offer["options"];var option=options.SingleOrDefault(x=>(string)x["id"]==optionId);if(option==null)throw new ArgumentException("Unknown route");currentTick=tick;Event("CONFIRM",offerId,optionId);
  confirmed.Add(new JObject{["offerId"]=offerId,["optionId"]=optionId,["routeId"]=option["routeId"].DeepClone(),["augmentId"]=option["augmentId"].DeepClone(),["stat"]=option["effect"]["stat"].DeepClone(),["valueBps"]=option["effect"]["valueBps"].DeepClone(),["atTick"]=tick,["sequence"]=journal.Count});
  routeDistances.Add(distanceMm);offerIndex++;previewIndex=0;phase="Recovering";recoveryTicks=0;return true;
 }
 public void SetThrottle(int bps){if(bps<minimum||bps>maximum)throw new ArgumentOutOfRangeException(nameof(bps));if(throttle==bps)return;throttle=bps;Event("THROTTLE",throttleBps:bps);}
 // Integral of smoothstep 3u²-2u³, so frame partition does not change combat time.
 static decimal Integral(decimal u)=>u*u*u-u*u*u*u/2m;
 public long TransformElapsed(long elapsed,bool paused){
  if(elapsed<0)throw new ArgumentOutOfRangeException(nameof(elapsed));if(paused||elapsed==0)return 0;
  decimal credit;decimal low=scaleBps/10000m;
  if(Selecting)credit=elapsed*low;
  else if(phase=="Recovering"){
   long duration=recoveryMs*10000L,part=Math.Min(elapsed,duration-recoveryTicks);decimal a=recoveryTicks/(decimal)duration,b=(recoveryTicks+part)/(decimal)duration;
   credit=part*low+(1m-low)*duration*(Integral(b)-Integral(a))+(elapsed-part);recoveryTicks+=part;
   if(recoveryTicks==duration)phase="Cruising";
  }else credit=elapsed;
  credit+=fraction;var ticks=checked((long)decimal.Floor(credit));fraction=credit-ticks;return ticks;
 }
 public void AdvanceDistance(long simulationTicks){if(simulationTicks<0)throw new ArgumentOutOfRangeException(nameof(simulationTicks));if(Selecting)return;decimal mm=simulationTicks*(decimal)speed*throttle/(TicksPerSecond*10000m)+distanceRemainder;long whole=checked((long)decimal.Floor(mm));distanceMm=checked(distanceMm+whole);distanceRemainder=mm-whole;}
 public JObject Capture()=>new JObject{["version"]="rail-journey-state-v1",["phase"]=phase,["offerIndex"]=offerIndex,["previewIndex"]=previewIndex,["throttleBps"]=throttle,["recoveryTicks"]=recoveryTicks,["fractionTicks"]=fraction.ToString("G29",CultureInfo.InvariantCulture),["distanceMm"]=distanceMm,["distanceRemainder"]=distanceRemainder.ToString("G29",CultureInfo.InvariantCulture),["currentTick"]=currentTick,["completedWaves"]=completedWaves,["routeDistancesMm"]=routeDistances.DeepClone(),["confirmed"]=confirmed.DeepClone(),["journal"]=journal.DeepClone()};
 public JObject ReadHud(){var state=Capture();state["offer"]=Selecting?Offer.DeepClone():JValue.CreateNull();state["minimumBps"]=minimum;state["maximumBps"]=maximum;state["speedMmPerSecond"]=speed*throttle/10000;state["recoveryMs"]=recoveryMs;state["carriages"]=config["carriages"]?.DeepClone()??new JArray();state["routeSide"]=offerIndex==0?0:((JArray)offers[offerIndex-1]["options"]).ToList().FindIndex(o=>(string)o["id"]==(string)confirmed.Last["optionId"])%2==0?-1:1;return state;}
 public void Restore(JObject value){
  if(value==null||(string)value["version"]!="rail-journey-state-v1")throw new ArgumentException("Rail state version");
  int oi=Int(value,"offerIndex",0,offers.Count),pi=Int(value,"previewIndex",0,3),th=Int(value,"throttleBps",minimum,maximum);string ph=Text(value,"phase");if(!new[]{"Cruising","Selecting","Recovering"}.Contains(ph))throw new ArgumentException("Rail phase");
  long rt=(long?)value["recoveryTicks"]??-1,dist=(long?)value["distanceMm"]??-1,ct=(long?)value["currentTick"]??-1;decimal frac=ParseFraction(value,"fractionTicks"),dr=ParseFraction(value,"distanceRemainder");int cw=Int(value,"completedWaves",0,10000);
  if(rt<0||rt>recoveryMs*10000L||dist<0||ct<0||frac<0||frac>=1||dr<0||dr>=1||ph=="Selecting"&&(oi==offers.Count||pi>=((JArray)offers[oi]["options"]).Count||cw!=(int)offers[oi]["afterCompletedWaves"])||ph=="Recovering"&&(oi==0||rt>=recoveryMs*10000L))throw new ArgumentException("Invalid rail state");
  var choices=value["confirmed"] as JArray;var events=value["journal"] as JArray;if(choices==null||choices.Count!=oi||events==null||events.Count>10000)throw new ArgumentException("Rail journal");
  long previousTick=0;for(int i=0;i<events.Count;i++){var row=events[i];long t=(long?)row["tick"]??-1;if((int?)row["sequence"]!=i+1||t<previousTick||t>ct||!new[]{"OPEN","PREVIEW","CONFIRM","THROTTLE"}.Contains((string)row["kind"]))throw new ArgumentException("Rail event order");previousTick=t;}
  for(int i=0;i<choices.Count;i++){var row=choices[i];var option=((JArray)offers[i]["options"]).SingleOrDefault(x=>(string)x["id"]==(string)row["optionId"]);if((string)row["offerId"]!=(string)offers[i]["id"]||option==null||(string)row["routeId"]!=(string)option["routeId"]||(string)row["augmentId"]!=(string)option["augmentId"]||(string)row["stat"]!=(string)option["effect"]["stat"]||(int?)row["valueBps"]!=(int?)option["effect"]["valueBps"]||(long?)row["atTick"]>ct)throw new ArgumentException("Rail effect/config mismatch");}
  var marks=value["routeDistancesMm"] as JArray;if(marks==null||marks.Count!=oi)throw new ArgumentException("Route distance history missing");long last=0;foreach(var mark in marks){if(mark.Type!=JTokenType.Integer||(long)mark<last||(long)mark>dist)throw new ArgumentException("Invalid route distance history");last=(long)mark;}ValidateJournal(events,choices,oi,pi,th,ph,rt,cw);routeDistances=(JArray)marks.DeepClone();
  phase=ph;offerIndex=oi;previewIndex=pi;throttle=th;recoveryTicks=rt;distanceMm=dist;fraction=frac;distanceRemainder=dr;currentTick=ct;completedWaves=cw;confirmed=(JArray)choices.DeepClone();journal=(JArray)events.DeepClone();
 }
 static decimal ParseFraction(JObject state,string key){if(state[key]?.Type!=JTokenType.String||!decimal.TryParse((string)state[key],NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var result)||result<0||result>=1||result.ToString("G29",CultureInfo.InvariantCulture)!=(string)state[key])throw new ArgumentException("Noncanonical fractional state");return result;}
 void ValidateJournal(JArray events,JArray choices,int index,int preview,int speedBps,string finalPhase,long restoredRecovery,int waves){
  var replay=new CombatRailJourney(config);
  foreach(var token in events){var row=token as JObject??throw new ArgumentException("Rail event object");long tick=(long)row["tick"];int count=Int(row,"completedWaves",replay.completedWaves,waves);if(replay.Offer!=null&&count>(int)replay.Offer["afterCompletedWaves"]||replay.Selecting&&count!=(int)replay.Offer["afterCompletedWaves"])throw new ArgumentException("Journal skips junction");replay.currentTick=tick;replay.completedWaves=count;string kind=Text(row,"kind");
   if(kind=="OPEN"){
    if(replay.Selecting||replay.Offer==null||(string)replay.Offer["id"]!=(string)row["offerId"])throw new ArgumentException("Unexpected offer open");
    replay.phase="Cruising";replay.CheckBoundary(count,tick);
   }else if(kind=="PREVIEW"){
    if(!replay.Selecting||(string)row["offerId"]!=(string)replay.Offer["id"])throw new ArgumentException("Preview without open");
    var options=(JArray)replay.Offer["options"];int selected=options.ToList().FindIndex(x=>(string)x["id"]==(string)row["optionId"]);replay.Preview(selected);
   }else if(kind=="CONFIRM"){
    if(!replay.Confirm(Text(row,"offerId"),Text(row,"optionId"),tick))throw new ArgumentException("Duplicate journal confirmation");
   }else replay.SetThrottle(Int(row,"throttleBps",minimum,maximum));
  }
  if(!JToken.DeepEquals(replay.journal,events)||!JToken.DeepEquals(replay.confirmed,choices)||replay.offerIndex!=index||replay.previewIndex!=preview||replay.throttle!=speedBps||replay.Selecting!=(finalPhase=="Selecting"))throw new ArgumentException("Rail journal/state mismatch");
  if(finalPhase=="Cruising"&&restoredRecovery!=(index==0?0:recoveryMs*10000L)||finalPhase=="Recovering"&&(index==0||restoredRecovery>=recoveryMs*10000L))throw new ArgumentException("Rail recovery state mismatch");
 }
}
}
