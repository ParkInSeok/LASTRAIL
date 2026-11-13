#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;

public static class LocalFiveCarSourceBuilder {
 public static void BuildReview(){Check();KyleGuardLocalPlayerBuilder.Build();}
 public static void Exercise(){
  const string folder="D:/Project/LASTRAIL/QA/KyleTauntThree-20261109/local-five-car-v01";
  string output=folder+"/exercise-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(output);
  CombatSceneDriver driver=null;bool current=true;JObject lastHud=null,lastState=null;Exception failure=null;
  int transfers=0,completed=0,effectCount=0,previousShield=0,previousTaunts=0,maxTaunts=0;long expiry=0,absorbedTotal=0;string priorPhase=null;
  var events=new JArray();var summary=new JObject{["scope"]="SAME_OWNER_FULL_HP_LOCAL_SIX_BODY_EXERCISE",["tickLimit"]=6000,["sourceModified"]=false,["databaseWrites"]=0,["visualReview"]=false};
  try{
   var document=JObject.Parse(File.ReadAllText(folder+"/source.json"));
   var replay=typeof(OrdinaryFiveCombatFactory).Assembly.GetType("LastRail.Battle.OrdinaryLocalExecutionReplay",true);
   var input=replay.GetMethod("ReadInput",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{document});
   Func<string,object> field=name=>input.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input);
   var original=(OrdinaryFiveStartInput)field("Original");var node=(OrdinaryNodeStart)field("Node");long step=node.ReadActions(original).StepTimeSpanTicks;
   var factory=OrdinaryFiveCombatFactory.FromNodeStart(original,node,(FirstPlayableScaling)field("Scaling"),step,(OrdinaryNodeAttempt)field("Attempt"));
   factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLiveSources",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(factory,new[]{field("GeometryJson")});
   factory=factory.WithBoardingSource((JObject)field("BoardingSource"),node.BoardingSourceHash);
   factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLocalUninstalledInterceptor",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(factory,null);
   driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>current);driver.Open();driver.SetRailThrottle(7000);
   lastHud=driver.HudState;lastState=JObject.Parse(driver.StateJson);
   var expected=new[]{1400,900,950,920,1000};
   for(int i=0;i<5;i++){var actor=((JArray)lastHud["actors"]).Single(a=>(string)a["id"]==original.Actors[i].OperatorId);if((int)actor["hp"]!=expected[i]||(int)actor["maxHp"]!=expected[i])throw new InvalidOperationException("Full original entry HP differs: "+original.Actors[i].OperatorId);}
   summary["entryFullHpVerified"]=true;summary["entryHp"]=new JArray(expected);
   File.WriteAllText(output+"/entry.json",new JObject{["hud"]=lastHud.DeepClone(),["state"]=lastState.DeepClone()}.ToString());
   using(var samples=new StreamWriter(output+"/samples.jsonl")){
    for(int n=0;n<6000&&driver.Tick<6000&&driver.Outcome==BasicCombatOutcome.Running;n++){
     string obstacle=(string)lastHud["ordinaryObstacle"]?["phase"],phase=(string)lastHud["localParty"]?["phase"];
     if(phase=="Complete"&&completed<transfers){completed=transfers;events.Add(new JObject{["kind"]="TRANSFER_COMPLETE",["tick"]=driver.Tick,["direction"]=completed==1?"LEFT":"RIGHT"});}
     if(obstacle=="COMBAT"&&(transfers==0||transfers==1&&completed==1)){
      driver.RequestLocalPartyEnd(transfers==0);transfers++;events.Add(new JObject{["kind"]="TRANSFER_REQUEST",["tick"]=driver.Tick,["direction"]=transfers==1?"LEFT":"RIGHT"});
     }
     long before=driver.Tick;driver.Advance(step*10,10);if(driver.Tick<=before)throw new InvalidOperationException("Native tick failed to progress");
     lastHud=driver.HudState;lastState=JObject.Parse(driver.StateJson);
     var actors=(JArray)lastHud["actors"];var kyle=actors.Single(a=>(string)a["id"]=="op_kyle");int shield=(int)kyle["kyleGuardShield"];
     int taunts=((JArray)lastState["taunts"]).Where(t=>(string)t["source"]=="op_kyle"&&(long)t["expiry"]>driver.Tick).Select(t=>(string)t["target"]).Distinct().Count();maxTaunts=Math.Max(maxTaunts,taunts);
     var effects=(JArray)lastState["effects"];var fresh=effects.Skip(effectCount).ToArray();long absorbed=fresh.Where(e=>(string)e["targetId"]=="op_kyle").Sum(e=>(long?)e["actualResult"]?["absorbed"]??0);absorbedTotal+=absorbed;
     foreach(var effect in fresh.Where(e=>(string)e["sourceId"]=="op_kyle"&&(string)e["kind"]=="SHIELD_ISSUE")){expiry=(long)effect["actualResult"]["expiry"];events.Add(new JObject{["kind"]="SHIELD_ISSUE",["tick"]=driver.Tick,["effect"]=effect.DeepClone()});}
     if(taunts>=3&&previousTaunts<3)events.Add(new JObject{["kind"]="THREE_LIVE_TAUNTS",["tick"]=driver.Tick,["taunts"]=lastState["taunts"].DeepClone()});
     if(previousShield>0&&shield==0)events.Add(new JObject{["kind"]="SHIELD_END",["tick"]=driver.Tick,["reason"]=(int)kyle["hp"]<=0?"DOWN":driver.Tick>=expiry?"EXPIRY":absorbed>0?"DEPLETION":"UNCLASSIFIED",["absorbedThisTick"]=absorbed,["expiry"]=expiry});
     phase=(string)lastHud["localParty"]?["phase"];
     if(driver.Tick%20==0||phase!=priorPhase||taunts!=previousTaunts||shield!=previousShield)samples.WriteLine(new JObject{["tick"]=driver.Tick,["phase"]=phase,["outcome"]=driver.Outcome.ToString(),["actors"]=new JArray(actors.Where(a=>(bool?)a["enemy"]==false&&(bool?)a["train"]==false).Select(a=>new JObject{["id"]=a["id"].DeepClone(),["hp"]=a["hp"].DeepClone(),["maxHp"]=a["maxHp"].DeepClone(),["footMm"]=a["footRelativeToFrontMm"]?.DeepClone(),["physicalInstanceId"]=a["physicalInstanceId"]?.DeepClone(),["partyMoving"]=a["partyMoving"]?.DeepClone()})),["kyleShield"]=shield,["liveTaunts"]=taunts,["kyleAbsorbedTotal"]=absorbedTotal}.ToString(Newtonsoft.Json.Formatting.None));
     priorPhase=phase;previousShield=shield;previousTaunts=taunts;effectCount=effects.Count;
    }
   }
   if((string)lastHud["localParty"]?["phase"]=="Complete")completed=transfers;
   summary["boundedRunEnded"]=true;summary["termination"]=driver.Tick>=6000?"TICK_LIMIT":driver.Outcome==BasicCombatOutcome.Running?"ITERATION_LIMIT":"COMBAT_OUTCOME";
  }catch(Exception error){failure=error is TargetInvocationException invocation?invocation.InnerException??error:error;summary["failure"]=failure.ToString();}
  finally{
   summary["tick"]=driver?.Tick??0;summary["outcome"]=driver?.Outcome.ToString();summary["transfersRequested"]=transfers;summary["transfersCompleted"]=completed;summary["maximumLiveKyleTaunts"]=maxTaunts;summary["kyleAbsorbedTotal"]=absorbedTotal;summary["events"]=events;
   summary["bothDirectionsCompleted"]=completed==2;summary["threeLiveTauntsObserved"]=maxTaunts>=3;
   File.WriteAllText(output+"/observations.json",summary.ToString());if(lastState!=null)File.WriteAllText(output+"/last-state.json",lastState.ToString());if(lastHud!=null)File.WriteAllText(output+"/last-hud.json",lastHud.ToString());
   current=false;driver?.Retire();UnityEngine.Debug.Log("Local six-body exercise: "+output);
  }
  if(failure!=null)throw failure;
 }
 public static void Check(){
  const string folder="D:/Project/LASTRAIL/QA/KyleTauntThree-20261109/local-five-car-v01";
  var document=JObject.Parse(File.ReadAllText(folder+"/source.json"));
  var flags=BindingFlags.NonPublic|BindingFlags.Static;
  var replay=typeof(OrdinaryFiveCombatFactory).Assembly.GetType("LastRail.Battle.OrdinaryLocalExecutionReplay",true);
  replay.GetMethod("ReadInput",flags).Invoke(null,new object[]{JObject.Parse(File.ReadAllText(folder+"/../uninstalled-source.json"))});
  object input;
  try{input=replay.GetMethod("ReadInput",flags).Invoke(null,new object[]{document});}catch(TargetInvocationException e){throw e.InnerException??e;}
  Func<string,object> field=name=>input.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input);
  var original=(OrdinaryFiveStartInput)field("Original");var node=(OrdinaryNodeStart)field("Node");
  if(!original.Rail.LocalFiveCar||original.Rail.PhysicalInstanceIds.Count!=6||original.Rail.TotalLengthMm!=77000||original.Rail.TrackLengthMm!=6000000||original.Rail.InitialFrontArcMm!=97000||!original.Roof.Slots.All(s=>s.PhysicalOrder==1))throw new InvalidOperationException("Independent six-body party source mismatch.");
  var factory=OrdinaryFiveCombatFactory.FromNodeStart(original,node,(FirstPlayableScaling)field("Scaling"),node.ReadActions(original).StepTimeSpanTicks,(OrdinaryNodeAttempt)field("Attempt"));
  factory=(OrdinaryFiveCombatFactory)typeof(OrdinaryFiveCombatFactory).GetMethod("WithLiveSources",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(factory,new[]{field("GeometryJson")});
  factory=factory.WithBoardingSource((JObject)field("BoardingSource"),node.BoardingSourceHash);
  File.WriteAllText(folder+"/reader-check.json",new JObject{["scope"]="EXPLICIT_LOCAL_SOURCE_READER_AND_FACTORY_ONLY",["historicalInputRead"]=true,["readInput"]=true,["factoryBinding"]=true,["physicalHostIds"]=new JArray(original.Rail.PhysicalInstanceIds),["roofOrders"]=new JArray(original.Roof.Slots.Select(s=>s.PhysicalOrder)),["serverStartIssued"]=false,["playerGameplayVerified"]=false}.ToString());
 }
}
#endif
