using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns frozen rays/contact reservations only. Borrowed original Board/Session own
 // placement, generation, clock, next and all HP. Upper root stages every change together.
 internal sealed class BMiraFirstProjectileOwner {
  internal const string TargetPolicy="basic-target-reacquisition-approved-20261015";
  readonly BMiraNativeTrialBinding context;readonly BDirectAttackInput input;readonly int capacity;
  readonly JObject authoring,initialNative;JObject native,lastObservation;FriendlyProjectileOwner flight;
  readonly JArray shots=new JArray(),contacts=new JArray();bool ended;
  internal bool Flying=>shots.Any(s=>(string)s["status"]=="FLYING");
  internal long Revision=>(long)native["observation"]["revision"];
  internal bool Ended=>ended;
  internal bool Equipped=>context.Equipped;
  internal BMiraFirstProjectileOwner(BDirectAttackBasicLoopBinding binding,BBoardingEventsNodeSnapshot node,BMiraNativeTrialBinding context,JObject authoring,JObject state,int capacity,BEnemyBoardingOwner board,BDirectAttackBuffSession session){
   this.context=context??throw new ArgumentNullException(nameof(context));input=binding.Composition.Attack;this.capacity=capacity;
   this.authoring=(JObject)authoring.DeepClone();initialNative=(JObject)state.DeepClone();native=(JObject)state.DeepClone();
   var original=context.Mira;var placement=original["actor"]["placement"];var stats=context.Stats;
   BuffWire.Need(context.AccountId==(string)node.OriginalSource.Capture()["origin"]["accountId"]&&context.RunId==node.OriginalSource.RunId,"mira_first_native_origin");
   var mira=input.Entities["op_mira"];BuffWire.Need(context.Equipped?input.SchemaVersion==BDirectAttackInput.EquippedMiraInputVersion:input.SchemaVersion!=BDirectAttackInput.EquippedMiraInputVersion,"mira_context_input_version");
   BuffWire.Need(placement is JObject&&mira.Slot==(int)placement["physicalSlot"]&&input.Formation.Slots[mira.Slot].CarriageId==(string)placement["carriageInstanceId"],"mira_first_native_placement");
   BuffWire.Need(JToken.DeepEquals(original["refit"]["departure"],binding.Composition.Departure)&&BDirectAttackInput.Digest(original["ownedProfile"])==(string)input.Capture()["ownedProfileHash"],"mira_first_native_departure");
   BuffWire.Need(JToken.DeepEquals(mira.Attack.Snapshot(),stats["attack"])&&mira.MaxHp==(int)stats["maxHp"]&&JToken.DeepEquals(mira.Defense.Snapshot(),stats["defense"])&&mira.Interval==checked((long)stats["basicIntervalMs"]*1000),"mira_first_native_stats");
   foreach(var key in new[]{"attackSources","defenseSources","effects","weakening","penetration"})BuffWire.Need(((JArray)mira.Original[key]).Count==0,"mira_first_no_effect");
   BuffWire.Need(((JObject)mira.Original["reductions"]).Properties().All(p=>p.Value is JArray a&&a.Count==0),"mira_first_no_reductions");
   BuffWire.Need(BDirectAttackInput.Digest(authoring["profile"])==(string)authoring["profileHash"]&&BDirectAttackInput.Digest(authoring["layout"])==(string)authoring["layoutHash"],"mira_first_authoring_pin");
   var layout=authoring["layout"];BuffWire.Need((string)layout["source"]["id"]=="op_mira"&&(int)layout["source"]["slot"]==mira.Slot,"mira_first_authoring_source");
   foreach(var car in (JArray)layout["cars"])BuffWire.Need(input.Formation.Slots[(int)car["slot"]].CarriageId==(string)car["id"],"mira_first_authoring_car");
   var targets=(JArray)layout["targets"];var enemies=node.OriginalSource.Waves.SelectMany(w=>w.Instances).ToArray();
   BuffWire.Need(targets.Count==enemies.Length&&new HashSet<string>(targets.Select(t=>(string)t["id"])).SetEquals(enemies.Select(e=>e.InstanceId)),"mira_first_authoring_targets");
   foreach(var target in targets)BuffWire.Need(enemies.Single(e=>e.InstanceId==(string)target["id"]).DefinitionId==(string)target["definitionId"],"mira_first_authoring_definition");
   ValidateState(state,0,1);lastObservation=Project(state,board,session);flight=new FriendlyProjectileOwner("mira-first:"+context.RunId+":"+node.OriginalSource.NodeId,capacity);
  }
  BMiraFirstProjectileOwner(BMiraFirstProjectileOwner o){context=o.context;input=o.input;capacity=o.capacity;authoring=o.authoring;initialNative=o.initialNative;native=(JObject)o.native.DeepClone();lastObservation=(JObject)o.lastObservation.DeepClone();flight=o.flight.Clone();foreach(var s in o.shots)shots.Add(s.DeepClone());foreach(var c in o.contacts)contacts.Add(c.DeepClone());ended=o.ended;}
  internal BMiraFirstProjectileOwner Clone()=>new BMiraFirstProjectileOwner(this);
  internal void CheckAuthoring(JObject value)=>BuffWire.Need(JToken.DeepEquals(authoring,value),"mira_first_authoring_changed");
  void ValidateState(JObject state,long tick,long revision){
   BuffWire.Need(state!=null&&(string)state["version"]==MiraJudgementSceneAdapter.NativeStateVersion&&state.Count==3&&state["poses"] is JArray,"mira_first_native_state");
   var observation=BuffWire.Object(state["observation"]);
   BuffWire.Need((long)observation["tick"]==tick&&(long)observation["revision"]==revision&&(string)observation["version"]==MiraJudgementSceneAdapter.ObservationVersion
    &&(string)observation["profileHash"]==(string)authoring["profileHash"]&&(string)observation["layoutHash"]==(string)authoring["layoutHash"]
    &&JToken.DeepEquals(observation["profile"],authoring["profile"])&&BDirectAttackInput.Digest(observation["basis"])==(string)observation["basisHash"]
    &&(string)observation["basisHash"]==(string)initialNative["observation"]["basisHash"],"mira_first_native_frame_pin");
   BuffWire.Need(JToken.DeepEquals(new JArray(((JArray)observation["targets"]).Select(t=>t["id"])),new JArray(((JArray)authoring["layout"]["targets"]).Select(t=>t["id"]))),"mira_first_native_target_set");
  }
  JObject Project(JObject state,BEnemyBoardingOwner board,BDirectAttackBuffSession session){
   var observation=(JObject)state["observation"].DeepClone();var source=observation["source"];
   BuffWire.Need((string)source["id"]=="op_mira"&&(int)source["slot"]==input.Entities["op_mira"].Slot,"mira_first_pose_placement");
   source["generation"]=session.ProjectileTargetGeneration("op_mira");
   foreach(var target in (JArray)observation["targets"]){string id=(string)target["id"];int slot=board.Slot(id);target["generation"]=board.Generation(id);if(slot>=0)target["slot"]=slot;target["eligible"]=board.Targetable(id)&&session.MiraArrivalEligible(id);}
   return observation;
  }
  internal JObject ProjectForFire(JObject state,BEnemyBoardingOwner board,BDirectAttackBuffSession session)=>Project(state,board,session);
  internal JObject Frame(JObject state,BEnemyBoardingOwner board,BDirectAttackBuffSession session){ValidateState(state,session.Tick,Revision+1);flight.AdvanceTo(session.Tick);return Project(state,board,session);}
  internal string Select(JObject observation,BEnemyBoardingOwner board,BDirectAttackBuffSession session,string preferred=null,long preferredGeneration=-1){
   if(session.Hp("op_mira")==0)return null;
   bool Eligible(string id){if(!board.Targetable(id)||!session.MiraArrivalEligible(id))return false;try{MiraJudgementGeometry.CreateRay(observation,BDirectAttackInput.Digest(observation),id);return true;}catch(InvalidOperationException){return false;}}
   if(preferred!=null&&board.Generation(preferred)==preferredGeneration&&Eligible(preferred))return preferred;
   return board.TargetableOrder().FirstOrDefault(Eligible);
  }
  internal void Launch(string id,string target,JObject observation,BEnemyBoardingOwner board,BDirectAttackBuffSession session,Func<long> next){
   var rays=new List<(string target,JObject ray,string branch,string kind,int bps)>();
   var ray=MiraJudgementGeometry.CreateRay(observation,BDirectAttackInput.Digest(observation),target);rays.Add((target,ray,"main","MAIN",10000));
   if(context.CoefficientBps>0){
    var anchor=(JArray)ray["anchorMm"];System.Numerics.BigInteger Square(JToken t){System.Numerics.BigInteger sum=0;for(int i=0;i<3;i++){var d=(System.Numerics.BigInteger)(long)t["anchorMm"][i]-(long)anchor[i];sum+=d*d;}return sum;}
    var candidates=((JArray)observation["targets"]).Where(t=>(string)t["id"]!=target&&(bool)t["eligible"]&&Square(t)<=36000000).OrderBy(Square).ThenBy(t=>(string)t["id"],StringComparer.Ordinal);
    foreach(var candidate in candidates){string secondary=(string)candidate["id"];try{var r=MiraJudgementGeometry.CreateRay(observation,BDirectAttackInput.Digest(observation),secondary);rays.Add((secondary,r,"secondary-"+rays.Count,"SECONDARY",context.CoefficientBps));}catch(InvalidOperationException){continue;}if(rays.Count==3)break;}
   }
   BuffWire.Need(shots.Count+rays.Count<=capacity,"mira_first_shot_capacity");var attack=session.CommitMiraLaunch(id,target,context.Critical);long sequence=next();
   var branches=new JArray(rays.Select(r=>new JObject{["branchId"]=r.branch,["pathId"]=id+":"+r.branch+":ray",["kind"]=r.kind,["contacts"]=new JArray(new JObject{["contactId"]=r.branch+"-hit",["targetId"]=r.target,["targetGeneration"]=board.Generation(r.target),["ordinal"]=0,["coefficientBps"]=r.bps})}));
   // Preserve the first-profile path contract byte-for-byte.
   if(!context.Equipped)branches[0]["pathId"]=id+":ray";
   flight.Launch(new JObject{["version"]=FriendlyProjectileOwner.Version,["opportunityId"]=id,["sourceId"]="op_mira",["sourceGeneration"]=session.ProjectileTargetGeneration("op_mira"),["tick"]=session.Tick,["sequence"]=sequence,["attack"]=attack.DeepClone(),
    ["geometry"]=new JObject{["version"]=MiraJudgementSceneAdapter.ObservationVersion,["revision"]=observation["revision"].DeepClone(),["hash"]=BDirectAttackInput.Digest(observation)},["branches"]=branches},true);
   foreach(var r in rays){var shot=new JObject{["id"]=r.branch=="main"?id:id+":"+r.branch,["targetId"]=r.target,["targetGeneration"]=board.Generation(r.target),["sourceGeneration"]=session.ProjectileTargetGeneration("op_mira"),["launchTick"]=session.Tick,["sequence"]=sequence,["attack"]=attack.DeepClone(),["ray"]=r.ray,["rayHash"]=BDirectAttackInput.Digest(r.ray),["status"]="FLYING"};
    if(context.Equipped){shot["opportunityId"]=id;shot["branchId"]=r.branch;shot["kind"]=r.kind;shot["coefficientBps"]=r.bps;}shots.Add(shot);}
  }
  internal void Resolve(JObject state,BEnemyBoardingOwner board,BDirectAttackBuffSession session,Func<long> next,List<BDirectAttackBuffReceipt> hits){
   var observation=Project(state,board,session);
   foreach(JObject shot in shots.Where(s=>(string)s["status"]=="FLYING"&&(long)s["launchTick"]<session.Tick)){
    observation=Project(state,board,session);string opportunity=(string)shot["opportunityId"]??(string)shot["id"],branch=(string)shot["branchId"]??"main";
    long launch=(long)shot["launchTick"];var trace=MiraJudgementGeometry.Trace((JObject)shot["ray"],(string)shot["rayHash"],lastObservation,observation,checked(((long)lastObservation["tick"]-launch)*input.StepTimeSpanTicks/10),checked((session.Tick-launch)*input.StepTimeSpanTicks/10));
    string kind=(string)trace["kind"],id=(string)shot["id"],target=(string)shot["targetId"];if(kind=="NONE")continue;
    if(kind=="BLOCKED"||kind=="EXPIRED"){flight.StopBranch(opportunity,branch,kind=="BLOCKED"?"WALL":"EXPIRED",next());shot["status"]=kind=="BLOCKED"?"BLOCKED":"MISS";}
    else {
     bool eligible=kind=="HIT"&&board.Targetable(target)&&board.Generation(target)==(long)shot["targetGeneration"]&&session.MiraArrivalEligible(target);
     var contact=flight.ResolveContact(new JObject{["version"]=FriendlyProjectileOwner.Version,["opportunityId"]=opportunity,["branchId"]=branch,["contactId"]=branch+"-hit",["tick"]=session.Tick,["sequence"]=next(),["targetGeneration"]=board.Generation(target),["eligible"]=eligible,["arrival"]=eligible?(JToken)session.MiraArrival(target):JValue.CreateNull()});
     shot["status"]=eligible?"HIT":"MISS";
     if(contact.NewlyResolved&&eligible){var receipt=session.CommitMiraContact(id+":hp",opportunity,target,contact,(string)shot["kind"]=="SECONDARY"?(int)shot["coefficientBps"]:10000);hits.Add(receipt);contacts.Add(receipt.Snapshot());}
    }
    shot["contact"]=trace;
   }
   native=(JObject)state.DeepClone();lastObservation=observation;
  }
  internal void Hold(bool held)=>flight.SetHeld(held);
  internal void Terminal(Func<long> next){if(ended)return;flight.End("COMBAT_END",next());ended=true;foreach(var s in shots.Where(s=>(string)s["status"]=="FLYING"))s["status"]="INTERRUPTED";}
  internal JObject Capture()=>new JObject{["context"]=context.Capture(),["contextHash"]=context.Hash,["targetPolicy"]=TargetPolicy,["preparationMode"]="SAME_AUTHORITY_STEP_NO_ADDED_DELAY",["authoring"]=authoring.DeepClone(),["initialNativeState"]=initialNative.DeepClone(),["nativeState"]=native.DeepClone(),["lastObservation"]=lastObservation.DeepClone(),["shots"]=shots.DeepClone(),["contacts"]=contacts.DeepClone(),["flight"]=flight.Capture(),["ended"]=ended};
 }
}